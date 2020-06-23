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
using Boku.Input;

namespace Boku.Programming
{
    /// <summary>
    /// Hybrid filter that provides the source of on-screen touch button input
    /// 
    /// 
    /// </summary>
    public class GUIButtonFilter : Filter
    {
        [XmlAttribute]
        public Classification.Colors color;

        public override ProgrammingElement Clone()
        {
            GUIButtonFilter clone = new GUIButtonFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(GUIButtonFilter clone)
        {
            base.CopyTo(clone);
            clone.color = this.color;
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            // When checking a touchButton filter, the target no longer has a valid position, because
            // technically the button has no real place in the gameworld. We thus strip that data
            // from the target.
            sensorTarget.Position = sensorTarget.GameThing.Movement.Position;
            sensorTarget.Range = 0.0f;
            sensorTarget.Direction = Vector3.Zero;
            return true;
        }

        public override bool MatchAction(Reflex reflex, out object param)
        {
            param = null;

            GUIButton button = GUIButtonManager.GetButton(color);

            bool clicked = (null != button) && button.Clicked;
            

            if (reflex.Sensor is MouseSensor)
            {
                clicked &= button.MouseControlled;

                MouseFilter mouseFilter = reflex.Data.GetFilterByType(typeof(MouseFilter)) as MouseFilter;
                if (null != mouseFilter)
                {
                    switch (mouseFilter.type)
                    {
                        case MouseFilterType.LeftButton:
                            clicked &= button.MouseLeftClick;
                            break;

                        case MouseFilterType.RightButton:
                            clicked &= !button.MouseLeftClick;
                            break;

                        case MouseFilterType.Hover:
                            clicked = button.MouseOver;
                            break;

                        default:
                            clicked = false;
                            break;
                    }
                }
            }
            else
            {
                clicked &= !button.MouseControlled;
            }

            return clicked;
        }
    }
}
