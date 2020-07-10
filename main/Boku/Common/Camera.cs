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

using Boku.SimWorld.Terra;

namespace Boku.Common
{
    public abstract class Camera
    {
        #region Members

        protected Vector3 from;             // Where we're looking from.
        protected Vector3 at;               // Where we're looking at.
        protected Vector3 up;               // What's up?
        protected float aspectRatio;        // width/height
        protected float nearClip;           // near clip distance
        protected float farClip;            // far clip distance
        protected Point resolution;         // Current screen resolution
        protected float fov;                // Vertical field of view in radians.

        protected float heightOffset;       // This is a offset added to the camera's 'at' position 
                                            // in the 'up' direction.  This is here to make controlling 
                                            // the camera in edit mode easier.
        protected float targetHeight;       // Used internally to support the twitch.


        protected BoundingSphere bound;     // Bounding sphere enclosing eye and near clip face of frustum.

        protected Matrix projectionMatrix;
        protected Matrix viewMatrix;
        protected Matrix viewProjectionMatrix;

        protected Frustum frustum = null;

        protected bool dirty = true;        // Has something changed?  If so call Recalc().

        #endregion

        #region Accessors
        /// <summary>
        /// Raw camera From. Actual used camera from in ActualFrom.
        /// </summary>
        public Vector3 From
        {
            get { return from; }
            set { Debug.Assert(!float.IsNaN(value.X)); from = value; dirty = true; }
        }
        /// <summary>
        /// Raw camera At. Actual used camera at in ActualAt.
        /// </summary>
        public Vector3 At
        {
            get { return at; }
            set { Debug.Assert(!float.IsNaN(value.X)); at = value; dirty = true; }
        }
        /// <summary>
        /// Raw up. Not necessarily orthonormal to ViewDir. Actually used
        /// up is Vector3.Cross(ViewDir, Vector3.Cross(Up, ViewDir)). Use ViewUp.
        /// </summary>
        public Vector3 Up
        {
            get { return up; }
            set { Debug.Assert(!float.IsNaN(value.X)); up = value; dirty = true; }
        }

        /// <summary>
        /// Composite camera from which gets fed into view transforms.
        /// </summary>
        public Vector3 ActualFrom
        {
            get { return new Vector3(from.X, from.Y, from.Z + heightOffset); }
        }
        /// <summary>
        /// Composite camera at which gets fed into view transforms.
        /// </summary>
        public Vector3 ActualAt
        {
            get { return new Vector3(at.X, at.Y, at.Z + heightOffset); }
        }
        
        public float AspectRatio
        {
            get { return aspectRatio; }
            set { aspectRatio = value; dirty = true; }
        }
        public float NearClip
        {
            get { return nearClip; }
            set { nearClip = value; dirty = true; }
        }
        public float FarClip
        {
            get { return farClip; }
            set { farClip = value; dirty = true; }
        }
        public Point Resolution
        {
            get { return resolution; }
            set
            {
                if (resolution != value)
                {
                    resolution = value;
                    dirty = true;
                }
            }
        }
        public BoundingSphere BoundingSphere
        {
            get 
            {
                if (dirty)
                {
                    Recalc();
                }
                return bound; 
            }
        }
        public Matrix ProjectionMatrix
        {
            get
            {
                if (dirty)
                {
                    Recalc();
                }
                return projectionMatrix;
            }
        }
        public Matrix ViewMatrix
        {
            get
            {
                if (dirty)
                {
                    Recalc();
                }
                return viewMatrix;
            }
        }
        public Matrix ViewProjectionMatrix
        {
            get
            {
                if (dirty)
                {
                    Recalc();
                }
                return viewProjectionMatrix;
            }
        }

        /// <summary>
        /// A normalized vector along the line of sight.
        /// </summary>
        public Vector3 ViewDir
        {
            get
            {
                if (dirty)
                {
                    Recalc();
                }
                return new Vector3(-viewMatrix.M13, -viewMatrix.M23, -viewMatrix.M33);
            }
        }
        /// <summary>
        /// The actual up vector orthonormal to the dir vector.
        /// </summary>
        public Vector3 ViewUp
        {
            get 
            {
                if (dirty)
                {
                    Recalc();
                }
                return new Vector3(viewMatrix.M12, viewMatrix.M22, viewMatrix.M32);
            }
        }

        /// <summary>
        /// The actual Side vector orthonormal to the dir vector.
        /// </summary>
        public Vector3 ViewRight
        {
            get 
            {
                if (dirty)
                {
                    Recalc();
                }
                return new Vector3(viewMatrix.M11, viewMatrix.M21, viewMatrix.M31);
            }
        }


