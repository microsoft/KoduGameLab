
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Audio;
using Boku.Common;
using Boku.Fx;
using Boku.SimWorld;

namespace Boku.UI2D
{
    /// <summary>
    /// Element for choosing among a list of pictures/icons.
    /// </summary>
    public class UIGridModularPictureListElement : UIGridElement
    {
        public class Picture
        {
            public Texture2D texture = null;
            public string label = null;
            public string picName = null;
            public int gradient = -1;

            public float size = 0.0f;
            public Vector2 position;

            public float _size = 0.0f;  // Actual values (as opposed to the twitched ones).
            public Vector2 _position;

            #region Accessors



            #endregion

        }   // end of class Picture

        public delegate void UIModularEvent(int index);

        private static Effect effect = null;

        private RenderTarget2D diffuse = null;

        private string normalMapName = null;
        private Texture2D normalMap = null;

        private Texture2D white = null;
        private Texture2D black = null;
        private Texture2D middleBlack = null;
        private Texture2D arrow = null;
        private Texture2D indicatorLit = null;

        private int curIndex = 0;   // Current selection.

        private UIModularEvent onChange = null;

        private List<Picture> pics = new List<Picture>();

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
                        HelpOverlay.Push("ModularPictureList");

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
            set { /* do nothing, should be removed from base class */ }
        }

        /// <summary>
        /// Delegate to be called when the selection is changed.
        /// </summary>
        public UIModularEvent OnChange
        {
            get { return onChange; }
            set { onChange = value; }
        }

        #endregion

        // c'tor
        /// <summary>
        /// Simple c'tor using a blob to hold the common data.
        /// </summary>
        /// <param name="blob"></param>
        /// <param name="label"></param>
        public UIGridModularPictureListElement(ParamBlob blob, string label)
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

        public void AddPicture(string picName, string text)
        {
            Picture pic = new Picture();

            pic.label = TextHelper.FilterInvalidCharacters(text);
            pic.picName = picName;

            pics.Add(pic);
        }

        public void AddGradientTile(int idx, string text)
        {
            Picture pic = new Picture();

            pic.label = TextHelper.FilterInvalidCharacters(text);
            pic.gradient = idx;

            pics.Add(pic);
        }   // end of AddGradient

        public int GetGradient(int index)
        {
            Debug.Assert(index < pics.Count, "Something's wrong...");

            return pics[index].gradient;
        }

        /// <summary>
        /// Set the index of the current choice
        /// </summary>
        /// <param name="gradient"></param>
        public void SetValue(int idx)
        {
            CurIndex = idx;
            recalcPositions = true;
        }   // end of SetValue()

        /// <summary>
        /// Finds picture with given label and sets it as current.
        /// </summary>
        /// <param name="label"></param>
        public void SetCurrentLabel(string label)
        {
            for (int i = 0; i < pics.Count; i++)
            {
                if (label == pics[i].label)
                {
                    CurIndex = i;
                    recalcPositions = true;
                    return;
                }
            }
            Debug.Assert(false, "Label not found.");
        }

        bool recalcPositions = true;

        public override void Update(ref Matrix parentMatrix)
        {
            // Do we need to render the gradients?
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
            RenderTarget2D rt = UI2D.Shared.RenderTarget64_64;
            for (int i = 0; i < pics.Count; i++)
            {
                if (pics[i].texture == null)
                {
                    if (pics[i].gradient >= 0)
                    {
                        // Create the texture gradient.
                        InGame.SetRenderTarget(rt);
                        ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();
                        quad.RenderGradient(SkyBox.Gradient(pics[i].gradient));
                        InGame.RestoreRenderTarget();

                        pics[i].texture = new Texture2D(device, 64, 64, false, SurfaceFormat.Color);

                        // Copy rendertarget result into texture.
                        int[] data = new int[64 * 64];
                        rt.GetData<int>(data);
                        pics[i].texture.SetData<int>(data);

                    }
                    else
                    {
                        // Load the texture image.
                        pics[i].texture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\" + pics[i].picName);
                    }
                    dirty = true;
                }
            }

            // Check for input but only if selected.
            if (selected)
            {
                GamePadInput pad = GamePadInput.GetGamePad0();

                if (Actions.ComboRight.WasPressedOrRepeat)
                {
                    curIndex = (curIndex + 1) % pics.Count;
                    Foley.PlayClickUp();
                    recalcPositions = true;
                    dirty = true;
                }

                if (Actions.ComboLeft.WasPressedOrRepeat)
                {
                    curIndex = (curIndex + pics.Count - 1) % pics.Count;
                    Foley.PlayClickDown();
                    recalcPositions = true;
                    dirty = true;
                }

            }

            if (recalcPositions)
            {
                if (onChange != null)
                {
                    OnChange(CurIndex);
                }
                RefreshPositions();
            }
            RefreshTexture();

            base.Update(ref parentMatrix);
        }   // end of UIGridModularPictureListElement Update()

