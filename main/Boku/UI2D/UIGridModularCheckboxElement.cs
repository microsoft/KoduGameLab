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
    /// This is the new, modular version call so because of the way the
    /// parts fit together.
    /// </summary>
    public class UIGridModularCheckboxElement : UIGridElement
    {
        public delegate void UICheckboxEvent();

        private static Effect effect = null;

        private RenderTarget2D diffuse = null;

        private string normalMapName = null;
        private Texture2D normalMap = null;
        
        private Texture2D checkboxWhite = null;
        private Texture2D checkOff = null;
        private Texture2D checkOn = null;

        private bool check = false;             // Do we render the checkmark?

        private UICheckboxEvent onCheck = null;
        private UICheckboxEvent onClear = null;
        private UICheckboxEvent onXButton = null;

        private string xButtonText = null;  // If not null, this is shown just above the Y button text.

        private AABB2D xButtonBox = new AABB2D();       // Bounding box for X button hits.

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
                    selected = value;
                    if (selected)
                    {
                        HelpOverlay.Push("ModularCheckbox");

                        TwitchManager.Set<float> set = delegate(float val, Object param) { dim = val; };
                        TwitchManager.CreateTwitch<float>(dim, 1.0f, set, 0.2f, TwitchCurve.Shape.EaseInOut);
                    }
                    else
                    {
                        HelpOverlay.Pop();

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

        #endregion

        // c'tor
        /// <summary>
        /// Simple c'tor using a blob to hold the common data.
        /// </summary>
        /// <param name="blob"></param>
        /// <param name="label"></param>
        public UIGridModularCheckboxElement(ParamBlob blob, string label)
        {
            this.label = label;

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

                if (Actions.Select.WasPressed)
                {
                    Actions.Select.ClearAllWasPressedState();

                    ToggleState();
                }

                if (Actions.X.WasPressed)
                {
                    Actions.X.ClearAllWasPressedState();

                    if (onXButton != null)
                    {
                        onXButton();
                    }
                }
            }

            RefreshTexture();

            base.Update(ref parentMatrix);
        }   // end of UIGridModularCheckboxElement Update()

        private void ToggleState()
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

            Foley.PlayPressA();
            dirty = true;
        }   // end of ToggleState()

        public override void HandleMouseInput(Vector2 hitUV)
        {
            //check x button first
            if (xButtonBox.Contains(hitUV))
            {
                if (MouseInput.Left.WasPressed)
                {
                    MouseInput.ClickedOnObject = xButtonBox;
                }

                if (MouseInput.Left.WasReleased && MouseInput.ClickedOnObject == xButtonBox)
                {
                    if (onXButton != null)
                    {
                        onXButton();
                    }
                }
            }
            else 
            {
                // The hit region is the square at the left end of the tile.
                float maxU = height / width;
                if (hitUV.X < maxU)
                {
                    if (MouseInput.Left.WasPressed)
                    {
                        MouseInput.ClickedOnObject = this;
                    }
                    if (MouseInput.Left.WasReleased && MouseInput.ClickedOnObject == this)
                    {
                        ToggleState();
                    }
                }
            }
        }   // end of HandleMouseInput()

        public override void HandleTouchInput(TouchContact touch, Vector2 hitUV)
        {
            //check x button first
            if (xButtonBox.Contains(hitUV))
            {
                if (TouchInput.WasTouched)
                {
                    touch.TouchedObject = xButtonBox;
                }

                if (TouchInput.WasReleased && touch.TouchedObject == xButtonBox)
                {
                    if (onXButton != null)
                    {
                        onXButton();
                    }
                }
            }
            else
            {
                // The hit region is the square at the left end of the tile.
                float maxU = height / width;
                if (hitUV.X < maxU)
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        touch.TouchedObject = this;
                    }
                    if (touch.phase == TouchPhase.Ended && touch.TouchedObject == this)
                    {
                        ToggleState();
                    }
                }
            }
        }

        public override void Render(Camera camera)
        {
            effect.CurrentTechnique = effect.Techniques["NormalMappedWithEnv"];
            effect.Parameters["DiffuseTexture"].SetValue(diffuse);

            effect.Parameters["WorldMatrix"].SetValue(worldMatrix);
            effect.Parameters["WorldViewProjMatrix"].SetValue(worldMatrix * camera.ViewProjectionMatrix);

            effect.Parameters["Alpha"].SetValue(alpha);
            effect.Parameters["DiffuseColor"].SetValue(new Vector4(dim, dim, dim, 1.0f));
            effect.Parameters["SpecularColor"].SetValue(specularColor);
            effect.Parameters["SpecularPower"].SetValue(specularPower);

            effect.Parameters["NormalMap"].SetValue(normalMap);

            geometry.Render(effect);

        }   // end of UIGridModularCheckboxElement Render()

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


                // Render the white region with highlight.
                Vector2 position = new Vector2(h - 2, 0);
                Vector2 size = new Vector2(w, h) - position;
                quad.Render(checkboxWhite, position, size, "TexturedNoAlpha");

                // Render the checkbox.
                position = Vector2.Zero;
                size.X = size.Y;
                if (check)
                {
                    quad.Render(checkOn, position, size, "TexturedRegularAlpha");
                }
                else
                {
                    quad.Render(checkOff, position, size, "TexturedRegularAlpha");
                }

                // Disable writing to alpha channel.
                // This prevents transparent fringing around the text.
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
                device.BlendState = UI2D.Shared.BlendStateColorWriteRGB;

                // Render the label text into the texture.
                int margin = 16;
                position.X = (int)size.X + margin;

                TextBlob blob = new TextBlob(Font, label, w - (int)position.X - margin);

                position.Y = (int)((h - blob.TotalSpacing) / 2.0f) - 2;
                
                if (blob.NumLines == 2)
                {
                    position.Y -= blob.TotalSpacing / 2.0f;
                }
                else if (blob.NumLines == 3)
                {
                    position.Y -= blob.TotalSpacing;
                }

                Color fontColor = new Color(127, 127, 127);
                Color shadowColor = new Color(0, 0, 0, 20);
                Vector2 shadowOffset = new Vector2(0, 6);

                blob.RenderWithButtons(position, fontColor, shadowColor, shadowOffset, maxLines: 3);

                // Render help button.
                /*
                if (ShowHelpButton)
                {
                    position.X = w - 54;
                    position.Y = h - 54;
                    size = new Vector2(64, 64);
                    quad.Render(ButtonTextures.YButton, position, size, "TexturedRegularAlpha");
                    position.X -= 10 + (int)font.MeasureString(Strings.Localize("editObjectParams.help")).X;
                    batch.Begin();
                    TextHelper.DrawString(Font, Strings.Localize("editObjectParams.help"), position + shadowOffset, shadowColor);
                    TextHelper.DrawString(Font, Strings.Localize("editObjectParams.help"), position, fontColor);
                    batch.End();

                    if (xButtonText != null)
                    {
                        position.X = w - 54;
                        position.Y = h - 54 - Font().LineSpacing - 6;
                        size = new Vector2(64, 64);
                        quad.Render(ButtonTextures.XButton, position, size, "TexturedRegularAlpha");
                        position.X -= 10 + (int)Font().MeasureString(Strings.Localize("editWorldParams.setCamera")).X;
                        batch.Begin();
                        TextHelper.DrawString(Font, Strings.Localize("editWorldParams.setCamera"), position + shadowOffset, shadowColor);
                        TextHelper.DrawString(Font, Strings.Localize("editWorldParams.setCamera"), position, fontColor);
                        batch.End();
                    }
                }
                */

                if (xButtonText != null)
                {
                    position.X = w - 54;
                    position.Y = h - 54;
                    size = new Vector2(64, 64);
                    quad.Render(ButtonTextures.XButton, position, size, "TexturedRegularAlpha");

                    Vector2 min = Vector2.Zero;
                    Vector2 max = Vector2.Zero;

                    max.X = (position.X + 54) / w;
                    min.Y = position.Y / h;

                    position.X -= 10 + (int)Font().MeasureString(Strings.Localize("editWorldParams.setCamera")).X;
                    SpriteBatch batch = UI2D.Shared.SpriteBatch;
                    batch.Begin();
                    TextHelper.DrawString(Font, Strings.Localize("editWorldParams.setCamera"), position + shadowOffset, shadowColor);
                    TextHelper.DrawString(Font, Strings.Localize("editWorldParams.setCamera"), position, fontColor);
                    batch.End();

                    min.X = position.X / w;
                    max.Y = min.Y + (float)Font().LineSpacing / h;

                    xButtonBox.Set(min, max);
                }

                // Restore default blend state.
                device.BlendState = BlendState.AlphaBlend;

                // Restore backbuffer.
                InGame.RestoreRenderTarget();

                dirty = false;
            }
        }   // end of UIGridModularCheckboxElement Render()

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
            if (checkboxWhite == null)
            {
                checkboxWhite = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\CheckboxWhite");
            }
            if (checkOn == null)
            {
                checkOn = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\CheckboxOn");
            }
            if (checkOff == null)
            {
                checkOff = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\CheckboxOff");
            }

        }   // end of UIGridModularCheckboxElement LoadContent()

        public override void InitDeviceResources(GraphicsDevice device)
        {
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
            BokuGame.Release(ref checkboxWhite);
            BokuGame.Release(ref checkOn);
            BokuGame.Release(ref checkOff);

            BokuGame.Unload(geometry);
            geometry = null;
        }   // end of UIGridModularCheckboxElement UnloadContent()

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
            // Note this really works best if w = 5 * h
            int h = 128;
            int w = (int)(width / height * h);

            // Create the diffuse texture.
            diffuse = new RenderTarget2D(device, w, h, false, SurfaceFormat.Color, DepthFormat.None);
            InGame.GetRT("UIGridModularCheckboxElement", diffuse);

            // Refresh the texture.
            dirty = true;
            RefreshTexture();
        }

        private void ReleaseRenderTargets()
        {
            InGame.RelRT("UIGridModularCheckboxElement", diffuse);
            BokuGame.Release(ref diffuse);
        }

    }   // end of class UIGridModularCheckboxElement

}   // end of namespace Boku.UI2D






