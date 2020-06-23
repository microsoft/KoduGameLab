
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;


namespace Boku.Common
{
    /// <summary>
    /// A layer of indirection which allows a collection of inputs to be 
    /// treated as a single button.  The collection may consist of a 0, 1 or 2 
    /// gamepad buttons and any number of keyboard keys.
    /// </summary>
    public class MetaButton
    {
        #region Members

        public static Keys Ctrl = (Keys)0x0100;
        public static Keys Shift = (Keys)0x0200;

        private List<Keys> keys = new List<Keys>();
        private Buttons b0 = 0;
        private Buttons b1 = 0;

        #endregion

        #region Accessors

        public bool WasPressed
        {
            get
            {
                bool result = false;

                if (b0 != 0)
                {
                    GamePadInput pad = GamePadInput.GetGamePad0();
                    GamePadInput.Button b = pad.GetButton(b0);
                    result |= b.WasPressed;
                    if (b1 != 0 && !result)
                    {
                        b = pad.GetButton(b1);
                        result |= b.WasPressed;
                    }
                }

                if (!result)
                {
                    for (int i = 0; i < keys.Count; i++)
                    {
                        if (keys[i] < (Keys)256 && !KeyboardInput.CtrlIsPressed && !KeyboardInput.ShiftIsPressed && !KeyboardInput.AltIsPressed)
                        {
                            result |= KeyboardInput.WasPressed(keys[i]);
                        }
                        else if ((keys[i] & Ctrl) != Keys.None)
                        {
                            result |= KeyboardInput.WasCtrlPressed(keys[i]);
                        }
                        else if ((keys[i] & Shift) != Keys.None)
                        {
                            result |= KeyboardInput.WasShiftPressed(keys[i]);
                        }
                    }
                }

                return result;
            }
        }

        public bool IsPressed
        {
            get
            {
                bool result = false;

                if (b0 != 0)
                {
                    GamePadInput pad = GamePadInput.GetGamePad0();
                    GamePadInput.Button b = pad.GetButton(b0);
                    result |= b.IsPressed;
                    if (b1 != 0 && !result)
                    {
                        b = pad.GetButton(b1);
                        result |= b.IsPressed;
                    }
                }

                if (!result)
                {
                    for (int i = 0; i < keys.Count; i++)
                    {
                        if (keys[i] < (Keys)256)
                        {
                            result |= KeyboardInput.IsPressed(keys[i]);
                        }
                        else if ((keys[i] & Ctrl) != Keys.None)
                        {
                            result |= KeyboardInput.IsCtrlPressed(keys[i]);
                        }
                        else if ((keys[i] & Shift) != Keys.None)
                        {
                            result |= KeyboardInput.IsShiftPressed(keys[i]);
                        }
                    }
                }

                return result;
            }
        }

        public bool WasPressedOrRepeat
        {
            get
            {
                bool result = false;

                if (b0 != 0)
                {
                    GamePadInput pad = GamePadInput.GetGamePad0();
                    GamePadInput.Button b = pad.GetButton(b0);
                    result |= b.WasPressedOrRepeat;
                    if (b1 != 0 && !result)
                    {
                        b = pad.GetButton(b1);
                        result |= b.WasPressedOrRepeat;
                    }
                }

                if (!result)
                {
                    for (int i = 0; i < keys.Count; i++)
                    {
                        if (keys[i] < (Keys)256)
                        {
                            result |= KeyboardInput.WasPressedOrRepeat(keys[i]);
                        }
                        else if ((keys[i] & Ctrl) != Keys.None)
                        {
                            result |= KeyboardInput.WasCtrlPressedOrRepeat(keys[i]);
                        }
                        else if ((keys[i] & Shift) != Keys.None)
                        {
                            result |= KeyboardInput.WasShiftPressedOrRepeat(keys[i]);
                        }
                    }
                }

                return result;
            }
        }

