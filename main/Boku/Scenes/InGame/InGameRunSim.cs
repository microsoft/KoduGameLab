
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

using Boku.Audio;
using Boku.Base;
using Boku.Fx;
using Boku.SimWorld;
using Boku.SimWorld.Terra;
using Boku.Common;
using Boku.Programming;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.SimWorld.Collision;
using Boku.Analyses;
using Boku.Common.Gesture;

namespace Boku
{
    /// <summary>
    /// This just separates out the simulation part 
    /// of InGame to make it easier to find things.
    /// </summary>
    public partial class InGame : GameObject, INeedsDeviceReset
    {

        protected class RunSimUpdateObj : InGameUpdateObject
        {
            #region Members
            
            private InGame parent = null;
            private Shared shared = null;
            public List<UpdateObject> updateList = null; // Children's update list.

            private CommandMap commandMap;

            const float k_TouchExitAreaWidthPercent = 0.50f;
            const float k_TouchExitAreaHeightPercent = 0.20f;

            #endregion

            #region Public

            public RunSimUpdateObj(InGame parent, ref Shared shared)
            {
                this.parent = parent;
                this.shared = shared;

                // Just create an empty command map to act as a placeholder on
                // on the command map stack.
                commandMap = new CommandMap(@"Sim");

                updateList = new List<UpdateObject>();
            }   // end of RunSimUpdateObj c'tor

