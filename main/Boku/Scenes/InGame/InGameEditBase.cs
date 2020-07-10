// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Input;
using KoiX.Scenes;

using Boku.Audio;
using Boku.Base;
using Boku.Common;
using Boku.Common.Xml;
using Boku.Fx;
using Boku.Input;
using Boku.Programming;
using Boku.SimWorld;
using Boku.SimWorld.Path;
using Boku.SimWorld.Terra;
using Boku.UI;
using Boku.UI2D;

namespace Boku
{
    public partial class InGame : GameObject, INeedsDeviceReset
    {
        public const float kMaxBrushRadius = 150.0f;

        /// <summary>
        /// Base class for functions common to the editing InGameUpdateObjs
        /// </summary>
        public class BaseEditUpdateObj : InGameUpdateObject
        {
            protected InGame parent = null;
            protected Shared shared = null;
            public List<UpdateObject> updateList = null; // Children's update list.

            protected bool active = false;

            protected static CommandMap commandMap;

            // private bool paused = false;    // Was the game paused when we activated?

            private int actorTabIndex = 0;      // When the user hits Tab or ShiftTab we cycle to the next/prev actor in the list.
                                                // If this index is invalid (we deleted enough actors, just loop back to 0).

            private const float snapCaptureRadius = 1.0f;   // If GameThing is within this range, snap cursor to it.
            private const float snapReleaseRadius = 1.5f;   // If cursor is more than this far away from the last thing
                                                            // we snapped to, reset snapGameThing to null.

            public static float SnapCaptureRadius
            {
                get { return snapCaptureRadius; }
            }
            public static float SnapCaptureRadiusSq
            {
                get { return SnapCaptureRadius * SnapCaptureRadius; }
            }
            public static float SnapReleaseRadius
            {
                get { return snapReleaseRadius; }
            }
            public static float SnapReleaseRadiusSq
            {
                get { return SnapReleaseRadius * SnapReleaseRadius; }
            }

            // c'tor
            public BaseEditUpdateObj(InGame parent, ref Shared shared)
            {
                this.parent = parent;
                this.shared = shared;

                // Just create an empty command map to act as a placeholder on
                // on the command map stack.
                if (commandMap == null)
                {
                    commandMap = new CommandMap("InGameEditBase");
                }

            }   // end of BaseEditUpdateObj c'tor

            public override void Update()
            {
                if (AuthUI.IsModalActive)
                {
                    return;
                }

                shared.tooManyLightsMessage.Update();

                base.Update();

                //parent.Camera.Update();
            }

            public void UpdateCamera()
            {
                UpdateCamera(false);
            }   // end of UpdateCamera()

