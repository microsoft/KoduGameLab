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

using KoiX;

using Boku.Base;
using Boku.Common;

namespace Boku.Input
{
    public class KeyboardKeyEventArgs : EventArgs
    {
        public KeyboardKeyEventArgs(Keys key)
        {
            this.key = key;
        }
        public Keys key;
    }
    public class KeyboardCharEventArgs : EventArgs
    {
        public KeyboardCharEventArgs(Char key)
        {
            this.key = key;
        }
        public Char key;
    }
    public delegate void InputKeyCommandDelegate(Object sender, KeyboardKeyEventArgs args);
    public delegate void InputCharCommandDelegate(Object sender, KeyboardCharEventArgs args);

    /// <summary>
    /// This class represents the abstract of the entire keyboard
    /// it is used when you are looking for generalized text input
    /// If you just need a few specific keys that act like a game pad, use KeyboardButtonCommand instead
    /// </summary>
    public class KeyboardCommand : InputCommand
    {
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

        public bool CapsLock
        {
            get
            {
#if NETFX_CORE
                Debug.Assert(false, "Do we need this?  Can we always just look for Shift being pressed?");
                return false;
#else
                return Console.CapsLock;
#endif
            }
        }
        public bool NumLock
        {
            get
            {
#if NETFX_CORE
                Debug.Assert(false, "No clue here...");
                return false;
#else
                return Console.NumberLock;
#endif
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

        // Debounce.  Basically don't allow the same key to be pressed twice within minKeyTime seconds.
        private struct KeyHit
        {
            public Keys key;
            public double time;
        }
        private double minKeyTime = 0.1;
        private int keyIndex = 0;
        private const int numPrevKeys = 8;
        private KeyHit[] prevKeys = new KeyHit[numPrevKeys];

        /// <summary>
        /// Checks a key against recent keys and returns true if it's a duplicate, false otherwise.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected bool DuplicateKey(Keys key)
        {
            double time = Time.WallClockTotalSeconds;

            for (int i = 0; i < numPrevKeys; i++)
            {
                if (prevKeys[i].key == key && time < prevKeys[i].time + minKeyTime)
                {
                    // We found a duplicate.
                    return true;
                }
            }

            // If current key is the same as the last one
            // pressed, just update the time on that entry.
            int prevIndex = (keyIndex + numPrevKeys - 1) % numPrevKeys;
            if (prevKeys[prevIndex].key == key)
            {
                prevKeys[prevIndex].time = time;
            }
            else
            {
                // Update the array with the new key.
                prevKeys[keyIndex].key = key;
                prevKeys[keyIndex].time = time;
                keyIndex = (keyIndex + 1) % numPrevKeys;
            }

            return false;

        }   // end of DupeKey()

        protected void OnChar(Keys primaryKey)
        {
            // don't support alt, ctrl, and windows keys pressed or
            // if the primary is a modifier key
            if (!IsModifierKey(primaryKey) && 
                keyState.IsKeyUp(Keys.LeftAlt) && keyState.IsKeyUp(Keys.RightAlt) && 
                keyState.IsKeyUp(Keys.LeftControl) && keyState.IsKeyUp(Keys.RightControl) && 
                keyState.IsKeyUp(Keys.LeftWindows) && keyState.IsKeyUp(Keys.RightWindows)) 
            {
                // Only accept a keystroke if not a duplicate.
                if (!DuplicateKey(primaryKey))
                {
                    Char charKey;
                    if (ConvertKey(out charKey, primaryKey, keyState.IsKeyDown(Keys.LeftShift) || keyState.IsKeyDown(Keys.RightShift)))
                    {
                        CharInput(this, new KeyboardCharEventArgs(charKey));
                    }
                }
            }
        }

        static Char[] DecimalKeyToShiftChar = new Char[] { ')', '!', '@', '#', '$', '%', '^', '&', '*', '(' };
        static Char[] ArithmeticToChar = new Char[] { '*', '+', ',', '-', '.', '/' };
        static Char[] Oem1ToChar = new Char[]      { ';', '=', ',', '-', '.', '/', '`' };
        static Char[] Oem1ToShiftChar = new Char[] { ':', '+', '<', '_', '>', '?', '~' };

        static Char[] Oem2ToChar = new Char[]      { '[', '\\', ']', '\'', ' ', ' ', ' ', '\\' };
        static Char[] Oem2ToShiftChar = new Char[] { '{', '|', '}', '"', ' ', ' ', ' ', '|' };

        protected bool ConvertKey(out Char charKey, Keys key, bool shift )
        {
            bool converted = false;
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
                if (shift ^ this.CapsLock)
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
            else if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
            {
                // number pad only with num lock
                if (this.NumLock)
                {
                    charKey = (Char)((int)'0' + (int)key - (int)Keys.NumPad0);
                    converted = true;
                }
            }
            else if (key >= Keys.Multiply && key <= Keys.Divide)
            {
                int offset = (int)key - (int)Keys.Multiply;
                charKey = ArithmeticToChar[offset];
                converted = true;
            }
            else if (key >= Keys.OemSemicolon && key <= Keys.OemTilde)
            {
                int offset = (int)key - (int)Keys.OemSemicolon;
                if (shift)
                {
                    charKey = Oem1ToShiftChar[offset];
                }
                else
                {
                    charKey = Oem1ToChar[offset];
                }
                converted = true;
            }
            else if (key >= Keys.OemOpenBrackets && key <= Keys.OemBackslash)
            {
                int offset = (int)key - (int)Keys.OemOpenBrackets;
                if (shift)
                {
                    charKey = Oem2ToShiftChar[offset];
                }
                else
                {
                    charKey = Oem2ToChar[offset];
                }
                converted = true;
            }
            return converted;
        }

        public KeyboardCommand()
        {
            Reset();
            timerAutoRepeat.TimerElapsed += OnRepeatPress;
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
                    if (IsRepeatableKey(key) /* && !IsModifierKey(key) */ )
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

        /// <summary>
        /// Is this key one we want to have autorepeat?
        /// </summary>
        /// <param name="key"></param>
        /// <returns>True if key should autorepeat</returns>
        private bool IsRepeatableKey(Keys key)
        {
            if (key == Keys.Back ||
                key == Keys.Delete ||
                key == Keys.Left ||
                key == Keys.Right ||
                key == Keys.Up ||
                key == Keys.Down)
            {
                return true;
            }

            return false;
        }   // end of isRepeatableKey()

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
            this.pressedKeys = keyState.GetPressedKeys();

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
            bool keyIsPressed = false;

            Keys[] currentPressedKeys = keyState.GetPressedKeys();
            
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
        /// <summary>
        /// Is the key currently being pressed down
        /// </summary>
        public bool this[Keys key]
        {
            get
            {
                return (keyState[key] == KeyState.Down);
            }
        }
    }
}
