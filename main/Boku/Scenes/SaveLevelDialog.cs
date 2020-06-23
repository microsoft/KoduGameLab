
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;


using Boku.Base;
using Boku.Common;
using Boku.Common.Xml;
using Boku.Common.Sharing;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.Programming;
using Boku.SimWorld;
using Boku.Web;
using Boku.Fx;

using Boku.Audio;
using BokuShared;
using Boku.Common.Gesture;

namespace Boku
{
    /// <summary>
    /// Display SaveLevel dialog.
    /// </summary>
    public class SaveLevelDialog : GameObject, INeedsDeviceReset
    {
        protected class Shared : INeedsDeviceReset
        {

            #region Members

            public SaveLevelDialog parent = null;

            public Camera camera = null;
            public Camera camera1k = null;      // Camera for rendering to the 1024x768 rt.

            public TagPicker tagPicker = null;

            public enum InputFocus
            {
                Name,
                Description,
                NumInputFocus,
            }

            public InputFocus focus = InputFocus.Name;

            // Info before user makes any changes.
            public string originalName = null;
            public string originalDescription = null;
            public UIGridElement.Justification originalDescJustification = UIGridElement.Justification.Left;
            public int originalVersion = 0;     // Version before user changes.

            // Post change info.
            public string curName = null;       // Current level name.
            public string curNameScrubbed = null;   // Scrubbed version of the above.
            public string curDesc = null;       // Current level description.
            public UIGridElement.Justification curDescJustification = UIGridElement.Justification.Left;
            public TextBlob descBlob = null;    // The formatted description.
            public int curVersion = 0;          // Current level version.  Version numbers should all be positive.
                                                // 0 indicates no version number.
            public int maxVersion = 999;        

            public string curString = null;     // The current string we are editing.
            public int cursorPosition = 0;      // Current cursor position.
                                                // 0 is before the 1st character, 1 is between 
                                                // the 1st and 2nd characters, etc.
            public string originalString = null;    // Original version of whichever string is currently being edited.

            public int topLine = 0;             // Which line of the description is being shown at the default starting position.
            public int descOffset = 0;          // Vertical offset (in pixels) for beginning of description.
            public int descTop = 170;           // Magic numbers all determined by pushing stuff around in Photoshop to match Brian's design.
            public int descMargin = 185;
            public int descWidth = 840;         // Max width in pixels for a line of the description.
            public int descMaxVisibleLines = 9; // Max number of lines visible in the description.  May change if the font changes.
            public int descIndent = 0;          // Indent for the description.  Will be filled in based on the width of the "description" string.

            public int nameWidth = 520;         // Max width in pixels for the level name.  Include label width.

            public bool editingText = false;    // Are we currently editing a line of text?

            public int curTags = 0;

            // Mouse input hit boxes.  All are in pixel coordinates.
            public AABB2D nameLabelBox = new AABB2D();      // Label for 'name' field.
            public AABB2D descLabelBox = new AABB2D();      // Label for 'description' field.
            public AABB2D nameBox = new AABB2D();           // Actual name text.
            public AABB2D descBox = new AABB2D();           // Actual description text.
            public AABB2D leftStickBox = new AABB2D();      // Left stick for selecting between name and description.
            public AABB2D rightStickBox = new AABB2D();     // Right stick for inc/dec version number.
            public AABB2D textAreaHitBox = new AABB2D();    // Region for description text.  Used for setting cursor on mouse click.
            public Vector2 rt1kRenderPos = Vector2.Zero;    // Position on full screen where 1k rt is rendered.
                                                            // Used to help with mouse hits.
            public AABB2D leftJustifyHitBox = new AABB2D();     // Buttons for justifying description text.
            public AABB2D centerJustifyHitBox = new AABB2D();
            public AABB2D rightJustifyHitBox = new AABB2D();

            // Buttons
            public Button tagsButton = null;
            public Button changeButton = null;
            public Button cancelButton = null;
            public Button saveButton = null;

            public Vector2 rtDisplayPosition;       // Where the rt is displayed on the screen.  Used to adjust mouse hit.
            public float rtScale = 1.0f;            // The scale the rt is displayed at.
        
            // Colors for button labels
            public Color labelColor = Color.White;
            public Color tagsColor = new Color(32, 140, 178);

            public float yOffset = 0.0f;    // Only used when virtual keyboard is onscreen.

            #endregion

            #region Accessors

            /// <summary>
            /// The raw version of the current name.  This is the version that is
            /// edited and written to.
            /// </summary>
            public string CurName
            {
                set
                {
                    curName = TextHelper.FilterInvalidCharacters(value);
                    if (!Censor.Scrub(curName, ref curNameScrubbed))
                    {
                        curNameScrubbed = curName;
                    }
                }
                get { return curName; }
            }

            /// <summary>
            /// The censored version of the name.  This is the version that is displayed and saved.
            /// </summary>
            public string CurNameScrubbed
            {
                get { return curNameScrubbed; }
            }

            #endregion

            #region Public

            // c'tor
            public Shared(SaveLevelDialog parent)
            {
                this.parent = parent;

                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

                // We're rendering the camera specific parts into a 1024x768 rendertarget and
                // then copying (with masking) into the 1280x720 rt and finally cropping it 
                // as needed for 4:3 display.
                camera = new PerspectiveUICamera();
                camera.Resolution = new Point(1280, 720);
                camera.Update();
                camera1k = new PerspectiveUICamera();
                camera1k.Resolution = new Point(1024, 768);
                camera1k.Update();

                // Create tag picker.
                tagPicker = new TagPicker();
                tagPicker.OnExit = parent.OnExit;
                tagPicker.WorldMatrix = Matrix.CreateTranslation(-2.5f, 0.0f, 0.0f);

                // Buttons
                {
                    GetTexture getTexture = delegate() { return ButtonTextures.XButton; };
                    tagsButton = new Button(Strings.Localize("saveLevelDialog.tags"), tagsColor, getTexture, UI2D.Shared.GetGameFont20);
                }
                {
                    GetTexture getTexture = delegate() { return ButtonTextures.AButton; };
                    changeButton = new Button(Strings.Localize("saveLevelDialog.change"), labelColor, getTexture, UI2D.Shared.GetGameFont20);
                }
                {
                    GetTexture getTexture = delegate() { return ButtonTextures.BButton; };
                    cancelButton = new Button(Strings.Localize("saveLevelDialog.cancel"), labelColor, getTexture, UI2D.Shared.GetGameFont20);
                }
                {
                    GetTexture getTexture = delegate() { return ButtonTextures.StartButton; };
                    saveButton = new Button(Strings.Localize("saveLevelDialog.save"), labelColor, getTexture, UI2D.Shared.GetGameFont20);
                }

            }   // end of Shared c'tor

            /// <summary>
            /// Resets the currently edited string to it's original state.
            /// </summary>
            public void ResetString()
            {
                curString = originalString;
                cursorPosition = curString.Length;
            }

            #endregion

            #region Internal

            public void LoadContent(bool immediate)
            {
                BokuGame.Load(tagPicker, immediate);
            }   // end of SaveLevelDialog Shared LoadContent()

            public void InitDeviceResources(GraphicsDevice device)
            {
                tagPicker.InitDeviceResources(device);
            }   // end of InitDeviceResources()

            public void UnloadContent()
            {
                BokuGame.Unload(tagPicker);
            }   // end of SaveLevelDialog Shared UnloadContent()

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
                BokuGame.DeviceReset(tagPicker, device);
            }

            #endregion

        }   // end of class Shared

        protected class UpdateObj : UpdateObject
        {
            #region Members

            private SaveLevelDialog parent = null;
            private Shared shared = null;

            #endregion

            #region Public

            public UpdateObj(SaveLevelDialog parent, Shared shared)
            {
                this.parent = parent;
                this.shared = shared;
            }

            // Used to detect when alt is released.
            bool prevAltPressed = false;

