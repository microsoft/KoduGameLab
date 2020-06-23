
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
using Boku.Common.Gesture;
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
        /// PreGame which displays the level description on the screen before starting a count-down timer.
        /// </summary>
        public class PreGameRacingWithDesc : PreGameBase
        {
            #region Members

            private TextBlob blob = null;

            private bool showingDescription = false;

            private double startTime = 0.0;
            private double duration = 3.0;

            private Texture2D texture1 = null;
            private Texture2D texture2 = null;
            private Texture2D texture3 = null;

            private string[] cueNames = new string[4]
            {
                "Go",
                "One",
                "Two",
                "Three"
            };

            private int phase = 4;

            #endregion

            #region Public

            // c'tor
            public PreGameRacingWithDesc()
            {
            }   // end of PreGameRacingWithDesc c'tor

            public override void Update()
            {
                if (Active)
                {
                    if (showingDescription)
                    {
                        // Waiting for A to be pressed.
                        if (Actions.Select.WasPressed)
                        {
                            Actions.Select.ClearAllWasPressedState();

                            showingDescription = false;
                            startTime = Time.WallClockTotalSeconds;
                            HelpOverlay.Pop();

                            // Don't let current pressed state leak into game.
                            GamePadInput.IgnoreUntilReleased(Buttons.A);
                        }

                        if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                        {
                            // Check if user click on bottom text.
                            if (MouseInput.Left.WasPressed && HelpOverlay.MouseHitBottomText(MouseInput.Position))
                            {
                                showingDescription = false;
                                startTime = Time.WallClockTotalSeconds;
                                HelpOverlay.Pop();

                                MouseInput.Left.ClearAllWasPressedState();
                            }
                        }

                        if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                        {
                            // Check if user click on bottom text.
                            if (TouchGestureManager.Get().TapGesture.WasTapped() &&
                                HelpOverlay.MouseHitBottomText(TouchInput.GetAsPoint(TouchInput.GetOldestTouch().position))
                                )
                            {
                                showingDescription = false;
                                startTime = Time.WallClockTotalSeconds;
                                HelpOverlay.Pop();
                            }
                        }

                    } 
                    else
                    {
                        int oldPhase = phase;
                        phase = (int)Math.Ceiling(startTime + duration - Time.WallClockTotalSeconds);
                        if (oldPhase != phase)
                        {
                            if ((phase < cueNames.Length) && (phase >= 0))
                            {
                                BokuGame.Audio.GetCue(cueNames[phase]).Play();
                            }
                        }
                        if (Time.WallClockTotalSeconds > startTime + duration)
                        {
                            // We're done.
                            Active = false;
                        }

                        // Check if user click on bottom text.
                        if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                        {
                            // Check if user click on bottom text.
                            if (TouchGestureManager.Get().TapGesture.WasTapped() &&
                                HelpOverlay.MouseHitBottomText(TouchInput.GetAsPoint(TouchInput.GetOldestTouch().position))
                                )
                            {
                                Active = false;
                            }
                        }
                        else if (MouseInput.Left.WasPressed && HelpOverlay.MouseHitBottomText(MouseInput.Position))
                        {
                            Active = false;
                            MouseInput.Left.ClearAllWasPressedState();
                        }
                    }
                }

                base.Update();
            }   // end of PreGameRacingWithDesc Update()

            public override void Render(Camera camera)
            {
                if (showingDescription)
                {
                    Vector2 pos = Vector2.Zero;
                    pos.X = BokuGame.bokuGame.GraphicsDevice.Viewport.Width / 4.0f;
                    pos.Y = BokuGame.bokuGame.GraphicsDevice.Viewport.Height / 2.0f - blob.NumLines / 2.0f * blob.Font().LineSpacing;
                    blob.RenderWithButtons(pos, Color.White, outlineColor: Color.Black, outlineWidth: 1.5f, maxLines: 20);
                }
                else
                {
                    ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();

                    Vector2 center = 0.5f * new Vector2(BokuGame.bokuGame.GraphicsDevice.Viewport.Width, BokuGame.bokuGame.GraphicsDevice.Viewport.Height);
                    Vector2 size = new Vector2(256.0f);

                    // Pick the right number texture to show.
                    double dt = Time.WallClockTotalSeconds - startTime;
                    Texture2D texture = texture3;
                    if (dt > 2.0)
                    {
                        texture = texture1;
                        dt -= 2.0f;
                    }
                    else if (dt > 1.0)
                    {
                        texture = texture2;
                        dt -= 1.0f;
                    }

                    size *= 1.0f + 2.0f * (float)dt;
                    center -= 0.5f * size;

                    Vector4 color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f - (float)dt);

                    quad.Render(texture, color, center, size, "TexturedRegularAlpha");
                }
            }   // end of PreGameRacingWithDesc Render()

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

                texture1 = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Count1");
                texture2 = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Count2");
                texture3 = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Count3");

                base.Activate();

                showingDescription = true;
                HelpOverlay.Push(@"PreGameDescription");

                UI2D.Shared.GetFont Font = BokuGame.bokuGame.GraphicsDevice.Viewport.Height < 720 ? UI2D.Shared.GetGameFont18Bold : UI2D.Shared.GetGameFont24Bold;
                blob = new TextBlob(Font, Terrain.Current.XmlWorldData.name + "\n\n" + Terrain.Current.XmlWorldData.description, (int)(BokuGame.bokuGame.GraphicsDevice.Viewport.Width / 2));
                blob.Justification = Terrain.Current.XmlWorldData.descJustification;

            }   // end of PreGameRacingWithDesc Activate()

            protected override void Deactivate()
            {
                // Restart the game clock.
                //Time.Paused = false;
                Time.ClockRatio = 1.0f;

                texture1 = null;
                texture2 = null;
                texture3 = null;

                // We need to be sure this really needs popping.  It may be off already.
                if (HelpOverlay.Peek() == @"PreGameDescription")
                {
                    HelpOverlay.Pop();
                }

                base.Deactivate();
            }   // end of PreGameRacingWithDesc Deactivate()

            #endregion

        }   // end of class PreGameRacingWithDesc

    }   // end of class InGame

}   // end of namespace Boku


