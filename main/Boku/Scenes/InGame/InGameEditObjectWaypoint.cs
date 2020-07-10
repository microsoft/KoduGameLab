// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.



using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Boku.Base;
using Boku.Common;
using Boku.Common.ParticleSystem;
using Boku.SimWorld;
using Boku.SimWorld.Path;
using Boku.SimWorld.Terra;
using Boku.Audio;
using Boku.Common.Gesture;

/// Mode transitions
/// Unknown
///         - Cursor under Node => Node
///         - Cursor under Edge => Edge
/// Node    
///         - <A> => MoveNode               || Node becomes selected node
///         - <X> => Path                   || node.Path becomes edit path
///         - <Y> => AddNode                || node becomes ActiveNode
///         - <Trig-L> => Object Edit       || node (and connected edges) deleted
///         - Move cursor => Object Edit    || node stops being edit node
///         - <B> => Tool Menu              || back out to tool menu
///
/// Path
///         - <A> => MovePath               || Path becomes selected path
///         - <X> => Node/Edge***           || If cursor under node(edge), node(edge) becomes edit 
///                                         || node(edge), else go to unknown.
///         - <Trig-L> => Object Edit       || Entire path is deleted
///         - Move cursor => Object Edit    || path stops being edit path
///         - <B> => Tool Menu              || back out to tool menu
///         
/// Edge
///         - <A> => MoveEdge               || Edge becomes selected edge
///         - <X> => Path                   || Edge.path becomes edit path
///         - <Trig-L> => Object Edit       || Edge (and connected nodes) deleted
///         - Move cursor => Object Edit    || edge stops being edit edge
///         - <B> => Tool Menu              || back out to tool menu
///         
/// MoveNode
///         - <A> => Node                   || drop selected node where it is, 
///                                         || have no selected node, node becomes edit node
///         - <X> => MovePath               || drop selected node where it is, select node.Path
///         - <Tr-L> => Object Edit         || delete selected node (and connected edges)
///         - <B> => Node                   || drop selected node where it is, 
///                                         || have no selected node, node becomes edit node
///         
/// MovePath
///         - <A> => Path                   || drop selected path where it is, 
///                                         || have no selected path, path becomes edit path
///         - <X> => MoveNode/MoveEdge***   || drop selected path where it is
///                                         || If cursor under node(edge), node(edge) becomes selected 
///                                         || node(edge), else go to unknown.
///         - <Trig-L> => Object Edit       || Entire path deleted.
///         - <B> => Path                   || drop selected path where it is
///                                         || have no selected path, path becomes edit path
///         
/// MoveEdge
///         - <A> => Edge                   || drop edge where it is
///                                         || have no selected edge, edge becomes edit edge
///         - <X> => MovePath               || drop edge where it is, edge.Path becomes selected path
///         - <Trig-L> => Object Edit       || edge and both nodes deleted.
///         - <B> => Edge                   || drop edge where it is
///                                         || have no selected edge, edge becomes edit edge
///         
/// AddNode
///         - <A> => AddNode                || Create node and edge to ActiveNode
///                                         || New node becomes ActiveNode
///         - <Trig-R> => AddNode           || Create node and edge to ActiveNode
///                                         || ActiveNode doesn't change.
///         - <B> => Object Edit            || ActiveNode stops being ActiveNode
namespace Boku
{
    /// <summary>
    /// UpdateObject for InGame -> EditObject
    /// </summary>
    public partial class InGame : GameObject, INeedsDeviceReset
    {
        /// <summary>
        /// Class supporting all manner of editing WayPoint paths, nodes, and edges.
        /// Note that the keyboard/mouse implementation is in the subclass MouseOver.cs.
        /// </summary>
        public partial class WayPointEdit
        {
            #region Members
            public enum EditMode
            {
                Unknown,
                Node,
                Path,
                Edge,
                MoveNode,
                MovePath,
                MoveEdge,
                AddNode
            };
            private EditMode mode = EditMode.Unknown;

            private WayPoint.Node activeNode = null;
            private WayPoint.Edge activeEdge = null;

            private WayPoint.Node fromNode = null;

            private Vector2 lastCursorPos = Vector2.Zero;
            private float moveTimer = 0.0f;

            private GamePadInput pad = null;
            #endregion Members

            #region Accessors
            /// <summary>
            /// Do we want the color palette visible?
            /// </summary>
            public bool ColorActive
            {
                get { return Active || mouseOver.Adjusting || touchOver.Adjusting; }
            }

            /// <summary>
            /// Are we active in gamepad editing?
            /// </summary>
            public bool Active
            {
                get { return Mode != EditMode.Unknown; }
            }

            /// <summary>
            /// Are we in a mode moving paths or subpaths around?
            /// </summary>
            public bool MoveMode
            {
                get
                {
                    switch(Mode)
                    {
                        case EditMode.MoveEdge:
                        case EditMode.MoveNode:
                        case EditMode.MovePath:
                            return true;
                    }
                    return false;
                }
            }

            /// <summary>
            /// Is the path we're currently editing being moved? If so,
            /// drop the rendering quality on it. Adds some hysteresis padding.
            /// </summary>
            public bool Moving
            {
                get { return MoveMode && (moveTimer > 0.0f); }
            }

            /// <summary>
            /// Are we currently adding nodes?
            /// </summary>
            public bool AddMode
            {
                get { return Mode == EditMode.AddNode; }
            }

            /// <summary>
            /// What is the node we are adding from?
            /// </summary>
            public WayPoint.Node FromNode
            {
                get { return fromNode; }
                private set 
                {
                    if (fromNode != value)
                    {
                        if (fromNode != null)
                            fromNode.ClearEdit();
                        fromNode = value;
                    }
                }
            }

            /// <summary>
            /// What node is currently selected? Can be null.
            /// </summary>
            public WayPoint.Node ActiveNode
            {
                get { return activeNode; }
                private set
                {
                    if (activeEdge != null)
                        activeEdge.ClearEdit();
                    activeEdge = null;
                    if (activeNode != value)
                    {
                        if (activeNode != null)
                            activeNode.ClearEdit();
                        activeNode = value;
                    }
                }
            }

            /// <summary>
            /// What edge is currently selected? Can be null.
            /// </summary>
            public WayPoint.Edge ActiveEdge
            {
                get { return activeEdge; }
                private set
                {
                    if (activeNode != null)
                        activeNode.Path.ClearEdit();
                    activeNode = null;
                    if (activeEdge != value)
                    {
                        if (activeEdge != null)
                            activeEdge.ClearEdit();
                        activeEdge = value;
                    }
                }
            }

            /// <summary>
            /// What path are we acting on, either with gamepad or mouse?
            /// </summary>
            public WayPoint.Path ActivePath
            {
                get
                {
                    if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                    {
                        if (touchOver.Path != null)
                        {
                            return touchOver.Path;
                        }
                    }
                    else if (mouseOver.Path != null)
                    {
                        return mouseOver.Path;
                    }
                    return SelectedPath;
                }
            }

            /// <summary>
            /// What path are we acting on with the gamepad?
            /// </summary>
            public WayPoint.Path SelectedPath
            {
                get
                {
                    if (FromNode != null)
                    {
                        return FromNode.Path;
                    }
                    if (ActiveNode != null)
                    {
                        return ActiveNode.Path;
                    }
                    if (ActiveEdge != null)
                    {
                        return ActiveEdge.Path;
                    }
                    return null;
                }
            }