            public override void Update()
            {
                if (AuthUI.IsModalActive)
                {
                    return;
                }

                if (parent.commandMap == CommandStack.Peek())
                {
                    GamePadInput pad = GamePadInput.GetGamePad0();

                    //TouchInput
                    if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                    {
                        bool altPressed = KeyboardInput.AltIsPressed;
                        bool released = prevAltPressed && !altPressed;

                        if (shared.editingText && released && specialChar != null)
                        {
                            char c = (char)int.Parse(specialChar);
                            if (TextHelper.CharIsValid(c))
                            {
                                TextInput(c);
                            }
                            specialChar = null;
                        }
                        prevAltPressed = altPressed;
                        TouchContact touch = TouchInput.GetOldestTouch();

#if NETFX_CORE
                        // Prevent keyboard input from leaking though.
                        if (touch != null && VirtualKeyboard.Active && VirtualKeyboard.HitBox.Contains(touch.position))
                        {
                            touch = null;
                        }
                        else
                        {
                            // Adjust if position has been offset by VirtualKeyboard.
                            if (touch != null)
                            {
                                Vector2 pos = touch.position;
                                pos.Y += shared.yOffset;
                                touch.position = pos;
                            }
                        }
#endif

                        if (touch != null)
                        {
                            //Vector2 hit = TouchInput.GetAspectRatioAdjustedPosition(touch.position, shared.camera, true);
                            Vector2 hit = (touch.position - shared.rtDisplayPosition) / shared.rtScale;

                            // Name label
                            if (shared.nameLabelBox.Touched(touch, hit))
                            {
                                // If we're editing, stop.
                                if (shared.editingText)
                                {
                                    Accept();
                                }

                                shared.focus = Shared.InputFocus.Name;
                            }

                            // Name 
                            if (shared.nameBox.Touched(touch, hit))
                            {
                                // If we were editing something else.
                                if (shared.editingText)
                                {
                                    Accept();
                                }

                                shared.focus = Shared.InputFocus.Name;
                                KeyboardInput.ShowOnScreenKeyboard();
                                ActivateEditing();
                            }

                            // Description label
                            if (shared.descLabelBox.Touched(touch, hit))
                            {
                                // If we're editing, stop.
                                if (shared.editingText)
                                {
                                    Accept();
                                }

                                shared.focus = Shared.InputFocus.Description;
                            }

                            // Description
                            if (shared.descBox.Touched(touch, hit))
                            {
                                // If we were editing something else.
                                if (shared.editingText)
                                {
                                    Accept();
                                }

                                shared.focus = Shared.InputFocus.Description;
                                KeyboardInput.ShowOnScreenKeyboard();
                                ActivateEditing();
                                

                                // TODO Think about refactoring this functionality into the TextBlob.

                                // Position cursor in description text blob based on mouse hit.
                                // If the user clicks into the text editing area, move the cursor to this position.
                                // Calc line we're on.
                                int line = (int)((hit.Y - shared.textAreaHitBox.Min.Y) / shared.descBlob.TotalSpacing);
                                int curLine = 0;
                                int x = 0;
                                shared.descBlob.FindCursorLineAndPosition(out curLine, out x);
                                if (curLine > line)
                                {
                                    for (int i = 0; i < curLine - line; i++)
                                    {
                                        shared.descBlob.CursorUp();
                                    }
                                }
                                else if (curLine < line)
                                {
                                    for (int i = 0; i < line - curLine; i++)
                                    {
                                        shared.descBlob.CursorDown();
                                    }
                                }

                                shared.descBlob.FindCursorLineAndPosition(out curLine, out x);
                                int mouseX = (int)(hit.X - shared.textAreaHitBox.Min.X - shared.rt1kRenderPos.X);
                                mouseX = Math.Max(mouseX, 0);
                                mouseX = Math.Min(mouseX, shared.descBlob.GetLineWidth(curLine));

                                while (x > mouseX)
                                {
                                    shared.descBlob.CursorLeft();

                                    int newX = x;
                                    shared.descBlob.FindCursorLineAndPosition(out curLine, out newX);

                                    //are we going in circles?  happens on center alignment if we can't scroll any further
                                    if (newX == x) break;

                                    x = newX;
                                }

                                while (x < mouseX)
                                {
                                    shared.descBlob.CursorRight();

                                    int newX = x;
                                    shared.descBlob.FindCursorLineAndPosition(out curLine, out newX);
                                    mouseX = Math.Min(mouseX, shared.descBlob.GetLineWidth(curLine));

                                    // Special case.  When we get to the end of the line the cursor may
                                    // wrap to the next line.  In that case, just back it up one space
                                    // and break out of the loop.
                                    if (curLine != line)
                                    {
                                        shared.descBlob.CursorLeft();
                                        break;
                                    }

                                    //are we going in circles?  happens on center alignment if we can't scroll any further
                                    if (newX == x) break;

                                    x = newX;
                                }
                            }

                            // Description Justification
                            if (shared.leftJustifyHitBox.Touched(touch, hit))
                            {
                                shared.descBlob.Justification = UIGridElement.Justification.Left;
                                shared.curDescJustification = UIGridElement.Justification.Left;
                            }
                            if (shared.centerJustifyHitBox.Touched(touch, hit))
                            {
                                shared.descBlob.Justification = UIGridElement.Justification.Center;
                                shared.curDescJustification = UIGridElement.Justification.Center;
                            }
                            if (shared.rightJustifyHitBox.Touched(touch, hit))
                            {
                                shared.descBlob.Justification = UIGridElement.Justification.Right;
                                shared.curDescJustification = UIGridElement.Justification.Right;
                            }

                            // Tags picker
                            if (shared.tagsButton.Box.Touched(touch, hit))
                            {
                                // If we're editing, stop.
                                if (shared.editingText)
                                {
                                    Accept();
                                }

                                shared.tagPicker.SetTags(shared.curTags);
                                shared.tagPicker.Active = true;
                            }

                            // Change
                            if (shared.changeButton.Box.Touched(touch, hit))
                            {
                                if (!shared.editingText)
                                {
                                    // Not already editing so go into editing.
                                    ActivateEditing();
                                }
                            }

                            // Cancel
                            if (shared.cancelButton.Box.Touched(touch, hit))
                            {
                                if (shared.editingText)
                                {
                                    // Back out of text editing.
                                    Cancel();
                                }
                                else
                                {
                                    // Back out of SaveLevelDialog
                                    parent.Deactivate();

                                    parent.button = SaveLevelDialogButtons.Cancel;
                                    parent.OnButtonPressed(parent);
                                }
                            }

                            // Save
                            if (shared.saveButton.Box.Touched(touch, hit))
                            {
                                // If we're editing, stop.
                                if (shared.editingText)
                                {
                                    Accept();
                                }

                                bool newName = NewName();
                                bool needsCheck = CheckPreserveLinks();

                                if (!newName)
                                {
                                    parent.overwriteWarning.Activate();
                                }
                                else if (needsCheck)
                                {
                                    //level has links and saved with new name - ask if we should preserve links
                                    parent.ShowPreserveLinksDialog();
                                }
                                else
                                {
                                    SaveLevelAndExit(newName, true);
                                }

                            }

                            // LeftStick
                            if (shared.leftStickBox.Touched(touch, hit))
                            {
                                if (shared.editingText)
                                {
                                    // Finish text editing.
                                    Accept();
                                }
                                float midY = (shared.leftStickBox.Max.Y + shared.leftStickBox.Min.Y) / 2.0f;
                                if (hit.Y < midY)
                                {
                                    shared.focus = Shared.InputFocus.Name;
                                }
                                else
                                {
                                    shared.focus = Shared.InputFocus.Description;
                                }
                            }

                            // RightStick
                            if (shared.rightStickBox.Touched(touch, hit))
                            {
                                if (shared.editingText)
                                {
                                    // Finish text editing.
                                    Accept();
                                }
                                float midY = (shared.rightStickBox.Max.Y + shared.rightStickBox.Min.Y) / 2.0f;
                                if (hit.Y < midY)
                                {
                                    if (shared.curVersion < shared.maxVersion)
                                    {
                                        ++shared.curVersion;
                                    }
                                }
                                else
                                {
                                    if (shared.curVersion > 0)
                                    {
                                        --shared.curVersion;
                                    }
                                }
                            }


                            // The scroll wheel only affects the cursor if editing the description.
                            if (shared.editingText && shared.focus == Shared.InputFocus.Description)
                            {
                                SwipeGestureRecognizer swipeGesture = TouchGestureManager.Get().SwipeGesture;
                                if (swipeGesture.WasSwiped())
                                {
                                    if (swipeGesture.SwipeDirection == Directions.North)
                                    {
                                        ScrollDown();
                                        ScrollDown();
                                        ScrollDown();
                                    }
                                    else if (swipeGesture.SwipeDirection == Directions.South)
                                    {
                                        ScrollUp();
                                        ScrollUp();
                                        ScrollUp();
                                    }
                                }
                            }


                            // Change button label colors based on mouse hover.
                            shared.tagsButton.SetHoverState(hit);
                            shared.changeButton.SetHoverState(hit);
                            shared.cancelButton.SetHoverState(hit);
                            shared.saveButton.SetHoverState(hit);
                        }

                    } 

                    // END of TouchInput

                    // MouseInput
                    if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                    {
                        bool altPressed = KeyboardInput.AltIsPressed;
                        bool released = prevAltPressed && !altPressed;

                        if (shared.editingText && released && specialChar != null)
                        {
                            char c = (char)int.Parse(specialChar);
                            if (TextHelper.CharIsValid(c))
                            {
                                TextInput(c);
                            }
                            specialChar = null;
                        }
                        prevAltPressed = altPressed;

                        Vector2 hit = (MouseInput.PositionVec - shared.rtDisplayPosition) / shared.rtScale;

                        // Name label
                        if (shared.nameLabelBox.LeftPressed(hit))
                        {
                            // If we're editing, stop.
                            if (shared.editingText)
                            {
                                Accept();
                            }

                            shared.focus = Shared.InputFocus.Name;
                        }

                        // Name 
                        if (shared.nameBox.LeftPressed(hit))
                        {
                            // If we were editing something else.
                            if (shared.editingText)
                            {
                                Accept();
                            }

                            shared.focus = Shared.InputFocus.Name;

                            ActivateEditing();
                        }

                        // Description label
                        if (shared.descLabelBox.LeftPressed(hit))
                        {
                            // If we're editing, stop.
                            if (shared.editingText)
                            {
                                Accept();
                            }

                            shared.focus = Shared.InputFocus.Description;
                        }

                        // Description
                        if (shared.descBox.LeftPressed(hit))
                        {
                            // If we were editing something else.
                            if (shared.editingText && shared.focus != Shared.InputFocus.Description)
                            {
                                Accept();
                            }

                            if (shared.focus != Shared.InputFocus.Description)
                            {
                                shared.focus = Shared.InputFocus.Description;

                                ActivateEditing();
                            }

                            // TODO Think about refactoring this functionality into the TextBlob.

                            // Position cursor in description text blob based on mouse hit.
                            // If the user clicks into the text editing area, move the cursor to this position.
                            // Calc line we're on.
                            int line = (int)((hit.Y - shared.textAreaHitBox.Min.Y) / shared.descBlob.TotalSpacing);
                            int curLine = 0;
                            int x = 0;
                            shared.descBlob.FindCursorLineAndPosition(out curLine, out x);
                            if (curLine > line)
                            {
                                for (int i = 0; i < curLine - line; i++)
                                {
                                    shared.descBlob.CursorUp();
                                }
                            }
                            else if (curLine < line)
                            {
                                for (int i = 0; i < line - curLine; i++)
                                {
                                    shared.descBlob.CursorDown();
                                }
                            }

                            shared.descBlob.FindCursorLineAndPosition(out curLine, out x);
                            int mouseX = (int)(hit.X - shared.textAreaHitBox.Min.X - shared.rt1kRenderPos.X);
                            mouseX = Math.Max(mouseX, 0);
                            mouseX = Math.Min(mouseX, shared.descBlob.GetLineWidth(curLine));

                            while (x > mouseX)
                            {
                                shared.descBlob.CursorLeft();
                                int newX = x;
                                shared.descBlob.FindCursorLineAndPosition(out curLine, out newX);

                                //are we going in circles?  happens on center alignment if we can't scroll any further
                                if (newX == x) break;

                                x = newX;
                            }

                            while (x < mouseX)
                            {
                                shared.descBlob.CursorRight();
                                int newX = x;
                                shared.descBlob.FindCursorLineAndPosition(out curLine, out newX);
                                mouseX = Math.Min(mouseX, shared.descBlob.GetLineWidth(curLine));

                                // Special case.  When we get to the end of the line the cursor may
                                // wrap to the next line.  In that case, just back it up one space
                                // and break out of the loop.
                                if (curLine != line)
                                {
                                    shared.descBlob.CursorLeft();
                                    break;
                                }

                                //are we going in circles?  happens on center alignment if we can't scroll any further
                                if (newX == x) break;

                                x = newX;
                            }
                        }

                        // Description Justification
                        if (shared.leftJustifyHitBox.LeftPressed(hit))
                        {
                            shared.descBlob.Justification = UIGridElement.Justification.Left;
                            shared.curDescJustification = UIGridElement.Justification.Left;
                        }
                        if (shared.centerJustifyHitBox.LeftPressed(hit))
                        {
                            shared.descBlob.Justification = UIGridElement.Justification.Center;
                            shared.curDescJustification = UIGridElement.Justification.Center;
                        }
                        if (shared.rightJustifyHitBox.LeftPressed(hit))
                        {
                            shared.descBlob.Justification = UIGridElement.Justification.Right;
                            shared.curDescJustification = UIGridElement.Justification.Right;
                        }

                        // Tags picker
                        if (shared.tagsButton.Box.LeftPressed(hit))
                        {
                            // If we're editing, stop.
                            if (shared.editingText)
                            {
                                Accept();
                            }

                            shared.tagPicker.SetTags(shared.curTags);
                            shared.tagPicker.Active = true;
                        }

                        // Change
                        if (shared.changeButton.Box.LeftPressed(hit))
                        {
                            if (!shared.editingText)
                            {
                                // Not already editing so go into editing.
                                ActivateEditing();
                            }
                        }

                        // Cancel
                        if (shared.cancelButton.Box.LeftPressed(hit))
                        {
                            if (shared.editingText)
                            {
                                // Back out of text editing.
                                Cancel();
                            }
                            else
                            {
                                // Back out of SaveLevelDialog
                                parent.Deactivate();

                                parent.button = SaveLevelDialogButtons.Cancel;
                                parent.OnButtonPressed(parent);
                            }
                        }

                        // Save
                        if (shared.saveButton.Box.LeftPressed(hit))
                        {
                            // If we're editing, stop.
                            if (shared.editingText)
                            {
                                Accept();
                            }

                            bool newName = NewName();
                            bool needsCheck = CheckPreserveLinks();

                            if (!newName)
                            {
                                parent.overwriteWarning.Activate();
                            }
                            else if (needsCheck)
                            {
                                //level has links and saved with new name - ask if we should preserve links
                                parent.ShowPreserveLinksDialog();
                            }
                            else
                            {
                                SaveLevelAndExit(newName, true);
                            }
                        }

                        // LeftStick
                        if (shared.leftStickBox.LeftPressed(hit))
                        {
                            if (shared.editingText)
                            {
                                // Finish text editing.
                                Accept();
                            }
                            float midY = (shared.leftStickBox.Max.Y + shared.leftStickBox.Min.Y) / 2.0f;
                            if (hit.Y < midY)
                            {
                                shared.focus = Shared.InputFocus.Name;
                            }
                            else
                            {
                                shared.focus = Shared.InputFocus.Description;
                            }
                        }

                        // RightStick
                        if (shared.rightStickBox.LeftPressed(hit))
                        {
                            if (shared.editingText)
                            {
                                // Finish text editing.
                                Accept();
                            }
                            float midY = (shared.rightStickBox.Max.Y + shared.rightStickBox.Min.Y) / 2.0f;
                            if (hit.Y < midY)
                            {
                                if (shared.curVersion < shared.maxVersion)
                                {
                                    ++shared.curVersion;
                                }
                            }
                            else
                            {
                                if (shared.curVersion > 0)
                                {
                                    --shared.curVersion;
                                }
                            }
                        }


                        // The scroll wheel only affects the cursor if editing the description.
                        if (shared.editingText && shared.focus == Shared.InputFocus.Description)
                        {
                            int scroll = MouseInput.ScrollWheel - MouseInput.PrevScrollWheel;
                            if (scroll > 0)
                            {
                                ScrollDown();
                            }
                            else if (scroll < 0)
                            {
                                ScrollUp();
                            }
                        }


                        // Change button label colors based on mouse hover.
                        shared.tagsButton.SetHoverState(hit);
                        shared.changeButton.SetHoverState(hit);
                        shared.cancelButton.SetHoverState(hit);
                        shared.saveButton.SetHoverState(hit);

                    }   // end of mouse input

                    if (pad.ButtonB.WasPressed || (!shared.editingText && KeyboardInput.WasPressed(Keys.Escape)) || (!shared.editingText && KeyboardInput.WasPressed(Keys.B)))
                    {
                        KeyboardInput.ClearAllWasPressedState(Keys.Escape);
                        KeyboardInput.ClearAllWasPressedState(Keys.B);

                        // Cancel

                        if (shared.editingText)
                        {
                            // Back out of text editing.
                            Cancel();
                        }
                        else
                        {
                            // Back out of SaveLevelDialog
                            parent.Deactivate();

                            parent.button = SaveLevelDialogButtons.Cancel;
                            parent.OnButtonPressed(parent);
                        }
                        pad.ButtonB.ClearAllWasPressedState();
                        GamePadInput.IgnoreUntilReleased(Buttons.B);
                    }

                    // Quick save.
                    if (pad.Start.WasPressed || (!shared.editingText && KeyboardInput.WasPressed(Keys.Enter)))
                    {
                        if (shared.editingText)
                        {
                            // If already editing text, finish up.
                            Accept();
                        }

                        bool newName = NewName();
                        bool needsCheck = CheckPreserveLinks();

                        if (!newName)
                        {
                            parent.overwriteWarning.Activate();
                        }
                        else if (needsCheck)
                        {
                            //level has links and saved with new name - ask if we should preserve links
                            parent.ShowPreserveLinksDialog();
                        }
                        else
                        {
                            SaveLevelAndExit(newName, true);
                        }
                    }

                    if (pad.ButtonA.WasPressed || (!shared.editingText && KeyboardInput.WasPressed(Keys.A)))
                    {
                        if (!shared.editingText)
                        {
                            // Not already editing so go into editing.
                            ActivateEditing();
                        }
                        pad.ButtonA.ClearAllWasPressedState();
                        GamePadInput.IgnoreUntilReleased(Buttons.A);
                    }

                    // Activate tag picker.
                    if (pad.ButtonX.WasPressed || (!shared.editingText && KeyboardInput.WasPressed(Keys.X)))
                    {
                        shared.tagPicker.SetTags(shared.curTags);
                        shared.tagPicker.Active = true;

                        KeyboardInput.ClearAllWasPressedState(Keys.X);
                    }

                    // LeftStick -- change focus index
                    if (pad.LeftStickUp.WasPressed || pad.LeftStickUp.WasRepeatPressed)
                    {
                        if (shared.editingText && shared.focus == Shared.InputFocus.Description)
                        {
                            ScrollDown();
                        }
                        else
                        {
                            if (shared.editingText && shared.focus == Shared.InputFocus.Name)
                            {
                                Accept();
                            }
                            shared.focus = (Shared.InputFocus)(((int)shared.focus + (int)Shared.InputFocus.NumInputFocus - 1) % (int)Shared.InputFocus.NumInputFocus);
                        }
                    }

                    if (pad.LeftStickDown.WasPressed || pad.LeftStickDown.WasRepeatPressed)
                    {
                        if (shared.editingText && shared.focus == Shared.InputFocus.Description)
                        {
                            ScrollUp();
                        }
                        else
                        {
                            if (shared.editingText && shared.focus == Shared.InputFocus.Name)
                            {
                                Accept();
                            }
                            shared.focus = (Shared.InputFocus)(((int)shared.focus + 1) % (int)Shared.InputFocus.NumInputFocus);
                        }
                    }

                    // RightStick -- change version number
                    if (pad.RightStickUp.WasPressed || pad.RightStickUp.WasRepeatPressed || (!shared.editingText && KeyboardInput.WasPressedOrRepeat(Keys.Up)))
                    {
                        if (shared.curVersion < shared.maxVersion)
                        {
                            ++shared.curVersion;
                        }
                    }

                    if (pad.RightStickDown.WasPressed || pad.RightStickDown.WasRepeatPressed || (!shared.editingText && KeyboardInput.WasPressedOrRepeat(Keys.Down)))
                    {
                        if (shared.curVersion > 0)
                        {
                            --shared.curVersion;
                        }
                    }

                    // Check if we've moved the cursor offscreen.
                    // If so, scroll to put the cursor back onto the screen.
                    {
                        bool needToScroll = false;
                        int line = 0;
                        int curPos = 0;
                        shared.descBlob.FindCursorLineAndPosition(out line, out curPos);
                        if (line < shared.topLine)
                        {
                            --shared.topLine;
                            needToScroll = true;
                        }
                        else if (line >= shared.topLine + shared.descMaxVisibleLines)
                        {
                            ++shared.topLine;
                            needToScroll = true;
                        }

                        if (needToScroll)
                        {
                            TwitchTextOffset();
                        }
                    }
                }

                // If we're not shutting down, update the child grids.
                if (parent.Active)
                {
                    Matrix world = Matrix.Identity;

                    if (shared.tagPicker != null)
                    {
                        shared.tagPicker.Update(shared.camera, ref world);
                        if (KeyboardInput.WasPressed(Keys.Tab))
                        {
                            shared.focus = (Shared.InputFocus)(((int)shared.focus + 1) % (int)Shared.InputFocus.NumInputFocus);
                        }
                    }

                }   // end if not shutting down.

            }   // end of Update()

