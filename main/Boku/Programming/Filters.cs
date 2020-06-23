
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

namespace Boku.Programming
{
    /// <summary>
    /// Filters represent the mechanism to narrow and define the input that comes from the Sensors
    /// before it is handed to the selector
    /// Can also be loosly thought of as an input parameter in some cases
    /// </summary>
    public abstract class Filter : ProgrammingElement
    {
        private BitArray outputs = new BitArray((int)SensorOutputType.SIZEOF);

        int outputCount;

        [XmlIgnore]
        public BitArray Outputs { get { return outputs; } }


        [XmlIgnore]
        public int OutputCount { get { return outputCount; } }


        [XmlArray]
        [XmlArrayItem("Output")]
        public List<SensorOutputType> XmlOutputs = new List<SensorOutputType>();


        protected void CopyTo(Filter clone)
        {
            base.CopyTo(clone);
            clone.XmlOutputs = this.XmlOutputs;
            clone.outputs = this.outputs;
            clone.outputCount = this.outputCount;
        }

        public override void OnLoad()
        {
            base.OnLoad();


            outputs.SetAll(false);

            outputCount = 0;

            foreach (SensorOutputType type in XmlOutputs)
            {
                outputs.Set((int)type, true);
                outputCount += 1;
            }
        }

        /// <summary>
        /// Filters objects, will be called for every GameThing the Sensor senses.
        /// This function is called during the first pass over the list of sensed
        /// objects.  The purpose of this is to cull that list of items that don't
        /// match the filter.
        /// </summary>
        /// <returns>false if the sensorTarget does not match the filter and should be excluded</returns>
        public abstract bool MatchTarget(Reflex reflex, SensorTarget sensorTarget);

        /// <summary>
        /// Filters the action, will be called once for each set of Sensed objects.
        /// After the MatchTarget() has been used to cull the list of objects produced by
        /// the sensor, we have to apply MatchAction to the resulting set of objects.
        /// MatchAction does not cull the list like MatchTarget does, rather it looks
        /// at the list as a whole and determines if the action should still be taken.
        /// For example:
        ///     When See Kodu Blue Many Do something...
        /// 'Kodu' and 'Blue' are filters which only have MatchTarget().  Once they
        /// have culled the list down to only contain blue kodus, then the 'Many'
        /// filter's MatchAction method is called to determine if the resulting
        /// list as a whole still passes the filter stage.
        /// 
        /// Note that for input filters we've implemented them with MatchAction().
        /// I'm not clear why this is or if it even makes a difference.  (****)
        /// </summary>
        /// <returns>false if the complete Sensed set should be ignored</returns>
        public abstract bool MatchAction(Reflex reflex, out object param);

        public virtual void PostProcessAction(bool firing, Reflex reflex, ref bool action) { }

        private static BitArray scratchCategories = new BitArray((int)BrainCategories.SIZEOF);
        private static BitArray scratchExclusions = new BitArray((int)BrainCategories.SIZEOF);
        private static BitArray scratchOutputs = new BitArray((int)SensorOutputType.SIZEOF);

        public override bool ReflexCompatible(GameActor actor, ReflexData reflex, ProgrammingElement replacedElement, bool allowArchivedCategories)
        {
            // A sensor must exist before any filters may appear.
            if (reflex.Sensor == null || reflex.Sensor is NullSensor)
                return false;

            // Check filter instance count
            {
                string selectionUpid = (replacedElement != null) ? replacedElement.upid : null;

                int count = reflex.GetFilterCount(this.upid);

                // Don't consider the selected one if it's of our type, since we'd be replacing it.
                if (selectionUpid == this.upid)
                    count -= 1;

                if (count >= this.MaxInstanceCount)
                    return false;
            }

            // Check filter class count
            {
                Type selectionType = (replacedElement != null) ? replacedElement.GetType() : null;

                int count = reflex.GetFilterCountByType(this.GetType());

                // Don't consider the selected one if it's of our type, since we'd be replacing it.
                if (selectionType == this.GetType())
                    count -= 1;

                if (count >= this.MaxClassCount)
                    return false;
            }

            return base.ReflexCompatible(actor, reflex, replacedElement, allowArchivedCategories);
        }

