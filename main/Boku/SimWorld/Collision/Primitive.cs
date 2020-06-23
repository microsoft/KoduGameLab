using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;
using Boku.Animatics;

namespace Boku.SimWorld.Collision
{
    /// <summary>
    /// Abstract base class for collision primitives.
    /// </summary>
    public abstract class CollisionPrimitive : ArbitraryComparable
    {
        #region Members

        private GameActor owner;
        private AnimationInstance animator;
        private ModelBone parentBone;

        private Matrix localToWorld;
        private Matrix worldToLocal;

        private float worldToLocalRadius = 1.0f;

        private BoundingSphere localSphere;
        private BoundingSphere worldSphere; /// cached localSphere transformed
                                            /// 
        private string debugName = "Unknown";

        #endregion Members

        #region Accessors

        /// <summary>
        /// The GameActor I represent some or all of.
        /// </summary>
        public GameActor Owner
        {
            get { return owner; }
            protected set { owner = value; }
        }
        /// <summary>
        /// Transform from my local space to world space, derived from
        /// Owner and ParentBone.
        /// </summary>
        public Matrix LocalToWorld
        {
            get { return localToWorld; }
            private set { localToWorld = value; }
        }
        /// <summary>
        /// Transform from world space to my local, derived from Owner
        /// and ParentBone.
        /// </summary>
        public Matrix WorldToLocal
        {
            get { return worldToLocal; }
            private set { worldToLocal = value; }
        }
        /// <summary>
        /// Bounding sphere in local space. Includes any children.
        /// </summary>
        public BoundingSphere LocalSphere
        {
            get { return localSphere; }
            protected set { localSphere = value; }
        }
        /// <summary>
        /// World space bounding sphere.
        /// </summary>
        public BoundingSphere WorldSphere
        {
            get { return worldSphere; }
        }
        /// <summary>
        /// Bone I'm attached to.
        /// </summary>
        public ModelBone ParentBone
        {
            get { return parentBone; }
        }
        /// <summary>
        /// Animator owning the bone I'm attached to.
        /// </summary>
        public AnimationInstance Animator
        {
            get { return animator; }
        }
        /// <summary>
        /// string identifier for debugging.
        /// </summary>
        public virtual string DebugName
        {
            get { return debugName; }
            set { debugName = value; }
        }

        #endregion Accessors

        #region Public

        #region Abstract

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
        public abstract bool Collide(Vector3 startPos, Vector3 endPos, float radius, ref CollInfo collPrim);

        /// <summary>
        /// Return a clone of this object.
        /// </summary>
        /// <returns></returns>
        public abstract CollisionPrimitive Clone(GameActor owner);

        #endregion Abstract


        /// <summary>
        /// Set the owner actor.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="parentBone"></param>
        public virtual void SetOwner(GameActor owner)
        {
            this.owner = owner;
            int lod = Math.Max(0, owner.Animators.Count - 1);
            this.animator = owner.Animators[lod];

            UpdateTransforms();
        }
        /// <summary>
        /// Setup bone attachment.
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="parentBone"></param>
        public virtual void SetBone(AnimationInstance animator, ModelBone parentBone)
        {
            this.parentBone = parentBone;
            this.animator = animator;

            UpdateTransforms();
        }
        /// <summary>
        /// Update cached transforms based on the owning game thing and
        /// any controlling bones.
        /// </summary>
        public virtual void UpdateTransforms()
        {
            Matrix rootToWorld = Owner != null 
                ? Owner.Movement.LocalMatrix 
                : Matrix.Identity;
            Matrix worldToRoot = Matrix.Invert(rootToWorld);

            if (parentBone != null)
            {
                Matrix localToRoot = animator != null
                    ? animator.LocalToWorld(parentBone.Index)
                    : parentBone.Transform;

                if (Owner != null)
                {
                    localToRoot = localToRoot * Matrix.CreateScale(Owner.ReScale);
                }

                Matrix rootToLocal = Matrix.Invert(localToRoot);

                LocalToWorld = localToRoot * rootToWorld;
                WorldToLocal = worldToRoot * rootToLocal;

                worldToLocalRadius = WorldToLocal.Right.Length();
            }
            else
            {
                if (Owner != null)
                {
                    LocalToWorld = Matrix.CreateScale(Owner.ReScale) * rootToWorld;
                    WorldToLocal = worldToRoot * Matrix.CreateScale(1.0f / Owner.ReScale);

                    worldToLocalRadius = WorldToLocal.Right.Length();
                }
                else
                {
                    LocalToWorld = rootToWorld;
                    WorldToLocal = worldToRoot;
                }
            }
            worldSphere.Radius = localSphere.Radius / worldToLocalRadius;
            worldSphere.Center = Vector3.Transform(localSphere.Center, LocalToWorld);
        }

        public float WorldToLocalRadius(float worldRadius)
        {
            return worldRadius * worldToLocalRadius;
        }

