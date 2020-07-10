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
    /// Totally custom element for choosing the camera mode.
    /// </summary>
    public class UIGridModularCameraModeElement : UIGridElement
    {
        public delegate void UIModularEvent(int index);

        private static Effect effect = null;

        private RenderTarget2D diffuse = null;

        private string normalMapName = null;
        private Texture2D normalMap = null;

        private Texture2D white = null;
        private Texture2D black = null;
        private Texture2D middleBlack = null;
        private Texture2D icons = null;
        private Texture2D indicatorLit = null;
        private Texture2D indicatorUnlit = null;

        private int curIndex = 0;   // Current selection.

        private UIModularEvent onSetCamera = null;
        private UIModularEvent onXButton = null;

        private string xButtonText = null;

        private AABB2D xButtonBox = new AABB2D();       // Bounding box for X button hits.
        private AABB2D iconButtonBox = new AABB2D();    // Bounding box around all icons.

        // Properties for the underlying 9-grid geometry.
        private float width;
        private float height;
        private float edgeSize;

        private Base9Grid geometry = null;

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

        public int CurIndex
        {
            get { return curIndex; }
            set { curIndex = value; dirty = true; }
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
                    selected = value;
                    if (selected)
                    {
                        if (SetHelpOverlay)
                        {
                            HelpOverlay.Push(@"ModularCameraMode");
                        }
                    
                        TwitchManager.Set<float> set = delegate(float val, Object param) { dim = val; };
                        TwitchManager.CreateTwitch<float>(dim, 1.0f, set, 0.2f, TwitchCurve.Shape.EaseInOut);
                    }
                    else
                    {
                        if (SetHelpOverlay)
                        {
                            HelpOverlay.Pop();
                        }

                        TwitchManager.Set<float> set = delegate(float val, Object param) { dim = val; };
                        TwitchManager.CreateTwitch<float>(dim, 0.5f, set, 0.2f, TwitchCurve.Shape.EaseInOut);
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

        /// <summary>
        /// Delegate to be called on return from SetCamera.
        /// Called by external source, ie this object just
        /// holds on to the delegate but doesn't know when
        /// to call it.
        /// </summary>
        public UIModularEvent OnSetCamera
        {
            get { return onSetCamera; }
            set { onSetCamera = value; }
        }
        /// <summary>
        /// Delegate to be called when the XButton is pressed.
        /// </summary>
        public UIModularEvent OnXButton
        {
            set { onXButton = value; }
        }

        #endregion

        // c'tor
        /// <summary>
        /// Simple c'tor using a blob to hold the common data.
        /// </summary>
        /// <param name="blob"></param>
        /// <param name="label"></param>
        public UIGridModularCameraModeElement(ParamBlob blob, string label)
        {
            this.label = TextHelper.FilterInvalidCharacters(label);

            // blob
            this.width = blob.width;
            this.height = blob.height;
            this.edgeSize = blob.edgeSize;

            this.Font = blob.Font;
            this.textColor = blob.textColor;
            this.useDropShadow = blob.useDropShadow;
            this.invertDropShadow = blob.invertDropShadow;
            this.dropShadowColor = blob.dropShadowColor;
            this.justify = blob.justify;

            this.normalMapName = blob.normalMapName;

        }

        /// <summary>
        /// Set an optional callback and text string for the element.
        /// </summary>
        /// <param name="onXButton"></param>
        /// <param name="text"></param>
        public void SetXButton(UIModularEvent onXButton, string text)
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

                if (Actions.ComboRight.WasPressedOrRepeat)
                {
                    Actions.ComboRight.ClearAllWasPressedState();

                    curIndex = (curIndex + 1) % 3;
                    Foley.PlayClickUp();
                    dirty = true;
                }

                if (Actions.ComboLeft.WasPressedOrRepeat)
                {
                    Actions.ComboLeft.ClearAllWasPressedState();

                    curIndex = (curIndex + 3 - 1) % 3;
                    Foley.PlayClickDown();
                    dirty = true;
                }

                if (Actions.X.WasPressed)
                {
                    Actions.X.ClearAllWasPressedState();

                    if (onXButton != null)
                    {
                        onXButton(curIndex);
                    }
                }
            }

            RefreshTexture();

            base.Update(ref parentMatrix);
        }   // end of UIGridModularCameraModeElement Update()

        private int clickedOnIndex = -1;
        public override void HandleMouseInput(Vector2 hitUV)
        {
            if (xButtonBox.Contains(hitUV))
            {
                if (MouseInput.Left.WasPressed)
                {
                    MouseInput.ClickedOnObject = xButtonBox;
                    clickedOnIndex = -1;
                }

                if (MouseInput.Left.WasReleased && MouseInput.ClickedOnObject == xButtonBox)
                {
                    if (onXButton != null)
                        onXButton(curIndex);
                    clickedOnIndex = -1;
                }
            }
            else if (iconButtonBox.Contains(hitUV))
            {
                if (MouseInput.Left.WasPressed)
                {
                    MouseInput.ClickedOnObject = iconButtonBox;
                    clickedOnIndex = (int)(3.0f * (hitUV.X - iconButtonBox.Min.X) / (iconButtonBox.Max.X  - iconButtonBox.Min.X));
                }

                if (MouseInput.Left.WasReleased)
                {
                    // Make sure we're still over the ClickedOnItem.
                    if (MouseInput.ClickedOnObject == iconButtonBox)
                    {
                        int newIndex = (int)(3.0f * (hitUV.X - iconButtonBox.Min.X) / (iconButtonBox.Max.X  - iconButtonBox.Min.X));

                        if (newIndex == clickedOnIndex && newIndex != CurIndex)
                        {
                            CurIndex = newIndex;
                        }
                    }
                    clickedOnIndex = -1;
                }
            }
        }   // end of HandleMouseInput()



        public override void HandleTouchInput(TouchContact touch, Vector2 hitUV)
        {
            /// \TODO: JB implement
            if (xButtonBox.Contains(hitUV))
            {
                if (TouchInput.WasTouched)
                {
                    touch.TouchedObject= xButtonBox;
                    clickedOnIndex = -1;
                }

                if (TouchInput.WasReleased && touch.TouchedObject == xButtonBox)
                {
                    if (onXButton != null)
                        onXButton(curIndex);
                    clickedOnIndex = -1;
                }
            }
            else if (iconButtonBox.Contains(hitUV))
            {
                if (TouchInput.WasTouched)
                {
                    touch.TouchedObject = iconButtonBox;
                    clickedOnIndex = (int)(3.0f * (hitUV.X - iconButtonBox.Min.X) / (iconButtonBox.Max.X - iconButtonBox.Min.X));
                }

                if (TouchInput.WasReleased)
                {
                    // Make sure we're still over the ClickedOnItem.
                    if (touch.TouchedObject == iconButtonBox)
                    {
                        int newIndex = (int)(3.0f * (hitUV.X - iconButtonBox.Min.X) / (iconButtonBox.Max.X - iconButtonBox.Min.X));

                        if (newIndex == clickedOnIndex && newIndex != CurIndex)
                        {
                            CurIndex = newIndex;
                        }
                    }
                    clickedOnIndex = -1;
                }
            }
        }


        public override void Render(Camera camera)
        {
            effect.CurrentTechnique = effect.Techniques["NormalMappedWithEnv"];
            effect.Parameters["DiffuseTexture"].SetValue(diffuse);

            effect.Parameters["WorldMatrix"].SetValue(worldMatrix);
            effect.Parameters["WorldViewProjMatrix"].SetValue(worldMatrix * camera.ViewProjectionMatrix);

            effect.Parameters["Alpha"].SetValue(Alpha);
            effect.Parameters["DiffuseColor"].SetValue(new Vector4(dim, dim, dim, 1.0f));
            effect.Parameters["SpecularColor"].SetValue(specularColor);
            effect.Parameters["SpecularPower"].SetValue(specularPower);

            effect.Parameters["NormalMap"].SetValue(normalMap);

            geometry.Render(effect);

        }   // end of UIGridModularCameraModeElement Render()

        /// <summary>
        /// If the state of the element has changed, we may need to re-create the texture.
        /// </summary>
        public void RefreshTexture()
        {
            if (dirty || diffuse.IsContentLost)
            {
                InGame.SetRenderTarget(diffuse);
                InGame.Clear(Color.White);

                int w = diffuse.Width;
                int h = diffuse.Height;

                ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();
                

                // Render the white background.
                Vector2 position = Vector2.Zero;
                Vector2 size = new Vector2(w, white.Height);
                quad.Render(white, position, size, "TexturedNoAlpha");

                // And the black parts.
                position.Y = 70;
                size.Y = h - 70;
                quad.Render(middleBlack, position, size, "TexturedRegularAlpha");
                position.Y = 64;
                size.Y = black.Height;
                quad.Render(black, position, size, "TexturedRegularAlpha");

                // The icons.
                position.X = (512 - icons.Width) / 2;
                position.Y = 80;
                size = new Vector2(icons.Width, icons.Height);
                quad.Render(icons, position, size, "TexturedRegularAlpha");

                // Bounding box
                Vector2 min = new Vector2(position.X / w, position.Y / h);
                Vector2 max = new Vector2((position.X + size.X) / w, (140 + 2.0f * indicatorLit.Height) / h);
                iconButtonBox.Set(min, max);

                // The indicators.
                size = new Vector2(indicatorLit.Width, indicatorLit.Height);
                position = new Vector2(105, 140);
                quad.Render(CurIndex == 0 ? indicatorLit : indicatorUnlit, position, size, "TexturedRegularAlpha");
                position = new Vector2(512 / 2 - size.X / 2, 140);
                quad.Render(CurIndex == 1 ? indicatorLit : indicatorUnlit, position, size, "TexturedRegularAlpha");
                position = new Vector2(512 - 105 - size.X, 140);
                quad.Render(CurIndex == 2 ? indicatorLit : indicatorUnlit, position, size, "TexturedRegularAlpha");


                // Disable writing to alpha channel.
                // This prevents transparent fringing around the text.
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
                device.BlendState = UI2D.Shared.BlendStateColorWriteRGB;

                // Render the label and value text into the texture.
                string title = label + " : ";
                switch (CurIndex)
                {
                    case 0:
                        title += Strings.Localize("editWorldParams.cameraModeFixedPosition");
                        break;
                    case 1:
                        title += Strings.Localize("editWorldParams.cameraModeFixedOffset");
                        break;
                    case 2:
                        title += Strings.Localize("editWorldParams.cameraModeFree");
                        break;
                }
                int margin = 0;
                position.X = 0;
                position.Y = (int)((64 - Font().LineSpacing) / 2.0f);
                int textWidth = (int)(Font().MeasureString(title).X);

                justify = Justification.Center;
                position.X = TextHelper.CalcJustificationOffset(margin, w, textWidth, justify);

                Color labelColor = new Color(127, 127, 127);
                Color valueColor = new Color(140, 200, 63);
                Color shadowColor = new Color(0, 0, 0, 20);
                Vector2 shadowOffset = new Vector2(0, 6);

                SpriteBatch batch = UI2D.Shared.SpriteBatch;
                batch.Begin();

                // Title.
                TextHelper.DrawString(Font, title, position + shadowOffset, shadowColor);
                TextHelper.DrawString(Font, title, position, labelColor);

                batch.End();

                if (xButtonText != null)
                {
                    UI2D.Shared.GetFont ButtonFont = UI2D.Shared.GetGameFont18Bold;
                    position.X = w - 44;
                    position.Y = h - 44;
                    size = new Vector2(48, 48);
                    quad.Render(ButtonTextures.XButton, position, size, "TexturedRegularAlpha");

                    max.X = (position.X + 44) / w;
                    min.Y = position.Y / h;

                    position.X -= 10 + (int)ButtonFont().MeasureString(Strings.Localize("editWorldParams.setCamera")).X;
                    batch.Begin();
                    TextHelper.DrawString(ButtonFont, Strings.Localize("editWorldParams.setCamera"), position + shadowOffset, shadowColor);
                    TextHelper.DrawString(ButtonFont, Strings.Localize("editWorldParams.setCamera"), position, labelColor);
                    batch.End();

                    min.X = position.X / w;
                    max.Y = min.Y + (float)ButtonFont().LineSpacing / h;

                    xButtonBox.Set(min, max);
                }

                // Restore default blend state.
                device.BlendState = BlendState.AlphaBlend;

                // Restore backbuffer.
                InGame.RestoreRenderTarget();

                dirty = false;
            }
        }   // end of UIGridModularCameraModeElement Render()

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
            if (white == null)
            {
                white = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\SliderWhite");
            }
            if (black == null)
            {
                black = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\RadioBoxBlack");
            }
            if (middleBlack == null)
            {
                middleBlack = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\MiddleBlack");
            }
            if (icons == null)
            {
                icons = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\CameraModeIcons");
            }
            if (indicatorLit == null)
            {
                indicatorLit = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\IndicatorLit");
            }
            if (indicatorUnlit == null)
            {
                indicatorUnlit = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\IndicatorUnlit");
            }

        }   // end of UIGridModularCameraModeElement LoadContent()

        const int w = 512;
        const int h = 210;

        public override void InitDeviceResources(GraphicsDevice device)
        {
            height = h * width / w;

            // Create the geometry.
            geometry = new Base9Grid(width, height, edgeSize);

            CreateRenderTargets(device);

            BokuGame.Load(geometry, true);

        }

        public override void UnloadContent()
        {
            base.UnloadContent();

            ReleaseRenderTargets();

            BokuGame.Release(ref effect);
            BokuGame.Release(ref normalMap);
            BokuGame.Release(ref white);
            BokuGame.Release(ref black);
            BokuGame.Release(ref middleBlack);
            BokuGame.Release(ref icons);
            BokuGame.Release(ref indicatorLit);
            BokuGame.Release(ref indicatorUnlit);

            BokuGame.Unload(geometry);
            geometry = null;
        }   // end of UIGridModularCameraModeElement UnloadContent()

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
            // Create the diffuse texture.
            diffuse = new RenderTarget2D(device, w, h, false, SurfaceFormat.Color, DepthFormat.None);
            InGame.GetRT("UIGridModularCameraModeElement", diffuse);

            // Refresh the texture.
            dirty = true;
            RefreshTexture();
        }

        private void ReleaseRenderTargets()
        {
            InGame.RelRT("UIGridModularCameraModeElement", diffuse);
            BokuGame.Release(ref diffuse);
        }

    }   // end of class UIGridModularCameraModeElement

}   // end of namespace Boku.UI2D






