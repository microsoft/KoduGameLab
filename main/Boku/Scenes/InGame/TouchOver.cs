// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using KoiX;
using KoiX.Input;
using KoiX.Scenes;

using Boku.Base;
using Boku.Common;
using Boku.Common.Gesture;
using Boku.SimWorld.Path;
using Boku.Common.ParticleSystem;
using Boku.Audio;


namespace Boku
{
    /// <summary>
    /// UpdateObject for InGame -> EditObject
    /// </summary>
    public partial class InGame : GameObject, INeedsDeviceReset
    {
        public partial class WayPointEdit
        {
            /// <summary>
            /// The implementation of the touch version of waypoint editing.
            /// </summary>
            public class TouchOver
            {
                #region Members
                private WayPoint.Node fromNode = null;

                private float height = 0;
                private bool actOnPath = false;

                private Vector2 moveFrom = Vector2.Zero;
                private Vector2 moveTo = Vector2.Zero;
                private float clickTimer = 0.0f;
                private float moveTimer = 0.0f;
                private float skeletonTimer = 0.0f;

                private float kMoveToTime = 0.25f;
                private float kDoubleClickTime = 0.25f;
                private float kSkeletonTime = 0.5f;

                private enum Mode
                {
                    None,
                    Over,
                    Drag,
                    Raise,
                    Add,
                    ReCenter
                };
                private Mode mode = Mode.None;

                public float distance = float.MaxValue;
                public WayPoint.Node node = null;
                public WayPoint.Edge edge = null;


                #endregion Members

                #region Accessors

                /// <summary>
                /// True if not moused over anything waypoint.
                /// </summary>
                public bool Empty
                {
                    get { return (node == null) && (edge == null); }
                }

                /// <summary>
                /// True if moused over anything waypoint.
                /// </summary>
                public bool Over
                {
                    get { return (node != null) || (edge != null); }
                }

                /// <summary>
                /// Input required to start dragging a path or subpath.
                /// </summary>
                public bool StartDragging
                {
                    get
                    {
                        return TouchGestureManager.Get().DragGesture.IsDragging && mode!=Mode.Drag;
                    }
                }
                /// <summary>
                /// Input required to continue dragging a path or subpath.
                /// </summary>
                public bool ContinueDragging
                {
                    get
                    {
                        return TouchGestureManager.Get().DragGesture.IsDragging && mode==Mode.Drag;
                    }
                }
                /// <summary>
                /// Input required to start raising/lowering a path or subpath.
                /// </summary>
                public bool StartRaising
                {
                    get { return false; /* LowLevelMouseInput.Left.WasPressed && KeyboardInput.ShiftIsPressed && !KeyboardInput.IsPressed(Keys.Space); */ }
                }
                /// <summary>
                /// Input required to continue raising/lowering a path or sub-path.
                /// </summary>
                public bool ContinueRaising
                {
                    get { return false; /* LowLevelMouseInput.Left.IsPressed && KeyboardInput.ShiftIsPressed && !KeyboardInput.IsPressed(Keys.Space); */ }
                }

                /// <summary>
                /// Input required to start any kind of waypoint moving.
                /// </summary>
                public bool StartMoving
                {
                    get { return StartDragging || StartRaising; }
                }

                /// <summary>
                /// Input required to continue any kind of waypoint moving.
                /// </summary>
                public bool ContinueMoving
                {
                    get { return ContinueDragging || ContinueRaising; }
                }

                /// <summary>
                /// Are we actively moving something?
                /// </summary>
                public bool Moving
                {
                    get { return Over && ContinueMoving; }
                }

                /// <summary>
                /// Do we have a path we can adjust the type or color of?
                /// </summary>
                public bool Adjusting
                {
                    get { return Over || Adding; }
                }

                /// <summary>
                /// Are we currently adding nodes to a path?
                /// </summary>
                public bool Adding
                {
                    get { return mode == Mode.Add; }
                }

