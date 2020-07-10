// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
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

namespace Boku.Programming
{
    public class ScoreCompareFilter : Filter
    {
        public enum ScoreCompare
        {
            Is,
            Above,
            Below,
            GTEQ,           // Greater than or equal to
            LTEQ,           // Less than or equal to
            NotIs
        }

        [XmlAttribute]
        public ScoreCompare op = ScoreCompare.Is;

        protected int scoreTriggerValue = -1;

        public ScoreCompareFilter()
        {
        }

        public override ProgrammingElement Clone()
        {
            ScoreCompareFilter clone = new ScoreCompareFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(ScoreCompareFilter clone)
        {
            base.CopyTo(clone);
            clone.op = this.op;
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            return true;
        }

        public override bool MatchAction(Reflex reflex, out object param)
        {
            param = null;
            // Previously the default here was false.  But when I removed the hiddendefault comparison
            // this started causing timers with random times to fail.  HiddenDefault is evil and
            // should never exist.
            // At a deeper level there's something not quite right with the architecture if it doesn't
            // allow scores to be used as timer inputs without adding hidden tiles.  Need to understand 
            // this better.
            bool match = true;

            if (reflex.targetSet != null && reflex.targetSet.Param != null)
            {
                GameScoredSensorResult sensorResult = (GameScoredSensorResult)reflex.targetSet.Param;

                Debug.Assert(sensorResult != null, "Why would this ever be null?");

                // Just looking to see if that bucket has changed.
                if (sensorResult.TestingForChange)
                {
                    match = sensorResult.ScoreChanged;
                }
                else
                {
                    // Actually doing a comparison.
                    switch (op)
                    {
                        case ScoreCompare.Is:
                            match = sensorResult.LeftValue == sensorResult.RightValue;
                            break;

                        case ScoreCompare.Above:
                            match = sensorResult.LeftValue > sensorResult.RightValue;
                            break;

                        case ScoreCompare.Below:
                            match = sensorResult.LeftValue < sensorResult.RightValue;
                            break;

                        case ScoreCompare.GTEQ:
                            match = sensorResult.LeftValue >= sensorResult.RightValue;
                            break;

                        case ScoreCompare.LTEQ:
                            match = sensorResult.LeftValue <= sensorResult.RightValue;
                            break;

                        case ScoreCompare.NotIs:
                            match = sensorResult.LeftValue != sensorResult.RightValue;
                            break;
                    }
                }

            }   // if not null target set

            return match;

        }   // end of MatchAction()

        public override void Reset(Reflex reflex)
        {
            int pointsRandom = 0;
            int pointsBase = 0;
            bool randomScore = false;

            int scoreCount = 0;

            // walk filters and set params if found
            //
            for (int indexFilter = 0; indexFilter < reflex.Filters.Count; indexFilter++)
            {
                Filter filter = reflex.Filters[indexFilter] as Filter;
                if (filter != null)
                {
                    if (filter is ScoreFilter)
                    {
                        scoreCount += 1;

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
                {
                    pointsRandom = 5;
                }
            }


            if (scoreCount > 0)
            {
                scoreTriggerValue = pointsBase + BokuGame.bokuGame.rnd.Next(pointsRandom);
            }
            else
            {
                scoreTriggerValue = -1;
            }

            base.Reset(reflex);
        }
    }
}
