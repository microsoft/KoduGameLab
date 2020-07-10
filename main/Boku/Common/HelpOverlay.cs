// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


// Uncomment this to see debug spew about the help overlay stack.
//#define DEBUG_SPEW

//#define SHOW_TITLE_SAFE_OVERLAY
//#define CYCLE_OVERLAY_PERCENTAGE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Input;
using KoiX.Text;

using Boku.Base;
using Boku.Fx;
using Boku.Common.Xml;
using Boku.Common.Localization;

namespace Boku.Common
{
    /// <summary>
    /// A static class which manages and displays help overlay screens.
    /// </summary>
    public class HelpOverlay
    {
        public class Entry
        {
            [XmlText]
            public string text = null;
            [XmlAttribute]
            public bool ghosted = false;
            [XmlAttribute]
            public bool mid = true;

            public bool MembersEqual(Entry entry)
            {
                return text == entry.text
                    && ghosted == entry.ghosted
                    && mid == entry.mid;
            }

            /// <summary>
            /// Copies any localized entry from src to the current Entry.
            /// </summary>
            /// <param name="src"></param>
            public static void CopyLocalized(Entry dst, Entry src)
            {
                if (src != null && !string.IsNullOrEmpty(src.text))
                {
                    if (dst == null)
                    {
                        dst = src;
                    }
                    else
                    {
                        dst.text = src.text;
                    }
                }
            }   // end of CopyLocalized()

        }

        public class Overlay
        {
            public string id = null;

            public Entry start = null;
            public Entry back = null;
            public Entry a = null;
            public Entry b = null;
            public Entry x = null;
            public Entry y = null;
            public Entry leftTrigger = null;
            public Entry rightTrigger = null;
            public Entry leftShoulder = null;
            public Entry rightShoulder = null;
            public Entry dpadVertical = null;
            public Entry dpadHorizontal = null;
            public Entry dpadUp = null;
            public Entry dpadDown = null;
            public Entry dpadRight = null;
            public Entry dpadLeft = null;
            public Entry leftStick = null;
            public Entry rightStick = null;
            public Entry bottom = null;

            public Entry keyMouse = null;                   // Note all keyboard/mouse strings are fit into a single Entry.
            public Entry keyMouseBottom = null;

            public Entry touch = null;
            public Entry touchBottom = null;

            // c'tor
            public Overlay()
            {
            }

            public bool MembersEqual(Overlay other)
            {
                //ToDo (DZ): This is rather verbose. Can we avoid such an ugly comparison? Either, we could change
                // overlay and overlay.Entity to structs or we could bring the gamepad stuff into single ojbects
                // like keyboard mouse is. Also, there might be .NET functions that do memberwise comparisons.
                return true
                    && (id == other.id)
                    && (start == null ? other.start == null : other.start != null && start.MembersEqual(other.start))
                    && (back == null ? other.back == null : other.back != null && back.MembersEqual(other.back))
                    && (a == null ? other.a == null : other.a != null && a.MembersEqual(other.a))
                    && (b == null ? other.b == null : other.b != null && b.MembersEqual(other.b))
                    && (x == null ? other.x == null : other.x != null && x.MembersEqual(other.x))
                    && (y == null ? other.y == null : other.y != null && y.MembersEqual(other.y))
                    && (leftTrigger == null ? other.leftTrigger == null : other.leftTrigger != null && leftTrigger.MembersEqual(other.leftTrigger))
                    && (rightTrigger == null ? other.rightTrigger == null : other.rightTrigger != null && rightTrigger.MembersEqual(other.rightTrigger))
                    && (leftShoulder == null ? other.leftShoulder == null : other.leftShoulder != null && leftShoulder.MembersEqual(other.leftShoulder))
                    && (rightShoulder == null ? other.rightShoulder == null : other.rightShoulder != null && rightShoulder.MembersEqual(other.rightShoulder))
                    && (dpadVertical == null ? other.dpadVertical == null : other.dpadVertical != null && dpadVertical.MembersEqual(other.dpadVertical))
                    && (dpadHorizontal == null ? other.dpadHorizontal == null : other.dpadHorizontal != null && dpadHorizontal.MembersEqual(other.dpadHorizontal))
                    && (leftStick == null ? other.leftStick == null : other.leftStick != null && leftStick.MembersEqual(other.leftStick))
                    && (rightStick == null ? other.rightStick == null : other.rightStick != null && rightStick.MembersEqual(other.rightStick))
                    && (bottom == null ? other.bottom == null : other.bottom != null && bottom.MembersEqual(other.bottom))

                    && (keyMouse == null ? other.keyMouse == null : other.keyMouse != null && keyMouse.MembersEqual(other.keyMouse))
                    && (keyMouseBottom == null ? other.keyMouseBottom == null : other.keyMouseBottom != null && keyMouseBottom.MembersEqual(other.keyMouseBottom))

                    && (touch == null ? other.touch == null : other.touch != null && touch.MembersEqual(other.touch))
                    && (touchBottom == null ? other.touchBottom == null : other.touchBottom != null && touchBottom.MembersEqual(other.touchBottom))
                    ;
            }   // end of MembersEqual()

            /// <summary>
            /// Copies any localized entries from src to the current HelpOverlay.
            /// </summary>
            /// <param name="src"></param>
            public void CopyLocalized(Overlay src)
            {
                Entry.CopyLocalized(start, src.start);
                Entry.CopyLocalized(back, src.back);
                Entry.CopyLocalized(a, src.a);
                Entry.CopyLocalized(b, src.b);
                Entry.CopyLocalized(x, src.x);
                Entry.CopyLocalized(y, src.y);
                Entry.CopyLocalized(leftTrigger, src.leftTrigger);
                Entry.CopyLocalized(rightTrigger,src.rightTrigger);
                Entry.CopyLocalized(leftShoulder, src.leftShoulder);
                Entry.CopyLocalized(rightShoulder, src.rightShoulder);
                Entry.CopyLocalized(dpadVertical, src.dpadVertical);
                Entry.CopyLocalized(dpadHorizontal, src.dpadHorizontal);
                Entry.CopyLocalized(dpadUp, src.dpadUp);
                Entry.CopyLocalized(dpadDown, src.dpadDown);
                Entry.CopyLocalized(dpadRight, src.dpadRight);
                Entry.CopyLocalized(dpadLeft, src.dpadLeft);
                Entry.CopyLocalized(leftStick, src.leftStick);
                Entry.CopyLocalized(rightStick, src.rightStick);
                Entry.CopyLocalized(bottom, src.bottom);

                Entry.CopyLocalized(keyMouse, src.keyMouse);
                Entry.CopyLocalized(keyMouseBottom, src.keyMouseBottom);

                Entry.CopyLocalized(touch, src.touch);
                Entry.CopyLocalized(touchBottom, src.touchBottom);
            }   // end of CopyLocalized()


        }   // end of class Overlay