            public void ScrollDown()
            {
                if (shared.topLine > 0)
                {
                    --shared.topLine;
                    TwitchTextOffset();

                    // This scroll may have moved the cursor off screen, if so
                    // move the cursor so that it's back on screen.
                    int line = 0;
                    int curPos = 0;
                    shared.descBlob.FindCursorLineAndPosition(out line, out curPos);
                    if (line >= shared.topLine + shared.descMaxVisibleLines)
                    {
                        shared.descBlob.CursorUp();
                    }
                }
            }   // end of ScrollDown()

            public void ScrollUp()
            {
                int numLines = shared.descBlob.NumLines;
                if (numLines - shared.descMaxVisibleLines > shared.topLine)
                {
                    ++shared.topLine;
                    TwitchTextOffset();

                    // This scroll may have moved the cursor off screen, if so
                    // move the cursor so that it's back on screen.
                    int line = 0;
                    int curPos = 0;
                    shared.descBlob.FindCursorLineAndPosition(out line, out curPos);
                    if (line < shared.topLine)
                    {
                        shared.descBlob.CursorDown();
                    }
                }
            }   // end of ScrollUp()

            public void TwitchTextOffset()
            {
                // Start a twitch to move the text text offset.
                TwitchManager.Set<float> set = delegate(float val, Object param) { shared.descOffset = (int)val; };
                TwitchManager.CreateTwitch<float>(shared.descOffset, -shared.topLine * parent.renderObj.Font().LineSpacing, set, 0.2f, TwitchCurve.Shape.OvershootOut);
            }   // end of TwitchTextOffset()