        public Frustum Frustum
        {
            get { return frustum; }
        }

        public float Fov
        {
            get { return fov; }
            set { fov = value; dirty = true; }
        }

        /// <summary>
        /// This is a offset added to the camera's 'at' position 
        /// in the 'up' direction.  This is here to make controlling 
        /// the camera in edit mode easier.
        /// </summary>
        public float HeightOffset
        {
            get { return heightOffset; }
            set { heightOffset = value; dirty = true; }
        }

        /// <summary>
        /// Should only be used for testing to force dirty to be set.
        /// </summary>
        public bool Dirty
        {
            get { return dirty; }
            set { dirty = value; }
        }

        #endregion

        #region Public 

        public Camera()
        {
            bound = new BoundingSphere();
            heightOffset = 0.0f;
            targetHeight = float.MinValue;
        }

        public abstract void Update();  // Called once per frame.
        public abstract void Recalc();  // Called whenever the values may be dirty.

        /// <summary>
        /// Transforms the input position into pixel based screen coordinates.
        /// </summary>
        public Point WorldToScreenCoords(Vector3 position)
        {
            Point result = new Point();

            Vector4 pos = new Vector4(position, 1.0f);

            Vector4 pos2 = Vector4.Transform(pos, ViewProjectionMatrix);
            pos2.X /= pos2.W;
            pos2.Y /= pos2.W;
            result.X = (int)(pos2.X * resolution.X / 2 + resolution.X / 2);
            result.Y = (int)(-pos2.Y * resolution.Y / 2 + resolution.Y / 2);

            return result;
        }   // end of Camera WorldToScreenCoords()

        /// <summary>
        /// Transforms the input position into pixel based screen coordinates returns Vector2.
        /// </summary>
        /// <param name="position"/>
        public Vector2 WorldToScreenCoordsVector2(Vector3 position)
        {
            Vector2 result = new Vector2();

            Vector4 pos = new Vector4(position, 1.0f);

            Vector4 pos2 = Vector4.Transform(pos, ViewProjectionMatrix);
            pos2.X /= pos2.W;
            pos2.Y /= pos2.W;
            result.X = (pos2.X * resolution.X *0.5f + resolution.X * 0.5f);
            result.Y = (-pos2.Y * resolution.Y *0.5f + resolution.Y *0.5f);

            return result;
        }   // end of Camera WorldToScreenCoords()

        /// <summary>
        /// Converts the given pixel position into a world vector 
        /// from the eye, through the pixel, into the world.
        /// </summary>
        /// <param name="position">Position in pixels</param>
        /// <returns></returns>
        public Vector3 ScreenToWorldCoords(Vector2 position)
        {
            Vector3 viewDir = ViewDir;
            Vector3 result = viewDir;

            // Offset position to center of screen.
            Vector2 pos = new Vector2(position.X - Resolution.X / 2.0f, position.Y - Resolution.Y / 2.0f);

            float h = (float)Math.Tan(Fov / 2.0f);
            float w = AspectRatio * h;

            Vector3 viewUp = ViewUp;

            /// No need to normalize right, viewDir & viewUp are orthonormal.
            Vector3 viewRight = Vector3.Cross(viewDir, viewUp);

            result += viewRight * w * pos.X / (Resolution.X / 2.0f);
            result += -viewUp * h * pos.Y / (Resolution.Y / 2.0f);

            result.Normalize();

            return result;
        }   // end of ScreenToWorldCoords()

        /// <summary>
        /// Converts pixels into camera space screen coords which
        /// assumes z=0.  Basically useful for converting mouse
        /// hits into camera space.
        /// </summary>
        /// <param name="pixels"></param>
        /// <returns></returns>
        public Vector2 PixelsToCameraSpaceScreenCoords(Vector2 pixels)
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
            Viewport vp = device.Viewport;

            Vector3 pos = new Vector3(pixels.X, pixels.Y, 0);
            pos = vp.Unproject(pos, projectionMatrix, viewMatrix, Matrix.Identity);

            // Project back out to z==0
            Vector3 dir = pos - ActualFrom;
            pos += dir * (pos.Z / dir.Z);

            Vector2 result = new Vector2(pos.X, pos.Y);

            return result;

        }   // end of PixelsToCameraSpaceScreenCoords()

        /// <summary>
        /// A helper function which smoothly changes the heightOffset of the camera.
        /// This takes the desired offset as a paramter, rather than the absolute height.
        /// </summary>
        /// <param name="height">The height we want the camera at point to be.</param>
        public void ChangeHeightOffsetNotHeight(float desiredOffset)
        {
            targetHeight = Terrain.GetTerrainHeightFlat(At) + desiredOffset;
            HeightOffset = MyMath.Lerp(HeightOffset, desiredOffset, Time.WallClockFrameSeconds);
        }   // end of Camera MoveHeightOffset()