            /// <summary>
            /// RunSimUpdateObj Update()
            /// </summary>
            /// <param name="camera"></param>
            public override void Update()
            {
                base.Update();

                parent.Camera.Update();
                
                float secs = Time.WallClockFrameSeconds;

                ThoughtBalloonManager.Update(shared.camera);
                SaidStringManager.Update();
#if !NETFX_CORE
                MicrobitManager.Update();
#endif

                // Start with visible cursor.
                parent.cursor3D.Activate();
                parent.cursor3D.Rep = Cursor3D.Visual.RunSim;
                parent.cursor3D.Hidden = false;

                //
                // Determine the correct camera mode.
                //

                //
                //  The priorities used to determine the camera mode when the game is running are:
                //
                //  1)  First person.  This can be either via programming or because the user zoomed
                //      into a bot the camera was following.
                //  2)  Follow mode caused by bot(s) programmed with "follow" camera view.
                //  3)  World tweak screen fixed camera or fixed offset camera.
                //  4)  Follow mode caused by user controlled bot(s).
                //  5)  Free camera.
                //

                // Start with a fake loop to break out of.
                while (true)
                {
                    Terrain terrain = InGame.inGame.Terrain;    // Just a shortcut.

                    //
                    // Always use edit mode when the game is paused except during victory on level with one of the fixed cameras
                    // or when game is paused for a bot to speak (modal text display).
                    //
                    bool victoryActive = VictoryOverlay.ActiveGameOver || VictoryOverlay.ActiveWinner;
                    if (Time.Paused && !((terrain.FixedCamera || terrain.FixedOffsetCamera) && victoryActive))
                    {
                        CameraInfo.Mode = CameraInfo.Modes.Edit;
                        CameraInfo.CameraFocusGameActor = null;
                        break;
                    }

                    //
                    // 1) First person
                    //
                    if (CameraInfo.FirstPersonActive)
                    {
                        CameraInfo.Mode = CameraInfo.Modes.Actor;

                        // We're following a single actor so update the FollowActor camera values.
                        shared.camera.FollowCameraValid = false;

                        // Turn off 3d cursor since we don't need it.
                        parent.cursor3D.Deactivate();
                        if (parent.cursorClone != null)
                        {
                            parent.cursorClone.Deactivate();
                            parent.cursorClone = null;
                        }
                        break;
                    }

                    //
                    // 2)  Follow mode caused by bot(s) programmed with "follow" camera view.
                    //
                    if (CameraInfo.ProgrammedFollowList.Count > 0)
                    {
                        // Note that even though we looked at the count of bot programmed to
                        // have the camera follow them, for this mode we want to keep all
                        // deserving bots in camera.  So, the rest of this section will use
                        // the merged follow list instead of just the programmed follow list.

                        SetUpCameraFollowMode();

                        break;
                    }

                    //
                    // 3) World tweak fixed cameras.  Note for fixed offset we have to let
                    //    the later modes do their stuff and then override the camera.
                    //

                    if (terrain.FixedCamera)
                    {
                        CameraInfo.Mode = CameraInfo.Modes.FixedTarget;
                        CameraInfo.CameraFocusGameActor = null;

                        // Turn off 3d cursor since we don't need it.
                        parent.cursor3D.Deactivate();
                        if (parent.cursorClone != null)
                        {
                            parent.cursorClone.Deactivate();
                            parent.cursorClone = null;
                        }

                        break;
                    }
                    
                    //
                    // 4) Follow mode caused by user controlled bot(s).
                    //
                    if (CameraInfo.MergedFollowList.Count > 0)
                    {
                        SetUpCameraFollowMode();

                        break;
                    }

                    //
                    // 5) Free!
                    //
                    // Not following an actor.
                    CameraInfo.Mode = CameraInfo.Modes.Edit;
                    CameraInfo.CameraFocusGameActor = null;

                    // Turn on 3d cursor in case we previously disabled it.
                    parent.cursor3D.Activate();
                    parent.CreateCursorClone();
                    parent.cursor3D.Hidden = false;

                    // We have no camera restrictions, so keep track of what the user is doing.
                    shared.camera.PlayValid = true;
                    shared.camera.PlayCameraFrom = shared.camera.From;
                    shared.camera.PlayCameraAt = shared.camera.At;

                    shared.camera.FollowCameraValid = false;
                    
                    
                    // Final break just to be sure the loop exits.
                    break;
                }

                //
                // Now that we're done, we need to check again to see if
                // we should be in FixedOffsetMode.
                //
                if (!Time.Paused && InGame.inGame.Terrain.FixedOffsetCamera && !CameraInfo.FirstPersonActive)
                {
                    CameraInfo.Mode = CameraInfo.Modes.FixedOffset;
                }

                // Zero out any offset while running.
                float t = Math.Min(Time.GameTimeFrameSeconds, 1.0f);
                shared.camera.HeightOffset = MyMath.Lerp(shared.camera.HeightOffset, 0.0f, t);

                //
                bool inputFocus = CommandStack.Peek() == commandMap;

                // Move the camera.
                switch(CameraInfo.Mode)
                {
                    case CameraInfo.Modes.Edit :
                        MoveCameraEditMode(inputFocus, false);
                        break;
                    case CameraInfo.Modes.Actor:
                        MoveCameraActorMode(true, false);
                        break;
                    case CameraInfo.Modes.FixedTarget:
                        MoveCameraFixedTargetMode(inputFocus);
                        break;
                    case CameraInfo.Modes.FixedOffset:
                        MoveCameraFixedOffsetMode(inputFocus);
                        break;
                    case CameraInfo.Modes.MultiTarget:
                        MoveCameraMultiTargetMode(inputFocus, false);
                        break;
                }

                shared.camera.Update();

                // Update terrain.
                parent.terrain.Update(shared.camera);

                // Update the list of objects using our local camera.
                for (int i = 0; i < updateList.Count; i++)
                {
                    UpdateObject obj = (UpdateObject)updateList[i];
                    obj.Update();
                }
                parent.UpdateObjects();

                /// Pregame must update after parent.UpdateObjects, in case it
                /// decides to switchToMiniHub
                if (InGame.inGame.preGame != null)
                {
                    InGame.inGame.preGame.Update();
                }

                // Update the particle system.
                shared.particleSystemManager.Update();
                DistortionManager.Update();
                FirstPersonEffectMgr.Update();

                // This must be done after all brains are updated.
                Scoreboard.Update(shared.camera);

                VictoryOverlay.Update();

                // Force the the HelpOverlay to be correct.
                if (!Time.Paused || VictoryOverlay.Active)
                {
                    if (InGame.inGame.PreGame != null && InGame.inGame.PreGame.Active)
                    {
                        if (HelpOverlay.Depth() != 1 || HelpOverlay.Peek() != "RunSimulationPreGame")
                        {
                            HelpOverlay.Clear();
                            HelpOverlay.Push("RunSimulationPreGame");
                        }
                    }
                    else
                    {
                        if (HelpOverlay.Depth() != 1 || HelpOverlay.Peek() != "RunSimulation")
                        {
                            HelpOverlay.Clear();
                            HelpOverlay.Push("RunSimulation");
                        }
                    }
                }
                else
                {
                    // We're paused.
                    if (HelpOverlay.Depth() != 2 || HelpOverlay.Peek(1) != "RunSimulation")
                    {
                        HelpOverlay.Clear();
                        HelpOverlay.Push("RunSimulation");
                        HelpOverlay.Push("PauseGame");
                    }
                }


            }   // end of RunSimUpdateObj Update()