            /// <summary>
            /// Determines if the user is saving the file with a new name and/or version number.
            /// </summary>
            /// <returns></returns>
            public bool NewName()
            {
                bool newName = shared.originalName != shared.CurNameScrubbed || shared.originalVersion != shared.curVersion;

                if ((InGame.XmlWorldData.genres & (int)Genres.MyWorlds) == 0)
                {
                    newName = true;
                }

                return newName;
            }   // end of NewName()


            /// <summary>
            /// Determines if the user needs to be asked if level links should be preserved during the save
            /// </summary>
            /// <returns></returns>
            public bool CheckPreserveLinks()
            {
                bool needsCheck = InGame.XmlWorldData.LinkedFromLevel!=null || InGame.XmlWorldData.LinkedToLevel!=null;

                //check if the level has been saved before (or has been saved but not as a local world)
                if (!XmlDataHelper.CheckWorldExistsByGenre(InGame.XmlWorldData.id, Genres.MyWorlds))
                {
                    //hasn't been saved or is non-local - either way, we'll be making a new copy and links will be preserved 
                    //automatically - don't have to ask the user
                    needsCheck = false;
                }

                return needsCheck;
            }   // end of CheckPreserveLinks()



            /// <summary>
            /// Saves the level and exits the SaveLevel dialog.  Assumes that
            /// any overwrite warnings have already been displayed to the user.
            /// </summary>
            /// <param name="newName"></param>
            public void SaveLevelAndExit(bool newName, bool preserveLinks)
            {
                InGame.XmlWorldData.name = shared.CurNameScrubbed;
                InGame.XmlWorldData.creator = Auth.CreatorName;
                if (shared.curVersion != 0)
                {
                    InGame.XmlWorldData.name += @" v" + shared.curVersion.ToString("D2");
                }
                InGame.XmlWorldData.description = shared.curDesc;
                InGame.XmlWorldData.descJustification = shared.curDescJustification;
                InGame.XmlWorldData.genres = shared.curTags;

                InGame.inGame.SaveLevel(newName, preserveLinks);

                parent.Deactivate();

                Instrumentation.RecordEvent(Instrumentation.EventId.LevelSaved, InGame.XmlWorldData.id.ToString());

                parent.button = SaveLevelDialogButtons.Save;
                parent.OnButtonPressed(parent);

                Foley.PlayHeal(null);

            }   // end of SaveLevelAndExit()

            /// <summary>
            /// Activates text editing for Name and Description fields or menu grid for Tags field.
            /// </summary>
            private void ActivateEditing()
            {
                switch (shared.focus)
                {
                    case Shared.InputFocus.Name:
                        shared.originalString = shared.CurName;
                        shared.ResetString();
                        shared.editingText = true;
                        break;

                    case Shared.InputFocus.Description:
                        shared.originalString = shared.curDesc;
                        shared.descBlob.RawText = shared.curDesc;
                        shared.descBlob.Justification = shared.curDescJustification;
                        shared.descBlob.End();
                        shared.editingText = true;
                        break;
                }
            }

            // Used to accumulate values when user is inputting special characters using the Alt key.
            string specialChar = null;

