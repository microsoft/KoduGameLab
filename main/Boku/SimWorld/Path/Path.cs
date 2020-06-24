
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;
using Boku.Common;
using Boku.SimWorld.Terra;

namespace Boku.SimWorld.Path
{
    public partial class WayPoint : INeedsDeviceReset
    {
        /// <summary>
        /// A connected collection of waypoint nodes and edges that all share a common color.
        /// We use the standard classification colors but only allow red, green, blue, yellow,
        /// orange and purple.
        /// </summary>
        public class Path
        {
            #region Members
            private List<Edge> edges = new List<Edge>();
            private List<Node> nodes = new List<Node>();

            private Classification.Colors color;
            private Vector4 rgbColor;

            private Road road = null;

            private bool moving = false;
            private bool edit = false;
            private bool select = false;
            #endregion Members

            #region Accessors
            public Road Road
            {
                get { return road; }
                set
                {
                    if (road != null)
                    {
                        road.Clear();
                    }
                    road = value;
                }
            }
            public string RoadName
            {
                get { return (road != null) && (road.Generator != null) ? road.Generator.ToString() : ""; }
                set { road.Generator = Road.GenFromString(value); }
            }
            public List<Edge> Edges
            {
                get { return edges; }
            }
            public List<Node> Nodes
            {
                get { return nodes; }
            }
            public int IndexColor
            {
                set
                {
                    color = ColorPalette.GetColorFromIndex(value);
                    rgbColor = Classification.ColorVector4(color);
                }
            }
            public Classification.Colors Color
            {
                get { return color; }
                set
                {
                    color = value;
                    rgbColor = Classification.ColorVector4(color);
                }
            }
            public Vector4 RGBColor
            {
                get { return rgbColor; }
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

            public float DefaultNodeHeight
            {
                get
                {
                    return (Road != null) && (Road.Generator != null)
                        ? Road.Generator.DefaultNodeHeight
                        : 0.0f;
                }
            }

            #endregion

            #region Public
            // c'tor
            public Path(Classification.Colors color)
            {
                this.color = color;
                rgbColor = Classification.ColorVector4(color);

                Paths.Add(this);

                Road = new Road(this);

                ///This might not be the best place for this.
                Node.PrevNode.Reset();

            }   // end of Path c'tor

            /// <summary>
            /// Deletes this path with all effects.
            /// </summary>
            public void Delete()
            {
                InGame.WayPointEdit.DeletePath(this);
            }   // end of Delete()

            /// <summary>
            /// Render spheres
            /// </summary>
            /// <param name="camera"></param>
            public void Render(Camera camera, bool edgesToo)
            {
                Vector4 color = rgbColor;

                // The shadow texture is rendered inverted so
                // for shadows always render in white.
                if (InGame.inGame.renderEffects == InGame.RenderEffect.ShadowPass)
                {
                    color = new Vector4(1, 1, 1, 1);
                }

                Debug.Assert(edgePts.Count == 0);
                Debug.Assert(edgeEdit.Count == 0);
                Debug.Assert(edgeSelect.Count == 0);

                // First render the edges.
                for (int i = 0; i < Edges.Count; i++)
                {
                    Edge edge = Edges[i];
                    Vector3 pos0 = edge.Node0.RenderPosition(!edgesToo);

                    Vector3 pos1 = edge.Node1.RenderPosition(!edgesToo);

                    if (Edit || edge.Edit)
                    {
                        edgeEdit.Add(pos0);
                        edgeEdit.Add(pos1);
                    }
                    else if (Select || edge.Select)
                    {
                        edgeSelect.Add(pos0);
                        edgeSelect.Add(pos1);
                    }
                    else
                    {
                        edgePts.Add(pos0);
                        edgePts.Add(pos1);
                    }
                    edge.Render(camera, color, !edgesToo);
                }
                if (edgesToo)
                {
                    Utils.DrawFatLine(camera, edgePts, color);
                    Utils.DrawFatLine(camera, edgeEdit, new Vector4(0.5f, 0.5f, 1.0f, 1.0f));
                    Utils.DrawFatLine(camera, edgeSelect, new Vector4(1.0f, 0.5f, 0.5f, 1.0f));
                }

                edgePts.Clear();
                edgeEdit.Clear();
                edgeSelect.Clear();

                // Then render the nodes.
                for (int i = 0; i < Nodes.Count; i++)
                {
                    Node node = Nodes[i];
                    node.Render(camera, !edgesToo, color);
                }

            }   // end of Path Render()

            private static List<Vector3> edgePts = new List<Vector3>();
            private static List<Vector3> edgeEdit = new List<Vector3>();
            private static List<Vector3> edgeSelect = new List<Vector3>();

            /// <summary>
            /// Removes the given node and any references to it from the path.  This may
            /// cause edges to also be removed which in turn may cause the path to be
            /// split into multiple paths.  How much fun is that?!?
            /// </summary>
            /// <param name="node"></param>
            public void RemoveNode(Node node)
            {
                Nodes.Remove(node);
                --numNodes;

                if (Nodes.Count > 0)
                {
                    for (int i = Edges.Count - 1; i >= 0; i--)
                    {
                        Edge e = Edges[i];
                        if (e.Node0 == node || e.Node1 == node)
                        {
                            RemoveEdge(e);
                        }
                    }

                    EnsureCoherence();
                }
                else
                {
                    // Path has no more nodes so remove it.
                    Edges.Clear();
                    WayPoint.RemovePath(node.Path);
                }

            }   // end of Path RemoveNode()

            public Node GetNearestNode(Vector2 pos)
            {
                Node nearestNode = null;
                float minDist = float.MaxValue;
                // Look at each waypoint in this path.
                for (int i = 0; i < Nodes.Count; ++i)
                {
                    Node n = Nodes[i];
                    float dist = Vector2.DistanceSquared(pos, n.Position2d);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearestNode = n;
                    }
                }
                return nearestNode;
            }

