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

        /// <summary>
        /// Creates a new MultiBlendController
        /// </summary>
        /// <param name="game">The game.</param>
        public MultiBlendController(Game game) : base(game) 
        {
            game.Components.Add(this);
            controllerDict = new Dictionary<IAnimationController, float>();

        }

        private Dictionary<IAnimationController, float> controllerDict;
  

        #region IAnimationController Members

        /// <summary>
        /// Gets the current bone transform.
        /// </summary>
        /// <param name="pose">The pose.</param>
        /// <returns>The current transform of the bone.</returns>
        public Matrix GetCurrentBoneTransform(BonePose pose)
        {
            if (controllerDict.Count == 0)
            {
                return pose.DefaultTransform;
            }
            Matrix transform = new Matrix();
            Matrix m;
            foreach (KeyValuePair<IAnimationController, float> k in controllerDict)
            {
                if (k.Key.ContainsAnimationTrack(pose))
                {
                    m = k.Key.GetCurrentBoneTransform(pose);
                    transform += k.Value * m;
                }
                else
                {
                    transform += k.Value * pose.DefaultTransform;
                }
            }
            return transform;
        }

        /// <summary>
        /// Gets a dictionary that maps controllers to their weights.
        /// </summary>
        public Dictionary<IAnimationController, float> ControllerWeightDictionary
        {
            get { return controllerDict; }
        }

        /// <summary>
        /// Returns true if the controller can affect the bone.
        /// </summary>
        /// <param name="pose">The bone.</param>
        /// <returns>True if the controller can affect the bone.</returns>
        public bool ContainsAnimationTrack(BonePose pose)
        {
            return true;
        }


        private void OnAnimationTracksChanged(EventArgs e)
        {
            if (AnimationTracksChanged != null)
                AnimationTracksChanged(this, e);
        }

        /// <summary>
        /// Fired when different bones can be affected by the controller.
        /// </summary>
        public event EventHandler AnimationTracksChanged;

        #endregion

        #region IAnimationController Members




        #endregion
    }
}
