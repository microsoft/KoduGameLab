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

using KoiX.Input;

using Boku.Base;
using Boku.Common;
using Boku.Input;

namespace Boku.Programming
{
    /// <summary>
    /// Hybrid filter that provides the source of gamepad button input
    /// 
    /// 
    /// </summary>
    public class KeyBoardKeyFilter : Filter
    {
        [XmlAttribute]
        public Keys key = Keys.None;
        [XmlAttribute]
        public Keys key2 = Keys.None;           // Option key used when multiple keys are treated as the same.
                                                // For instance the "shiftkey" filter reacts to both leftShift and rightShift. 

        [XmlIgnore]
        public Vector2 dir = new Vector2(0, 1);

        [XmlAttribute]
        public string direction
        {
            get { return dir.ToString(); }
            set { Utils.Vector2FromString(value, out dir, dir); }
        }

        public KeyBoardKeyFilter()
        {
        }

        public KeyBoardKeyFilter(Keys key, Vector2 dir)
        {
            this.key = key;
            this.dir = dir;
        }

        public override ProgrammingElement Clone()
        {
            KeyBoardKeyFilter clone = new KeyBoardKeyFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(KeyBoardKeyFilter clone)
        {
            base.CopyTo(clone);
            clone.key = this.key;
            clone.key2 = this.key2;
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            return false;
        }
        public override bool MatchAction(Reflex reflex, out object param)
        {
            bool result = false;

            result = KeyboardInputX.IsPressed(key);
            if (key2 != Keys.None)
            {
                // yes, this should be an "or".
                result |= KeyboardInputX.IsPressed(key2);
            }

            // Return as a parameter a vector that can be used for input to the movement system, so
            // that players can drive and turn bots using keys.
            param = dir;

            // Special case arrow and WASD keys.
            if(upid == "filter.ArrowKeys")
            {
                Vector2 direction = Vector2.Zero;
                if (KeyboardInputX.IsPressed(Keys.Up))
                {
                    direction += new Vector2(0, 1);
                    result = true;
                }
                if (KeyboardInputX.IsPressed(Keys.Down))
                {
                    direction += new Vector2(0, -1);
                    result = true;
                }
                if (KeyboardInputX.IsPressed(Keys.Left))
                {
                    direction += new Vector2(-1, 0);
                    result = true;
                }
                if (KeyboardInputX.IsPressed(Keys.Right))
                {
                    direction += new Vector2(1, 0);
                    result = true;
                }

                if (result && direction == Vector2.Zero)
                {
                }

                param = direction;
            }
            else if (upid == "filter.WASDKeys")
            {
                Vector2 direction = Vector2.Zero;
                if (KeyboardInputX.IsPressed(Keys.W))
                {
                    direction += new Vector2(0, 1);
                    result = true;
                }
                if (KeyboardInputX.IsPressed(Keys.S))
                {
                    direction += new Vector2(0, -1);
                    result = true;
                }
                if (KeyboardInputX.IsPressed(Keys.A))
                {
                    direction += new Vector2(-1, 0);
                    result = true;
                }
                if (KeyboardInputX.IsPressed(Keys.D))
                {
                    direction += new Vector2(1, 0);
                    result = true;
                }

                param = direction;
            }
            

            return result;
        }

        public override void Reset(Reflex reflex)
        {
            base.Reset(reflex);
        }

    }   // end of class KeyBoardKeyFilter

}   // end of namespace Boku.Programming
