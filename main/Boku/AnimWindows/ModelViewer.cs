// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

/*
 * ModelViewer.cs
 * Copyright (c) 2006 Michael Nikonov, David Astle
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be included
 * in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
#define WINDOWS

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
#endregion

namespace Xclna.Xna.Animation
{
    /// <summary>
    /// A camera for interfacing with the ModelViewer
    /// </summary>
    public interface IModelViewerCamera
    {
        /// <summary>
        /// The world matrix of the model that is being viewed.
        /// </summary>
        Matrix ModelWorld { get;}
        /// <summary>
        /// The view matrix of the camera.
        /// </summary>
        Matrix View { get;}
        /// <summary>
        /// The projection matrix used by the camera.
        /// </summary>
        Matrix Projection { get;}
        /// <summary>
        /// Updates the camera.
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        void Update(GameTime gameTime);
    }

    /// <summary>
    /// The default camera used by the model viewer.
    /// </summary>
    public sealed class DefaultModelViewerCamera : IModelViewerCamera
    {
        private Matrix world, view, projection;
        private Vector3 cameraPosition, up, right;
        private float fieldOfView, nearDistance, farDistance, windowWidth, windowHeight,
            centerX, centerY, aspectRatio;
        private int initialZoom;
        // Radius of the arcball
        private float arcRadius;
        private float camOffsetX = 0, camOffsetY = 0;
        private BoundingSphere sphere;

        private Viewport viewPort;
        private Vector3 modelPos = Vector3.One;
#if WINDOWS
        private MouseState lastState;
        private KeyboardState lastKeyboardState;
#endif

        // The model
        private Model model;

        /// <summary>
        /// Creates a new instance of the default model viewer camera.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="model">The model to view.</param>
        public DefaultModelViewerCamera(Game game, Model model)
        {
            this.model = model;
            // Basic parameter initialization
            sphere = new BoundingSphere(Vector3.Zero, 1.0f);
            game.IsMouseVisible = true;
            IGraphicsDeviceService graphics =
                (IGraphicsDeviceService)game.Services.GetService(
                typeof(IGraphicsDeviceService));
            viewPort = graphics.GraphicsDevice.Viewport;
            windowWidth = (float)viewPort.Width;
            windowHeight = (float)viewPort.Height;
            fieldOfView = MathHelper.PiOver4;
            nearDistance = .1f;
            centerX = windowWidth / 2.0f;
            centerY = windowHeight / 2.0f;
            aspectRatio = windowWidth / windowHeight;
            up = Vector3.Up;
            right = Vector3.Right;
#if WINDOWS
            MouseState state = Mouse.GetState();
            initialZoom = state.ScrollWheelValue;
#endif // WINDOWS
            this.model = model;
            // Merge the bounding spheres
            foreach (ModelMesh mesh in model.Meshes)
                sphere = BoundingSphere.CreateMerged(sphere, mesh.BoundingSphere);

            world = Matrix.Identity;
            farDistance = Math.Min(sphere.Radius * 1000, float.MaxValue);
            projection = Matrix.CreatePerspectiveFieldOfView(
                fieldOfView, aspectRatio, nearDistance, farDistance);
            cameraPosition = new Vector3(0, 0, sphere.Radius * 5);
            arcRadius = cameraPosition.Length() / 2.0f;
            view = Matrix.CreateLookAt(
                cameraPosition, Vector3.Zero, up);
        }

        // Returns true if the user clicked on the boundings sphere, false otherwise
        private bool IntersectPoint(int x, int y,
            out Vector3 intersectionPoint)
        {
            BoundingSphere sphere = new BoundingSphere(new Vector3(),
                arcRadius);
            // Convert the mouse position to a 3d vector
            Vector3 location = new Vector3(x, y, 0);
            // Location of mouse point converted to true 3d space
            location = viewPort.Unproject(location, projection, view, Matrix.Identity);
            // direction vector of camera
            Vector3 direction = location - cameraPosition;
            direction.Normalize();
            // Ray in the direction of the direction vector
            Ray r = new Ray(cameraPosition, direction);
            // Used to calculate where the ray intersects the sphere
            float? intersectFactor = r.Intersects(sphere);
            if (intersectFactor == null)
            {
                intersectionPoint = Vector3.Zero;
                return false;
            }
            else
            {
                intersectionPoint = cameraPosition +
                    (direction * ((float)intersectFactor));
                return true;
            }
        }

        #region IModelViewerCamera Members

        /// <summary>
        /// The world matrix of the model being viewed.
        /// </summary>
        public Matrix ModelWorld
        {
            get { return world; }
        }
        
        /// <summary>
        /// The view matrix of the camera.
        /// </summary>
        public Matrix View
        {
            get
            {
                Vector3 lookAt =
                    up * camOffsetY * sphere.Radius / 3.0f +
                    right * camOffsetX * sphere.Radius / 3.0f;
                return Matrix.CreateLookAt(cameraPosition,
                    lookAt, up);

            }
        }

        /// <summary>
        /// The projection matrix used by the camera.
        /// </summary>
        public Matrix Projection
        {
            get { return projection; }
        }

        /// <summary>
        /// Updates the model view camera
        /// </summary>
        /// <param name="gameTime">The time passed</param>
        public void Update(GameTime gameTime)
        {
#if WINDOWS
            MouseState state = Mouse.GetState();
            KeyboardState ks = Keyboard.GetState();

        
            // Adjust the zoom
            if (state.ScrollWheelValue != lastState.ScrollWheelValue)
            {
                float zoom =

                     sphere.Radius * 5 -
                    (sphere.Radius / 10.0f) *
                    (((float) (state.ScrollWheelValue - initialZoom) ) / 40.0f);
                // Clamp the zoom to the model radius
                if (zoom < sphere.Radius / 4.0f)
                    zoom = sphere.Radius / 4.0f;
                // Re-adjust the arcball radius
                arcRadius = zoom / 2.0f;
                // Re-adjust the camera position
                Vector3 n = Vector3.Normalize(cameraPosition);
                cameraPosition = n * zoom;
                view = Matrix.CreateLookAt(
                    cameraPosition, Vector3.Zero,
                    up);
            }



            // Update the view if the user drags the mouse
            if ((state.LeftButton == ButtonState.Pressed
                || state.RightButton == ButtonState.Pressed)
                && (state.X != lastState.X || state.Y != lastState.Y))
            {
                // The current click point and last clik point
                Vector3 curPt, lastPt;
                // If the mouse click intersects the arcball bounding sphere
                if (IntersectPoint(lastState.X, lastState.Y, out lastPt)
                    && IntersectPoint(state.X, state.Y, out curPt))
                {
                    // Do all sorts of crazy trig!
                    Vector3 cross = Vector3.Cross(lastPt, curPt);
                    cross.Normalize();
                    lastPt.Normalize();
                    curPt.Normalize();
                    float ang = (float)Math.Acos(Vector3.Dot(lastPt,
                        curPt));
                    Matrix axisRot = Matrix.CreateFromAxisAngle(
                        cross, ang * 2.0f);
                    if (state.LeftButton == ButtonState.Pressed)
                    {
                        world *= axisRot;
                    }

                    if (state.RightButton == ButtonState.Pressed)
                    {
                        Matrix invertRot = Matrix.Invert(axisRot);

                        cameraPosition = Vector3.Transform(cameraPosition,
                            invertRot);
                        up = Vector3.Normalize(Vector3.Transform(up,
                            invertRot));
                        right = Vector3.Normalize(Vector3.Transform(right,
                            invertRot));

                    }


                }

            }




            view = Matrix.CreateLookAt(
             cameraPosition, Vector3.Zero,
             up);
            if (ks.IsKeyDown(Keys.Right))
            {
                this.camOffsetX += .1f;
            }
            if (ks.IsKeyDown(Keys.Left))
            {
                this.camOffsetX -= .1f;
            }
            if (ks.IsKeyDown(Keys.Up))
            {
                this.camOffsetY += .1f;
            }
            if (ks.IsKeyDown(Keys.Down))
            {
                this.camOffsetY -= .1f;
            }

            view = Matrix.CreateLookAt(
                cameraPosition,
                Vector3.Zero,
                up);

            KeyboardState keyboardState = Keyboard.GetState();

            lastState = state;
            lastKeyboardState = keyboardState;


#endif
        }

        #endregion
    }

    /// <summary>
    /// A viewer animated models.
    /// </summary>
    public class ModelViewer : DrawableGameComponent
    {


        // The effects
        List<Effect> effects = new List<Effect>();

        // The viewing space bounding sphere.
        private ModelAnimator animator;
        private Model model;
        private IModelViewerCamera cam;

        /// <summary>
        /// Gets the animator that animates the model for this viewer.
        /// </summary>
        public ModelAnimator Animator
        {
            get { return animator; }
        }



        /// <summary>
        /// Creates a new instance of ModelViewer.
        /// </summary>
        /// <param name="game">The game to which the viewer will be attached.</param>
        /// <param name="model">The model to view.</param>
        public ModelViewer(Game game, Model model) : base(game)
        {
            cam = new DefaultModelViewerCamera(game, model);
            UpdateOrder = 3;
            Add(model);
            game.Components.Add(this);
        }

        /// <summary>
        /// Adds a model to the viewer.
        /// </summary>
        /// <param name="model">The model to add.</param>
        private void Add(Model model)
        {
            this.model = model;

            // Create an init the new controller
            animator = new ModelAnimator(Game, model);
            animator.Enabled = true;
            animator.Visible = true;

            InitializeEffects(model);

        }



        // Initialize the effects so the models look reasonable and the lighting
        // Is as it is in the directx Mesh viewer
        private void InitializeEffects(Model model)
        {


            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (Effect ef in mesh.Effects)
                {
                    effects.Add(ef);
                    ef.Parameters["View"].SetValue(cam.View);
                    ef.Parameters["EyePosition"].SetValue(Matrix.Invert(cam.View).Translation);
                    ef.Parameters["Projection"].SetValue(cam.Projection);
                    ef.Parameters["World"].SetValue(cam.ModelWorld);

                    if (ef is BasicPaletteEffect)
                    {
                        BasicPaletteEffect effect = (BasicPaletteEffect)ef;

                        effect.EnableDefaultLighting();
                        effect.DirectionalLight0.Direction = new Vector3(0, 0, -1);
                    }
                    else if (ef is BasicEffect)
                    {
                        BasicEffect effect = (BasicEffect)ef;
                        effect.EnableDefaultLighting();
                        effect.DirectionalLight0.Direction = new Vector3(0, 0, -1);
                        effect.DirectionalLight1.Enabled = false;
                        effect.DirectionalLight2.Enabled = false;
                        effect.AmbientLightColor = Color.Black.ToVector3();
                        effect.EmissiveColor = Color.Black.ToVector3();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the camera used by the viewer.
        /// </summary>
        public IModelViewerCamera Camera
        {
            get { return cam; }
            set 
            {
                if (value == null)
                    throw new ArgumentNullException("Camera can not be null.");
                cam = value;
            }
        }




        /// <summary>
        /// Updates the ModelViewer.
        /// </summary>
        /// <param name="gameTime">The GameTime.</param>
        public override void Update(GameTime gameTime)
        {
            if (cam != null)
            {
                cam.Update(gameTime);

                animator.World = cam.ModelWorld;
                foreach (Effect effect in effects)
                {
                    effect.Parameters["View"].SetValue(cam.View);
                    effect.Parameters["EyePosition"].SetValue(Matrix.Invert(cam.View).Translation);
                    effect.Parameters["Projection"].SetValue(cam.Projection);
                }
            }
            base.Update(gameTime);
        }
    }
}
