// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Boku.SimWorld.Path
{
    class RoundWallGen : HiWallGen
    {
        #region Public
        /// <summary>
        /// Initialize data tables. Could be loaded from xml.
        /// </summary>
        public RoundWallGen()
        {
            widthTable = new float[] { 0.38f, 0.38f, 0.70f, 0.92f, 1.00f, 0.92f, 0.71f, 0.71f, 0.70f, 0.38f, 0.00f };
            heightTable = new float[] { 1.92f, 1.92f, 1.71f, 1.38f, 1.00f, 0.62f, 0.30f, 0.29f, 0.29f, 0.08f, 0.00f };
            tex0Source = new float[] { 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 1.00f, 0.00f, 0.00f, 0.00f };
            uvSource = new float[] { 0.00f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f, 0.00f, 0.00f, 0.00f, 0.00f };
            centerHeight = 2.00f;

            Shininess = 0.0f;

            StretchUp = true;
            StretchUpEnd = true;

            uvXfm = new Vector4(0.2f, 1.0f, 0.1f, 1.0f);

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
                            + @"Textures\Terrain\GroundTextures\FREESAMPLES_23");
            }
            if (diffTex1 == null)
            {
                diffTex1 = BokuGame.Load<Texture2D>(
                                    BokuGame.Settings.MediaPath
                            + @"Textures\Terrain\GroundTextures\rock2");
            }
            if (normTex0 == null)
            {
                normTex0 = BokuGame.Load<Texture2D>(
                            BokuGame.Settings.MediaPath
                            + @"Textures\Terrain\GroundTextures\FREESAMPLES_23_norm");
            }
            if (normTex1 == null)
            {
                normTex1 = BokuGame.Load<Texture2D>(
                            BokuGame.Settings.MediaPath
                            + @"Textures\Terrain\GroundTextures\RIVROCK1_norm");
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

        #endregion Public
    }

    class RoboWall3 : HiWallGen
    {
        #region Public
        /// <summary>
        /// Initialize data tables. Could be loaded from xml.
        /// </summary>
        public RoboWall3()
        {
            widthTable = new float[] { 0.38f, 0.38f, 0.70f, 0.92f, 1.00f, 0.92f, 0.71f, 0.71f, 0.70f, 0.38f, 0.00f };
            heightTable = new float[] { 1.92f, 1.92f, 1.71f, 1.38f, 1.00f, 0.62f, 0.30f, 0.29f, 0.29f, 0.08f, 0.00f };
            tex0Source = new float[] { 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f };
            uvSource = new float[] { 0.00f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f, 0.00f, 0.00f, 0.00f, 0.00f };
            centerHeight = 2.00f;

            Shininess = 0.0f;

            StretchUp = true;
            StretchUpEnd = true;

            uvXfm = new Vector4(0.2f, 1.0f, 0.1f, 1.0f);

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
                            + @"Textures\Terrain\GroundTextures\simple_white");
            }
            if (diffTex1 == null)
            {
                diffTex1 = BokuGame.Load<Texture2D>(
                                    BokuGame.Settings.MediaPath
                            + @"Textures\Terrain\GroundTextures\simple_white");
            }
            if (normTex0 == null)
            {
                normTex0 = BokuGame.Load<Texture2D>(
                            BokuGame.Settings.MediaPath
                            + @"Textures\Terrain\GroundTextures\Blue128x128");
            }
            if (normTex1 == null)
            {
                normTex1 = BokuGame.Load<Texture2D>(
                            BokuGame.Settings.MediaPath
                            + @"Textures\Terrain\GroundTextures\Blue128x128");
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

        #endregion Public
    }
}
