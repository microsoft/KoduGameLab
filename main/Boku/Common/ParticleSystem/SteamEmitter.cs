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

namespace Boku.Common.ParticleSystem
{
    
    // Emits "steam puffs" as a series of expanding, rotating 
    // sprites which fade and grow as they age.
    public class SteamEmitter : BaseSpriteEmitter
    {
        private static Texture2D texture = null;

        #region accessors
        protected override Texture2D Texture
        {
            get { return texture; }
        }
        #endregion

        // c'tor
        public SteamEmitter(ParticleSystemManager manager)
            : base(manager)
        {
            EmissionRate = 20.0f;
            Color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            StartRadius = 0.5f;
            EndRadius = 0.1f;
            StartAlpha = 0.6f;
            EndAlpha = 0.0f;
            MinLifetime = 1.0f;       // Particle lifetime.
            MaxLifetime = 4.0f;

            MaxRotationRate = 2.0f;

            Gravity = new Vector3(0.0f, 0.0f, 1.0f);
            this.MaxSpeed = 1.5f;
        }   // end of c'tor

        new public static void LoadContent(bool immediate)
        {
            // Load the texture.
            if (SteamEmitter.texture == null)
            {
                SteamEmitter.texture = KoiLibrary.LoadTexture2D(@"Textures/DustPuff");
            }
        }   // end of SteamEmitter LoadContent()

        new public static void InitDeviceResources(GraphicsDevice device)
        {
        }

        new public static void UnloadContent()
        {
            SteamEmitter.texture = null;
        }   // end of SteamEmitter UnloadContent()

        new public static void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class SteamEmitter

}   // end of namespace Boku.Common
