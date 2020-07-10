// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using Boku.Common;

namespace KoiX
{
    /// <summary>
    /// 2D camera class designed to work with the Sprite class.
    /// 
    /// Works in coordinate space Sprites work in:
    ///     unit is pixels
    ///     X is to the right
    ///     Y is down
    ///     Z (such as it is) is into the screen
    ///     
    ///     Hence, positive rotation is clockwise.  Note that rotating the camera clockwise
    ///     makes it appear like everything being rendered is rotating counter-clockwise.
    /// 
    ///     Position is at center of screen.
    ///     Size is ScreenSize / Zoom
    ///     
    ///     Zoom defaults to 1 (1:1 pixels)
    ///     Zoom > 1 is zoomed in.
    ///     Zoom < 1 is zoomed out.
    /// </summary>
    public class SpriteCamera
    {
        #region Members

        Vector2 position;   // Position used for rendering.
        Vector2 _position;  // Twitch target used for testing changes.
        Vector2 deltaPos;   // Change in position since last update.

        float rotation;
        float _rotation;    // Twitch target.
        float cos = 1.0f;   // Internal values based on rotation angle.
        float sin = 0.0f;

        float zoom = 1.0f;
        float minZoom = 0.125f;
        float maxZoom = 8;

        float twitchTime = 0;   // Default to no twitch.

        Vector2 screenSize;

        Matrix view = Matrix.Identity;      // Used with SpriteBatch.Begin()
        Matrix proj = Matrix.Identity;
        Matrix viewProj = Matrix.Identity;

        Matrix invView = Matrix.Identity;

        #endregion

        #region Accessors

        /// <summary>
        /// Location where camera is centered.
        /// </summary>
        public Vector2 Position
        {
            get { return position; }
            set 
            {
                if (_position != value)
                {
                    if (twitchTime == 0)
                    {
                        deltaPos = value - position;
                        position = value;
                        _position = value;
                    }
                    else
                    {
                        _position = value;
                        TwitchManager.Set<Vector2> set = delegate(Vector2 val, object param) { deltaPos = val - position; position = val; };
                        TwitchManager.CreateTwitch<Vector2>(position, _position, set, twitchTime, TwitchCurve.Shape.EaseInOut);
                    }
                }
            }
        }

        public Vector2 DeltaPos
        {
            get { return deltaPos; }
        }

        /// <summary>
        /// value == 1 is 1:1 pixels
        /// value > 1 is zoomed in
        /// value less than 1 is zoomed out
        /// </summary>
        public float Zoom
        {
            get { return zoom; }
            set
            {
                // TODO (****) Twitch this?
                value = MathHelper.Clamp(value, minZoom, maxZoom);
                if (zoom != value)
                {
                    zoom = value;
                }
            }
        }

        /// <summary>
        /// Camera rotation.  When setting this the camera
        /// rotates around the center of the screen.
        /// </summary>
        public float Rotation
        {
            get { return rotation; }
            set 
            {
                float rot = MathHelper.WrapAngle(value);
                if (rot != _rotation)
                {
                    if (twitchTime == 0)
                    {
                        _rotation = rot;
                        rotation = rot;
                    }
                    else
                    {
                        _rotation = rot;
                        TwitchManager.Set<float> set = delegate(float val, object param) { rotation = val; };
                        TwitchManager.CreateTwitch<float>(rotation, rotation, set, twitchTime, TwitchCurve.Shape.EaseInOut);
                    }
                }
            }
        }

        public float MinZoom
        {
            get { return minZoom; }
            set
            {
                if (minZoom != value)
                {
                    minZoom = value;
                    Zoom = Zoom;        // Force clamping to new limit.
                }
            }
        }

        public float MaxZoom
        {
            get { return maxZoom; }
            set
            {
                if (maxZoom != value)
                {
                    maxZoom = value;
                    Zoom = Zoom;        // Force clamping to new limit.
                }
            }
        }

        public float TwitchTime
        {
            get { return twitchTime; }
            set { twitchTime = value; }
        }

        public Matrix ProjMatrix
        {
            get { return proj; }
        }

        public Matrix ViewProjMatrix
        {
            get { return viewProj; }
        }

        /// <summary>
        /// This is the matrix that needs to be passed to batch.Begin()
        /// when making this work with DrawString or Sprites.
        /// </summary>
        public Matrix ViewMatrix
        {
            get { return view; }
        }

        public Matrix InverseViewMatrix
        {
            get { return invView; }
        }

        /// <summary>
        /// Screensize.
        /// </summary>
        public Vector2 ScreenSize
        {
            get { return screenSize; }
        }