            #endregion

            #region Internal

            //
            // Functions for updating the camera position.  There's
            // one for each mode the camera can be in.
            //

            const float kCursorSpeed = 20.0f;   // Meters per second.  TODO (****) should this scale based on camera distance from cursor?
            const float kOrbitSpeed = 2.0f;     // Radians per second.
            const float kZoomFactor = 1.1f;     // Multiplicative factor applied per second.

            /// <summary>
            /// Reads user input to orbit and track the camera position.
            /// </summary>
            private void OrbitAndTrackCamera()
            {
                GamePadInput pad = GamePadInput.GetGamePad0();
                float tics = Time.WallClockFrameSeconds;

                // Right stick to orbit around cursor.
                // Note in first person mode, the sense of rotation is reversed.
                float dRotation = (GamePadInput.InvertCamX() ? -pad.RightStick.X : pad.RightStick.X)
                    * tics * kOrbitSpeed;
                float dPitch = GamePadInput.InvertCamY() ? -pad.RightStick.Y : pad.RightStick.Y;

                if (CameraInfo.FirstPersonActive)
                {
                    parent.Camera.DesiredRotation -= dRotation * 2.0f;
                    parent.Camera.FirstPersonDesiredPitchDelta = -dPitch * kOrbitSpeed;
                }
                else
                {
                    parent.Camera.DesiredRotation += dRotation;
                    parent.Camera.DesiredPitch -= dPitch * tics * kOrbitSpeed;
                }

                // If the user has programmed the right mouse button,
                // don't also use it for moving the camera.
                if (!InGame.inGame.ProgramUsesRightMouse)
                {
                    parent.MouseEdit.DoCamera(parent.Camera);
                }

                if (KoiLibrary.LastTouchedDeviceIsTouch)
                {
                    bool bAllowCameraMovement = true;

                    bAllowCameraMovement = !TouchVirtualController.IsVirtualControllerActedOn();

                    SwipeGestureRecognizer swipeGesture = TouchGestureManager.Get().SwipeGesture;
                    if ( bAllowCameraMovement && swipeGesture.IdentifiedFinger )
                    {
#if NETFX_CORE
                        float halfWidth = (float)BokuGame.bokuGame.Window.ClientBounds.Width * 0.5f;
                        float height = (float)BokuGame.bokuGame.Window.ClientBounds.Height;
#else
                        float halfWidth = (float)XNAControl.Instance.ClientSize.Width * 0.5f;
                        float height = (float)XNAControl.Instance.ClientSize.Height;
#endif

                        //center half of the screen width-wise
                        float minX = halfWidth - (halfWidth * k_TouchExitAreaWidthPercent);
                        float maxX = halfWidth + (halfWidth * k_TouchExitAreaWidthPercent);
                        
                        //bottom 20% height-wise
                        float minY = height - (height * k_TouchExitAreaHeightPercent);

                        Vector2 pos = swipeGesture.InitialPosition;
                        bAllowCameraMovement = !(pos.X >= minX && pos.X <= maxX && pos.Y >= minY);
                    }

                    if (bAllowCameraMovement)
                    {
                        parent.TouchEdit.DoCamera(parent.Camera);
                        parent.TouchEdit.AddPitchYawByDrag(parent.Camera);
                        parent.TouchEdit.ProcessCameraRotation(parent.Camera);
                    }
                }

                // Shoulder buttons track camera in/out.
                // Don't change zoom while in first person mode unless we got there via zooming in.
                if (!CameraInfo.FirstPersonActive || CameraInfo.FirstPersonViaZoom)
                {
                    if (Actions.ZoomOut.IsPressed)
                    {
                        parent.Camera.DesiredDistance *= 1.0f + tics * kZoomFactor;
                    }
                    if (Actions.ZoomIn.IsPressed)
                    {
                        parent.Camera.DesiredDistance *= 1.0f - tics * kZoomFactor;
                    }
                    parent.MouseEdit.DoZoom(parent.Camera);
                    parent.TouchEdit.DoZoom(parent.Camera);
                }
            }   // end of RunSimUpdateObj OrbitAndTrackCamera()

