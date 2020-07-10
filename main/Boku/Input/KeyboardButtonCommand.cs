// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Xml;
using System.Xml.Serialization;
using System.Collections;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

namespace Boku.Input
{

    /// <summary>
    /// Represents the Keyboard key
    /// this is used when you need specific keys that act like a game pad
    /// If you need generalized text input, use KeyboardCommand instead
    /// </summary>
    public class KeyboardButton : ButtonCommand
    {
        [XmlAttribute]
        public Keys key;

        public KeyboardButton(Keys key)
        {
            this.key = key;
        }
        public KeyboardButton()
        {
            this.key = Keys.None;
        }
        override public void Update()
        {
            Pressed = keyState.IsKeyDown(this.key);
            if (timerAutoRepeat.Running)
            {
                timerAutoRepeat.Update();
            }
        }
        public override void Sync()
        {
            timerAutoRepeat.Clear();
            // specifically don't call property Press as it would cause events
            if (keyState.IsKeyDown(this.key))
            {
                this.state = ButtonCommandState.Press;
            }
            else
            {
                this.state = ButtonCommandState.ReleasePress;
            }
        }
    }
}
