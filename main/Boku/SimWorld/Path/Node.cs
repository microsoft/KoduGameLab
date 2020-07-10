// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;
using Boku.Fx;
using Boku.Common;
using Boku.Common.ParticleSystem;
using Boku.SimWorld.Terra;

namespace Boku.SimWorld.Path
{
    public partial class WayPoint : INeedsDeviceReset
    {
        public class Node
        {
            #region HelperClasses
            /// <summary>
            /// A helper class to keep track of the last time 
            /// this node was visited by a particular actor.
            /// </summary>
            protected class TimeStamp
            {
                public Object actor = null;
                public double time = double.MinValue;

                // c'tor
                public TimeStamp(object actor, double time)
                {
                    this.actor = actor;
                    this.time = time;
                }
            }   // end of class TimeStamp

            /// <summary>
            /// A static helper class to keep track of the last node that
            /// was visited by a given actor.  Used in a static array so 
            /// that there's only a single entry per actor.
            /// </summary>
            public class PrevNode
            {
                #region Members
                private Object actor = null;
                private Node node = null;

                private static List<PrevNode> nodes = new List<PrevNode>();

                #endregion Members

                #region Public
                /// <summary>
                /// Reset to virgin state.
                /// </summary>
                public static void Reset()
                {
                    nodes.Clear();
                }

                /// <summary>
                /// Sets the previously visited node for a given actor.
                /// </summary>
                /// <param name="actor"></param>
                /// <param name="node"></param>
                public static void SetPrevNode(Object actor, Node node)
                {
                    // Make sure we have a valid list.
                    Debug.Assert(nodes != null, "List allocated at startup");

                    // Search through list looking for PrevNode matching actor.
                    // If found, update, if not, create a new PrevNode and add to list.
                    bool found = false;
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        PrevNode p = nodes[i];
                        if (p.actor == actor)
                        {
                            p.node = node;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        PrevNode p = new PrevNode(actor, node);
                        nodes.Add(p);
                    }

                }   // end of PrevNode SetPrevNode()

                /// <summary>
                /// Returns the previously visited node for an actor.  May be null.
                /// </summary>
                /// <param name="actor"></param>
                /// <returns></returns>
                public static Node GetPrevNode(Object actor)
                {
                    // Make sure we have a valid list.
                    Debug.Assert(nodes != null, "List allocated at startup");

                    Node result = null;
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        PrevNode p = nodes[i];
                        if (p.actor == actor)
                        {
                            result = p.node;
                            break;
                        }
                    }

                    return result;
                }   // end of PrevNode GetPrevNode()

                public static void ReleasePrevNode(Object actor)
                {
                    for (int i = nodes.Count - 1; i >= 0; --i)
                    {
                        PrevNode p = nodes[i];
                        if (p.actor == actor)
                        {
                            nodes.RemoveAt(i);
                            break;
                        }
                    }
                }

                #endregion Public

                #region Internal
                // c'tor
                private PrevNode(Object actor, Node node)
                {
                    this.actor = actor;
                    this.node = node;
                }   // end of PrevNode c'tor
                #endregion Internal

            }   // end of class PrevNode

            #endregion HelperClasses

            #region Members
            private bool perActorVisitTimes = false;    // If set to false then last visited times are shared among
            // all actors rather than being individualized.

            private Path path = null;
            private Vector3 position;
            private float height;
            private List<TimeStamp> timestamps = new List<TimeStamp>();
#if !MF_ALWAYS_AIR
            private bool airborne = false;
#endif // !MF_ALWAYS_AIR
            public int temp = 0;        // Internal use only.  Used when scanning paths for coherence.

            public int target = 0;
            public Vector4 targetColor = Vector4.One;

            private bool moving = false;
            private bool edit = false;
            private bool select = false;

            #endregion Members

            #region Accessors

            /// <summary>
            /// The maximum height above terrain level that nodes are allowed to be placed.
            /// </summary>
            public const float kMaxHeight = 1.0e5f;

