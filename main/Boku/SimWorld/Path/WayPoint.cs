
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.SimWorld.Terra;

namespace Boku.SimWorld.Path
{
    /// 
    /// Terms:
    ///     Node:  A point in the world which also has some memory of being visited.
    ///     Edge:  A connection between two Nodes.
    ///     Path:  A connected collection of Nodes and Edges that shares a common color.  Nodes and Edges
    ///             can only belong to a single path, they cannot be shared.  If two Nodes from differing
    ///             Paths are joined by an edge then thier Paths are joined into one Path.  If a Node or 
    ///             Edge is deleted causing a Path to no longer be connected then it is split into
    ///             multiple paths.
    /// 
    /// Usage:
    ///     When an actor needs to find a nearby path you should call GetNearestPath(color, position).  This
    ///     will return the path closest to 'position' matching 'color'.
    /// 
    ///     Given the path you can then find the nearest edge by calling GetNearestEdgeFromPath(path, position).
    /// 
    ///     Once you have the edge you can decide how to use it.  If you want to find the nodes connected by
    ///     the edge they are edge.Node0 and edge.Node1.  These each have a Position accessor on them for 
    ///     getting their location.  If instead of going toward the nodes you first want to get on the path 
    ///     you can call edge.NearestPoint(position).  This will return the point on the edge closest to 'position'.
    /// 
    ///     When an actor arrives at a node it can call GetNextNodeFromPath(path, curNode, object).  This takes the
    ///     'path', finds the collection of edges connected to 'curNode' and then finds the next node which has
    ///     least recently been visited by 'object'.  There's also a GetNextEdgeFromPath(path, curNode, object) call 
    ///     if you prefer to get the edge rather than the path.
    ///
    ///     After an actor has arrived at a node and queried for the next edge to traverse it should call 
    ///     node.SetVisitedTime(object) where 'object' is probably just 'this'.  The SetVisitedTime call sets 
    ///     the last visited time and associates that time with the 'object' reference.
    /// 
    ///     Note that both the SetVisited and GetNext* calls can be passed a null for 'object'.  This will cause all 
    ///     the actors' visited time information to be shared.
    /// 
    ///     An implication of this whole thing is that actors will need to remember what path they are on and what
    ///     node they are heading toward.  (And the edge if you're having them go toward the edge before going 
    ///     toward the node).  Becoming "distracted" and forgetting what path you're on is fine but it's not something
    ///     that you want to do every frame.
    /// 
    /// <summary>
    /// The WayPoint class provides a home for the static functions that deal with waypoints
    /// and also provides a namespace wrapper since the inside classes have such generic names.
    /// </summary>
    public partial class WayPoint : INeedsDeviceReset
    {
        #region Members
        private static float radius = 0.5f;     // Radius of waypoint nodes.
        private static float sphereHeight = 0.1f;     // Height above terrain for waypoint nodes.

        private static Random rnd = new Random();

        private static List<Path> paths = new List<Path>();

        private static float alpha = 1.0f;

        private static int numNodes = 0;
        private static int numEdges = 0;
        private static float roadCost = 0.0f;
        #endregion Members

        #region Accessors
        public static List<Path> Paths
        {
            get { return paths; }
        }
        public static float Alpha
        {
            get { return alpha; }
            set { alpha = value; }
        }

        /// <summary>
        /// Bookkeeping, total number of nodes currently made.
        /// </summary>
        public static int NumNodes
        {
            get { return numNodes; }
        }
        /// <summary>
        /// Bookkeeping, total number of edges currently made.
        /// </summary>
        public static int NumEdges
        {
            get { return numEdges; }
        }
        public static float RoadCost
        {
            get { return roadCost; }
            set 
            { 
                roadCost = value;
                if (roadCost < 0)
                    roadCost = 0;
            }
        }
        #endregion

        #region Public

        /// <summary>
        /// Constructor
        /// </summary>
        private WayPoint()
        {
        }

