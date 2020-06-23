
/// Define MF_PITCH_OR_YAW for a mode that lets the user be affecting
/// pitch OR yaw, but not both at a time. The mode is determined when
/// the right button is down and the mouse moves. 
// #define MF_PITCH_OR_YAW 

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Boku.Base;
using Boku.Common;
using Boku.Fx;
using Boku.SimWorld.Collision;
using Boku.SimWorld.Terra;
using Boku.SimWorld;

namespace Boku
{
    /// <summary>
    /// This section of InGame deals with the mouse control of the camera
    /// and picking objects.
    /// </summary>
    public class MouseEdit
    {
        #region Child Classes
        /// <summary>
        /// Overblown struct for holding the global information on what objects the mouse
        /// is over.
        /// </summary>
        public class MouseHitInfo
        {
            #region Members
            /// <summary>
            /// Actor under mouse.
            /// </summary>
            private GameActor actorHit = null;
            /// <summary>
            /// LOS ray hit on actorHit's collision bounds.
            /// </summary>
            private Vector3 actorPosition = Vector3.Zero;

            /// <summary>
            /// Position of hit with terrain.
            /// </summary>
            private Vector3 terrainPosition = Vector3.Zero;
            /// <summary>
            /// Whether terrain is under mouse.
            /// </summary>
            private bool terrainHit = false;
            /// <summary>
            /// True if no terrain under mouse, but ray hits zero plane.
            /// </summary>
            private bool zeroPlaneHit = false;

            /// <summary>
            /// The material on the terrain where the mouse LOS hits. Undefined if !terrainHit.
            /// </summary>
            private ushort terrainMaterial = 0;

            /// <summary>
            /// If we're dragging around this bot via it's anchor this
            /// will be the vertical offset from the anchor to the bot.
            /// </summary>
            private float verticalOffset = 0.0f;

            #endregion Members

            #region Accessors

            /// <summary>
            /// Is there an unoccluded actor under the mouse?
            /// </summary>
            public bool HaveActor
            {
                get { return actorHit != null; }
            }

            /// <summary>
            /// Return any unoccluded actor under the mouse. Must be
            /// closer than any other other actor under mouse, and closer than terrain.
            /// </summary>
            public GameActor ActorHit
            {
                get { return actorHit; }
                internal set { actorHit = value; }
            }

            /// <summary>
            /// Where the mouse LOS ray hits the ActorHit's collision hull. For
            /// mobile bots, that's a sphere, for other bots (e.g. factory) it's 
            /// a set of collision primitives. Undefined if !HaveActor.
            /// </summary>
            public Vector3 ActorPosition
            {
                get 
                {
                    Vector3 pos = actorPosition;

                    /*
                    // Not needed here.  Want to snap the actor's position, not the position of the ray intersect.
                    if (InGame.inGame.SnapToGrid)
                    {
                        // Snap the Actor's position to the center of a terrain cube.
                        pos = InGame.inGame.SnapPosition(pos);
                    }
                    */

                    return pos;
                }
                internal set { actorPosition = value; }
            }

            /// <summary>
            /// Whether current mouse LOS ray hits the terrain
            /// </summary>
            public bool TerrainHit
            {
                get { return terrainHit; }
                internal set { terrainHit = value; }
            }

            /// <summary>
            /// True if current mouse LOS hits no terrain, but does cross the 
            /// zero height plane.
            /// </summary>
            public bool ZeroPlaneHit
            {
                get { return zeroPlaneHit; }
                internal set { zeroPlaneHit = value; }
            }

            /// <summary>
            /// Where the ray hits terrain (or zero plane, check flags).
            /// </summary>
            public Vector3 TerrainPosition
            {
                get 
                {
                    Vector3 pos = terrainPosition;

                    if (InGame.inGame.SnapToGrid)
                    {
                        // Snap the position of the hit to the center of the terrain cube.
                        pos = InGame.SnapPosition(pos);
                    }

                    return pos; 
                }
                internal set { terrainPosition = value; }
            }

            /// <summary>
            /// What terrain material is under the mouse. Undefined if !TerrainHit.
            /// </summary>
            public ushort TerrainMaterial
            {
                get { return terrainMaterial; }
                internal set { terrainMaterial = value; }
            }

            /// <summary>
            /// If we're dragging around this bot via it's anchor this
            /// will be the vertical offset from the anchor to the bot.
            /// </summary>
            public float VerticalOffset
            {
                get { return verticalOffset; }
                set { verticalOffset = value; }
            }

            #endregion Accessors

            #region Public
            #endregion Public

            #region Internal
            /// <summary>
            /// Reset before doing another test.
            /// </summary>
            internal void Clear()
            {
                actorHit = null;
                actorPosition = Vector3.Zero;
                terrainHit = false;
                terrainPosition = Vector3.Zero;
            }
            #endregion Internal
        };

        #endregion Child Classes

        #region Members
        private InGame owner = null;

        private enum OrbitMode
        {
            Pitch,
            Yaw,
            None
        };
        private OrbitMode orbitMode;

        Vector4 pixelToNDC;
        Vector4 ndcToPixel;
        Vector2 cursorOffset;
        Vector2 resolution;

        private GameActor quasiSelected = null;
        private Distortion quasiHighlight = null;

        private Vector2 clickPosition;

        private float clickTime = 0.0f;
        private float moveTime = 0.0f;
        private Vector2 moveFrom = Vector2.Zero;
        private Vector2 moveTo = Vector2.Zero;
        private float kDoubleClickTimeOut = 0.25f;

        private GameActor dragObject = null;
        private Distortion dragHighlight = null;
        private bool dragSelected = false;

        private static bool disableLeftDrag = false;
        private static bool disableRightOrbit = false;

        private static float kMoveTotalTime = 0.15f;
        private static float kMaxRayCast = 500.0f;

        private static MouseHitInfo mouseHitInfo = new MouseHitInfo();
        #endregion Members

        #region Accessors