        private static bool active = true;                  // Controls whether or not we render.
        private static float alpha = 1.0f;                  // Controls transparency of overlay.
        private static Vector2 screenSize = Vector2.Zero;

        private static List<Overlay> overlayList = null;    // A list of all available overlays.
        private static List<Overlay> stack = null;          // The currently active overlay.  Note that it is valid to
        // push null.

        private static RenderTarget2D overlayRT = null;
        private static string currentID = null;             // ID of the currently rendered overlay.

        private static float outlineWidth = 1.5f;

        private static GetFont Font = SharedX.GetGameFont18Bold;
        private static TextBlob blob = null;                // Shared TextBlob for all overlay rendering.

#if SHOW_TITLE_SAFE_OVERLAY
        private static Texture2D titleSafe = null;
#endif

        // Currently the help overlay content is arranged into two groups 
        // which reside on the left and at the bottom of the screen.  
        // The following variables define the position and size of these
        // groups for rendering to the screen.  The data for the vertices
        // is filled in when the overlay texture is rebuilt.
        private static Vector2 leftGroupPosition = Vector2.Zero;
        private static Vector2 leftGroupSize = Vector2.Zero;
        private static Vector2 bottomGroupPosition = Vector2.Zero;
        private static Vector2 bottomGroupSize = Vector2.Zero;

        private static ScreenSpaceQuad.Vertex[] verts = null;
        private static ScreenSpaceQuad.Vertex[] shiftedVerts = null;    // Shifted to account for tutorial mode.

        private static Texture2D toolIcon = null;

        public enum DisplayModes
        {
            GamePad,
            KeyboardMouse,
            Touch
        };
        private static DisplayModes curMode = DisplayModes.KeyboardMouse;
        private static DisplayModes prevMode = DisplayModes.KeyboardMouse;

        private static int prevHelpLevel = -1;

        private static bool suppressYButton = false;
        private static bool dirty = false;              // Do we need to re-render?

        #region Accessors

        public static bool Dirty
        {
            get { return dirty; }
            set { dirty = value; }
        }

        public static bool Active
        {
            get { return active; }
            set { active = value; }
        }
        public static float Alpha
        {
            get { return alpha; }
            set { alpha = value; }
        }

        /// <summary>
        /// The tool icon for the overlay to render into the upper
        /// right hand corner.  If null, nothing is rendered.
        /// </summary>
        public static Texture2D ToolIcon
        {
            get { return toolIcon; }
            set 
            {
                if (toolIcon != value)
                {
                    toolIcon = value;
                    dirty = true;
                }
            }
        }

        /// <summary>
        /// Suppress the help text for the Y button.  This is a bit hackish
        /// but makes it much easier to deal with the off case where we don't
        /// have help for an AddItemMenu element.
        /// </summary>
        public static bool SuppressYButton
        {
            get { return suppressYButton; }
            set
            {
                if (suppressYButton != value)
                {
                    dirty = true;
                }
                suppressYButton = value;
            }
        }

        public static DisplayModes DisplayMode
        {
            get { return curMode; }
            set { curMode = value; }
        }
        #endregion

        // private c'tor
        private HelpOverlay()
        {
        }

        /// <summary>
        /// Loads the available help overlays as listed in the inputFile.
        /// </summary>
        /// <param name="mediaPath"></param>
        /// <param name="inputFile"></param>
        public static void Init()
        {
            // Init lists.
            overlayList = new List<Overlay>();
            stack = new List<Overlay>();

            // Read in overlay information.
            XmlOverlayData overlayData = new XmlOverlayData();
            overlayData.ReadFromXml(LocalizationResourceManager.HelpOverlaysResource.Name);

            overlayList = overlayData.overlay;

            verts = new ScreenSpaceQuad.Vertex[12]; // Enough for 2 quads.
            shiftedVerts = new ScreenSpaceQuad.Vertex[12];

            blob = new TextBlob(Font, "testing", 100);
        }   // end of HelpOverlay Init()

        /// <summary>
        /// Pushes the overlay identified by id onto the stack.
        /// </summary>
        /// <param name="id"></param>
        public static void Push(string id)
        {
            // Handle simple case first.
            if (id == null)
            {
                stack.Add(null);
#if DEBUG_SPEW
                Debug.Print("pushing explicit NULL");
                DebugDump();
#endif
            }
            else
            {
                // Find the right overlay to push.
                for (int i = 0; i < overlayList.Count; i++)
                {
                    Overlay o = overlayList[i];
                    if (o.id == id)
                    {
                        stack.Add(o);

#if DEBUG_SPEW
                        Debug.Print("pushing " + id);
                        DebugDump();
#endif

                        return;
                    }
                }

                // Valid overlay not found, push null.
                stack.Add(null);
#if DEBUG_SPEW
                Debug.Print("pushing NULL since '" + id + "' not found");
                DebugDump();
#endif
            }

            // Ensure alpha is full on change.
            alpha = 1.0f;

            suppressYButton = false;
        }   // end of HelpOverlay Push()

        /// <summary>
        /// Pops the current overlay off the stack.
        /// </summary>
        /// <returns>The id just popped off the stack.</returns>
        public static string Pop()
        {
            string result = null;

            if (stack.Count > 0)
            {
                Overlay o = stack[stack.Count - 1];
                if (o != null)
                {
                    result = o.id;
                }

                stack.RemoveAt(stack.Count - 1);
            }

#if DEBUG_SPEW
            if (result != null)
            {
                Debug.Print("popped " + result);
            }
            else
            {
                Debug.Print("popped NULL");
            }
            DebugDump();
#endif
            // Ensure alpha is full on change.
            alpha = 1.0f;

            suppressYButton = false;

            return result;
        }   // end of HelpOverlay Pop()

