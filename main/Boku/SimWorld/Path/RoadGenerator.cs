using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;

using Boku.Base;

namespace Boku.SimWorld.Path
{
    public abstract class RoadGenerator : INeedsDeviceReset
    {
        #region Members
        bool stretchUp = false;
        bool stretchUpEnd = false;
        #endregion Members
        #region Accessors
        /// <summary>
        /// Return the maximum width
        /// </summary>
        public virtual float MaxWidth
        {
            get { return 0.0f; }
        }

        /// <summary>
        /// Return the maximum height generated (relative to the terrain).
        /// </summary>
        public virtual float MaxHeight
        {
            get { return 0.0f; }
        }

        public virtual float MinHeight
        {
            get { return 0.0f; }
        }

        /// <summary>
        /// Maximum dimension
        /// </summary>
        public float MaxDim
        {
            get { return Math.Max(MaxWidth, MaxHeight); }
        }

        /// <summary>
        /// The width and height of our collision geometry
        /// </summary>
        /// <returns></returns>
        public virtual float CollWidth
        {
            get { return MaxWidth; }
        }
        /// <summary>
        /// Collision height along straight sections.
        /// </summary>
        public virtual float CollHeight
        {
            get { return MaxHeight; }
        }
        /// <summary>
        /// Collision low along straight sections.
        /// </summary>
        public virtual float CollBase
        {
            get { return MinHeight; }
        }
        /// <summary>
        ///  Collision height at intersections.
        /// </summary>
        public virtual float CollEndHeight // for the intersections
        {
            get { return CollHeight; }
        }
        /// <summary>
        /// Low height for collisions at intersections.
        /// </summary>
        public virtual float CollEndBase
        {
            get { return MinHeight; }
        }
        /// <summary>
        /// Collision radius at intersections.
        /// </summary>
        public virtual float CollRadius // for the intersections
        {
            get { return MaxWidth; }
        }
        /// <summary>
        /// The height at which a new node created for this generator starts.
        /// </summary>
        public virtual float DefaultNodeHeight
        {
            get { return 0.0f; }
        }
        /// <summary>
        /// Return true if the generator does slicing and stitching at intersections,
        /// or false if it just drops a geometry at each node.
        /// </summary>
        /// <returns></returns>
        public virtual bool Trims
        {
            get { return true; }
        }

        /// <summary>
        /// Return true if objects are elevated by this road, else false
        /// </summary>
        /// <returns></returns>
        public virtual bool MakesGround
        {
            get { return false; }
        }

        /// <summary>
        /// Return true if objects collide with this (ala a wall), else false (ala a road).
        /// </summary>
        /// <returns></returns>
        public virtual bool MakesBlocker
        {
            get { return false; }
        }

        /// <summary>
        /// Can things travel under this road section when it's elevated?
        /// </summary>
        public virtual bool PassUnder
        {
            get { return !StretchUp && (CollBase >= 0.0f); }
        }
        /// <summary>
        /// Can things travel under this road intersection when it's elevated?
        /// </summary>
        public virtual bool PassUnderEnd
        {
            get { return !StretchUpEnd && (CollEndBase >= 0.0f); }
        }

        /// <summary>
        /// Whether only base is planted or the profile is stretched rising from ground.
        /// </summary>
        public virtual bool StretchUp
        {
            get { return stretchUp; }
            protected set { stretchUp = value; }
        }

        /// <summary>
        /// Whether only base of intersection is planted or the profile is stretched rising from ground.
        /// </summary>
        public virtual bool StretchUpEnd
        {
            get { return stretchUpEnd; }
            protected set { stretchUpEnd = value; }
        }

        public virtual float CostPerMeter
        {
            get { return 0.01f; }
        }
        public virtual float CostPerNode
        {
            get { return 0.1f; }
        }
        #endregion Accessors

        #region Public
        /// <summary>
        /// Generate a straight section
        /// </summary>
        /// <param name="section">the section to hold the output</param>
        public abstract void NewSection(Road.Section section);

        /// <summary>
        /// Generate a new fan filling the sector defined by p0, p1, and pC as the center.
        /// Assumes |p0 - pC| == |p1 - pC| == radius == roadwidth/2, and that the fan goes from
        /// p0 to p1 counterclockwise.
        /// </summary>
        /// <param name="node">the shared node at the intersection</param>
        /// <param name="first">edge of start of the arc</param>
        /// <param name="second">edge of end of the arc</param>
        public abstract bool NewFan(
            Road.Intersection isect, 
            Road.Section first, 
            Road.Section second,
            List<Road.RenderObj> fans);

