
#region Using Statements

using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Content;

using System.Diagnostics;

#endregion

namespace KoiX.Input
{
    /// <summary>
    /// Keyboard input wrapper that treats the keyboard as an ASCII input device rather than a huge gamepad.
    /// Note that XNA only gives you access to the raw key state so some things don't work as expected.
    /// For example, curState.IsKeyDown( Keys.CapsLock ) only returns true when the user is actually holding
    /// the key down.  There seems to be no way to determine the actualy state fo the "lock".  But NumLock
    /// works.  Oh well.
    /// </summary>
    public class LowLevelKeyboardInput
    {
        static Keys rawKey = Keys.None;
        static char asciiChar = (char)0;

        static KeyboardState curState;
        static KeyboardState prevState;

        #region Accessors
        /// <summary>
        /// Returns the ascii char of the whatever key was pressed 
        /// this frame.  Returns NUL(0) if none were pressed or if
        /// a special character was pressed in which case use RawKey
        /// to get the pressed rawKey.
        /// </summary>
        public static char AsciiKey
        {
            get { return asciiChar; }
        }
        /// <summary>
        /// If not an ascii character then it may be a special rawKey.  Note this only
        /// returns up->down transition.
        /// </summary>
        public static Keys RawKey
        {
            get { return rawKey; }
        }

        /// <summary>
        /// Are one of the Alt keys currently pressed.
        /// </summary>
        public static bool AltPressed
        {
            get { return curState.IsKeyDown(Keys.RightAlt) || curState.IsKeyDown(Keys.LeftAlt); }
        }

        /// <summary>
        /// Are one of the Ctrl keys currently pressed.
        /// </summary>
        public static bool CtrlPressed
        {
            get { return curState.IsKeyDown(Keys.RightControl) || curState.IsKeyDown(Keys.LeftControl); }
        }

        /// <summary>
        /// Are one of the Shift keys currently pressed.
        /// </summary>
        public static bool ShiftPressed
        {
            get { return curState.IsKeyDown(Keys.RightShift) || curState.IsKeyDown(Keys.LeftShift); }
        }


        #endregion

        // c'tor
        LowLevelKeyboardInput()
        {
        }   // end of LowLevelKeyboardInput c'tor

        /// <summary>
        /// One time call to set up Keyboard input functionality.
        /// </summary>
        public static void Init()
        {
            prevState = Keyboard.GetState();
        }   // end of LowLevelKeyboardInput Init()

        public static bool IsPressed(Keys key)
        {
            return curState.IsKeyDown(key);
        }

        /// <summary>
        /// Must be called once per frame to update the current state of the keyboard input handling.
        /// When events are detected, also calls any registered event handlers.
        /// </summary>
        /// <returns>True is keyboard state has changed.</returns>
        public static bool Update()
        {
            curState = Keyboard.GetState();

            rawKey = Keys.None;
            asciiChar = (char)0;

            // If nothing changed, we're done
            if (curState == prevState)
            {
                return false;
            }

            /*
            for (Keys kk = (Keys)0; kk < (Keys)256; kk++)
            {
                Debug.Print( ( (int)kk ).ToString() + " " + kk.ToString() );
            }
            */

            rawKey = LetterWasPressed();
            if (rawKey != Keys.None)
            {
                asciiChar = (char)(32 + (int)rawKey);    // Correct for lower case

                // Check for upper case.
                if (curState.IsKeyDown(Keys.LeftShift) || curState.IsKeyDown(Keys.RightShift) || curState.IsKeyDown(Keys.CapsLock))
                {
                    asciiChar = (char)((int)asciiChar - 32);
                }

                // Check for control rawKey
                if (curState.IsKeyDown(Keys.LeftControl) || curState.IsKeyDown(Keys.RightControl))
                {
                    asciiChar = (char)((int)asciiChar - 96);
                }
            }

            // If not a letter then check for other keys.
            if (asciiChar == 0)
            {
                CheckOtherAsciiWasPressed();
            }

            // If still nothing, then check for special keys
            if (asciiChar == 0)
            {
                CheckSpecialWasPressed();
            }

            if (rawKey != Keys.None)
            {
                // InputEventManager stuff moved to KeyboardInput since autorepeat works there.
                // Should this file even exist?  Both this and Keyboard Input use XNA to get
                // keyboard state.  Seems like only one should exist.

                //KeyInput input = new KeyInput(Time.WallClockTotalSeconds, rawKey, asciiChar);
                //KoiLibrary.InputEventManager.ProcessKeyboardEvent(input);
            }

            prevState = curState;

#if DEBUG
            // In debug mode, want to ignore F5 so it's easier to debug
            // gamepad and touch input.
            if (rawKey == Keys.F5 || rawKey == Keys.None)
            {
                rawKey = Keys.None;
                asciiChar = (char)0;
                return false;
            }
#endif

            return true;
        }   // end of LowLevelKeyboardInput Update()