            /// <summary>
            /// Common camera controls for edit modes.
            /// </summary>
            /// <param name="preventZoom">Lock the zoom.  This is kind of a hack used 
            /// by the tool palette since that palette uses the shoulder buttons to 
            /// cycle through it.  Without locking the zoom we'd zoom in and out 
            /// as we're moving though the tool options.</param>
            public void UpdateCamera(bool preventZoom)
            {
                float secs = Time.WallClockFrameSeconds;

                // If we were in first person mode, reset things as needed.
                if (CameraInfo.FirstPersonActive)
                {
                    CameraInfo.FirstPersonActor.SetFirstPerson(false);
                    CameraInfo.ResetAllLists();
                    CameraInfo.Mode = CameraInfo.Modes.Edit;
                    InGame.inGame.Camera.FollowCameraDistance = 10.0f;
                }

                // Check if we have input focus.  Don't do any input
                // related update if we don't.
                if (CommandStack.Peek() == commandMap)
                {
                    // Grab the current state of the gamepad.
                    GamePadInput pad = GamePadInput.GetGamePad0();

                    // From all edit modes, <back> should bring up the tool menu.
                    if (Actions.ToolMenu.WasPressed)
                    {
                        Actions.ToolMenu.ClearAllWasPressedState();

                        Foley.PlayBack();
                        parent.CurrentUpdateMode = UpdateMode.ToolMenu;

                        return;
                    }

                    // From all edit modes, <start> should to to the mini-hub.
                    if (Actions.MiniHub.WasPressed)
                    {
                        Actions.MiniHub.ClearAllWasPressedState();

                        Foley.PlayPressStart();
                        parent.SwitchToMiniHub();

                        return;
                    }

                    // From all edit modes the B button should activate the ToolMenu.
                    // Edit object handles the B button itself since it may be in Waypoint mode.
                    if (Actions.AltToolMenu.WasPressed && parent.CurrentUpdateMode != UpdateMode.EditObject)
                    {
                        Actions.AltToolMenu.ClearAllWasPressedState();
                        parent.CurrentUpdateMode = UpdateMode.ToolMenu;

                        return;
                    }

                    // If the ToolMenu is active and it's modal, don't allow the camera to move.
                    if (InGame.inGame.toolMenuUpdateObj.active && XmlOptionsData.ModalToolMenu)
                    {
                        return;
                    }

                    //
                    // Use common camera controls for all edit modes.
                    //

                    const float cursorSpeed = 20.0f;    // Meters per second.
                    const float orbitSpeed = 2.0f;      // Radians per second.
                    const float zoomFactor = 1.1f;

                    // Right stick to orbit around cursor.
                    float drot = GamePadInput.InvertCamX() ? -pad.RightStick.X : pad.RightStick.X;
                    float dpitch = GamePadInput.InvertCamY() ? -pad.RightStick.Y : pad.RightStick.Y;
                    parent.Camera.DesiredRotation += drot * Time.WallClockFrameSeconds * orbitSpeed;
                    parent.Camera.DesiredPitch -= dpitch * Time.WallClockFrameSeconds * orbitSpeed;

                    // Left/right arrow keys also orbit but not if the tool menu or a picker is up.
                    //if (inGame.CurrentUpdateMode != UpdateMode.ToolMenu && !HelpOverlay.Peek().EndsWith("Picker") )
                    //{
                    //    if (KeyboardInputX.IsPressed(Keys.Left))
                    //    {
                    //        parent.Camera.DesiredRotation -= 1.0f * Time.WallClockFrameSeconds * orbitSpeed;
                    //    }
                    //    else if (KeyboardInputX.IsPressed(Keys.Right))
                    //    {
                    //        parent.Camera.DesiredRotation += 1.0f * Time.WallClockFrameSeconds * orbitSpeed;
                    //    }
                    //}

                    // Shoulder buttons track camera in/out.
                    if (!preventZoom)
                    {
                        if (Actions.ZoomOut.IsPressed)
                        {
                            parent.Camera.DesiredDistance *= 1.0f + Time.WallClockFrameSeconds * zoomFactor;
                        }
                        if (Actions.ZoomIn.IsPressed)
                        {
                            float desiredDistance = parent.Camera.DesiredDistance * (1.0f - Time.WallClockFrameSeconds * zoomFactor);
                            // If not in RunSim mode, don't allow the camera to get closer than 4 meters.
                            if (InGame.inGame.CurrentUpdateMode != UpdateMode.RunSim)
                            {
                                desiredDistance = Math.Max(4.0f, desiredDistance);
                            }
                            parent.Camera.DesiredDistance = desiredDistance;
                        }
                        //parent.MouseEdit.DoZoom(parent.Camera);
                    }

                    if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
                    {
                        parent.MouseEdit.DoCamera(parent.Camera);
                    }

                    // Left stick to control cursor position.  
                    // Cursor movement is relative to view heading.
                    // Cursor speed grows with view distance.
                    Vector2 position = new Vector2(shared.CursorPosition.X, shared.CursorPosition.Y);
                    Vector2 forward = new Vector2((float)Math.Cos(parent.Camera.Rotation), (float)Math.Sin(parent.Camera.Rotation));
                    Vector2 right = new Vector2(forward.Y, -forward.X);
                    float speedFactor = (parent.Camera.Distance - 10.0f) / 50.0f;       // At 10 meters out start growing the speedFactor.
                    speedFactor = MathHelper.Clamp(speedFactor, 1.0f, 3.0f);
                    position += forward * pad.LeftStick.Y * Time.WallClockFrameSeconds * cursorSpeed * speedFactor;
                    position += right * pad.LeftStick.X * Time.WallClockFrameSeconds * cursorSpeed * speedFactor;

                    // Numpad controls cursor position. NumLock must be on!
                    float y = KeyboardInputX.IsPressed(Keys.NumPad7) || KeyboardInputX.IsPressed(Keys.NumPad8) || KeyboardInputX.IsPressed(Keys.NumPad9) ? 1.0f : 0.0f;
                    y += KeyboardInputX.IsPressed(Keys.NumPad1) || KeyboardInputX.IsPressed(Keys.NumPad2) || KeyboardInputX.IsPressed(Keys.NumPad3) ? -1.0f : 0.0f;
                    float x = KeyboardInputX.IsPressed(Keys.NumPad3) || KeyboardInputX.IsPressed(Keys.NumPad6) || KeyboardInputX.IsPressed(Keys.NumPad9) ? 1.0f : 0.0f;
                    x += KeyboardInputX.IsPressed(Keys.NumPad1) || KeyboardInputX.IsPressed(Keys.NumPad4) || KeyboardInputX.IsPressed(Keys.NumPad7) ? -1.0f : 0.0f;
                    position += forward * y * Time.WallClockFrameSeconds * cursorSpeed * speedFactor;
                    position += right * x * Time.WallClockFrameSeconds * cursorSpeed * speedFactor;

                    // Allow LeftStickClick RightStickClick to cycle though actors.
                    if (inGame.gameThingList.Count > 0)
                    {
                        // Move to next actor
                        if (Actions.NextActor.WasPressed)
                        {
                            Actions.NextActor.ClearAllWasPressedState();

                            // If we have an actor in focus, find index.
                            GameActor focusActor = InGame.inGame.EditFocusObject;
                            if (focusActor != null)
                            {
                                for (int i = 0; i < inGame.gameThingList.Count; i++)
                                {
                                    if (focusActor == inGame.gameThingList[i])
                                    {
                                        actorTabIndex = i;
                                        break;
                                    }
                                }
                            }

                            // Increment index.
                            actorTabIndex = (actorTabIndex + 1) % inGame.gameThingList.Count;

                            Vector3 actorPos = inGame.gameThingList[actorTabIndex].Movement.Position;
                            Vector2 delta = new Vector2(actorPos.X - position.X, actorPos.Y - position.Y);
                            position += delta;
                        }

                        // Move to prev actor
                        if (Actions.PrevActor.WasPressed)
                        {
                            Actions.PrevActor.ClearAllWasPressedState();

                            // If we have an actor in focus, find index.
                            GameActor focusActor = InGame.inGame.EditFocusObject;
                            if (focusActor != null)
                            {
                                for (int i = 0; i < inGame.gameThingList.Count; i++)
                                {
                                    if (focusActor == inGame.gameThingList[i])
                                    {
                                        actorTabIndex = i;
                                        break;
                                    }
                                }
                            }

                            // Decrement index.
                            actorTabIndex = (actorTabIndex + inGame.gameThingList.Count - 1) % inGame.gameThingList.Count;

                            Vector3 actorPos = inGame.gameThingList[actorTabIndex].Movement.Position;
                            Vector2 delta = new Vector2(actorPos.X - position.X, actorPos.Y - position.Y);
                            position += delta;
                        }
                    }

                    if (!shared.editWayPoint.Dragging)
                    {
                        // TODO (mouse)  Why was this here?
                        //position = parent.MouseEdit.DoCursor(parent.Camera, position);
                    }
                    position = shared.editWayPoint.DoCursor(position);

                    // Keep cursor within 50 units of existing terrain.
                    float maxBrush = InGame.kMaxBrushRadius;
                    Vector2 min = new Vector2(InGame.inGame.totalBounds.Min.X, InGame.inGame.totalBounds.Min.Y) - new Vector2(maxBrush, maxBrush);
                    Vector2 max = new Vector2(InGame.inGame.totalBounds.Max.X, InGame.inGame.totalBounds.Max.Y) + new Vector2(maxBrush, maxBrush);

                    position.X = MathHelper.Clamp(position.X, min.X, max.X);
                    position.Y = MathHelper.Clamp(position.Y, min.Y, max.Y);

                    shared.CursorPosition = new Vector3(position, shared.CursorPosition.Z);

                }   // end if we have input focus.

                // Keep the camera from going into the ground.
                shared.KeepCameraAboveGround();

                // Update camera based on new position/orientation.
                parent.Camera.DesiredAt = shared.CursorPosition;

                // Finally, call Update on the camera to let all the changes filter through.
                parent.Camera.Update();

            }   // end of BaseEditUpdateObj UpdateCamera()

