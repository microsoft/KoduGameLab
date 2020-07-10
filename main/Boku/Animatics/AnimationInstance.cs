// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Boku.Animatics
{
    public class AnimationInstance
    {
        #region Members
        /// <summary>
        /// Dictionary of animations.
        /// </summary>
        private Animation.Dict animDict;
        /// <summary>
        /// Dictionary of the bone hierarchy.
        /// </summary>
        private Bone.Dict boneDict;
        /// <summary>
        /// Dictionary of skin data.
        /// </summary>
        private SkinDataList skinList;

        /// <summary>
        /// Current animation controller.
        /// </summary>
        private BaseController controller = null;

        /// <summary>
        /// Temp scratch pad for local to parent transforms when generating local to worlds.
        /// </summary>
        private static Matrix[] localToParent;
        /// <summary>
        /// Current local to world  transforms for each bone.
        /// </summary>
        private Matrix[] localToWorld;
        /// <summary>
        /// Current matrix palette info for each skin datum.
        /// </summary>
        private Matrix[] palette;

        /// <summary>
        /// Tick outn of our controller last time we sync'd up (update).
        /// </summary>
        private long lastTicks = -1;

        /// <summary>
        /// Adding an arbitrary numeric id for sorting. Greater and lesser don't mean anything,
        /// we just want same animators to be grouped by the sort.
        /// </summary>
        private static long nextId = 0;
        public readonly long id = nextId++;
        #endregion Members

        #region Accessors
        /// <summary>
        /// Current matrix palette info for each skin datum.
        /// Will update if necessary.
        /// </summary>
        public Matrix[] Palette
        {
            get 
            {
                Update();
                return palette; 
            }
        }
        /// <summary>
        /// Local to World transform for a specific bone
        /// Will update if necessary.
        /// </summary>
        /// <param name="bone"></param>
        /// <returns></returns>
        public Matrix LocalToWorld(Bone bone)
        {
            Update();
            return localToWorld[bone.Index];
        }
        /// <summary>
        ///  Local to world transform for the bone with given index.
        ///  Will update if necessary.
        /// </summary>
        /// <param name="boneIndex"></param>
        /// <returns></returns>
        public Matrix LocalToWorld(int boneIndex)
        {
            Update();
            return localToWorld[boneIndex];
        }
        /// <summary>
        /// The array of local to world transforms by bone.
        /// Will update if necessary.
        /// </summary>
        public Matrix[] LocalToWorldList
        {
            get 
            {
                Update(); 
                return localToWorld;
            }
        }
        /// <summary>
        /// Look up the index of a bone by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int BoneIndex(string name)
        {
            Bone bone = boneDict[name];

            return bone != null ? boneDict[name].Index : -1;
        }
        /// <summary>
        /// Find an animation by name. Will return null if none found.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Animation FindAnimation(string name)
        {
            return animDict[name];
        }
        /// <summary>
        /// Return whether we have an animation by that name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool HasAnimation(string name)
        {
            return FindAnimation(name) != null;
        }
        /// <summary>
        /// Return the number of bones we know about.
        /// </summary>
        public int NumBones
        {
            get { return boneDict.Length; }
        }
        /// <summary>
        /// Return the name of the first animation in the list.
        /// </summary>
        public string FirstAnimationName
        {
            get { return animDict.Length > 0 ? animDict[0].Name : null; }
        }
        /// <summary>
        /// Return whether we have any animations.
        /// </summary>
        public bool Empty
        {
            get { return animDict.Length == 0; }
        }
        #endregion Accessors

        #region Public
        /// <summary>
        /// Attempt to make an animation instance, return null if
        /// there isn't one to be made (e.g. the model isn't animated).
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static AnimationInstance TryMake(Model model)
        {
            AnimationInstance inst = new AnimationInstance();

            inst.animDict = Animation.Dict.Extract(model);
            if (inst.animDict == null)
                return null;

            inst.boneDict = Bone.Dict.Extract(model.Bones);
            if (inst.boneDict == null)
                return null;
            
            inst.skinList = SkinDataList.Extract(model);
            if (inst.skinList == null)
                return null;

            if (!inst.Bind())
                return null;

            return inst;
        }

        /// <summary>
        /// Set the current animation controller.
        /// </summary>
        /// <param name="controller"></param>
        public void SetAnimation(BaseController controller)
        {
            this.controller = controller;
            SetDefaultTransforms();
            UpdateToWorld();
            SetPalette();
        }

        /// <summary>
        /// Helper to make a new SimpleController based on an animation from
        /// our dictionary. May return null if there's no animation named name
        /// or backup.
        /// </summary>
        /// <param name="name">First animation name to try for.</param>
        /// <param name="backup">Backup animation name if name isn't there. May be null.</param>
        /// <returns></returns>
        public SimpleController TryMake(string name, string backup)
        {
            return SimpleController.TryMake(this, name, backup);
        }
        #endregion Public

        #region Internal
        /// <summary>
        /// Sync up transforms from the current state of the controller if necessary.
        /// </summary>
        private void Update()
        {
            if (controller != null)
            {
                if (lastTicks != controller.CurrentTicks)
                {
                    SetDefaultTransforms();

                    controller.GetTransforms(this, localToParent);

                    UpdateToWorld();
                    SetPalette();

                    lastTicks = controller.CurrentTicks;
                }
            }
        }

        /// <summary>
        /// Check that we have enough scratch space to update.
        /// </summary>
        private void CheckLocalToParent()
        {
            if ((localToParent == null) || (localToParent.Length < localToWorld.Length))
            {
                localToParent = new Matrix[localToWorld.Length];
            }
        }
        /// <summary>
        /// Copy the default transforms out of the bones. 
        /// </summary>
        private void SetDefaultTransforms()
        {
            Debug.Assert(localToWorld.Length == boneDict.Length);
            CheckLocalToParent();

            if (boneDict.Length > 0)
            {
                for (int i = 0; i < boneDict.Length; ++i)
                {
                    localToParent[i] = boneDict[i].DefaultBoneToParent;
                }
            }
        }

        /// <summary>
        /// Propagate the parent transforms down the hierarchy.
        /// </summary>
        private void UpdateToWorld()
        {
            Debug.Assert(localToParent.Length >= boneDict.Length);
            Debug.Assert(localToWorld.Length >= boneDict.Length);

            for (int i = 0; i < boneDict.Length; ++i)
            {
                if (boneDict[i].Parent != null)
                {
                    Debug.Assert(boneDict[i].Parent.Index < i);
                    localToWorld[i] = localToParent[i] * localToWorld[boneDict[i].Parent.Index];
                }
                else
                {
                    localToWorld[i] = localToParent[i];
                }
            }
        }

        /// <summary>
        /// Set the matrix palette used for skinning.
        /// </summary>
        private void SetPalette()
        {
            for (int i = 0; i < skinList.Length; ++i)
            {
                SkinDataList.Datum skinData = skinList[i];
                palette[skinData.PaletteIndex]
                    = skinData.ModelToLocal * localToWorld[skinData.BoneIndex];
            }
        }

        /// <summary>
        /// All of our parts have been made, now bind them together into us.
        /// </summary>
        /// <returns></returns>
        private bool Bind()
        {
            animDict.Bind(this);

            localToWorld = new Matrix[boneDict.Length];
            palette = new Matrix[skinList.Length];

            SetDefaultTransforms();
            UpdateToWorld();
            SetPalette();

            return true;
        }
        private AnimationInstance()
        {
        }

        #endregion Internal
    }
}

///
///
///
///	Update()
///    {
///		Gets the relative transforms from the boneposes into pose
///			pose = localToBoneToWorld
///		Concatenates into palette as follows:
///
///		for(int i = [0,skinInfo.count])
///			info = skinInfo[i]
///			palette[skinInfo[i].PaletteIndex] 
///                = skinInfo[i].InverseBindPoseTransform 
///                    * localToBoneToWorld[skinInfo[i].BoneIndex];
///
///         So InverseBindPose == ModelToLocal
///     }
///
///
///	AnimationInstance(Model model)
///	{
///	    Pull the AnimationDict out of the model (via tag "Animations")
///	    
///     Make the BoneDict out of the model's bonelist
///     
///     Pull the SkinDataList out of the model (via tag "SkinInfo")
/// 
///     Allocate the localToWorld[model.Bones.Count]
///     Initialize localToWorld[] via model.CopyAbsoluteBoneTransformsTo(localToWorld);
/// 
///     Allocate the palette
///	}
///
