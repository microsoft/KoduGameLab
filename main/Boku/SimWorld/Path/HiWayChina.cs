﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;

using Boku.Common;
using Boku.SimWorld.Terra;

namespace Boku.SimWorld.Path
{
    public class HiWayChina : HiWayGen
    {
        #region Public
        /// <summary>
        /// Initialize data tables. Could be loaded from xml.
        /// </summary>
        public HiWayChina()
        {
            widthTable = new float[]  { 2.20f, 2.20f, 2.20f, 2.60f, 2.60f, 2.60f };
            heightTable = new float[] { 0.25f, 0.25f, 0.3f, 0.3f, 0.00f, -1.0f };
            tex0Source = new float[]  { 1.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f };
            uvSource = new float[]    { 0.00f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f };
            centerHeight = 0.25f;

            uvXfm = new Vector4(0.1f, 0.1f, 0.25f, 0.25f);

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
                diffTex0 = KoiLibrary.LoadTexture2D(@"Textures\Terrain\GroundTextures\StuccoWhite");
            }
            if (diffTex1 == null)
            {
                diffTex1 = KoiLibrary.LoadTexture2D(@"Textures\White");
            }
            if (normTex0 == null)
            {
                normTex0 = KoiLibrary.LoadTexture2D(@"Textures\DistortionWake");
            }
            if (normTex1 == null)
            {
                normTex1 = KoiLibrary.LoadTexture2D(@"Textures\EggDetail1");
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