        static Keys LetterWasPressed()
        {
            for (Keys k = Keys.A; k <= Keys.Z; k++)
            {
                if (curState.IsKeyDown(k) && prevState.IsKeyUp(k))
                {
                    return k;
                }
            }

            return Keys.None;
        }   // end of KeyBoardInput.LetterWasPressed()

        static void CheckOtherAsciiWasPressed()
        {
            // NumPad numbers
            if (curState.IsKeyUp(Keys.NumLock))
            {
                if (CheckAsciiKey(Keys.NumPad0, '0')) return;
                if (CheckAsciiKey(Keys.NumPad1, '1')) return;
                if (CheckAsciiKey(Keys.NumPad2, '2')) return;
                if (CheckAsciiKey(Keys.NumPad3, '3')) return;
                if (CheckAsciiKey(Keys.NumPad4, '4')) return;
                if (CheckAsciiKey(Keys.NumPad5, '5')) return;
                if (CheckAsciiKey(Keys.NumPad6, '6')) return;
                if (CheckAsciiKey(Keys.NumPad7, '7')) return;
                if (CheckAsciiKey(Keys.NumPad8, '8')) return;
                if (CheckAsciiKey(Keys.NumPad9, '9')) return;
            }

            // Regular numbers
            if (curState.IsKeyDown(Keys.LeftShift) || curState.IsKeyDown(Keys.RightShift) || curState.IsKeyDown(Keys.CapsLock))
            {
                if (CheckAsciiKey(Keys.D0, '!')) return;
                if (CheckAsciiKey(Keys.D1, '@')) return;
                if (CheckAsciiKey(Keys.D2, '#')) return;
                if (CheckAsciiKey(Keys.D3, '$')) return;
                if (CheckAsciiKey(Keys.D4, '%')) return;
                if (CheckAsciiKey(Keys.D5, '^')) return;
                if (CheckAsciiKey(Keys.D6, '&')) return;
                if (CheckAsciiKey(Keys.D7, '*')) return;
                if (CheckAsciiKey(Keys.D8, '(')) return;
                if (CheckAsciiKey(Keys.D9, ')')) return;
            }
            else
            {
                if (CheckAsciiKey(Keys.D0, '0')) return;
                if (CheckAsciiKey(Keys.D1, '1')) return;
                if (CheckAsciiKey(Keys.D2, '2')) return;
                if (CheckAsciiKey(Keys.D3, '3')) return;
                if (CheckAsciiKey(Keys.D4, '4')) return;
                if (CheckAsciiKey(Keys.D5, '5')) return;
                if (CheckAsciiKey(Keys.D6, '6')) return;
                if (CheckAsciiKey(Keys.D7, '7')) return;
                if (CheckAsciiKey(Keys.D8, '8')) return;
                if (CheckAsciiKey(Keys.D9, '9')) return;
            }

            if (CheckAsciiKey(Keys.Add, '+')) return;
            if (CheckAsciiKey(Keys.Decimal, '.')) return;
            if (CheckAsciiKey(Keys.Divide, '/')) return;
            if (CheckAsciiKey(Keys.Multiply, '*')) return;
            if (CheckAsciiKey(Keys.Space, ' ')) return;
            if (CheckAsciiKey(Keys.Subtract, '-')) return;
            if (CheckAsciiKey(Keys.Tab, '\t')) return;

            if (curState.IsKeyDown(Keys.LeftShift) || curState.IsKeyDown(Keys.RightShift) || curState.IsKeyDown(Keys.CapsLock))
            {
                if (CheckAsciiKey(Keys.OemBackslash, '\\')) return;
                if (CheckAsciiKey(Keys.OemCloseBrackets, '}')) return;
                if (CheckAsciiKey(Keys.OemComma, '<')) return;
                if (CheckAsciiKey(Keys.OemMinus, '_')) return;
                if (CheckAsciiKey(Keys.OemOpenBrackets, '{')) return;
                if (CheckAsciiKey(Keys.OemPeriod, '>')) return;
                if (CheckAsciiKey(Keys.OemPipe, '|')) return;
                if (CheckAsciiKey(Keys.OemPlus, '+')) return;
                if (CheckAsciiKey(Keys.OemQuestion, '?')) return;
                if (CheckAsciiKey(Keys.OemQuotes, '"')) return;
                if (CheckAsciiKey(Keys.OemSemicolon, ':')) return;
                if (CheckAsciiKey(Keys.OemTilde, '~')) return;
            }
            else
            {
                if (CheckAsciiKey(Keys.OemBackslash, '\\')) return;
                if (CheckAsciiKey(Keys.OemCloseBrackets, ']')) return;
                if (CheckAsciiKey(Keys.OemComma, ',')) return;
                if (CheckAsciiKey(Keys.OemMinus, '-')) return;
                if (CheckAsciiKey(Keys.OemOpenBrackets, '[')) return;
                if (CheckAsciiKey(Keys.OemPeriod, '.')) return;
                if (CheckAsciiKey(Keys.OemPipe, '\\')) return;
                if (CheckAsciiKey(Keys.OemPlus, '=')) return;
                if (CheckAsciiKey(Keys.OemQuestion, '/')) return;
                if (CheckAsciiKey(Keys.OemQuotes, '\'')) return;
                if (CheckAsciiKey(Keys.OemSemicolon, ';')) return;
                if (CheckAsciiKey(Keys.OemTilde, '`')) return;
            }


        }   // end of KeyBoardInput OtherWasPressed()

