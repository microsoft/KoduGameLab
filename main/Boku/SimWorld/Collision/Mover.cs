using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

using Boku.Base;
using Boku.Common;

namespace Boku.SimWorld.Collision
{
    internal class Mover : CollisionPrimitive
    {
        #region Members
        /// <summary>
        /// These are all in world space
        /// </summary>
        private Vector3 center = Vector3.Zero;
        private float radius = 0;
        private Vector3 delta = Vector3.Zero; // temp for the frame, starts off vel * dt.
        #endregion Members

        #region Accessors
        /// <summary>
        /// World space center
        /// </summary>
        public Vector3 Center
        {
            get { return center; }
            private set { center = value; }
        }
        /// <summary>
        /// World Space radius
        /// </summary>
        public float Radius
        {
            get { return radius; }
            set { radius = value; }
        }

        /// <summary>
        /// Temp for the frame, starts off vel * dt, but collisions can truncate or
        /// even bend this. At end of frame, final delta is applied to owner's position.
        /// </summary>
        public Vector3 Delta
        {
            get { return delta; }
            internal set { delta = value; }
        }

        /// <summary>
        /// Distance underneath to test for additional touched items (no collision, just touch)
        /// </summary>
        public float TouchCushion
        {
            get { return Owner.TouchCushion; }
        }
        #endregion Accessors

        #region Public
        /// <summary>
        /// Set owner and derive a debug name from it.
        /// </summary>
        /// <param name="owner"></param>
        public override void SetOwner(GameActor owner)
        {
            base.SetOwner(owner);
            DebugName = owner.GetType().ToString() + owner.uniqueNum;
        }

        /// <summary>
        /// Return a clone of this object.
        /// </summary>
        /// <returns></returns>
        public override CollisionPrimitive Clone(GameActor owner)
        {
            Mover mover = this.MemberwiseClone() as Mover;
            mover.SetOwner(owner);
            return mover;
        }

        /// <summary>
        /// Update cached transforms based on the owning game thing and
        /// any controlling bones.
        /// </summary>
        public override void UpdateTransforms()
        {
            base.UpdateTransforms();

            if (Owner != null)
            {
                Delta = Vector3.Zero;
                if (Owner.Movement.PrevPosition != Vector3.Zero)
                {
                    Delta = Owner.Movement.Position - Owner.Movement.PrevPosition;
                }

                Vector3 collCenter = Owner.CollisionCenter;
                collCenter = Vector3.Transform(collCenter, Owner.Movement.LocalMatrix);
                collCenter -= Delta;
                Center = collCenter;
            }
        }

        /// <summary>
        /// Evaluate collisions with the moving sphere.
        /// If returns true, there was a collision as described in collPrim,
        /// else no collision and collPrim untouched.
        /// </summary>
        /// <param name="startPositionOther"></param>
        /// <param name="p1"></param>
        /// <param name="radius"></param>
        /// <param name="collPrim"></param>
        /// <returns></returns>
        public override bool Collide(Vector3 startPositionOther, Vector3 endPositionOther, float radius, ref CollInfo collPrim)
        {
            Vector3 myRadii = Radius * Owner.SquashScale;

            /// Compensate for our motion.
            // We can then use this for "this" motion while "other" is now motionless.
            Vector3 endPositionThis = endPositionOther - Delta;

            Vector3 centerCenterDelta = startPositionOther - Center;
            float sumOfRadiiSquared = radius + Radius;
            sumOfRadiiSquared *= sumOfRadiiSquared;
            float centerCenterDistSquared = centerCenterDelta.LengthSquared();
            double C = centerCenterDistSquared - sumOfRadiiSquared;

            if (C <= 0.0)
            {
                /// Already touching
                return AlreadyTouching(startPositionOther, centerCenterDelta * Radius / (radius + Radius), ref collPrim);
            }

            /// The aren't touching. The only degeneracies to worry about 
            /// are that they aren't moving, they miss each other, or
            /// they hit outside range p0->p1.
            /// They aren't moving shows up as p1==p0.
            /// A miss will show up as a lack of real roots to the quadratic.
            /// Outside time range shows up as t < 0 || t > 1.

            Vector3 dir = endPositionThis - startPositionOther;
            double A = dir.LengthSquared();
            if (A < Single.Epsilon)
            {
                /// Not moving and not already touching.
                return false;
            }
            double B = 2.0 * Vector3.Dot(dir, centerCenterDelta);

            double determ = B * B - 4.0 * A * C;
            if (determ < 0.0)
            {
                /// Missed.
                return false;
            }
            double t = -B - Math.Sqrt(determ);
            t /= 2.0 * A;

            if ((t < 0) || (t > 1))
            {
                /// Outside interval
                return false;
            }
            Vector3 centerOld = startPositionOther + ((float)t) * (endPositionOther - startPositionOther);
            Vector3 normalOld = centerOld - Center;
            Vector3 contactOld = Center + normalOld * Radius / (Radius + radius);
            return SetCollPrim(
                startPositionOther,
                centerOld,
                contactOld,
                normalOld,
                Center + delta * (float)t,
                ref collPrim);
             
        }   // end of Collide()

        /// <summary>
        /// Specialized check against other Movers
        /// </summary>
        /// <param name="other"></param>
        /// <param name="collPrim"></param>
        /// <returns></returns>
        public bool Collide(Mover other, ref CollInfo collPrim)
        {
            float dt = Time.GameTimeFrameSeconds;

            Vector3 p0 = other.Owner.Movement.Position;
            Vector3 p1 = p0
                - Owner.Movement.Velocity * dt
                + other.Owner.Movement.Velocity * dt;

            return Collide(
                p0,
                p1,
                other.Radius,
                ref collPrim);
        }

        #endregion Public

        #region Internal

        /// <summary>
        /// Fill in a collPrim for initial condition already in contact.
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="del"></param>
        /// <param name="collPrim"></param>
        /// <returns></returns>
        private bool AlreadyTouching(Vector3 p0, Vector3 del, ref CollInfo collPrim)
        {
            Vector3 contact = Center + del;
            return SetCollPrimTouching(
                p0,
                p0,
                contact,
                p0 - contact,
                Center,
                ref collPrim);
        }

        #endregion Internal
    }
}
