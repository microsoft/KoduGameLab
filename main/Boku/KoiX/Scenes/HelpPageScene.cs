
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI;

using KoiX.Geometry;

namespace KoiX.Scenes
{
    /// <summary>
    /// Placeholder class for displaying the old-style help pages.
    /// This just loads the texture and shows it.  Eventaully we need
    /// to completely rethink how we want this help to look.  Right 
    /// now it's pretty much a hold-over from the original Xbox release.
    /// </summary>
    public class HelpPageScene : BasePageScene
    {
        #region Members

        string textureName;
        Texture2D texture;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public HelpPageScene(string sceneName, string textureName)
            : base(sceneName)
        {
            this.textureName = textureName;
        }   // end of c'tor

        public override void Render(RenderTarget2D rt)
        {
            if (rt != null)
            {
                KoiLibrary.GraphicsDevice.SetRenderTarget(rt);
            }

            if(texture != null)
            {
                GraphicsDevice device = KoiLibrary.GraphicsDevice;
                device.Clear(Color.Black);

                // Start with texture size.
                Vector2 size = new Vector2(texture.Width, texture.Height);
                // Scale to tightly fit TargetResolution without distorting aspect ratio.
                Vector2 ratio = TargetResolution / size;
                size *= (float)Math.Min(ratio.X, ratio.Y);
                RectangleF rect = new RectangleF(-size / 2.0f, size);
                RoundedRect.Render(camera, rect, 0, Color.White, texture: texture);
            }

            if (rt != null)
            {
                KoiLibrary.GraphicsDevice.SetRenderTarget(null);
            }

            base.Render(rt);
        }   // end of Render()

        #endregion

        #region Internal

        public override void LoadContent()
        {
            texture = KoiLibrary.LoadTexture2D(textureName);

            base.LoadContent();
        }

        public override void UnloadContent()
        {
            DeviceResetX.Release(ref texture);

            base.UnloadContent();
        }

        #endregion

    }   // end of class HelpPageScene
}   // end of namespace KoiX.Scenes