            private void MoveCameraEditMode(bool inputFocus, bool ignoreRotation)
            {
                if (inputFocus)
                {
                    if (!ignoreRotation)
                    {
                        OrbitAndTrackCamera();
                    }

                    GamePadInput pad = GamePadInput.GetGamePad0();

                    // Left stick to control cursor position.  Cursor movement is relative to view heading.
                    Vector2 position = new Vector2(shared.CursorPosition.X, shared.CursorPosition.Y);
                    Vector2 forward = new Vector2((float)Math.Cos(parent.Camera.Rotation), (float)Math.Sin(parent.Camera.Rotation));
                    Vector2 right = new Vector2(forward.Y, -forward.X);
                    position += forward * pad.LeftStick.Y * Time.WallClockFrameSeconds * kCursorSpeed;
                    position += right * pad.LeftStick.X * Time.WallClockFrameSeconds * kCursorSpeed;
                    // Numpad controls cursor position. NumLock must be on!
                    float y = KeyboardInputX.IsPressed(Keys.NumPad7) || KeyboardInputX.IsPressed(Keys.NumPad8) || KeyboardInputX.IsPressed(Keys.NumPad9) ? 1.0f : 0.0f;
                    y += KeyboardInputX.IsPressed(Keys.NumPad1) || KeyboardInputX.IsPressed(Keys.NumPad2) || KeyboardInputX.IsPressed(Keys.NumPad3) ? -1.0f : 0.0f;
                    float x = KeyboardInputX.IsPressed(Keys.NumPad3) || KeyboardInputX.IsPressed(Keys.NumPad6) || KeyboardInputX.IsPressed(Keys.NumPad9) ? 1.0f : 0.0f;
                    x += KeyboardInputX.IsPressed(Keys.NumPad1) || KeyboardInputX.IsPressed(Keys.NumPad4) || KeyboardInputX.IsPressed(Keys.NumPad7) ? -1.0f : 0.0f;
                    position += forward * y * Time.WallClockFrameSeconds * kCursorSpeed;
                    position += right * x * Time.WallClockFrameSeconds * kCursorSpeed;

                    // If the user has programmed the left mouse button,
                    // don't also use it for moving the camera.
                    // Is this what we really want?  The normal left mouse input only
                    // effects the camera when dragged.  Note that ProgramUsesLeftMouse has changed behaviour.
                    // Now it is only true if the mouse left is used in a way that would conflict with normal
                    // camera movement.  If there is no conflict then we left it go through.
                    if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse && !InGame.inGame.ProgramUsesLeftMouse)
                    {
                        //position = parent.MouseEdit.DoCursor(parent.Camera, position);
                    }

                    if (KoiLibrary.LastTouchedDeviceIsTouch)
                    {
                        position = new Vector2(parent.Camera.DesiredAt.X, parent.Camera.DesiredAt.Y);
                        position = parent.TouchEdit.DoCursor(parent.Camera, position);   
                    }

                    shared.CursorPosition = new Vector3(position, shared.CursorPosition.Z);

                    // Ensure that the 3D cursor is active.
                    if (parent.cursorClone == null)
                    {
                        parent.CreateCursorClone();
                    }
                    parent.cursor3D.Activate();
                    parent.cursor3D.DiffuseColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

                    // Assign the new position to the 3d cursor and then read it back since
                    // it will modify the height to accommodate the terrain.
                    parent.cursor3D.Position = shared.CursorPosition;
                    shared.CursorPosition = parent.cursor3D.Position;
                }

