using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace Boku.Common.ParticleSystem
{
    class SwearEmitter : WreathEmitter
    {
        protected static Texture2D swearTexture = null;

        #region accessors
        protected override Texture2D Texture
        {
            get { return swearTexture; }
            set { swearTexture = value; }
        }
        #endregion accessors

        public SwearEmitter(ParticleSystemManager manager)
            : base(manager)
        {
            WreathRadius = 0.75f;
            WreathRate = 0.5f;
            NumTiles = 8;
            MaxRotationRate = 0.0f;
        }   // end of c'tor

        new public static void LoadContent(bool immediate)
        {
            // Load the texture.
            if (swearTexture == null)
            {
                swearTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures/Swear");
            }
        }

        new public static void InitDeviceResources(GraphicsDevice device)
        {
        }

        new public static void UnloadContent()
        {
            SwearEmitter.swearTexture = null;
        }

        new public static void DeviceReset(GraphicsDevice device)
        {
        }

    }
}
