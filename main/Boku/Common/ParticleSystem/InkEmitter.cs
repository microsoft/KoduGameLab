// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

using KoiX;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;

namespace Boku.Common.ParticleSystem
{
    // Emits "Explosion" as a series of expanding, rotating 
    // sprites which fade and grow as they age.  The sprites
    // are rendered using additive blending.
    public class InkEmitter : BaseSpriteEmitter
    {
        private static Texture2D texture = null;

        #region accessors
        protected override Texture2D Texture
        {
            get { return texture; }
        }
        #endregion

        // c'tor
        public InkEmitter(ParticleSystemManager manager)
            : base(manager)
        {
            Color = new Vector4(0.13f, 0.1f, 0.2f, 1.0f);
            StartRadius = 0.7f;
            EndRadius = 1.4f;
  
            StartAlpha = 1.0f;
            EndAlpha = 0.0f;

            MinLifetime = 0.2f;     // Particle lifetime.
            MaxLifetime = 1.0f;

            EmissionRate = 50.0f;  // Particles per second.

            MaxSpeed = 100.0f;

            PositionJitter = 0.5f;  // Random offset for each particle.
            Gravity = new Vector3(0.0f, 0.0f, 0.0f);
           // Color = new Vector4(1.0f, 1.0f, 1.0f, 0.5f);
            MaxRotationRate = 0.2f;

            /*
            Color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            StartRadius = 0.0f;
            EndRadius = 1.0f;
            StartAlpha = 1.0f;
            EndAlpha = 0.0f;
            MinLifetime = 0.2f;     // Particle lifetime.
            MaxLifetime = 1.0f;
            EmissionRate = 100.0f;  // Particles per second.

            MaxRotationRate = 2.0f;
            */

        }   // end of c'tor

        new public static void LoadContent(bool immediate)
        {
            // Load the texture.
            if (InkEmitter.texture == null)
            {
                InkEmitter.texture = KoiLibrary.LoadTexture2D(@"Textures/InkJet");
            }
        }   // end of ExplosionEmitter LoadContent()

        new public static void InitDeviceResources(GraphicsDevice device)
        {
        }

        new public static void UnloadContent()
        {
            InkEmitter.texture = null;
        }   // end of ExplosionEmitter UnloadContent()

        new public static void DeviceReset(GraphicsDevice device)
        {
        }

        public override void Update()
        {
            base.Update();            
        }

    }   // end of class InkEmitter
}
