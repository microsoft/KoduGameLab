
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.Fx;
using Boku.Audio;
using Boku.Programming;

namespace Boku
{
    /// <summary>
    /// A static class which manages and displays victory and gameover overlay screens.
    /// </summary>
    public class VictoryOverlay
    {
        private static Effect effect;
        private static Texture2D textureTeam;
        private static Texture2D textureOptions;
        private static Texture2D textureOptionsKey;
        private static Texture2D textureWinner;
        private static Texture2D textureGameOver;

        private static Classification.Colors activeTeam;    // Controls whether or not we render.
        private static GamePadSensor.PlayerId activePlayer; // Controls whether or not we render.
        private static bool activeWinner;                   // Controls whether or not we render.
        private static bool activeGameOver;                 // Controls whether or not we render.

        private static float alphaTeam;         // Controls transparency of overlay.
        private static float alphaWinner;       // Controls transparency of overlay.
        private static float alphaGameOver;     // Controls transparency of overlay.

        private static double timeoutTeam;
        private static double timeoutWinner;
        private static double timeoutGameOver;

        private const float displayLife = float.MaxValue;       // How many seconds to display before going away.

        private static bool wasRenderingAsThumbnail = false;    // Indicates that when activated something else in the system
                                                                // had set the RenderAsThumbnail flag.  We need to wait until
                                                                // this is cleared before rendering our texture.

        private static AABB2D homeHitBox = new AABB2D();        // Hit regions for mouse clicking on buttons.
        private static AABB2D editHitBox = new AABB2D();
        private static AABB2D restartHitBox = new AABB2D();
        private static PerspectiveUICamera camera = null;       // Used for re-mapping to account for rendering to a rendertarget 
                                                                // which may differ in size from the current window.

        private static bool dirty = false;                      // Force a refresh of the rendertarget texture.

        protected struct Vertex : IVertexType
        {
            private Vector2 pos;    // Expanded to a Vector4 in the vertex shader.
            private Vector2 tex;

            static VertexDeclaration decl = null;
            static VertexElement[] elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                // Total = 16 bytes
            };

            // c'tor
            public Vertex(Vector2 pos, Vector2 tex)
            {
                this.pos = pos;
                this.tex = tex;
            }   // end of Vertex c'tor

            public VertexDeclaration VertexDeclaration
            {
                get
                {
                    if (decl == null || decl.IsDisposed)
                    {
                        decl = new VertexDeclaration(elements);
                    }
                    return decl;
                }
            }

        }   // end of Vertex

        private static Vertex[] localVerts = new Vertex[4];
        private static VertexBuffer vbuf = null;

        #region Accessors

        /// <summary>
        /// Returns true if any of the game over conditions have been met.
        /// </summary>
        public static bool GameOver
        {
            get { return VictoryOverlay.ActiveGameOver || VictoryOverlay.ActiveWinner || VictoryOverlay.ActiveTeam != Classification.Colors.NotApplicable || ActivePlayer != GamePadSensor.PlayerId.Dynamic; }
        }

        public static Classification.Colors ActiveTeam
        {
            get { return activeTeam; }
            set 
            {
                if (activeTeam != value)
                {
                    activeTeam = value;
                    if (value != Classification.Colors.NotApplicable)
                    {
                        timeoutTeam = Time.WallClockTotalSeconds + displayLife;
                        AlphaTeam = 1.0f;
                        Foley.PlayWin();
                        dirty = true;

                        // Clear any input that may have triggered the win.  Otherwise
                        // we run the risk or immediately dismissing the dialog.
                        GamePadInput.ClearAllWasPressedState();
                        KeyboardInput.ClearAllWasPressedState();
                    }
                    else
                    {
                        AlphaTeam = 0.0f;
                    }
                }
            }
        }
        public static GamePadSensor.PlayerId ActivePlayer
        {
            get { return activePlayer; }
            set
            {
                if (activePlayer != value)
                {
                    activePlayer = value;
                    if (value != GamePadSensor.PlayerId.Dynamic)
                    {
                        timeoutTeam = Time.WallClockTotalSeconds + displayLife;
                        AlphaTeam = 1.0f;
                        Foley.PlayWin();
                        dirty = true;

                        // Clear any input that may have triggered the win.  Otherwise
                        // we run the risk or immediately dismissing the dialog.
                        GamePadInput.ClearAllWasPressedState();
                        KeyboardInput.ClearAllWasPressedState();
                    }
                    else
                    {
                        AlphaTeam = 0.0f;
                    }
                }
            }
        }
        public static float AlphaTeam
        {
            get { return alphaTeam; }
            set 
            {
                TwitchManager.Set<float> set = delegate(float val, Object param) { alphaTeam = val; };
                TwitchManager.CreateTwitch<float>(alphaTeam, value, set, 0.2f, TwitchCurve.Shape.EaseInOut);
            }
        }

