// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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

namespace Boku.Input
{
    /// <summary>
    /// This class represents a wrapper of mulitple button commands to one virtual command
    /// The wrapped commands can come from analog sources (triggers and sticks)
    /// </summary>
    public class VirtualButton : ButtonCommand
    {
        [XmlArrayItem(typeof(GamePadAButton)),
            XmlArrayItem(typeof(GamePadBButton)),
            XmlArrayItem(typeof(GamePadXButton)),
            XmlArrayItem(typeof(GamePadYButton)),
            XmlArrayItem(typeof(GamePadRightShoulderButton)),
            XmlArrayItem(typeof(GamePadLeftShoulderButton)),
            XmlArrayItem(typeof(GamePadRightStickButton)),
            XmlArrayItem(typeof(GamePadLeftStickButton)),
            XmlArrayItem(typeof(GamePadBackButton)),
            XmlArrayItem(typeof(GamePadStartButton)),
            XmlArrayItem(typeof(GamePadDownButton)),
            XmlArrayItem(typeof(GamePadLeftButton)),
            XmlArrayItem(typeof(GamePadRightButton)),
            XmlArrayItem(typeof(GamePadUpButton)),
            XmlArrayItem(typeof(GamePadDownExButton)),
            XmlArrayItem(typeof(GamePadDownLeftExButton)),
            XmlArrayItem(typeof(GamePadLeftExButton)),
            XmlArrayItem(typeof(GamePadDownRightExButton)),
            XmlArrayItem(typeof(GamePadRightExButton)),
            XmlArrayItem(typeof(GamePadUpRightExButton)),
            XmlArrayItem(typeof(GamePadUpExButton)),
            XmlArrayItem(typeof(GamePadUpLeftExButton)),
            XmlArrayItem(typeof(GamePadLeftTrigger)),
            XmlArrayItem(typeof(GamePadRightTrigger)),
            XmlArrayItem(typeof(GamePadLeftThumbStick)),
            XmlArrayItem(typeof(GamePadRightThumbStick)),
            XmlArrayItem(typeof(KeyboardButton))]
        public List<InputCommand> commands;

        override public void Update()
        {
            for (int indexCommand = 0; indexCommand < this.commands.Count; indexCommand++)
            {
                InputCommand command = this.commands[indexCommand];
                command.Update();
            }
        }
        override public void Sync()
        {
            for (int indexCommand = 0; indexCommand < this.commands.Count; indexCommand++)
            {
                InputCommand command = this.commands[indexCommand];
                command.Sync();
            }
        }
        override public void Reset()
        {
            for (int indexCommand = 0; indexCommand < this.commands.Count; indexCommand++)
            {
                InputCommand command = this.commands[indexCommand];
                command.Reset();
            }
        }
    }
}