                /// <summary>
                /// The path (if any) we are currently mousing about with. May be null.
                /// </summary>
                public WayPoint.Path Path
                {
                    get
                    {
                        return fromNode != null
                            ? fromNode.Path
                            : node != null
                                ? node.Path
                                : edge != null
                                    ? edge.Path
                                    : null;
                    }
                }

                /// <summary>
                /// Are actions supposed to act on the whole path?
                /// </summary>
                public bool ActOnPath
                {
                    get { return actOnPath; }
                    set { actOnPath = value; }
                }

                #endregion Accessors

                #region Public

                /// <summary>
                /// Entry point for each frame's update of waypoint editing.
                /// </summary>
                /// <param name="inGame"></param>
                /// <param name="camera"></param>
                public void Update(InGame inGame, Camera camera)
                {
                    CheckDirection();
                    if (mode == Mode.ReCenter)
                    {
                        /// We're transitioning position to new spot.
                        AdvanceReCenter();
                    }
                    else
                    {
                        if (!ContinueMoving && TouchInput.WasTouched && TouchInput.TouchCount == 1 && InGame.inGame.TouchEdit.HasNonUITouch())  
                        {
                            //Only update "over" target when a tap was registered
                            FindOver(camera);
                        }
                        if (!Empty)
                        {
                            CheckReCenter();
                        }
                        if (Path != null)
                        {
                            Path.Moving = skeletonTimer > 0;
                        }

                    }
                    CheckMode();
                    clickTimer -= Time.WallClockFrameSeconds;
                    skeletonTimer -= Time.WallClockFrameSeconds;

                    DoDrag(camera);
                    DoAdd(camera);
                }

                /// <summary>
                /// Allow a shot at affecting the world's cursor position.
                /// </summary>
                /// <param name="pos"></param>
                /// <returns></returns>
                public Vector2 DoCursor(Vector2 pos)
                {
                    if (mode == Mode.ReCenter)
                    {
                        pos = moveTo + moveTimer / kMoveToTime * (moveFrom - moveTo);
                    }
                    return pos;
                }

                /// <summary>
                /// Return the appropriate help id when interacting with waypoints.
                /// Will return null if dormant.
                /// </summary>
                /// <returns></returns>
                public string UpdateHelpOverlay()
                {
                    string helpID = null;
                    if (mode == Mode.Add)
                    {
                        helpID = node == fromNode
                            ? "EditWayPointEndFree"
                            : "EditWayPointAddFree";
                    }
                    else if (Over)
                    {
                        if (actOnPath)
                        {
                            Debug.Assert(Path != null, "If we're over something, it must belong to a path");
                            helpID = "EditWayPointPathFree";
                        }
                        else if (node != null)
                        {
                            helpID = "EditWayPointNodeFree";
                        }
                        else
                        {
                            Debug.Assert(edge != null, "If we're over something, it must be a node or an edge");
                            helpID = "EditWayPointEdgeFree";
                        }
                    }
                    return helpID;
                }

                /// <summary>
                /// Start a new path, and go into Add mode to continue adding more nodes
                /// until cancelled. Assumes
                /// </summary>
                /// <param name="pos"></param>
                /// <param name="objColorIndex"></param>
                public void NewPath(Vector3 pos, int objColorIndex)
                {
                    fromNode = WayPoint.CreateNewNode(null, objColorIndex, pos);
                    //in touch mode, also auto-select newly created nodes
                    node = fromNode;
                    mode = Mode.Add;
                    Foley.PlayMakePath();
                    Changed();
                }

                /// <summary>
                /// Start adding to current path.
                /// </summary>
                public void StartAdding(WayPoint.Node from)
                {
                    fromNode = from;
                    mode = Mode.Add;
                }

                /// <summary>
                /// Break out of add mode.
                /// </summary>
                public void StopAdding()
                {
                    mode = Mode.None;
                    fromNode = null;
                }

                /// <summary>
                /// Reset our state
                /// </summary>
                public void Clear()
                {
                    distance = float.MaxValue;
                    node = null;
                    edge = null;
                }

                #endregion Public

                #region Internal

