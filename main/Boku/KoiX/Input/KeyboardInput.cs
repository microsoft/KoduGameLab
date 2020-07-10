// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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
    /// XNA based version of keyboard input.
    /// </summary>
    public static class KeyboardInputX
    {
        const char none = (char)0;

        // A class to allow us to map Keys values to characters.
        public class KeyCharMapping
        {
            public char normal;
            public char shift;
            public char green;      // ChatPad
            public char orange;     // ChatPad

            // c'tor
            public KeyCharMapping(char normal, char shift)
            {
                this.normal = normal;
                this.shift = shift;
                this.green = none;
                this.orange = none;
            }

            public KeyCharMapping(char normal, char shift, char green, char orange)
            {
                this.normal = normal;
                this.shift = shift;
                this.green = green;
                this.orange = orange;
            }
        }

        #region Members

        const int kNumKeys = 255;

        /// <summary>
        /// Keys act exactly like buttons so use the same class.
        /// </summary>
        static GamePadInput.Button[] keys = new GamePadInput.Button[kNumKeys];
        static bool keysInitialized = false;

        #region KeyCharMap
        static KeyCharMapping[] keyCharMap = {
                                                           new KeyCharMapping(none, none),     // 0
                                                           new KeyCharMapping(none, none),     // 1
                                                           new KeyCharMapping(none, none),     // 2
                                                           new KeyCharMapping(none, none),     // 3
                                                           new KeyCharMapping(none, none),     // 4
                                                           new KeyCharMapping(none, none),     // 5
                                                           new KeyCharMapping(none, none),     // 6
                                                           new KeyCharMapping(none, none),     // 7
                                                           new KeyCharMapping(none, none),     // 8 backspace
                                                           new KeyCharMapping(none, none),     // 9 tab
                                                           new KeyCharMapping(none, none),     // 10
                                                           new KeyCharMapping(none, none),     // 11
                                                           new KeyCharMapping(none, none),     // 12
                                                           new KeyCharMapping(none, none),     // 13 enter
                                                           new KeyCharMapping(none, none),     // 14
                                                           new KeyCharMapping(none, none),     // 15
                                                           new KeyCharMapping(none, none),     // 16
                                                           new KeyCharMapping(none, none),     // 17
                                                           new KeyCharMapping(none, none),     // 18
                                                           new KeyCharMapping(none, none),     // 19 pause
                                                           new KeyCharMapping(none, none),     // 20 capslock
                                                           new KeyCharMapping(none, none),     // 21
                                                           new KeyCharMapping(none, none),     // 22
                                                           new KeyCharMapping(none, none),     // 23
                                                           new KeyCharMapping(none, none),     // 24
                                                           new KeyCharMapping(none, none),     // 25
                                                           new KeyCharMapping(none, none),     // 26
                                                           new KeyCharMapping(none, none),     // 27 escape
                                                           new KeyCharMapping(none, none),     // 28
                                                           new KeyCharMapping(none, none),     // 29
                                                           new KeyCharMapping(none, none),     // 30
                                                           new KeyCharMapping(none, none),     // 31
                                                           new KeyCharMapping(' ', ' '),       // 32 space
                                                           new KeyCharMapping(none, none),     // 33 page up
                                                           new KeyCharMapping(none, none),     // 34 page down
                                                           new KeyCharMapping(none, none),     // 35 end
                                                           new KeyCharMapping(none, none),     // 36 home
                                                           new KeyCharMapping(none, none),     // 37 left
                                                           new KeyCharMapping(none, none),     // 38 up
                                                           new KeyCharMapping(none, none),     // 39 right
                                                           new KeyCharMapping(none, none),     // 40 down
                                                           new KeyCharMapping(none, none),     // 41 select
                                                           new KeyCharMapping(none, none),     // 42 print
                                                           new KeyCharMapping(none, none),     // 43 execute
                                                           new KeyCharMapping(none, none),     // 44 print screen
                                                           new KeyCharMapping(none, none),     // 45 insert
                                                           new KeyCharMapping(none, none),     // 46 delete
                                                           new KeyCharMapping(none, none),     // 47 help
                                                           new KeyCharMapping('0', ')'),       // 48
                                                           new KeyCharMapping('1', '!'),       // 49
                                                           new KeyCharMapping('2', '@'),       // 50
                                                           new KeyCharMapping('3', '#'),       // 51
                                                           new KeyCharMapping('4', '$'),       // 52
                                                           new KeyCharMapping('5', '%'),       // 53
                                                           new KeyCharMapping('6', '^'),       // 54
                                                           new KeyCharMapping('7', '&'),       // 55
                                                           new KeyCharMapping('8', '*'),       // 56
                                                           new KeyCharMapping('9', '('),       // 57
                                                           new KeyCharMapping(none, none),     // 58
                                                           new KeyCharMapping(none, none),     // 59
                                                           new KeyCharMapping(none, none),     // 60
                                                           new KeyCharMapping(none, none),     // 61
                                                           new KeyCharMapping(none, none),     // 62
                                                           new KeyCharMapping(none, none),     // 63
                                                           new KeyCharMapping(none, none),     // 64
                                                           new KeyCharMapping('a', 'A', '~', 'á'),       // 65
                                                           new KeyCharMapping('b', 'B', '|', '+'),       // 66
                                                           new KeyCharMapping('c', 'C', '»', 'ç'),       // 67
                                                           new KeyCharMapping('d', 'D', '{', 'ò'),       // 68
                                                           new KeyCharMapping('e', 'E', (char)128, 'é'),       // 69, 128 is the Euro symbol
                                                           new KeyCharMapping('f', 'F', '}', '£'),       // 70
                                                           new KeyCharMapping('g', 'G', '¨', '¥'),       // 71
                                                           new KeyCharMapping('h', 'H', '/', '\\'),      // 72
                                                           new KeyCharMapping('i', 'I', '*', 'í'),       // 73
                                                           new KeyCharMapping('j', 'J', '\'', '"'),      // 74
                                                           new KeyCharMapping('k', 'K', '[', '¤'),       // 75
                                                           new KeyCharMapping('l', 'L', ']', 'ø'),       // 76
                                                           new KeyCharMapping('m', 'M', '>', 'µ'),       // 77
                                                           new KeyCharMapping('n', 'N', '<', 'ñ'),       // 78
                                                           new KeyCharMapping('o', 'O', '(', 'ó'),       // 79
                                                           new KeyCharMapping('p', 'P', ')', '='),       // 80
                                                           new KeyCharMapping('q', 'Q', '!', '¡'),       // 81
                                                           new KeyCharMapping('r', 'R', '#', '$'),       // 82
                                                           new KeyCharMapping('s', 'S', 'š', 'ß'),       // 83
                                                           new KeyCharMapping('t', 'T', '%', 'þ'),       // 84
                                                           new KeyCharMapping('u', 'U', '&', 'ú'),       // 85
                                                           new KeyCharMapping('v', 'V', '-', '_'),       // 86
                                                           new KeyCharMapping('w', 'W', '@', 'å'),       // 87
                                                           new KeyCharMapping('x', 'X', '«', 'œ'),       // 88
                                                           new KeyCharMapping('y', 'Y', '^', 'ý'),       // 89
                                                           new KeyCharMapping('z', 'Z', '`', 'æ'),       // 90
                                                           new KeyCharMapping(none, none),     // 91 left windows
                                                           new KeyCharMapping(none, none),     // 92 right windows
                                                           new KeyCharMapping(none, none),     // 93 apps
                                                           new KeyCharMapping(none, none),     // 94
                                                           new KeyCharMapping(none, none),     // 95 sleep
                                                           new KeyCharMapping('0', '0'),       // 96 numpad0
                                                           new KeyCharMapping('1', '1'),       // 97 numpad1
                                                           new KeyCharMapping('2', '2'),       // 98 numpad2
                                                           new KeyCharMapping('3', '3'),       // 99 numpad3
                                                           new KeyCharMapping('4', '4'),       // 100 numpad4
                                                           new KeyCharMapping('5', '5'),       // 101 numpad5
                                                           new KeyCharMapping('6', '6'),       // 102 numpad6
                                                           new KeyCharMapping('7', '7'),       // 103 numpad7
                                                           new KeyCharMapping('8', '8'),       // 104 numpad8
                                                           new KeyCharMapping('9', '9'),       // 105 numpad9
                                                           new KeyCharMapping('*', '*'),       // 106 multiply
                                                           new KeyCharMapping('+', '+'),       // 107 add
                                                           new KeyCharMapping(none, none),     // 108 separator
                                                           new KeyCharMapping('-', '-'),       // 109 subtract
                                                           new KeyCharMapping('.', '.'),       // 110 decimal
                                                           new KeyCharMapping('/', '/'),       // 111 divide
                                                           new KeyCharMapping(none, none),     // 112
                                                           new KeyCharMapping(none, none),     // 113
                                                           new KeyCharMapping(none, none),     // 114
                                                           new KeyCharMapping(none, none),     // 115
                                                           new KeyCharMapping(none, none),     // 116
                                                           new KeyCharMapping(none, none),     // 117
                                                           new KeyCharMapping(none, none),     // 118
                                                           new KeyCharMapping(none, none),     // 119
                                                           new KeyCharMapping(none, none),     // 120
                                                           new KeyCharMapping(none, none),     // 121
                                                           new KeyCharMapping(none, none),     // 122
                                                           new KeyCharMapping(none, none),     // 123
                                                           new KeyCharMapping(none, none),     // 124
                                                           new KeyCharMapping(none, none),     // 125
                                                           new KeyCharMapping(none, none),     // 126
                                                           new KeyCharMapping(none, none),     // 127
                                                           new KeyCharMapping(none, none),     // 128
                                                           new KeyCharMapping(none, none),     // 129
                                                           new KeyCharMapping(none, none),     // 130
                                                           new KeyCharMapping(none, none),     // 131
                                                           new KeyCharMapping(none, none),     // 132
                                                           new KeyCharMapping(none, none),     // 133
                                                           new KeyCharMapping(none, none),     // 134
                                                           new KeyCharMapping(none, none),     // 135
                                                           new KeyCharMapping(none, none),     // 136
                                                           new KeyCharMapping(none, none),     // 137
                                                           new KeyCharMapping(none, none),     // 138
                                                           new KeyCharMapping(none, none),     // 139
                                                           new KeyCharMapping(none, none),     // 140
                                                           new KeyCharMapping(none, none),     // 141
                                                           new KeyCharMapping(none, none),     // 142
                                                           new KeyCharMapping(none, none),     // 143
                                                           new KeyCharMapping(none, none),     // 144 numlock
                                                           new KeyCharMapping(none, none),     // 145 scroll
                                                           new KeyCharMapping(none, none),     // 146
                                                           new KeyCharMapping(none, none),     // 147
                                                           new KeyCharMapping(none, none),     // 148
                                                           new KeyCharMapping(none, none),     // 149
                                                           new KeyCharMapping(none, none),     // 150
                                                           new KeyCharMapping(none, none),     // 151
                                                           new KeyCharMapping(none, none),     // 152
                                                           new KeyCharMapping(none, none),     // 153
                                                           new KeyCharMapping(none, none),     // 154
                                                           new KeyCharMapping(none, none),     // 155
                                                           new KeyCharMapping(none, none),     // 156
                                                           new KeyCharMapping(none, none),     // 157
                                                           new KeyCharMapping(none, none),     // 158
                                                           new KeyCharMapping(none, none),     // 159
                                                           new KeyCharMapping(none, none),     // 160 left shift
                                                           new KeyCharMapping(none, none),     // 161 right shift
                                                           new KeyCharMapping(none, none),     // 162 left control
                                                           new KeyCharMapping(none, none),     // 163 right control
                                                           new KeyCharMapping(none, none),     // 164 left alt
                                                           new KeyCharMapping(none, none),     // 165 right alt
                                                           new KeyCharMapping(none, none),     // 166 browser back
                                                           new KeyCharMapping(none, none),     // 167 brwoser forward
                                                           new KeyCharMapping(none, none),     // 168 browser refresh
                                                           new KeyCharMapping(none, none),     // 169 browser stop
                                                           new KeyCharMapping(none, none),     // 170 browser search
                                                           new KeyCharMapping(none, none),     // 171 browser favorites
                                                           new KeyCharMapping(none, none),     // 172 browser home
                                                           new KeyCharMapping(none, none),     // 173 volume mute
                                                           new KeyCharMapping(none, none),     // 174 volume down
                                                           new KeyCharMapping(none, none),     // 175 volume up
                                                           new KeyCharMapping(none, none),     // 176
                                                           new KeyCharMapping(none, none),     // 177
                                                           new KeyCharMapping(none, none),     // 178
                                                           new KeyCharMapping(none, none),     // 179
                                                           new KeyCharMapping(none, none),     // 180
                                                           new KeyCharMapping(none, none),     // 181
                                                           new KeyCharMapping(none, none),     // 182
                                                           new KeyCharMapping(none, none),     // 183
                                                           new KeyCharMapping(none, none),     // 184
                                                           new KeyCharMapping(none, none),     // 185
                                                           new KeyCharMapping(';', ':'),                // 186 oem semicolon
                                                           new KeyCharMapping('=', '+'),                // 187 oem plus
                                                           new KeyCharMapping(',', '<', ':', ';'),      // 188 oem comma
                                                           new KeyCharMapping('-', '_'),                // 189 oem minus
                                                           new KeyCharMapping('.', '>', '?', '¿'),      // 190 oem period
                                                           new KeyCharMapping('/', '?'),                // 191 oem question
                                                           new KeyCharMapping('`', '~'),                // 192 oem tilde
                                                           new KeyCharMapping(none, none),     // 193
                                                           new KeyCharMapping(none, none),     // 194
                                                           new KeyCharMapping(none, none),     // 195
                                                           new KeyCharMapping(none, none),     // 196
                                                           new KeyCharMapping(none, none),     // 197
                                                           new KeyCharMapping(none, none),     // 198
                                                           new KeyCharMapping(none, none),     // 199
                                                           new KeyCharMapping(none, none),     // 200
                                                           new KeyCharMapping(none, none),     // 201
                                                           new KeyCharMapping(none, none),     // 202
                                                           new KeyCharMapping(none, none),     // 203
                                                           new KeyCharMapping(none, none),     // 204
                                                           new KeyCharMapping(none, none),     // 205
                                                           new KeyCharMapping(none, none),     // 206
                                                           new KeyCharMapping(none, none),     // 207
                                                           new KeyCharMapping(none, none),     // 208
                                                           new KeyCharMapping(none, none),     // 209
                                                           new KeyCharMapping(none, none),     // 210
                                                           new KeyCharMapping(none, none),     // 211
                                                           new KeyCharMapping(none, none),     // 212
                                                           new KeyCharMapping(none, none),     // 213
                                                           new KeyCharMapping(none, none),     // 214
                                                           new KeyCharMapping(none, none),     // 215
                                                           new KeyCharMapping(none, none),     // 216
                                                           new KeyCharMapping(none, none),     // 217
                                                           new KeyCharMapping(none, none),     // 218
                                                           new KeyCharMapping('[', '{'),       // 219 oem open brackets
                                                           new KeyCharMapping('\\', '|'),      // 220 oem pipe
                                                           new KeyCharMapping(']', '}'),       // 221 oem close brackets
                                                           new KeyCharMapping('\'', '"'),      // 222 oem quotes
                                                           new KeyCharMapping(none, none),     // 223 oem 8
                                                           new KeyCharMapping(none, none),     // 224
                                                           new KeyCharMapping(none, none),     // 225
                                                           new KeyCharMapping('\\', '|'),      // 226 oem backslash
                                                           new KeyCharMapping(none, none),     // 227
                                                           new KeyCharMapping(none, none),     // 228
                                                           new KeyCharMapping(none, none),     // 229
                                                           new KeyCharMapping(none, none),     // 230
                                                           new KeyCharMapping(none, none),     // 231
                                                           new KeyCharMapping(none, none),     // 232
                                                           new KeyCharMapping(none, none),     // 233
                                                           new KeyCharMapping(none, none),     // 234
                                                           new KeyCharMapping(none, none),     // 235
                                                           new KeyCharMapping(none, none),     // 236
                                                           new KeyCharMapping(none, none),     // 237
                                                           new KeyCharMapping(none, none),     // 238
                                                           new KeyCharMapping(none, none),     // 239
                                                           new KeyCharMapping(none, none),     // 240
                                                           new KeyCharMapping(none, none),     // 241
                                                           new KeyCharMapping(none, none),     // 242
                                                           new KeyCharMapping(none, none),     // 243
                                                           new KeyCharMapping(none, none),     // 244
                                                           new KeyCharMapping(none, none),     // 245
                                                           new KeyCharMapping(none, none),     // 246
                                                           new KeyCharMapping(none, none),     // 247
                                                           new KeyCharMapping(none, none),     // 248
                                                           new KeyCharMapping(none, none),     // 249
                                                           new KeyCharMapping(none, none),     // 250
                                                           new KeyCharMapping(none, none),     // 251
                                                           new KeyCharMapping(none, none),     // 252
                                                           new KeyCharMapping(none, none),     // 253
                                                           new KeyCharMapping(none, none),     // 254

                                                       };
        #endregion

        static bool[] prevPressed = new bool[kNumKeys];
        static bool[] curPressed = new bool[kNumKeys];
        /// <summary>
        /// Whether any keys were pressed this frame. Set during Update().
        /// </summary>
        static bool wasTouched = false;

        #endregion

        #region Accessors

        /// <summary>
        /// Is a control key (left or right) pressed
        /// </summary>
        public static bool CtrlIsPressed
        {
            get { return IsPressed(Keys.RightControl) || IsPressed(Keys.LeftControl); }
        }
        /// <summary>
        /// Is a shift key (left or right) pressed
        /// </summary>
        public static bool ShiftIsPressed
        {
            get { return IsPressed(Keys.RightShift) || IsPressed(Keys.LeftShift); }
        }
        /// <summary>
        /// Is a alt key (left or right) pressed
        /// </summary>
        public static bool AltIsPressed
        {
            get { return IsPressed(Keys.RightAlt) || IsPressed(Keys.LeftAlt); }
        }
        /// <summary>
        /// Was a control key (left or right) pressed
        /// </summary>
        public static bool CtrlWasPressed
        {
            get { return WasPressed(Keys.RightControl) || WasPressed(Keys.LeftControl); }
        }
        /// <summary>
        /// Was a shift key (left or right) pressed
        /// </summary>
        public static bool ShiftWasPressed
        {
            get { return WasPressed(Keys.RightShift) || WasPressed(Keys.LeftShift); }
        }
        /// <summary>
        /// Was a alt key (left or right) pressed
        /// </summary>
        public static bool AltWasPressed
        {
            get { return WasPressed(Keys.RightAlt) || WasPressed(Keys.LeftAlt); }
        }

        /// <summary>
        /// Return whether any keys were pressed this frame. Currently is an OR of
        /// all keys[].WasPressed. Might make sense to be keys[].IsPressed.
        /// </summary>
        public static bool WasTouched
        {
            get { return wasTouched; }
        }

        #endregion

        #region Public

#if XBOX
        // On the chatpad these need to be made "sticky"
        static bool chatPadShift = false;
        static bool chatPadGreen = false;
        static bool chatPadOrange = false;
        static bool chatPadCapsLock = false;    // Toggled by orange-capslock
#endif

        /// <summary>
        /// If the game loses focus, we want to clear and keyboard state
        /// so autorepeat doesn't go nuts.
        /// </summary>
        public static void LostFocusEventHandler(object sender, EventArgs e)
        {
            for (int i = 0; i < kNumKeys; i++)
            {
                keys[i].ClearAllWasPressedState();
                keys[i].IsPressed = false;
            }

        }   // end of LostFocus()


        public static void Init()
        {
            if (!keysInitialized)
            {
                for (int i = 0; i < kNumKeys; i++)
                {
                    keys[i] = new GamePadInput.Button();
                }
                keysInitialized = true;
            }
        }   // end of Init();

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True if WasTouched is true.</returns>
        public static bool Update()
        {
            Debug.Assert(keysInitialized);

            // If this count isn't right, we may have accidentally deleted an entry.
            Debug.Assert(keyCharMap.Length == kNumKeys);

            /*
            // Ignore input if the guide is visible or game is not active.
            if (Morph.UI.Sharing.GamerServices.IsGuideVisible || !BokuGame.bokuGame.IsActive)
            {
                return;
            }
            */

            KeyboardState curState = Keyboard.GetState();

            // Move last frame's state to prev and get new state.
            for (int i = 1; i < kNumKeys; i++)
            {
                prevPressed[i] = curPressed[i];

                curPressed[i] = curState[(Keys)i] == KeyState.Down;
            }

#if XBOX
            // Blend chat pad state into curState
            // Note, no need to remap inputs since we're treating them all the same.
            for(int p=0; p<4; p++)
            {
                GamePadState pad = GamePad.GetState((PlayerIndex)p);

                if (pad.IsConnected)
                {
                    KeyboardState padState = Keyboard.GetState((PlayerIndex)p);
                    for (int i = 0; i < kNumKeys; i++)
                    {
                        curPressed[i] |= padState[(Keys)i] == KeyState.Down;
                    }
                }
            }
            
#endif

            wasTouched = false;

            // Update all keys.
            // Ignore 0.  Maybe others?
            for (int i = 1; i < kNumKeys; i++)
            {
                ButtonState state = curPressed[i] ? ButtonState.Pressed : ButtonState.Released;
                keys[i].Update(state);

#if !XBOX
                // Only mark us as touched on the PC. Don't want the chatpad or
                // USB keyboard throwing the UI into keyboard/mouse mode on the 360.
                if (keys[i].WasPressedOrRepeat)
                {
#if DEBUG
                    // In debug mode, we want to ignore F5.  This makes it easier to
                    // debug input state changes.  For instance, when trying to debug 
                    // gamepad input, it messes things up if we keep switching to
                    // keyboard input.
                    if(i!=116)
                    {
#endif
                        // Submit key events to InputEventManager.
                        Keys key = (Keys)i;
                        KeyInput input = new KeyInput(Time.WallClockTotalSeconds, key, KeyToChar(key));
                        KoiLibrary.InputEventManager.ProcessKeyboardEvent(input);
                        wasTouched = true;
#if DEBUG
                    }
#endif
                }

#endif
            }

#if XBOX
            // Handle sticky keys.
            if (keys[(int)Keys.LeftShift].WasPressed)
            {
                chatPadShift = !chatPadShift;
            }

            if (chatPadOrange && keys[(int)Keys.CapsLock].WasPressed)
            {
                chatPadCapsLock = !chatPadCapsLock;
                chatPadOrange = false;
            }

            if (keys[(int)Keys.ChatPadGreen].WasPressed)
            {
                chatPadGreen = !chatPadGreen;
                chatPadOrange = false;
            }

            if (keys[(int)Keys.ChatPadOrange].WasPressed)
            {
                chatPadOrange = !chatPadOrange;
                chatPadGreen = false;
            }
#endif

            return wasTouched;

        }   // end of Update()

        public static void ShowOnScreenKeyboard()
        {
            Debug.Assert(false);
        }   // end of ShowOnscreenKeyboard()

        /// <summary>
        /// Is the specified key currently pressed?
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool IsPressed(Keys key)
        {
            return keys[(int)key].IsPressed;
        }   // end of IsPressed()

        /// <summary>
        /// Short cut to make checking for control-key presses easier.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool IsCtrlPressed(Keys key)
        {
            return (keys[(int)Keys.LeftControl].IsPressed || keys[(int)Keys.RightControl].IsPressed) && keys[(int)key & 0xff].IsPressed;
        }   // end of IsCtrlPressed()

        /// <summary>
        /// Short cut to make checking for shift-key presses easier.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool IsShiftPressed(Keys key)
        {
            return (keys[(int)Keys.LeftShift].IsPressed || keys[(int)Keys.RightShift].IsPressed) && keys[(int)key & 0xff].IsPressed;
        }   // end of IsShiftPressed()

        /// <summary>
        /// Short cut to make checking for alt-key presses easier.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool IsAltPressed(Keys key)
        {
            return (keys[(int)Keys.LeftAlt].IsPressed || keys[(int)Keys.RightAlt].IsPressed) && keys[(int)key & 0xff].IsPressed;
        }   // end of IsAltPressed()

        /// <summary>
        /// Was the specified key pressed this frame?
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool WasPressed(Keys key)
        {
            return keys[(int)key].WasPressed;
        }   // end of WasPressed()

        /// <summary>
        /// Short cut to make checking for control-key presses easier.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool WasCtrlPressed(Keys key)
        {
            return (keys[(int)Keys.LeftControl].IsPressed || keys[(int)Keys.RightControl].IsPressed) && keys[(int)key & 0xff].WasPressed;
        }   // end of WasCtrlPressed()

        /// <summary>
        /// Short cut to make checking for shift-key presses easier.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool WasShiftPressed(Keys key)
        {
            return (keys[(int)Keys.LeftShift].IsPressed || keys[(int)Keys.RightShift].IsPressed) && keys[(int)key & 0xff].WasPressed;
        }   // end of WasShiftPressed()

        /// <summary>
        /// Short cut to make checking for alt-key presses easier.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool WasAltPressed(Keys key)
        {
            return (keys[(int)Keys.LeftAlt].IsPressed || keys[(int)Keys.RightAlt].IsPressed) && keys[(int)key & 0xff].WasPressed;
        }   // end of WasAltPressed()

        /// <summary>
        /// Was the specified key repeat pressed this frame?
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool WasRepeatPressed(Keys key)
        {
            return keys[(int)key].WasRepeatPressed;
        }   // end of WasRepeatPressed()

        /// <summary>
        /// Short cut to make checking for control-key presses easier.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool WasCtrlRepeatPressed(Keys key)
        {
            return (keys[(int)Keys.LeftControl].IsPressed || keys[(int)Keys.RightControl].IsPressed) && keys[(int)key & 0xff].WasRepeatPressed;
        }   // end of WasCtrlRepeatPressed()

        /// <summary>
        /// Short cut to make checking for shift-key presses easier.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool WasShiftRepeatPressed(Keys key)
        {
            return (keys[(int)Keys.LeftShift].IsPressed || keys[(int)Keys.RightShift].IsPressed) && keys[(int)key & 0xff].WasRepeatPressed;
        }   // end of WasShiftRepeatPressed()

        /// <summary>
        /// Short cut to make checking for alt-key presses easier.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool WasAltRepeatPressed(Keys key)
        {
            return (keys[(int)Keys.LeftAlt].IsPressed || keys[(int)Keys.RightAlt].IsPressed) && keys[(int)key & 0xff].WasRepeatPressed;
        }   // end of WasAltRepeatPressed()

        /// <summary>
        /// Was the specified key pressed or repeat pressed this frame?
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool WasPressedOrRepeat(Keys key)
        {
            return keys[(int)key].WasPressedOrRepeat;
        }   // end of WasPressedOrRepeat()

        /// <summary>
        /// Short cut to make checking for control-key presses easier.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool WasCtrlPressedOrRepeat(Keys key)
        {
            return (keys[(int)Keys.LeftControl].IsPressed || keys[(int)Keys.RightControl].IsPressed) && keys[(int)key & 0xff].WasPressedOrRepeat;
        }   // end of WasCtrlPressedOrRepeat()

        /// <summary>
        /// Short cut to make checking for shift-key presses easier.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool WasShiftPressedOrRepeat(Keys key)
        {
            return (keys[(int)Keys.LeftShift].IsPressed || keys[(int)Keys.RightShift].IsPressed) && keys[(int)key & 0xff].WasPressedOrRepeat;
        }   // end of WasShiftPressedOrRepeat()

        /// <summary>
        /// Short cut to make checking for alt-key presses easier.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool WasAltPressedOrRepeat(Keys key)
        {
            return (keys[(int)Keys.LeftAlt].IsPressed || keys[(int)Keys.RightAlt].IsPressed) && keys[(int)key].WasPressedOrRepeat;
        }   // end of WasAltPressedOrRepeat()

        /// <summary>
        /// Clear WasPressed and WasRepeatPressed for the given key.
        /// </summary>
        /// <param name="key"></param>
        public static void ClearAllWasPressedState(Keys key)
        {
            keys[(int)key & 0xff].ClearAllWasPressedState();
        }   // end of ClearAllWasPressedState()

        /// <summary>
        /// Clear WasPressed and WasRepeatPressed for all keys.
        /// </summary>
        public static void ClearAllWasPressedState()
        {
            for (int i = 0; i < kNumKeys; ++i)
            {
                keys[i].ClearAllWasPressedState();
            }
        } // end of ClearAllWasPressedState()

        /// <summary>
        /// Ignore this key's pressed state until it has been released.
        /// </summary>
        /// <param name="key"></param>
        public static void IgnoreUntilReleased(Keys key)
        {
            keys[(int)key & 0xff].IgnoreUntilReleased = true;
        } // end of IgnoreUntilReleased()


        /// <summary>
        /// Returns the key that was pressed this frame.  If none
        /// pressed returns Keys.None.
        /// Note this only returns the first key found pressed this
        /// frame.  There may be more than one.
        /// This should ONLY be used for debug stuff.
        /// </summary>
        /// <returns></returns>
        public static Keys GetKeyPressed()
        {
            Keys result = Keys.None;

            for (int i = 0; i < kNumKeys; i++)
            {
                if (keys[i].WasPressed)
                {
                    result = (Keys)i;
                    break;
                }
            }

            return result;
        }   // end of GetKeyPressed()

        #endregion

        #region Private

        /// <summary>
        /// Maps the given key to the appropriate char value.  Pays attention
        /// to the state of shift and capslock (and orange/green on chatpad).  
        /// Returns 0 if no valid mapping.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        static char KeyToChar(Keys key)
        {
            char result = none;

            bool kbdShift = keys[(int)Keys.LeftShift].IsPressed ||
                            keys[(int)Keys.RightShift].IsPressed;

#if !XBOX
            if (key >= Keys.A && key <= Keys.Z)
            {
                // TODO (****) What is the right Win8 version of this?
                // Do we even want to use this code for Win8???
#if !NETFX_CORE
                kbdShift ^= Console.CapsLock;
#endif
            }

            result = kbdShift ? keyCharMap[(int)key].shift : keyCharMap[(int)key].normal;
#endif

#if XBOX           
            // Skip key modifiers.
            if (key == Keys.LeftShift || key == Keys.CapsLock || key == Keys.ChatPadGreen || key == Keys.ChatPadOrange)
            {
                return none;
            }

            bool chatShift = chatPadShift ^ chatPadCapsLock;
            chatPadShift = false;

            if(key == Keys.OemPeriod || key == Keys.OemComma)
            {
                if (chatPadOrange == true)
                {
                    result = keyCharMap[(int)key].orange;
                    chatPadOrange = false;
                }
                else if (chatPadGreen == true)
                {
                    result = keyCharMap[(int)key].green;
                    chatPadGreen = false;
                }
                else
                {
                    // Only look at tke keyboard shift.  We don't want to apply it on the chatpad.
                    result = kbdShift ? keyCharMap[(int)key].shift : keyCharMap[(int)key].normal;
                }
            }
            else if(key >= Keys.A && key <= Keys.Z)
            {
                if (chatPadOrange == true)
                {
                    result = keyCharMap[(int)key].orange;
                    chatPadOrange = false;
                }
                else if (chatPadGreen == true)
                {
                    result = keyCharMap[(int)key].green;
                    chatPadGreen = false;
                } 
                else
                {
                    result = chatShift || kbdShift ? keyCharMap[(int)key].shift : keyCharMap[(int)key].normal;
                }
            }
            else
            {
                result = keyCharMap[(int)key].normal;
            }
#endif

            return result;
        }   // end of KeyToChar()

        #endregion

    } 
}   // end of namespace KoiX.Input