        public static void ClearPaths()
        {
            Paths.Clear();
            Path.ResetBudgets();
        }
        /// <summary>
        /// Return the total weighted cost for all paths in the scene.
        /// </summary>
        public static float TotalCost
        {
            get
            {
                const float kCostPath = 0.0f;
                const float kCostNode = 0.5f;
                const float kCostEdge = 0.5f;

                Debug.Assert(NumNodes >= 0);
                Debug.Assert(NumEdges >= 0);
                Debug.Assert(RoadCost >= 0);

                return Paths.Count * kCostPath
                    + NumNodes * kCostNode
                    + NumEdges * kCostEdge
                    + RoadCost;
            }
        }
        public static void ResetCost()
        {
            RoadCost = 0.0f;
        }

        /// <summary>
        /// Initialize statics.
        /// </summary>
        public static void Init(bool immediate)
        {
        }   // end of WayPoint Init()

        public static void Fini()
        {
        }

        public static void RemovePath(Path path)
        {
            path.ClearAll();
            Paths.Remove(path);
        }

        /// <summary>
        /// Entry point to render all the existing paths.
        /// </summary>
        /// <param name="camera">Current camera.</param>
        public static void RenderPaths(Camera camera, bool forShadow)
        {
            for (int i = 0; i < Paths.Count; i++)
            {
                Path path = Paths[i];
                bool isMoving = path.Moving; // InGame.IsMoving(path);
                bool isBlank = path.RoadName == "";
                bool isThings = (path.Road != null) && !(path.Road.BlocksTravel && path.Road.IsGround);
                if (!forShadow || isMoving || isBlank)
                {
                    bool edgesToo = isMoving || isBlank || isThings;

                    path.Render(camera, edgesToo);
                }
            }
        }   // end of WayPoint Render()

        /// <summary>
        /// Pass through render to the road system.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="shadowTexture"></param>
        public static void RenderRoads(Camera camera)
        {
            for (int i = 0; i < Paths.Count; i++)
            {
                Path path = Paths[i];
                if (path.Road != null)
                {
                    path.Road.Render(camera);
                }
            }
        }

