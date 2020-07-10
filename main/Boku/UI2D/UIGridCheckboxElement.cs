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

using Boku.Audio;
using Boku.Common;
using Boku.Fx;

namespace Boku.UI2D
{
    /// <summary>
    /// An instance of UIElement that uses a 9-grid element for its geometry
    /// and creates a texture on the fly into which the checkbox and the 
    /// associated text string are rendered.
    /// </summary>
    public class UIGridCheckboxElement : UIGridElement
    {
        public delegate void UICheckboxEvent();

        private static Effect effect = null;

        private RenderTarget2D diffuse = null;
        private Texture2D normalMap = null;
        private Texture2D checkbox = null;
        private Texture2D checkmark = null;
        private string normalMapName = null;

        private bool check = false;             // Do we render the checkmark?

        private UICheckboxEvent onCheck = null;
        private UICheckboxEvent onClear = null;
        private UICheckboxEvent onXButton = null;

        private string xButtonText = null;  // If not null, this is shown just above the Y button text.

        // Properties for the underlying 9-grid geometry.
        private float width;
        private float height;
        private float edgeSize;

        private Base9Grid geometry = null;

        private Vector4 baseColor;          // Color that shows through where the texture is transparent.
        private Vector4 selectedColor;
        private Vector4 unselectedColor;
        private bool selected = false;

        private Vector4 specularColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        private float specularPower = 8.0f;

        private string label = null;
        private Color textColor;
        private Color dropShadowColor;
        private bool useDropShadow = false;
        private bool invertDropShadow = false;  // Puts the drop shadow above the regular letter instead of below.
        private Justification justify = Justification.Left;

        #region Accessors
        /// <summary>
        /// Is the checkbox checked?
        /// </summary>
        public bool Check
        {
            get { return check; }
            set 
            { 
                check = value; 
                dirty = true; 
            }
        }
        /// <summary>
        /// Delegate to be called when the box is checked.
        /// </summary>
        public UICheckboxEvent OnCheck
        {
            set { onCheck = value; }
        }
        /// <summary>
        /// Delegate to be called when the checkbox is cleared.
        /// </summary>
        public UICheckboxEvent OnClear
        {
            set { onClear = value; }
        }

        /// <summary>
        /// Set or cleared by the owning grid to tell this element whether it's the selected element or not.
        /// </summary>
        public override bool Selected
        {
            get { return selected; }
            set
            {
                if (selected != value)
                {
                    if (value)
                    {
                        HelpOverlay.Push(@"UIGridCheckbox");

                        // Create a twitch to change to selected color
                        TwitchManager.Set<Vector4> set = delegate(Vector4 val, Object param) { baseColor = val; };
                        TwitchManager.CreateTwitch<Vector4>(baseColor, selectedColor, set, 0.15, TwitchCurve.Shape.EaseInOut);
                    }
                    else
                    {
                        HelpOverlay.Pop();

                        // Create a twitch to change to unselected color.
                        TwitchManager.Set<Vector4> set = delegate(Vector4 val, Object param) { baseColor = val; };
                        TwitchManager.CreateTwitch<Vector4>(baseColor, unselectedColor, set, 0.15, TwitchCurve.Shape.EaseInOut);
                    }
                    selected = value;
                }
            }
        }
        /// <summary>
        /// Label string for the UI element.
        /// </summary>
        public override string Label
        {
            get { return label; }
            set { label = value; }
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
        public UIGridCheckboxElement(ParamBlob blob, string label)
        {
            this.label = label;

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
        public UIGridCheckboxElement(float width, float height, float edgeSize, string normalMapName, Color baseColor, string label, Shared.GetFont font, Justification justify, Color textColor)
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
        public UIGridCheckboxElement(float width, float height, float edgeSize, string normalMapName, Color baseColor, string label, Shared.GetFont font, Justification justify, Color textColor, Color dropShadowColor, bool invertDropShadow)
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

        /// <summary>
        /// Set an optional callback and text string for the element.
        /// </summary>
        /// <param name="onXButton"></param>
        /// <param name="text"></param>
        public void SetXButton(UICheckboxEvent onXButton, string text)
        {
            this.onXButton = onXButton;
            this.xButtonText = text;
            dirty = true;
        }   // end of SetXButton()

        public override void Update(ref Matrix parentMatrix)
        {
            // Check for input but only if selected.
            if (selected)
            {
                GamePadInput pad = GamePadInput.GetGamePad0();

                if (pad.ButtonA.WasPressed)
                {
                    check = !check;
                    if (check && onCheck != null)
                    {
                        onCheck();
                    }
                    else if (!check && onClear != null)
                    {
                        onClear();
                    }

                    Foley.PlayClick();
                    dirty = true;
                }

                if (pad.ButtonX.WasPressed)
                {
                    if (onXButton != null)
                    {
                        onXButton();
                    }
                }
            }

            RefreshTexture();

            base.Update(ref parentMatrix);
        }   // end of UIGridCheckboxElement Update()

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

        }   // end of UIGridCheckboxElement Render()

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
                

                // Render the checkbox.
                int margin = 24;
                Vector2 position = new Vector2(margin, margin);
                Vector2 size = new Vector2(height - 2.0f * margin, height - 2.0f * margin);
                Vector4 lightGrey = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);
                quad.Render(checkbox, lightGrey, position, size, @"TexturedRegularAlpha");

                // Render the checkmark.
                if (check)
                {
                    quad.Render(checkmark, position, size, @"TexturedRegularAlpha");
                }

                // Render the label text into the texture.
                margin += (int)size.X + 16;
                int x = 0;
                int y = (int)((height - Font().LineSpacing) / 2.0f) - 2;
                int textWidth = (int)(Font().MeasureString(label).X);

                x = TextHelper.CalcJustificationOffset(margin, width, textWidth, justify);

                SpriteBatch batch = UI2D.Shared.SpriteBatch;
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

                    if (xButtonText != null)
                    {
                        x = width - 54;
                        y = height - 54 - Font().LineSpacing - 6;
                        position = new Vector2(x, y);
                        size = new Vector2(64, 64);
                        quad.Render(ButtonTextures.XButton, position, size, "TexturedRegularAlpha");
                        x -= 10 + (int)Font().MeasureString(Strings.Localize("editWorldParams.setCamera")).X;
                        batch.Begin();
                        TextHelper.DrawStringWithShadow(Font, batch, x, y, Strings.Localize("editWorldParams.setCamera"), textColor, dropShadowColor, invertDropShadow);
                        batch.End();
                    }
                }

                // Restore backbuffer.
                InGame.RestoreRenderTarget();

                dirty = false;
            }
        }   // end of UIGridCheckboxElement Render()

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

            // Load the check textures.
            if (checkbox == null)
            {
                checkbox = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\WhiteCheckBox");
            }
            if (checkmark == null)
            {
                checkmark = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\WhiteCheck");
            }

        }   // end of UIGridCheckboxElement LoadContent()

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

            BokuGame.Release(ref effect);
            BokuGame.Release(ref normalMap);
            BokuGame.Release(ref checkbox);
            BokuGame.Release(ref checkmark);

            BokuGame.Unload(geometry);
            geometry = null;
        }   // end of UIGridCheckboxElement UnloadContent()

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
            InGame.GetRT("UIGridCheckBoxElement", diffuse);

            // Refresh the texture.
            dirty = true;
            RefreshTexture();
        }

        private void ReleaseRenderTargets()
        {
            InGame.RelRT("UIGridCheckBoxElement", diffuse);
            BokuGame.Release(ref diffuse);
        }

    }   // end of class UIGridCheckboxElement

}   // end of namespace Boku.UI2D
