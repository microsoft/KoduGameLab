
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
        /// The joint between 2 or more edges within a path, at a node
        /// </summary>
        public class Intersection
        {
            #region Members
            protected Road road;
            protected WayPoint.Node node;
            protected List<Section> sections = new List<Section>();
            protected List<RenderObj> fans = new List<RenderObj>();
            protected BoundingSphere sphere;
            protected bool abstain = false;
            protected Plane plane = new Plane(Vector3.UnitZ, 0.0f);

            protected const int kNumSectionStrengths = 20;
            protected float[][] sectionStrengths = null;
            private float cost = 0.0f;
            #endregion Members

            #region Accessors
            /// <summary>
            /// The owning road
            /// </summary>
            public Road Road
            {
                get { return road; }
            }
            /// <summary>
            /// The node this intersection centers on.
            /// </summary>
            public WayPoint.Node Node
            {
                get { return node; }
            }
            /// <summary>
            /// All of the sections leading into this intersection.
            /// </summary>
            public List<Section> Sections
            {
                get { return sections; }
                private set { sections = value; }
            }
            /// <summary>
            /// List of renderable fans.
            /// </summary>
            public List<RenderObj> Fans
            {
                get { return fans; }
            }
            /// <summary>
            /// Bounding sphere
            /// </summary>
            public BoundingSphere Sphere
            {
                get { return sphere; }
            }
            /// <summary>
            /// Height of the base of this intersection
            /// </summary>
            public float Height
            {
                get { return node.Position.Z; }
            }
            /// <summary>
            /// Whether this section of road is airborne or hugs the ground.
            /// </summary>
            public bool Airborne
            {
                get { return node.Airborne; }
            }
            /// <summary>
            /// Temp for ray cast walking.
            /// </summary>
            public bool Abstain
            {
                get { return abstain; }
                set { abstain = value; }
            }
            /// <summary>
            /// A plane approximating this intersection.
            /// </summary>
            public Plane Plane
            {
                get { return plane; }
                private set { plane= value; }
            }

            /// <summary>
            /// Return an estimate of the cost of this chunk.
            /// </summary>
            public float Cost
            {
                get { return cost; }
                private set { cost = value; }
            }
            #endregion

            #region Public

            /// <summary>
            /// If the position is on the intersection, and the intersection
            /// IsGround, compute the height there and return true.
            /// Otherwise return false and leave height unmodified.
            /// </summary>
            /// <param name="pos"></param>
            /// <param name="height"></param>
            /// <returns></returns>
            public bool GetHeight(Vector3 pos, ref float height)
            {
                if (Road.Generator.GetHeight(
                    Node.Position,
                    pos,
                    ref height))
                {
                    const float kTolerance = 0.001f;
                    float baseHeight = BaseHeight(pos);
                    if (height >= 0.0f)
                    {
                        if (Road.Generator.PassUnderEnd && (pos.Z + kTolerance <= baseHeight))
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
            /// Add collision info to list if the sphere is touching this intersection.
            /// </summary>
            /// <param name="center"></param>
            /// <param name="radius"></param>
            /// <param name="collisions"></param>
            public void GetCollisions(Vector3 center, float radius, List<CollisionInfo> collisions)
            {
                float collRadius = Road.Generator.CollRadius;
                if (collRadius > 0.0f)
                {
                    float baseHeight = BaseHeight(center);
                    float collTop = baseHeight + Road.Generator.CollEndHeight;
                    float collBot = baseHeight + Road.Generator.CollEndBase;
                    if (!Road.Generator.PassUnderEnd)
                        collBot = 0.0f;
                    if ((center.Z + radius < collTop) 
                        && (center.Z - radius > collBot))
                    {
                        Vector2 cen2 = new Vector2(center.X, center.Y);
                        cen2 -= Node.Position2d;
                        float distSq = cen2.LengthSquared();
                        float radSumSq = radius + collRadius;
                        radSumSq *= radSumSq;

                        if (distSq < radSumSq)
                        {
                            CollisionInfo info = new CollisionInfo();
                            info.norm.X = cen2.X;
                            info.norm.Y = cen2.Y;
                            info.norm.Z = 0.0f;
                            float dist = cen2.Length();
                            if (dist > 0.0f)
                            {
                                info.norm /= dist;
                                info.depth = Math.Max(0.0f, radius + collRadius - dist);
                            }
                            else
                            {
                                info.norm = Vector3.UnitZ;
                                info.depth = radius * collRadius;
                            }
                            collisions.Add(info);
                        }
                    }
                }
            }

            /// <summary>
            /// Check whether the ray passes through this intersection.
            /// </summary>
            /// <param name="src"></param>
            /// <param name="dst"></param>
            /// <param name="hitBlock"></param>
            /// <returns></returns>
            public bool Blocked(Vector3 src, Vector3 dst, ref Terrain.HitBlock hitBlock)
            {
                float collRadius = Road.Generator.CollRadius;
                if (collRadius > 0.0f)
                {
                    Vector2 src2 = new Vector2(src.X, src.Y);
                    Vector2 src2toCenter = src2 - Node.Position2d;
                    float distSq = src2toCenter.LengthSquared();
                    float radSumSq = collRadius;
                    radSumSq *= radSumSq;

                    Vector2 cen2 = Node.Position2d;

                    float baseHeight = BaseHeight(cen2);
                    float collTop = baseHeight + Road.Generator.CollEndHeight;
                    float collBot = baseHeight + Road.Generator.CollEndBase;
                    if (!Road.Generator.PassUnderEnd)
                        collBot = 0.0f;

                    Vector3 normal = Plane.Normal;
                    float srcDist = Vector3.Dot(normal, src);
                    float dstDist = Vector3.Dot(normal, dst);
                    float topDist = Vector3.Dot(normal, new Vector3(cen2, collTop));
                    float botDist = Vector3.Dot(normal, new Vector3(cen2, collBot));
                    if (distSq < radSumSq)
                    {
                        hitBlock.CrossesPath = true;
                        /// src is over the intersection, check for degenerate case.
                        if ((srcDist < topDist) && (srcDist > botDist))
                        {
                            hitBlock.Position = src;
                            hitBlock.Normal = Plane.Normal;
                            hitBlock.Min = hitBlock.Max = collTop;
                            hitBlock.BlockHeight = collTop;
                            return true;
                        }

                    }
                    if((srcDist > topDist)&&(dstDist < topDist))
                    {
                        /// The ray crosses the collision top plane. See if it intersects
                        /// the collision top cap.
                        float t = (srcDist - topDist) / (srcDist - dstDist);
                        Vector3 hitPoint = src + t * (dst - src);
                        distSq = new Vector2(
                            cen2.X - hitPoint.X,
                            cen2.Y - hitPoint.Y).LengthSquared();
                        if (distSq < radSumSq)
                        {
                            hitBlock.Position = hitPoint;
                            hitBlock.Normal = Plane.Normal;
                            hitBlock.Min = collTop;
                            hitBlock.Max = src.Z;
                            hitBlock.BlockHeight = collTop;
                            return true;
                        }
                    }
                    if (Road.Generator.PassUnderEnd)
                    {
                        if((srcDist < botDist) && (dstDist > botDist))
                        {
                            /// The ray crosses the collision top plane. See if it intersects
                            /// the collision top cap.
                            float t = (srcDist - botDist) / (srcDist - dstDist);
                            Vector3 hitPoint = src + t * (dst - src);
                            distSq = new Vector2(
                                cen2.X - hitPoint.X,
                                cen2.Y - hitPoint.Y).LengthSquared();
                            if (distSq < radSumSq)
                            {
                                hitBlock.Position = hitPoint;
                                hitBlock.Normal = -Plane.Normal;
                                hitBlock.Min = collBot;
                                hitBlock.Max = src.Z;
                                hitBlock.BlockHeight = collTop;
                                return true;
                            }
                        }
                    }
                    Vector2 dst2 = new Vector2(dst.X, dst.Y);

                    /// Okay, look for intersection with circle.
                    float a = Vector2.DistanceSquared(dst2, src2);
                    if (a > 0.0f)
                    {
                        float b = 2.0f * Vector2.Dot(src2 - cen2, dst2 - src2);
                        float c = Vector2.DistanceSquared(src2, cen2) - radSumSq;

                        float det = b * b - 4.0f * a * c;
                        if (det >= 0)
                        {
                            float t = (float)((-b - Math.Sqrt(det)) / (2.0f * a));
                            if ((t >= 0.0f) && (t <= 1.0f))
                            {
                                hitBlock.CrossesPath = true;
                                Vector3 hitPoint = src + t * (dst - src);
                                if ((hitPoint.Z < collTop)&&(hitPoint.Z > collBot))
                                {
                                    hitBlock.Position = hitPoint;
                                    hitBlock.Normal = Vector3.Normalize(new Vector3(
                                        hitPoint.X - cen2.X,
                                        hitPoint.Y - cen2.Y,
                                        0.0f));
                                    hitBlock.Min = Math.Min(src.Z, hitPoint.Z);
                                    hitBlock.Max = Math.Max(src.Z, hitPoint.Z);
                                    hitBlock.BlockHeight = collTop;
                                    return true;
                                }
                            }
                        }
                    }
                }
                return false;
            }

            /// <summary>
            /// Check whether the ray originates in this intersection and exits.
            /// If so, find the exit point.
            /// </summary>
            /// <param name="src"></param>
            /// <param name="dst"></param>
            /// <param name="hitBlock"></param>
            /// <returns></returns>
            public bool Leaves(Vector3 src, Vector3 dst, ref Terrain.HitBlock hitBlock)
            {
                float collRadius = Road.Generator.CollRadius;
                if (collRadius > 0.0f)
                {
                    Vector2 src2 = new Vector2(src.X, src.Y);
                    Vector2 src2toCenter = src2 - Node.Position2d;
                    float distSq = src2toCenter.LengthSquared();
                    float radSumSq = collRadius;
                    radSumSq *= radSumSq;

                    Vector2 cen2 = Node.Position2d;

                    if (distSq > radSumSq)
                    {
                        return false;
                    }
                    hitBlock.CrossesPath = true;
                    Vector2 dst2 = new Vector2(dst.X, dst.Y);

                    /// Okay, look for intersection with circle.
                    float a = Vector2.DistanceSquared(dst2, src2);
                    if (a > 0.0f)
                    {
                        float b = 2.0f * Vector2.Dot(src2 - cen2, dst2 - src2);
                        float c = Vector2.DistanceSquared(src2, cen2) - radSumSq;

                        float det = b * b - 4.0f * a * c;
                        if (det >= 0)
                        {
                            float t = (float)((-b - Math.Sqrt(det)) / (2.0f * a));
                            if ((t >= 0.0f) && (t <= 1.0f))
                            {
                                Vector3 hitPoint = src + t * (dst - src);
                                hitBlock.Position = hitPoint;
                                hitBlock.Normal = Vector3.Normalize(new Vector3(
                                    cen2.X - hitPoint.X,
                                    cen2.Y - hitPoint.Y,
                                    0.0f));
                                hitBlock.Min = Math.Min(src.Z, hitPoint.Z);
                                hitBlock.Max = Math.Max(src.Z, hitPoint.Z);
                                hitBlock.BlockHeight = 0.0f;
                                return true;
                            }
                        }
                    }
                }
                return false;
            }


            /// <summary>
            /// Render this intersection of road/wall
            /// </summary>
            /// <param name="camera"></param>
            public void Render(Camera camera)
            {
                Frustum.CullResult cull = camera.Frustum.CullTest(Sphere);
                if (cull != Frustum.CullResult.TotallyOutside)
                {
                    foreach (RenderObj fan in Fans)
                    {
                        fan.Render(camera, Road);
                    }
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
                WayPoint.RoadCost -= Cost;
            }

            /// <summary>
            /// Comparison function for sorting sections around the intersection.
            /// </summary>
            public class EdgeCompare : IComparer<Section>
            {
                WayPoint.Node center;
                public EdgeCompare(WayPoint.Node node)
                {
                    center = node;
                }
                public int Compare(Section left, Section right)
                {
                    if (left == right)
                    {
                        return 0;
                    }
                    if (left == null)
                    {
                        return right == null ? 0 : -1;
                    }
                    if (right == null)
                    {
                        return 1;
                    }
                    float rads0 = (float)left.EdgeAngle(center);
                    float rads1 = (float)right.EdgeAngle(center);
                    return rads0 < rads1
                        ? -1
                        : rads0 > rads1
                            ? 1
                            : 0;

                }
            }

            /// <summary>
            /// Connect all the sections of this road that meet at this node.
            /// </summary>
            /// <param name="inRoad"></param>
            /// <param name="inNode"></param>
            /// <returns></returns>
            public bool Connect(Road road, WayPoint.Node node)
            {
                this.road = road;
                this.node = node;
                // Make a list of all edges hitting this node. This should
                // probably already be stored on the node.
                sections.Clear();
                foreach (Section section in Road.Sections)
                {
                    WayPoint.Edge edge = section.Edge;
                    if ((edge.Node0 == node) || (edge.Node1 == node))
                    {
                        sections.Add(section);
                        section.AddIntersection(this);
                    }
                }
                // Sort them by angle
                sections.Sort(new EdgeCompare(node));

                BuildNormal();
                BuildSectionStrengths();

                return true;
            }

            /// <summary>
            /// Do all the building we can without knowing whether our neighbors have
            /// been built yet.
            /// </summary>
            /// <returns></returns>
            public bool Build()
            {
                bool doDrop = sections.Count == 0;
                bool doTrim = Road.Generator.Trims && (sections.Count > 1);
                bool doSingle = (!Road.Generator.Trims || (sections.Count == 1)) && (sections.Count > 0);

                if (doDrop)
                {
                    MakeFan(null, null);
                }
                else if (doTrim)
                {

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
                            MakeFan(sections[first], sections[second]);
                        }
                    }

                }
                else if (doSingle)
                {
                    // Make an end cap
                    MakeFan(sections[0], sections[0]);
                }

                return Fans.Count > 0;
            }

            /// <summary>
            /// Finish up any building that requires knowing that our neighbors have been
            /// built (but not necessarily finished)
            /// </summary>
            public void Finish()
            {
                AABB box = AABB.EmptyBox();
                foreach (RenderObj renderObj in Fans)
                {
                    renderObj.Finish(this);
                    box.Union(renderObj.Sphere);
                }
                sphere = box.MakeSphere();
                WayPoint.RoadCost += ComputeCost();
            }

            /// <summary>
            /// Return the height we think pos should be at. 
            /// </summary>
            /// <param name="pos"></param>
            /// <returns></returns>
            public float BaseHeight(Vector2 pos)
            {
                float baseHeight = Terrain.GetTerrainHeightFlat(pos);

                if (Airborne)
                {
                    float rampHeight = RampHeight(pos);

#if MF_AIRBORNE_THRU
                    if (node.Airborne)
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
            /// Return the height we think pos should be at. 
            /// </summary>
            /// <param name="pos"></param>
            /// <returns></returns>
            public float BaseHeight(Vector3 pos)
            {
                return BaseHeight(new Vector2(pos.X, pos.Y));
            }

            /// <summary>
            /// Project pos onto our plane
            /// </summary>
            /// <param name="pos"></param>
            /// <returns></returns>
            public float PlaneHeight(Vector2 pos)
            {
                return (-Plane.D - Plane.Normal.X * pos.X - Plane.Normal.Y * pos.Y)
                    / Plane.Normal.Z;
            }

            public bool GetHeightAndNormal(Vector3 pos, ref float height, ref Vector3 normal)
            {
                if (GetHeight(pos, ref height))
                {
                    normal = Plane.Normal;

                    return true;
                }
                return false;
            }

            #endregion Public

            #region Internal

            protected void BuildNormal()
            {
                Vector3 normal = Vector3.UnitZ;
                if (Sections.Count <= 1)
                {
                    normal = Vector3.UnitZ;
                }
                //else if (Sections.Count <= 1)
                //{
                //    WayPoint.Node other = Sections[0].Edge.OtherNode(Node);
                //    Vector3 toOther = other.Position - Node.Position;
                //    Vector3 right = new Vector3(toOther.Y, -toOther.X, 0.0f);
                //    normal = Vector3.Normalize(Vector3.Cross(right, toOther));
                //}
                else
                {
                    /// This isn't exact, but it's fast, robust, and close enough
                    normal = Vector3.Zero;
                    foreach (Section section in Sections)
                    {
                        WayPoint.Node other = section.Edge.OtherNode(Node);
                        Vector3 toOther = other.Position - Node.Position;
                        Vector3 right = new Vector3(toOther.Y, -toOther.X, 0.0f);
                        normal += Vector3.Normalize(Vector3.Cross(right, toOther));
                    }
                    normal.Normalize();
                }
                Plane = new Plane(normal, -Vector3.Dot(normal, Node.Position));
            }

            protected void BuildSectionStrengths()
            {
                sectionStrengths = null;
                if (sections.Count > 1)
                {
                    sectionStrengths = new float[sections.Count][];

                    for (int i = 0; i < sections.Count; ++i)
                    {
                        sectionStrengths[i] = new float[kNumSectionStrengths];

                        int dirIndex = DirectionIndex(sections[i]);
                        sectionStrengths[i][dirIndex] = 1.0f;

                        int iNext = i > 0 ? i - 1 : sections.Count - 1;

                        int dirNext = DirectionIndex(sections[iNext]);
                        if (dirNext <= dirIndex)
                            dirNext += kNumSectionStrengths;

                        float norm = 1.0f / (dirNext - dirIndex);

                        for (int j = dirIndex + 1; j != dirNext; ++j)
                        {
                            int index = j < kNumSectionStrengths ? j : j - kNumSectionStrengths;

                            sectionStrengths[i][index] = 1.0f - ((float)(j - dirIndex)) * norm;
                            sectionStrengths[i][index] *= sectionStrengths[i][index];
                        }

                        int iPrev = i + 1 < sections.Count ? i + 1 : 0;

                        int dirPrev = DirectionIndex(sections[iPrev]);
                        if (dirPrev >= dirIndex)
                            dirPrev -= kNumSectionStrengths;

                        norm = 1.0f / (dirIndex - dirPrev);

                        for (int j = dirIndex - 1; j != dirPrev; --j)
                        {
                            int index = j >= 0 ? j : j + kNumSectionStrengths;

                            sectionStrengths[i][index] = 1.0f - ((float)(dirIndex - j)) * norm;
                            sectionStrengths[i][index] *= sectionStrengths[i][index];
                        }
                    }
                }
                else if (sections.Count > 0)
                {
                    /// Would be cheaper to just special case this in the lookup, since
                    /// if count == 1, we know the lookup == 1.0f.
                    sectionStrengths = new float[1][];
                    sectionStrengths[0] = new float[kNumSectionStrengths];
                    for (int i = 0; i < kNumSectionStrengths; ++i)
                    {
                        sectionStrengths[0][i] = 1.0f;
                    }
                }
            }

            protected int DirectionIndex(Section section)
            {
                Vector2 pos = section.Edge.Node0 == Node
                    ? section.Edge.Node1.Position2d
                    : section.Edge.Node0.Position2d;

                return DirectionIndex(pos);
            }

            protected int DirectionIndex(Vector2 pos)
            {
                Vector2 toPos = pos - Node.Position2d;
                double dir = (Math.PI - Math.Atan2(toPos.Y, toPos.X)) / (Math.PI * 2.0);
                int dirIndex = (int)(dir * kNumSectionStrengths);

                return dirIndex;
            }

            protected float SectionHeight(Vector2 pos)
            {
                float h = 0.0f;
                if (sections.Count > 0)
                {
                    int dirIndex = DirectionIndex(pos);

                    float wgt = 0.0f;

                    Debug.Assert(sectionStrengths.Length == sections.Count, "Should be a matched pair");
                    for (int i = 0; i < sections.Count; ++i)
                    {
                        float sectionStrength = sectionStrengths[i][dirIndex];
                        if (sectionStrength > 0.0f)
                        {
                            h += sections[i].RampHeight(pos) * sectionStrength;
                            wgt += sectionStrength;
                        }
                    }
                    if (wgt > 0.0f)
                        h /= wgt;
                }
                else
                {
                    h = PlaneHeight(pos);
                }

                return h;
            }

            protected float AttenFromNode(Vector2 pos)
            {
                return 0.0f;
                //float dist = Vector2.Distance(pos, Node.Position2d);
                //return MathHelper.Clamp(1.0f - dist / Road.Generator.MaxWidth, 0.0f, 1.0f);
            }

            protected float RampHeight(Vector2 pos)
            {
                return SectionHeight(pos);
                //float nodeStrength = AttenFromNode(pos);

                //float nodeHeight = PlaneHeight(pos);

                //float sectHeight = SectionHeight(pos);

                //return sectHeight + nodeStrength * (nodeHeight - sectHeight);
            }

            /// <summary>
            /// Generate a filler arc of a circle fan, with center at pC,
            /// and the arc going from p0 to p1, counterclockwise.
            /// </summary>
            /// <param name="p0"></param>
            /// <param name="p1"></param>
            /// <param name="pC"></param>
            protected void MakeFan(Section first, Section second)
            {
                Road.Generator.NewFan(this, first, second, fans);
            }

            /// <summary>
            /// Compute and cache away the cost of this intersection.
            /// Cached away so we can deduct it later when we go away.
            /// </summary>
            /// <returns></returns>
            private float ComputeCost()
            {
                Cost = 0;
                if ((road.Generator != null) && (Fans.Count > 0))
                {
                    Cost = road.Generator.CostPerNode;
                }
                return Cost;
            }
            #endregion Internal
        }
    }
}
