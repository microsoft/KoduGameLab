
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
using Boku.Common.Gesture;
using Boku.Programming;

namespace Boku
{
    /// <summary>
    /// This section of InGame deals with the mouse control of the camera
    /// and picking objects.
    /// </summary>
    public class TouchEdit
    {
        #region Child Classes
        /// <summary>
        /// Overblown struct for holding the global information on what objects the touch
        /// is over.
        /// </summary>
        public class TouchHitInfo
        {
            #region Members
            /// <summary>
            /// Actor under touch.
            /// </summary>
            private GameActor actorHit = null;
            /// <summary>
            /// LOS ray hit on actorHit's collision bounds.
            /// </summary>
            private Vector3 actorPosition = Vector3.Zero;

            private Vector3 lastTouchEditPos = Vector3.Zero;

            /// <summary>
            /// Position of hit with terrain.
            /// </summary>
            private Vector3 terrainPosition = Vector3.Zero;
            /// <summary>
            /// Whether terrain is under touch.
            /// </summary>
            private bool terrainHit = false;
            /// <summary>
            /// True if no terrain under touch, but ray hits zero plane.
            /// </summary>
            private bool zeroPlaneHit = false;

            /// <summary>
            /// The material on the terrain where the touch LOS hits. Undefined if !terrainHit.
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
            /// Is there an unoccluded actor under the touch?
            /// </summary>
            public bool HaveActor
            {
                get { return actorHit != null; }
            }

            /// <summary>
            /// Return any unoccluded actor under the touch. Must be
            /// closer than any other other actor under touch, and closer than terrain.
            /// </summary>
            public GameActor ActorHit
            {
                get { return actorHit; }
                internal set { actorHit = value; }
            }

            /// <summary>
            /// Where the touch LOS ray hits the ActorHit's collision hull. For
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

