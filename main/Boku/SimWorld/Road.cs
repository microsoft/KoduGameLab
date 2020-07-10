// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Common;
using Boku.Base;

namespace Boku.SimWorld
{
    /// <summary>
    /// The visual representation for a path
    /// </summary>
    public class Road 
    {
        private float width = 3.0f;
        private float height = 1.0f;
        private float step = 1.0f;
        private double radStep = Math.PI / 12.0;
        private bool needsRebuild = true;

        private RoadGenerator generator;
        private WayPoint.Path path = null;
        private List<Section> sections = new List<Section>();
        private List<Intersection> intersections = new List<Intersection>();

        static private RoadGenerator[] generators = 
            { 
                null,
                new HiWallGen(),
                new HiWayGen(),
                new CastleGen()
            };
        static private int genIndex = 0;
        static public void NextGen() { if (++genIndex >= generators.Length) { genIndex = 0; } }
        static public void PrevGen() { if (--genIndex < 0) { genIndex = generators.Length - 1; } }
        static public RoadGenerator CurrGen() { return generators[genIndex]; }
        public void ChangeGen() { Generator = CurrGen(); }

        #region Accessors
        public bool NeedsRebuild
        {
            get { return needsRebuild; }
            set { needsRebuild = value; }
        }
        public float Width
        {
            get { return width; }
            set { width = value; }
        }
        public float Height
        {
            get { return height; }
            set { height = value; }
        }
        public float Step
        {
            get { return step; }
            set { step = value; }
        }
        public double RadStep
        {
            get { return radStep; }
            set { radStep = value; }
        }
        public WayPoint.Path Path
        {
            get { return path; }
        }
        public RoadGenerator Generator
        {
            get { return generator; }
            set { generator = value; NeedsRebuild = true; }
        }
        public List<Section> Sections
        {
            get { return sections; }
        }
        public List<Intersection> Intersections
        {
            get { return intersections; }
        }
        #endregion

        public Road(RoadGenerator gen, WayPoint.Path inPath)
        {
            generator = generators[genIndex];
            path = inPath;
        }

        public void Render(Camera camera)
        {
            CheckRebuild();

            foreach (Section section in sections)
            {
                section.Render(camera);
            }
            foreach (Intersection isect in intersections)
            {
                isect.Render(camera);
            }
        }

