
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

using KoiX;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI;
using KoiX.UI.Dialogs;

using Boku;
using Boku.Base;
using Boku.Common;
using Boku.Fx;
using Boku.SimWorld;
using Boku.SimWorld.Path;
using Boku.SimWorld.Terra;

namespace KoiX.Scenes
{
    public class EditWorldScene : BaseScene
    {
        public enum ToolMode
        {
            Home,
            Play,
            CameraMove,
            EditObject,
            Paths,
            TerrainPaint,
            TerrainRaiseLower,
            TerrainSpikeyHilly,
            TerrainSmoothLevel,
            Water,
            EraseObjects,
            WorldSettings,
            None
        }

        public const float kMaxBrushRadius = 150.0f;

        #region Members

        static EditWorldScene instance;
        static HitInfo mouseMouseTouchHitInfo = new HitInfo();

        // Camera used to scale toolBar and palette.
        SpriteCamera camera;

        ToolBarDialog toolBarDialog;
        ToolPaletteDialog toolPaletteDialog;
        GamePadToolBarDialog gamePadToolBarDialog;

        // Current tool mode based on state of ToolBarDialog and ToolPaletteDialog / GamePadToolBarDialog.
        ToolMode currentToolMode = ToolMode.None;

        int actorTabIndex = 0;  // When the user hits Tab or CTab we cycle to the next/prev actor in the list.
                                // If this index is invalid (we deleted enough actors, just loop back to 0).

        // Cursor and brush stuff.
        Vector3 cursorPosition;
        Vector3 snapToGridError;    // Error from "real" position when cursor is moved due to grid snapping.

        #endregion

        #region Accessors

        static public ToolMode CurrentToolMode
        {
            get { return instance.currentToolMode; }
            set { instance.currentToolMode = value; }
        }

        /// <summary>
        /// Are we currently editing terrain?
        /// </summary>
        static public bool EditingTerrain
        {
            get
            {
                return instance.currentToolMode == ToolMode.TerrainPaint
                    || instance.currentToolMode == ToolMode.TerrainRaiseLower
                    || instance.currentToolMode == ToolMode.TerrainSmoothLevel
                    || instance.currentToolMode == ToolMode.TerrainSpikeyHilly;
            }                    
        }

        /// <summary>
        /// This frame's cached info on what the mouse is over. Pretty much read only,
        /// maintained internally.
        /// </summary>
        public static HitInfo MouseTouchHitInfo
        {
            get { return mouseMouseTouchHitInfo; }
        }

        public Vector3 CursorPosition
        {
            get
            {
                Vector3 pos = cursorPosition;

                if (InGame.inGame.SnapToGrid)
                {
                    pos -= snapToGridError;
                    Vector3 prevPos = pos;

                    pos = InGame.SnapPosition(pos);

                    snapToGridError = pos - prevPos;
                }

                return pos;
            }
            set
            {
                cursorPosition = value;
            }
        }

        #endregion

        #region Public

        // c'tor
        public EditWorldScene()
            : base("EditWorldScene")
        {
            instance = this;

            camera = new SpriteCamera();
            // Key/Mouse tools.
            toolBarDialog = new ToolBarDialog();
            toolPaletteDialog = new ToolPaletteDialog();
            // GamePad tools.
            gamePadToolBarDialog = new GamePadToolBarDialog();
        }

