// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.SimWorld.Terra;      // Used for limiting camera to skybox.

namespace Boku.Common
{
    /// <summary>
    /// A wrapper class for the normal camera that smooths its motion 
    /// and does collision detection with the terrain and game things.
    /// </summary>
    public class SmoothCamera : SimCamera
    {
        #region Members

        private Vector3 desiredAt = Vector3.Zero;   // Values which represent where we

        private Vector3 eyeOffset = new Vector3(-10.0f, 0.0f, 4.0f);
        private Vector3 desiredEyeOffset = new Vector3(-10.0f, 0.0f, 4.0f);

        private float rotation = 0.0f;              // Values derived from the current At and EyeOffset values.
        private float pitch = 0.0f;                 // Should not be directly settable.
        private float distance = 10.0f;

        private float desiredRotation = 0.0f;       // Values dereived from the desired At and EyeOffset values.
        private float desiredPitch = 0.0f;          // Should not be directly settable.
        private float desiredDistance = 10.0f;

        private float firstPersonDistance = 1.1f;   // Distance at which 1st person mode kicks in.
        private float firstPersonPitch = 0.0f;      // Pitch to use in first person mode.
        private float firstPersonDesiredPitch = 0.0f;   // Pitch we want for first person mode.
        private float firstPersonPitchSpring = 1.0f;    // Spring strength pushing pitch toward zero.

        private bool playValid = false;                                 // Are the following values valid?  These are only used
        private Vector3 playCameraAt = new Vector3(10, 10, 0);          // when not following an actor, with no fixed camera
        private Vector3 playCameraFrom = new Vector3(-10, -10, 10);     // and no staring position for the camera.

        private bool followCameraValid = false;     // Is the following value valid?
        private float followCameraDistance = 10.0f; // Stating distance to use when following a single actor.

        private float minDistance = 1.0f;
        private float maxPitch = 1.5f;              // Don't allow the pitch to go vertical.

        private bool bumped = false;                // If we're in bumped state, don't change Z unless X or Y change.                     
        private bool pitchChanged = false;
        #endregion

        #region Accessors

        /// <summary>
        /// This flag indicates whether or not the camera is in first person mode.
        /// In this mode the viewpoint is treated as being exactly at the look-at
        /// target and the pitch is locked at 0.
        /// </summary>

        /// <summary>
        /// Distance at which first person mode is activated when following a bot.
        /// </summary>
        public float FirstPersonDistance
        {
            get { return firstPersonDistance; }
        }

        /// <summary>
        /// The eye position of the camera.  This should only be used to
        /// set the position if you want the camera to jump.  If you want
        /// the camera to transition smoothly, set the Desired* values.
        /// </summary>
        public Vector3 EyeOffset
        {
            get { return eyeOffset; }
            set 
            {
                Debug.Assert(!float.IsNaN(value.X));
                eyeOffset = value;
                desiredEyeOffset = value;
                UpdateValues();
            } 
        }

        /// <summary>
        /// The viewing target of the camera.  This should only be used to
        /// set the position if you want the camera to jump.  If you want
        /// the camera to transition smoothly, set the Desired* values.
        /// </summary>
        public new Vector3 At
        {
            get { return base.At; }
            set 
            { 
                base.At = value; 
                UpdateValues();
            }
        }

        /// <summary>
        /// The viewing target of the camera with smooth transitioning.
        /// </summary>
        public Vector3 DesiredAt
        {
            get { return desiredAt; }
            set 
            {
                if (desiredAt != value)
                {
                    desiredAt = value; 
                }
            }
        }

        /// <summary>
        /// The offset of the camera from the At position.
        /// </summary>
        public Vector3 DesiredEyeOffset
        {
            get { return desiredEyeOffset; }
            set 
            {
                Debug.Assert(!float.IsNaN(value.X));
                desiredEyeOffset = value;
                UpdateValues();
            }
        }

        /// <summary>
        /// The camera's rotation around the focus point.
        /// The camera will blend smoothly to this setting.
        /// </summary>
        public float DesiredRotation
        {
            get { return desiredRotation; }
            set 
            {
                if (desiredRotation != value)
                {
                    // Modify desiredEyeOffset to get the new rotation value.
                    float dTheta = value - desiredRotation;
                    Matrix mat = Matrix.CreateRotationZ(dTheta);
                    desiredEyeOffset = Vector3.TransformNormal(desiredEyeOffset, mat);
                    desiredRotation = value;
                }
            }
        }