            /// <summary>
            /// Common update for rest of world.
            /// </summary>
            public void UpdateWorld()
            {
                // Update terrain.
                parent.terrain.Update(shared.camera);

                // Update the list of objects using our local camera.
                for (int i = 0; i < updateList.Count; i++)
                {
                    UpdateObject obj = (UpdateObject)updateList[i];
                    obj.Update();
                }
                parent.UpdateObjects();

                // Update the particle system.
                shared.particleSystemManager.Update();
                DistortionManager.Update();
                FirstPersonEffectMgr.Update();

                // Update the waypoints.
                WayPoint.UpdateAllPaths(shared.camera);

                // Toggle SnapToGrid?
                if (KeyboardInputX.WasPressed(Keys.F3))
                {
                    InGame.inGame.SnapToGrid = !InGame.inGame.SnapToGrid;
                }

            }   // end of BaseEditUpdateObj UpdateWorld()

            public void UpdateObjects()
            {
                // Update the list of objects using our local camera.
                for (int i = 0; i < updateList.Count; i++)
                {
                    UpdateObject obj = (UpdateObject)updateList[i];
                    obj.Update();
                }
                parent.UpdateObjects();
            }

            /// <summary>
            /// Error accumulated due to snap to grid.
            /// </summary>
            private Vector2 brushPositionError = Vector2.Zero;

