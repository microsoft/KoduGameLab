// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Content;

using KoiX.Managers;

using Boku;
using Boku.Common;
using Boku.Common.Sharing;
using Boku.Programming;

namespace KoiX.Input
{
    /// <summary>
    /// A thin wrapper around the GamePad stuff to hide catching of edge transitions, 
    /// support of auto-repeat, treating analog inputs as buttons, etc.
    /// 
    /// *IsPressed functions return true if the button is currently pressed.
    /// *WasPressed functions return true if the button was pressed this frame.
    /// *WasRepeatPressed returns true if a button press was triggered by autorepeat.
    /// *WasReleased functions return true if the button was released this frame.
    ///
    /// To use this class call GamePadInput.Init() once to set things up and then
    /// call GamePadInput.Update() once per frame to update the state.
    /// 
    /// When you need input you can grab the gamepad and query it for state:
    ///     GamePadInput pad = GamePadInput.GetGamePad0();
    ///     if ( pad.AWasPressed ) 
    ///     { 
    ///         /* handle press of A button */ 
    ///     }
    /// 
    ///     If you also want autorepeat to work.
    ///     if ( pad.AWasPressed || pad.AWasRepeatPressed ) 
    ///     { 
    ///         /* handle press of A button */ 
    ///     }
    /// 
    /// GamePad0 is a composite of all four controllers.  This allows multiple 
    /// controllers to provide input at the same time.
    /// 
    /// </summary>
    public partial class GamePadInput : ArbitraryComparable
    {
        /// <summary>
        /// Enum used to enumerate GamePad elements.
        /// </summary>
        public enum Element
        {
            None,
            AButton,
            BButton,
            XButton,
            YButton,
            Back,
            Start,
            LeftStick,
            RightStick,
            DPad,
            LeftBumper,
            RightBumper,
            LeftTrigger,
            RightTrigger,
        }

        static GamePadInput gamePad0 = null;
        static GamePadInput gamePad1 = null;
        static GamePadInput gamePad2 = null;
        static GamePadInput gamePad3 = null;
        static GamePadInput gamePad4 = null;

        static bool noControllers = false;
        static bool lostControllers = false;
        static PlayerIndex lastTouched = PlayerIndex.One;

        /// <summary>
        /// A dialog for when we have to warn the user. Usually null.
        /// </summary>
        static Boku.ModularMessageDialog dialog = null;

        /// <summary>
        /// A dialog for when we have to warn the user that there's a new version of Kodu. Usually null.
        /// </summary>
        static Boku.ModularMessageDialog versionDialog = null;

        /// <summary>
        /// If this is non-zero ClearAllWasPressedState will be called
        /// at the end of Update and this will be decremented.
        /// </summary>
        static int clearFrameCount = 0;

        /// <summary>
        /// Internal array of default images. Access through DefaultPictures(PlayerIndex p).
        /// </summary>
        static Texture2D[] _defaultPictures = new Texture2D[4];
        static Texture2D[] _controllerIcons = new Texture2D[4];

        /// <summary>
        /// Which kind of input is the user using.
        /// Deprecated.  Should be using KoiLibrary.LastTouchedDevice.
        /// </summary>
        public enum InputMode
        {
            None,
            KeyboardMouse,
            GamePad,
            Touch
        };
        // Deprecated.  Should be using KoiLibrary.LastTouchedDevice.
        static InputMode activeMode = InputMode.None;
        static InputMode previousMode = InputMode.None;

        bool isCurrentlyVirtualController = false;

        // These are set in MicrobitManager.GetDeviceDesc() and are meant
        // to be used to control which icons are shown to the user.
        public static bool Xbox360ControllerFound = false;
        public static bool XboxOneControllerFound = false;

        /// <summary>
        /// Contains the info for a single button.
        /// </summary>
        public class Button : ArbitraryComparable
        {
            // Internal values needed for autorepeat.
            static float autoRepeatDelay = 0.3f;    // Delay before starting autorepeat.
            static float autoRepeatRate = 15.0f;    // Num repeats per second.
            static float doubleClickDelay = 0.5f;   // Max time gap to count as a double click.

            public enum RepeatState
            {
                Inactive,
                InDelay,
                Active
            }

            RepeatState state = RepeatState.Inactive;
            bool isPressed = false;
            bool wasPressed = false;
            bool wasRepeatPressed = false;
            bool wasReleased = false;
            bool wasDoubleClicked = false;
            double lastPressedTime = 0.0;

            // Makes the button pretend that it's not pressed until the user actually 
            // releases the button.  This helps prevent UI commands from "leaking"
            // over into the game.  Note this only prevents the WasPressed and WasRepeatPressed
            // values from going true.  IsPressed will still be valid and WasReleased will
            // still occur.  CHANGE : IsPressed is now effected by this.  Does this create any bugs???
            bool ignoreUntilReleased;

            #region Accessors
            public static float AutoRepeatRate
            {
                get { return autoRepeatRate; }
            }
            public static float AutoRepeatDelay
            {
                get { return autoRepeatDelay; }
            }
            public bool IsPressed
            {
                get
                {
                    if (ignoreUntilReleased)
                    {
                        return false;
                    }
                    return isPressed;
                }
                set { isPressed = value; }
            }
            public ButtonState ButtonState
            {
                get { return isPressed ? ButtonState.Pressed : ButtonState.Released; }
            }
            public bool WasPressed
            {
                get { return wasPressed; }
                set { wasPressed = value; }
            }
            public bool WasRepeatPressed
            {
                get { return wasRepeatPressed; }
                set { wasRepeatPressed = value; }
            }
            public bool WasPressedOrRepeat
            {
                get { return WasPressed || WasRepeatPressed; }
            }
            public bool WasReleased
            {
                get { return wasReleased; }
                set { wasReleased = value; }
            }
            public bool WasDoubleClicked
            {
                get { return wasDoubleClicked; }
                set { wasDoubleClicked = value; }
            }
            public bool IgnoreUntilReleased
            {
                get { return ignoreUntilReleased; }
                set
                {
                    if (value)
                    {
                        // Maintain isPressed state across reset.  This prevents
                        // WasReleased from not registering when single frame long
                        // presses occur.
                        bool pressed = isPressed;
                        Reset();
                        isPressed = pressed;
                    }
                    ignoreUntilReleased = value;
                }
            }   // end of Button IgnoreUntilReleased()
            #endregion

            /// <summary>
            /// Reset back to default state.  Used when controller is disconnected.
            /// </summary>
            public void Reset()
            {
                isPressed = false;
                wasPressed = false;
                wasRepeatPressed = false;
                wasReleased = false;
                wasDoubleClicked = false;
            }   // end of Button Reset()

            public void ClearAllWasPressedState()
            {
                wasPressed = false;
                wasRepeatPressed = false;
                wasReleased = false;
                wasDoubleClicked = false;
            }   // end of Button ClearAllWasPressed()

            /// <summary>
            /// Version of Update used for analog inputs acting as buttons.  
            /// For analog sticks this assumes that the input value is flipped 
            /// around so that "pressed" is in the positive direction.
            /// </summary>
            /// <param name="value"></param>
            public void Update(float value)
            {
                const float threshold = 0.5f;
                ButtonState buttonState = value > threshold ? ButtonState.Pressed : ButtonState.Released;

                Update(buttonState);
            }   // end of Button Update()

            /// <summary>
            /// Updates all the internal values based on the current state.
            /// </summary>
            /// <param name="buttonState"></param>
            /// <param name="insideWindow">Mouse buttons act differently when the mouse is inside/outside of the window.</param>
            public void Update(ButtonState buttonState, bool insideWindow = true)
            {
                if (ignoreUntilReleased && buttonState == ButtonState.Pressed)
                {
                    if (!isPressed)
                    {
                        wasReleased = true;
                    }
                    return;
                }

                ignoreUntilReleased = false;

                // Clear edge cases.
                wasPressed = false;
                wasRepeatPressed = false;
                wasReleased = false;

                if (insideWindow)
                {
                    if (buttonState == ButtonState.Pressed && !isPressed)
                    {
                        isPressed = true;
                        wasPressed = true;
                    }
                    else if (buttonState == ButtonState.Released && isPressed)
                    {
                        isPressed = false;
                        wasReleased = true;
                    }

                    // Check for autorepeat and doubleclick.
                    double curTime = Time.WallClockTotalSeconds;

                    if (wasPressed)
                    {
                        // Is this press a DoubleClick?
                        if (curTime - lastPressedTime < doubleClickDelay)
                        {
                            wasDoubleClicked = true;
                        }
                        state = RepeatState.InDelay;
                        lastPressedTime = curTime;
                    }
                    else if (isPressed)
                    {
                        double dt = curTime - lastPressedTime;
                        if (state == RepeatState.InDelay && dt >= autoRepeatDelay)
                        {
                            wasRepeatPressed = true;
                            state = RepeatState.Active;
                            lastPressedTime = curTime;
                        }
                        else if (state == RepeatState.Active && dt >= 1.0 / autoRepeatRate)
                        {
                            wasRepeatPressed = true;
                            lastPressedTime = curTime;
                        }
                    }
                    else
                    {
                        state = RepeatState.Inactive;
                    }

                }   // end of if insideWindow

            }   // end of Button Update()

        }   // end of class Button