        public static bool ActiveWinner
        {
            get { return activeWinner; }
            set
            {
                if(!Active)
                {
                    if (activeWinner != value)
                    {
                        activeWinner = value;
                        if (value)
                        {
                            timeoutWinner = Time.WallClockTotalSeconds + displayLife;
                            AlphaWinner = 1.0f;
                            Foley.PlayWin();
                            dirty = true;

                            // Clear any input that may have triggered the win.  Otherwise
                            // we run the risk or immediately dismissing the dialog.
                            GamePadInput.ClearAllWasPressedState();
                            KeyboardInput.ClearAllWasPressedState();
                        }
                        else
                        {
                            AlphaWinner = 0.0f;
                        }
                    }
                }
            }
        }
        public static float AlphaWinner
        {
            get { return alphaWinner; }
            set
            {
                TwitchManager.Set<float> set = delegate(float val, Object param) { alphaWinner = val; };
                TwitchManager.CreateTwitch<float>(alphaWinner, value, set, 0.2f, TwitchCurve.Shape.EaseInOut);
            }
        }

        public static bool ActiveGameOver
        {
            get { return activeGameOver; }
            set
            {
                if (!Active)
                {
                    if (activeGameOver != value)
                    {
                        activeGameOver = value;
                        if (value)
                        {
                            timeoutGameOver = Time.WallClockTotalSeconds + displayLife;
                            AlphaGameOver = 1.0f;
                            Foley.PlayEndGame();
                            dirty = true;
                        }
                        else
                        {
                            AlphaGameOver = 0.0f;
                        }
                    }
                }
            }
        }
        public static float AlphaGameOver
        {
            get { return alphaGameOver; }
            set
            {
                TwitchManager.Set<float> set = delegate(float val, Object param) { alphaGameOver = val; };
                TwitchManager.CreateTwitch<float>(alphaGameOver, value, set, 0.2f, TwitchCurve.Shape.EaseInOut);
            }
        }

        /// <summary>
        /// Are any of the victory modes currently active.
        /// </summary>
        public static bool Active
        {
            get
            {
                return ActiveGameOver 
                        || ActivePlayer != GamePadSensor.PlayerId.Dynamic
                        || ActiveTeam != Classification.Colors.NotApplicable
                        || ActiveWinner;
            }
        }

        #endregion

        // private c'tor
        private VictoryOverlay()
        {
        }

        public static void Reset()
        {
            activeTeam = Classification.Colors.NotApplicable;
            activePlayer = GamePadSensor.PlayerId.Dynamic;
            activeWinner = false;
            activeGameOver = false;

            alphaTeam = 0.0f;
            alphaWinner = 0.0f;
            alphaGameOver = 0.0f;

            timeoutTeam = 0.0;
            timeoutWinner = 0.0;
            timeoutGameOver = 0.0;
        }