        /// <summary>
        /// A helper function which smoothly changes the heightOffset of the camera.
        /// </summary>
        /// <param name="height">The height we want the camera at point to be.</param>
        public void ChangeHeightOffset(float height)
        {
            float desiredOffset = height - At.Z;
            // Be sure this doesn't go negative.
            desiredOffset = Math.Max(0.0f, desiredOffset);
            targetHeight = height;
            HeightOffset = MyMath.Lerp(HeightOffset, desiredOffset, Time.WallClockFrameSeconds);
        }   // end of Camera MoveHeightOffset()

        /// <summary>
        /// Clears the heightOffset back to default.
        /// </summary>
        /// <param name="value">New value for offset.</param>
        /// <param name="twitchTime">The duration of the twitch.</param>
        public void ClearHeightOffset(float value, float twitchTime)
        {
            // Only start a twitch if we're not already disabled.
            if (targetHeight != float.MinValue)
            {
                //HeightOffset = height;
                float desiredOffset = value;
                TwitchManager.Set<float> set = delegate(float val, Object param) { HeightOffset = val; };
                TwitchManager.CreateTwitch<float>(HeightOffset, value, set, twitchTime, TwitchCurve.Shape.EaseInOut);

                targetHeight = float.MinValue;  // Effectively disable the offset.
            }
        }   // end of Camera ClearHeightOffset()

        /// <summary>
        /// Resets the camera's height offset.  If out of water, the default value is
        /// used.  If in water the offset is set ot half way between the ground and
        /// the surface of the water.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="twitchTime"></param>
        public void SetDefaultHeightOffset(Vector3 position, float twitchTime)
        {
            float water = Terrain.GetWaterBase(position);
            if (water > 0.0)
            {
                float height = (water + position.Z) / 2.0f;
                ChangeHeightOffset(height);
            }
            else
            {
                // No water, return to default height.
                float height = InGame.inGame.CurrentUpdateMode == InGame.UpdateMode.RunSim ? 0.0f : 1.0f;
                
                ClearHeightOffset(height, twitchTime);
            }

        }   // end of SetDefaultHeightOffset()

        public float GetFocalDistance()
        {
            Vector3 virtualAt = At + heightOffset * Up;
            return Vector3.Distance(From, virtualAt);
        }   // end of Camera GetFocalDistance()