                /// <summary>
                /// Set the object (either node or edge) as current mouse-over object.
                /// No checks done, assumes this is the closest object we are moused-over.
                /// </summary>
                /// <param name="obj"></param>
                /// <param name="pos"></param>
                /// <param name="dist"></param>
                private void Set(object obj, Vector3 pos, float dist)
                {
                    this.distance = dist;
                    this.height = pos.Z;
                    if ((this.node = obj as WayPoint.Node) == null)
                    {
                        this.edge = obj as WayPoint.Edge;
                    }
                    Debug.Assert((obj == null) || (node != null) || (edge != null), "Something not a node nor edge passed in?");
                }

                /// <summary>
                /// If this object is closer than current mouse-over object, replace the current.
                /// </summary>
                /// <param name="obj"></param>
                /// <param name="src"></param>
                /// <param name="dst"></param>
                private void ClosestCheck(object obj, Vector3 src, Vector3 dst)
                {
                    float dist = Vector3.Distance(src, dst);
                    if (dist < this.distance)
                    {
                        if (!TouchEdit.MouseTouchHitInfo.HaveActor
                            || (Vector3.DistanceSquared(
                            TouchEdit.MouseTouchHitInfo.ActorPosition, src) > dist * dist))
                        {
                            Set(obj, dst, dist);
                        }
                    }
                }

                /// <summary>
                /// Find the closest edge or node that we are moused-over.
                /// Loops over all edges and nodes in the scene. 
                /// </summary>
                /// <param name="camera"></param>
                private void FindOver(Camera camera)
                {
                    WayPoint.Path oldPath = Path;
                    Clear();
                    int pathCount = WayPoint.Paths.Count;
                    for (int iPath = 0; iPath < pathCount; ++iPath)
                    {
                        WayPoint.Path path = WayPoint.Paths[iPath];

                        int nodeCount = path.Nodes.Count;
                        for (int iNode = 0; iNode < nodeCount; ++iNode)
                        {
                            bool asRoad = false;
                            WayPoint.Node node = path.Nodes[iNode];
                            Vector3 pos = node.RenderPosition(asRoad);

                            CheckObject(camera, pos, node);
                        }

                        int edgeCount = path.Edges.Count;
                        for (int iEdge = 0; iEdge < edgeCount; ++iEdge)
                        {
                            bool asRoad = false;

                            WayPoint.Edge edge = path.Edges[iEdge];

                            Vector3 pos = (edge.Node0.RenderPosition(asRoad) + edge.Node1.RenderPosition(asRoad)) * 0.5f;

                            CheckObject(camera, pos, edge);

                        }
                    }
                    if ((oldPath != null) && (oldPath != Path))
                    {
                        oldPath.ClearEdit();
                    }
                }

                /// <summary>
                /// Check a node or edge to see if we're moused-over it and it's closer
                /// than the current.
                /// </summary>
                /// <param name="camera"></param>
                /// <param name="objPos"></param>
                /// <param name="obj"></param>
                private void CheckObject(Camera camera, Vector3 objPos, object obj)
                {
                    if (LOSCheck(camera, objPos))
                    {
                        ClosestCheck(obj, camera.From, objPos);
                    }
                }

                /// <summary>
                /// Advance the clock controlling recentering.
                /// </summary>
                private void AdvanceReCenter()
                {
                    /// A left click can cancel the ReCenter.
                    if (TouchInput.WasTouched)
                    {
                        mode = Mode.None;
                        fromNode = null;
                    }
                    moveTimer -= Time.GameTimeFrameSeconds;
                    if (moveTimer <= 0)
                    {
                        moveTimer = 0;
                        mode = Mode.None;
                        fromNode = null;
                    }
                }

                /// <summary>
                /// Look for a double click to start a recenter operation.
                /// </summary>
                private void CheckReCenter()
                {
                    if (TouchInput.WasTouched)
                    {
                        if (clickTimer <= 0.0f)
                        {
                            clickTimer = kDoubleClickTime;
                        }
                        else
                        {
                            ReCenter();
                            clickTimer = 0;
                        }
                    }
                }

