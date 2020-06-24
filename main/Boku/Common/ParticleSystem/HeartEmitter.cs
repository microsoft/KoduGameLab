
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
    // Emits "Heart puffs" as a series of expanding, rotating 
    // sprites which fade and grow as they age.  These are to
    // be used when an object is being dragged or when it 
    // bounces on the ground.
    public class HeartEmitter : BaseSpriteEmitter
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
        public HeartEmitter(ParticleSystemManager manager)
            : base(manager)
        {
            EmissionRate = 20.0f;
            Color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            StartRadius = 0.1f;
            EndRadius = 0.5f;
            PositionJitter = 0.5f;
            StartAlpha = 1.0f;
            EndAlpha = 0.0f;
            MinLifetime = 3.0f;         // Particle lifetime.
            MaxLifetime = 6.0f;

            MaxRotationRate = 0.0f;

            // Have hearts float up a bit.
            Gravity = new Vector3(0.0f, 0.0f, 0.2f);
            MaxSpeed = 10.0f;
        }   // end of c'tor

        new public static void LoadContent(bool immediate)
        {
            // Load the texture.
            if (HeartEmitter.texture == null)
            {
                HeartEmitter.texture = KoiLibrary.LoadTexture2D(@"Textures/Heart01");
            }
        }   // end of HeartEmitter LoadContent()

        new public static void InitDeviceResources(GraphicsDevice device)
        {
        }

        new public static void UnloadContent()
        {
            HeartEmitter.texture = null;
        }   // end of HeartEmitter UnloadContent()

        new public static void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class HeartEmitter

}   // end of namespace Boku.Common
