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

using Boku.Common;

namespace Boku.Input
{
    /// <summary>
    /// Represents the GamePad Left Thumbstick
    /// </summary>
    public class GamePadLeftThumbStick : StickCommand
    {
        public GamePadLeftThumbStick(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
            position = Vector2.Zero;
        }
        public GamePadLeftThumbStick()
        {
            position = Vector2.Zero;
        }
        override protected Vector2 StickValue()
        {
            return GamePadInput.GetGamePad( this.playerIndex ).LeftStick;
        }

    }
    /// <summary>
    /// Represents the GamePad Right Thumbstick
    /// </summary>
    public class GamePadRightThumbStick : StickCommand
    {
        public GamePadRightThumbStick(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
            position = Vector2.Zero;
        }
        public GamePadRightThumbStick()
        {
            position = Vector2.Zero;
        }
        override protected Vector2 StickValue()
        {
            return GamePadInput.GetGamePad(this.playerIndex).RightStick;
        }
    }
}