            /// <summary>
            /// True if this node or any neighbors are not On Ground
            /// </summary>
            public bool Airborne
            {
#if MF_ALWAYS_AIR
                get { return true; }
                set { ; }
#else // MF_ALWAYS_AIR
                get { return airborne; }
                set { airborne = value; }
#endif // MF_ALWAYS_AIR
            }
            /// <summary>
            /// True if this node hugs the terrain.
            /// </summary>
            public bool OnGround
            {
                get { return height <= 0.0f; }
            }
            /// <summary>
            /// Where is its base.
            /// </summary>
            public Vector3 Position
            {
                get { return position; }
                set 
                {
                    position.X = value.X;
                    position.Y = value.Y;
                    position.Z = height + Terrain.GetTerrainHeightFlat(position);
                }
            }
            /// <summary>
            /// Height off the terrain
            /// </summary>
            public float Height
            {
                get { return height; }
                set
                {
                    height = MathHelper.Clamp(value, 0, kMaxHeight);
                    float terrHeight = Terrain.GetTerrainHeightFlat(position);

                    float newZ = terrHeight + height;
                    if (position.Z != newZ)
                    {
                        position.Z = newZ;
                        if (path != null)
                        {
                            path.RecalcHeights();
                        }
                    }
                }
            }
            /// <summary>
            /// What path does it belong to
            /// </summary>
            public Path Path
            {
                get { return path; }
                set { path = value; }
            }
            /// <summary>
            /// How big are the spheres representing nodes.
            /// </summary>
            public static float Radius
            {
                get { return radius; }
            }
            /// <summary>
            /// What height above the ground do the spheres get rendered.
            /// </summary>
            public static float SphereHeight
            {
                get { return sphereHeight; }
            }
            /// <summary>
            /// Position as a Vector2.
            /// </summary>
            public Vector2 Position2d
            {
                get { return new Vector2(position.X, position.Y); }
                set { position.X = value.X; position.Y = value.Y; }
            }

            public bool IsTarget
            {
                get { return target > 0; }
            }
            public bool Moving
            {
                get { return moving; }
                set { moving = value; }
            }
            public bool Edit
            {
                get { return edit; }
                set { edit = value; }
            }
            public bool Select
            {
                get { return select; }
                set { select = value; }
            }

            /// <summary>
            /// Debug functions for tracking where a path follower is on the path.
            /// </summary>
            /// <param name="color"></param>
            public void BeginTarget(Vector4 color)
            {
                ++target;
                targetColor = color;
            }
            /// <summary>
            /// Debug functions for tracking where a path follower is on the path.
            /// </summary>
            /// <param name="color"></param>
            public void BeginTarget(GameActor actor)
            {
                BeginTarget(Classification.ColorVector4(actor.ClassColor));
            }
            /// <summary>
            /// Debug functions for tracking where a path follower is on the path.
            /// </summary>
            public void EndTarget()
            {
                Debug.Assert(IsTarget, "Popping an empty stack");
                --target;
            }
            #endregion

            #region Public
            // c'tor
            /// <summary>
            /// Create a new waypoint node.
            /// </summary>
            /// <param name="path">The path to add this node to.  If null, then a new path will be created.</param>
            /// <param name="position">Position of the new waypoint.  The height will be adjusted automatically.</param>
            public Node(Path path, Vector3 position)
            {
                Height = position.Z;
                Position = position;

                if (path != null)
                {
                    this.path = path;
                }
                else
                {
                    // TODO Should probably use whatever the current color is?
                    path = new Path(Classification.Colors.Red);
                }

                path.AddNode(this);
            }   // end of Node c'tor

            /// <summary>
            /// c'tor which assumes path will be set later.
            /// </summary>
            /// <param name="position"></param>
            public Node(Vector3 position)
            {
                Height = 0.0f;
                Position = position;
            }   // end of Node c'tor

            public void Rotate(Matrix delta)
            {
                Position = Vector3.Transform(Position, delta);
                InGame.IsLevelDirty = true; // Force a save.
            }
            /// <summary>
            /// Apply the translation and force an update of our path.
            /// </summary>
            /// <param name="delta"></param>
            public void Translate(Vector3 delta)
            {
                if (delta != Vector3.Zero)
                {
                    Vector3 pos = Position;
                    pos.X += delta.X;
                    pos.Y += delta.Y;
                    Height += delta.Z;
                    Position = pos;

                    Path.RecalcHeights();
                }
            }

            public void ClearEdit()
            {
                Moving = false;
                Edit = false;
                Select = false;
                Path.ClearEdit();
            }