        /// <summary>
        /// This frame's cached info on what the mouse is over. Pretty much read only,
        /// maintained internally.
        /// </summary>
        public static MouseHitInfo HitInfo
        {
            get { return mouseHitInfo; }
        }

        public GameActor HighLit
        {
            get { return quasiSelected; }
        }

        /// <summary>
        /// Return a float for how strong the left trigger, virtualized to
        /// mouse input, currently is.
        /// </summary>
        public float LeftTrigger
        {
            get
            {
                return MouseInput.Left.IsPressed && KeyboardInput.CtrlIsPressed
                    ? (KeyboardInput.ShiftIsPressed ? 1.0f : 0.25f)
                    : 0.0f;
            }
        }

        /// <summary>
        /// Return a float for how strong the right trigger, virtualized to
        /// mouse input, currently is.
        /// </summary>
        public float RightTrigger
        {
            get
            {
                return MouseInput.Left.IsPressed && KeyboardInput.IsPressed(Keys.Space)
                    ? (KeyboardInput.ShiftIsPressed ? 1.0f : 0.25f)
                    : 0.0f; 
            }
        }

        /// <summary>
        /// Return a float for how strong the middle trigger (A button), virtualized to
        /// mouse input, currently is.
        /// </summary>
        public float MiddleTrigger
        {
            get
            {
                return MouseInput.Left.IsPressed && KeyboardInput.AltIsPressed
                    ? (KeyboardInput.ShiftIsPressed ? 0.5f : 0.25f)
                    : 0.0f;
            }
        }

        /// <summary>
        /// True if left action is indicated (analogous to gamepad left trigger).
        /// </summary>
        public bool LeftAction
        {
            get { return MouseInput.Left.WasPressed && KeyboardInput.CtrlIsPressed; }
        }

        /// <summary>
        /// True if middle action is indicated, analogous to gamepad A button.
        /// </summary>
        public bool MiddleAction
        {
            get { return MouseInput.Left.WasPressed && KeyboardInput.AltIsPressed; }
        }

        /// <summary>
        /// True if right action is indicated, analogous to gamepad right trigger.
        /// </summary>
        public bool RightAction
        {
            get { return MouseInput.Left.WasPressed && KeyboardInput.IsPressed(Keys.Space); }
        }

        /// <summary>
        /// True if any kind of move was initiated this frame. Only checks input,
        /// not whether action actually started.
        /// </summary>
        public bool StartMove
        {
            get { return StartDrag || StartRaise || StartRotate; }
        }

        /// <summary>
        /// True if an object drag was initiated this frame. Only checks input,
        /// not whether action actually started.
        /// </summary>
        public bool StartDrag
        {
            get { return MouseInput.Left.WasPressed && !KeyboardInput.ShiftIsPressed; }
        }

        /// <summary>
        /// True if an object raise/lower was initiated this frame. Only checks input,
        /// not whether action actually started.
        /// </summary>
        public bool StartRaise
        {
            get { return MouseInput.Left.WasPressed && KeyboardInput.ShiftIsPressed; }
        }

        /// <summary>
        /// True if an object rotation was initiated this frame. Only checks input,
        /// not whether action actually started.
        /// </summary>
        public bool StartRotate
        {
            get { return MouseInput.Right.WasPressed && KeyboardInput.ShiftIsPressed; }
        }

        /// <summary>
        /// True if input for any kind of move is active.
        /// </summary>
        public bool ContinueMove
        {
            get { return ContinueDrag || ContinueRaise || ContinueRotate; }
        }

        /// <summary>
        /// True if input for an object drag is active.
        /// </summary>
        public bool ContinueDrag
        {
            get { return MouseInput.Left.IsPressed && !KeyboardInput.ShiftIsPressed; }
        }

        /// <summary>
        /// True if input for object raise/lower is active.
        /// </summary>
        public bool ContinueRaise
        {
            get { return MouseInput.Left.IsPressed && KeyboardInput.ShiftIsPressed; }
        }

        /// <summary>
        /// True if input for object rotation is active.
        /// </summary>
        public bool ContinueRotate
        {
            get { return MouseInput.Right.IsPressed && KeyboardInput.ShiftIsPressed; }
        }

        #region Internal
        /// <summary>
        /// Are we currently dragging an object?
        /// </summary>
        private bool DraggingObject
        {
            get { return dragHighlight != null; }
        }
        #endregion Internal

        #endregion Accessors

        #region Public
        /// <summary>
        /// Constructor, grab ref to the owning InGame.
        /// </summary>
        /// <param name="inGame"></param>
        public MouseEdit(InGame inGame)
        {
            owner = inGame;
        }

        /// <summary>
        /// Return the id for the appropriate help overlay for the current
        /// state of object edit with mouse/keyboard. This should be overridden
        /// by waypoint edit if waypoint edit is ongoing.
        /// </summary>
        /// <returns></returns>
        public string UpdateHelpOverlay()
        {
            string helpID = null;
            if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
            {
                if (quasiSelected == null)
                {
                    if (owner.HaveClipboard)
                    {
                        helpID = "ObjectEdit";
                    }
                    else
                    {
                        helpID = "ObjectEditEmptyClipboard";
                    }
                }
                else
                {
                    if (quasiSelected.IsTree)
                    {
                        helpID = "TreeEditMouseOver";
                    }
                    else
                    {
                        helpID = "ObjectEditMouseOver";
                    }
                }
            }
            return helpID;
        }

        /// <summary>
        /// Reset state to initial.
        /// </summary>
        public void Clear()
        {
            EndQuasi();
            quasiSelected = null;
            clickTime = 0;
            moveTime = 0;
            orbitMode = OrbitMode.None;

            EndDrag();
        }

        /// <summary>
        /// Handle any camera orbit from mouse control.
        /// </summary>
        /// <param name="camera"></param>
        public void DoCamera(SmoothCamera camera)
        {
            CacheTransforms(camera);
#if MF_PITCH_OR_YAW
            if (CheckOrbitMode())
            {
                Orbit(camera);
            }
#else // MF_PITCH_OR_YAW
            //if (AffectingOrbit)
            {
                PitchAndYaw(camera);
            }
#endif // MF_PITCH_OR_YAW
            disableRightOrbit = false;
        }

