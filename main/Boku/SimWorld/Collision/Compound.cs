// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;

using Boku.Base;
using Boku.Common;

namespace Boku.SimWorld.Collision
{
    public class Compound : CollisionPrimitive
    {
        #region Members
        private List<CollisionPrimitive> children = new List<CollisionPrimitive>();
        #endregion Members

        #region Accessors
        /// <summary>
        /// string identifier for debugging.
        /// </summary>
        public override string DebugName
        {
            get { return base.DebugName; }
            set 
            { 
                base.DebugName = value;

                for (int i = 0; i < children.Count; ++i)
                {
                    children[i].DebugName = value;
                }
            }
        }
        #endregion Accessors

        #region Public
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
            bool hit = false;

            float combinedRadius = WorldSphere.Radius + radius + Vector3.Distance(startPos, endPos) * 0.5f;
            float distance = Vector3.Distance(WorldSphere.Center, (startPos + endPos) * 0.5f);
            if (distance <= combinedRadius)
            {

                CollInfo test = new CollInfo();
                test.DistSq = Single.MaxValue;

                for (int i = 0; i < children.Count; ++i)
                {
                    if (children[i].Collide(startPos, endPos, radius, ref test))
                    {
                        if (test.DistSq <= collPrim.DistSq)
                        {
                            collPrim = test;
                            collPrim.TopPrimitive = this;
                            hit = true;
                        }
                    }
                }
            }
            else
            {
                /// Just a spot to put a breakpoint.
                hit = false;
            }
            return hit;
        }

        /// <summary>
        /// Return a clone of this object.
        /// </summary>
        /// <returns></returns>
        public override CollisionPrimitive Clone(GameActor owner)
        {
            Debug.Assert(false, "Clone operation not supported or needed for Compound type");
            return null;
        }

        /// <summary>
        /// Build self out of the list of children primitives. The list will be
        /// cloned, no one from source will be modified or kept.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="src"></param>
        /// <returns></returns>
        public bool Build(GameActor owner, List<CollisionPrimitive> src)
        {
            children.Clear();
            for (int i = 0; i < src.Count; ++i)
            {
                AddChildClone(owner, src[i]);
            }
            SetOwner(owner);
            return children.Count > 0;
        }
        /// <summary>
        /// Set the owner actor.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="parentBone"></param>
        public override void SetOwner(GameActor owner)
        {
            for(int i = 0; i < children.Count; ++i)
            {
                children[i].SetOwner(owner);
            }
            base.SetOwner(owner);
        }
        /// <summary>
        /// Update cached transforms based on the owning game thing and
        /// any controlling bones.
        /// </summary>
        public override void UpdateTransforms()
        {
            base.UpdateTransforms();

            for(int i = 0; i < children.Count; ++i)
            {
                children[i].UpdateTransforms();
            }
        }
        #endregion Public

        #region Internal

        private void AddChildClone(GameActor owner, CollisionPrimitive src)
        {
            CollisionPrimitive child = src.Clone(owner);

            if (children.Count > 0)
            {
                /// The world sphere for a primitive with no owner is
                /// the sphere when the root is at the origin. That is,
                /// the local sphere with any bone transforms folded in.
                /// That is our local space.
                LocalSphere = UnionBounds(LocalSphere, src.WorldSphere);
            }
            else
            {
                LocalSphere = src.WorldSphere;
            }
            children.Add(child);
        }

        private BoundingSphere UnionBounds(BoundingSphere s0, BoundingSphere s1)
        {
            Vector3 c0 = s0.Center;
            Vector3 c1 = s1.Center;
            Vector3 c1_c0 = c1 - c0;
            float length = c1_c0.Length();
            c1_c0 /= length;

            Vector3 p0 = c0 - c1_c0 * Math.Max(s0.Radius, s1.Radius - length);
            Vector3 p1 = c1 + c1_c0 * Math.Max(s1.Radius, s0.Radius - length);

            Vector3 center = (p0 + p1) * 0.5f;
            float radius = Vector3.Distance(center, p0);

            return new BoundingSphere(center, radius);
        }
        #endregion Internal
    }
}
