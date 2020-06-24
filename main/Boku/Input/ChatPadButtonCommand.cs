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
    /// Represents the ChatPad key
    /// this is used when you need specific keys that act like a game pad
    /// If you need generalized text input, use ChatPadCommand instead
    /// </summary>
    public class ChatPadButton : ButtonCommand
    {
        [XmlAttribute]
        public PlayerIndex playerIndex;

        [XmlAttribute]
        public Keys key;

        public ChatPadButton(Keys key)
        {
            this.key = key;
        }
        public ChatPadButton()
        {
            this.key = Keys.None;
        }
        override public void Update()
        {
            if ( GamePadInput.GetGamePad( this.playerIndex ).IsConnected )
            {
                Pressed = chatpadStates[(int)this.playerIndex].IsKeyDown(this.key);
                if (timerAutoRepeat.Running)
                {
                    timerAutoRepeat.Update();
                }
            }
            else
            {
                Reset();
            }
        }
        public override void Sync()
        {
            timerAutoRepeat.Clear();
            // specifically don't call property Press as it would cause events
            if (chatpadStates[(int)this.playerIndex].IsKeyDown(this.key))
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
