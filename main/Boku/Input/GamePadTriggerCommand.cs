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

using KoiX;
using KoiX.Input;

using Boku.Common;

namespace Boku.Input
{
    /// <summary>
    /// Represents the GamePad Left Trigger
    /// </summary>
    public class GamePadLeftTrigger : TriggerCommand
    {
        public GamePadLeftTrigger(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
            position = 0.0f;
        }
        public GamePadLeftTrigger()
        {
            position = 0.0f;
        }
        protected override float TriggerValue()
        {
            return GamePadInput.GetGamePad(this.playerIndex).LeftTrigger;
        }

    }
    /// <summary>
    /// Represents the GamePad Right Trigger
    /// </summary>
    public class GamePadRightTrigger : TriggerCommand
    {
        public GamePadRightTrigger(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
            position = 0.0f;
        }
        public GamePadRightTrigger()
        {
            position = 0.0f;
        }
        protected override float TriggerValue()
        {
            return GamePadInput.GetGamePad(this.playerIndex).RightTrigger;
        }
    }
}