        public bool WasRepeatPressed
        {
            get
            {
                bool result = false;

                if (b0 != 0)
                {
                    GamePadInput pad = GamePadInput.GetGamePad0();
                    GamePadInput.Button b = pad.GetButton(b0);
                    result |= b.WasRepeatPressed;
                    if (b1 != 0 && !result)
                    {
                        b = pad.GetButton(b1);
                        result |= b.WasRepeatPressed;
                    }
                }

                if (!result)
                {
                    for (int i = 0; i < keys.Count; i++)
                    {
                        if (keys[i] < (Keys)256)
                        {
                            result |= KeyboardInput.WasRepeatPressed(keys[i]);
                        }
                        else if ((keys[i] & Ctrl) != Keys.None)
                        {
                            result |= KeyboardInput.WasCtrlRepeatPressed(keys[i]);
                        }
                        else if ((keys[i] & Shift) != Keys.None)
                        {
                            result |= KeyboardInput.WasShiftRepeatPressed(keys[i]);
                        }
                    }
                }

                return result;
            }
        }

        #endregion

        #region Public

        /// <summary>
        /// c'tor
        /// </summary>
        /// <param name="keyArray">One or more comma seperated Keys.</param>
        public MetaButton(params Keys[] keyArray)
        {
            foreach (Keys k in keyArray)
            {
                keys.Add(k);
            }
        }

        /// <summary>
        /// c'tor
        /// </summary>
        /// <param name="button">Use 0 for no button.</param>
        /// <param name="keyArray">One or more comma seperated Keys.</param>
        public MetaButton(Buttons button, params Keys[] keyArray)
        {
            this.b0 = button;

            foreach (Keys k in keyArray)
            {
                keys.Add(k);
            }
        }

        /// <summary>
        /// c'tor
        /// </summary>
        /// <param name="b0">Use 0 for no button.</param>
        /// <param name="b1">Use 0 for no button.</param>
        /// <param name="keyArray">One or more comma seperated Keys.</param>
        public MetaButton(Buttons b0, Buttons b1, params Keys[] keyArray)
        {
            this.b0 = b0;
            this.b1 = b1;

            // If there's only one valid button, make sure it's b0.
            if (b0 == 0 && b1 != 0)
            {
                b0 = b1;
                b1 = 0;
            }

            foreach (Keys k in keyArray)
            {
                keys.Add(k);
            }
        }

        public void ClearAllWasPressedState()
        {
            ClearAllWasPressedState(0);
        }

        public void ClearAllWasPressedState(int numFrames)
        {
            if (b0 != 0)
            {
                GamePadInput pad = GamePadInput.GetGamePad0();
                GamePadInput.Button b = pad.GetButton(b0);
                b.ClearAllWasPressedState();
                b.IgnoreUntilReleased = true;
                if (b1 != 0)
                {
                    b = pad.GetButton(b0);
                    b.ClearAllWasPressedState();
                    b.IgnoreUntilReleased = true;
                }
                if (numFrames > 0)
                {
                    GamePadInput.ClearAllWasPressedState(numFrames);
                }
            }

            for (int i = 0; i < keys.Count; i++)
            {
                KeyboardInput.ClearAllWasPressedState(keys[i]);
            }
        }   // end of ClearAllWasPressedState()

        /// <summary>
        /// Ignore all buttons and keys associated with this metabutton
        /// until they have been released. Note these happen independently, so if
        /// this metabutton maps to A & B, and A & B are pressed when this is called,
        /// then as soon as A is released it can be pressed, even though B has not
        /// been released yet.
        /// </summary>
        public void IgnoreUntilReleased()
        {
            if (b0 != 0)
            {
                GamePadInput.IgnoreUntilReleased(b0);
                if (b1 != 0)
                {
                    GamePadInput.IgnoreUntilReleased(b1);
                }
            }
            for (int i = 0; i < keys.Count; ++i)
            {
                KeyboardInput.IgnoreUntilReleased(keys[i]);
            }
        }

        #endregion

    }   // end of class MetaButton

}   // end of namespace Boku.Common
