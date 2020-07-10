// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Common;
using Boku.SimWorld.Terra;

namespace Boku.SimWorld.Path
{
    public class HiWayWide : HiWayGen
    {
         #region Public
        /// <summary>
        /// Initialize data tables. Could be loaded from xml.
        /// </summary>
        public HiWayWide()
        {
            widthTable = new float[] { 2.80f, 3.00f, 3.00f, 3.10f, 3.10f, 3.10f, 0.0f };
            heightTable = new float[] { 0.25f, 0.25f, 0.30f, 0.30f, 0.00f, 0.00f, 0.0f };
            tex0Source = new float[] { 1.00f, 1.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f };
            uvSource = new float[] { 0.00f, 0.00f, 1.00f, 1.00f, 1.00f, 0.00f, 0.00f };
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
                diffTex0 = BokuGame.Load<Texture2D>(
                                    BokuGame.Settings.MediaPath
                            + @"Textures\White");
            }
            if (diffTex1 == null)
            {
                diffTex1 = BokuGame.Load<Texture2D>(
                                    BokuGame.Settings.MediaPath
                            + @"Textures\Terrain\GroundTextures\StuccoWhite");
            }
            if (normTex0 == null)
            {
                normTex0 = BokuGame.Load<Texture2D>(
                            BokuGame.Settings.MediaPath
                            + @"Textures\Terrain\GroundTextures\rocktop_no_bevel");
            }
            if (normTex1 == null)
            {
                normTex1 = BokuGame.Load<Texture2D>(
                            BokuGame.Settings.MediaPath
                            + @"Textures\DistortionWake");
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