        public override void Update()
        {
            if (Active)
            {
                // Keep camera in sync with screen size.
                BaseScene.SetCameraToTargetResolution(camera);

                BokuGame.bokuGame.shaderGlobals.Update();

                // Based on current input mode, change tool dialogs.
                if (KoiLibrary.LastTouchedDeviceIsGamepad)
                {
                    if (toolBarDialog.Active)
                    {
                        DialogManagerX.KillDialog(toolBarDialog);
                        DialogManagerX.KillDialog(toolPaletteDialog);
                    }
                    if (!gamePadToolBarDialog.Active)
                    {
                        DialogManagerX.ShowDialog(gamePadToolBarDialog);
                    }
                }
                else
                {
                    if (gamePadToolBarDialog.Active)
                    {
                        DialogManagerX.KillDialog(gamePadToolBarDialog);
                    }
                    if (!toolBarDialog.Active)
                    {
                        DialogManagerX.ShowDialog(toolBarDialog, camera);
                        DialogManagerX.ShowDialog(toolPaletteDialog, camera);
                    }
                }

                // Keep CurrentToolMode up to date.
                switch (ToolBarDialog.CurTool)
                {
                    case EditModeTools.Home:
                        currentToolMode = ToolMode.Home;
                        break;
                    case EditModeTools.Play:
                        currentToolMode = ToolMode.None;
                        break;
                    case EditModeTools.CameraMove:
                        currentToolMode = ToolMode.CameraMove;
                        break;
                    case EditModeTools.EditObject:
                        currentToolMode = ToolMode.EditObject;
                        break;
                    case EditModeTools.Paths:
                        currentToolMode = ToolMode.Paths;
                        break;
                    case EditModeTools.TerrainPaint:
                        currentToolMode = ToolMode.TerrainPaint;
                        break;
                    case EditModeTools.TerrainRaiseLower:
                        currentToolMode = ToolMode.TerrainRaiseLower;
                        break;
                    case EditModeTools.Water:
                        currentToolMode = ToolMode.Water;
                        break;
                    case EditModeTools.EraseObjects:
                        currentToolMode = ToolMode.EraseObjects;
                        break;
                    case EditModeTools.WorldSettings:
                        currentToolMode = ToolMode.WorldSettings;
                        break;
                    case EditModeTools.None:
                        currentToolMode = ToolMode.None;
                        break;
                }

                // For continuous keyboard input we need to check every frame.  this
                // means we need to manually check for modal dialogs.
                // TODO (scoy) Maybe add a new input type that triggers every frame
                // like MousePosition but for the keyboard?
                if (!DialogManagerX.ModalDialogIsActive)
                {
                    if (LowLevelKeyboardInput.IsPressed(Keys.W)) InGame.inGame.Camera.MoveWASD(Keys.W);
                    if (LowLevelKeyboardInput.IsPressed(Keys.A)) InGame.inGame.Camera.MoveWASD(Keys.A);
                    if (LowLevelKeyboardInput.IsPressed(Keys.S)) InGame.inGame.Camera.MoveWASD(Keys.S);
                    if (LowLevelKeyboardInput.IsPressed(Keys.D)) InGame.inGame.Camera.MoveWASD(Keys.D);
                }

                // TODO (scoy) After things are more cleaned up and settled, consider where these might better live.
                // Keep all the world systems ticking over.
                UpdateWorld();

            }   // end if Active.
        }   // end of Update()

        public override void Render(RenderTarget2D rt)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            if (rt != null)
            {
                device.SetRenderTarget(rt);
            }

            // Set up lighting.
            BokuGame.bokuGame.shaderGlobals.Render(InGame.inGame.Camera);

            // Render all the active objects.
            BokuGame.gameListManager.Render();

            if (rt != null)
            {
                device.SetRenderTarget(null);
            }
        }   // end of Render()

        /// <summary>
        /// Activate this scene.
        /// </summary>
        /// <param name="args">optional argument list.  Most Scenes will not use one but for those cases where it's needed this is here.</param>
        /// <summary>
        /// Shouldn't be called by user code.  Is called by SceneManager when 
        /// switching scenes.
        /// </summary>
        public override void Activate(params object[] args)
        {
            if (!Active)
            {
                if (args != null)
                {
                    foreach (object arg in args)
                    {
                        // Do something with each arg...
                    }
                }

                InGame.inGame.Activate();
                InGame.inGame.CurrentUpdateMode = InGame.UpdateMode.MouseEdit;

                base.Activate(args);
            }
        }   // end of default Activate()

        /// <summary>
        /// Shouldn't be called by user code.  Is called by SceneManager when 
        /// switching scenes.
        /// </summary>
        public override void Deactivate()
        {
            if (Active)
            {
                // Autosave any changes.
                if (InGame.IsLevelDirty)
                {
                    InGame.UnDoStack.Store();
                }

                DialogManagerX.KillDialog(toolBarDialog);
                DialogManagerX.KillDialog(toolPaletteDialog);

                base.Deactivate();
            }
        }   // end of default Deactivate()

        #endregion

        #region InputEventHandler