                shared.camera.DesiredAt = shared.CursorPosition;

            }   // end of RunSimUpdateObj MoveCameraEditMode()
            
            private void MoveCameraActorMode(bool inputFocus, bool ignoreRotation)
            {
                // Start with any first person actor.  If none then follow
                // the first one on the focus list.
                GameActor actor = CameraInfo.FirstPersonActor;
                if (actor == null)
                {
                    actor = CameraInfo.CameraFocusGameActor;
                }

                if (inputFocus)
                {
                    if (!ignoreRotation)
                    {
                        OrbitAndTrackCamera();
                    }

                    GamePadInput pad = GamePadInput.GetGamePad0();

                    if (actor != null)
                    {
                        shared.CursorPosition = actor.Movement.Position;

                        // If we're controlling an actor, align the camera with the actor's heading.  
                        // The user is still able to use the right stick to orbit around the actor but 
                        // as soon as the stick is let go the camera will return to being directly 
                        // behind the actor.
                        if (actor.Chassis.HasFacingDirection)
                        {
                            // Normal, non-first person mode.
                            // Use lighter weighting if the user is pushing on the stick.
                            float lerpWeighting = Time.GameTimeFrameSeconds;
                            // Use the square of the spring strength to give a little more resolution at the low end.
                            lerpWeighting *= pad.RightStick.X == 0.0f ? InGame.CameraSpringStrength * InGame.CameraSpringStrength * 10.0f : 0.5f;
                            lerpWeighting = Math.Min(lerpWeighting, 1.0f);
                            float dtheta = actor.Movement.RotationZ - parent.Camera.DesiredRotation;
                            if (dtheta > MathHelper.Pi)
                            {
                                dtheta -= MathHelper.TwoPi;
                            }
                            if (dtheta < -MathHelper.Pi)
                            {
                                dtheta += MathHelper.TwoPi;
                            }
                            parent.Camera.DesiredRotation = parent.Camera.DesiredRotation + dtheta * lerpWeighting;
                        }
                    }
                }
                else
                {
                    // We may not have input focus but we still need to move the 
                    // camera.  This may be because we in PreGame mode and want 
                    // the camera to snap to the right place before the game starts.
                    if (actor != null)
                    {
                        shared.CursorPosition = actor.Movement.Position;

                        // If we're controlling an actor, align the camera with the actor's heading.  
                        float secs = Time.WallClockFrameSeconds;
                        float t = Math.Max(1.0f, 10.0f * secs);
                        parent.Camera.DesiredRotation = MyMath.Lerp(parent.Camera.Rotation, actor.Movement.RotationZ, t);
                        parent.Camera.DesiredAt = MyMath.Lerp(parent.Camera.At, actor.Movement.Position, t);
                    }
                }

                // Are we close enough for first person mode?
                if (CameraInfo.CameraFocusGameActor != null)
                {
                    float closeEnough = Math.Max(parent.Camera.FirstPersonDistance, 1.5f * CameraInfo.CameraFocusGameActor.CollisionRadius);
                    CameraInfo.FirstPersonViaZoom = parent.Camera.Distance < closeEnough;
                }

                shared.KeepCameraAboveGround();

                // If we're controlling a bot (or in first person mode) raise our target height by
                // the chassis eye offset.  This keeps the camera out of the ground for bots like
                // the Fastbot which have their origin at the bottom.
                if (actor != null)
                {
                    // Try and smooth out the vertical for the camera.  
                    // Note that running into the ground will still cause the camera to jump upward.
                    Vector3 curAt = shared.camera.DesiredAt;
                    Vector3 target = shared.CursorPosition + new Vector3(0, 0, actor.Chassis.EyeOffset);
                    // Only lerp in Z.
                    curAt.X = target.X;
                    curAt.Y = target.Y;
                    float dt = InGame.inGame.PreGameActive ? Time.WallClockFrameSeconds : Time.GameTimeFrameSeconds;
                    dt = Math.Min(1.0f, dt * 20.0f);
                    shared.camera.DesiredAt = MyMath.Lerp(curAt, target, dt);
                }
                else
                {
                    shared.camera.DesiredAt = shared.CursorPosition;
                }

            }   // end of RunSimUpdateObj MoveCameraActorMode()

