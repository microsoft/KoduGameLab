// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;


namespace Boku.Common
{
    /// <summary>
    /// Actions is just a container class to hold the standard MetaButtons.
    /// </summary>
    public class Actions
    {
        #region Members

        public static MetaButton Help = new MetaButton(Buttons.Y, Keys.Y, Keys.F1);
        public static MetaButton Pause = new MetaButton(Keys.Pause);
        public static MetaButton Unpause = new MetaButton(Buttons.A, Keys.Pause, Keys.A, Keys.Enter);
        public static MetaButton Start = new MetaButton(Buttons.Start, Keys.Home);
        public static MetaButton Restart = new MetaButton(Buttons.A, Keys.A, Keys.Enter);

        public static MetaButton Select = new MetaButton(Buttons.A, Keys.A, Keys.Enter);
        public static MetaButton Unselect = new MetaButton(Buttons.B, Keys.B, Keys.Escape);
        public static MetaButton Cancel = new MetaButton(Buttons.B, Buttons.Back, Keys.B, Keys.Escape);

        // In edit modes, "Cancel" is split into multiple varients.  Kind of ugly...
        public static MetaButton RunSim = new MetaButton(Buttons.Back, Keys.Escape, Keys.Enter);
        public static MetaButton MiniHub = new MetaButton(Buttons.Start, Keys.Home, Keys.F10);
        public static MetaButton ToolMenu = new MetaButton(Buttons.Back, Keys.Escape);
        public static MetaButton AltToolMenu = new MetaButton(Buttons.B, Keys.B);

        public static MetaButton Cut = new MetaButton(Buttons.LeftTrigger, MetaButton.Ctrl | Keys.X, Keys.Delete);
        public static MetaButton ProgrammingEditorCut = new MetaButton(Buttons.LeftTrigger, Buttons.X, MetaButton.Ctrl | Keys.X, Keys.Delete, Keys.Back);
        public static MetaButton Copy = new MetaButton(MetaButton.Ctrl | Keys.C, MetaButton.Ctrl | Keys.Insert);
        public static MetaButton ProgrammingEditorCopy = new MetaButton(Buttons.X, MetaButton.Ctrl | Keys.C, MetaButton.Ctrl | Keys.Insert);
        public static MetaButton Paste = new MetaButton(Buttons.RightTrigger, MetaButton.Ctrl | Keys.V, Keys.Insert);

        public static MetaButton Undo = new MetaButton(Buttons.X, Keys.X, MetaButton.Ctrl | Keys.Z);
        public static MetaButton Redo = new MetaButton(Buttons.Y, Keys.Y, MetaButton.Ctrl | Keys.Y);

        public static MetaButton Up = new MetaButton(Buttons.LeftThumbstickUp, Keys.Up);
        public static MetaButton Down = new MetaButton(Buttons.LeftThumbstickDown, Keys.Down);
        public static MetaButton Right = new MetaButton(Buttons.LeftThumbstickRight, Keys.Right);
        public static MetaButton Left = new MetaButton(Buttons.LeftThumbstickLeft, Keys.Left);
        public static MetaButton RightUp = new MetaButton(Buttons.RightThumbstickUp, MetaButton.Shift | Keys.Up);
        public static MetaButton RightDown = new MetaButton(Buttons.RightThumbstickDown, MetaButton.Shift | Keys.Down);

        public static MetaButton Next = new MetaButton(Buttons.RightShoulder, Keys.Tab);
        public static MetaButton Prev = new MetaButton(Buttons.LeftShoulder, MetaButton.Shift | Keys.Tab);

        public static MetaButton NextActor = new MetaButton(Keys.Tab);
        public static MetaButton PrevActor = new MetaButton(MetaButton.Shift | Keys.Tab);

        // Camera control
        public static MetaButton ZoomIn = new MetaButton(Buttons.RightShoulder, Keys.PageUp);
        public static MetaButton ZoomOut = new MetaButton(Buttons.LeftShoulder, Keys.PageDown);

        // Object editing.
        public static MetaButton Add = new MetaButton(Buttons.A, Keys.A);
        public static MetaButton Program = new MetaButton(Buttons.Y, Keys.Y);
        public static MetaButton Tweak = new MetaButton(Buttons.X, Keys.X);
        public static MetaButton ColorLeft = new MetaButton(Buttons.DPadLeft, Keys.Left);
        public static MetaButton ColorRight = new MetaButton(Buttons.DPadRight, Keys.Right);
        public static MetaButton Raise = new MetaButton(Buttons.DPadUp, Keys.Up);
        public static MetaButton Lower = new MetaButton(Buttons.DPadDown, Keys.Down);
        public static MetaButton Bigger = new MetaButton(Buttons.DPadUp, Keys.Up);
        public static MetaButton Smaller = new MetaButton(Buttons.DPadDown, Keys.Down);
        public static MetaButton NextType = new MetaButton(Buttons.DPadUp, Keys.Tab);
        public static MetaButton PrevType = new MetaButton(Buttons.DPadDown, MetaButton.Shift | Keys.Tab);
        public static MetaButton NextTreeType = new MetaButton(Buttons.DPadUp, Keys.Up);
        public static MetaButton PrevTreeType = new MetaButton(Buttons.DPadDown, Keys.Down);