        /// <summary>
        /// If the position is on the road, compute the height of the road and return true.
        /// Else return false.
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="pos"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public virtual bool GetHeight(
            Vector3 p0, 
            Vector3 p1, 
            Vector3 pos, 
            ref float height)
        {
            return false;
        }

        /// <summary>
        /// If the position is on the road, compute the height of the road and return true.
        /// Else return false.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="pos"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public virtual bool GetHeight(
            Vector3 center, 
            Vector3 pos, 
            ref float height)
        {
            return false;
        }

        /// <summary>
        /// Some generators might need a chance to update every frame.
        /// </summary>
        public virtual void Update()
        {
        }

        #endregion Public

        /// <summary>
        ///  That should probably be the end of the RoadGenerator base class, the rest
        /// is common code shared (optionally) by the derived classes.
        /// </summary>
        #region Internal

        public struct RoadVertex : IVertexType
        {
            public Vector3 pos;
            public Vector3 norm;
            public Vector2 uv;
            public Color texSelect; // .a selects uv source, vert or horizontal

            static VertexDeclaration decl = null;
            // MAFROAD - eventually want to convert the texcoord Vector2 to HalfVector2,
            //          to get this back down to 32 bytes.
            static VertexElement[] elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(32, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                // Total == 36 bytes (and counting)
            };

            public static RoadVertex operator*(RoadVertex v, float f)
            {
                RoadVertex ret = v;
                ret.pos *= f;
                ret.norm *= f;
                ret.uv *= f;
                Vector4 texSel = ret.texSelect.ToVector4();
                texSel *= f;
                ret.texSelect = new Color(texSel);

                return ret;
            }
            public static RoadVertex operator+(RoadVertex v0, RoadVertex v1)
            {
                RoadVertex ret = v0;
                ret.pos += v1.pos;
                ret.norm += v1.norm;
                ret.uv += v1.uv;
                Vector4 v0TexSel = v0.texSelect.ToVector4();
                Vector4 v1TexSel = v1.texSelect.ToVector4();
                v0TexSel += v1TexSel;
                ret.texSelect = new Color(v0TexSel);

                return ret;
            }

            public VertexDeclaration VertexDeclaration
            {
                get
                {
                    if (decl == null || decl.IsDisposed)
                    {
                        decl = new VertexDeclaration(elements);
                    }
                    return decl;
                }
            }

        }


        #region TrimGeometry
        
        /// <summary>
        /// Trim all geometry for two sections by the bisecting plane (if appropriate)
        /// </summary>
        /// <param name="plane"></param>
        public virtual bool Trim(Road.Section first, Road.Section second)
        {
            RoadStdRenderObj roFirst = first.RenderObj as RoadStdRenderObj;
            RoadStdRenderObj roSecond = second.RenderObj as RoadStdRenderObj;
            if ((roFirst == null) || (roSecond == null))
            {
                return false;
            }


            WayPoint.Node node0 = null;
            WayPoint.Node node1 = null;
            WayPoint.Node nodeC = null;
            if (!FindBend(first, second, out nodeC, out node0, out node1))
            {
                return false;
            }

            Vector2 edge0 = Vector2.Normalize(node0.Position2d - nodeC.Position2d);
            Vector2 edge1 = Vector2.Normalize(node1.Position2d - nodeC.Position2d);
            float dot = Vector2.Dot(edge0, edge1);
            {
                float cross = edge0.X * edge1.Y - edge0.Y * edge1.X;
                double rads = Math.Atan2(cross, dot);
                if (rads > 0.0)
                {
                    Vector2 bisect = dot > -0.999f
                        ? Vector2.Normalize(edge0 + edge1)
                        : new Vector2(-edge0.Y, edge0.X);

                    Vector3 norm = new Vector3(-bisect.Y, bisect.X, 0.0f);
                    float dist = Vector3.Dot(norm, nodeC.Position);
                    Vector4 plane = new Vector4(norm, dist);

                    TrimToPlane(-plane, roFirst);
                    TrimToPlane(plane, roSecond);

                    return true;
                }
            }

            return false;
        }

