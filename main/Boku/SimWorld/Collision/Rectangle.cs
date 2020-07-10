// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Microsoft.Xna.Framework;

using Boku.Base;

namespace Boku.SimWorld.Collision
{
    internal class Rectangle : CollisionPrimitive
    {
        #region Members
        /// <summary>
        /// These define the Rectangle in rootSpace
        /// </summary>
        private float halfWidth = 0.0f;
        private float halfHeight = 0.0f;
        private Matrix planeToLocal = Matrix.Identity;
        private Matrix localToPlane = Matrix.Identity;

        /// <summary>
        /// These are derived from the root's transform and the root space plane.
        /// </summary>
        private Matrix worldToPlane;
        private Matrix planeToWorld;

        #endregion Members

        #region Accessors
        /// <summary>
        /// Width along local X
        /// </summary>
        public float Width
        {
            get { return halfWidth * 2.0f; }
        }
        /// <summary>
        /// Height along local Y
        /// </summary>
        public float Height
        {
            get { return halfHeight * 2.0f; }
        }
        /// <summary>
        /// Distance from origin to edge along +-X
        /// </summary>
        public float HalfWidth
        {
            get { return halfWidth; }
            private set { halfWidth = value; }
        }
        /// <summary>
        /// Distance from origin to edge along +-Y
        /// </summary>
        public float HalfHeight
        {
            get { return halfHeight; }
            private set { halfHeight = value; }
        }

        #region Internals
        /// <summary>
        /// Cached transform from world space to plane space.
        /// In plane space, the rectangle is flat on the XY plane and centered at the origin.
        /// </summary>
        private Matrix WorldToPlane
        {
            get { return worldToPlane; }
            set { worldToPlane = value; }
        }
        /// <summary>
        /// Cached transform from plane space to world space.
        /// In plane space, the rectangle is flat on the XY plane and centered at the origin.
        /// </summary>
        private Matrix PlaneToWorld
        {
            get { return planeToWorld; }
            set { planeToWorld = value; }
        }
        #endregion Internals
        #endregion Accessors

        #region Public
        /// <summary>
        /// Return a clone of this object.
        /// </summary>
        /// <returns></returns>
        public override CollisionPrimitive Clone(GameActor owner)
        {
            Debug.Assert(false);
            return null;
        }
        /// <summary>
        /// Make self a copy of src.
        /// </summary>
        /// <param name="src"></param>
        public void CopyFrom(Rectangle src)
        {
            HalfWidth = src.HalfWidth;
            HalfHeight = src.HalfHeight;
            planeToLocal = src.planeToLocal;
            localToPlane = src.localToPlane;
            SetBone(src.Animator, src.ParentBone);
            DebugName = src.DebugName;
        }

        private static bool disabled = false;
        /// <summary>
        /// Evaluate collisions with the moving sphere.
        /// If returns true, there was a collision as described in collPrim,
        /// else no collision and collPrim untouched.
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="radius"></param>
        /// <param name="collPrim"></param>
        /// <returns></returns>
        public override bool Collide(Vector3 p0, Vector3 p1, float radius, ref CollInfo collPrim)
        {
            /// Bounding sphere test here!!!
            if (disabled)
                return false;

            if (CheckTouching(p0, radius, ref collPrim))
            {
                return true;
            }

            /// Find point on sphere tangent to plane parallel to this.
            Vector3 worldNormal = WorldNormal;
            Vector3 pTan0 = p0 - worldNormal * radius;
            Vector3 pTan1 = p1 - worldNormal * radius;

            /// Project it along velocity onto plane
            float stepDist = Vector3.Dot(pTan1 - pTan0, worldNormal);
            if (stepDist >= -Single.Epsilon) /// We're moving away from it
            {
                return false;
            }

            Vector3 center = planeToWorld.Translation;
            /// Check if we're already inside the plane
            float centerDist = Vector3.Dot(center, worldNormal) - Vector3.Dot(pTan0, worldNormal);
            if (centerDist > 0)
            {
                if (centerDist > radius)
                {
                    /// We're more than halfway embedded from the start, skip out
                    return false;
                }
                pTan0 += centerDist * worldNormal;
                pTan1 += centerDist * worldNormal;
                centerDist = 0.0f;
            }

            float t = centerDist / stepDist;
            if ((t < 0.0f) || (t > 1.0f))
            {
                return false;
            }

            Vector3 pProj = pTan0 + t * (pTan1 - pTan0);
            Vector3 pProjPlane = Vector3.Transform(pProj, worldToPlane);
            if (Inside(pProjPlane))
            {
                /// The tangent on the sphere smacked into a side.
                Vector3 centerAtT = p0 + t * (p1 - p0);

                return SetCollPrim(p0, centerAtT, pProj, centerAtT - pProj, ref collPrim);
            }

            /// pProj is where the sphere hits the plane.
            /// Find the closest point on the rect to pProj
            pProjPlane.X = MathHelper.Clamp(pProjPlane.X, -HalfWidth, HalfWidth);
            pProjPlane.Y = MathHelper.Clamp(pProjPlane.Y, -HalfHeight, HalfHeight);
            Vector3 pHit = Vector3.Transform(pProjPlane, planeToWorld);

            /// Try for an early out.
            if (Vector3.DistanceSquared(pHit, pProj) > radius * radius)
            {
                return false;
            }

            /// pHit now the point on the rect the sphere will make first contact,
            /// if indeed it's going to make contact.
            /// Now cast a ray back from pHit to see where on the sphere makes contact.
            if (RaySphere(p0, radius, pHit, pHit - (p1 - p0), ref t))
            {
                Vector3 centerAtT = p0 + t * (p1 - p0);
                return SetCollPrim(p0, centerAtT, pHit, centerAtT - pHit, ref collPrim);
            }

            return false;
        }

