
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
    /// <summary>
    /// Text editor for adding text to programming tiles from within programming UI.
    /// </summary>
    public class TextEditor : GameObject, INeedsDeviceReset
    {
        public const string kTM_TouchButtonLabel = "touchButtonLabels";

        public delegate void OnButtonLabelEditDone(bool bCanceled, string text);

        protected class Shared : INeedsDeviceReset
        {

            #region Members

            public TextEditor parent = null;

            public ReflexData reflexData = null;

            public string targetMode = null;    // Either "say", "said" or kTM_TouchButtonLabel or "comment"
            public OnButtonLabelEditDone onButtonLabelEditDone = null; //Used as callback for when we finished entering text.

            public bool IsTargetModeLabel { get { return targetMode == kTM_TouchButtonLabel; } }

            public Camera camera = null;
            public Camera camera1k = null;      // Camera for rendering to the 1024x768 rt.
            public Texture2D thumbnail = null;  // Scene image for background.

            public int mode = 0;                // Display mode:    0 == fullscreen
                                                //                  1 == thought balloon, pick lines sequntially
                                                //                  2 == thought balloon, pick lines randomly

            public TextBlob blob = null;

            public AABB2D aHitBox = new AABB2D();       // Mouse hit region for <A> Save.
            public AABB2D bHitBox = new AABB2D();       // Mouse hit region for <B> Back.
            public AABB2D yHitBox = new AABB2D();       // Mouse hit region for <Y> display mode.
            public AABB2D scrollHitBox = new AABB2D();  // Mouse hit region for scrolling text.
            public AABB2D fullscreenHitBox = new AABB2D();  // Mouse hit regions for choosing display mode.
            public AABB2D sequentialHitBox = new AABB2D();
            public AABB2D randomHitBox = new AABB2D();
            public AABB2D textAreaHitBox = new AABB2D();    // Mouse hit region for text area.
            public Vector2 rt1kRenderPos = Vector2.Zero;    // Position on full screen where 1k rt is rendered.
            // Used to help with mouse hits.

            public AABB2D leftJustifyHitBox = new AABB2D();
            public AABB2D centerJustifyHitBox = new AABB2D();
            public AABB2D rightJustifyHitBox = new AABB2D();

            //public string text = null;          // The text we're editing.
            //public List<string> textLines = null;   // The text broken into individual lines.

            public int cursorPosition = 0;      // Current cursor position.
            // 0 is before the 1st character, 1 is between 
            // the 1st and 2nd characters, etc.

            public int topLine = 0;             // Which line of the text is being shown at the default starting position.
            public int textOffset = 0;          // Vertical offset (in pixels) for beginning of text.
            public int textTop = 80;            // Magic numbers all determined by pushing stuff around in Photoshop.
            public int textMargin = 80;
            public int textWidth = 880;

            public int textVisibleLines = 11;   // How many lines can we see in the window?
            public int textMaxNumLines = 100;   // Do we need/want this limit?
                                                // How do we limit fullscreen vs thought balloon?

            public Vector2 renderPosition;
            public Vector2 renderSize;
            public Vector2 renderScale;
            public bool useRtCoords = false;    // Rendering to an RT?

            #endregion

            #region Accessors
            #endregion

            #region Public

            // c'tor
            public Shared(TextEditor parent)
            {
                this.parent = parent;

                GraphicsDevice device = KoiLibrary.GraphicsDevice;

                // We're rendering the camera specific parts into a 1024x768 rendertarget and
                // then copying (with masking) into the 1280x720 rt and finally cropping it 
                // as needed for 4:3 display.
                camera = new PerspectiveUICamera();
                camera.Resolution = new Point(1280, 720);
                camera1k = new PerspectiveUICamera();
                camera1k.Resolution = new Point(1024, 768);

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

            private TextEditor parent = null;
            private Shared shared = null;

            #endregion

            #region Public

            public UpdateObj(TextEditor parent, Shared shared)
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
                if (shared.useRtCoords)
                {
                    hit = ScreenWarp.ScreenToRT(hit);
                }
                hit = (hit - shared.renderPosition + BokuGame.ScreenPosition) / shared.renderScale;


                // Touch input
                TouchContact touch = TouchInput.GetOldestTouch();
                Vector2 touchHit = Vector2.Zero;
                if (touch != null)
                {
                    touchHit = touch.position;
                    if (shared.useRtCoords)
                    {
                        touchHit = ScreenWarp.ScreenToRT(touchHit);
                    }
                    touchHit = (touchHit - shared.renderPosition + BokuGame.ScreenPosition) / shared.renderScale;
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
                    // Ignore the edited text, don't copy back to the reflex.  Just exit.
                    // Don't Ignore the EditDone Callback.
                    if (null != shared.onButtonLabelEditDone)
                    {
                        shared.onButtonLabelEditDone(true, "");
                        shared.onButtonLabelEditDone = null;
                    }

                    parent.Deactivate();
                    pad.ButtonB.ClearAllWasPressedState();
                }
                else if (bSave)
                {
                    // Save the edited text into the current reflex and then deactivate.
                    if (shared.targetMode == "say")
                    {
                        shared.reflexData.sayMode = shared.mode;
                        string str = shared.blob.ScrubbedText;
                        str = TextHelper.FilterURLs(str);
                        str = TextHelper.FilterEmail(str);
                        shared.reflexData.sayString = str;
                        shared.reflexData.sayJustification = shared.blob.Justification;
                    }
                    else if (shared.targetMode == "said")
                    {
                        shared.reflexData.saidMode = shared.mode;
                        string str = shared.blob.ScrubbedText;
                        str = TextHelper.FilterURLs(str);
                        str = TextHelper.FilterEmail(str);
                        shared.reflexData.saidString = str;
                        shared.reflexData.saidJustification = shared.blob.Justification;
                    }
                    else if (shared.targetMode == kTM_TouchButtonLabel)
                    {
                        //
                        Debug.Assert(null != shared.onButtonLabelEditDone);

                        string str = shared.blob.ScrubbedText;
                        str = TextHelper.FilterURLs(str);
                        str = TextHelper.FilterEmail(str);

                        shared.onButtonLabelEditDone(false, str);
                        shared.onButtonLabelEditDone = null;
                    }
                    else
                    {
                        Debug.Assert(false, "Unknown text editor targetMode");
                    }

                    parent.Deactivate();
                    pad.ButtonA.ClearAllWasPressedState();
                    KeyboardInputX.ClearAllWasPressedState(Keys.Escape);
                }

                if (shared.leftJustifyHitBox.LeftPressed(hit) || (touch != null && shared.leftJustifyHitBox.Touched(touch, touchHit)))
                {
                    if (shared.leftJustifyHitBox.LeftPressed(hit) || (touch != null && shared.leftJustifyHitBox.Touched(touch, touchHit)))
                    {
                        shared.blob.Justification = TextHelper.Justification.Left;
                    }
                    if (shared.centerJustifyHitBox.LeftPressed(hit) || (touch != null && shared.centerJustifyHitBox.Touched(touch, touchHit)))
                    {
                        shared.blob.Justification = TextHelper.Justification.Center;
                    }
                    if (shared.rightJustifyHitBox.LeftPressed(hit) || (touch != null && shared.rightJustifyHitBox.Touched(touch, touchHit)))
                    {
                        shared.blob.Justification = TextHelper.Justification.Right;
                    }

                    if (pad.ButtonY.WasPressed || KeyboardInputX.WasPressed(Keys.Tab))
                    {
                        // Cycle through the choices.
                        int numChoices = shared.targetMode == "say" ? 3 : 2;
                        shared.mode = ++shared.mode % numChoices;

                        pad.ButtonY.ClearAllWasPressedState();
                        KeyboardInputX.ClearAllWasPressedState(Keys.Tab);
                    }
                }

                if (touch != null && shared.yHitBox.Touched(touch, touchHit))
                {
                    // See if we hit the top or bottom of the box.
                    int numChoices = shared.targetMode == "say" ? 3 : 2;
                    if (touchHit.Y < (shared.yHitBox.Min.Y + shared.yHitBox.Max.Y) / 2)
                    {
                        shared.mode = (shared.mode + numChoices - 1) % numChoices;
                    }
                    else
                    {
                        shared.mode = ++shared.mode % numChoices;
                    }
                }

                if (shared.yHitBox.LeftPressed(hit))
                {
                    // See if we hit the top or bottom of the box.
                    int numChoices = shared.targetMode == "say" ? 3 : 2;
                    if (hit.Y < (shared.yHitBox.Min.Y + shared.yHitBox.Max.Y) / 2)
                    {
                        shared.mode = (shared.mode + numChoices - 1) % numChoices;
                    }
                    else
                    {
                        shared.mode = ++shared.mode % numChoices;
                    }
                }

                // Check explicit hits.
                if (shared.fullscreenHitBox.LeftPressed(hit) ||
                    (touch != null && shared.fullscreenHitBox.Touched(touch, touchHit)))
                {
                    shared.mode = 0;
                }
                if (shared.sequentialHitBox.LeftPressed(hit) ||
                    (touch != null && shared.sequentialHitBox.Touched(touch, touchHit)))
                {
                    shared.mode = 1;
                }
                if (shared.randomHitBox.LeftPressed(hit) ||
                    (touch != null && shared.randomHitBox.Touched(touch, touchHit)))
                {
                    shared.mode = 2;
                }

                Vector2 testhit = hit;
                if (touch != null)
                    testhit = touchHit;

                bool bMouseHit = !shared.scrollHitBox.LeftPressed(hit) && shared.textAreaHitBox.LeftPressed(hit);
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
                    int mouseX = (int)(testhit.X - shared.textAreaHitBox.Min.X - shared.rt1kRenderPos.X);

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

                // Scroll text???
                if (shared.blob.NumLines != 0)
                {
                    bool mouseScrollUp = false;
                    bool mouseScrollDown = false;
                    if (shared.scrollHitBox.LeftPressed(hit) ||
                        (touch != null && shared.scrollHitBox.Touched(touch, touchHit)))
                    {
                        // See if we hit the top or bottom of the box.
                        if (testhit.Y < (shared.scrollHitBox.Min.Y + shared.scrollHitBox.Max.Y) / 2)
                        {
                            mouseScrollUp = true;
                        }
                        else
                        {
                            mouseScrollDown = true;
                        }
                    }

                    if (pad.LeftStickUp.WasPressed || pad.LeftStickUp.WasRepeatPressed || mouseScrollUp)
                    {
                        ScrollDown();
                    }

                    if (pad.LeftStickDown.WasPressed || pad.LeftStickDown.WasRepeatPressed || mouseScrollDown)
                    {
                        ScrollUp();
                    }

                    // Mouse wheel.
                    int scroll = LowLevelMouseInput.DeltaScrollWheel;
                    if (scroll > 0)
                    {
                        ScrollDown();
                    }
                    else if (scroll < 0)
                    {
                        ScrollUp();
                    }

                    // If we're active, don't let anything else get any input.
                    GamePadInput.ClearAllWasPressedState();

                    // Check if we've moved the cursor offscreen.
                    // If so, scroll to put the cursor back onto the screen.
                    bool needToScroll = false;
                    int line = 0;
                    int curPos = 0;
                    shared.blob.FindCursorLineAndPosition(out line, out curPos);
                    if (line < shared.topLine)
                    {
                        --shared.topLine;
                        needToScroll = true;
                    }
                    else if (line >= shared.topLine + shared.textVisibleLines)
                    {
                        ++shared.topLine;
                        needToScroll = true;
                    }

                    if (needToScroll)
                    {
                        TwitchTextOffset();
                    }

                }

                // If we're not shutting down...
                if (parent.Active)
                {
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
                    shared.blob.FindCursorLineAndPosition(out line, out curPos);
                    if (line >= shared.topLine + shared.textVisibleLines)
                    {
                        shared.blob.CursorUp();
                    }
                }
            }   // end of ScrollDown()

            public void ScrollUp()
            {
                int numLines = shared.blob.NumLines;
                if (numLines - shared.textVisibleLines > shared.topLine)
                {
                    ++shared.topLine;
                    TwitchTextOffset();

                    // This scroll may have moved the cursor off screen, if so
                    // move the cursor so that it's back on screen.
                    int line = 0;
                    int curPos = 0;
                    shared.blob.FindCursorLineAndPosition(out line, out curPos);
                    if (line < shared.topLine)
                    {
                        shared.blob.CursorDown();
                    }
                }
            }   // end of ScrollUp()

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

            public void TextInputAltOnly(char c)
            {
                // Only pass along characters when the Alt key is pressed.
                if (KeyboardInput.AltIsPressed)
                {
                    TextInput(c);
                }
            }   // end of TextInputAltOnly()

            public void TextInput(char c)
            {
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

                // Ignore the Esc key.  It will be handled by KeyInput().
                if ((int)c == 27)
                {
                    return;
                }

                string str = new string(c, 1);
                str = TextHelper.FilterInvalidCharacters(str);

#if !NETFX_CORE
                // Copy?  Just copy the whole description to the clipboard since we don't
                // support any kind of selection.
                if (c == 3)
                {
                    System.Windows.Forms.Clipboard.SetText(shared.blob.ScrubbedText);
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

                shared.blob.InsertString(str);

            }   // end of UpdateObj TextInput()

            public void KeyInput(Keys key)
            {
                switch (key)
                {
                    case Keys.Enter:
                        shared.blob.Enter();
                        break;

                    case Keys.Escape:
                        Cancel();
                        break;

                    case Keys.Left:
                        shared.blob.CursorLeft();
                        break;

                    case Keys.Right:
                        shared.blob.CursorRight();
                        break;

                    case Keys.Up:
                        shared.blob.CursorUp();
                        break;

                    case Keys.Down:
                        shared.blob.CursorDown();
                        break;

                    case Keys.Home:
                        shared.blob.Home();
                        break;

                    case Keys.End:
                        shared.blob.End();
                        break;

                    case Keys.Back:
                        shared.blob.Backspace();
                        break;

                    case Keys.Delete:
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
            }   // end of Accept()

            public void Cancel()
            {
            }   // end of Cancel()

            public void Discard()
            {
            }   // end of Discard()

            public override void Activate()
            {
                //KeyboardInputX.OnKey = KeyInput;
#if NETFX_CORE
                Debug.Assert(false, "Does this work?  Why did we prefer winKeyboard?");
                KeyboardInputX.OnChar = TextInput;
#else
                // ARGH!
                // WinKeyboard handles Greek tonos properly which is probably why we switched over.
                // KeyboardInput handle Alt+ characters properly, which is why we need to switch back.
                // If both are hooked up, duplicates ensue.
                // So, create a filtered version of TextInput that only passes on stuff when Alt is
                // pressed.
                BokuGame.bokuGame.winKeyboard.CharacterEntered = TextInput;
                KeyboardInput.OnChar = TextInput;
#endif
            }

            public override void Deactivate()
            {
                //KeyboardInputX.OnKey = null;
#if NETFX_CORE
                KeyboardInputX.OnChar = null;
#else
                BokuGame.bokuGame.winKeyboard.CharacterEntered = null;
                KeyboardInput.OnChar = null;
#endif
            }

            #endregion

        }   // end of class TextEditor UpdateObj  

        protected class RenderObj : RenderObject, INeedsDeviceReset
        {
            #region Members

            private Shared shared;

            public Texture2D backgroundTexture = null;    // The background frame we render over.
            public Texture2D leftStick = null;

            public Texture2D leftJustifyTexture = null;
            public Texture2D centerJustifyTexture = null;
            public Texture2D rightJustifyTexture = null;

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

                RenderTarget2D rtFull = SharedX.RenderTargetDepthStencil1280_720;   // Rendertarget we render whole display into.
                RenderTarget2D rt1k = SharedX.RenderTargetDepthStencil1024_768;

                Vector2 screenSize = BokuGame.ScreenSize;
                Vector2 rtSize = new Vector2(rtFull.Width, rtFull.Height);

                CameraSpaceQuad csquad = CameraSpaceQuad.GetInstance();
                ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();

                Color darkTextColor = new Color(20, 20, 20);
                Color greyTextColor = new Color(127, 127, 127);
                Color greenTextColor = new Color(106, 189, 69);
                Color whiteTextColor = new Color(255, 255, 255);

                // Render the text text into the 1k rendertarget.
                InGame.SetRenderTarget(rt1k);
                InGame.Clear(Color.Transparent);

                // Set up params for rendering UI with this camera.
                BokuGame.bokuGame.shaderGlobals.SetCamera(shared.camera1k);

                SpriteBatch batch = KoiLibrary.SpriteBatch;

                //
                // Text.
                //

                Vector2 pos;
                pos = new Vector2(shared.textMargin, shared.textTop + shared.textOffset);
                shared.blob.RenderText(null, pos, darkTextColor, renderCursor: true);

                // Set the hit box for the text area.  Add 200 to the width to 
                // allow clicking to the right to get to the end of the line.
                pos.Y += shared.topLine * shared.blob.TotalSpacing;
                shared.textAreaHitBox.Set(pos, pos + new Vector2(shared.textWidth + 200, shared.textVisibleLines * shared.blob.TotalSpacing));

                // Render the scene to our rendertarget.
                InGame.SetRenderTarget(rtFull);

                // Set up params for rendering UI with this camera.
                BokuGame.bokuGame.shaderGlobals.SetCamera(shared.camera);

                InGame.Clear(Color.Transparent);

                // Now render the background tiles.
                Vector2 backgroundSize = new Vector2(backgroundTexture.Width, backgroundTexture.Height);
                pos = (rtSize - backgroundSize) / 2.0f;
                ssquad.Render(backgroundTexture, pos, backgroundSize, @"TexturedRegularAlpha");

                // Now render the contents of the rt1k texture but with the edges blended using the mask.
                Vector2 rt1kSize = new Vector2(rt1k.Width, rt1k.Height);
                pos = (rtSize - rt1kSize) / 2.0f;
                pos.Y = 0.0f;
                // Magic numbers for text editor.
                Vector4 limits = new Vector4(0.095f, 0.112f, 0.57f, 0.64f);
                ssquad.RenderWithYLimits(rt1k, limits, pos, rt1kSize, @"TexturedPreMultAlpha");

                shared.rt1kRenderPos = pos;

                //
                // Add labels.
                //

                batch.Begin();

                if (!shared.IsTargetModeLabel)
                {
                    if (shared.targetMode == "say")
                    {
                        TextHelper.DrawString(Font, Strings.Localize("programming.fullScreen"), new Vector2(246, 514), shared.mode == 0 ? greenTextColor : whiteTextColor);
                        TextHelper.DrawString(Font, Strings.Localize("programming.thoughtBalloonSequential"), new Vector2(246, 554), shared.mode == 1 ? greenTextColor : whiteTextColor);
                        TextHelper.DrawString(Font, Strings.Localize("programming.thoughtBalloonRandom"), new Vector2(246, 594), shared.mode == 2 ? greenTextColor : whiteTextColor);
                    }
                    else
                    {
                        TextHelper.DrawString(Font, Strings.Localize("programming.triggerAtBeginning"), new Vector2(246, 534), shared.mode == 0 ? greenTextColor : whiteTextColor);
                        TextHelper.DrawString(Font, Strings.Localize("programming.triggerAtEnd"), new Vector2(246, 574), shared.mode == 1 ? greenTextColor : whiteTextColor);
                    }
                }

                TextHelper.DrawString(Font, Strings.Localize("programming.save"), new Vector2(970, 525), whiteTextColor);
                TextHelper.DrawString(Font, Strings.Localize("programming.back"), new Vector2(970, 583), whiteTextColor);

                batch.End();

                // Mouse hit boxes.  The buttons are already baked into the texture so we have to account for them.
                Vector2 min = new Vector2(970, 525) + new Vector2(-50, 0);
                Vector2 max = min + Font().MeasureString(Strings.Localize("programming.save")) + new Vector2(50, 0);
                shared.aHitBox.Set(min, max);

                min = new Vector2(970, 583) + new Vector2(-50, 0);
                max = min + Font().MeasureString(Strings.Localize("programming.back")) + new Vector2(50, 0);
                shared.bHitBox.Set(min, max);

                min = new Vector2(190, 520);
                max = min + new Vector2(48, 104);
                shared.yHitBox.Set(min, max);

                if (shared.targetMode == "say")
                {
                    shared.fullscreenHitBox.Set(new Vector2(246, 514), new Vector2(246, 514) + Font().MeasureString(Strings.Localize("programming.fullScreen")));
                    shared.sequentialHitBox.Set(new Vector2(246, 554), new Vector2(246, 554) + Font().MeasureString(Strings.Localize("programming.thoughtBalloonSequential")));
                    shared.randomHitBox.Set(new Vector2(246, 594), new Vector2(246, 594) + Font().MeasureString(Strings.Localize("programming.thoughtBalloonRandom")));
                }
                else if (shared.IsTargetModeLabel)
                {
                    //For the label mode, invalidate the hit boxes for these options.
                    shared.fullscreenHitBox.Set(new Vector2(-1, -1), new Vector2(-1, -1));
                    shared.sequentialHitBox.Set(new Vector2(-1, -1), new Vector2(-1, -1));
                    shared.randomHitBox.Set(new Vector2(-1, -1), new Vector2(-1, -1));
                }
                else
                {
                    shared.fullscreenHitBox.Set(new Vector2(246, 534), new Vector2(246, 534) + Font().MeasureString(Strings.Localize("programming.triggerAtBeginning")));
                    shared.sequentialHitBox.Set(new Vector2(246, 574), new Vector2(246, 574) + Font().MeasureString(Strings.Localize("programming.triggerAtEnd")));
                    shared.randomHitBox.Set(new Vector2(-1, -1), new Vector2(-1, -1));
                }

                // Add left stick if needed.
                if (shared.blob.NumLines >= shared.textVisibleLines)
                {
                    pos = new Vector2(142, 380);
                    ssquad.Render(leftStick, pos, new Vector2(leftStick.Width, leftStick.Height), "TexturedRegularAlpha");

                    shared.scrollHitBox.Set(pos, pos + new Vector2(leftStick.Width, leftStick.Height));
                }
                else
                {
                    shared.scrollHitBox.Set(Vector2.Zero, Vector2.Zero);
                }

                //If in label edit mode, hide these buttons and invalidate the hit boxes.
                //All other modes is the contrary. 
                if (shared.IsTargetModeLabel)
                {
                    shared.leftJustifyHitBox.Set(new Vector2(-1, -1), new Vector2(-1, -1));
                    shared.centerJustifyHitBox.Set(new Vector2(-1, -1), new Vector2(-1, -1));
                    shared.rightJustifyHitBox.Set(new Vector2(-1, -1), new Vector2(-1, -1));
                }
                else
                {
                    // Text justification buttons.
                    // left
                    Vector4 color = Vector4.One;
                    min = new Vector2(760, 510);
                    max = min + new Vector2(32, 32);
                    shared.leftJustifyHitBox.Set(min, max);
                    color = shared.blob.Justification == TextHelper.Justification.Left ? greenTextColor.ToVector4() : whiteTextColor.ToVector4();
                    ssquad.Render(leftJustifyTexture, color, min, new Vector2(32, 32), "TexturedRegularAlpha");

                    // center
                    min.X += 36;
                    max.X += 36;
                    shared.centerJustifyHitBox.Set(min, max);
                    color = shared.blob.Justification == TextHelper.Justification.Center ? greenTextColor.ToVector4() : whiteTextColor.ToVector4();
                    ssquad.Render(centerJustifyTexture, color, min, new Vector2(32, 32), "TexturedRegularAlpha");

                    // right
                    min.X += 36;
                    max.X += 36;
                    shared.rightJustifyHitBox.Set(min, max);
                    color = shared.blob.Justification == TextHelper.Justification.Right ? greenTextColor.ToVector4() : whiteTextColor.ToVector4();
                    ssquad.Render(rightJustifyTexture, color, min, new Vector2(32, 32), "TexturedRegularAlpha");
                }
                /*
                // Debug display of mouse hit boxes.
                ssquad.Render(new Vector4(1, 0, 0, 0.5f), shared.aHitBox.Min, shared.aHitBox.Max - shared.aHitBox.Min);
                ssquad.Render(new Vector4(1, 0, 0, 0.5f), shared.bHitBox.Min, shared.bHitBox.Max - shared.bHitBox.Min);
                ssquad.Render(new Vector4(1, 0, 0, 0.5f), shared.yHitBox.Min, shared.yHitBox.Max - shared.yHitBox.Min);
                ssquad.Render(new Vector4(1, 0, 0, 0.5f), shared.scrollHitBox.Min, shared.scrollHitBox.Max - shared.scrollHitBox.Min);

                ssquad.Render(new Vector4(1, 0, 0, 0.5f), shared.fullscreenHitBox.Min, shared.fullscreenHitBox.Max - shared.fullscreenHitBox.Min);
                ssquad.Render(new Vector4(1, 0, 0, 0.5f), shared.sequentialHitBox.Min, shared.sequentialHitBox.Max - shared.sequentialHitBox.Min);
                ssquad.Render(new Vector4(1, 0, 0, 0.5f), shared.randomHitBox.Min, shared.randomHitBox.Max - shared.randomHitBox.Min);
                */

                InGame.RestoreRenderTarget();

                device.Clear(ClearOptions.DepthBuffer | ClearOptions.Target, new Color(20, 20, 20), 1.0f, 0);
                // Start by using the blurred version of the scene as a backdrop.
                // If the thumbnail is no longer valid, just use black.
                if (!shared.thumbnail.GraphicsDevice.IsDisposed && !shared.thumbnail.IsDisposed)
                {
                    ssquad.Render(shared.thumbnail, BokuGame.ScreenPosition, BokuGame.ScreenSize, @"TexturedNoAlpha");
                }

                // Copy the rendered scene to the screen.
                // Calc scaling.
                shared.renderPosition = BokuGame.ScreenPosition;
                shared.renderSize = new Vector2(rtFull.Width, rtFull.Height);
                Vector2 scale = BokuGame.ScreenSize / shared.renderSize;
                shared.renderScale.X = Math.Min(Math.Min(scale.X, scale.Y), 1.0f);
                shared.renderScale.Y = shared.renderScale.X;
                shared.renderSize *= shared.renderScale;

                // Center the position.
                shared.renderPosition = BokuGame.ScreenPosition + (BokuGame.ScreenSize - shared.renderSize) / 2.0f;

                ssquad.Render(rtFull, shared.renderPosition, shared.renderSize, @"TexturedRegularAlpha");

            }   // end of TextEditor RenderObj Render()

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
                    backgroundTexture = KoiLibrary.LoadTexture2D(@"Textures\TextEditor\TextEditorBackground");
                }

                if (leftStick == null)
                {
                    leftStick = KoiLibrary.LoadTexture2D(@"Textures\HelpCard\leftStick");
                }

                if (leftJustifyTexture == null)
                {
                    leftJustifyTexture = KoiLibrary.LoadTexture2D(@"Textures\TextEditor\LeftJustify");
                }

                if (centerJustifyTexture == null)
                {
                    centerJustifyTexture = KoiLibrary.LoadTexture2D(@"Textures\TextEditor\CenterJustify");
                }

                if (rightJustifyTexture == null)
                {
                    rightJustifyTexture = KoiLibrary.LoadTexture2D(@"Textures\TextEditor\RightJustify");
                }

            }   // end of InitDeviceResources()

            public void UnloadContent()
            {
                DeviceResetX.Release(ref backgroundTexture);
                DeviceResetX.Release(ref leftStick);
                DeviceResetX.Release(ref leftJustifyTexture);
                DeviceResetX.Release(ref centerJustifyTexture);
                DeviceResetX.Release(ref rightJustifyTexture);
            }   // end of TextEditor RenderObj UnloadContent()

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
            }

            #endregion

        }   // end of class TextEditor RenderObj     

        #region Members

        public static TextEditor Instance = null;

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
        public TextEditor()
        {
            TextEditor.Instance = this;

            shared = new Shared(this);

            // Create the RenderObject and UpdateObject parts of this mode.
            updateObj = new UpdateObj(this, shared);
            renderObj = new RenderObj(shared);

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
                // HACK -- In the programming editor if the cursor is on one tile and the user
                // clicks on another tile which happens to be the "say" tile the cursor is moved
                // and then the text editor is activated.  The problem is that because of the 
                // delayed refresh the command stack gets out of sync.  What happens is that the 
                // commandMap for the text editor is pushed onto the stack and then the next frame
                // the old tile is deactivated and the new tile activated.  This leaves the 
                // ReflexCard CommandMap at the top of the stack instead of the TextEditor.
                // So, detect and apply the bandaid.
                if (CommandStack.Peek().name == "ReflexCard")
                {
                    // Pull the text editor command map to the top.  Note that this
                    // only works because the CommandStack isn't really  a stack.  :-)
                    CommandStack.Pop(commandMap);
                    CommandStack.Push(commandMap);
                }

                updateObj.Update();
            }
        }   // end of Update()

        public void Render(Camera camera)
        {
            if (Active)
            {
                renderObj.Render(camera);
            }
        }   // end of Render()

        #endregion

        #region Internal

        override public void Activate()
        {
            Debug.Assert(false, "Should this ever be activated through here?");
            Activate(null, null, false);
        }

        /// <summary>
        /// Activate the text editor.  Currently we have 3 valid 
        /// modes "say" for the 'say' actuator text, "said" for 
        /// the said filter text and "comment" for using the editor
        /// to comment on levels via Socl.  
        /// The targetMode determines where the resulting text is placed
        /// in the reflex and the options given to the user at the
        /// bottom of the edit box.
        /// </summary>
        /// <param name="reflexData"></param>
        /// <param name="targetMode"></param>
        /// <param name="useRtCoords">Assume rendering to RT for hit testing.</param>
        public void Activate(ReflexData reflexData, string targetMode, bool useRtCoords)
        {
            shared.targetMode = targetMode;
            shared.useRtCoords = useRtCoords;

            if (reflexData == null)
                return;

            if (targetMode != "say" && targetMode != "said")
            {
                Debug.Assert(false, "Unknown text editor targetMode");
                return;
            }

            shared.reflexData = reflexData;

            if (state != States.Active)
            {

                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Push(commandMap);

                state = States.Active;

                // Get the current scene thumbnail.
                shared.thumbnail = InGame.inGame.SmallThumbNail;

                // Tell InGame we're using the thumbnail so no need to do full render.
                InGame.inGame.RenderWorldAsThumbnail = true;

                HelpOverlay.Push(@"TextEditor");

                // Get current state from reflex.
                if (targetMode == "say")
                {
                    shared.blob = new TextBlob(renderObj.Font, "", shared.textWidth);
                    shared.blob.Justification = TextHelper.Justification.Left;
                }
                else
                {
                    shared.blob = new TextBlob(renderObj.Font, reflexData.saidString, shared.textWidth);
                    shared.blob.Justification = reflexData.saidJustification;
                    shared.mode = reflexData.saidMode;
                }

                shared.blob.End();

                shared.topLine = 0;
                shared.textOffset = 0;

                updateObj.Activate();
            }
        }   // end of Activate

        public void StartEditLabel(OnButtonLabelEditDone doneCallback, string original)
        {
            if (null != doneCallback && state != States.Active)
            {
                shared.onButtonLabelEditDone = doneCallback;
                shared.targetMode = kTM_TouchButtonLabel;

                //------------------ Copied from other implementation --------------
                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Push(commandMap);

                // Get the current scene thumbnail.
                shared.thumbnail = InGame.inGame.SmallThumbNail;

                // Tell InGame we're using the thumbnail so no need to do full render.
                InGame.inGame.RenderWorldAsThumbnail = true;

                HelpOverlay.Push(@"TextEditor");

                //------------------ Copied from other implementation END --------------

                state = States.Active;

                shared.blob = new TextBlob(renderObj.Font, (null != original) ? original : "", shared.textWidth);
                shared.blob.Justification = TextHelper.Justification.Left;
                shared.blob.End();

                shared.mode = 0;
                shared.topLine = 0;
                shared.textOffset = 0;



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

                shared.reflexData = null;

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

    }   // end of class TextEditor

}   // end of namespace Boku