        /// <summary>
        /// Zoom in or out.
        /// </summary>
        /// <param name="camera"></param>
        public void DoZoom(SmoothCamera camera)
        {
            // Don't zoom if the AddItem pie menu or pickers are active.
            if (InGame.inGame.editObjectUpdateObj.newItemSelectorShim.State == Boku.UI.UIShim.States.Active 
                || InGame.inGame.mouseEditUpdateObj.PickersActive)
            {
                return;
            }

            int scrollChange = MouseInput.ScrollWheel - MouseInput.PrevScrollWheel;

            float scrollRate = 0.2f;

            // Adjust rate for keyboard.
            if (Actions.ZoomIn.IsPressed || Actions.ZoomOut.IsPressed)
            {
                scrollRate = Time.WallClockFrameSeconds;
            }

            // F4 Recenter camera:
            //  If any characters in game, move to nearest.
            //  else center at origin.
            //  Adjust zoom to reasonable value.
            //  Adjust camera height to reasonable angle.
            if (KeyboardInput.WasPressed(Keys.F4))
            {
                // Find nearest actor position.
                Vector3 nearestPosition = Vector3.Zero;
                float dist2 = float.MaxValue;
                foreach (GameThing thing in InGame.inGame.gameThingList)
                {
                    if(!(thing is CursorThing))
                    {
                        float d2 = (thing.Movement.Position - camera.ActualAt).LengthSquared();
                        if (d2 < dist2)
                        {
                            nearestPosition = thing.Movement.Position;
                            dist2 = d2;
                        }
                    }
                }

                // Apply new position to cursor.  Camera follows the cursor.
                Boku.InGame.inGame.shared.CursorPosition = nearestPosition;

                camera.DesiredDistance = 20.0f;
                camera.DesiredPitch = -0.5f;     // ~30 degrees.

            }   // end if F4 pressed.

            if (scrollChange < 0 || Actions.ZoomOut.IsPressed)
            {
                camera.DesiredDistance *= 1.0f + scrollRate;
            }
            else if (scrollChange > 0 || Actions.ZoomIn.IsPressed)
            {
                if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                {
                    // Calc where the mouse is aiming and move the cursor towards there.  
                    // The camera will follow.
                    Vector3 mouseAimPosition = FindHit(camera, MouseInput.Position);
                    InGame.inGame.shared.CursorPosition = MyMath.Lerp(camera.ActualAt, mouseAimPosition, 1.5f * scrollRate);
                }

                float desiredDistance = camera.DesiredDistance * (1.0f - scrollRate);
                // If not in RunSim mode, don't allow the camera to get closer than 4 meters.
                if (InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.RunSim)
                {
                    desiredDistance = Math.Max(4.0f, desiredDistance);
                }
                camera.DesiredDistance = desiredDistance;
            }
        }

        /// <summary>
        /// Handle any cursor movement (translation) from mouse control.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public Vector2 DoCursor(Camera camera, Vector2 position)
        {
            //bool affectingDrag = AffectingDrag && (moveTime <= 0);
            bool affectingDrag = moveTime <= 0;
            owner.Cursor3D.Hidden = affectingDrag;

            CheckReCenter(camera);

            if (DraggingObject)
            {
                DragObject(camera);
            }
            else if (affectingDrag)
            {
                position = Drag(camera, position);
            }
            position = CheckMoveTo(position);

            disableLeftDrag = false;
            return position;
        }

        /// <summary>
        /// Handle object edit controls via mouse.
        /// </summary>
        public void DoObject(Camera camera)
        {
            ChangeQuasi(mouseHitInfo.ActorHit);
            CheckQuasi(false);
            CheckDragObject();

            CheckCloneDelete(camera);
        }

        public void KillSelectionHighlight()
        {
            CheckQuasi(true);
        }

        /// <summary>
        /// Disable using the left mouse click to drag the terrain (move the camera).
        /// Is reset every frame.
        /// </summary>
        public static void DisableLeftDrag()
        {
            disableLeftDrag = true;
        }

        /// <summary>
        /// Disable using the right mouse click to orbit the camera.
        /// Is reset every frame.
        /// </summary>
        public static void DisableRightOrbit()
        {
            disableRightOrbit = true;
        }

