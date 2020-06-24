
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;

using Boku.Base;
using Boku.Common;
using Boku.SimWorld.Terra;

namespace Boku.SimWorld.Path
{
    public partial class WayPoint : INeedsDeviceReset
    {
        public class Edge
        {
            #region Members
            private Node node0 = null;
            private Node node1 = null;

            private bool moving = false;
            private bool edit = false;
            private bool select = false;

            public enum Direction
            {
                Up = 1,
                Down = 2,
                Both = 3
            };
            private Direction dir = Direction.Both;
            #endregion Members

            #region Accessors
            public Node Node0
            {
                get { return node0; }
                set { node0 = value; }
            }
            public Node Node1
            {
                get { return node1; }
                set { node1 = value; }
            }
            public Path Path
            {
                get
                {
                    if (node0 != null)
                    {
                        Debug.Assert(node1 != null);
                        Debug.Assert(node0.Path == node1.Path);
                        return node0.Path;
                    }
                    return null;
                }
            }
            public bool Moving
            {
                get { return moving; }
                set { moving = value; }
            }
            public bool Edit
            {
                get { return edit; }
                set 
                { 
                    edit = value;
                    Node0.Edit = value;
                    Node1.Edit = value;
                }
            }
            public bool Select
            {
                get { return select; }
                set 
                { 
                    select = value;
                    Node0.Select = value;
                    Node1.Select = value;
                }
            }
            public Direction Dir
            {
                get { return dir; }
                set { dir = value; }
            }
            #endregion

            #region Public
            /// <summary>
            /// Edge c'tor.  In addition to creating the edge this also checks to 
            /// make sure it's not a duplicate and adds it to the right path.
            /// </summary>
            /// <param name="node0"></param>
            /// <param name="node1"></param>
            public Edge(Node node0, Node node1)
            {
                this.node0 = node0;
                this.node1 = node1;

                Path path = node0.Path;

                // If the nodes are equal then this is a fake edge used to make 
                // things work better for paths with only 1 node so don't add it 
                // to the path.
                if (node0 == node1)
                {
                    return;
                }

                // By default, add this edge to the path that owns the first node.
                // But first, see if it's a duplicate.
                for (int i = 0; i < path.Edges.Count; i++)
                {
                    Edge e = (Edge)path.Edges[i];

                    if ((e.Node0 == node0 && e.Node1 == node1) || (e.Node0 == node1 && e.Node1 == node0))
                    {
                        // Duplicate, so don't add it.
                        return;
                    }
                }

                path.AddEdge(this);

                // Then if the second node belongs to a different path we need to 
                // merge the second node's path into the first.
                if (node0.Path != node1.Path)
                {
                    MergePaths(node0.Path, node1.Path);
                }

            }   // end of Edge c'tor

            /// <summary>
            /// Deletes the edge.
            /// </summary>
            public void Delete()
            {
                Path p = Node0.Path;
                p.RemoveEdge(this);

                // Removing this edge may split the path into two so 
                // try and keep things sane.
                p.EnsureCoherence();
            }   // end of Edge Delete()

            static List<Vector3> ptsScratch = new List<Vector3>();
            /// <summary>
            /// Renders an edge as a simple line which follows the terrain.
            /// </summary>
            /// <param name="camera"></param>
            /// <param name="color"></param>
            public void Render(Camera camera, Vector4 color, bool asRoad)
            {

                if (Path.Edit || Path.Select || Edit || Select)
                {
                    float backLength = 0.666f;

                    if (Path.Edit || Edit)
                        color *= new Vector4(0.5f, 0.5f, 1.0f, 1.0f);
                    else if (Path.Select || Select)
                        color *= new Vector4(1.0f, 0.5f, 0.5f, 1.0f);

                    float scale = 1.0f + 0.1f * (float)(Math.Sin(10.0 * Time.WallClockTotalSeconds) + 1.0);
                    scale *= 0.5f;

                    Vector3 center = HandlePosition;

                    float height = Terrain.GetTerrainAndPathHeight(new Vector3(center.X, center.Y, float.MaxValue));
                    center.Z = Math.Max(height, center.Z);

                    if ((Dir & Direction.Down) != 0)
                    {
                        Vector3 toEdge0 = Node0.RenderPosition(asRoad) - center;
                        toEdge0.Normalize();
                        Vector3 p0 = toEdge0 * backLength + center;
                        p0.Z += Node.Radius + Node.SphereHeight;

                        Utils.DrawSolidCone(camera, p0, toEdge0, scale, color);
                    }

                    if ((Dir & Direction.Up) != 0)
                    {
                        Vector3 toEdge1 = Node1.RenderPosition(asRoad) - center;
                        toEdge1.Normalize();
                        Vector3 p1 = toEdge1 * backLength + center;
                        p1.Z += Node.Radius + Node.SphereHeight;

                        Utils.DrawSolidCone(camera, p1, toEdge1, scale, color);
                    }
                }

            }   // end of Edge Render()

