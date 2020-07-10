// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using Boku.Base;

namespace Boku.SimWorld.Collision
{
    /// <summary>
    /// Cylinder is defined by a central axis along Z, 
    /// extending from local origin up a distance Length in Z,
    /// with an X/Y cross section that's a circle of radius Radius.
    /// 
    /// In local space the xyz bounds are [-Radius, Radius][-Radius, Radius][0, Length]
    /// </summary>
    internal class Cylinder : CollisionPrimitive
    {
        #region Members

        private float length = 0.0f;
        private float radius = 0.0f;

        #endregion Members

        #region Accessors

        /// <summary>
        /// Length from local origin to end.
        /// </summary>
        public float Length
        {
            get { return length; }
            protected set { length = value; }
        }
        /// <summary>
        /// Radius
        /// </summary>
        public float Radius
        {
            get { return radius; }
            protected set { radius = value; }
        }

        #endregion Accessors

        #region Public

        /// <summary>
        /// Return a clone of this object.
        /// </summary>
        /// <returns></returns>
        public override CollisionPrimitive Clone(GameActor owner)
        {
            Cylinder cyl = this.MemberwiseClone() as Cylinder;
            cyl.SetOwner(owner);
            return cyl;
        }

        private static bool disabled = false;
        /// <summary>
        /// Evaluate collisions with the moving sphere.
        /// If returns true, there was a collision as described in collPrim,
        /// else no collision and collPrim untouched.
        /// </summary>
        /// <param name="startPos"></param>
        /// <param name="endPos"></param>
        /// <param name="radius"></param>
        /// <param name="collPrim"></param>
        /// <returns></returns>
        public override bool Collide(Vector3 startPos, Vector3 endPos, float radius, ref CollInfo collPrim)
        {
            /// Three possibilities of a collision here.
            /// One is that the sphere hit our top or bottom cap
            /// Second is the sphere hits the side.
            /// Third is sphere hits edge between side and cap.
            /// 
            if (disabled)
                return false;

            if (CheckTouching(startPos, radius, ref collPrim))
            {
                return true;
            }

            Vector3 p0loc = Vector3.Transform(startPos, WorldToLocal);
            Vector3 p1loc = Vector3.Transform(endPos, WorldToLocal);

            float localRadius = WorldToLocalRadius(radius);

            /// Check the end caps first.
            /// Note that the cap checks look only for the sphere's tangent 
            /// hitting on the cap, not for further misses.
            float t = CheckCapTop(p0loc, p1loc, localRadius);
            if (IsHit(t))
            {
                Vector3 center = startPos + t * (endPos - startPos);

                Vector3 norm = Vector3.TransformNormal(Vector3.UnitZ, LocalToWorld);

                return SetCollPrim(
                    startPos,
                    center,
                    center - norm * radius,
                    norm,
                    ref collPrim);
            }

            t = CheckCapBot(p0loc, p1loc, localRadius);
            if (IsHit(t))
            {
                Vector3 center = startPos + t * (endPos - startPos);
                Vector3 norm = Vector3.TransformNormal(-Vector3.UnitZ, LocalToWorld);

                return SetCollPrim(
                    startPos,
                    center,
                    center - norm * radius,
                    norm,
                    ref collPrim);
            }
            
            /// Now check the sides.
            t = CheckSides(p0loc, p1loc, localRadius);
            if (IsHit(t))
            {
                Vector3 localCenter = p0loc + t * (p1loc - p0loc);
                if ((localCenter.Z >= 0) && (localCenter.Z <= Length))
                {
                    Vector3 center = startPos + t * (endPos - startPos);
                    Vector3 localNorm = new Vector3(localCenter.X, localCenter.Y, 0.0f);
                    Vector3 norm = Vector3.TransformNormal(localNorm, LocalToWorld);

                    return SetCollPrim(
                        startPos,
                        center,
                        center - norm * radius,
                        norm,
                        ref collPrim);
                }
            }
            
            return false;
        }

        #endregion Public

        #region Internal

        /// <summary>
        /// Return the closest point to the input in local space.
        /// </summary>
        /// <param name="p0Local"></param>
        /// <returns></returns>
        protected Vector3 ClosestPointLocal(Vector3 p0Local)
        {
            p0Local.Z = MathHelper.Clamp(p0Local.Z, 0, Length);

            Vector2 xy = new Vector2(p0Local.X, p0Local.Y);
            float length = xy.Length();
            xy /= length;
            xy *= Math.Min(length, Radius);

            p0Local.X = xy.X;
            p0Local.Y = xy.Y;

            return p0Local;
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

            Vector3 closestLocal = ClosestPointLocal(p0Local);

            Vector3 closest = Vector3.Transform(closestLocal, LocalToWorld);

            if (Vector3.DistanceSquared(closest, p0) <= radius * radius)
            {
                Vector3 localNorm = Vector3.Zero;
                if (p0Local.Z >= Length)
                {
                    localNorm = Vector3.UnitZ;
                }
                else if (p0Local.Z <= 0)
                {
                    localNorm = -Vector3.UnitZ;
                }
                else
                {
                    localNorm.X = p0Local.X;
                    localNorm.Y = p0Local.Y;
                }
                Vector3 norm = Vector3.TransformNormal(localNorm, LocalToWorld);

                /// Make sure the contact point is on the surface.
                if ((closestLocal.Z < Length) && (closestLocal.Z > 0))
                {
                    /// It's not on an end cap, check if it needs pushing out to the sides.
                    Vector2 xy = new Vector2(closestLocal.X, closestLocal.Y);
                    if (xy.LengthSquared() < Radius * Radius)
                    {
                        /// Yep, push it out.
                        xy.Normalize();
                        xy *= Radius;
                        closestLocal.X = xy.X;
                        closestLocal.Y = xy.Y;
                        closest = Vector3.Transform(closestLocal, LocalToWorld);
                    }
                }
                else
                {
                    // This case happens when we're within the infinite cylinder 
                    // but outside of either end cap.
                    return false;
                }
                return SetCollPrimTouching(
                    p0,
                    p0,
                    closest,
                    p0 - closest,
                    Vector3.Zero,
                    ref collPrim);
            }
            return false;
        }