            public Vector3 LastTouchEditPos
            {
                get { return lastTouchEditPos; }
                set { lastTouchEditPos = value; }

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

        Vector4 pixelToNDC;
        Vector4 ndcToPixel;
        Vector2 cursorOffset;
        Vector2 resolution;

        private GameActor quasiSelected = null;
        private bool quasiSelectionActive = false; //whether or not the user is actively touching the selection actor (but not dragging)
        private Distortion quasiHighlight = null;


        private float moveTime = 0.0f;
        private Vector2 moveFrom = Vector2.Zero;
        private Vector2 moveTo = Vector2.Zero;

        private GameActor dragObject = null;
        private Distortion dragHighlight = null;

        private static float kMoveTotalTime = 0.15f;
        private static float kMaxRayCast = 500.0f;

        //how much to scale up collision radius for ray checks when in touch mode
        private static float kTouchCollisionScale = 1.75f;

        private static TouchHitInfo touchHitInfo = new TouchHitInfo();
        #endregion Members

        #region Accessors

        /// <summary>
        /// This frame's cached info on what the mouse is over. Pretty much read only,
        /// maintained internally.
        /// </summary>
        public static TouchHitInfo HitInfo
        {
            get { return touchHitInfo; }
        }

        public GameActor HighLit
        {
            get { return quasiSelected; }
        }

        /// <summary>
        /// True if an object rotation was initiated this frame. Only checks input,
        /// not whether action actually started.
        /// </summary>
        public bool StartRotate
        {
            get { return TouchGestureManager.Get().RotateGesture.WasActivated; }
        }

        /// <summary>
        /// True if input for object rotation is active.
        /// </summary>
        public bool ContinueRotate
        {
            get { return TouchGestureManager.Get().RotateGesture.IsRotating; }
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
        public TouchEdit(InGame inGame)
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
            if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
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
            moveTime = 0;

            dragObject = null;
            if (dragHighlight != null)
            {
                dragHighlight.Die();
                dragHighlight = null;
            }
        }

        /// <summary>
        /// Handle any camera orbit from touch control.
        /// </summary>
        /// <param name="camera"></param>
        public void DoCamera(SmoothCamera camera)
        {
            CacheTransforms(camera);
        }

        /// <summary>
        /// Zoom in or out.
        /// </summary>
        /// <param name="camera"></param>
        public void DoZoom(SmoothCamera camera)
        {
            // Don't zoom if the AddItem pie menu or pickers are active.
            if (InGame.inGame.editObjectUpdateObj.newItemSelectorShim.State == Boku.UI.UIShim.States.Active
                || InGame.inGame.touchEditUpdateObj.PickersActive)
            {
                return;
            }

            PinchGestureRecognizer pinchRecognizer = TouchGestureManager.Get().GetActiveGesture(TouchGestureType.Pinch, TouchGestureType.DoubleDrag ) as PinchGestureRecognizer;
            if ( null != pinchRecognizer && pinchRecognizer.IsPinching )
            {
                for(int i=0; i<TouchInput.TouchCount; i++)
                {
                    TouchContact touch = TouchInput.GetTouchContactByIndex(i);
                    if (InGame.inGame.touchEditUpdateObj.ToolBar.IsOverUIButton(touch, true))
                    {
                        Debug.WriteLine("Failing Pinch!");
                        return;
                    }
                }

                //Get world position of touch pinch average.
                Vector3 worldPos = FindHit(camera, TouchInput.GetAsPoint(pinchRecognizer.AveragePosition));

                //Pinch scale is 1 when normal.  Fixing scale value so that when scale is half (0.5) we adjust it to 2 for twice as small.
                float pinchScale = pinchRecognizer.Scale;
                Debug.Assert( pinchScale > 0.0f );
                pinchScale = (pinchScale < 1.0f) ? (1/pinchScale) : pinchScale;

                //Calculating speed of translation.
                float zoomSpeed = 25.0f * pinchScale * (float)Time.WallClockFrameSeconds;
               
                //Getting camera specific values for moving the camera (Not really zoom as the frustum doesnt change, we're just bringing the camera closer and further away).
                Vector3 camPos = camera.DesiredAt + camera.DesiredEyeOffset;
                Vector3 nextCamPos = camPos;
                
                Vector3 cameraPosToPinchPos_Norm = Vector3.Normalize( worldPos - camPos );

                //When this is set to true we restrict zoom partially.
                bool bRestrictZoom = false;

                //Using the vector we get from camera position to touch world hit position, we push and pull the camera about that vector.
                //Because the camera uses the At <-> EyeOffset to do positioning we need to also move the At position as well as zooming to properly translate the camera about the vector.
                switch( pinchRecognizer.GetPinchState() )
                {
                    case PinchGestureRecognizer.PinchState.Shrinking:
                        //fingers are moving closer together, zoom OUT
                        nextCamPos += -cameraPosToPinchPos_Norm * zoomSpeed;
                        break;

                    case PinchGestureRecognizer.PinchState.Growing:
                        nextCamPos += cameraPosToPinchPos_Norm * zoomSpeed;

                        bRestrictZoom = InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.RunSim;

                        break;
                }

                //Get the deltaPos vector for projection on camera side and forward unit vectors.
                Vector3 deltaCamPos = nextCamPos - camPos;

                float forwardDelta = Vector3.Dot( deltaCamPos, -camera.ViewDir );
                float sideDelta = Vector3.Dot( deltaCamPos, camera.ViewRight );

                
                //Calculate new desired values.
                Vector3 desiredAtDelta = sideDelta * camera.ViewRight;
                float desiredDistance = camera.DesiredDistance + forwardDelta;

                // If not in RunSim mode, don't allow the camera to get closer than 4 meters.
                if (bRestrictZoom)
                {
                    float initialDesiredDistance = desiredDistance;
                    desiredDistance = Math.Max(4.0f, desiredDistance);
                    
                    //We have altered the distance so we must also alter desiredAt
                    if( desiredDistance != initialDesiredDistance )
                    {
                        float delta_originalDistances = camera.DesiredDistance - initialDesiredDistance;
                        Debug.Assert( delta_originalDistances > 0.0f );

                        float percent = (camera.DesiredDistance - 4.0f) / delta_originalDistances;
                        desiredAtDelta *= percent;
                    }
                }

                camera.DesiredAt += desiredAtDelta;
                camera.DesiredDistance = desiredDistance;
            }
        }

        /// <summary>
        /// Handle any cursor movement (translation) from touch control.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public Vector2 DoCursor(Camera camera, Vector2 position)
        {
            //bool affectingDrag = AffectingDrag && (moveTime <= 0);
            bool affectingDrag = moveTime <= 0;
            owner.Cursor3D.Hidden = affectingDrag;

            //disabling recenter completely in touch mode for now, it's very distracting
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
            return position;
        }

        /// <summary>
        /// Handle object edit controls via touch.
        /// </summary>
        public void DoObject(Camera camera)
        {
            //only change quasi on taps
            if (TouchGestureManager.Get().TapGesture.WasValidEditObjectTap ||
                TouchGestureManager.Get().DoubleTapGesture.WasRecognized ||
                TouchGestureManager.Get().TouchHoldGesture.WasRecognized ||
                (TouchGestureManager.Get().DragGesture.WasActivated && touchHitInfo.HaveActor))
            {
                //only allow editing highlight for non-UI touches or if we're "un-touching" the actor
                if (Boku.InGame.inGame.TouchEdit.HasNonUITouch() || touchHitInfo.ActorHit == null)
                {
                    ChangeQuasi(touchHitInfo.ActorHit, false);        
                }
            }
            else if (TouchInput.TouchCount==1 && TouchInput.InitialActorHit != null)
            {
                //don't have a gesture to finish, but we have a single touch on an actor
                if (TouchGestureManager.Get().DragGesture.IsDragging)
                {
                    ChangeQuasi(TouchInput.InitialActorHit, false);
                }
                else if (TouchGestureManager.Get().TouchHoldGesture.SlightHoldMade)
                {                    
                    ChangeQuasi(TouchInput.InitialActorHit, true);
                }
            }
            CheckQuasi(false);
            CheckDragObject();
        }

        public void KillSelectionHighlight()
        {
            CheckQuasi(true);
        }


        /// <summary>
        /// Cache away info about what object and/or terrain are
        /// currently under the mouse.
        /// </summary>
        /// <param name="camera"></param>
        public static void Update(Camera camera)
        {            
            touchHitInfo.Clear();

            TouchContact touchInput = TouchInput.GetOldestTouch();
            if (touchInput == null)
            {
                return;
            }           

            Vector3 src = camera.ActualFrom;
            Vector3 ray = FindRay(camera, TouchInput.GetAsPoint(touchInput.position));
            Vector3 dst = src + ray * 100;

            // If we're dragging a bot, then we must be hitting it, right?
            if (InGame.inGame.touchEditUpdateObj.ToolBox.EditObjectsToolInstance.DraggingObject)
            {
                // Nothing to do here.  If we're dragging an object with the mouse then by definition
                // it is under the mouse pointer and needs to stay there.  So, don't clear mouseHitInfo
                // or even bother testing the other objects.
            }
            else
            {
                touchHitInfo.Clear();

                // The collision system ignores creatables and ghosts so we have to work with them here.
                // Since we now also want to work with anchors we completely skip the above collision system
                // test and just use this loop for testing.
                for (int i = 0; i < InGame.inGame.gameThingList.Count; i++)
                {
                    GameActor ga = InGame.inGame.gameThingList[i] as GameActor;

                    //Ignore null and first person actor
                    if (ga != null && ga != CameraInfo.FirstPersonActor )
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
                        float collisionRadius = MathHelper.Max(ga.CollisionSphere.Radius, minRadius) * kTouchCollisionScale;

                        if (radius < collisionRadius)
                        {
                            float distToCreatable = (nearestPoint - src).Length();
                            touchHitInfo.VerticalOffset = 0.0f;

                            if (touchHitInfo.ActorHit == null)
                            {
                                // No previous hit, so just fill in the result.
                                touchHitInfo.ActorHit = ga;
                                touchHitInfo.ActorPosition = src + ray * distToCreatable;
                            }
                            else
                            {
                                // Compare dist with existing hit.
                                float distToExisting = (touchHitInfo.ActorPosition - src).Length();
                                if (distToCreatable < distToExisting)
                                {
                                    // New one is closer, so replace previous.
                                    touchHitInfo.ActorHit = ga;
                                    touchHitInfo.ActorPosition = src + ray * distToCreatable;
                                }
                            }
                        }
                    }
                }
            }

            dst = src + ray * kMaxRayCast;
            Vector3 hitPoint = dst;
            if (Terrain.LOSCheckTerrainAndPath(src, dst, ref hitPoint))
            {
                touchHitInfo.TerrainHit = hitPoint.Z > Terrain.Current.MinHeight * 0.5f;
                touchHitInfo.TerrainPosition = hitPoint;
                touchHitInfo.TerrainMaterial = Terrain.GetMaterialType(new Vector2(hitPoint.X, hitPoint.Y));

                /// If we have an actor detected under the cursor, see if it was
                /// occluded by terrain.
                if (touchHitInfo.HaveActor)
                {
                    if (Vector3.DistanceSquared(src, touchHitInfo.TerrainPosition)
                        < Vector3.DistanceSquared(src, touchHitInfo.ActorPosition))
                    {
                        touchHitInfo.ActorHit = null;
                    }
                }
            }
            else
            {
                //Debug.Assert(src.Z > 0, "Assuming camera is always above zero plane.");
                if (ray.Z < 0)
                {
                    touchHitInfo.TerrainPosition = FindAtHeight(camera, TouchInput.GetAsPoint(TouchInput.GetTouchContactByIndex(0).position), 0);
                    if (Vector3.DistanceSquared(touchHitInfo.TerrainPosition, src)
                        <= kMaxRayCast * kMaxRayCast)
                    {
                        touchHitInfo.ZeroPlaneHit = true;
                    }
                }
            }
            if (touchHitInfo.HaveActor && (InGame.WayPointEdit.TouchOverDistance < float.MaxValue))
            {
                float wayDist = InGame.WayPointEdit.TouchOverDistance;
                float actorDistSq = Vector3.DistanceSquared(src, touchHitInfo.ActorPosition);

                if (actorDistSq > wayDist * wayDist)
                {
                    touchHitInfo.ActorHit = null;
                }
            }

            // We have determined the actorHit, so fill in the initial actor hit for TouchInput, if
            // applicable.
            TouchInput.SetInitialActorHit();
        }