            public void IncDir()
            {
                switch(Dir)
                {
                    case Direction.Both:
                        Dir = Direction.Down;
                        break;
                    case Direction.Down:
                        Dir = Direction.Up;
                        break;
                    case Direction.Up:
                        Dir = Direction.Both;
                        break;
                }
                Boku.InGame.IsLevelDirty = true;
            }
            public void DecDir()
            {
                switch (Dir)
                {
                    case Direction.Both:
                        Dir = Direction.Up;
                        break;
                    case Direction.Down:
                        Dir = Direction.Both;
                        break;
                    case Direction.Up:
                        Dir = Direction.Down;
                        break;
                }
                Boku.InGame.IsLevelDirty = true;
            }

            /// <summary>
            /// Return true if you can leave the given node along this edge.
            /// </summary>
            /// <param name="n"></param>
            /// <returns></returns>
            public bool CanLeaveNode(Node n)
            {
                return ((n == Node0) && ((Dir & Direction.Up) != 0))
                        || ((n == Node1) && ((Dir & Direction.Down) != 0));
            }

            /// <summary>
            /// Returns the endpoint of this edge nearest to the given position.
            /// Takes into account the "Direction" of this edge (if not Both).
            /// </summary>
            /// <param name="pos"></param>
            /// <returns></returns>
            public Node NearestNode(Vector3 pos)
            {
                if (Dir == Direction.Up)
                {
                    return Node1;
                }
                else if (Dir == Direction.Down)
                {
                    return Node0;
                }
                else
                {
                    Vector3 delta0 = pos - node0.Position;
                    Vector3 delta1 = pos - node1.Position;
                    float distSquared0 = delta0.LengthSquared();
                    float distSquared1 = delta1.LengthSquared();

                    if (distSquared0 <= distSquared1)
                    {
                        return node0;
                    }
                    else
                    {
                        return node1;
                    }
                }
            }   // end of Edge NearestNode

            public void Rotate(Matrix delta)
            {
                Node0.Rotate(delta);
                Node1.Rotate(delta);

                Path.RecalcHeights();
            }
            /// <summary>
            /// Pass on the translation to our nodes and force an update on our path.
            /// </summary>
            /// <param name="delta"></param>
            public void Translate(Vector3 delta)
            {
                if (delta != Vector3.Zero)
                {
                    Node0.Height += delta.Z;
                    Node1.Height += delta.Z;
                    Node0.Position += delta;
                    Node1.Position += delta;
                    Path.RecalcHeights();
                }
            }
            public void ClearEdit()
            {
                Moving = false;
                Edit = false;
                Select = false;
                Node0.ClearEdit();
                Node1.ClearEdit();
                Path.ClearEdit();
            }

            /// <summary>
            /// Returns distance from input point to edge.
            /// </summary>
            /// <param name="p"></param>
            /// <returns></returns>
            public float Distance(Vector3 p)
            {
                float result = (float)Math.Sqrt(DistanceSquared(p));
                return result;
            }   // end of Edge Distance()

            /// <summary>
            /// Returns distance^2 from input point to edge.
            /// </summary>
            /// <param name="p"></param>
            /// <returns></returns>
            public float DistanceSquared(Vector3 p)
            {
                Vector3 n0 = node0.Position;
                Vector3 n1 = node1.Position;
                Vector3 d = n1 - n0;

                // Find projection of vector to point onto line segment that is the edge.
                float t = Vector3.Dot(d, (p - n0));

                if (t <= 0.0f)
                {
                    p -= n0;
                    return p.LengthSquared();
                }

                float lenD = d.LengthSquared();
                if (t >= lenD)
                {
                    p -= n1;
                    return p.LengthSquared();
                }

                p -= n0;

                float result = 0.0f;
                if (Math.Abs(lenD) > float.Epsilon)
                {
                    // The Abs function is here because round off error can result in 
                    // a slightly negative number which then becomes a NAN when some
                    // function upstream of here tries to take the square root of it.
                    result = Math.Abs(p.LengthSquared() - t * t / lenD);
                }

                return result;

            }   // end of Edge DistSquared()

