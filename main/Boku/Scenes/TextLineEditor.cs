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

using KoiX;
using KoiX.Input;
using KoiX.Text;

using Boku.Base;
using Boku.Common;
using Boku.Common.Xml;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.Programming;
using Boku.SimWorld;
using Boku.Web;
using Boku.Fx;

using Boku.Audio;
using BokuShared;

namespace Boku
{
    public class TextLineEditor : GameObject, INeedsDeviceReset
    {

        public delegate void OnEditDone(bool bCanceled, string text);
        public delegate bool ValidateText(TextBlob textBlob);

        protected class Shared : INeedsDeviceReset
        {

            #region Members

            public TextLineEditor parent = null;

            public OnEditDone onEditDone = null; //Used as callback for when we finished entering text.
            public ValidateText validateTextCallback = null; //Used to limit text length or content.

            public TextBlob blob = null;

            public AABB2D location { get; set; }       // Postion on screen
            
            public AABB2D aHitBox = new AABB2D();       // Mouse hit region for <A> Save.
            public AABB2D bHitBox = new AABB2D();       // Mouse hit region for <B> Back.

            public AABB2D textAreaHitBox = new AABB2D();    // Mouse hit region for text area.

            public int cursorPosition = 0;      // Current cursor position.
            // 0 is before the 1st character, 1 is between 
            // the 1st and 2nd characters, etc.

            public int topLine = 0;             // Which line of the text is being shown at the default starting position.
            public int textOffset = 0;          // Vertical offset (in pixels) for beginning of text.
            public int textTop = 8;            // Magic numbers all determined by pushing stuff around in Photoshop.
            public int textMargin = 8;
            public int textWidth = 9999;

            public int textVisibleLines = 1;   // How many lines can we see in the window?


            public string iconName;
            public DateTime lastKeyPressedAt;
            #endregion

            #region Accessors
            #endregion

            #region Public

            // c'tor
            public Shared(TextLineEditor parent)
            {
                this.parent = parent;

            }   // end of Shared c'tor

            /// <summary>
            /// Given a font, text string and a pixel position, returns the index
            /// of the character nearest the pixel position when the text string
            /// is laid out for rendering.
            /// </summary>
            /// <param name="font">Font to be used for measuring</param>
            /// <param name="str">Text string</param>
            /// <param name="pixelPos">Position in pixels</param>
            /// <returns></returns>
            public int FindCharAtWidth(GetFont Font, string str, int pixelPos)
            {
                int charPos = 0;
                for (int i = 0; i < str.Length; i++)
                {
                    int x = (int)Font().MeasureString(str.Substring(0, i)).X;
                    if (x >= pixelPos)
                        break;
                    ++charPos;
                }

                return charPos;
            }   // end of FindCharAtWidth()

            #endregion

            #region Internal

            public void LoadContent(bool immediate)
            {
            }   // end of TextEditor Shared LoadContent()

            public void InitDeviceResources(GraphicsDevice device)
            {
            }   // end of InitDeviceResources()

            public void UnloadContent()
            {
            }   // end of TextEditor Shared UnloadContent()

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
            }

            #endregion

        }   // end of class Shared

        protected class UpdateObj : UpdateObject
        {
            #region Members

            private TextLineEditor parent = null;
            private Shared shared = null;

            #endregion

            #region Public

            public UpdateObj(TextLineEditor parent, Shared shared)
            {
                this.parent = parent;
                this.shared = shared;
            }

            // Used to detect when alt is released.
            bool prevAltPressed = false;