        #endregion

    }   // end of class Camera  

    /// <summary>
    /// Camera with orthographic projection used for programming UI.
    /// </summary>
    public class UiCamera : Camera
    {
        #region Members

        private float dpi = 96.0f;
        private Vector3 offset = Vector3.Zero;          // A translation offset for the whole camera.
        private Vector3 desiredOffset = Vector3.Zero;   // Offset set by user input.  This may be changed several times per frame which is why
                                                        // we also need to have the targetOffset so we don't kick of twitches each frame.
        private Vector3 targetOffset = Vector3.Zero;    // Offset targetted by the twitch.

        private float tutorialScale = 1.0f;             // Scale factor used to adjust for tutorial mode.
        private Vector3 tutorialOffset = Vector3.Zero;  // Offset used to adjust for tutorial mode.

        #endregion

        #region Accessors

        public float Dpi
        {
            get { return dpi; }
            set
            {
                if (value != dpi)
                {
                    dpi = value;
                    dirty = true;
                    frustum = new Frustum();
                    Recalc();
                }
            }
        }

        public float DefaultDPI
        {
            get
            {
                float smallestAxis = Math.Min( resolution.X, resolution.Y );

                float defaultDpi = 96.0f / 720.0f * smallestAxis;

                // At samller resolutions, zoom in a bit to increase legibility.
                if (smallestAxis < 600.0f)
                {
                    defaultDpi /= 1.0f - 0.5f * (600.0f - smallestAxis) / 600.0f;
                }


                return defaultDpi;
            }
        }

        /// <summary>
        /// Adds a vertical translation offset to the while camera.  No rotation.
        /// </summary>
        public Vector3 Offset
        {
            get { return desiredOffset; }
            set 
            {
                value.Z = 0.0f;     // Don't move in depth
                desiredOffset = value;
            }
        }

        /// <summary>
        /// Scale induced by being in tutorial mode.
        /// </summary>
        public float TutorialScale
        {
            get { return tutorialScale; }
        }

        #endregion

        #region Public

        public UiCamera()
        {
            Center();
            nearClip = 1.0f;
            farClip = 50.0f;
            fov = 1.0f;     // radians

            resolution = new Point((int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);

            dpi = DefaultDPI;

            frustum = new Frustum();
            Recalc();
        }
        public void Center()
        {
            this.from = new Vector3(0, 0, 11.5f);
            this.at = new Vector3(0, 0, 0);
            this.up = new Vector3(0, 1, 0);
        }
        public float Width
        {
            get
            {
                return resolution.X / Dpi;
            }
        }
        public float Height
        {
            get
            {
                return resolution.Y / Dpi;
            }
        }

        /// <summary>
        /// Once per frame update.
        /// </summary>
        public override void Update()
        {
            //offset = MyMath.Lerp(offset, desiredOffset, 5.0f * Time.WallClockFrameSeconds);
            //dirty = true;

            if (dirty)
            {
                Recalc();
            }

            // Calc scale from tutorial mode.  If changed, mark as dirty.
            float scale = BokuGame.ScreenSize.Y / Resolution.Y;
            if (scale != tutorialScale)
            {
                tutorialScale = scale;
                tutorialOffset = 0.5f * new Vector3(BokuGame.ScreenPosition / Dpi, 0);

                dirty = true;
            }

            if (desiredOffset != targetOffset)
            {
                targetOffset = desiredOffset;

                TwitchManager.Set<Vector3> set = delegate(Vector3 value, Object param) { offset = value; dirty = true; };
                TwitchManager.CreateTwitch<Vector3>(offset, targetOffset, set, 0.4f, TwitchCurve.Shape.OvershootOut);
            }
        }
        
        /// <summary>
        /// Takes the current camera params and Recalcs the internal matrices.
        /// </summary>
        public override void Recalc()
        {
            if (dirty)
            {
                dpi = DefaultDPI;

                Vector3 actualAt = ActualAt;

                aspectRatio = (float)resolution.X / resolution.Y;

                //projectionMatrix = Matrix.CreatePerspectiveFieldOfView(fov, aspectRatio, nearClip, farClip);

                projectionMatrix = Matrix.CreateOrthographic(Width / tutorialScale, Height / tutorialScale, nearClip, farClip);

                viewMatrix = Matrix.CreateLookAt(ActualFrom + offset + tutorialOffset, actualAt + offset + tutorialOffset, Up);

                viewProjectionMatrix = viewMatrix * projectionMatrix;

                frustum.Update(ref viewProjectionMatrix);

                dirty = false;
            }
        }

        #endregion
    }   // end of class UiCamera


    /// <summary>
    /// Orthographic camera set up for looking straight down to render shadow textures.
    /// </summary>
    public class ShadowCamera : Camera
    {
        #region Members

        float radius = 25.0f;

        Texture2D shadowTexture;
        Texture2D shadowMask = null;

        Vector4 offsetScale;
        Vector4 maskOffsetScale;

        #endregion

        #region Accessors
        public Texture2D ShadowTexture
        {
            get { return shadowTexture; }
            set { shadowTexture = value; }
        }
        public Texture2D ShadowMask
        {
            get { return shadowMask; }
        }
        public Vector4 OffsetScale
        {
            get { return offsetScale; }
        }
        public Vector4 MaskOffsetScale
        {
            get { return maskOffsetScale; }
        }
        #endregion Accessors

        #region Public

        public void Locate(Camera camera)
        {
            Vector2 center = new Vector2(camera.ActualFrom.X, camera.ActualFrom.Y);

            float backFrac = 1.0f;
            Vector3 viewDir = camera.ViewDir;
            Vector2 cameraDir = new Vector2(viewDir.X, viewDir.Y);
            center += cameraDir * (backFrac * radius);

            this.from = new Vector3(center.X, center.Y, 1000.0f);
            this.at = new Vector3(center.X, center.Y, 0.0f);
            this.up = new Vector3(0.0f, 1.0f, 0.0f);
            this.nearClip = 1.0f;
            this.farClip = 2000.0f;

            // Calc offset and scale.  Y coord needs to be flipped.
            Vector2 min = new Vector2(center.X - radius, center.Y - radius);
            Vector2 max = new Vector2(center.X + radius, center.Y + radius);

            maskOffsetScale = new Vector4(
                -min.X / (max.X - min.X),
                max.Y / (max.Y - min.Y),
                1.0f / (max.X - min.X),
                -1.0f / (max.Y - min.Y));

            center = Align(center);
            min = new Vector2(center.X - radius, center.Y - radius);
            max = new Vector2(center.X + radius, center.Y + radius);

            offsetScale = new Vector4(
                -min.X / (max.X - min.X),
                max.Y / (max.Y - min.Y),
                1.0f / (max.X - min.X),
                -1.0f / (max.Y - min.Y));

            this.Resolution = new Point(1024, 1024);
            this.dirty = true;

            if (frustum == null)
                frustum = new Frustum();

            Recalc();

            if (shadowMask == null)
            {
                shadowMask = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\shadowmask");
            }
        }

        public float Width
        {
            get { return radius; }
        }
        public float Height
        {
            get { return radius; }
        }

        /// <summary>
        /// Once per frame update.
        /// </summary>
        public override void Update()
        {
        }

        /// <summary>
        /// Takes the current camera params and updates the internal matrices.
        /// </summary>
        public override void Recalc()
        {
            if (dirty)
            {
                aspectRatio = (float)resolution.X / resolution.Y;

                projectionMatrix = Matrix.CreateOrthographic(radius * 2.0f, radius * 2.0f, nearClip, farClip);

                viewMatrix = Matrix.CreateLookAt(ActualFrom, ActualAt, Up);

                viewProjectionMatrix = viewMatrix * projectionMatrix;

                frustum.Update(ref viewProjectionMatrix);

                dirty = false;
            }
        }

        /// <summary>
        /// Load up the mask used for the shadow region.
        /// </summary>
        /// <param name="name"></param>
        public void LoadShadowMask(string name)
        {
            shadowMask = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + name);
        }

        public void ReleaseShadowMask()
        {
            BokuGame.Release(ref shadowMask);
        }

        #endregion

        #region Internal
        protected Vector2 Align(Vector2 pt)
        {
            pt.X *= resolution.X;
            pt.X = (float)Math.Floor(pt.X);
            pt.X /= resolution.X;

            pt.Y *= resolution.Y;
            pt.Y = (float)Math.Floor(pt.Y);
            pt.Y /= resolution.Y;

            return pt;
        }
        #endregion Internal

    }   // end of class ShadowCamera


    public class PerspectiveUICamera : SimCamera
    {
        #region Public

        public float DPI
        {
            get
            {
                return resolution.Y / 7.5f;
            }
        }

        public PerspectiveUICamera()
        {
            Fov = 1.0f; // radians

            // Force z so that we get 96 dpi.
            // float z = Resolution.Y / (2.0f * 96.0f * (float)Math.Tan(Fov / 2.0f));

            // Force z so that we get a vertical res of 7.5 units.
            float z = (7.5f / 2.0f) / (float)Math.Tan(Fov / 2.0f);

            From = new Vector3(0, 0, z);
            At = new Vector3(0, 0, 0);
            Up = new Vector3(0, 1, 0);
            NearClip = 0.1f;
            FarClip = 50.0f;

            Recalc();
        }

        #endregion 
    }   // end of class PerspectiveUICamera

    public class SimCamera : Camera
    {
        #region Public

        public SimCamera()
        {
            this.from = new Vector3(100, 100, 100);;
            this.at = new Vector3(0, 0, 0);
            this.up = new Vector3(0, 0, 1);
            this.fov = 1.0f;   // radians
            this.nearClip = 0.1f;
            this.farClip = 1000.0f;

            this.resolution = new Point((int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);

            frustum = new Frustum();

            Recalc();
        }

        /// <summary>
        /// Once per frame update.
        /// </summary>
        public override void Update()
        {
        }

        /// <summary>
        /// Takes the current camera params and Recalcs the internal matrices.
        /// </summary>
        public override void Recalc()
        {
            if (dirty)
            {
                aspectRatio = (float)resolution.X / resolution.Y;

                projectionMatrix = Matrix.CreatePerspectiveFieldOfView(fov, aspectRatio, nearClip, farClip);
                viewMatrix = Matrix.CreateLookAt(ActualFrom, ActualAt, Up);

                viewProjectionMatrix = viewMatrix * projectionMatrix;

                frustum.Update(ref viewProjectionMatrix);

                // Recalc the bounding sphere and corners.
                {
                    double dx = nearClip * Math.Sin(fov / 2);
                    double dy = dx / aspectRatio;
                    double d = Math.Sqrt(dx * dx + dy * dy);
                    double alpha = Math.Atan(d / nearClip);
                    double theta = Math.PI / 2.0 - 2.0 * alpha;
                    float radius = (float)(d / Math.Cos(theta));
                    bound.Center = ActualFrom;
                    bound.Radius = (float)Math.Sqrt(NearClip * NearClip + dx * dx + dy * dy);
                }

                dirty = false;
            }

        }

        #endregion

    }   // end of class SimCamera

}   // end of namespace Boku.Common  


