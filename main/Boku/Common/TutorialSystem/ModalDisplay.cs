
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

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
using Boku.Common;
using Boku.Common.Xml;
using Boku.Input;
using Boku.UI2D;

namespace Boku.Common.TutorialSystem
{
    public class ModalDisplay : INeedsDeviceReset
    {
        public delegate void OnPress();

        #region Members

        private enum States
        {
            Inactive,
            Active,
        }
        private States state = States.Inactive;

        private CommandMap commandMap = new CommandMap("ModalDisplay");

        private OnPress OnContinue = null;
        private OnPress OnBack = null;
        private OnPress OnExitTutorial = null;

        public Texture2D backgroundTexture = null;      // The background tiles we render over.
        public Texture2D leftStick = null;

        public Camera camera = null;
        public Texture2D thumbnail = null;          // Scene image for background.
        public bool useBackgroundThumbnail = true;  // Use scene image for background unless this is false.
        private bool prevRenderWorldAsThumbnail = false;

        // Text to display depending on input mode.
        private string gamepadText = "";
        private string mouseText = "";
        private string touchText = "";

        public TextBlob blob = null;
        private Vector2 displayPosition;

        private Button continueButton = null;
        private Button backButton = null;
        private Button exitTutorialButton = null;

        private AABB2D upBox = new AABB2D();    // Up/down arrows for scrolling text.
        private AABB2D downBox = new AABB2D();
        private bool useRtCoords = false;       // Rendering to a rendertarget instead of the backbuffer?

        // Color constants.
        private Color lightTextColor = new Color(191, 191, 191);
        private Color hoverTextColor = new Color(50, 255, 50);

        public int topLine = 0;             // Which line of the text is being shown at the default starting position.
        public int textOffset = 0;          // Vertical offset (in pixels) for beginning of text.
        public int textTop = 80;            // Magic numbers all determined by pushing stuff around in Photoshop.
        public int textMargin = 80;
        public int textWidth = 880;

        public int textVisibleLines = 11;   // How many lines can we see in the window?

        private Vector2 renderPosition;     // Where to render the rendertarget on the backbuffer.
        private float renderScale;          // Scale used to shrink rendertarget if needed.  Should always be <= 1.0.

        #endregion

        #region Accessors

        private GetFont Font
        {
            get { return SharedX.GetGameFont20; }
        }

        public bool Active
        {
            get { return (state == States.Active); }
        }

        public bool UseBackgroundThumbnail
        {
            get { return useBackgroundThumbnail; }
        }

        #endregion

        #region Public

        // c'tor
        public ModalDisplay(OnPress OnContinue, OnPress OnBack, OnPress OnExitTutorial)
        {
            this.OnContinue = OnContinue;
            this.OnBack = OnBack;
            this.OnExitTutorial = OnExitTutorial;

            // We're rendering the text into a 1024x768 rt and then composing that with
            // the background elements into another 1024x768 rt.  This is then scaled
            // and rendered onto the backbuffer.
            camera = new PerspectiveUICamera();
            camera.Resolution = new Point(1024, 768);

            {
                GetTexture getTexture = delegate() { return ButtonTextures.AButton; };
                continueButton = new Button(Strings.Localize("tutorial.continue"), Color.White, getTexture, SharedX.GetGameFont20);
            }
            {
                GetTexture getTexture = delegate() { return ButtonTextures.BButton; };
                backButton = new Button(Strings.Localize("tutorial.back"), Color.White, getTexture, SharedX.GetGameFont20);
            }
            {
                GetTexture getTexture = delegate() { return ButtonTextures.XButton; };
                exitTutorialButton = new Button(Strings.Localize("tutorial.exitTutorial"), Color.White, getTexture, SharedX.GetGameFont20);
            }


        }   // end of c'tor