        PlayerIndex player = PlayerIndex.One;
        bool isConnected = false;
        public bool IsConnected
        {
            get { return isConnected; }
        }

        /// <summary>
        /// Has this controller ever been touched? Cleared on disconnect.
        /// </summary>
        bool everTouched = false;
        /// <summary>
        /// Was this controller touched this frame? Set each update.
        /// </summary>
        bool wasTouched = false;

        // Standard buttons.
        Button A = new Button();
        Button B = new Button();
        Button X = new Button();
        Button Y = new Button();

        Button start = new Button();
        Button back = new Button();

        Button dPadUp = new Button();
        Button dPadDown = new Button();
        Button dPadRight = new Button();
        Button dPadLeft = new Button();

        Button leftShoulder = new Button();
        Button rightShoulder = new Button();

        Button leftStickButton = new Button();
        Button rightStickButton = new Button();

        // Analog inputs treated as buttons.
        Button leftTriggerButton = new Button();
        Button rightTriggerButton = new Button();

        Button leftStickLeft = new Button();
        Button leftStickRight = new Button();
        Button leftStickUp = new Button();
        Button leftStickDown = new Button();

        Button rightStickLeft = new Button();
        Button rightStickRight = new Button();
        Button rightStickUp = new Button();
        Button rightStickDown = new Button();

        // Analog controls.
        float leftTrigger = 0.0f;
        float rightTrigger = 0.0f;



        bool leftTriggerChanged = false;
        bool rightTriggerChanged = false;

        Vector2 leftStick;
        Vector2 rightStick;

        bool leftStickChanged = false;
        bool rightStickChanged = false;

        bool leftStickIgnoreUntilZero = true;      // Force the stick to return 0,0 until it actually is 0,0
        bool rightStickIgnoreUntilZero = true;     // and then resume normal behavior.

        bool leftStickIgnoreForGamePad0 = false;    // Ignore this stick for this frame when creating GamePad0.
        bool rightStickIgnoreForGamePad0 = false;

        bool invertYAxis = false;
        bool invertXAxis = false;
        bool invertCamY = false;
        bool invertCamX = false;

        #region Accessors
        public Button ButtonA
        {
            get { return A; }
        }
        public Button ButtonB
        {
            get { return B; }
        }
        public Button ButtonX
        {
            get { return X; }
        }
        public Button ButtonY
        {
            get { return Y; }
        }
        public Button Start
        {
            get { return start; }
        }
        public Button Back
        {
            get { return back; }
        }
        public Button DPadUp
        {
            get { return dPadUp; }
        }
        public Button DPadDown
        {
            get { return dPadDown; }
        }
        public Button DPadLeft
        {
            get { return dPadLeft; }
        }
        public Button DPadRight
        {
            get { return dPadRight; }
        }
        public Button LeftShoulder
        {
            get { return leftShoulder; }
        }
        public Button RightShoulder
        {
            get { return rightShoulder; }
        }
        public Button LeftStickButton
        {
            get { return leftStickButton; }
        }
        public Button RightStickButton
        {
            get { return rightStickButton; }
        }

        //
        // Analog controls treated as buttons
        //

        public Button LeftTriggerButton
        {
            get { return leftTriggerButton; }
        }
        public Button RightTriggerButton
        {
            get { return rightTriggerButton; }
        }

        /// <summary>
        /// Is the left stick pushed to the left?
        /// </summary>
        public Button LeftStickLeft
        {
            get { return leftStickLeft; }
        }
        /// <summary>
        /// Is the left stick pushed to the right?
        /// </summary>
        public Button LeftStickRight
        {
            get { return leftStickRight; }
        }
        /// <summary>
        /// Is the left stick pushed up?
        /// </summary>
        public Button LeftStickUp
        {
            get { return leftStickUp; }
        }
        /// <summary>
        /// Is the left stick pushed down?
        /// </summary>
        public Button LeftStickDown
        {
            get { return leftStickDown; }
        }

        /// <summary>
        /// Is the right stick pushed to the left?
        /// </summary>
        public Button RightStickLeft
        {
            get { return rightStickLeft; }
        }
        /// <summary>
        /// Is the right stick pushed to the right?
        /// </summary>
        public Button RightStickRight
        {
            get { return rightStickRight; }
        }
        /// <summary>
        /// Is the right stick pushed up?
        /// </summary>
        public Button RightStickUp
        {
            get { return rightStickUp; }
        }
        /// <summary>
        /// Is the right stick pushed down?
        /// </summary>
        public Button RightStickDown
        {
            get { return rightStickDown; }
        }

        //
        // Analog controls 
        //

        public Vector2 LeftStick
        {
            get { return leftStick; }
        }
        public Vector2 RightStick
        {
            get { return rightStick; }
        }
        public bool LeftStickChanged
        {
            get { return leftStickChanged; }
        }
        public bool RightStickChanged
        {
            get { return rightStickChanged; }
        }
        public float LeftTrigger
        {
            get { return leftTrigger; }
        }
        public float RightTrigger
        {
            get { return rightTrigger; }
        }
        public bool LeftTriggerChanged
        {
            get { return leftTriggerChanged; }
        }
        public bool RightTriggerChanged
        {
            get { return rightTriggerChanged; }
        }

        /// <summary>
        /// True if there are no controllers plugged in. 
        /// </summary>
        public static bool NoControllers
        {
            get { return noControllers; }
            set { noControllers = value; }
        }

        /// <summary>
        /// True if controllers have been lost since init.
        /// </summary>
        static bool LostControllers
        {
            get { return lostControllers; }
            set { lostControllers = value; }
        }

        /// <summary>
        /// Return the last real controller/player to have used an input device. If
        /// multiple players touched last frame, it will be one of them
        /// but no guarantee which.
        /// </summary>
        public static PlayerIndex LastTouched
        {
            get { return lastTouched; }
        }

        /// <summary>
        /// Which input device has the user most recently used. Currently collapse
        /// keyboard and mouse into a single mode, because they are equivalent to the UI,
        /// but we actually know which was last used.
        /// Deprecated.  Should be using KoiLibrary.LastTouchedDevice.
        /// </summary>
        public static InputMode ActiveMode
        {
            get { return activeMode; }
        }
        public static InputMode PreviousMode
        {
            get { return previousMode; }
        }

        /// <summary>
        /// Is there an active dialog ie for a lost controller.
        /// </summary>
        public static bool DialogActive
        {
            get { return dialog != null; }
        }

        /// <summary>
        /// Has this controller ever been touched?
        /// </summary>
        public bool EverTouched
        {
            get { return everTouched; }
        }
        #endregion

        // c'tor
        // Made since we want to treat this as a singleton.
        GamePadInput(PlayerIndex player)
        {
            this.player = player;
        }   // end of GamePadInput c'tor

        /// <summary>
        /// Needs to be called once before using the GamePads.
        /// </summary>
        public static void Init()
        {
            if (gamePad0 == null)
            {
                gamePad0 = new GamePadInput(0);
            }
            if (gamePad1 == null)
            {
                gamePad1 = new GamePadInput(PlayerIndex.One);
            }
            if (gamePad2 == null)
            {
                gamePad2 = new GamePadInput(PlayerIndex.Two);
            }
            if (gamePad3 == null)
            {
                gamePad3 = new GamePadInput(PlayerIndex.Three);
            }
            if (gamePad4 == null)
            {
                gamePad4 = new GamePadInput(PlayerIndex.Four);
            }

        }   // end of GamePadInput Init()

