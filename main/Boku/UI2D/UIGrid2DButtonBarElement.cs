// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Common;
using Boku.Fx;
using Boku.Programming;

namespace Boku.UI2D
{
    /// <summary>
    /// An instance of UIElement designed to fit at the bottom of a dialog
    /// and contain buttons.
    /// </summary>
    public class UIGrid2DButtonBarElement : UIGridElement
    {
        #region Members

        private static Effect effect = null;

        private Texture2D normalMap = null;
        private string normalMapName = null;
        private Texture2D diffuse = null;
        private string diffuseTextureName = null;

        // Properties for the underlying 9-grid geometry.
        private float width;
        private float height;
        private float edgeSize;
        private bool noZ = false;
        private bool ignorePowerOf2 = false;

        private Base9Grid geometry = null;

        private Vector4 baseColor;          // Color that shows through where the texture is transparent.
        private Vector4 selectedColor;
        private Vector4 unselectedColor;
        private bool selected = false;

        private Vector4 specularColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        private float specularPower = 32.0f;

        private bool altShader = false;
        private bool altPreMultAlphaShader = false;
        private bool greyFlatShader = false;

        #endregion

        #region Accessors

        public override bool Selected
        {
            get { return selected; }
            set
            {
                if (selected != value)
                {
                    if (value)
                    {
                        // Create a twitch to change to selected color
                        TwitchManager.Set<Vector4> set = delegate(Vector4 val, Object param) { baseColor = val; };
                        TwitchManager.CreateTwitch<Vector4>(baseColor, selectedColor, set, 0.15, TwitchCurve.Shape.EaseInOut);

                        // Also change grey state.
                        TwitchGrey(0.0f, 0.15f, TwitchCurve.Shape.EaseInOut);
                    }
                    else
                    {
                        // Create a twitch to change to unselected color.
                        TwitchManager.Set<Vector4> set = delegate(Vector4 val, Object param) { baseColor = val; };
                        TwitchManager.CreateTwitch<Vector4>(baseColor, unselectedColor, set, 0.15, TwitchCurve.Shape.EaseInOut);

                        // Also change grey state.
                        TwitchGrey(1.0f, 0.15f, TwitchCurve.Shape.EaseInOut);
                    }
                    selected = value;
                }
            }
        }
        public Color SpecularColor
        {
            set { specularColor = value.ToVector4(); }
        }
        public float SpecularPower
        {
            set { specularPower = value; }
        }
        public Vector4 BaseColor
        {
            set { baseColor = value; }
            get { return baseColor; }
        }
        public Texture2D DiffuseTexture
        {
            get { return diffuse; }
            set { diffuse = value; }
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

        /// <summary>
        /// Uses an alternate shader for rendering.  This shader treats the texture
        /// contents as fully lit and doesn't attenuate the textured pixels by the 
        /// alpha of the object color.
        /// </summary>
        public bool AltShader
        {
            get { return altShader; }
            set { altShader = value; }
        }

        /// <summary>
        /// Uses an alternate shader for rendering.  This shader treats the texture
        /// contents as fully lit and doesn't attenuate the textured pixels by the 
        /// alpha of the object color.  Assumes pre-multiplied alpha in the texture.
        /// </summary>
        public bool AltPreMultAlphaShader
        {
            get { return altPreMultAlphaShader; }
            set { altPreMultAlphaShader = value; }
        }

        /// <summary>
        /// Should be set to true if the texture for this tile is not from a generated rendertarget and should always be stretched fully across the tile.
        /// </summary>
        public bool IgnorePowerOf2
        {
            get { return ignorePowerOf2; }
            set { ignorePowerOf2 = value; }
        }

        #endregion

        // c'tor
        /// <summary>
        /// Simple c'tor using a blob to hold the common data.
        /// </summary>
        /// <param name="blob"></param>
        /// <param name="label"></param>
        public UIGrid2DButtonBarElement(ParamBlob blob)
        {
            this.diffuseTextureName = @"GridElements\RadioBoxBlack";

            // blob
            this.width = blob.width;
            this.height = blob.height;
            this.edgeSize = blob.edgeSize;
            this.selectedColor = blob.selectedColor.ToVector4();
            this.unselectedColor = blob.unselectedColor.ToVector4();
            this.baseColor = unselectedColor;
            this.altShader = blob.altShader;
            this.altPreMultAlphaShader = blob.altPreMultAlphaShader;
            this.greyFlatShader = blob.greyFlatShader;
            this.ignorePowerOf2 = blob.ignorePowerOf2;

            this.normalMapName = blob.normalMapName;
            this.elementName = blob.elementName;
        }

        public void Update()
        {
            Matrix parentMatrix = Matrix.Identity;

            base.Update(ref parentMatrix);
        }   // end of UIGrid2DButtonBar Update()

        public override void HandleMouseInput(Vector2 hitUV)
        {
        }   // end of HandleMouseInput()

        public override void HandleTouchInput(TouchContact touch, Vector2 hitUV)
        {
        }   // end of HandleTouchInput()

        public override void Render(Camera camera)
        {
            effect.CurrentTechnique = effect.Techniques["NormalMappedWithEnv"];
            effect.Parameters["DiffuseTexture"].SetValue(diffuse);

            effect.Parameters["WorldMatrix"].SetValue(worldMatrix);
            effect.Parameters["WorldViewProjMatrix"].SetValue(worldMatrix * camera.ViewProjectionMatrix);

            effect.Parameters["Alpha"].SetValue(Alpha);
            effect.Parameters["DiffuseColor"].SetValue(new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            effect.Parameters["SpecularColor"].SetValue(specularColor);
            effect.Parameters["SpecularPower"].SetValue(specularPower);

            effect.Parameters["NormalMap"].SetValue(normalMap);

            geometry.Render(effect);

        }   // end of UIGrid2DButtonBar Render()

        public override void LoadContent(bool immediate)
        {
            // Init the effect.
            if (effect == null)
            {
                effect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\UI2D");
                ShaderGlobals.RegisterEffect("UI2D", effect);
            }

            // Load the normal map texture.
            if (normalMapName != null)
            {
                normalMap = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\" + normalMapName);
            }

            // Load the diffuse texture.
            if (diffuseTextureName != null)
            {
                diffuse = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\" + diffuseTextureName);
            }

        }   // end of UIGrid2DButtonBar LoadContent()

        public override void InitDeviceResources(GraphicsDevice device)
        {
            const int dpi = 96;
            int w = (int)(dpi * width);
            int h = (int)(dpi * height);

            // Create the geometry.
            if (BokuGame.RequiresPowerOf2 && !IgnorePowerOf2)
            {
                float u = (float)w / (float)MyMath.GetNextPowerOfTwo(w);
                float v = (float)h / (float)MyMath.GetNextPowerOfTwo(h);
                geometry = new Base9Grid(width, height, edgeSize, u, v);
            }
            else
            {
                geometry = new Base9Grid(width, height, edgeSize);
            }

            BokuGame.Load(geometry, true);
        }

        public override void UnloadContent()
        {
            base.UnloadContent();

            BokuGame.Release(ref effect);
            BokuGame.Release(ref normalMap);
            BokuGame.Release(ref diffuse);

            BokuGame.Unload(geometry);
            geometry = null;
        }   // end of UIGrid2DButtonBar UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public override void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class UIGrid2DButtonBarElement

}   // end of namespace Boku.UI2D






