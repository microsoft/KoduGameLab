
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

namespace Boku
{
    /// <summary>
    /// TODO This is temporary class.  Waiting until we know what this should look like.
    /// </summary>
    /// <param name="score"></param>
    public delegate void ScoreEvent(TeamScore score);
    public class TeamScore
    {
        protected int score = 0;
        protected int prevScore = 0;

        public event ScoreEvent ScoreChanged;

        #region Accessors
        public int Score
        {
            get
            {
                return score;
            }
            set
            {
                if (score != value)
                {
                    prevScore = score;
                    score = value;
                    if (ScoreChanged != null)
                    {
                        ScoreChanged(this);
                    }
                }
            }
        }
        public int PrevScore
        {
            get { return prevScore; }
        }
        #endregion

        public static void Render(TeamScore a, TeamScore b)
        {
            if (a != null)
            {
                String str = "Team A " + a.score.ToString();
                int width = BokuGame.fontBerlinSansFBDemiBold20.MeasureString(str);
                TextHelper.DrawStringWithShadow(BokuGame.fontBerlinSansFBDemiBold20, BokuGame.bokuGame.GraphicsDevice.Viewport.Width / 2 - width - 5, 20, str, Color.Red, Color.Black, false);
            }

            if (b != null)
            {
                String str = "Team B " + b.score.ToString();
                Color brightGreen = new Color(0, 255, 0);
                TextHelper.DrawStringWithShadow(BokuGame.fontBerlinSansFBDemiBold20, BokuGame.bokuGame.GraphicsDevice.Viewport.Width / 2 + 5, 20, str, brightGreen, Color.Black, false);
            }

            VictoryOverlay.Render();
        }

        public void ClearScoreChangedEvents()
        {
            ScoreChanged = null;
        }

    }   // end of class TeamScore

}   // end of namespace Boku


