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
using Boku.SimWorld.Terra;

namespace Boku.Programming
{
    /// <summary>
    /// Senses thing by hearing
    /// 
    /// 
    /// </summary>
    public class SoundSensor : Sensor
    {
        #region Members
        private SensorTargetSet senseSet = new SensorTargetSet();
        private SensorTargetSet.Enumerator senseSetIter = null;

        private Random rnd = new Random();

        #endregion Members


        #region Public
        public SoundSensor()
        {
            senseSetIter = (SensorTargetSet.Enumerator)senseSet.GetEnumerator();
        }

        public override ProgrammingElement Clone()
        {
            SoundSensor clone = new SoundSensor();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(SoundSensor clone)
        {
            base.CopyTo(clone);
        }

        public override void Reset(Reflex reflex)
        {
            senseSet.Clear();
            base.Reset(reflex);
        }

        public override void StartUpdate(GameActor gameActor)
        {
            senseSet.Clear();
        }

        public override void ThingUpdate(GameActor gameActor, GameThing gameThing, Vector3 direction, float range)
        {
            CruiseMissile missile = gameThing as CruiseMissile;
            // don't test missiles we have launched
            if ((missile == null || missile.Launcher != gameActor)
                && (gameThing.ActorHoldingThis != gameActor))
            {
                float hearingRange = gameActor.HearingRange;

                if (range < hearingRange)
                {
                    senseSet.Add(gameThing, direction, range);
                }
            }
            if (senseSet.Count == 0)
            {
                senseSet.Clear();
            }
        }

        public override void FinishUpdate(GameActor gameActor)
        {
        }


        public override void ComposeSensorTargetSet(GameActor gameActor, Reflex reflex)
        {
            List<Filter> filters = reflex.Filters;

            // add normal sightSet of items to the targetset
            //
            senseSetIter.Reset();
            while (senseSetIter.MoveNext())
            {
                SensorTarget target = (SensorTarget)senseSetIter.Current;

                bool match = true;
                bool cursorFilterPresent = false;
                for (int indexFilter = 0; indexFilter < filters.Count; indexFilter++)
                {
                    Filter filter = filters[indexFilter] as Filter;
                    ClassificationFilter cursorFilter = filter as ClassificationFilter;
                    if (cursorFilter != null && cursorFilter.classification.IsCursor)
                    {
                        cursorFilterPresent = true;
                    }

                    if (!filter.MatchTarget(reflex, target))
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    if (!target.Classification.IsCursor || cursorFilterPresent)
                    {
                        reflex.targetSet.Add(target);
                    }
                }
            }

            if (ListeningForMusic(filters))
            {
                if (HearMusic(filters))
                {
                    SensorTarget sensorTarget = SensorTargetSpares.Alloc();
                    sensorTarget.Init(gameActor, Vector3.UnitZ, 0.0f);
                    reflex.targetSet.AddOrFree(sensorTarget);
                }
            }

            reflex.targetSet.Action = TestObjectSet(reflex);
            if (reflex.targetSet.Action)
            {
                foreach (SensorTarget targ in reflex.targetSet)
                {
                    gameActor.AddSoundLine(targ.GameThing);
                }
            }
        }

        #endregion Public

        #region Internal

        private bool ListeningForMusic(List<Filter> filters)
        {
            bool listenForMusic = false;
            foreach (Filter f in filters)
            {
                ClassificationFilter classFilter = f as ClassificationFilter;
                if (classFilter != null)
                {
                    /// Don't mess around, a class filter disqualifies the whole process.
                    return false;
                }
                SoundFilter soundFilter = f as SoundFilter;
                if (soundFilter != null)
                {
                    if (!BokuGame.Audio.IsSpatial(soundFilter.sound))
                    {
                        listenForMusic = true;
                    }
                }
            }
            return listenForMusic;
        }

        private bool HearMusic(List<Filter> filters)
        {
            bool musicHeard = false;
            bool invert = false;
            foreach (Filter f in filters)
            {
                /// This inversion is ugly, but we don't have a sensorTarget to add to the
                /// list, so a Count==Zero filter would always come up true. An alternative
                /// might be to pretend the GameActor is making the music, so there always
                /// would be a direct object. Going to go with that, leaving this code
                /// here in case something goes wrong, will try to delete after checkin.
                //CountFilter countFilter = f as CountFilter;
                //if (countFilter != null)
                //{
                //    if ((countFilter.count1 == 0) && (countFilter.operand1 == Operand.Equal))
                //    {
                //        invert = true;
                //    }
                //}
                SoundFilter soundFilter = f as SoundFilter;
                if (soundFilter != null)
                {
                    if (BokuGame.Audio.IsPlaying(null, soundFilter.sound))
                    {
                        musicHeard = true;
                    }
                }
            }
            if (invert)
                musicHeard = !musicHeard;
            return musicHeard;
        }
        #endregion Internal
    }
}
