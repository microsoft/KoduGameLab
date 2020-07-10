// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Boku.Animatics
{
    public class Animation
    {
        public class Dict
        {
            #region Members
            private readonly Animation[] animData;
            private readonly Dictionary<string, Animation> animDict;
            #endregion Members

            #region Accessors
            /// <summary>
            /// Number of animations contained
            /// </summary>
            public int Length
            {
                get { return animData.Length; }
            }
            /// <summary>
            /// Access animation by integer index.
            /// </summary>
            /// <param name="i"></param>
            /// <returns></returns>
            public Animation this[int i]
            {
                get { return animData[i]; }
            }
            /// <summary>
            /// Access an animation by name.
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public Animation this[string name]
            {
                get 
                {
                    Animation anim = null;
                    if (animDict.TryGetValue(name, out anim))
                        return anim;
                    return null; 
                }
            }
            #endregion Accessors

            #region Public
            /// <summary>
            /// Load up self from the input stream. This happens automagically
            /// when the model is loaded. Use Extract to get an instance off the model.
            /// </summary>
            /// <param name="input"></param>
            internal Dict(ContentReader input)
            {
                int numAnims = input.ReadInt32();

                animData = new Animation[numAnims];
                animDict = new Dictionary<string, Animation>(numAnims);

                for (int i = 0; i < numAnims; ++i)
                {
                    animData[i] = new Animation(input);
                    animDict.Add(animData[i].Name, animData[i]);
                }
            }

            /// <summary>
            /// Extract self from the model's tags.
            /// </summary>
            /// <param name="model"></param>
            /// <returns></returns>
            internal static Dict Extract(Model model)
            {
                Dictionary<string, object> tagData = (Dictionary<string, object>)model.Tag;
                if (tagData != null && tagData.ContainsKey("Animations"))
                {
                    return (Dict)tagData["Animations"];
                }
                return null;
            }

            /// <summary>
            /// Bind all contained animations to the animation instance.
            /// </summary>
            /// <param name="inst"></param>
            public void Bind(AnimationInstance inst)
            {
                for (int i = 0; i < animData.Length; ++i)
                {
                    animData[i].Bind(inst);
                }
            }
            #endregion Public
            #region Internal
            #endregion Internal
        }

        #region Members
        /// <summary>
        /// Name of this animation
        /// </summary>
        private readonly string name;

        /// <summary>
        /// Linear array of Key lists for each bone.
        /// </summary>
        private readonly BoneKeys[] boneData;
        /// <summary>
        /// Dictionary for finding key lists for a specific bone.
        /// </summary>
        private readonly Dictionary<string, BoneKeys> boneDict;

        /// <summary>
        /// Lookup table for getting bone index from index into boneData.
        /// </summary>
        private int[] keyIdxToBoneIdx;
        #endregion Members

        #region Accessors
        /// <summary>
        /// Name of this animation
        /// </summary>
        public string Name
        {
            get { return name; }
        }
        /// <summary>
        /// Length of this animation in ticks. Last frame is at duration.
        /// </summary>
        public long Duration
        {
            get { return boneData != null ? boneData[0].Duration : 0; }
        }
        /// <summary>
        /// The array of key lists, one for each bone.
        /// </summary>
        internal BoneKeys[] KeysList
        {
            get { return boneData; }
        }
        /// <summary>
        /// Lookup a bone index from an index into the key list.
        /// </summary>
        /// <param name="keyIndex"></param>
        /// <returns></returns>
        internal int KeyIndexToBoneIndex(int keyIndex)
        {
            return keyIdxToBoneIdx[keyIndex];
        }
        #endregion Accessors

        #region Public
        /// <summary>
        /// Read self from input stream.
        /// </summary>
        /// <param name="input"></param>
        private Animation(ContentReader input)
        {
            name = input.ReadString();

            int numBoneData = input.ReadInt32();

            boneData = new BoneKeys[numBoneData];
            boneDict = new Dictionary<string, BoneKeys>(numBoneData);

            long duration = 0;
            for (int i = 0; i < numBoneData; ++i)
            {
                boneData[i] = new BoneKeys(input);

                boneDict.Add(boneData[i].BoneName, boneData[i]);

                Debug.Assert((duration == 0) || (duration == boneData[i].Duration));
                duration = boneData[i].Duration;
            }
        }

        /// <summary>
        /// Bind self to animation instance. This just means building the
        /// key index to bone index lookup table.
        /// </summary>
        /// <param name="inst"></param>
        private void Bind(AnimationInstance inst)
        {
            keyIdxToBoneIdx = new int[KeysList.Length];
            for (int i = 0; i < keyIdxToBoneIdx.Length; ++i)
            {
                keyIdxToBoneIdx[i] = inst.BoneIndex(KeysList[i].BoneName);
            }
        }
        #endregion Public

        #region Internal
        #endregion Internal
    }
}

/// From How to: Determine an assembly's fully qualified name
/// Type t = typeof(System.Data.DataSet);
/// string s = t.Assembly.FullName.ToString();
/// Console.WriteLine("The fully qualified assembly name containing the specified class is {0}.", s);
/// Which gives:
////Boku.Animatics.AnimationReader, Boku, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null.
////Boku.Animatics.Animation+Dict, Boku, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null.
////Boku.Animatics.SkinDataReader, Boku, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null.
////Boku.Animatics.SkinDataList, Boku, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null.
///
/// From
////Type t = typeof(Boku.Animatics.AnimationReader);
////string s = t.AssemblyQualifiedName;
////Console.WriteLine("The fully qualified assembly name containing the specified class is {0}.", s);

////t = typeof(Boku.Animatics.Animation.Dict);
////s = t.AssemblyQualifiedName;
////Console.WriteLine("The fully qualified assembly name containing the specified class is {0}.", s);

////t = typeof(Boku.Animatics.SkinDataReader);
////s = t.AssemblyQualifiedName;
////Console.WriteLine("The fully qualified assembly name containing the specified class is {0}.", s);

////t = typeof(Boku.Animatics.SkinDataList);
////s = t.AssemblyQualifiedName;
////Console.WriteLine("The fully qualified assembly name containing the specified class is {0}.", s);