        /// <summary>
        /// Check for sphere hitting side of cylinder. 
        /// </summary>
        /// <param name="p0loc"></param>
        /// <param name="p1loc"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        protected float CheckSides(Vector3 p0loc, Vector3 p1loc, float radius)
        {
            Vector2 p02 = new Vector2(p0loc.X, p0loc.Y);
            Vector2 p12 = new Vector2(p1loc.X, p1loc.Y);
            Vector2 dir = p12 - p02;

            double A = dir.LengthSquared();
            if (A < Double.Epsilon)
            {
                return -1.0f;
            }
            double B = 2.0f * Vector2.Dot(dir, p02);
            double R2 = radius * Radius;
            R2 *= R2;
            double C = p02.LengthSquared() - R2;

            double determ = B * B - 4.0f * A * C;
            if (determ < 0)
            {
                return -1.0f;
            }
            double t = (-B - Math.Sqrt(determ)) / (2.0f * A);

            return (float)t;
        }

        /// <summary>
        /// Check for sphere collision against top cap.
        /// </summary>
        /// <param name="p0loc"></param>
        /// <param name="p1loc"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        protected float CheckCapTop(Vector3 p0loc, Vector3 p1loc, float radius)
        {
            /// First check if the ending position is inside the plane, because otherwise
            /// we don't bother doing anything.
            p1loc.Z -= radius + Length;
            if (p1loc.Z < 0.0f)
            {
                /// Okay, ending position is on correct side of plane (inside)
                /// If our starting position is outside, great to go ahead.
                /// If it's inside, it may be precision errors, so if the center
                /// is outside, just compensate and pretend.
                if (p0loc.Z > Length)
                {
                    p0loc.Z -= radius + Length;
                    if (p0loc.Z < 0.0f)
                        p0loc.Z = 0.0f;

                    /// Project it along velocity onto plane
                    float t = p0loc.Z / (p0loc.Z - p1loc.Z);

                    Vector3 pPlnLoc = p0loc + t * (p1loc - p0loc);
                    if (pPlnLoc.LengthSquared() < Radius * Radius)
                    {
                        return t;
                    }
                }
            }

            return -1.0f;
        }

        /// <summary>
        /// Check for sphere collision against bottom cap.
        /// </summary>
        /// <param name="p0loc"></param>
        /// <param name="p1loc"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        protected float CheckCapBot(Vector3 p0loc, Vector3 p1loc, float radius)
        {
            /// First check if the ending position is inside the plane, because otherwise
            /// we don't bother doing anything.
            p1loc.Z += radius;
            if (p1loc.Z > 0.0f)
            {
                /// Okay, ending position is on correct side of plane (inside)
                /// If our starting position is outside, great to go ahead.
                /// If it's inside, it may be precision errors, so if the center
                /// is outside, just compensate and pretend.
                if (p0loc.Z <= 0)
                {
                    p0loc.Z += radius;
                    if (p0loc.Z > 0.0f)
                    {
                        p0loc.Z = 0.0f;
                    }

                    /// Project it along velocity onto plane
                    float t = p0loc.Z / (p0loc.Z - p1loc.Z);

                    Vector3 pPlnLoc = p0loc + t * (p1loc - p0loc);
                    if (pPlnLoc.LengthSquared() < Radius * Radius)
                    {
                        return t;
                    }
                }
            }

            return -1.0f;
        }

        /// <summary>
        /// See if the line paramter indicates a hit.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        protected bool IsHit(float t)
        {
            return (t >= 0.0f) && (t <= 1.0f);
        }

        /// <summary>
        /// Create self from local bounding box.
        /// </summary>
        /// <param name="localMin"></param>
        /// <param name="localMax"></param>
        public override void Make(Vector3 localMin, Vector3 localMax)
        {
            length = Math.Max(localMax.Z, -localMin.Z);
            radius = localMax.X;

            localMin.Z = -length;
            localMin.X = localMin.Y = radius;
            localMax.Y = radius;

            base.Make(localMin, localMax);

        }

        #endregion Internal

    }
}
