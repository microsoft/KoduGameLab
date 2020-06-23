
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
    public class DirectionFilter : Filter
    {
        [XmlAttribute]
        public Directions direction;

        public override ProgrammingElement Clone()
        {
            DirectionFilter clone = new DirectionFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(DirectionFilter clone)
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

            GamePadStickFilter stickFilt = reflex.Data.GetFilterByType(typeof(GamePadStickFilter)) as GamePadStickFilter;
            TouchGestureFilter touchFilt = reflex.Data.GetFilterByType(typeof(TouchGestureFilter)) as TouchGestureFilter;

            int directFiltCount = 0;
            Directions combinedDirection = 0;

            if (stickFilt != null)
            {
                Vector2 stick = stickFilt.stickPosition;
                stick.Normalize();

                for (int i = 0; i < reflex.Filters.Count; ++i)
                {
                    Filter filter = reflex.Filters[i];

                    if (!(filter is DirectionFilter))
                        continue;

                    DirectionFilter directFilt = filter as DirectionFilter;

                    // The first filter will handle all direction filters present, so if we are not
                    // the first filter, just bail with a "true" result.
                    if (directFiltCount > 0 && directFilt == this)
                        return true;

                    // Is the stick in the correct quadrant for this direction?
                    if (!directFilt.IsStickPositionValid(stick))
                        return false;

                    combinedDirection |= directFilt.direction;
                    
                    directFiltCount += 1;
                }

                // If we've gotten here we're good.  The stick is in the 
                // right quadrant for the filter so return true.

                result = true;
            }
            else if (touchFilt != null)
            {
                for (int i = 0; i < reflex.Filters.Count; ++i)
                {
                    Filter filter = reflex.Filters[i];

                    if (!(filter is DirectionFilter))
                        continue;

                    DirectionFilter directFilt = filter as DirectionFilter;

                    // The first filter will handle all direction filters present, so if we are not
                    // the first filter, just bail with a "true" result.
                    if (directFiltCount > 0 && directFilt == this)
                        return true;

                    combinedDirection |= directFilt.direction;

                    directFiltCount += 1;
                }

                if (combinedDirection == touchFilt.Direction)
                {
                    result = true;
                }
            }

            return result;
        }

        public bool IsStickPositionValid(Vector2 stickPosition)
        {
            if ((direction & Directions.North) != 0 && stickPosition.Y <= 0)
                return false;
            if ((direction & Directions.South) != 0 && stickPosition.Y >= 0)
                return false;
            if ((direction & Directions.East) != 0 && stickPosition.X <= 0)
                return false;
            if ((direction & Directions.West) != 0 && stickPosition.X >= 0)
                return false;

            return true;
        }
    }
}
