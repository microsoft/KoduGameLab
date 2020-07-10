// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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
using Boku.SimWorld;
using Boku.Common;
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
        /// PreGame which displays a count-down timer before starting the game.
        /// </summary>
        public class PreGameRacing : PreGameBase
        {
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

            // c'tor
            public PreGameRacing()
            {
            }   // end of PreGameRacing c'tor

            public override void Update()
            {
                if (Active)
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
                }

                base.Update();
            }   // end of PreGameRacing Update()

            public override void Render(Camera camera)
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
            }   // end of PreGameRacing Render()

            protected override void Activate()
            {
                // Pause the game clock.
                // Instead of fully pausing the clock just let it run extremely slow.  This still
                // lets updates happen as expected and results in objects appearing when they
                // should and the camera moving to where it needs to be.
                //Time.Paused = true;
                Time.ClockRatio = 0.0001f;

                startTime = Time.WallClockTotalSeconds;

                texture1 = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Count1");
                texture2 = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Count2");
                texture3 = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Count3");

                phase = 4;

                base.Activate();
            }   // end of PreGameRacing Activate()

            protected override void Deactivate()
            {
                // Restart the game clock.
                //Time.Paused = false;
                Time.ClockRatio = 1.0f;

                texture1 = null;
                texture2 = null;
                texture3 = null;

                base.Deactivate();
            }   // end of PreGameRacing Deactivate()

        }   // end of class PreGameRacing

    }   // end of class InGame

}   // end of namespace Boku


