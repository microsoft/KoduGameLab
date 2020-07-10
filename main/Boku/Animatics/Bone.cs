// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace Boku.Animatics
{
    public class Bone
    {
        #region Container Class
        public class Dict
        {
            #region Members
            /// <summary>
            /// Dictionary of the bones in our hierarchy.
            /// </summary>
            Dictionary<string, Bone> boneDict = null;
            /// <summary>
            /// Linear list of bones in our hierarchy.
            /// </summary>
            Bone[] boneList = null;
            #endregion Members

            #region Accessors
            /// <summary>
            /// Get a bone by index.
            /// </summary>
            /// <param name="idx"></param>
            /// <returns></returns>
            public Bone this[int idx]
            {
                get { return boneList[idx]; }
            }
            /// <summary>
            /// Find a bone by name
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public Bone this[string name]
            {
                get { return boneDict[name]; }
            }

            /// <summary>
            /// How many bones are there?
            /// </summary>
            public int Length
            {
                get { return boneList.Length; }
            }
            #endregion Accessors

            #region Public
            /// <summary>
            /// Build a dictionary out of the input bone collection.
            /// </summary>
            /// <param name="src"></param>
            /// <returns></returns>
            public static Dict Extract(ModelBoneCollection src)
            {
                Bone[] bones = new Bone[src.Count];

                for (int i = 0; i < src.Count; ++i)
                {
                    if (src[i].Parent == null)
                    {
                        AddBoneSubtree(src[i], bones);
                    }
                }
                return new Dict(bones);
            }
            #endregion Public

            #region Internal
            /// <summary>
            /// Construct self out of the list of Bones.
            /// </summary>
            /// <param name="src"></param>
            internal Dict(IList<Bone> src)
            {
                boneList = new Bone[src.Count];
                src.CopyTo(boneList, 0);

                boneDict = new Dictionary<string, Bone>(src.Count);
                for (int i = 0; i < src.Count; ++i)
                {
                    string name = src[i].Name;
                    Debug.Assert(name != null);
                    Debug.Assert(name != string.Empty);
                    Debug.Assert(!boneDict.ContainsKey(name));

                    boneDict.Add(name, src[i]);
                }
            }

            /// <summary>
            /// Recursively build Bone's for self and children from ModelBones.
            /// </summary>
            /// <param name="modelBone"></param>
            /// <param name="dst"></param>
            /// <returns></returns>
            private static Bone AddBoneSubtree(
                ModelBone modelBone, 
                Bone[] dst)
            {
                int index = modelBone.Index;
                string name = modelBone.Name;

                Matrix defaultBoneToParent = modelBone.Transform;

                Bone parent = modelBone.Parent != null
                    ? dst[modelBone.Parent.Index]
                    : null;

                int numChildren = modelBone.Children.Count;
                Bone[] children = new Bone[numChildren];

                Bone bone = new Bone(
                    index,
                    name,
                    defaultBoneToParent,
                    parent,
                    children);
                dst[bone.Index] = bone;

                for (int i = 0; i < numChildren; ++i)
                {
                    children[i] = AddBoneSubtree(
                        modelBone.Children[i],
                        dst);
                }

                return bone;
            }
            #endregion Internal

        }
        #endregion Container Class

        #region Members
        #endregion Members

        #region Accessors
        /// <summary>
        /// Index accessing from list, same as from source mesh ModelBoneCollection
        /// </summary>
        public readonly int Index;
        /// <summary>
        /// Name when accessing by name lookup.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Transform when not animated.
        /// </summary>
        public readonly Matrix DefaultBoneToParent;
        /// <summary>
        /// Transform parent in the hierarchy.
        /// </summary>
        public readonly Bone Parent;
        /// <summary>
        /// Transform children in the hierarchy.
        /// </summary>
        public Bone[] Children;
        #endregion Accessors

        #region Public

        #endregion Public

        #region Internal
        /// <summary>
        /// Construct self from inputs. Everything readonly here.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="name"></param>
        /// <param name="defaultTransform"></param>
        /// <param name="parent"></param>
        /// <param name="children"></param>
        private Bone(
            int index,
            string name,
            Matrix defaultTransform,
            Bone parent,
            Bone[] children)
        {
            this.Index = index;
            this.Name = name;

            this.DefaultBoneToParent = defaultTransform;

            this.Parent = parent;
            this.Children = children;
        }
        #endregion Internal
    };
};
