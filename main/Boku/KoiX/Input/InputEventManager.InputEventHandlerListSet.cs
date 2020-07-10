// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Content;

using KoiX.Managers;


namespace KoiX.Input
{
    public partial class InputEventManager : InputEventHandler
    {
        /// <summary>
        /// The idea behind these sets is to create isolated sets of inputs so
        /// modal dialogs work correctly.  The process should look like:
        /// 
        /// push new set
        /// modal dialog RegisterForInputEvents
        /// ...do dialog stuff
        /// modal dialog UnregisterForInputEvents
        /// pop set
        /// 
        /// This ensures that absolutely no input will ever go to previously 
        /// registered input event handlers.
        /// </summary>
        public class InputEventHandlerListSet
        {
            #region Members

            static Stack<InputEventHandlerListSet> setStack = new Stack<InputEventHandlerListSet>();

            // List of objects which have registered themselves as
            // interested in these events.
            public List<InputEventHandler> MouseLeftDownList = new List<InputEventHandler>();
            public List<InputEventHandler> MouseMiddleDownList = new List<InputEventHandler>();
            public List<InputEventHandler> MouseRightDownList = new List<InputEventHandler>();
            public List<InputEventHandler> MouseLeftUpList = new List<InputEventHandler>();
            public List<InputEventHandler> MouseMiddleUpList = new List<InputEventHandler>();
            public List<InputEventHandler> MouseRightUpList = new List<InputEventHandler>();
            public List<InputEventHandler> MouseMoveList = new List<InputEventHandler>();
            public List<InputEventHandler> MousePositionList = new List<InputEventHandler>();
            public List<InputEventHandler> MouseHoverList = new List<InputEventHandler>();
            public List<InputEventHandler> MouseWheelList = new List<InputEventHandler>();
            public List<InputEventHandler> KeyboardList = new List<InputEventHandler>();
            public List<InputEventHandler> WinFormsKeyboardList = new List<InputEventHandler>();
            public List<InputEventHandler> TouchList = new List<InputEventHandler>();
            public List<InputEventHandler> TapList = new List<InputEventHandler>();
            public List<InputEventHandler> DoubleTapList = new List<InputEventHandler>();
            public List<InputEventHandler> HoldList = new List<InputEventHandler>();
            public List<InputEventHandler> OnePointDragList = new List<InputEventHandler>();
            public List<InputEventHandler> TwoPointDragList = new List<InputEventHandler>();
            public List<InputEventHandler> GamePadList = new List<InputEventHandler>();

            #endregion

            #region Accessors

            public static InputEventHandlerListSet CurSet
            {
                get { return setStack.Peek(); }
            }

            #endregion

            #region Public

            /// <summary>
            /// Private c'tor.  You should use PushSet() to get a new set..
            /// </summary>
            private InputEventHandlerListSet()
            {
            }   // end of c'tor

            public static InputEventHandlerListSet PushSet()
            {
                InputEventHandlerListSet set = new InputEventHandlerListSet();
                setStack.Push(set);

                return set;
            }   // end of PushSet()

            public static void PopSet()
            {
                ValidateEmptyLists();
                setStack.Pop();

                Debug.Assert(setStack.Count > 0, "Stack can never be empty.");

            }   // end of PopSet()

            /// <summary>
            /// When switching scenes, there should only be one set on the
            /// stack and all the lists should be empty.  If not, we're 
            /// leaking somewhere.
            /// 
            /// Actually, the count can be 2.  This occurs at startup if the 
            /// AuthSignIn dialog is active.
            /// </summary>
            public static void ValidateSceneSwitch()
            {
                Debug.Assert(setStack.Count == 1);

                ValidateEmptyLists();
            }   // end of ValidateSceneSwitch()

            /// <summary>
            /// Check that all lists are empty.  
            /// This should be true on pop.
            /// If any of these fire, it's not the end of the world but
            /// it indicates that something is not being processed in 
            /// the right order or that something is not properly 
            /// unregistering itself on deactivation.
            /// So, if these fire, instead of commenting them out, go
            /// and fix what's broken.
            /// </summary>
            public static void ValidateEmptyLists()
            {
                Debug.Assert(CurSet.MouseLeftDownList.Count == 0);
                Debug.Assert(CurSet.MouseMiddleDownList.Count == 0);
                Debug.Assert(CurSet.MouseRightDownList.Count == 0);
                Debug.Assert(CurSet.MouseLeftUpList.Count == 0);
                Debug.Assert(CurSet.MouseMiddleUpList.Count == 0);
                Debug.Assert(CurSet.MouseRightUpList.Count == 0);
                Debug.Assert(CurSet.MouseMoveList.Count == 0);
                Debug.Assert(CurSet.MousePositionList.Count == 0);
                Debug.Assert(CurSet.MouseHoverList.Count == 0);
                Debug.Assert(CurSet.MouseWheelList.Count == 0);
                Debug.Assert(CurSet.KeyboardList.Count == 0);
                Debug.Assert(CurSet.WinFormsKeyboardList.Count == 0);
                Debug.Assert(CurSet.TouchList.Count == 0);
                Debug.Assert(CurSet.TapList.Count == 0);
                Debug.Assert(CurSet.DoubleTapList.Count == 0);
                Debug.Assert(CurSet.HoldList.Count == 0);
                Debug.Assert(CurSet.OnePointDragList.Count == 0);
                Debug.Assert(CurSet.TwoPointDragList.Count == 0);
                Debug.Assert(CurSet.GamePadList.Count == 0);
            }   // end of ValidateEmptyLists()

            #endregion

        }   // end of  class InputEventHandlerListSet

    }   // end of class InputEventManager

}   // end of namespace KoiX.Input