            public void TextInput(char c)
            {
                // If tagPicker is active, ignore input.
                if (shared.tagPicker.Active)
                    return;

                // We use tabs to toggle between editing the world name and description.
                // So, don't add them to the string here.
                if (c == '\t')
                    return;

                // Handle special character input.
                if (KeyboardInput.AltWasPressed)
                {
                    specialChar = null;
                }
                if (KeyboardInput.AltIsPressed)
                {
                    // accumulate keystrokes
                    specialChar += c;
                    return;
                }

                if (shared.editingText)
                {
                    string str = new string(c, 1);
                    str = TextHelper.FilterInvalidCharacters(str);

                    UI2D.Shared.GetFont Font = parent.renderObj.Font;

                    if (!string.IsNullOrEmpty(str))
                    {
                        // Check if we've gotten too long.
                        if (shared.focus == Shared.InputFocus.Name)
                        {
                            shared.curString = shared.curString.Insert(shared.cursorPosition, str);
                            shared.cursorPosition++;

                            int labelWidth = (int)Font().MeasureString(Strings.Localize("saveLevelDialog.name")).X;
                            int width = (int)Font().MeasureString(shared.curString).X;
                            if (width + labelWidth > shared.nameWidth)
                            {
                                // Bzzzt!
                                Foley.PlayNoBudget();
                                shared.curString = shared.curString.Remove(shared.cursorPosition - 1, 1);
                                shared.cursorPosition--;
                            }
                            else
                            {
                                Foley.PlayClickDown();
                            }
                            UpdateEditedString();
                        }
                        else if (shared.focus == Shared.InputFocus.Description)
                        {
#if !NETFX_CORE
                            // Copy?  Just copy the whole description to the clipboard since we don't
                            // support any kind of selection.
                            if (c == 3)
                            {
                                System.Windows.Forms.Clipboard.SetText(shared.descBlob.ScrubbedText);
                            }

                            // Paste?
                            if (c == 22)
                            {
                                if (System.Windows.Forms.Clipboard.ContainsText())
                                {
                                    str = System.Windows.Forms.Clipboard.GetText();
                                }
                            }
#endif

                            // With the description it's a bit different since this gets 
                            // broken up into multiple lines.  What we do here is break 
                            // the lines up and then scroll if we need to in order to
                            // keep the current line within the window.
                            shared.descBlob.InsertString(str);
                            if (shared.descBlob.NumLines > shared.descMaxVisibleLines)
                            {
                                // Bzzzt!
                                Foley.PlayNoBudget();
                                //shared.curString = shared.curString.Remove(shared.cursorPosition - 1, 1);
                                //shared.cursorPosition--;
                            }
                            else
                            {
                                Foley.PlayClickDown();
                            }
                        }
                    }
                }
            }   // end of UpdateObj TextInput()

            // TODO (****) Clean this up.  We're mixing text as input with text as control here.
            // Probably the right thing to do would be to push/pop the text input callbacks
            // dynamically to mirror the state we're in.
            
            public void KeyInput(Keys key)
            {
                // If tagPicker is active, ignore input.
                if (shared.tagPicker.Active)
                    return;

                // Grab the tab and use it for cycling through the focus options.
                if (key == Keys.Tab)
                {
                    // If editing, finish up.
                    if (shared.editingText)
                    {
                        Accept();
                    }

                    // Close the tag picker if we need to.
                    shared.tagPicker.Active = false;

                    // Move the focus.
                    shared.focus = (Shared.InputFocus)(((int)shared.focus + 1) % (int)Shared.InputFocus.NumInputFocus);

                    ActivateEditing();

                    KeyboardInput.ClearAllWasPressedState(Keys.Tab);

                    return;
                }

                if (shared.editingText)
                {

                    if (shared.focus == Shared.InputFocus.Name)
                    {
                        //
                        // Editing the level name.
                        //
                        bool changed = false;

                        KeyboardInput.ClearAllWasPressedState(key);

                        switch (key)
                        {
                            case Keys.Enter:
                                Foley.PlayClickDown();
                                Accept();
                                break;

                            case Keys.Escape:
                                Foley.PlayClickDown();
                                Cancel();
                                break;

                            case Keys.Left:
                                if (shared.cursorPosition > 0)
                                {
                                    shared.cursorPosition--;
                                    Foley.PlayClickDown();
                                }
                                break;
                            case Keys.Right:
                                if (shared.cursorPosition < shared.curString.Length)
                                {
                                    shared.cursorPosition++;
                                    Foley.PlayClickDown();
                                }
                                break;

                            case Keys.Home:
                                Foley.PlayClickDown();
                                shared.cursorPosition = 0;
                                break;

                            case Keys.End:
                                Foley.PlayClickDown();
                                shared.cursorPosition = shared.curString.Length;
                                break;

                            case Keys.Back:
                                if (shared.curString.Length > 0 && shared.cursorPosition > 0)
                                {
                                    shared.curString = shared.curString.Substring(0, shared.cursorPosition - 1) + shared.curString.Substring(shared.cursorPosition);
                                    shared.cursorPosition--;
                                    changed = true;
                                    Foley.PlayClickDown();
                                }
                                break;

                            case Keys.Delete:
                                if (shared.curString.Length > 0 && shared.cursorPosition < shared.curString.Length)
                                {
                                    shared.curString = shared.curString.Substring(0, shared.cursorPosition) + shared.curString.Substring(shared.cursorPosition + 1);
                                    changed = true;
                                    Foley.PlayClickDown();
                                }
                                break;

                        }   // end of switch on special characters.

                        if (changed)
                        {
                            UpdateEditedString();
                        }
                    }
                    else if (shared.focus == Shared.InputFocus.Description)
                    {
                        //
                        // Editing the description.
                        //

                        KeyboardInput.ClearAllWasPressedState(key);

                        switch (key)
                        {
                            case Keys.Enter:
                                Foley.PlayClickDown();
                                shared.descBlob.Enter();
                                break;

                            case Keys.Escape:
                                Foley.PlayClickDown();
                                Cancel();
                                break;

                            case Keys.Left:
                                Foley.PlayClickDown();
                                shared.descBlob.CursorLeft();
                                break;

                            case Keys.Right:
                                Foley.PlayClickDown();
                                shared.descBlob.CursorRight();
                                break;

                            case Keys.Up:
                                Foley.PlayClickDown();
                                shared.descBlob.CursorUp();
                                break;

                            case Keys.Down:
                                Foley.PlayClickDown();
                                shared.descBlob.CursorDown();
                                break;

                            case Keys.Home:
                                Foley.PlayClickDown();
                                shared.descBlob.Home();
                                break;

                            case Keys.End:
                                Foley.PlayClickDown();
                                shared.descBlob.End();
                                break;

                            case Keys.Back:
                                Foley.PlayClickDown();
                                shared.descBlob.Backspace();
                                break;

                            case Keys.Delete:
                                Foley.PlayClickDown();
                                shared.descBlob.Delete();
                                break;

                        }   // end of switch on special characters.
                    }
                }
            }   // end of UpdateObj KeyInput()

            private void UpdateEditedString()
            {
                UI2D.Shared.GetFont Font = parent.renderObj.Font;

                // Update the string so it gets rendered.
                switch (shared.focus)
                {
                    case Shared.InputFocus.Name:
                        shared.CurName = shared.curString;
                        break;

                    case Shared.InputFocus.Description:
                        shared.curDesc = shared.curString;
                        shared.descBlob.RawText = Strings.Localize("saveLevelDialog.description") + shared.curDesc;
                        shared.descBlob.Justification = shared.curDescJustification;
                        break;
                }
            }

            /// <summary>
            /// User has accepted the currently edited string.  We don't need to 
            /// copy to the current position since it should already be there.  
            /// So, just turn off editing.
            /// </summary>
            public void Accept()
            {
                shared.editingText = false;

                // If the user has created a new name, set the version to 0.
                if (shared.focus == Shared.InputFocus.Name && shared.CurNameScrubbed != shared.originalString)
                {
                    // Scrub user text for URLs or email addresses.
                    shared.CurName = TextHelper.FilterURLs(shared.CurNameScrubbed);
                    shared.CurName = TextHelper.FilterEmail(shared.CurNameScrubbed);
                    
                    shared.curVersion = 0;
                }

                if (shared.focus == Shared.InputFocus.Description)
                {
                    shared.curDesc = shared.descBlob.ScrubbedText;
                    shared.curDescJustification = shared.descBlob.Justification;

                    // Scrub user text for URLs or email addresses.
                    shared.curDesc = TextHelper.FilterURLs(shared.curDesc);
                    shared.curDesc = TextHelper.FilterEmail(shared.curDesc);
                    shared.descBlob.RawText = shared.curDesc;
                }



            }   // end of Accept()