            private void MoveCameraFixedTargetMode(bool inputFocus)
            {
                InGame.inGame.RestoreFixedCamera();
            }   // end of RunSimUpdateObj MoveCameraFixedMode()

            private void MoveCameraFixedOffsetMode(bool inputFocus)
            {
                if (CameraInfo.CameraFocusGameActor != null)
                {
                    MoveCameraActorMode(inputFocus, true);
                    InGame.inGame.HideCursor();
                    InGame.inGame.Cursor3D.Hidden = true;
                }
                else if (CameraInfo.MergedFollowList.Count > 0)
                {
                    MoveCameraMultiTargetMode(inputFocus, true);
                    InGame.inGame.HideCursor();
                    InGame.inGame.Cursor3D.Hidden = true;
                }
                else
                {
                    // Not following anyone so let user control cursor.
                    MoveCameraEditMode(inputFocus, true);
                    InGame.inGame.ShowCursor();
                    InGame.inGame.Cursor3D.Hidden = false;
                    InGame.inGame.Cursor3D.DiffuseColor = new Vector4(0.5f, 0.9f, 0.8f, 0.3f);
                }
                if (Terrain.Current.FixedOffsetCamera)
                {
                    InGame.inGame.Camera.EyeOffset = Terrain.Current.FixedOffset;
                }
            }   // end of RunSimUpdateObj MoveCameraFixedOffsetMode()

