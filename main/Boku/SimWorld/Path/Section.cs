
#define MF_AIRBORNE_THRU

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Microsoft.Xna.Framework;

using Boku.Common;
using Boku.Base;
using Boku.SimWorld.Terra;

namespace Boku.SimWorld.Path
{
    public partial class Road
    {
        /// <summary>
        /// The visual representation for a section of a path between two nodes
        /// </summary>
        public class Section
        {
            #region Members
            protected Road road;
            protected WayPoint.Edge edge;
            protected Intersection isect0;
            protected Intersection isect1;
            protected RenderObj renderObj;
            protected BoundingSphere sphere;
            protected float edgeLength;
            protected Matrix worldToSection;
            protected Matrix sectionToWorld;
            protected bool abstain = false;
            private float cost = 0.0f;
            #endregion Members

            #region Accessors
            /// <summary>
            /// Owning road
            /// </summary>
            public Road Road
            {
                get { return road; }
            }
            /// <summary>
            /// Edge this section visually represents
            /// </summary>
            public WayPoint.Edge Edge
            {
                get { return edge; }
            }
            /// <summary>
            /// Renderable version of this section.
            /// </summary>
            public RenderObj RenderObj
            {
                get { return renderObj; }
                set { renderObj = value; }
            }
            /// <summary>
            /// Bounding sphere
            /// </summary>
            public BoundingSphere Sphere
            {
                get { return sphere; }
            }
            /// <summary>
            /// Whether this section is airborne or hugs ground.
            /// </summary>
            public bool Airborne
            {
                get { return Edge.Node0.Airborne || Edge.Node1.Airborne; }
            }
            /// <summary>
            /// Temp state used in ray walking.
            /// </summary>
            public bool Abstain
            {
                get { return abstain; }
                set { abstain = value; }
            }

            /// <summary>
            /// Return an estimate of the cost of this section of road.
            /// </summary>
            public float Cost
            {
                get { return cost; }
                private set { cost = value; }
            }
            #endregion

            #region Public
            /// <summary>
            /// If the position is on the section, and the section
            /// IsGround, compute the height there, and if the height
            /// is greater than the input height, return true.
            /// Otherwise return false and leave height unmodified.
            /// </summary>
            /// <param name="pos"></param>
            /// <param name="height"></param>
            /// <returns></returns>
            public bool GetHeight(Vector3 pos, ref float height)
            {
                if (Road.Generator.GetHeight(
                    Edge.Node0.Position,
                    Edge.Node1.Position,
                    pos,
                    ref height))
                {
                    const float kTolerance = 0.001f;
                    float baseHeight = BaseHeight(pos);
                    if (height >= 0.0f)
                    {
                        if (Road.Generator.PassUnder && (pos.Z + kTolerance <= baseHeight))
                        {
                            return false;
                        }
                    }
                    height += baseHeight;
                    return true;
                }
                return false;
            }

