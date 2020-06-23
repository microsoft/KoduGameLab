/*
 * SkinInfo.cs
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
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework.Graphics;

namespace Xclna.Xna.Animation
{

    /// <summary>
    /// A structure that contains information for a bindpose skin offset.
    /// Represents the inverse bind pose for a bone.
    /// </summary>
    public struct SkinInfo
    {
        /// <summary>
        /// Creates a new SkinInfo.
        /// </summary>
        /// <param name="name">The name of the bone attached to the transform.</param>
        /// <param name="inverseBindPoseTransform">The inverse bind pose transform for the bone.</param>
        /// <param name="paletteIndex">The index in the MatrixPalette for the bone.</param>
        /// <param name="boneIndex">The index of the bone.</param>
        public SkinInfo(string name, Matrix inverseBindPoseTransform,
            int paletteIndex, int boneIndex)
        {
            BoneName = name;
            InverseBindPoseTransform = inverseBindPoseTransform;
            PaletteIndex = paletteIndex;
            BoneIndex = boneIndex;
        }
        /// <summary>
        /// The name of the bone attached to the transform.
        /// </summary>
        public readonly string BoneName;
        /// <summary>
        /// The transform for the bone.
        /// </summary>
        public readonly Matrix InverseBindPoseTransform;
        /// <summary>
        /// The index in the MatrixPalette for the bone.
        /// </summary>
        public readonly int PaletteIndex;
        /// <summary>
        /// The index of the bone.
        /// </summary>
        public readonly int BoneIndex;
    }

    /// <summary>
    /// A collection of SkinInfo objects.
    /// </summary>
    public class SkinInfoCollection : ReadOnlyCollection<SkinInfo>
    {
        private SkinInfoCollection(Model model, SkinInfo[] info)
            : base(info)
        {
 

        }

        internal SkinInfoCollection(IList<SkinInfo> info)
            : base(info)
        { }

        /// <summary>
        /// Finds the skinning info for the model and calculates the inverse
        /// reference poses required for animation.
        /// </summary>
        /// <param name="model">The model that contains the skinning info.</param>
        /// <returns>A collection of SkinInfo objects.</returns>
        public static SkinInfoCollection FromModel(Model model)
        {
            // This is created in the content pipeline
            Dictionary<string, object> modelTagData =
                    (Dictionary<string, object>)model.Tag;
            // An array of bone names that are used by the palette
            SkinInfoCollection[] info = (SkinInfoCollection[])modelTagData["SkinInfo"];
            return info[0];
        }



    }





}
