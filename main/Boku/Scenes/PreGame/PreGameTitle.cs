
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
        public class PreGameTitle : PreGameBase
        {
            private double startTime = 0.0;
            private double duration = 3.0;

            // c'tor
            public PreGameTitle()
            {
            }   // end of PreGameTitle c'tor

            public override void Update()
            {
                if (Active)
                {
                    if (Time.WallClockTotalSeconds > startTime + duration)
                    {
                        // We're done.
                        Active = false;
                    }
                }

                base.Update();
            }   // end of PreGameTitle Update()

            public override void Render(Camera camera)
            {
                Point center = new Point(BokuGame.bokuGame.GraphicsDevice.Viewport.Width / 2, BokuGame.bokuGame.GraphicsDevice.Viewport.Height / 2);

                string title = TextHelper.FilterInvalidCharacters(InGame.CurrentWorldName);

                if (title != null)
                {
                    UI2D.Shared.GetFont Font = UI2D.Shared.GetGameFont30Bold;

                    int width = (int)Font().MeasureString(TextHelper.FilterInvalidCharacters(title)).X;
                    center.X -= width / 2;
                    center.Y -= 30;

                    float alpha = 1.0f;

                    float dt = (float)(Time.WallClockTotalSeconds - startTime);

                    if (dt < 0.5)
                    {
                        alpha = dt * 2.0f;
                    }
                    else if (dt > 2.0f)
                    {
                        alpha = Math.Max(0.0f, 3.0f - dt);
                    }

                    TextHelper.DrawStringNoBatch(Font, title, new Vector2(center.X + 2, center.Y + 2), Color.White, outlineColor: Color.Black, outlineWidth: 1.5f);
                }
            }   // end of PreGameTitle Render()

            protected override void Activate()
            {
                // Pause the game clock.
                // Instead of fully pausing the clock just let it run extremely slow.  This still
                // lets updates happen as expected and results in objects appearing when they
                // should and the camera moving to where it needs to be.
                //Time.Paused = true;
                Time.ClockRatio = 0.0001f;

                startTime = Time.WallClockTotalSeconds;

                base.Activate();
            }   // end of PreGameTitle Activate()

            protected override void Deactivate()
            {
                // Restart the game clock.
                //Time.Paused = false;
                Time.ClockRatio = 1.0f;

                base.Deactivate();
            }   // end of PreGameTitle Deactivate()

        }   // end of class PreGameTitle

    }   // end of class InGame

}   // end of namespace Boku


