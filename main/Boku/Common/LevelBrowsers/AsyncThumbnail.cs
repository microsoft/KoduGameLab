using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;

namespace Boku.Common
{
    /// <summary>
    /// If the thumbnail has not yet been loaded, this object will return the "please wait" image.
    /// </summary>
    public partial class AsyncThumbnail : IDisposable
    {
        public static Texture2D pleaseWaitTexture;
        private Texture2D texture;

        public bool Loading;

        public bool IsLoaded
        {
            get { return texture != null; }
        }

        /// <summary>
        /// The texture to be used as a thumbnail. Do not assign textures loaded from ContentLoader,
        /// as this object will dispose of the texture resource when garbage-collected (assumes was
        /// created from a byte buffer).
        /// </summary>
        public Texture2D Texture
        {
            get { return texture ?? pleaseWaitTexture; }
            set { texture = value; }
        }

        ~AsyncThumbnail()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (texture != null)
            {
                // We need to dispose of these resources manually since they weren't loaded by a ContentManager.
                texture.Dispose();
                texture = null;
            }
        }

        public static void LoadContent(bool immediate)
        {
            pleaseWaitTexture = KoiLibrary.LoadTexture2D(@"Textures\LoadLevel\WaitLarge");
        }

        public static void InitDeviceResources(GraphicsDevice device)
        {
        }

        public static void UnloadContent()
        {
            DeviceResetX.Release(ref pleaseWaitTexture);
        }

        /// <summary>
        /// Recreate render targets.
        /// </summary>
        public static void DeviceReset(GraphicsDevice device)
        {
        }

    }

}
