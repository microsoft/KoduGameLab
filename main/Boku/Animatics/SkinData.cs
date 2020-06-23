
using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Boku.Animatics
{
    internal class SkinDataList
    {
        #region Child Class
        internal class Datum
        {
            /// <summary>
            /// Index of my bone in the bone list
            /// </summary>
            public readonly int BoneIndex;
            /// <summary>
            /// Index of my destination in the matrix palette.
            /// </summary>
            public readonly int PaletteIndex; 

            /// <summary>
            /// Name of my bone.
            /// </summary>
            public readonly string BoneName;

            /// <summary>
            /// Inverse of transform to initial pose
            /// </summary>
            public readonly Matrix ModelToLocal;

            /// <summary>
            /// Construct self from inputs, since everything inside is readonly.
            /// </summary>
            /// <param name="boneIndex"></param>
            /// <param name="paletteIndex"></param>
            /// <param name="boneName"></param>
            /// <param name="m2l"></param>
            public Datum(
                int boneIndex,
                int paletteIndex,
                string boneName,
                Matrix m2l)
            {
                this.BoneIndex = boneIndex;
                this.PaletteIndex = paletteIndex;
                this.BoneName = boneName;
                this.ModelToLocal = m2l;
            }
        }
        #endregion Child Class

        #region Members
        /// <summary>
        /// The list of skin data.
        /// </summary>
        private Datum[] skinList;
        #endregion Members

        #region Accessors
        /// <summary>
        /// Number of skin data we have.
        /// </summary>
        public int Length
        {
            get { return skinList.Length; }
        }
        /// <summary>
        /// Access skin data by index.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public Datum this[int i]
        {
            get { return skinList[i]; }
        }
        #endregion Accessors

        #region Public
        /// <summary>
        /// Load self from stream. Done automagically to be extracted later from a mesh.
        /// </summary>
        /// <param name="input"></param>
        internal SkinDataList(ContentReader input)
        {
            int numSkinData = input.ReadInt32();

            skinList = new Datum[numSkinData];
            for (int i = 0; i < numSkinData; ++i)
            {
                int boneIndex = input.ReadInt32();
                string boneName = input.ReadString();
                Matrix modelToLocal = input.ReadMatrix();
                int paletteIndex = input.ReadInt32();

                skinList[i] = new Datum(
                    boneIndex,
                    paletteIndex,
                    boneName,
                    modelToLocal);
            }
        }

        /// <summary>
        /// Pull the skin data out of a model. Will assert if there is no skin data.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        internal static SkinDataList Extract(Model model)
        {
            Dictionary<string, object> tagInfo = (Dictionary<string, object>)model.Tag;
            if ((tagInfo != null) && tagInfo.ContainsKey("SkinInfo"))
            {
                SkinDataList[] multiMesh = (SkinDataList[])tagInfo["SkinInfo"];
                Debug.Assert(multiMesh.Length >= 1);
                for (int i = 1; i < multiMesh.Length; ++i)
                {
                    Debug.Assert(multiMesh[i].Length == 0);
                }
                return multiMesh[0];
            }
            Debug.Assert(false, "Missing skin data");
            return null;
        }
        #endregion Public

        #region Internal
        #endregion Internal
    }
}
