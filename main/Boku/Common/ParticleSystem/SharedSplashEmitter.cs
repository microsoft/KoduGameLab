// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Diagnostics;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;

using Boku.Base;
using Boku.Fx;

namespace Boku.Common.ParticleSystem
{
    public class SharedSplashEmitter : SharedSmokeEmitter
    {
        #region Members
        private float drag = -1.0f;
        private float minSpeed = 1.0f;
        #endregion Members

        #region Accessors
        protected override string TextureName
        {
            get { return @"Splash01"; }
        }
        protected override string TechniqueName
        {
            get { return @"TexturedColorPassNormalAlphaDrag"; }
        }
        #endregion

        #region Public
        public SharedSplashEmitter(ParticleSystemManager manager, int maxParticles)
            : base(manager, maxParticles)
        {
        }

        public override void PreRender(Camera camera)
        {
            base.PreRender(camera);

            Vector2 decay = new Vector2(drag, minSpeed);
            Effect.Parameters["Drag"].SetValue(decay);
        }

        #endregion Public
    };
};