            public override void Update()
            {
                bool altPressed = KeyboardInputX.AltIsPressed;
                bool released = prevAltPressed && !altPressed;

                if (released && specialChar != null)
                {
                    // If specialChar is anything but valid numbers this will throw.
                    // Ignore and move on.
                    try
                    {
                        char c = (char)int.Parse(specialChar);
                        if (TextHelper.CharIsValid(c))
                        {
                            TextInput(c);
                        }
                    }
                    catch { }

                    specialChar = null;
                }
                prevAltPressed = altPressed;

                // Our children may have input focus but we can still steal away the buttons we care about.
                GamePadInput pad = GamePadInput.GetGamePad0();

                // Mouse input
                Vector2 hit = LowLevelMouseInput.PositionVec;

                hit = (hit - BokuGame.ScreenPosition);

                // Touch input
                TouchContact touch = TouchInput.GetOldestTouch();
                Vector2 touchHit = Vector2.Zero;
                if (touch != null)
                {
                    touchHit = touch.position;
                    touchHit = (touchHit - BokuGame.ScreenPosition);
                }

                // If the virtual keybaord is active, don't let touch input leak through.
                if (touch != null && VirtualKeyboard.Active && VirtualKeyboard.HitBox.Contains(touch.position))
                {
                    touch = null;
                }

                bool bSave = false;
                bool bCanceled = false;


                if (null != touch)
                {
                    parent.touchedThisFrame = true;

                    if (shared.bHitBox.Touched(touch, touchHit))
                    {
                        bCanceled = true;
                    }
                    if (shared.aHitBox.Touched(touch, touchHit))
                    {
                        bSave = true;
                    }

                }

                bCanceled = bCanceled || pad.ButtonB.WasPressed || shared.bHitBox.LeftPressed(hit);
                bSave = bSave || pad.ButtonA.WasPressed || KeyboardInputX.WasPressed(Keys.Escape) || shared.aHitBox.LeftPressed(hit);


                if (bCanceled)
                {
                    // Cancel.
                    // Ignore the edited text.  Just exit.
                    // Don't Ignore the EditDone Callback.
                    if (null != shared.onEditDone)
                    {
                        shared.onEditDone(true, "");
                        shared.onEditDone = null;
                    }

                    parent.Deactivate();
                    pad.ButtonB.ClearAllWasPressedState();
                }
                else if (bSave)
                {
                    shared.onEditDone(false, shared.blob.ScrubbedText);
                    shared.onEditDone = null;

                    parent.Deactivate();
                    pad.ButtonA.ClearAllWasPressedState();
                    KeyboardInputX.ClearAllWasPressedState(Keys.Escape);
                }

                Vector2 testhit = hit;
                if (touch != null)
                    testhit = touchHit;

                bool bMouseHit = shared.textAreaHitBox.LeftPressed(hit);
                bool bTouched = touch != null && shared.textAreaHitBox.Touched(touch, touchHit);

                // Check for hits in the text area.  The scroll arrows intrude a bit so ignore anything in that area.
                if (bMouseHit || bTouched)
                {
                    if (bTouched)
                    {
                        KeyboardInputX.ShowOnScreenKeyboard();
                    }

                    // Move the cursor to where the user pressed.
                    // Calc line we're on.
                    int line = (int)((testhit.Y - shared.textAreaHitBox.Min.Y) / shared.blob.TotalSpacing) + shared.topLine;
                    int curLine = 0;
                    int x = 0;
                    shared.blob.FindCursorLineAndPosition(out curLine, out x);
                    if (curLine > line)
                    {
                        for (int i = 0; i < curLine - line; i++)
                        {
                            shared.blob.CursorUp();
                        }
                    }
                    else if (curLine < line)
                    {
                        for (int i = 0; i < line - curLine; i++)
                        {
                            shared.blob.CursorDown();
                        }
                    }

                    shared.blob.FindCursorLineAndPosition(out curLine, out x);
                    int mouseX = (int)(testhit.X - shared.textAreaHitBox.Min.X);

                    // Ensure mouseX is within the current line.  Be sure to account for justification.
                    int curLineWidth = shared.blob.GetLineWidth(curLine);
                    int totalWidth = shared.blob.Width;

                    //Calculate margin based on justification.
                    int margin = 0;
                    switch (shared.blob.Justification)
                    {
                        case TextHelper.Justification.Center:
                            margin = (totalWidth - curLineWidth) / 2;
                            break;
                        case TextHelper.Justification.Right:
                            margin = totalWidth - curLineWidth;
                            break;
                    }

                    mouseX = Math.Max(mouseX, margin);
                    mouseX = Math.Min(mouseX, margin + curLineWidth);

                    shared.blob.SetCursorPosition(curLine, mouseX);
                    shared.blob.FindCursorLineAndPosition(out curLine, out x);
                }

                // If we're not shutting down...
                if (parent.Active)
                {
                }   // end if not shutting down.

            }   // end of Update()