        /// <summary>
        /// Publicly accessible ray/sphere test. Determines how far along the ray segment the ray hits
        /// the sphere, if at all.
        /// Note, a ray that starts inside it the sphere is assumed to immediately hit.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="p0">start of ray</param>
        /// <param name="p1">end of ray</param>
        /// <param name="t"></param>
        /// <returns>True is ray hits.</returns>
        public static bool RaySphere(Vector3 center, float radius, Vector3 p0, Vector3 p1, ref float t)
        {
            Vector3 dir = p1 - p0;

            double a = dir.LengthSquared();
            if (a <= 0.000001)
            {
                // There is no movement along a ray, either p0 is in the sphere or 
                // there is no hit.
                if (Vector3.DistanceSquared(center, p0) <= radius * radius)
                {
                    // Inside the sphere, immediate hit.
                    t = 0;
                    return true;
                }
                // Outside the sphere, no hit.
                return false;
            }

            double b = 2.0 * Vector3.Dot(dir, p0 - center);
            double c = Vector3.DistanceSquared(p0, center) - radius * radius;

            Debug.Assert(a > 0.0, "degenerate ray should have been filtered out already");

            double det = b * b - 4.0 * a * c;
            if (det < 0)
            {
                return false;
            }
            t = (float)((-b - Math.Sqrt(det)) / (2.0 * a));

            return (t >= 0.0f) && (t <= 1.0f);
        }   // end of RaySphere()

        /// <summary>
        /// Test a ray segment against an axis-aligned ellipsoid.
        /// 
        /// A ray inside the ellipsoid will return an immediate hit.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radii"></param>
        /// <param name="rayStart">ray start</param>
        /// <param name="rayEnd">ray end</param>
        /// <param name="hitPosition">Position of hit, if any.</param>
        /// <param name="hitNormal">Normal at hit point, if any.</param>
        /// <returns>True if hit.</returns>
        public static bool RayEllipsoid(Vector3 center, Vector3 radii, Vector3 rayStart, Vector3 rayEnd, ref Vector3 hitPosition, ref Vector3 hitNormal)
        {
            bool hit = false;

            // Adjust ray inputs so that ellipsoid is at origin.
            rayStart -= center;
            rayEnd -= center;

            // Adjust ray inputs to make ellipse into unit sphere.
            rayStart /= radii;
            rayEnd /= radii;

            // We now test the ray against the unit sphere centered at the origin.
            float t = -1;
            if (RaySphere(Vector3.Zero, 1.0f, rayStart, rayEnd, ref t))
            {
                hit = true;

                hitPosition = rayStart + t * (rayEnd - rayStart);
                hitNormal = hitPosition;    // Works since this is a unit sphere.

                // Undo scaling and offset.
                hitPosition = hitPosition * radii + center;
                hitNormal = hitNormal / radii;
                hitNormal.Normalize();
            }

            return hit;
        }   // end of RayEllipsoid()

        #endregion Public

        #region Internal

        /// <summary>
        /// Construct self from local space bounding box.
        /// </summary>
        /// <param name="localMin"></param>
        /// <param name="localMax"></param>
        public virtual void Make(Vector3 localMin, Vector3 localMax)
        {
            localSphere.Center = (localMin + localMax) * 0.5f;
            localSphere.Radius = Vector3.Distance(localMin, localMax) * 0.5f;
        }

        /// <summary>
        /// Helper func to set up a collision reporting struct.
        /// For use when two bodies already touching at beginning of frame.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="center"></param>
        /// <param name="contact"></param>
        /// <param name="normal"></param>
        /// <param name="collPrim"></param>
        /// <returns></returns>
        public bool SetCollPrimTouching(
            Vector3 from,
            Vector3 center,
            Vector3 contact,
            Vector3 normal,
            Vector3 struck,
            ref CollInfo collPrim)
        {
            SetCollPrim(from, center, contact, normal, struck, ref collPrim);
            collPrim.Touching = true;
            return true;
        }
        /// <summary>
        /// Helper func to set up a collision reporting struct.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="center"></param>
        /// <param name="contact"></param>
        /// <param name="normal"></param>
        /// <param name="collPrim"></param>
        /// <returns></returns>
        protected bool SetCollPrim(
            Vector3 from,
            Vector3 center,
            Vector3 contact,
            Vector3 normal,
            ref CollInfo collPrim)
        {
            return SetCollPrim(
                from,
                center,
                contact,
                normal,
                Vector3.Zero,
                ref collPrim);
        }
        /// <summary>
        /// Helper func to set up a collision reporting struct.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="center"></param>
        /// <param name="contact"></param>
        /// <param name="normal"></param>
        /// <param name="struck"></param>
        /// <param name="collPrim"></param>
        /// <returns></returns>
        public bool SetCollPrim(
            Vector3 from, 
            Vector3 center, 
            Vector3 contact, 
            Vector3 normal,
            Vector3 struck,
            ref CollInfo collPrim)
        {
            collPrim.DistSq = Vector3.DistanceSquared(from, contact);
            collPrim.Center = center;
            collPrim.Contact = contact;
            if (normal.LengthSquared() == 0.0f)
            {
                collPrim.Normal = Vector3.UnitZ;
            }
            else
            {
                collPrim.Normal = normal;
            }
            collPrim.Struck = struck;
            collPrim.Touching = false;
            collPrim.LowPrimitive = this;
            collPrim.TopPrimitive = this;

            return true;
        }
        #endregion Internal
    }
}