        /// <summary>
        /// Gets the current score values based of the set of filters.
        /// This used to return the ScoreBucket but by calculating the scores here
        /// we can allow richer comparisons.
        /// The Health Filter can also be used as a value as can SettingsFilters.
        /// 
        /// The result values depend on how the sensor is used.
        /// 
        /// --Testing for Health
        /// WHEN Health Comparison Points
        /// 
        /// --Testing for change.
        /// WHEN Red DO
        ///     Bucket will contain the bucket to test for changed value from the previous frame.
        ///   
        /// --Testing against a value.
        /// WHEN Red 5Points 2Points Green
        ///     Left will contain the value for red.
        ///     Right will contain value to compare against.
        ///     CompareOp will contain the op to apply, in this case equals.
        /// 
        /// --Comparisons
        /// WHEN Red White 2Points > Blue Blue Random Green 5Points
        ///     Left will contain the sum of the values to the left of the comparison.
        ///     Right will contain the sum of the values to the right of the comparison.
        ///     CompareOp will contain the op to apply.
        ///     
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="reflex"></param>
        /// <returns></returns>
        public static GameScoredSensorResult ScoresFromFilterSet(GameActor actor, Reflex reflex)
        {
            List<Filter> filters = reflex.Filters;

            GameScoredSensorResult result = new GameScoredSensorResult();

            bool testingHealth = reflex.sensorUpid == "sensor.health";

            if (!testingHealth && TestScoreChange(filters))
            {
                // Find ScoreBucketFilter and grab it's bucket.
                foreach (Filter filter in filters)
                {
                    ScoreBucketFilter sbf = filter as ScoreBucketFilter;
                    if (sbf != null)
                    {
                        int curScore = Scoreboard.GetScore(actor, sbf);
                        int prevScore = Scoreboard.GetPrevScore(actor, sbf);

                        result.TestingForChange = true;
                        result.ScoreChanged = curScore != prevScore;
                    }
                }
            }
            else if (!testingHealth && TestScoreValue(filters))
            {
                bool hasScoreBucket = false;  // Do we have a scorebucket in our list of filters?
                int scoreBucketIndex = -1;
                for (int i = 0; i < filters.Count; i++)
                {
                    if (filters[i] is ScoreBucketFilter)
                    {
                        hasScoreBucket = true;
                        scoreBucketIndex = i;
                    }
                }

                Filter specialFilter = null;    // Will be either RandomFilter or PercentFiter if found.
                int pointsPostSpecial = 0;

                if (hasScoreBucket)
                {
                    // Compare first scorebucket value to sum of all other values.

                    // Use first found scorebucket as left value.
                    result.LeftValue = Scoreboard.GetScore(actor, filters[scoreBucketIndex] as ScoreBucketFilter);

                    // Sum remaining tiles for right value.
                    for (int i = 0; i < filters.Count; i++)
                    {
                        if (i == scoreBucketIndex)
                        {
                            // Skip over the first scorebucket filter.
                            continue;
                        }

                        if (filters[i] is RandomFilter || filters[i] is PercentFilter)
                        {
                            Debug.Assert(specialFilter == null, "Two special tiles should not be valid here.");
                            specialFilter = filters[i];
                        }

                        ScoreFilter sf = filters[i] as ScoreFilter;
                        if (sf != null)
                        {
                            if (specialFilter != null)
                            {
                                pointsPostSpecial += sf.points;
                            }
                            else
                            {
                                result.RightValue += sf.points;
                            }
                        }

                        if (filters[i] is HealthFilter)
                        {
                            if (specialFilter != null)
                            {
                                pointsPostSpecial += actor.HitPoints;
                            }
                            else
                            {
                                result.RightValue += actor.HitPoints;
                            }
                        }

                        SettingsFilter settings = filters[i] as SettingsFilter;
                        if (settings != null)
                        {
                            if (specialFilter != null)
                            {
                                pointsPostSpecial += actor.GetSettingsValue(actor, settings.name);
                            }
                            else
                            {
                                result.RightValue += actor.GetSettingsValue(actor, settings.name);
                            }
                        }
                    }   // end of loop over filters.

                }
                else
                {
                    // No scorebucket exists therefore compare the total to the Red score.

                    // Use red bucket as left value.
                    result.LeftValue = Scoreboard.GetGlobalScore((ScoreBucket)Classification.Colors.Red);

                    // Sum point tiles for right value.
                    for (int i = 0; i < filters.Count; i++)
                    {
                        if (filters[i] is RandomFilter || filters[i] is PercentFilter)
                        {
                            Debug.Assert(specialFilter == null, "Two special tiles should not be valid here.");
                            specialFilter = filters[i];
                        }

                        ScoreFilter sf = filters[i] as ScoreFilter;
                        if (sf != null)
                        {
                            if (specialFilter != null)
                            {
                                pointsPostSpecial += sf.points;
                            }
                            else
                            {
                                result.RightValue += sf.points;
                            }
                        }

                        if (filters[i] is HealthFilter)
                        {
                            if (specialFilter != null)
                            {
                                pointsPostSpecial += actor.HitPoints;
                            }
                            else
                            {
                                result.RightValue += actor.HitPoints;
                            }
                        }

                        SettingsFilter settings = filters[i] as SettingsFilter;
                        if (settings != null)
                        {
                            if (specialFilter != null)
                            {
                                pointsPostSpecial += actor.GetSettingsValue(actor, settings.name);
                            }
                            else
                            {
                                result.RightValue += actor.GetSettingsValue(actor, settings.name);
                            }
                        }

                    }   // end of loop over filters.
                }

                // Calc effect of special tiles.
                if (specialFilter != null)
                {
                    if (specialFilter is RandomFilter)
                    {
                        if (pointsPostSpecial > 0)
                        {
                            result.RightValue += BokuGame.bokuGame.rnd.Next(pointsPostSpecial);
                        }
                        else if (pointsPostSpecial < 0)
                        {
                            result.RightValue -= BokuGame.bokuGame.rnd.Next(-pointsPostSpecial);
                        }
                    }
                    if (specialFilter is PercentFilter)
                    {
                        result.RightValue = (int)Math.Round((float)result.RightValue / 100.0f * (float)pointsPostSpecial);
                    }
                }

                // Set op to equals.
                result.CompareOp = ScoreCompareFilter.ScoreCompare.Is;
            }
            else if (TestComparison(filters))
            {
                // If testing health, the lhs is just the # hitkpoints.
                if (testingHealth)
                {
                    result.LeftValue = actor.HitPoints;
                }

                // Sum left side.
                Filter specialFilter = null;    // Will be either RandomFilter or PercentFiter if found.
                int pointsPostSpecial = 0;

                int filterIndex = 0;
                for (; filterIndex < filters.Count; filterIndex++)
                {
                    // Did we find a comparison filter, if so we're done with the left side.
                    if (filters[filterIndex] is ScoreCompareFilter)
                    {
                        break;
                    }

                    // Did we find a special filter?
                    if (filters[filterIndex] is RandomFilter || filters[filterIndex] is PercentFilter)
                    {
                        Debug.Assert(specialFilter == null, "Two special tiles should not be valid here.");
                        specialFilter = filters[filterIndex];
                    }

                    ScoreFilter sf = filters[filterIndex] as ScoreFilter;
                    if (sf != null)
                    {
                        if (specialFilter != null)
                        {
                            pointsPostSpecial += sf.points;
                        }
                        else
                        {
                            result.LeftValue += sf.points;
                        }
                    }

                    ScoreBucketFilter sbf = filters[filterIndex] as ScoreBucketFilter;
                    if (sbf != null)
                    {
                        int sbValue = Scoreboard.GetScore(actor, sbf);
                        if (specialFilter != null)
                        {
                            pointsPostSpecial += sbValue;
                        }
                        else
                        {
                            result.LeftValue += sbValue;
                        }
                    }

                    if (filters[filterIndex] is HealthFilter)
                    {
                        if (specialFilter != null)
                        {
                            pointsPostSpecial += actor.HitPoints;
                        }
                        else
                        {
                            result.LeftValue += actor.HitPoints;
                        }
                    }

                    SettingsFilter settings = filters[filterIndex] as SettingsFilter;
                    if (settings != null)
                    {
                        if (specialFilter != null)
                        {
                            pointsPostSpecial += actor.GetSettingsValue(actor, settings.name);
                        }
                        else
                        {
                            result.LeftValue += actor.GetSettingsValue(actor, settings.name);
                        }
                    }

                }   // end of loop over left hand side

                // Apply effect of specials.
                if (specialFilter != null)
                {
                    if (specialFilter is RandomFilter)
                    {
                        if (pointsPostSpecial > 0)
                        {
                            result.LeftValue += BokuGame.bokuGame.rnd.Next(pointsPostSpecial);
                        }
                        else if (pointsPostSpecial < 0)
                        {
                            result.LeftValue -= BokuGame.bokuGame.rnd.Next(-pointsPostSpecial);
                        }
                    }
                    if (specialFilter is PercentFilter)
                    {
                        result.LeftValue = (int)Math.Round((float)result.LeftValue / 100.0f * (float)pointsPostSpecial);
                    }
                }

                // Grab op from ComparisonFilter which we should be looking at.
                result.CompareOp = (filters[filterIndex] as ScoreCompareFilter).op;
                ++filterIndex;

                // Now sum right hand side.
                specialFilter = null;
                pointsPostSpecial = 0;

                for (; filterIndex < filters.Count; filterIndex++)
                {
                    // Did we find a comparison filter, if so we're broken.  There should be only one.
                    if (filters[filterIndex] is ScoreCompareFilter)
                    {
                        Debug.Assert(false, "Invalid to have two compare filters.");
                    }

                    // Did we find a random filter?
                    if (filters[filterIndex] is RandomFilter)
                    {
                        Debug.Assert(specialFilter == null, "Two special tiles should not be valid here.");
                        specialFilter = filters[filterIndex];
                    }

                    ScoreFilter sf = filters[filterIndex] as ScoreFilter;
                    if (sf != null)
                    {
                        if (specialFilter != null)
                        {
                            pointsPostSpecial += sf.points;
                        }
                        else
                        {
                            result.RightValue += sf.points;
                        }
                    }

                    ScoreBucketFilter sbf = filters[filterIndex] as ScoreBucketFilter;
                    if (sbf != null)
                    {
                        int sbValue = Scoreboard.GetScore(actor, sbf);
                        if (specialFilter != null)
                        {
                            pointsPostSpecial += sbValue;
                        }
                        else
                        {
                            result.RightValue += sbValue;
                        }
                    }

                    if (filters[filterIndex] is HealthFilter)
                    {
                        if (specialFilter != null)
                        {
                            pointsPostSpecial += actor.HitPoints;
                        }
                        else
                        {
                            result.RightValue += actor.HitPoints;
                        }
                    }

                    SettingsFilter settings = filters[filterIndex] as SettingsFilter;
                    if (settings != null)
                    {
                        if (specialFilter != null)
                        {
                            pointsPostSpecial += actor.GetSettingsValue(actor, settings.name);
                        }
                        else
                        {
                            result.RightValue += actor.GetSettingsValue(actor, settings.name);
                        }
                    }

                }   // end of loop over right hand side

                // Apply effec of special tiles.
                if (specialFilter != null)
                {
                    if (specialFilter is RandomFilter)
                    {
                        if (pointsPostSpecial > 0)
                        {
                            result.RightValue += BokuGame.bokuGame.rnd.Next(pointsPostSpecial);
                        }
                        else if (pointsPostSpecial < 0)
                        {
                            result.RightValue -= BokuGame.bokuGame.rnd.Next(-pointsPostSpecial);
                        }
                    }
                    if (specialFilter is PercentFilter)
                    {
                        result.RightValue = (int)Math.Round((float)result.RightValue / 100.0f * (float)pointsPostSpecial);
                    }
                }
            }
            else
            {
                Debug.Assert(false, "Not sure what we're testing for.  Not good.");
            }

            return result;
        }   // end of ScoresFromFilterSet()