            public Edge GetNearestEdgeHandle(Vector2 pos)
            {
                Edge nearestEdge = null;
                float minDist = float.MaxValue;
                if (Edges.Count > 0)
                {
                    // Look at each edge in this path.
                    for (int i = 0; i < Edges.Count; ++i)
                    {
                        Edge e = Edges[i];
                        float dist = e.DistanceSquaredToHandle(pos);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            nearestEdge = e;
                        }
                    }
                }
                return nearestEdge;
            }

            public void Rotate(Matrix delta)
            {
                for (int i = 0; i < Nodes.Count; ++i)
                {
                    Node n = Nodes[i];

                    n.Rotate(delta);
                }
                RecalcHeights();
            }
            public void Translate(Vector3 delta)
            {
                if (delta != Vector3.Zero)
                {
                    for (int i = 0; i < Nodes.Count; ++i)
                    {
                        Node n = Nodes[i];
                        Vector3 pos = n.Position;
                        pos.X += delta.X;
                        pos.Y += delta.Y;
                        n.Height += delta.Z;
                        n.Position = pos;
                    }
                    RecalcHeights();
                }
            }

            public void ClearEdit()
            {
                Moving = false;
                Edit = false;
                Select = false;
            }

            public void IncDir()
            {
                for (int i = 0; i < Edges.Count; ++i)
                {
                    Edges[i].IncDir();
                }
            }
            public void DecDir()
            {
                for (int i = 0; i < Edges.Count; ++i)
                {
                    Edges[i].DecDir();
                }
            }

            #region Budget Junk
            internal void ClearAll()
            {
                Road.Clear();
                ClearEdges();
                ClearNodes();
            }
            internal void AddNode(Node node)
            {
                ++numNodes;
                node.Path = this;
                Nodes.Add(node);
            }
            internal void ClearNodes()
            {
                numNodes -= Nodes.Count;
                Nodes.Clear();
            }
            internal void AddEdge(Edge edge)
            {
                ++numEdges;
                Edges.Add(edge);
                RecalcAirborne();
            }
            internal void RemoveEdge(Edge edge)
            {
                --numEdges;
                Edges.Remove(edge);
                RecalcAirborne();
            }
            internal void ClearEdges()
            {
                numEdges -= Edges.Count;
                Edges.Clear();
            }
            internal static void ResetBudgets()
            {
                numNodes = 0;
                numEdges = 0;
                roadCost = 0.0f;
            }
            #endregion Budget Junk