            public void Cancel()
            {
                // The user has hit <esc>, restore the string and quit editing.
                shared.editingText = false;
                switch (shared.focus)
                {
                    case Shared.InputFocus.Name:
                        shared.CurName = shared.originalString;
                        break;
                    case Shared.InputFocus.Description:
                        shared.curDesc = shared.originalString;
                        shared.curDescJustification = shared.originalDescJustification;
                        break;
                }
            }   // end of Cancel()

            public void Discard()
            {
                shared.editingText = false;
                switch (shared.focus)
                {
                    case Shared.InputFocus.Name:
                        shared.CurName = shared.originalString;
                        break;
                    case Shared.InputFocus.Description:
                        shared.curDesc = shared.originalString;
                        shared.curDescJustification = shared.originalDescJustification;
                        break;
                }
            }   // end of Discard()

            #endregion

            #region Internal

            public override void Activate()
            {
                KeyboardInput.OnKey = KeyInput;
#if NETFX_CORE
                Debug.Assert(false, "Does this work?  Why did we prefer winKeyboard?");
                KeyboardInput.OnChar = TextInput;
#else
                BokuGame.bokuGame.winKeyboard.CharacterEntered = TextInput;
#endif
            }

            public override void Deactivate()
            {
                KeyboardInput.OnKey = null;
                KeyboardInput.OnChar = null;
#if !NETFX_CORE
                BokuGame.bokuGame.winKeyboard.CharacterEntered = null;
#endif
            }

            #endregion

        }   // end of class SaveLevelDialog UpdateObj  

        protected class RenderObj : RenderObject, INeedsDeviceReset
        {
            #region Members

            private Shared shared;

            public Texture2D backgroundTexture = null;      // The background frame we render over.  Includes the stick images.
            public Texture2D leftStick = null;
            public Texture2D rightStick = null;
            public Texture2D dropShadow = null;             // For when the tags menu is active.
            public Texture2D leftJustifyTexture = null;
            public Texture2D centerJustifyTexture = null;
            public Texture2D rightJustifyTexture = null;

            public bool menuActive = false;                 // Used to trigger changes in the shadow under tags menu.
            public float dropShadowAlpha = 0.0f;            // Shadow opacity.

            public UI2D.Shared.GetFont Font = UI2D.Shared.GetGameFont20;


            #endregion

            #region Public

            public RenderObj(Shared shared)
            {
                this.shared = shared;
            }

            public override void Render(Camera camera)
            {
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

                RenderTarget2D rtFull = UI2D.Shared.RenderTargetDepthStencil1280_720;   // Rendertarget we render whole display into.
                RenderTarget2D rt1k = UI2D.Shared.RenderTargetDepthStencil1024_768;

                Vector2 rtSize = new Vector2(rtFull.Width, rtFull.Height);

                CameraSpaceQuad csquad = CameraSpaceQuad.GetInstance();
                ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();

                Color selectedHeaderColor = new Color(220, 220, 0);
                Color unselectedHeaderColor = new Color(20, 20, 20);
                Color textBodyColor = new Color(127, 127, 127);
                Color lightTextBodyColor = new Color(191, 191, 191);

                // First we render the text to the 1k x 1k rt using the mask.
                InGame.SetRenderTarget(rt1k);
                InGame.Clear(Color.Transparent);

                // Set up params for rendering UI with this camera.
                Fx.ShaderGlobals.SetCamera(shared.camera1k);

                SpriteBatch batch = UI2D.Shared.SpriteBatch;

                //
                // Text.
                //

                bool renderCursor = shared.editingText && shared.focus == Shared.InputFocus.Description;
                Vector2 pos;
                shared.descMargin = 90;
                shared.descTop = 140;
                pos = new Vector2(shared.descMargin + shared.descIndent, shared.descTop + shared.descOffset);
                shared.descBlob.RenderWithButtons(pos, textBodyColor, renderCursor: renderCursor);

                // The +40 just allows the user to press to the right of the description text to get the cursor to the end of the line.
                shared.textAreaHitBox.Set(pos, pos + new Vector2(shared.descBlob.Width + 40, shared.descMaxVisibleLines * shared.descBlob.TotalSpacing));

                //
                // Render the scene to our rendertarget.
                //
                InGame.SetRenderTarget(rtFull);

                // Set up params for rendering UI with this camera.
                Fx.ShaderGlobals.SetCamera(shared.camera);

                InGame.Clear(Color.Transparent);

                // Now render the background frames.
                Vector2 size = new Vector2(backgroundTexture.Width, backgroundTexture.Height);
                pos = (rtSize - size) / 2.0f;

                ssquad.Render(backgroundTexture, pos, size, "TexturedRegularAlpha");

                // Now render the contents of the rt1k texture but with the edges blended using the mask.
                pos.X = (rtFull.Width - rt1k.Width) / 2.0f;
                pos.Y = 0.0f;
                size = new Vector2(rt1k.Width, rt1k.Height);

                // Magic numbers for description text editor.
                Vector4 limits = new Vector4(0.18f, 0.19f, 0.56f, 0.58f);
                ssquad.RenderWithYLimits(rt1k, limits, pos, new Vector2(rt1k.Width, rt1k.Height), @"TexturedRegularAlpha");

                shared.rt1kRenderPos = pos;

                // Impossible to calc so these numbers were derived experimentally.
                shared.descBox.Set(new Vector2(360, 140), new Vector2(1060, 450));



                batch.Begin();

                // Description.
                int leftEdge = shared.descMargin + (1280 - 1024) / 2;
                pos = new Vector2(leftEdge, shared.descTop);
                TextHelper.DrawString(Font, Strings.Localize("saveLevelDialog.description"), pos, shared.focus == Shared.InputFocus.Description ? selectedHeaderColor : textBodyColor);
                shared.descLabelBox.Set(pos, pos + new Vector2(Font().MeasureString(Strings.Localize("saveLevelDialog.description")).X, Font().LineSpacing));

                string str = null;

                // Level name.
                pos = new Vector2(leftEdge, 69);
                TextHelper.DrawString(Font, Strings.Localize("saveLevelDialog.name"), pos, shared.focus == Shared.InputFocus.Name ? selectedHeaderColor : lightTextBodyColor);
                int labelWidth = (int)Font().MeasureString(Strings.Localize("saveLevelDialog.name")).X;
                shared.nameLabelBox.Set(pos, pos + new Vector2(labelWidth, Font().LineSpacing));
                pos.X += labelWidth;
                str = TextHelper.AddEllipsis(Font, shared.CurNameScrubbed, 550);
                TextHelper.DrawString(Font, str, pos, lightTextBodyColor);
                shared.nameBox.Set(pos, pos + new Vector2(550, Font().LineSpacing));

                // TODO (****) We should be using TextBlob for all text editing!

                // If we're currently editing the name, render the cursor at the right position.
                if (shared.editingText && shared.focus == Shared.InputFocus.Name)
                {
                    string tmpText = shared.curNameScrubbed.Substring(0, shared.cursorPosition);
                    int stringWidth = (int)(Font().MeasureString(tmpText).X);

                    float cursorHeight = Font().LineSpacing + 4.0f;
                    Vector2 cursorTop = new Vector2(pos.X + stringWidth, pos.Y);
                    Vector2 cursorBottom = cursorTop;
                    cursorBottom.Y += cursorHeight;

                    // Render the user text with cursor.
                    Utils.Draw2DLine(cursorTop, cursorBottom, textBodyColor.ToVector4());
                }

                // Version
                str = Strings.Localize("saveLevelDialog.version") + shared.curVersion.ToString("D2");
                int width = (int)Font().MeasureString(str).X;
                int left = 975 - width / 2;
                TextHelper.DrawString(Font, Strings.Localize("saveLevelDialog.version"), new Vector2(left, 69), selectedHeaderColor);
                width = (int)Font().MeasureString(Strings.Localize("saveLevelDialog.version")).X;
                TextHelper.DrawString(Font, shared.curVersion.ToString("D2"), new Vector2(left + width, 69), lightTextBodyColor);

                // RightStick for version changing.
                pos = new Vector2(1058, 57);
                size = new Vector2(42, 63);
                if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse ||
                    GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                {
                    ssquad.Render(UI2D.Shared.UpDownArrowsTexture, pos, size, "TexturedRegularAlpha");
                }
                else
                {
                    ssquad.Render(rightStick, pos, size, "TexturedRegularAlpha");
                }
                shared.rightStickBox.Set(pos, pos + size);

                // Left stick for changing focus.
                pos = new Vector2(180, 90);
                size = new Vector2(42, 63);
                if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                {
                    // We use mouse clicking for setting focus so don't render arrows.
                    //ssquad.Render(UI2D.Shared.UpDownArrowsTexture, pos, size, "TexturedRegularAlpha");
                }
                else
                {
                    ssquad.Render(leftStick, pos, size, "TexturedRegularAlpha");
                }
                shared.leftStickBox.Set(pos, pos + size);

                // Safety warning.
                {
                    Vector2 safetyPos = new Vector2(leftEdge, 462);
                    string safetyString = Strings.Localize("saveLevelDialog.safetyWarning");
                    TextHelper.DrawString(UI2D.Shared.GetGameFont15_75, safetyString, safetyPos, AuthUI.ErrorColor, maxWidth: 700);
                }

                // Tags
                Vector2 buttonSize = new Vector2(56, 56);
                pos = new Vector2(leftEdge, 548);
                shared.tagsButton.Render(pos);
                pos.X += shared.tagsButton.GetSize().X + 16.0f;

                int bits = shared.curTags;
                string tags = null;
                for (int shift = 1; shift < 32; shift++)
                {
                    int i = 1 << shift;
                    // Since we're saving, ignore the BuiltInWorlds genre since 
                    // this will get stripped out on save.  Don't strip it out 
                    // here since we still might back out plus we need it 
                    // when the actual save happens for force a new GUID to
                    // be generated.
                    if ((i & bits) != 0 && i != (int)Genres.BuiltInWorlds && i != (int)Genres.StarterWorlds)
                    {
                        tags += Strings.GetGenreName(i) + " ";
                    }
                }
                if (tags != null)
                {
                    //TextHelper.DrawString(Font, tags, pos, textBodyColor);
                    batch.End();

                    TextBlob blob = new TextBlob(Font, tags, 420);
                    blob.RenderWithButtons(pos, lightTextBodyColor, maxLines: 3);

                    batch.Begin();

                }
                else
                {
                    TextHelper.DrawString(Font, Strings.Localize("saveLevelDialog.noTags"), pos, textBodyColor);
                }

                //
                // Buttons
                //

                pos = new Vector2(1040, 547);    // Right edge of line of buttons.

                pos.X -= shared.cancelButton.GetSize().X;
                shared.cancelButton.Render(pos);

                int buttonMargin = UI2D.Button.Margin;
                pos.X -= (Math.Max(shared.changeButton.GetSize().X, shared.saveButton.GetSize().X) + buttonMargin);
                shared.changeButton.Render(pos);

                pos.Y += 67;
                shared.saveButton.Render(pos);

                batch.End();

                // Description justification buttons.
                {
                    Color greenTextColor = new Color(106, 189, 69);
                    Color whiteTextColor = new Color(255, 255, 255);

                    // left
                    Vector4 color = Vector4.One;
                    Vector2 min = new Vector2(942, 460);
                    Vector2 max = min + new Vector2(32, 32);
                    shared.leftJustifyHitBox.Set(min, max);
                    color = shared.descBlob.Justification == UIGridElement.Justification.Left ? greenTextColor.ToVector4() : whiteTextColor.ToVector4();
                    ssquad.Render(leftJustifyTexture, color, min, new Vector2(32, 32), "TexturedRegularAlpha");

                    // center
                    min.X += 36;
                    max.X += 36;
                    shared.centerJustifyHitBox.Set(min, max);
                    color = shared.descBlob.Justification == UIGridElement.Justification.Center ? greenTextColor.ToVector4() : whiteTextColor.ToVector4();
                    ssquad.Render(centerJustifyTexture, color, min, new Vector2(32, 32), "TexturedRegularAlpha");

                    // right
                    min.X += 36;
                    max.X += 36;
                    shared.rightJustifyHitBox.Set(min, max);
                    color = shared.descBlob.Justification == UIGridElement.Justification.Right ? greenTextColor.ToVector4() : whiteTextColor.ToVector4();
                    ssquad.Render(rightJustifyTexture, color, min, new Vector2(32, 32), "TexturedRegularAlpha");
                }

                //
                // Render the tags grid if active with shadow under it.
                //
                RenderDropShadow(rtSize);
                if (shared.tagPicker != null && shared.tagPicker.Active)
                {
                    shared.tagPicker.Render(shared.camera);
                }

                //
                // Now that the whole UI has been rendered, shrink/crop to make it fit.
                //

                InGame.RestoreRenderTarget();

                // Start by using the blurred version of the scene as a backdrop.
                // If the thumbnail is no longer valid, just use black.
                if (InGame.inGame.SmallThumbNail != null && !InGame.inGame.SmallThumbNail.IsDisposed && !InGame.inGame.SmallThumbNail.GraphicsDevice.IsDisposed)
                {
                    InGame.Clear(Color.Transparent);
                    ssquad.Render(InGame.inGame.SmallThumbNail, Vector2.Zero, BokuGame.ScreenSize, @"TexturedNoAlpha");
                }
                else
                {
                    InGame.Clear(new Color(20, 20, 20));
                }

                // Copy the rendered scene to the rendertarget.
                // Need to fit the RT within the screen.  If screen is
                // bigger than RT then just center it.
                Vector2 screenSize = BokuGame.ScreenSize;
                Vector2 ratios = screenSize / rtSize;

                // Figure out which dimension will constrain us.
                shared.rtScale = Math.Min(ratios.X, ratios.Y);
                // Clamp to 1 so we don't stretch result, only allow shinking.
                //shared.rtScale = Math.Min(shared.rtScale, 1.0f);
                Vector2 newSize = rtSize * shared.rtScale;
                // Calc position to center RT on screen.
                shared.rtDisplayPosition = (screenSize - newSize) / 2.0f;

                // Clamp position to integer coords.
                shared.rtDisplayPosition = new Vector2((int)shared.rtDisplayPosition.X, (int)shared.rtDisplayPosition.Y);

                // Render dialog.
                ssquad.Render(rtFull, shared.rtDisplayPosition + BokuGame.ScreenPosition, newSize, @"TexturedRegularAlpha");

#if NETFX_CORE
                VirtualKeyboard.Render();
#endif
            }   // end of SaveLevelDialog RenderObj Render()