            /// <summary>
            /// What's the color index of any path we're currently editing,
            /// either through gamepad or mouse?
            /// </summary>
            public int ColorIndex
            {
                get
                {
                    if (Mode == EditMode.AddNode)
                    {
                        Debug.Assert(FromNode != null);
                        return ColorPalette.GetIndexFromColor(FromNode.Path.Color);
                    }
                    WayPoint.Path path = ActivePath;
                    if (path != null)
                    {
                        return ColorPalette.GetIndexFromColor(path.Color);
                    }
                    return -1;
                }
            }

            /// <summary>
            /// What's the node or edge we currently have selected?
            /// </summary>
            public object SnapObject
            {
                get
                {
                    if (ActiveNode != null)
                        return ActiveNode;
                    if (ActiveEdge != null)
                        return ActiveEdge;
                    return null;
                }
            }

            /// <summary>
            /// What's the distance from our currently selected object to the input position?
            /// </summary>
            /// <param name="cursorPos"></param>
            /// <returns></returns>
            public float SnapDistance(Vector2 cursorPos)
            {
                if (ActiveNode != null)
                {
                    return ActiveNode.Distance(cursorPos);
                }
                if (ActiveEdge != null)
                {
                    return ActiveEdge.DistanceToHandle(cursorPos);
                }
                return float.MaxValue;
            }

            /// <summary>
            /// Constant for how close a node/edge has to get to the cursor to get captured.
            /// </summary>
            public static float SnapCaptureRadius
            {
                get { return BaseEditUpdateObj.SnapCaptureRadius; }
            }

            /// <summary>
            /// Constant for the square of how close a node/edge has to get to the cursor to get captured.
            /// </summary>
            public static float SnapCaptureRadiusSq
            {
                get { return SnapCaptureRadius * SnapCaptureRadius; }
            }

            /// <summary>
            /// Constant for how close a node/edge has to get from the cursor to get released.
            /// </summary>

            public static float SnapReleaseRadius
            {
                get { return BaseEditUpdateObj.SnapReleaseRadius; }
            }
            /// <summary>
            /// Constant for the square of how close a node/edge has to get from the cursor to get released.
            /// </summary>
            public static float SnapReleaseRadiusSq
            {
                get { return SnapReleaseRadius * SnapReleaseRadius; }
            }

            /// <summary>
            /// Accessor for how far from the camera to the closest edge/node which
            /// is currently moused over.
            /// </summary>
            public static float MouseOverDistance
            {
                get { return mouseOver.distance; }
            }

            
            public static float TouchOverDistance
            {
                get { return touchOver.distance; }
            }


            /// <summary>
            /// Is the mouse currently being used to drag path parts about? If
            /// so, probably don't want to interpret it as something else like
            /// dragging the camera.
            /// </summary>
            public bool Dragging
            {
                get {
                    if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                        return touchOver.Moving;
                    else
                        return mouseOver.Moving;
                }
            }

            #region Internal Accessors
            /// <summary>
            /// The owning inGame object.
            /// </summary>
            private InGame Parent
            {
                get { return InGame.inGame; }
            }

            /// <summary>
            /// The current mode, from the EditMode enum.
            /// </summary>
            private EditMode Mode
            {
                get { return mode; }
                set
                {
                    if (mode != value)
                    {
                        mode = value;
                        if (MoveMode)
                            ClearRot();
                    }
                }
            }
            #endregion Internal Accessors

            /// <summary>
            /// We need to give the height of the current node/edge as a camera
            /// offset.
            /// </summary>
            public float CameraHeightOffset
            {
                get
                {
                    float height = 0.0f;
                    if (ActivePath != null)
                    {
                        Vector2 pos = Vector2.Zero;
                        if (AddMode)
                        {
                            if (FromNode != null)
                            {
                                height = FromNode.Height;
                                pos = FromNode.Position2d;
                            }
                        }
                        else 
                        {
                            if (ActiveNode != null)
                            {
                                height = ActiveNode.Height;
                                pos = ActiveNode.Position2d;
                            }
                            if (ActiveEdge != null)
                            {
                                height = (ActiveEdge.Node0.Height + ActiveEdge.Node1.Height) * 0.5f;
                                pos = (ActiveEdge.Node0.Position2d + ActiveEdge.Node1.Position2d) * 0.5f;
                            }
                        }
                        height += Terrain.GetTerrainHeightFlat(CursorPosition2d()) 
                            - Terrain.GetTerrainAndPathHeight(CursorPosition());
                    }
                    return height;
                }
            }
            #endregion Accessors

            #region Public

            /// <summary>
            /// Reset to fresh state, letting go of anything we might have grabbed.
            /// </summary>
            public void Clear()
            {
                ClearEdit();
                ActiveNode = null;
                ActiveEdge = null;
                FromNode = null;
                Mode = EditMode.Unknown;
            }

            /// <summary>
            /// Update for all waypoint edit ops. Will pass off to MouseOver if in Keyboard
            /// mode.
            /// </summary>
            /// <returns></returns>
            public bool Update()
            {
                pad = GamePadInput.GetGamePad0();
                
                ValidateMode();

                AdjustTypeAndColor();

                bool changedMode = false;

                if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                {
                    mouseOver.Update(Parent, Parent.Camera);
                }
                else
                {
                    switch (Mode)
                    {
                        case EditMode.Unknown:
                            break;
                        case EditMode.Node:
                            changedMode = UpdateNode();
                            break;
                        case EditMode.Path:
                            changedMode = UpdatePath();
                            break;
                        case EditMode.Edge:
                            changedMode = UpdateEdge();
                            break;
                        case EditMode.MoveNode:
                            changedMode = MoveNode();
                            break;
                        case EditMode.MovePath:
                            changedMode = MovePath();
                            break;
                        case EditMode.MoveEdge:
                            changedMode = MoveEdge();
                            break;
                        case EditMode.AddNode:
                            changedMode = AddNode();
                            break;
                    }
                    CheckCursorMovement();
                }

                ValidateMode();

                return changedMode;
            }

            /// <summary>
            /// Render the highlight for whatever nodes/edges are being edited,
            /// including the fake node/edge placeholders when adding a new node.
            /// For GamePadInput, the real node/edge highlight rendering happens 
            /// in the waypoint code, but the mouseOver version handles the whole thing.
            /// </summary>
            /// <param name="camera"></param>
            public void RenderWayPointSelection(Camera camera)
            {
                if (Mode == EditMode.AddNode)
                {
                    Debug.Assert(FromNode != null);

                    Vector3 addPosition = AddPosition(camera);

                    RenderSelection(camera, FromNode, addPosition);
                }
                mouseOver.Render(camera);
            }

            /// <summary>
            /// Render the placeholder new node/edge pair when adding a new node.
            /// </summary>
            /// <param name="camera"></param>
            /// <param name="from"></param>
            /// <param name="addPosition"></param>
            private static void RenderSelection(Camera camera, WayPoint.Node from, Vector3 addPosition)
            {
                Vector2 addPos2 = new Vector2(addPosition.X, addPosition.Y);
                float dist2d = Vector2.Distance(addPos2, from.Position2d);

                if (dist2d > SnapCaptureRadius)
                {
                    from.RenderNewEdge(camera, addPosition);
                }
            }

