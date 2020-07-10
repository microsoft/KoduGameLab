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
    /// Common functionality used for sliders.  The actual instances will 
    /// differ in whether they are integer or float valued.
    /// </summary>
    public abstract class UIGridBaseModularSliderElement : UIGridElement
    {
        private static Effect effect = null;

        protected RenderTarget2D diffuse = null;
        private Texture2D normalMap = null;
        private string normalMapName = null;
        protected Texture2D sliderWhite = null;
        protected Texture2D sliderBlack = null;
        protected Texture2D sliderBeadEnd = null;
        protected Texture2D sliderBeadMiddle = null;

        protected float displayValue = 0.0f;    // Used by the twitches to smoothly change the slider position.
                                                // This is the value currently being displayed as opposed
                                                // to the actual current value.

        // Properties for the underlying 9-grid geometry.
        private float width;
        private float height;
        private float edgeSize;

        private Base9Grid geometry = null;

        protected bool selected = false;
        protected bool useRightStick = false;   

        protected Vector4 specularColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        protected float specularPower = 8.0f;

        protected string label = null;
        protected Color textColor;
        protected Color dropShadowColor;
        protected bool useDropShadow = false;
        protected bool invertDropShadow = false;    // Puts the drop shadow above the regular letter instead of below.
        protected Justification justify = Justification.Left;


        #region fast-scrolling support
        private bool fastScrolling;
        private float fastScrollTime = 1.5f;
        private int fastScrollScalar = 5;
        private double lastNonPressTime;
        #endregion


        #region Accessors
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
                            HelpOverlay.Push("ModularButtonElement");
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
            set { /* Should be removed */ }
        }

        /// <summary>
        /// The amount of time of continuous input before we switch to fast scrolling.
        /// </summary>
        public float FastScrollTime
        {
            get { return fastScrollTime; }
            set { fastScrollTime = Math.Max(0, value); }
        }

        /// <summary>
        /// The scroll increment multiplier to employ when fast scrolling.
        /// </summary>
        public int FastScrollScalar
        {
            get { return fastScrollScalar; }
            set { fastScrollScalar = Math.Max(0, value); }
        }

        /// <summary>
        /// True if we are currently fast scrolling.
        /// </summary>
        public bool FastScrolling
        {
            get { return fastScrolling; }
            private set { fastScrolling = value; }
        }

        /// <summary>
        /// By default the sliders look for input on the left stick.
        /// When UseRightStick is set to true, they look for the right 
        /// stick input and ignore the left stick.
        /// </summary>
        public bool UseRightStick
        {
            get { return useRightStick; }
            set { useRightStick = value; }
        }

        #endregion

        // c'tor
        /// <summary>
        /// Simple c'tor using a blob to hold the common data.
        /// </summary>
        /// <param name="blob"></param>
        /// <param name="label"></param>
        public UIGridBaseModularSliderElement(ParamBlob blob, string label)
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
        public UIGridBaseModularSliderElement(float width, float height, float edgeSize, string normalMapName, Color baseColor, string label, Shared.GetFont font, Justification justify, Color textColor)
        {
            this.width = width;
            this.height = height;
            this.edgeSize = edgeSize;
            //this.baseColor = baseColor.ToVector4();

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
        public UIGridBaseModularSliderElement(float width, float height, float edgeSize, string normalMapName, Color baseColor, string label, Shared.GetFont font, Justification justify, Color textColor, Color dropShadowColor, bool invertDropShadow)
        {
            this.width = width;
            this.height = height;
            this.edgeSize = edgeSize;
            //this.baseColor = baseColor.ToVector4();

            this.normalMapName = normalMapName;

            this.Font = font;
            this.label = TextHelper.FilterInvalidCharacters(label);
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

                bool leftStickTouched = pad.DPadLeft.IsPressed || pad.LeftStickLeft.IsPressed || pad.DPadRight.IsPressed || pad.LeftStickRight.IsPressed;
                bool rightStickTouched = pad.RightStickLeft.IsPressed || pad.RightStickRight.IsPressed;
                bool keyboardTouched = KeyboardInput.IsPressed(Keys.Left) || KeyboardInput.IsPressed(Keys.Right);

                // If there is no scroll input currently, reset fast scroll timer.
                if (useRightStick)
                {
                    if(!rightStickTouched)
                    {
                        lastNonPressTime = Time.WallClockTotalSeconds;
                    }
                }
                else
                {
                    // Using left stick.
                    if (!leftStickTouched && !keyboardTouched)
                    {
                        lastNonPressTime = Time.WallClockTotalSeconds;
                    }
                }

                double time = Time.WallClockTotalSeconds;
                double delta = time - lastNonPressTime;
                FastScrolling = (Time.WallClockTotalSeconds - lastNonPressTime > FastScrollTime);

                // Handle input changes here.
                if (useRightStick)
                {
                    if (pad.RightStickLeft.WasPressedOrRepeat)
                    {
                        if (DecrementCurrentValue())
                            Foley.PlayClickDown();
                    }
                    if (pad.RightStickRight.WasPressedOrRepeat)
                    {
                        if (IncrementCurrentValue())
                            Foley.PlayClickUp();
                    }
                }
                else
                {
                    if (Actions.ComboLeft.WasPressedOrRepeat)
                    {
                        if (DecrementCurrentValue())
                            Foley.PlayClickDown();
                    }
                    if (Actions.ComboRight.WasPressedOrRepeat)
                    {
                        if (IncrementCurrentValue())
                            Foley.PlayClickUp();
                    }
                }
            }

            RefreshTexture();

            base.Update(ref parentMatrix);
        }   // end of UIGridBaseModularSliderElement Update()

        public override void HandleMouseInput(Vector2 hitUV)
        {
            // Press in slider region?
            if (MouseInput.Left.WasPressed && hitUV.Y > 0.5f)
            {
                MouseInput.ClickedOnObject = this;
            }

            if (MouseInput.ClickedOnObject == this && MouseInput.Left.IsPressed)
            {
                // Adjust for ends of slide not filling all the way to border of shape.
                float value = hitUV.X;
                value = (value - 0.05f) / 0.9f;
                value = MyMath.Clamp(value, 0.0f, 1.0f);

                float delta = GetSliderPercentage() - value;
                if(SetSliderPercentage(value))
                {
                    // Only make nose if the value changes.
                    if (delta > 0)
                    {
                        Foley.PlayClickDown();
                    }
                    else if (delta < 0)
                    {
                        Foley.PlayClickUp();
                    }
                }
            }

        }   // end of HandleMouseInput()

        public override void HandleTouchInput(TouchContact touch, Vector2 hitUV)
        {
            // Press in slider region?
            if (touch.phase == TouchPhase.Began && hitUV.Y > 0.5f)
            {
                touch.TouchedObject = this;
            }

            if (touch.TouchedObject == this && TouchPhase.Stationary != touch.phase)
            {
                // Adjust for ends of slide not filling all the way to border of shape.
                float value = hitUV.X;
                value = (value - 0.05f) / 0.9f;
                value = MyMath.Clamp(value, 0.0f, 1.0f);

                float delta = GetSliderPercentage() - value;
                if (SetSliderPercentage(value))
                {
                    // Only make nose if the value changes.
                    if (delta > 0)
                    {
                        Foley.PlayClickDown();
                    }
                    else if (delta < 0)
                    {
                        Foley.PlayClickUp();
                    }
                }
            }

        }   // end of HandleTouchInput()

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

        }   // end of UIGridBaseModularSliderElement Render()

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
                Vector2 size = new Vector2(w, h);
                quad.Render(sliderWhite, position, size, "TexturedNoAlpha");

                // And the black part.
                int blackHeight = 70;   // From Photoshop...
                position.Y = h - blackHeight;
                size.Y = blackHeight;
                quad.Render(sliderBlack, position, size, "TexturedRegularAlpha");

                // Disable writing to alpha channel.
                // This prevents transparent fringing around the text.
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
                device.BlendState = UI2D.Shared.BlendStateColorWriteRGB;

                // Render the label and value text into the texture.
                int margin = 0;
                position.X = 0;
                position.Y = (int)(((h - blackHeight) - Font().LineSpacing) / 2.0f) - 2;
                int textWidth = (int)(Font().MeasureString(label).X);

                justify = Justification.Center;
                position.X = TextHelper.CalcJustificationOffset(margin, w, textWidth, justify);

                Color labelColor = new Color(127, 127, 127);
                Color valueColor = new Color(140, 200, 63);
                Color shadowColor = new Color(0, 0, 0, 20);
                Vector2 shadowOffset = new Vector2(0, 6);

                SpriteBatch batch = UI2D.Shared.SpriteBatch;
                batch.Begin();
                TextHelper.DrawString(Font, label, position + shadowOffset, shadowColor);
                TextHelper.DrawString(Font, label, position, labelColor);

                string valueString = GetFormattedValue();
                margin = 48;
                position.X = w - margin - (int)Font().MeasureString(valueString).X;
                TextHelper.DrawString(Font, valueString, position, valueColor);
                batch.End();

                // Render the value bead.
                int left = 22;
                int top = 93;
                int right = w - left;
                int verticalRadius = 8;
                int horizontalRadius = 6;

                float percent = GetSliderPercentage();
                int len = right - left - horizontalRadius * 2;
                len = (int)(len * percent);
                len = Math.Max(len, 7);
                right = len + left + horizontalRadius * 2 - 2;

                quad.Render(sliderBeadEnd, new Vector2(left, top), new Vector2(horizontalRadius, verticalRadius * 2), "TexturedRegularAlpha");
                quad.Render(sliderBeadEnd, new Vector2(right, top), new Vector2(-horizontalRadius, verticalRadius * 2), "TexturedRegularAlpha");
                quad.Render(sliderBeadMiddle, new Vector2(left + horizontalRadius, top), new Vector2(len - 2, verticalRadius * 2), "TexturedRegularAlpha");

                /*
                // Render help button.
                if (ShowHelpButton)
                {
                    position.X = w - 54;
                    position.Y = h - 54;
                    size = new Vector2(64, 64);
                    quad.Render(ButtonTextures.YButton, position, size, "TexturedRegularAlpha");
                    position.X -= 10 + (int)Font().MeasureString(Strings.Localize("editObjectParams.help")).X;
                    batch.Begin();
                    TextHelper.DrawString(Font, Strings.Localize("editObjectParams.help"), position + shadowOffset, shadowColor);
                    TextHelper.DrawString(Font, Strings.Localize("editObjectParams.help"), position, fontColor);
                    batch.End();
                }
                */

                // Restore default blend state.
                device.BlendState = BlendState.AlphaBlend;

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
        /// Returns a count of the number of stops the slider has based
        /// on the min value, max value and increment.
        /// </summary>
        /// <returns></returns>
        public abstract float GetNumStops();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value">Percentage value to set.  Should be in [0, 1] range.</param>
        /// <returns>True if this action caused the value to change.</returns>
        public abstract bool SetSliderPercentage(float value);

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
                effect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\UI2D");
                ShaderGlobals.RegisterEffect("UI2D", effect);
            }

            // Load the normal map texture.
            if (normalMapName != null)
            {
                normalMap = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\" + normalMapName);
            }

            // Load the textures.
            if (sliderWhite == null)
            {
                sliderWhite = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\SliderWhite");
            }
            if (sliderBlack == null)
            {
                sliderBlack = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\SliderBlack");
            }
            if (sliderBeadEnd == null)
            {
                sliderBeadEnd = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\SliderBeadEnd");
            }
            if (sliderBeadMiddle == null)
            {
                sliderBeadMiddle = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\SliderBeadMiddle");
            }

        }   // end of UIGridBaseModularSliderElement LoadContent()

        public override void InitDeviceResources(GraphicsDevice device)
        {
            CreateRenderTargets(device);

            // Create the geometry.
            geometry = new Base9Grid(width, height, edgeSize);

            BokuGame.Load(geometry, true);
        }

        public override void UnloadContent()
        {
            base.UnloadContent();

            ReleaseRenderTargets();

            BokuGame.Release(ref effect);
            BokuGame.Release(ref normalMap);
            BokuGame.Release(ref sliderWhite);
            BokuGame.Release(ref sliderBlack);
            BokuGame.Release(ref sliderBeadEnd);
            BokuGame.Release(ref sliderBeadMiddle);

            BokuGame.Unload(geometry);
            geometry = null;
        }   // end of UIGridBaseModularSliderElement UnloadContent()

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
            diffuse = new RenderTarget2D(device, w, h, false, SurfaceFormat.Color, DepthFormat.None);
            InGame.GetRT("UIGrid2DBaseSliderElement", diffuse);

            // Refresh the texture.
            dirty = true;
            RefreshTexture();
        }

        private void ReleaseRenderTargets()
        {
            InGame.RelRT("UIGrid2DBaseSliderElement", diffuse);
            BokuGame.Release(ref diffuse);
        }

    }   // end of class UIGridBaseModularSliderElement

}   // end of namespace Boku.UI2D