                /// <summary>
                /// Setup to recenter to the input position.
                /// </summary>
                /// <param name="pos"></param>
                /// <returns></returns>
                private bool ReCenter(Vector2 pos)
                {
                    moveTo = pos;
                    moveFrom = inGame.Cursor3D.Position2d;
                    moveTimer = kMoveToTime;
                    mode = Mode.ReCenter;

                    return true;
                }

                /// <summary>
                /// Setup to recenter on the currently moused-over node or edge.
                /// No-op if there is no current node or edge.
                /// </summary>
                /// <returns></returns>
                private bool ReCenter()
                {
                    if (Path != null)
                    {
                        Vector2 pos = Vector2.Zero;
                        if (node != null)
                        {
                            pos = node.Position2d;
                        }
                        else if (edge != null)
                        {
                            pos = edge.HandlePosition2d;
                        }
                        return ReCenter(pos);
                    }
                    return false;
                }


                /// <summary>
                /// Look to see if we should be adding nodes, and if so, 
                /// if we should add one now.
                /// </summary>
                /// <param name="camera"></param>
                private void DoAdd(Camera camera)
                {
                    CheckAddDelete();
                    if (mode == Mode.Add)
                    {
                        if (act.PutNodeDone || act.PutNodeGo)
                        {
                            DoAddNode(camera);
                        }
                        if (act.PutNodeDone)
                        {
                            mode = Mode.None;
                            fromNode = null;
                        }
                    }
                }

                /// <summary>
                /// Actually add a node to the scene.
                /// </summary>
                /// <param name="camera"></param>
                private void DoAddNode(Camera camera)
                {
                    Debug.Assert(fromNode != null);

                    if (inGame.UnderBudget)
                    {
                        if (fromNode != node)
                        {
                            Click(ModeChange.Add);
                            if (node != null)
                            {
                                WayPoint.CreateNewEdge(fromNode, node);
                                fromNode = node;
                            }
                            else
                            {
                                Vector3 pos = AddPosition(camera, fromNode);
                                fromNode = WayPoint.CreateNewNode(fromNode, inGame.shared.curObjectColor, pos);
                                //in touch mode, when we place a new node, have it highlighted, since there is no mouse over equivalent
                                node = fromNode;
                            }
                            Changed();
                        }
                        else if (TouchGestureManager.Get().DoubleTapGesture.WasRecognized)
                        {
                            //allow finish by double tapping selected node
                            node = null;
                            mode = Mode.None;
                            fromNode = null;
                        }
                    }
                    else
                    {
                        Click(ModeChange.NoBudget);
                    }
                }

                /// <summary>
                /// Find the correct position to add a node.
                /// </summary>
                /// <param name="camera"></param>
                /// <param name="from"></param>
                /// <returns></returns>
                private Vector3 AddPosition(Camera camera, WayPoint.Node from)
                {
                    bool asRoad = false;
                    float height = from.RenderPosition(asRoad).Z;
                    Vector2 pos = Vector2.Zero;
                    TouchContact touch = TouchInput.GetOldestTouch();
                    if (touch != null)
                    {
                        pos = touch.position;
                    }

                    Vector3 hit = TouchEdit.FindAtHeight(camera, TouchInput.GetAsPoint(pos), height);

                    return hit;
                }

