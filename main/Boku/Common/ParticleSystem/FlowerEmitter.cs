
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
    // Emits "Flower puffs" as a series of expanding, rotating 
    // sprites which fade and grow as they age.  These are to
    // be used when an object is being dragged or when it 
    // bounces on the ground.
    public class FlowerEmitter : BaseSpriteEmitter
    {
        private static Texture2D texture = null;

        #region accessors
        protected override Texture2D Texture
        {
            get { return texture; }
        }
        /// <summary>
        /// Avoid the lighting effecting your color. These are arcade effect
        /// that don't want or need realistic lighting.
        /// </summary>
        public override bool IsEmissive
        {
            get { return true; }
        }

        #endregion

        // c'tor
        public FlowerEmitter(ParticleSystemManager manager)
            : base(manager)
        {
            EmissionRate = 15.0f;
            Color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            StartRadius = 0.1f;
            EndRadius = 0.5f;
            PositionJitter = 0.5f;
            StartAlpha = 1.0f;
            EndAlpha = 0.0f;
            MinLifetime = 2.5f;       // Particle lifetime.
            MaxLifetime = 5.0f;

            MaxRotationRate = 2.0f;

            // Have flowers float up a bit.
            Gravity = new Vector3(0.0f, 0.0f, 0.2f);
            MaxSpeed = 10.0f;
        }   // end of c'tor

        new public static void LoadContent(bool immediate)
        {
            // Load the texture.
            if (FlowerEmitter.texture == null)
            {
                FlowerEmitter.texture = KoiLibrary.LoadTexture2D(@"Textures/Daisy01");
            }
        }   // end of FlowerEmitter LoadContent()

        new public static void InitDeviceResources(GraphicsDevice device)
        {
        }

        new public static void UnloadContent()
        {
            FlowerEmitter.texture = null;
        }   // end of FlowerEmitter UnloadContent()

        new public static void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class FlowerEmitter

}   // end of namespace Boku.Common
