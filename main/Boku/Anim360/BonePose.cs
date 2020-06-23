/*
 * BonePose.cs
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
using Microsoft.Xna.Framework.Graphics;
using System.Collections.ObjectModel;

namespace Xclna.Xna.Animation
{

    /// <summary>
    /// A collection of BonePose objects that represent the bone transforms of a model
    /// as affected by animations.
    /// </summary>
    public class BonePoseCollection 
        : System.Collections.ObjectModel.ReadOnlyCollection<BonePose>
    {
        // A dictionary for quick access to bone poses based on bone name
        private Dictionary<string, BonePose> boneDict 
            = new Dictionary<string, BonePose>();

        // This class should not be externally instantiated
        internal BonePoseCollection(IList<BonePose> anims)
            :
            base(anims)
        {
            for (int i = 0; i < anims.Count; i++)
            {
                string boneName = anims[i].Name;
                if (boneName != null && boneName != "" && !boneDict.ContainsKey(boneName))
                {
                    boneDict.Add(boneName, anims[i]);
                }
            }
        }

        // Creates a set of bonepose objects from a skeleton
        internal static BonePoseCollection FromModelBoneCollection(
            ModelBoneCollection bones)
        {
            BonePose[] anims = new BonePose[bones.Count];
            for (int i = 0; i < bones.Count; i++)
            {
                if (bones[i].Parent==null)
                {
                    BonePose ba = new BonePose(
                        bones[i],
                        bones,
                        anims);

                }
            }

            return new BonePoseCollection(anims);
        }

        /// <summary>
        /// Computes the absolute transforms for the collection and copies
        /// the values.
        /// </summary>
        /// <param name="transforms">The array into which the transforms will be 
        /// copied.</param>
        public void CopyAbsoluteTransformsTo(Matrix[] transforms)
        {
            for (int i = 0; i < transforms.Length; i++)
            {
                if (i > 0) // not root
                {
                    // This works because the skeleton is always flattened;
                    // the parent index is always lower than the child index.
                    Matrix curTransform = this[i].GetCurrentTransform();
                    Matrix parentTransform = transforms[this[i].Parent.Index];
                    Vector3 currentTranslation = curTransform.Translation;
                    Matrix parentRotation = Matrix.CreateFromQuaternion(
                        Quaternion.CreateFromRotationMatrix(parentTransform));
                    Matrix currentRotation = Matrix.CreateFromQuaternion(
                        Quaternion.CreateFromRotationMatrix(curTransform));

                    currentTranslation = Vector3.Transform(currentTranslation,
                        parentRotation);
                    currentTranslation += parentTransform.Translation;
                    currentTranslation = parentTransform.Translation + curTransform.Translation;
                    
                    transforms[i] = currentRotation * parentRotation;
                    transforms[i] = curTransform * parentTransform;

   
                }
                else
                {
                    transforms[i] = this[i].GetCurrentTransform();
                }
            }
        }


        /// <summary>
        /// Gets a BonePose object.
        /// </summary>
        /// <param name="boneName">The name of the bone for which the BonePose 
        /// will be returned.</param>
        /// <returns>The BonePose associated with the bone name.</returns>
        public BonePose this[string boneName]
        {
            get { return boneDict[boneName]; }
        }

    }

    /// <summary>
    /// Represents the current pose of a model bone.
    /// </summary>
    public class BonePose
    {
        // Used when no animation is set
        private Matrix defaultMatrix;
        // Buffers for interpolation when blending
        private static Matrix returnMatrix, blendMatrix, currentMatrixBuffer;
        private int index;

        // The bone name
        private string name;
        private BonePose parent = null;
        private IAnimationController currentAnimation = null;
        private IAnimationController currentBlendAnimation = null;
        // THe amount to interpolate between the current animation and 
        // the current blend animation
        private float blendFactor = 0;
        private BonePoseCollection children;

        // True if the current animation contains a track for this bone
        private bool doesAnimContainChannel = false;
        // True if the current blend animation contains a track for this bone
        private bool doesBlendContainChannel = false;

        // Internal creation
        internal BonePose(ModelBone bone, 
            ModelBoneCollection bones,
            BonePose[] anims)
        {
            // Set the values according to the bone
            index = bone.Index;
            name = bone.Name;
            defaultMatrix = bone.Transform;
            if (bone.Parent != null)
                parent = anims[bone.Parent.Index];
            anims[index] = this;

            // Recurse on children
            List<BonePose> childList = new List<BonePose>();
            foreach (ModelBone child in bone.Children)
            {
                BonePose newChild = new BonePose(
                    bones[child.Index],
                    bones,
                    anims);
                childList.Add(newChild);
            }
            children = new BonePoseCollection(childList);
        }

        /// <summary>
        /// Gets the immediate children of the current bone.
        /// </summary>
        public BonePoseCollection Children
        {
            get { return children; }
        }

        // Finds the hierarchy for which this bone is the root
        private void FindHierarchy(List<BonePose> poses)
        {
            poses.Add(this);
            foreach (BonePose child in children)
            {
                child.FindHierarchy(poses);
            }
        }

        /// <summary>
        /// Finds a collection of bones that represents the tree of BonePoses with
        /// the current BonePose as the root.
        /// </summary>
        public BonePoseCollection GetHierarchy()
        {
 
                List<BonePose> poses = new List<BonePose>();
                FindHierarchy(poses);
                return new BonePoseCollection(poses);
            
        }


        /// <summary>
        /// Gets the bone's parent.
        /// </summary>
        public BonePose Parent
        {
            get { return parent; }
        }

        /// <summary>
        /// Gets the index of the bone.
        /// </summary>
        public int Index
        {
            get { return index; }
        }

        /// <summary>
        /// Gets the name of the bone.
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// Gets or sets the current animation that affects this bone.  If null,
        /// then DefaultTransform will be used for this bone's transform.
        /// </summary>
        public IAnimationController CurrentController
        {
            get { return currentAnimation; }
            set
            {
                // Don't do anything if the animation hasn't changed
                if (currentAnimation != value)
                {
                    if (value != null)
                    {
                        if (currentAnimation != null)
                            currentAnimation.AnimationTracksChanged -= current_AnimationTracksChanged;
                        if (name != null)
                        {
                            // Update info on whether or not the current anim
                            // contains a track for this bone
                            doesAnimContainChannel = 
                                value.ContainsAnimationTrack(this);
                            value.AnimationTracksChanged += new EventHandler(current_AnimationTracksChanged);
                        }
                    }
                    else // A null animation; use defaulttransform
                        doesAnimContainChannel = false;
                    currentAnimation = value;
                }
            }

        }

        void current_AnimationTracksChanged(object sender, EventArgs e)
        {
            doesAnimContainChannel = this.currentAnimation.ContainsAnimationTrack(this);
        }

        /// <summary>
        /// Gets or sets the blend animation that affects this bone.  If the value
        /// is null, then no blending will occur.
        /// </summary>
        public IAnimationController CurrentBlendController
        {
            get { return currentBlendAnimation; }
            set
            {
                // Don't do anything if the animation hasn't changed
                if (currentBlendAnimation != value)
                {

                    if (value != null)
                    {
                        if (currentBlendAnimation != null)
                            currentBlendAnimation.AnimationTracksChanged -= blend_AnimationTracksChanged;
                        if (name != null)
                        {
                            // Update info on whether or not the current anim
                            // contains a track for this bone
                            doesBlendContainChannel =
                                value.ContainsAnimationTrack(this);
                            value.AnimationTracksChanged += new EventHandler(blend_AnimationTracksChanged);
                        }
                    }
                    else
                        doesBlendContainChannel = false;
                    currentBlendAnimation = value;
                }
            }
        }

        void blend_AnimationTracksChanged(object sender, EventArgs e)
        {
            doesBlendContainChannel =
                this.currentBlendAnimation.ContainsAnimationTrack(this);
        }


        /// <summary>
        /// Gets or sets the amount to interpolate between the current animation and
        /// the current blend animation, if the current blend animation is not null
        /// </summary>
        public float BlendFactor
        {
            get { return blendFactor; }
            set { blendFactor = value; }
        }
        
        /// <summary>
        /// Represents the matrix used by the BonePose when it is not affected by
        /// an animation or when the animation does not contain a track for the bone.
        /// </summary>
        public Matrix DefaultTransform
        {
            get { return defaultMatrix; }
            set { defaultMatrix = value; }
        }

        /// <summary>
        /// Calculates the current transform, based on the animations, for the bone
        /// represented by the BonePose object.
        /// </summary>
        public Matrix GetCurrentTransform()
        {
 
            // If the bone is not currently affected by an animation
            if (currentAnimation == null || !doesAnimContainChannel)
            {
                // If the bone is affected by a blend animation,
                // blend the defaultTransform with the blend animation
                if (currentBlendAnimation != null && doesBlendContainChannel)
                {
                    blendMatrix = currentBlendAnimation.GetCurrentBoneTransform(this);
                    Util.SlerpMatrix(
                        ref defaultMatrix, 
                        ref blendMatrix, 
                        BlendFactor,
                        out returnMatrix);
                }
                    // else return the default transform
                else
                    return defaultMatrix;
            }
                // The bone is affected by an animation
            else
            {
                // Find the current transform in the animation for the bone
                currentMatrixBuffer = currentAnimation.GetCurrentBoneTransform(this);
                // If the bone is affected by a blend animation, blend the
                // current animation transform with the current blend animation
                // transform
                if (currentBlendAnimation != null && doesBlendContainChannel)
                {
                    blendMatrix = currentBlendAnimation.GetCurrentBoneTransform(this);
                    Util.SlerpMatrix(
                        ref currentMatrixBuffer,
                        ref blendMatrix, 
                        BlendFactor,
                        out returnMatrix);
                }
                    // Else just return the current animation transform
                else
                    return currentMatrixBuffer;
            }
            
            return returnMatrix;
            
        }
    }
}