                /// <summary>
                /// Drag the current node/edge/path by mouse movement. Includes raise/lower
                /// as well as horizontal drag.
                /// </summary>
                /// <param name="camera"></param>
                private void DoDrag(Camera camera)
                {

                    Vector2 pos = Vector2.Zero;
                    Vector2 prevPos = Vector2.Zero;
                    TouchContact touch = TouchInput.GetOldestTouch();
                    if (touch != null)
                    {
                        pos = touch.position;
                        prevPos = touch.previousPosition;
                    }

                    Vector3 newPosition = Vector3.Zero;
                    if (mode == Mode.Drag)
                    {
                        newPosition = TouchEdit.FindAtHeight(camera, TouchInput.GetAsPoint(TouchGestureManager.Get().DragGesture.DragPosition), height);
                    }

                    Vector3 delta = Vector3.Zero;
                    if (mode == Mode.Raise)
                    {
                        float dheight = LowLevelMouseInput.DeltaPosition.Y;
                        float kUpDownSpeed = -0.002f;
                        dheight *= kUpDownSpeed;

                        Vector3 dragPos = camera.ActualFrom;
                        bool asRoad = false;
                        if (node != null)
                        {
                            dragPos = node.RenderPosition(asRoad);
                        }
                        if (edge != null)
                        {
                            dragPos = (edge.Node0.RenderPosition(asRoad) + edge.Node1.RenderPosition(asRoad)) * 0.5f;
                        }

                        float dist = Vector3.Distance(dragPos, camera.ActualFrom);
                        dheight *= dist;
                        delta.Z = dheight;
                    }

                    if (newPosition != Vector3.Zero && Path != null)
                    {
                        if (actOnPath)
                        {
                            if (node != null)
                            {
                                Path.Translate(newPosition - node.Position);
                            }
                            else if (edge != null)
                            {
                                Path.Translate(newPosition - edge.HandlePosition);
                            }
                        }
                        else if (node != null)
                        {
                            node.Translate(newPosition - node.Position);
                        }
                        else if (edge != null)
                        {
                            edge.Translate(newPosition - edge.HandlePosition);
                        }

                        skeletonTimer = kSkeletonTime;
                    }

                    if ((delta != Vector3.Zero) && (Path != null))
                    {
                        if (actOnPath)
                        {
                            MovePath(Path, delta);
                        }
                        else if (node != null)
                        {
                            MoveNode(node, delta);
                        }
                        else if (edge != null)
                        {
                            MoveEdge(edge, delta);
                        }

                        skeletonTimer = kSkeletonTime;
                    }
                }

                /// <summary>
                /// Move the node by specified amount.
                /// </summary>
                /// <param name="node"></param>
                /// <param name="delta"></param>
                private void MoveNode(WayPoint.Node node, Vector3 delta)
                {
                    node.Translate(delta);
                    Changed();
                }

                /// <summary>
                /// Move the edge by specified amount.
                /// </summary>
                /// <param name="node"></param>
                /// <param name="delta"></param>
                private void MoveEdge(WayPoint.Edge edge, Vector3 delta)
                {
                    edge.Translate(delta);
                    Changed();
                }

                /// <summary>
                /// Move the path by specified amount.
                /// </summary>
                /// <param name="node"></param>
                /// <param name="delta"></param>
                private void MovePath(WayPoint.Path path, Vector3 delta)
                {
                    path.Translate(delta);
                    Changed();
                }

                /// <summary>
                /// See if a path or edge direction change was requested, and 
                /// perform it if it was.
                /// </summary>
                private void CheckDirection()
                {
                    if (Actions.ChangeDirection.WasPressed)
                    {
                        if (actOnPath)
                        {
                            if (Path != null)
                            {
                                Actions.ChangeDirection.ClearAllWasPressedState();
                                Path.IncDir();
                                Changed();
                                Click(ModeChange.Accept);
                            }
                        }
                        else if (edge != null)
                        {
                            Actions.ChangeDirection.ClearAllWasPressedState();
                            edge.IncDir();
                            Changed();
                            Click(ModeChange.Accept);
                        }
                    }
                }

                /// <summary>
                /// If we're moused over a node and have the right mouse click,
                /// start adding more nodes.
                /// If we're over an edge and have the right mouse clicks, 
                /// split the edge and select the new node.
                /// </summary>
                /// <returns></returns>
                private bool CheckAddDelete()
                {
                    if (edge != null)
                    {
                        if (act.AddNodes || act.MouseSplit)
                        {
                            act.AddNodes = false;
                            act.MouseSplit = false;
                            SplitEdge(edge);
                            edge = null;
                            return true;
                        }
                        else if (act.Delete)
                        {
                            if (actOnPath)
                            {
                                DeletePath(Path);
                            }
                            else
                            {
                                DeleteEdge(edge);
                            }
                            edge = null;
                            return true;
                        }
                    }
                    if (node != null)
                    {
                        if (act.MouseAddNodes)
                        {
                            act.MouseAddNodes = false;
                            fromNode = node;
                            mode = Mode.Add;
                            Click(ModeChange.Accept);
                            Changed();
                            return true;
                        }
                        else if (act.Delete)
                        {
                            if (actOnPath)
                            {
                                DeletePath(Path);
                            }
                            else
                            {
                                DeleteNode(node);
                            }
                            node = null;
                            return true;
                        }
                    }
                    return false;
                }