        /// <summary>
        /// Returns the id of the overlay on top of the stack.  The
        /// stack is not changed.
        /// </summary>
        /// <returns></returns>
        public static string Peek()
        {
            string result = null;

            if (stack.Count > 0)
            {
                Overlay o = stack[stack.Count - 1];
                if (o != null)
                {
                    result = o.id;
                }
            }

            return result;
        }   // end of HelpOverlay Peek()

        /// <summary>
        /// Returns the id of the overlay at the specific index.
        /// 0 is the top element of the stack, 1 is the next, etc.
        /// The stack is not changed.
        /// </summary>
        /// <param name="index"></param>
        /// <returns>null if invalid index</returns>
        public static string Peek(int index)
        {
            string result = null;
            index = stack.Count - 1 - index;
            if (stack.Count > index && index >= 0)
            {
                Overlay o = stack[index];
                if (o != null)
                {
                    result = o.id;
                }
            }

            return result;
        }   // end of HelpOverlay Peek()

        /// <summary>
        /// Replace the top element on the stack with the new one.
        /// Returns the previous top.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string ReplaceTop(string id)
        {
            string result = Peek();
            if (result != id)
            {
                Pop();
                Push(id);
            }

            return result;
        }   // end of ReplaceTop()

        /// <summary>
        /// Returns the depth of the stack.
        /// </summary>
        /// <returns></returns>
        public static int Depth()
        {
            return stack.Count;
        }

        /// <summary>
        /// Removes the specific overlay from the stack.  
        /// Searches the stack from the top down.
        /// </summary>
        public static void Remove(string id)
        {
#if DEBUG_SPEW
            bool removed = false;
#endif
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                Overlay o = stack[i];
                if (o != null && String.Compare(id, o.id) == 0)
                {
                    stack.RemoveAt(i);
#if DEBUG_SPEW
                    removed = true;
                    Debug.Print("Removed " + id);
                    DebugDump();
#endif
                    break;
                }
            }

#if DEBUG_SPEW
            if (!removed)
            {
                Debug.Print("Failed to remove " + id);
                DebugDump();
            }
#endif

            // Ensure alpha is full on change.
            alpha = 1.0f;

            suppressYButton = false;
        }   // end of HelpOverlay Remove()


        /// <summary>
        /// Clear the help overlay stack.
        /// </summary>
        public static void Clear()
        {
#if DEBUG_SPEW
            Debug.Print("\nHelpOverlay Stack cleared");
#endif
            stack.Clear();
            suppressYButton = false;
        }   // end of HelpOverlay Clear()

        public static void DebugDump()
        {
#if DEBUG_SPEW
            Debug.Print("\nHelpOverlay Stack " + stack.Count.ToString());
#endif
            for (int i = 0; i < stack.Count; i++)
            {
                Overlay o = stack[i];
#if DEBUG_SPEW
                if (o != null)
                {
                    Debug.Print("    " + i.ToString() + " : " + o.id);
                }
                else
                {
                    Debug.Print("    " + i.ToString() + " : NULL");
                }
#endif
            }
        }   // end of HelpOverlay DebugDump()

        private static int leftButtonPosition = 10;         // Where the left edge of the buttons start.  Adjusted based on screen resolution.
        private static int startingVerticalPosition = 10;  // How far down the screen the help starts.  Adjusted based on screen resolution.
        private static int buttonSize = 40;                 // The current artwork uses 64x64 textures but the image is only 40x40.
        //private static int buttonArtSize = 40;
        private static int buttonLabelMargin = -11;         // Space between button icon and text.  Negative since button art doesn't fill full space.
        private static int verticalOffset = 4;              // Vertical offset between button icon and text.
        private static int verticalSpacing = -12;           // Space between buttons, vertically.
        private static int bottomMargin = 60;               // How far off the bottom to put the bottom text.  Adjusted based on screen resolution.

#if CYCLE_OVERLAY_PERCENTAGE
        static int tic = 0;
#endif

        static int desiredHelpLevel = 2;