        public override void RegisterForEvents()
        {
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftDown);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseRightDown);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseWheel);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);

            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Tap);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.OnePointDrag);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.TwoPointDrag);

        }   // end of RegisterForEvents()

        public override bool ProcessMouseLeftDownEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseFocusObject == null)
            {
                // Claim mouse focus as ours.
                KoiLibrary.InputEventManager.MouseFocusObject = this;

                // Register to get move and left up events.
                KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseMove);
                KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftUp);

                return true;
            }

            return base.ProcessMouseLeftDownEvent(input);
        }   // end of ProcessMouseLeftDownEvent()

        public override bool ProcessMouseRightDownEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseFocusObject == null)
            {
                // Claim mouse focus as ours.
                KoiLibrary.InputEventManager.MouseFocusObject = this;

                // Register to get move and left up events.
                KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseMove);
                KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseRightUp);

                return true;
            }

            return base.ProcessMouseRightDownEvent(input);
        }   // end of ProcessMouseRightDownEvent()

        public override bool ProcessMouseMoveEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseFocusObject == this)
            {
                SmoothCamera camera = InGame.inGame.Camera;

                if (LowLevelMouseInput.Left.IsPressed)
                {
                    DragTerrain(input.Position, input.DeltaPosition);
                }
                else if (LowLevelMouseInput.Right.IsPressed)
                {
                    // Orbit camera.
                    Vector2 pos = LowLevelMouseInput.PositionVec;
                    Vector2 prev = LowLevelMouseInput.PrevPositionVec;
                    camera.Orbit(prev, pos);
                }

                return true;
            }

            return base.ProcessMousePositionEvent(input);
        }   // end of ProcessMousePositionEvent()

        public override bool ProcessMouseLeftUpEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseFocusObject == this)
            {
                // Release mouse focus.
                if (KoiLibrary.InputEventManager.MouseFocusObject == this)
                {
                    KoiLibrary.InputEventManager.MouseFocusObject = null;
                }

                // Stop getting move and up events.
                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MousePosition);
                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MouseLeftUp);

                return true;
            }
            return false;
        }   // end of ProcessMouseLeftUpEvent()

        public override bool ProcessMouseRightUpEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseFocusObject == this)
            {
                // Release mouse focus.
                if (KoiLibrary.InputEventManager.MouseFocusObject == this)
                {
                    KoiLibrary.InputEventManager.MouseFocusObject = null;
                }

                // Stop getting move and up events.
                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MousePosition);
                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MouseRightUp);

                return true;
            }
            return false;
        }   // end of ProcessMouseRightUpEvent()

        public override bool ProcessMouseWheelEvent(MouseInput input)
        {
            if (KoiLibrary.InputEventManager.MouseFocusObject == null)
            {
                SmoothCamera camera = InGame.inGame.Camera;
                camera.DoZoom(input.E.Delta);
                return true;
            }
            return base.ProcessMouseWheelEvent(input);
        }   // end of ProcessMouseWheelEvent()

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            if (KoiLibrary.InputEventManager.MouseFocusObject == null)
            {
                bool handled = false;
                SmoothCamera camera = InGame.inGame.Camera;

                switch(input.Key)
                {
                    case Keys.W:
                    case Keys.A:
                    case Keys.S:
                    case Keys.D:
                        camera.MoveWASD(input.Key);
                        handled = true;
                        break;

                    case Keys.Home:
                        SceneManager.SwitchToScene("HomeMenuScene", frameDelay: 1);
                        // Refresh the thumbnail during our 1 frame delay.
                        InGame.RefreshThumbnail = true;
                        handled = true;
                        break;

                    case Keys.Back:
                    case Keys.Escape:
                        SceneManager.SwitchToScene("RunSimScene");
                        handled = true;
                        break;

                    case Keys.Tab:
                        if (LowLevelKeyboardInput.ShiftPressed)
                        {
                            PrevActor();
                        }
                        else
                        {
                            NextActor();
                        }

                        handled = true;
                        break;

                    case Keys.F4:
                        // F4 Recenter camera:
                        //  If any characters in game, move to nearest.
                        //  else center at origin.
                        //  Adjust zoom to reasonable value.
                        //  Adjust camera height to reasonable angle.
                        {

                            // Find nearest actor position.
                            Vector3 nearestPosition = Vector3.Zero;
                            float dist2 = float.MaxValue;
                            foreach (GameThing thing in InGame.inGame.gameThingList)
                            {
                                if (!(thing is CursorThing))
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
                            CursorPosition = nearestPosition;

                            camera.DesiredDistance = 20.0f;
                            camera.DesiredPitch = -0.5f;     // ~30 degrees.

                        }   // end if F4 pressed.
                        handled = true;
                        break;

                }   // end of switch

                if (handled)
                {
                    return true;
                }
            }

            return base.ProcessKeyboardEvent(input);
        }   // end of ProcessKeyboardEvent()

        public override bool ProcessTouchTapEvent(TapGestureEventArgs gesture)
        {
            Debug.Assert(Active);

            Vector3 target = InGame.inGame.Camera.FindHit(gesture.Position);
            InGame.inGame.Camera.DesiredAt = target;

            return true;
        }   // end of ProcessTouchTapEvent()

        public override bool ProcessTouchOnePointDragEvent(OnePointDragGestureEventArgs gesture)
        {
            Debug.Assert(Active);

            // Are we at the beginning of a drag?
            if (gesture.Gesture == GestureType.OnePointDragBegin && KoiLibrary.InputEventManager.TouchFocusObject == null)
            {
                // Take focus.
                KoiLibrary.InputEventManager.TouchFocusObject = this;

                return true;
            }

            if (KoiLibrary.InputEventManager.TouchFocusObject == this)
            {
                // End of drag?  Release focus.
                if (gesture.Gesture == GestureType.OnePointDragEnd)
                {
                    KoiLibrary.InputEventManager.TouchFocusObject = null;

                    return true;
                }

                DragTerrain(gesture.CurrentPosition, gesture.DeltaPosition);

                return true;
            }

            return base.ProcessTouchOnePointDragEvent(gesture);
        }   // end of ProcessTouchOnePointDragEvent()

        public override bool ProcessTouchTwoPointDragEvent(TwoPointDragGestureEventArgs gesture)
        {
            Debug.Assert(Active);

            // Handle orbiting.
            // TODO (scoy) Make sure this works.  We currently have two modes of orbit.  The
            // first works like mouse orbit using the Center location as the cursor.  The
            // second uses the relative angle of the touches to orbit.
            // Which works better?  Currently overlaying both...

            // Mouse style orbit:
            Vector2 prevCenter = gesture.CurrentCenter - gesture.DeltaCenter;
            InGame.inGame.Camera.Orbit(prevCenter, gesture.CurrentCenter);
            
            // Twist orbit:  We want to orbit around the center of the gesture
            // rather than the camera at point.  This feels more direct.
            Vector3 rotationPoint = InGame.inGame.Camera.FindHit(gesture.CurrentCenter);
            InGame.inGame.Camera.RotateAboutArbitraryPoint(rotationPoint, gesture.DeltaAngle);

            // Handle pinch zooming.
            float zoom = (gesture.CurrentDistance - gesture.DeltaDistance) / gesture.CurrentDistance;
            InGame.inGame.Camera.DesiredDistance *= zoom;

            return true;
        }   // end of ProcessTouchTwoPointDragEvent()

        #endregion



        #region Internal

        /// <summary>
        /// Use mouse or touch input to drag terrain.
        /// </summary>
        /// <param name="curPosition"></param>
        /// <param name="delta"></param>
        void DragTerrain(Vector2 curPosition, Vector2 delta)
        {
            Vector2 prevPosition = curPosition - delta;
            if (delta != Vector2.Zero)
            {
                SmoothCamera camera = InGame.inGame.Camera;

                Vector3 prevPos = camera.FindHit(prevPosition);
                Vector3 pos = camera.FindAtHeight(curPosition, prevPos.Z);
                Vector3 del = pos - prevPos;

                CursorPosition -= del;
                camera.DesiredAt = CursorPosition;
            }
        }   // end of DragTerrain()

        void NextActor()
        {
            if (InGame.inGame.gameThingList.Count > 0)
            {
                actorTabIndex = (actorTabIndex + 1) % InGame.inGame.gameThingList.Count;

                Vector3 actorPos = InGame.inGame.gameThingList[actorTabIndex].Movement.Position;
                // Move cursor.
                CursorPosition = actorPos;
                // Update camera based on new position/orientation.
                InGame.inGame.Camera.DesiredAt = CursorPosition;
            }
        }   // end of NextActor()

        void PrevActor()
        {
            if (InGame.inGame.gameThingList.Count > 0)
            {
                actorTabIndex = (actorTabIndex + InGame.inGame.gameThingList.Count - 1) % InGame.inGame.gameThingList.Count;

                Vector3 actorPos = InGame.inGame.gameThingList[actorTabIndex].Movement.Position;
                // Move cursor.
                CursorPosition = actorPos;
                // Update camera based on new position/orientation.
                InGame.inGame.Camera.DesiredAt = CursorPosition;
            }
        }   // end of PrevActor()

        /// <summary>
        /// Common update for rest of world.
        /// </summary>
        public void UpdateWorld()
        {
            // Update terrain.
            InGame.inGame.Terrain.Update(InGame.inGame.Camera);

            // Since we're eliminating InGameMouseEdit which normally calls this
            // we need to call it explicitely.
            InGame.inGame.UpdateObjects();

            // Update the particle system.
            InGame.inGame.ParticleSystemManager.Update();
            DistortionManager.Update();
            FirstPersonEffectMgr.Update();

            // Update the waypoints.
            WayPoint.UpdateAllPaths(InGame.inGame.Camera);

            // Keep the edit brush in sync with the cursor position.
            UpdateEditBrush();

            // TODO (scoy) Not sure this is 100% the right place for this.
            ToolTipManager.Update();
            ThoughtBalloonManager.Update(InGame.inGame.Camera);

        }   // end of UpdateWorld()

        /// <summary>
        /// Keeps the edit brush stuff in sync with the current world cursor.
        /// 
        /// TODO (scoy) Should edit brush be a seperate object owned by the scene?
        /// </summary>
        public void UpdateEditBrush()
        {
            float secs = Time.WallClockFrameSeconds;
            HitInfo hitInfo = MouseEdit.MouseTouchHitInfo;

            InGame.inGame.Cursor3D.Position = CursorPosition;
            CursorPosition = InGame.inGame.Cursor3D.Position;

            Vector2 newPosition = new Vector2(CursorPosition.X, CursorPosition.Y);

            if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
            {
                newPosition = ApplyTerrainLimits(InGame.inGame.Camera, LowLevelMouseInput.PositionVec);
                InGame.inGame.Cursor3D.AltPosition = new Vector3(newPosition, 0);
            }
            else if (KoiLibrary.LastTouchedDeviceIsTouch)
            {
                TouchContact editingTouch = FindNonUITouch();
                if (editingTouch != null)
                {
                    newPosition = ApplyTerrainLimits(InGame.inGame.Camera, editingTouch.position);

                    InGame.inGame.Cursor3D.AltPosition = new Vector3(newPosition, 0);

                    ToggleTouch3DCursor();

                    //every frame track whether or not it's valid to apply the current terrain tool (paint, etc.)
                    //this ensures we don't apply the tool just by selecting it
                    if (TouchInput.TouchCount == 1 && InGame.inGame.TouchEdit.HasNonUITouch() && !TouchInput.WasMultiTouch)
                    {
                        float touchAge = (float)(Time.WallClockTotalSeconds - TouchInput.Touches[0].startTime);

                        //edit brush only allowed if touch has been around for at least 1/4 of a second
                        InGame.inGame.shared.editBrushAllowedForTouch = touchAge >= 0.25f;
                    }
                    else
                    {
                        InGame.inGame.shared.editBrushAllowedForTouch = false;
                    }
                }
            }
            else
            {
                InGame.inGame.Cursor3D.AltPosition = InGame.inGame.Cursor3D.Position;
            }

            const float minEditBrushMoveSq = 0.25f * 0.25f;
            if (Vector2.DistanceSquared(newPosition, InGame.inGame.shared.editBrushPosition) >= minEditBrushMoveSq)
            {
                InGame.inGame.shared.editBrushMoved = true;
            }
            else
            {
                InGame.inGame.shared.editBrushMoved = false;
            }
            // Update the position, even if it's only a tiny movement.
            if (InGame.inGame.SnapToGrid)
            {
                Vector2 pos = newPosition + snapToGridError.XY();

                pos -= snapToGridError.XY();
                Vector2 prevPos = pos;

                pos = InGame.SnapPosition(pos);

                snapToGridError = new Vector3(pos - prevPos, 0);

                InGame.inGame.shared.editBrushPosition = pos;
            }
            else
            {
                InGame.inGame.shared.editBrushPosition = newPosition;
            }

            //
            // Adjust brush size, but only if it's not the selection brush (handled elsewhere).
            //
            Brush2DManager.Brush2D brush = Brush2DManager.GetActiveBrush();
            if (InGame.inGame.shared.editBrushSizeActive && brush.Shape != Brush2DManager.BrushShape.Magic)
            {
                if (InGame.inGame.SnapToGrid)
                {
                    if (Actions.BrushSmaller.WasPressedOrRepeat)
                    {
                        InGame.inGame.shared.editBrushRadius -= 0.5f;
                        InGame.inGame.shared.editBrushRadius = MathHelper.Max(0.5f, InGame.inGame.shared.editBrushRadius);
                    }
                    if (Actions.BrushLarger.WasPressedOrRepeat)
                    {
                        InGame.inGame.shared.editBrushRadius += 0.5f;
                    }
                }
                else
                {
                    const float brushGrowthRate = 1.0f;
                    if (Actions.BrushSmaller.IsPressed)
                    {
                        InGame.inGame.shared.editBrushRadius *= 1.0f - brushGrowthRate * secs;
                    }
                    if (Actions.BrushLarger.IsPressed)
                    {
                        InGame.inGame.shared.editBrushRadius *= 1.0f + brushGrowthRate * secs;
                    }
                }
                if (InGame.inGame.shared.editBrushRadius < Terrain.Current.CubeSize)
                {
                    InGame.inGame.shared.editBrushRadius = Terrain.Current.CubeSize;
                }
                /// A radius of about 400 adds roughly the entire terrain budget
                /// to the scene at once. We'll try for allowing the user 1/3 of the
                /// budget at a single go (1/3 of the budget is the top of the green zone)
                /// and see what kind of complaints we get.
                /// maf-taking it down further, for many benefits.
                if (InGame.inGame.shared.editBrushRadius > kMaxBrushRadius)
                {
                    InGame.inGame.shared.editBrushRadius = kMaxBrushRadius;
                }
            }

        }   // end of EditUpdateBrush()

        /// <summary>
        /// Determine whether to draw the 3D cursor in Touch Mode
        /// </summary>
        private void ToggleTouch3DCursor()
        {
            bool hideCursor = true;
            Brush2DManager.Brush2D brush = Brush2DManager.GetActiveBrush();

            if (brush.Shape == Brush2DManager.BrushShape.Magic)
            {
                hideCursor = false;
            }
            else if ((InGame.inGame.touchEditUpdateObj != null) &&
                ((EditWorldScene.CurrentToolMode == EditWorldScene.ToolMode.Water) ||
                 (EditWorldScene.CurrentToolMode == EditWorldScene.ToolMode.EraseObjects)))
            {
                hideCursor = false;
            }

            InGame.inGame.Cursor3D.Hidden = hideCursor;
        }   // end of ToggleTouch3DCursor()

        /// <summary>
        /// Find the latest TouchContact that wasn't UI focused.
        /// TODO (scoy) This is very much tied to the old UI system.  Figure
        /// out a better way to do this for the new system.
        /// </summary>
        /// <returns></returns>
        TouchContact FindNonUITouch()
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
        }   // end of FindNoUITouch()
    
        /// <summary>
        /// For the given 3d camera and mouse/touch position, limit
        /// how far away we are.  This is used for positioning the
        /// edit cursor and prevents it from running off to infinity
        /// at the horizon.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public Vector2 ApplyTerrainLimits(SmoothCamera camera, Vector2 position)
        {
            Vector3 hit = camera.FindHit(position);

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
        }   // end of ApplyTerrainLimits()


        public override void LoadContent()
        {
            toolBarDialog.LoadContent();
            toolPaletteDialog.LoadContent();

            base.LoadContent();
        }

        public override void UnloadContent()
        {
            toolBarDialog.UnloadContent();
            toolPaletteDialog.UnloadContent();

            base.UnloadContent();
        }

        #endregion


    }   // end of class EditWorldScene

}   // end of namespace KoiX.Scenes
