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

using Boku.Base;
using Boku.Common;

namespace Boku.Input
{
    /// <summary>
    /// This class represents the abstract of the entire chatpad
    /// it is used when you are looking for generalized text input
    /// If you just need a few specific keys that act like a game pad, use ChatPadButton instead
    /// </summary>
    public class ChatPadCommand : InputCommand
    {
        [XmlAttribute]
        public PlayerIndex playerIndex;

        /// <summary>
        /// Press event, when state changes from released to pressed these are triggered
        /// </summary>
        public event InputKeyCommandDelegate Press;
        /// <summary>
        /// RepeatPress event, when the state continues to be pressed, called at a repeat interval
        /// </summary>
        public event InputKeyCommandDelegate RepeatPress;
        /// <summary>
        /// ReleasePress event, when state changes from pressed to released these are triggered
        /// </summary>
        public event InputKeyCommandDelegate ReleasePress;
        /// <summary>
        /// OnChar event, when a char is pressed or repeated
        /// a char is defined as any string usable key;
        /// this excludes the modifier keys and most keys with atl,ctl, or windows keys pressed
        /// </summary>
        public event InputCharCommandDelegate CharInput;

        [XmlAttribute]
        public string BindPress;
        [XmlAttribute]
        public string BindRepeatPress;
        [XmlAttribute]
        public string BindReleasePress;

        [XmlAttribute]
        public string BindCharInput;

        public ChatPadCommand()
        {
            Reset();
            timerAutoRepeat.TimerElapsed += OnRepeatPress;
        }

        public bool CapsLock
        {
            get
            {
                return false;
            }
        }
        public bool NumLock
        {
            get
            {
                return false;
            }
        }
        protected Keys[] pressedKeys;
        protected enum KeyCommandState
        {
            Press,
            Repeat,
            ReleasePress,
        }

        /// <summary>
        /// Current generalized pressed state (is a key in this state)
        /// </summary>
        protected KeyCommandState state;

        // NOTE: do not call Start on this timer, it is being used in a caller update model
        protected Boku.Base.GameTimer timerAutoRepeat = new Boku.Base.GameTimer(Boku.Base.GameTimer.ClockType.WallClock);

        protected void OnChar(Keys primaryKey)
        {
            // don't support alt, ctrl, and windows keys pressed or
            // if the primary is a modifier key
            if (!IsModifierKey(primaryKey) &&
                keyState.IsKeyUp(Keys.LeftAlt) && keyState.IsKeyUp(Keys.RightAlt) &&
                keyState.IsKeyUp(Keys.LeftControl) && keyState.IsKeyUp(Keys.RightControl) &&
                keyState.IsKeyUp(Keys.LeftWindows) && keyState.IsKeyUp(Keys.RightWindows))
            {
                Char charKey;
                if (ConvertKey(out charKey, primaryKey, keyState.IsKeyDown(Keys.LeftShift) || keyState.IsKeyDown(Keys.RightShift)))
                {
                    CharInput(this, new KeyboardCharEventArgs(charKey));
                }
            }
        }

        static Char[] DecimalKeyToShiftChar = new Char[] { ')', '!', '@', '#', '$', '%', '^', '&', '*', '(' };

        protected bool ConvertKey(out Char charKey, Keys key, bool shift)
        {
            bool converted = false;
            shift = shift ^ this.CapsLock;
            charKey = '*';
            if (key == Keys.Space)
            {
                charKey = ' ';
                converted = true;
            }
            if (key >= Keys.A && key <= Keys.Z)
            {
                // alpha
                charKey = (Char)((int)'a' + (int)key - (int)Keys.A);
                if (shift)
                {
                    charKey = Char.ToUpper(charKey);
                }
                converted = true;
            }
            else if (key >= Keys.D0 && key <= Keys.D9)
            {
                // numberals
                int zeroOffset = (int)key - (int)Keys.D0;
                if (shift)
                {
                    charKey = DecimalKeyToShiftChar[zeroOffset];
                }
                else
                {
                    charKey = (Char)((int)'0' + zeroOffset);
                }
                converted = true;
            }
            return converted;
        }
        protected void OnRepeatPress(Boku.Base.GameTimer timer)
        {
            // NOTE: do not call Start on this timer, it is being used in a caller update model
            timerAutoRepeat.Reset(timeAutoRepeat);
            if (RepeatPress != null || CharInput != null)
            {
                for (int indexKey = 0; indexKey < this.pressedKeys.Length; indexKey++)
                {
                    Keys key = this.pressedKeys[indexKey];
                    if (!IsModifierKey(key))
                    {
                        if (RepeatPress != null)
                        {
                            RepeatPress(this, new KeyboardKeyEventArgs(key));
                        }
                        if (CharInput != null)
                        {
                            OnChar(key);
                        }
                    }
                }
            }
            this.state = KeyCommandState.Repeat;
        }

