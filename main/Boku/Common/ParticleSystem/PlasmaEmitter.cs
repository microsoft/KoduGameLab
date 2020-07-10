// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Boku.Common.ParticleSystem
{
    public class PlasmaEmitter : SmokeEmitter
    {
        /// <summary>
        /// Avoid the lighting effecting your color. These are arcade effect
        /// that don't want or need realistic lighting.
        /// </summary>
        public override bool IsEmissive
        {
            get { return true; }
        }

        public PlasmaEmitter(ParticleSystemManager manager)
            : base(manager)
        {
            ExplicitBloom = 0.9f;
            StartAlpha = 0f;
            EndAlpha = 0f;
        }
    }

}
