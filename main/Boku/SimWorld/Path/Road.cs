// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;

using Boku.Common;
using Boku.Base;
using Boku.SimWorld.Terra;

namespace Boku.SimWorld.Path
{
    /// <summary>
    /// The visual representation for a path
    /// </summary>
    public partial class Road
    {
        #region Members
        private static float step = 0.25f;
        private double radStep = Math.PI / 12.0;
        private bool needsRebuild = true;
        private static bool wireFrame = false;

        private RoadGenerator generator;
        private WayPoint.Path path = null;
        private List<Section> sections = new List<Section>();
        private List<Intersection> intersections = new List<Intersection>();
        #endregion Members

        #region Accessors
        /// <summary>
        /// This road has a height on which things can move.
        /// </summary>
        public bool IsGround
        {
            get { return Generator != null ? Generator.MakesGround : false; }
        }
        /// <summary>
        ///  This road acts as a wall
        /// </summary>
        public bool BlocksTravel
        {
            get { return Generator != null ? Generator.MakesBlocker : false; }
        }
        /// <summary>
        /// Geometry needs to be regenerated.
        /// </summary>
        public bool NeedsRebuild
        {
            get { return needsRebuild; }
        }
        ///// <summary>
        ///// Widest width of the road
        ///// </summary>
        public static float Step
        {
            get { return step; }
            set { step = value; }
        }
        /// <summary>
        /// Stepsize in radians around sections in build.
        /// </summary>
        public double RadStep
        {
            get { return radStep; }
            set { radStep = value; }
        }
        /// <summary>
        /// The path this road is the visualization for.
        /// </summary>
        public WayPoint.Path Path
        {
            get { return path; }
        }
        /// <summary>
        /// The generator this road uses to generate visualization.
        /// </summary>
        public RoadGenerator Generator
        {
            get { return generator; }
            set { generator = value; Rebuild(); }
        }
        /// <summary>
        /// The road sections in this road.
        /// </summary>
        public List<Section> Sections
        {
            get { return sections; }
            private set { sections = value; }
        }
        /// <summary>
        /// The road intersections in this road.
        /// </summary>
        public List<Intersection> Intersections
        {
            get { return intersections; }
            private set { intersections = value; }
        }
        #endregion

        #region Public
        /// <summary>
        /// Constructor, takes in owning path, which this road represents.
        /// </summary>
        /// <param name="inPath"></param>
        public Road(WayPoint.Path inPath)
        {
            generator = generators[genIndex];
            path = inPath;
        }

        /// <summary>
        /// Render all elementes of this road.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="shadowTex"></param>
        public void Render(Camera camera)
        {
            if (Path.Moving)
                return;

            CheckRebuild();

            if (Generator != null)
            {
                Generator.Update();
            }

            foreach (Section section in Sections)
            {
                section.Render(camera);
            }
            FlushBatch(camera);
            foreach (Intersection isect in Intersections)
            {
                isect.Render(camera);
            }
            FlushBatch(camera);
        }

        /// <summary>
        /// Look for any road UNDER input position, get height 
        /// and return true and update height only if higher than passed in height, 
        /// else return false w/ height untouched.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public bool GetHeight(Vector3 pos, ref float height)
        {
            bool ret = false;
            if (IsGround)
            {
                foreach (Section section in Sections)
                {
                    float tstHeight = 0.0f;
                    if (section.GetHeight(pos, ref tstHeight))
                    {
                        if (tstHeight > height)
                        {
                            height = tstHeight;
                            ret = true;
                        }
                    }
                }
                if (!ret)
                {
                    foreach (Intersection isect in Intersections)
                    {
                        float tstHeight = 0.0f;
                        if (isect.GetHeight(pos, ref tstHeight))
                        {
                            if (tstHeight > height)
                            {
                                height = tstHeight;
                                ret = true;
                            }
                        }
                    }
                }
            }
            return ret;
        }
        public bool GetHeightAndNormal(Vector3 pos, ref float maxHeight, ref Vector3 normal)
        {
            bool ret = false;
            if (IsGround)
            {
                foreach (Section section in Sections)
                {
                    float tstHeight = 0.0f;
                    Vector3 tstNormal = Vector3.UnitZ;
                    if (section.GetHeightAndNormal(pos, ref tstHeight, ref tstNormal))
                    {
                        if (tstHeight > maxHeight)
                        {
                            maxHeight = tstHeight;
                            normal = tstNormal;
                            ret = true;
                        }
                    }
                }
                if (!ret)
                {
                    foreach (Intersection isect in Intersections)
                    {
                        float tstHeight = 0.0f;
                        Vector3 tstNormal = Vector3.UnitZ;
                        if (isect.GetHeightAndNormal(pos, ref tstHeight, ref tstNormal))
                        {
                            if (tstHeight > maxHeight)
                            {
                                maxHeight = tstHeight;
                                normal = tstNormal;
                                ret = true;
                            }
                        }
                    }
                }
            }
            return ret;
        }
        /// <summary>
        /// Collision results return structure.
        /// </summary>
        public struct CollisionInfo
        {
            public float depth;
            public Vector3 norm;
        }
        /// <summary>
        /// Find intersections with input sphere and any road elements.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="collisions"></param>
        /// <returns></returns>
        public bool GetCollisions(Vector3 center, float radius, List<CollisionInfo> collisions)
        {
            if (BlocksTravel)
            {
                foreach (Section section in Sections)
                {
                    section.GetCollisions(center, radius, collisions);
                }
                foreach (Intersection isect in Intersections)
                {
                    isect.GetCollisions(center, radius, collisions);
                }
            }
            return collisions.Count > 0;
        }