        static protected void TrimToPlane(Vector4 plane, RoadStdRenderObj ro)
        {
            List<Int16> idxIn = new List<Int16>(ro.Indices);
            List<Int16> idxOut = new List<Int16>(ro.Indices.Length);
            List<RoadVertex> verts = new List<RoadVertex>(ro.Verts);
            List<Vector4> planes = new List<Vector4>(1);
            planes.Add(plane);
            TrimIndexedTriList(planes, idxIn, idxOut, verts);
            ro.Indices = new Int16[idxOut.Count];
            idxOut.CopyTo(ro.Indices);
            ro.Verts = new RoadVertex[verts.Count];
            verts.CopyTo(ro.Verts);
            ro.DirtyBuffers();
        }

        /// <summary>
        /// Generate a list of indices for the input geometry trimmed to the exterior
        /// of the planes in the list.
        /// This is a convex trim, for a concave trim, rather than trimming the output
        /// of one plane, you would trim the rejects of the previous trim.
        /// Convex trim:
        ///     slice geometry at plane[0] boundary
        ///     discard geometry on negative side of plane[0]
        ///     slice positive side geometry at plane[1] boundary
        ///     discard geometry on negative side of plane[1]
        ///     [repeat for rest of planes]
        /// Concave trim:
        ///     slice geometry at plane[0] boundary
        ///     accept geometry on positive side of plane[0]
        ///     slice negative side geometry at plane[1] boundary
        ///     accept positive side geometry of plane[1]
        ///     slice negative side geometry at plane[2] boundary
        ///     accept positive side geometry of plane[2]
        ///     [repeat for rest of planes]
        /// </summary>
        /// <param name="planes"></param>
        /// <param name="idxIn"></param>
        /// <param name="idxOut"></param>
        /// <param name="vtxData"></param>
        /// <returns></returns>
        static public bool TrimIndexedTriList(List<Vector4> planes, 
            List<Int16> idxIn,
            List<Int16> idxOut,
            List<RoadVertex> vtxData)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            UInt32 numIndicesIn = (UInt32)idxIn.Count;
            UInt32 numTrisIn = numIndicesIn / 3; // Hardcoding for IndexElemetnSize.SixteenBits / Int16
            Debug.Assert(numTrisIn * 3 == numIndicesIn);
            idxOut.Clear();

            UInt32 numVertsIn = (UInt32)vtxData.Count;

            List<Vector3> baryIn = new List<Vector3>(3);
            List<Vector3> baryOut = new List<Vector3>(3);

