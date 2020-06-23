
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

using Boku.Base;
using Boku.SimWorld;
using Boku.SimWorld.Terra;
using Boku.Common;
using Boku.Common.TutorialSystem;
using Boku.Programming;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.Fx;

namespace Boku
{
    public partial class InGame : GameObject, INeedsDeviceReset
    {
        /// <summary>
        /// PreGame which displays the level description on the screen.
        /// </summary>
        public class PreGameDesc : PreGameBase
        {
            #region Members

            private TextBlob blob = null;
            private string logo = null;

            #endregion

            public string Logo
            {
                set { logo = value; }
            }

            #region Public

            // c'tor
            public PreGameDesc()
            {
            }   // end of PreGameDesc c'tor

            public override void Update()
            {
                if (Active)
                {
                    // If we're in tutorial mode, don't keep showing pregame to user.
                    // We do, however, let it be shown right at the beginning, hence the
                    // test of the CurStepIndex.
                    if (TutorialManager.Active && TutorialManager.CurStepIndex > 2)
                    {
                        Active = false;
                    }

                    if (Actions.Select.WasPressed || Actions.X.WasPressed)
                    {
                        Actions.Select.ClearAllWasPressedState();
                        Actions.X.ClearAllWasPressedState();

                        Active = false;

                        // Don't let current pressed state leak into game.
                        Actions.Select.IgnoreUntilReleased();
                        Actions.X.IgnoreUntilReleased();
                    }

                    // Exit pre-game mode.
                    if (KeyboardInput.WasPressed(Keys.Escape))
                    {
                        KeyboardInput.ClearAllWasPressedState(Keys.Escape);
                        Active = false;
                    }

                    if (Actions.ToolMenu.WasPressed)
                    {
                        Actions.ToolMenu.ClearAllWasPressedState();

                        Active = false;
                        InGame.inGame.CurrentUpdateMode = UpdateMode.ToolMenu;
                    }

                    if (Actions.MiniHub.WasPressed)
                    {
                        Actions.MiniHub.ClearAllWasPressedState();

                        // Leave active so when we come out of the mini-hub we know to re-start pregame.
                        //Active = false;
                        InGame.inGame.SwitchToMiniHub();
                    }

                    // Check if user click on bottom text.
                    if (MouseInput.Left.WasPressed && HelpOverlay.MouseHitBottomText(MouseInput.Position))
                    {
                        Active = false;
                        MouseInput.Left.ClearAllWasPressedState();
                    }
                }
            }   // end of PreGameDesc Update()

            public override void Render(Camera camera)
            {
                Texture2D logoTexture = null;
                if (!string.IsNullOrEmpty(logo))
                {
                    switch(logo)
                    {
                        case "n23" :
                            logoTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\NASA_JPL");
                            break;
                        default :
                            logoTexture = null;
                            break;
                    }
                }

                Vector2 pos = Vector2.Zero;
                pos.X = BokuGame.bokuGame.GraphicsDevice.Viewport.Width / 4.0f;
                pos.Y = BokuGame.bokuGame.GraphicsDevice.Viewport.Height / 2.0f - blob.NumLines / 2.0f * blob.Font().LineSpacing;

                if (logoTexture != null)
                {
                    Vector2 logoSize = new Vector2(logoTexture.Width, logoTexture.Height);

                    ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();
                    // Position logo in upper right corner.
                    Vector2 logoPos = new Vector2(BokuGame.bokuGame.GraphicsDevice.Viewport.Width * 0.98f - logoSize.X, BokuGame.bokuGame.GraphicsDevice.Viewport.Width * 0.02f);
                    // Force to be pixel aligned.
                    logoPos.X = (int)logoPos.X;
                    logoPos.Y = (int)logoPos.Y;
                    ssquad.Render(logoTexture, logoPos, logoSize, "TexturedRegularAlpha");
                }

                blob.RenderWithButtons(pos, Color.White, outlineColor: Color.Black, outlineWidth: 1.5f, maxLines: 20);

            }   // end of PreGameDesc Render()

            #endregion

            #region Internal

            protected override void Activate()
            {
                // Pause the game clock.
                // Instead of fully pausing the clock just let it run extremely slow.  This still
                // lets updates happen as expected and results in objects appearing when they
                // should and the camera moving to where it needs to be.
                //Time.Paused = true;
                Time.ClockRatio = 0.0001f;

                base.Activate();

                HelpOverlay.Push(@"PreGameDescription");

                UI2D.Shared.GetFont Font = BokuGame.bokuGame.GraphicsDevice.Viewport.Height < 720 ? UI2D.Shared.GetGameFont18Bold : UI2D.Shared.GetGameFont24Bold;
                blob = new TextBlob(Font, Terrain.Current.XmlWorldData.name +"\n\n" + Terrain.Current.XmlWorldData.description, (int)(BokuGame.bokuGame.GraphicsDevice.Viewport.Width / 2));
                blob.Justification = Terrain.Current.XmlWorldData.descJustification;
            
            }   // end of PreGameDesc Activate()

            protected override void Deactivate()
            {
                // Restart the game clock.
                //Time.Paused = false;
                Time.ClockRatio = 1.0f;

                HelpOverlay.Pop();

                base.Deactivate();
            }   // end of PreGameDesc Deactivate()

            #endregion

        }   // end of class PreGameDesc

    }   // end of class InGame

}   // end of namespace Boku