        private static void RefreshTexture()
        {
            RenderTarget2D rt = UI2D.Shared.RenderTargetDepthStencil1280_720;
            ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();

            InGame.SetRenderTarget(rt);
            InGame.Clear(Color.Transparent);

            bool showTeam = (ActiveTeam != Classification.Colors.NotApplicable);
            bool showPlayer = ActivePlayer != GamePadSensor.PlayerId.Dynamic;

            if (showTeam || showPlayer)
            {
                // Main graphic.
                Vector2 size = new Vector2(textureWinner.Width, textureWinner.Height);
                Vector2 pos = new Vector2((rt.Width - size.X) / 2.0f, 0.0f);
                quad.Render(textureWinner, pos, size, "TexturedPreMultAlpha");

                // Team or Player.
                size = new Vector2(textureTeam.Width, textureTeam.Height);
                pos = new Vector2((rt.Width - size.X) / 2.0f, 354);
                quad.Render(textureTeam, pos, size, "TexturedPreMultAlpha");

                string label = String.Empty;
                if (showTeam)
                {
                    label = Strings.Localize("gameOver.team") + " ";
                }
                if (showPlayer)
                {
                    // Nothing to add here.
                }

                // Get correct color for team.
                Texture2D glyph = null;

                switch(ActivePlayer)
                {
                    case GamePadSensor.PlayerId.One:
                        glyph = CardSpace.Cards.CardFaceTexture("filter.player1");
                        label += CardSpace.Cards.GetLabel("filter.player1");
                        break;
                    case GamePadSensor.PlayerId.Two:
                        glyph = CardSpace.Cards.CardFaceTexture("filter.player2");
                        label += CardSpace.Cards.GetLabel("filter.player2");
                        break;
                    case GamePadSensor.PlayerId.Three:
                        glyph = CardSpace.Cards.CardFaceTexture("filter.player3");
                        label += CardSpace.Cards.GetLabel("filter.player3");
                        break;
                    case GamePadSensor.PlayerId.Four:
                        glyph = CardSpace.Cards.CardFaceTexture("filter.player4");
                        label += CardSpace.Cards.GetLabel("filter.player4");
                        break;
                    default :
                        // Do nothing...
                        break;
                }

                switch (ActiveTeam)
                {
                    case Classification.Colors.White:
                        glyph = CardSpace.Cards.CardFaceTexture("filter.white");
                        label += Strings.Localize("colorNames.white");
                        break;
                    case Classification.Colors.Black:
                        glyph = CardSpace.Cards.CardFaceTexture("filter.Black");
                        label += Strings.Localize("colorNames.black");
                        break;
                    case Classification.Colors.Grey:
                        glyph = CardSpace.Cards.CardFaceTexture("filter.Grey");
                        label += Strings.Localize("colorNames.grey");
                        break;
                    case Classification.Colors.Red:
                        glyph = CardSpace.Cards.CardFaceTexture("filter.red");
                        label += Strings.Localize("colorNames.red");
                        break;
                    case Classification.Colors.Green:
                        glyph = CardSpace.Cards.CardFaceTexture("filter.green");
                        label += Strings.Localize("colorNames.green");
                        break;
                    case Classification.Colors.Blue:
                        glyph = CardSpace.Cards.CardFaceTexture("filter.blue");
                        label += Strings.Localize("colorNames.blue");
                        break;
                    case Classification.Colors.Orange:
                        glyph = CardSpace.Cards.CardFaceTexture("filter.orange");
                        label += Strings.Localize("colorNames.orange");
                        break;
                    case Classification.Colors.Yellow:
                        glyph = CardSpace.Cards.CardFaceTexture("filter.yellow");
                        label += Strings.Localize("colorNames.yellow");
                        break;
                    case Classification.Colors.Purple:
                        glyph = CardSpace.Cards.CardFaceTexture("filter.purple");
                        label += Strings.Localize("colorNames.purple");
                        break;
                    case Classification.Colors.Pink:
                        glyph = CardSpace.Cards.CardFaceTexture("filter.pink");
                        label += Strings.Localize("colorNames.pink");
                        break;
                    case Classification.Colors.Brown:
                        glyph = CardSpace.Cards.CardFaceTexture("filter.brown");
                        label += Strings.Localize("colorNames.brown");
                        break;
                    default:
                        break;
                }

                // Grabbing the glyph from CardSpace may have changed rendertargets
                // since we now create them somewhat on demand.
                InGame.SetRenderTarget(rt);

                if (glyph != null)
                {
                    size = new Vector2(90, 90);
                    pos += new Vector2(7, 10);
                    quad.Render(glyph, pos, size, "TexturedPreMultAlpha");
                }
                
                // Options.
                if (GamePadInput.ActiveMode == GamePadInput.InputMode.GamePad)
                {
                    size = new Vector2(textureOptions.Width, textureOptions.Height);
                    pos = new Vector2((rt.Width - size.X) / 2.0f, 470);
                    quad.Render(textureOptions, pos, size, "TexturedPreMultAlpha");
                }
                else
                {
                    size = new Vector2(textureOptionsKey.Width, textureOptionsKey.Height);
                    pos = new Vector2((rt.Width - size.X) / 2.0f, 470);
                    quad.Render(textureOptionsKey, pos, size, "TexturedPreMultAlpha");

                    // Add key face icons.
                    Color color = new Color(20, 20, 20);
                    TextBlob blob = new TextBlob(UI2D.Shared.GetGameFont20, "[home]", 100);
                    blob.Justification = Boku.UI2D.UIGridElement.Justification.Center;

                    pos.Y += 16;
                    blob.RenderWithButtons(pos, color);
                    homeHitBox.Set(pos, pos + new Vector2(100, blob.TotalSpacing));

                    blob.RawText = "[esc]";
                    pos.Y += 50;
                    blob.RenderWithButtons(pos, color);
                    editHitBox.Set(pos, pos + new Vector2(100, blob.TotalSpacing));

                    blob.RawText = "[enter]";
                    pos.Y += 50;
                    blob.RenderWithButtons(pos, color);
                    restartHitBox.Set(pos, pos + new Vector2(100, blob.TotalSpacing));

                }

                // Add button labels.
                SpriteBatch batch = UI2D.Shared.SpriteBatch;
                UI2D.Shared.GetFont Font = UI2D.Shared.GetGameFont24Bold;
                //Color fontColor = new Color(10, 75, 108);
                Color fontColor = new Color(127, 127, 127);
                Color shadowColor = new Color(0, 0, 0, 20);
                Vector2 shadowOffset = new Vector2(0, 6);

                // Disable writing to alpha channel.
                // This prevents transparent fringing around the text.
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
                device.BlendState = UI2D.Shared.BlendStateColorWriteRGB;
                
                if (label != null)
                {
                    // Center the team name.
                    int len = (int)Font().MeasureString(label).X;
                    pos = new Vector2(565, 386);
                    pos.X += (textureTeam.Width - textureTeam.Height - len) / 2;
                    TextHelper.DrawStringNoBatch(Font, label, pos + shadowOffset, shadowColor);
                    TextHelper.DrawStringNoBatch(Font, label, pos, fontColor);
                }

                pos = new Vector2(572, 482);
                TextHelper.DrawStringNoBatch(Font, Strings.Localize("gameOver.browse"), pos + shadowOffset, shadowColor);
                TextHelper.DrawStringNoBatch(Font, Strings.Localize("gameOver.browse"), pos, fontColor);
                
                pos.Y += 51;
                TextHelper.DrawStringNoBatch(Font, Strings.Localize("gameOver.edit"), pos + shadowOffset, shadowColor);
                TextHelper.DrawStringNoBatch(Font, Strings.Localize("gameOver.edit"), pos, fontColor);

                pos.Y += 51;
                TextHelper.DrawStringNoBatch(Font, Strings.Localize("gameOver.restart"), pos + shadowOffset, shadowColor);
                TextHelper.DrawStringNoBatch(Font, Strings.Localize("gameOver.restart"), pos, fontColor);
                
                // Restore default blend state.
                device.BlendState = BlendState.AlphaBlend;
                
            }

            if (ActiveWinner)
            {
                // Main graphic.
                Vector2 size = new Vector2(textureWinner.Width, textureWinner.Height);
                Vector2 pos = new Vector2((rt.Width - size.X) / 2.0f, 0.0f);
                quad.Render(textureWinner, pos, size, "TexturedPreMultAlpha");

                // Options.
                if (GamePadInput.ActiveMode == GamePadInput.InputMode.GamePad)
                {
                    size = new Vector2(textureOptions.Width, textureOptions.Height);
                    pos = new Vector2((rt.Width - size.X) / 2.0f, 470);
                    quad.Render(textureOptions, pos, size, "TexturedPreMultAlpha");
                }
                else
                {
                    size = new Vector2(textureOptionsKey.Width, textureOptionsKey.Height);
                    pos = new Vector2((rt.Width - size.X) / 2.0f, 470);
                    quad.Render(textureOptionsKey, pos, size, "TexturedPreMultAlpha");

                    // Add key face icons.
                    Color color = new Color(20, 20, 20);
                    TextBlob blob = new TextBlob(UI2D.Shared.GetGameFont20, "[home]", 100);
                    blob.Justification = Boku.UI2D.UIGridElement.Justification.Center;

                    pos.Y += 16;
                    blob.RenderWithButtons(pos, color);
                    homeHitBox.Set(pos, pos + new Vector2(100, blob.TotalSpacing));

                    blob.RawText = "[esc]";
                    pos.Y += 50;
                    blob.RenderWithButtons(pos, color);
                    editHitBox.Set(pos, pos + new Vector2(100, blob.TotalSpacing));

                    blob.RawText = "[enter]";
                    pos.Y += 50;
                    blob.RenderWithButtons(pos, color);
                    restartHitBox.Set(pos, pos + new Vector2(100, blob.TotalSpacing));

                }

                // Add button labels.
                SpriteBatch batch = UI2D.Shared.SpriteBatch;
                UI2D.Shared.GetFont Font = UI2D.Shared.GetGameFont24Bold;
                Color fontColor = new Color(10, 75, 108);

                pos = new Vector2(572, 482);
                TextHelper.DrawStringNoBatch(Font, Strings.Localize("gameOver.browse"), pos, fontColor);
                pos.Y += 51;
                TextHelper.DrawStringNoBatch(Font, Strings.Localize("gameOver.edit"), pos, fontColor);
                pos.Y += 51;
                TextHelper.DrawStringNoBatch(Font, Strings.Localize("gameOver.restart"), pos, fontColor);
            }

            if (ActiveGameOver)
            {
                // Main graphic.
                Vector2 size = new Vector2(textureGameOver.Width, textureGameOver.Height);
                Vector2 pos = new Vector2((rt.Width - size.X) / 2.0f, 0.0f);
                quad.Render(textureGameOver, pos, size, "TexturedPreMultAlpha");

                // Options.
                if (GamePadInput.ActiveMode == GamePadInput.InputMode.GamePad)
                {
                    size = new Vector2(textureOptions.Width, textureOptions.Height);
                    pos = new Vector2((rt.Width - size.X) / 2.0f, 470);
                    quad.Render(textureOptions, pos, size, "TexturedPreMultAlpha");
                }
                else
                {
                    size = new Vector2(textureOptionsKey.Width, textureOptionsKey.Height);
                    pos = new Vector2((rt.Width - size.X) / 2.0f, 470);
                    quad.Render(textureOptionsKey, pos, size, "TexturedPreMultAlpha");

                    // Add key face icons.
                    Color color = new Color(20, 20, 20);
                    TextBlob blob = new TextBlob(UI2D.Shared.GetGameFont20, "[home]", 100);
                    blob.Justification = Boku.UI2D.UIGridElement.Justification.Center;

                    pos.Y += 16;
                    blob.RenderWithButtons(pos, color);
                    homeHitBox.Set(pos, pos + new Vector2(100, blob.TotalSpacing));

                    blob.RawText = "[esc]";
                    pos.Y += 50;
                    blob.RenderWithButtons(pos, color);
                    editHitBox.Set(pos, pos + new Vector2(100, blob.TotalSpacing));

                    blob.RawText = "[enter]";
                    pos.Y += 50;
                    blob.RenderWithButtons(pos, color);
                    restartHitBox.Set(pos, pos + new Vector2(100, blob.TotalSpacing));

                }

                // Add button labels.
                SpriteBatch batch = UI2D.Shared.SpriteBatch;
                UI2D.Shared.GetFont Font = UI2D.Shared.GetGameFont24Bold;
                Color fontColor = new Color(10, 75, 108);

                pos = new Vector2(572, 482);
                TextHelper.DrawStringNoBatch(Font, Strings.Localize("gameOver.browse"), pos, fontColor);
                pos.Y += 51;
                TextHelper.DrawStringNoBatch(Font, Strings.Localize("gameOver.edit"), pos, fontColor);
                pos.Y += 51;
                TextHelper.DrawStringNoBatch(Font, Strings.Localize("gameOver.restart"), pos, fontColor);
            }

            InGame.RestoreRenderTarget();

            dirty = false;

        }   // end of RefreshTexture()