        public static void RefreshTexture()
        {
            // If the Window has grown, re-allocate the overlayRT to match.  Note that we don't bother
            // to shrink the rt if the window shrinks. 
            if ((int)BokuGame.ScreenSize.X > overlayRT.Width || (int)BokuGame.ScreenSize.Y > overlayRT.Height)
            {
                ReleaseRenderTargets();
                CreateRenderTargets(KoiLibrary.GraphicsDevice);
            }

            try
            {
                if (stack.Count > 0)
                {
                    Overlay top = stack[stack.Count - 1];

#if CYCLE_OVERLAY_PERCENTAGE
                // Animate the overscan percentage to make it easier to debug/adjust.
                if (tic != (int)Time.WallClockTotalSeconds)
                {
                    XmlOptionsData.OverscanPercent += 5;
                    if (XmlOptionsData.OverscanPercent > 25)
                        XmlOptionsData.OverscanPercent = 0;
                    tic = (int)Time.WallClockTotalSeconds;
                }
#endif

                    // Did the window size change?
                    if (BokuGame.ScreenSize != screenSize)
                    {
                        // Update the screenSize and mark as dirty so we refresh the texture.
                        screenSize = BokuGame.ScreenSize;
                        dirty = true;
                    }

                    // Make sure we've got the proper current mode.
                    if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
                    {
                        HelpOverlay.DisplayMode = DisplayModes.KeyboardMouse;
                    }
                    else if (KoiLibrary.LastTouchedDeviceIsGamepad)
                    {
                        HelpOverlay.DisplayMode = DisplayModes.GamePad;
                    }
                    else if (KoiLibrary.LastTouchedDeviceIsTouch)
                    {
                        HelpOverlay.DisplayMode = DisplayModes.Touch;
                    }

                    // A bit of a hack.  If we are using a modal tool menu then we lock down the camera 
                    // when the menu is active.  So, also force the help level to mid so that we don't
                    // show the tips for moving the camera.
                    desiredHelpLevel = XmlOptionsData.HelpLevel;
                    if (XmlOptionsData.ModalToolMenu && top != null && top.id.StartsWith("ToolMenu") && desiredHelpLevel == 2)
                    {
                        desiredHelpLevel = 1;
                    }

                    // If we've change ids, modes, help levels or are just dirty, rebuild the texture.
                    if (top != null && (top.id != currentID || prevMode != curMode || prevHelpLevel != desiredHelpLevel || dirty))
                    {
                        prevHelpLevel = desiredHelpLevel;

                        FilterInvalidCharacters(top);

                        ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();

                        // Get font/batch instances for this frame.
                        SpriteBatch batch = KoiLibrary.SpriteBatch;

                        // Render the texture for the current help overlay.
                        // Set the rendertarget(s)
                        InGame.SetRenderTarget(overlayRT);

                        // Clear.
                        InGame.Clear(Color.Transparent);

                        // The extra 4 and 10 is just to make it look nice and make sure the help isn't right on the edge.
                        int horizontalMargin = 4;
                        int verticalMargin = 10;

                        leftButtonPosition = horizontalMargin;
                        startingVerticalPosition = verticalMargin;
                        bottomMargin = verticalMargin + Font().LineSpacing;

                        // Render overlay specific stuff here.
                        int y = startingVerticalPosition;

                        leftGroupPosition = new Vector2(Single.MaxValue, Single.MaxValue);
                        leftGroupSize = Vector2.Zero;
                        Vector2 pos = Vector2.Zero;
                        Vector2 size = Vector2.Zero;

                        Color foreColor = new Color(255, 255, 255, 255);
                        Color shadowColor = new Color(40, 40, 40, 255);

                        if (curMode == DisplayModes.GamePad)
                        {
                            // Start button
                            RenderLabel(ButtonTextures.StartButton, top.start, leftButtonPosition, ref y, buttonSize + verticalSpacing);

                            // Back button
                            RenderLabel(ButtonTextures.BackButton, top.back, leftButtonPosition, ref y, buttonSize + verticalSpacing);

                            // Y button
                            if (!SuppressYButton)
                            {
                                RenderLabel(ButtonTextures.YButton, top.y, leftButtonPosition, ref y, buttonSize + verticalSpacing);
                            }
                            else
                            {
                                y += buttonSize + verticalSpacing;
                            }

                            // X button
                            RenderLabel(ButtonTextures.XButton, top.x, leftButtonPosition, ref y, buttonSize + verticalSpacing);

                            // B button
                            RenderLabel(ButtonTextures.BButton, top.b, leftButtonPosition, ref y, buttonSize + verticalSpacing);

                            // A button
                            RenderLabel(ButtonTextures.AButton, top.a, leftButtonPosition, ref y, buttonSize + verticalSpacing);

                            // DPad up/down
                            RenderLabel(ButtonTextures.DPadUpDown, top.dpadVertical, leftButtonPosition, ref y, buttonSize + verticalSpacing);
                            // DPad right/left
                            RenderLabel(ButtonTextures.DPadRightLeft, top.dpadHorizontal, leftButtonPosition, ref y, buttonSize + verticalSpacing);

                            // DPad up
                            RenderLabel(ButtonTextures.DPadUp, top.dpadUp, leftButtonPosition, ref y, buttonSize + verticalSpacing);
                            // DPad down
                            RenderLabel(ButtonTextures.DPadDown, top.dpadDown, leftButtonPosition, ref y, buttonSize + verticalSpacing);

                            // DPad right
                            RenderLabel(ButtonTextures.DPadRight, top.dpadRight, leftButtonPosition, ref y, buttonSize + verticalSpacing);
                            // DPad left
                            RenderLabel(ButtonTextures.DPadLeft, top.dpadLeft, leftButtonPosition, ref y, buttonSize + verticalSpacing);

                            // Left trigger
                            RenderLabel(ButtonTextures.LeftTrigger, top.leftTrigger, leftButtonPosition, ref y, buttonSize + verticalSpacing);

                            // Right trigger
                            RenderLabel(ButtonTextures.RightTrigger, top.rightTrigger, leftButtonPosition, ref y, buttonSize + verticalSpacing);

                            // Left Stick
                            RenderLabel(ButtonTextures.LeftStick, top.leftStick, leftButtonPosition, ref y, buttonSize + verticalSpacing);

                            // Right Stick
                            RenderLabel(ButtonTextures.RightStick, top.rightStick, leftButtonPosition, ref y, buttonSize + verticalSpacing);

                            // Left shoulder
                            RenderLabel(ButtonTextures.LeftShoulderArrow, top.leftShoulder, leftButtonPosition, ref y, buttonSize + verticalSpacing);

                            // Right shoulder
                            RenderLabel(ButtonTextures.RightShoulderArrow, top.rightShoulder, leftButtonPosition, ref y, buttonSize + verticalSpacing);

                            // BottomText
                            bottomGroupSize = Vector2.Zero;
                            pos = Vector2.Zero;
                            if (top.bottom != null && top.bottom.text != null && top.bottom.text.Length > 0)
                            {
                                if (desiredHelpLevel == 2 || (desiredHelpLevel == 1 && top.bottom.mid == true))
                                {
                                    int width = (int)screenSize.X - 2 * horizontalMargin;
                                    blob.RawText = top.bottom.text;
                                    blob.Width = width;
                                    blob.Justification = TextHelper.Justification.Center;

                                    int x = horizontalMargin;
                                    y = (int)screenSize.Y - bottomMargin - (blob.NumLines - 1) * blob.Font().LineSpacing;
                                    pos = new Vector2(x, y);

                                    size = new Vector2(width, Font().LineSpacing);

                                    blob.RenderText(null, pos, foreColor, outlineColor: shadowColor, outlineWidth: outlineWidth);

                                    bottomGroupPosition = new Vector2(x, y);
                                    bottomGroupSize = new Vector2(size.X + 1, blob.NumLines * Font().LineSpacing + 6);
                                }
                            }

                        }
                        else if (curMode == DisplayModes.KeyboardMouse)
                        {
                            // curMode is keyboard/mouse

                            if (desiredHelpLevel != 0)
                            {
                                if (top.keyMouse != null && top.keyMouse.text != null)
                                {
                                    Char[] delimitersLines = new Char[] { '\n' };

                                    string str = top.keyMouse.text;
                                    string[] lines = str.Split(delimitersLines);

                                    int width = (int)(screenSize.X / 2.0f);
                                    blob.Width = width;
                                    blob.Justification = TextHelper.Justification.Left; 
                                    pos = new Vector2(leftButtonPosition, y);

                                    bool skipBlanks = true;     // Are we skipping blank lines at the beginning?

                                    for (int i = 0; i < lines.Length; i++)
                                    {
                                        if (lines[i] == null)
                                            continue;

                                        string curString = lines[i].Trim();
                                        if (curString.Length == 0)
                                        {
                                            // If not at the beginning, skip a half line's worth of spacing.
                                            if (!skipBlanks)
                                            {
                                                pos.Y += blob.TotalSpacing / 2.0f;
                                            }
                                            continue;
                                        }

                                        skipBlanks = false; // Found our first non-blank line.

                                        // If we're currently at the middle help level and we run across
                                        // the HelpLevelMid string, skip all the rest of the entries.
                                        string help = curString.ToLower();
                                        if (help == "helplevelmid")
                                        {
                                            if (desiredHelpLevel == 1)
                                            {
                                                // Skip the rest of the entries.
                                                break;
                                            }
                                            else
                                            {
                                                // Just skip this one since we're not at a help level that cares.
                                                continue;
                                            }
                                        }

                                        blob.RawText = curString;
                                        blob.RenderText(null, pos, foreColor, outlineColor: shadowColor, outlineWidth: outlineWidth);

                                        // Adjust drawing region.
                                        leftGroupPosition.X = MathHelper.Min(leftGroupPosition.X, pos.X);
                                        leftGroupPosition.Y = MathHelper.Min(leftGroupPosition.Y, pos.Y);

                                        // Adjust position.
                                        pos.Y += blob.NumLines * blob.TotalSpacing;

                                        leftGroupSize.X = MathHelper.Max(leftGroupSize.X, blob.GetLineWidth(0) + 1);
                                        leftGroupSize.Y = MathHelper.Max(leftGroupSize.Y, pos.Y);

                                    }
                                }
                            }

                            // BottomText
                            bottomGroupSize = Vector2.Zero;
                            
                            pos = Vector2.Zero;
                            if (top.keyMouseBottom != null && top.keyMouseBottom.text != null && top.keyMouseBottom.text.Length > 0)
                            {
                                if (desiredHelpLevel == 2 || (desiredHelpLevel == 1 && top.keyMouseBottom.mid == true))
                                {
                                    int width = (int)screenSize.X - 2 * horizontalMargin;
                                    blob.RawText = top.keyMouseBottom.text;
                                    blob.Width = width;
                                    blob.Justification = TextHelper.Justification.Center;

                                    int x = horizontalMargin;
                                    y = (int)screenSize.Y - bottomMargin - (blob.NumLines - 1) * blob.Font().LineSpacing;
                                    pos = new Vector2(x, y);

                                    size = new Vector2(width, Font().LineSpacing);

                                    blob.RenderText(null, pos, foreColor, outlineColor: shadowColor, outlineWidth: outlineWidth);

                                    bottomGroupPosition = new Vector2(x, y);
                                    bottomGroupSize = new Vector2(size.X + 1, blob.NumLines * Font().LineSpacing + 6);
                                }
                            }
                        }
                        else if (curMode == DisplayModes.Touch)
                        {
                            // curMode is touch

                            if (desiredHelpLevel != 0)
                            {
                                if (top.touch != null && top.touch.text != null)
                                {
                                    Char[] delimitersLines = new Char[] { '\n' };

                                    string str = top.touch.text;
                                    string[] lines = str.Split(delimitersLines);

                                    int width = (int)(screenSize.X / 2.0f);
                                    blob.Width = width;
                                    blob.Justification = TextHelper.Justification.Left; 
                                    pos = new Vector2(leftButtonPosition, y);

                                    bool skipBlanks = true;     // Are we skipping blank lines at the beginning?

                                    for (int i = 0; i < lines.Length; i++)
                                    {
                                        if (lines[i] == null)
                                            continue;

                                        string curString = lines[i].Trim();
                                        if (curString.Length == 0)
                                        {
                                            // If not at the beginning, skip a half line's worth of spacing.
                                            if (!skipBlanks)
                                            {
                                                pos.Y += blob.TotalSpacing / 2.0f;
                                            }
                                            continue;
                                        }

                                        skipBlanks = false; // Found our first non-blank line.

                                        // If we're currently at the middle help level and we run across
                                        // the HelpLevelMid string, skip all the rest of the entries.
                                        string help = curString.ToLower();
                                        if (help == "helplevelmid")
                                        {
                                            if (desiredHelpLevel == 1)
                                            {
                                                // Skip the rest of the entries.
                                                break;
                                            }
                                            else
                                            {
                                                // Just skip this one since we're not at a help level that cares.
                                                continue;
                                            }
                                        }

                                        blob.RawText = curString;
                                        blob.RenderText(null, pos, foreColor, outlineColor: shadowColor, outlineWidth: outlineWidth);

                                        // Adjust drawing region.
                                        leftGroupPosition.X = MathHelper.Min(leftGroupPosition.X, pos.X);
                                        leftGroupPosition.Y = MathHelper.Min(leftGroupPosition.Y, pos.Y);

                                        // Adjust position.
                                        pos.Y += blob.NumLines * blob.TotalSpacing;

                                        leftGroupSize.X = MathHelper.Max(leftGroupSize.X, blob.GetLineWidth(0) + 1);
                                        leftGroupSize.Y = MathHelper.Max(leftGroupSize.Y, pos.Y);

                                    }
                                }
                            }

                            // BottomText
                            bottomGroupSize = Vector2.Zero;
                            pos = Vector2.Zero;
                            if (top.touchBottom != null && top.touchBottom.text != null && top.touchBottom.text.Length > 0)
                            {
                                if (desiredHelpLevel == 2 || (desiredHelpLevel == 1 && top.touchBottom.mid == true))
                                {
                                    int width = (int)screenSize.X - 2 * horizontalMargin;
                                    blob.RawText = top.touchBottom.text;
                                    blob.Width = width;
                                    blob.Justification = TextHelper.Justification.Center;

                                    int x = horizontalMargin;
                                    y = (int)screenSize.Y - bottomMargin - (blob.NumLines - 1) * blob.Font().LineSpacing;
                                    pos = new Vector2(x, y);

                                    size = new Vector2(width, Font().LineSpacing);

                                    blob.RenderText(null, pos, foreColor, outlineColor: shadowColor, outlineWidth: outlineWidth);

                                    bottomGroupPosition = new Vector2(x, y);
                                    bottomGroupSize = new Vector2(size.X + 1, blob.NumLines * Font().LineSpacing + 6);
                                }
                            }
                        }

                        // Free refs.
                        batch = null;

                        // Restore the original backbuffer (original rendertarget).
                        InGame.RestoreRenderTarget();

                        currentID = top.id;

                        // Now update the vertices to match the new overlay content.
                        // Pad size a couple of pixels to accomodate the button icon textures.
                        leftGroupPosition -= new Vector2(2.0f);
                        leftGroupSize += new Vector2(4.0f, 6.0f);
                        bottomGroupPosition -= new Vector2(2.0f);
                        bottomGroupSize += new Vector2(4.0f);

                        // Magical half pixel offset.  Needs to be added into the position but not the UV
                        Vector2 magic = new Vector2(-0.5f, -0.5f);

                        // Transform position and size to homogeneous coordinates.
                        pos = 2.0f * (leftGroupPosition + magic) / screenSize - new Vector2(1.0f, 1.0f);
                        size = leftGroupSize * 2.0f / screenSize;
                        // Scale down help overlay for small screens.
                        float scale = screenSize.Y < 720 ? screenSize.Y / 720.0f : 1.0f;
                        size *= scale;

                        Vector2 uvPos = leftGroupPosition / screenSize;
                        Vector2 uvSize = leftGroupSize / screenSize;

                        // Fill in the vertex data.
                        verts[0] = verts[3] = new ScreenSpaceQuad.Vertex(new Vector2(pos.X + size.X, -pos.Y - size.Y), uvPos + uvSize);
                        verts[1] = new ScreenSpaceQuad.Vertex(new Vector2(pos.X + size.X, -pos.Y), uvPos + new Vector2(uvSize.X, 0));
                        verts[2] = verts[4] = new ScreenSpaceQuad.Vertex(new Vector2(pos.X, -pos.Y), uvPos);
                        verts[5] = new ScreenSpaceQuad.Vertex(new Vector2(pos.X, -pos.Y - size.Y), uvPos + new Vector2(0, uvSize.Y));

                        Vector2 bot = bottomGroupPosition;
                        // Center on screen.
                        bot.X = (int)(screenSize.X - bottomGroupSize.X * scale) / 2.0f;
                        bot.Y += (int)((1.0f - scale) * bottomGroupSize.Y);
                        pos = 2.0f * (bot + magic) / screenSize - new Vector2(1.0f, 1.0f);
                        size = bottomGroupSize * 2.0f / screenSize;
                        size *= scale;

                        uvPos = bottomGroupPosition / screenSize;
                        uvSize = bottomGroupSize / screenSize;

                        verts[6] = verts[9] = new ScreenSpaceQuad.Vertex(new Vector2(pos.X + size.X, -pos.Y - size.Y), uvPos + uvSize);
                        verts[7] = new ScreenSpaceQuad.Vertex(new Vector2(pos.X + size.X, -pos.Y), uvPos + new Vector2(uvSize.X, 0));
                        verts[8] = verts[10] = new ScreenSpaceQuad.Vertex(new Vector2(pos.X, -pos.Y), uvPos);
                        verts[11] = new ScreenSpaceQuad.Vertex(new Vector2(pos.X, -pos.Y - size.Y), uvPos + new Vector2(0, uvSize.Y));

                        dirty = false;
                        prevMode = curMode;
                    }
                }
            }
            catch
            {
                // Do nothing...
                Debug.Assert(false, "Hey, something is throwing.  Fix it!");
            }
        }   // end of HelpOverlay RefreshTexture()