            /// <summary>
            /// Draw it.
            /// </summary>
            /// <param name="camera"></param>
            /// <param name="color"></param>
            public void Render(Camera camera, bool asRoad, Vector4 color)
            {
                bool isTarget = IsTarget;
                bool isEdit = Edit || Path.Edit;
                bool isSelect = Select || Path.Select;
                bool isMouse = false;

                Vector3 pos = Position;

                Render(camera, pos, Height, color, asRoad, Path, isEdit, isSelect, isMouse, isTarget, targetColor);
            }
            public static Vector3 NodeRenderPosition(Vector3 pos, float height, bool asRoad)
            {
                if (asRoad)
                {
                    pos.Z = Terrain.GetTerrainAndPathHeight(pos);
                }
                else
                {
                    pos.Z = height + Terrain.GetTerrainHeightFlat(pos);
                }
                pos.Z += SphereHeight + Radius;

                return pos;
            }
            public Vector3 RenderPosition(bool asRoad)
            {
                return NodeRenderPosition(Position, Height, asRoad);
            }
            public static void Render(
                Camera camera, 
                Vector3 pos, 
                float height,
                Vector4 color, 
                bool asRoad,
                Path path, 
                bool isEdit, 
                bool isSelect,
                bool isMouse,
                bool isTarget,
                Vector4 targetColor)
            {
                // Set radius and translation.
                pos.Z = NodeRenderPosition(pos, height, asRoad).Z;

                float currentRadius = radius;
                if (isEdit || isSelect || isMouse)
                {
                    currentRadius += 0.1f * (float)(Math.Sin(10.0 * Time.WallClockTotalSeconds) + 1.0);
                }
                if (camera.Frustum.CullTest(pos, currentRadius) == Frustum.CullResult.TotallyOutside)
                    return;

                Sphere sphere = Sphere.GetInstance();

                // Get the effect we need.  Borrow it from the particle system manager.
                ParticleSystemManager manager = InGame.inGame.ParticleSystemManager;

                // Set up common rendering values.
                Effect effect = manager.Effect3d;
                Vector4 renderColor = color;

                // Set parameters.
                manager.Parameter(ParticleSystemManager.EffectParams3d.Radius).SetValue(currentRadius);
                manager.Parameter(ParticleSystemManager.EffectParams3d.SpecularColor).SetValue(new Vector4(0.9f));
                manager.Parameter(ParticleSystemManager.EffectParams3d.SpecularPower).SetValue(16.0f);

                manager.Parameter(ParticleSystemManager.EffectParams3d.Shininess).SetValue(0.4f);

                // Set up world matrix.
                Matrix worldMatrix = Matrix.Identity;
                worldMatrix.Translation = pos;

                if (isTarget)
                {
                    renderColor = targetColor;
                    BokuGame.bokuGame.GraphicsDevice.RasterizerState = UI2D.Shared.RasterStateWireframe;
                }

                effect.CurrentTechnique = manager.Technique(ParticleSystemManager.EffectTech3d.TransparentColorPassNoZ);

                Vector4 darkColor = renderColor * 0.5f;
                Vector4 emissiveColor = Vector4.UnitW;
                if (isEdit)
                {
                    darkColor = new Vector4(0, 0, 1.0f, 1.0f);
                    emissiveColor = new Vector4(0, 0, 1.0f, 1.0f);
                }
                else if (isSelect)
                {
                    if (isMouse)
                    {
                        darkColor = new Vector4(1.0f, 0, 1.0f, 1.0f);
                        emissiveColor = new Vector4(1.0f, 0, 1.0f, 1.0f);
                    }
                    else
                    {
                        darkColor = new Vector4(1.0f, 0, 0, 1.0f);
                        emissiveColor = new Vector4(1.0f, 0, 0, 1.0f);
                    }
                }
                else if (isMouse)
                {
                    darkColor = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
                    emissiveColor = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
                }
                manager.Parameter(ParticleSystemManager.EffectParams3d.DiffuseColor).SetValue(darkColor);
                manager.Parameter(ParticleSystemManager.EffectParams3d.EmissiveColor).SetValue(emissiveColor);
                manager.Parameter(ParticleSystemManager.EffectParams3d.Alpha).SetValue(0.25f);

                sphere.Render(camera, ref worldMatrix, effect);

                effect.CurrentTechnique = Alpha > 0.99f
                    ? manager.Technique(ParticleSystemManager.EffectTech3d.OpaqueColorPass)
                    : manager.Technique(ParticleSystemManager.EffectTech3d.TransparentColorPass);

                manager.Parameter(ParticleSystemManager.EffectParams3d.Radius).SetValue(radius);
                manager.Parameter(ParticleSystemManager.EffectParams3d.DiffuseColor).SetValue(renderColor);
                manager.Parameter(ParticleSystemManager.EffectParams3d.EmissiveColor).SetValue(Vector4.Zero);
                manager.Parameter(ParticleSystemManager.EffectParams3d.Alpha).SetValue(Alpha);

                // Render
                sphere.Render(camera, ref worldMatrix, effect);

                if (isTarget)
                {
                    BokuGame.bokuGame.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
                }

            }   // end of Node Render()

