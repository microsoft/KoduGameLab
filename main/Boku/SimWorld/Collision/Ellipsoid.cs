using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using Boku.Base;

namespace Boku.SimWorld.Collision
{
    internal class Ellipsoid : CollisionPrimitive
    {
        #region Members
        private Vector3 radii = new Vector3(1.0f, 1.0f, 1.0f);
        #endregion Members

        #region Accessors
        /// <summary>
        /// Radius in local space
        /// </summary>
        public Vector3 Radii
        {
            get { return radii; }
            protected set { radii = value; }
        }
        #endregion Accessors

        #region Public
        /// <summary>
        /// Return a clone of this object.
        /// </summary>
        /// <returns></returns>
        public override CollisionPrimitive Clone(GameActor owner)
        {
            Ellipsoid ell = this.MemberwiseClone() as Ellipsoid;
            ell.SetOwner(owner);
            return ell;
        }

        static bool disabled = false;
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
            if (disabled)
                return false;

            //p1 = p0;
            //p1.Y -= 1.0f;

            /// Check against bounding sphere.

            /// Check degenerate case where we are already in contact
            if (CheckTouching(p0, radius, ref collPrim))
            {
                return true;
            }

            /// Move the problem to the space in which the expanded ellipse is a unit sphere
            /// centered at the origin.
            /// 
            /// Expand our radii by radius
            float localRadius = WorldToLocalRadius(radius);
            Matrix ellipseToSphere = Matrix.CreateScale(
                1.0f / (Radii.X + localRadius),
                1.0f / (Radii.X + localRadius),
                1.0f / (Radii.Z + localRadius));

            Matrix worldToSphere = WorldToLocal * ellipseToSphere;

            /// Convert ray test to expanded ellipsoid space
            Vector3 p0sph = Vector3.Transform(p0, worldToSphere);
            Vector3 p1sph = Vector3.Transform(p1, worldToSphere);

            /// If we have a hit...
            float t = 0.0f;
            bool hit = RaySphere(Vector3.Zero, 1.0f, p0sph, p1sph, ref t);
            if(hit)
            {
                Matrix sphereToEllipse = Matrix.CreateScale(
                    Radii.X + localRadius,
                    Radii.Y + localRadius,
                    Radii.Z + localRadius);

                Matrix sphereToWorld = sphereToEllipse * LocalToWorld;

                Vector3 hitWorld = p0 + t * (p1 - p0);

                /// Get the hit point and normal in sphere space
                Vector3 hitsph = Vector3.Transform(hitWorld, worldToSphere);
                Matrix normSphereToLocal = Matrix.CreateScale(
                    ellipseToSphere.M11 * ellipseToSphere.M11,
                    ellipseToSphere.M22 * ellipseToSphere.M22,
                    ellipseToSphere.M33 * ellipseToSphere.M33);
                Vector3 localNormal = Vector3.TransformNormal(hitsph, normSphereToLocal);

                /// Transform the hit point and normal back to world space
                Vector3 normWorld = Vector3.TransformNormal(localNormal, LocalToWorld);
                normWorld.Normalize();
                
                /// Move the hit point into the ellipse by the normalized hit normal 
                /// times the radius.

                SetCollPrim(
                    p0,
                    hitWorld,
                    hitWorld - normWorld * radius,
                    normWorld,
                    ref collPrim);

                return true;
            }

            return false;
        }

        #endregion Public

        #region Internal
        /// <summary>
        /// Create self from local bounding box.
        /// </summary>
        /// <param name="localMin"></param>
        /// <param name="localMax"></param>
        public override void Make(Vector3 localMin, Vector3 localMax)
        {
            base.Make(localMin, localMax);

            radii.X = localMax.X;
            radii.Y = localMax.Y;
            radii.Z = localMax.Z;
        }

        /// <summary>
        /// Check for initial condition of contact. If so, fill in the collPrim
        /// and return true, else return false and leave collPrim alone.
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="radius"></param>
        /// <param name="collPrim"></param>
        /// <returns></returns>
        protected bool CheckTouching(Vector3 p0, float radius, ref CollInfo collPrim)
        {
            Vector3 p0Local = Vector3.Transform(p0, WorldToLocal);

            Vector3 closestLocal = p0Local;
            float lengthSq = p0Local.X * p0Local.X / (Radii.X * Radii.X)
                        + p0Local.Y * p0Local.Y / (Radii.Y * Radii.Y)
                        + p0Local.Z * p0Local.Z / (Radii.Z * Radii.Z);
            if (lengthSq > 1.0f)
            {
                float invLength = (float)(1.0 / Math.Sqrt(lengthSq));
                closestLocal *= invLength;
            }
            Vector3 closest = Vector3.Transform(closestLocal, LocalToWorld);

            if (Vector3.DistanceSquared(closest, p0) <= radius * radius)
            {
                Vector3 normal = lengthSq < 1.0f 
                    ? p0 - LocalToWorld.Translation 
                    : p0 - closest;
                return SetCollPrimTouching(
                    p0,
                    p0,
                    closest,
                    normal,
                    Vector3.Zero,
                    ref collPrim);
            }
            return false;
        }
        #endregion Internal
    }
}
