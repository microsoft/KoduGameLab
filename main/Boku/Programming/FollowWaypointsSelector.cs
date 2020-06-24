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

using KoiX;

using Boku.Base;
using Boku.Common;
using Boku.SimWorld;
using Boku.SimWorld.Path;
using Boku.SimWorld.Terra;

namespace Boku.Programming
{
    /// <summary>
    /// It will interact with a Waypoint set and provide target positions for the actuator 
    /// based upon the state of following said Waypoint set.
    /// 
    /// this selector is known as “Path”, was “Follow”, and “Waypoints”.  
    /// 
    /// </summary>
    public class FollowWaypointsSelector : Selector
    {
        public class State
        {
            WayPoint.Node activeNode;
            // SGI_MOD - keeping track of the past nodes we've been to
            WayPoint.Node previousNode;
            // Different from activeNode, as it seems activeNode could be representing the next node we want to go to
            WayPoint.Node currentNode;

            WayPoint.Path activePath;
            WayPoint.Edge activeEdge;
            Vector3 followTarget = Vector3.Zero;
            Classification.Colors pathColor = Classification.Colors.NotApplicable;

            int frameLastUpdated = 0;   // We need some way to arbitrate between multiple reflexes trying
                                        // to move along different paths.  So, when a path is followed we
                                        // set this to match the current frame number.  If this equals the
                                        // current from number then we assume that an earlier reflex already
                                        // is moving along a path.

            public GameActor parent;    // Owning GameActor.  Not actually needed or used but does 
                                        // make debugging a bit easier.
            
            /// <summary>
            /// Owning GameActor.  Not actually needed or
            /// used but does make debugging a bit easier.
            /// </summary>
            /// <param name="parent"></param>
            public State(GameActor parent)
            {
                this.parent = parent;
            }

            public WayPoint.Node ActiveNode
            {
                get { return activeNode; }
                set { activeNode = value; }
            }

            public WayPoint.Node PreviousNode
            {
                get { return previousNode; }
                set { previousNode = value; }
            }

            public WayPoint.Node CurrentNode
            {
                get { return currentNode; }
                set { currentNode = value; }
            }

            public WayPoint.Path ActivePath
            {
                get { return activePath; }
                set { activePath = value; }
            }

            public WayPoint.Edge ActiveEdge
            {
                get { return activeEdge; }
                set 
                {
                    if (value != activeEdge)
                    {
                        activeEdge = value;
                    }
                }
            }

            public Vector3 FollowTarget
            {
                get { return followTarget; }
                set { followTarget = value; }
            }

            public Classification.Colors PathColor
            {
                get { return pathColor; }
                set { pathColor = value; }
            }

            public int FrameLastUpdated
            {
                get { return frameLastUpdated; }
                set { frameLastUpdated = value; }
            }

            public void Reset()
            {
                ActivePath = null;
                ActiveEdge = null;
                ActiveNode = null;
                PreviousNode = null;
                CurrentNode = null;
                FollowTarget = Vector3.Zero;
                PathColor = Classification.Colors.NotApplicable;
            }

            /// <summary>
            /// Does the current state reflect having a path that matches 
            /// the input color?
            ///
            /// Note that looking for a path with color NotApplicable is valid
            /// for any color path.
            /// </summary>
            /// <param name="color"></param>
            /// <returns></returns>
            public bool ValidForColor(Classification.Colors color)
            {
                bool valid = (color == Classification.Colors.NotApplicable || color == PathColor) && ActiveEdge != null && ActivePath != null;
                return valid;
            }

        }   // end of class State


        private bool used = false;

        /// <summary>
        /// Whether to affect the altitude of the thing following the path.
        /// </summary>
        private bool controlZ = true;
        
        [XmlAttribute]
        public float strength;
        [XmlAttribute]
        public float accuracy = 2.0f;

        public FollowWaypointsSelector()
        {
        }

