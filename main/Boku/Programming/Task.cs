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

using KoiX.Input;

using Boku.Base;
using Boku.Common;


namespace Boku.Programming
{
    /// <summary>
    /// This represents a single running task (not multi-threading but a single thing to accomplish)
    /// It contains an ordered list (priority) of Reflexes that represent how the task is run
    /// The reflexes share a set of sensors that represent input and 
    /// they share a set of acutators that represent their output
    /// All reflexes are considered to be run in parrallel
    /// When ever a reflex gets updated, it updates its output in the associated Actuators Arbitrator
    /// When ever the actuators gets updated, it will request the arbitrator for the data to use
    /// </summary>
    public class Task
    {
        // This bit array exists for backward compatibility with the now-antiquated verb arbitration paradigm.
        private BitArray verbExecutions = new BitArray((int)GameThing.Verbs.SIZEOF);

        // True if any of the sensors in this task want their ThingUpdate method called.
        private bool haveThingUpdateSensors;

        private int maxReflexIndentation;

        private int sensorCount;

        protected Brain brain;

        [XmlAttribute]
        public string upid;

        [XmlArrayItem(typeof(Reflex))]
        public List<Reflex> reflexes = new List<Reflex>();

        [XmlIgnore]
        public Brain Brain
        {
            get { return this.brain; }
            set { this.brain = value; }
        }

        [XmlIgnore]
        public GameActor GameActor
        {
            get { return brain.GameActor; }
        }
        [XmlIgnore]
        public List<GameThing> GameThings
        {
            get { return Brain.GameThings; }
        }

        [XmlIgnore]
        public bool IsUserControlled;

        [XmlIgnore]
        public bool IsLeftMouseButtonPresent;

        [XmlIgnore]
        public bool IsRightMouseButtonPresent;

        [XmlIgnore]
        public bool IsTurning;


        public Task()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="atLoadTime">If true this fixup is happening when the level is being loaded.  If false then it's happening during brain editing.</param>
        /// <param name="incomingLanguageVersion">If atLoadTime is true, this is the version of the language for the level being loaded.</param>
        public void Fixup(bool atLoadTime, int incomingLanguageVersion)
        {
            haveThingUpdateSensors = false;

            sensorCount = 0;

            maxReflexIndentation = 0;

            Stack<Reflex> parentStack = new Stack<Reflex>();

            for (int indexReflex = 0; indexReflex < reflexes.Count; indexReflex++)
            {
                Reflex reflex = reflexes[indexReflex] as Reflex;

                reflex.Task = this;

                maxReflexIndentation = Math.Max(maxReflexIndentation, reflex.Indentation);

                while (parentStack.Count > reflex.Indentation)
                {
                    parentStack.Pop();
                }

                Reflex currParent = null;

                if (parentStack.Count > 0)
                {
                    currParent = parentStack.Peek();
                }

                reflex.Parent = currParent;

                reflex.Fixup(atLoadTime, incomingLanguageVersion);

                parentStack.Push(reflex);

                Sensor sensor = reflex.Sensor;
                if (sensor != null)
                {
                    sensorCount += 1;
                    haveThingUpdateSensors |= sensor.WantThingUpdate;
                }
            }

            Reset();
        }   // end of FixUp()

        public void Reset()
        {
            for (int indexReflex = 0; indexReflex < reflexes.Count; indexReflex++)
            {
                Reflex reflex = reflexes[indexReflex] as Reflex;
                reflex.Reset();
            }
        }