            /// <summary>
            /// Return the proper HelpOverlay ID for the current mode of waypoint edit operation.
            /// If no waypoint editing going on, will return null.
            /// </summary>
            /// <returns></returns>
            public string UpdateHelpOverlay()
            {
                string helpID = mouseOver.UpdateHelpOverlay();
                if (helpID == null)
                {
                    switch (Mode)
                    {
                        case EditMode.Unknown:
                            break;
                        case EditMode.Node:
                            helpID = "EditWayPointNode";
                            break;
                        case EditMode.Path:
                            helpID = "EditWayPointPath";
                            break;
                        case EditMode.Edge:
                            helpID = "EditWayPointEdge";
                            break;
                        case EditMode.MoveNode:
                            helpID = "EditWayPointMoveNode";
                            break;
                        case EditMode.MovePath:
                            helpID = "EditWayPointMovePath";
                            break;
                        case EditMode.MoveEdge:
                            helpID = "EditWayPointMoveEdge";
                            break;
                        case EditMode.AddNode:
                            if (FromNode != ActiveNode)
                            {
                                helpID = "EditWayPointAddNode";
                            }
                            else
                            {
                                helpID = "EditWayPointEndAddNode";
                            }
                            break;
                        default:
                            Debug.Assert(false, "Got a mode we don't have an overlay for?");
                            break;
                    }

                    if (MoveMode)
                    {
                        Parent.cursor3D.DiffuseColor = Color.Red.ToVector4();
                    }
                    else if (Active)
                    {
                        Parent.cursor3D.DiffuseColor = Color.Blue.ToVector4();
                    }
                }
                return helpID;
            }

            /// <summary>
            /// Create a new path, and put us in Add mode to continue building on it.
            /// Handles gamepad mode or passes off to mouseOver for keyboard mode.
            /// </summary>
            /// <param name="pos"></param>
            /// <param name="objColorIndex"></param>
            public void NewPath(Vector3 pos, int objColorIndex)
            {
                if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                {
                    mouseOver.NewPath(pos, objColorIndex);
                }
                else if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                {
                    touchOver.NewPath(pos, objColorIndex);
                }
                else
                {
                    WayPoint.Node node = WayPoint.CreateNewNode(null, objColorIndex, pos);
                    EnterAddMode(node);
                    Foley.PlayMakePath();
                    Changed();
                }
            }

            /// <summary>
            /// Move the cursor, generally to move it under a node or edge that
            /// has been double clicked.
            /// </summary>
            /// <param name="pos"></param>
            /// <returns></returns>
            public Vector2 DoCursor(Vector2 pos)
            {
                if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                {
                    return touchOver.DoCursor(pos);
                }
                else
                {
                    return mouseOver.DoCursor(pos);
                }
                
            }

            #endregion Public

            #region Internal

            /// <summary>
            /// Force the system into add mode, like if the user's just created a single
            /// node, we might assume they want to continue adding nodes for a bit.
            /// </summary>
            /// <param name="from"></param>
            private void EnterAddMode(WayPoint.Node from)
            {
                FromNode = from;
                Mode = EditMode.AddNode;
            }

            /// <summary>
            /// Find the right position to add a new node.
            /// </summary>
            /// <param name="camera"></param>
            /// <returns></returns>
            private Vector3 AddPosition(Camera camera)
            {
                Vector3 pos = CursorPosition();
                return pos;
            }

            #region Child class to abstract user actions.
            /// <summary>
            /// A little abstraction on inputs that cause actions specific to Waypoint editing.
            /// Each includes a setter which only has effect if the value is false, at which
            /// time it clears all the input(s) which would make the getter true.
            /// </summary>
            private class Acts
            {
                #region Members
                GamePadInput pad = null;
                #endregion Members

                #region Accessors
                /// <summary>
                /// Cycle to next direction.
                /// </summary>
                public bool ChangeDirection
                {
                    get
                    {
                        return Actions.ChangeDirection.WasPressed;
                    }
                    set
                    {
                        if (!value)
                        {
                            Actions.ChangeDirection.ClearAllWasPressedState();
                        }
                    }
                }
                /// <summary>
                /// Toggle between editing whole path vs. single node/edge.
                /// </summary>
                public bool TogglePath
                {
                    get
                    {
                        return Actions.TogglePath.WasPressed;
                    }
                    set
                    {
                        if (!value)
                        {
                            Actions.TogglePath.ClearAllWasPressedState();
                        }
                    }
                }
                /// <summary>
                /// Pickup whatever's being edited to move it.
                /// </summary>
                public bool Pickup
                {
                    get
                    {
                        /// Also want possibility of pickup via double click
                        return Actions.PathPickup.WasPressed;
                    }
                    set
                    {
                        if (!value)
                        {
                            Actions.PathPickup.ClearAllWasPressedState();
                        }
                    }
                }
                /// <summary>
                /// Put whatever is picked up down.
                /// </summary>
                public bool Put
                {
                    get
                    {
                        return Actions.PathPut.WasPressed;
                    }
                    set
                    {
                        if (!value)
                        {
                            Actions.PathPut.ClearAllWasPressedState();
                        }
                    }
                }
                /// <summary>
                /// Finished here, exit edit mode.
                /// </summary>
                public bool Done
                {
                    get
                    {
                        return Actions.PathDone.WasPressed;
                    }
                    set
                    {
                        if (!value)
                        {
                            Actions.PathDone.ClearAllWasPressedState();
                        }
                    }
                }
                /// <summary>
                /// Delete whatever is being edited.
                /// </summary>
                public bool Delete
                {
                    get
                    {
                        if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                        {
                            return (TouchGestureManager.Get().TapGesture.WasRecognized &&
                                InGame.inGame.touchEditUpdateObj.ToolBar.IsButtonActionToggledOn(ToolBar.TouchControls.BrushActionIDs.baDelete));
                        }
                        else
                        {
                            return Actions.PathDelete.WasPressed
                                || (Left && Control);
                        }
                    }
                    set
                    {
                        if (!value)
                        {
                            if (GamePadInput.ActiveMode != GamePadInput.InputMode.Touch)
                            {
                                Actions.PathDelete.ClearAllWasPressedState();
                            }
                        }
                    }
                }
                /// <summary>
                /// Split current edge, Gamepad specific.
                /// </summary>
                public bool Split
                {
                    get
                    {
                        return Actions.SplitEdge.WasPressed && (mouseOver.edge == null) && (touchOver.edge == null);
                    }
                    set
                    {
                        if (!value)
                        {
                            Actions.SplitEdge.ClearAllWasPressedState();
                        }
                    }
                }
                /// <summary>
                /// Mouse version of split current edge.
                /// </summary>
                public bool MouseSplit
                {
                    get
                    {
                        if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                        {
                            //FIXME: need touch button or gesture for this
                            return (touchOver.edge != null) && (TouchGestureManager.Get().TapGesture.WasRecognized && Space);
                        }
                        else
                        {
                            return (mouseOver.edge != null)
                                && (Actions.SplitEdge.WasPressed || (Space && Left));
                        }
                    }
                    set
                    {
                        if (!value)
                        {
                            if (GamePadInput.ActiveMode != GamePadInput.InputMode.Touch)
                            {
                                Actions.SplitEdge.ClearAllWasPressedState();
                                Space = false;
                                Left = false;
                            }
                        }
                    }
                }
                /// <summary>
                /// Start adding more nodes, gamepad specific.
                /// </summary>
                public bool AddNodes
                {
                    get
                    {
                        return Actions.AddNodes.WasPressed && (mouseOver.node == null) && (touchOver.node == null);
                    }
                    set
                    {
                        if (!value)
                        {
                            Actions.AddNodes.ClearAllWasPressedState();
                        }
                    }
                }
                /// <summary>
                /// Mouse version of start adding more nodes.
                /// </summary>
                public bool MouseAddNodes
                {
                    get
                    {
                        return (mouseOver.node != null)
                            && (Actions.AddNodes.WasPressed || (Left && Space));
                    }
                    set
                    {
                        if (!value)
                        {
                            Actions.AddNodes.ClearAllWasPressedState();
                            Left = false;
                            Space = false;
                        }
                    }
                }
                /// <summary>
                /// Add a node and continue in Add mode.
                /// </summary>
                public bool PutNodeGo
                {
                    get
                    {
                        if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                        {
                            return TouchGestureManager.Get().TapGesture.WasRecognized && InGame.inGame.TouchEdit.HasNonUITouch();
                        }
                        else
                        {
                            return Actions.PutNodeGo.WasPressed
                                || Left;
                        }
                    }
                    set
                    {
                        if (!value)
                        {
                            Actions.PutNodeGo.ClearAllWasPressedState();
                            Left = false;
                        }
                    }
                }
                /// <summary>
                /// Add a node and stop adding.
                /// </summary>
                public bool PutNodeDone
                {
                    get
                    {
                        return Actions.PutNodeDone.WasPressed
                            || (Left && Alt);
                    }
                    set
                    {
                        if (!value)
                        {
                            Actions.PutNodeDone.ClearAllWasPressedState();
                            Left = false;
                            Alt = false;
                        }
                    }
                }
                /// <summary>
                /// The space bar is pressed.
                /// </summary>
                public bool Space
                {
                    get { return KeyboardInput.IsPressed(Keys.Space); }
                    set
                    {
                        if (!value)
                        {
                            KeyboardInput.ClearAllWasPressedState(Keys.Space);
                        }
                    }
                }
                /// <summary>
                /// The alt key (left or right) is pressed.
                /// </summary>
                public bool Alt
                {
                    get { return KeyboardInput.IsPressed(Keys.LeftAlt) || KeyboardInput.IsPressed(Keys.RightAlt); }
                    set
                    {
                        if (!value)
                        {
                            KeyboardInput.ClearAllWasPressedState(Keys.LeftAlt);
                            KeyboardInput.ClearAllWasPressedState(Keys.RightAlt);
                        }
                    }
                }
                /// <summary>
                /// The control key (left or right) is pressed.
                /// </summary>
                public bool Control
                {
                    get { return KeyboardInput.IsPressed(Keys.LeftControl) || KeyboardInput.IsPressed(Keys.RightControl); }
                    set
                    {
                        if (!value)
                        {
                            KeyboardInput.ClearAllWasPressedState(Keys.LeftControl);
                            KeyboardInput.ClearAllWasPressedState(Keys.RightControl);
                        }
                    }
                }
                /// <summary>
                /// The left mouse button is pressed.
                /// </summary>
                public bool Left
                {
                    get { return MouseInput.Left.WasPressed; }
                    set
                    {
                        if (!value)
                        {
                            MouseInput.Left.ClearAllWasPressedState();
                        }
                    }
                }
                #endregion Accessors