                /// <summary>
                /// Splits the current edge adding a new node in the middle.
                /// </summary>
                public void SplitCurrentEdge()
                {
                    if (edge != null)
                    {
                        SplitEdge(edge);
                        edge = null;
                    }
                }

                /// <summary>
                /// Check for any mode transitions.
                /// </summary>
                private void CheckMode()
                {
                    // TODO (****) Case where mode == none and fromNode is valid?
                    //Debug.Assert((mode == Mode.Add) || (fromNode == null));
                    switch (mode)
                    {
                        case Mode.Add:
                            if (act.Done)
                            {
                                act.Done = false;
                                mode = Mode.None;
                                fromNode = null;
                            }
                            break;
                        case Mode.Over:
                            if (node != null &&
                                fromNode == null &&
                                Boku.InGame.inGame.touchEditUpdateObj.ToolBar.IsButtonActionToggledOn(ToolBar.TouchControls.BrushActionIDs.baNode))
                            {
                                //in add mode but selected an existing node to start - set it as the node to add from
                                fromNode = node;
                                mode = Mode.Add;
                            }

                            if (StartDragging)
                            {
                                mode = Mode.Drag;
                            }
                            else if (StartRaising)
                            {
                                mode = Mode.Raise;
                            }
                            break;

                        case Mode.None:
                            if (StartDragging)
                            {
                                mode = Mode.Drag;
                            }
                            else if (StartRaising)
                            {
                                mode = Mode.Raise;
                            }                           
                            break;

                        case Mode.Drag:
                            if (!ContinueDragging || Empty)
                            {
                                mode = ContinueRaising ? Mode.Raise : Mode.None;
                            }
                            break;

                        case Mode.Raise:
                            if (!ContinueRaising || Empty)
                                mode = ContinueDragging
                                    ? Mode.Drag
                                    : Mode.None;
                            break;

                    }
                    if ((mode == Mode.None) && !Empty)
                    {
                        mode = Mode.Over;
                    }
                }

                /// <summary>
                /// Do a LOS check for a hypothetical node at given position with standard radius.
                /// </summary>
                /// <param name="camera"></param>
                /// <param name="objPos"></param>
                /// <returns></returns>
                private bool LOSCheck(Camera camera, Vector3 objPos)
                {
                    return TouchEdit.TouchOver(camera, objPos, WayPoint.Node.Radius);
                }

                #region Rendering

                /// <summary>
                /// Render any highlights for the path being edited.
                /// </summary>
                /// <param name="camera"></param>
                public void Render(Camera camera)
                {
                    /// If we aren't in edit object mode, then insta-bail.
                    if (!(inGame.CurrentUpdateMode == UpdateMode.EditObject
                        || (inGame.CurrentUpdateMode == UpdateMode.TouchEdit && EditWorldScene.CurrentToolMode == EditWorldScene.ToolMode.Paths)))
                    {
                        Clear();
                        return;
                    }

                    //only render next edge when not in touch
                    if (mode == Mode.Add && !KoiLibrary.LastTouchedDeviceIsTouch)
                    {
                        Vector3 pos = AddPosition(camera, fromNode);
                        RenderSelection(camera, fromNode, pos);
                    }

                    if (inGame.renderEffects == RenderEffect.Normal)
                    {
                        if (actOnPath)
                        {
                            RenderPath(camera, Path);
                        }
                        else if (node != null)
                        {
                            RenderNode(camera, node);
                        }
                        else if (edge != null)
                        {
                            RenderEdge(camera, edge);
                        }
                    }
                }