        public void Build()
        {
            if (Generator != null)
            {
                foreach (WayPoint.Edge edge in path.Edges)
                {
                    Section section = new Section();

                    if (section.Connect(this, edge))
                    {
                        sections.Add(section);
                    }
                }

                foreach (WayPoint.Node node in path.Nodes)
                {
                    Intersection isect = new Intersection();

                    if (isect.Connect(this, node))
                    {
                        intersections.Add(isect);
                    }
                }
            }
        }

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
            NeedsRebuild = true;
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
                NeedsRebuild = false;
            }
        }

        /// <summary>
        /// Flatten the terrain under the path.
        /// </summary>
        public void Flatten()
        {
            foreach (Section section in Sections)
            {
                Flatten(section.Edge.Node0.Position2d, section.Edge.Node1.Position2d);
            }
        }

        /// <summary>
        /// The renderable version of a section or intersection
        /// </summary>
        public interface RenderObj
        {
            /// <summary>
            /// Throw away anything big that needs explict pushing
            /// </summary>
            void Clear();

            /// <summary>
            /// Render yourself and all your stuff.
            /// </summary>
            /// <param name="camera"></param>
            void Render(Camera camera, Road road);
        }

        /// <summary>
        /// The visual representation for a section of a path between two nodes
        /// </summary>
        public class Section
        {
            protected Road road;
            protected WayPoint.Edge edge;
            protected RenderObj renderObj;

            #region Accessors
            public Road Road
            {
                get { return road; }
            }
            public WayPoint.Edge Edge
            {
                get { return edge; }
            }
            public RenderObj RenderObj
            {
                get { return renderObj; }
                set { renderObj = value; }
            }
            #endregion

            /// <summary>
            /// Render this section of road/wall
            /// </summary>
            /// <param name="camera"></param>
            public void Render(Camera camera)
            {
                if (RenderObj != null)
                {
                    RenderObj.Render(camera, Road);
                }
            }

            /// <summary>
            /// Free up anything that needs explicit freeing (device stuff)
            /// </summary>
            public void Clear()
            {
                renderObj.Clear();
            }

            /// <summary>
            /// Build section of geometry between the two nodes
            /// </summary>
            /// <param name="n0"></param>
            /// <param name="n1"></param>
            public bool Connect(Road inRoad, WayPoint.Edge inEdge)
            {
                road = inRoad;
                edge = inEdge;

                road.Generator.NewSection(this);

                return renderObj != null;
            }

            public double EdgeAngle(WayPoint.Node center)
            {
                Vector2 fromCenter = Vector2.Normalize(Edge.Node0.Position2d - Edge.Node1.Position2d);
                if (center == Edge.Node0)
                {
                    fromCenter = -fromCenter;
                }
                double rads = Math.Atan2(fromCenter.Y, fromCenter.X);
                return rads;
            }
        }

        /// <summary>
        /// The joint between 2 or more edges within a path, at a node
        /// </summary>
        public class Intersection
        {
            protected Road road;
            protected WayPoint.Node node;
            protected List<Section> sections = new List<Section>();
            protected List<RenderObj> fans = new List<RenderObj>();

            #region Accessors
            public Road Road
            {
                get { return road; }
            }
            public WayPoint.Node Node
            {
                get { return node; }
            }
            public List<RenderObj> Fans
            {
                get { return fans; }
            }
            #endregion

            /// <summary>
            /// Render this intersection of road/wall
            /// </summary>
            /// <param name="camera"></param>
            public void Render(Camera camera)
            {
                foreach (RenderObj fan in Fans)
                {
                    fan.Render(camera, Road);
                }
            }

            /// <summary>
            /// Free up anything that needs explicit freeing (device stuff)
            /// </summary>
            public void Clear()
            {
                foreach (RenderObj fan in Fans)
                {
                    fan.Clear();
                }
            }

            public class EdgeCompare : IComparer<Section>
            {
                WayPoint.Node center;
                public EdgeCompare(WayPoint.Node node)
                {
                    center = node;
                }
                public int Compare(Section left, Section right)
                {
                    if (left == null)
                    {
                        return right == null ? 0 : -1;
                    }
                    if (right == null)
                    {
                        return 1;
                    }
                    double rads0 = left.EdgeAngle(center);
                    double rads1 = right.EdgeAngle(center);
                    return rads0 < rads1
                        ? -1
                        : rads0 > rads1
                            ? 1
                            : 0;

                }
            }

            public bool Connect(Road inRoad, WayPoint.Node inNode)
            {
                road = inRoad;
                node = inNode;
                // Make a list of all edges hitting this node. This should
                // probably already be stored on the node.
                sections.Clear();
                foreach (Section section in Road.Sections)
                {
                    WayPoint.Edge edge = section.Edge;
                    if ((edge.Node0 == node) || (edge.Node1 == node))
                    {
                        sections.Add(section);
                    }
                }

                if (sections.Count > 1)
                {
                    // Sort them by angle
                    sections.Sort(new EdgeCompare(node));

                    for (int first = 0; first < sections.Count; ++first)
                    {
                        int second = first + 1;
                        if (second >= sections.Count)
                        {
                            second = 0;
                        }
                        // Trim away any overlap between these two sections.
                        if (!Road.Generator.Trim(sections[first], sections[second]))
                        {
                            // If the angle between these was too big to cause a trim
                            // then we need to insert a fan.
                            MakeFan(node, sections[first], sections[second]);
                        }
                    }

                }
                else if (sections.Count == 1)
                {
                    // Make an end cap
                    MakeFan(node, sections[0], sections[0]);
                }

                return Fans.Count > 0;
            }

            /// <summary>
            /// Generate a filler arc of a circle fan, with center at pC,
            /// and the arc going from p0 to p1, counterclockwise.
            /// </summary>
            /// <param name="p0"></param>
            /// <param name="p1"></param>
            /// <param name="pC"></param>
            protected void MakeFan(WayPoint.Node node, Section first, Section second)
            {
                RenderObj ro = Road.Generator.NewFan(node, first, second);
                if (ro != null)
                {
                    fans.Add(ro);
                }
            }
        }

        protected Rectangle SmoothBox(Vector2 start, Vector2 end)
        {
            Terrain terrain = InGame.inGame.Terrain;
            HeightMap heightMap = terrain.HeightMap;

            start -= terrain.Min;
            end -= terrain.Min;

            Vector2 posMin;
            posMin.X = Math.Min(start.X, end.X);
            posMin.Y = Math.Min(start.Y, end.Y);

            Vector2 posMax;
            posMax.X = Math.Max(start.X, end.X);
            posMax.Y = Math.Max(start.Y, end.Y);

            Vector2 smoothWidth = Generator.SmoothWidth();
            posMin.X -= smoothWidth.Y;
            posMin.Y -= smoothWidth.Y;
            posMax.X += smoothWidth.Y;
            posMax.Y += smoothWidth.Y;

            posMin /= (terrain.Max - terrain.Min);
            posMax /= (terrain.Max - terrain.Min);

            // Convert these to height map numbers.  Add 1 to the max values since
            // these will be used as loop limits.
            Point min = new Point((int)(posMin.X * heightMap.Size.X), (int)(posMin.Y * heightMap.Size.Y));
            Point max = new Point(1 + (int)(posMax.X * heightMap.Size.X), 1 + (int)(posMax.Y * heightMap.Size.Y));
            // Clamp to valid limits.
            min.X = Math.Max(min.X, 0);
            min.Y = Math.Max(min.Y, 0);
            max.X = Math.Min(max.X, heightMap.Size.X);
            max.Y = Math.Min(max.Y, heightMap.Size.Y);

            Rectangle ret;
            ret.X = min.X;
            ret.Y = min.Y;
            ret.Width = max.X - min.X + 1;
            ret.Height = max.Y - min.Y + 1;

            return ret;
        }

        protected Vector2 TerrainPosFromCoord(int i, int j)
        {
            HeightMap heightMap = InGame.inGame.Terrain.HeightMap;

            return new Vector2(i * heightMap.Scale.X / (heightMap.Size.X - 1),
                    j * heightMap.Scale.Y / (heightMap.Size.Y - 1));            
        }

        protected void Flatten(Vector2 start, Vector2 end)
        {
            // Prep up some constants
            Vector2 axis = end - start;
            float invAxLenSq = 1.0f / axis.LengthSquared();

            Vector2 smoothWidth = Generator.SmoothWidth();
            float smoothNorm = smoothWidth.Y > smoothWidth.X 
                                ? 1.0f / (smoothWidth.Y - smoothWidth.X)
                                : 1.0f;

            float rate = 0.005f;

            Terrain terrain = InGame.inGame.Terrain;
            HeightMap heightMap = terrain.HeightMap;

            // Find 2D bounding box that we'll be flattening
            Rectangle box = SmoothBox(start, end);
            Point min = new Point(box.X, box.Y);
            Point max = new Point(box.X + box.Width - 1, box.Y + box.Height - 1);

            // Foreach terrain vertex in the box
            for (int iy = min.Y; iy <= max.Y; ++iy)
            {
                for (int ix = min.X; ix <= max.X; ++ix)
                {

                    // Classify the vertex
                    Vector2 vtxPos = TerrainPosFromCoord(ix, iy);
                    float height = heightMap.GetHeight(ix, iy);

                    Vector2 smoothPoint;
                    float dot = Vector2.Dot(vtxPos - start, axis) * invAxLenSq;
                    if (dot <= 0.0f)
                    {
                        // Off start end, do distance to start
                        smoothPoint = start;
                    }
                    else if (dot >= 1.0f)
                    {
                        // Off end end, do distance to end

                        smoothPoint = end;
                    }
                    else
                    {
                        // Normal midrange, do distance to segment
                        smoothPoint = start + dot * axis;
                    }
                    float dist = Vector2.Distance(vtxPos, smoothPoint);
                    float smoothness = (smoothWidth.Y - dist) * smoothNorm;
                    if (smoothness > 0.0f)
                    {
                        smoothness *= rate;
                        if (smoothness > 1.0f)
                            smoothness = 1.0f;

                        float smoothHeight = heightMap.GetHeight(smoothPoint);
                        height = height + (smoothHeight - height) * smoothness;

                        heightMap.SetHeight(ix, iy, height);
                    }
                }
            }

            terrain.RefreshFromHeightMap(min, max);
    
        }
    }

}