                #region Public
                /// <summary>
                /// Tell what gamepad to listen to.
                /// </summary>
                /// <param name="pad"></param>
                public void Update(GamePadInput pad)
                {
                    this.pad = pad;
                }
                #endregion Public

            };
            /// <summary>
            /// Singleton for input inquiry.
            /// </summary>
            private static Acts act = new Acts();
            /// <summary>
            /// Accessor for the Acts singleton.
            /// </summary>
            private static Acts Act
            {
                get { return act; }
            }
            #endregion Child class to abstract user actions.

            /// <summary>
            /// We're beginning to move a path, start the hysteresis timer.
            /// </summary>
            private void StartMovement()
            {
                float kMoveTimeout = 0.25f;
                moveTimer = kMoveTimeout;
            }

            /// <summary>
            /// See if we're still moving the path, and set it's Moving state
            /// accordingly. When path.Moving is true, it's rendered as wireframe
            /// (nodes and edges only) and the conformation to the terrain is skipped.
            /// </summary>
            private void CheckCursorMovement()
            {
                Vector3 delta = DeltaPosition();
                if (delta != Vector3.Zero)
                {
                    StartMovement();
                }
                else
                {
                    if (moveTimer > 0.0f)
                        moveTimer -= Time.WallClockFrameSeconds;
                }
                lastCursorPos = CursorPosition2d();

                if (ActivePath != null)
                {
                    ActivePath.Moving = Moving;
                }
            }

            /// <summary>
            /// Mode changes which have sounds associated. Used as input for Click().
            /// </summary>
            private enum ModeChange
            {
                MoveOut,
                MoveBack,
                Accept,
                Cancel,
                Add,
                Delete,
                NoBudget
            }
            
            /// <summary>
            /// Play the right sound to go with the mode change.
            /// </summary>
            /// <param name="change"></param>
            private static void Click(ModeChange change)
            {
                Click(change, false);
            }

            /// <summary>
            /// Play the right sound to go with the mode change.
            /// </summary>
            /// <param name="change"></param>
            /// <param name="quiet">If true, don't play the associated sound.</param>
            private static void Click(ModeChange change, bool quiet)
            {
                switch(change)
                {
                    case ModeChange.Accept:
                        if (!quiet)
                        {
                            Foley.PlayEditPath();
                        }
                        break;
                    case ModeChange.MoveBack:
                        if (!quiet)
                        {
                            Foley.PlayBack();
                        }
                        break;
                    case ModeChange.MoveOut:
                        if (!quiet)
                        {
                            Foley.PlayShuffle();
                        }
                        break;
                    case ModeChange.Cancel:
                        if (!quiet)
                        {
                            Foley.PlayBack();
                        }
                        break;
                    case ModeChange.Add:
                        Instrumentation.IncrementCounter(Instrumentation.CounterId.AddItem);

                        if (!quiet)
                        {
                            Foley.PlayMakePath();
                        }
                        break;
                    case ModeChange.Delete:
                        Instrumentation.IncrementCounter(Instrumentation.CounterId.DeleteItem);

                        if (!quiet)
                        {
                            Foley.PlayCut();
                        }
                        break;
                    case ModeChange.NoBudget:
                        Instrumentation.IncrementCounter(Instrumentation.CounterId.AddItemNoBudget);

                        if (!quiet)
                        {
                            Foley.PlayNoBudget();
                        }
                        break;
                }
            }

            /// Node    
            ///         - <A> => MoveNode               || Node becomes selected node
            ///         - <X> => Path                   || node.Path becomes edit path
            ///         - <Y> => AddNode                || node becomes ActiveNode
            ///         - <Trig-L> => Object Edit       || node (and connected edges) deleted
            ///         - Move cursor => Object Edit    || node stops being edit node
            ///         - <B> => Tool Menu              || back out to tool menu
            /// <summary>
            /// Perform update when in EditMode.Node
            /// </summary>
            /// <returns></returns>
            private bool UpdateNode()
            {
                Debug.Assert(Mode == EditMode.Node);
                Debug.Assert(ActiveNode != null);
                if (act.Pickup)
                {
                    act.Pickup = false;
                    Mode = EditMode.MoveNode;
                    Click(ModeChange.Accept);
                    Foley.PlayEditPath();
                    return true;
                }
                if (act.TogglePath)
                {
                    act.TogglePath = false;
                    Mode = EditMode.Path;
                    Click(ModeChange.Accept);
                    return true;
                }
                if (act.AddNodes)
                {
                    act.AddNodes = false;
                    Mode = EditMode.AddNode;
                    FromNode = ActiveNode;
                    Click(ModeChange.Add);
                    return true;
                }
                if (act.Delete)
                {
                    act.Delete = false;
                    DeleteNode(ActiveNode);
                    ActiveNode = null;
                    Mode = EditMode.Unknown;
                    Changed();
                    return true;
                }
                ClearEdit();
                ActiveNode.Edit = true;

                return false;
            }