            public void TwitchTextOffset()
            {
                // Start a twitch to move the text text offset.
                TwitchManager.Set<float> set = delegate(float val, Object param) { shared.textOffset = (int)val; };
                TwitchManager.CreateTwitch<float>(shared.textOffset, -shared.topLine * parent.renderObj.Font().LineSpacing, set, 0.2f, TwitchCurve.Shape.OvershootOut);
            }   // end of TwitchTextOffset()

            #endregion

            #region Internal

            // Used to accumulate values when user is inputting special characters using the Alt key.
            string specialChar = null;

            public void TextInput(char c)
            {
                shared.lastKeyPressedAt = DateTime.Now;
                // Handle special character input.
               if (KeyboardInputX.AltWasPressed)
                {
                    specialChar = null;
                }
                if (KeyboardInputX.AltIsPressed)
                {
                    // accumulate keystrokes
                    specialChar += c;
                    return;
                }

                //Ignore backspace and return character
                if (c == '\b' || c == '\r')
                {
                    return;
                }
                var oldText = shared.blob.RawText;
                shared.blob.InsertString(new string(c, 1));

                //Validate the text if required.
                if (shared.validateTextCallback != null && !shared.validateTextCallback(shared.blob))
                {
                        //Invalid so restore text.
                        shared.blob.RawText = oldText;
                        Foley.PlayNoBudget();
                }
                else
                {
                    Foley.PlayClickDown();
                }

            }   // end of UpdateObj TextInput()

            public void KeyInput(Keys key)
            {
                switch (key)
                {
                    case Keys.Enter:
                        Foley.PlayClickDown();
                        KeyboardInputX.ClearAllWasPressedState(Keys.Enter);
                        Accept();
                        break;

                    case Keys.Escape:
                        Foley.PlayClickDown();
                        KeyboardInputX.ClearAllWasPressedState(Keys.Escape);
                        Cancel();
                        break;

                    case Keys.Left:
                        Foley.PlayClickDown();
                        shared.blob.CursorLeft();
                        break;

                    case Keys.Right:
                        Foley.PlayClickDown();
                        shared.blob.CursorRight();
                        break;

                    //case Keys.Up:
                    //    shared.blob.CursorUp();
                    //    break;

                    //case Keys.Down:
                    //    shared.blob.CursorDown();
                    //    break;

                    case Keys.Home:
                        Foley.PlayClickDown();
                        shared.blob.Home();
                        break;

                    case Keys.End:
                        Foley.PlayClickDown();
                        shared.blob.End();
                        break;

                    case Keys.Back:
                        Foley.PlayClickDown();
                        shared.blob.Backspace();
                        break;

                    case Keys.Delete:
                        Foley.PlayClickDown();
                        shared.blob.Delete();
                        break;

                }   // end of switch on special characters.

            }   // end of UpdateObj KeyInput()

            /// <summary>
            /// User has accepted the currently edited string.  We don't need to 
            /// copy to the current position since it should already be there.  
            /// So, just turn off editing.
            /// </summary>
            public void Accept()
            {
                if (null != shared.onEditDone)
                {
                    shared.blob.RawText = shared.blob.RawText.Trim();//trim string before return
                    shared.onEditDone(false, shared.blob.RawText);
                    shared.onEditDone = null;
                }
                parent.Deactivate();
            }   // end of Accept()

            public void Cancel()
            {
                if (null != shared.onEditDone)
                {
                    shared.blob.RawText = shared.blob.RawText.Trim();//trim string before return
                    shared.onEditDone(true, shared.blob.RawText);
                    shared.onEditDone = null;
                }
                parent.Deactivate();
            }   // end of Cancel()

            public void Discard()
            {
            }   // end of Discard()

            public override void Activate()
            {
                //KeyboardInputX.OnKey = KeyInput;
                shared.lastKeyPressedAt = DateTime.Now;
#if NETFX_CORE
                Debug.Assert(false, "Does this work?  Why did we prefer winKeyboard?");
                KeyboardInputX.OnChar = TextInput;
#else
                BokuGame.bokuGame.winKeyboard.CharacterEntered = TextInput;
                //KeyboardInputX.OnChar = TextInput;
#endif
            }