            /// <summary>
            /// Returns distance from input point to edge.
            /// </summary>
            /// <param name="p"></param>
            /// <returns></returns>
            public float Distance(Vector2 p)
            {
                float result = (float)Math.Sqrt(DistanceSquared(p));
                return result;
            }   // end of Edge Distance()

            /// <summary>
            /// Returns distance^2 from input point to edge.
            /// </summary>
            /// <param name="p"></param>
            /// <returns></returns>
            public float DistanceSquared(Vector2 p)
            {
                Vector2 n0 = new Vector2(node0.Position.X, node0.Position.Y);
                Vector2 n1 = new Vector2(node1.Position.X, node1.Position.Y);
                Vector2 d = n1 - n0;

                // Find projection of vector to point onto line segment that is the edge.
                float t = Vector2.Dot(d, (p - n0));

                if (t <= 0.0f)
                {
                    p -= n0;
                    return p.LengthSquared();
                }

                float lenD = d.LengthSquared();
                if (t >= lenD)
                {
                    p -= n1;
                    return p.LengthSquared();
                }

                p -= n0;

                float result = 0.0f;
                if (Math.Abs(lenD) > float.Epsilon)
                {
                    // The Abs function is here because round off error can result in 
                    // a slightly negative number which then becomes a NAN when some
                    // function upstream of here tries to take the square root of it.
                    result = Math.Abs(p.LengthSquared() - t * t / lenD);
                }

                return result;

            }   // end of Edge DistSquared()

            /// <summary>
            /// Return the _3D_ distance from p to edge handle. Pass in Vector2 for 2d distance.
            /// </summary>
            /// <param name="p"></param>
            /// <returns></returns>
            public float DistanceSquaredToHandle(Vector3 p)
            {
                return Vector3.DistanceSquared(HandlePosition, p);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="p"></param>
            /// <returns></returns>
            public float DistanceSquaredToHandle(Vector2 p)
            {
                return Vector2.DistanceSquared(HandlePosition2d, p);
            }

            /// <summary>
            /// Return 3D distance to "handle" of this edge.
            /// </summary>
            /// <param name="p"></param>
            /// <returns></returns>
            public float DistanceToHandle(Vector3 p)
            {
                return (float)Math.Sqrt(DistanceSquaredToHandle(p));
            }
            /// <summary>
            /// Return 2D distance to "handle" of this edge.
            /// </summary>
            /// <param name="p"></param>
            /// <returns></returns>
            public float DistanceToHandle(Vector2 p)
            {
                return (float)Math.Sqrt(DistanceSquaredToHandle(p));
            }
            /// <summary>
            /// The 2D position of this edges "handle"
            /// </summary>
            public Vector2 HandlePosition2d
            {
                get { return (Node0.Position2d + Node1.Position2d) * 0.5f; }
            }

            /// <summary>
            ///  The 3D position of this edges "handle"
            /// </summary>
            public Vector3 HandlePosition
            {
                get { return (Node0.Position + Node1.Position) * 0.5f; }
            }

            /// <summary>
            /// Returns the point on the edge nearest to the input point.
            /// </summary>
            /// <param name="p"></param>
            /// <returns></returns>
            public Vector3 NearestPoint(Vector3 p)
            {
                Vector3 n0 = node0.Position;
                Vector3 n1 = node1.Position;
                Vector3 d = n1 - n0;

                // Find projection of vector to point onto line segment that is the edge.
                float t = Vector3.Dot(d, (p - n0));

                if (t <= 0.0f)
                {
                    return n0;
                }

                float lenD = d.LengthSquared();
                if (t >= lenD)
                {
                    return n1;
                }

                Vector3 result;
                if (Math.Abs(lenD) < float.Epsilon)
                {
                    result = p;
                }
                else
                {
                    result = n0 + t * d / lenD;
                }

                return result;

            }   // end of Edge NearestPoint()

            /// <summary>
            /// Return the opposite node from the input.
            /// </summary>
            /// <param name="n"></param>
            /// <returns></returns>
            public Node OtherNode(Node n)
            {
                return n == Node0 ? Node1 : Node0;
            }
            #endregion Public

        }   // end of class Edge

    }
}
