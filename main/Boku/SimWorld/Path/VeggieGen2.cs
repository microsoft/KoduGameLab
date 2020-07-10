// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using Boku.Common;
using Boku.SimWorld.Terra;

namespace Boku.SimWorld.Path
{
    class VeggieGen2 : VeggieGen
    {
        #region Accessors
        public override FBXModel EndModel
        {
            get
            {
                if (endModel == null)
                    endModel = ActorManager.GetActor("Popsy").Model;
                return endModel;
            }
        }
        #endregion Accessors

        #region Public

        /// <summary>
        /// Put down a tree at the intersection.
        /// </summary>
        public override bool NewFan(
            Road.Intersection isect, 
            Road.Section first, 
            Road.Section second,
            List<Road.RenderObj> fans)
        {
            FBXModel model = EndModel;
            if (model != null)
            {
                RoadMultiRenderObj multiRo = new RoadMultiRenderObj();

                Vector2 center = isect.Node.Position2d;

                float area = width * width * (float)Math.PI;

                int count = (int)Math.Ceiling(area * density);

                for (int i = 0; i < count; ++i)
                {
                    double rndAng = BokuGame.bokuGame.rnd.NextDouble() * Math.PI * 2.0;
                    float rndOut = (float)(BokuGame.bokuGame.rnd.NextDouble() * 2.0 - 1.0);

                    float cos = (float)Math.Cos(rndAng);
                    float sin = (float)Math.Sin(rndAng);

                    Vector2 rndPos = center;
                    rndPos.X += cos * rndOut;
                    rndPos.Y += sin * rndOut;
                    float height = Terrain.GetTerrainHeightFlat(rndPos);

                    Matrix localToWorld = Matrix.CreateTranslation(rndPos.X, rndPos.Y, height);

                    RoadFBXRenderObj ro = new RoadFBXRenderObj();
                    ro.Model = model;
                    ro.Animator = EndAnim;
                    ro.LocalToWorld = localToWorld;

                    multiRo.RenderObjList.Add(ro);

                }
                
                fans.Add(multiRo);

                return count > 0;
            }
            return false;
        }
        #endregion Public
    }
}