        /// <summary>
        /// The camera's pitch around the focus point.
        /// The camera will blend smoothly to this setting.
        /// </summary>
        public float DesiredPitch
        {
            get { return desiredPitch; }
            set 
            {
                value = MathHelper.Clamp(value, -maxPitch, maxPitch);

                if (desiredPitch != value)
                {
                    // Modify desiredEyeOffset to get the new pitch value.
                    float dTheta = value - desiredPitch;
                    // Don't allow the pitch to increase if bumped this frame
                    if (!(bumped && dTheta > 0.0f))
                    {
                        Matrix mat = Matrix.CreateRotationZ(-desiredRotation) * Matrix.CreateRotationY(-dTheta) * Matrix.CreateRotationZ(desiredRotation);
                        desiredEyeOffset = Vector3.TransformNormal(desiredEyeOffset, mat);
                        desiredPitch = value;
                        UpdateValues();
                        pitchChanged = true;
                    }
                }
            }
        }

        /// <summary>
        /// The camera's distance from the focus point.
        /// The camera will blend smoothly to this setting.
        /// </summary>
        public float DesiredDistance
        {
            get { return desiredDistance; }
            set 
            {
                Debug.Assert(!float.IsNaN(value));
                value = Math.Max(value, minDistance);
                // Limit zoom out ot 90% of distance to far clip plane.
                value = Math.Min(value, FarClip * 0.9f);

                desiredEyeOffset *= value / desiredDistance;
                desiredDistance = value;
            }
        }

        public float BumpedZ
        {
            set 
            {
                Vector3 cur = eyeOffset;
                cur.Z += value;
                eyeOffset = cur;
                bumped = true;

                // This undoes the last pitch change and sets the desired
                // pitch at exactly the value needed to avoid bouncing.
                if (pitchChanged)
                {
                    DesiredPitch = pitch;
                }
            }
        }


        /// <summary>
        /// The camera's rotation around the focus point.
        /// </summary>
        public float Rotation
        {
            get { return rotation; }
            set 
            {
                // Modify EyeOffset to get the new rotation value.
                float dTheta = value - rotation;
                Matrix mat = Matrix.CreateRotationZ(dTheta);
                eyeOffset = Vector3.TransformNormal(eyeOffset, mat);
                rotation = value;
            }
        }

        /// <summary>
        /// The camera's pitch around the focus point.
        /// </summary>
        public float Pitch
        {
            get { return pitch; }
            set 
            {
                value = MathHelper.Clamp(value, -maxPitch, maxPitch);

                // Modify EyeOffset to get the new pitch value.
                float dTheta = value - pitch;
                Matrix mat = Matrix.CreateRotationZ(-rotation) * Matrix.CreateRotationY(-dTheta) * Matrix.CreateRotationZ(rotation);
                eyeOffset = Vector3.TransformNormal(eyeOffset, mat);
                pitch = value;
            }
        }

        /// <summary>
        /// The camera's distance from the focus point.
        /// </summary>
        public float Distance
        {
            get { return distance; }
            set 
            {
                float dist = eyeOffset.Length();
                if (dist != value)
                {
                    eyeOffset *= value / dist;
                    UpdateValues();
                }
            }
        }

        /// <summary>
        /// Is FollowCameraDistance valid?
        /// Only used when in play mode when following a single actor with no
        /// other camera restrictions, ie no fixed camera.
        /// </summary>
        public bool FollowCameraValid
        {
            get { return followCameraValid; }
            set { followCameraValid = value; }
        }

        /// <summary>
        /// Starting camera distance when following a single actor.
        /// </summary>
        public float FollowCameraDistance
        {
            get { return followCameraDistance; }
            set { followCameraDistance = value; }
        }


        /// <summary>
        /// Are the PlayCameraFrom and PlayCameraAt values valid?
        /// Only used when in play mode with no camera restrictions, ie not
        /// following any actors, no fixed camera, no camera starting position.
        /// </summary>
        public bool PlayValid
        {
            get { return playValid; }
            set { playValid = value; }
        }

        /// <summary>
        /// Starting camera position to be used when in play mode with no camera restrictions.
        /// </summary>
        public Vector3 PlayCameraFrom
        {
            get { return playCameraFrom; }
            set { playCameraFrom = value; }
        }

        /// <summary>
        /// Starting view target to be used when in play mode with no camera restrictions.
        /// </summary>
        public Vector3 PlayCameraAt
        {
            get { return playCameraAt; }
            set { playCameraAt = value; }
        }