        public Button GetButton(Buttons button)
        {
            Button result = null;

            switch (button)
            {
                case Buttons.DPadUp:
                    result = DPadUp;
                    break;
                case Buttons.DPadDown:
                    result = DPadDown;
                    break;
                case Buttons.DPadLeft:
                    result = DPadLeft;
                    break;
                case Buttons.DPadRight:
                    result = DPadRight;
                    break;
                case Buttons.Start:
                    result = Start;
                    break;
                case Buttons.Back:
                    result = Back;
                    break;
                case Buttons.LeftStick:
                    result = LeftStickButton;
                    break;
                case Buttons.RightStick:
                    result = RightStickButton;
                    break;
                case Buttons.LeftShoulder:
                    result = LeftShoulder;
                    break;
                case Buttons.RightShoulder:
                    result = RightShoulder;
                    break;
                case Buttons.BigButton:
                    result = null;
                    break;
                case Buttons.A:
                    result = A;
                    break;
                case Buttons.B:
                    result = B;
                    break;
                case Buttons.X:
                    result = X;
                    break;
                case Buttons.Y:
                    result = Y;
                    break;
                case Buttons.LeftThumbstickLeft:
                    result = LeftStickLeft;
                    break;
                case Buttons.RightTrigger:
                    result = RightTriggerButton;
                    break;
                case Buttons.LeftTrigger:
                    result = LeftTriggerButton;
                    break;
                case Buttons.RightThumbstickUp:
                    result = RightStickUp;
                    break;
                case Buttons.RightThumbstickDown:
                    result = RightStickDown;
                    break;
                case Buttons.RightThumbstickRight:
                    result = RightStickRight;
                    break;
                case Buttons.RightThumbstickLeft:
                    result = RightStickLeft;
                    break;
                case Buttons.LeftThumbstickUp:
                    result = LeftStickUp;
                    break;
                case Buttons.LeftThumbstickDown:
                    result = LeftStickDown;
                    break;
                case Buttons.LeftThumbstickRight:
                    result = LeftStickRight;
                    break;
            }

            Debug.Assert(result != null);

            return result;
        }   // end of GetButton()

        /// <summary>
        /// Given an input element, returns the related button.
        /// Note that this only works for elements that act 
        /// like buttons.
        /// Returns null if no button found.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public Button GetButton(GamePadInput.Element element)
        {
            Button result = null;

            switch (element)
            {
                case GamePadInput.Element.Start:
                    result = Start;
                    break;
                case GamePadInput.Element.Back:
                    result = Back;
                    break;
                case GamePadInput.Element.LeftStick:
                    result = LeftStickButton;
                    break;
                case GamePadInput.Element.RightStick:
                    result = RightStickButton;
                    break;
                case GamePadInput.Element.LeftBumper:
                    result = LeftShoulder;
                    break;
                case GamePadInput.Element.RightBumper:
                    result = RightShoulder;
                    break;
                case GamePadInput.Element.AButton:
                    result = A;
                    break;
                case GamePadInput.Element.BButton:
                    result = B;
                    break;
                case GamePadInput.Element.XButton:
                    result = X;
                    break;
                case GamePadInput.Element.YButton:
                    result = Y;
                    break;
                case GamePadInput.Element.RightTrigger:
                    result = RightTriggerButton;
                    break;
                case GamePadInput.Element.LeftTrigger:
                    result = LeftTriggerButton;
                    break;
            }

            return result;
        }   // end of GetButton()

        /// <summary>
        /// Convert the zero based logical player index into a real controller/player index.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static PlayerIndex LogicalToReal(PlayerIndex player)
        {
            return (PlayerIndex)LogicalToReal((int)player);
        }
        /// <summary>
        /// Convert integer logical gamepad index to integer cast of real controller/player index.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static int LogicalToReal(int i)
        {
            return (int)GetGamePad(i + 1).player;
        }
        /// <summary>
        /// Convert real controller/player index into a logical gamepad index, suitable
        /// for passing into GetGamePad(). Converts PlayerIndex.One-Four to [1..4].
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static int RealToGamePad(PlayerIndex player)
        {
            return (int)player + 1;
        }

        public static PlayerIndex RealToLogical(PlayerIndex player)
        {
            return (PlayerIndex)(RealToGamePad(player) - 1);
        }

        /// <summary>
        /// Convert the logical player index (PlayerIndex.One==0 - PlayerIndex.Four==3)
        /// into a gamepad index [1..4].
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static int LogicalToGamePad(PlayerIndex player)
        {
            return ((int)player) + 1;
        }

        /// <summary>
        /// Returns the requested GamePadInput object.
        /// </summary>
        public static GamePadInput GetGamePad0()
        {
            return gamePad0;
        }

        public static GamePadInput GetGamePad1()
        {
            return gamePad1;
        }

        public static GamePadInput GetGamePad2()
        {
            return gamePad2;
        }

        public static GamePadInput GetGamePad3()
        {
            return gamePad3;
        }

        public static GamePadInput GetGamePad4()
        {
            return gamePad4;
        }

        public static GamePadInput GetGamePad(int i)
        {
            switch (i)
            {
                case 1:
                    return gamePad1;
                case 2:
                    return gamePad2;
                case 3:
                    return gamePad3;
                case 4:
                    return gamePad4;
            }
            return gamePad0;
        }

        public static GamePadInput GetGamePad(PlayerIndex playerIdx)
        {
            return GetGamePad((int)playerIdx + 1);
        }

        /// <summary>
        /// Get the gamer tag for input logical player index,
        /// creating one if that controller isn't signed in.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static string GetGamerTag(PlayerIndex player)
        {
            int mapIdx = (int)player + 1;
            return "Player" + mapIdx.ToString();
        }


        /// <summary>
        /// Set the invert y property for the input logical player index,
        /// passing along the value for saving in the options file.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="invert"></param>
        public static void SetInvertYAxis(PlayerIndex player, bool invert)
        {
            Boku.Common.Xml.XmlOptionsData.SetInvertYAxis(GetGamerTag(player), invert);
            GetGamePad(LogicalToGamePad(player)).invertYAxis = invert;
        }
        /// <summary>
        /// Return whether this logical player index is set to invert Y
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool InvertYAxis(PlayerIndex player)
        {
            return GetGamePad(LogicalToGamePad(player)).invertYAxis;
        }
        /// <summary>
        /// Set the invert x property for the input logical player index,
        /// passing along the value for saving in the options file.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="invert"></param>
        public static void SetInvertXAxis(PlayerIndex player, bool invert)
        {
            Boku.Common.Xml.XmlOptionsData.SetInvertXAxis(GetGamerTag(player), invert);
            GetGamePad(LogicalToGamePad(player)).invertXAxis = invert;
        }
        /// <summary>
        /// Return whether this logical player index is set to invert X
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool InvertXAxis(PlayerIndex player)
        {
            return GetGamePad(LogicalToGamePad(player)).invertXAxis;
        }
        /// <summary>
        /// Set whether the input logical player wants camera Y control inverted.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="invert"></param>
        public static void SetInvertCamY(PlayerIndex player, bool invert)
        {
            Boku.Common.Xml.XmlOptionsData.SetInvertCamY(GetGamerTag(player), invert);
            GetGamePad(LogicalToGamePad(player)).invertCamY = invert;
        }
        /// <summary>
        /// Return whether the last touched controller wants camera Y inverted
        /// </summary>
        /// <returns></returns>
        public static bool InvertCamY()
        {
            return GetGamePad(RealToGamePad(LastTouched)).invertCamY;
        }
        /// <summary>
        /// Set whether the input logical player wants camera X control inverted.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="invert"></param>
        public static void SetInvertCamX(PlayerIndex player, bool invert)
        {
            Boku.Common.Xml.XmlOptionsData.SetInvertCamX(GetGamerTag(player), invert);
            GetGamePad(LogicalToGamePad(player)).invertCamX = invert;
        }
        /// <summary>
        /// Return whether the last touched controller wants camera X inverted
        /// </summary>
        /// <returns></returns>
        public static bool InvertCamX()
        {
            return GetGamePad(RealToGamePad(LastTouched)).invertCamX;
        }