        // Waypoint editing
        public static MetaButton ChangeDirection = new MetaButton(Buttons.Y, Keys.Y);
        public static MetaButton TogglePath = new MetaButton(Buttons.X, Keys.X);
        public static MetaButton PathPickup = new MetaButton(Buttons.A);
        public static MetaButton PathPut = new MetaButton(Buttons.A, Keys.Enter);
        public static MetaButton PathDone = new MetaButton(Buttons.B, Keys.Escape, Keys.B);
        public static MetaButton PathDelete = new MetaButton(Buttons.LeftTrigger, Keys.Delete);
        public static MetaButton SplitEdge = new MetaButton(Buttons.RightTrigger, Keys.Insert);
        public static MetaButton AddNodes = new MetaButton(Buttons.Y, Keys.Insert);
        public static MetaButton PutNodeGo = new MetaButton(Buttons.A, Keys.Insert);
        public static MetaButton PutNodeDone = new MetaButton(Buttons.Y, Keys.Enter);
        
        // Brush/Material pickers.
        public static MetaButton PickerX = new MetaButton(Buttons.X, Keys.X);
        public static MetaButton PickerY = new MetaButton(Buttons.Y, Keys.Y);
        public static MetaButton PickerLeft = new MetaButton(Buttons.RightTrigger, Buttons.DPadRight, Keys.Right);
        public static MetaButton PickerRight = new MetaButton(Buttons.LeftTrigger, Buttons.DPadLeft, Keys.Left);
        public static MetaButton BrushLarger = new MetaButton(Buttons.DPadRight, Keys.Right);
        public static MetaButton BrushSmaller = new MetaButton(Buttons.DPadLeft, Keys.Left);
        public static MetaButton MaterialCubic = new MetaButton(Buttons.DPadUp, Keys.Up);
        public static MetaButton MaterialFabric = new MetaButton(Buttons.DPadDown, Keys.Down);
        public static MetaButton Sample = new MetaButton(Buttons.Y, Keys.Y);

        // Programming
        public static MetaButton PrintKodu = new MetaButton(Buttons.Y, MetaButton.Ctrl | Keys.P, Keys.PrintScreen);

        // LoadLevelMenu
        public static MetaButton SortBy = new MetaButton(Buttons.Y, Keys.Y);

        // ShareHub
        public static MetaButton StartSharing = new MetaButton(Buttons.Start, Keys.Enter);

        // Explicit buttons, should be used rarely if at all...
        public static MetaButton A = new MetaButton(Buttons.A, Keys.A);
        public static MetaButton B = new MetaButton(Buttons.B, Keys.B);
        public static MetaButton X = new MetaButton(Buttons.X, Keys.X);
        public static MetaButton Y = new MetaButton(Buttons.Y, Keys.Y);

        // Combines left stick and dPad.
        public static MetaButton ComboUp = new MetaButton(Buttons.LeftThumbstickUp, Buttons.DPadUp, Keys.Up);
        public static MetaButton ComboDown = new MetaButton(Buttons.LeftThumbstickDown, Buttons.DPadDown, Keys.Down);
        public static MetaButton ComboRight = new MetaButton(Buttons.LeftThumbstickRight, Buttons.DPadRight, Keys.Right);
        public static MetaButton ComboLeft = new MetaButton(Buttons.LeftThumbstickLeft, Buttons.DPadLeft, Keys.Left);

        // Used by help screens which have the left stick mapped elsewhere.
        public static MetaButton AltUp = new MetaButton(Buttons.RightThumbstickUp);
        public static MetaButton AltDown = new MetaButton(Buttons.RightThumbstickDown);
        public static MetaButton AltRight = new MetaButton(Buttons.RightThumbstickRight);
        public static MetaButton AltLeft = new MetaButton(Buttons.RightThumbstickLeft);

        // KeyMouse edit mode.
        public static MetaButton CameraMove = new MetaButton(Keys.Space);

        // Keyboard only.
        public static MetaButton PrintScreen = new MetaButton(Keys.PrintScreen, MetaButton.Ctrl | Keys.P);
        public static MetaButton ShiftPrintScreen = new MetaButton(MetaButton.Shift | Keys.PrintScreen, MetaButton.Ctrl | Keys.P);

        #endregion

        #region Internal

        // c'tor
        private Actions()
        {
        }

        #endregion

        /// <summary>
        /// Line segment with slope between 0 and 1.
        /// Real valued endpoints.
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        static void LineSlope_0_1(Vector2 p0, Vector2 p1)
        {
            Vector2 d = p1 - p0;
            float m = d.Y / d.X;
        }

    }   // end of class Actions

}   // end of namespace Boku.Common.ButtonCollections