        /// <summary>
        /// Test whether the touch location is over a sphere at worldPos.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="worldPos"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static bool TouchOver(Camera camera, Vector3 worldPos, float radius)
        {
            Vector3 ray = FindRay(camera, TouchInput.GetAsPoint(TouchInput.GetOldestTouch().position));

            Vector3 world = worldPos - camera.ActualFrom;
            Vector3 proj = world - Vector3.Dot(world, ray) * ray;

            return proj.LengthSquared() <= radius * radius;
        }

        public bool HasNonUITouch()
        {
            TouchContact editingTouch = FindNonUITouch();
            return editingTouch != null;
        }

        /// <summary>
        /// Handle terrain edit controls via touch.
        /// </summary>
        public Vector2 DoTerrain(Camera camera)
        {
            TouchContact editingTouch = FindNonUITouch();

            Vector3 hit;

            if ( editingTouch!= null )
            {
                hit = FindHit(camera, TouchInput.GetAsPoint(editingTouch.position));

                if (HitInfo.LastTouchEditPos != hit)
                {
                    HitInfo.LastTouchEditPos = hit;
                }
                
                return new Vector2(hit.X, hit.Y);
            }
            
            return new Vector2(HitInfo.LastTouchEditPos.X, HitInfo.LastTouchEditPos.Y);
        }

