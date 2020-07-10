// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
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
    public class RotationFilter : Filter
    {
        [XmlAttribute]
        public Directions direction;

        public override ProgrammingElement Clone()
        {
            RotationFilter clone = new RotationFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(RotationFilter clone)
        {
            base.CopyTo(clone);
            clone.direction = this.direction;
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            return true; // not the right type, don't effect the filtering
        }

        public override bool MatchAction(Reflex reflex, out object param)
        {
            bool result = false;
            param = null;

            TouchGestureFilter touchFilt = reflex.Data.GetFilterByType(typeof(TouchGestureFilter)) as TouchGestureFilter;

            if (touchFilt != null)
            {
                float rotation = touchFilt.DeltaRotation;

                for (int i = 0; i < reflex.Filters.Count; ++i)
                {
                    Filter filter = reflex.Filters[i];

                    if (!(filter is RotationFilter))
                    {
                        continue;
                    }

                    RotationFilter rotateFilt = filter as RotationFilter;

                    // does the rotation direction violate any of the rotation filters?
                    if (!rotateFilt.IsRotationValid(rotation))
                    {
                        return false;
                    }
                }

                // If we've gotten here we're good.  The rotation is in the
                // right direction for the filter so return true.

                result = true;

            }

            return result;
        }

        public bool IsRotationValid(float rotationRad)
        {
            //if not specified, consider it valid
            if (direction == Directions.None)
            {
                return true;
            }

            //otherwise, check that the direction matches the input
            if ((direction & Directions.Clockwise) != 0 && rotationRad > 0)
            {
                return true;
            }

            if ((direction & Directions.Counterclockwise) != 0 && rotationRad < 0)
            {
                return true;
            }

            return false;
        }
    }
}