        /// <summary>
        /// Get height at position if it is on a road.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static bool GetHeight(Vector3 pos, ref float height)
        {
            bool ret = false;
            foreach (Path path in paths)
            {
                if (path.Road != null)
                {
                    if (path.Road.GetHeight(pos, ref height))
                    {
                        ret = true;
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Get height at position if it is on a road.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static bool GetHeight(Vector3 pos, ref float height, ref Classification.Colors color)
        {
            bool ret = false;
            foreach (Path path in paths)
            {
                if (path.Road != null)
                {
                    if (path.Road.GetHeight(pos, ref height))
                    {
                        color = path.Color;
                        ret = true;
                    }
                }
            }
            return ret;
        }

        public static bool GetHeightAndNormal(Vector3 pos, ref float maxHeight, ref Vector3 normal)
        {
            bool ret = false;
            maxHeight = 0.0f;
            foreach (Path path in Paths)
            {
                if (path.Road != null)
                {
                    if (path.Road.GetHeightAndNormal(pos, ref maxHeight, ref normal))
                    {
                        ret = true;
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Collision check against a sphere.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="collisions"></param>
        /// <returns></returns>
        public static bool GetCollisions(Vector3 center, float radius, List<Road.CollisionInfo> collisions)
        {
            foreach (Path path in Paths)
            {
                if (path.Road != null)
                {
                    path.Road.GetCollisions(center, radius, collisions);
                }
            }
            return collisions.Count > 0;
        }

        /// <summary>
        /// Line of sight test.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="hitBlock"></param>
        /// <returns></returns>
        public static bool Blocked(Vector3 src, Vector3 dst, ref Terrain.HitBlock hitBlock)
        {
            bool ret = false;
            foreach (Path path in Paths)
            {
                if (path.Road != null)
                {
                    if (path.Road.Blocked(src, dst, ref hitBlock))
                    {
                        dst = hitBlock.Position;
                        ret = true;
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Tell all elements culled from LeavesRoad test to rejoin.
        /// </summary>
        public static void ResetAbstain()
        {
            foreach (Path path in Paths)
            {
                if (path.Road != null)
                {
                    path.Road.ResetAbstain();
                }
            }
        }

        /// <summary>
        /// Find where the ray leaves the road.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="hitBlock"></param>
        /// <returns></returns>
        public static bool LeavesRoad(Vector3 src, Vector3 dst, ref Terrain.HitBlock hitBlock)
        {
            bool ret = false;
            foreach (Path path in Paths)
            {
                if (path.Road != null)
                {
                    if (path.Road.LeavesRoad(src, dst, ref hitBlock))
                    {
                        /// This isn't quite right, really should be like
                        /// Road.LeavesRoad(), but might be close enough,
                        /// and is definitely cheaper.
                        src = hitBlock.Position;
                        ret = true;
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Merges all the content of the source path into the destination path.
        /// </summary>
        /// <param name="dst">destination path</param>
        /// <param name="src">source path</param>
        static private void MergePaths(Path dst, Path src)
        {
            ///Note that no nodes or edges are created or destroyed, just moved from one
            ///path to another. So no counts bookkeeping here.
            // Move all nodes.
            for (int i = 0; i < src.Nodes.Count; i++)
            {
                Node n = src.Nodes[i];
                n.Path = dst;
                dst.Nodes.Add(n);
            }

            // Move all edges.
            for (int i = 0; i < src.Edges.Count; i++)
            {
                Edge e = src.Edges[i];
                dst.Edges.Add(e);
            }

            // Remove src from list of paths and clean up.
            Paths.Remove(src);
            src.Nodes.Clear();
            src.Edges.Clear();

        }   // end of WayPoint MergePaths()

        /// <summary>
        /// Scans through all the edges of all the paths.  If if finds an edge 
        /// connecting two nodes in different paths then it merges those paths.
        /// </summary>
        static private void MergePaths()
        {
            bool merged = false;
            for (int i = 0; i < Paths.Count; i++)
            {
                Path p = Paths[i];
                for (int j = 0; j < p.Edges.Count; j++)
                {
                    Edge e = p.Edges[j];
                    if (e.Node0.Path != p)
                    {
                        MergePaths(p, e.Node0.Path);
                        merged = true;
                        break;
                    }
                    else if (e.Node1.Path != p)
                    {
                        MergePaths(p, e.Node1.Path);
                        merged = true;
                        break;
                    }
                }

                if (merged)
                {
                    i=-1;   // We merged two paths so we have to start over.
                    merged = false;
                }
            }
        }   // end of WayPoint MergePaths()

        /// <summary>
        /// Just adds the nodes' shadows to the shadow list.
        /// </summary>
        /// <param name="camera"></param>
        public static void UpdateAllPaths(Camera camera)
        {
        }   // end of WayPoint UpdateAllPaths()

        /// <summary>
        /// To be called when the terrain changes to make sure that
        /// the waypoints stay where they should be.
        /// </summary>
        public static void RecalcWayPointNodeHeights()
        {
            if (Paths != null)
            {
                for (int i = 0; i < Paths.Count; i++)
                {
                    Path p = Paths[i];

                    p.RecalcHeights();
                }
            }
        }   // end WayPoint RecalcWayPointNodeHeights()

        /// <summary>
        /// Force a rebuild of all geometry.
        /// </summary>
        public static void RebuildAll()
        {
            if (Paths != null)
            {
                foreach (Path path in Paths)
                {
                    if (path.Road != null)
                    {
                        path.Road.Rebuild();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the waypoint nearest to the given position.  May be from any path.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static Node GetNearestWayPoint(Vector3 pos)
        {
            return GetNearestWayPoint(new Vector2(pos.X, pos.Y));
        }   // end of WayPoint GetNearestWayPoint()

        /// <summary>
        /// Gets the waypoint nearest to the given position.  May be from any path.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static Node GetNearestWayPoint(Vector2 pos)
        {
            Node nearestNode = null;
            float minDist = float.MaxValue;

            for (int i = 0; i < Paths.Count; i++)
            {
                Path p = Paths[i];

                Node n = p.GetNearestNode(pos);
                if (n != null)
                {
                    float dist = n.DistanceSquared(pos);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearestNode = n;
                    }
                }
            }

            return nearestNode;
        }   // end of WayPoint GetNearestWayPoint()

        /// <summary>
        /// Gets the edge nearest to the given position.  May be from any path.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static Edge GetNearestEdgeHandle(Vector3 pos)
        {
            return GetNearestEdgeHandle(new Vector2(pos.X, pos.Y));
        }   // end of WayPoint GetNearestEdge()

        /// <summary>
        /// Gets the edge nearest to the given position.  May be from any path.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static Edge GetNearestEdgeHandle(Vector2 pos)
        {
            Edge nearestEdge = null;
            float minDist = float.MaxValue;

            for (int i = 0; i < Paths.Count; i++)
            {
                Path p = Paths[i];

                Edge e = p.GetNearestEdgeHandle(pos);
                if (e != null)
                {
                    float dist = e.DistanceSquaredToHandle(pos);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearestEdge = e;
                    }
                }
            }

            return nearestEdge;
        }   // end of WayPoint GetNearestEdge()

        /// <summary>
        /// This not only creates a new node in the path but also creates an edge connecting
        /// the new node with the currently active node and then changing the currently
        /// active node to be this newly created node.
        /// </summary>
        /// <param name="colorIndex"></param>
        /// <param name="position"></param>
        public static WayPoint.Node CreateNewNode(Node from, int colorIndex, Vector3 position)
        {
            Node n = new Node(position);

            if (from != null)
            {
                // Add to existing path.
                from.Path.AddNode(n);

                // Add an edge connecting the nodes.
                Edge e = new Edge(from, n);

                n.Height = from.Height;
            }
            else
            {
                Path p = new Path(ColorPalette.GetColorFromIndex(colorIndex));
                p.AddNode(n);

                n.Height = n.Path.DefaultNodeHeight;
            }

            n.Path.RecalcHeights();

            return n;

        }   // end of WayPoint CreateNewNode()

        /// <summary>
        /// Join two nodes with a new edge.
        /// </summary>
        /// <param name="n0"></param>
        /// <param name="n1"></param>
        public static Edge CreateNewEdge(Node n0, Node n1)
        {
            // Note the c'tor takes care of adding the new edge to
            // the right path and merging paths if two have been 
            // joined by the creation of the new edge.
            Edge e = new Edge(n0, n1);

            // MAFROAD - hmmm...
            if (n0.Path.Road != null)
            {
                n0.Path.Road.Rebuild();
            }
            return e;
        }   // end of WayPoint CreateNewEdge()

        /// <summary>
        /// Create an edge with the given direction.
        /// </summary>
        /// <param name="n0"></param>
        /// <param name="n1"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static Edge CreateNewEdge(Node n0, Node n1, Edge.Direction direction)
        {
            Edge e = CreateNewEdge(n0, n1);
            e.Dir = direction;
            return e;
        }

        /// <summary>
        /// Get the color of the currently active node.  If the active node is null
        /// then it gets the color of the current color palette selection.
        /// </summary>
        /// <returns></returns>
        public static Vector4 GetCurrentColor()
        {
            int colorIndex = InGame.inGame.shared.curObjectColor;
            Classification.Colors color = ColorPalette.GetColorFromIndex(colorIndex);
            return Classification.ColorVector4(color);
        }   // end of WayPoint GetActiveColor()

        /// <summary>
        /// Returns the path nearest the position of the right color.  May return null if none exit.
        /// "Nearest" is defined as the path containing an edge which is nearest to the input point.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Path GetNearestPath(Classification.Colors color, Vector3 position)
        {
            Path nearestPath = null;
            float distSquared = float.MaxValue;
            for (int i = 0; i < Paths.Count; i++)
            {
                Path p = Paths[i];
                if (p.Color == color || color == Classification.Colors.NotApplicable)
                {
                    if (p.Edges.Count == 0)
                    {
                        // This path must only have a single node 
                        // so check that instead of the edges.
                        Node n = p.Nodes[0];
                        float dist2 = Vector3.DistanceSquared(n.Position, position);
                        if (dist2 < distSquared)
                        {
                            distSquared = dist2;
                            nearestPath = p;
                        }
                    }
                    else
                    {
                        for (int j = 0; j < p.Edges.Count; j++)
                        {
                            Edge e = p.Edges[j];
                            float dist2 = e.DistanceSquared(position);

                            if (dist2 < distSquared)
                            {
                                nearestPath = p;
                                distSquared = dist2;
                            }
                        }
                    }
                }   // end if path is the right color.
            }

            return nearestPath;

        }   // end of WayPoint GetNearestPath()

        /// <summary>
        /// Given a path and a point, returns the edge in the path nearest to the position.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Edge GetNearestEdgeFromPath(Path path, Vector2 position)
        {
            Edge nearestEdge = null;
            float distSquared = float.MaxValue;

            // This path may only have a single node.  If so, create a fake edge to return.
            if (path.Edges.Count == 0)
            {
                Node n = path.Nodes[0];
                nearestEdge = new Edge(n, n);
            }
            else
            {
                for (int j = 0; j < path.Edges.Count; j++)
                {
                    Edge e = path.Edges[j];
                    float dist2 = e.DistanceSquared(position);

                    if (dist2 < distSquared)
                    {
                        nearestEdge = e;
                        distSquared = dist2;
                    }
                }
            }

            return nearestEdge;
        }   // end of WayPoint GetNearestEdgeFromPath()

        /// <summary>
        /// Returns the next node in the path.  "Next" is calculated by taking all the possible 
        /// edges, eliminating the one the actor just came in on and then randomly selecting among 
        /// the remaining ones biasing the selection toward nodes which have been visited least 
        /// recently.  If a path has only a single node this will always be returned.
        /// </summary>
        /// <param name="path">The path the actor is on.</param>
        /// <param name="curNode">The current node the actor is at.  This should not be null.  If the actor is just starting on the path then GetNearestEdgeFromPath() should be used to get a starting point.</param>
        /// <param name="actor">The actor.  May be null.</param>
        /// <returns></returns>
        public static Node GetNextNodeFromPath(Path path, Node curNode, Object actor)
        {
            Edge e = GetNextEdgeFromPath(path, curNode, actor);

            Node result = curNode;

            if (e != null)
            {
                result = e.Node0 == curNode ? e.Node1 : e.Node0;
            }
            // else just return curNode since we have a path with only a single node.

            return result;
        }   // end of WayPoint GetNextNodeFromPath()

        /// <summary>
        /// Returns the next edge in the path.  "Next" is calculated by taking all the possible 
        /// edges, eliminating the one the actor just came in on and then randomly selecting among 
        /// the remaining ones biasing the selection toward nodes which have been visited least 
        /// recently.  If a path has only a single node this _will_ return null.  
        /// 
        /// Note, this should be called before calling SetVisitedTime() in the actor otherwise
        /// the actor's previous node will be the current one and not the actual previous one.
        /// TODO Refactor this because this is a lame restriction.
        /// </summary>
        /// <param name="path">The path the actor is on.</param>
        /// <param name="curNode">The current node the actor is at.  This should not be null.  If the actor is just starting on the path then GetNearestEdgeFromPath() should be used to get a starting point.</param>
        /// <param name="actor">The actor.  May be null.</param>
        /// <returns></returns>
        public static Edge GetNextEdgeFromPath(Path path, Node curNode, Object actor)
        {
            Edge nextEdge = null;

            Node prevNode = Node.PrevNode.GetPrevNode(actor);

            // Create a temp list with all the edges connecting to the 
            // curNode except for the edge we just came in from.
            List<Edge> edges = scratchEdges;
            edges.Clear();
            Edge prevEdge = null;
            for (int i = 0; i < path.Edges.Count; i++)
            {
                Edge e = path.Edges[i];
                if (e.Node0 == curNode || e.Node1 == curNode)
                {
                    if (!(prevNode == e.Node0 || prevNode == e.Node1))
                    {
                        if (e.CanLeaveNode(curNode))
                        {
                            edges.Add(e);
                        }
                    }
                    else
                    {
                        if (e.CanLeaveNode(curNode))
                        {
                            prevEdge = e;
                        }
                    }
                }
            }

            if (edges.Count == 0)
            {
                // If the count is 0 that means that we're at a dead end and 
                // need to return along the edge we came in on.  If there's no
                // edges in the path then create a fake edge and return that.
                if (path.Edges.Count > 0)
                {
                    nextEdge = prevEdge;
                }
                else
                {
                    nextEdge = null;
                }
            }
            else if(edges.Count == 1)
            {
                // We have only one candidate edge so pick it.
                nextEdge = edges[0];
            }
            else
            {
                // Get time at previous node.
                double time = 0.0f;
                if (prevNode != null)
                {
                    time = prevNode.GetLastVisitedTime(actor);
                }

                double totalTime = 0.0;
                // Sum delta times from all possible nodes.
                for (int i = 0; i < edges.Count; i++)
                {
                    Edge e = edges[i];
                    if (e.Node0 == curNode)
                    {
                        totalTime += Math.Max(0.0f, time - e.Node1.GetLastVisitedTime(actor));
                    }
                    else if (e.Node1 == curNode) // Should always be true...
                    {
                        totalTime += Math.Max(0.0f, time - e.Node0.GetLastVisitedTime(actor));
                    }
                }

                // Pick a random number scaled by the total time.
                double rndTime = rnd.NextDouble() * totalTime;

                // Figure out which edge this corresponds to.
                double accumulatedTime = 0.0;
                for (int i = 0; i < edges.Count; i++)
                {
                    Edge e = edges[i];
                    if (e.Node0 == curNode)
                    {
                        accumulatedTime += Math.Max(0.0f, time - e.Node1.GetLastVisitedTime(actor));
                    }
                    else if (e.Node1 == curNode) // Should always be true...
                    {
                        accumulatedTime += Math.Max(0.0f, time - e.Node0.GetLastVisitedTime(actor));
                    }

                    if (accumulatedTime >= rndTime)
                    {
                        nextEdge = e;
                        break;
                    }
                }

            }

            return nextEdge;

        }   // end of WayPoint GetNextEdgeFromPath()
        private static List<Edge> scratchEdges = new List<Edge>();

        /// <summary>
        /// Given an edge and an actor (but without a current node), find the next node that
        /// should be visited based on least recently visited time.
        /// </summary>
        /// <param name="edge">The edge the actor is on</param>
        /// <param name="actor">The actor. maybe null</param>
        /// <returns>The next node on the edge</returns>
        public static Node GetNextNodeFromEdge(Edge edge, Object actor)
        {
            Node nextNode = null;
            if (edge.Node0.GetLastVisitedTime(actor) < edge.Node1.GetLastVisitedTime(actor))
            {
                nextNode = edge.Node0;
            }
            else
            {
                nextNode = edge.Node1;
            }
            return nextNode;
        }

        /// <summary>
        /// Load content stuff.
        /// </summary>
        /// <param name="graphics"></param>
        public void LoadContent(bool immediate)
        {
            Init(immediate);
        }

        /// <summary>
        /// Load graphics device dependent stuff.
        /// </summary>
        /// <param name="graphics"></param>
        public void InitDeviceResources(GraphicsDevice device)
        {
        }

        /// <summary>
        /// Dump graphics device dependent stuff.
        /// </summary>
        public void UnloadContent()
        {
            Fini();
        }

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
        }

        #endregion Public

    }   // end of class WayPoint

}   // end of namespace Boku.SimWorld






