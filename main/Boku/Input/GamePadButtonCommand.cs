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
    /// This represents the abstraction for the game pad button unput command
    /// It provides core work for all game pad buttons and relies on ButtonCommand for
    /// exposed calls
    /// 
    /// There is currently no need to use this class outside this module as all
    /// usefull classes are represented below
    /// </summary>
    abstract public class GamePadButtonCommand : ButtonCommand
    {
        [XmlAttribute]
        public PlayerIndex playerIndex;

        override public void Update()
        {
            if (GamePadInput.GetGamePad(this.playerIndex).IsConnected)
            {
                // specifically call property
                this.Pressed = IsPressed();
                
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
        override public void Sync()
        {
            // sync base state to current value
            // specifically do not call property, set the value
            timerAutoRepeat.Clear();

            if (IsPressed())
            {
                this.state = ButtonCommandState.Press;
            }
            else
            {
                this.state = ButtonCommandState.ReleasePress;
            }
        }
        abstract protected bool IsPressed();
    }
    /// <summary>
    /// Represents the GamePad A Button
    /// </summary>
    public class GamePadAButton : GamePadButtonCommand
    {
        public GamePadAButton()
        {
        }
        public GamePadAButton(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
        }
        override protected bool IsPressed()
        {
            return GamePadInput.GetGamePad(this.playerIndex).ButtonA.ButtonState == ButtonState.Pressed;
        }
    }

    /// <summary>
    /// Represents the GamePad B Button
    /// </summary>
    public class GamePadBButton : GamePadButtonCommand
    {
        public GamePadBButton()
        {
        }
        public GamePadBButton(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
        }
        override protected bool IsPressed()
        {
            return GamePadInput.GetGamePad(this.playerIndex).ButtonB.ButtonState == ButtonState.Pressed;
        }

    }
    /// <summary>
    /// Represents the GamePad X Button
    /// </summary>
    public class GamePadXButton : GamePadButtonCommand
    {
        public GamePadXButton()
        {
        }
        public GamePadXButton(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
        }
        override protected bool IsPressed()
        {
            return GamePadInput.GetGamePad(this.playerIndex).ButtonX.ButtonState == ButtonState.Pressed;
        }
    }
    /// <summary>
    /// Represents the GamePad Y Button
    /// </summary>
    public class GamePadYButton : GamePadButtonCommand
    {
        public GamePadYButton()
        {
        }
        public GamePadYButton(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
        }
        override protected bool IsPressed()
        {
            return GamePadInput.GetGamePad(this.playerIndex).ButtonY.ButtonState == ButtonState.Pressed;
        }
    }
    /// <summary>
    /// Represents the GamePad Right Shoulder Button
    /// </summary>
    public class GamePadRightShoulderButton : GamePadButtonCommand
    {
        public GamePadRightShoulderButton()
        {
        }
        public GamePadRightShoulderButton(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
        }
        override protected bool IsPressed()
        {
            return GamePadInput.GetGamePad(this.playerIndex).RightShoulder.ButtonState == ButtonState.Pressed;
        }
    }
    /// <summary>
    /// Represents the GamePad Left Shoulder Button
    /// </summary>
    public class GamePadLeftShoulderButton : GamePadButtonCommand
    {
        public GamePadLeftShoulderButton()
        {
        }
        public GamePadLeftShoulderButton(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
        }
        override protected bool IsPressed()
        {
            return GamePadInput.GetGamePad(this.playerIndex).LeftShoulder.ButtonState == ButtonState.Pressed;
        }
    }
    /// <summary>
    /// Represents the GamePad Right Stick button
    /// </summary>
    public class GamePadRightStickButton : GamePadButtonCommand
    {
        public GamePadRightStickButton()
        {
        }
        public GamePadRightStickButton(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
        }
        override protected bool IsPressed()
        {
            return GamePadInput.GetGamePad(this.playerIndex).RightStickButton.ButtonState == ButtonState.Pressed;
        }

    }
    /// <summary>
    /// Represents the GamePad Left Stick button
    /// </summary>
    public class GamePadLeftStickButton : GamePadButtonCommand
    {
        public GamePadLeftStickButton()
        {
        }
        public GamePadLeftStickButton(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
        }
        override protected bool IsPressed()
        {
            return GamePadInput.GetGamePad(this.playerIndex).LeftStickButton.ButtonState == ButtonState.Pressed;
        }
    }
    /// <summary>
    /// Represents the GamePad Back button
    /// </summary>
    public class GamePadBackButton : GamePadButtonCommand
    {
        public GamePadBackButton()
        {
        }
        public GamePadBackButton(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
        }
        override protected bool IsPressed()
        {
            return GamePadInput.GetGamePad(this.playerIndex).Back.ButtonState == ButtonState.Pressed;
        }
    }
    /// <summary>
    /// Represents the GamePad Start button
    /// </summary>
    public class GamePadStartButton : GamePadButtonCommand
    {
        public GamePadStartButton()
        {
        }
        public GamePadStartButton(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
        }
        override protected bool IsPressed()
        {
            return GamePadInput.GetGamePad(this.playerIndex).Start.ButtonState == ButtonState.Pressed;
        }
    }
    /// <summary>
    /// Represents the GamePad DPad Down button
    /// Use the Ex version is you are testing all 8 directions
    /// </summary>
    public class GamePadDownButton : GamePadButtonCommand
    {
        public GamePadDownButton()
        {
        }
        public GamePadDownButton(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
        }
        override protected bool IsPressed()
        {
            return GamePadInput.GetGamePad(this.playerIndex).DPadDown.ButtonState == ButtonState.Pressed;
        }
    }
    /// <summary>
    /// Represents the GamePad DPad Left button
    /// Use the Ex version is you are testing all 8 directions
    /// </summary>
    public class GamePadLeftButton : GamePadButtonCommand
    {
        public GamePadLeftButton()
        {
        }
        public GamePadLeftButton(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
        }
        override protected bool IsPressed()
        {
            return GamePadInput.GetGamePad(this.playerIndex).DPadLeft.ButtonState == ButtonState.Pressed;
        }
    }
    /// <summary>
    /// Represents the GamePad DPad Right button
    /// Use the Ex version is you are testing all 8 directions
    /// </summary>
    public class GamePadRightButton : GamePadButtonCommand
    {
        public GamePadRightButton()
        {
        }
        public GamePadRightButton(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
        }
        override protected bool IsPressed()
        {
            return GamePadInput.GetGamePad(this.playerIndex).DPadRight.ButtonState == ButtonState.Pressed;
        }
    }
    /// <summary>
    /// Represents the GamePad DPad Up button
    /// Use the Ex version is you are testing all 8 directions
    /// </summary>
    public class GamePadUpButton : GamePadButtonCommand
    {
        public GamePadUpButton()
        {
        }
        public GamePadUpButton(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
        }
        override protected bool IsPressed()
        {
            return GamePadInput.GetGamePad(this.playerIndex).DPadUp.ButtonState == ButtonState.Pressed;
        }
    }


    /// <summary>
    /// Represents the GamePad DPad Down button
    /// Use the non-Ex version is you are testing only 4 directions
    /// </summary>
    public class GamePadDownExButton : GamePadButtonCommand
    {
        public GamePadDownExButton()
        {
        }
        public GamePadDownExButton(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
        }

        override protected bool IsPressed()
        {
            GamePadInput pad = GamePadInput.GetGamePad(this.playerIndex);

            return (pad.DPadDown.ButtonState == ButtonState.Pressed &&
                    pad.DPadLeft.ButtonState == ButtonState.Released &&
                    pad.DPadRight.ButtonState == ButtonState.Released);
        }
    }
    /// <summary>
    /// Represents the GamePad DPad Down & Left button
    /// Use the non-Ex version is you are testing only 4 directions
    /// </summary>
    public class GamePadDownLeftExButton : GamePadButtonCommand
    {
        public GamePadDownLeftExButton()
        {
        }
        public GamePadDownLeftExButton(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
        }
        override protected bool IsPressed()
        {
            GamePadInput pad = GamePadInput.GetGamePad(this.playerIndex);

            return (pad.DPadDown.ButtonState == ButtonState.Pressed &&
                    pad.DPadLeft.ButtonState == ButtonState.Pressed &&
                    pad.DPadRight.ButtonState == ButtonState.Released &&
                    pad.DPadUp.ButtonState == ButtonState.Released);
        }
    }
    /// <summary>
    /// Represents the GamePad DPad Left button
    /// Use the non-Ex version is you are testing only 4 directions
    /// </summary>
    public class GamePadLeftExButton : GamePadButtonCommand
    {
        public GamePadLeftExButton()
        {
        }
        public GamePadLeftExButton(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
        }
        override protected bool IsPressed()
        {
            GamePadInput pad = GamePadInput.GetGamePad(this.playerIndex);

            return (pad.DPadLeft.ButtonState == ButtonState.Pressed &&
                    pad.DPadUp.ButtonState == ButtonState.Released &&
                    pad.DPadDown.ButtonState == ButtonState.Released);
        }
    }
    /// <summary>
    /// Represents the GamePad DPad Down & Right button
    /// Use the non-Ex version is you are testing only 4 directions
    /// </summary>
    public class GamePadDownRightExButton : GamePadButtonCommand
    {
        public GamePadDownRightExButton()
        {
        }
        public GamePadDownRightExButton(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
        }
        override protected bool IsPressed()
        {
            GamePadInput pad = GamePadInput.GetGamePad(this.playerIndex);

            return (pad.DPadDown.ButtonState == ButtonState.Pressed &&
                    pad.DPadRight.ButtonState == ButtonState.Pressed &&
                    pad.DPadLeft.ButtonState == ButtonState.Released &&
                    pad.DPadUp.ButtonState == ButtonState.Released);
        }
    }
    /// <summary>
    /// Represents the GamePad DPad Right button
    /// Use the non-Ex version is you are testing only 4 directions
    /// </summary>
    public class GamePadRightExButton : GamePadButtonCommand
    {
        public GamePadRightExButton()
        {
        }
        public GamePadRightExButton(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
        }
        override protected bool IsPressed()
        {
            GamePadInput pad = GamePadInput.GetGamePad(this.playerIndex);

            return (pad.DPadRight.ButtonState == ButtonState.Pressed &&
                    pad.DPadUp.ButtonState == ButtonState.Released &&
                    pad.DPadDown.ButtonState == ButtonState.Released);
        }
    }
    /// <summary>
    /// Represents the GamePad DPad Up Right button
    /// Use the non-Ex version is you are testing only 4 directions
    /// </summary>
    public class GamePadUpRightExButton : GamePadButtonCommand
    {
        public GamePadUpRightExButton()
        {
        }
        public GamePadUpRightExButton(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
        }
        override protected bool IsPressed()
        {
            GamePadInput pad = GamePadInput.GetGamePad(this.playerIndex);

            return (pad.DPadRight.ButtonState == ButtonState.Pressed &&
                    pad.DPadUp.ButtonState == ButtonState.Pressed &&
                    pad.DPadDown.ButtonState == ButtonState.Released &&
                    pad.DPadLeft.ButtonState == ButtonState.Released);
        }
    }
    /// <summary>
    /// Represents the GamePad DPad Up button
    /// Use the non-Ex version is you are testing only 4 directions
    /// </summary>
    public class GamePadUpExButton : GamePadButtonCommand
    {
        public GamePadUpExButton()
        {
        }
        public GamePadUpExButton(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
        }
        override protected bool IsPressed()
        {
            GamePadInput pad = GamePadInput.GetGamePad(this.playerIndex);

            return (pad.DPadUp.ButtonState == ButtonState.Pressed &&
                    pad.DPadLeft.ButtonState == ButtonState.Released &&
                    pad.DPadRight.ButtonState == ButtonState.Released &&
                    pad.DPadDown.ButtonState == ButtonState.Released);
        }
    }
    /// <summary>
    /// Represents the GamePad DPad Down & Left button
    /// Use the non-Ex version is you are testing only 4 directions
    /// </summary>
    public class GamePadUpLeftExButton : GamePadButtonCommand
    {
        public GamePadUpLeftExButton()
        {
        }
        public GamePadUpLeftExButton(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
        }
        override protected bool IsPressed()
        {
            GamePadInput pad = GamePadInput.GetGamePad(this.playerIndex);

            return (pad.DPadUp.ButtonState == ButtonState.Pressed &&
                    pad.DPadLeft.ButtonState == ButtonState.Pressed &&
                    pad.DPadRight.ButtonState == ButtonState.Released &&
                    pad.DPadDown.ButtonState == ButtonState.Released);
        }
    }

    /// <summary>
    /// Represents the GamePad left trigger as a digital value.
    /// </summary>
    public class GamePadLeftTriggerButton : GamePadButtonCommand
    {
        public GamePadLeftTriggerButton()
        {
        }
        public GamePadLeftTriggerButton(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
        }
        protected override bool IsPressed()
        {
            return GamePadInput.GetGamePad(this.playerIndex).LeftTriggerButton.ButtonState == ButtonState.Pressed;
        }
    }

    /// <summary>
    /// Represents the GamePad right trigger as a digital value.
    /// </summary>
    public class GamePadRightTriggerButton : GamePadButtonCommand
    {
        public GamePadRightTriggerButton()
        {
        }
        public GamePadRightTriggerButton(PlayerIndex playerIndex)
        {
            this.playerIndex = playerIndex;
        }
        protected override bool IsPressed()
        {
            return GamePadInput.GetGamePad(this.playerIndex).RightTriggerButton.ButtonState == ButtonState.Pressed;
        }
    }

}
