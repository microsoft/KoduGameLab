
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Programming;

namespace Boku.Common
{
    using Score = Scoreboard.Score;

    /// <summary>
    /// Class which encapsulates a set of scores.
    /// Original use is for adding local variables to each actor.
    /// Future refactoring should include using one of these for the global scores.
    /// </summary>
    public class ScoreSet
    {
        #region Members

        Dictionary<int, Score> scores;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public ScoreSet()
        {
            scores = new Dictionary<int, Score>();

            for (ScoreBucket bucket = ScoreBucket.ScoreA; bucket <= ScoreBucket.ScoreZ; ++bucket)
            {
                Score score = new Score();
                scores.Add((int)bucket, score);
            }

        }   // end of c'tor

        /// <summary>
        /// Resets some or all properties on all scores.
        /// </summary>
        /// <param name="flags"></param>
        public void Reset(ScoreResetFlags flags)
        {
            foreach (Score score in scores.Values)
            {
                score.Reset(flags);
            }
        }   // end of Reset()

        /// <summary>
        /// Resets some or all properties of the score corresponding to the given bucket.
        /// </summary>
        /// <param name="bucket"></param>
        /// <param name="flags"></param>
        public void ResetScore(ScoreBucket bucket, ScoreResetFlags flags)
        {
            if (bucket == ScoreBucket.NotApplicable)
            {
                Reset(flags);
            }
            else if (scores.ContainsKey((int)bucket))
            {
                scores[(int)bucket].Reset(flags);
            }
        }   // endof ResetScore()

        /// <summary>
        /// Bring scoreboard up to date so that previous values equal current ones.
        /// </summary>
        public void FreshenScores()
        {
            foreach (Score score in scores.Values)
            {
                score.Prev = score.Curr;
            }
        }   // end of FreshenScores()

        /// <summary>
        /// Make a copy of the current state of the ScoreSet.
        /// </summary>
        /// <param name="scoresCopy"></param>
        public void Snapshot(Dictionary<int, Scoreboard.Score> scoresCopy)
        {
            foreach(KeyValuePair<int, Score> kvp in scores)
            {
                // Check if copy already has a matching entry.  If not, add one.
                Score score;
                if(!scoresCopy.TryGetValue(kvp.Key, out score))
                {
                    score = new Score();
                    scoresCopy.Add(kvp.Key, score);
                }

                // Update values.
                score.Prev = kvp.Value.Prev;
                score.Curr = kvp.Value.Curr;
            }
        }   // end of Snapshot()

        /// <summary>
        /// Get the current value of a score.
        /// </summary>
        /// <param name="bucket"></param>
        /// <returns></returns>
        public int GetScore(ScoreBucket bucket)
        {
            return scores[(int)bucket].Curr;
        }

        /// <summary>
        /// Get the value of the score in the previous frame.
        /// </summary>
        /// <param name="bucket"></param>
        /// <returns></returns>
        public int GetPrevScore(ScoreBucket bucket)
        {
            return scores[(int)bucket].Prev;
        }

        public bool IsColorBucket(ScoreBucket bucket)
        {
            return bucket >= ScoreBucket.ColorFirst && bucket <= ScoreBucket.ColorLast;
        }

        /// <summary>
        /// Set the current value of a score.
        /// </summary>
        /// <param name="bucket">The bucket of the score register</param>
        /// <param name="value">The value the score should become</param>
        public void SetScore(ScoreBucket bucket, int value)
        {
            Score score = scores[(int)bucket];
            score.Curr = value;
        }   // end of SetScore()

        #endregion

        #region Internal
        #endregion

    }   // end of class ScoreSet
}   // end of namespace Boku.Common
