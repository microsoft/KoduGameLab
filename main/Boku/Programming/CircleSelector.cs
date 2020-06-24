using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using System.Xml;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;

using Boku.Base;
using Boku.Common;
using Boku.SimWorld.Chassis;

namespace Boku.Programming
{
    /// <summary>
    /// Circle the target
    /// 
    /// this selector will do one of two things.  
    /// First, if the distance to the first Action Thing is greater than 
    /// the circling distance, then it will move toward the first Action Thing.  
    /// Otherwise if the distance is equal or less than the circling distance, 
    /// it will calculate a set of waypoints that circle the target and 
    /// start navigating those points.
    /// 
    /// NOTE - mattmac - 1.2.2008 - this doesn't work when the thing you're trying to circle is moving
    /// waypoints are not updated until the target moves beyond a threshold
    /// Basically, it draws a circle around the character and then follows it blindly until the
    /// character moves beyond a certain distance. The waypoints should be edited every frame when
    /// the target is moving, if waypoints are indeed the best approach to take.
    /// There is no easy answer; the target may be faster than the circler, and the target
    /// will necessarily go out of the circler's sight periodically. Cut?
    /// 
    /// Rewritten. It's now much shorter, doesn't have those ridiculous waypoints, and 
    /// actually works. *** 12.9.08
    /// </summary>
    public class CircleSelector : Selector
    {
        [XmlAttribute]
        public float strength;
        [XmlAttribute]
        public float distance;
        [XmlAttribute]
        public float accuracy;

        [XmlIgnore]
        private bool CCW = true;
        [XmlIgnore]
        private float radiusScale = 1.0f;
        [XmlIgnore]
        private bool controlZ = false;

        /// <summary>
        /// How far outside the circle to try to get to avoid obstacles. Accumulated
        /// over runtime.
        /// </summary>
        private float escapeRadius = 0.0f;