            bool anyTrimmed = false;
            // for each plane
            foreach (Vector4 plane in planes)
            {
                // for each triangle
                for (int iTri = 0; iTri < numTrisIn; iTri++)
                {
                    Int16 idx0 = idxIn[iTri * 3 + 0];
                    Int16 idx1 = idxIn[iTri * 3 + 1];
                    Int16 idx2 = idxIn[iTri * 3 + 2];

                    RoadVertex vtx0 = vtxData[idx0];
                    RoadVertex vtx1 = vtxData[idx1];
                    RoadVertex vtx2 = vtxData[idx2];

                    baryIn.Clear();
                    baryIn.Add(new Vector3(1.0f, 0.0f, 0.0f));
                    baryIn.Add(new Vector3(0.0f, 1.0f, 0.0f));
                    baryIn.Add(new Vector3(0.0f, 0.0f, 1.0f));
                    
                    baryOut.Clear();

                    // Run the triangle through the trimmer
                    bool trimmed = Trim(vtx0.pos, vtx1.pos, vtx2.pos, baryIn, baryOut, plane);
                    // if the triangle wasn't trimmed
                    anyTrimmed = anyTrimmed || trimmed;
                    if (!trimmed)
                    {
                        // Copy the indices as is into the output list
                        idxOut.Add(idx0);
                        idxOut.Add(idx1);
                        idxOut.Add(idx2);
                    }
                    else if (baryOut.Count > 2)
                    {
                        // We've been trimmed, but we still have non-zero count of
                        // vertices, which means we contain new vertices.

                        // We could get clever here and tell which vertices are new
                        // and which have been generated, or we can be lazy and just
                        // assume all are new.

                        // In either case, we append any new vertices to the end of
                        // the vertex list, and add the new indices to the output list


                        Int16 idxPivot = AddVertex(vtxData, baryOut[0], idx0, idx1, idx2);

                        Int16 idxLast = AddVertex(vtxData, baryOut[1], idx0, idx1, idx2);

                        Int16 idxNext = AddVertex(vtxData, baryOut[2], idx0, idx1, idx2);

                        idxOut.Add(idxPivot);
                        idxOut.Add(idxLast);
                        idxOut.Add(idxNext);

                        for (int iBary = 3; iBary < baryOut.Count; ++iBary)
                        {
                            idxLast = idxNext;
                            idxNext = AddVertex(vtxData, baryOut[iBary], idx0, idx1, idx2);

                            idxOut.Add(idxPivot);
                            idxOut.Add(idxLast);
                            idxOut.Add(idxNext);
                        }

                        anyTrimmed = true;
                    }
                    // else the whole triangle was culled

                }
            }
            return anyTrimmed;
        }

        /// <summary>
        /// Trim a single polygon to the positive side of a plane.
        /// Input polygon is expressed as a list of barycentric coordinates.
        /// Input is assumed to be convex.
        /// Output is just the barycentric coordinates of the final list,
        /// and may have up to one more points than Input, or up to all of them
        /// may be culled out.
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="bary"></param>
        /// <param name="pPln"></param>
        /// <param name="nPln"></param>
        static public bool Trim(Vector3 p0, Vector3 p1, Vector3 p2,
            List<Vector3> baryIn, List<Vector3> baryOut,
            Vector4 plane)
        {
            bool trimmed = false;
            Vector3 plnNorm = new Vector3(plane.X, plane.Y, plane.Z);
            float slop = -0.0f;
            float plnDist = plane.W + slop;
            // for each point in list
            for (int iIn = 0; iIn < baryIn.Count; ++iIn)
            {
                Vector3 baryThis = baryIn[iIn];
                Vector3 posThis = p0 * baryThis.X
                                + p1 * baryThis.Y
                                + p2 * baryThis.Z;

                // If this one is in
                float distThis = Vector3.Dot(posThis, plnNorm) - plnDist;
                if (distThis >= 0.0f)
                {
                    // Add to outlist
                    baryOut.Add(baryThis);
                }
                else
                {
                    trimmed = true;
                    // If previous is in
                    Vector3 baryPrev = iIn > 0 ? baryIn[iIn - 1] : baryIn[baryIn.Count - 1];
                    Vector3 posPrev = p0 * baryPrev.X
                                    + p1 * baryPrev.Y
                                    + p2 * baryPrev.Z;
                    float distPrev = Vector3.Dot(posPrev, plnNorm) - plnDist;
                    if (distPrev > 0.0f)
                    {
                        // Add interp to plane(prev, this)
                        float interp = -distPrev / (distThis - distPrev);
                        Vector3 baryInterp = baryPrev + (baryThis - baryPrev) * interp;
                        baryOut.Add(baryInterp);
                    }

                    // If next is in
                    Vector3 baryNext = iIn < baryIn.Count - 1 ? baryIn[iIn + 1] : baryIn[0];
                    Vector3 posNext = p0 * baryNext.X
                                    + p1 * baryNext.Y
                                    + p2 * baryNext.Z;
                    float distNext = Vector3.Dot(posNext, plnNorm) - plnDist;
                    if (distNext > 0.0f)
                    {
                        // Add interp to plane(next, this)
                        float interp = -distNext / (distThis - distNext);
                        Vector3 baryInterp = baryNext + (baryThis - baryNext) * interp;
                        baryOut.Add(baryInterp);
                    }
                }
            }
            return trimmed;
        }

        /// <summary>
        /// Generate (if necessary) an interpolated vertex, returning the index
        /// for the vertex based on this barycentric coordinate
        /// </summary>
        /// <param name="vtxData"></param>
        /// <param name="bary"></param>
        /// <param name="idx0"></param>
        /// <param name="idx1"></param>
        /// <param name="idx2"></param>
        /// <returns></returns>
        static protected Int16 AddVertex(List<RoadVertex> vtxData,
                                Vector3 bary,
                                Int16 idx0,
                                Int16 idx1,
                                Int16 idx2)
        {
            if ((bary.X == 1.0f)
                && (bary.Y == 0.0f)
                && (bary.Z == 0.0f))
            {
                // original first vert
                return idx0;
            }
            else if ((bary.X == 0.0f)
                && (bary.Y == 1.0f)
                && (bary.Z == 0.0f))
            {
                // original second vert
                return idx1;
            }
            else if ((bary.X == 0.0f)
                   && (bary.Y == 0.0f)
                   && (bary.Z == 1.0f))
            {
                // original third vert
                return idx2;
            }
            RoadVertex vtx0 = vtxData[idx0];
            RoadVertex vtx1 = vtxData[idx1];
            RoadVertex vtx2 = vtxData[idx2];

            RoadVertex newVtx = vtx0 * bary.X
                                + vtx1 * bary.Y
                                + vtx2 * bary.Z;

            vtxData.Add(newVtx);
            return (Int16)(vtxData.Count - 1);
        }

        #endregion TrimGeometry

        #region Utility
        
        /// <summary>
        /// Find the node shared by two sections and the unshared nodes
        /// </summary>
        /// <param name="first">First section</param>
        /// <param name="second">Second section</param>
        /// <param name="node0">Unshared node from first</param>
        /// <param name="node1">Unshared node from second</param>
        /// <param name="nodeC">Shared node</param>
        /// <returns>Return true if they do share a node</returns>
        static protected bool FindBend(Road.Section first, Road.Section second,
                                out WayPoint.Node nodeC,
                                out WayPoint.Node node0,
                                out WayPoint.Node node1)
        {
            // Find the common and end nodes
            if (first.Edge.Node0 == second.Edge.Node0)
            {
                nodeC = first.Edge.Node0;
                node0 = first.Edge.Node1;
                node1 = second.Edge.Node1;
            }
            else if (first.Edge.Node0 == second.Edge.Node1)
            {
                nodeC = first.Edge.Node0;
                node0 = first.Edge.Node1;
                node1 = second.Edge.Node0;
            }
            else if (first.Edge.Node1 == second.Edge.Node0)
            {
                nodeC = first.Edge.Node1;
                node0 = first.Edge.Node0;
                node1 = second.Edge.Node1;
            }
            else if (first.Edge.Node1 == second.Edge.Node1)
            {
                nodeC = first.Edge.Node1;
                node0 = first.Edge.Node0;
                node1 = second.Edge.Node0;
            }
            else
            {
                node0 = null;
                node1 = null;
                nodeC = null;

                // Might assert, since these two edges don't share a node.
                // But not asserting lets this be used to check whether
                // two sections share a node.
                return false;
            }
            return true;
        }

        static protected bool FindBend(Road.Section first, Road.Section second,
                                        WayPoint.Node nodeC,
                                        out WayPoint.Node node0,
                                        out WayPoint.Node node1)
        {
            node0 = null;
            node1 = null;
            if (first.Edge.Node0 == nodeC)
            {
                node0 = first.Edge.Node1;
            }
            else if (first.Edge.Node1 == nodeC)
            {
                node0 = first.Edge.Node0;
            }
            if (second.Edge.Node0 == nodeC)
            {
                node1 = second.Edge.Node1;
            }
            else if (second.Edge.Node1 == nodeC)
            {
                node1 = second.Edge.Node0;
            }
            return (node0 != null) && (node1 != null);
        }

        protected float DistanceSqToSection(Vector3 p0_3d, Vector3 p1_3d, Vector3 pos_3d)
        {
            Vector2 p0 = new Vector2(p0_3d.X, p0_3d.Y);
            Vector2 p1 = new Vector2(p1_3d.X, p1_3d.Y);
            Vector2 pos = new Vector2(pos_3d.X, pos_3d.Y);
            Vector2 axis = p1 - p0;
            float invAxLenSq = 1.0f / axis.LengthSquared();

            Vector2 smoothPoint;
            float dot = Vector2.Dot(pos - p0, axis) * invAxLenSq;
            /// This gives a nice naturally rounded caps, but sadly that's
            /// not what we want. We want to know if we're off either end,
            /// so we can back out and let the intersection take over.
            //if (dot <= 0.0f)
            //{
            //    // Off start end, do distance to start
            //    smoothPoint = p0;
            //}
            //else if (dot >= 1.0f)
            //{
            //    // Off end end, do distance to end

            //    smoothPoint = p1;
            //}
            //else
            if ((dot >= 0.0f) && (dot <= 1.0f))
            {
                // Normal midrange, do distance to segment
                smoothPoint = p0 + dot * axis;
                return Vector2.DistanceSquared(pos, smoothPoint);
            }
            return -1.0f;
        }
        #endregion Utility

        public virtual void LoadContent(bool immediate) { }
        public virtual void InitDeviceResources(GraphicsDevice device) { }
        public virtual void UnloadContent() { }
        public virtual void DeviceReset(GraphicsDevice device) { }

        #endregion Internal
    }

}