            /// Path
            ///         - <A> => MovePath               || Path becomes selected path
            ///         - <X> => Node/Edge***           || If cursor under node(edge), node(edge) becomes edit 
            ///                                         || node(edge), else go to unknown.
            ///         - <Trig-L> => Object Edit       || Entire path is deleted
            ///         - Move cursor => Object Edit    || path stops being edit path
            ///         - <B> => Tool Menu              || back out to tool menu
            ///         
            /// <summary>
            /// 
            /// </summary>
            /// <param name="pad"></param>
            /// <returns></returns>
            private bool UpdatePath()
            {
                Debug.Assert(Mode == EditMode.Path);
                Debug.Assert(ActivePath != null);

                if (act.ChangeDirection)
                {
                    act.ChangeDirection = false;
                    Click(ModeChange.Accept);
                    ActivePath.IncDir();
                    Changed();
                    return false;
                }
                if (act.Pickup)
                {
                    act.Pickup = false;
                    Mode = EditMode.MovePath;
                    Click(ModeChange.Accept);
                    return true;
                }
                if (act.TogglePath)
                {
                    act.TogglePath = false;
                    CheckCursor(ActivePath, false);
                    Click(ModeChange.Accept);
                    return true;
                }
                if (act.Delete)
                {
                    act.Delete = false;
                    DeletePath(ActivePath);
                    ActiveNode = null;
                    Mode = EditMode.Unknown;
                    Changed();
                    return true;
                }

                ClearEdit();
                ActivePath.Edit = true;

                return false;
            }

            /// Edge
            ///         - <A> => MoveEdge               || Edge becomes selected edge
            ///         - <X> => Path                   || Edge.path becomes edit path
            ///         - <Trig-L> => Object Edit       || Edge (and connected nodes) deleted
            ///         - Move cursor => Object Edit    || edge stops being edit edge
            ///         - <B> => Tool Menu              || back out to tool menu
            ///         
            /// <summary>
            /// 
            /// </summary>
            /// <param name="pad"></param>
            /// <returns></returns>
            private bool UpdateEdge()
            {
                Debug.Assert(Mode == EditMode.Edge);
                Debug.Assert(ActiveEdge != null);

                if (act.ChangeDirection)
                {
                    act.ChangeDirection = false;
                    Click(ModeChange.Accept);
                    ActiveEdge.IncDir();
                    Changed();
                    return false;
                }
                if (act.Pickup)
                {
                    act.Pickup = false;
                    Mode = EditMode.MoveEdge;
                    Click(ModeChange.Accept);
                    return true;
                }
                if (act.TogglePath)
                {
                    act.TogglePath = false;
                    Mode = EditMode.Path;
                    Click(ModeChange.Accept);
                    return true;
                }
                if (act.Delete)
                {
                    act.Delete = false;
                    WayPoint.Edge edge = ActiveEdge;
                    ActiveEdge = null;
                    DeleteEdge(edge);
                    Mode = EditMode.Unknown;
                    Changed();
                    return true;
                }
                if (act.Split)
                {
                    act.Split = false;
                    ActiveNode = SplitEdge(ActiveEdge);
                    Mode = ActiveNode != null ? EditMode.Node : EditMode.Unknown;
                    return true;
                }

                ClearEdit();
                ActiveEdge.Edit = true;

                return false;
            }

            /// MoveNode
            ///         - <A> => Node                   || drop selected node where it is, 
            ///                                         || have no selected node, node becomes edit node
            ///         - <X> => MovePath               || drop selected node where it is, select node.Path
            ///         - <Tr-L> => Object Edit         || delete selected node (and connected edges)
            ///         - <B> => Node                   || drop selected node where it is, 
            ///                                         || have no selected node, node becomes edit node
            ///         
            /// <summary>
            /// 
            /// </summary>
            /// <param name="pad"></param>
            /// <returns></returns>
            private bool MoveNode()
            {
                Debug.Assert(Mode == EditMode.MoveNode);
                Debug.Assert(ActiveNode != null);

                Vector3 delta = DeltaPosition();
                if (delta != Vector3.Zero)
                {
                    ActiveNode.Translate(delta);
                    Changed();
                }

                if (act.Put)
                {
                    act.Put = false;
                    Mode = EditMode.Node;
                    Click(ModeChange.Accept);
                    return true;
                }
                if (act.TogglePath)
                {
                    act.TogglePath = false;
                    Mode = EditMode.MovePath;
                    Click(ModeChange.Accept);
                    return true;
                }
                if (act.Delete)
                {
                    act.Delete = false;
                    DeleteNode(ActiveNode);
                    ActiveNode = null;
                    Mode = EditMode.Unknown;
                    Changed();
                    return true;
                }
                if (act.Done)
                {
                    act.Done = false;
                    Mode = EditMode.Node;
                    Click(ModeChange.MoveOut);
                    return true;
                }

                ClearEdit();
                ActiveNode.Select = true;

                return false;
            }

            /// MoveEdge
            ///         - <A> => Edge                   || drop edge where it is
            ///                                         || have no selected edge, edge becomes edit edge
            ///         - <X> => MovePath               || drop edge where it is, edge.Path becomes selected path
            ///         - <Trig-L> => Object Edit       || edge and both nodes deleted.
            ///         - <B> => Edge                   || drop edge where it is
            ///                                         || have no selected edge, edge becomes edit edge
            /// <summary>
            /// 
            /// </summary>
            /// <param name="pad"></param>
            /// <returns></returns>
            private bool MoveEdge()
            {
                Debug.Assert(Mode == EditMode.MoveEdge);
                Debug.Assert(ActiveEdge != null);

                Vector3 delta = DeltaPosition();
                if (delta != Vector3.Zero)
                {
                    ActiveEdge.Translate(delta);
                    Changed();
                }

                if (act.ChangeDirection)
                {
                    act.ChangeDirection = false;
                    Click(ModeChange.Accept);
                    ActiveEdge.IncDir();
                    Changed();
                    return false;
                }
                if (act.Put)
                {
                    act.Put = false;
                    Mode = EditMode.Edge;
                    Click(ModeChange.Accept);
                    return true;
                }
                if (act.TogglePath)
                {
                    act.TogglePath = false;
                    Mode = EditMode.MovePath;
                    Click(ModeChange.Accept);
                    return true;
                }
                if (act.Delete)
                {
                    act.Delete = false;
                    WayPoint.Edge edge = ActiveEdge;
                    ActiveEdge = null;
                    DeleteEdge(edge);
                    Mode = EditMode.Unknown;
                    Changed();
                    return true;
                }
                if (act.Done)
                {
                    act.Done = false;
                    Mode = EditMode.Edge;
                    Click(ModeChange.MoveOut);
                    return true;
                }

                ClearEdit();
                ActiveEdge.Select = true; ;

                return false;
            }

