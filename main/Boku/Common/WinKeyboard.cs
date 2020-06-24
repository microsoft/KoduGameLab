
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

#if !NETFX_CORE
    using System.Windows.Forms;
#endif

using System.Runtime.InteropServices;

namespace Boku.Common
{
#if !NETFX_CORE
    /// <summary>
    /// Class that handles intercepting Windows keyboard messages.
    /// TODO Should this be IDispose?
    /// </summary>
    public class WinKeyboard : NativeWindow
    {
        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        static extern int ToUnicode(uint virtualKey, uint scanCode, byte[] keyStates, [MarshalAs(UnmanagedType.LPArray)] [Out] char[] chars, int charMaxCount, uint flags);
        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        static extern bool GetKeyboardState([Out] byte[] keyStates);

        #region Constants

        /// <summary>
        /// Flag returned by a window in response to WM_GETDLGCODE to indicate that it
        /// is interested in receiving WM_CHAR messages.
        /// </summary>
        public const int DLGC_WANTCHARS = 0x0080;

        /// <summary>
        /// Flag returned by a window in response to WM_GETDLGCODE to indicate that it
        /// wants to process all keyboard input from the user.
        /// </summary>
        public const int DLGC_WANTALLKEYS = 0x0004;

        /// <summary>
        /// Bit in lparam that indicates whether a WM_KEYDOWN is being generated due
        /// to the keyboard's auto-repeat.
        /// </summary>
        public const int WM_KEYDOWN_WASDOWN = (1 << 30);

        /// <summary>List of window message relevant to the input capturer</summary>
        public enum WindowMessages : int
        {
            /// <summary>
            /// Sent to a window to ask which types of input it processes.
            /// </summary>
            WM_GETDLGCODE = 0x0087,

            /// <summary>
            /// Transmits raw input data to the window.
            /// </summary>
            WM_INPUT = 0x00FF,

            /// <summary>
            /// Indicates that the user has pressed a key on the keyboard.
            /// </summary>
            WM_KEYDOWN = 0x0100,

            /// <summary>
            /// Indicates that the user has released a key on the keyboard.
            /// </summary>
            WM_KEYUP = 0x0101,

            /// <summary>
            /// Indicates that the user has entered text.
            /// </summary>
            WM_CHAR = 0x0102,

            /// <summary>
            /// Indicates that the mouse wheel has been rotated.
            /// </summary>
            WM_MOUSEWHEEL = 0x020A,


            WM_IME_REPORT = 0x0280,
            WM_IME_SETCONTEXT = 0x0281,
            WM_IME_NOTIFY = 0x0282,
            WM_IME_CONTROL = 0x0283,
            WM_IME_COMPOSITIONFULL = 0x0284,
            WM_IME_SELECT = 0x0285,
            WM_IME_CHAR = 0x0286,
            WM_IME_REQUEST = 0x0288,
            WM_IME_KEYDOWN = 0x0290,
            WM_IME_KEYUP = 0x0291,
        }

        #endregion

        #region Members

        /// <summary>
        /// Flags that will be added to the result of WM_GETDLGCODE
        /// </summary>
        const int DlgCodeFlags = (DLGC_WANTALLKEYS | DLGC_WANTCHARS);

        /// <summary>
        /// Raw key down event handler.
        /// </summary>
        public KeyboardInput.KeyboardKeyEvent KeyPressed;

        /// <summary>
        /// Raw key up event handler.
        /// </summary>
        public KeyboardInput.KeyboardKeyEvent KeyReleased;

        /// <summary>
        /// Processed character event handler.
        /// </summary>
        public KeyboardInput.KeyboardCharEvent CharacterEntered;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public WinKeyboard(Form form)
        {
            // Attach the input grabber to the window.
            IntPtr mainformHandle = MainForm.Instance.Handle;
            IntPtr xnaControlHandle = XNAControl.Instance.Handle;
            AssignHandle(xnaControlHandle);

            // Uncomment for forms based keyboard input.
            //form.KeyPreview = true;
            //form.KeyPress += new KeyPressEventHandler(KeyPressHandler);
            //form.KeyDown += new KeyEventHandler(KeyDownHandler);
        }

        #endregion

        #region Internal

        // Flag for Greek tonos(accent) character.
        bool tonos = false;
        char tonosGlyph = (char)0x384;

        // Flag for SpanishAccent characters.
        bool spanishSingleAccent = false;
        char spanishSingleAccentGlyph = (char)0xb4;
        bool spanishDoubleAccent = false;
        char spanishDoubleAccentGlyph = (char)0xa8;