        /// <summary>
        /// Given a button, will ignore all input on that button until the button 
        /// is released.  After the release everything goes back to normal.
        /// Currently only A, B, X, and Y supported.
        /// - Added a few more. maf -
        /// </summary>
        /// <param name="button"></param>
        public static void IgnoreUntilReleased(Buttons button)
        {
            switch (button)
            {
                case Buttons.A:
                    gamePad0.A.IgnoreUntilReleased = true;
                    gamePad1.A.IgnoreUntilReleased = true;
                    gamePad2.A.IgnoreUntilReleased = true;
                    gamePad3.A.IgnoreUntilReleased = true;
                    gamePad4.A.IgnoreUntilReleased = true;
                    break;
                case Buttons.B:
                    gamePad0.B.IgnoreUntilReleased = true;
                    gamePad1.B.IgnoreUntilReleased = true;
                    gamePad2.B.IgnoreUntilReleased = true;
                    gamePad3.B.IgnoreUntilReleased = true;
                    gamePad4.B.IgnoreUntilReleased = true;
                    break;
                case Buttons.X:
                    gamePad0.X.IgnoreUntilReleased = true;
                    gamePad1.X.IgnoreUntilReleased = true;
                    gamePad2.X.IgnoreUntilReleased = true;
                    gamePad3.X.IgnoreUntilReleased = true;
                    gamePad4.X.IgnoreUntilReleased = true;
                    break;
                case Buttons.Y:
                    gamePad0.Y.IgnoreUntilReleased = true;
                    gamePad1.Y.IgnoreUntilReleased = true;
                    gamePad2.Y.IgnoreUntilReleased = true;
                    gamePad3.Y.IgnoreUntilReleased = true;
                    gamePad4.Y.IgnoreUntilReleased = true;
                    break;
                case Buttons.Back:
                    gamePad0.Back.IgnoreUntilReleased = true;
                    gamePad1.Back.IgnoreUntilReleased = true;
                    gamePad2.Back.IgnoreUntilReleased = true;
                    gamePad3.Back.IgnoreUntilReleased = true;
                    gamePad4.Back.IgnoreUntilReleased = true;
                    break;
                case Buttons.Start:
                    gamePad0.Start.IgnoreUntilReleased = true;
                    gamePad1.Start.IgnoreUntilReleased = true;
                    gamePad2.Start.IgnoreUntilReleased = true;
                    gamePad3.Start.IgnoreUntilReleased = true;
                    gamePad4.Start.IgnoreUntilReleased = true;
                    break;
                case Buttons.DPadDown:
                    gamePad0.DPadDown.IgnoreUntilReleased = true;
                    gamePad1.DPadDown.IgnoreUntilReleased = true;
                    gamePad2.DPadDown.IgnoreUntilReleased = true;
                    gamePad3.DPadDown.IgnoreUntilReleased = true;
                    gamePad4.DPadDown.IgnoreUntilReleased = true;
                    break;
                case Buttons.DPadLeft:
                    gamePad0.DPadLeft.IgnoreUntilReleased = true;
                    gamePad1.DPadLeft.IgnoreUntilReleased = true;
                    gamePad2.DPadLeft.IgnoreUntilReleased = true;
                    gamePad3.DPadLeft.IgnoreUntilReleased = true;
                    gamePad4.DPadLeft.IgnoreUntilReleased = true;
                    break;
                case Buttons.DPadRight:
                    gamePad0.DPadRight.IgnoreUntilReleased = true;
                    gamePad1.DPadRight.IgnoreUntilReleased = true;
                    gamePad2.DPadRight.IgnoreUntilReleased = true;
                    gamePad3.DPadRight.IgnoreUntilReleased = true;
                    gamePad4.DPadRight.IgnoreUntilReleased = true;
                    break;
                case Buttons.DPadUp:
                    gamePad0.DPadUp.IgnoreUntilReleased = true;
                    gamePad1.DPadUp.IgnoreUntilReleased = true;
                    gamePad2.DPadUp.IgnoreUntilReleased = true;
                    gamePad3.DPadUp.IgnoreUntilReleased = true;
                    gamePad4.DPadUp.IgnoreUntilReleased = true;
                    break;
                case Buttons.RightShoulder:
                    gamePad0.RightShoulder.IgnoreUntilReleased = true;
                    gamePad1.RightShoulder.IgnoreUntilReleased = true;
                    gamePad2.RightShoulder.IgnoreUntilReleased = true;
                    gamePad3.RightShoulder.IgnoreUntilReleased = true;
                    gamePad4.RightShoulder.IgnoreUntilReleased = true;
                    break;
                case Buttons.LeftShoulder:
                    gamePad0.LeftShoulder.IgnoreUntilReleased = true;
                    gamePad1.LeftShoulder.IgnoreUntilReleased = true;
                    gamePad2.LeftShoulder.IgnoreUntilReleased = true;
                    gamePad3.LeftShoulder.IgnoreUntilReleased = true;
                    gamePad4.LeftShoulder.IgnoreUntilReleased = true;
                    break;
                default:
                    Debug.Assert(false, "Non supported button.");
                    break;
            }
        }

        /// <summary>
        /// Ignore the left stick this frame for GamePad0 composition.
        /// </summary>
        public void LeftStickIgnoreForGamePad0()
        {
            leftStickIgnoreForGamePad0 = true;
        }

        /// <summary>
        /// Ignore the right stick this frame for GamePad0 composition.
        /// </summary>
        public void RightStickIgnoreForGamePad0()
        {
            rightStickIgnoreForGamePad0 = true;
        }

        /// <summary>
        /// Return the default image for this player.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        static Texture2D DefaultPictures(PlayerIndex player)
        {
            if (_defaultPictures[(int)player] == null)
            {
                LoadContent(true);
            }
            return _defaultPictures[(int)player];
        }

        /// <summary>
        /// Return the real controller icon for the LOGICAL player index.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static Texture2D GetControllerIcon(PlayerIndex player)
        {
            if (_controllerIcons[(int)player] == null)
            {
                LoadContent(true);
            }
            return _controllerIcons[LogicalToReal((int)player)];
        }
        /// <summary>
        /// Load content, just to get the default gamer pictures in.
        /// </summary>
        /// <param name="immediate"></param>
        public static void LoadContent(bool immediate)
        {
            _defaultPictures[0] = KoiLibrary.LoadTexture2D(@"Textures\Player1");
            _defaultPictures[1] = KoiLibrary.LoadTexture2D(@"Textures\Player2");
            _defaultPictures[2] = KoiLibrary.LoadTexture2D(@"Textures\Player3");
            _defaultPictures[3] = KoiLibrary.LoadTexture2D(@"Textures\Player4");

            _controllerIcons[0] = KoiLibrary.LoadTexture2D(@"Textures\Tiles\Controller1");
            _controllerIcons[1] = KoiLibrary.LoadTexture2D(@"Textures\Tiles\Controller2");
            _controllerIcons[2] = KoiLibrary.LoadTexture2D(@"Textures\Tiles\Controller3");
            _controllerIcons[3] = KoiLibrary.LoadTexture2D(@"Textures\Tiles\Controller4");
        }

        /// <summary>
        /// Nothing to do.
        /// </summary>
        /// <param name="graphics"></param>
        public static void InitDeviceResources(GraphicsDevice device)
        {
        }

        /// <summary>
        /// Discard default player images.
        /// </summary>
        public static void UnloadContent()
        {
            for (int i = 0; i < _defaultPictures.Length; ++i)
            {
                _defaultPictures[i] = null;
            }
        }

        /// <summary>
        /// Recreate render targets.
        /// </summary>
        public static void DeviceReset(GraphicsDevice device)
        {
        }

        void CheckStick(ref Vector2 curValue, Vector2 oldValue, ref bool changed)
        {
            changed = curValue != oldValue;
            curValue = oldValue;
        }   // end of GamePadInput CheckStick()

        void CheckTrigger(ref float curValue, float oldValue, ref bool changed)
        {
            changed = curValue != oldValue;
            curValue = oldValue;
        }   // end of GamePadInput CheckTrigger()

        /// <summary>
        /// Should be called once per frame. Should be after Keyboard and MouseInput
        /// updates, because GamePadInput keeps track of who was the last touched.
        /// </summary>
        /// <returns>True is any gamepade was touched this frame.</returns>
        public static bool Update()
        {
            // If any buttons on Pad0 have been set to IgnoreUntilReleased we
            // need to copy this info to each of the gamePads.
            CopyIgnoredState();

            NoControllers = true;   // Will be set to false in UpdatePad if any are connected.

            //Update Gamepad 1 with the virtual controller if in touch mode.
            if (InputMode.Touch == activeMode)
            {
                if (gamePad1.IsControllerStateNeutral())
                {
                    gamePad1.UpdatePadFromVirtualController();
                }
                else
                {
                    gamePad1.UpdatePad();
                }
            }
            else
            {
                gamePad1.UpdatePad();
            }

            gamePad2.UpdatePad();
            gamePad3.UpdatePad();
            gamePad4.UpdatePad();

            // Pad0 is a special case.
            gamePad0.ComposeUpdate();


            if (clearFrameCount > 0)
            {
                ClearAllWasPressedState();
                --clearFrameCount;
            }

            //CheckControllers();

            CheckKeyboardMouseActive();

            return gamePad0.wasTouched;

        }   // end of GamePadInput Update()