            /// MovePath
            ///         - <A> => Path                   || drop selected path where it is, 
            ///                                         || have no selected path, path becomes edit path
            ///         - <X> => MoveNode/MoveEdge***   || drop selected path where it is
            ///                                         || If cursor under node(edge), node(edge) becomes selected 
            ///                                         || node(edge), else go to unknown.
            ///         - <Trig-L> => Object Edit       || Entire path deleted.
            ///         - <B> => Path                   || drop selected path where it is
            ///                                         || have no selected path, path becomes edit path
            ///         
            ///         
            /// <summary>
            /// 
            /// </summary>
            /// <param name="pad"></param>
            /// <returns></returns>
            private bool MovePath()
            {
                Debug.Assert(Mode == EditMode.MovePath);
                Debug.Assert(ActivePath != null);

                Vector3 delta = DeltaPosition();
                if (delta != Vector3.Zero)
                {
                    ActivePath.Translate(delta);
                    Changed();
                }

                if (act.ChangeDirection)
                {
                    act.ChangeDirection = false;
                    Click(ModeChange.Accept);
                    ActivePath.IncDir();
                    Changed();
                    return false;
                }
                if (act.Put)
                {
                    act.Put = false;
                    Mode = EditMode.Path;
                    Click(ModeChange.Accept);
                    return true;
                }
                if (act.TogglePath)
                {
                    act.TogglePath = false;
                    CheckCursor(ActivePath, true);
                    Click(ModeChange.Accept);
                    return true;
                }
                if (act.Delete)
                {
                    act.Delete = false;
                    DeletePath(ActivePath);
                    ActiveNode = null;
                    Mode = EditMode.Unknown;
                    Changed();
                    return true;
                }
                if (act.Done)
                {
                    act.Done = false;
                    Mode = EditMode.Path;
                    Click(ModeChange.MoveOut);
                    return true;
                }

                ClearEdit();
                ActivePath.Select = true;

                return false;
            }