            public override void Deactivate()
            {
                //KeyboardInputX.OnKey = null;
#if NETFX_CORE
                KeyboardInputX.OnChar = null;
#else
                BokuGame.bokuGame.winKeyboard.CharacterEntered = null;
#endif
            }

            #endregion

        }   // end of class TextInput UpdateObj  

        protected class RenderObj : RenderObject, INeedsDeviceReset
        {
            #region Members

            private Shared shared;

            public Texture2D backgroundTexture = null;    // The background frame we render over.
            public Texture2D iconTexture = null;    // Accept button

            #endregion

            #region Accessors

            public GetFont Font
            {
                get { return KoiX.SharedX.GetGameFont20; }
            }

            #endregion

            #region Public

            public RenderObj(Shared shared)
            {
                this.shared = shared;
            }

            public override void Render(Camera camera)
            {
                GraphicsDevice device = KoiLibrary.GraphicsDevice;

                CameraSpaceQuad csquad = CameraSpaceQuad.GetInstance();
                ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();

                Color darkTextColor = new Color(20, 20, 20);

                var size = shared.location.Size;
                // Render background.
                var pos = shared.location.Min;

                if (shared.parent.Active)
                {
                    var highliteSize = new Vector2(1, 1);
                    ssquad.Render(new Vector4(0, 0, 0, 255), pos - highliteSize, size + (highliteSize * 2));
                }
                ssquad.Render(backgroundTexture, pos, size, @"TexturedRegularAlpha");

                var iconOffset = new Vector2();
                if (iconTexture!=null)
                {
                    ssquad.Render(iconTexture, pos, new Vector2(iconTexture.Width, iconTexture.Height),
                        @"TexturedRegularAlpha");
                    iconOffset = new Vector2(iconTexture.Width,iconTexture.Height);
                }
                //Setup text clipping
                var oldViewport = device.Viewport;
                var newViewport = device.Viewport;
                newViewport.X = (int)shared.location.Min.X + (int)iconOffset.X;
                newViewport.Y = (int)shared.location.Min.Y;
                newViewport.Width = (int)shared.location.Size.X - (int)iconOffset.X;
                newViewport.Height = (int)shared.location.Size.Y;
                device.Viewport = newViewport; 

                    //handle horizontal scrolling
                var offset = 0;
                int curLine,curPos;
                shared.blob.FindCursorLineAndPosition(out curLine, out curPos);
                if (curPos > newViewport.Width - 30)
                    offset = -curPos + ((int)newViewport.Width - 30);
                pos = new Vector2(shared.textMargin+offset, shared.textTop + shared.textOffset);

                    //Flash cursor every half second
                var cursorOn = DateTime.Now.Millisecond > 500 & shared.parent.Active;

                    //Render text
                shared.blob.RenderText(null, pos, darkTextColor, renderCursor: cursorOn);

                    //Restore viewport
                device.Viewport = oldViewport;

            }   // end of TextInput RenderObj Render()

            #endregion