            public void RenderNewEdge(Camera camera, Vector3 pos)
            {
                Vector4 color = Path.RGBColor;

                _edgePts_scratch.Clear();
                Vector3 pos0 = NodeRenderPosition(Position, Height, Path.Road.Generator != null);
                _edgePts_scratch.Add(pos0);

                Vector3 pos1 = NodeRenderPosition(pos, Height, false);
                _edgePts_scratch.Add(pos1);

                Utils.DrawFatLine(camera, _edgePts_scratch, color);

                pos1.Z = 0.0f;
                Render(camera,
                    pos1,
                    Height,
                    color,
                    false,
                    Path,
                    false,
                    true,
                    false,
                    false,
                    Vector4.Zero);
            }
            private List<Node> _otherNodes_scratch = new List<Node>();
            private List<Vector3> _edgePts_scratch = new List<Vector3>();

            /// <summary>
            /// Deletes the node.
            /// </summary>
            public void Delete()
            {
                Path.RemoveNode(this);
                InGame.IsLevelDirty = true; // Force a save.
            }   // end of Node Delete()

            /// <summary>
            /// Set the current time as the most recently visited time for the given actor.  Actor may be null.
            /// </summary>
            /// <param name="actor"></param>
            /// <returns></returns>
            public void SetVisitedTime(Object actor)
            {
                // Update the PrevNode list.  Do this first 
                // in case actor gets nulled out.
                PrevNode.SetPrevNode(actor, this);

                if (!perActorVisitTimes)
                {
                    actor = null;
                }

                // See if we've already got an entry for this 
                // actor.  If so, update it.
                for (int i = 0; i < timestamps.Count; i++)
                {
                    TimeStamp t = timestamps[i];
                    if (t.actor == actor)
                    {
                        t.time = Time.WallClockTotalSeconds;
                        return;
                    }
                }

                // No entry found.  Create a new one.
                TimeStamp ts = new TimeStamp(actor, Time.WallClockTotalSeconds);
                timestamps.Add(ts);

            }   // end of Node SetVisitedTime()

            /// <summary>
            /// Returns the time this node was last visited by this actor.  Actor may be null.
            /// </summary>
            /// <param name="actor"></param>
            /// <returns></returns>
            public double GetLastVisitedTime(Object actor)
            {
                if (!perActorVisitTimes)
                {
                    actor = null;
                }

                for (int i = 0; i < timestamps.Count; i++)
                {
                    TimeStamp t = timestamps[i];
                    if (t.actor == actor)
                    {
                        return t.time;
                    }
                }

                return 0.0;
            }   // end of Node GetLastVisitedTime()

            /// <summary>
            /// _2D_ distance squared from pos to this node.
            /// </summary>
            /// <param name="pos"></param>
            /// <returns></returns>
            public float DistanceSquared(Vector2 pos)
            {
                return Vector2.DistanceSquared(pos, Position2d);
            }

            /// <summary>
            /// _3D_ distance squared from pos to this node.
            /// </summary>
            /// <param name="pos"></param>
            /// <returns></returns>
            public float DistanceSquared(Vector3 pos)
            {
                return Vector3.DistanceSquared(pos, Position);
            }

            /// <summary>
            /// _2D_ distance to pos.
            /// </summary>
            /// <param name="pos"></param>
            /// <returns></returns>
            public float Distance(Vector2 pos)
            {
                return (float)Math.Sqrt(DistanceSquared(pos));
            }

            /// <summary>
            /// _3D_ distance to pos.
            /// </summary>
            /// <param name="pos"></param>
            /// <returns></returns>
            public float Distance(Vector3 pos)
            {
                return (float)Math.Sqrt(DistanceSquared(pos));
            }

            #endregion Public

            #region Internal
            #endregion Internal

        }   // end of class Node

    }
}