        /// <summary>
        /// Copies any ignored state from Pad0 to the other pads then clears Pad0.
        /// </summary>
        static void CopyIgnoredState()
        {
            if (gamePad0.ButtonA.IgnoreUntilReleased)
            {
                gamePad1.ButtonA.IgnoreUntilReleased = true;
                gamePad2.ButtonA.IgnoreUntilReleased = true;
                gamePad3.ButtonA.IgnoreUntilReleased = true;
                gamePad4.ButtonA.IgnoreUntilReleased = true;
                gamePad0.ButtonA.IgnoreUntilReleased = false;
            }

            if (gamePad0.ButtonB.IgnoreUntilReleased)
            {
                gamePad1.ButtonB.IgnoreUntilReleased = true;
                gamePad2.ButtonB.IgnoreUntilReleased = true;
                gamePad3.ButtonB.IgnoreUntilReleased = true;
                gamePad4.ButtonB.IgnoreUntilReleased = true;
                gamePad0.ButtonB.IgnoreUntilReleased = false;
            }

            if (gamePad0.ButtonX.IgnoreUntilReleased)
            {
                gamePad1.ButtonX.IgnoreUntilReleased = true;
                gamePad2.ButtonX.IgnoreUntilReleased = true;
                gamePad3.ButtonX.IgnoreUntilReleased = true;
                gamePad4.ButtonX.IgnoreUntilReleased = true;
                gamePad0.ButtonX.IgnoreUntilReleased = false;
            }

            if (gamePad0.ButtonY.IgnoreUntilReleased)
            {
                gamePad1.ButtonY.IgnoreUntilReleased = true;
                gamePad2.ButtonY.IgnoreUntilReleased = true;
                gamePad3.ButtonY.IgnoreUntilReleased = true;
                gamePad4.ButtonY.IgnoreUntilReleased = true;
                gamePad0.ButtonY.IgnoreUntilReleased = false;
            }

            if (gamePad0.Back.IgnoreUntilReleased)
            {
                gamePad1.Back.IgnoreUntilReleased = true;
                gamePad2.Back.IgnoreUntilReleased = true;
                gamePad3.Back.IgnoreUntilReleased = true;
                gamePad4.Back.IgnoreUntilReleased = true;
                gamePad0.Back.IgnoreUntilReleased = false;
            }

            if (gamePad0.Start.IgnoreUntilReleased)
            {
                gamePad1.Start.IgnoreUntilReleased = true;
                gamePad2.Start.IgnoreUntilReleased = true;
                gamePad3.Start.IgnoreUntilReleased = true;
                gamePad4.Start.IgnoreUntilReleased = true;
                gamePad0.Start.IgnoreUntilReleased = false;
            }

            if (gamePad0.DPadUp.IgnoreUntilReleased)
            {
                gamePad1.DPadUp.IgnoreUntilReleased = true;
                gamePad2.DPadUp.IgnoreUntilReleased = true;
                gamePad3.DPadUp.IgnoreUntilReleased = true;
                gamePad4.DPadUp.IgnoreUntilReleased = true;
                gamePad0.DPadUp.IgnoreUntilReleased = false;
            }

            if (gamePad0.DPadDown.IgnoreUntilReleased)
            {
                gamePad1.DPadDown.IgnoreUntilReleased = true;
                gamePad2.DPadDown.IgnoreUntilReleased = true;
                gamePad3.DPadDown.IgnoreUntilReleased = true;
                gamePad4.DPadDown.IgnoreUntilReleased = true;
                gamePad0.DPadDown.IgnoreUntilReleased = false;
            }

            if (gamePad0.DPadLeft.IgnoreUntilReleased)
            {
                gamePad1.DPadLeft.IgnoreUntilReleased = true;
                gamePad2.DPadLeft.IgnoreUntilReleased = true;
                gamePad3.DPadLeft.IgnoreUntilReleased = true;
                gamePad4.DPadLeft.IgnoreUntilReleased = true;
                gamePad0.DPadLeft.IgnoreUntilReleased = false;
            }

            if (gamePad0.DPadRight.IgnoreUntilReleased)
            {
                gamePad1.DPadRight.IgnoreUntilReleased = true;
                gamePad2.DPadRight.IgnoreUntilReleased = true;
                gamePad3.DPadRight.IgnoreUntilReleased = true;
                gamePad4.DPadRight.IgnoreUntilReleased = true;
                gamePad0.DPadRight.IgnoreUntilReleased = false;
            }

            if (gamePad0.LeftShoulder.IgnoreUntilReleased)
            {
                gamePad1.LeftShoulder.IgnoreUntilReleased = true;
                gamePad2.LeftShoulder.IgnoreUntilReleased = true;
                gamePad3.LeftShoulder.IgnoreUntilReleased = true;
                gamePad4.LeftShoulder.IgnoreUntilReleased = true;
                gamePad0.LeftShoulder.IgnoreUntilReleased = false;
            }

            if (gamePad0.RightShoulder.IgnoreUntilReleased)
            {
                gamePad1.RightShoulder.IgnoreUntilReleased = true;
                gamePad2.RightShoulder.IgnoreUntilReleased = true;
                gamePad3.RightShoulder.IgnoreUntilReleased = true;
                gamePad4.RightShoulder.IgnoreUntilReleased = true;
                gamePad0.RightShoulder.IgnoreUntilReleased = false;
            }

            if (gamePad0.LeftStickButton.IgnoreUntilReleased)
            {
                gamePad1.LeftStickButton.IgnoreUntilReleased = true;
                gamePad2.LeftStickButton.IgnoreUntilReleased = true;
                gamePad3.LeftStickButton.IgnoreUntilReleased = true;
                gamePad4.LeftStickButton.IgnoreUntilReleased = true;
                gamePad0.LeftStickButton.IgnoreUntilReleased = false;
            }

            if (gamePad0.RightStickButton.IgnoreUntilReleased)
            {
                gamePad1.RightStickButton.IgnoreUntilReleased = true;
                gamePad2.RightStickButton.IgnoreUntilReleased = true;
                gamePad3.RightStickButton.IgnoreUntilReleased = true;
                gamePad4.RightStickButton.IgnoreUntilReleased = true;
                gamePad0.RightStickButton.IgnoreUntilReleased = false;
            }

        }   // end of CopyIgnoredState()

        /// <summary>
        /// This unfortunate addition is a render call on the off chance that we
        /// have some dire warning to display. Most of the time a no-op.
        /// </summary>
        public static void Render()
        {
            if (dialog != null)
            {
                dialog.Render();
            }
            if (versionDialog != null)
            {
                versionDialog.Render();
            }
        }

        bool IsControllerStateNeutral()
        {
            bool neutral = true;

            GamePadState state = GamePad.GetState(player);
            if (state.IsConnected)
            {
                neutral = neutral && IsReleased(state.Buttons.A);
                neutral = neutral && IsReleased(state.Buttons.B);
                neutral = neutral && IsReleased(state.Buttons.X);
                neutral = neutral && IsReleased(state.Buttons.Y);

                neutral = neutral && IsReleased(state.Buttons.Start);
                neutral = neutral && IsReleased(state.Buttons.Back);
                neutral = neutral && IsReleased(state.Buttons.BigButton);

                neutral = neutral && IsReleased(state.Buttons.LeftShoulder);
                neutral = neutral && IsReleased(state.Buttons.RightShoulder);

                neutral = neutral && IsReleased(state.Buttons.LeftStick);
                neutral = neutral && IsReleased(state.Buttons.RightStick);

                neutral = neutral && IsReleased(state.DPad.Down);
                neutral = neutral && IsReleased(state.DPad.Up);
                neutral = neutral && IsReleased(state.DPad.Left);
                neutral = neutral && IsReleased(state.DPad.Right);

                neutral = neutral && state.Triggers.Left == 0.0f;
                neutral = neutral && state.Triggers.Right == 0.0f;

                neutral = neutral && Vector2.Zero == state.ThumbSticks.Left;
                neutral = neutral && Vector2.Zero == state.ThumbSticks.Right;
            }

            return neutral;
        }

        bool IsReleased(ButtonState bs)
        {
            return ButtonState.Released == bs;
        }

