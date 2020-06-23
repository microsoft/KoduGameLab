using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;

namespace Boku.Animatics
{
    public class BlendController : BaseController
    {
        #region Members
        /// <summary>
        /// The animations we are blending together.
        /// </summary>
        private List<BaseController> animations = new List<BaseController>();

        /// <summary>
        /// Scratch spaces
        /// </summary>
        private static Matrix[] _childLocalToParent;
        private static Matrix[] _defaults;

        private readonly string name; // currently debug only.
        #endregion Members

        #region Accessors
        #endregion Accessors

        #region Public
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="inst"></param>
        /// <param name="name"></param>
        public BlendController(AnimationInstance inst, string name)
        {
            this.name = name;
            this.Weight = 1.0f;
        }

        /// <summary>
        /// Advance the clock by ticks. No heavy lifting, just update internal clock state.
        /// Mostly just propagate to children, but our CurrentTicks is also used by owner
        /// to keep track of when to sync up transforms.
        /// </summary>
        /// <param name="advTicks"></param>
        public override void Update(long time)
        {
            for (int i = 0; i < animations.Count; ++i)
            {
                BaseController anim = animations[i];
                anim.Update(time);
            }
            CurrentTicks += time;
        }

        /// <summary>
        /// Compute the localToParent values we know about, leaving the rest alone.
        /// localToParent has already been populated with default values.
        /// </summary>
        /// <param name="inst"></param>
        /// <param name="localToParent"></param>
        internal override void GetTransforms(AnimationInstance inst, Matrix[] localToParent)
        {
            int numBones = inst.NumBones;

            CheckLocalToParent(localToParent, numBones);

            for (int i = 0; i < animations.Count; ++i)
            {
                BaseController anim = animations[i];

                float wgt = anim.Weight;
                if (wgt > 0)
                {
                    ReZero(numBones);
                    anim.GetTransforms(inst, _childLocalToParent);

                    for (int j = 0; j < numBones; ++j)
                    {
                        if (_childLocalToParent[j].M44 != 0)
                        {
                            localToParent[j] += _childLocalToParent[j] * wgt;
                        }
                        else
                        {
                            localToParent[j] += _defaults[j] * wgt;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add another controller to the list.
        /// </summary>
        /// <param name="anim"></param>
        public void Add(BaseController anim)
        {
            animations.Add(anim);
        }
        /// <summary>
        /// Remove a controller from the list. You generally don't want to do this,
        /// just set the controller's weight to zero.
        /// </summary>
        /// <param name="anim"></param>
        public void RemoveController(BaseController anim)
        {
            animations.Remove(anim);
        }
        #endregion Public

        #region Internal
        /// <summary>
        /// Make sure we have enough scratch space. Copy the input defaults from localToParent
        /// and then zero it out so we can accumulate into it.
        /// </summary>
        /// <param name="localToParent"></param>
        /// <param name="numBones"></param>
        private void CheckLocalToParent(Matrix[] localToParent, int numBones)
        {
            if ((_childLocalToParent == null) || (_childLocalToParent.Length < numBones))
            {
                _childLocalToParent = new Matrix[numBones];
                _defaults = new Matrix[numBones];
            }
            Debug.Assert(_childLocalToParent.Length == _defaults.Length);

            CopyDefaults(localToParent, numBones);

        }
        /// <summary>
        /// Copy defaults off from localToParent, then zero it out.
        /// </summary>
        /// <param name="localToParent"></param>
        /// <param name="num"></param>
        private void CopyDefaults(Matrix[] localToParent, int num)
        {
            Matrix zero = Matrix.CreateScale(0.0f);
            zero.M44 = 0.0f;

            for (int i = 0; i < num; ++i)
            {
                _defaults[i] = localToParent[i];
                localToParent[i] = zero;
            }
        }
        /// <summary>
        /// Zero out the child scratch matrix space.
        /// </summary>
        /// <param name="num"></param>
        private void ReZero(int num)
        {
            Matrix zero = Matrix.CreateScale(0.0f);
            zero.M44 = 0.0f;

            for (int i = 0; i < num; ++i)
            {
                _childLocalToParent[i] = zero;
            }
        }
        #endregion Internal
    }
}