                /// <summary>
                /// Render a highlighting version of given node.
                /// </summary>
                /// <param name="camera"></param>
                /// <param name="node"></param>
                private void RenderNode(Camera camera, WayPoint.Node node)
                {
                    bool asRoad = false;
                    RenderSphere(camera,
                        node.RenderPosition(asRoad),
                        CurrentRadius(),
                        CurrentColor());
                }

                /// <summary>
                /// Render a highlight for the given edge.
                /// </summary>
                /// <param name="camera"></param>
                /// <param name="edge"></param>
                private void RenderEdge(Camera camera, WayPoint.Edge edge)
                {
                    bool asRoad = false;

                    bool edit = edge.Edit;
                    edge.Edit = true;
                    edge.Render(camera, edge.Path.RGBColor, asRoad);
                    edge.Edit = edit;

                    Vector3 pos = (edge.Node0.RenderPosition(asRoad) + edge.Node1.RenderPosition(asRoad)) * 0.5f;

                    RenderSphere(camera,
                        pos,
                        CurrentRadius(),
                        CurrentColor());
                }

                /// <summary>
                /// Render highlights for all nodes and edges on given path.
                /// </summary>
                /// <param name="camera"></param>
                /// <param name="path"></param>
                private void RenderPath(Camera camera, WayPoint.Path path)
                {
                    if (path != null)
                    {
                        int numNodes = path.Nodes.Count;
                        for (int i = 0; i < numNodes; ++i)
                        {
                            RenderNode(camera, path.Nodes[i]);
                        }
                        int numEdges = path.Edges.Count;
                        for (int i = 0; i < numEdges; ++i)
                        {
                            RenderEdge(camera, path.Edges[i]);
                        }
                    }
                }

                /// <summary>
                /// Render a node like sphere at givin position with given color.
                /// </summary>
                /// <param name="camera"></param>
                /// <param name="pos"></param>
                /// <param name="radius"></param>
                /// <param name="color"></param>
                private void RenderSphere(Camera camera, Vector3 pos, float radius, Vector4 color)
                {
                    if (camera.Frustum.CullTest(pos, radius) == Frustum.CullResult.TotallyOutside)
                        return;

                    Sphere sphere = Sphere.GetInstance();
                    ParticleSystemManager manager = InGame.inGame.ParticleSystemManager;
                    Effect effect = manager.Effect3d;
                    effect.CurrentTechnique = manager.Technique(ParticleSystemManager.EffectTech3d.TransparentColorPassNoZ);

                    Matrix worldMatrix = Matrix.Identity;
                    worldMatrix.Translation = pos;

                    manager.Parameter(ParticleSystemManager.EffectParams3d.DiffuseColor).SetValue(color);
                    manager.Parameter(ParticleSystemManager.EffectParams3d.EmissiveColor).SetValue(color);
                    manager.Parameter(ParticleSystemManager.EffectParams3d.Alpha).SetValue(color.W);
                    manager.Parameter(ParticleSystemManager.EffectParams3d.Radius).SetValue(radius);

                    sphere.Render(camera, ref worldMatrix, effect);

                }

                /// <summary>
                /// Calculate the current undulating node radius for rendering.
                /// </summary>
                /// <returns></returns>
                private float CurrentRadius()
                {
                    return WayPoint.Node.Radius
                        + 0.2f * (float)(Math.Sin(10.0 * Time.WallClockTotalSeconds) + 1.0);
                }

                /// <summary>
                /// Calculate the proper highlight color for nodes/edges being edited.
                /// </summary>
                /// <returns></returns>
                private Vector4 CurrentColor()
                {
                    switch (mode)
                    {
                        case Mode.Raise:
                        case Mode.Drag:
                            return new Vector4(1.0f, 0.0f, 1.0f, 0.25f);
                        default:
                            break;
                    }
                    return new Vector4(1.0f, 1.0f, 0.0f, 0.25f);
                }

                #endregion Rendering

                #endregion Internal
            };

            /// <summary>
            /// The singleton for handling mouse/keyboard editing of waypoints.
            /// </summary>
            public static TouchOver touchOver = new TouchOver();
        };
    };
};
