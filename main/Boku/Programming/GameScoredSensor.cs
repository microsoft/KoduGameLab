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
using Boku.SimWorld;

namespace Boku.Programming
{
    /// <summary>
    /// Senses when the a game score has changed.
    /// Actually this has also mutated to being use to compare scores against a value
    /// and against each other.  Because of this we need something better for the 
    /// param than just a scorebucket.  Hence the GameScoredSensorResult class.
    /// 
    /// This sensor demonstrates one of the few Event based sensors.  
    /// It will only get updated when the actual score changes.
    /// </summary>
    public class GameScoredSensor : Sensor
    {
        int frame;
        Dictionary<int, Scoreboard.Score> scores = new Dictionary<int, Scoreboard.Score>();

        public override ProgrammingElement Clone()
        {
            GameScoredSensor clone = new GameScoredSensor();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(GameScoredSensor clone)
        {
            base.CopyTo(clone);
        }

        public Scoreboard.Score GetScore(Classification.Colors color)
        {
            Scoreboard.Score score;
            if (scores.TryGetValue((int)color, out score))
                return score;
            return null;
        }

        public override void Reset(Reflex reflex)
        {
            frame = 0;
            scores.Clear();

            base.Reset(reflex);
        }

        public override void StartUpdate(GameActor gameActor)
        {
            // We need at least two frames to detect changes in score.
            if (frame > 0)
                Scoreboard.Snapshot(scores);
            frame += 1;
        }

        public override void ThingUpdate(GameActor gameActor, GameThing gameThing, Vector3 direction, float range)
        {
        }

        public override void FinishUpdate(GameActor gameActor)
        {
        }


        public override void ComposeSensorTargetSet(GameActor gameActor, Reflex reflex)
        {
            reflex.targetSet.Param = Filter.ScoresFromFilterSet(gameActor, reflex);

            reflex.targetSet.Action = TestObjectSet(reflex);
        }
    }   // end of class GameScoredSensor

    /// <summary>
    /// A wrapper class to contain all the options/data for the Scored sensor.
    /// The values depend on how the sensor is used.
    /// 
    /// --Testing for change.
    /// WHEN Red DO
    ///     TestingForChange will be true.
    ///     ScoreChanged will tell you whether the score has changed this frame.
    ///   
    /// --Testing against a value.
    /// WHEN Red 5Points 2Points Green
    ///     Left will contain the value for red.
    ///     Right will contain value to compare against.
    ///     CompareOp will contain the op to apply.
    /// 
    /// --Comparisons
    /// WHEN Red White 2Points > Blue Blue Random Green 5Points
    ///     Left will contain the sum of the values to the left of the comparison.
    ///     Right will contain the sum of the values to the right of the comparison.
    ///     CompareOp will contain the op to apply.
    ///  
    /// </summary>
    public class GameScoredSensorResult
    {
        public bool TestingForChange = false;
        public bool ScoreChanged = false;

        public int LeftValue = 0;
        public int RightValue = 0;

        public ScoreCompareFilter.ScoreCompare CompareOp = ScoreCompareFilter.ScoreCompare.Is;

    }   // end of class GameScoredSensorResult

}   // end of namespace Boku.Programming
