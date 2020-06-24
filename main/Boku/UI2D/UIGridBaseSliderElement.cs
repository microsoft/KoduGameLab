
using System;
using System.Collections;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Input;
using KoiX.Text;

using Boku.Audio;
using Boku.Common;
using Boku.Fx;

namespace Boku.UI2D
{
    /// <summary>
    /// Common functionality used for sliders.  The actual instances will 
    /// differ in whether they are integer or float valued.
    /// </summary>
    public abstract class UIGridBaseSliderElement : UIGridElement
    {
        private static Effect effect = null;

        protected RenderTarget2D diffuse = null;
        private Texture2D normalMap = null;
        protected Texture2D sliderBox = null;
        private string normalMapName = null;

        protected float displayValue = 0.0f;    // Used by the twitches to smoothly change the slider position.
                                                // This is the value currently being displayed as opposed
                                                // to the actual current value.

        // Properties for the underlying 9-grid geometry.
        private float width;
        private float height;
        private float edgeSize;

        private Base9Grid geometry = null;

        protected Vector4 baseColor;        // Color that shows through where the texture is transparent.
        protected Vector4 selectedColor;
        protected Vector4 unselectedColor;
        protected bool selected = false;

        protected Vector4 specularColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        protected float specularPower = 8.0f;

        protected string label = null;
        protected Color textColor;
        protected Color dropShadowColor;
        protected bool useDropShadow = false;
        protected bool invertDropShadow = false;    // Puts the drop shadow above the regular letter instead of below.
        protected TextHelper.Justification justify = TextHelper.Justification.Left;