        /// <summary>
        /// Cache away info about what object and/or terrain are
        /// currently under the mouse.
        /// </summary>
        /// <param name="camera"></param>
        public static void Update(Camera camera)
        {
            Vector3 src = camera.ActualFrom;
            Vector2 position = MouseInput.PositionVec;
            Vector3 ray = FindRay(camera, position);
            Vector3 dst = src + ray * 100;

            // If we're dragging a bot, then we must be hitting it, right?
            if (InGame.inGame.mouseEditUpdateObj.ToolBox.EditObjectsToolInstance.DraggingObject)
            {
                // Nothing to do here.  If we're dragging an object with the mouse then by definition
                // it is under the mouse pointer and needs to stay there.  So, don't clear mouseHitInfo
                // or even bother testing the other objects.
            }
            else
            {
                mouseHitInfo.Clear();

                float distToNearestHit = float.MaxValue;

                // First, loop through the gamethings and test against all things with CollisionPrims.
                {
                    CollInfo collInfo = new CollInfo();
                    for (int i = 0; i < InGame.inGame.gameThingList.Count; i++)
                    {
                        GameActor ga = InGame.inGame.gameThingList[i] as GameActor;

                        if (ga != null && ga != CameraInfo.FirstPersonActor && ga.SRO.CollisionPrims != null)
                        {
                            // The CollisionPrimitive Collide() function is done in model space so we
                            // need to offset the ray by the position of the model in world space.
                            Vector3 offset = ga.Movement.Position;

                            foreach (CollisionPrimitive cp in ga.SRO.CollisionPrims)
                            {
                                if (cp.Collide(src - offset, dst - offset, 0.1f, ref collInfo))
                                {
                                    // Do we have a closer hit?
                                    if (collInfo.DistSq < distToNearestHit * distToNearestHit)
                                    {
                                        distToNearestHit = (float)Math.Sqrt(collInfo.DistSq);
                                        mouseHitInfo.ActorHit = ga;
                                        mouseHitInfo.ActorPosition = collInfo.Contact + offset;

                                        // Update dst so we don't bother testing actors further
                                        // away than this.
                                        dst = src + ray * distToNearestHit;
                                    }
                                }
                            }   // end of loop over each collision prim.
                        }   // end if there are any collision prims.
                    }   // end loop over gamethings.
                }

                /*
                if (CollSys.TestBest(src, dst, 0.25f, _scratchHits))
                {
                    Debug.Assert(_scratchHits.Count > 0, "TestBest returned true but hit list empty.");
                    mouseHitInfo.ActorHit = _scratchHits[0].Other as GameActor;
                    Debug.Assert(mouseHitInfo.ActorHit != null, "Non-game actor returned in hit list.");
                    mouseHitInfo.ActorPosition = _scratchHits[0].Contact;
                }
                _scratchHits.Clear();
                */

                // The collision system ignores creatables and ghosts so we have to work with them here.
                // Since we now also want to work with anchors we completely skip the above collision system
                // test and just use this loop for testing.
                // In this loop we use the collision sphere for testing and skip over the StaticProps which
                // have collisionPrims and were tested above.

                // Second, loop through gamething list again, this time looking at things without collision prims.
                {
                    for (int i = 0; i < InGame.inGame.gameThingList.Count; i++)
                    {
                        GameActor ga = InGame.inGame.gameThingList[i] as GameActor;

                        // Ignore null and first person actor.
                        if (ga != null && ga != CameraInfo.FirstPersonActor)
                        {
                            // Hit test the ray against the bot's collision sphere.
                            Vector3 center = ga.CollisionSphere.Center + ga.Movement.Position;
                            Vector3 dirToBot = center - src;
                            float dot = Vector3.Dot(dirToBot, ray);
                            Vector3 nearestPoint = dot * ray + src;
                            Vector3 delta = nearestPoint - center;
                            float radius = delta.Length();

                            // Some bots (lights) have a tiny hit radius so use a min value to make them pickable.
                            float minRadius = 0.2f;
                            float collisionRadius = MathHelper.Max(ga.CollisionSphere.Radius, minRadius);

                            // No need to test actors with collisionPrims since we did it above.
                            // but we still may want to check thier anchor points below.
                            if (ga.SRO.CollisionPrims == null)
                            {
                                if (radius < collisionRadius)
                                {
                                    float distToCreatable = (nearestPoint - src).Length();
                                    mouseHitInfo.VerticalOffset = 0.0f;

                                    if (mouseHitInfo.ActorHit == null)
                                    {
                                        // No previous hit, so just fill in the result.
                                        mouseHitInfo.ActorHit = ga;
                                        mouseHitInfo.ActorPosition = src + ray * distToCreatable;
                                    }
                                    else
                                    {
                                        // Compare dist with existing hit.
                                        float distToExisting = (mouseHitInfo.ActorPosition - src).Length();
                                        if (distToCreatable < distToExisting)
                                        {
                                            // New one is closer, so replace previous.
                                            mouseHitInfo.ActorHit = ga;
                                            mouseHitInfo.ActorPosition = src + ray * distToCreatable;
                                        }
                                    }
                                }
                            }

                            // If in key/mouse mode also allow picking bots by selecting their anchor points.
                            // This is the point directly under the bot at terrain level (or atltitude == 0).
                            if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                            {
                                // Hit test the ray against a sphere at the bot's position moved to ground level.
                                // For all bots, use the save hit radius.
                                center = ga.CollisionSphere.Center + ga.Movement.Position;
                                float verticalOffset = center.Z - Terrain.GetTerrainHeight(center);
                                center.Z -= verticalOffset;
                                dirToBot = center - src;
                                dot = Vector3.Dot(dirToBot, ray);
                                nearestPoint = dot * ray + src;
                                delta = nearestPoint - center;
                                radius = delta.Length();
                                // 0.5f is the default radius we're using for the collision sphere at the anchor point.
                                if (radius < 0.5f)
                                {
                                    float distToCreatable = (nearestPoint - src).Length();
                                    mouseHitInfo.VerticalOffset = verticalOffset;

                                    if (mouseHitInfo.ActorHit == null)
                                    {
                                        // No previous hit, so just fill in the result.
                                        mouseHitInfo.ActorHit = ga;
                                        mouseHitInfo.ActorPosition = src + ray * distToCreatable;
                                    }
                                    else
                                    {
                                        // Compare dist with existing hit.
                                        float distToExisting = (mouseHitInfo.ActorPosition - src).Length();
                                        if (distToCreatable < distToExisting)
                                        {
                                            // New one is closer, so replace previous.
                                            mouseHitInfo.ActorHit = ga;
                                            mouseHitInfo.ActorPosition = src + ray * distToCreatable;
                                        }
                                    }
                                }
                            }   // end if KeyMouse mode.
                        }
                    }
                }
            }

            dst = src + ray * kMaxRayCast;
            Vector3 hitPoint = dst;
            if (Terrain.LOSCheckTerrainAndPath(src, dst, ref hitPoint))
            {
                mouseHitInfo.TerrainHit = hitPoint.Z > Terrain.Current.MinHeight * 0.5f;
                mouseHitInfo.TerrainPosition = hitPoint;
                mouseHitInfo.TerrainMaterial = Terrain.GetMaterialType(new Vector2(hitPoint.X, hitPoint.Y));

                // If we don't have a valid material where we hit, then don't count it as a hit.
                // The underlying problem here seems to be that when a material is erased that
                // the matching heightmap entry is not set back to 0.
                if (mouseHitInfo.TerrainMaterial == TerrainMaterial.EmptyMatIdx)
                {
                    mouseHitInfo.TerrainHit = false;
                }

                /// If we have an actor detected under the cursor, see if it was
                /// occluded by terrain.
                if (mouseHitInfo.HaveActor)
                {
                    if (Vector3.DistanceSquared(src, mouseHitInfo.TerrainPosition)
                        < Vector3.DistanceSquared(src, mouseHitInfo.ActorPosition))
                    {
                        mouseHitInfo.ActorHit = null;
                    }
                }
            }
            else
            {
                //Debug.Assert(src.Z > 0, "Assuming camera is always above zero plane.");
                if (ray.Z < 0)
                {
                    mouseHitInfo.TerrainPosition = FindAtHeight(camera, MouseInput.Position, 0);
                    if (Vector3.DistanceSquared(mouseHitInfo.TerrainPosition, src)
                        <= kMaxRayCast * kMaxRayCast)
                    {
                        mouseHitInfo.ZeroPlaneHit = true;
                    }
                }
            }
            if (mouseHitInfo.HaveActor && (InGame.WayPointEdit.MouseOverDistance < float.MaxValue))
            {
                float wayDist = InGame.WayPointEdit.MouseOverDistance;
                float actorDistSq = Vector3.DistanceSquared(src, mouseHitInfo.ActorPosition);

                if (actorDistSq > wayDist * wayDist)
                {
                    mouseHitInfo.ActorHit = null;
                }
            }
        }