            #endregion

            #region Internal

            private void RenderDropShadow(Vector2 rtSize)
            {
                if (menuActive)
                {
                    if (!shared.tagPicker.Active)
                    {
                        menuActive = false;

                        //auxMenuShadowAlpha = 0.0f;
                        TwitchManager.Set<float> set = delegate(float val, Object param) { dropShadowAlpha = val; };
                        TwitchManager.CreateTwitch<float>(dropShadowAlpha, 0.0f, set, 0.2f, TwitchCurve.Shape.EaseInOut);
                    }
                }
                else
                {
                    if (shared.tagPicker.Active)
                    {
                        menuActive = true;

                        //auxMenuShadowAlpha = 0.8f;
                        TwitchManager.Set<float> set = delegate(float val, Object param) { dropShadowAlpha = val; };
                        TwitchManager.CreateTwitch<float>(dropShadowAlpha, 1.0f, set, 0.2f, TwitchCurve.Shape.EaseInOut);
                    }
                }

                if (dropShadowAlpha > 0.0f)
                {
                    ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();
                    quad.Render(dropShadow, new Vector4(1.0f, 1.0f, 1.0f, dropShadowAlpha), Vector2.Zero, rtSize, @"TexturedRegularAlpha");
                }
            }   // end of RenderDropShadow()

            public override void Activate()
            {
            }

            public override void Deactivate()
            {
            }

            /// <summary>
            /// Helper function to save some typing...
            /// </summary>
            /// <param name="tex"></param>
            /// <param name="path"></param>
            public void LoadTexture(ref Texture2D tex, string path)
            {
                if (tex == null)
                {
                    tex = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + path);
                }
            }   // end of LoadTexture()

            public void LoadContent(bool immediate)
            {
            }   // end of LoadContent()

            public void InitDeviceResources(GraphicsDevice device)
            {
                if (backgroundTexture == null)
                {
                    backgroundTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\SaveLevel\SaveLevelBackground");
                }

                if (leftStick == null)
                {
                    leftStick = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\HelpCard\LeftStick");
                }

                if (rightStick == null)
                {
                    rightStick = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\HelpCard\RightStick");
                }

                if (leftJustifyTexture == null)
                {
                    leftJustifyTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\TextEditor\LeftJustify");
                }
                if (centerJustifyTexture == null)
                {
                    centerJustifyTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\TextEditor\CenterJustify");
                }
                if (rightJustifyTexture == null)
                {
                    rightJustifyTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\TextEditor\RightJustify");
                }

                if (dropShadow == null)
                {
                    dropShadow = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\LoadLevel\AuxMenuShadow");
                }

            }   // end of InitDeviceResources()

