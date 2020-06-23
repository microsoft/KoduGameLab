/*
 * MultiBlendController.cs
 * Copyright (c) 2006 David Astle
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

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using System.Collections.ObjectModel;

namespace Xclna.Xna.Animation
{

    /// <summary>
    /// An IAnimationController that blends an arbitrary number of other controllers together
    /// using the formula M_final = sum(weight_i * M_i)
    /// </summary>
    public sealed class MultiBlendController : GameComponent, IAnimationController
    {
        #region Members

        private List<IAnimationController> animationControllers = null;

        private float weight = 1.0f;

        #endregion

        #region Accessors

        public float Weight
        {
            get { return weight; }
            set { weight = value; }
        }

        public List<IAnimationController> AnimationControllers
        {
            get { return animationControllers; }
        }

        #endregion

        #region Public

        /// <summary>
        /// Creates a new MultiBlendController
        /// </summary>
        /// <param name="game">The game.</param>
        public MultiBlendController(Game game) : base(game) 
        {
            if (game != null)
            {
                game.Components.Add(this);
            }

            animationControllers = new List<IAnimationController>();

        }   // end of c'tor


        /// <summary>
        /// Gets the current bone transform.
        /// </summary>
        /// <param name="pose">The pose.</param>
        /// <returns>The current transform of the bone.</returns>
        public Matrix GetCurrentBoneTransform(BonePose pose)
        {
            if (animationControllers.Count == 0)
            {
                return pose.DefaultTransform;
            }

            Matrix transform = new Matrix();
            Matrix m;

            for (int i = 0; i < animationControllers.Count; i++)
            {
                IAnimationController ac = animationControllers[i];

                if(ac.Weight > 0.0f)
                {
                    if(ac.ContainsAnimationTrack(pose))
                    {
                        m = ac.GetCurrentBoneTransform(pose);
                        transform += ac.Weight == 1.0f ? m : ac.Weight * m;
                    }
                    else
                    {
                        transform += ac.Weight == 1.0f ? pose.DefaultTransform : ac.Weight * pose.DefaultTransform;
                    }
                }
            }
            
            return transform;

        }   // end of GetCurrentBoneTransform()

        /// <summary>
        /// Returns true if the controller can affect the bone.
        /// </summary>
        /// <param name="pose">The bone.</param>
        /// <returns>True if the controller can affect the bone.</returns>
        public bool ContainsAnimationTrack(BonePose pose)
        {
            return true;
        }   // end of ContainsAnimationTrack()

        private void OnAnimationTracksChanged(EventArgs e)
        {
            if (AnimationTracksChanged != null)
            {
                AnimationTracksChanged(this, e);
            }
        }   // end of OnAnimationTracksChanged()

        /// <summary>
        /// Fired when different bones can be affected by the controller.
        /// </summary>
        public event EventHandler AnimationTracksChanged;

        /// <summary>
        /// Advances the current time in the animation.
        /// </summary>
        /// <param name="ticks">Elapsed time for this frame.  1 tick = 100 nanoseconds.</param>
        public void UpdateAnim(long ticks)
        {
            for (int i = 0; i < animationControllers.Count; i++)
            {
                IAnimationController ac = animationControllers[i];

                // This skips updates on animations that have a weighting of 0.
                // This saves CPU cycles but also gets the animations out of sync.  Is this a problem?
                if (ac.Weight > 0.0f)
                {
                    ac.UpdateAnim(ticks);
                }
            }

        }   // end of UpdateAnim()

        #endregion

    }
}