        private int hitIndex = -1;
        public override void HandleMouseInput(Vector2 hitUV)
        {
            // Only respond to clicks in the black region of the element
            if(hitUV.Y > 0.4f)
            {
                int hit = (int)(hitUV.X * 9.0f);

                if (MouseInput.Left.WasPressed)
                {
                    // Ignore reselecting the current selection.
                    hitIndex = hit == 4 ? -1 : hit;
                }

                if (MouseInput.Left.WasReleased)
                {
                    if (hit == hitIndex)
                    {
                        if (hit < 4)
                        {
                            int steps = hit == 0 ? 1 : 4 - hit;
                            curIndex = (curIndex + pics.Count - steps) % pics.Count;
                            Foley.PlayClickDown();
                            recalcPositions = true;
                            dirty = true;
                        }
                        else if (hit > 4)
                        {
                            int steps = hit == 8 ? 1 : hit - 4;
                            curIndex = (curIndex + steps) % pics.Count;
                            Foley.PlayClickDown();
                            recalcPositions = true;
                            dirty = true;
                        }
                    }

                    hitIndex = -1;
                }

            }
        }   // end of HandleMouseInput()


        public override void HandleTouchInput(TouchContact touch, Vector2 hitUV)
        {
            // Only respond to clicks in the black region of the element
            if (hitUV.Y > 0.4f)
            {
                int hit = (int)(hitUV.X * 9.0f);

                if (TouchInput.WasTouched)
                {
                    // Ignore reselecting the current selection.
                    hitIndex = hit == 4 ? -1 : hit;
                }

                if (TouchInput.WasReleased)
                {
                    if (hit == hitIndex)
                    {
                        if (hit < 4)
                        {
                            int steps = hit == 0 ? 1 : 4 - hit;
                            curIndex = (curIndex + pics.Count - steps) % pics.Count;
                            Foley.PlayClickDown();
                            recalcPositions = true;
                            dirty = true;
                        }
                        else if (hit > 4)
                        {
                            int steps = hit == 8 ? 1 : hit - 4;
                            curIndex = (curIndex + steps) % pics.Count;
                            Foley.PlayClickDown();
                            recalcPositions = true;
                            dirty = true;
                        }
                    }

                    hitIndex = -1;
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

        }   // end of UIGridModularPictureListElement Render()

        private int limit = 3;

        private void RefreshPositions()
        {
            float kDefaultSize = 50.0f;
            float kSelectedSize = 60.0f;
            float kSpacing = 60.0f;

            Vector2 centerPosition = new Vector2(diffuse.Width / 2.0f, 105.0f);

            // Set center, in-focus pic.
            TwitchPic(CurIndex, kSelectedSize, centerPosition);

            int index;
            Vector2 position;
            Vector2 spacing = new Vector2(kSpacing, 0.0f);
            float size = 0.0f;
            for (int i = 1; i <= pics.Count / 2; i++)
            {
                index = (CurIndex + i) % pics.Count;
                position = centerPosition + MathHelper.Min(i, limit) * spacing;
                size = i <= limit ? kDefaultSize : 0.0f;
                TwitchPic(index, size, position);

                index = (CurIndex - i + pics.Count) % pics.Count;
                position = centerPosition - MathHelper.Min(i, limit) * spacing;
                size = i <= limit ? kDefaultSize : 0.0f;
                TwitchPic(index, size, position);
            }

            recalcPositions = false;

        }   // end of RefreshPositions()

        private void TwitchPic(int index, float size, Vector2 position)
        {
            float kTwitchTime = 0.1f;

            if(size != pics[index]._size)
            {
                // Scale.
                TwitchManager.Set<float> set = delegate(float val, Object param) { pics[index].size = val; dirty = true; };
                TwitchManager.CreateTwitch<float>(pics[index].size, size, set, kTwitchTime, TwitchCurve.Shape.EaseInOut);
                pics[index]._size = size;
            }
            if (position != pics[index]._position)
            {
                // Position.
                TwitchManager.Set<Vector2> set = delegate(Vector2 val, Object param) { pics[index].position = val; dirty = true; };
                TwitchManager.CreateTwitch<Vector2>(pics[index].position, position, set, kTwitchTime, TwitchCurve.Shape.EaseInOut);
                pics[index]._position = position;
            }
        }   // end of TwitchPick()

        /// <summary>
        /// If the state of the element has changed, we may need to re-create the texture.
        /// </summary>
        private void RefreshTexture()
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

                // The arrows.
                position.X = 20;
                position.Y = 90;
                size = new Vector2(arrow.Width, arrow.Height);
                quad.Render(arrow, position, size, "TexturedRegularAlpha");
                position.X = w - position.X;
                size.X = -size.X;
                quad.Render(arrow, position, size, "TexturedRegularAlpha");

                // The indicator.
                size = new Vector2(indicatorLit.Width, indicatorLit.Height);
                position = new Vector2(512 / 2 - size.X / 2, 140);
                quad.Render(indicatorLit, position, size, "TexturedRegularAlpha");

                // The pictures.  Render them from the outside-in so that they 
                // ovelap correctly.
                Vector2 border = new Vector2(3, 3);
                int index;
                for (int i = limit; i > 0; i--)
                {
                    index = (CurIndex + i) % pics.Count;
                    size = new Vector2(pics[index].size);
                    position = pics[index].position - 0.5f * size;
                    quad.Render(new Vector4(0, 0, 0, 1), position, size);
                    quad.Render(pics[index].texture, position + border, size - 2.0f * border, "TexturedRegularAlpha");

                    index = (CurIndex - i + pics.Count) % pics.Count;
                    size = new Vector2(pics[index].size);
                    position = pics[index].position - 0.5f * size;
                    quad.Render(new Vector4(0, 0, 0, 1), position, size);
                    quad.Render(pics[index].texture, position + border, size - 2.0f * border, "TexturedRegularAlpha");
                }
                index = CurIndex;
                size = new Vector2(pics[index].size);
                position = pics[index].position - 0.5f * size;
                quad.Render(new Vector4(0, 0, 0, 1), position, size);
                quad.Render(pics[index].texture, position + border, size - 2.0f * border, "TexturedRegularAlpha");

                // Disable writing to alpha channel.
                // This prevents transparent fringing around the text.
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
                device.BlendState = UI2D.Shared.BlendStateColorWriteRGB;

                // Render the label and value text into the texture.
                string title = label + " : " + pics[CurIndex].label;
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

                // Restore default blend state.
                device.BlendState = BlendState.AlphaBlend;


                // Restore backbuffer.
                InGame.RestoreRenderTarget();

                dirty = false;
            }
        }   // end of UIGridModularPictureListElement Render()

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
            if (arrow == null)
            {
                arrow = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\LeftArrowLit");
            }
            if (indicatorLit == null)
            {
                indicatorLit = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\IndicatorLit");
            }

        }   // end of UIGridModularPictureListElement LoadContent()

        const int w = 512;
        const int h = 175;

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
            BokuGame.Release(ref arrow);
            BokuGame.Release(ref indicatorLit);

            BokuGame.Unload(geometry);
            geometry = null;

            // Release all textures.
            for (int i = 0; i < pics.Count; i++)
            {
                Picture pic = pics[i];
                BokuGame.Release(ref pic.texture);
            }

        }   // end of UIGridModularPictureListElement UnloadContent()

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
            InGame.GetRT("UIGridModularPictureListElement", diffuse);

            // Refresh the texture.
            dirty = true;
            RefreshTexture();

        }

        private void ReleaseRenderTargets()
        {
            InGame.RelRT("UIGridModularPictureListElement", diffuse);
            BokuGame.Release(ref diffuse);
        }

    }   // end of class UIGridModularPictureListElement

}   // end of namespace Boku.UI2D






