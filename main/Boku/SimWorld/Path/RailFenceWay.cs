// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;

namespace Boku.SimWorld.Path
{
    public class RailFenceWay : HiWayGen
    {
        #region ChildClasses
        private class Rails : HiWayGen
        {
            #region Accessors
            /// <summary>
            /// No trimming.
            /// </summary>
            public override bool Trims
            {
                get { return false; }
            }
            /// <summary>
            /// Can block travel.
            /// </summary>
            public override bool MakesBlocker
            {
                get { return true; }
            }
            #endregion Accessors

            #region Public
            /// <summary>
            /// Setup data tables. Could come from xml.
            /// </summary>
            public Rails()
            {
                widthTable = new float[]    { 0.10f, 0.10f, 0.00f, 0.00f, 0.00f, 0.00f, 0.10f, 0.10f, 0.00f };
                heightTable = new float[]   { 1.95f, 1.85f, 1.80f, 1.80f, 1.00f, 1.00f, 0.95f, 0.85f, 0.80f };
                tex0Source = new float[]    { 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f };
                tex1Source = null;
                tex2Source = null;
                // For UV source, 0 is horizontal mapping, 1 is vertical mapping
                uvSource = new float[] { 1.00f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f };
                centerHeight = 2.00f;

                InitDelTables();
                skipTable[4] = true;

                diffTex0 = null;
                diffTex1 = null;
                normTex0 = null;
                normTex1 = null;

                BokuGame.Load(this);
            }
            /// <summary>
            /// No trimming
            /// </summary>
            /// <param name="first"></param>
            /// <param name="second"></param>
            /// <returns></returns>
            public override bool Trim(Road.Section first, Road.Section second)
            {
                return false;
            }
            /// <summary>
            /// Load textures.
            /// </summary>
            /// <param name="graphics"></param>
            public override void LoadContent(bool immediate)
            {
                if (diffTex0 == null)
                {
                    diffTex0 = KoiLibrary.LoadTexture2D(@"Textures\Terrain\GroundTextures\RIVROCK1");
                }
                if (diffTex1 == null)
                {
                    diffTex1 = KoiLibrary.LoadTexture2D(@"Textures\Terrain\GroundTextures\FREESAMPLES_38");
                }
                if (normTex0 == null)
                {
                    normTex0 = KoiLibrary.LoadTexture2D(@"Textures\Terrain\GroundTextures\RIVROCK1_norm");
                }
                if (normTex1 == null)
                {
                    normTex1 = KoiLibrary.LoadTexture2D(@"Textures\Terrain\GroundTextures\FREESAMPLES_38_norm");
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

        private class Posts : HiWayGen
        {
            #region Accessors
            /// <summary>
            /// No trimming.
            /// </summary>
            public override bool Trims
            {
                get { return false; }
            }
            /// <summary>
            /// Can block travel.
            /// </summary>
            public override bool MakesBlocker
            {
                get { return true; }
            }
            #endregion Accessors

            #region Public
            /// <summary>
            /// Setup data tables. Could come from xml.
            /// </summary>
            public Posts()
            {
                widthTable = new float[]    { 0.20f, 0.20f, 0.20f };
                heightTable = new float[]   { 2.30f, 2.30f, -1.00f };
                tex0Source = new float[]    { 1.00f, 1.00f, 1.00f };
                tex1Source = null;
                tex2Source = null;
                // For UV source, 0 is horizontal mapping, 1 is vertical mapping
                uvSource = new float[] { 0.00f, 1.00f, 1.00f };
                centerHeight = 2.30f;

                InitDelTables();

                diffTex0 = null;
                diffTex1 = null;
                normTex0 = null;
                normTex1 = null;
                
                BokuGame.Load(this);
            }
            /// <summary>
            /// No trimming
            /// </summary>
            /// <param name="first"></param>
            /// <param name="second"></param>
            /// <returns></returns>
            public override bool Trim(Road.Section first, Road.Section second)
            {
                return false;
            }
            /// <summary>
            /// Create new intersection fan.
            /// </summary>
            /// <param name="node"></param>
            /// <param name="first"></param>
            /// <param name="second"></param>
            /// <returns></returns>
            public override bool NewFan(
                Road.Intersection isect, 
                Road.Section first, 
                Road.Section second,
                List<Road.RenderObj> fans)
            {
                return NewFan(isect, new Vector2(1.0f, 0.0f), MathHelper.TwoPi, fans);
            }
            /// <summary>
            /// Load textures.
            /// </summary>
            /// <param name="graphics"></param>
            public override void LoadContent(bool immediate)
            {
                if (diffTex0 == null)
                {
                    diffTex0 = KoiLibrary.LoadTexture2D(@"Textures\Terrain\GroundTextures\RIVROCK1");
                }
                if (diffTex1 == null)
                {
                    diffTex1 = KoiLibrary.LoadTexture2D(@"Textures\Terrain\GroundTextures\FREESAMPLES_38");
                }
                if (normTex0 == null)
                {
                    normTex0 = KoiLibrary.LoadTexture2D(@"Textures\Terrain\GroundTextures\RIVROCK1_norm");
                }
                if (normTex1 == null)
                {
                    normTex1 = KoiLibrary.LoadTexture2D(@"Textures\Terrain\GroundTextures\FREESAMPLES_38_norm");
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
        #endregion ChildClasses

        #region Members
        Rails rails = new Rails();
        Posts posts = new Posts();
        #endregion Members

        #region Accessors
        /// <summary>
        /// Fences block travel
        /// </summary>
        public override bool MakesBlocker
        {
            get { return true; }
        }
        /// <summary>
        /// Composite max dimensions
        /// </summary>
        public override float MaxWidth
        {
            get { return Math.Max(rails.MaxWidth, posts.MaxWidth); }
        }
        /// <summary>
        /// Composite max dimensions
        /// </summary>
        public override float MaxHeight
        {
            get { return Math.Max(rails.MaxHeight, posts.MaxHeight); }
        }
        /// <summary>
        /// Along section, collide like rails
        /// </summary>
        public override float CollWidth
        {
            get { return rails.CollWidth; }
        }
        /// <summary>
        /// Along section, collide like rails
        /// </summary>
        public override float CollHeight
        {
            get { return rails.CollHeight; }
        }
        /// <summary>
        /// The low point for collisions in between nodes.
        /// </summary>
        public override float CollBase
        {
            get { return rails.MinHeight; }
        }
        /// <summary>
        /// At intersections, collide like posts
        /// </summary>
        public override float CollEndHeight
        {
            get { return posts.CollHeight; }
        }
        /// <summary>
        /// The low point for collisions at intersections.
        /// </summary>
        public override float CollEndBase
        {
            get { return posts.MinHeight; }
        }
        /// <summary>
        /// At intersections, collide like posts
        /// </summary>
        public override float CollRadius // for the intersections
        {
            get { return posts.CollRadius; }
        }
        #endregion Accessors

        #region Public
        public RailFenceWay()
        {
        }

        /// <summary>
        /// Along sections, get height of rails.
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="pos"></param>
        /// <param name="airborne"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public override bool GetHeight(Vector3 p0, Vector3 p1, Vector3 pos, ref float height)
        {
            return rails.GetHeight(p0, p1, pos, ref height);
        }
        /// <summary>
        /// At intersections, check height as posts.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="pos"></param>
        /// <param name="airborne"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public override bool GetHeight(Vector3 center, Vector3 pos, ref float height)
        {
            return posts.GetHeight(center, pos, ref height);
        }

        /// <summary>
        /// Sections are rails.
        /// </summary>
        /// <param name="section"></param>
        public override void NewSection(Road.Section section)
        {
            rails.NewSection(section);
        }

        /// <summary>
        /// Trim based on whether intersections want trimming.
        /// </summary>
        public override bool Trims
        {
            get { return posts.Trims; }
        }
        /// <summary>
        /// Create a new post.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public override bool NewFan(
            Road.Intersection isect, 
            Road.Section first, 
            Road.Section second,
            List<Road.RenderObj> fans)
        {
            return posts.NewFan(isect, first, second, fans);
        }

        /// <summary>
        /// Give components a chance to load graphics content.
        /// </summary>
        /// <param name="graphics"></param>
        public override void LoadContent(bool immediate)
        {
            rails.LoadContent(immediate);
            posts.LoadContent(immediate);
        }

        public override void InitDeviceResources(GraphicsDevice device)
        {
            rails.InitDeviceResources(device);
            posts.InitDeviceResources(device);
        }
        /// <summary>
        /// Give components a chance to unload graphics content.
        /// </summary>
        /// <param name="graphics"></param>
        public override void UnloadContent()
        {
            rails.UnloadContent();
            posts.UnloadContent();
        }
        #endregion Public

    }
}