            /// <summary>
            /// If position is over the road, return true and set height and normal,
            /// else false.
            /// </summary>
            /// <param name="pos3d"></param>
            /// <param name="height"></param>
            /// <param name="normal"></param>
            /// <returns></returns>
            public bool GetHeightAndNormal(Vector3 pos3d, ref float height, ref Vector3 normal)
            {
                if (GetHeight(pos3d, ref height))
                {
                    normal = GetNormal(new Vector2(pos3d.X, pos3d.Y));
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Add a collision info to collisions if the input sphere intersects this section.
            /// </summary>
            /// <param name="center"></param>
            /// <param name="radius"></param>
            /// <param name="collisions"></param>
            public void GetCollisions(Vector3 center, float radius, List<CollisionInfo> collisions)
            {
                // Transform the center to our space
                Vector2 cen2 = new Vector2(center.X, center.Y);
                Vector2 cen2sec = Vector2.Transform(cen2, worldToSection);

                float collWidth = Road.Generator.CollWidth;

                if ((Math.Abs(cen2sec.X) - radius < edgeLength * 0.5f + collWidth)
                    && (Math.Abs(cen2sec.Y) - radius < collWidth))
                {
                    float baseHeight = BaseHeight(cen2);
                    bool hit = (center.Z - radius < baseHeight + Road.Generator.CollHeight);
                    if (hit && Road.Generator.PassUnder)
                    {
                        hit = center.Z + radius > baseHeight + Road.Generator.CollBase;
                    }
                    if (hit)
                    {
                        CollisionInfo info = new CollisionInfo();
                        if (cen2sec.X < -edgeLength * 0.5f)
                        {
                            info.norm.X = center.X - edge.Node0.Position2d.X;
                            info.norm.Y = center.Y - edge.Node0.Position2d.Y;
                            info.norm.Z = 0.0f;

                            float distance = Vector2.Distance(cen2, edge.Node0.Position2d)
                                            - collWidth - radius;
                            info.depth = -distance;
                        }
                        else if (cen2sec.X > edgeLength * 0.5f)
                        {
                            info.norm.X = center.X - edge.Node1.Position2d.X;
                            info.norm.Y = center.Y - edge.Node1.Position2d.Y;
                            info.norm.Z = 0.0f;

                            float distance = Vector2.Distance(cen2, edge.Node1.Position2d)
                                                            - collWidth - radius;
                            info.depth = -distance;
                        }
                        else if (cen2sec.Y > 0.0f)
                        {
                            info.norm.X = sectionToWorld.M21;
                            info.norm.Y = sectionToWorld.M22;
                            info.norm.Z = 0.0f;

                            info.depth = radius + collWidth - cen2sec.Y;
                        }
                        else
                        {
                            info.norm.X = -sectionToWorld.M21;
                            info.norm.Y = -sectionToWorld.M22;
                            info.norm.Z = 0.0f;

                            // minus negative 
                            info.depth = radius + collWidth + cen2sec.Y;
                        }
                        info.depth = Math.Max(info.depth, 0.0f);
                        if (info.depth > 0.0f)
                        {
                            info.norm.Normalize();
                            Debug.Assert(!float.IsNaN(info.norm.X));
                            Debug.Assert(!float.IsNaN(info.norm.Y));
                            Debug.Assert(!float.IsNaN(info.norm.Z));
                            Debug.Assert(!float.IsNaN(info.depth));
                            collisions.Add(info);
                        }
                    }
                }
            }

            /// Basic idea here is we'll transform the problem into the space where
            /// the section of road is a rectangle, so all tests become trivial because
            /// it's an axis aligned problem.
            /// <summary>
            /// 
            /// </summary>
            /// <param name="src"></param>
            /// <param name="dst"></param>
            /// <param name="hitBlock"></param>
            /// <returns></returns>
            public bool Blocked(Vector3 src, Vector3 dst, ref Terrain.HitBlock hitBlock)
            {
                Vector2 src2 = new Vector2(src.X, src.Y);
                Vector2 src2sec = Vector2.Transform(src2, worldToSection);
                Vector2 dst2 = new Vector2(dst.X, dst.Y);
                Vector2 dst2sec = Vector2.Transform(dst2, worldToSection);

                float collWidth = Road.Generator.CollWidth;

                if (InsideRect(src2sec))
                {
                    hitBlock.CrossesPath = true;
                    /// src point is on the path.
                    float baseHeight = BaseHeight(src2);

                    Vector3 degNormal = GetNormal(src2);
                    float degCollisionHeight = baseHeight + Road.Generator.CollHeight;
                    float degCollisionBase = baseHeight + Road.Generator.CollBase;
                    float degHeightDist = Vector3.Dot(degNormal, new Vector3(src2.X, src2.Y, degCollisionHeight));
                    float degBaseDist = Vector3.Dot(degNormal, new Vector3(src2.X, src2.Y, degCollisionBase));

                    float startDist = Vector3.Dot(src, degNormal);
                    float endDist = Vector3.Dot(dst, degNormal);

                    if ((startDist > degHeightDist) && (endDist < degHeightDist))
                    {
                        float t = (startDist - degHeightDist) / (startDist - endDist);
                        Vector2 hit2sec = src2sec + t * (dst2sec - src2sec);
                        if (InsideRect(hit2sec))
                        {
                            hitBlock.Position = src + t * (dst - src);
                            hitBlock.Normal = degNormal;
                            hitBlock.Min = Math.Min(src.Z, degCollisionHeight);
                            hitBlock.Max = Math.Max(src.Z, degCollisionHeight);
                            hitBlock.BlockHeight = degCollisionHeight;
                            return true;
                        }
                    }
                    if (Road.Generator.PassUnder)
                    {
                        if ((startDist < degBaseDist) && (endDist > degBaseDist))
                        {
                            float t = (startDist - degBaseDist) / (startDist - endDist);
                            Vector2 hit2sec = src2sec + t * (dst2sec - src2sec);
                            if (InsideRect(hit2sec))
                            {
                                hitBlock.Position = src + t * (dst - src);
                                hitBlock.Normal = -degNormal;
                                hitBlock.Min = Math.Min(src.Z, degBaseDist);
                                hitBlock.Max = Math.Max(src.Z, degBaseDist);
                                hitBlock.BlockHeight = degCollisionHeight;
                                return true;
                            }
                        }
                    }
                }

                /// For the height of the wall, we'll appoximate it with the height
                /// at the point where the ray from src to dst crosses the wall's center.
                /// If src and dst are both on the same side of the wall, we'll approximate
                /// with the center point between src and dst.
                Vector2 cen2sec = CenterPoint(src2sec, dst2sec);
                Vector2 cen2 = Vector2.Transform(cen2sec, sectionToWorld);
                float cenBaseHeight = BaseHeight(cen2);
                Vector3 cenNormal = GetNormal(cen2);
                float cenCollisionHeight = cenBaseHeight + Road.Generator.CollHeight;
                float cenCollisionBase = cenBaseHeight + Road.Generator.CollBase;
                float cenHeightDist = Vector3.Dot(cenNormal, new Vector3(cen2.X, cen2.Y, cenCollisionHeight));
                float cenBaseDist = Vector3.Dot(cenNormal, new Vector3(cen2.X, cen2.Y, cenCollisionBase));

                /// Three interesting cases:
                /// a) the ray pierces the top of the box.
                /// b) the ray pierces the bottom of the box (with PassUnder)
                /// c) the ray pierces the side of the box.
                float srcDist = Vector3.Dot(src, cenNormal);
                float dstDist = Vector3.Dot(dst, cenNormal);
                if((srcDist > cenHeightDist) && (dstDist < cenHeightDist))
                {
                    /// The ray pierces the infinite plane of the box top, see if it hits the finite
                    /// box top, case a.
                    float t = (srcDist - cenHeightDist) / (srcDist - dstDist);
                    Vector2 hit2sec = src2sec + t * (dst2sec - src2sec);
                    if (InsideRect(hit2sec))
                    {
                        hitBlock.Position = src + t * (dst - src);
                        hitBlock.Normal = cenNormal;
                        hitBlock.Min = Math.Min(src.Z, cenCollisionHeight);
                        hitBlock.Max = Math.Max(src.Z, cenCollisionHeight);
                        hitBlock.BlockHeight = cenCollisionHeight;
                        hitBlock.CrossesPath = true;
                        return true;
                    }
                }
                if (Road.Generator.PassUnder)
                {
                    if ((srcDist < cenBaseDist) && (dstDist > cenBaseDist))
                    {
                        /// The ray pierces the infinite plane of the box top, see if it hits the finite
                        /// box top, case a.
                        float t = (srcDist - cenBaseDist) / (srcDist - dstDist);
                        Vector2 hit2sec = src2sec + t * (dst2sec - src2sec);
                        if (InsideRect(hit2sec))
                        {
                            hitBlock.Position = src + t * (dst - src);
                            hitBlock.Normal = -cenNormal;
                            hitBlock.Min = Math.Min(src.Z, cenCollisionBase);
                            hitBlock.Max = Math.Max(src.Z, cenCollisionBase);
                            hitBlock.BlockHeight = cenCollisionBase;
                            hitBlock.CrossesPath = true;
                            return true;
                        }
                    }
                }

                /// Now case c, piercing the side of the box.
                /// This is the last chance for a hit.
                /// 
                if (src2sec.Y < -collWidth)
                {
                    if (dst2sec.Y > -collWidth)
                    {
                        float t = (src2sec.Y + collWidth) / (src2sec.Y - dst2sec.Y);
                        Vector2 hit2sec = src2sec + t * (dst2sec - src2sec);
                        if (InsideRect(hit2sec))
                        {
                            hitBlock.CrossesPath = true;
                            Vector3 hitPoint = src + t * (dst - src);
                            bool hit = hitPoint.Z < cenCollisionHeight;
                            if (hit && Road.Generator.PassUnder)
                            {
                                hit = hitPoint.Z > cenCollisionBase;
                            }
                            if (hit)
                            {
                                hitBlock.Position = hitPoint;
                                hitBlock.Normal = new Vector3(
                                    -sectionToWorld.M21,
                                    -sectionToWorld.M22,
                                    0.0f);
                                hitBlock.Min = Math.Min(src.Z, hitPoint.Z);
                                hitBlock.Max = Math.Max(src.Z, hitPoint.Z);
                                hitBlock.BlockHeight = cenCollisionHeight;

                                return true;
                            }
                        }
                    }
                }
                else if (src2sec.Y > collWidth)
                {
                    if (dst2sec.Y < collWidth)
                    {
                        float t = (src2sec.Y - collWidth) / (src2sec.Y - dst2sec.Y);
                        Vector2 hit2sec = src2sec + t * (dst2sec - src2sec);
                        if (InsideRect(hit2sec))
                        {
                            hitBlock.CrossesPath = true;
                            Vector3 hitPoint = src + t * (dst - src);
                            bool hit = hitPoint.Z < cenCollisionHeight;
                            if (hit && Road.Generator.PassUnder)
                            {
                                hit = hitPoint.Z > cenCollisionBase;
                            }
                            if (hit)
                            {
                                hitBlock.Position = hitPoint;
                                hitBlock.Normal = new Vector3(
                                    sectionToWorld.M21,
                                    sectionToWorld.M22,
                                    0.0f);
                                hitBlock.Min = Math.Min(src.Z, hitPoint.Z);
                                hitBlock.Max = Math.Max(src.Z, hitPoint.Z);
                                hitBlock.BlockHeight = cenCollisionHeight;

                                return true;
                            }
                        }
                    }
                }
                /// Note that we don't check hitting the ends of the box,
                /// because we expect the intersection to catch those cases.
                return false;
            }

            /// <summary>
            /// Look for the exit point of ray from src to dst. Returns false of no
            /// exit found. That can be because either src is not inside the sections bounds
            /// or dst is inside.
            /// </summary>
            /// <param name="src"></param>
            /// <param name="dst"></param>
            /// <param name="hitBlock"></param>
            /// <returns></returns>
            public bool Leaves(Vector3 src, Vector3 dst, ref Terrain.HitBlock hitBlock)
            {
                Vector2 src2 = new Vector2(src.X, src.Y);
                Vector2 src2sec = Vector2.Transform(src2, worldToSection);

                float collWidth = Road.Generator.CollWidth;

                if (!InsideRect(src2sec))
                {
                    return false;
                }

                hitBlock.CrossesPath = true;

                Vector2 dst2 = new Vector2(dst.X, dst.Y);
                Vector2 dst2sec = Vector2.Transform(dst2, worldToSection);

                if (dst2sec.Y < -collWidth)
                {
                    float t = (src2sec.Y + collWidth) / (src2sec.Y - dst2sec.Y);
                    Vector2 hit2sec = src2sec + t * (dst2sec - src2sec);
                    if (InsideRect(hit2sec))
                    {
                        Vector3 hitPoint = src + t * (dst - src);

                        hitBlock.Position = hitPoint;
                        hitBlock.Normal = new Vector3(
                            sectionToWorld.M21,
                            sectionToWorld.M22,
                            0.0f);
                        hitBlock.Min = Math.Min(src.Z, hitPoint.Z);
                        hitBlock.Max = Math.Max(src.Z, hitPoint.Z);
                        hitBlock.BlockHeight = 0.0f;

                        return true;
                    }
                }
                else if (dst2sec.Y > collWidth)
                {
                    float t = (src2sec.Y - collWidth) / (src2sec.Y - dst2sec.Y);
                    Vector2 hit2sec = src2sec + t * (dst2sec - src2sec);
                    if (InsideRect(hit2sec))
                    {
                        Vector3 hitPoint = src + t * (dst - src);
                        hitBlock.Position = hitPoint;
                        hitBlock.Normal = new Vector3(
                            -sectionToWorld.M21,
                            -sectionToWorld.M22,
                            0.0f);
                        hitBlock.Min = Math.Min(src.Z, hitPoint.Z);
                        hitBlock.Max = Math.Max(src.Z, hitPoint.Z);
                        hitBlock.BlockHeight = 0.0f;

                        return true;
                    }
                }
                float halfEdge = edgeLength * 0.5f;
                if (dst2sec.X < -halfEdge)
                {
                    float t = (src2sec.X + halfEdge) / (src2sec.X - dst2sec.X);

                    Vector2 hit2sec = src2sec + t * (dst2sec - src2sec);
                    if (InsideRect(hit2sec))
                    {
                        Vector3 hitPoint = src + t * (dst - src);
                        hitBlock.Position = hitPoint;
                        hitBlock.Normal = new Vector3(
                            sectionToWorld.M11,
                            sectionToWorld.M12,
                            0.0f);
                        hitBlock.Min = Math.Min(src.Z, hitPoint.Z);
                        hitBlock.Max = Math.Max(src.Z, hitPoint.Z);
                        hitBlock.BlockHeight = 0.0f;

                        return true;
                    }
                }
                else if (dst2sec.X > halfEdge)
                {
                    float t = (src2sec.X - halfEdge) / (src2sec.X - dst2sec.X);

                    Vector2 hit2sec = src2sec + t * (dst2sec - src2sec);
                    if (InsideRect(hit2sec))
                    {
                        Vector3 hitPoint = src + t * (dst - src);
                        hitBlock.Position = hitPoint;
                        hitBlock.Normal = new Vector3(
                            -sectionToWorld.M11,
                            -sectionToWorld.M12,
                            0.0f);
                        hitBlock.Min = Math.Min(src.Z, hitPoint.Z);
                        hitBlock.Max = Math.Max(src.Z, hitPoint.Z);
                        hitBlock.BlockHeight = 0.0f;

                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Render this section of road/wall
            /// </summary>
            /// <param name="camera"></param>
            public void Render(Camera camera)
            {
                if (RenderObj != null)
                {
                    Frustum.CullResult cull = camera.Frustum.CullTest(Sphere);
                    if (cull != Frustum.CullResult.TotallyOutside)
                    {
                        RenderObj.Render(camera, Road);
                    }
                }
            }

            /// <summary>
            /// Free up anything that needs explicit freeing (device stuff)
            /// </summary>
            public void Clear()
            {
                renderObj.Clear();
                WayPoint.RoadCost -= Cost;
            }

            /// <summary>
            /// Build section of geometry between the two nodes
            /// </summary>
            /// <param name="n0"></param>
            /// <param name="n1"></param>
            public bool Connect(Road road, WayPoint.Edge edge)
            {
                this.road = road;
                this.edge = edge;

                return true;
            }

            /// <summary>
            /// Construct self as much as can be done without assurance any neighbors 
            /// have been built.
            /// </summary>
            /// <returns></returns>
            public bool Build()
            {
                road.Generator.NewSection(this);

                CacheCollisionXfms(road.Generator);

                return renderObj != null;
            }

            /// <summary>
            /// Finish construction now that all other sections and intersections have
            /// been built.
            /// </summary>
            public void Finish()
            {
                if (renderObj != null)
                {
                    renderObj.Finish(this);
                    sphere = renderObj.Sphere;
                    WayPoint.RoadCost += ComputeCost();
                }
            }

            /// <summary>
            /// Return the angle between the two edges in radians.
            /// </summary>
            /// <param name="center"></param>
            /// <returns></returns>
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

            /// <summary>
            /// The height of the road at this position. Assumes position is over road.
            /// Only public for access by Road RenderObjects during construction.
            /// </summary>
            /// <param name="pos"></param>
            /// <returns></returns>
            public float BaseHeight(Vector3 pos)
            {
                return BaseHeight(new Vector2(pos.X, pos.Y));
            }

            /// <summary>
            /// The height of the road at this position. Assumes position is over road.
            /// Only public for access by Road RenderObjects during construction.
            /// </summary>
            /// <param name="pos"></param>
            /// <returns></returns>
            public float BaseHeight(Vector2 pos)
            {
                float baseHeight = Terrain.GetTerrainHeightFlat(pos);
                if (Airborne)
                {
                    Vector2 pos0 = edge.Node0.Position2d;
                    Vector2 pos1 = edge.Node1.Position2d;
                    Vector2 pos0to1 = pos1 - pos0;
                    float t = Vector2.Dot(pos - pos0, pos0to1) / pos0to1.LengthSquared();
                    t = MathHelper.Clamp(t, 0.0f, 1.0f);
                    //float rampHeight = RampHeight(pos);
                    //if (t < 0.5f)
                    //{
                    //    float shortest = isect0.ShortestEdge * 0.5f;
                    //    t = Vector2.Distance(pos, pos0) / shortest;
                    //    t = 1.0f - MathHelper.Clamp(t, 0.0f, 1.0f);
                    //    rampHeight += t * (isect0.BaseHeight(pos) - rampHeight);
                    //}
                    //else
                    //{
                    //    float shortest = isect1.ShortestEdge * 0.5f;
                    //    t = Vector2.Distance(pos, pos1) / shortest;
                    //    t = 1.0f - MathHelper.Clamp(t, 0.0f, 1.0f);
                    //    rampHeight += t * (isect1.BaseHeight(pos) - rampHeight);
                    //}
                    float rampHeight = t < 0.5f
                        ? isect0.BaseHeight(pos)
                        : isect1.BaseHeight(pos);

#if MF_AIRBORNE_THRU
                    if (!edge.Node0.OnGround || !edge.Node1.OnGround)
                    {
                        baseHeight = rampHeight;
                    }
                    else
#endif // MF_AIRBORNE_THRU
                    {
                        baseHeight = Math.Max(baseHeight, rampHeight);
                    }
                }
                return baseHeight;
            }

            /// <summary>
            /// Get this sections notion of the height at pos, disregarding other
            /// sections and intersections.
            /// </summary>
            /// <param name="pos"></param>
            /// <returns></returns>
            public float RampHeight(Vector2 pos)
            {
                if (Airborne)
                {
                    Vector2 pos0 = edge.Node0.Position2d;
                    Vector2 pos1 = edge.Node1.Position2d;
                    Vector2 pos0to1 = pos1 - pos0;
                    float t = Vector2.Dot(pos - pos0, pos0to1) / pos0to1.LengthSquared();
                    t = MathHelper.Clamp(t, 0.0f, 1.0f);

                    if (t < 0.5f)
                    {
                        if (edge.Node0.Airborne)
                        {
                            Plane other = new Plane();
                            if (edge.Node1.Airborne)
                            {
                                other = isect1.Plane;
                            }
                            else
                            {
                                other.Normal = Vector3.UnitZ;
                                other.D = -Terrain.GetTerrainHeightFlat((pos0 + pos1) * 0.5f);
                                t = t * 2.0f;
                            }
                            t = t * t * (3.0f - 2.0f * t);

                            float baseHeight = PlaneHeight(isect0.Plane, pos);
                            baseHeight += t * (PlaneHeight(other, pos) - baseHeight);

                            return baseHeight;
                        }

                    }
                    else
                    {
                        if (edge.Node1.Airborne)
                        {
                            Plane other = new Plane();
                            if (edge.Node0.Airborne)
                            {
                                other = isect0.Plane;
                                t = 1.0f - t;
                            }
                            else
                            {
                                other.Normal = Vector3.UnitZ;
                                other.D = -Terrain.GetTerrainHeightFlat((pos0 + pos1) * 0.5f);
                                t = (1.0f - t) * 2.0f;
                            }
                            t = t * t * (3.0f - 2.0f * t);

                            float baseHeight = PlaneHeight(isect1.Plane, pos);
                            baseHeight += t * (PlaneHeight(other, pos) - baseHeight);

                            return baseHeight;
                        }
                    }
                }

                return 0.0f;
            }

            /// <summary>
            /// Store off the two intersections at our end points.
            /// </summary>
            /// <param name="isect"></param>
            public void AddIntersection(Intersection isect)
            {
                if (isect.Node == edge.Node0)
                {
                    isect0 = isect;
                }
                else
                {
                    Debug.Assert(isect.Node == edge.Node1, "Receiving intersection for unknown node");
                    isect1 = isect;
                }
            }
            #endregion Public

            #region Internal
            /// <summary>
            /// Build transforms from world space into section space. In section
            /// space, the section rect is an axis aligned box dimensioned
            /// edgeLength X CollWidth centered at (0,0).
            /// </summary>
            /// <param name="gen"></param>
            protected void CacheCollisionXfms(RoadGenerator gen)
            {
                worldToSection = Matrix.Identity;
                sectionToWorld = Matrix.Identity;

                // Center at the point between the nodes
                Vector2 nodeCenter = (Edge.Node1.Position2d + Edge.Node0.Position2d) * 0.5f;

                sectionToWorld.M41 = nodeCenter.X;
                sectionToWorld.M42 = nodeCenter.Y;

                // Align the section's X axis with the axis between the nodes.
                // Apply no scaling, so computations based on length can still 
                // be done using world space units.

                Vector2 nodeAxis = Edge.Node1.Position2d - Edge.Node0.Position2d;

                edgeLength = nodeAxis.Length();
                if (edgeLength < 0.01f)
                {
                    Vector3 position = edge.Node1.Position;
                    position.Y = edge.Node0.Position2d.Y + 0.01f;
                    edge.Node1.Position = position;
                    nodeAxis = edge.Node1.Position2d - edge.Node0.Position2d;
                    edgeLength = nodeAxis.Length();
                }

                nodeAxis /= edgeLength;

                sectionToWorld.M11 = nodeAxis.X;
                sectionToWorld.M12 = nodeAxis.Y;

                sectionToWorld.M21 = -nodeAxis.Y;
                sectionToWorld.M22 = nodeAxis.X;

                // Rotation is just the transpose
                worldToSection.M11 = sectionToWorld.M11;
                worldToSection.M12 = sectionToWorld.M21;
                worldToSection.M21 = sectionToWorld.M12;
                worldToSection.M22 = sectionToWorld.M22;

                // Translation is negated and rotated
                worldToSection.M41 = -sectionToWorld.M41 * sectionToWorld.M11
                    - sectionToWorld.M42 * sectionToWorld.M12;
                worldToSection.M42 = -sectionToWorld.M41 * sectionToWorld.M21
                    - sectionToWorld.M42 * sectionToWorld.M22;
            }

            /// <summary>
            /// Test whether position is contained in the section's rect.
            /// </summary>
            /// <param name="pos"></param>
            /// <returns></returns>
            private bool InsideRect(Vector2 pos)
            {
                float collWidth = Road.Generator.CollWidth + 1.0e-5f;
                float halfEdge = edgeLength * 0.5f + 1.0e-5f;

                return (Math.Abs(pos.X) <= halfEdge)
                    && (Math.Abs(pos.Y) <= collWidth);
            }
            /// <summary>
            /// Take a guess at a good place to sample the terrain.
            /// </summary>
            /// <param name="src2sec"></param>
            /// <param name="dst2sec"></param>
            /// <returns></returns>
            private Vector2 CenterPoint(Vector2 src2sec, Vector2 dst2sec)
            {
                if (src2sec.Y * dst2sec.Y >= 0.0f)
                {
                    /// Same side of central axis.
                    return new Vector2((src2sec.X + dst2sec.X) * 0.5f, 0.0f);
                }
                /// Opposite sides. Find the crossing point.
                float t = src2sec.Y / (src2sec.Y - dst2sec.Y);
                return src2sec + t * (dst2sec - src2sec);
            }

            /// <summary>
            /// Compute the normal at the given position, assuming pos is over
            /// (or nearly over) the road.
            /// </summary>
            /// <param name="pos"></param>
            /// <param name="cenHeight"></param>
            /// <returns></returns>
            protected Vector3 GetNormal(Vector2 pos)
            {
                Vector3 pos0 = edge.Node0.Position;
                Vector3 pos1 = edge.Node1.Position;
                Vector3 pos0to1 = pos1 - pos0;
                Vector3 right = new Vector3(pos0to1.Y, -pos0to1.X, 0.0f);
                Vector3 normal = Vector3.Cross(right, pos0to1);

                normal.Normalize();

                return normal;
            }

            /// <summary>
            /// Project pos onto the plane and return it's height.
            /// Assumes the plane normal is not horizontal.
            /// </summary>
            /// <param name="pln"></param>
            /// <param name="pos"></param>
            /// <returns></returns>
            private static float PlaneHeight(Plane pln, Vector2 pos)
            {
                return (-pln.D - pln.Normal.X * pos.X - pln.Normal.Y * pos.Y)
                    / pln.Normal.Z;
            }

            /// <summary>
            /// Estimate the cost of the section of road.
            /// Cached away so we can deduct it later when we go away.
            /// </summary>
            /// <returns></returns>
            private float ComputeCost()
            {
                Cost = 0;
                if (road.Generator != null)
                {
                    Cost = (edge.Node0.Position - edge.Node1.Position).Length()
                            * road.Generator.CostPerMeter;
                }
                return Cost;
            }

            #endregion Internal

        }
    }
}