        /// <summary>
        /// Test whether the mouse is over a sphere at worldPos.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="worldPos"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static bool MouseOver(Camera camera, Vector3 worldPos, float radius)
        {
            Vector3 ray = FindRay(camera, new Vector2(MouseInput.Position.X, MouseInput.Position.Y));

            Vector3 world = worldPos - camera.ActualFrom;
            Vector3 proj = world - Vector3.Dot(world, ray) * ray;

            return proj.LengthSquared() <= radius * radius;
        }

        /// <summary>
        /// Handle terrain edit controls via mouse.
        /// </summary>
        public Vector2 DoTerrain(Camera camera)
        {
            Vector3 hit = FindHit(camera, MouseInput.Position);

            // Limit hit distance to n units from camera.  This
            // stops painting of far away terrain when camera is
            // almost horizontal.
            float limit = 50.0f + 5.0f * camera.ActualFrom.Z;
            Vector2 pos = new Vector2(hit.X, hit.Y);
            Vector2 cam = new Vector2(camera.ActualFrom.X, camera.ActualFrom.Y);
            Vector2 delta = pos - cam;
            float dist = delta.Length();

            if (dist > limit)
            {
                pos = cam + delta * limit / dist;
            }

            return pos;
        }

        /// <summary>
        /// Return whether a sample of the terrain material under the mouse
        /// was just collected.
        /// </summary>
        /// <returns></returns>
        public static bool TriggerSample()
        {
            return !HitInfo.HaveActor
                && HitInfo.TerrainHit
                && MouseInput.Left.WasPressed;
        }
        #endregion Public

        #region Internal