        public float Cos
        {
            get { return cos; }
        }

        public float Sin
        {
            get { return sin; }
        }

        #endregion

        #region Public

        public SpriteCamera()
        {
            // Ensure we're in a useful state upon creation.
            Update();
        }   // end of c'tor

        public void Update()
        {
            Vector2 updateScreenSize = screenSize;
            // TODO (****) Should be this ClientSize or ViewportSize?  When does it make 
            // a difference?  Figure this out and document.
            updateScreenSize.X = KoiLibrary.ClientRect.Width;
            updateScreenSize.Y = KoiLibrary.ClientRect.Height;

            Update(updateScreenSize);
        }

        public void Update(Vector2 updateScreenSize)
        {
            // TODO (****) Should be this ClientSize or VeiwportSize?  When does it make 
            // a difference?  Figure this out and document.
            //screenSize.X = KoiLibrary.ClientRect.Width;
            //screenSize.Y = KoiLibrary.ClientRect.Height;
            screenSize = updateScreenSize;

            proj = Matrix.CreateOrthographic(screenSize.X, screenSize.Y, -10.0f, 10.0f);

            sin = (float)Math.Sin(rotation);
            cos = (float)Math.Cos(rotation);

            // Create needed sub-matrix directly.  Probably saves no real time...
            Matrix mat = Matrix.Identity;
            mat.M11 = zoom * cos;
            mat.M12 = -zoom * sin;
            mat.M21 = zoom * sin;
            mat.M22 = zoom * cos;
            mat.M41 = -position.X * cos * zoom + -position.Y * sin * zoom;
            mat.M42 = -position.Y * cos * zoom - -position.X * sin * zoom;

            /*
            view = Matrix.CreateTranslation(new Vector3(-position, 0)) 
                    * Matrix.CreateRotationZ(-rotation)
                    * Matrix.CreateScale(new Vector3(zoom, zoom, 1))
                    * Matrix.CreateTranslation(new Vector3(screenSize / 2.0f, 0));      // Put 0,0 at center of screen.
            */

            view = mat;
            view.M41 += screenSize.X / 2.0f;
            view.M42 += screenSize.Y / 2.0f;

            invView = Matrix.Invert(view);

            /*
            viewProjRoundedRect = Matrix.CreateTranslation(new Vector3(-position, 0))
                                    * Matrix.CreateRotationZ(-rotation)
                                    * Matrix.CreateScale(new Vector3(zoom, -zoom, 1))
                                    * proj;
            */

            // TODO (****) Need to figure out when each matrix is needed and be more
            // clean about how they should be used.

            // Flip Y axis for ViewProj version which is used by SpriteBatch and Koi.Geometry.
            mat.M12 *= -1;
            mat.M22 *= -1;
            mat.M42 *= -1;

            viewProj = mat * proj;

        }   // end of Update()

        public void Render()
        {
        }   // end of Render()

        //
        //
        // TODO (****)  Need to verify that these all work!
        //
        //

        /// <summary>
        /// Test rectangle to see if it is visible in the camera.
        /// Note this is camera units (zoomed) not raw pixels.
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public Frustum.CullResult CullTest(RectangleF rect)
        {
            // Get rect vertices.
            Vector2[] p = new Vector2[4];
            p[0] = rect.Position;
            p[1] = p[0] + new Vector2(rect.Size.X, 0);
            p[2] = p[0] + new Vector2(0, rect.Size.Y);
            p[3] = p[0] + new Vector2(rect.Size.X, rect.Size.Y);

            // Transform into homogeneous coords.
            for (int i = 0; i < 4; i++)
            {
                p[i] = Vector2.Transform(p[i], ViewProjMatrix);
            }

            // Most common case is that all points are inside so look at that one first.
            Frustum.CullResult result = Frustum.CullResult.TotallyInside;
            for (int i = 0; i < 4; i++)
            {
                // Check if point is outside.
                if (p[i].X < -1 || p[i].X > 1 || p[i].Y < -1 || p[i].Y > 1)
                {
                    // Found one that's outside, break;
                    result = Frustum.CullResult.PartiallyInside;
                    break;
                }
            }

            // All points are inside so we're done.
            if (result == Frustum.CullResult.TotallyInside)
            {
                return result;
            }

            // See if all points are fully outside the same side of the screen.
            if (p[0].X < -1 && p[1].X < -1 && p[2].X < -1 && p[3].X < -1)
            {
                return Frustum.CullResult.TotallyOutside;
            }
            if (p[0].X > 1 && p[1].X > 1 && p[2].X > 1 && p[3].X > 1)
            {
                return Frustum.CullResult.TotallyOutside;
            }
            if (p[0].Y < -1 && p[1].Y < -1 && p[2].Y < -1 && p[3].Y < -1)
            {
                return Frustum.CullResult.TotallyOutside;
            }
            if (p[0].Y > 1 && p[1].Y > 1 && p[2].Y > 1 && p[3].Y > 1)
            {
                return Frustum.CullResult.TotallyOutside;
            }

            // TODO (****) Note this is not full correct.  You can still have a 
            // rect which is oriented diagonally with all its vertices off screen
            // but overlapping a corner of the screen.  A proper, general solution 
            // would probably be to do the fully inside test and then test each
            // line segment of the rect vs each edge of the screen.  Bit too much
            // overkill for now.

            return result;
        }

