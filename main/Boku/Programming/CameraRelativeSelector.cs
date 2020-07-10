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

namespace Boku.Programming
{
    /// <summary>
    /// this selector will calculate a vector that provides movement based 
    /// upon the input vector (from a stick) and the camera view vector.
    /// </summary>
    public class CameraRelativeSelector : Selector
    {
        private ActionSet movementSet = new ActionSet();


        [XmlAttribute]
        public float strength;

        public CameraRelativeSelector()
        {
        }
        public override ProgrammingElement Clone()
        {
            CameraRelativeSelector clone = new CameraRelativeSelector();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(CameraRelativeSelector clone)
        {
            base.CopyTo(clone);
            clone.strength = this.strength;
        }

        public override void Reset(Reflex reflex)
        {
            ClearActionSet(movementSet);
            base.Reset(reflex);
        }

        public override ActionSet ComposeActionSet(Reflex reflex, GameActor gameActor)
        {
            ClearActionSet(movementSet);
            UpdateCanBlend(reflex);

            if (!reflex.targetSet.Action)
                return movementSet;

            // calculate a vector relative to the camera and input stick
            // see "Game Programming Gems 2", "Classic Super Mario 64 Third-Person Control and Animation"
            //
            Vector2 valueStick = Vector2.Zero;
            if (reflex.targetSet.Param != null && reflex.targetSet.Param is Vector2)
            {
                valueStick = (Vector2)reflex.targetSet.Param;
            }

            Vector3 cameraDir = InGame.inGame.Camera.ViewDir;
            cameraDir.Z = 0.0f;
            if (cameraDir != Vector3.Zero)
            {
                cameraDir.Normalize();
            }

            Vector3 cameraRight = new Vector3(cameraDir.Y, -cameraDir.X, 0.0f);

            Vector3 value = new Vector3((valueStick.X * cameraRight.X) + (valueStick.Y * cameraDir.X),
                    (valueStick.X * cameraRight.Y) + (valueStick.Y * cameraDir.Y),
                    0.0f);
            value *= strength;

            bool apply = reflex.ModifyHeading(gameActor, Modifier.ReferenceFrames.World, ref value);

            if (apply)
            {
                if (reflex.Actuator != null && 
                        (reflex.Actuator.category & Actuator.Category.Action) == Actuator.Category.Action)
                {
                    // support it as action even it it supports direction also
                    movementSet.AddActionTarget(AllocEffector(0.0f, value, null, reflex), 0.0f);
                }
                else
                {
                    // radius should be from object
                    movementSet.AddAttractor(AllocEffector(value.Length() * 3.0f, value, null, reflex), 0.0f);
                }
            }

            return movementSet;
        }

        public override void Used(bool newUse)
        {
        }

        public override bool ReflexCompatible(ReflexData clip, ProgrammingElement replacedElement)
        {
            bool compatible = false;
            Actuator actuator = clip.Actuator;
            Sensor sensor = clip.Sensor;

            if (actuator != null && sensor != null)
            {
                Actuator.Category category = Actuator.Category.Direction | Actuator.Category.TargetlessDirection;
                if ((actuator.category & category) != 0 &&
                        (sensor.category & Sensor.Category.GamePad) != 0)
                {
                    compatible = true;
                    // check the filters for compatibility against our type
                    for (int indexFilter = 0; indexFilter < clip.Filters.Count; indexFilter++)
                    {
                        Filter filter = clip.Filters[indexFilter] as Filter;
                        if (filter != null)
                        {
                            if (!filter.ElementCompatible(clip, this))
                            {
                                compatible = false;
                                break;
                            }
                        }
                    }
                }
            }

            return compatible;
        }
    }
}