            private void MoveCameraMultiTargetMode(bool inputFocus, bool ignoreRotation)
            {
                Vector3 lookAt = shared.camera.At;
                Vector3 lookFrom = shared.camera.From;

                // Find centroid of targets.
                List<GameActor> focusList = CameraInfo.MergedFollowList;
                Vector3 min = new Vector3(float.MaxValue);
                Vector3 max = new Vector3(float.MinValue);
                for (int i = 0; i < focusList.Count; i++)
                {
                    Vector3 pos = focusList[i].Movement.Position;
                    if (pos.X < min.X)
                        min.X = pos.X;
                    if (pos.X > max.X)
                        max.X = pos.X;
                    if (pos.Y < min.Y)
                        min.Y = pos.Y;
                    if (pos.Y > max.Y)
                        max.Y = pos.Y;
                    if (pos.Z < min.Z)
                        min.Z = pos.Z;
                    if (pos.Z > max.Z)
                        max.Z = pos.Z;
                }
                lookAt = (min + max) / 2.0f;

                // Now find the "long axis" of the collection of targets.  This will be used
                // as a vector orthogonal to the camera's view direction.

                // Find the axis between the 2 bots that are furthest apart.
                Vector3 axis = Vector3.UnitX;
                float dist = 0.0f;
                for (int i = 0; i < focusList.Count - 1; i++)
                {
                    for (int j = i + 1; j < focusList.Count; j++)
                    {
                        Vector3 delta = focusList[i].Movement.Position - focusList[j].Movement.Position;
                        delta.Z = 0.0f;     // Ignore vertical.
                        float length = delta.Length();
                        if (length > dist)
                        {
                            dist = length;
                            axis = delta / length;
                        }
                    }
                }

                float secs = Time.WallClockFrameSeconds;

                if (!ignoreRotation)
                {
                    GamePadInput pad = GamePadInput.GetGamePad0();

                    // Adjust pitch.
                    float dPitch = GamePadInput.InvertCamY() ? -pad.RightStick.Y : pad.RightStick.Y;
                    parent.Camera.DesiredPitch -= dPitch * Time.WallClockFrameSeconds * kOrbitSpeed;
                    
                    // Shoulder buttons track camera in/out.
                    if (pad.LeftShoulder.IsPressed)
                    {
                        parent.Camera.DesiredDistance *= 1.0f + Time.WallClockFrameSeconds * kZoomFactor;
                    }
                    if (pad.RightShoulder.IsPressed)
                    {
                        parent.Camera.DesiredDistance *= 1.0f - Time.WallClockFrameSeconds * kZoomFactor;
                    }

                    // Now that we've got the axis, figure out the desired camera rotation.
                    float axisRot = (float)Math.Acos(axis.X);
                    if (axis.Y < 0.0f)
                    {
                        axisRot = MathHelper.TwoPi - axisRot;
                    }
                    // Add pi/2 to get the orthogonal vector.
                    float desiredRot = axisRot + MathHelper.PiOver2;

                    // If we have input focus, let the user push the rotation angle around.
                    if (inputFocus)
                    {
                        float dRotation = pad.RightStick.X * kOrbitSpeed;
                        if (GamePadInput.InvertCamX())
                            dRotation = -dRotation;
                        parent.Camera.Rotation += dRotation * secs * 4.0f;
                    }

                    // Now decide if we want to use this angle for the camera or the exact opposite.
                    // To determine this we see if the delta between the angles is more than pi/2.
                    float dTheta = desiredRot - parent.Camera.Rotation;
                    // Force into +- pi range.
                    while (dTheta > MathHelper.Pi)
                    {
                        dTheta -= MathHelper.TwoPi;
                    }
                    while (dTheta < -MathHelper.Pi)
                    {
                        dTheta += MathHelper.TwoPi;
                    }
                    if (Math.Abs(dTheta) > MathHelper.PiOver2)
                    {
                        // Use opposite.
                        if (dTheta > 0.0f)
                            dTheta -= MathHelper.Pi;
                        else
                            dTheta += MathHelper.Pi;
                    }
                    desiredRot = parent.Camera.Rotation + dTheta;

                    parent.Camera.DesiredRotation = desiredRot;

                    // Based on rotation, calc new lookFrom position.
                    float dz = (float)Math.Sin(-parent.Camera.Pitch);
                    float dxy = (float)Math.Sqrt(1.0f - dz * dz);
                    Vector3 offset = new Vector3();
                    offset = new Vector3(-dxy * (float)Math.Cos(parent.Camera.Rotation), -dxy * (float)Math.Sin(parent.Camera.Rotation), dz);
                    lookFrom = lookAt + parent.Camera.Distance * offset;

                    // Now adjust camera distance.
                    bool tooClose = false;
                    for (int i = 0; i < focusList.Count; i++)
                    {
                        BoundingSphere s = focusList[i].BoundingSphere;
                        s.Center += focusList[i].Movement.LocalMatrix.Translation;

                        // Increase the radius to provide a little cushion at the edge of the screen.  
                        // For user controlled characters this cushion should be greater.
                        s.Radius *= focusList[i].Movement.UserControlled ? 5.0f : 3.0f;

                        if (shared.camera.Frustum.CullTest(s) != Frustum.CullResult.TotallyInside)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    const float kCameraZoomInSpeed = 0.1f;
                    const float kCameraZoomOutSpeed = 0.4f;
                    if (tooClose)
                    {
                        parent.Camera.DesiredDistance *= 1.0f + kCameraZoomOutSpeed * secs;
                    }
                    else
                    {
                        const float kMinCameraDist = 20.0f;
                        if (parent.Camera.Distance > kMinCameraDist)
                        {
                            parent.Camera.DesiredDistance *= 1.0f - kCameraZoomInSpeed * secs;
                        }
                    }
                }

                parent.Camera.DesiredAt = lookAt;
                InGame.inGame.Cursor3D.Position = lookAt;

            }   // end of RunSimUpdateObj MoveCameraMultiTargetMode()

            /// <summary>
            /// Sets up the camera mode to follow one or more bots.
            /// </summary>
            private void SetUpCameraFollowMode()
            {
                // Are we following a single actor?
                if (CameraInfo.MergedFollowList.Count == 1)
                {
                    CameraInfo.Mode = CameraInfo.Modes.Actor;
                    CameraInfo.CameraFocusGameActor = CameraInfo.MergedFollowList[0];

                    // We're following a single actor so update the FollowActor camera values.
                    shared.camera.FollowCameraValid = true;
                    shared.camera.FollowCameraDistance = shared.camera.Distance;
                }
                else
                {
                    CameraInfo.Mode = CameraInfo.Modes.MultiTarget;
                    CameraInfo.CameraFocusGameActor = null;

                    shared.camera.FollowCameraValid = false;
                }
                shared.camera.PlayValid = false;

                // Turn off 3d cursor since we don't need it.
                parent.cursor3D.Deactivate();
                if (parent.cursorClone != null)
                {
                    parent.cursorClone.Deactivate();
                    parent.cursorClone = null;
                }
            }   // end of SetUpCameraFollowMode()


            private object timerInstrument = null;

            // Just a safeguard against being activated/deactivated multiple times.
            private bool active = false;
            public bool Active
            {
                get { return active; }
            }

            public override void Activate()
            {
                if(!active)
                {
                    active = true;

                    CommandStack.Push(commandMap);
                    HelpOverlay.Push("RunSimulation");

                    // Wake everything up.
                    parent.ActivateAllGameThings();

                    // This is commented out since it was removing Creatables from the level
                    // when it shouldn't.  In particular if you restart Kodu, find a level in
                    // the LoadLevelMenu, and choose Edit, it would remove all the creatables.
                    // Running and then editing would restore them so it's not peristing the 
                    // removal.
                    // Normally I'd just delete the line but if this change starts causing issues
                    // it might help to know this was here.  If it's now 2019 or later you can 
                    // probably feel free to remove this.  :-)
                    //parent.RemoveCreatablesFromScene();

                    timerInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.InGameRunSim);

#if !NETFX_CORE
                    // Refresh the list of attached microbits.
                    {
                        System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(MicrobitManager.RefreshWorker));
                        t.Start();
                    }
#endif

                    // Be sure all Auth UI is hidden.
                    AuthUI.HideAllDialogs();

                    base.Activate();
                }