        private static GamePadInput.InputMode prevMode = GamePadInput.InputMode.None;

        public static void Update()
        {
            // Should only happens during device reset.
            if (dirty)
            {
                RefreshTexture();
            }

            // If we've changed modes, refresh the texture.
            if (GamePadInput.ActiveMode != prevMode)
            {
                RefreshTexture();
                prevMode = GamePadInput.ActiveMode;
            }

            if (wasRenderingAsThumbnail && !InGame.inGame.RenderWorldAsThumbnail)
            {
                RefreshTexture();
                wasRenderingAsThumbnail = false;
            }

            if (InGame.inGame.RenderWorldAsThumbnail)
            {
                wasRenderingAsThumbnail = true;
            }

            if (Active && GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
            {
                camera.Resolution = new Point(1280, 720);   // Match the render target we're using.
                Vector2 mouseHit = MouseInput.GetAspectRatioAdjustedPosition(camera, false);
                
                if (homeHitBox.LeftPressed(mouseHit))
                {
                    InGame.inGame.SwitchToMiniHub();
                }

                if (editHitBox.LeftPressed(mouseHit))
                {
                    InGame.inGame.CurrentUpdateMode = InGame.UpdateMode.EditObject;
                }

                if (restartHitBox.LeftPressed(mouseHit))
                {
                    InGame.inGame.ResetSim(preserveScores: false, removeCreatablesFromScene: true, keepPersistentScores: false);
                }
            }
            else if (Active && GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
            {
                camera.Resolution = new Point(1280, 720);   // Match the render target we're using.
                for (int i = 0; i < TouchInput.TouchCount; i++)
                {
                    TouchContact touch = TouchInput.GetTouchContactByIndex(i);

                    // Touch input on grid.
                    // Hit the in-focus tile, then open popup.
                    // Hit another tile, then bring that one to focus.  Note because of overlap of
                    // the tiles we should do this center-out.

                    Vector2 touchHit = TouchInput.GetAspectRatioAdjustedPosition(
                        touch.position,
                        camera,
                        false
                    );
                    if (homeHitBox.Touched(touch, touchHit))
                    {
                        InGame.inGame.SwitchToMiniHub();
                    }

                    if (editHitBox.Touched(touch, touchHit))
                    {
                        InGame.inGame.CurrentUpdateMode = InGame.UpdateMode.EditObject;
                    }

                    if (restartHitBox.Touched(touch, touchHit))
                    {
                        InGame.inGame.ResetSim(preserveScores: false, removeCreatablesFromScene: true, keepPersistentScores: false);
                    }
                }

               
            }

        }   // end of Update()

        /// <summary>
        /// Renders any active victory overlay.
        /// </summary>
        public static void Render()
        {
            try
            {
                RenderTarget2D rt = UI2D.Shared.RenderTargetDepthStencil1280_720;
                ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();

                // Adjust for other screen sizes and aspect ratios.
                Vector2 origin = Vector2.Zero;
                int w = (int)BokuGame.ScreenSize.X;
                int h = (int)BokuGame.ScreenSize.Y;
                Vector2 size = BokuGame.ScreenSize;
                float margin = (h * 1280.0f / 720.0f - w) * 0.5f;

                origin.X = -margin;
                size.X += margin * 2.0f;

                Vector4 color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

                if (ActiveWinner && AlphaWinner > 0)
                {
                    if (timeoutWinner < Time.WallClockTotalSeconds)
                    {
                        ActiveWinner = false;
                    }
                    else
                    {
                        color.W = AlphaWinner;
                        quad.Render(rt, color, origin, size, @"TexturedRegularAlpha");
                    }
                }

                if ((ActiveTeam != Classification.Colors.NotApplicable || ActivePlayer != GamePadSensor.PlayerId.Dynamic) && AlphaTeam > 0)
                {
                    if (timeoutTeam < Time.WallClockTotalSeconds)
                    {
                        ActiveTeam = Classification.Colors.NotApplicable;
                        ActivePlayer = GamePadSensor.PlayerId.Dynamic;
                    }
                    else
                    {
                        color.W = AlphaTeam;
                        quad.Render(rt, color, origin, size, @"TexturedRegularAlpha");
                    }
                }

                if (ActiveGameOver && AlphaGameOver > 0)
                {
                    if (timeoutGameOver < Time.WallClockTotalSeconds)
                    {
                        ActiveGameOver = false;
                    }
                    else
                    {
                        color.W = AlphaGameOver;
                        quad.Render(rt, color, origin, size, @"TexturedRegularAlpha");
                    }
                }
            }
            catch
            {
                // During device reset this can fail.  What's happening is that if the victory overlay is
                // active when the window in minimixzed/restored, the system can still think that the 
                // rendertarget is 'set' on the device causing the Texture2D call to throw.
                // So, catch the exception and force the rendertarget to be redrawn.
                dirty = true;
            }

        }   // end of VictoryOverlay Render()


        public static void LoadContent(bool immediate)
        {
            if (effect == null)
            {
                // Use the help overlay shader since it does what we want.
                effect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\HelpOverlay");
            }

            // Read in the textures.
            try
            {
                textureTeam = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\WinTeam");
                textureOptions = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\WinOptions");
                textureOptionsKey = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\WinOptionsKey");
                textureWinner = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Winner");
                textureGameOver = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GameOver");
            }
            catch (ContentLoadException e)
            {
                if (e != null)
                {
                    Debug.Assert(false, "Missing content.");
                }
            }

        }   // end of VictoryOverlay LoadContent()

        public static void InitDeviceResources(GraphicsDevice device)
        {
            // Done here since the camera needs a valid graphics device to init correctly.
            camera = new PerspectiveUICamera();

            // Init the vertex buffer.
            if (vbuf == null)
            {
                vbuf = new VertexBuffer(device, typeof(Vertex), 4, BufferUsage.WriteOnly);

                // Check the dimensions of the destination.
                // TODO (****) *** This needs to scale with the window.
                int width = device.Viewport.Width;
                int height = device.Viewport.Height;

                float pixelWidth = 1.0f / width;
                float pixelHeight = 1.0f / height;

                // Fill in the local vertex data.
                localVerts[0] = new Vertex(new Vector2(-1.0f - pixelWidth, 1.0f + pixelHeight), new Vector2(0.0f, 0.0f));
                localVerts[1] = new Vertex(new Vector2(1.0f - pixelWidth, 1.0f + pixelHeight), new Vector2(1.0f, 0.0f));
                localVerts[2] = new Vertex(new Vector2(-1.0f - pixelWidth, -1.0f + pixelHeight), new Vector2(0.0f, 1.0f));
                localVerts[3] = new Vertex(new Vector2(1.0f - pixelWidth, -1.0f + pixelHeight), new Vector2(1.0f, 1.0f));

                // Copy to vertex buffer.
                vbuf.SetData<Vertex>(localVerts);
            }
        }

        public static void UnloadContent()
        {
            BokuGame.Release(ref effect);
            BokuGame.Release(ref vbuf);

            BokuGame.Release(ref textureTeam);
            BokuGame.Release(ref textureOptions);
            BokuGame.Release(ref textureOptionsKey);
            BokuGame.Release(ref textureWinner);
            BokuGame.Release(ref textureGameOver);
        }   // end of VictoryOverlay UnloadContent()

        /// <summary>
        /// Recreate render targets.
        /// </summary>
        public static void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class VictoryOverlay

}   // end of namespace Boku