        #region Accessors
        /// <summary>
        /// Set or cleared by the owning grid to tell this element whether it's the selected element.
        /// </summary>
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
                        HelpOverlay.Push(@"ModularSlider");
                    }
                    else
                    {
                        HelpOverlay.Pop();
                    }
                }
            }
        }
        /// <summary>
        /// Label string for the UI element.
        /// </summary>
        public override string Label
        {
            get { return label; }
            set { label = TextHelper.FilterInvalidCharacters(value); }
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
        public Color TextColor
        {
            get { return textColor; }
        }
        public Color DropShadowColor
        {
            get { return dropShadowColor; }
        }
        public bool UseDropShadow
        {
            get { return useDropShadow; }
        }
        public bool InvertDropShadow
        {
            get { return invertDropShadow; }
        }
        public float Width
        {
            get { return width; }
        }
        public float Height
        {
            get { return height; }
        }

        /// <summary>
        /// Exposed to allow custom rendering of the texture.
        /// </summary>
        public RenderTarget2D Diffuse
        {
            get { return diffuse; }
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
        public UIGridBaseSliderElement(ParamBlob blob, string label)
        {
            this.label = TextHelper.FilterInvalidCharacters(label);

            // blob
            this.width = blob.width;
            this.height = blob.height;
            this.edgeSize = blob.edgeSize;
            this.selectedColor = blob.selectedColor.ToVector4();
            this.unselectedColor = blob.unselectedColor.ToVector4();
            this.baseColor = unselectedColor;

            this.Font = blob.Font;
            this.textColor = blob.textColor;
            this.useDropShadow = blob.useDropShadow;
            this.invertDropShadow = blob.invertDropShadow;
            this.dropShadowColor = blob.dropShadowColor;
            this.justify = blob.justify;

            this.normalMapName = blob.normalMapName;

        }

        /// <summary>
        /// Long form c'tor for use with no drop shadow.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="edgeSize"></param>
        /// <param name="normalMapName"></param>
        /// <param name="baseColor"></param>
        /// <param name="label"></param>
        /// <param name="justify"></param>
        /// <param name="textColor"></param>
        public UIGridBaseSliderElement(float width, float height, float edgeSize, string normalMapName, Color baseColor, String label, GetFont font, TextHelper.Justification justify, Color textColor)
        {
            this.width = width;
            this.height = height;
            this.edgeSize = edgeSize;
            this.baseColor = baseColor.ToVector4();

            this.normalMapName = normalMapName;

            this.Font = font;
            this.label = label;
            this.justify = justify;
            this.textColor = textColor;
            useDropShadow = false;

        }

        /// <summary>
        /// Long for c'tor for use with a drop shadow.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="edgeSize"></param>
        /// <param name="normalMapName"></param>
        /// <param name="baseColor"></param>
        /// <param name="label"></param>
        /// <param name="justify"></param>
        /// <param name="textColor"></param>
        /// <param name="dropShadowColor"></param>
        /// <param name="invertDropShadow"></param>
        public UIGridBaseSliderElement(float width, float height, float edgeSize, string normalMapName, Color baseColor, String label, GetFont font, TextHelper.Justification justify, Color textColor, Color dropShadowColor, bool invertDropShadow)
        {
            this.width = width;
            this.height = height;
            this.edgeSize = edgeSize;
            this.baseColor = baseColor.ToVector4();

            this.normalMapName = normalMapName;

            this.Font = font;
            this.label = label;
            this.justify = justify;
            this.textColor = textColor;
            this.dropShadowColor = dropShadowColor;
            useDropShadow = true;
            this.invertDropShadow = invertDropShadow;

        }

        public override void Update(ref Matrix parentMatrix)
        {
            // Check for input but only if selected.
            if (selected)
            {
                GamePadInput pad = GamePadInput.GetGamePad0();

                // Handle input changes here.
                if (pad.DPadLeft.WasPressed
                    || pad.DPadLeft.WasRepeatPressed
                    || pad.LeftStickLeft.WasPressed
                    || pad.LeftStickLeft.WasRepeatPressed)
                {
                    if(DecrementCurrentValue())
                        Foley.PlayClick();
                }
                if (pad.DPadRight.WasPressed
                    || pad.DPadRight.WasRepeatPressed
                    || pad.LeftStickRight.WasPressed
                    || pad.LeftStickRight.WasRepeatPressed)
                {
                    if(IncrementCurrentValue())
                        Foley.PlayClick();
                }
            }

            RefreshTexture();

            base.Update(ref parentMatrix);
        }   // end of UIGridBaseSliderElement Update()

        public override void HandleMouseInput(Vector2 hitUV)
        {
        }   // end of HandleMouseInput()

        public override void HandleTouchInput(TouchContact touch, Vector2 hitUV)
        {
        }   // end of HandleTouchInput()

        public override void Render(Camera camera)
        {
            if (diffuse == null)
            {
                effect.CurrentTechnique = effect.Techniques["NormalMappedNoTexture"];
            }
            else
            {
                effect.CurrentTechnique = effect.Techniques["NormalMapped"];
                effect.Parameters["DiffuseTexture"].SetValue(diffuse);
            }

            effect.Parameters["WorldMatrix"].SetValue(worldMatrix);
            effect.Parameters["WorldViewProjMatrix"].SetValue(worldMatrix * camera.ViewProjectionMatrix);

            effect.Parameters["Alpha"].SetValue(BaseColor.W);
            effect.Parameters["DiffuseColor"].SetValue(baseColor);
            effect.Parameters["SpecularColor"].SetValue(specularColor);
            effect.Parameters["SpecularPower"].SetValue(specularPower);
            effect.Parameters["NormalMap"].SetValue(normalMap);

            geometry.Render(effect);

        }   // end of UIGridBaseSliderElement Render()

        /// <summary>
        /// If the state of the element has changed, we may need to re-create the texture.
        /// </summary>
        public void RefreshTexture()
        {
            if (dirty)
            {
                InGame.SetRenderTarget(diffuse);
                InGame.Clear(Color.Transparent);

                int width = diffuse.Width;
                int height = diffuse.Height;

                ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();

                // Render the slide under the box.
                int margin = 36;
                float aspectRatio = 5.25f;      // Based on art, should be more flexible.
                Vector4 sliderColor = Color.Red.ToVector4();
                Vector2 position = new Vector2(margin, margin);
                Vector2 size = new Vector2(aspectRatio * (height - 2.0f * margin), height - 2.0f * margin);
                // Scale size based on current value and ranges.
                size.X *= GetSliderPercentage();
                quad.Render(sliderColor, position, size);

                // Calc position of text overlay.
                int x = (int)(position.X + aspectRatio * (height - 2.0f * margin) / 2.0f);
                int y = (height - Font().LineSpacing) / 2;

                // Render the slider box over the slider itself.
                margin = 24;
                aspectRatio = 4.0f;   // Based on art, should be more flexible.
                position = new Vector2(margin, margin);
                size = new Vector2(aspectRatio * (height - 2.0f * margin), height - 2.0f * margin);
                Vector4 lightGrey = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);
                quad.Render(sliderBox, lightGrey, position, size, @"TexturedRegularAlpha");

                // Render the current slider value in the center of the slider area.
                string valueString = GetFormattedValue();
                x -= (int)(Font().MeasureString(valueString).X) / 2;

                SpriteBatch batch = KoiLibrary.SpriteBatch;
                batch.Begin();
                if (useDropShadow)
                {
                    TextHelper.DrawStringWithShadow(Font, batch, x, y, valueString, textColor, dropShadowColor, false);
                }
                else
                {
                    TextHelper.DrawString(Font, valueString, new Vector2(x, y), textColor);
                }
                batch.End();

                // Render the label text into the texture.
                margin += (int)size.X + 16;
                x = 0;
                y = (int)((height - Font().LineSpacing) / 2.0f) - 2;
                int textWidth = (int)(Font().MeasureString(label).X);

                x = TextHelper.CalcJustificationOffset(margin, width, textWidth, justify);

                batch.Begin();
                TextHelper.DrawStringWithShadow(Font, batch, x, y, label, textColor, dropShadowColor, invertDropShadow);
                batch.End();

                // Render help button.
                if (ShowHelpButton)
                {
                    x = width - 54;
                    y = height - 54;
                    position = new Vector2(x, y);
                    size = new Vector2(64, 64);
                    quad.Render(ButtonTextures.YButton, position, size, "TexturedRegularAlpha");
                    x -= 10 + (int)Font().MeasureString(Strings.Localize("editObjectParams.help")).X;
                    batch.Begin();
                    TextHelper.DrawStringWithShadow(Font, batch, x, y, Strings.Localize("editObjectParams.help"), textColor, dropShadowColor, invertDropShadow);
                    batch.End();
                }

                // Restore backbuffer.
                InGame.RestoreRenderTarget();

                dirty = false;
            }
        }   // end of UIGridIntegerSliderElement RefreshTexture()

        /// <summary>
        /// Returns 0..1 indicating how full the slider should be rendered.
        /// </summary>
        /// <returns></returns>
        public abstract float GetSliderPercentage();

        /// <summary>
        /// Returns the current value formatted properly for overlaying on the slider.
        /// </summary>
        /// <returns></returns>
        public abstract string GetFormattedValue();

        /// <summary>
        /// Increments the current slider value by whatever increment the user specified.
        /// </summary>
        /// <returns>True if the value changed.</returns>
        public abstract bool IncrementCurrentValue();

        /// <summary>
        /// Decrements the current slider value by whatever increment the user specified.
        /// </summary>
        /// <returns>True if the value changed.</returns>
        public abstract bool DecrementCurrentValue();

        public override void LoadContent(bool immediate)
        {
            // Init the effect.
            if (effect == null)
            {
                effect = KoiLibrary.LoadEffect(@"Shaders\UI2D");
                ShaderGlobals.RegisterEffect("UI2D", effect);
            }

            // Load the normal map texture.
            if (normalMapName != null)
            {
                normalMap = KoiLibrary.LoadTexture2D(@"Textures\UI2D\" + normalMapName);
            }

            // Load the textures.
            if (sliderBox == null)
            {
                sliderBox = KoiLibrary.LoadTexture2D(@"Textures\UI2D\WhiteSliderBox");
            }

        }   // end of UIGridBaseSliderElement LoadContent()

        public override void InitDeviceResources(GraphicsDevice device)
        {
            const int dpi = 128;
            int w = (int)(dpi * width);
            int h = (int)(dpi * height);

            // Create the geometry.
            if (BokuGame.RequiresPowerOf2)
            {
                float u = (float)w / (float)MyMath.GetNextPowerOfTwo(w);
                float v = (float)h / (float)MyMath.GetNextPowerOfTwo(h);
                geometry = new Base9Grid(width, height, edgeSize, u, v);
            }
            else
            {
                geometry = new Base9Grid(width, height, edgeSize);
            }

            CreateRenderTargets(device);

            BokuGame.Load(geometry, true);
        }

        public override void UnloadContent()
        {
            base.UnloadContent();

            ReleaseRenderTargets();

            DeviceResetX.Release(ref effect);
            DeviceResetX.Release(ref normalMap);
            DeviceResetX.Release(ref sliderBox);

            BokuGame.Unload(geometry);
            geometry = null;
        }   // end of UIGridBaseSliderElement UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public override void DeviceReset(GraphicsDevice device)
        {
            ReleaseRenderTargets();
            CreateRenderTargets(device);
        }

        private void CreateRenderTargets(GraphicsDevice device)
        {
            const int dpi = 128;
            int w = (int)(dpi * width);
            int h = (int)(dpi * height);

            // Create the diffuse texture.
            int originalWidth = w;
            int originalHeight = h;
            if (BokuGame.RequiresPowerOf2)
            {
                w = MyMath.GetNextPowerOfTwo(w);
                h = MyMath.GetNextPowerOfTwo(h);
            }

            diffuse = new RenderTarget2D(device, w, h, false, SurfaceFormat.Color, DepthFormat.None);
            SharedX.GetRT("UIGrid2DBaseSliderElement", diffuse);

            // Refresh the texture.
            dirty = true;
            RefreshTexture();
        }

        private void ReleaseRenderTargets()
        {
            SharedX.RelRT("UIGrid2DBaseSliderElement", diffuse);
            DeviceResetX.Release(ref diffuse);
        }

    }   // end of class UIGridBaseSliderElement

}   // end of namespace Boku.UI2D






