
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
    // Emits "smoke" as a series of expanding, rotating 
    // sprites which fade and grow as they age.


    public class SmokeEmitter : BaseSpriteEmitter
    {
        private static Texture2D texture = null;

        #region accessors
        protected override Texture2D Texture
        {
            get { return texture; }
        }
        #endregion

      

        // c'tor
        public SmokeEmitter(ParticleSystemManager manager)
            : base(manager)
        {
            Color = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
            StartRadius = 0.2f;
            EndRadius = 3.0f;
            StartAlpha = 1.0f;
            EndAlpha = 0.0f;
            MinLifetime = 1.5f;     // Particle lifetime.
            MaxLifetime = 4.0f;
            EmissionRate = 50.0f;   // Particles per second.

            ExplicitBloom = 0.004f;     // ~1.0f / 255.0f;

            MaxRotationRate = 2.0f;
        }   // end of c'tor

        new public static void LoadContent(bool immediate)
        {
            // Load the texture.
            if (SmokeEmitter.texture == null)
            {
                SmokeEmitter.texture = KoiLibrary.LoadTexture2D(@"Textures\Smoke");
            }
        }   // end of SmokeEmitter LoadContent()

        new public static void InitDeviceResources(GraphicsDevice device)
        {
        }

        new public static void UnloadContent()
        {
            SmokeEmitter.texture = null;
        }   // end of SmokeEmitter UnloadContent()

        new public static void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class SmokeEmitter

}   // end of namespace Boku.Common

