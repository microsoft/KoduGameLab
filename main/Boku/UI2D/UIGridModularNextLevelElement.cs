// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.IO;
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
    public class UIGridModularNextLevelElement : UIGridElement
    {
        public delegate void UINextLevelEvent();

        private static Effect effect = null;

        private RenderTarget2D diffuse = null;

        private string normalMapName = null;
        private Texture2D normalMap = null;

        private Texture2D nextLevelWhite = null;
        private Texture2D nextLevelMiddleBlack = null;
        private Texture2D nextLevelBlack = null;
        private Texture2D nextLevelNone = null;

        private LevelMetadata nextLevel = null;

        private UINextLevelEvent onSetNextLevel = null;
        private UINextLevelEvent onClear = null;
        private UINextLevelEvent onXButton = null;

        // Properties for the underlying 9-grid geometry.
        private float width;
        private float height;
        private float edgeSize;

        private Base9Grid geometry = null;

        private bool selected = false;

        private Vector4 specularColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        private float specularPower = 8.0f;

        private Justification justify = Justification.Left;

        AABB2D mainHitBox = null;
        AABB2D clearHitBox = null;

        #region Accessors
        /// <summary>
        /// Is the checkbox checked?
        /// </summary>
        public LevelMetadata NextLevel
        {
            get { return nextLevel; }
            set
            {
                nextLevel = value;
                dirty = true;
            }
        }
        /// <summary>
        /// Delegate to be called when the box is checked.
        /// </summary>
        public UINextLevelEvent OnSetNextLevel
        {
            set { onSetNextLevel = value; }
        }
        /// <summary>
        /// Delegate to be called when the checkbox is cleared.
        /// </summary>
        public UINextLevelEvent OnClear
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
                        if (HelpOverlay.Peek() != "ModularNextLevel")
                        {
                            HelpOverlay.Push("ModularNextLevel");
                        }

                        TwitchManager.Set<float> set = delegate(float val, Object param) { dim = val; };
                        TwitchManager.CreateTwitch<float>(dim, 1.0f, set, 0.2f, TwitchCurve.Shape.EaseInOut);
                    }
                    else
                    {
                        if (HelpOverlay.Peek()=="ModularNextLevel")
                        {
                            HelpOverlay.Pop();
                        }

                        TwitchManager.Set<float> set = delegate(float val, Object param) { dim = val; };
                        TwitchManager.CreateTwitch<float>(dim, 0.5f, set, 0.2f, TwitchCurve.Shape.EaseInOut);
                    }
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
        public UIGridModularNextLevelElement(ParamBlob blob)
        {
            // blob
            this.width = blob.width;
            this.height = blob.height;
            this.edgeSize = blob.edgeSize;

            this.Font = blob.Font;
            this.justify = blob.justify;

            this.normalMapName = blob.normalMapName;

        }

        /// <summary>
        /// Set an optional callback and text string for the element.
        /// </summary>
        /// <param name="onXButton"></param>
        /// <param name="text"></param>
        public void SetXButton(UINextLevelEvent onXButton)
        {
            this.onXButton = onXButton;
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

                    onSetNextLevel();
                }

                if (Actions.X.WasPressed)
                {
                    Actions.X.ClearAllWasPressedState();

                    NextLevel = null;
                    if (onClear != null)
                    {
                        onClear();
                    }
                }
            }

            //load thumbnail if it hasn't loaded yet
            if (nextLevel != null && !nextLevel.Thumbnail.IsLoaded)
            {
                if (nextLevel.ThumbnailBytes == null)
                {
                    string texFilename = BokuGame.Settings.MediaPath + Utils.FolderNameFromFlags(nextLevel.Genres) + nextLevel.WorldId.ToString();
                    Stream texStream = Storage4.TextureFileOpenRead(texFilename);
                    if (texStream != null)
                    {
                        nextLevel.Thumbnail.Texture = Storage4.TextureLoad(texStream);
                        Storage4.Close(texStream);
                    }
                }
                else
                {
                    MemoryStream stream = new MemoryStream(nextLevel.ThumbnailBytes);
                    nextLevel.Thumbnail.Texture = Storage4.TextureLoad(stream);
                    nextLevel.ThumbnailBytes = null;
                }
            }

            //refresh the render target
            RefreshTexture();

            base.Update(ref parentMatrix);
        }   // end of UIGridModularCheckboxElement Update()

        public override void HandleMouseInput(Vector2 hitUV)
        {
            if (MouseInput.Left.WasPressed)
            {
                MouseInput.ClickedOnObject = this;
            }
            if (MouseInput.Left.WasReleased && MouseInput.ClickedOnObject == this)
            {
                //if the click was in the clear hit box, clear it out - otherwise, assume selection
                if (clearHitBox.Contains(hitUV))
                {
                    NextLevel = null;
                    if (onClear != null)
                    {
                        onClear();
                    }
                }
                else
                {
                    onSetNextLevel();
                }
            }
        }   // end of HandleMouseInput()

        public override void HandleTouchInput(TouchContact touch, Vector2 hitUV)
        {
            if (touch.phase == TouchPhase.Began)
            {
                touch.TouchedObject = this;
            }
            if (touch.phase == TouchPhase.Ended && touch.TouchedObject == this)
            {
                //if the touch was in the clear hit box, clear it out - otherwise, assume selection
                if (clearHitBox.Contains(hitUV))
                {
                    NextLevel = null;
                    if (onClear != null)
                    {
                        onClear();
                    }
                }
                else
                {
                    onSetNextLevel();
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

                // Render the white background.
                Vector2 position = Vector2.Zero;
                Vector2 size = new Vector2(w, nextLevelWhite.Height);
                quad.Render(nextLevelWhite, position, size, "TexturedNoAlpha");

                // And the black parts.
                position.Y = 70;
                size.Y = h - 70;
                quad.Render(nextLevelMiddleBlack, position, size, "TexturedRegularAlpha");
                position.Y = 64;
                size.Y = nextLevelBlack.Height;
                quad.Render(nextLevelBlack, position, size, "TexturedRegularAlpha");
                

                // Render the image.
                position.X = 6;
                position.Y = 70;
                size.X = size.Y = h - position.Y - 6;
                if (nextLevel!=null)
                {
                    //render the thumbnail when "on"
                    quad.Render(nextLevel.Thumbnail.Texture, position, size, "TexturedRegularAlpha");
                }
                else
                {
                    quad.Render(nextLevelNone, position, size, "TexturedRegularAlpha");
                }

                // Disable writing to alpha channel.
                // This prevents transparent fringing around the text.
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
                device.BlendState = UI2D.Shared.BlendStateColorWriteRGB;

                // Render the label text into the texture.
                int margin = 16;
                position.X = (int)size.X + margin;
                position.Y = (int)((128 - Font().LineSpacing) / 2.0f);
                TextBlob blob = null;

                if (nextLevel != null)
                {
                    blob = new TextBlob(Font, nextLevel.Name, w - (int)position.X - margin);
                }
                else
                {
                    blob = new TextBlob(Font, Strings.Localize("editWorldParams.noLevelSelected"), w - (int)position.X - margin);
                }

                position.Y = (int)((h - blob.TotalSpacing) / 2.0f) - 2;

                Color fontColor = new Color(127, 127, 127);
                Color shadowColor = new Color(0, 0, 0, 20);
                Vector2 shadowOffset = new Vector2(0, 6);

                //render the main label
                string label = Strings.Localize("editWorldParams.nextLevel");

                // prepare the label position
                int labelWidth = (int)(Font().MeasureString(label).X);
                Vector2 labelPosition = Vector2.Zero;
                labelPosition.X = TextHelper.CalcJustificationOffset(0, w, labelWidth, Justification.Center);
                labelPosition.Y = (int)((64 - Font().LineSpacing) / 2.0f);

                SpriteBatch batch = UI2D.Shared.SpriteBatch;
                batch.Begin();
                //render the main label
                TextHelper.DrawString(Font, label, labelPosition + shadowOffset, shadowColor);
                TextHelper.DrawString(Font, label, labelPosition, fontColor);
                batch.End();

                //render the world name
                blob.RenderWithButtons(position, fontColor, shadowColor, shadowOffset, maxLines: 3);

                //only render X button if we have a level
                if (nextLevel != null)
                {
                    float clearLabelLength = Font().MeasureString(Strings.Localize("editWorldParams.clearNextLevel")).X;
                    //render the X button
                    position.X = w - 54;
                    position.Y = h - 54;
                    size = new Vector2(64, 64);
                   
                    quad.Render(ButtonTextures.XButton, position, size, "TexturedRegularAlpha");

                    //prepare position for "clear" label
                    position.X -= 10 + (int)clearLabelLength;

                    float hitBoxU = (float)(w - clearLabelLength - 64) / (float)w;
                    float hitBoxV = (float)(h - 64) / (float)h;
                    //update the hit box for "clear" - basically bottom right corner
                    clearHitBox.Set(new Vector2(hitBoxU, hitBoxV), new Vector2(1.0f, 1.0f));

                    //render the clear button text
                    batch.Begin();
                    TextHelper.DrawString(Font, Strings.Localize("editWorldParams.clearNextLevel"), position + shadowOffset, shadowColor);
                    TextHelper.DrawString(Font, Strings.Localize("editWorldParams.clearNextLevel"), position, fontColor);
                    batch.End();
                }
                else
                {
                    //no clear hit box if it's not being rendered
                    clearHitBox.Set(Vector2.Zero, Vector2.Zero);
                }


                // Restore write channels
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

            // Load the background textures.
            if (nextLevelWhite == null)
            {
                nextLevelWhite = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\SliderWhite");
            }

            if (nextLevelMiddleBlack == null)
            {
                nextLevelMiddleBlack = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\MiddleBlack");
            }
            
            if (nextLevelBlack == null)
            {
                nextLevelBlack = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\RadioBoxBlack");
            }

            if (nextLevelNone == null)
            {
                nextLevelNone = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\NoNextLevel");
            }

        }   // end of UIGridModularCheckboxElement LoadContent()

        public override void InitDeviceResources(GraphicsDevice device)
        {
            int w = 512;
            int h = 64 + 128;           

            //make sure the height keeps proper dimensions
            height = h * width / w;

            // Create the geometry.
            geometry = new Base9Grid(width, height, edgeSize);
            mainHitBox = new AABB2D();
            clearHitBox = new AABB2D();

            CreateRenderTargets(device);

            BokuGame.Load(geometry, true);
        }

        public override void UnloadContent()
        {
            base.UnloadContent();

            ReleaseRenderTargets();

            BokuGame.Release(ref effect);
            BokuGame.Release(ref normalMap);
            BokuGame.Release(ref nextLevelWhite);
            BokuGame.Release(ref nextLevelMiddleBlack);
            BokuGame.Release(ref nextLevelBlack);
            BokuGame.Release(ref nextLevelNone);

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
            int h = 64 + 128; //64 for title, 128 for body
            int w = (int)(width / height * h);

            diffuse = new RenderTarget2D(
                device,
                w, h,
                false,      // mip levels
                SurfaceFormat.Color,
                DepthFormat.None);

            InGame.GetRT("UIGridModularNextLevelElement", diffuse);

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






