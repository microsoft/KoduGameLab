
using System;
using System.Diagnostics;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;
using Boku.Fx;

namespace Boku.Common.ParticleSystem
{
    public class SharedSparkEmitter : SharedSmokeEmitter
    {
        #region Accessors
        protected override string TextureName
        {
            get { return @"Fire01"; }
        }
        protected override string TechniqueName
        {
            get { return @"TexturedColorPassOneOneBlend"; }
        }
        /// <summary>
        /// Avoid the lighting effecting spark color. 
        /// </summary>
        public override bool IsEmissive
        {
            get { return true; }
        }

        #endregion

        #region Public
        public SharedSparkEmitter(ParticleSystemManager manager, int maxParticles)
            : base(manager, maxParticles)
        {
        }

        #endregion Public
    };
};
