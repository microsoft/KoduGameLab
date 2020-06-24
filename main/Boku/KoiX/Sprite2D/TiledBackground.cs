
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace KoiX
{
    /// <summary>
    /// Class for scrolling, rotating, tiled backgrounds
    /// meant to be used with SpriteCamera.
    /// </summary>
    public class TiledBackground : IDeviceResetX
    {
        #region Members

        BasicEffect effect;

        Texture2D texture;
        string textureName;

        VertexPositionTexture[] vertices = new VertexPositionTexture[4];
        short[] indices = { 0, 1, 2, 2, 1, 3 };

        float zoom = 1.0f;      // Local zoom factor.  Multiplied with camera zoom.
        //float parallax = 1.0f;  // Controls ratio of background scrolling to camera scrolling.

        float alpha = 1.0f;

        #endregion

        #region Accessors

        /// <summary>
        /// Local zoom factor.  Multiplied with camera zoom.
        /// </summary>
        public float Zoom
        {
            get { return zoom; }
            set { zoom = value; }
        }

        #endregion

        #region Public

        public TiledBackground(string textureName, float alpha = 1.0f)
        {
            this.textureName = textureName;
            this.alpha = alpha;

            vertices[0] = new VertexPositionTexture(new Vector3(-1,-1, 0), new Vector2(0, 0));
            vertices[1] = new VertexPositionTexture(new Vector3(-1, 1, 0), new Vector2(0, 1));
            vertices[2] = new VertexPositionTexture(new Vector3(1, -1, 0), new Vector2(1, 0));
            vertices[3] = new VertexPositionTexture(new Vector3(1, 1, 0), new Vector2(1, 1));
        }

        public void Update(SpriteCamera camera)
        {
            // Update UV coords to take camera and zoom factor into account.
            // With zoom == 1.0 we should have 1 to 1 pixel scaling.
            // Camera at 0, 0 should have texture centered on screen.

            Vector2 screenSize = camera.ScreenSize;
            Vector2 textureSize = new Vector2(texture.Width, texture.Height);

            // Default positions in pixels.
            vertices[0].TextureCoordinate = new Vector2(0, screenSize.Y); 
            vertices[1].TextureCoordinate = new Vector2(0, 0);
            vertices[2].TextureCoordinate = new Vector2(screenSize.X, screenSize.Y);
            vertices[3].TextureCoordinate = new Vector2(screenSize.X, 0);

            // Magic half pixel offset.
            vertices[0].TextureCoordinate += new Vector2(0.5f);
            vertices[1].TextureCoordinate += new Vector2(0.5f);
            vertices[2].TextureCoordinate += new Vector2(0.5f);
            vertices[3].TextureCoordinate += new Vector2(0.5f);

            // Get inverse of camera transform.
            Matrix mat = camera.InverseViewMatrix;

            // Adjust for texture zoom if needed.
            if (zoom != 0.0f)
            {
                mat *= Matrix.CreateScale(1.0f / zoom);
            }

            for (int i = 0; i < 4; i++)
            {
                // Transform using camera.
                vertices[i].TextureCoordinate = Vector2.Transform(vertices[i].TextureCoordinate, mat);

                // Convert from pixels to UV coords.
                vertices[i].TextureCoordinate /= textureSize;
            }

        }   // end of Update()

        public void Render(SpriteCamera camera)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            SamplerState prevSampleState = device.SamplerStates[0];
            device.SamplerStates[0] = SamplerState.LinearWrap;

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserIndexedPrimitives<VertexPositionTexture>(PrimitiveType.TriangleList, vertices, 0, 4, indices, 0, 2);
            }

            // Restore sampler state.
            device.SamplerStates[0] = prevSampleState;

        }   // end of Render()

        #endregion

        #region Internal
        #endregion

        #region IDeviceReset Members

        public void LoadContent()
        {
            if (effect == null)
            {
                effect = new BasicEffect(KoiLibrary.GraphicsDevice);

                effect.LightingEnabled = false;
                effect.Alpha = alpha;

                effect.World = Matrix.Identity;
                effect.View = Matrix.Identity;
                effect.Projection = Matrix.CreateOrthographic(2, 2, -10, 10);

            }

            if (texture == null)
            {
                texture = KoiLibrary.LoadTexture2D(textureName);

                effect.TextureEnabled = true;
                effect.Texture = texture;
            }
        }

        public void UnloadContent()
        {
            DeviceResetX.Release(ref effect);
            DeviceResetX.Release(ref texture);
        }

        public void DeviceResetHandler(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        #endregion
    }   // end of class TiledBackground

}   // end of namespace KoiX