        public void Update()
        {
            if (Active)
            {
                // Due to command stack funkiness, need to bring the command map to top.
                if (CommandStack.Peek() != commandMap)
                {
                    CommandStack.Pop(commandMap);
                    CommandStack.Push(commandMap);
                }

                GamePadInput pad = GamePadInput.GetGamePad0();

                if (Actions.Select.WasPressed)
                {
                    Actions.Select.ClearAllWasPressedState();

                    if (OnContinue != null)
                    {
                        OnContinue();
                    }
                    Deactivate();
                    return;
                }

                /*
                if (Actions.Cancel.WasPressed)
                {
                    Actions.Cancel.ClearAllWasPressedState();

                    if (OnBack != null)
                    {
                        OnBack();
                    }
                    Deactivate();
                    return;
                }
                */

                if (Actions.X.WasPressed)
                {
                    Actions.X.ClearAllWasPressedState();

                    if (OnExitTutorial != null)
                    {
                        OnExitTutorial();
                    }
                    Deactivate();
                    return;
                }

                // Change text?
                if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
                {
                    if (blob.RawText != mouseText)
                    {
                        blob.RawText = mouseText;
                    }
                }
                else if (KoiLibrary.LastTouchedDeviceIsGamepad)
                {
                    if (blob.RawText != gamepadText)
                    {
                        blob.RawText = gamepadText;
                    }
                }
                else if (KoiLibrary.LastTouchedDeviceIsTouch)
                {
                    if (blob.RawText != touchText)
                    {
                        blob.RawText = touchText;
                    }
                }

                // Scroll text???
                if (blob.NumLines != 0)
                {
                    int scroll = LowLevelMouseInput.DeltaScrollWheel;

                    if (Actions.Up.WasPressedOrRepeat || scroll > 0)
                    {
                        ScrollDown();
                    }

                    if (Actions.Down.WasPressedOrRepeat || scroll < 0)
                    {
                        ScrollUp();
                    }

                    // If we're not shutting down...
                    if (Active)
                    {
                    }   // end if not shutting down.
                }

                // We should be on top and owning all input
                // focus so don't let anthing trickle down.
                GamePadInput.ClearAllWasPressedState();

                // Disable the help overlay's tool icon because in some situations
                // it can overlap the text making it unreadable.
                HelpOverlay.ToolIcon = null;

                // If active we need to pre-render the text to the 1k rendertarget since
                // changing render targets on the Xbox forces a resolve.
                PreRender();

                Vector2 hit;
                if (KoiLibrary.LastTouchedDeviceIsTouch)
                {
                    TouchContact touch = TouchInput.GetOldestTouch();
                    if (touch != null)
                    {
                        Vector2 touchHit = touch.position;

                        // Adjust for position and scaling of final rendering.
                        touchHit -= renderPosition;
                        touchHit /= renderScale;

                        HandleTouchInput(touch, touchHit);
                    }
                    else
                    {
                        continueButton.ClearState();
                        backButton.ClearState();
                        exitTutorialButton.ClearState();
                    }
                }
                else if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
                {
                    // Consider adjusting modal dialog size to take overscan into account BUT NOT tutorial mode.
                    hit = LowLevelMouseInput.PositionVec;

                    // Adjust for position and scaling of final rendering.
                    hit -= renderPosition;
                    hit /= renderScale;

                    HandleMouseInput(hit);
                }

            }   // end if active.

        }   // end of Update()

        private void HandleTouchInput(TouchContact touch, Vector2 hit)
        {
            continueButton.SetHoverState(hit);
            backButton.SetHoverState(hit);
            exitTutorialButton.SetHoverState(hit);

            if (continueButton.Box.Contains(hit))
            {
                if (touch.phase == TouchPhase.Ended)
                {
                    if (OnContinue != null)
                    {
                        OnContinue();
                    }
                    Deactivate();
                }
            }
            else if (exitTutorialButton.Box.Contains(hit))
            {
                if (touch.phase == TouchPhase.Ended)
                {
                    if (OnExitTutorial != null)
                    {
                        OnExitTutorial();
                    }
                    Deactivate();
                }
            }
            else if (upBox.Contains(hit))
            {
                if (touch.phase == TouchPhase.Ended)
                {
                    ScrollDown();
                }
            }
            else if (downBox.Contains(hit))
            {
                if (touch.phase == TouchPhase.Ended)
                {
                    ScrollUp();
                }
            }

        }   // end of HandleTouchInput()