        /// <summary>
        /// Check whether user has requested any clone/delete/object add type actions,
        /// and perform them if requested.
        /// </summary>
        /// <param name="camera"></param>
        private void CheckCloneDelete(Camera camera)
        {
            if (quasiSelected != null)
            {
                if (RightAction)
                {
                    MouseInput.Left.IgnoreUntilReleased = true;
                    owner.ActivateNewItemSelector(quasiSelected.Movement.Position, true);
                }
                else if (LeftAction)
                {
                    /// Cut
                    owner.CutAction(quasiSelected);
                    quasiSelected = null;
                    EndDrag();
                }
                else if (MiddleAction)
                {
                    owner.CloneAction(quasiSelected);
                }
            }
            else
            {
                if (mouseHitInfo.TerrainHit || mouseHitInfo.ZeroPlaneHit)
                {
                    float terrDistSq = Vector3.DistanceSquared(camera.ActualFrom, mouseHitInfo.TerrainPosition);
                    float wayDist = InGame.WayPointEdit.MouseOverDistance;
                    if (terrDistSq < wayDist * wayDist)
                    {
                        if (RightAction)
                        {
                            MouseInput.Left.IgnoreUntilReleased = true;
                            owner.ActivateNewItemSelector(mouseHitInfo.TerrainPosition, true);
                        }
                        else if (MiddleAction)
                        {
                            Vector3 pos = FindHit(camera, MouseInput.Position);
                            owner.PasteAction(null, pos);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Internal scratch list for LOS hits. Don't use this except within single function.
        /// </summary>
        private static List<HitInfo> _scratchHits = new List<HitInfo>();

        /// <summary>
        /// See if we've clicked something that should become selected.
        /// </summary>
        /// <param name="camera"></param>
        private void CheckReCenter(Camera camera)
        {
            GameActor focus = HitInfo.ActorHit;
            if (focus == null)
            {
                if (MouseInput.Left.WasPressed)
                {
                    if (clickTime > 0)
                    {
                        /// We've double clicked on terrain. Let's recenter there.
                        if (HitInfo.TerrainHit || HitInfo.ZeroPlaneHit)
                        {
                            ReCenter(HitInfo.TerrainPosition);
                        }
                    }
                    clickTime = kDoubleClickTimeOut;
                }
            }
            else
            {
                /// We're moused over something, check for click actions.
                if (MouseInput.Left.WasPressed)
                {
                    if (clickTime > 0)
                    {
                        ReCenter(focus.Movement.Position);
                    }
                    clickTime = kDoubleClickTimeOut;
                }
            }

            clickTime -= Time.WallClockFrameSeconds;
        }

        /// <summary>
        /// Begin recentering to the new position.
        /// </summary>
        /// <param name="pos"></param>
        private void ReCenter(Vector2 pos)
        {
            moveTo = pos;
            moveFrom = owner.Cursor3D.Position2d;
            moveTime = kMoveTotalTime;
        }

        /// <summary>
        /// ReCenter the world to the new position.
        /// </summary>
        /// <param name="pos"></param>
        private void ReCenter(Vector3 pos)
        {
            ReCenter(new Vector2(pos.X, pos.Y));
        }

        /// <summary>
        /// Is the mouse currently moving?
        /// </summary>
        /// <returns></returns>
        private bool MouseMoving()
        {
            return (MouseInput.Position.X != MouseInput.PrevPosition.X)
                || (MouseInput.Position.Y != MouseInput.PrevPosition.Y);
        }

        /// <summary>
        /// Drag or release object based on mouse left click.
        /// </summary>
        private void CheckDragObject()
        {
            if (StartMove)
            {
                if ((quasiSelected != null) && (dragObject == null) && (moveTime <= 0))
                {
                    dragObject = quasiSelected;
                    dragSelected = owner.IsSelected(dragObject);

                    dragHighlight = owner.MakeAura(dragObject, 0.35f);

                    dragHighlight.TintAura(1.0f, 0.0f, 1.0f);
                }
            }
            else if (!ContinueMove)
            {
                if (dragObject != null)
                {
                    if (dragSelected)
                    {
                        ReCenter(dragObject.Movement.Position);
                    }

                    EndDrag();
                }
            }
        }

        /// <summary>
        /// Terminate the visual drag effect.
        /// </summary>
        private void EndDrag()
        {            
            if (dragObject != null)
            {
                dragObject = null;
            }

            if (dragHighlight != null)
            {
                dragHighlight.Die();
                dragHighlight = null;
            }
        }

        /// <summary>
        /// If the cursor is currently transitioning to a new spot, move it along.
        /// </summary>
        /// <param name="move"></param>
        /// <returns></returns>
        private Vector2 CheckMoveTo(Vector2 move)
        {
            if (moveTime > 0)
            {
                moveTime -= Time.WallClockFrameSeconds;
                if (moveTime < 0)
                    moveTime = 0;

                float t = moveTime / kMoveTotalTime;

                move = moveTo + t * (moveFrom - moveTo);
            }
            return move;
        }

        /// <summary>
        /// Kill the highlight for the quasi selected object (yellow) if
        /// we're no longer moused over it.
        /// </summary>
        private void CheckQuasi(bool forceKill)
        {
            if (quasiSelected == null || forceKill)
            {
                EndQuasi();
            }
            else if (owner.IsSelected(quasiSelected))
            {
                EndQuasi();
            }
            else if (DraggingObject)
            {
                EndQuasi();
            }
            if (!forceKill && !DraggingObject && (quasiHighlight == null))
            {
                MakeQuasiHighlight();
            }
        }

        /// <summary>
        /// Generate the mouse over visual highlight.
        /// </summary>
        private void MakeQuasiHighlight()
        {
            if ((quasiSelected != null)) // && !owner.IsSelected(quasiSelected))
            {
                quasiHighlight = owner.MakeAura(quasiSelected, 0.35f);
                quasiHighlight.TintAura(1.0f, 1.0f, 0.0f);
                quasiSelected.ReactToCursor();
            }
        }

        /// <summary>
        /// Create a yellow highlight for the mouse-over'd object.
        /// </summary>
        /// <param name="actor"></param>
        private void ChangeQuasi(GameActor actor)
        {
            if (!DraggingObject)
            {
                if (quasiSelected != actor)
                {
                    EndQuasi();
                    quasiSelected = actor;
                    MakeQuasiHighlight();
                }
            }
        }

        /// <summary>
        /// Kill the mouse-over highlight (yellow).
        /// </summary>
        private void EndQuasi()
        {
            if (quasiHighlight != null)
            {
                quasiHighlight.Die();
                quasiHighlight = null;
            }
        }

        /// <summary>
        /// Rotate the camera by user mouse grab.
        /// </summary>
        /// <param name="camera"></param>
        private void Orbit(SmoothCamera camera)
        {
            /// Either pitch or yaw according to current mode.
            /// 
            if (orbitMode == OrbitMode.Pitch)
            {
                Pitch(camera);
            }
            else
            {
                Yaw(camera);
            }
        }

        /// <summary>
        /// Decide whether user is controlling yaw or pitch.
        /// </summary>
        /// <returns></returns>
        private bool CheckOrbitMode()
        {
            if (MouseInput.Right.WasPressed)
            {
                clickPosition = PixelToNDC(MouseInput.Position) - cursorOffset;
            }
            if (MouseInput.Right.IsPressed)
            {
                if (orbitMode == OrbitMode.None)
                {
                    Vector2 pos = PixelToNDC(MouseInput.Position) - cursorOffset;

                    Vector2 prev = clickPosition;

                    Vector2 move = pos - prev;
                    float kMinPixelMove = 5.0f / 768.0f;
                    if (move.Length() > kMinPixelMove)
                    {
                        if (prev == Vector2.Zero)
                        {
                            orbitMode = OrbitMode.Pitch;
                        }
                        else
                        {
                            move.Normalize();
                            prev.Normalize();

                            float yawStrength = Math.Abs(move.X * prev.Y - move.Y * prev.X);
                            float pitchStrength = Math.Abs(move.Y);

                            Vector2 deadzone = DeadZone(MouseInput.PrevPosition);

                            if (pitchStrength * deadzone.Y > yawStrength * deadzone.X)
                            {
                                orbitMode = OrbitMode.Pitch;
                            }
                            else
                            {
                                orbitMode = OrbitMode.Yaw;
                            }
                        }
                    }
                }
            }
            else
            {
                orbitMode = OrbitMode.None;
            }
            return AffectingOrbit && (orbitMode != OrbitMode.None);
        }

        /// <summary>
        /// Compute the amount to disadvantage pitch and yaw based on the current
        /// mouse position. Lower X disadvantages yaw, lower Y disadvantages pitch.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private Vector2 DeadZone(Point pos)
        {
            Vector2 ndc = PixelToNDC(pos);
            ndc -= cursorOffset;

            Vector2 dead = Vector2.Zero;
            dead.Y = Smooth(Math.Abs(ndc.Y), 0.0f, 0.25f);
            dead.X = 1.0f - (1.0f - dead.Y) * (1.0f - Smooth(Math.Abs(ndc.X), 0.0f, 0.25f));

            return dead;
        }

        /// <summary>
        /// Pitch the camera to match mouse dragging.
        /// </summary>
        /// <param name="camera"></param>
        private void Pitch(SmoothCamera camera)
        {
            Point pos = MouseInput.Position;
            Point prev = MouseInput.PrevPosition;
            float dp = (pos.Y - prev.Y) / resolution.Y;

            if (dp != 0)
            {
                float scale = PixelToNDC(pos).Y - cursorOffset.Y;

                scale *= scale;
                scale = MathHelper.Clamp(1.0f - scale, 0.0f, 1.0f);

                float maxScale = 1.0f;
                float minScale = 0.5f;
                scale *= (maxScale - minScale);
                scale += minScale;

                float speed = 6.0f;

                dp *= scale * speed;
                camera.DesiredPitch -= dp;
            }
        }

        /// <summary>
        /// Rotate the camera about Z to match mouse dragging to spin the world
        /// about the cursor.
        /// </summary>
        /// <param name="camera"></param>
        private void Yaw(SmoothCamera camera)
        {
            Point move = new Point(
                MouseInput.Position.X - MouseInput.PrevPosition.X,
                MouseInput.Position.Y - MouseInput.PrevPosition.Y);
            if (move != Point.Zero)
            {
                Vector3 cursorPos = owner.Cursor3D.Position;

                Vector3 prevPos = FindAtHeight(camera, MouseInput.PrevPosition, cursorPos.Z);

                Vector3 pos = FindAtHeight(camera, MouseInput.Position, prevPos.Z);


                Vector2 toPrev = new Vector2(prevPos.X - cursorPos.X, prevPos.Y - cursorPos.Y);
                Vector2 toPos = new Vector2(pos.X - cursorPos.X, pos.Y - cursorPos.Y);

                if ((toPrev != Vector2.Zero) && (toPos != Vector2.Zero))
                {
                    toPrev.Normalize();
                    toPos.Normalize();

                    float sinTheta = toPos.X * toPrev.Y - toPos.Y * toPrev.X;

                    float rads = (float)Math.Asin(sinTheta);

                    camera.DesiredRotation += rads;
                }
            }
        }

        /// <summary>
        /// Adjust yaw to mouse movement horizontally across the screen.
        /// </summary>
        /// <param name="camera"></param>
        private void YawLeftRight(SmoothCamera camera)
        {
            Point pos = MouseInput.Position;
            Point prev = MouseInput.PrevPosition;
            float dy = (pos.X - prev.X) / resolution.X;

            float scale = PixelToNDC(pos).X - cursorOffset.Y;

            scale *= scale;
            scale = MathHelper.Clamp(1.0f - scale, 0.0f, 1.0f);

            float maxScale = 1.0f;
            float minScale = 0.5f;
            scale *= (maxScale - minScale);
            scale += minScale;

            float speed = 6.0f;

            dy *= scale * speed;
            camera.DesiredRotation -= dy;
        }

        /// <summary>
        /// Adjust pitch and yaw to mouse input.
        /// </summary>
        /// <param name="camera"></param>
        private void PitchAndYaw(SmoothCamera camera)
        {
            if (MouseInput.Right.IsPressed)
            {
                Pitch(camera);
                YawLeftRight(camera);
            }
        }

        /// <summary>
        /// Move the dragged object from the new mouse position.
        /// </summary>
        /// <param name="camera"></param>
        private void DragObject(Camera camera)
        {
            if (dragObject != null && (MouseInput.PrevPosition != MouseInput.Position))
            {
                if (ContinueDrag)
                {
                    float height = dragObject.Movement.Position.Z;
                    Vector3 prevPos = FindAtHeight(camera, MouseInput.PrevPosition, height);
                    Vector3 pos = FindAtHeight(camera, MouseInput.Position, height);
                    Vector2 delPos = new Vector2(pos.X - prevPos.X, pos.Y - prevPos.Y);
                    float kMaxDragDistSq = 2500.0f;
                    if (delPos.LengthSquared() < kMaxDragDistSq)
                    {
                        Vector3 oldPos = dragObject.Movement.Position;
                        Vector2 newPos = new Vector2(oldPos.X + delPos.X, oldPos.Y + delPos.Y);

                        owner.DragSelectedObject(dragObject, newPos, false);
                    }
                }
                if (ContinueRaise)
                {
                    /// This could be easily made to exactly follow the mouse, just find
                    /// the height at which the object's 2d position intersects the mouse's
                    /// Y position on the screen. This one is easy because moving up and down
                    /// on a line doesn't involve any ambiguity in following a ray out into the
                    /// world. Or, put simply, it really is a 2D problem.
                    /// Still, not much point, because the harder bits would still be sloppy,
                    /// and it seems close enough.
                    float dheight = MouseInput.Position.Y - MouseInput.PrevPosition.Y;
                    float kRaiseRate = -0.002f;
                    dheight *= kRaiseRate;
                    float dist = Vector3.Distance(dragObject.Movement.Position, camera.ActualFrom);
                    dheight *= dist;

                    dragObject.HeightOffset += dheight;

                    Vector3 pos = dragObject.Movement.Position;
                    pos.Z = dragObject.GetPreferredAltitude();
                    dragObject.Movement.Position = pos;

                    camera.ChangeHeightOffset(dragObject.Movement.Altitude);
                    InGame.IsLevelDirty = true;
                }
                if (ContinueRotate)
                {
                    float drads = MouseInput.Position.X - MouseInput.PrevPosition.X;
                    float kRotRate = 0.01f;
                    drads *= kRotRate;

                    dragObject.Movement.RotationZ += drads;
                    InGame.IsLevelDirty = true;
                }
            }
        }

        /// <summary>
        /// Move the world according to mouse dragging.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private Vector2 Drag(Camera camera, Vector2 position)
        {
            if (MouseInput.Left.IsPressed)
            {
                Point move = new Point(
                    MouseInput.Position.X - MouseInput.PrevPosition.X,
                    MouseInput.Position.Y - MouseInput.PrevPosition.Y);

                if (move != Point.Zero)
                {
                    Vector3 prevPos = FindHit(camera, MouseInput.PrevPosition);

                    Vector3 pos = FindAtHeight(camera, MouseInput.Position, prevPos.Z);

                    Vector2 del = new Vector2(pos.X - prevPos.X, pos.Y - prevPos.Y);

                    //Debug.Print("prevPos={0}, {1}, {2}", prevPos.X, prevPos.Y, prevPos.Z);
                    //Debug.Print("pos={0}, {1}, {2}", pos.X, pos.Y, pos.Z);

                    position -= del;

                }
            }
            return position;
        }

        /// <summary>
        /// Find where a ray through input mouse position (pixel coords) hits the terrain.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="mouse"></param>
        /// <returns></returns>
        public static Vector3 FindHit(Camera camera, Point mouse)
        {
            Vector3 ray = FindRay(camera, new Vector2(mouse.X, mouse.Y));
            Vector3 src = camera.ActualFrom;

            Vector3 dst = src + ray * kMaxRayCast;

            Vector3 hitPoint = dst;
            if (!Terrain.LOSCheckTerrainAndPath(src, dst, ref hitPoint))
            {
                if (ray.Z < 0)
                {
                    float t = -src.Z / ray.Z;
                    ray *= t;
                }
                ray = LimitRay(ray);
                hitPoint = src + ray;
            }

            return hitPoint;
        }

        /// <summary>
        /// Cut off the ray at a maximal distance in the horizontal plane.
        /// </summary>
        /// <param name="ray"></param>
        /// <returns></returns>
        private static Vector3 LimitRay(Vector3 ray)
        {
            Vector2 ray2d = new Vector2(ray.X, ray.Y);
            float len = ray2d.Length();
            if (len > kMaxRayCast)
            {
                ray2d *= kMaxRayCast / len;

                ray.X = ray2d.X;
                ray.Y = ray2d.Y;
            }
            return ray;
        }

        /// <summary>
        /// Find where a ray through input mouse position (pixel coords) 
        /// passes through the horizontal plane at height h.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="mouse"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public static Vector3 FindAtHeight(Camera camera, Point mouse, float h)
        {
            Vector3 ray = FindRay(camera, new Vector2(mouse.X, mouse.Y));

            Vector3 eye = camera.ActualFrom;

            float t = (h - eye.Z) / ray.Z;

            t = Math.Max(t, 0);
            ray *= t;
            ray = LimitRay(ray);

            Vector3 pos = eye + ray;
            pos.Z = h;

            return pos;
        }

        /// <summary>
        /// Transform a mouse position (pixel coords) into a normalized
        /// world space ray.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="mouse"></param>
        /// <returns></returns>
        private static Vector3 FindRay(Camera camera, Vector2 mouse)
        {
            Vector3 ray = camera.ScreenToWorldCoords(mouse);

            return ray;
        }

        /// <summary>
        /// Test whether the user is currently using the mouse to control the camera.
        /// Will return false if the user is using the mouse for something else, like editing.
        /// </summary>
        private bool AffectingOrbit
        {
            get
            {
                return
                    MouseInput.Right.IsPressed

                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.Space)
                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.LeftControl)
                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.RightControl)
                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.LeftAlt)
                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.RightAlt)
                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.LeftShift)
                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.RightShift)
                    
