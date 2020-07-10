// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Boku.Animatics
{
    public struct BoneKeys
    {
        #region Child Struct
        /// <summary>
        /// Simple KeyFrame struct.
        /// </summary>
        internal struct KeyFrame
        {
            /// <summary>
            /// My relative transform.
            /// </summary>
            public readonly Matrix BoneToParent;

            /// <summary>
            /// Set our readonly innards from the input bone to parent transform
            /// and time stamp.
            /// </summary>
            /// <param name="b2p"></param>
            /// <param name="t"></param>
            public KeyFrame(Matrix b2p)
            {
                BoneToParent = b2p;
            }
        }
        #endregion Child Struct

        #region Members
        /// <summary>
        /// Name of the bone for which these key frames apply
        /// </summary>
        private readonly string boneName;
        /// <summary>
        /// List of keys for this bone.
        /// </summary>
        private readonly KeyFrame[] keys;
        /// <summary>
        /// Length of the animation. Last key.Time == duration.
        /// </summary>
        private readonly long duration;
        #endregion Members

        #region Accessors
        /// <summary>
        /// Name of the bone for which these keys apply.
        /// </summary>
        internal string BoneName
        {
            get { return boneName; }
        }
        /// <summary>
        /// List of keys for this bone.
        /// </summary>
        private KeyFrame[] Keys
        {
            get { return keys; }
        }
        /// <summary>
        /// Duration of the animation. Last key.Time == Duration.
        /// </summary>
        public long Duration
        {
            get { return duration; }
        }
        #endregion Accessors

        #region Public

        /// <summary>
        /// Construct and load self from reader. This will leave this keyset
        /// deeply imbedded within the model, but will automagically come out
        /// via Animation.Dict.Extract.
        /// </summary>
        /// <param name="input"></param>
        public BoneKeys(ContentReader input)
        {
            boneName = input.ReadString();

            int numKeys = input.ReadInt32();
            keys = new KeyFrame[numKeys];

            duration = input.ReadInt64();

            for (int i = 0; i < numKeys; ++i)
            {
                keys[i] = new KeyFrame(
                    input.ReadMatrix());
            }
        }

        /// <summary>
        /// Compute keyframe index from tickcount
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public int IndexAtTime(long t)
        {
            int idx = (int)(t * 30 / TimeSpan.TicksPerSecond);
            idx = idx < 0
                ? 0
                : (idx >= keys.Length
                    ? keys.Length - 1
                    : idx);


            return idx;
        }
        /// <summary>
        /// Return the transform at a given tickcount
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Matrix AtTime(long t)
        {
            return AtIndex(IndexAtTime(t));
        }
        /// <summary>
        /// Return the transform at a given keyframe index.
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public Matrix AtIndex(int idx)
        {
            return Keys[idx].BoneToParent;
        }
        #endregion Public

        #region Internal
        #endregion Internal
    }
}