        private static void FilterInvalidCharacters(Overlay o)
        {
            if (o.a != null)
                o.a.text = TextHelper.FilterInvalidCharacters(o.a.text);
            if (o.b != null)
                o.b.text = TextHelper.FilterInvalidCharacters(o.b.text);
            if (o.x != null)
                o.x.text = TextHelper.FilterInvalidCharacters(o.x.text);
            if (o.y != null)
                o.y.text = TextHelper.FilterInvalidCharacters(o.y.text);

            if (o.start != null)
                o.start.text = TextHelper.FilterInvalidCharacters(o.start.text);
            if (o.back != null)
                o.back.text = TextHelper.FilterInvalidCharacters(o.back.text);

            if (o.dpadHorizontal != null)
                o.dpadHorizontal.text = TextHelper.FilterInvalidCharacters(o.dpadHorizontal.text);
            if (o.dpadVertical != null)
                o.dpadVertical.text = TextHelper.FilterInvalidCharacters(o.dpadVertical.text);

            if (o.leftShoulder != null)
                o.leftShoulder.text = TextHelper.FilterInvalidCharacters(o.leftShoulder.text);
            if (o.rightShoulder != null)
                o.rightShoulder.text = TextHelper.FilterInvalidCharacters(o.rightShoulder.text);

            if (o.leftStick != null)
                o.leftStick.text = TextHelper.FilterInvalidCharacters(o.leftStick.text);
            if (o.rightStick != null)
                o.rightStick.text = TextHelper.FilterInvalidCharacters(o.rightStick.text);

            if (o.leftTrigger != null)
                o.leftTrigger.text = TextHelper.FilterInvalidCharacters(o.leftTrigger.text);
            if (o.rightTrigger != null)
                o.rightTrigger.text = TextHelper.FilterInvalidCharacters(o.rightTrigger.text);

            if (o.bottom != null)
                o.bottom.text = TextHelper.FilterInvalidCharacters(o.bottom.text);
        }   // end of FilterInvalidCharacters()

