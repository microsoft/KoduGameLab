
using System;
using System.Collections;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Common;
using Boku.Programming;
using Boku.Fx;

namespace Boku.UI2D
{
    /// <summary>
    /// An instance of UIElement specific to the programming help.
    /// It allows a foreground and background texture to be composited.  
    /// The background texture has selected and unselected versions.
    /// The foreground texture name is expected to be CardSpace upid.
    /// </summary>
    public class UIGrid2DDualTextureElement : UIGridElement
    {
        #region Members

        private static Effect effect = null;

        private Texture2D foreTexture = null;
        private string foreTextureName = null;

        private Texture2D backSelectedTexture = null;
        private Texture2D backUnselectedTexture = null;

        private float width;
        private float height;
        private bool noZ = false;

        private bool selected = false;

        #endregion

        #region Accessors

        public override bool Selected
        {
            get { return selected; }
            set
            {
                if (selected != value)
                {
                    selected = value;
                    if (selected)
                    {
                        // Create a twitch to change to selected.
                        TwitchManager.Set<float> set = delegate(float val, Object param) { alpha = val; };
                        TwitchManager.CreateTwitch<float>(alpha, 1.0f, set, 0.15, TwitchCurve.Shape.EaseInOut);

                        // Also change grey state.
                        //TwitchGrey(0.0f, 0.15f, TwitchCurve.Shape.EaseInOut);
                    }
                    else
                    {
                        // Create a twitch to change to unselected.
                        TwitchManager.Set<float> set = delegate(float val, Object param) { alpha = val; };
                        TwitchManager.CreateTwitch<float>(alpha, 0.0f, set, 0.15, TwitchCurve.Shape.EaseInOut);

                        // Also change grey state.
                        //TwitchGrey(1.0f, 0.15f, TwitchCurve.Shape.EaseInOut);
                    }
                }
            }
        }
        public float Width
        {
            get { return width; }
        }
        public float Height
        {
            get { return height; }
        }
        public bool NoZ
        {
            get { return noZ; }
            set { noZ = value; }
        }

        public override Vector2 Size
        {
            get { return new Vector2(width, height); }
            set { /* do nothing, should be removed from base class */ }
        }

        #endregion

        // c'tor
        /// <summary>
        /// Simple c'tor using a blob to hold the common data.
        /// </summary>
        /// <param name="blob"></param>
        /// <param name="label"></param>
        public UIGrid2DDualTextureElement(ParamBlob blob, string foreTextureName)
        {
            this.foreTextureName = foreTextureName;

            // blob
            this.width = blob.width;
            this.height = blob.height;

            // Use to blend selected/unselected.  Start unselected.
            alpha = 0.0f;
        }


        public void Update()
        {
            Matrix parentMatrix = Matrix.Identity;

            base.Update(ref parentMatrix);

            if (dirty)
            {
            }

        }   // end of UIGrid2DDualTextureElement Update()

        public override void HandleMouseInput(Vector2 hitUV)
        {
        }   // end of HandleMouseInput()


        public override void HandleTouchInput(TouchContact touch, Vector2 hitUV)
        {
        }  // end of HandleTouchInput()


        public override void Render(Camera camera)
        {
            CameraSpaceQuad quad = CameraSpaceQuad.GetInstance();

            Vector2 size = new Vector2(width, height);
            Vector2 pos = new Vector2(worldMatrix.Translation.X, worldMatrix.Translation.Y);

            // Not fully selected?
            if (alpha < 1.0f)
            {
                quad.Render(camera, backUnselectedTexture, 0.5f, pos, size, @"TexturedRegularAlpha");
            }

            // Not fully unselected?
            if (alpha > 0.0f)
            {
                quad.Render(camera, backSelectedTexture, alpha, pos, size, @"TexturedRegularAlpha");
            }

            quad.Render(camera, foreTexture, pos, size, @"TexturedRegularAlpha");

        }   // end of UIGrid2DDualTextureElement Render()

        public override void LoadContent(bool immediate)
        {
            // Init the effect.
            if (effect == null)
            {
                effect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\UI2D");
                ShaderGlobals.RegisterEffect("UI2D", effect);
            }

            // Load the diffuse texture.
            if (foreTextureName != null)
            {
                foreTexture = Boku.Programming.CardSpace.Cards.CardFaceTexture(foreTextureName);
            }

            // Load the overlay texture.
            if (backSelectedTexture == null)
            {
                backSelectedTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\HelpCard\GreenSquare");
            }
            if (backUnselectedTexture == null)
            {
                backUnselectedTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\HelpCard\GreySquare");
            }

        }   // end of UIGrid2DDualTextureElement LoadContent()

        public override void InitDeviceResources(GraphicsDevice device)
        {
        }

        public override void UnloadContent()
        {
            base.UnloadContent();

            BokuGame.Release(ref foreTexture);
            BokuGame.Release(ref backSelectedTexture);
            BokuGame.Release(ref backUnselectedTexture);

        }   // end of UIGrid2DDualTextureElement UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public override void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class UIGrid2DDualTextureElement

}   // end of namespace Boku.UI2D