        public CircleSelector()
        {
        }
        public override ProgrammingElement Clone()
        {
            CircleSelector clone = new CircleSelector();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(CircleSelector clone)
        {
            base.CopyTo(clone);
            clone.strength = this.strength;
            clone.distance = this.distance;
            clone.accuracy = this.accuracy;
        }

        public override void Reset(Reflex reflex)
        {
            base.Reset(reflex);
        }

        public override void Fixup(Reflex reflex)
        {
            base.Fixup(reflex);

            radiusScale = 1.0f;
            for (int imod = 0; imod < reflex.Modifiers.Count; ++imod)
            {
                CircleDistanceModifier cdMod = reflex.Modifiers[imod] as CircleDistanceModifier;
                if (cdMod != null)
                {
                    radiusScale *= cdMod.radiusScale;
                }
            }

            Task task = reflex.Task;
            controlZ = true;
            for (int iref = 0; iref < task.reflexes.Count; ++iref)
            {
                Reflex otherReflex = task.reflexes[iref] as Reflex;
                if ((otherReflex != null) && (otherReflex != reflex))
                {
                    Selector otherSelector = otherReflex.Selector;
                    if ((otherSelector is MoveDownSelector)
                        || (otherSelector is MoveUpDownSelector)
                        || (otherSelector is MoveUpSelector)
                        || (otherSelector is FollowWaypointsSelector))
                    {
                        controlZ = false;
                        break;
                    }
                }
            }
        }

        public override ActionSet ComposeActionSet(Reflex reflex, GameActor gameActor)
        {
            ClearActionSet(actionSet);
            UpdateCanBlend(reflex);

            if (!reflex.targetSet.AnyAction)
                return actionSet;

            // The targetSet is in order by distance use the closest one.
            // Should be null if 0 count in target set.
            SensorTarget target = reflex.targetSet.Nearest;

            if (target != null && !(target.GameThing is NullActor))
            {
                // calculate a vector toward target
                Vector3 toTarget = target.Position - gameActor.Movement.Position;

                float targetZ = target.GameThing.WorldCollisionCenter.Z;
                float deltaZ = targetZ - gameActor.WorldCollisionCenter.Z;

                toTarget.Z = 0;
                float dist = toTarget.Length();

                float targetRadius = 0;
                if (target.GameThing != null && !(target.GameThing is NullActor))
                {
                    targetRadius = target.GameThing.BoundingSphere.Radius;
                }
                float radius = targetRadius + gameActor.BoundingSphere.Radius;
                radius = Math.Max(radius, 1.0f);
                radius *= 8.0f * radiusScale;
                radius *= (1.0f + escapeRadius);

                Vector3 toTargetUnit = Vector3.Normalize(toTarget);

                // Calculate the desired angle relative to the vector toward target.
                float localTheta;
                if (dist > radius)
                {
                    localTheta = (float)Math.Cos(radius / dist);
                }
                else
                {
                    localTheta = -(float)Math.Cos(dist / radius * Math.PI * 0.5);
                }

                // Get the vector from the local desired angle.
                Vector3 localDir = new Vector3(
                    (float)Math.Cos(localTheta),
                    (float)Math.Sin(localTheta),
                    0);

                // Scale the local vector by the strength setting from CardSpace.
                localDir *= strength;

                // Modify the local vector (left, right, quickly, slowly, etc).
                bool apply = reflex.ModifyHeading(gameActor, Modifier.ReferenceFrames.Local, ref localDir);

                if (apply)
                {
                    // Because CCW is considered "left".
                    if (CCW)
                        localDir.X *= -1;

                    // Get the modified desired angle.
                    localTheta = MyMath.ZRotationFromDirection(localDir);

                    // Combine with the rotation of vector toward target relative to the actor.
                    float thetaToTarget = MyMath.ZRotationFromDirection(toTargetUnit);
                    float finalTheta = thetaToTarget + localTheta - MathHelper.PiOver2;

                    // cap it within 0-pi*2;
                    finalTheta = (finalTheta + MathHelper.TwoPi) % (MathHelper.TwoPi);

                    if (controlZ
                        && (gameActor.Chassis is FloatInAirChassis
                            || gameActor.Chassis is SwimChassis
                            || gameActor.Chassis is SaucerChassis))
                    {
                        gameActor.Chassis.TargetAltitude = targetZ;
                    }
                    else
                    {
                        deltaZ = 0;
                    }

                    // Build a heading vector in world space.
                    Vector3 finalDir = new Vector3(
                        (float)Math.Cos(finalTheta),
                        (float)Math.Sin(finalTheta),
                        deltaZ);

                    finalDir.Normalize();

                    CheckEscape(reflex, gameActor, target.Position, target.GameThing, finalDir);

                    finalDir *= localDir.Length();

                    actionSet.AddAction(Action.AllocVelocityAction(reflex, finalDir, autoTurn:true));
                }
            }

            return actionSet;
        }

        public override void Used(bool newUse)
        {
        }

        /// <summary>
        /// Look to see if we are blocked from where we are trying to go. If
        /// we are, we'll expand out our circle radius until we are no longer
        /// blocked, and then slowly shrink it back again.
        /// </summary>
        /// <param name="gameActor"></param>
        private void CheckEscape(Reflex reflex, GameActor gameActor, Vector3 targetPos, GameThing target, Vector3 finalDir)
        {
            Vector3 from = gameActor.WorldCollisionCenter;

            bool escaping = false;
            float escapeSpeed = 10.0f;
            GameActor.BlockedInfo blockInfo = new GameActor.BlockedInfo();
            if (gameActor.Blocked(
                from + finalDir * gameActor.Chassis.SafeStoppingDistance(),
                ref blockInfo))
            {
                gameActor.AddLOSLine(from, blockInfo.Contact, new Vector4(1.0f, 0, 0, 1.0f));
                if (!reflex.ModifierParams.HasTurn)
                {
                    CCW = !CCW;
                }
                else
                {
                    float kMinEscape = -0.25f;

                    targetPos.Z = from.Z;

                    if ((escapeRadius > kMinEscape) && !gameActor.Blocked(target, targetPos, ref blockInfo))
                    {
                        escapeRadius -= escapeSpeed * Time.GameTimeFrameSeconds;
                        escaping = true;
                    }
                    else
                    {
                        escapeRadius += escapeSpeed * Time.GameTimeFrameSeconds;
                        escaping = true;
                    }
                }
            }
            if (!escaping)
            {
                if (escapeRadius > 0)
                {
                    escapeRadius = Math.Max(0.0f, escapeRadius - escapeSpeed * 0.5f * Time.GameTimeFrameSeconds);
                }
                else
                {
                    escapeRadius = Math.Min(0.0f, escapeRadius + escapeSpeed * 0.5f * Time.GameTimeFrameSeconds);
                }
            }
            float kMaxEscape = 25.0f;
            if (escapeRadius > kMaxEscape)
            {
                escapeRadius = kMaxEscape;
            }
        }
    }
}