                if (Program2.CmdLine.Exists("analytics"))
                {
#if !NETFX_CORE
                    Console.WriteLine("Begin Analytics");
#endif
                    ObjectAnalysis oa = new ObjectAnalysis();
                    oa.beginAnalysis(Program2.StartupWorldFilename.ToString());


                  //  GamePadInput.stopActiveInputTimer();

                    //deactivate the menu on exit to stop the timer
                    Deactivate();

                    // Wave bye, bye.
#if NETFX_CORE
                    Windows.UI.Xaml.Application.Current.Exit();
#else
                BokuGame.bokuGame.Exit();
#endif
                }
            }   // end of RunSimUpdateObj Activate()

            public override void Deactivate()
            {
                if (active)
                {
                    active = false;

                    if (parent.cursorClone != null)
                    {
                        parent.cursorClone.Deactivate();
                        parent.cursorClone = null;
                    }
                    // Deactivate the PreGame if any.
                    if (parent.PreGame != null)
                    {
                        parent.PreGame.Active = false;
                    }

                    CommandStack.Pop(commandMap);
                    HelpOverlay.Pop();

                    Instrumentation.StopTimer(timerInstrument);

#if !NETFX_CORE
                    MicrobitManager.ReleaseDevices();
#endif

                    base.Deactivate();

          //          ObjectAnalysis oa = new ObjectAnalysis();
                    //oa.beginAnalysis("out.txt");


                }
            }   // end of RunSimUpdateObj Deactivate()

            #endregion

        }   // end of class RunSimUpdateObj


    }   // end of class InGame

}   // end of namespace Boku