        /// <summary>
        /// Renders the overlay on the top of the stack.
        /// </summary>
        public static void Render()
        {
            if (XmlOptionsData.HelpLevel != 0 && Active && stack.Count > 0 && Alpha > 0.0f)
            {
                Overlay top = stack[stack.Count - 1];
                if (top != null)
                {
                    SpriteBatch batch = KoiLibrary.SpriteBatch;
                    batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                    {
                        if (leftGroupSize != Vector2.Zero)
                        {
                            Rectangle srcRect = new Rectangle((int)leftGroupPosition.X, (int)leftGroupPosition.Y, (int)leftGroupSize.X, (int)leftGroupSize.Y);
                            // dstRect adjusted to account for TutorialMode.
                            Rectangle dstRect = new Rectangle((int)leftGroupPosition.X, (int)leftGroupPosition.Y, (int)leftGroupSize.X, (int)leftGroupSize.Y);
                            batch.Draw(overlayRT, dstRect, srcRect, Color.White);
                        }

                        if (bottomGroupSize != Vector2.Zero)
                        {
                            Rectangle srcRect = new Rectangle((int)bottomGroupPosition.X, (int)bottomGroupPosition.Y, (int)bottomGroupSize.X, (int)bottomGroupSize.Y);
                            Rectangle dstRect = new Rectangle((int)bottomGroupPosition.X, (int)bottomGroupPosition.Y, (int)bottomGroupSize.X, (int)bottomGroupSize.Y);
                            batch.Draw(overlayRT, dstRect, srcRect, Color.White);
                        }
                    }
                    batch.End();

                    // As part of the HelpOverlay if we're in the tool menu 
                    // render the appropriate tool icon into the upper 
                    // right-hand corner of the screen.
                    if (toolIcon != null && !toolIcon.GraphicsDevice.IsDisposed)
                    {
                        // Calc size, shrink for smaller displays.
                        Vector2 size = new Vector2(toolIcon.Width, toolIcon.Height);
                        if (screenSize.Y < 720)
                        {
                            float scale = screenSize.Y / 720.0f;
                            size *= scale;
                        }

                        // Calc position.
                        batch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
                        {
                            Rectangle dstRect = new Rectangle((int)(screenSize.X - size.X), 0, (int)size.X, (int)size.Y);
                            batch.Draw(toolIcon, dstRect, Color.White);
                        }
                        batch.End();
                    }

                }
#if SHOW_TITLE_SAFE_OVERLAY
                {
                    ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();
                    ssquad.Render(titleSafe, Vector2.Zero, new Vector2(KoiLibrary.GraphicsDevice.Viewport.Width, KoiLibrary.GraphicsDevice.Viewport.Height), @"TexturedRegularAlpha");
                }
#endif
            }
        }   // end of HelpOverlay Render()

