
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
    /// Senses touch events: tap, slide, swipes, rotate, pinch etc...
    /// </summary>
    public class TouchSensor : Sensor
    {
        private SensorTargetSet senseSet = new SensorTargetSet();
        private SensorTargetSet.Enumerator senseSetIter = null;

        private TerrainSensor terrainSensor;

        public TouchSensor()
        {
            senseSetIter = (SensorTargetSet.Enumerator)senseSet.GetEnumerator();
        }

        public override ProgrammingElement Clone()
        {
            TouchSensor clone = new TouchSensor();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(TouchSensor clone)
        {
            base.CopyTo(clone);
        }

        public override void Reset(Reflex reflex)
        {
            senseSet.Clear();

            int terrainCount = reflex.Data.GetFilterCountByType(typeof(TerrainFilter));
            if (terrainCount > 0 && terrainSensor == null)
                terrainSensor = new TerrainSensor();
            else if (terrainCount == 0)
                terrainSensor = null;

            base.Reset(reflex);
        }

        public override void StartUpdate(GameActor gameActor)
        {
            senseSet.Clear();

            if (terrainSensor != null)
            {
                terrainSensor.StartUpdate(gameActor);
            }
        }

        public override void ThingUpdate(GameActor gameActor, GameThing gameThing, Vector3 direction, float range)
        {
            if (terrainSensor != null)
            {
                terrainSensor.ThingUpdate(gameActor, gameThing, direction, range);
            }
        }

        public override void FinishUpdate(GameActor gameActor)
        {
            if (terrainSensor != null)
            {
                //if (TouchEdit.MouseTouchHitInfo.TerrainHit)
                {
                    terrainSensor.OverrideSenseMaterial = TouchEdit.MouseTouchHitInfo.TerrainMaterial;
                }

                terrainSensor.FinishUpdate(gameActor);
            }
            else
            {
                if (TouchEdit.MouseTouchHitInfo.HaveActor)
                {
                    GameActor touchdActor = TouchEdit.MouseTouchHitInfo.ActorHit;

                    Vector3 actorCenter = Vector3.Transform(
                        gameActor.BoundingSphere.Center,
                        gameActor.Movement.LocalMatrix);

                    Vector3 thingCenter = Vector3.Transform(
                        touchdActor.BoundingSphere.Center,
                        touchdActor.Movement.LocalMatrix);

                    Vector3 direction = thingCenter - actorCenter;

                    float range = direction.Length();
                    if (range > 0.0f)
                    {
                        direction *= 1.0f / range; // Normalize.
                    }

                    SensorTarget target = SensorTargetSpares.Alloc();
                    target.Init(touchdActor, direction, range);
                    senseSet.AddOrFree(target);
                }

                senseSet.Add(NullActor.Instance, Vector3.Zero, float.MaxValue);
            }
        }

        private new bool TestObjectSet(Reflex reflex)
        {
            // JW - This may not be needed for this sensor..
            // We test both MatchTarget and MatchAction on the filters within the ComposeSensorTarget function.
            // This ensures we don't check the not filter twice (double negation is unhappiness!!)
            return true;
            //List<Filter> filters = reflex.Filters;

            //bool match = true;
            //object param;
            //for (int indexFilter = 0; indexFilter < filters.Count; indexFilter++)
            //{
            //    Filter filter = filters[indexFilter] as Filter;
            //    // JW - The touch gesture filter was already handled when composing the action set.
            //    // We handle additional filters here, such as direction.
            //    if (!(filter is TouchGestureFilter))
            //    {
            //        if (!filter.MatchAction(reflex, out param))
            //        {
            //            match = false;
            //            break;
            //        }
            //        if (param != null)
            //        {
            //            reflex.targetSet.Param = param;
            //        }
            //    }
            //}

            //if (reflex.Data.GetFilterCountByType(typeof(NotFilter)) > 0)
            //{
            //    match = !match;
            //}

            //return match;

            ////// Also match terrain sensor?
            ////if (terrainSensor != null)
            ////{
            ////    return terrainSensor.TestObjectSet(reflex);
            ////}
            ////else
            ////{
            ////    match = true;
            ////    return match;
            ////}
        }

        public override void ComposeSensorTargetSet(GameActor gameActor, Reflex reflex)
        {
            if (terrainSensor != null)
            {
                terrainSensor.ComposeSensorTargetSet(gameActor, reflex);
            }
            else
            {
                List<Filter> filters = reflex.Filters;

                // The sensor will use the first TouchGestureFilter it finds. Since any given reflex
                // can only have a single one of these, this works well.
                TouchGestureFilter gestureFilter = null;
                TouchButtonFilter buttonFilter = null;
                GUIButtonFilter guiButtonFilter = null;

                for (int i = 0; i < filters.Count; i++)
                {
                    if (filters[i] is TouchGestureFilter)
                    {
                        Debug.Assert( null == gestureFilter );
                        gestureFilter = filters[i] as TouchGestureFilter;
                    }
                    if (filters[i] is TouchButtonFilter)
                    {
                        Debug.Assert( null == buttonFilter );
                        buttonFilter = filters[i] as TouchButtonFilter;
                    }
                    if (filters[i] is GUIButtonFilter)
                    {
                        Debug.Assert(null == guiButtonFilter);
                        guiButtonFilter = filters[i] as GUIButtonFilter;
                    }
                }

                //if we don't have a filter, do nothing
                if ( null == gestureFilter &&
                     null == buttonFilter &&
                     null == guiButtonFilter )
                {
                    //Debug.Assert(false, "Did not find a touch filter for touch sensor.");
                    return;
                }

                // Cause the filter to detect what is under the touch cursor and set that information on the reflex.
                // MatchAction will be true if the TouchFilter passes but then we still need to filter on any other 
                // filters including the ever annoying "not", "me" and "anything" filters.
                Object param = null;
                bool matchAction = false;

                bool notFilter = reflex.Data.FilterExists("filter.not");
                bool anythingFilter = reflex.Data.FilterExists("filter.anything");

                if (gestureFilter != null)
                {
                    matchAction = gestureFilter.MatchAction(reflex, out param);
                }

                if (buttonFilter != null)
                {
                    // Make the actual on screen button visible if a reflex contains the touch button filter.
                    //TouchButtons.MakeVisible(buttonFilter.button);

                    matchAction = buttonFilter.MatchAction(reflex, out param);
                }

                if (guiButtonFilter != null)
                {
                    matchAction = guiButtonFilter.MatchAction(reflex, out param);
                }




                // Give the anythingFilter a chance to kill the action.
                if (anythingFilter && reflex.TouchActor == null)
                {
                    matchAction = false;
                }

                // Handle the "Not" filter
                if (notFilter)
                {
                    if (matchAction)
                    {
                        matchAction = false;
                        reflex.TouchActor = null;
                        reflex.TouchPosition = null;
                    }
                    else
                    {
                        matchAction = true;
                    }
                }

                bool touchCursorTarget = false;

                // We are moving towards a remembered target position if we're the movement actuator.
                // We do not require a match on the current frame to continue the movement.
                // Note, in the presence of a "Not" filter, there is no target to "not move" to, so this
                // is negated.
                // Also, support turning toward a remembered position.
                bool movingToTouchPos = (reflex.actuatorUpid == "actuator.movement" && !notFilter);
                movingToTouchPos |= (reflex.actuatorUpid == "actuator.turn" && !notFilter);

                if (movingToTouchPos && (reflex.TouchActor == reflex.Task.GameActor))
                {
                    // When moving with touch, going towards oneself doesn't make sense. By discarding
                    // the touch actor and using the terrain position, we have a better definition of this
                    // movement. This allows for small positional corrections around the target's bounding 
                    // box as well as letting slide movements that begin on oneself to work.
                    reflex.TouchActor = null;
                    reflex.TouchPosition = TouchEdit.MouseTouchHitInfo.TerrainPosition;
                }

                if (matchAction || movingToTouchPos)
                {
                    bool validTarget = false;
                    SensorTarget target = SensorTargetSpares.Alloc();
                    // If we have an actor touch target, check if it passes the filters.
                    // If not, null it out.
                    if (reflex.TouchActor != null)
                    {
                        // Create a target.
                        target.Init(reflex.Task.GameActor, reflex.TouchActor);
                        validTarget = true;
                    }
                    else
                    {
                        target.Init(reflex.Task.GameActor, reflex.Task.GameActor);
                        // If we've got a me filter and not TouchActor we've failed.
                        if(reflex.Data.FilterExists("filter.me"))
                        {
                            matchAction = false;
                        }

                        // NOTE: For classification purposes, if the user doesn't touch a particular actor,
                        // we create an 'empty' classification. This is because we need a classification object
                        // in order to do reflexes such as 'tap move blimp -> shoot'. If you don't tap an actor,
                        // it needs to compare the absence of an actor to the blimp.
                        if (reflex.TouchPosition != null)
                        {
                            // We need to add the touch position to the targetSet.
                            target.Classification = new Classification();
                            target.GameThing = senseSet.Nearest.GameThing;
                            target.Position = reflex.TouchPosition.Value;
                            target.Direction = target.Position - reflex.Task.GameActor.Movement.Position;
                            target.Range = target.Direction.Length();

                            // If we've arrived at the target position, clear MousePosition.
                            Vector2 dir2d = new Vector2(target.Direction.X, target.Direction.Y);
                            if (dir2d.LengthSquared() < 0.1f)
                            {
                                reflex.TouchPosition = null;
                            }

                            target.Direction /= target.Range;
                            touchCursorTarget = true;
                            validTarget = true;
                        }
                        else
                        {
                            // We don't have an actor or a position. Only with an action match should we
                            // create a sensor target.
                            if (matchAction)
                            {
                                // Calc a fake position for this target.
                                GameActor actor = reflex.Task.GameActor;
                                Vector3 pos = actor.Movement.Position;

                                target.Classification = new Classification();
                                target.GameThing = senseSet.Nearest.GameThing;
                                target.Position = pos;
                                target.Direction = target.Position - reflex.Task.GameActor.Movement.Position;
                                target.Range = target.Direction.Length();
                                target.Direction /= target.Range;
                                validTarget = true;
                            }
                        }
                    }

                    // Test the target against the filters.
                    if (validTarget)
                    {
                        for (int i = 0; i < filters.Count; i++)
                        {
                            if (!(filters[i] is TouchGestureFilter))
                            {
                                if (!filters[i].MatchTarget(reflex, target) ||
                                    !filters[i].MatchAction(reflex, out param))
                                {
                                    if (!notFilter)
                                    {
                                        // Failed a match on one of the filters and we don't have a negation
                                        // so null things out and short circuit.
                                        reflex.TouchActor = null;
                                        validTarget = false;
                                        matchAction = false;
                                        touchCursorTarget = false;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    // If we still have a target, add it to the target set.
                    if (validTarget)
                    {
                        reflex.targetSet.Add(target);
                    }
                    else
                    {
                        // Don't really need this target so free it.
                        SensorTargetSpares.Free(target);
                    }
                }

                reflex.targetSet.Action = reflex.targetSet.Count > 0; // && TestObjectSet(reflex);
                
                //reflex.targetSet.Action |= matchAction;

                // This forces the movement to keep going toward the clicked position/bot
                // even if the touch is no longer clicked.
                reflex.targetSet.ActionMouseTarget = touchCursorTarget;

                if (matchAction)
                {
                    bool postProcessCheck = PostProcessAction(matchAction, reflex);
                    if (!postProcessCheck)
                    {
                        // We matched the action, but it failed the post process check (used by "Once" modifier),
                        // so we clear the target set.
                        reflex.targetSet.Clear();
                    }
                }
            }
        }
    }

}   // end of namespace Boku.Programming 
