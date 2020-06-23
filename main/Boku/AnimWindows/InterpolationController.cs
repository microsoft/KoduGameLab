/*
 * InterpolationController.cs
 * Copyright (c) 2007 David Astle
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
using Microsoft.Xna.Framework.Graphics;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

namespace Xclna.Xna.Animation
{
    /// <summary>
    /// An interpolation method.
    /// </summary>
    public enum InterpolationMethod
    {
        /// <summary>
        /// No interpolation.
        /// </summary>
        None,
        /// <summary>
        /// Linear interpolation.
        /// </summary>
        Linear,
        /// <summary>
        /// Spline based interpolation.  Higher quality than linear but more expensive.
        /// </summary>
        SplineBased
    }

    /// <summary>
    /// A controller that performs interpolation during runtime.
    /// </summary>
    public class InterpolationController : AnimationController
    {
        private static Matrix curTransform, nextTransform, transform;
        private InterpolationMethod interpMethod;

        /// <summary>
        /// Creats a new InterpolationController.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="source">The source animation.</param>
        /// <param name="interpMethod">The interpolation method.</param>
        public InterpolationController(Game game, AnimationInfo source, InterpolationMethod interpMethod)
            : base(game, source)
        {
            this.interpMethod = interpMethod;
        }

        /// <summary>
        /// Returns the current transform for a bone.
        /// </summary>
        /// <param name="pose">The bone.</param>
        /// <returns>The bone's current transform.</returns>
        public override Matrix GetCurrentBoneTransform(BonePose pose)
        {
            BoneKeyframeCollection channel = base.AnimationSource.AnimationChannels[
                pose.Name];
            int curIndex = channel.GetIndexByTime(base.ElapsedTime);
            if (interpMethod == InterpolationMethod.None)
            {
                return channel[curIndex].Transform;
            }
            int nextIndex = curIndex + 1;
            if (nextIndex >= channel.Count)
            {
                return channel[curIndex].Transform;
            }
            // Numerator of the interpolation factor
            double interpNumerator = (double)(ElapsedTime - channel[curIndex].Time);
            // Denominator of the interpolation factor
            double interpDenom = (double)(channel[nextIndex].Time
                - channel[curIndex].Time);
            // The interpolation factor, or amount to interpolate between the current
            // and next frame
            double interpAmount = interpNumerator / interpDenom;

            curTransform = channel[curIndex].Transform;
            nextTransform = channel[nextIndex].Transform;
            if (interpMethod == InterpolationMethod.Linear)
            {
                Matrix.Lerp(ref curTransform, ref nextTransform,
                    (float)interpAmount, out transform);
            }
            else
            {
                Util.SlerpMatrix(ref curTransform, ref nextTransform,
                    (float)interpAmount, out transform);
            }
            return transform;
        }

        /// <summary>
        /// Gets or sets the interpolation method.
        /// </summary>
        public InterpolationMethod InterpolationMethod
        {
            get { return interpMethod; }
            set { interpMethod = value; }
        }
    }

}