        /// <summary>
        /// Renders a single line for the help overlay including the button icon and the associated text.
        /// </summary>
        /// <param name="texture">Button icon texture</param>
        /// <param name="text">Text string</param>
        /// <param name="ghosted">Should this item be ghosted?</param>
        /// <param name="x">Position</param>
        /// <param name="y">Position  Note that y is changed by the increment amount if the label is rendered.</param>
        /// <param name="yIncrement">The amount y is incremented if the label is rendered.</param>
        private static void RenderLabel(Texture2D texture, Entry entry, int x, ref int y, int yIncrement)
        {
            if (entry != null && entry.text != null && entry.text.Length > 0)
            {
                if (desiredHelpLevel == 2 || (desiredHelpLevel == 1 && entry.mid == true))
                {
                    Color foreColor = new Color(255, 255, 255, 255);
                    Color shadowColor = new Color(40, 40, 40, 255);
                    if (entry.ghosted)
                    {
                        foreColor = new Color(255, 255, 255, 127);
                        shadowColor = new Color(40, 40, 40, 127);
                    }
                    ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();
                    quad.Render(texture, foreColor.ToVector4(), new Vector2(x, y + verticalOffset), new Vector2(buttonSize), @"TexturedRegularAlpha");

                    string text = TextHelper.FilterInvalidCharacters(entry.text);
                    
#if NETFX_CORE
                    TextHelper.DrawString(Font, text, new Vector2(x + buttonSize + buttonLabelMargin + 1, y + 1), shadowColor);
                    TextHelper.DrawString(Font, text, new Vector2(x + buttonSize + buttonLabelMargin, y), foreColor);
#else
                    SysFont.StartBatch(null);
                    SysFont.DrawString(text, new Vector2(x + buttonSize + buttonLabelMargin, y), new RectangleF(), Font().systemFont, foreColor, outlineColor: shadowColor, outlineWidth: 1.5f);
                    SysFont.EndBatch();
#endif
                    
                    // Adjust drawing region.
                    leftGroupPosition.X = MathHelper.Min(leftGroupPosition.X, x);
                    leftGroupPosition.Y = MathHelper.Min(leftGroupPosition.Y, y);

                    int width = (int)(buttonSize + buttonLabelMargin + 1 + Font().MeasureString(text).X);
                    leftGroupSize.X = MathHelper.Max(leftGroupSize.X, width + 1);
                    leftGroupSize.Y = MathHelper.Max(leftGroupSize.Y, y + Font().LineSpacing - leftGroupPosition.Y);

                    y += yIncrement;
                }
            }
        }   // end of RenderLabel()

        /// <summary>
        /// Check if the given input position falls withing the bounds of the bottom text.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static bool MouseHitBottomText(Point position)
        {
            bool result = false;

            if (position.X > bottomGroupPosition.X && position.X < bottomGroupPosition.X + bottomGroupSize.X
                && position.Y > bottomGroupPosition.Y && position.Y < bottomGroupPosition.Y + bottomGroupSize.Y)
            {
                result = true;
            }

            return result;
        }

        #region INeedsDeviceReset Members

        public static void LoadContent(bool immediate)
        {
        }   // end of HelpOverlay LoadContent()

        public static void InitDeviceResources(GraphicsDevice device)
        {
            CreateRenderTargets(device);

            // Force a refresh of the texture.  This ensure proper device reset handling.
            currentID = null;
            RefreshTexture();

#if SHOW_TITLE_SAFE_OVERLAY
            titleSafe = KoiLibrary.LoadTexture2D(@"Textures\TitleSafe");
#endif

        }

        public static void UnloadContent()
        {
            ReleaseRenderTargets();

#if SHOW_TITLE_SAFE_OVERLAY
            DeviceResetX.Release(ref titleSafe);
#endif
        }   // end of HelpOverlay UnloadContent()

