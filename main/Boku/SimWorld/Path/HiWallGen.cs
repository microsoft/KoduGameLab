// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Boku.SimWorld.Path
{
    class HiWallGen : HiWayGen
    {
        #region Accessors
        public override float DefaultNodeHeight
        {
            get { return 2.0f; }
        }
        #endregion Accessors

        #region Public
        /// <summary>
        /// Initialize data tables. Could be loaded from xml.
        /// </summary>
        public HiWallGen()
        {
            widthTable = new float[]  { 0.10f, 0.40f, 0.40f, 0.40f, 0.50f, 0.60f, 0.60f, 0.60f };
            heightTable = new float[] { 0.90f, 0.00f, 0.00f, 0.15f, 0.40f, 0.40f, 0.30f, -1.0f };
            tex0Source =  new float[] { 0.00f, 0.00f, 0.00f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f };
            uvSource = new float[] { 1.00f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f };
            centerHeight = 0.5f;

            uvXfm = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

            InitDelTables();

            diffTex0 = null;
            diffTex1 = null;
            normTex0 = null;
            normTex1 = null;

            BokuGame.Load(this);
        }

        /// <summary>
        /// Load up textures.
        /// </summary>
        /// <param name="graphics"></param>
        public override void LoadContent(bool immediate)
        {
            if (diffTex0 == null)
            {
                diffTex0 = BokuGame.Load<Texture2D>(
                                    BokuGame.Settings.MediaPath
                            + @"Textures\Terrain\GroundTextures\StuccoYellow");
            }
            if (diffTex1 == null)
            {
                diffTex1 = BokuGame.Load<Texture2D>(
                                    BokuGame.Settings.MediaPath
                            + @"Textures\White");
            }
            if (normTex0 == null)
            {
                normTex0 = BokuGame.Load<Texture2D>(
                            BokuGame.Settings.MediaPath
                            + @"Textures\Terrain\GroundTextures\RIVROCK1_norm");
            }
            if (normTex1 == null)
            {
                normTex1 = BokuGame.Load<Texture2D>(
                            BokuGame.Settings.MediaPath
                            + @"Textures\Terrain\GroundTextures\rock2_norm");
            }

            base.LoadContent(immediate);
        }

        public override void UnloadContent()
        {
            BokuGame.Release(ref diffTex0);
            BokuGame.Release(ref diffTex1);
            BokuGame.Release(ref normTex0);
            BokuGame.Release(ref normTex1);
            base.UnloadContent();
        }

        /// <summary>
        /// We will block travel.
        /// </summary>
        /// <returns></returns>
        public override bool MakesBlocker
        {
            get { return true; }
        }
        #endregion Public
    }
}
