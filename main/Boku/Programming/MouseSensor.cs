
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
    /// Senses mouse events: left button click, right button click, or hover over terrain type or an actor.
    /// </summary>
    public class MouseSensor : Sensor
    {
        private SensorTargetSet senseSet = new SensorTargetSet();
        private SensorTargetSet.Enumerator senseSetIter = null;

        private TerrainSensor terrainSensor;

        public MouseSensor()
        {
            senseSetIter = (SensorTargetSet.Enumerator)senseSet.GetEnumerator();
        }

        public override ProgrammingElement Clone()
        {
            MouseSensor clone = new MouseSensor();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(MouseSensor clone)
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
                //if (MouseEdit.HitInfo.TerrainHit)
                {
                    terrainSensor.OverrideSenseMaterial = MouseEdit.HitInfo.TerrainMaterial;
                }

                terrainSensor.FinishUpdate(gameActor);
            }
            else
            {
                if (MouseEdit.HitInfo.HaveActor)
                {
                    GameActor mousedActor = MouseEdit.HitInfo.ActorHit;

                    Vector3 actorCenter = Vector3.Transform(
                        gameActor.BoundingSphere.Center,
                        gameActor.Movement.LocalMatrix);

                    Vector3 thingCenter = Vector3.Transform(
                        mousedActor.BoundingSphere.Center,
                        mousedActor.Movement.LocalMatrix);

                    Vector3 direction = thingCenter - actorCenter;

                    float range = direction.Length();
                    if (range > 0.0f)
                    {
                        direction *= 1.0f / range; // Normalize.
                    }

                    SensorTarget target = SensorTargetSpares.Alloc();
                    target.Init(mousedActor, direction, range);
                    senseSet.AddOrFree(target);
                }

                senseSet.Add(NullActor.Instance, Vector3.Zero, float.MaxValue);
            }
        }

        private new bool TestObjectSet(Reflex reflex)
        {
            if (terrainSensor != null)
            {
                return terrainSensor.TestObjectSet(reflex);
            }
            else
            {
                bool match = true;
                return match;
            }
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

                MouseFilter mouseFilter = reflex.Data.GetFilterByType(typeof(MouseFilter)) as MouseFilter;

                if (mouseFilter == null)
                {
                    // TODO this should trigger a hint.  You should always have a mouse filter with a mouse sensor.
                    return;
                }

                // Cause the filter to detect what is under the mouse cursor and set that information on the reflex.
                // MatchAction will be true if the MouseFilter passes but then we still need to filter on any other 
                // filters including the ever annoying "not", "me" and "anything" filters.
                Object param = null;
                bool matchAction = mouseFilter.MatchAction(reflex, out param);

                //-----------------------------
                //GUI Button filter.
                GUIButtonFilter guiButtonFilter = reflex.Data.GetFilterByType(typeof(GUIButtonFilter)) as GUIButtonFilter;
                if( null != guiButtonFilter )
                {
                    matchAction = guiButtonFilter.MatchAction(reflex, out param);
                }

                bool notFilter = reflex.Data.FilterExists("filter.not");
                bool anythingFilter = reflex.Data.FilterExists("filter.anything");

                // Give the anythingFilter a chance to kill the action.
                if (anythingFilter && reflex.MouseActor == null)
                {
                    matchAction = false;
                }

                bool mouseCursorTarget = false;
                bool mouseTargetNeeded = false;
                if (matchAction || reflex.IsMovement)
                {
                    GameActor mouseActor = reflex.MouseActor;   // Preserve this in case we null out the reflex ref and then restore it because of "not".  Argh.

                    // If we have an actor mouse target, check if it passes the filters.
                    // If not, null it out.
                    if (reflex.MouseActor != null)
                    {
                        // Create a target.
                        SensorTarget target = SensorTargetSpares.Alloc();
                        target.Init(reflex.Task.GameActor, reflex.MouseActor);

                        // Test againt the filters.
                        for (int i = 0; i < filters.Count; i++)
                        {
                            if (!filters[i].MatchTarget(reflex, target))
                            {
                                reflex.MouseActor = null;
                                matchAction = false;
                            }
                        }

                        // Handle "not" filter.
                        if (notFilter)
                        {
                            if (reflex.MouseActor == null)
                            {
                                // Restore MouseActor.
                                reflex.MouseActor = mouseActor;
                            }
                            else
                            {
                                // Killed by the "not" filter.
                                reflex.MouseActor = null;
                                reflex.MousePosition = null;
                                matchAction = false;
                            }
                        }

                        // If we stil have a target, add it to the target set.
                        if (reflex.MouseActor != null)
                        {
                            reflex.targetSet.Add(target);
                            mouseCursorTarget = true;
                        }
                        else
                        {
                            // Don't really need this target so free it.
                            SensorTargetSpares.Free(target);
                        }
                    }
                    else
                    {
                        // No MouseActor case (the mouse click was not on an actor)

                        // If we have any classification filters, then we must have been 
                        // looking for an actor. Since we don't have an actor, the 
                        // result is false.
                        if (reflex.hasClassificationFitler)
                        {
                            matchAction = false;
                        }

                        // If we've got a me filter and not MouseActor we've failed.
                        if (reflex.hasMeFilter)
                        {
                            matchAction = false;
                        }

                        // MouseActor is null so we didn't click on anything which means that if we have an anything 
                        // filter, matchAction should stay false.  Unless, of course we also have a notFilter. 
                        if (!anythingFilter || notFilter)
                        {
                            // No mouse actor but if we have a "not" or this is a movement toward reflex then the result should be true.
                            // The WHEN Mouse Over DO Turn Toward option is used for mouse-look style controls.
                            // Note that if the reflex is Move Forward we don't want this to trigger.
                            if (notFilter || (reflex.IsMovement && reflex.selectorUpid == "selector.towardclosest") || (reflex.IsMovement && reflex.actuatorUpid == "actuator.turn"))
                            {
                                mouseTargetNeeded = true;
                                matchAction = true;
                            }
                        }
                    }
                }
                else
                {
                    if (notFilter || reflex.IsMovement)
                    {
                        mouseTargetNeeded = true;
                    }
                }

                // If MouseActor is null, we may still want to target a position on the ground.
                //if (reflex.MouseActor == null && !matchAction && reflex.IsMovement)
                if(mouseTargetNeeded || (reflex.MouseActor == null && matchAction))
                {
                    if (reflex.MousePosition != null)
                    {
                        // We need to add the mouse position to the targetSet.
                        SensorTarget target = SensorTargetSpares.Alloc();
                        target.GameThing = senseSet.Nearest != null ? senseSet.Nearest.GameThing : NullActor.Instance;
                        target.Position = reflex.MousePosition.Value;
                        target.Direction = target.Position - reflex.Task.GameActor.Movement.Position;
                        target.Range = target.Direction.Length();

                        // If we've arrived at the target position, clear MousePosition.
                        Vector2 dir2d = new Vector2(target.Direction.X, target.Direction.Y);
                        if (dir2d.LengthSquared() < 0.1f)
                        {
                            reflex.MousePosition = null;
                        }

                        target.Direction /= target.Range;
                        reflex.targetSet.Add(target);
                        mouseCursorTarget = true;
                    }
                    else
                    {
                        // Is this a mouse move case?  If so translate the param values into
                        // a target the actuator can use.
                        MouseFilter filter = (MouseFilter)reflex.Data.GetFilterByType(typeof(MouseFilter));
                        if(filter.type == MouseFilterType.Move)
                        {
                            Vector2 position = (Vector2)param;

                            SensorTarget target = SensorTargetSpares.Alloc();

                            target.GameThing = senseSet.Nearest.GameThing;
                            // Calc fake position.
                            GameActor actor = reflex.Task.GameActor;
                            Vector3 pos = actor.Movement.Position;

                            target.Position = pos;
                            target.Direction = target.Position - reflex.Task.GameActor.Movement.Position;
                            target.Range = target.Direction.Length();
                            if (target.Range == 0)
                            {
                                target.Direction = Vector3.Zero;
                            }
                            else
                            {
                                target.Direction /= target.Range;
                            }
                            reflex.targetSet.Add(target);
                            reflex.targetSet.Param = position;
                            mouseCursorTarget = true;
                        }
                    }
                }

                reflex.targetSet.Action = reflex.targetSet.Count > 0 && TestObjectSet(reflex);
                reflex.targetSet.Action |= matchAction;

                // This forces the movement to keep going toward the clicked position/bot
                // even if the mouse is no longer clicked.
                reflex.targetSet.ActionMouseTarget = mouseCursorTarget;

                reflex.targetSet.Action = PostProcessAction(reflex.targetSet.Action, reflex);
            }
        }
    }

}   // end of namespace Boku.Programming 
