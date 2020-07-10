// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

/*
 * ModelAnimator.cs
 * Copyright (c) 2007 David Astle, Michael Nikonov
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

#define GI
#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System;
using System.Collections.ObjectModel;
#endregion

namespace Xclna.Xna.Animation
{

    /// <summary>
    /// Animates and draws a model that was processed with AnimatedModelProcessor
    /// </summary>
    public  class ModelAnimator : DrawableGameComponent
    {


        #region Member Variables
        // Stores the world transform for the animation controller.
        private Matrix world = Matrix.Identity;

        // Model to be animated
        private readonly Model model;

        // Skeletal structure containg transforms
        private BonePoseCollection bonePoses;

        private AnimationInfoCollection animations;

        // List of attached objects
        private IList<IAttachable> attachedObjects = new List<IAttachable>();

        // Store the number of meshes in the model
        private readonly int numMeshes;
        
        // Used to avoid reallocation
        private static Matrix skinTransform;
        // Buffer for storing absolute bone transforms
//        private Matrix[] pose;
        public Matrix[] pose;           // mbm made public during integration....
        // Array used for the matrix palette
        public Matrix[][] palette;      // mattmac made public so the render code can get at it
        // Inverse reference pose transforms
        public SkinInfoCollection[] skinInfo;

        /// <summary>
        /// Adding an arbitrary numeric id for sorting. Greater and lesser don't mean anything,
        /// we just want same animators to be grouped by the sort.
        /// </summary>
        private static long nextId = 0;
        public readonly long id = nextId++;
        #endregion

        #region General Properties


        /// <summary>
        /// Gets or sets the world matrix for the animation scene.
        /// </summary>
        public Matrix World
        {
            get
            {
                return world;
            }
            set
            {
                world = value;
            }
        }

        /// <summary>
        /// Gets the model associated with this controller.
        /// </summary>
        public Model Model
        { get { return model; } }

        /// <summary>
        /// Gets the animations that were loaded in from the content pipeline
        /// for this model.
        /// </summary>
        public AnimationInfoCollection Animations
        { get { return animations; } }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of ModelAnimator.
        /// </summary>
        /// <param name="game">The game to which this component will belong.</param>
        /// <param name="model">The model to be animated.</param>
        public ModelAnimator(Game game, Model model) : base(game)
        {
            this.model = model;

            animations = AnimationInfoCollection.FromModel(model);
            bonePoses = BonePoseCollection.FromModelBoneCollection(
                model.Bones);
            numMeshes = model.Meshes.Count;

            // The pose gives us the fully accumulated matrices for the entire model
            pose = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(pose);
            // Get all the skinning info for the model
            Dictionary<string, object> modelTagInfo = (Dictionary<string, object>)model.Tag;
            if (modelTagInfo == null)
                throw new Exception("Model Processor must subclass AnimatedModelProcessor.");
            skinInfo = (SkinInfoCollection[])modelTagInfo["SkinInfo"];
            if (skinInfo == null)
                throw new Exception("Model processor must pass skinning info through the tag.");

            // Now we will build an array of arrays - one array for each mesh containing all the matrices animating in that mesh
            palette = new Matrix[model.Meshes.Count][];
            for (int i = 0; i < skinInfo.Length; i++)
            {
                if (Util.IsSkinned(model.Meshes[i]))
                    palette[i] = new Matrix[skinInfo[i].Count];
                else
                    palette[i] = null;
            }
            // Update after AnimationController by default
            base.UpdateOrder = 1;
            if(game != null) 
                game.Components.Add(this);
   
        }
        #endregion
        /// <summary>
        /// Returns skinning information for a mesh.
        /// </summary>
        /// <param name="index">The index of the mesh.</param>
        /// <returns>Skinning information for the mesh.</returns>
        public SkinInfoCollection GetMeshSkinInfo(int index)
        {
            return skinInfo[index];
        }

        #region Animation and Update Routines

        /// <summary>
        /// Updates the animator by finding the current absolute transforms.
        /// </summary>
        public void Update()
        {
            bonePoses.CopyAbsoluteTransformsTo(pose);
            for (int i = 0; i < skinInfo.Length; i ++) 
            {
                if (palette[i] == null)
                    continue;
                SkinInfoCollection infoCollection = skinInfo[i];
                for (int j = 0; j < infoCollection.Count; ++j)
                //foreach (SkinInfo info in infoCollection)
                {
                    SkinInfo info = infoCollection[j];
                    skinTransform = info.InverseBindPoseTransform;
                    Matrix.Multiply(ref skinTransform, ref pose[info.BoneIndex],
                       out palette[i][info.PaletteIndex]);
                }
            }

            for (int i = 0; i < attachedObjects.Count; ++i)
            //foreach (IAttachable attached in attachedObjects)
            {
                IAttachable attached = attachedObjects[i];
                attached.CombinedTransform = attached.LocalTransform *
                    Matrix.Invert(pose[model.Meshes[0].ParentBone.Index]) *
                    pose[attached.AttachedBone.Index] * world;
            }

        }


        /// <summary>
        /// Copies the current absolute transforms to the specified array.
        /// </summary>
        /// <param name="transforms">The array to which the transforms will be copied.</param>
        public void CopyAbsoluteTransformsTo(Matrix[] transforms)
        {
            pose.CopyTo(transforms, 0);
        }

        /// <summary>
        /// Gets the current absolute transform for the given bone index.
        /// </summary>
        /// <param name="boneIndex"></param>
        /// <returns>The current absolute transform for the bone index.</returns>
        public Matrix GetAbsoluteTransform(int boneIndex)
        {
            return pose[boneIndex];
        }


        /// <summary>
        /// Gets a list of objects that are attached to a bone in the model.
        /// </summary>
        public IList<IAttachable> AttachedObjects
        {
            get { return attachedObjects; }
        }

        /// <summary>
        /// Gets the BonePoses associated with this ModelAnimator.
        /// </summary>
        public BonePoseCollection BonePoses
        {
            get { return bonePoses; }
        }

        #endregion
    }
}