        private bool DegenerateBlocked(Vector3 src, ref Terrain.HitBlock hitBlock)
        {
            float pathHeight = 0.0f;
            /// Handle unlikely degenerate input.
            if (GetHeight(src, ref pathHeight))
            {
                hitBlock.Max = hitBlock.Min = pathHeight;
                hitBlock.Position = new Vector3(src.X, src.Y, pathHeight);
                hitBlock.Normal = Vector3.UnitZ;
                hitBlock.BlockHeight = pathHeight;
                hitBlock.CrossesPath = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Find the first (if any) wall blocking ray.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="hitBlock"></param>
        /// <returns></returns>
        public bool Blocked(Vector3 src, Vector3 dst, ref Terrain.HitBlock hitBlock)
        {
            if (!BlocksTravel && !IsGround)
            {
                return false;
            }
            /// Care about two cases:
            /// a. src on path, dst off (dst possibly off end of world)
            ///     need point where leave path, and normal back in there.
            /// b. ray src to dst passes through path 
            ///     need first point hitting path, and normal out from there
            const float kCloseTogether = Single.Epsilon;
            if (Vector3.DistanceSquared(src, dst) <= kCloseTogether)
            {
                return DegenerateBlocked(src, ref hitBlock);
            }

            bool hit = false;
            foreach (Section section in Sections)
            {
                if (section.Blocked(src, dst, ref hitBlock))
                {
                    dst = hitBlock.Position;
                    hit = true;
                }
            }
            foreach (Intersection isect in Intersections)
            {
                if (isect.Blocked(src, dst, ref hitBlock))
                {
                    dst = hitBlock.Position;
                    hit = true;
                }
            }
            return hit;
        }
        public void ResetAbstain()
        {
            foreach (Section section in Sections)
            {
                section.Abstain = false;
            }
            foreach (Intersection isect in Intersections)
            {
                isect.Abstain = false;
            }
        }
        public bool LeavesRoad(Vector3 src, Vector3 dst, ref Terrain.HitBlock hitBlock)
        {
            if (!BlocksTravel && !IsGround)
            {
                return false;
            }
            bool leaves = false;
            bool onPath = false;
            do
            {
                onPath = false;
                foreach (Section section in Sections)
                {
                    if (!section.Abstain && section.Leaves(src, dst, ref hitBlock))
                    {
                        src = hitBlock.Position;
                        section.Abstain = true;
                        onPath = true;
                        leaves = true;
                        break;
                    }
                }
                foreach (Intersection isect in Intersections)
                {
                    if (!isect.Abstain && isect.Leaves(src, dst, ref hitBlock))
                    {
                        src = hitBlock.Position;
                        isect.Abstain = true;
                        onPath = true;
                        leaves = true;
                        break;
                    }
                }
            }
            while (onPath);

            return leaves;
        }

        /// <summary>
        /// Rebuild the road.
        /// </summary>
        public void Build()
        {
            if (Generator != null)
            {
                Step = Terrain.Current.CubeSize * 0.5f;

                foreach (WayPoint.Edge edge in path.Edges)
                {
                    Section section = new Section();

                    if (section.Connect(this, edge))
                    {
                        Sections.Add(section);
                    }
                }
                foreach (WayPoint.Node node in path.Nodes)
                {
                    Intersection isect = new Intersection();

                    if (isect.Connect(this, node))
                    {
                        Intersections.Add(isect);
                    }
                }

                List<Section> intermedSections = Sections;
                Sections = new List<Section>();

                foreach (Section section in intermedSections)
                {
                    if (section.Build())
                    {
                        Sections.Add(section);
                    }
                }
                List<Intersection> intermedIsects = Intersections;
                Intersections = new List<Intersection>();
                foreach (Intersection isect in intermedIsects)
                {
                    if (isect.Build())
                    {
                        Intersections.Add(isect);
                    }
                }

                foreach (Section section in Sections)
                {
                    section.Finish();
                }
                foreach (Intersection isect in Intersections)
                {
                    isect.Finish();
                }
            }

            // We just built so no need to rebuild unless something changes.
            needsRebuild = false;

        }   // end of Build()

        /// <summary>
        /// Free up anything that needs explicit freeing (device stuff)
        /// </summary>
        public void Clear()
        {
            foreach (Section section in sections)
            {
                section.Clear();
            }
            sections.Clear();
            foreach (Intersection isection in intersections)
            {
                isection.Clear();
            }
            intersections.Clear();
        }

        /// <summary>
        /// Queue up an imminent rebuild
        /// </summary>
        public void Rebuild()
        {
            needsRebuild = true;
        }
        /// <summary>
        /// Do a rebuild if necessary.
        /// </summary>
        public void CheckRebuild()
        {
            if (NeedsRebuild)
            {
                Clear();
                Build();
                needsRebuild = false;
            }
        }
        #endregion Public

        #region Internal
        /// <summary>
        /// Add a renderable to the batching list pending FlushBatch.
        /// </summary>
        /// <param name="obj"></param>
        public void AddBatch(RoadStdRenderObj obj)
        {
            batch.Add(obj);
        }

        /// <summary>
        /// Pass through device resets
        /// </summary>
        /// <param name="graphics"></param>
        public static void LoadContent(bool immediate)
        {
            InitGenerators();
            foreach (RoadGenerator gen in generators)
            {
                if (gen != null)
                {
                    BokuGame.Load(gen);
                }
            }
            WayPoint.RebuildAll();
        }

        public static void InitDeviceResources(GraphicsDevice device)
        {
        }

        public static void UnloadContent()
        {
            if (generators != null)
            {
                foreach (RoadGenerator gen in generators)
                {
                    if (gen != null)
                    {
                        BokuGame.Unload(gen);
                    }
                }
                generators = null;
            }
            WayPoint.RebuildAll();
        }

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public static void DeviceReset(GraphicsDevice device)
        {
            foreach (RoadGenerator gen in generators)
            {
                BokuGame.DeviceReset(gen, device);
            }
        }

        #region Batching
        private List<RoadStdRenderObj> batch = new List<RoadStdRenderObj>();
        private void FlushBatch(Camera camera)
        {
            if (batch.Count > 0)
            {
                GraphicsDevice device = KoiLibrary.GraphicsDevice;

                Effect effect = RoadStdRenderObj.Effect;
                EffectTechnique technique = RoadStdRenderObj.Technique;
                if (technique != null)
                {
                    if (wireFrame)
                    {
                        device.RasterizerState = SharedX.RasterStateWireframe;
                    }

                    RoadStdRenderObj.Parameter(RoadStdRenderObj.EffectParams.WorldViewProjMatrix).SetValue(camera.ViewProjectionMatrix);
                    RoadStdRenderObj.Parameter(RoadStdRenderObj.EffectParams.WorldMatrix).SetValue(Matrix.Identity);

                    effect.CurrentTechnique = technique;

                    for (int iPass = 0; iPass < technique.Passes.Count; ++iPass)
                    {
                        EffectPass pass = technique.Passes[iPass];

                        pass.Apply();

                        foreach (RoadStdRenderObj obj in batch)
                        {
                            obj.RenderBatch(this);
                        }
                    }

                    if (wireFrame)
                    {
                        device.RasterizerState = RasterizerState.CullCounterClockwise;
                    }
                }
                batch.Clear();
            }
        }
        #endregion Batching

        #region Generators
        private static RoadGenerator[] generators = null;

        private static int kFirstRoad = 1;
        private static int kFirstWall = 4;
        private static int kFirstVeg = 9;
        private static int genIndex = 0;
        private static int lastRoadCreated = kFirstRoad;
        private static int lastWallCreated = kFirstWall;
        private static int lastVegCreated = kFirstVeg;

        private static void InitGenerators()
        {
            generators = new RoadGenerator[15];

            int i = 0;
            generators[i++] = null;
            generators[i++] = new HiWayGen();
            generators[i++] = new HiWayWide();
            generators[i++] = new HiWayChina();
            generators[i++] = new HiWallGen2();
            generators[i++] = new HiWallGen();
            // generators[i++] = new RoundWallGen();
            generators[i++] = new RoundWallGen2();
            generators[i++] = new RailFenceWay();
            generators[i++] = new RoboWall3();
            // generators[i++] = new CastleGen();
            generators[i++] = new VeggieGen2();
            generators[i++] = new VeggieGen2_B();
            generators[i++] = new VeggieGen2_C();
            generators[i++] = new VeggieGen2_D();
            generators[i++] = new VeggieGen();
            generators[i++] = new VeggieGen_C();

        }   // end of InitRoads()

        public static int LastRoadCreated
        {
            get { return lastRoadCreated; }
            private set { lastRoadCreated = value; }
        }
        public static int LastWallCreated
        {
            get { return lastWallCreated; }
            private set { lastWallCreated = value; }
        }
        public static int LastVegCreated
        {
            get { return lastVegCreated; }
            private set { lastVegCreated = value; }
        }
        public static void IncGen() { if (++genIndex >= generators.Length) { genIndex = 0; } }
        public static void DecGen() { if (--genIndex < 0) { genIndex = generators.Length - 1; } }
        public static void WrapGen(bool wasVeg)
        {
            if (wasVeg)
            {
                if (genIndex >= generators.Length)
                    genIndex = kFirstVeg;
                else if (genIndex < kFirstVeg)
                    genIndex = generators.Length - 1;
            }
            else
            {
                while (genIndex >= kFirstVeg)
                    genIndex -= kFirstVeg;
                while (genIndex < 0)
                    genIndex += kFirstVeg;
            }
            GenIndex = genIndex;
        }
        public static RoadGenerator CurrGen() { return generators[genIndex]; }

        public void ChangeGen() { Generator = CurrGen(); }

        public static int GenIndex
        {
            get
            {
                return genIndex;
            }
            set
            {
                Debug.Assert((value >= 0) && (value < generators.Length));
                genIndex = value;

                if ((genIndex >= kFirstRoad) && (genIndex < kFirstWall))
                    LastRoadCreated = genIndex;
                else if ((genIndex >= kFirstWall) && (genIndex < kFirstVeg))
                    LastWallCreated = genIndex;
                else if (genIndex >= kFirstVeg)
                    LastVegCreated = genIndex;
            }
        }
        public void AdvanceGen(int adv)
        {
            int index = FindGenIndex(Generator);

            bool wasVeg = index >= kFirstVeg;

            genIndex = index + adv;
            WrapGen(wasVeg);
            Generator = CurrGen();
        }

        public static RoadGenerator GenFromString(string name)
        {
            foreach (RoadGenerator gen in generators)
            {
                if ((gen != null) && (gen.ToString() == name))
                {
                    return gen;
                }
            }
            return null;
        }
        /// <summary>
        /// Find the index of the given generator.
        /// </summary>
        /// <returns></returns>
        private static int FindGenIndex(RoadGenerator gen)
        {
            for (int i = 0; i < generators.Length; ++i)
            {
                if (gen == generators[i])
                    return i;
            }
            /// If we didn't find it, fall back on the invisible plain path.
            return 0;
        }
        #endregion Generators

        #endregion Internal

        #region RenderObj
        /// <summary>
        /// The renderable version of a section or intersection
        /// </summary>
        public interface RenderObj
        {
            /// <summary>
            /// The bounding sphere for this geometry chunk.
            /// </summary>
            BoundingSphere Sphere
            {
                get;
            }

            /// <summary>
            /// Throw away anything big that needs explict pushing
            /// </summary>
            void Clear();

            /// <summary>
            /// Render yourself and all your stuff.
            /// </summary>
            /// <param name="camera"></param>
            void Render(Camera camera, Road road);

            void Finish(Section section);

            void Finish(Intersection isect);
        }
        #endregion RenderObj


    }

}
