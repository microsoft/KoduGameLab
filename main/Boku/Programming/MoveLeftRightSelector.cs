// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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

using Boku.Base;
using Boku.Common;
using Boku.SimWorld;
using Boku.SimWorld.Chassis;

namespace Boku.Programming
{
    /// <summary>
    /// Moves a bot up or down in response to gamestick direction.
    /// 
    /// 
    /// 
    /// ARCHIVED!!!
    /// 
    /// 
    /// 
    /// </summary>
    public class MoveLeftRightSelector : Selector
    {
        public override ProgrammingElement Clone()
        {
            MoveLeftRightSelector clone = new MoveLeftRightSelector();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(MoveLeftRightSelector clone)
        {
            base.CopyTo(clone);
        }

        public override void Used(bool newUse)
        {
        }

        public override ActionSet ComposeActionSet(Reflex reflex, GameActor gameActor)
        {
            ClearActionSet(actionSet);
            UpdateCanBlend(reflex);

            if (!reflex.targetSet.AnyAction)
                return actionSet;

            Vector2 valueStick = Vector2.Zero;
            if (reflex.targetSet.Param != null && reflex.targetSet.Param is Vector2)
            {
                // Read the gamepad input
                valueStick = (Vector2)reflex.targetSet.Param;

                // Flip gamepad axes for buttons and triggers, since they report in the y-axis.
                foreach (Filter filter in reflex.Filters)
                {
                    if (filter is GamePadButtonFilter || filter is GamePadTriggerFilter)
                        valueStick = new Vector2(valueStick.Y, -valueStick.X);
                }

                valueStick.Y = 0;
            }
            else
            {
                valueStick = new Vector2(0.75f, 0);
            }

            // Pass stick value to modifiers (quickly, slowly, left, right, etc)
            Vector3 stick3d = new Vector3(valueStick.X, 0, 0);
            bool apply = reflex.ModifyHeading(gameActor, Modifier.ReferenceFrames.Local, ref stick3d);

            if (apply)
            {
                float strength = stick3d.Length();

                Vector3 velocity = new Vector3(gameActor.Movement.Velocity.X, gameActor.Movement.Velocity.Y, 0);

                float angle = 0;

                float maxAngle = MathHelper.Pi / 2.01f;

                // Special handling of the "toward" turn modifier, since it doesn't have enough information to know
                // which way "toward" actually is for an actor.
                if (reflex.targetSet.Nearest != null && reflex.HasModifier("modifier.turntoward"))
                {
                    SensorTarget target = reflex.targetSet.Nearest;

                    Vector3 toTarget = target.Direction;
                    toTarget.Z = 0;
                    toTarget.Normalize();

                    angle = MyMath.ZRotationFromDirection(toTarget);

                    Vector3 unit = new Vector3((float)Math.Cos(angle), (float)Math.Sin(angle), 0);

                    // radius should be from object
                    actionSet.AddAttractor(Action.AllocAttractor(target.Range, unit * 0.001f, target.GameThing, reflex, specialInstruction: BaseAction.SpecialInstruction.MatchVectorScale | BaseAction.SpecialInstruction.EnforceMinScale), 0.4f);
                }
                else
                {
                    // Dampen sensitivity for small stick movements.
                    strength *= strength;

                    if (strength > 0.001f)
                    {
                        // Scale the strength so that the speed modifiers have a visible effect.
                        strength *= 0.3f;

                        // See if the modifiers want to give us an absolute direction in which to turn.
                        Vector3 modifiedDirection = Vector3.Zero;
                        reflex.ModifyHeading(gameActor, Modifier.ReferenceFrames.World, ref modifiedDirection);

                        if (modifiedDirection != Vector3.Zero)
                        {
                            // Yes, we have an absolute turn direction.
                            angle = MyMath.ZRotationFromDirection(modifiedDirection);

                            // Check to see whether we're already facing this direction.
                            if (Math.Abs(angle - gameActor.Movement.RotationZ) < 0.001f)
                                apply = false;

                            // Put the angle into the local reference frame so that the code below can act on it.
                            // The actor's rotation will be added back in later.
                            angle -= gameActor.Movement.RotationZ;
                        }
                        else
                        {
                            // For relative turning angle, start with the max angle possible in the desired direction. It will be
                            // reduced according to stick input strength in the code below.
                            angle = maxAngle * -Math.Sign(stick3d.X);
                        }

                        if (apply)
                        {
                            // If the actor is moving very slow or standing still, then build a turn vector who's angle
                            // is derived from the strength of the stick input.
                            angle = angle * strength;

                            // Put the angle into the world reference frame.
                            angle += gameActor.Movement.RotationZ;

                            // cap it within 0-pi*2;
                            angle = (angle + MathHelper.TwoPi) % (MathHelper.TwoPi);

                            Vector3 unit = new Vector3((float)Math.Cos(angle), (float)Math.Sin(angle), 0);

                            // radius should be from object; none in this case
                            actionSet.AddAttractor(Action.AllocAttractor(1.0f, unit * 0.001f, null, reflex, specialInstruction: BaseAction.SpecialInstruction.MatchVectorScale | BaseAction.SpecialInstruction.EnforceMinScale), 0.0f);
                        }
                    }
                }
            } // apply

            return actionSet;
        }

        public override bool ActorCompatible(GameActor gameActor)
        {
            if (gameActor != null && !gameActor.Chassis.HasFacingDirection)
                return false;

            return base.ActorCompatible(gameActor);
        }
    }
}