        private TouchContact FindNonUITouch()
        {
            TouchContact editingTouch = null;
            TouchContact secondaryTouch = null;

            // find the touch contact that is editing the world/terrain
            foreach (TouchContact t in TouchInput.Touches)
            {
                if (InGame.inGame.IsOverUIButton(t, true))
                {
                    secondaryTouch = t;
                }
            }
            if (secondaryTouch == null)
            {
                editingTouch = TouchInput.GetTouchContactByIndex(0);
            }
            else if (TouchInput.TouchCount == 2)
            {
                editingTouch = TouchInput.GetTouchContactByIndex(1);
                if (editingTouch == secondaryTouch)
                {
                    editingTouch = TouchInput.GetTouchContactByIndex(0);
                }
            }

            if (InGame.inGame.IsOverUIButton(editingTouch, true))
            {
                editingTouch = null;  // CANT have 2 fingers on editing buttons at the same time!
            }

            return editingTouch;
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
                && TouchInput.WasTouched;
        }
        #endregion Public

        #region Internal
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
            /// Double tap gesture.
            DoubleTapGestureRecognizer dblTapGesture = TouchGestureManager.Get().DoubleTapGesture;

            if (dblTapGesture.WasRecognized && 
                (HitInfo.TerrainHit || HitInfo.ZeroPlaneHit) &&
                !GUIButtonManager.IsOverUIButton( dblTapGesture.Position ) &&
                (null == InGame.inGame.touchEditUpdateObj || null == InGame.inGame.touchEditUpdateObj.ToolBar || InGame.inGame.touchEditUpdateObj.ToolBar.CurrentMode != InGame.BaseEditUpdateObj.ToolMode.EditObject) )
            {
                ReCenter(HitInfo.TerrainPosition);
            }
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
        /// Is the touch position currently moving?
        /// </summary>
        /// <returns></returns>
        private bool TouchMoving()
        {
            return ( TouchInput.WasMoved );
        }

        /// <summary>
        /// Drag or release object based on mouse left click.
        /// </summary>
        private void CheckDragObject()
        {
            if (TouchGestureManager.Get().DragGesture.IsDragging)
            {
                //do we have something to drag?
                if ((quasiSelected != null) && (dragObject == null) && (moveTime <= 0))
                {
                    dragObject = quasiSelected;

                    dragHighlight = owner.MakeAura(dragObject, 0.35f);
                    dragHighlight.TintAura(1.0f, 0.0f, 1.0f);
                }
            }
            else
            {
                //were we dragging something?  if so, stop dragging, remove highlight
                if (dragObject != null)
                {
                    ReCenter(dragObject.Movement.Position);

                    EndDrag();
                }
            }
        }

        /// <summary>
        /// Terminate the visual drag effect.
        /// </summary>
        private void EndDrag()
        {
            dragObject = null;
            if (dragHighlight != null)
            {
                dragHighlight.Die();
                dragHighlight = null;
            }
            moveTime = 0;
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
            else if (DraggingObject)
            {
                EndQuasi();
            }
            else if (quasiSelectionActive &&
                        (TouchInput.InitialActorHit == null || TouchGestureManager.Get().TouchHoldGesture.IsValidated == false))
            {
                //user is no longer active on the select object, change the highlight back
                ChangeQuasi(quasiSelected, false);
            }
            if (!forceKill && !DraggingObject && (quasiHighlight == null))
            {
                MakeQuasiHighlight();
            }
        }

        /// <summary>
        /// Generate the touch over visual highlight.
        /// </summary>
        private void MakeQuasiHighlight()
        {
            if ((quasiSelected != null))
            {
                quasiHighlight = owner.MakeAura(quasiSelected, 0.35f);
                if (quasiSelectionActive)
                {
                    //orange
                    quasiHighlight.TintAura(1.0f, 0.33f, 0.0f);
                }
                else
                {
                    //yellow
                    quasiHighlight.TintAura(1.0f, 1.0f, 0.0f);
                }
                quasiSelected.ReactToCursor();
            }
        }



        /// <summary>
        /// Create a yellow highlight for the mouse-over'd object.
        /// </summary>
        /// <param name="actor"></param>
        private void ChangeQuasi(GameActor actor, bool activeTouch)
        {
            if (!DraggingObject)
            {
                if (quasiSelected != actor || quasiSelectionActive != activeTouch)
                {
                    EndQuasi();
                    quasiSelected = actor;
                    quasiSelectionActive = activeTouch;
                    MakeQuasiHighlight();
                }
            }
        }