        public void UpdateSensors(BrainCategories category)
        {
            UpdateUserControlled();
            UpdateIsTurning();
            UpdateMouseButtonPresence();

            if (IsLeftMouseButtonPresent)
                MouseEdit.DisableLeftDrag();
            if (IsRightMouseButtonPresent)
                MouseEdit.DisableRightOrbit();

            for (int i = 0; i < reflexes.Count; i++)
            {
                Reflex reflex = reflexes[i] as Reflex;
                reflex.targetSet.Clear();
            }

            if (reflexes.Count > 0)
            {
                UpdateGameThingSensors(category);

                for (int indentationLevel = 0; indentationLevel <= maxReflexIndentation; ++indentationLevel)
                {
                    // After evaluating root-level reflexes, open up to all sensor categories.
                    if (indentationLevel > 0)
                        category = BrainCategories.NotSpecified;

                    for (int i = 0; i < reflexes.Count; i++)
                    {
                        Reflex reflex = reflexes[i];

                        // Only evaluate reflexes at the current level of indentation.
                        if (reflex.Indentation != indentationLevel)
                            continue;

                        // Only evaluate sub-reflexes of parents that evaluated true except
                        // in the case where the reflex actuator is movement based.
                        if (reflex.Parent != null && !reflex.Parent.targetSet.Action)
                        {
                            // Nested reflexes with once modifiers need to clear their "once" state.
                            reflex.ResetOnceModifiers();

                            // Nested reflexes with active mouse targets should still be acted on
                            // so create valid target and action sets for them.  Note that this
                            // only applies to reflexes with a movement actuator.  This is so you
                            // can have WHEN MouseLeft DO Move and the result will be that the 
                            // bot continues to move toward the target (position or bot) even 
                            // after the parent reflex goes false.
                            bool mouseTarget = reflex.MousePosition != null || reflex.MouseActor != null;
                            if (mouseTarget && reflex.IsMovement)
                            {
                                // Create a sensor target set 
                                if (reflex.MouseActor != null)
                                {
                                    // We need to add the mouse actor to the targetSet.
                                    SensorTarget target = SensorTargetSpares.Alloc();
                                    target.Init(reflex.Task.GameActor, reflex.MouseActor);
                                    reflex.targetSet.Add(target);
                                }
                                else if (reflex.MousePosition != null)
                                {
                                    // We need to add the mouse position to the targetSet.
                                    SensorTarget target = SensorTargetSpares.Alloc();
                                    target.GameThing = GameActor;
                                    target.Position = reflex.MousePosition.Value;
                                    target.Direction = target.Position - reflex.Task.GameActor.Movement.Position;
                                    target.Range = target.Direction.Length();
                                    target.Direction /= target.Range;
                                    reflex.targetSet.Add(target);
                                }
                                reflex.targetSet.ActionMouseTarget = true;
                                reflex.CreateActionSet(GameActor, 0);
                            }

                            continue;
                        }

                        // Don't evaluate reflexes that don't match the given categories, if any were specified.
                        if ((category != BrainCategories.NotSpecified) && (reflex.Sensor == null || !reflex.Sensor.Categories.Get((int)category)))
                            continue;

                        reflex.Update(GameActor, i);
                    }
                }
            }
        }

        public void UpdateActuators()
        {
            // Must update actuators on every update so gamepad/timer reflexes get applied.
            RunActuators();

            // After actuators do their thing, perform a post-process step to handle things
            // like ConstraintModifiers, which need to limit the actor in a specific way.
            for (int i = 0; i < reflexes.Count; ++i)
            {
                Reflex reflex = reflexes[i] as Reflex;
                if (reflex.actedOn)
                {
                    reflex.ApplyConstraints();
                }
                reflex.AdjustOnceModifiers();
                reflex.actedOn = false;
            }
        }