            /// <summary>
            /// Determine whether to draw the 3D cursor in Touch Mode
            /// </summary>
            private void ToggleTouch3DCursor()
            {
                bool hideCursor = true;
                Brush2DManager.Brush2D brush = Brush2DManager.GetActiveBrush();

                if(brush.Shape == Brush2DManager.BrushShape.Magic)
                {
                    hideCursor = false;
                }
                else if ((InGame.inGame.touchEditUpdateObj != null) &&
                    ((EditWorldScene.CurrentToolMode == EditWorldScene.ToolMode.Water) ||
                     (EditWorldScene.CurrentToolMode == EditWorldScene.ToolMode.EraseObjects)))
                {
                    hideCursor = false;
                }

                parent.Cursor3D.Hidden = hideCursor;
            }

            /// <summary>
            /// Update the edit brush for texture and heightmap editing.
            /// </summary>
            public void UpdateEditBrush()
            {
                float secs = Time.WallClockFrameSeconds;

                // Grab the current state of the gamepad.
                GamePadInput pad = GamePadInput.GetGamePad0();

                if (InGame.inGame.CurrentUpdateMode == UpdateMode.MouseEdit
                    && EditWorldScene.CurrentToolMode != EditWorldScene.ToolMode.EditObject
                    && EditWorldScene.CurrentToolMode != EditWorldScene.ToolMode.Paths)
                {
                    ColorPalette.Active = false;
                }

                parent.cursor3D.Position = shared.CursorPosition;
                shared.CursorPosition = parent.cursor3D.Position;

                Vector2 newPosition = new Vector2(shared.CursorPosition.X, shared.CursorPosition.Y);

                if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
                {
                    newPosition = parent.MouseEdit.DoTerrain(parent.Camera);
                    parent.Cursor3D.AltPosition = new Vector3(newPosition, 0);
                }
                else if (KoiLibrary.LastTouchedDeviceIsTouch)
                {

                    newPosition = parent.TouchEdit.DoTerrain(parent.Camera);

                    parent.Cursor3D.AltPosition = new Vector3(newPosition, 0);

                    ToggleTouch3DCursor();

                    //every frame track whether or not it's valid to apply the current terrain tool (paint, etc.)
                    //this ensures we don't apply the tool just by selecting it
                    if (TouchInput.TouchCount == 1 && InGame.inGame.TouchEdit.HasNonUITouch() && !TouchInput.WasMultiTouch)
                    {
                        float touchAge = (float)(Time.WallClockTotalSeconds - TouchInput.Touches[0].startTime);

                        //edit brush only allowed if touch has been around for at least 1/4 of a second
                        shared.editBrushAllowedForTouch = touchAge >= 0.25f;
                    }
                    else
                    {
                        shared.editBrushAllowedForTouch = false;
                    }
                }
                else
                {
                    parent.Cursor3D.AltPosition = parent.Cursor3D.Position;
                }

                const float minEditBrushMoveSq = 0.25f * 0.25f;
                if (Vector2.DistanceSquared(newPosition, shared.editBrushPosition) >= minEditBrushMoveSq)
                {
                    shared.editBrushMoved = true;
                }
                else
                {
                    shared.editBrushMoved = false;
                }
                // Update the position, even if it's only a tiny movement.
                if (InGame.inGame.SnapToGrid)
                {
                    Vector2 pos = newPosition + brushPositionError;

                    pos -= brushPositionError;
                    Vector2 prevPos = pos;

                    pos = InGame.SnapPosition(pos);

                    brushPositionError = pos - prevPos;

                    shared.editBrushPosition = pos;
                }
                else
                {
                    shared.editBrushPosition = newPosition;
                }

                //
                // Adjust brush size, but only if it's not the selection brush (handled elsewhere).
                //
                Brush2DManager.Brush2D brush = Brush2DManager.GetActiveBrush();
                if (shared.editBrushSizeActive && brush.Shape != Brush2DManager.BrushShape.Magic)
                {
                    if (InGame.inGame.SnapToGrid)
                    {
                        if (Actions.BrushSmaller.WasPressedOrRepeat)
                        {
                            shared.editBrushRadius -= 0.5f;
                            shared.editBrushRadius = MathHelper.Max(0.5f, shared.editBrushRadius);
                        }
                        if (Actions.BrushLarger.WasPressedOrRepeat)
                        {
                            shared.editBrushRadius += 0.5f;
                        }
                    }
                    else
                    {
                        const float brushGrowthRate = 1.0f;
                        if (Actions.BrushSmaller.IsPressed)
                        {
                            shared.editBrushRadius *= 1.0f - brushGrowthRate * secs;
                        }
                        if (Actions.BrushLarger.IsPressed)
                        {
                            shared.editBrushRadius *= 1.0f + brushGrowthRate * secs;
                        }
                    }
                    if (shared.editBrushRadius < Terrain.Current.CubeSize)
                    {
                        shared.editBrushRadius = Terrain.Current.CubeSize;
                    }
                    /// A radius of about 400 adds roughly the entire terrain budget
                    /// to the scene at once. We'll try for allowing the user 1/3 of the
                    /// budget at a single go (1/3 of the budget is the top of the green zone)
                    /// and see what kind of complaints we get.
                    /// ***-taking it down further, for many benefits.
                    if (shared.editBrushRadius > kMaxBrushRadius)
                    {
                        shared.editBrushRadius = kMaxBrushRadius;
                    }
                }

            }   // end of BaseEditUpdateObj UpdateEditBrush()




            public override void Activate()
            {
                if (!active)
                {
                    CommandStack.Push(commandMap);

                    if (parent.Camera.Distance < 1.5f)
                    {
                        parent.Camera.Distance = 20.0f;
                        CameraInfo.FirstPersonViaZoom = false;
                    }

                    // Pause all GameThings.
                    parent.PauseAllGameThings();

                    active = true;

                    base.Activate();
                }
            }   // end of BaseEditUpdateObj Activate()

            public override void Deactivate()
            {
                if (active)
                {
                    CommandStack.Pop(commandMap);

                    active = false;

                    base.Deactivate();
                }
            }   // end of BaseEditUpdateObj Deactivate()

        }   // end of class BaseEditUpdateObj

    }   // end of class InGame

}   // end of namespace Boku