        /// <summary>
        /// Kill the touch-over highlight (yellow).
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
        /// Decide whether user is controlling yaw or pitch.
        /// </summary>
        /// <returns></returns>
        private bool CheckOrbitMode()
        {
#if MF_PITCH_OR_YAW
            if (TouchInput.TouchCount == 2) //PV: to fingers down, start the orbit process
            {
                if (TouchInput.WasTouched)
                {
                    touchPosition = PixelToNDC(TouchInput.GetAsPoint(TouchInput.GetTouchContactByIndex(0).position)) - cursorOffset;
                }

                if (TouchInput.IsTouched)
                {
                    if (orbitMode == OrbitMode.None)
                    {
                        Vector2 pos = PixelToNDC(TouchInput.GetAsPoint(TouchInput.GetTouchContactByIndex(0).position)) - cursorOffset;

                        Vector2 prev = touchPosition;

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

                                Vector2 deadzone = DeadZone(TouchInput.GetAsPoint(TouchInput.GetTouchContactByIndex(0).previousPosition));

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
            }
            return AffectingOrbit && (orbitMode != OrbitMode.None);
#else //!MF_PITCH_OR_YAW
            return false;
#endif // MF_PITCH_OR_YAW

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

        public void AddPitchYawByDrag(SmoothCamera camera)
        {
            //don't allow switching from multi touch to single touch drag
            if (TouchInput.IsTouched && TouchInput.TouchCount == 1 && !TouchInput.WasMultiTouch)
            {
                //check the latest touch (since we know we only have one) and handle drag (pitches/yaws)
                TouchContact touch = TouchInput.GetTouchContactByIndex(0);

                float touchAge = (float)(Time.WallClockTotalSeconds - touch.startTime);
                DragGestureRecognizer dragGesture = TouchGestureManager.Get().DragGesture;

                if ( null != dragGesture && dragGesture.IsDragging )
                {
                    if (touch.position - touch.previousPosition != Vector2.Zero 
                        && resolution.X>0.0f && resolution.Y>0.0f)
                    {

                        Vector2 pos = dragGesture.DragPosition;
                        Vector2 prevPos = dragGesture.DragPrevPosition;

                        float dpX = (pos.X - prevPos.X) / resolution.X;
                        float dpY = (pos.Y - prevPos.Y) / resolution.Y;

                        float scaleX = PixelToNDC(pos).X - cursorOffset.X;
                        float scaleY = PixelToNDC(pos).Y - cursorOffset.Y;

                        //convert to x^2 curve for smoother transition, clamp to [0, 1] range
                        scaleX = MathHelper.Clamp(1.0f - scaleX * scaleX, 0.0f, 1.0f);
                        scaleY = MathHelper.Clamp(1.0f - scaleY * scaleY, 0.0f, 1.0f);

                        float maxScale = 1.0f;
                        float minScale = 0.5f;

                        //how fast does the terrain move with the drag
                        float speed = 6.0f;

                        scaleY = scaleY * (maxScale - minScale) + minScale;
                        scaleX = scaleX * (maxScale - minScale) + minScale;

                        dpY = dpY * scaleY * speed;
                        dpX = dpX * scaleX * speed;

                        //do drag rotation absolutely
                        camera.DesiredRotation -= dpX;
                        camera.DesiredPitch -= dpY;
                    }
                }
            }
        }

        /// <summary>
        /// Adjust yaw to mouse movement horizontally across the screen.
        /// </summary>
        /// <param name="camera"></param>
        public void ProcessCameraRotation(SmoothCamera camera)
        {
            if (TouchInput.IsTouched && TouchInput.TouchCount == 2) 
            {
                //check if both touches are moving
                for (int i = 0; i < TouchInput.TouchCount; i++)
                {
                    if (TouchInput.GetTouchContactByIndex(i).phase != TouchPhase.Moved)
                    {
                        return;
                    }
                }

                //Rotate if rotation was active before DoubleDrag and Pinch.
                RotationGestureRecognizer rotationGesture = TouchGestureManager.Get().GetActiveGesture( TouchGestureType.Rotate, TouchGestureType.DoubleDrag, TouchGestureType.Pinch ) as RotationGestureRecognizer;

                if ( null != rotationGesture && rotationGesture.IsRotating )
                {
                    Vector3 worldPos = FindHit(camera, TouchInput.GetAsPoint(rotationGesture.AveragePosition));

                    camera.RotateAboutArbitraryPoint(worldPos, rotationGesture.RotationDelta);
                }
            }
        }
        private int twitchCount = 0;

        protected void rotTwitchComplete(Object param)
        {
            TouchEdit touchEdit = param as TouchEdit;

            touchEdit.twitchCount--;
            if (touchEdit.twitchCount < 0)
                touchEdit.twitchCount = 0;
        }

        /// <summary>
        /// Helper function that returns both touch contacts
        /// </summary>
        /// <param name="touch0"></param>
        /// <param name="touch1"></param>
        /// <returns>false if less than two fingers are touching</returns>
        private bool GetTouchContact(ref TouchContact touch0, ref TouchContact touch1)
        {
            for (int i = 0; i < TouchInput.TouchCount; i++)
            {
                if (touch0 == null)
                {
                    touch0 = TouchInput.GetTouchContactByIndex(i);
                    continue;
                }

                if (touch1 == null)
                {
                    touch1 = TouchInput.GetTouchContactByIndex(i);
                    continue;
                }
            }

            if (touch0 == null || touch1 == null) //missing finger??
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Helper function to return the distance between the two touch fingers
        /// </summary>
        private bool GetTouchDistance(ref Vector2 distance, ref Vector2 touchPos0, ref Vector2 touchPos1)
        {
            TouchContact touch0 = null;
            TouchContact touch1 = null;

            //check if two fingers are down
            if (GetTouchContact(ref touch0, ref touch1) == false)
                return false;

            //bail if the fingers are jittering by 3 pixels
            //if (touch0.deltaPosition.LengthSquared() <= 4.0f * 4.0f && touch1.deltaPosition.LengthSquared() <= 4.0f * 4.0f)
            //{
            //    return false;
            //}
            //if (touch0.deltaPosition.LengthSquared() != touch1.deltaPosition.LengthSquared() )
            //{
            //    return false;
            //}

            distance = touch0.position - touch1.position;
            touchPos0 = touch0.position;
            touchPos1 = touch1.position;

            //Debug.Print("touch0.position=" + touch0.position.ToString());
            //Debug.Print("touch1.position=" + touch1.position.ToString());
            return true;
        }

        /// <summary>
        /// Determines if two touch fingers are moving in the same direction
        /// </summary>
        /// <returns>true moving in the same direction </returns>
        private bool TouchMovingInSameDirection()
        {
            TouchContact touch0 = null;
            TouchContact touch1 = null;
            if (GetTouchContact(ref touch0, ref touch1) == false)
                return false;

            Vector2 dir0 = touch0.deltaPosition;
            Vector2 dir1 = touch1.deltaPosition;

            //start processing if both fingers are moving
            if (dir0.LengthSquared() > 0.0f &&
                dir1.LengthSquared() > 0.0f)
            {
                //normalize the direction vectors of the touch contacts
                dir0.Normalize();
                dir1.Normalize();

                //if the vectors are within five degrees of each other then return true
                return Vector2.Dot(dir0, dir1) >= Math.Cos(MathHelper.ToRadians(5.0f)); 

            }
            return false;
        }

        /// <summary>
        /// Determine if two fingers are moving apart
        /// </summary>
        /// <returns></returns>
        private bool TouchMovingOppositeDirection()
        {
            TouchContact touch0 = null;
            TouchContact touch1 = null;

            if (GetTouchContact(ref touch0, ref touch1) == false)
                return false;

            Vector2 dir0 = touch0.deltaPosition;
            Vector2 dir1 = touch1.deltaPosition;

            float lengthDir0 = dir0.LengthSquared();
            float lengthDir1 = dir1.LengthSquared();

            //start processing if both fingers are moving
            if (lengthDir0 > 0.0f &&
                lengthDir1 > 0.0f)
            {
                //normalize the direction vectors of the touch contacts
                dir0.Normalize();
                dir1.Normalize();

                //gCosMovingDirection = Vector2.Dot(dir0, dir1);
                //gZoomTouchPos0 = dir0;
                //gZoomTouchPos1 = dir1;

                //if the vectors are within 170 degrees of each other then return true
                return Vector2.Dot(dir0, dir1) <= Math.Cos(MathHelper.ToRadians(160.0f));
            }

            //only one finger moving
            if (lengthDir0 > 0.0f && !(lengthDir1 > 0.0f))
            {
                Vector2 dir = touch0.startPosition - touch1.startPosition;
                dir.Normalize();

                dir0 = touch0.position - touch0.startPosition;
                dir0.Normalize();

                float dot = Vector2.Dot(dir0, dir);

                bool movingApart = dot >= Math.Cos(MathHelper.ToRadians(60.0f)) && dot <= Math.Cos(MathHelper.ToRadians(120.0f));

                return !movingApart;
            }

            if (!(lengthDir0 > 0.0f) && lengthDir1 > 0.0f)
            {
                Vector2 dir = touch1.startPosition - touch0.startPosition;
                dir.Normalize();

                dir1 = touch1.position - touch1.startPosition;
                dir1.Normalize();

                float dot = Vector2.Dot(dir1, dir);

                bool movingApart = dot >= Math.Cos(MathHelper.ToRadians(60.0f)) && dot <= Math.Cos(MathHelper.ToRadians(120.0f));

                return !movingApart;
            }

            return false;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="touchPosition"></param>
        /// <param name="granularityX"></param>
        /// <param name="granularityY"></param>
        /// <returns></returns>
        private Vector2 GetTouchInGridResolution(Vector2 touchPosition, float xGranularity, float yGranularity)
        {
            //float xCameraInc = camera.Resolution.X / resolutionX;
            float xOffset = (float)Math.Floor(touchPosition.X / xGranularity) * xGranularity;
            float yOffset = (float)Math.Floor(touchPosition.Y / yGranularity) * yGranularity;

            //now return a position that is at the center of the grid
            Vector2 gridPosition = new Vector2(xOffset+xGranularity*0.5f, yOffset+yGranularity*0.5f);

            return gridPosition;
        }

        /// <summary>
        /// Move the dragged object from the new mouse position.
        /// </summary>
        /// <param name="camera"></param>
        private void DragObject(Camera camera)
        {
            Debug.Assert(TouchInput.IsTouched, "Touch data missing!");
            if (TouchInput.TouchCount != 1) //PV: only only drag when you touch with one finger
                return;

            TouchContact touchInput = TouchInput.GetTouchContactByIndex(0);
            if (touchInput == null)
                return;

            if (dragObject != null && touchInput.deltaPosition.LengthSquared()>0.0f) //PV: draging has some movement
            {
                if (TouchGestureManager.Get().DragGesture.IsDragging)
                {
                    Point prevTouchPosition = TouchInput.GetAsPoint(touchInput.previousPosition);
                    Point touchPosition = TouchInput.GetAsPoint(touchInput.position);

                    float height = dragObject.Movement.Position.Z;
                    Vector3 prevPos = FindAtHeight(camera, prevTouchPosition, height);
                    Vector3 pos = FindAtHeight(camera, touchPosition, height);
                    Vector2 delPos = new Vector2(pos.X - prevPos.X, pos.Y - prevPos.Y);
                    float kMaxDragDistSq = 2500.0f;
                    if (delPos.LengthSquared() < kMaxDragDistSq)
                    {
                        Vector3 oldPos = dragObject.Movement.Position;
                        Vector2 newPos = new Vector2(oldPos.X + delPos.X, oldPos.Y + delPos.Y);

                        owner.DragSelectedObject(dragObject, newPos, false);
                    }
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
            //two ways to drag: one finger drag (any direction), two finger drag (horizontal only)
            
            //first, check early out for conditions that both reject:
            //don't allow dragging if rotating, pinching, double dragging (vertical), or we don't have a single touch
            if (!TouchInput.IsTouched ||
                TouchInput.TouchCount<=0 || 
                TouchInput.TouchCount>2)
            {
                return position;
            }

            //check for double drag pan (horizontal)
            DoubleDragGestureRecognizer dblDragGesture = TouchGestureManager.Get().DoubleDragGesture;
            if (null != dblDragGesture && dblDragGesture.IsDragging)
            {
                //we're good, use this gesture as the pan

                //find the position on the terrain and calculate the delta
                Vector3 prevPos = FindHit(camera, TouchInput.GetAsPoint(dblDragGesture.PreviousAveragePosition));
                Vector3 pos = FindAtHeight(camera, TouchInput.GetAsPoint(dblDragGesture.AveragePosition), prevPos.Z);
                Vector2 del = new Vector2(pos.X - prevPos.X, pos.Y - prevPos.Y);

                position -= del;
                return position;
            }


            return position;
        }


        private List<Vector2Object> previousDeltas = new List<Vector2Object>(); //Vector2.Zero;
        private Vector2 previousDelta = Vector2.Zero;

        private Vector2 lastTouchVelocity = Vector2.Zero;
        private Vector2 lastTouchPosition = Vector2.Zero;
        private Vector2 lastTouchStartPosition = Vector2.Zero;

        private Vector2 previousPosition = Vector2.Zero;
        private Vector2 currentPosition = Vector2.Zero;
        private List<Vector2> listPositions = new List<Vector2>();
        private List<float> listPositionTimes = new List<float>();
        

        /// <summary>
        /// Simple object class to store the cameras delta position (used in the twitch delta
        /// </summary>
        class Vector2Object : Object
        {
            public Vector2Object(Vector2 v)
            {
                theVector2 = v;
                isDone = false;
            }

            public Vector2 theVector2;
            public bool isDone;
        }

        //if this delta is done we remove the vector from the list
        protected void TwitchCompleteEvent(Object param)
        {
            Vector2Object vectorObj = param as Vector2Object;

            vectorObj.isDone = true;
            //previousDeltas.RemoveAt(indexObj.theIndex);
        }

        protected void velocityDelegate(Vector2 val, Object param)
        {
            Vector2Object vectorObj = param as Vector2Object;

            //save the new delta in list of deltas
            vectorObj.theVector2 = val;
        }

        private Vector2 ComputeDeltaPosition(Camera camera, Vector2 previousPosition, Vector2 currentPosition)
        {
            Vector3 prevPos = FindHit(camera, TouchInput.GetAsPoint(previousPosition)/*, true*/); //PV - clamp the ray height
            Vector3 pos = FindAtHeight(camera, TouchInput.GetAsPoint(currentPosition), prevPos.Z);
            Vector2 del = new Vector2(pos.X - prevPos.X, pos.Y - prevPos.Y);

            //PV - Temp hack to prevent the camera from reversing directions
            Vector3 ray = FindRay(camera, TouchInput.GetAsPoint(previousPosition));
            if (ray.Z > 0.0f)
                del *= -1.0f;

            //float len = del.Length();
            //if (len > 10.0f)
            //{
                //del.Normalize();
                //del *= 10.0f;
            //}

            return del;
        }

        private void UpdateTwitches(ref Vector2 position)
        {
            int i = 0;
            for (i = 0; i < previousDeltas.Count; i++) // Vector2Object delta in previousDeltas)
            {
                Vector2Object delta = previousDeltas[i];
                if (!delta.isDone)
                    position -= delta.theVector2;
            }

            foreach (Vector2Object delta in previousDeltas)
            {
                if (delta.isDone)
                {
                    previousDeltas.Remove(delta);
                    break;
                }
            }
        }

        /// <summary>
        /// Find the hit point of the ground at z=0.0f
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="mouse"></param>
        /// <returns></returns>
        public static Vector3 FindHitGround(Camera camera, Point mouse)
        {
            Vector3 ray = FindRay(camera, mouse);
            Vector3 src = camera.ActualFrom;

            Vector3 hit = new Vector3();

            //
            //check if the ray intersect the horizontal plane
            Plane thePlane = new Plane(new Vector3(0.0f, 0.0f, 1.0f), 0.0f);
            float distance1 = 0.0f;

            if (PlaneIntersection(src, ray, thePlane, ref distance1))
            {
                distance1 = Math.Min(distance1, kMaxRayCast);
                hit = src + ray * distance1;
            }
            else // try intersecting with a vertical plane (along the camera view vector)
            {
                //get camera's view vector
                Vector3 viewDir = camera.ViewDir;

                //remove the z (height)
                viewDir.Z = 0.0f;
                viewDir.Normalize();

                //set up the plane
                thePlane.Normal = viewDir;
                thePlane.D = -Vector3.Dot(viewDir, camera.ActualAt);

                if (PlaneIntersection(src, ray, thePlane, ref distance1))
                {
                    distance1 = Math.Min(distance1, kMaxRayCast);
                    hit = src + ray * distance1;
                }
                //no intersection? we're screwed
            }

            return hit;
        }

        /// <summary>
        /// Find where a ray through input mouse position (pixel coords) hits the terrain.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="mouse"></param>
        /// <returns></returns>
        public static Vector3 FindHit(Camera camera, Point mouse)
        {
            Vector3 ray = FindRay(camera, mouse);
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

        private static Vector3 LimitRay3D(Vector3 ray)
        {
            float len = ray.Length();
            if (len > kMaxRayCast)
            {
                ray.Normalize();
                ray *= kMaxRayCast;

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
            Vector3 ray = FindRay(camera, mouse);

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
        /// Transform a touch position (pixel coords) into a normalized
        /// world space ray.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="touchInput"></param>
        /// <returns></returns>
        private static Vector3 FindRay(Camera camera, Point touchInput)
        {
            Vector3 ray = camera.ScreenToWorldCoords(
                new Vector2(touchInput.X, touchInput.Y));

            return ray;
        }

        /// <summary>
        /// Test whether the user is currently touching the screen to control the camera.
        /// Will return false if the user is using the mouse for something else, like editing.
        /// </summary>
        private bool AffectingOrbit
        {
            get
            {
                return TouchInput.IsTouched;
                //return 
                //    TouchInput.IsTouched
                //    && !disableRightOrbit;
            }
            /*
                return
                    TouchInput.Right.IsPressed

                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.Space)
                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.LeftControl)
                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.RightControl)
                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.LeftAlt)
                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.RightAlt)
                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.LeftShift)
                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.RightShift)
                    
                    && !disableRightOrbit;
            }
             */
        }

        /// <summary>
        /// Test whether the user is currently using touch to drag the world.
        /// Will return false if the user is using the mouse for something else, like editing.
        /// </summary>
        private bool AffectingDrag
        {
            get
            {
                return TouchInput.IsTouched;
                    /*
                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.Space)
                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.LeftControl)
                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.RightControl)
                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.LeftAlt)
                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.RightAlt)
                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.LeftShift)
                    && !KeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.RightShift)
                    */
                    //&& !disableLeftDrag;
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
        /// Transform pixel coordinates to NDC.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private Vector2 PixelToNDC(Vector2 p)
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


        ////////////////////////

        /// <summary>
        /// Helper function to find a ray plane itersection
        /// </summary>
        /// <param name="srcPos"></param>
        /// <param name="ray"></param>
        /// <param name="plane"></param>
        /// <param name="hitDistance"></param>
        /// <returns></returns>
        static private bool PlaneIntersection(Vector3 srcPos, Vector3 ray, Plane plane, ref float hitDistance)
        {
            Ray ray1 = new Ray(srcPos, ray);

            float? distance = ray1.Intersects(plane);

            if( distance !=null )
                hitDistance = distance.GetValueOrDefault();

            return distance!=null;
        }

        #endregion Helpers

        #endregion Internal
    };
};
