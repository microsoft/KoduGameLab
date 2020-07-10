// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using System.Xml;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.Input;

namespace Boku.Programming
{
    /// <summary>
    /// Filter that manages the score state and returns a positive action when
    /// a specific score defined by other filters has been passed
    /// 
    /// NOTE: This tile has been archived so it is no longerbeing updated along
    /// with the other scoring tiles.
    /// 
    /// </summary>
    public class ScoreTriggerFilter : Filter
    {
        protected int scoreTriggerValue = 0;

        public ScoreTriggerFilter()
        {
        }

        public override ProgrammingElement Clone()
        {
            ScoreTriggerFilter clone = new ScoreTriggerFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(ScoreTriggerFilter clone)
        {
            base.CopyTo(clone);
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            return true;
        }

        public override bool MatchAction(Reflex reflex, out object param)
        {
            param = null;
            bool match = false;

            if (reflex.targetSet != null && reflex.targetSet.Param != null)
            {
                GameScoredSensor sensor = reflex.Sensor as GameScoredSensor;
                Classification.Colors color = (Classification.Colors) reflex.targetSet.Param;

                int curr = 0, prev;

                Scoreboard.Score score = sensor.GetScore(color);

                if (score != null)
                {
                    curr = score.Curr;
                    prev = score.Prev;

                    // If the score changed and jumped across or landed on the trigger score
                    if ((prev != curr) &&
                        ((prev < scoreTriggerValue && curr >= scoreTriggerValue) ||
                        (prev > scoreTriggerValue && curr <= scoreTriggerValue)))
                    {
                        match = true;
                    }
                }
            }
            return match;
        }

        public override void Reset(Reflex reflex)
        {
            int pointsRandom = 0;
            int pointsBase = 0;
            bool randomScore = false;

            // walk filters and set params if found
            //
            for (int indexFilter = 0; indexFilter < reflex.Filters.Count; indexFilter++)
            {
                Filter filter = reflex.Filters[indexFilter] as Filter;
                if (filter != null)
                {
                    if (filter is ScoreFilter)
                    {
                        ScoreFilter scoreFilter = filter as ScoreFilter;
                        if (randomScore)
                        {
                            pointsRandom += scoreFilter.points;
                        }
                        else
                        {
                            pointsBase += scoreFilter.points;
                        }
                    }
                    if (filter is RandomFilter)
                    {
                        randomScore = true;
                    }
                }

                if (randomScore && pointsRandom == 0)
                    pointsRandom = 5;
            }

            this.scoreTriggerValue = pointsBase + BokuGame.bokuGame.rnd.Next(pointsRandom);

            base.Reset(reflex);
        }
    }
}