            /// <summary>
            /// Scans through the path ensuring that all the nodes in the path are 
            /// connected by edges.  If not, it splits this path into multiple paths 
            /// of the same color.  Also checks all edges to make sure that they are 
            /// only connected to nodes in this path.  If not, they are just deleted.
            /// </summary>
            public void EnsureCoherence()
            {
                // Delete any edges that reference nodes not in this path.
                for (int i = Edges.Count - 1; i >= 0; i--)
                {
                    Edge e = Edges[i];
                    if (!Nodes.Contains(e.Node0) || !Nodes.Contains(e.Node1))
                    {
                        RemoveEdge(e);
                    }
                }

                if (Nodes.Count == 0)
                {
                    // Hmm, degenerate path.  What to do?  Should never get here.
                    throw new Exception("Degenerate waypoint path found.");
                }

                // Sweep through all nodes marking them as "not in".
                Node n = null;
                for (int i = 0; i < Nodes.Count; i++)
                {
                    n = Nodes[i];
                    n.temp = 0;
                }

                // Mark first node as in.
                n = Nodes[0];
                n.temp = 1;

                // Flood fill this out to the other connected nodes.
                Flood(n);

                // At this point, any nodes that are not marked are not connected to this path and
                // should be removed from this path and given their own path.
                bool newPaths = false;
                for (int i = Nodes.Count - 1; i > 0; i--)
                {
                    n = Nodes[i];
                    if (n.temp != 1)
                    {
                        ///Note that in this loop, no nodes or edges are created or destroyed,
                        ///just moved from one path to another. So no bookkeeping on counts
                        ///is needed.
                        
                        // Remove this node from the current path.
                        Nodes.RemoveAt(i);
                        // Create a new path for the node in the same color and add the node to the path.
                        Path path = new Path(n.Path.color);
                        n.Path = path;
                        path.Nodes.Add(n);

                        // Move any edges that originate from this node to the new path.
                        for (int j = Edges.Count - 1; j >= 0; j--)
                        {
                            Edge e = Edges[j];
                            if (e.Node0 == n)
                            {
                                Edges.RemoveAt(j);
                                n.Path.Edges.Add(e);
                            }
                        }

                        newPaths = true;
                    }
                }

                // Finally, if we removed any nodes and created new paths for
                // them we may have multiple paths that need to be merged.
                if (newPaths)
                {
                    MergePaths();
                }

                // ***ROAD - nuke the road so we'll rebuild it.
                if (Road != null)
                {
                    Road.Rebuild();
                }

            }   // end of Path EnsureCoherence()

            /// <summary>
            /// Cache which nodes are off the ground or have neighbors off the ground.
            /// </summary>
            public void RecalcAirborne()
            {
                if (Edges.Count > 0)
                {
                    foreach (Node node in Nodes)
                    {
                        node.Airborne = false;
                        node.Position = node.Position;    // Setting the position automatically resets the height.                    
                    }
                    foreach (Edge edge in Edges)
                    {
                        if (!edge.Node0.OnGround || !edge.Node1.OnGround)
                        {
                            edge.Node0.Airborne = true;
                            edge.Node1.Airborne = true;
                        }
                    }
                }
                else
                {
                    foreach (Node node in Nodes)
                    {
                        node.Airborne = !node.OnGround;
                        node.Position = node.Position;
                    }
                }
            }

            /// <summary>
            /// Recalculate heights based on terrain, rebuild visuals if necessary.
            /// </summary>
            /// <param name="onLoad">True if this is being called on initial load.  False otherwise.  Only affects setting of IsLevelDirty flag.</param>
            public void RecalcHeights(bool onLoad = false)
            {
                RecalcAirborne();

                if (Road != null)
                {
                    Road.Rebuild();
                }
                if (!onLoad)
                {
                    InGame.IsLevelDirty = true; // Force a save.
                }
            }
            #endregion Public

            #region Internal

            private void Flood(Node n)
            {
                // For each edge connected to this node, look at the other node.
                // If it's not been visited, mark it and recursively call Flood().
                for (int i = 0; i < Edges.Count; i++)
                {
                    Edge e = Edges[i];
                    if (e.Node0 == n)
                    {
                        if (e.Node1.temp != n.temp)
                        {
                            e.Node1.temp = n.temp;
                            Flood(e.Node1);
                        }
                    }
                    else if (e.Node1 == n)
                    {
                        if (e.Node0.temp != n.temp)
                        {
                            e.Node0.temp = n.temp;
                            Flood(e.Node0);
                        }
                    }
                }

            }   // end of Path Flood()

            #endregion Internal

        }   // end of class Path

    }
}
