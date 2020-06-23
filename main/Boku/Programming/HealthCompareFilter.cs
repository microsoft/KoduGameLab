using System;
using System.Collections.Generic;

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
    /// <summary>
    /// Filter that returns true when the compare condition is met on the actor's health. One of:
    ///  Is - trigger when the actor's health becomes the given value.
    ///  Above - trigger when the actor's health becomes above the given value.
    ///  Below - trigger when the actor's health becomes below the given value.
    ///  
    /// NOTE These filters have been archived in favor of using the score comparison filters.
    /// </summary>
    public class HealthCompareFilter : Filter
    {
        public enum HealthCompare
        {
            Is,
            Above,
            Below
        }

        [XmlAttribute]
        public HealthCompare op = HealthCompare.Is;

        protected int triggerValue = 0;

        public HealthCompareFilter()
        {
        }

        public override ProgrammingElement Clone()
        {
            HealthCompareFilter clone = new HealthCompareFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(HealthCompareFilter clone)
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
            bool match = false;

            int curr = reflex.Task.Brain.GameActor.HitPoints;
            int prev = reflex.Task.Brain.GameActor.PrevHitPoints;

            switch (op)
            {
                case HealthCompare.Is:
                    //match = (curr == triggerValue) && (prev != triggerValue);
                    match = (curr == triggerValue);
                    break;

                case HealthCompare.Above:
                    //match = (curr > triggerValue) && (prev <= triggerValue);
                    match = (curr > triggerValue);
                    break;

                case HealthCompare.Below:
                    //match = (curr < triggerValue) && (prev >= triggerValue);
                    match = (curr < triggerValue);
                    break;
            }

            return match;
        }

        public override void Reset(Reflex reflex)
        {
            int pointsRandom = 0;
            int pointsBase = 0;
            bool randomScore = false;
            int pointsTotal = 0;

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
                    
                    if (filter is ScoreBucketFilter)
                    {
                    }

                    if (filter is RandomFilter)
                    {
                        randomScore = true;
                    }
                }

                pointsTotal = pointsBase;
                // Add in random, may be negative.
                if (pointsRandom >= 0)
                {
                    pointsTotal += BokuGame.bokuGame.rnd.Next(pointsRandom);
                }
                else
                {
                    pointsTotal -= BokuGame.bokuGame.rnd.Next(-pointsRandom);
                }
            }

            this.triggerValue = pointsTotal;

            base.Reset(reflex);
        }
    }
}