        override public void Reset()
        {
            state = KeyCommandState.ReleasePress;
            timerAutoRepeat.Clear();
            pressedKeys = new Keys[0];
        }

        public override void Sync()
        {
            state = KeyCommandState.ReleasePress;
            timerAutoRepeat.Clear();
            this.pressedKeys = chatpadStates[(int)this.playerIndex].GetPressedKeys();

            for (int indexKey = 0; indexKey < this.pressedKeys.Length; indexKey++)
            {
                Keys key = this.pressedKeys[indexKey];
                if (!IsModifierKey(key))
                {
                    // at least one non-modifier key
                    this.state = KeyCommandState.Press;
                    break;
                }
            }
        }

        protected bool IsModifierKey(Keys key)
        {
            return (key == Keys.LeftShift ||
                            key == Keys.RightShift ||
                            key == Keys.LeftAlt ||
                            key == Keys.RightAlt ||
                            key == Keys.LeftControl ||
                            key == Keys.RightControl ||
                            key == Keys.LeftWindows ||
                            key == Keys.RightWindows ||
                            key == Keys.CapsLock ||
                            key == Keys.NumLock ||
                            key == Keys.Scroll);
        }

        override public void Update()
        {
            if (GamePadInput.GetGamePad(this.playerIndex).IsConnected)
            {
                bool keyIsPressed = false;

                Keys[] currentPressedKeys = chatpadStates[(int)this.playerIndex].GetPressedKeys();

                // walk both sets of keys; the prev and current
                // which are in order and 
                // trigger repeat, press, and release events
                int indexPrev = 0;
                int indexCurr = 0;

                Keys keyPrev;
                Keys keyCurr;

                // while either set still has keys
                while (indexPrev < this.pressedKeys.Length || indexCurr < currentPressedKeys.Length)
                {
                    if (indexPrev < this.pressedKeys.Length)
                    {
                        keyPrev = this.pressedKeys[indexPrev];
                    }
                    else
                    {
                        keyPrev = (Keys)255;// not valid, reached end of this set
                    }
                    if (indexCurr < currentPressedKeys.Length)
                    {
                        keyCurr = currentPressedKeys[indexCurr];
                    }
                    else
                    {
                        keyCurr = (Keys)255; // not valid, reached end of this set
                    }

                    if (keyPrev == keyCurr)
                    {
                        indexPrev++;
                        indexCurr++;
                        // key still pressed
                        if (!IsModifierKey(keyCurr))
                        {
                            keyIsPressed = true;
                        }
                    }
                    else if (keyPrev < keyCurr)
                    {
                        indexPrev++;
                        // key was released
                        if (ReleasePress != null)
                        {
                            ReleasePress(this, new KeyboardKeyEventArgs(keyPrev));
                        }
                    }
                    else
                    {
                        indexCurr++;
                        // new key pressed
                        if (Press != null)
                        {
                            Press(this, new KeyboardKeyEventArgs(keyCurr));
                        }
                        if (CharInput != null)
                        {
                            OnChar(keyCurr);
                        }
                        if (!IsModifierKey(keyCurr))
                        {
                            keyIsPressed = true;
                        }
                    }
                }

                // update generalized state
                if (keyIsPressed)
                {
                    if (state == KeyCommandState.ReleasePress)
                    {
                        // NOTE: do not call Start on this timer, it is being used in a caller update model
                        timerAutoRepeat.Reset(timeFirstAutoRepeat);
                        state = KeyCommandState.Press;
                    }
                }
                else
                {
                    if (state != KeyCommandState.ReleasePress)
                    {
                        timerAutoRepeat.Clear();
                        state = KeyCommandState.ReleasePress;
                    }
                }

                this.pressedKeys = currentPressedKeys;

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
        /// <summary>
        /// Is the key currently being pressed down
        /// </summary>
        public bool this[Keys key]
        {
            get
            {
                bool pressed = false;
                if (GamePadInput.GetGamePad(this.playerIndex).IsConnected)
                {
                    pressed = (chatpadStates[(int)this.playerIndex][key] == KeyState.Down);
                }
                return pressed;
            }
        }
    }
}
