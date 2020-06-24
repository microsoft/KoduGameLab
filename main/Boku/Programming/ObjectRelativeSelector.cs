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

namespace Boku.Programming
{
    /// <summary>
    /// this selector will calculate a vector that provides movement based 
    /// upon the input vector (from a stick) and the camera view vector.
    /// </summary>
    public class ObjectRelativeSelector : Selector
    {
        [XmlAttribute]
        public float strength;

        [XmlIgnore]
        public Vector2 defaultStickVector = Vector2.Zero;

        [XmlAttribute]
        public string defaultStick
        {
            get { return defaultStickVector.ToString(); }
            set { Utils.Vector2FromString(value, out defaultStickVector, Vector2.Zero); }
        }

        public ObjectRelativeSelector()
        {
        }

        public override ProgrammingElement Clone()
        {
            ObjectRelativeSelector clone = new ObjectRelativeSelector();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(ObjectRelativeSelector clone)
        {
            base.CopyTo(clone);
            clone.strength = this.strength;
            clone.defaultStickVector = this.defaultStickVector;
        }

        public override void Reset(Reflex reflex)
        {
            base.Reset(reflex);
        }

        public override ActionSet ComposeActionSet(Reflex reflex, GameActor gameActor)
        {
            ClearActionSet(actionSet);
            UpdateCanBlend(reflex);

            if (!reflex.targetSet.AnyAction)
                return actionSet;

            Vector2 valueStick = defaultStickVector;
            if (reflex.targetSet.Param != null && reflex.targetSet.Param is Vector2)
            {
                // Read the gamepad input
                valueStick = (Vector2)reflex.targetSet.Param;
            }

            Vector3 actorDir = gameActor.Movement.Heading;
            actorDir.Z = 0;

            // The direction we want to be moving.
            Vector3 targetDirection = Vector3.Zero;

            bool isForwardSel = this.Categories.Get((int)BrainCategories.ForwardSelector);

            Vector3 cameraRight = new Vector3(actorDir.Y, -actorDir.X, 0.0f);
            targetDirection = new Vector3((valueStick.X * cameraRight.X) + (valueStick.Y * actorDir.X),
                                (valueStick.X * cameraRight.Y) + (valueStick.Y * actorDir.Y),
                                 0.0f);

            // We used to normalize here but that makes movement binary instead of analog, ie
            // it acts as if the stick is fully over all the time instead of allowing for 
            // finer control.
            /*
            value.Normalize();
            // If value was 0,0,0 we'll get NaNs when we normalize so check for this 
            // and restore valid values for value.  (sorry, couldn't resist)
            if (float.IsNaN(value.X))
            {
                value = Vector3.Zero;
            }
            */

            targetDirection *= strength;

            bool absoluteDirection = false;

            // See if the modifiers want to give us an absolute direction.
            Vector3 modifiedDirection = Vector3.Zero;
            bool apply = reflex.ModifyHeading(gameActor, Modifier.ReferenceFrames.World, ref modifiedDirection);
            if (apply && modifiedDirection != Vector3.Zero)
            {
                // Yes, we have an absolute direction. We still need to apply the non-frame modifiers (speed, color, etc)
                float stickPower = targetDirection.Length();
                targetDirection = modifiedDirection;
                if (stickPower != 0)
                    targetDirection *= stickPower;
                apply = reflex.ModifyHeading(gameActor, Modifier.ReferenceFrames.Local, ref targetDirection);
                absoluteDirection = true;
            }
            else
            {
                // No absolute direction given, we'll use the input given to us by the brain.
                apply = reflex.ModifyHeading(gameActor, Modifier.ReferenceFrames.World, ref targetDirection);
            }

            if (apply)
            {
                // TODO (scoy) Move all the Rover specific stuff to the Rover class.  None of this should be here.
                // SGI_MOD also apply the speed modifier for hills for the Rover
                Boku.SimWorld.Chassis.RoverChassis RovChassis = gameActor.Chassis as Boku.SimWorld.Chassis.RoverChassis;
                if (RovChassis != null)
                {
                    RovChassis.ModifyHeading(ref targetDirection);
                }

                if (!reflex.IsUserControlled)
                {
                    if (absoluteDirection)
                    {
                        actionSet.AddAction(Action.AllocVelocityAction(reflex, targetDirection, autoTurn: true));
                    }
                    else
                    {
                        // Move Forward
                        // TODO (scoy) Is this controllable by stick?  If so, 1.0 should be replaced by stick value.
                        actionSet.AddAction(Action.AllocSpeedAction(reflex, 1.0f));
                    }
                }
                else
                {
                    // If this actor is user controlled, we need to rotate the heading to make it camera relative.
                    // Unless the direction given is an absolute one, like "North".
                    // Note, we need to test for Zero here otherwise we add an action even if the user isn't giving
                    // any inputs.
                    if (targetDirection != Vector3.Zero)
                    {
                        if (!absoluteDirection && !isForwardSel && reflex.Task.IsUserControlled)
                        {
                            Matrix mat = Matrix.CreateRotationZ(InGame.inGame.Camera.Rotation - gameActor.Movement.RotationZ);
                            targetDirection = Vector3.TransformNormal(targetDirection, mat);
                        }

                        actionSet.AddAction(Action.AllocVelocityAction(reflex, targetDirection, autoTurn: true));
                    }

                }
            }

            return actionSet;
        }

        public override void Used(bool newUse)
        {
        }
    }
}