        /// <summary>
        /// Look at this filter set and determine if it's testing to see if a score has changed.
        /// 
        /// Filter set should contain a single Scorebucket and an optional Not.  Nothing else is valid.
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        static bool TestScoreChange(List<Filter> filters)
        {
            bool result = false;

            foreach (Filter filter in filters)
            {
                if (filter is ScoreBucketFilter)
                {
                    // If we previously found a scorebucket, not valid.
                    if (result == true)
                    {
                        return false;
                    }
                    // This is the first scorebucket so we're good.
                    result = true;
                }
                else if (filter is ScoreCompareFilter && filter.hiddenDefault)
                {
                    // Valid, just ignore it.
                }
                else if (filter is NotFilter)
                {
                }
                else
                {
                    // Shouldn't be here, not valid.
                    return false;
                }
            }   // end of loop over filters.

            return result;
        }   // end of TestScoreChange()

        /// <summary>
        /// Look at this filter set and determine if it's testing a score against a value
        /// without using a comparison operator.  Equals is implied.
        /// 
        /// There can be any number of scorebucket and point filters.  We compare whatever 
        /// the first scorebucket is (no matter position) to the sum of all the other values 
        /// even if the first tiles is a point tile.  This preserves the legacy behaviour of 
        /// being able to program
        ///     WHEN Scored 10Points Red
        ///     WHEN Scored 10Points 10Points Blue
        /// 
        /// If no scorebucket is found then we compare the sum of the points to the Red Scorebucket (default).
        /// 
        /// Filter set should have no comparison filters.  Although there will be a hidden one.
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        static bool TestScoreValue(List<Filter> filters)
        {
            bool result = false;

            // Are there any comparisons?  Only allow the hidden one.
            foreach (Filter filter in filters)
            {
                if (filter is ScoreCompareFilter && !filter.hiddenDefault)
                {
                    return false;
                }
            }

            result = true;

            return result;
        }   // end of TestScoreChange()