        private void HandleMouseInput(Vector2 hit)
        {
            // Hovering?
            continueButton.SetHoverState(hit);
            backButton.SetHoverState(hit);
            exitTutorialButton.SetHoverState(hit);

            if (continueButton.Box.LeftPressed(hit))
            {
                if (OnContinue != null)
                {
                    OnContinue();
                }
                Deactivate();
            }
            /*
            else if (backButton.Box.LeftPressed(hit))
            {
                if (OnBack != null)
                {
                    OnBack();
                }
                Deactivate();
            }
            */
            else if (exitTutorialButton.Box.LeftPressed(hit))
            {
                if (OnExitTutorial != null)
                {
                    OnExitTutorial();
                }
                Deactivate();
            }
            else if (upBox.LeftPressed(hit))
            {
                ScrollDown();
            }
            else if (downBox.LeftPressed(hit))
            {
                ScrollUp();
            }

        }   // end of HandleMouseInput()

        private void PreRender()
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            RenderTarget2D rt1k = SharedX.RenderTarget1024_768;

            CameraSpaceQuad csquad = CameraSpaceQuad.GetInstance();
            ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();

            Color darkTextColor = new Color(20, 20, 20);
            Color greyTextColor = new Color(127, 127, 127);
            Color greenTextColor = new Color(8, 123, 110);
            Color whiteTextColor = new Color(255, 255, 255);

            // Render the text into the 1k rendertarget.
            InGame.SetRenderTarget(rt1k);
            InGame.Clear(Color.Transparent);

            // Set up params for rendering UI with this camera.
            BokuGame.bokuGame.shaderGlobals.SetCamera(camera);

            //
            // Text.
            //

            // If we don't have enough text to go into scrolling, center vertically.
            int centering = 0;
            if (blob.NumLines < textVisibleLines)
            {
                centering += (int)(blob.TotalSpacing * (textVisibleLines - blob.NumLines) / 2.0f);
            }

            Vector2 pos;
            pos = new Vector2(textMargin, textTop + textOffset + centering);
            blob.RenderText(null, pos, darkTextColor);

            InGame.RestoreRenderTarget();

        }   // end of PreRender()