        /// <summary>
        /// Change in pitch to use in first person mode.  Springs back to 0.
        /// The expected input is in the range [-1, 1]
        /// </summary>
        public float FirstPersonDesiredPitchDelta
        {
            set
            {
                float secs = Time.WallClockFrameSeconds;
                firstPersonDesiredPitch = MathHelper.Clamp(firstPersonDesiredPitch + value * secs, -maxPitch, maxPitch);
                // Spring toward zero if stick is centered.
                firstPersonPitchSpring = 1.0f - Math.Abs(value) / 2.0f;
            }
        }

        #endregion

        #region Public
        
        /// <summary>
        /// c'tor
        /// </summary>
        public SmoothCamera()
        {
        }   // end of SmoothCamera c'tor

        public void RotateAboutArbitraryPoint(Vector3 position, float angleRads)
        {
            Matrix toTargetSpace = Matrix.CreateTranslation(-position) * 
                                    Matrix.CreateRotationZ(angleRads) *
                                    Matrix.CreateTranslation(position);

            desiredAt = Vector3.Transform(desiredAt, toTargetSpace);
            desiredEyeOffset = Vector3.TransformNormal(desiredEyeOffset, toTargetSpace);
        }

        public override void Update()
        {
            bumped = false;

            // Calc blend value for lerping.
            // Use wall clock time so this works even if the game is paused.
            float secs = Time.WallClockFrameSeconds;

            float blend = 5.0f * Time.WallClockFrameSeconds;

            if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
            {
                //faster interpolation in touch mode
                blend = 10.0f * Time.WallClockFrameSeconds;
            }

            // Soften up the camera movement if tracking multiple targets.
            if (CameraInfo.Mode == CameraInfo.Modes.MultiTarget)
            {
                blend *= 0.2f;
            }

            blend = Math.Min(blend, 1.0f);

            // Blend the eyeOffset toward the desired value.
            eyeOffset = MyMath.Lerp(eyeOffset, desiredEyeOffset, blend);
            TranslateOffset(blend);

            // Check if the camera needs to be moved to avoid the terrain.
            InGame.inGame.shared.KeepCameraAboveGround();

            // If we hit the terrain, update the From and At values again.
            if (bumped)
                TranslateOffset(blend);

            // Update real and desired angles for use this frame.
            UpdateValues();

            base.Update();

            pitchChanged = false;
        }   // end of SmoothCamera Update()

        #endregion

        #region Internal

        /// <summary>
        /// Translates the current eyeOffset to the proper From/At values.
        /// </summary>
        private void TranslateOffset(float blend)
        {
            if (CameraInfo.FirstPersonActive)
            {
                // In first person mode we want to put the camera in the desiredAt
                // position and set its facing direction based on the rotation.
                // Use a lerp here to smooth the transition from follow
                // mode to first person mode.
                float t = Math.Min(10.0f * Time.GameTimeFrameSeconds, 1.0f);
                base.From = MyMath.Lerp(base.From, desiredAt, t);
                float sinRot = (float)Math.Sin(rotation);
                float cosRot = (float)Math.Cos(rotation);
                float sinPitch = (float)Math.Sin(firstPersonPitch);
                float cosPitch = (float)Math.Cos(firstPersonPitch);
                base.At = base.From + new Vector3(cosRot * cosPitch, sinRot * cosPitch, sinPitch);

                t *= 0.8f * InGame.CameraSpringStrength;
                firstPersonPitch = MyMath.Lerp(firstPersonPitch, firstPersonDesiredPitch, t);
                firstPersonDesiredPitch = MyMath.Lerp(firstPersonDesiredPitch, 0.0f, firstPersonPitchSpring * t);
            }
            else
            {
                base.At = MyMath.Lerp(base.At, desiredAt, blend);
                base.From = base.At + eyeOffset;
            }

        }   // end of TranslateOffset()

        /// <summary>
        /// Update the rotation, pitch and distance values based on the eyeOffset.
        /// </summary>
        private void UpdateValues()
        {
            distance = eyeOffset.Length();
            Vector3 offsetNoZ = eyeOffset;
            offsetNoZ.Z = 0.0f;
            float len = offsetNoZ.Length();
            rotation = MyMath.ZRotationFromDirection(-offsetNoZ);
            pitch = (float)Math.Atan2(-eyeOffset.Z, len);

            desiredDistance = desiredEyeOffset.Length();
            offsetNoZ = desiredEyeOffset;
            offsetNoZ.Z = 0.0f;
            len = offsetNoZ.Length();
            desiredRotation = MyMath.ZRotationFromDirection(-offsetNoZ);
            desiredPitch = (float)Math.Atan2(-desiredEyeOffset.Z, len);
        }   // end of UpdateValues()
        
        #endregion

    }   // end of class SmoothCamera

}   // end of namespace Boku.Common