        /// <summary>
        /// Call update methods on Sensors for each GameActor.
        /// </summary>
        private void UpdateGameThingSensors(BrainCategories category)
        {
            if (sensorCount > 0)
            {
                for (int i = 0; i < reflexes.Count; ++i)
                {
                    if (reflexes[i] == null)
                        continue;

                    Sensor sensor = (reflexes[i] as Reflex).Sensor;

                    if (sensor == null)
                        continue;

                    if (category == BrainCategories.NotSpecified || sensor.Categories.Get((int)category))
                    {
                        sensor.StartUpdate(GameActor);
                    }
                }

                if (haveThingUpdateSensors)
                {
                    Vector3 actorCenter = Vector3.Transform(GameActor.BoundingSphere.Center, GameActor.Movement.LocalMatrix);
                    for (int indexGameThing = 0; indexGameThing < brain.GameThings.Count; indexGameThing++)
                    {
                        // There a number of objects we don't want to consider to testing...
                        GameThing gameThing = brain.GameThings[indexGameThing] as GameThing;

                        if (gameThing.Ignored )
                            continue;

                        /*
                        // Handled lower down now...
                        // Don't test self.
                        if (this.GameActor == gameThing)
                            continue;
                        */

                        // Don't test dead things.  You know you can't see dead things.
                        if (!gameThing.IsAlive())
                            continue;

                        /*
                        // Handled lower down now...
                        // Also, don't sense things that are dead in another way (knocked-out).
                        if (gameThing.CurrentState == GameThing.State.Dead)
                            continue;
                        */

                        // Don't test things we're holding.
                        if (gameThing == this.GameActor.ThingBeingHeldByThisActor)
                            continue;

                        // Don't test missiles we've fired.
                        CruiseMissile missile = gameThing as CruiseMissile;
                        if (missile != null && missile.Launcher == this.GameActor)
                            continue;

                        Vector3 thingCenter = Vector3.Transform(gameThing.BoundingSphere.Center, gameThing.Movement.LocalMatrix);

                        Vector3 direction = thingCenter - actorCenter;

                        float range = direction.Length();
                        if (range > 0.0f)
                        {
                            direction *= 1.0f / range; // Normalize.
                        }

                        for (int i = 0; i < reflexes.Count; ++i)
                        {
                            if (reflexes[i] == null)
                                continue;

                            Sensor sensor = (reflexes[i] as Reflex).Sensor;

                            if (sensor == null)
                                continue;

                            // Handle strangeness of me filter.  Basically this is adds an implicit "not me" filter
                            // except in the case where the user explicitly added the "me " filter.
                            Reflex reflex = (Reflex)reflexes[i];
                            if(reflex.hasMeFilter)
                            {
                                if (GameActor != gameThing)
                                {
                                    continue;
                                }
                                direction = GameActor.Movement.Facing;
                            }
                            else
                            {
                                if (GameActor == gameThing)
                                {
                                    continue;
                                }
                            }

                            // Ignore dead things unless explicitly looking for them.
                            if (reflex.hasDeadFilter)
                            {
                                // We're looking for dead things so continue if not dead.
                                if (gameThing.CurrentState != GameThing.State.Dead)
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                // We're not looking for dead things so ignore them.
                                if (gameThing.CurrentState == GameThing.State.Dead)
                                {
                                    continue;
                                }
                            }

                            // Ignore squashed things unless explicitly looking for them.
                            if (reflex.hasSquashedFilter)
                            {
                                // We're looking for dead things so continue if not dead.
                                if (gameThing.CurrentState != GameThing.State.Squashed)
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                // We're not looking for squashed things so ignore them.
                                if (gameThing.CurrentState == GameThing.State.Squashed)
                                {
                                    continue;
                                }
                            }

                            // Ignore missiles unless explicitly looking for them.
                            if (reflex.hasMissileFilter)
                            {
                                // We're looking for missiles, continue otherwise
                                if (!(gameThing is CruiseMissile))
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                // We're not looking for missiles so ignore them.
                                if (gameThing is CruiseMissile)
                                {
                                    continue;
                                }
                            }

                            if (sensor.WantThingUpdate && (category == BrainCategories.NotSpecified || sensor.Categories.Get((int)category)))
                            {
                                if (gameThing.CanBeSensed())
                                {
                                    sensor.ThingUpdate(GameActor, gameThing, direction, range);
                                }
                            }
                        }

                    }  // foreach gamething

                } // if haveThingUpdateSensors

                for (int i = 0; i < reflexes.Count; ++i)
                {
                    if (reflexes[i] == null)
                        continue;

                    Sensor sensor = reflexes[i].Sensor;

                    if (sensor == null)
                        continue;

                    if (category == BrainCategories.NotSpecified || sensor.Categories.Get((int)category))
                    {
                        sensor.FinishUpdate(GameActor);
                    }
                }
            } // if sensorCount > 0
        }

