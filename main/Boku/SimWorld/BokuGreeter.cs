using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;
using Boku.Common;
using Boku.Common.Xml;
using Boku.Fx;

namespace Boku.SimWorld
{
    public class BokuGreeter : GameActor
    {
        public BokuGreeter(string classificationName, BaseChassis chassis, GetModelInstance getModelInstance, StaticActor staticActor)
            : base(classificationName, classificationName, chassis, getModelInstance, getModelInstance, staticActor)
        {
        }

        private static TextureCube envTexture = null;

        /// <summary>
        /// The environment map we use on the greeter, so it can be different than the ingame map.
        /// </summary>
        internal static TextureCube EnvMap
        {
            get { return envTexture; }
        }

        public override void LoadContent(bool immediate)
        {
            base.LoadContent(immediate);

            if (envTexture == null)
            {
                envTexture = BokuGame.Load<TextureCube>(BokuGame.Settings.MediaPath + @"Textures\EnvBrian");
            }
        }

        public override void InitDeviceResources(GraphicsDevice device)
        {
            base.InitDeviceResources(device);
        }

        public override void UnloadContent()
        {
            base.UnloadContent();

            envTexture = null;
        }
    }

    /// <summary>
    /// Inner child rendering object for BokuGreeter.
    /// </summary>
    public class BokuGreeterModel : FBXModel
    {
        // c'tor
        public BokuGreeterModel(string resourceName)
            : base(resourceName)
        {
            NeverDisplayCollisions = true;
        }

        /// <summary>
        /// Set our envmap as current, render, then restore.
        /// </summary>
        public override void Render(Camera camera, ref Matrix rootToWorld, List<List<PartInfo>> listPartsReplacement)
        {
            ShaderGlobals.PushEnvMap(BokuGreeter.EnvMap);

            base.Render(camera, ref rootToWorld, listPartsReplacement);

            ShaderGlobals.PopEnvMap();
        }

    }   // end of class BokuGreeterModel
}