        /// <summary>
        /// Checks for the current input rawKey, if was pressed then
        /// assigns value to asciiChar and returns true.
        /// </summary>
        static bool CheckAsciiKey(Keys key, char value)
        {
            if (curState.IsKeyDown(key) && prevState.IsKeyUp(key))
            {
                rawKey = key;
                asciiChar = value;
                return true;
            }
            return false;
        }   // end of KeyBoardInput CheckAsciiKey()

        static void CheckSpecialWasPressed()
        {
            if (CheckSpecialKey(Keys.Back)) return;
            if (CheckSpecialKey(Keys.Down)) return;
            if (CheckSpecialKey(Keys.Delete)) return;
            if (CheckSpecialKey(Keys.End)) return;
            if (CheckSpecialKey(Keys.Enter)) return;
            if (CheckSpecialKey(Keys.Escape)) return;
            if (CheckSpecialKey(Keys.F1)) return;
            if (CheckSpecialKey(Keys.F2)) return;
            if (CheckSpecialKey(Keys.F3)) return;
            if (CheckSpecialKey(Keys.F4)) return;
            if (CheckSpecialKey(Keys.F5)) return;
            if (CheckSpecialKey(Keys.F6)) return;
            if (CheckSpecialKey(Keys.F7)) return;
            if (CheckSpecialKey(Keys.F8)) return;
            if (CheckSpecialKey(Keys.F9)) return;
            if (CheckSpecialKey(Keys.F10)) return;
            if (CheckSpecialKey(Keys.F11)) return;
            if (CheckSpecialKey(Keys.F12)) return;
            if (CheckSpecialKey(Keys.F13)) return;
            if (CheckSpecialKey(Keys.F14)) return;
            if (CheckSpecialKey(Keys.F15)) return;
            if (CheckSpecialKey(Keys.F16)) return;
            if (CheckSpecialKey(Keys.F17)) return;
            if (CheckSpecialKey(Keys.F18)) return;
            if (CheckSpecialKey(Keys.F19)) return;
            if (CheckSpecialKey(Keys.F20)) return;
            if (CheckSpecialKey(Keys.F21)) return;
            if (CheckSpecialKey(Keys.F22)) return;
            if (CheckSpecialKey(Keys.F23)) return;
            if (CheckSpecialKey(Keys.F24)) return;
            if (CheckSpecialKey(Keys.Help)) return;
            if (CheckSpecialKey(Keys.Home)) return;
            if (CheckSpecialKey(Keys.Insert)) return;
            if (CheckSpecialKey(Keys.Left)) return;
            if (CheckSpecialKey(Keys.PageDown)) return;
            if (CheckSpecialKey(Keys.PageUp)) return;
            if (CheckSpecialKey(Keys.Right)) return;
            if (CheckSpecialKey(Keys.Up)) return;

        }   // end of KeyBoardInput CheckSpecialWasPressed()

        /// <summary>
        /// Checks for the current input rawKey, if was pressed then
        /// assigns value to asciiChar and returns true.
        /// </summary>
        static bool CheckSpecialKey(Keys key)
        {
            if (curState.IsKeyDown(key) && prevState.IsKeyUp(key))
            {
                rawKey = key;
                return true;
            }
            return false;
        }   // end of KeyBoardInput CheckSpecialKey()

    }   // end of class LowLevelKeyboardInput

}   // end of namespace KoiX.Input