            public void UnloadContent()
            {
                BokuGame.Release(ref backgroundTexture);
                BokuGame.Release(ref leftStick);
                BokuGame.Release(ref rightStick);
                BokuGame.Release(ref leftJustifyTexture);
                BokuGame.Release(ref centerJustifyTexture);
                BokuGame.Release(ref rightJustifyTexture);
                BokuGame.Release(ref dropShadow);
            }   // end of SaveLevelDialog RenderObj UnloadContent()

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
            }

            #endregion

        }   // end of class SaveLevelDialog RenderObj     

        #region Members

        // delegates
        public delegate void SaveLevelDialogButtonHandler(SaveLevelDialog dialog);
        public SaveLevelDialogButtonHandler OnButtonPressed = null;

        public enum SaveLevelDialogButtons
        {
            None,
            Cancel,
            Save,
            Accept,
            Discard,
        }
        private SaveLevelDialogButtons button = SaveLevelDialogButtons.None;

        public static SaveLevelDialog Instance = null;

        /// <summary>
        /// We need to have a ref to the parent PieSelector since, if we paste in
        /// a line of example code, we also need to deactivate the pie selector.
        /// </summary>
        public PieSelector parent = null;

        // List objects.
        protected Shared shared = null;
        protected RenderObj renderObj = null;
        protected UpdateObj updateObj = null;

        protected ModularMessageDialog overwriteWarning = null;    // Dialog to warn the user that they're about to overwrite a file.

        private enum States
        {
            Inactive,
            Active,
        }
        private States state = States.Inactive;

        private CommandMap commandMap = new CommandMap("SaveLevelDialog");

        #endregion

        #region Accessors

        public bool Active
        {
            get { return (state == States.Active); }
        }

        /// <summary>
        /// Returns the button the user pressed when exiting the dialog.
        /// </summary>
        public SaveLevelDialogButtons Button
        {
            get { return button; }
            set { button = value; }
        }

        #endregion

        #region Public

        // c'tor
        public SaveLevelDialog()
        {
            SaveLevelDialog.Instance = this;

            shared = new Shared(this);

            // Create the RenderObject and UpdateObject parts of this mode.
            updateObj = new UpdateObj(this, shared);
            renderObj = new RenderObj(shared);

            // Init the overwrite warning dialog.
            SetUpOverwriteWarning();

        }   // end of SaveLevelDialog c'tor

        public void Update()
        {
            if (Active)
            {
                updateObj.Update();
                overwriteWarning.Update();
            }
        }   // end of Update()

        public void Render(Camera camera)
        {
            if (Active)
            {
                renderObj.Render(camera);
                overwriteWarning.Render();  // Rendered last so it's on top if active.
            }
        }   // end of Render()

        public void OnSelect(UIGrid grid)
        {
            // We should never get here.  The individual elements should handle selection on their own.
            Debug.Assert(false);
        }   // end of OnSelect()

        public void OnExit(ModularCheckboxList list)
        {
            // Copy state from grid into current value;
            shared.curTags = shared.tagPicker.GetTags();
        }   // end of OnExit()

        #endregion

        #region Internal

        internal void ShowPreserveLinksDialog()
        {
            //handler for "continue" - user wants to play anyway
            ModularMessageDialog.ButtonHandler handlerA = delegate(ModularMessageDialog dialog)
            {
                //close the dialog
                dialog.Deactivate();

                updateObj.SaveLevelAndExit(true, true);
            };

            //handler for "continue" - user wants to play anyway
            ModularMessageDialog.ButtonHandler handlerB = delegate(ModularMessageDialog dialog)
            {
                //close the dialog
                dialog.Deactivate();

                updateObj.SaveLevelAndExit(true, false);
            };

            string text = Strings.Localize("loadLevelMenu.preserveLinksMessage");
            string labelA = Strings.Localize("textDialog.yes");
            string labelB = Strings.Localize("textDialog.no");
            ModularMessageDialogManager.Instance.AddDialog(text, handlerA, labelA, handlerB, labelB);
        }

        /// <summary>
        /// Set up the warning dialog that will be displayed if the user attempts
        /// to save a file using an existing file name.  The options given to the
        /// user are:
        ///     A -- overwrite
        ///     B -- back
        ///     Y -- increment version number and save
        /// </summary>
        private void SetUpOverwriteWarning()
        {
            ModularMessageDialog.ButtonHandler handlerA = delegate(ModularMessageDialog dialog)
            {
                dialog.Deactivate();

                // Overwrite with current name.
                updateObj.SaveLevelAndExit(false, true);
            };

            ModularMessageDialog.ButtonHandler handlerB = delegate(ModularMessageDialog dialog)
            {
                dialog.Deactivate();

                // Back out gracefully.
                // This just returns us to the SaveLevel dialog with nothing changed.
            };

            ModularMessageDialog.ButtonHandler handlerY = delegate(ModularMessageDialog dialog)
            {
                dialog.Deactivate();

                // Increment version number and save.
                shared.curVersion = Math.Min(shared.curVersion + 1, shared.maxVersion);
                
                ShowPreserveLinksDialog();

            };

            overwriteWarning = new ModularMessageDialog(Strings.Localize("saveLevelDialog.overwriteWarning"),
                handlerA, Strings.Localize("saveLevelDialog.overwrite"),
                handlerB, Strings.Localize("saveLevelDialog.back"),
                null, null,
                handlerY, Strings.Localize("saveLevelDialog.incrementVersion"));

        }   // end of SetUpOverwriteWarning()

        private object timerInstrument = null;

        public override void Activate()
        {
            if (state != States.Active)
            {
                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Push(commandMap);

                // Get the current file info.
                string name = StripVersionNumber(InGame.XmlWorldData.name, ref shared.curVersion);
                shared.originalName = shared.CurName = name;
                shared.originalVersion = shared.curVersion;
                shared.originalDescription = shared.curDesc = InGame.XmlWorldData.description;
                shared.originalDescJustification = shared.curDescJustification = InGame.XmlWorldData.descJustification;

                if (shared.originalDescription == null || shared.originalDescription.Length == 0)
                {
//removed default text.
//                    shared.originalDescription = shared.curDesc = Strings.Localize("saveLevelDialog.descriptionPromptPC");
                    shared.originalDescJustification = shared.curDescJustification = UIGridElement.Justification.Left;
                }

                shared.descIndent = (int)renderObj.Font().MeasureString(Strings.Localize("saveLevelDialog.description")).X;
                shared.descBlob = new TextBlob(renderObj.Font, shared.curDesc, shared.descWidth - shared.descIndent);
                shared.descBlob.Justification = shared.curDescJustification;

                // Strip the 'special' tags.
                InGame.XmlWorldData.genres &= ~((int)Genres.Special);
                shared.curTags = InGame.XmlWorldData.genres;

                state = States.Active;

                // Tell InGame we're using the thumbnail so no need to do full render.
                InGame.inGame.RenderWorldAsThumbnail = true;

                HelpOverlay.Push(@"SaveLevel");

                updateObj.Activate();

                timerInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.SaveLevelDialog);
            }
        }   // end of Activate

        /// <summary>
        /// Strips the version number off the name string and returns the string without it.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version">Version number found in original string, 0 if none found.</param>
        /// <returns></returns>
        private string StripVersionNumber(string name, ref int version)
        {
            int pos = name.LastIndexOf(@" v");
            if (pos == -1)
            {
                // No version number found.
                version = 0;
            }
            else
            {
                try
                {
                    version = int.Parse(name.Substring(pos + 2));
                }
                catch
                {
                    version = 0;
                }
                if (version != 0)
                {
                    // Got a valid version number, trim it off the name.
                    name = name.Substring(0, pos);
                }
            }

            return name;
        }   // end of StripVersionNumber()

        override public void Deactivate()
        {
            if (state != States.Inactive)
            {
                // Make sure VirutalKeyboard is also shut down.
                VirtualKeyboard.Deactivate();

                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Pop(commandMap);

                state = States.Inactive;

                // Ensure editing state is reset and focus goes back to the top.
                shared.editingText = false;
                shared.focus = Shared.InputFocus.Name;

                InGame.inGame.RenderWorldAsThumbnail = false;

                HelpOverlay.Pop();

                updateObj.Deactivate();

                Instrumentation.StopTimer(timerInstrument);
            }
        }

        public override bool Refresh(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            Debug.Assert(false, "This object is not designed to be put into any lists.");
            return true;
        }   // end of Refresh()

        public void LoadContent(bool immediate)
        {
            BokuGame.Load(renderObj, immediate);

            BokuGame.Load(overwriteWarning);
        }   // end of SaveLevelDialog LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
            BokuGame.Load(shared, true);    // This needs to be done after the aux menus are set up.

            overwriteWarning.InitDeviceResources(device);
        }

        public void UnloadContent()
        {
            BokuGame.Unload(shared);
            BokuGame.Unload(renderObj);

            BokuGame.Unload(overwriteWarning);
        }   // end of SaveLevelDialog UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
            BokuGame.DeviceReset(shared, device);
            BokuGame.DeviceReset(renderObj, device);
            BokuGame.DeviceReset(overwriteWarning, device);
        }

        #endregion

    }   // end of class SaveLevelDialog

}   // end of namespace Boku
