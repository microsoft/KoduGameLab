
using System;
using System.Xml;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.Common.Gesture;

namespace Boku.Input
{
    public static partial class VirtualKeyboard
    {
        public class KeySet
        {
            #region Members

            string name = "";

            List<Key> keys = new List<Key>();

            #endregion

            #region Accessors

            public List<Key> Keys
            {
                get { return keys; }
            }

            public string Name
            {
                get { return name; }
            }

            #endregion

            #region Public

            public KeySet(string name)
            {
                this.name = name;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns>Return key if autorepeating.</returns>
            public Key Update()
            {
                Key result = null;

                foreach (Key k in keys)
                {
                    if (k.Update())
                    {
                        OnKeyPressed(k);
                        result = k;
                    }
                }

                return result;
            }   // end of Update()

            public void Render()
            {
                foreach (Key k in keys)
                {
                    k.Render();
                }
            }   // end of Render()

            /// <summary>
            /// Test hit location against keys and returns string with any generated characters.
            /// </summary>
            /// <param name="hitPosition"></param>
            /// <returns></returns>
            public Key HitTest(Vector2 hitPosition)
            {
                Key result = null;

                VirtualKeyboard.dirty = true;
                foreach (Key k in keys)
                {
                    if (k.HitTest(hitPosition))
                    {
                        result = OnKeyPressed(k);
                        break;
                    }
                }

                return result;
            }   // end of HitTest()

            Key OnKeyPressed(Key k)
            {
                Key result = null;

                // Ignore dimmed keys.
                if (k.labelColor == VirtualKeyboard.dimmedTextColor)
                {
                    // do nothing
                }
                else
                {
                    // If this key should cause a transition...
                    if (k.OnKey != null)
                    {
                        k.OnKey(k);
                    }
                    else
                    {
                        // Output key.
                        result = k;
                    }
                }

                return result;
            }   // end OnKeyPressed

            #endregion

            #region Internal

            public void LoadContent(bool immediate)
            {
            }

            public void InitDeviceResources(GraphicsDevice device)
            {
            }

            public void UnloadContent()
            {
            }

            public void DeviceReset(GraphicsDevice device)
            {
            }

            #endregion

        }   // end of class KeySet

    }   // end of class VirtualKeyboard

}   // end of namespace Boku.Input