        /// <summary>
        /// Recreate render targets.
        /// </summary>
        public static void DeviceReset(GraphicsDevice device)
        {
            ReleaseRenderTargets();
            CreateRenderTargets(device);
        }

        private static void ReleaseRenderTargets()
        {
            SharedX.RelRT("HelpOverlay", overlayRT);
            DeviceResetX.Release(ref overlayRT);
        }

        private static void CreateRenderTargets(GraphicsDevice device)
        {
            int width = (int)BokuGame.ScreenSize.X;
            int height = (int)BokuGame.ScreenSize.Y;
            overlayRT = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.None);
            SharedX.GetRT("HelpOverlay", overlayRT);

            // Set the rendertarget(s)
            InGame.SetRenderTarget(overlayRT);

            // Clear.
            InGame.Clear(Color.Transparent);

            // Restore the original backbuffer (original rendertarget).
            InGame.RestoreRenderTarget();

            /*
            // If batch has "gone bad", replace it.
            SpriteBatch batch = null;
            try
            {
                batch = KoiLibrary.SpriteBatch;
                batch.Begin();
                batch.End();
            }
            catch
            {
                batch = new SpriteBatch(device);
                KoiLibrary.SpriteBatch = batch;
            }
            */

            dirty = true;
            //RefreshTexture();
        }

        #endregion

    }   // end of class HelpOverlay

    //
    //
    // Xml file reading.
    //
    //

    public class XmlOverlayData
    {
        [XmlElement(Type = typeof(HelpOverlay.Overlay))]
        public List<HelpOverlay.Overlay> overlay = null;

        public XmlOverlayData()
        {
            overlay = new List<HelpOverlay.Overlay>();
        }

        /// <summary>
        /// Returns true on success, false if failed.
        /// </summary>
        public bool ReadFromXml(string filename)
        {
            bool success = true;

            // First we read in the help overlay data from the default language path, ie EN.
            // Then if a localized version exists, we read that one and overwrite any entries
            // that have the same key.  This way any missing entries will still be represented
            // by the English version.

            // Fix up the filename with the full path.
            string defaultFile = Path.Combine(Localizer.DefaultLanguageDir, filename);

            // Read the Xml file into local data.
            XmlOverlayData data = Load(defaultFile);

            // Build a dictionary of the default English data
            Dictionary<string, HelpOverlay.Overlay> dict = new Dictionary<string, HelpOverlay.Overlay>(data.overlay.Count);
            foreach (HelpOverlay.Overlay overlay in data.overlay)
            {
                dict[overlay.id] = overlay;
            }

            // Is our run-time local language different from the default?
            if (!Localizer.IsLocalDefault)
            {
                string localPath = Localizer.LocalLanguageDir;

                // Do we have a directory for the local language?
                if (localPath != null)
                {
                    string localFile = Path.Combine(localPath, filename);

                    if (Storage4.FileExists(localFile, StorageSource.All))
                    {
                        XmlOverlayData localData = Load(localFile);
                        Dictionary<string, HelpOverlay.Overlay> localDict = new Dictionary<string, HelpOverlay.Overlay>(localData.overlay.Count);
                        foreach (HelpOverlay.Overlay overlay in localData.overlay)
                        {
                            localDict[overlay.id] = overlay;
                        }

                        // Replace as much of the default data as we can with localized data
                        string[] keys = dict.Keys.ToArray();
                        foreach (string key in keys)
                        {
                            if (localDict.ContainsKey(key))
                            {
                                if (Localizer.ShouldReportMissing && dict[key].MembersEqual(localDict[key]))
                                {
                                    Localizer.ReportIdentical(filename, key);
                                }
                                else
                                {
                                    // dict[key] = localDict[key];
                                    // Previously we treated overlays as a unit and replaced
                                    // them all or nothing.  Now we only want to replace on 
                                    // a per Entery basis if the Entry has been localized.
                                    dict[key].CopyLocalized(localDict[key]);
                                }
                            }
                            else
                            {
                                Localizer.ReportMissing(filename, key);
                            }
                        }

                        data.overlay = dict.Values.ToList();
                    }
                    else
                    {
                        Localizer.ReportMissing(filename, "CAN'T FIND FILE!");
                    }
                }
                else
                {
                    Localizer.ReportMissing(localPath, "CAN'T FIND PATH FOR THIS LANGUAGE!");
                }
            }

            if (data == null)
            {
                success = false;
            }
            else
            {
                this.overlay = data.overlay;
            }

            return success;
        }   // end of XmlOverlayData ReadFromXml()


        private static XmlOverlayData Load(string filename)
        {
            XmlOverlayData data = null;
            Stream stream = null;

            // First try with StorageSoruce.All so we get the version downloaded
            // from the servers.  If that fails then get the TitleSpace version.
            try
            {
                stream = Storage4.OpenRead(filename, StorageSource.All);

                XmlSerializer serializer = new XmlSerializer(typeof(XmlOverlayData));
                data = (XmlOverlayData)serializer.Deserialize(stream);
            }
            catch (Exception e)
            {
                data = null;
                if (e != null)
                {
#if !NETFX_CORE
                    string message = e.Message;
                    if (e.InnerException != null)
                    {
                        message += e.InnerException.Message;
                    }
                    System.Windows.Forms.MessageBox.Show(
                        message,
                        "Error reading " + filename,
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Error
                        );
#endif
                }
            }
            finally
            {
                Storage4.Close(stream);
            }

            // If we don't have data.  Delete the server version of 
            // the file and try loading the TitleSpace version.
            if (data == null)
            {
                // Don't delete the server version since this might actually be someone 
                // trying to do a localization.
                //Storage4.Delete(filename);

                try
                {
                    stream = Storage4.OpenRead(filename, StorageSource.TitleSpace);

                    XmlSerializer serializer = new XmlSerializer(typeof(XmlOverlayData));
                    data = (XmlOverlayData)serializer.Deserialize(stream);
                }
                catch (Exception)
                {
                    data = null;
                }
                finally
                {
                    Storage4.Close(stream);
                }
            }

            return data;
        }   // end of Load()

    }   // end of class XmlOverlayData


}   // end of namespace Boku.Common
