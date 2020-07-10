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

namespace Boku.Programming
{
    /// <summary>
    /// This represents the container for all programming for one GameActor
    /// It contains and manages multiple tasks (not multi-threading but a single thing to accomplish)
    /// Only one task is active and running at one time
    /// </summary>
    public class Brain : ArbitraryComparable
    {
        public const double kStunSeconds = 1f; // exposing so zap can be same length.
        private const string kIdTaskFormat = "task.managed{0}";
        public static int kCountDefaultTasks = 12;

        private GameActor gameActor;
        private int indexActiveTask = -1; // current running task
        private int indexStartTask = -1; // task to start on or reset to
        private double stunnedUntil;  // in Time.GameTimeTotalSeconds

        public Task GetTask(int indexTask)
        {
            if (indexTask >= 0 && indexTask < tasks.Count)
            {
                return tasks[indexTask];
            }
            return null;
        }

        public Task GetTask(string upid)
        {
            for (int indexTask = 0; indexTask < tasks.Count; indexTask++)
            {
                Task task = tasks[indexTask];
                if (task.upid == upid)
                {
                    return task;
                }
            }
            return null;
        }

        /// <summary>
        /// True if this brain is replaceable by a brain with no programming.
        /// Note that only task[0] is checked, because (currently) there would be no way
        /// to get to other tasks if task[0] is unprogrammed. If another task is capable
        /// of being initial task, that one would need to be checked.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                if (tasks.Count > 0)
                {
                    Task task = tasks[0];
                    if (task.reflexes.Count > 0)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        private void InitEmptyTasks()
        {
            tasks.Clear();

            // fill remaining spots with empty tasks
            for (int indexTask = 0; indexTask < kCountDefaultTasks; indexTask++)
            {
                Task task = new Task();
                task.upid = String.Format(kIdTaskFormat, indexTask);
                task.Brain = this;
                this.tasks.Add(task);
            }
        }

        public void InitEmpty()
        {
            InitEmptyTasks();
            indexActiveTask = 0;
            indexStartTask = 0;
            stunnedUntil = 0f;
        }

 
        [XmlIgnore]
        public GameActor GameActor
        {
            get { return this.gameActor; }
            set
            {
                if (this.gameActor != value)
                {
                    this.gameActor = value;
//                    this.updateTimer = new PerfTimer( ">>> Update Brain " + this.gameActor.GetType().ToString() );
                    Fixup();
                }
            }
        }

        [XmlIgnore]
        public List<GameThing> GameThings { get { return InGame.inGame.gameThingList; } }

        [XmlArrayItem(typeof(Task))]
        public List<Task> tasks = new List<Task>();

        [XmlAttribute]
        public int StartTaskId
        {
            get { return this.indexStartTask; }
            set
            {
                if (this.indexStartTask != value)
                {
                    this.indexStartTask = value;
                }
            }
        }

        [XmlAttribute]
        public int ActiveTaskId
        {
            get { return this.indexActiveTask; }
            set
            {
                // if StartTaskId was not set by serialization, use the stored ActiveTaskId
                // this is for backward compatiblity with brains that did not have a StartTaskId
                //
                if (this.indexStartTask == -1)
                {
                    this.indexStartTask = value;
                }

                // always reset which will restart timers and all sorts of other programming behaviors,
                // but preserve our stun state since we're just s
                Reset(value, false, false);
            }
        }

        [XmlIgnore]
        public bool Stunned
        {
            get { return Time.GameTimeTotalSeconds < stunnedUntil; }
        }

        public void Stun()
        {
            stunnedUntil = Time.GameTimeTotalSeconds + kStunSeconds;
        }

        public void UnStun()
        {
            stunnedUntil = 0;
        }

        [XmlIgnore]
        public int TaskCount { get { return this.tasks.Count; } }

        [XmlIgnore]
        public Task ActiveTask
        {
            get
            {
                if (this.indexActiveTask >= 0 && this.indexActiveTask < this.tasks.Count)
                {
                    return this.tasks[this.indexActiveTask];
                }
                return null;
            }
            set
            {
                int indexNew = this.tasks.IndexOf(value);
                this.ActiveTaskId = indexNew;
            }
        }

        public void UpdateSensors(BrainCategories category)
        {
            ActiveTask.UpdateSensors(category);
        }

        public void UpdateActuators()
        {
            Vector3 prevPosition = GameActor.Movement.Position;
            
            ActiveTask.UpdateActuators();

            // If immobile, lock us down.
            if (GameActor.TweakImmobile || GameActor.TweakImmobileNoRot)
            {
                GameActor.Movement.Position = prevPosition;
                GameActor.Movement.Velocity = Vector3.Zero;
            }
        }

        public void Reset(int indexNewTask, bool resetVelocity, bool resetStun)
        {
            this.indexActiveTask = indexNewTask;
            Task task = ActiveTask;
            if (task != null)
            {
                task.Reset();
            }
            
            RegisterReflexSupportedObject();

            if (resetStun)
            {
                stunnedUntil = 0f;
            }
        }

        public void Wipe()
        {
            Reset(0, true, true);
        }

        public void Fixup()
        {
            /// If this is an old bot that only has 6 task pages, go ahead
            /// and fill it out to the current number.
            if ((tasks.Count > 0) && (tasks.Count < kCountDefaultTasks))
            {
                int count = tasks.Count;
                for (int indexTask = count; indexTask < kCountDefaultTasks; ++indexTask)
                {
                    Task task = new Task();
                    task.upid = String.Format(kIdTaskFormat, indexTask);
                    task.Brain = this;
                    this.tasks.Add(task);
                }

            }
            for (int indexTask = 0; indexTask < tasks.Count; indexTask++)
            {
                Task task = tasks[indexTask];
                
                task.Brain = this;
                task.Fixup(atLoadTime: true, incomingLanguageVersion: int.Parse(InGame.XmlWorldData.KCodeVersion));
            }
        }

        public Sensor GetSensor(string idObj)
        {
            Sensor sensor = CardSpace.Cards.GetSensor(idObj); // Cards.GetSensor() will clone
            return sensor;
        }
        public Filter GetFilter(string idObj)
        {
            return CardSpace.Cards.GetFilter(idObj); // Cards.GetFilter() will clone
        }

        public Selector GetSelector( string idObj)
        {
            Selector selector = CardSpace.Cards.GetSelector(idObj); // Cards.GetSelector() will clone
            return selector;
        }

        public Modifier GetModifier(string idObj)
        {
            Modifier mod = CardSpace.Cards.GetModifier(idObj); // Cards.GetModifier() will clone
            return mod;
        }
        public Actuator GetActuator(string idObj)
        {
            Actuator actuator = CardSpace.Cards.GetActuator(idObj); // Cards.GetActuator() will clone
            return actuator;
        }

        /// <summary>
        /// This function will Register any objects that should be registered when used by a reflex.
        /// 
        /// This is used by things like the scores and GUI buttons which only appear onscreen
        /// when used in an actor's kode.
        /// </summary>
        internal void RegisterReflexSupportedObject()
        {
            for (int i = 0; i < tasks.Count; ++i)
            {
                Task task = tasks[i];

                for (int j = 0; j < task.reflexes.Count; ++j)
                {
                    Reflex reflex = task.reflexes[j] as Reflex;

                    //Register Score Bucket
                    if (reflex.Actuator != null && reflex.Actuator.Categories.Get((int)BrainCategories.DoSetScore))
                    {
                        ScoreBucket bucket = Modifier.ScoreBucketFromModifierSet(reflex.Modifiers);
                        if (bucket != ScoreBucket.NotApplicable)
                        {
                            Scoreboard.Activate(bucket);
                        }
                    }

                    //Register Color Touch Button 
                    foreach (Filter filter in reflex.Filters)
                    {
                        if (filter is GUIButtonFilter)
                        {
                            Classification.Colors eColor = (filter as GUIButtonFilter).color;
                            
                            if( eColor >= (Classification.Colors)Classification.ColorInfo.First &&
                                eColor <= (Classification.Colors)Classification.ColorInfo.Last)
                            {
                                Debug.Assert( null != GUIButtonManager.GetButton(eColor) );
                                GUIButtonManager.GetButton(eColor).Active = true;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Remove invalid elements from all reflexes.
        /// </summary>
        public void Validate()
        {
            for (int i = 0; i < tasks.Count; ++i)
            {
                Task task = tasks[i];

                task.Validate();
            }
        }

        /// <summary>
        /// Make copy of the provided brain, duplicating all internal elements so that
        /// no references are shared.
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static Brain DeepCopy(Brain srcBrain)
        {
            Brain brain = new Brain();

            for (int i = 0; i < srcBrain.tasks.Count; ++i)
            {
                Task srcTask = srcBrain.tasks[i];
                Task dstTask = Task.DeepCopy(srcTask);
                brain.tasks.Add(dstTask);
            }

            return brain;
        }

        public static Brain CreateEmpty()
        {
            Brain brain = new Brain();
            brain.InitEmpty();
            return brain;
        }
    }
}