        void UpdatePadFromVirtualController()
        {
            isCurrentlyVirtualController = isConnected = Boku.InGame.ShowVirtualController;

            // Only check buttons if connected.  Else leave in default state.
            if (isConnected)
            {
                NoControllers = false;

                // Regular buttons
                A.Update(TouchVirtualController.GetButtonState(TouchVirtualController.TouchButtonType.Button_A));
                B.Update(TouchVirtualController.GetButtonState(TouchVirtualController.TouchButtonType.Button_B));
                X.Update(TouchVirtualController.GetButtonState(TouchVirtualController.TouchButtonType.Button_X));
                Y.Update(TouchVirtualController.GetButtonState(TouchVirtualController.TouchButtonType.Button_Y));

                start.Update(ButtonState.Released);
                back.Update(ButtonState.Released);

                dPadUp.Update(ButtonState.Released);
                dPadDown.Update(ButtonState.Released);
                dPadLeft.Update(ButtonState.Released);
                dPadRight.Update(ButtonState.Released);

                leftShoulder.Update(ButtonState.Released);
                rightShoulder.Update(ButtonState.Released);

                leftStickButton.Update(ButtonState.Released);
                rightStickButton.Update(ButtonState.Released);

                // Analog inputs treated as buttons.

                leftTriggerButton.Update(ButtonState.Released);
                rightTriggerButton.Update(ButtonState.Released);

                // Use intermediate values in case we're ignoring input.
                Vector2 leftStickValue = TouchVirtualController.GetLeftStickValue();
                Vector2 rightStickValue = Vector2.Zero;
                if (leftStickIgnoreUntilZero)
                {
                    if (leftStickValue == Vector2.Zero)
                    {
                        leftStickIgnoreUntilZero = false;
                    }
                    else
                    {
                        leftStickValue = Vector2.Zero;
                    }
                }
                if (rightStickIgnoreUntilZero)
                {
                    if (rightStickValue == Vector2.Zero)
                    {
                        rightStickIgnoreUntilZero = false;
                    }
                    else
                    {
                        rightStickValue = Vector2.Zero;
                    }
                }

                leftStickLeft.Update(-leftStickValue.X);
                leftStickRight.Update(leftStickValue.X);
                leftStickUp.Update(leftStickValue.Y);
                leftStickDown.Update(-leftStickValue.Y);

                RightStickLeft.Update(-rightStickValue.X);
                RightStickRight.Update(rightStickValue.X);
                RightStickUp.Update(rightStickValue.Y);
                RightStickDown.Update(-rightStickValue.Y);

                // Analog inputs.

                CheckTrigger(ref leftTrigger, 0.0f, ref leftTriggerChanged);
                CheckTrigger(ref rightTrigger, 0.0f, ref rightTriggerChanged);

                CheckStick(ref leftStick, leftStickValue, ref leftStickChanged);
                CheckStick(ref rightStick, rightStickValue, ref rightStickChanged);


                //Do not set wasTouched.  This will force change the mode to gamepad.  Instead we set the isConnected boolean up above tue ensure input is processed properly.

            }   // end if controller is connected.
            else
            {
                // Not connected so clear to neutral.  Users should be checking for 
                // connected but this makes sure nothing bad happens if they don't.
                ResetToZero();
            }
        }


        void UpdatePad()
        {
            bool wasVirtual = isCurrentlyVirtualController;
            isCurrentlyVirtualController = false;

            GamePadState state = GamePad.GetState(player);
            bool isFound = !isConnected && state.IsConnected;
            if (isFound)
            {
                /// No action necessary.
            }

            bool isLost = isConnected && !state.IsConnected && !wasVirtual;
            if (isLost)
            {
                // Trigger the warning message.
                everTouched = false;
                DialogManagerX.ShowDialog(DialogCenter.GamepadDisconnetDialog);
            }

            isConnected = state.IsConnected;

            // Only check buttons if connected.  Else leave in default state.
            if (isConnected)
            {
                NoControllers = false;

                // Regular buttons.

                A.Update(state.Buttons.A);
                B.Update(state.Buttons.B);
                X.Update(state.Buttons.X);
                Y.Update(state.Buttons.Y);

                start.Update(state.Buttons.Start);
                back.Update(state.Buttons.Back);

                dPadUp.Update(state.DPad.Up);
                dPadDown.Update(state.DPad.Down);
                dPadLeft.Update(state.DPad.Left);
                dPadRight.Update(state.DPad.Right);

                leftShoulder.Update(state.Buttons.LeftShoulder);
                rightShoulder.Update(state.Buttons.RightShoulder);

                leftStickButton.Update(state.Buttons.LeftStick);
                rightStickButton.Update(state.Buttons.RightStick);

                // Analog inputs treated as buttons.

                leftTriggerButton.Update(state.Triggers.Left);
                rightTriggerButton.Update(state.Triggers.Right);

                // Use intermediate values in case we're ignoring input.
                Vector2 leftStickValue = state.ThumbSticks.Left;
                Vector2 rightStickValue = state.ThumbSticks.Right;
                if (leftStickIgnoreUntilZero)
                {
                    if (leftStickValue == Vector2.Zero)
                    {
                        leftStickIgnoreUntilZero = false;
                    }
                    else
                    {
                        leftStickValue = Vector2.Zero;
                    }
                }
                if (rightStickIgnoreUntilZero)
                {
                    if (rightStickValue == Vector2.Zero)
                    {
                        rightStickIgnoreUntilZero = false;
                    }
                    else
                    {
                        rightStickValue = Vector2.Zero;
                    }
                }

                leftStickLeft.Update(-leftStickValue.X);
                leftStickRight.Update(leftStickValue.X);
                leftStickUp.Update(leftStickValue.Y);
                leftStickDown.Update(-leftStickValue.Y);

                RightStickLeft.Update(-rightStickValue.X);
                RightStickRight.Update(rightStickValue.X);
                RightStickUp.Update(rightStickValue.Y);
                RightStickDown.Update(-rightStickValue.Y);

                // Analog inputs.

                CheckTrigger(ref leftTrigger, state.Triggers.Left, ref leftTriggerChanged);
                CheckTrigger(ref rightTrigger, state.Triggers.Right, ref rightTriggerChanged);

                CheckStick(ref leftStick, leftStickValue, ref leftStickChanged);
                CheckStick(ref rightStick, rightStickValue, ref rightStickChanged);

                wasTouched = Touched();
                if (wasTouched && !everTouched)
                {
                    //Bind(player);
                }
                if (wasTouched)
                {
                    lastTouched = player;
                }

            }   // end if controller is connected.
            else
            {
                // Not connected so clear to neutral.  Users should be checking for 
                // connected but this makes sure nothing bad happens if they don't.
                ResetToZero();
            }

        }   // end of GamePadInput Update()

        /// <summary>
        /// On first touch of a controller, check if the logical player has a default preference
        /// for InvertY, and let XmlOptionsData override if it's been set explicitly.
        /// </summary>
        /// <param name="player"></param>
        static void CheckGameDefaults(PlayerIndex player)
        {
            GamePadInput gamePad = GetGamePad(LogicalToGamePad(player));
            gamePad.invertYAxis = gamePad.invertXAxis = false;

            string gamerTag = GetGamerTag(player);
            gamePad.invertYAxis = Boku.Common.Xml.XmlOptionsData.GetInvertYAxis(gamerTag, gamePad.invertYAxis);
            gamePad.invertXAxis = Boku.Common.Xml.XmlOptionsData.GetInvertXAxis(gamerTag, gamePad.invertXAxis);
            /// Then use this in GamePadStickFilter.MatchAction.
        }

        /// <summary>
        /// Create a virtual composite pad that blends the inputs from the 4 real pads.
        /// </summary>
        void ComposeUpdate()
        {
            gamePad0.isConnected = gamePad1.isConnected
                || gamePad2.isConnected
                || gamePad3.isConnected
                || gamePad4.isConnected;
            gamePad0.wasTouched = gamePad1.wasTouched
                || gamePad2.wasTouched
                || gamePad3.wasTouched
                || gamePad4.wasTouched;
            gamePad0.ResetAllStateForSinglePad();

            // Start by finding first active pad and just copying it's state wholesale.  Then blend in any other pads.
            BlendPadValues(gamePad1, gamePad0);
            BlendPadValues(gamePad2, gamePad0);
            BlendPadValues(gamePad3, gamePad0);
            BlendPadValues(gamePad4, gamePad0);

            if (leftStickIgnoreUntilZero)
            {
                if (leftStick == Vector2.Zero)
                {
                    leftStickIgnoreUntilZero = false;
                }
                else
                {
                    leftStick = Vector2.Zero;
                }
            }
            if (rightStickIgnoreUntilZero)
            {
                if (rightStick == Vector2.Zero)
                {
                    rightStickIgnoreUntilZero = false;
                }
                else
                {
                    rightStick = Vector2.Zero;
                }
            }

        }   // end of ComposeUpdate() 

