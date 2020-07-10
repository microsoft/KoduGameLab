// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;

using Boku.Common;
using Boku.SimWorld.Terra;

namespace Boku.SimWorld.Path
{
    class CastleGen : HiWayGen
    {
        #region Members
        protected Vector2 castleDim = new Vector2(1.0f, 6.66f);
        #endregion Members

        #region Accessors
        /// <summary>
        /// Physics height for the castles at intersections.
        /// </summary>
        public override float CollEndHeight
        {
            get { return castleDim.Y; }
        }
        /// <summary>
        /// Physics radius for the castles at intersections.
        /// </summary>
        public override float CollRadius
        {
            get { return castleDim.X; }
        }

        #endregion Accessors

        #region Public
        /// <summary>
        /// Constructor initializes data tables. Could come from xml.
        /// </summary>
        public CastleGen()
        {
            widthTable = new float[]  { 0.05f, 0.20f, 0.20f, 0.20f, 0.30f, 0.30f, 0.30f };
            heightTable = new float[] { 4.30f, 2.00f, 2.30f, 2.30f, 2.30f, 2.30f, -1.00f };
            tex0Source = new float[]  { 1.00f, 1.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f };
            uvSource = new float[]    { 1.00f, 1.00f, 1.00f, 0.00f, 0.00f, 1.00f, 1.00f };
            centerHeight = 4.4f;

            InitDelTables();

            uvXfm.X = 0.25f;
            uvXfm.Y = 0.25f;
            uvXfm.Z = 0.25f;
            uvXfm.W = 0.25f;

            diffTex0 = null;
            diffTex1 = null;
            normTex0 = null;
            normTex1 = null;

            BokuGame.Load(this);
        }

        /// <summary>
        /// Whether the road generated will block travel.
        /// </summary>
        /// <returns></returns>
        public override bool MakesBlocker
        {
            get { return true; }
        }

        /// <summary>
        /// Whether the road generated can be stood upon.
        /// </summary>
        /// <returns></returns>
        public override bool MakesGround
        {
            get { return true; }
        }

        /// <summary>
        /// Whether the road sections need to be trimmed at intersections.
        /// </summary>
        /// <returns></returns>
        public override bool Trims
        {
            get { return false; }
        }

        /// <summary>
        /// Load up textures.
        /// </summary>
        /// <param name="graphics"></param>
        public override void LoadContent(bool immediate)
        {
            if (diffTex0 == null)
            {
                diffTex0 = KoiLibrary.LoadTexture2D(@"Textures\Terrain\GroundTextures\FREESAMPLES_38");
            }
            if (diffTex1 == null)
            {
                diffTex1 = KoiLibrary.LoadTexture2D(@"Textures\Terrain\GroundTextures\Concrete03");
            }
            if (normTex0 == null)
            {
                normTex0 = KoiLibrary.LoadTexture2D(@"Textures\Terrain\GroundTextures\FREESAMPLES_38_norm");
            }
            if (normTex1 == null)
            {
                normTex1 = KoiLibrary.LoadTexture2D(@"Textures\Terrain\GroundTextures\Blue128x128");
            }

            base.LoadContent(immediate);
        }

        /// <summary>
        /// Generate a castle as an intersection.
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
            bool didBase = base.NewFan(isect, first, second, fans);

            WayPoint.Node node = isect.Node;
            /*
             * centerheight
             * 4.4
             * height
             * 4.3, 2.0, 2.3, 2.3, -1.0
             * width
             * 0.05, 0.2, 0.2, 0.3, 0.3
             * */
            RoadFBXRenderObj ro = null;
            FBXModel model = ActorManager.GetActor("Castle").Model;
            if (model != null)
            {
                ro = new RoadFBXRenderObj();

                ro.Model = model;

                Matrix localToWorld = Matrix.Identity;
                Vector3 position = node.Position;
                localToWorld.Translation = position;
                ro.LocalToWorld = localToWorld;

                fans.Add(ro);

                return true;
            }
            return didBase;
        }

        /// <summary>
        /// We don't need to trim.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public override bool Trim(Road.Section first, Road.Section second)
        {
            return false;
        }

        /// <summary>
        /// If the position is on the road, compute the height of the road and return true.
        /// Else return false.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="pos"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public override bool GetHeight(Vector3 center, Vector3 pos, ref float height)
        {
            float distSq = Vector2.DistanceSquared(
                new Vector2(center.X, center.Y), new Vector2(pos.X, pos.Y));
            if (center.Z < pos.Z)
            {
                if (distSq < castleDim.X * castleDim.X)
                {
                    height = castleDim.Y;
                    return true;
                }
            }
            if (distSq < CollRadius * CollRadius)
            {
                height = castleDim.Y;
                return true;
            }
            return false;
        }
        #endregion Public
    }
}