        /// <summary>
        /// Look at this filter set and determine if it's testing 2 sets of values
        /// using a comparison filter.
        /// 
        /// Just look for a comparison filter.
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        static bool TestComparison(List<Filter> filters)
        {
            bool result = false;

            // Are there any comparisons?
            foreach (Filter filter in filters)
            {
                if (filter is ScoreCompareFilter)
                {
                    result = true;
                    break;
                }
            }

            return result;
        }   // end of TestScoreChange()


    }   // end of abstract class Filter

    
    public class NullFilter : Filter
    {
        public NullFilter()
        {
            this.upid = ProgrammingElement.upidNull;
        }

        public override ProgrammingElement Clone()
        {
            NullFilter clone = new NullFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(NullFilter clone)
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
            return true;
        }
    }

    public class AnythingFilter : Filter
    {
        public AnythingFilter()
        {
        }

        public override ProgrammingElement Clone()
        {
            AnythingFilter clone = new AnythingFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(AnythingFilter clone)
        {
            base.CopyTo(clone);
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            return true;
        }

        /// <summary>
        /// Should only return true if there's anything in the senseSet list, not all the time.
        /// But there's no easy way to get to the senseSet list since that is not common to
        /// all sensors.  We can't do it the right way so just hack it.
        /// </summary>
        /// <param name="reflex"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public override bool MatchAction(Reflex reflex, out object param)
        {
            param = null;
            return true;
        }
    }

    public class NothingFilter : Filter
    {
        public NothingFilter()
        {
        }

        public override ProgrammingElement Clone()
        {
            NothingFilter clone = new NothingFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(NothingFilter clone)
        {
            base.CopyTo(clone);
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            // doesn't effect match target
            return true;
        }
        public override bool MatchAction(Reflex reflex, out object param)
        {
            bool match = (reflex.targetSet == null || reflex.targetSet.Count == 0);
            param = null;
            return match;
        }
    }
}