        /// <summary>
        /// Blends the values from one pad into another to create a virtual composite pad.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        static void BlendPadValues(GamePadInput src, GamePadInput dst)
        {
            bool srcIsGamePad = GamePad.GetCapabilities(src.player).GamePadType == GamePadType.GamePad;

            if ((srcIsGamePad || src.isCurrentlyVirtualController) && src.isConnected)
            {
                // buttons
                BlendButton(src.ButtonA, dst.ButtonA);
                BlendButton(src.ButtonB, dst.ButtonB);
                BlendButton(src.ButtonX, dst.ButtonX);
                BlendButton(src.ButtonY, dst.ButtonY);

                BlendButton(src.Back, dst.Back);
                BlendButton(src.Start, dst.Start);

                BlendButton(src.DPadUp, dst.DPadUp);
                BlendButton(src.DPadDown, dst.DPadDown);
                BlendButton(src.DPadLeft, dst.DPadLeft);
                BlendButton(src.DPadRight, dst.DPadRight);

                BlendButton(src.LeftShoulder, dst.LeftShoulder);
                BlendButton(src.RightShoulder, dst.RightShoulder);

                BlendButton(src.LeftTriggerButton, dst.LeftTriggerButton);
                BlendButton(src.RightTriggerButton, dst.RightTriggerButton);

                BlendButton(src.LeftStickButton, dst.LeftStickButton);
                BlendButton(src.RightStickButton, dst.RightStickButton);

                BlendButton(src.LeftStickUp, dst.LeftStickUp);
                BlendButton(src.LeftStickDown, dst.LeftStickDown);
                BlendButton(src.LeftStickRight, dst.LeftStickRight);
                BlendButton(src.LeftStickLeft, dst.LeftStickLeft);

                BlendButton(src.RightStickUp, dst.RightStickUp);
                BlendButton(src.RightStickDown, dst.RightStickDown);
                BlendButton(src.RightStickRight, dst.RightStickRight);
                BlendButton(src.RightStickLeft, dst.RightStickLeft);

                // triggers
                dst.leftTrigger = BlendTrigger(src.LeftTrigger, dst.LeftTrigger);
                dst.rightTrigger = BlendTrigger(src.RightTrigger, dst.RightTrigger);

                // analog sticks
                if (src.leftStickIgnoreForGamePad0)
                    src.leftStickIgnoreForGamePad0 = false;
                else
                    dst.leftStick = BlendStick(src.LeftStick, dst.LeftStick);

                if (src.rightStickIgnoreForGamePad0)
                    src.rightStickIgnoreForGamePad0 = false;
                else
                    dst.rightStick = BlendStick(src.RightStick, dst.RightStick);
            }

        }   // end of BlendPadValues()

        static void BlendButton(Button src, Button dst)
        {
            dst.IsPressed |= src.IsPressed;
            dst.WasPressed |= src.WasPressed;
            dst.WasRepeatPressed |= src.WasRepeatPressed;
            dst.WasReleased |= src.WasReleased;
        }   // end of BlendButton()

        /// <summary>
        /// Use max value as composite.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        static float BlendTrigger(float src, float dst)
        {
            return Math.Max(src, dst);
        }

        /// <summary>
        /// Use max absolute value as composite.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        static Vector2 BlendStick(Vector2 src, Vector2 dst)
        {
            if (Math.Abs(src.X) > Math.Abs(dst.X))
            {
                dst.X = src.X;
            }
            if (Math.Abs(src.Y) > Math.Abs(dst.Y))
            {
                dst.Y = src.Y;
            }

            return dst;
        }

        /// <summary>
        /// Resets the current state of the pad as if it wasn't connected.
        /// </summary>
        void ResetToZero()
        {
            A.Reset();
            B.Reset();
            X.Reset();
            Y.Reset();
            start.Reset();
            back.Reset();
            leftShoulder.Reset();
            rightShoulder.Reset();
            leftStickButton.Reset();
            rightStickButton.Reset();

            leftTriggerButton.Reset();
            rightTriggerButton.Reset();

            leftStickLeft.Reset();
            leftStickRight.Reset();
            leftStickUp.Reset();
            leftStickDown.Reset();

            rightStickLeft.Reset();
            rightStickRight.Reset();
            rightStickUp.Reset();
            rightStickDown.Reset();

            leftTrigger = rightTrigger = 0.0f;
            leftTriggerChanged = rightTriggerChanged = false;
            leftStick = rightStick = new Vector2(0.0f, 0.0f);
            leftStickChanged = rightStickChanged = false;
        }   // end of GamePadInput ResetToZero()

        /// <summary>
        /// Force the stick to return 0,0 until it actually is 0,0 at which 
        /// point return to normal functionality.  This is primarily useful 
        /// in pie menus so that after a selection the cursor doesn't run 
        /// off in whatever direction the user is pressing.
        /// </summary>
        public void IgnoreLeftStickUntilZero()
        {
            leftStickIgnoreUntilZero = true;
        }   // end of IgnoreLeftStickUntilZero()

        /// <summary>
        /// Force the stick to return 0,0 until it actually is 0,0 at which 
        /// point return to normal functionality.  This is primarily useful 
        /// in pie menus so that after a selection the cursor doesn't run 
        /// off in whatever direction the user is pressing.
        /// </summary>
        public void IgnoreRightStickUntilZero()
        {
            rightStickIgnoreUntilZero = true;
        }   // end of IgnoreRightStickUntilZero()

        /// <summary>
        /// Clears all the WasPressed and WasRepeatPressed values to false.  Useful
        /// to ensure no false triggers when transitioning between modes.
        /// </summary>
        public static void ClearAllWasPressedState()
        {
            gamePad0.ClearAllWasPressedStateForSinglePad();
            gamePad1.ClearAllWasPressedStateForSinglePad();
            gamePad2.ClearAllWasPressedStateForSinglePad();
            gamePad3.ClearAllWasPressedStateForSinglePad();
            gamePad4.ClearAllWasPressedStateForSinglePad();
        }   // end of GamePadInput ClearAllWasPressedState()


        /// <summary>
        /// Ignores all gamepad input's pressed state until released, for all gamepads.
        /// </summary>
        public static void IgnoreAllUntilReleased()
        {
            gamePad0.IgnoreUntilReleasedForSinglePad();
            gamePad1.IgnoreUntilReleasedForSinglePad();
            gamePad2.IgnoreUntilReleasedForSinglePad();
            gamePad3.IgnoreUntilReleasedForSinglePad();
            gamePad4.IgnoreUntilReleasedForSinglePad();
        }   // end of GamePadInput IgnoreUntilReleasedForSinglePad()


        /// <summary>
        /// Clears all the WasPressed and WasRepeatPressed values to false.  Useful
        /// to ensure no false triggers when transitioning between modes.
        /// This flaover, with the 'frames' arg will do this clear immediately
        /// and also for the next frameCount updates.  This is a bit of a hacked
        /// attempt to reduce the button mashing problems in the programming UI
        /// by allowing everything to settle out for a few frames before looking
        /// for the next input.
        /// </summary>
        /// <param name="frameCount"></param>
        public static void ClearAllWasPressedState(int frameCount)
        {
            ClearAllWasPressedState();

            clearFrameCount = frameCount;
        }

        void ResetAllStateForSinglePad()
        {
            A.Reset();
            B.Reset();
            X.Reset();
            Y.Reset();

            LeftShoulder.Reset();
            RightShoulder.Reset();

            LeftTriggerButton.Reset();
            RightTriggerButton.Reset();

            Start.Reset();
            Back.Reset();

            DPadLeft.Reset();
            DPadRight.Reset();
            DPadUp.Reset();
            DPadDown.Reset();

            LeftStickLeft.Reset();
            LeftStickRight.Reset();
            LeftStickUp.Reset();
            LeftStickDown.Reset();

            RightStickLeft.Reset();
            RightStickRight.Reset();
            RightStickUp.Reset();
            RightStickDown.Reset();

            ResetToZero();
        }
        void ClearAllWasPressedStateForSinglePad()
        {
            if (isConnected)
            {
                A.ClearAllWasPressedState();
                B.ClearAllWasPressedState();
                X.ClearAllWasPressedState();
                Y.ClearAllWasPressedState();

                LeftShoulder.ClearAllWasPressedState();
                RightShoulder.ClearAllWasPressedState();

                LeftTriggerButton.ClearAllWasPressedState();
                RightTriggerButton.ClearAllWasPressedState();

                Start.ClearAllWasPressedState();
                Back.ClearAllWasPressedState();

                DPadLeft.ClearAllWasPressedState();
                DPadRight.ClearAllWasPressedState();
                DPadUp.ClearAllWasPressedState();
                DPadDown.ClearAllWasPressedState();

                LeftStickLeft.ClearAllWasPressedState();
                LeftStickRight.ClearAllWasPressedState();
                LeftStickUp.ClearAllWasPressedState();
                LeftStickDown.ClearAllWasPressedState();

                RightStickLeft.ClearAllWasPressedState();
                RightStickRight.ClearAllWasPressedState();
                RightStickUp.ClearAllWasPressedState();
                RightStickDown.ClearAllWasPressedState();
            }
        }   // end of GamePadInput ClearAllWasPressedState()

