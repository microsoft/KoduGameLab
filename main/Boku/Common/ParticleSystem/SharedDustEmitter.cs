// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Diagnostics;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;
using Boku.Fx;

namespace Boku.Common.ParticleSystem
{
    public class SharedDustEmitter : SharedSmokeEmitter
    {
        #region Accessors
        protected override string TextureName
        {
            get { return @"DustPuff"; }
        }
        #endregion

        #region Public
        public SharedDustEmitter(ParticleSystemManager manager, int maxParticles)
            : base(manager, maxParticles)
        {
        }

        #endregion Public

    };
};
