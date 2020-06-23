/*
 * IAnimationController.cs
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
    /// An interface used by BonePose that allows an animation to affect the bone
    /// as a function of time.
    /// </summary>
    public interface IAnimationController
    {
        /// <summary>
        /// Gets the current transform for the given BonePose object in the animation.
        /// This is only called when a bone pose is affected by the current animation.
        /// </summary>
        /// <param name="pose">The BonePose object querying for the current transform in
        /// the animation.</param>
        /// <returns>The current transform of the bone.</returns>
        Matrix GetCurrentBoneTransform(BonePose pose);
        /// <summary>
        /// Gets a value determining whether the animation can potentially affect the
        /// given BonePose.
        /// </summary>
        /// <param name="pose">The BonePose to test.</param>
        /// <returns>True if the animation can affect the bone and contains a track
        /// for it.</returns>
        bool ContainsAnimationTrack(BonePose pose);

        /// <summary>
        /// Raised when the animation tracks have changed so that different bones are affect.
        /// </summary>
        event EventHandler AnimationTracksChanged;
    }


}
