
using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using KoiX;
using KoiX.Input;

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
    /// and picking objects.  Note that this is also used to support the
    /// MouseSensor in RunSim mode.
    /// </summary>
    public class MouseEdit
    {
        #region Members
     
        InGame owner = null;

        enum OrbitMode
        {
            Pitch,
            Yaw,
            None
        };

        // Why do we care about Normalized Device Coordinates?
        Vector4 pixelToNDC;
        Vector4 ndcToPixel;
        Vector2 cursorOffset;
        Vector2 resolution;

        GameActor quasiSelected = null;
        Distortion quasiHighlight = null;

        float clickTime = 0.0f;
        float moveTime = 0.0f;
        Vector2 moveFrom = Vector2.Zero;
        Vector2 moveTo = Vector2.Zero;
        float kDoubleClickTimeOut = 0.25f;

        GameActor dragObject = null;
        Distortion dragHighlight = null;
        bool dragSelected = false;

        static bool disableLeftDrag = false;
        static bool disableRightOrbit = false;

        static float kMoveTotalTime = 0.15f;
        static float kMaxRayCast = 500.0f;

        static HitInfo mouseTouchHitInfo = new HitInfo();
        #endregion Members

        #region Accessors

        /// <summary>
        /// This frame's cached info on what the mouse is over. Pretty much read only,
        /// maintained internally.
        /// </summary>
        public static HitInfo MouseTouchHitInfo
        {
            get { return mouseTouchHitInfo; }
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
                return LowLevelMouseInput.Left.IsPressed && KeyboardInputX.CtrlIsPressed
                    ? (KeyboardInputX.ShiftIsPressed ? 1.0f : 0.25f)
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
                return LowLevelMouseInput.Left.IsPressed && KeyboardInputX.IsPressed(Keys.Space)
                    ? (KeyboardInputX.ShiftIsPressed ? 1.0f : 0.25f)
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
                return LowLevelMouseInput.Left.IsPressed && KeyboardInputX.AltIsPressed
                    ? (KeyboardInputX.ShiftIsPressed ? 0.5f : 0.25f)
                    : 0.0f;
            }
        }

        /// <summary>
        /// True if left action is indicated (analogous to gamepad left trigger).
        /// </summary>
        public bool LeftAction
        {
            get { return LowLevelMouseInput.Left.WasPressed && KeyboardInputX.CtrlIsPressed; }
        }

        /// <summary>
        /// True if middle action is indicated, analogous to gamepad A button.
        /// </summary>
        public bool MiddleAction
        {
            get { return LowLevelMouseInput.Left.WasPressed && KeyboardInputX.AltIsPressed; }
        }

        /// <summary>
        /// True if right action is indicated, analogous to gamepad right trigger.
        /// </summary>
        public bool RightAction
        {
            get { return LowLevelMouseInput.Left.WasPressed && KeyboardInputX.IsPressed(Keys.Space); }
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
            get { return LowLevelMouseInput.Left.WasPressed && !KeyboardInputX.ShiftIsPressed; }
        }

        /// <summary>
        /// True if an object raise/lower was initiated this frame. Only checks input,
        /// not whether action actually started.
        /// </summary>
        public bool StartRaise
        {
            get { return LowLevelMouseInput.Left.WasPressed && KeyboardInputX.ShiftIsPressed; }
        }

        /// <summary>
        /// True if an object rotation was initiated this frame. Only checks input,
        /// not whether action actually started.
        /// </summary>
        public bool StartRotate
        {
            get { return LowLevelMouseInput.Right.WasPressed && KeyboardInputX.ShiftIsPressed; }
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
            get { return LowLevelMouseInput.Left.IsPressed && !KeyboardInputX.ShiftIsPressed; }
        }

        /// <summary>
        /// True if input for object raise/lower is active.
        /// </summary>
        public bool ContinueRaise
        {
            get { return LowLevelMouseInput.Left.IsPressed && KeyboardInputX.ShiftIsPressed; }
        }

        /// <summary>
        /// True if input for object rotation is active.
        /// </summary>
        public bool ContinueRotate
        {
            get { return LowLevelMouseInput.Right.IsPressed && KeyboardInputX.ShiftIsPressed; }
        }

        #region Internal
        /// <summary>
        /// Are we currently dragging an object?
        /// </summary>
        bool DraggingObject
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
            if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
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

            EndDrag();
        }

        /// <summary>
        /// Handle any camera orbit from mouse control.
        /// </summary>
        /// <param name="camera"></param>
        public void DoCamera(SmoothCamera camera)
        {
            CacheTransforms(camera);
            disableRightOrbit = false;
        }

        /// <summary>
        /// Zoom in or out.
        /// </summary>
        /// <param name="camera"></param>
        public void DoZoom(SmoothCamera camera)
        {
            int scrollChange = LowLevelMouseInput.DeltaScrollWheel;

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
            if (KeyboardInputX.WasPressed(Keys.F4))
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
                if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
                {
                    // Calc where the mouse is aiming and move the cursor towards there.  
                    // The camera will follow.
                    Vector3 mouseAimPosition = FindHit(camera, LowLevelMouseInput.Position);
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
        /// Handle object edit controls via mouse.
        /// </summary>
        public void SetFocusActorGlow(Camera camera)
        {
            ChangeQuasi(mouseTouchHitInfo.ActorHit);
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
        /// Note this is only being called here, via Update, for
        /// RunSim mode.  We can probably re-think that also.  It may
        /// make more sense to not do this every frame and only call
        /// it when an input tile needs the info.
        /// 
        /// Actually this is also being called by InGame.UpdateObjects()
        /// which is called both during edit and runsim modes.
        /// </summary>
        /// <param name="camera"></param>
        public static void Update(Camera camera)
        {
            UpdateHitInfo(camera, LowLevelMouseInput.PositionVec);
        }   // end of Update()

        public static void UpdateHitInfo(Camera camera, Vector2 position)
        {
            Vector3 src = camera.ActualFrom;
            //Vector2 position = LowLevelMouseInput.PositionVec;
            Vector3 ray = FindRay(camera, position);
            Vector3 dst = src + ray * 100;

            // If we're dragging a bot, then we must be hitting it, right?
            if (InGame.inGame.mouseEditUpdateObj.ToolBox.EditObjectsToolInstance.DraggingObject)
            {
                // Nothing to do here.  If we're dragging an object with the mouse then by definition
                // it is under the mouse pointer and needs to stay there.  So, don't clear mouseMouseTouchHitInfo
                // or even bother testing the other objects.
            }
            else
            {
                mouseTouchHitInfo.Clear();

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
                                        mouseTouchHitInfo.ActorHit = ga;
                                        mouseTouchHitInfo.ActorPosition = collInfo.Contact + offset;

                                        // Update dst so we don't bother testing actors further
                                        // away than this.
                                        dst = src + ray * distToNearestHit;
                                    }
                                }
                            }   // end of loop over each collision prim.
                        }   // end if there are any collision prims.
                    }   // end loop over gamethings.
                }

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
                                    mouseTouchHitInfo.VerticalOffset = 0.0f;

                                    if (mouseTouchHitInfo.ActorHit == null)
                                    {
                                        // No previous hit, so just fill in the result.
                                        mouseTouchHitInfo.ActorHit = ga;
                                        mouseTouchHitInfo.ActorPosition = src + ray * distToCreatable;
                                    }
                                    else
                                    {
                                        // Compare dist with existing hit.
                                        float distToExisting = (mouseTouchHitInfo.ActorPosition - src).Length();
                                        if (distToCreatable < distToExisting)
                                        {
                                            // New one is closer, so replace previous.
                                            mouseTouchHitInfo.ActorHit = ga;
                                            mouseTouchHitInfo.ActorPosition = src + ray * distToCreatable;
                                        }
                                    }
                                }
                            }

                            // If in key/mouse mode also allow picking bots by selecting their anchor points.
                            // This is the point directly under the bot at terrain level (or atltitude == 0).
                            if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
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
                                    mouseTouchHitInfo.VerticalOffset = verticalOffset;

                                    if (mouseTouchHitInfo.ActorHit == null)
                                    {
                                        // No previous hit, so just fill in the result.
                                        mouseTouchHitInfo.ActorHit = ga;
                                        mouseTouchHitInfo.ActorPosition = src + ray * distToCreatable;
                                    }
                                    else
                                    {
                                        // Compare dist with existing hit.
                                        float distToExisting = (mouseTouchHitInfo.ActorPosition - src).Length();
                                        if (distToCreatable < distToExisting)
                                        {
                                            // New one is closer, so replace previous.
                                            mouseTouchHitInfo.ActorHit = ga;
                                            mouseTouchHitInfo.ActorPosition = src + ray * distToCreatable;
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
                mouseTouchHitInfo.TerrainHit = hitPoint.Z > Terrain.Current.MinHeight * 0.5f;
                mouseTouchHitInfo.TerrainPosition = hitPoint;
                mouseTouchHitInfo.TerrainMaterial = Terrain.GetMaterialType(new Vector2(hitPoint.X, hitPoint.Y));

                // If we don't have a valid material where we hit, then don't count it as a hit.
                // The underlying problem here seems to be that when a material is erased that
                // the matching heightmap entry is not set back to 0.
                if (mouseTouchHitInfo.TerrainMaterial == TerrainMaterial.EmptyMatIdx)
                {
                    mouseTouchHitInfo.TerrainHit = false;
                }

                // If we have an actor detected under the cursor, see if it was
                // occluded by terrain.
                if (mouseTouchHitInfo.HaveActor)
                {
                    if (Vector3.DistanceSquared(src, mouseTouchHitInfo.TerrainPosition)
                        < Vector3.DistanceSquared(src, mouseTouchHitInfo.ActorPosition))
                    {
                        mouseTouchHitInfo.ActorHit = null;
                    }
                }
            }
            else
            {
                //Debug.Assert(src.Z > 0, "Assuming camera is always above zero plane.");
                if (ray.Z < 0)
                {
                    mouseTouchHitInfo.TerrainPosition = FindAtHeight(camera, LowLevelMouseInput.Position, 0);
                    if (Vector3.DistanceSquared(mouseTouchHitInfo.TerrainPosition, src)
                        <= kMaxRayCast * kMaxRayCast)
                    {
                        mouseTouchHitInfo.ZeroPlaneHit = true;
                    }
                }
            }

            // If we think we are over an actor, we also need to check paths.  If
            // there's a path under the cursor that's closer to us than the actor,
            // remvoe the actor from the HitInfo.
            if (mouseTouchHitInfo.HaveActor && (InGame.WayPointEdit.MouseOverDistance < float.MaxValue))
            {
                float wayDist = InGame.WayPointEdit.MouseOverDistance;
                float actorDistSq = Vector3.DistanceSquared(src, mouseTouchHitInfo.ActorPosition);

                if (actorDistSq > wayDist * wayDist)
                {
                    mouseTouchHitInfo.ActorHit = null;
                }
            }

        }   // end of UpdateHitInfo()

        /// <summary>
        /// Test whether the mouse is over a sphere at worldPos.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="worldPos"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static bool MouseOver(Camera camera, Vector3 worldPos, float radius)
        {
            Vector3 ray = FindRay(camera, new Vector2(LowLevelMouseInput.Position.X, LowLevelMouseInput.Position.Y));

            Vector3 world = worldPos - camera.ActualFrom;
            Vector3 proj = world - Vector3.Dot(world, ray) * ray;

            return proj.LengthSquared() <= radius * radius;
        }

        /// <summary>
        /// Handle terrain edit controls via mouse.
        /// </summary>
        public Vector2 DoTerrain(Camera camera)
        {
            Vector3 hit = FindHit(camera, LowLevelMouseInput.Position);

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
            return !MouseTouchHitInfo.HaveActor
                && MouseTouchHitInfo.TerrainHit
                && LowLevelMouseInput.Left.WasPressed;
        }
        #endregion Public

        #region Internal

        /// <summary>
        /// Check whether user has requested any clone/delete/object add type actions,
        /// and perform them if requested.
        /// </summary>
        /// <param name="camera"></param>
        void CheckCloneDelete(Camera camera)
        {
            if (quasiSelected != null)
            {
                if (RightAction)
                {
                    LowLevelMouseInput.Left.IgnoreUntilReleased = true;
                    //owner.ActivateNewItemSelector(quasiSelected.Movement.Position, true);
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
                if (mouseTouchHitInfo.TerrainHit || mouseTouchHitInfo.ZeroPlaneHit)
                {
                    float terrDistSq = Vector3.DistanceSquared(camera.ActualFrom, mouseTouchHitInfo.TerrainPosition);
                    float wayDist = InGame.WayPointEdit.MouseOverDistance;
                    if (terrDistSq < wayDist * wayDist)
                    {
                        if (RightAction)
                        {
                            LowLevelMouseInput.Left.IgnoreUntilReleased = true;
                            //owner.ActivateNewItemSelector(mouseTouchHitInfo.TerrainPosition, true);
                        }
                        else if (MiddleAction)
                        {
                            Vector3 pos = FindHit(camera, LowLevelMouseInput.Position);
                            owner.PasteAction(null, pos);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Internal scratch list for LOS hits. Don't use this except within single function.
        /// </summary>
        static List<MouseTouchHitInfo> _scratchHits = new List<MouseTouchHitInfo>();

        /// <summary>
        /// See if we've clicked something that should become selected.
        /// </summary>
        /// <param name="camera"></param>
        void CheckReCenter(Camera camera)
        {
            GameActor focus = MouseTouchHitInfo.ActorHit;
            if (focus == null)
            {
                if (LowLevelMouseInput.Left.WasPressed)
                {
                    if (clickTime > 0)
                    {
                        /// We've double clicked on terrain. Let's recenter there.
                        if (MouseTouchHitInfo.TerrainHit || MouseTouchHitInfo.ZeroPlaneHit)
                        {
                            ReCenter(MouseTouchHitInfo.TerrainPosition);
                        }
                    }
                    clickTime = kDoubleClickTimeOut;
                }
            }
            else
            {
                /// We're moused over something, check for click actions.
                if (LowLevelMouseInput.Left.WasPressed)
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
        void ReCenter(Vector2 pos)
        {
            moveTo = pos;
            moveFrom = owner.Cursor3D.Position2d;
            moveTime = kMoveTotalTime;
        }

        /// <summary>
        /// ReCenter the world to the new position.
        /// </summary>
        /// <param name="pos"></param>
        void ReCenter(Vector3 pos)
        {
            ReCenter(new Vector2(pos.X, pos.Y));
        }

        /// <summary>
        /// Is the mouse currently moving?
        /// </summary>
        /// <returns></returns>
        bool MouseMoving()
        {
            return LowLevelMouseInput.DeltaPosition != Point.Zero;
        }

        /// <summary>
        /// Drag or release object based on mouse left click.
        /// </summary>
        void CheckDragObject()
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
        void EndDrag()
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
        Vector2 CheckMoveTo(Vector2 move)
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
        void CheckQuasi(bool forceKill)
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
        void MakeQuasiHighlight()
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
        void ChangeQuasi(GameActor actor)
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
        void EndQuasi()
        {
            if (quasiHighlight != null)
            {
                quasiHighlight.Die();
                quasiHighlight = null;
            }
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
        static Vector3 LimitRay(Vector3 ray)
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
        static Vector3 FindRay(Camera camera, Vector2 mouse)
        {
            Vector3 ray = camera.ScreenToWorldCoords(mouse);

            return ray;
        }

        /// <summary>
        /// Test whether the user is currently using the mouse to control the camera.
        /// Will return false if the user is using the mouse for something else, like editing.
        /// </summary>
        bool AffectingOrbit
        {
            get
            {
                return
                    LowLevelMouseInput.Right.IsPressed

                    && !KeyboardInputX.IsPressed(Microsoft.Xna.Framework.Input.Keys.Space)
                    && !KeyboardInputX.IsPressed(Microsoft.Xna.Framework.Input.Keys.LeftControl)
                    && !KeyboardInputX.IsPressed(Microsoft.Xna.Framework.Input.Keys.RightControl)
                    && !KeyboardInputX.IsPressed(Microsoft.Xna.Framework.Input.Keys.LeftAlt)
                    && !KeyboardInputX.IsPressed(Microsoft.Xna.Framework.Input.Keys.RightAlt)
                    && !KeyboardInputX.IsPressed(Microsoft.Xna.Framework.Input.Keys.LeftShift)
                    && !KeyboardInputX.IsPressed(Microsoft.Xna.Framework.Input.Keys.RightShift)
                    
                    && !disableRightOrbit;
            }
        }

        /// <summary>
        /// Test whether the user is currently using the mouse to drag the world.
        /// Will return false if the user is using the mouse for something else, like editing.
        /// </summary>
        bool AffectingDrag
        {
            get
            {
                return
                    LowLevelMouseInput.Left.IsPressed 

                    && !KeyboardInputX.IsPressed(Microsoft.Xna.Framework.Input.Keys.Space)
                    && !KeyboardInputX.IsPressed(Microsoft.Xna.Framework.Input.Keys.LeftControl)
                    && !KeyboardInputX.IsPressed(Microsoft.Xna.Framework.Input.Keys.RightControl)
                    && !KeyboardInputX.IsPressed(Microsoft.Xna.Framework.Input.Keys.LeftAlt)
                    && !KeyboardInputX.IsPressed(Microsoft.Xna.Framework.Input.Keys.RightAlt)
                    && !KeyboardInputX.IsPressed(Microsoft.Xna.Framework.Input.Keys.LeftShift)
                    && !KeyboardInputX.IsPressed(Microsoft.Xna.Framework.Input.Keys.RightShift)
                    
                    && !disableLeftDrag;
            }
        }

        #region Helpers

        /// <summary>
        /// Simple hermite smoothing function.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        float Smooth(float x)
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
        float Smooth(float x, float lo, float hi)
        {
            x -= lo;
            x /= hi - lo;
            return Smooth(x);
        }

        /// <summary>
        /// Cache away anything useful about the camera.
        /// </summary>
        /// <param name="camera"></param>
        void CacheTransforms(SmoothCamera camera)
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
        Vector2 PixelToNDC(Point p)
        {
            return new Vector2(p.X * pixelToNDC.X + pixelToNDC.Z, -p.Y * pixelToNDC.Y - pixelToNDC.W);
        }
        /// <summary>
        /// Transform NDC coordinates to pixel coords.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        Vector2 NDCToPixel(Vector2 p)
        {
            return new Vector2(p.X * ndcToPixel.X + ndcToPixel.Z, -p.Y * ndcToPixel.Y + ndcToPixel.W);
        }

        #endregion Helpers

        #endregion Internal

    }   // end of class MouseEdit

}   // end of namespace Boku