                    && !disableRightOrbit;
            }
        }

        /// <summary>
        /// Test whether the user is currently using the mouse to drag the world.
        /// Will return false if the user is using the mouse for something else, like editing.
        /// </summary>
        private bool AffectingDrag
        {
            get
            {
                return
                    MouseInput.Left.IsPressed 

                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.Space)
                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.LeftControl)
                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.RightControl)
                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.LeftAlt)
                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.RightAlt)
                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.LeftShift)
                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.RightShift)
                    
                    && !disableLeftDrag;
            }
        }

        #region Helpers

        /// <summary>
        /// Simple hermite smoothing function.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private float Smooth(float x)
        {
            x = MathHelper.Clamp(x, 0.0f, 1.0f);
            return 3.0f * x * x - 2.0f * x * x * x;
        }
        /// <summary>
        /// Hermite interp with lo mapping to 0, hi mapping to 1.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="lo"></param>
        /// <param name="hi"></param>
        /// <returns></returns>
        private float Smooth(float x, float lo, float hi)
        {
            x -= lo;
            x /= hi - lo;
            return Smooth(x);
        }

        /// <summary>
        /// Cache away anything useful about the camera.
        /// </summary>
        /// <param name="camera"></param>
        private void CacheTransforms(SmoothCamera camera)
        {
            resolution = new Vector2(camera.Resolution.X, camera.Resolution.Y);

            pixelToNDC.X = 2.0f / resolution.X;
            pixelToNDC.Y = 2.0f / resolution.Y;
            pixelToNDC.Z = -1.0f;
            pixelToNDC.W = -1.0f;

            ndcToPixel.X = resolution.X / 2.0f;
            ndcToPixel.Y = resolution.Y / 2.0f;
            ndcToPixel.Z = resolution.X / 2.0f;
            ndcToPixel.W = resolution.Y / 2.0f;

            Vector4 cursorNDC = Vector4.Transform(
                owner.Cursor3D.Position, camera.ViewProjectionMatrix);

            cursorOffset = new Vector2(
                cursorNDC.X / cursorNDC.W,
                cursorNDC.Y / cursorNDC.W);
        }
        /// <summary>
        /// Transform pixel coordinates to NDC.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private Vector2 PixelToNDC(Point p)
        {
            return new Vector2(p.X * pixelToNDC.X + pixelToNDC.Z, -p.Y * pixelToNDC.Y - pixelToNDC.W);
        }
        /// <summary>
        /// Transform NDC coordinates to pixel coords.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private Vector2 NDCToPixel(Vector2 p)
        {
            return new Vector2(p.X * ndcToPixel.X + ndcToPixel.Z, -p.Y * ndcToPixel.Y + ndcToPixel.W);
        }

        #endregion Helpers

        #endregion Internal
    };
};