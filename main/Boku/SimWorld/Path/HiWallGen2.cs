// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;

namespace Boku.SimWorld.Path
{
    class HiWallGen2 : HiWallGen
    {
        #region Public
        /// <summary>
        /// Initialize data tables. Could be loaded from xml.
        /// </summary>
        public HiWallGen2()
        {
            widthTable = new float[]  { 0.010f, 0.200f, 0.300f, 0.400f, 0.500f, 0.600f, 0.600f, 0.600f };
            heightTable = new float[] { 0.409f, 0.408f, 0.405f, 0.403f, 0.400f, 0.390f, 0.390f, -1.00f };
            tex0Source =  new float[] { 1.000f, 1.000f, 1.000f, 1.000f, 1.000f, 1.000f, 0.000f, 0.00f };
            uvSource = new float[]    { 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 1.000f, 1.00f };
            centerHeight = 0.410f;

            Shininess = 0.1f;

            uvXfm = new Vector4(0.3f, 0.3f, 0.3f, 0.3f);

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
                diffTex0 = KoiLibrary.LoadTexture2D(@"Textures\Terrain\GroundTextures\wall");
            }
            if (diffTex1 == null)
            {
                diffTex1 = KoiLibrary.LoadTexture2D(@"Textures\Terrain\GroundTextures\White128x128");
            }
            if (normTex0 == null)
            {
                normTex0 = KoiLibrary.LoadTexture2D(@"Textures\Terrain\GroundTextures\wall_norm");
            }
            if (normTex1 == null)
            {
                normTex1 = KoiLibrary.LoadTexture2D(@"Textures\Terrain\GroundTextures\FREESAMPLES_23_norm");
            }

            base.LoadContent(immediate);
        }

        public override void UnloadContent()
        {
            DeviceResetX.Release(ref diffTex0);
            DeviceResetX.Release(ref diffTex1);
            DeviceResetX.Release(ref normTex0);
            DeviceResetX.Release(ref normTex1);
            base.UnloadContent();
        }

        #endregion Public
    }
}
