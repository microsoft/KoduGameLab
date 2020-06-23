
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
    /// Modal text display.  Used for the 'say' verb when 'fullscreen' is checked.
    /// </summary>
    public class ScrollableTextDisplay : INeedsDeviceReset
    {
        #region Members

        private enum States
        {
            Inactive,
            Active,
        }
        private States state = States.Inactive;
        private string rawText = null;
        private GameActor thinker = null;

        private CommandMap commandMap = new CommandMap("TextDisplay");

        public Texture2D backgroundTexture = null;      // The background tiles we render over.
        public Texture2D leftStick = null;

        public Camera camera = null;
        public Texture2D thumbnail = null;          // Scene image for background.
        public bool useBackgroundThumbnail = true;  // Use scene image for background unless this is false.
        private bool prevRenderWorldAsThumbnail = false;

        public TextBlob blob = null;
        private Vector2 displayPosition;
        private AABB2D hitBoxA = new AABB2D();  // Mouse hit region for <A> Continue.
        private AABB2D hitBoxB = new AABB2D();  // Mouse hit region for <B>.
        private AABB2D upBox = new AABB2D();    // Up/down arrows for scrolling text.
        private AABB2D downBox = new AABB2D();
        private bool useRtCoords = false;      // Rendering to an RT?

        // Text color for button label.
        private Color labelAColor = new Color(191, 191, 191);
        private Color labelBColor = new Color(191, 191, 191);
        // Color targetted by the twitch.  Used for comparisons so
        // we know whether or not to start a new twitch.
        private Color labelATargetColor = Color.Gray;
        private Color labelBTargetColor = Color.Gray;

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

        public string textB = "";

        public bool userHitA = false;       // Used to let system know what user hit when exiting.  Hack.

        private bool dirty = true;          // Text RT needs to be re-rendered.

        #endregion

        #region Accessors

        private UI2D.Shared.GetFont Font
        {
            get { return UI2D.Shared.GetGameFont20; }
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
        public ScrollableTextDisplay()
        {
            // We're rendering the text into a 1024x768 rt and then composing that with
            // the background elements into another 1024x768 rt.  This is then scaled
            // and rendered onto the backbuffer.
            camera = new PerspectiveUICamera();
            camera.Resolution = new Point(1024, 768);

        }   // end of c'tor

        public void Update(Camera camera)
        {
            if (Active)
            {

                GamePadInput pad = GamePadInput.GetGamePad0();

#if !NETFX_CORE
                // For games using micro:bit, allow buttons to dismiss ingame dialogs.
                if (InGame.inGame.CurrentUpdateMode == InGame.UpdateMode.RunSim)
                {
                    Microbit bit = MicrobitExtras.GetMicrobitOrNull(GamePadSensor.PlayerId.All);
                    if (bit != null)
                    {
                        // Allow either button to dismiss display.
                        if (bit.State.ButtonA.IsPressed() || bit.State.ButtonB.IsPressed())
                        {
                            Deactivate();
                        }
                    }
                }
#endif

                if (Actions.Select.WasPressed)
                {
                    Actions.Select.ClearAllWasPressedState();

                    Deactivate();
                }

                if (!string.IsNullOrEmpty(textB))
                {
                    if (Actions.B.WasPressed)
                    {
                        Actions.B.ClearAllWasPressedState();

                        userHitA = false;
                        Deactivate();
                    }
                }

                // We need to be able to slip out to the mini-hub here since
                // continuous, repeated calls to TextDisplay can lock the 
                // user out of control.
                if (Actions.MiniHub.WasPressed)
                {
                    Actions.MiniHub.ClearAllWasPressedState();

                    Deactivate();
                    InGame.inGame.SwitchToMiniHub();
                }

                // We need to be able to slip out to the tool menu here since
                // continuous, repeated calls to TextDisplay can lock the 
                // user out of control.
                if (Actions.ToolMenu.WasPressed)
                {
                    Actions.ToolMenu.ClearAllWasPressedState();

                    Deactivate();
                    if (InGame.inGame.State == InGame.States.Active)
                    {
                        InGame.inGame.CurrentUpdateMode = InGame.UpdateMode.ToolMenu;
                    }
                }

                // Scroll text???
                if (blob.NumLines != 0)
                {
                    int scroll = MouseInput.ScrollWheel - MouseInput.PrevScrollWheel;

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
                if (dirty)
                {
                    PreRender();
                    dirty = false;
                }

                // If we're rendering this into a 1280x720 rt we need a matching camera to calc mouse hits.
                if (useBackgroundThumbnail)
                {
                    camera = new PerspectiveUICamera();
                    camera.Resolution = new Point(1280, 720);
                }

                if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                {
                    for (int i = 0; i < TouchInput.TouchCount; i++)
                    {
                        TouchContact touch = TouchInput.GetTouchContactByIndex(i);

                        Vector2 touchHit = touch.position;

                        // Adjust for position and scaling of final rendering.
                        touchHit -= renderPosition;
                        touchHit /= renderScale;

                        if (useRtCoords)
                        {
                            touchHit = ScreenWarp.ScreenToRT(touch.position);
                        }
                        HandleTouchInput(touch, touchHit);
                    }
                }
                else if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                {
                    Vector2 hit = MouseInput.PositionVec;

                    // Adjust for position and scaling of final rendering.
                    hit -= renderPosition;
                    hit /= renderScale;

                    if (useRtCoords)
                    {
                        //hit = MouseInput.GetMouseInRtCoords();
                    }
                    HandleMouseInput(hit);
                }

            }   // end if active.

        }   // end of Update()

        // Used for scrolling.
        float accumulatedTouchInput = 0;
        float prevTouchY = 0;

        private void HandleTouchInput(TouchContact touch, Vector2 hit)
        {
            if (hitBoxA.Touched(touch, hit))
            {
                userHitA = true;
                Deactivate();
            }
            else if (hitBoxB.Touched(touch, hit))
            {
                userHitA = false;
                Deactivate();
            }
            else if (upBox.Touched(touch, hit))
            {
                ScrollDown();
            }
            else if (downBox.Touched(touch, hit))
            {
                ScrollUp();
            } 
            else
            {
                // Touch is active, but none of the buttons were hit so assume user is trying to scroll text.
                if (touch.phase == TouchPhase.Began)
                {
                    prevTouchY = touch.position.Y;
                }
                if (touch.phase == TouchPhase.Moved)
                {
                    // Note we calc the delta ourselves since the TouchInput code
                    // may return the TouchContact for multiple frames.
                    float delta = touch.position.Y - prevTouchY;

                    // Adjust for screen / rt ratio.
                    Vector2 ratio = TouchInput.GetWinRTRatio(camera);
                    delta /= ratio.Y;

                    accumulatedTouchInput += delta;
                    prevTouchY = touch.position.Y;
                    if (accumulatedTouchInput > blob.TotalSpacing / 2)
                    {
                        accumulatedTouchInput -= blob.TotalSpacing;
                        ScrollDown();
                    }
                    else if (accumulatedTouchInput < -blob.TotalSpacing / 2)
                    {
                        accumulatedTouchInput += blob.TotalSpacing;
                        ScrollUp();
                    }
                }
            }

            // Check for hover and adjust text color to match.
            Color newColor;

            newColor = hitBoxA.Contains(hit) ? hoverTextColor : lightTextColor;
            if (newColor != labelATargetColor)
            {
                labelATargetColor = newColor;
                Vector3 curColor = new Vector3(labelAColor.R / 255.0f, labelAColor.G / 255.0f, labelAColor.B / 255.0f);
                Vector3 destColor = new Vector3(newColor.R / 255.0f, newColor.G / 255.0f, newColor.B / 255.0f);

                TwitchManager.Set<Vector3> set = delegate(Vector3 value, Object param)
                {
                    labelAColor.R = (byte)(value.X * 255.0f + 0.5f);
                    labelAColor.G = (byte)(value.Y * 255.0f + 0.5f);
                    labelAColor.B = (byte)(value.Z * 255.0f + 0.5f);
                };
                TwitchManager.CreateTwitch<Vector3>(curColor, destColor, set, 0.1f, TwitchCurve.Shape.EaseOut);
            }

            newColor = hitBoxB.Contains(hit) ? hoverTextColor : lightTextColor;
            if (newColor != labelBTargetColor)
            {
                labelBTargetColor = newColor;
                Vector3 curColor = new Vector3(labelBColor.R / 255.0f, labelBColor.G / 255.0f, labelBColor.B / 255.0f);
                Vector3 destColor = new Vector3(newColor.R / 255.0f, newColor.G / 255.0f, newColor.B / 255.0f);

                TwitchManager.Set<Vector3> set = delegate(Vector3 value, Object param)
                {
                    labelBColor.R = (byte)(value.X * 255.0f + 0.5f);
                    labelBColor.G = (byte)(value.Y * 255.0f + 0.5f);
                    labelBColor.B = (byte)(value.Z * 255.0f + 0.5f);
                };
                TwitchManager.CreateTwitch<Vector3>(curColor, destColor, set, 0.1f, TwitchCurve.Shape.EaseOut);
            }
        }   // end of HandleTouchInput()

        private void HandleMouseInput(Vector2 hit)
        {
            if (hitBoxA.LeftPressed(hit))
            {
                userHitA = true;
                Deactivate();
            }
            else if (hitBoxB.LeftPressed(hit))
            {
                userHitA = false;
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

            // Check for hover and adjust text color to match.
            Color newColor;

            newColor = hitBoxA.Contains(hit) ? hoverTextColor : lightTextColor;
            if (newColor != labelATargetColor)
            {
                labelATargetColor = newColor;
                Vector3 curColor = new Vector3(labelAColor.R / 255.0f, labelAColor.G / 255.0f, labelAColor.B / 255.0f);
                Vector3 destColor = new Vector3(newColor.R / 255.0f, newColor.G / 255.0f, newColor.B / 255.0f);

                TwitchManager.Set<Vector3> set = delegate(Vector3 value, Object param)
                {
                    labelAColor.R = (byte)(value.X * 255.0f + 0.5f);
                    labelAColor.G = (byte)(value.Y * 255.0f + 0.5f);
                    labelAColor.B = (byte)(value.Z * 255.0f + 0.5f);
                };
                TwitchManager.CreateTwitch<Vector3>(curColor, destColor, set, 0.1f, TwitchCurve.Shape.EaseOut);
            }

            newColor = hitBoxB.Contains(hit) ? hoverTextColor : lightTextColor;
            if (newColor != labelBTargetColor)
            {
                labelBTargetColor = newColor;
                Vector3 curColor = new Vector3(labelBColor.R / 255.0f, labelBColor.G / 255.0f, labelBColor.B / 255.0f);
                Vector3 destColor = new Vector3(newColor.R / 255.0f, newColor.G / 255.0f, newColor.B / 255.0f);

                TwitchManager.Set<Vector3> set = delegate(Vector3 value, Object param)
                {
                    labelBColor.R = (byte)(value.X * 255.0f + 0.5f);
                    labelBColor.G = (byte)(value.Y * 255.0f + 0.5f);
                    labelBColor.B = (byte)(value.Z * 255.0f + 0.5f);
                };
                TwitchManager.CreateTwitch<Vector3>(curColor, destColor, set, 0.1f, TwitchCurve.Shape.EaseOut);
            }
        }   // end of HandleMouseInput()

        /// <summary>
        /// Renders the dialog into a rendertarget.  This happens during Update() phase so
        /// that we don't need to swap rendertargets during the Render call.  This allows
        /// the dialog to be safely rendered into the scene when the scene itself is being
        /// rendered into a rendertarget.
        /// </summary>
        private void PreRender()
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            //
            // First, render the text into a rendertarget.
            //

            RenderTarget2D rt1k = UI2D.Shared.RenderTarget1024_768;

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
            Fx.ShaderGlobals.SetCamera(camera);

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

            int startLine = 0;
            if (textOffset < 0)
            {
                startLine = -(int)(textOffset / blob.TotalSpacing);
                pos.Y += startLine * blob.TotalSpacing;
            }

            // Clamp to pixel coords.
            pos.X = (int)pos.X;
            pos.Y = (int)pos.Y;
            blob.RenderWithButtons(pos, greyTextColor, startLine: startLine, maxLines: textVisibleLines + 2);

            InGame.RestoreRenderTarget();

            //
            // Second, render all the chrome with the text into the final rendertarget.

            RenderTarget2D rtFull = UI2D.Shared.RenderTargetDepthStencil1024_768;   // Rendertarget we render whole display into.

            // Render the scene to our rendertarget.
            InGame.SetRenderTarget(rtFull);

            // Clear to transparent.
            InGame.Clear(Color.Transparent);

            // Set up params for rendering UI with this camera.
            Fx.ShaderGlobals.SetCamera(camera);

            Vector2 rtSize = new Vector2(rtFull.Width, rtFull.Height);

            // Now render the background tiles.
            Vector2 backgroundSize = new Vector2(backgroundTexture.Width, backgroundTexture.Height);
            pos = (rtSize - backgroundSize) / 2.0f;
            // Clamp to pixel coords.
            pos.X = (int)pos.X;
            pos.Y = (int)pos.Y;
            ssquad.Render(backgroundTexture, pos, backgroundSize, @"TexturedRegularAlpha");

            displayPosition = pos;

            // Now render the contents of the rt1k texture but with the edges blended using the mask.
            Vector2 rt1kSize = new Vector2(rt1k.Width, rt1k.Height);
            pos -= new Vector2(40, 70);

            // Clamp to nearest pixel;
            pos.X = (int)pos.X;
            pos.Y = (int)pos.Y;

            try//minimize bug fix.
            {
                Vector4 limits = new Vector4(0.095f, 0.112f, 0.57f, 0.64f);
                ssquad.RenderWithYLimits(rt1k, limits, pos, rt1kSize, @"TexturedRegularAlpha");
            }
            catch
            {
                InGame.RestoreRenderTarget();
                return;
            }

            //
            // Add button icon with label.
            //

            SpriteBatch batch = UI2D.Shared.SpriteBatch;
            Vector2 min;    // Used to capture info for mouse hit boxes.
            Vector2 max;

            batch.Begin();
            {
                pos = new Vector2(rtSize.X / 2.0f, rtSize.Y / 2.0f + backgroundSize.Y * 0.28f);
                int buttonWidth = 48;
                pos.X -= (Font().MeasureString(Strings.Localize("textDialog.continue")).X + buttonWidth) / 2.0f;
                // Also account for B button if we have one.
                int margin = 24;
                if (!string.IsNullOrEmpty(textB))
                {
                    pos.X -= (Font().MeasureString(textB).X + buttonWidth + margin) / 2.0f;
                }
                min = pos;

                // Render the A button and it's label.
                ssquad.Render(ButtonTextures.AButton, pos, new Vector2(56, 56), @"TexturedRegularAlpha");
                pos.X += buttonWidth;
                max = new Vector2(pos.X + Font().MeasureString(Strings.Localize("textDialog.continue")).X, min.Y + buttonWidth);
                hitBoxA.Set(min, max);

                TextHelper.DrawString(Font, Strings.Localize("textDialog.continue"), pos, labelAColor);

                pos.X += Font().MeasureString(Strings.Localize("textDialog.continue")).X + margin;
                min = pos;

                // Render the B button and it's label.
                if (!string.IsNullOrEmpty(textB))
                {
                    ssquad.Render(ButtonTextures.BButton, pos, new Vector2(56, 56), @"TexturedRegularAlpha");
                    pos.X += buttonWidth;
                    max = new Vector2(pos.X + Font().MeasureString(textB).X, min.Y + buttonWidth);
                    hitBoxB.Set(min, max);

                    TextHelper.DrawString(Font, textB, pos, labelBColor);
                }

            }
            batch.End();

            // Add left stick if needed.
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

        }   // end of PreRender()

        public void Render()
        {
            if (Active)
            {
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

                RenderTarget2D rtFull = UI2D.Shared.RenderTargetDepthStencil1024_768;   // Rendertarget we render whole display into.
                Vector2 rtSize = new Vector2(rtFull.Width, rtFull.Height);

                ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();

                Color darkTextColor = new Color(20, 20, 20);
                Color greyTextColor = new Color(127, 127, 127);
                Color greenTextColor = new Color(8, 123, 110);
                Color whiteTextColor = new Color(255, 255, 255);

                // No put it all together.
                // Start with the background.
                Vector2 screenSize = BokuGame.ScreenSize;
                if (useRtCoords)
                {
                    // Need the size of the RT instead.
                    screenSize = InGame.GetCurrentRenderTargetSize();
                    Debug.Assert(screenSize != Vector2.Zero);
                }

                if (useBackgroundThumbnail)
                {
                    if (!thumbnail.GraphicsDevice.IsDisposed && !thumbnail.IsDisposed)
                    {
                        // Render the blurred thumbnail (if valid) full screen.
                        if (!thumbnail.GraphicsDevice.IsDisposed)
                        {
                            InGame.RestoreViewportToFull();
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
                Vector2 scale = screenSize / dialogSize;
                renderScale = Math.Min(Math.Min(scale.X, scale.Y), 1.0f);
                Vector2 renderSize = rtSize * renderScale;

                // Center on screen.
                renderPosition = (screenSize - renderSize) / 2.0f;

                // Clamp to pixel coords.
                renderPosition.X = (int)renderPosition.X;
                renderPosition.Y = (int)renderPosition.Y;

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
            TwitchManager.Set<float> set = delegate(float val, Object param) { textOffset = (int)val; dirty = true; };
            TwitchManager.CreateTwitch<float>(textOffset, -topLine * Font().LineSpacing, set, 0.2f, TwitchCurve.Shape.OvershootOut);
        }   // end of TwitchTextOffset()

        public void OnSelect(UIGrid grid)
        {
            // We should never actually get here.  The TextDisplay UpdateObj 
            // should consume all 'A' presses before the grids get them...

            Debug.Assert(false);

        }   // end of OnSelect()

        public void OnCancel(UIGrid grid)
        {
            // We should never actually get here.  The TextDisplay UpdateObj 
            // should consume all 'B' presses before the grids get them...

            Debug.Assert(false);

        }   // end of OnCancel()

        public void LoadContent(bool immediate)
        {
        }   // end of ScrollableTextDisplay LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
            if (backgroundTexture == null)
            {
                backgroundTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\TextEditor\TextDisplayBackground");
            }

            if (leftStick == null)
            {
                leftStick = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\HelpCard\LeftStick");
            }
        }   // end of ScrollableTextDisplay InitDeviceResources()

        public void UnloadContent()
        {
            BokuGame.Release(ref backgroundTexture);
            BokuGame.Release(ref leftStick);
        }   // end of ScrollableTextDisplay UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
        }

        /// <summary>
        /// Activates scrollable text display.
        /// </summary>
        /// <param name="text">Text to display.</param>
        /// <param name="useBackgroundThumbnail">Should the game thumbnail be used as a backdrop when rendering?</param>
        /// <param name="useRtCoords">Should hit testing be adjusted for rendering to an RT?</param>
        public void Activate(GameActor thinker, string text, UIGridElement.Justification justification, bool useBackgroundThumbnail, bool useRtCoords)
        {
            Activate(thinker, text, justification, useBackgroundThumbnail, useRtCoords, true);
        }

        /// <summary>
        /// Activates scrollable text display.
        /// </summary>
        /// <param name="text">Text to display.</param>
        /// <param name="useBackgroundThumbnail">Should the game thumbnail be used as a backdrop when rendering?</param>
        /// <param name="useRtCoords">Should hit testing be adjusted for rendering to an RT?</param>
        /// <param name="scrubText">Should text be scrubbed through bad word filter?</param>
        public void Activate(GameActor thinker, string text, UIGridElement.Justification justification, bool useBackgroundThumbnail, bool useRtCoords, bool scrubText)
        {
            if (text == null)
                return;

            if (state != States.Active)
            {
                this.useBackgroundThumbnail = useBackgroundThumbnail;
                this.useRtCoords = useRtCoords;

                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Push(commandMap);

                state = States.Active;

                // Get the current scene thumbnail.  If we're using this from the main menu (options)
                // then use the title screen image instead.
                if (InGame.inGame.State == InGame.States.Inactive)
                {
                    thumbnail = BokuGame.bokuGame.mainMenu.BackgroundTexture;
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
                if (thinker != null)
                {
                    text = TextHelper.ApplyStringSubstitutions(text, thinker as GameActor);
                }
                rawText = text;
                text = TextHelper.RemoveTags(text);
                blob = new TextBlob(UI2D.Shared.GetGameFont20, text, textWidth);
                blob.Justification = justification;
                // By default TextBlob scrubs the text.  If the scrubText is false
                // then set the text again via a path that doesn't do any scrub.
                if (!scrubText)
                {
                    blob.RawTextNoScrub = text;
                }

                this.thinker = thinker;

                topLine = 0;
                textOffset = 0;

                // If a second button is needed, the text for it should be set after activation.
                textB = null;
                userHitA = true;

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

                SaidStringManager.AddEntry(thinker, rawText, false);

                textB = null;
            }
        }

#endregion

    }   // end of class ScrollableTextDisplay

}   // end of namespace Boku