            /// AddNode
            ///         - <A> => AddNode                || Create node and edge to ActiveNode
            ///                                         || New node becomes ActiveNode
            ///         - <Trig-R> => AddNode           || Create node and edge to ActiveNode
            ///                                         || ActiveNode doesn't change.
            ///         - <B> => Object Edit            || ActiveNode stops being ActiveNode
            /// <summary>
            /// 
            /// </summary>
            /// <param name="pad"></param>
            /// <returns></returns>
            private bool AddNode()
            {
                Debug.Assert(Mode == EditMode.AddNode);
                Debug.Assert(FromNode != null);

                CheckCursorAddMode();

                if (act.PutNodeDone)
                {
                    act.PutNodeDone = false;
                    ActiveNode = DoAddNode();
                    FromNode = null;
                    Mode = EditMode.Node;
                    Changed();
                    return true;
                }
                if (act.PutNodeGo)
                {
                    act.PutNodeGo = false;
                    if (FromNode != ActiveNode)
                    {
                        FromNode = DoAddNode();
                        Changed();
                        return false;
                    }
                    ActiveNode = null;
                    FromNode = null;
                    Mode = EditMode.Unknown;
                    Click(ModeChange.MoveOut);
                    return true;
                }
                if (act.AddNodes)
                {
                    act.AddNodes = false;
                    DoAddNode();
                    Changed();
                    return false;
                }
                if (act.Done)
                {
                    act.Done = false;
                    ActiveNode = null;
                    FromNode = null;
                    Mode = EditMode.Unknown;
                    Click(ModeChange.MoveOut);
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Actually perform the add (if budget allows), including sounds and effects.
            /// </summary>
            /// <returns></returns>
            private WayPoint.Node DoAddNode()
            {
                Debug.Assert(FromNode != null);

                if (Parent.UnderBudget)
                {
                    if (FromNode != ActiveNode)
                    {
                        Click(ModeChange.Add);
                        if (ActiveNode != null)
                        {
                            WayPoint.CreateNewEdge(FromNode, ActiveNode);
                            return ActiveNode;
                        }
                        else
                        {
                            Vector3 pos = AddPosition(Parent.Camera);
                            return WayPoint.CreateNewNode(FromNode, Parent.shared.curObjectColor, pos);
                        }
                    }
                }
                else
                {
                    Click(ModeChange.NoBudget);
                }
                return FromNode; // no change
            }

            /// <summary>
            /// Reset any state in the path being edited, because we're about
            /// to stop editing it.
            /// </summary>
            private void ClearEdit()
            {
                if (ActivePath != null)
                    ActivePath.ClearEdit();
                if (ActiveNode != null)
                    ActiveNode.ClearEdit();
                if (ActiveEdge != null)
                    ActiveEdge.ClearEdit();
            }

            /// <summary>
            /// Helper to split an edge in two, creating 2 new edges connecting the original
            /// nodes to a new centrally placed node.
            /// </summary>
            /// <param name="edge"></param>
            /// <returns></returns>
            public static WayPoint.Node SplitEdge(WayPoint.Edge edge)
            {
                if (inGame.UnderBudget)
                {
                    WayPoint.Node node0 = edge.Node0;
                    WayPoint.Node node1 = edge.Node1;
                    Vector3 center = edge.HandlePosition;
                    int colorIndex = ColorPalette.GetIndexFromColor(edge.Path.Color); ;

                    edge.Delete();

                    WayPoint.Node middle = WayPoint.CreateNewNode(node0, colorIndex, center);
                    WayPoint.CreateNewEdge(middle, node1);

                    Click(ModeChange.Add);
                    Changed();

                    return node1;
                }
                else
                {
                    Click(ModeChange.NoBudget);
                }
                return null;
            }

            /// <summary>
            /// Special check when in Add mode, to see if the cursor is close enough
            /// to another node to make a connection, or is far enough on its own
            /// to add a new node.
            /// </summary>
            private void CheckCursorAddMode()
            {
                Vector3 addPosition = AddPosition(Parent.Camera);
                Vector2 addPos2 = new Vector2(addPosition.X, addPosition.Y);
                WayPoint.Node node = WayPoint.GetNearestWayPoint(addPos2);
                if (node != null)
                {
                    float distSq = node.DistanceSquared(addPos2);
                    if (node == ActiveNode)
                    {
                        if (distSq > SnapReleaseRadiusSq)
                        {
                            ActiveNode = null;
                        }
                    }
                    else
                    {
                        if (distSq < SnapCaptureRadiusSq)
                        {
                            ActiveNode = node;
                        }
                    }
                }
            }

            /// <summary>
            /// Adjust type and color of any path being edited. Handles
            /// both gamepad and mouse editing.
            /// </summary>
            private void AdjustTypeAndColor()
            {
                bool active = Active || mouseOver.Adjusting;

                if (active)
                {
                    bool upDown = Actions.NextType.WasPressedOrRepeat
                        || Actions.PrevType.WasPressedOrRepeat;
                    bool leftRight = Actions.ColorRight.WasPressedOrRepeat
                        || Actions.ColorLeft.WasPressedOrRepeat;
                    if (upDown != leftRight)
                    {
                        // DOn't make UI noises on repeat presses.
                        bool quiet = Actions.NextType.WasRepeatPressed 
                            || Actions.PrevType.WasRepeatPressed 
                            || Actions.ColorLeft.WasRepeatPressed 
                            || Actions.ColorRight.WasRepeatPressed;
                        Click(ModeChange.Accept, quiet);
                        Changed();
                        if (upDown)
                        {
                            AdjustType();
                        }
                        else
                        {
                            Debug.Assert(leftRight);
                            AdjustColor();
                        }
                    }
                    else if (Active)
                    {
                        AdvanceSystemColor(0);
                    }
                }
            }

            /// <summary>
            /// Is the D-Pad up/down currently for raising and lowering?
            /// </summary>
            private bool UpDownIsMove
            {
                get
                {
                    return MoveMode;
                }
            }

            /// <summary>
            /// Is the D-Pad up/down currently for changing path type?
            /// </summary>
            private bool UpDownIsType
            {
                get
                {
                    return !MoveMode;
                }
            }

            /// <summary>
            /// If appropriate, cycle to the next/prev road type.
            /// </summary>
            private void AdjustType()
            {
                if (UpDownIsType)
                {
                    WayPoint.Path path = ActivePath;
                    if (path != null)
                    {
                        Debug.Assert(path.Road != null);

                        /// Note that since Actions.PrevType includes ShiftTab, and
                        /// Actions.NextType includes Tab, you have to check for PrevType
                        /// first and only if it's false check for NextType.
                        if (Actions.PrevType.WasPressedOrRepeat)
                        {
                            path.Road.AdvanceGen(-1);
                            Changed();
                        }
                        else if (Actions.NextType.WasPressedOrRepeat)
                        {
                            path.Road.AdvanceGen(1);
                            Changed();
                        }
                    }
                }
            }

            #region UNUSED ROTATION CODE
            /// <summary>
            /// Unused code for rotating paths.
            /// </summary>
            private void RotateEdge()
            {
                Debug.Assert(ActiveEdge != null);

                Matrix frameRot = Matrix.Identity;
                if (FrameRotation(ref frameRot))
                {
                    StartMovement();
                    ActiveEdge.Rotate(frameRot);
                }
            }

            /// <summary>
            /// Unused code for rotating paths.
            /// </summary>
            private void RotatePath()
            {
                Debug.Assert(ActivePath != null);

                Matrix frameRot = Matrix.Identity;
                if (FrameRotation(ref frameRot))
                {
                    StartMovement();
                    ActivePath.Rotate(frameRot);
                }
            }

            /// <summary>
            /// Unused code for rotating paths.
            /// </summary>
            /// <param name="frameRot"></param>
            /// <returns></returns>
            private bool FrameRotation(ref Matrix frameRot)
            {
                float thisRot = Parent.Camera.Rotation;
                Vector3 pivot = Vector3.Zero;
                if(ActiveNode != null)
                    pivot = ActiveNode.Position;
                else if(ActiveEdge != null)
                    pivot = ActiveEdge.HandlePosition;

                bool didRot = false;
                float kMinRot = 0.01f;
                if (Math.Abs(lastRot - thisRot) > kMinRot)
                {
                    frameRot = Matrix.CreateTranslation(-pivot)
                        * Matrix.CreateRotationZ(thisRot - lastRot)
                        * Matrix.CreateTranslation(pivot);
                    lastRot = thisRot;
                    didRot = true;
                }

                return didRot;
            }

            /// <summary>
            /// Unused code for rotating paths.
            /// </summary>
            private void ClearRot()
            {
                lastRot = Parent.Camera.Rotation;
            }

            /// <summary>
            /// Unused variable for rotating paths.
            /// </summary>
            private float lastRot = 0.0f;
            #endregion UNUSED ROTATION CODE

            /// <summary>
            /// If appropriate cycle the path to the next/prev color.
            /// </summary>
            private void AdjustColor()
            {
                WayPoint.Path path = ActivePath;
                if (path != null)
                {
                    int adv = Actions.ColorRight.WasPressedOrRepeat ? 1 : -1;
                    AdvanceSystemColor(adv);
                    path.IndexColor = Parent.shared.curObjectColor;
                }
            }

            /// <summary>
            /// Advance or decrement the system color palette selection.
            /// </summary>
            /// <param name="delta"></param>
            private void AdvanceSystemColor(int delta)
            {
                WayPoint.Path path = ActivePath;
                if (path != null)
                {
                    int index = ColorPalette.GetIndexFromColor(path.Color);
                    index = (index + ColorPalette.NumEntries + delta) % ColorPalette.NumEntries;
                    Parent.shared.curObjectColor = index;
                }
            }

            /// <summary>
            /// Compute delta position for this frame. Combines horizontal cursor movement
            /// and D-pad up/down. Zeros out noise.
            /// </summary>
            /// <param name="pad"></param>
            /// <returns></returns>
            private Vector3 DeltaPosition()
            {
                float secs = Time.WallClockFrameSeconds;

                float dheight = 0.0f;
                if (UpDownIsMove)
                {
                    dheight += Actions.Raise.IsPressed ? 1.0f : 0.0f;
                    dheight += Actions.Lower.IsPressed ? -1.0f : 0.0f;
                    dheight *= 4.0f * secs;
                }

                Vector3 delta = new Vector3(CursorPosition2d() - lastCursorPos, dheight);

                float kSmall = 0.0001f;
                if (Math.Abs(delta.X) < kSmall)
                    delta.X = 0.0f;
                if (Math.Abs(delta.Y) < kSmall)
                    delta.Y = 0.0f;
                if (Math.Abs(delta.Z) < kSmall)
                    delta.Z = 0.0f;

                return delta;
            }


            /// <summary>
            /// Delete input node with effects.
            /// </summary>
            /// <param name="node"></param>
            private static void DeleteNode(WayPoint.Node node)
            {
                if (node != null)
                {
                    DeleteEffectAt(node.Position);
                    node.Delete();
                    Click(ModeChange.Delete);
                    Changed();
                }
            }

            /// <summary>
            /// Drop a deletion effect at pos.
            /// </summary>
            /// <param name="pos"></param>
            private static void DeleteEffectAt(Vector3 pos)
            {
                Vector3 explodePos = pos;
                float minHeight = Terrain.GetTerrainAndPathHeight(explodePos)
                    + WayPoint.Node.SphereHeight
                    + WayPoint.Node.Radius;
                explodePos.Z = Math.Max(explodePos.Z, minHeight);
                ExplosionManager.CreateSpark(explodePos, 100, 0.1f, 1.5f, new Vector4(0.2f, 0.1f, 3.0f, 1.0f));
            }

            /// <summary>
            /// Delete an edge with effects.
            /// </summary>
            /// <param name="edge"></param>
            private static void DeleteEdge(WayPoint.Edge edge)
            {
                DeleteEffectAt(edge.Node0.Position);
                DeleteEffectAt(edge.Node1.Position);
                edge.Delete();
                Click(ModeChange.Delete);
                Changed();
            }

            /// <summary>
            /// Delete a whole path with effects.
            /// </summary>
            /// <param name="path"></param>
            public static void DeletePath(WayPoint.Path path)
            {
                for (int i = 0; i < path.Nodes.Count; ++i)
                {
                    DeleteEffectAt(path.Nodes[i].Position);
                }
                WayPoint.RemovePath(path);
                Click(ModeChange.Delete);
                Changed();
            }

            /// objDist is distance to nearest object. 
            /// If we are in unknown mode, we grab a node or edge only if
            ///     dist < objDist && dist < snapCapDist
            /// if we are in edit mode (Node,Path,Edge), then we give it up only if
            ///     dist > objDist || dist > snapRelDist
            /// if we are in AddMode or MoveMode we don't care and won't change.
            /// <summary>
            /// Check for mode changes based on cursor movement.
            /// </summary>
            /// <param name="maxDist"></param>
            /// <returns></returns>
            public object CheckCursor(float objDist)
            {
                float objDistSq = objDist * objDist;
                Vector2 cursorPos = CursorPosition2d();

                ///First, look for the modes where we don't care and won't change.
                switch (Mode)
                {
                    case EditMode.AddNode:
                    case EditMode.MovePath:
                    case EditMode.MoveNode:
                    case EditMode.MoveEdge:
                        return SnapObject;
                    case EditMode.Path:
                        CheckRelease(cursorPos, objDistSq);
                        return SnapObject;
                }

                float nodeDistSq = float.MaxValue;
                WayPoint.Node closestNode = WayPoint.GetNearestWayPoint(cursorPos);
                if (closestNode != null)
                {
                    nodeDistSq = closestNode.DistanceSquared(cursorPos);
                }

                float edgeDistSq = float.MaxValue;
                WayPoint.Edge closestEdge = WayPoint.GetNearestEdgeHandle(cursorPos);
                if (closestEdge != null)
                {
                    edgeDistSq = closestEdge.DistanceSquaredToHandle(cursorPos);
                }

                if (closestNode != null)
                {
                    /// Assuming that if we found an edge we must have found a node.

                    if (nodeDistSq < edgeDistSq)
                    {
                        if (CanGrab(nodeDistSq, objDistSq))
                        {
                            Mode = EditMode.Node;
                            ActiveNode = closestNode;
                        }
                    }
                    else
                    {
                        if (CanGrab(edgeDistSq, objDistSq))
                        {
                            Mode = EditMode.Edge;
                            ActiveEdge = closestEdge;
                        }
                    }
                }

                CheckRelease(cursorPos, objDistSq);

                return SnapObject;
            }

            /// <summary>
            /// See if the currently selected node or edge is far enough from the cursor to escape.
            /// </summary>
            /// <param name="cursorPos"></param>
            /// <param name="objDistSq"></param>
            private void CheckRelease(Vector2 cursorPos, float objDistSq)
            {
                CheckReleaseNode(cursorPos, objDistSq);
                CheckReleaseEdge(cursorPos, objDistSq);
            }

            /// <summary>
            /// Look at where the cursor is relative to our path, and update mode
            /// accordingly. If it gets close enough to an unselected node/edge, it will get captured
            /// by it. But if it gets far enough from the selected node/edge, it will break free.
            /// </summary>
            /// <param name="path"></param>
            /// <param name="moveMode"></param>
            private void CheckCursor(WayPoint.Path path, bool moveMode)
            {
                Vector2 cursorPos = CursorPosition2d();

                WayPoint.Node closestNode = path.GetNearestNode(cursorPos);
                float nodeDistSq = closestNode != null ? closestNode.DistanceSquared(cursorPos) : float.MaxValue;
                if (nodeDistSq > SnapReleaseRadiusSq)
                    closestNode = null;

                WayPoint.Edge closestEdge = path.GetNearestEdgeHandle(cursorPos);
                float edgeDistSq = closestEdge != null ? closestEdge.DistanceSquaredToHandle(cursorPos) : float.MaxValue;
                if (edgeDistSq > SnapReleaseRadiusSq)
                    closestEdge = null;

                if ((closestNode != null) && (closestEdge != null))
                {
                    if (nodeDistSq > edgeDistSq)
                        closestNode = null;
                    else
                        closestEdge = null;
                }
                Debug.Assert((closestNode == null) || (closestEdge == null));

                if (closestNode != null)
                {
                    ActiveNode = closestNode;
                    Mode = moveMode ? EditMode.MoveNode : EditMode.Node;
                }
                else if (closestEdge != null)
                {
                    ActiveEdge = closestEdge;
                    Mode = moveMode ? EditMode.MoveEdge : EditMode.Edge;
                }
                else // both are null
                {
                    ActiveNode = null;
                    ActiveEdge = null;
                    Mode = EditMode.Unknown;
                }

            }

            /// <summary>
            /// See if we're close enough to a node/edge to grab it, which includes
            /// being closer than the closest object distance.
            /// </summary>
            /// <param name="distSq"></param>
            /// <param name="objDistSq"></param>
            /// <returns></returns>
            private bool CanGrab(float distSq, float objDistSq)
            {
                if (Mode == EditMode.Unknown)
                {
                    if (distSq > objDistSq)
                        return false;
                }
                return distSq < SnapCaptureRadiusSq;
            }

            /// <summary>
            /// Are we far enough away for the selected to escape?
            /// </summary>
            /// <param name="distSq"></param>
            /// <param name="objDistSq"></param>
            /// <returns></returns>
            private bool MustRelease(float distSq, float objDistSq)
            {
                if (distSq > SnapReleaseRadiusSq)
                    return true;

                switch (Mode)
                {
                    case EditMode.Node:
                    case EditMode.Path:
                    case EditMode.Edge:
                        {
                            if (distSq > objDistSq)
                                return true;
                        }
                        break;
                }

                return false;
            }

            /// <summary>
            /// See if there's a selected node that's reached escape distance.
            /// </summary>
            /// <param name="cursorPos"></param>
            /// <param name="objDistSq"></param>
            private void CheckReleaseNode(Vector2 cursorPos, float objDistSq)
            {
                if (ActiveNode != null)
                {
                    float distSq = ActiveNode.DistanceSquared(cursorPos);
                    if (MustRelease(distSq, objDistSq))
                    {
                        Mode = EditMode.Unknown;
                        ActiveNode = null;
                    }
                }
            }

            /// <summary>
            /// See if there's selected edge that's reached escape distance.
            /// </summary>
            /// <param name="cursorPos"></param>
            /// <param name="objDistSq"></param>
            private void CheckReleaseEdge(Vector2 cursorPos, float objDistSq)
            {
                if (ActiveEdge != null)
                {
                    float distSq = ActiveEdge.DistanceSquaredToHandle(cursorPos);
                    if (MustRelease(distSq, objDistSq))
                    {
                        Mode = EditMode.Unknown;
                        ActiveEdge = null;
                    }
                }
            }

            /// <summary>
            /// Helper for the current cursor position.
            /// </summary>
            /// <returns></returns>
            protected Vector3 CursorPosition()
            {
                return InGame.inGame.shared.CursorPosition;
            }

            /// <summary>
            /// Helper for the current horizontal cursor position.
            /// </summary>
            /// <returns></returns>
            protected Vector2 CursorPosition2d()
            {
                Vector3 cursorPos = CursorPosition();

                return new Vector2(cursorPos.X, cursorPos.Y);
            }

            /// <summary>
            /// Just a bunch of assertions that the current state is self consistent.
            /// </summary>
            private void ValidateMode()
            {
                if (ActiveNode != null)
                {
                    Debug.Assert(ActiveEdge == null);
                    Debug.Assert(
                        (Mode != EditMode.Edge)
                        && (Mode != EditMode.MoveEdge));
                }
                if (ActiveEdge != null)
                {
                    Debug.Assert(ActiveNode == null);
                    Debug.Assert(
                        (Mode != EditMode.Node)
                        && (Mode != EditMode.MoveNode));
                }
                switch (Mode)
                {
                    case EditMode.Unknown:
                        Debug.Assert(ActiveNode == null);
                        Debug.Assert(ActiveEdge == null);
                        break;
                    case EditMode.Node:
                    case EditMode.MoveNode:
                        Debug.Assert(ActiveNode != null);
                        Debug.Assert(ActiveEdge == null);
                        break;
                    case EditMode.Edge:
                    case EditMode.MoveEdge:
                        Debug.Assert(ActiveNode == null);
                        Debug.Assert(ActiveEdge != null);
                        break;
                    case EditMode.Path:
                    case EditMode.MovePath:
                        Debug.Assert((ActiveNode == null) || (ActiveEdge == null));
                        Debug.Assert((ActiveNode != null) || (ActiveEdge != null));
                        break;
                    case EditMode.AddNode:
                        Debug.Assert(FromNode != null);
                        break;

                }
            }

            /// <summary>
            /// Helper to force a save.
            /// </summary>
            private static void Changed()
            {
                InGame.IsLevelDirty = true;
            }

            #endregion Internal
        }
    }
}