        #endregion Public

        #region Internal

        /// <summary>
        /// Find the closest point on the rectangle to the input point, in world space coords.
        /// </summary>
        /// <param name="worldPos"></param>
        /// <returns></returns>
        private Vector3 ClosestPoint(Vector3 worldPos)
        {
            Vector3 closePos = Vector3.Transform(worldPos, worldToPlane);
            closePos.X = MathHelper.Clamp(closePos.X, -HalfWidth, HalfWidth);
            closePos.Y = MathHelper.Clamp(closePos.Y, -HalfHeight, HalfHeight);
            closePos.Z = 0.0f;
            closePos = Vector3.Transform(closePos, planeToWorld);

            return closePos;
        }
        /// <summary>
        /// Return whether the plane space point is inside the rectangle.
        /// </summary>
        /// <param name="projPos"></param>
        /// <returns></returns>
        private bool Inside(Vector3 projPos)
        {
            return (Math.Abs(projPos.X) <= halfWidth)
                && (Math.Abs(projPos.Y) <= halfHeight);
        }

        /// <summary>
        /// Check for initial condition of contact. If so, fill in the collPrim
        /// and return true, else return false and leave collPrim alone.
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="radius"></param>
        /// <param name="collPrim"></param>
        /// <returns></returns>
        private bool CheckTouching(Vector3 worldPos, float radius, ref CollInfo collPrim)
        {
            Vector3 closePos = ClosestPoint(worldPos);

            if (Vector3.DistanceSquared(worldPos, closePos) <= radius * radius)
            {
                Vector3 normal = worldPos - closePos;
                if (Vector3.Dot(normal, WorldNormal) < 0.0f)
                {
                    normal = WorldNormal;
                }
                return SetCollPrimTouching(
                    worldPos,
                    worldPos,
                    closePos,
                    normal,
                    Vector3.Zero,
                    ref collPrim);
            }

            return false;
        }

        /// <summary>
        /// Cycle through to the next axis.
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        private Vector3 NextAxis(Vector3 axis)
        {
            return new Vector3(axis.Y, axis.Z, axis.X);
        }
        /// <summary>
        /// Create self from local space bounding box.
        /// </summary>
        /// <param name="localMin"></param>
        /// <param name="localMax"></param>
        /// <param name="axis"></param>
        public void Make(Vector3 localMin, Vector3 localMax, Vector3 axis)
        {
            if (Vector3.Dot(axis, new Vector3(1.0f, 1.0f, 1.0f)) >= 0.0f)
            {
                /// Project onto max plane
                localMin += axis * (localMax - localMin);
            }
            else
            {
                /// Project onto min plane
                localMax += axis * (localMax - localMin);
            }
            Vector3 right = NextAxis(axis);
            Vector3 up = NextAxis(right);
            planeToLocal = Matrix.Identity;
            planeToLocal.Backward = axis;
            planeToLocal.Right = right;
            planeToLocal.Up = up;

            localToPlane = Matrix.Transpose(planeToLocal);

            planeToLocal = Matrix.Multiply(
                planeToLocal, 
                Matrix.CreateTranslation((localMin + localMax) * 0.5f));
            localToPlane = Matrix.Multiply(
                Matrix.CreateTranslation(-(localMin + localMax) * 0.5f),
                localToPlane);

            halfWidth = (float)Math.Abs(Vector3.Dot(localMax - localMin, right) * 0.5f);
            halfHeight = (float)Math.Abs(Vector3.Dot(localMax - localMin, up) * 0.5f);

            /// Gen our bounding sphere based on our flattened dimensions.
            base.Make(localMin, localMax);
        }

        /// <summary>
        /// Update cached transforms based on the owning game thing and
        /// any controlling bones.
        /// </summary>
        public override void UpdateTransforms()
        {
            base.UpdateTransforms();

            worldToPlane = WorldToLocal * localToPlane;
            planeToWorld = planeToLocal * LocalToWorld;
        }

        /// <summary>
        /// Normal in world space, not cached.
        /// </summary>
        protected Vector3 WorldNormal
        {
            get { return Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitZ, PlaneToWorld)); }
        }

        #endregion Internal
    }
}
