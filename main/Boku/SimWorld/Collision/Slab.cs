using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;
using Boku.Animatics;

namespace Boku.SimWorld.Collision
{
    /// <summary>
    /// Compound primitive consisting of 6 sides, forming a rectangular box.
    /// </summary>
    internal class Slab : CollisionPrimitive
    {
        #region Members
        Rectangle[] sides = new Rectangle[6];
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
                sides[0].DebugName = value + "-X";
                sides[1].DebugName = value + "+X";
                sides[2].DebugName = value + "-Y";
                sides[3].DebugName = value + "+Y";
                sides[4].DebugName = value + "-Z";
                sides[5].DebugName = value + "+Z";
            }
        }
        #endregion Accessors

        #region Public
        /// <summary>
        /// Constructor
        /// </summary>
        public Slab()
        {
            for (int i = 0; i < 6; ++i)
            {
                sides[i] = new Rectangle();
            }
        }
        /// <summary>
        /// Return a clone of this object.
        /// </summary>
        /// <returns></returns>
        public override CollisionPrimitive Clone(GameActor owner)
        {
            Slab slab = new Slab();
            for (int i = 0; i < 6; ++i)
            {
                slab.sides[i].CopyFrom(sides[i]);
            }
            slab.SetOwner(owner);
            return slab;
        }
        /// <summary>
        /// Set the owner actor.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="parentBone"></param>
        public override void SetOwner(GameActor owner)
        {
            foreach (Rectangle rect in sides)
            {
                rect.SetOwner(owner);
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

            foreach (Rectangle rect in sides)
            {
                rect.UpdateTransforms();
            }
        }

        /// <summary>
        /// Setup bone attachment.
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="parentBone"></param>
        public override void SetBone(AnimationInstance animator, ModelBone parentBone)
        {
            base.SetBone(animator, parentBone);

            foreach (Rectangle rect in sides)
            {
                rect.SetBone(animator, parentBone);
            }
        }

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
            ///Check sphere first.
            
            CollInfo testPrim = new CollInfo();
            testPrim.DistSq = Single.MaxValue;
            bool hit = false;
            foreach (Rectangle rect in sides)
            {
                if (rect.Collide(p0, p1, radius, ref testPrim))
                {
                    if (testPrim.DistSq < collPrim.DistSq)
                    {
                        hit = true;
                        collPrim = testPrim;
                        collPrim.TopPrimitive = this;
                    }
                }
            }
            return hit;
        }

        #endregion Public

        #region Internal

        /// <summary>
        /// Create self (and children) from local space bounding box.
        /// </summary>
        /// <param name="localMin"></param>
        /// <param name="localMax"></param>
        public override void Make(Vector3 localMin, Vector3 localMax)
        {
            base.Make(localMin, localMax);

            sides[0].Make(localMin, localMax, -Vector3.UnitX);
            sides[1].Make(localMin, localMax, Vector3.UnitX);

            sides[2].Make(localMin, localMax, -Vector3.UnitY);
            sides[3].Make(localMin, localMax, Vector3.UnitY);

            sides[4].Make(localMin, localMax, -Vector3.UnitZ);
            sides[5].Make(localMin, localMax, Vector3.UnitZ);

            sides[0].DebugName += "-X";
            sides[1].DebugName += "+X";
            sides[2].DebugName += "-Y";
            sides[3].DebugName += "+Y";
            sides[4].DebugName += "-Z";
            sides[5].DebugName += "+Z";
        }

        #endregion Internal
    }
}