        public void Render()
        {
            if (Active)
            {
                GraphicsDevice device = KoiLibrary.GraphicsDevice;

                RenderTarget2D rtFull = SharedX.RenderTargetDepthStencil1024_768;   // Rendertarget we render whole display into.
                RenderTarget2D rt1k = SharedX.RenderTarget1024_768;

                Vector2 rtSize = new Vector2(rtFull.Width, rtFull.Height);

                CameraSpaceQuad csquad = CameraSpaceQuad.GetInstance();
                ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();

                Color darkTextColor = new Color(20, 20, 20);
                Color greyTextColor = new Color(127, 127, 127);
                Color greenTextColor = new Color(8, 123, 110);
                Color whiteTextColor = new Color(255, 255, 255);

                // Render the scene to our rendertarget.
                InGame.SetRenderTarget(rtFull);

                // Clear to transparent.
                InGame.Clear(Color.Transparent);

                // Set up params for rendering UI with this camera.
                BokuGame.bokuGame.shaderGlobals.SetCamera(camera);

                Vector2 pos;

                // Now render the background tiles.
                Vector2 backgroundSize = new Vector2(backgroundTexture.Width, backgroundTexture.Height);
                pos = (rtSize - backgroundSize) / 2.0f;
                ssquad.Render(backgroundTexture, pos, backgroundSize, @"TexturedRegularAlpha");

                displayPosition = pos;

                // Now render the contents of the rt1k texture but with the edges blended using the mask.
                Vector2 rt1kSize = new Vector2(rt1k.Width, rt1k.Height);
                pos -= new Vector2(40, 70);
                try//minimize bug fix.
                {
                    Vector4 limits = new Vector4(0.095f, 0.112f, 0.57f, 0.64f);
                    ssquad.RenderWithYLimits(rt1k, limits, pos, rt1kSize, @"TexturedPreMultAlpha");
                }
                catch
                {
                    return;
                }

                //
                // Render buttons.
                //

                float margin = 64.0f;
                float totalWidth = continueButton.GetSize().X + /* margin + backButton.GetSize().X + */ margin + exitTutorialButton.GetSize().X;
                pos = new Vector2(rtSize.X / 2.0f, rtSize.Y / 2.0f + backgroundSize.Y * 0.28f);
                pos.X -= totalWidth / 2.0f;

                SpriteBatch batch = KoiLibrary.SpriteBatch;
                batch.Begin();

                continueButton.Render(pos);
                pos.X += continueButton.GetSize().X + margin;

                /*
                backButton.Render(pos);
                pos.X += backButton.GetSize().X + margin;
                */

                exitTutorialButton.Render(pos);

                batch.End();

                // Add left stick if needed.
                Vector2 min;
                Vector2 max;
                if (blob.NumLines >= textVisibleLines)
                {
                    pos = displayPosition + new Vector2(-31, 300);
                    ssquad.Render(leftStick, pos, new Vector2(leftStick.Width, leftStick.Height), "TexturedRegularAlpha");
                    min = pos;
                    max = min + new Vector2(leftStick.Width, leftStick.Height / 2.0f);
                    upBox.Set(min, max);
                    min.Y = max.Y;
                    max.Y += leftStick.Height / 2.0f;
                    downBox.Set(min, max);
                }

                InGame.RestoreRenderTarget();
                InGame.SetViewportToScreen();

                // No put it all together.
                // Start with the background.
                if (useBackgroundThumbnail)
                {
                    if (!thumbnail.GraphicsDevice.IsDisposed && !thumbnail.IsDisposed)
                    {
                        // Render the blurred thumbnail (if valid) full screen.
                        if (!thumbnail.GraphicsDevice.IsDisposed)
                        {
                            InGame.RestoreViewportToFull();
                            Vector2 screenSize = new Vector2(device.Viewport.Width, device.Viewport.Height);
                            ssquad.Render(thumbnail, Vector2.Zero, screenSize, @"TexturedNoAlpha");
                            InGame.SetViewportToScreen();
                        }
                    }
                    else
                    {
                        // No valid thumbnail, clear to dark.
                        device.Clear(darkTextColor);
                    }
                }

                // Calc scaling and position for rt.
                renderPosition = Vector2.Zero;
                renderScale = 1.0f;

                // The part of the dialog we care about is 1024x600 so we want to use
                // that as the size to fit to the screen.
                Vector2 dialogSize = new Vector2(1024, 600);
                Vector2 scale = BokuGame.ScreenSize / dialogSize;
                renderScale = Math.Min(Math.Min(scale.X, scale.Y), 1.0f);
                Vector2 renderSize = rt1kSize * renderScale;

                // Center on screen.
                renderPosition = (BokuGame.ScreenSize - renderSize) / 2.0f;

                ssquad.Render(rtFull, renderPosition, renderSize, @"TexturedRegularAlpha");

            }
        }   // end of ScrollableTextDisplay Render()

        #endregion

        #region Internal

        private void ScrollDown()
        {
            if (topLine > 0)
            {
                --topLine;
                TwitchTextOffset();

                // This scroll may have moved the cursor off screen, if so
                // move the cursor so that it's back on screen.
                int line = 0;
                int curPos = 0;
                blob.FindCursorLineAndPosition(out line, out curPos);
                if (line >= topLine + textVisibleLines)
                {
                    blob.CursorUp();
                }
            }
        }   // end of ScrollDown()