        public override ProgrammingElement Clone()
        {
            FollowWaypointsSelector clone = new FollowWaypointsSelector();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(FollowWaypointsSelector clone)
        {
            base.CopyTo(clone);
            clone.strength = this.strength;
            clone.accuracy = this.accuracy;
        }

        public override void Reset(Reflex reflex)
        {
            this.used = false;
            base.Reset(reflex);
        }

        /// <summary>
        /// Look at whether something else is controlling vertical height, in which
        /// case we become strictly 2D.
        /// </summary>
        /// <param name="myReflex"></param>
        public override void Fixup(Reflex myReflex)
        {
            base.Fixup(myReflex);

            /// Look over the reflex. If anything else wants to control vertical motion,
            /// then we won't.
            Task task = myReflex.Task;

            controlZ = true;
            for (int iref = 0; iref < task.reflexes.Count; ++iref)
            {
                Reflex otherReflex = task.reflexes[iref] as Reflex;
                if ((otherReflex != null) && (otherReflex != myReflex))
                {
                    Selector otherSelector = otherReflex.Selector;
                    if ((otherSelector is MoveDownSelector)
                        || (otherSelector is MoveUpDownSelector)
                        || (otherSelector is MoveUpSelector))
                    {
                        controlZ = false;
                        break;
                    }
                }
            }
        }

        public override ActionSet ComposeActionSet(Reflex reflex, GameActor gameActor)
        {
            ClearActionSet(actionSet);
            UpdateCanBlend(reflex);

            if (!reflex.targetSet.AnyAction)
                return actionSet;

            Vector3 target = Vector3.Zero;
            bool approachPoint = true;
            if (!this.used)
            {
                approachPoint = FindClosestMatchingWaypointSetTarget(reflex, gameActor, out target);
            }

            if (approachPoint)
            {
                gameActor.followPathState.FollowTarget = target;

                // Calc 2d dist to target.
                Vector3 delta = gameActor.Movement.Position - target;
                delta.Z = 0;    // Force 2d.

                // Test if we are close enough that we reached our follow target.
                if (delta.Length() < accuracy)
                {
                    if (ClosestNodeIsTarget(gameActor))
                    {
                        bool validTarget = ReachedWaypoint(gameActor, ref target);
                        if (validTarget)
                        {
                            gameActor.followPathState.FollowTarget = target;
                        }
                        else
                        {
                            // No valid target so just bail.
                            return actionSet;
                        }
                    }
                }

                target = gameActor.followPathState.FollowTarget;

                // If not controlling Z, we want movement to be 2D so set 
                // the target Z value to match the actor's current Z value.
                if (!controlZ)
                {
                    target.Z = gameActor.Movement.Altitude;
                }

                actionSet.AddAction(Action.AllocTargetLocationAction(reflex, target, autoTurn: true));
            }

            return actionSet;

        }   // end of ComposeActionSet()

        private float TargetAltitude(GameActor actor)
        {
            WayPoint.Node activeNode = actor.followPathState.ActiveNode;
            if (activeNode != null)
            {
                Debug.Assert(actor.followPathState.ActiveEdge != null);

                WayPoint.Node otherNode = actor.followPathState.ActiveEdge.OtherNode(activeNode);
                Vector3 toTargetNode = actor.Movement.Position - activeNode.Position;
                Vector3 nodeToNode = otherNode.Position - activeNode.Position;
                float distSq = nodeToNode.LengthSquared();
                float altitude = activeNode.Position.Z;
                if (distSq > 0)
                {
                    float parm = Vector3.Dot(toTargetNode, nodeToNode) / distSq;
                    altitude = altitude + parm * (otherNode.Position.Z - altitude);
                }
                return altitude;
            }

            return actor.followPathState.FollowTarget.Z;
        }
        protected bool ClosestNodeIsTarget(GameActor actor)
        {
            WayPoint.Node activeNode = actor.followPathState.ActiveNode;
            if (activeNode != null)
            {
                Vector3 actorPos = actor.Movement.Position;
                Vector2 actor2d = new Vector2(actorPos.X, actorPos.Y);
                float activeDistSq = Vector2.DistanceSquared(actor2d, activeNode.Position2d);

                List<WayPoint.Edge> edges = actor.followPathState.ActivePath.Edges;

                for (int i = 0; i < edges.Count; ++i)
                {
                    WayPoint.Edge edge = edges[i];

                    WayPoint.Node other = null;
                    if (edge.Node0 == activeNode)
                    {
                        other = edge.Node1;
                    }
                    else if (edge.Node1 == activeNode)
                    {
                        other = edge.Node0;
                    }
                    if (other != null)
                    {
                        float otherDistSq = Vector2.DistanceSquared(actor2d, other.Position2d);
                        if (otherDistSq < activeDistSq)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Finds the nearest matching path.  If found, also set the targetPosition
        /// which is the point we should be heading toward.  If we already have a path
        /// then this is the position of the next node.  If we didn't have a path, this
        /// is the nearest point on the edge of the new path.
        /// </summary>
        /// <param name="reflex"></param>
        /// <param name="gameActor"></param>
        /// <param name="targetPostion">Where to go.</param>
        /// <returns>True is valid path found.</returns>
        protected bool FindClosestMatchingWaypointSetTarget(Reflex reflex, GameActor gameActor, out Vector3 targetPostion)
        {
            Vector3 actorPos = gameActor.Movement.Position;
            Vector2 actorPos2d = new Vector2(actorPos.X, actorPos.Y);

            bool pathFound = false;
            targetPostion = Vector3.Zero;

            // Get the color of the path we're targeting.
            Classification.Colors color;
            if (reflex.ModifierParams.HasColor)
            {
                color = reflex.ModifierParams.Color;
            }
            else
            {
                color = Classification.Colors.NotApplicable;
            }

            // If we've already got a valid path and it matches this color
            // then we're good.
            // Note, we also look at the frame counter since the state may be
            // from a while ago.  We only want this to count if it's from the previous frame.
            if (gameActor.followPathState.ValidForColor(color) && gameActor.followPathState.FrameLastUpdated + 1 == Time.FrameCounter)
            {
                // Valid path.
                targetPostion = gameActor.followPathState.FollowTarget;
                gameActor.followPathState.FrameLastUpdated = Time.FrameCounter;
                pathFound = true;
            }
            else if (gameActor.followPathState.ActivePath == null || gameActor.followPathState.FrameLastUpdated < Time.FrameCounter)
            {
                // We currently don't have any path (or we have an old path) so try and find a path that matches color.
                // Colors.NotApplicable will match any path.
                WayPoint.Path path = WayPoint.GetNearestPath(color, actorPos);
                if (path != null)
                {
                    // Found as matching path, set the actor's state to follow this path.
                    gameActor.followPathState.ActivePath = path;
                    // Use the actual path color rather than the color we searched for since that my be NotApplicable.
                    gameActor.followPathState.PathColor = path.Color;
                    // Note, even in a path with a single node we still return
                    // a degenerate edge so this will always be valid.
                    gameActor.followPathState.ActiveEdge = WayPoint.GetNearestEdgeFromPath(path, actorPos2d);

                    if (gameActor.followPathState.ActiveEdge != null)
                    {
                        Vector3 nearestPoint = gameActor.followPathState.ActiveEdge.NearestPoint(actorPos);
                        targetPostion = nearestPoint;
                    }
                    else
                    {
                    }
                    
                    gameActor.followPathState.ActiveNode = null;
                    gameActor.followPathState.CurrentNode = null;
                    gameActor.followPathState.FrameLastUpdated = Time.FrameCounter;
                    pathFound = true;
                }
            }

            return pathFound;
        }   // end of FindClosestMatchingWaypointSetTarget()

        /// <summary>
        /// Based on the name this is only supposed to be called when we're
        /// following a valid path and reach one of the WayPoints (nodes).
        /// So, if this is call without a valid path, it's probably an error
        /// upstream.
        /// </summary>
        /// <param name="gameActor"></param>
        /// <returns></returns>
        protected bool ReachedWaypoint(GameActor gameActor, ref Vector3 target)
        {
            bool validTarget = true;
            target = Vector3.Zero;

            // Save out the previously visited nodes.
            gameActor.followPathState.PreviousNode = gameActor.followPathState.CurrentNode;
            gameActor.followPathState.CurrentNode = gameActor.followPathState.ActiveNode;
            if(gameActor.followPathState.ActiveNode != null)
            {
                WayPoint.Node activeNode = gameActor.followPathState.ActiveNode;

                Vector3 actorPosition = gameActor.Movement.Position;
                Vector2 actorPosition2d = new Vector2(actorPosition.X, actorPosition.Y);
                float distance = Vector2.Distance(actorPosition2d, activeNode.Position2d);

                if(distance < accuracy)
                {
                    validTarget = NewWaypointTarget(gameActor, ref target);
                }
            }
            else
            {
                // Reached the edge itself.  Select the nearest node as the next target.

                Vector3 actorPosition = gameActor.Movement.Position;
                Vector2 actorPosition2d = new Vector2(actorPosition.X, actorPosition.Y);

                WayPoint.Node nextNode = gameActor.followPathState.ActiveEdge.NearestNode(actorPosition);
                target = nextNode.Position;

                if (gameActor.followPathState.ActiveNode != null)
                {
                    gameActor.followPathState.ActiveNode.EndTarget();
                    gameActor.followPathState.ActiveNode = null;
                }
                gameActor.followPathState.ActiveNode = nextNode;
                nextNode.BeginTarget(gameActor);

                // Visit the "other" node on this edge so that the actor doesn't loop
                // back around unless absolutely necessary.
                if (nextNode == gameActor.followPathState.ActiveEdge.Node0)
                {
                    gameActor.followPathState.ActiveEdge.Node1.SetVisitedTime(gameActor);
                }
                else
                {
                    gameActor.followPathState.ActiveEdge.Node0.SetVisitedTime(gameActor);
                }
            }

            Debug.Assert(gameActor.followPathState.ActiveNode != null, "Still haven't found a node?");

            // Only check if we are end of path if there are more than 1 nodes in the path
            if (gameActor.followPathState.ActivePath.Nodes.Count > 1)
            {
                if (gameActor.followPathState.PreviousNode == gameActor.followPathState.ActiveNode)
                {
                    gameActor.ReachedEOP = gameActor.followPathState.PathColor;
                }
                else
                {
                    gameActor.ReachedEOP = Classification.Colors.None;
                }
            }
            else
            {

                gameActor.ReachedEOP = Classification.Colors.None;
            }

            return validTarget;
        }

        /// <summary>
        /// Returns the current waypoint target location.
        /// </summary>
        /// <param name="gameActor"></param>
        /// <param name="target"></param>
        /// <returns>True if the target is valid, false otherwise.</returns>
        protected bool NewWaypointTarget(GameActor gameActor, ref Vector3 target)
        {
            bool validTarget = false;

            WayPoint.Node activeNode = gameActor.followPathState.ActiveNode;

            target = gameActor.followPathState.FollowTarget;
            WayPoint.Edge newEdge = WayPoint.GetNextEdgeFromPath(gameActor.followPathState.ActivePath, activeNode, gameActor);

            if (newEdge != null)
            {
                activeNode.EndTarget();
                activeNode.SetVisitedTime(gameActor);

                // find the opposite node to the new edge of the current node
                if (newEdge.Node0 == activeNode)
                {
                    // use node1 off new edge
                    activeNode = newEdge.Node1;
                }
                else
                {
                    // use node0 off new edge
                    activeNode = newEdge.Node0;
                }

                target = activeNode.Position;
                activeNode.BeginTarget(gameActor);
                gameActor.followPathState.ActiveNode = activeNode;

                gameActor.followPathState.ActiveEdge = newEdge;

                validTarget = true;
            }

            return validTarget;
        }

        public override void Used(bool newUse)
        {
            this.used = true;
        }
    }
}