        /// <summary>
        /// Converts a point in screen coords to camera coords,
        /// ie mouse input to camera coords.
        /// </summary>
        /// <param name="screenPoint"></param>
        /// <returns></returns>
        public Point ScreenToCamera(Point screenPoint)
        {
            Vector2 cameraPoint = new Vector2(screenPoint.X, screenPoint.Y);

            cameraPoint = ScreenToCamera(cameraPoint);

            Point result = new Point((int)cameraPoint.X, (int)cameraPoint.Y);
            return result;
        }   // end of ScreenToCamera()

        /// <summary>
        /// Converts a point in screen coords to camera coords,
        /// ie mouse input to camera coords.
        /// </summary>
        /// <param name="screenPoint"></param>
        /// <returns></returns>
        public Vector2 ScreenToCamera(Vector2 screenPoint)
        {
            Vector2 cameraPoint = Vector2.Transform(screenPoint, InverseViewMatrix);
            
            return cameraPoint;
        }   // end of ScreenToCamera()

        /// <summary>
        /// Converts a point in camera space to screen space.
        /// </summary>
        /// <param name="camerPoint"></param>
        /// <returns></returns>
        public Vector2 CameraToScreen(Vector2 cameraPoint)
        {
            Vector2 screenPoint = Vector2.Transform(cameraPoint, ViewMatrix);

            return screenPoint;
        }   // end of CameraToScreen()

        public void RotateAroundPoint(float deltaRotation, Vector2 centerOfRotation)
        {
            // Offset position to account for rotation not around center.
            Vector2 offset = Position - centerOfRotation;
            Matrix rot = Matrix.CreateRotationZ(deltaRotation);
            Vector2 newOffset = Vector2.Transform(offset, rot);

            Position += newOffset - offset;

            // Update the rotation.
            Rotation += deltaRotation;

        }   // end of RotateAroundPoint()

        /// <summary>
        /// 
        /// </summary>
        /// <param name="zoomScale">Factor to multiple current scale by.  1.0 means no change.</param>
        /// <param name="centerOfZoom">Center needs to be in camera coordinates.</param>
        public void ZoomAroundPoint(float zoomScale, Vector2 centerOfZoom)
        {
            // We apply zoom first because the actual result may depend
            // on whether the camera clamps the zoom to a fixed range or not.
            float prevZoom = Zoom;
            Zoom *= zoomScale;
            float newZoom = Zoom;

            // Calc what the actual scaling was.
            zoomScale = newZoom / prevZoom;

            if (zoomScale != 1.0f)
            {
                // Offset position to account for zoom not around center.
                Vector2 offset = Position - centerOfZoom;
                Vector2 newOffset = offset * zoomScale;

                Position -= newOffset - offset;
            }

        }   // end of ZoomAroundPoint()

        /*
        /// <summary>
        /// Transforms rect and checks if it can safely be culled.
        /// </summary>
        /// <param name="rect"></param>
        /// <returns>true if rect should be culled.</returns>
        public bool CullTest(RectangleF rect)
        {
            Vector2[] points = new Vector2[4];
            points[0] = rect.Position;
            points[1] = points[0] + new Vector2(rect.Width, 0);
            points[2] = points[0] + new Vector2(rect.Width, rect.Height);
            points[3] = points[0] + new Vector2(0, rect.Height);

            // Transform points.
            for (int i = 0; i < 4; i++)
            {
                points[i] = Vector2.Transform(points[i], ViewProjMatrix);

                // On screen?  Don't cull.
                if (points[i].X > -1 && points[i].X < 1 && points[i].Y > -1 && points[i].Y < 1)
                {
                    return false;
                }
            }

            // TODO This will wrongly cull rects that cover all or part of the screen but
            // don't have any vertices on the screen.

            return true;
        }   // end of CullTest()
        */
        #endregion

        #region Internal

        #endregion
    }   // end of class SpriteCamera

}   // end of namespace KoiX