        private void RunActuators()
        {
            verbExecutions.SetAll(false);

            GameActor.Movement.UserControlled = IsUserControlled;

            // Do not update actuators during pre-game.
            if (InGame.inGame.PreGameActive)
                return;

            int initialTaskId = Brain.ActiveTaskId;

            for (int i = 0; i < reflexes.Count; ++i)
            {
                Reflex reflex = reflexes[i] as Reflex;
                if (reflex.Actuator != null && reflex.targetSet.AnyAction)
                {
                    if (reflex.Actuator is VerbActuator)
                    {
                        // Ugh, be backward compatible with the old arbitrated verb behavior.
                        VerbActuator verbAct = reflex.Actuator as VerbActuator;

                        // If the verb is exclusive AND we've already seen in then don't process it.
                        if (GameThing.VerbIsExclusive(verbAct.Verb) && verbExecutions.Get((int)verbAct.Verb))
                        {
                            continue;
                        }
                        else
                        {
                            // This is the first time we've seen the verb so flag it.
                            verbExecutions.Set((int)verbAct.Verb, true);
                        }
                    }

                    // If we have a mouse sensor AND a once modifier then
                    // we want to tag the mouse button with IgnoreUntilReleased.
                    // This makes once work correctly without blocking other
                    // reflexes from seeing the mouse.
                    if (reflex.Sensor is MouseSensor)
                    {
                        OnceModifier om = null;
                        for (int j = 0; j < reflex.Modifiers.Count; j++)
                        {
                            om = reflex.Modifiers[j] as OnceModifier;
                            if(om != null)
                                break;
                        }
                        if (om != null)
                        {
                            MouseFilter mf = null;
                            for (int j = 0; j < reflex.Filters.Count; j++)
                            {
                                mf = reflex.Filters[j] as MouseFilter;
                                if (mf != null)
                                    break;
                            }
                            if (mf != null)
                            {
                                // By setting IgnoreUntilReleased, this prevents another
                                // reflex from being triggered until the mouse is released.
                                switch (mf.type)
                                {
                                    case MouseFilterType.LeftButton:
                                        LowLevelMouseInput.Left.IgnoreUntilReleased = true;
                                        break;
                                    case MouseFilterType.RightButton:
                                        LowLevelMouseInput.Right.IgnoreUntilReleased = true;
                                        break;
                                    default:
                                        // Nothing to see here, move along.
                                        break;
                                }
                            }
                        }
                    }

                    reflex.Actuator.Update(reflex);

                    // Stop updating actuators if we switched pages.
                    if (Brain.ActiveTaskId != initialTaskId)
                        break;
                }
            }
        }

        public void AddReflex(Reflex reflex)
        {
            reflexes.Add(reflex);
        }
        
        public void RemoveReflex(Reflex reflex)
        {
            reflexes.Remove(reflex);
        }
        
        public void InsertReflexBefore(Reflex reflexRef, Reflex reflexNew)
        {
            int indexInsert = reflexes.IndexOf(reflexRef);
            reflexes.Insert(indexInsert, reflexNew);
        }

        public void InsertReflexAfter(Reflex reflexRef, Reflex reflexNew)
        {
            int indexInsert = reflexes.IndexOf(reflexRef);
            indexInsert++;
            reflexes.Insert(indexInsert, reflexNew);
        }

        public void SwapReflexes(Reflex reflexPrev, Reflex reflexNew)
        {
            int indexPrev = reflexes.IndexOf(reflexPrev);
            int indexNew = reflexes.IndexOf(reflexNew);

            reflexes.RemoveAt(indexPrev);
            reflexes.Insert(indexPrev, reflexNew);
            reflexes.RemoveAt(indexNew);
            reflexes.Insert(indexNew, reflexPrev);

            Reset();
        }

        public void UpdateUserControlled()
        {
            IsUserControlled = false;

            // Only return true for user input that affects movement.
            for (int i = 0; i < reflexes.Count; ++i)
            {
                Reflex reflex = reflexes[i] as Reflex;

                if (reflex.IsUserControlled)
                {
                    IsUserControlled = true;
                    CameraInfo.AddUserControlled(GameActor);
                    break;
                }
            }
        }

        public void UpdateIsTurning()
        {
            IsTurning = false;

            // Only return true for user input that affects movement.
            for (int i = 0; i < reflexes.Count; ++i)
            {
                Reflex reflex = reflexes[i] as Reflex;

                if (reflex.IsTurning)
                {
                    IsTurning = true;
                    break;
                }
            }
        }

        public void UpdateMouseButtonPresence()
        {
            IsLeftMouseButtonPresent = false;
            IsRightMouseButtonPresent = false;

            for (int i = 0; i < reflexes.Count; ++i)
            {
                Reflex reflex = reflexes[i] as Reflex;

                IsLeftMouseButtonPresent |= reflex.leftMouseButtonPresent;
                IsRightMouseButtonPresent |= reflex.rightMouseButtonPresent;
            }
        }


        /// <summary>
        /// Remove all invalid elements from reflexes
        /// </summary>
        internal void Validate()
        {
            for (int i = 0; i < reflexes.Count; ++i)
            {
                Reflex reflex = reflexes[i] as Reflex;
                reflex.Validate();
            }
        }

        public static Task DeepCopy(Task srcTask)
        {
            Task dstTask = new Task();

            dstTask.upid = srcTask.upid;

            for (int i = 0; i < srcTask.reflexes.Count; ++i)
            {
                Reflex srcReflex = srcTask.reflexes[i] as Reflex;
                Reflex dstReflex = Reflex.DeepCopy(srcReflex);
                dstTask.reflexes.Add(dstReflex);
            }

            return dstTask;
        }
    }
}