        private void ScrollUp()
        {
            int numLines = blob.NumLines;
            if (numLines - textVisibleLines > topLine)
            {
                ++topLine;
                TwitchTextOffset();

                // This scroll may have moved the cursor off screen, if so
                // move the cursor so that it's back on screen.
                int line = 0;
                int curPos = 0;
                blob.FindCursorLineAndPosition(out line, out curPos);
                if (line < topLine)
                {
                    blob.CursorDown();
                }
            }
        }   // end of ScrollUp()

        private void TwitchTextOffset()
        {
            // Start a twitch to move the text text offset.
            TwitchManager.Set<float> set = delegate(float val, Object param) { textOffset = (int)val; };
            TwitchManager.CreateTwitch<float>(textOffset, -topLine * Font().LineSpacing, set, 0.2f, TwitchCurve.Shape.OvershootOut);
        }   // end of TwitchTextOffset()

        public void LoadContent(bool immediate)
        {
        }   // end of ScrollableTextDisplay LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
            if (backgroundTexture == null)
            {
                backgroundTexture = KoiLibrary.LoadTexture2D(@"Textures\TextEditor\TextDisplayBackground");
            }

            if (leftStick == null)
            {
                leftStick = KoiLibrary.LoadTexture2D(@"Textures\HelpCard\LeftStick");
            }
        }   // end of ScrollableTextDisplay InitDeviceResources()

        public void UnloadContent()
        {
            DeviceResetX.Release(ref backgroundTexture);
            DeviceResetX.Release(ref leftStick);
        }   // end of ScrollableTextDisplay UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
        }

        public void Activate(string gamepadText, string mouseText, string touchText, bool useBackgroundThumbnail, bool useOverscanForHitTesting)
        {
            this.gamepadText = gamepadText;
            this.mouseText = mouseText;
            this.touchText = touchText;

            if (gamepadText == null && mouseText == null && touchText == null)
            {
                Debug.Assert(false, "What are you trying to do?");
                return;
            }

            if (state != States.Active)
            {
                this.useBackgroundThumbnail = useBackgroundThumbnail;
                this.useRtCoords = useOverscanForHitTesting;

                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Push(commandMap);

                state = States.Active;

                // Get the current scene thumbnail.  If we're using this from the main menu (options)
                // then use the title screen image instead.
                if (InGame.inGame.State == InGame.States.Inactive && false)
                {
                    //thumbnail = BokuGame.bokuGame.mainMenu.BackgroundTexture;
                }
                else
                {
                    thumbnail = InGame.inGame.SmallThumbNail;
                }

                // Tell InGame we're using the thumbnail so no need to do full render.
                prevRenderWorldAsThumbnail = InGame.inGame.RenderWorldAsThumbnail;
                if (!prevRenderWorldAsThumbnail)
                {
                    InGame.inGame.RenderWorldAsThumbnail = true;
                }
                Time.Paused = true;

                HelpOverlay.Push(@"ScrollableTextDisplay");

                // Get text string.
                blob = new TextBlob(SharedX.GetGameFont20, mouseText, textWidth);
                blob.LineSpacingAdjustment = 6; // Taller lines to make programming tiles fit better.

                topLine = 0;
                textOffset = 0;

                PreRender();    // Set up text rendering for first frame.
            }
        }   // end of Activate

        public void Deactivate()
        {
            if (state != States.Inactive)
            {
                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Pop(commandMap);

                state = States.Inactive;

                HelpOverlay.Pop();

                // Turn off thumbnail rendering if needed.
                if (!prevRenderWorldAsThumbnail)
                {
                    InGame.inGame.RenderWorldAsThumbnail = false;
                }
                Time.Paused = false;
            }
        }

        #endregion

    }   // end of class ModalDisplay

}   // end of namespace Boku.Common.TutorialSystem
