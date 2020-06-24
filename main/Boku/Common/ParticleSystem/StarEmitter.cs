using System;
using System.Collections.Generic;
using System.Text;

using KoiX;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace Boku.Common.ParticleSystem
{
    class StarEmitter : WreathEmitter
    {
        protected static Texture2D starTexture = null;

        #region accessors
        protected override Texture2D Texture
        {
            get { return starTexture; }
            set { starTexture = value; }
        }
        #endregion accessors

        public StarEmitter(ParticleSystemManager manager)
            : base(manager)
        {
            WreathRadius = 0.5f;
            WreathRate = -1.0f;
            NumTiles = 2;
        }   // end of c'tor

        new public static void LoadContent(bool immediate)
        {
            // Load the texture.
            if (starTexture == null)
            {
                starTexture = KoiLibrary.LoadTexture2D(@"Textures/StarPlanet");
            }
        }   // end of WreathEmitter LoadContent()

        new public static void InitDeviceResources(GraphicsDevice device)
        {
        }

        new public static void UnloadContent()
        {
            StarEmitter.starTexture = null;
        }

        new public static void DeviceReset(GraphicsDevice device)
        {
        }

    }
}