            #region Internal

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
                    tex = KoiLibrary.LoadTexture2D(path);
                }
            }   // end of LoadTexture()

            public void LoadContent(bool immediate)
            {
            }   // end of LoadContent()

            public void InitDeviceResources(GraphicsDevice device)
            {
                if (backgroundTexture == null)
                {
                    backgroundTexture = KoiLibrary.LoadTexture2D(@"Textures\GridElements\CheckBoxWhite");
                }
                if (!string.IsNullOrEmpty(shared.iconName))
                {
                    iconTexture = KoiLibrary.LoadTexture2D(shared.iconName);
                }
                

            }   // end of InitDeviceResources()

            public void UnloadContent()
            {
                DeviceResetX.Release(ref backgroundTexture);
                DeviceResetX.Release(ref iconTexture);
            }   // end of TextInput RenderObj UnloadContent()

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
            }

            #endregion

        }   // end of class TextInput RenderObj     

        #region Members

        public static TextLineEditor Instance = null;

        /// <summary>
        /// We need to have a ref to the parent PieSelector since, if we paste in
        /// a line of example code, we also need to deactivate the pie selector.
        /// </summary>
        public PieSelector parent = null;

        // List objects.
        protected Shared shared = null;
        protected RenderObj renderObj = null;
        protected UpdateObj updateObj = null;

        private enum States
        {
            Inactive,
            Active,
        }
        private States state = States.Inactive;

        private CommandMap commandMap = new CommandMap("TextEditor");

        private bool touchedThisFrame = false;
        public bool WasTouchedThisFrame { get { return touchedThisFrame; } }

        #endregion

        #region Accessors

        public bool Active
        {
            get { return (state == States.Active); }
        }

        #endregion

        #region Public

        // c'tor
        public TextLineEditor(AABB2D location,string original,string iconName=null)
        {
            TextLineEditor.Instance = this;

            shared = new Shared(this);
            shared.location = location;

            // Set the hit box for the text area.  
            shared.textAreaHitBox.Set(shared.location);

            // Create the RenderObject and UpdateObject parts of this mode.
            updateObj = new UpdateObj(this, shared);
            renderObj = new RenderObj(shared);

            shared.blob = new TextBlob(renderObj.Font, (null != original) ? original : "", shared.textWidth);
            shared.blob.Justification = TextHelper.Justification.Left;
            shared.blob.End();

            shared.topLine = 0;
            shared.textOffset = 0;

            shared.iconName = iconName;
            // We use the updateObj for this deserialization since the text callbacks 
            // belong to it.  Otherwise nothing will ever hook up right.
            //commandMap = CommandMap.Deserialize(updateObj, @"TextEditor.Xml");

        }   // end of TextEditor c'tor

        public void OnSelect(UIGrid grid)
        {
            // We should never actually get here.  The TextEditor UpdateObj 
            // should consume all 'A' presses before the grids get them...

            Debug.Assert(false);

        }   // end of OnSelect()

        public void OnCancel(UIGrid grid)
        {
            // We should never actually get here.  The TextEditor UpdateObj 
            // should consume all 'B' presses before the grids get them...

            Debug.Assert(false);

        }   // end of OnCancel()

        public void Update()
        {
            touchedThisFrame = false;
            if (Active)
            {
                updateObj.Update();
            }
        }   // end of Update()

        public void Render(Camera camera)
        {
            //if (Active)
            {
                renderObj.Render(camera);
            }
        }   // end of Render()

        #endregion

        #region Internal

        override public void Activate()
        {
            Debug.Assert(false, "Should this ever be activated through here?");
            //Activate(null, null, false);
        }

        public string GetText()
        {
            return shared.blob.RawText.Trim();
        }
        public void SetText(string text)
        {
            // Ensure that we have a valid string even if it's empty.
            if (text == null)
            {
                text = string.Empty;
            }
            shared.blob.RawText = text.Trim();
        }
        public double GetSecondsSinceLastKeypress()
        {
            var elapsed = DateTime.Now - shared.lastKeyPressedAt;
            return elapsed.TotalMilliseconds / 1000.0;
        }

        public void Activate(OnEditDone doneCallback, string original,ValidateText validateTextCallback=null)
        {
            SetText(original);
            shared.blob.End();  // Start with cursor at end of existing string.

            if (state != States.Active)
            {

                shared.onEditDone = doneCallback;
                shared.validateTextCallback = validateTextCallback;

                //------------------ Copied from other implementation --------------
                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Push(commandMap);

                HelpOverlay.Push(@"TextLineEditor");

                //------------------ Copied from other implementation END --------------

                state = States.Active;

                //shared.blob.s

                updateObj.Activate();
            }

        }

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

                HelpOverlay.Pop();

                updateObj.Deactivate();
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

        }   // end of TextEditor LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
            BokuGame.Load(shared, true);    // This needs to be done after the aux menus are set up.
        }

        public void UnloadContent()
        {
            BokuGame.Unload(shared);
            BokuGame.Unload(renderObj);
        }   // end of TextEditor UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
            BokuGame.DeviceReset(shared, device);
            BokuGame.DeviceReset(renderObj, device);
        }

        #endregion

    }   // end of class TextInput

}