        /// <summary>
        /// WndProc to handle input messages.
        /// </summary>
        /// <param name="message">Window message sent to the window</param>
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x0240)
            {
            }

            base.WndProc(ref message);

            switch (message.Msg)
            {
                case (int)WindowMessages.WM_GETDLGCODE:
                    {
                        // Window is being asked which types of input it can process.
                        int returnCode = message.Result.ToInt32();
                        returnCode |= DlgCodeFlags;
                        message.Result = new IntPtr(returnCode);
                    }
                    break;

                case (int)WindowMessages.WM_KEYDOWN:
                    {
                        //Debug.Assert(false, "We're using forms based keyboard input now, why are we here?");
                        
                        // Key on the keyboard was pressed.
                        int virtualKeyCode = message.WParam.ToInt32();
                        OnKeyPressed((Keys)virtualKeyCode);

                        {
                            char[] chars = new char[1];
                            uint flags = 0;
                            byte[] keyStates = new byte[0x100];
                            GetKeyboardState(keyStates);

                            int result = ToUnicode((uint)virtualKeyCode, (uint)virtualKeyCode, keyStates, chars, chars.Length, flags);

                            if (result == -1)
                            {
                                // Unicode, dead key.
                            }
                            else if (result == 0)
                            {
                                // Key has no Unicode value.
                            }
                            else if (result == 1)
                            {

                                // If tonos is true then the previous character was the tonos.
                                // If the following character is valid then pass through the 
                                // accented version of the character.  If not, then pass through
                                // both the tonos and the character.
                                if (tonos)
                                {
                                    // Add tonos if valid.
                                    switch(chars[0])
                                    {
                                        // Alpha
                                        case (char)0x03b1:
                                            chars[0] = (char)0x3ac;
                                            break;

                                        // Epsilon
                                        case (char)0x03b5:
                                            chars[0] = (char)0x3ad;
                                            break;

                                        // Eta
                                        case (char)0x03b7:
                                            chars[0] = (char)0x3ae;
                                            break;

                                        // Iota
                                        case (char)0x03b9:
                                            chars[0] = (char)0x3af;
                                            break;

                                        // Omicron
                                        case (char)0x03bf:
                                            chars[0] = (char)0x3cc;
                                            break;

                                        // Upsilon
                                        case (char)0x03c5:
                                            chars[0] = (char)0x3cd;
                                            break;

                                        // Omega
                                        case (char)0x03c9:
                                            chars[0] = (char)0x3ce;
                                            break;

                                        default:
                                            // Need to pass on both the tonos and the character.
                                            // Pass on tonos here.  The character will be sent below.
                                            OnCharacterEntered(tonosGlyph);
                                            break;
                                    }

                                    tonos = false;
                                }   // end of if tonos
                                else if (spanishSingleAccent)
                                {
                                    // Add accent if valid.
                                    switch(chars[0])
                                    {
                                        case 'a': chars[0] = 'á'; break;
                                        case 'A': chars[0] = 'Á'; break;
                                        case 'e': chars[0] = 'é'; break;
                                        case 'E': chars[0] = 'É'; break;
                                        case 'i': chars[0] = 'í'; break;
                                        case 'I': chars[0] = 'Í'; break;
                                        case 'o': chars[0] = 'ó'; break;
                                        case 'O': chars[0] = 'Ó'; break;
                                        case 'u': chars[0] = 'ú'; break;
                                        case 'U': chars[0] = 'Ú'; break;

                                        default:
                                            // Need to pass on both the accent and the character.
                                            // Pass on accent here.  The character will be sent below.
                                            // WHY?  No clue.
                                            OnCharacterEntered(spanishSingleAccentGlyph);
                                            break;
                                    }

                                    spanishSingleAccent = false;
                                }   // end of spanishSingleAccent
                                else if (spanishDoubleAccent)
                                {
                                    // Add accent if valid.
                                    switch (chars[0])
                                    {
                                        case 'u': chars[0] = 'ü'; break;
                                        case 'U': chars[0] = 'Ü'; break;

                                        default:
                                            // Need to pass on both the accent and the character.
                                            // Pass on accent here.  The character will be sent below.
                                            // WHY?  No clue.
                                            OnCharacterEntered(spanishDoubleAccentGlyph);
                                            break;
                                    }

                                    spanishDoubleAccent = false;
                                }   // end if spanishDoubleAccent
                                else if (chars[0] == tonosGlyph)
                                {
                                    // Tonos was typed.  Just hold on to it until the next character.
                                    tonos = true;
                                }
                                else if (chars[0] == spanishSingleAccentGlyph)
                                {
                                    spanishSingleAccent = true;
                                }
                                else if (chars[0] == spanishDoubleAccentGlyph)
                                {
                                    spanishDoubleAccent = true;
                                }

                                if (!tonos && !spanishSingleAccent && !spanishDoubleAccent)
                                {
                                    OnCharacterEntered(chars[0]);
                                }
                            }
                            else
                            {
                                // Unexpected return value.
                            }
                        }
                        break;
                    }

                case (int)WindowMessages.WM_KEYUP:
                    {
                        // Key on the keyboard was released.
                        int virtualKeyCode = message.WParam.ToInt32();
                        OnKeyReleased((Keys)virtualKeyCode);
                        break;
                    }

                case (int)WindowMessages.WM_CHAR:
                    {
                        // Processed character.
                        char character = (char)message.WParam.ToInt32();
                        //OnCharacterEntered(character);
                        break;
                    }

                case (int)WindowMessages.WM_MOUSEWHEEL:
                    {
                        // For some reason I don't understand yet, this WndProc blocks the
                        // normal reading of the mouse scroll wheel.  So, read it here and
                        // pass along the into to MouseInput.
                        short ticks = (short)(message.WParam.ToInt32() >> 16);
                        //MouseInputOld.ExternalScrollValue += ticks;
                        break;
                    }

                case (int)WindowMessages.WM_IME_REPORT:
                    {
                        break;
                    }

                case (int)WindowMessages.WM_IME_SETCONTEXT:
                    {
                        break;
                    }
                
                case (int)WindowMessages.WM_IME_NOTIFY:
                    {
                        break;
                    }
                
                case (int)WindowMessages.WM_IME_CONTROL:
                    {
                        break;
                    }
                
                case (int)WindowMessages.WM_IME_COMPOSITIONFULL:
                    {
                        break;
                    }
                
                case (int)WindowMessages.WM_IME_SELECT:
                    {
                        break;
                    }
                
                // Characters input via IME come in here.  Pass them along.
                case (int)WindowMessages.WM_IME_CHAR:
                    {
                        // Processed character.
                        char character = (char)message.WParam.ToInt32();
                        OnCharacterEntered(character);
                        break;
                    }
                
                case (int)WindowMessages.WM_IME_REQUEST:
                    {
                        break;
                    }
                
                case (int)WindowMessages.WM_IME_KEYDOWN:
                    {
                        break;
                    }
                
                case (int)WindowMessages.WM_IME_KEYUP:
                    {
                        break;
                    }
            }   // end of switch on message.

        }   // end of WndProc

        
        protected void OnKeyPressed(Keys key)
        {
            if (KeyPressed != null)
            {
                Debug.Assert(false);
                // TODO This has a conflict since Windows.Keys != XNA.Keys
                //KeyPressed(key);
            }
        }

        protected void OnKeyReleased(Keys key)
        {
            if (KeyReleased != null)
            {
                Debug.Assert(false);
                // TODO This has a conflict since Windows.Keys != XNA.Keys
                //KeyReleased(key);
            }
        }
        
        protected void OnCharacterEntered(char character)
        {
            if (CharacterEntered != null)
            {
                CharacterEntered(character);
            }
        }

        void KeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e != null)
            {
                char[] chars = new char[1];
                uint flags = 0;
                byte[] keyStates = new byte[0x100];
                GetKeyboardState(keyStates);

                int result = ToUnicode((uint)e.KeyValue, (uint)e.KeyCode, keyStates, chars, chars.Length, flags);

                if (result == -1)
                {
                    // Unicode, dead key.
                }
                else if (result == 0)
                {
                    // Key has no Unicode value.
                }
                else if (result == 1)
                {
                    OnCharacterEntered(chars[0]);
                    e.SuppressKeyPress = true;
                }
                else
                {
                    // Unexpected return value.
                }
            }
        }

        /// <summary>
        /// Forms based keyboard handler.  Gathers inputs and puts into a list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void KeyPressHandler(object sender, KeyPressEventArgs e)
        {
            if (e != null)
            {
            }
        }


        #endregion
    }   // end of class WinKeyboard
#endif
}   // end of namespace Boku.Common