        void IgnoreUntilReleasedForSinglePad()
        {
            if (isConnected)
            {
                A.IgnoreUntilReleased = true;
                B.IgnoreUntilReleased = true;
                X.IgnoreUntilReleased = true;
                Y.IgnoreUntilReleased = true;

                LeftShoulder.IgnoreUntilReleased = true;
                RightShoulder.IgnoreUntilReleased = true;

                LeftTriggerButton.IgnoreUntilReleased = true;
                RightTriggerButton.IgnoreUntilReleased = true;

                Start.IgnoreUntilReleased = true;
                Back.IgnoreUntilReleased = true;

                DPadLeft.IgnoreUntilReleased = true;
                DPadRight.IgnoreUntilReleased = true;
                DPadUp.IgnoreUntilReleased = true;
                DPadDown.IgnoreUntilReleased = true;

                LeftStickLeft.IgnoreUntilReleased = true;
                LeftStickRight.IgnoreUntilReleased = true;
                LeftStickUp.IgnoreUntilReleased = true;
                LeftStickDown.IgnoreUntilReleased = true;

                RightStickLeft.IgnoreUntilReleased = true;
                RightStickRight.IgnoreUntilReleased = true;
                RightStickUp.IgnoreUntilReleased = true;
                RightStickDown.IgnoreUntilReleased = true;
            }
        }   // end of GamePadInput ClearAllWasPressedState()


        static object timerInstrumentGamepad = null;
        static object timerInstrumentKeyboard = null;
        static object activeTimer = new object();

        /// <summary>
        /// See whether the user is actively using the keyboard/mouse or the game pad
        /// (or neither). Note this is sticky, so touching the gamepad will leave it in
        /// gamepad mode until the keyboard is touched or mouse moved and vv.
        /// </summary>
        static void CheckKeyboardMouseActive()
        {
            if (gamePad0.wasTouched)
            {
                if (activeTimer != timerInstrumentGamepad)
                {
                    stopActiveInputTimer();
                    timerInstrumentGamepad = Instrumentation.StartTimer(Instrumentation.TimerId.GamePadInputTime);
                    activeTimer = timerInstrumentGamepad;
                }
                previousMode = activeMode;
                activeMode = InputMode.GamePad;
                BokuGame.bokuGame.IsMouseVisible = false;
                Time.startActiveInstrumentationClock();
            }
            //if we recieve keyboard input and we're in touch mode, stay in touch mode
            else if (TouchInput.IsTouched || (KeyboardInputX.WasTouched && activeMode == InputMode.Touch))
            {
                if (activeTimer != timerInstrumentKeyboard)
                {
                    stopActiveInputTimer();
                    timerInstrumentKeyboard = Instrumentation.StartTimer(Instrumentation.TimerId.KeyboardMouseInputTime);
                    activeTimer = timerInstrumentKeyboard;
                }
                previousMode = activeMode;
                activeMode = InputMode.Touch;
                BokuGame.bokuGame.IsMouseVisible = false;
                Time.startActiveInstrumentationClock();
            }
            else if (KeyboardInputX.WasTouched || MouseInput.WasTouched)
            {
                if (activeTimer != timerInstrumentKeyboard)
                {
                    stopActiveInputTimer();
                    timerInstrumentKeyboard = Instrumentation.StartTimer(Instrumentation.TimerId.KeyboardMouseInputTime);
                    activeTimer = timerInstrumentKeyboard;
                }
                previousMode = activeMode;
                activeMode = InputMode.KeyboardMouse;
                BokuGame.bokuGame.IsMouseVisible = true;
                Time.startActiveInstrumentationClock();
            }
            else
            {
                //check for inactive time
                Time.startInactiveCheck();
            }

            // JW - Some places in the code check for mouse input without making sure they are in the mouseinput
            // mode. So, we want to make sure that the mouse isn't in any kind of active state when we change
            // modes.
            if (activeMode != InputMode.KeyboardMouse)
            {
                LowLevelMouseInput.Left.Reset();
                LowLevelMouseInput.Middle.Reset();
                LowLevelMouseInput.Right.Reset();
            }
        }



        public static void stopActiveInputTimer()
        {
            //check if a timer is active, and then stop it
            if (activeTimer == timerInstrumentGamepad ||
                activeTimer == timerInstrumentKeyboard)
            {
                Instrumentation.StopTimer(activeTimer);
            }
        }

        bool Touched()
        {
            if (!isConnected)
                return false;

            GamePadCapabilities caps = GamePad.GetCapabilities(player);
            bool guitar = caps.GamePadType == GamePadType.AlternateGuitar
                        || caps.GamePadType == GamePadType.Guitar;

            return A.WasPressed
                || B.WasPressed
                || X.WasPressed
                || Y.WasPressed
                || start.WasPressed
                || back.WasPressed
                || dPadUp.WasPressed
                || dPadDown.WasPressed
                || dPadRight.WasPressed
                || dPadLeft.WasPressed
                || leftShoulder.WasPressed
                || rightShoulder.WasPressed
                || (!guitar && leftTriggerButton.WasPressed)
                || rightTriggerButton.WasPressed
                || leftStickChanged
                || rightStickChanged;
        }

        /*
        /// <summary>
        /// Check status of controllers and warn as appropriate.
        /// </summary>
        static void CheckControllers()
        {
            if (LostControllers)
            {
                DialogManagerX.ShowDialog(DialogCenter.GamepadDisconnetDialog);
            }
            if (dialog != null)
            {
                dialog.Update();

                if (KeyboardInputX.WasPressed(Keys.Escape))
                {
#if NETFX_CORE
                    Windows.UI.Xaml.Application.Current.Exit();
#else
                    BokuGame.bokuGame.Exit();
#endif
                }
                if (KeyboardInputX.WasPressed(Keys.Enter))
                {
                    KeyboardInputX.ClearAllWasPressedState(Keys.Enter);
                    dialog.Deactivate();
                    NoControllers = false;
                }

                ClearAllWasPressedState();
                if (!dialog.Active)
                {
                    dialog = null;
                }
            }
            if (versionDialog != null)
            {
                versionDialog.Update();
            }
        }
        */

        /*
        /// <summary>
        /// Create and activate a dialog warning that there is no controller plugged in.
        /// Note that they have to plug in a controller to get rid of the dialog.
        /// </summary>
        static void CreateNoControllerDialog()
        {
        }

        /// <summary>
        /// Create and activate a dialog wanring that a controller has been unplugged.
        /// </summary>
        static void CreateLostControllerDialog()
        {
            // Clear this here instead of in the handler.  This way on the PC the
            // user can press the A key and continue on in key/mouse mode without
            // having to restore the controller.
            LostControllers = false;

            if (dialog == null)
            {
                ModularMessageDialog.ButtonHandler handlerA = delegate(ModularMessageDialog diag)
                {
                    diag.Deactivate();
                    Time.Paused = _wasPaused;
                };

                string title = Strings.Localize("gamePadInputDialog.unPluggedMoron");
                string actionA = Strings.Localize("gamePadInputDialog.unPluggedAction");
#if DEBUG
                title += " Debug Build.";
#endif // DEBUG
                dialog = new ModularMessageDialog(
                    title,
                    handlerA, actionA,
                    null, null,
                    null, null,
                    null, null);
            }
            if (BokuGame.bokuGame.IsActive && (dialog != null) && !dialog.Active)
            {
                dialog.Activate();
                _wasPaused = Time.Paused;
                Time.Paused = true;
            }
        }
        static bool _wasPaused = false;
        */

    }   // end of class GamePad

}   // end of namespace KoiX.Input




