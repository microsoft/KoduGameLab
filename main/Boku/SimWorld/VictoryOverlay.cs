
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

using KoiX;
using KoiX.Geometry;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI;

using Boku.Base;
using Boku.Common;
using Boku.Fx;
using Boku.Audio;
using Boku.Programming;

namespace Boku
{
    /// <summary>
    /// A static class which manages and displays victory and gameover overlay screens.
    /// 
    /// TODO (****) Not really sure if making this all static makes sense.  Not worth
    /// changing, for now.
    /// </summary>
    public class VictoryOverlay
    {
        static SpriteCamera camera = new SpriteCamera();

        static Texture2D textureWinner;
        static Texture2D textureGameOver;

        static Classification.Colors activeTeam;    // Controls whether or not we render.
        static GamePadSensor.PlayerId activePlayer; // Controls whether or not we render.
        static bool activeWinner;                   // Controls whether or not we render.
        static bool activeGameOver;                 // Controls whether or not we render.

        static float alphaTeam;         // Controls transparency of overlay.
        static float alphaWinner;       // Controls transparency of overlay.
        static float alphaGameOver;     // Controls transparency of overlay.

        static TextBlob blob;

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
                        AlphaTeam = 1.0f;
                        Foley.PlayWin();

                        // Clear any input that may have triggered the win.  Otherwise
                        // we run the risk or immediately dismissing the dialog.
                        GamePadInput.ClearAllWasPressedState();
                        KeyboardInputX.ClearAllWasPressedState();
                    }
                    else
                    {
                        AlphaTeam = 0.0f;
                    }
                    DialogManagerX.ShowDialog(DialogCenter.GameOverDialog, camera);
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
                        AlphaTeam = 1.0f;
                        Foley.PlayWin();

                        // Clear any input that may have triggered the win.  Otherwise
                        // we run the risk or immediately dismissing the dialog.
                        GamePadInput.ClearAllWasPressedState();
                        KeyboardInputX.ClearAllWasPressedState();
                    }
                    else
                    {
                        AlphaTeam = 0.0f;
                    }
                    DialogManagerX.ShowDialog(DialogCenter.GameOverDialog, camera);
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
                            AlphaWinner = 1.0f;
                            Foley.PlayWin();

                            // Clear any input that may have triggered the win.  Otherwise
                            // we run the risk or immediately dismissing the dialog.
                            GamePadInput.ClearAllWasPressedState();
                            KeyboardInputX.ClearAllWasPressedState();
                        }
                        else
                        {
                            AlphaWinner = 0.0f;
                        }
                        DialogManagerX.ShowDialog(DialogCenter.GameOverDialog, camera);
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
                            AlphaGameOver = 1.0f;
                            Foley.PlayEndGame();
                        }
                        else
                        {
                            AlphaGameOver = 0.0f;
                        }
                        DialogManagerX.ShowDialog(DialogCenter.GameOverDialog, camera);
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

        // c'tor
        VictoryOverlay()
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
        }   // end of Reset()

        public static void Update()
        {
            // Keep camera in sync with screen size.
            BaseScene.SetCameraToTargetResolution(camera);

            // Lazy init.
            if (blob == null)
            {
                blob = new TextBlob(SharedX.GetGameFont30Bold, "testing", 428);
            }

            if (Active)
            {
            }

        }   // end of Update()

        /// <summary>
        /// Renders any active victory overlay.
        /// </summary>
        public static void Render()
        {
            SpriteBatch batch = KoiLibrary.SpriteBatch;

            // Need to figure out textures _before_ calling batch.Begin since CardSpace may also need to do some rendering.
            // Do we have a player or team tile to show?
            bool showTeam = (ActiveTeam != Classification.Colors.NotApplicable);
            bool showPlayer = ActivePlayer != GamePadSensor.PlayerId.Dynamic;

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

            if (showPlayer)
            {
                switch (ActivePlayer)
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
                    default:
                        // Do nothing...
                        break;
                }
            }

            if (showTeam)
            {
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
            }

            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, samplerState: null, depthStencilState: null, rasterizerState: null, effect: null, transformMatrix: camera.ViewMatrix);
            {
                if (ActiveGameOver)
                {
                    // Main graphic.
                    Vector2 size = new Vector2(textureGameOver.Width, textureGameOver.Height);
                    Vector2 pos = -size / 2.0f;
                    Rectangle rect = new Rectangle((int)pos.X, (int)pos.Y - 100, (int)size.X, (int)size.Y);
                    batch.Draw(textureGameOver, rect, Color.White);
                }


                if (ActiveWinner || showTeam || showPlayer)
                {
                    // Main graphic.  Note that the graphic itself is slightly asymmetric
                    // so the positioning is adjusted just to look good.
                    Vector2 size = new Vector2(textureWinner.Width, textureWinner.Height);
                    Vector2 pos = new Vector2(-640, -235);
                    // If showing the extra tile, move backdrop up a bit so it's not covered.
                    if (showTeam || showPlayer)
                    {
                        pos.Y -= 120.0f;
                    }
                    Rectangle rect = new Rectangle((int)pos.X, (int)pos.Y - 100, (int)size.X, (int)size.Y);
                    batch.Draw(textureWinner, rect, Color.White);
                }
            }
            batch.End();

            if (showTeam || showPlayer)
            {
                // Grab the GameOverDialog so we can use its theme settings.
                BaseDialog dialog = DialogCenter.GameOverDialog;

                // Baseplate.
                Vector2 size = new Vector2(dialog.Rectangle.Width, 176);
                Vector2 pos = new Vector2(-size.X / 2.0f, -100);
                RectangleF rectangle = new RectangleF(pos, size);
                RoundedRect.Render(camera, rectangle, dialog.CornerRadius, dialog.OutlineColor,
                                    outlineColor: dialog.OutlineColor, outlineWidth: dialog.OutlineWidth,
                                    twoToneSecondColor: dialog.BodyColor, twoToneSplitPosition: 176, twoToneHorizontalSplit: false,
                                    bevelStyle: dialog.BevelStyle, bevelWidth: dialog.BevelWidth,
                                    shadowStyle: dialog.ShadowStyle, shadowOffset: dialog.ShadowOffset, shadowSize: dialog.ShadowSize, shadowAttenuation: 0.85f);

                // Add glyph.
                if (glyph != null)
                {
                    batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, samplerState: null, depthStencilState: null, rasterizerState: null, effect: null, transformMatrix: camera.ViewMatrix);
                    {
                        batch.Draw(glyph, pos + new Vector2(28, 26), Color.White);
                    }
                    batch.End();
                }

                // Add text.
                blob.RawText = label;
                blob.RenderText(camera, pos + new Vector2(200, 60), Color.White, outlineColor: Color.Black, outlineWidth: 0.8f);

            }   // end if showTeam or showPlayer

        }   // end of VictoryOverlay Render()


        public static void LoadContent(bool immediate)
        {
            // Read in the textures.
            try
            {
                textureWinner = KoiLibrary.LoadTexture2D(@"Textures\Winner");
                textureGameOver = KoiLibrary.LoadTexture2D(@"Textures\GameOver");
            }
            catch (ContentLoadException e)
            {
                if (e != null)
                {
                    Debug.Assert(false, "Missing content.");
                }
            }

        }   // end of VictoryOverlay LoadContent()

        public static void UnloadContent()
        {
            DeviceResetX.Release(ref textureWinner);
            DeviceResetX.Release(ref textureGameOver);
        }   // end of VictoryOverlay UnloadContent()

        /// <summary>
        /// Recreate render targets.
        /// </summary>
        public static void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class VictoryOverlay

}   // end of namespace Boku
