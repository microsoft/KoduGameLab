using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

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
    public class FanEmitter : BaseSpriteEmitter
    {
        private static Texture2D texture = null;

        private GameThing attachTo = null;

        private bool bForwards = true;

        #region accessors
        protected override Texture2D Texture
        {
            get { return texture; }
        }
        public GameThing AttachTo
        {
            get { return attachTo; }
            set
            {
                attachTo = value;
            }
        }

        public bool Forwards
        {
            get { return bForwards; }
            set
            {
                bForwards = value;
            }
        }
        #endregion

        // c'tor
        public FanEmitter(ParticleSystemManager manager)
            : base(manager)
        {
            StartRadius = 0.1f;
            EndRadius = 0.1f;
  
            StartAlpha = 1.0f;
            EndAlpha = 1.0f;

            MinLifetime = 0.5f;     // Particle lifetime.
            MaxLifetime = 2.0f;

            EmissionRate = 4.0f;  // Particles per second.

            MaxSpeed = 5.0f;

            PositionJitter = 0.7f;  // Random offset for each particle.
            Gravity = new Vector3(0.0f, 0.0f, 0.0f);
            Color = new Vector4(1.0f, 1.01f, 1.0f, 0.8f);
            MaxRotationRate = 0.0f;

            NumTiles = 2;
        }   // end of c'tor

        new public static void LoadContent(bool immediate)
        {
            // Load the texture.
            if (FanEmitter.texture == null)
            {
                FanEmitter.texture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures/FanBlow");
            }
        }   // end of ExplosionEmitter LoadContent()

        new public static void InitDeviceResources(GraphicsDevice device)
        {
        }

        new public static void UnloadContent()
        {
            FanEmitter.texture = null;
        }   // end of ExplosionEmitter UnloadContent()

        new public static void DeviceReset(GraphicsDevice device)
        {
        }

        public override void Update()
        {
            base.Update();

            if (attachTo != null)            
            {
                // Use modifier to match vfx with actual range
                InitParticleVelocity = Vector3.Normalize(attachTo.Movement.Facing) * ((attachTo as GameActor).FinalPushRange / MaxLifetime) * 0.9f;

                if (!bForwards)
                {
                    InitParticleVelocity *= -1.0f;
                }
            }
        }

    }   // end of class FanEmitter
}