
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
using Boku.Base;
using Boku.Common;
using Boku.Fx;

namespace Boku.UI2D
{
    /// <summary>
    /// An instance of UIElement that uses a 9-grid element for its geometry
    /// and creates a texture on the fly into which the PictureList and the 
    /// associated text string are rendered.
    /// </summary>
    public class UIGridPictureListElement : UIGridElement
    {
        public delegate void UIPictureListEvent(int curIndex);

        private static Effect effect = null;

        private RenderTarget2D diffuse = null;
        private Texture2D normalMap = null;
        private Texture2D leftArrow = null;
        private Texture2D rightArrow = null;
        private string normalMapName = null;

        private UIPictureListEvent onChange = null;
        private UIPictureListEvent onXButton = null;

        private string xButtonText = null;  // If not null, this is shown just above the Y button text.

        // Properties for the underlying 9-grid geometry.
        private float width;
        private float height;
        private float edgeSize;

        private int backgroundWidth;    // Size of expected rendertarget.  May be bigger due to power of 2 issues
        private int backgroundHeight;   // but we still need to render assuming this size to get things to line up.

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

        /// <summary>
        /// Internal class which holds info for each picture in the list.
        /// </summary>
        protected class Picture : INeedsDeviceReset
        {
            public string label = null;
            public string textureName = null;
            private Texture2D texture = null;

            // Specific to gradient tiles.
            public Vector4[] gradient = null;
            private RenderTarget2D rt = null;
            
            public Vector2 position = Vector2.Zero;
            public float scale = 1.0f;      // Used to change the size while rendering.  The currently selected 
                                            // Picture will be rendered larger than the others.
            public float alpha = 1.0f;      // Alpha value used to render picture.  When set to 0 this indicates
                                            // that this picture should not be rendered.

            public float unselectedScale = 0.9f;
            public float selectedScale = 1.2f;
            public bool dirty = false;

            #region Accessors
            public float Scale
            {
                set
                {
                    if (scale != value)
                    {
                        // Create a twitch to change the scale.
                        TwitchManager.Set<float> set = delegate(float val, Object param) { scale = val; dirty = true; };
                        TwitchManager.CreateTwitch<float>(scale, value, set, 0.15f, TwitchCurve.Shape.EaseInOut);
                    }
                }
                get { return scale; }
            }
            public float UnselectedScale
            {
                get { return unselectedScale; }
            }
            public float Alpha
            {
                set
                {
                    if (alpha != value)
                    {
                        // Create a twitch to change the alpha.
                        TwitchManager.Set<float> set = delegate(float val, Object param) { alpha = val; dirty = true; };
                        TwitchManager.CreateTwitch<float>(alpha, value, set, value == 0.0f ? 0.10f : 0.25f, TwitchCurve.Shape.EaseInOut);
                    }
                }
            }
            public Vector2 Position
            {
                set
                {
                    // Create a twitch to move the position.
                    TwitchManager.Set<Vector2> set = delegate(Vector2 val, Object param) { position = val; dirty = true; };
                    TwitchManager.CreateTwitch<Vector2>(position, value, set, 0.15f, TwitchCurve.Shape.EaseInOut);
                }
            }
            public Texture2D Texture
            {
                get
                {
                    if (gradient == null)
                    {
                        return texture;
                    }
                    else
                    {
                        return rt;
                    }
                }
                set { texture = value; }
            }
            #endregion


            #region Internal
            public void LoadContent(bool immediate)
            {
                if (gradient == null)
                {
                    texture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\" + textureName);
                }
            }

            public void InitDeviceResources(GraphicsDevice device)
            {
                CreateRenderTargets(device);
            }

            public void UnloadContent()
            {
                if (gradient == null)
                {
                    BokuGame.Release(ref texture);
                }
                else
                {
                    ReleaseRenderTargets();
                }
            }

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
                ReleaseRenderTargets();
                CreateRenderTargets(device);
            }

            private void CreateRenderTargets(GraphicsDevice device)
            {
                if (gradient != null)
                {
                    // Create the rendertarget.
                    int size = 64;
                    rt = new RenderTarget2D(device, size, size, false, SurfaceFormat.Color, DepthFormat.None);
                    InGame.GetRT("UIGridPictureListElement", rt);

                    // Render the gradient into the rendertarget.
                    InGame.SetRenderTarget(rt);

                    ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();
                    quad.RenderGradient(gradient);

                    // Restore backbuffer.
                    InGame.RestoreRenderTarget();
                }
            }

            private void ReleaseRenderTargets()
            {
                InGame.RelRT("UIGridPictureListElement", rt);
                BokuGame.Release(ref rt);
            }

            #endregion
        }   // end of class picture

        private int curIndex = -1;      // Which picture is currently highlighted.  Init to invalid 
                                        // value to force an recalc right at the start.
        private int leftIndex = 0;      // The index of the _visible_ picture that is leftmost.  This 
                                        // is needed since we may have more pictures than can be shown 
                                        // at any given time.
        private List<Picture> pictures = new List<Picture>();
        // TODO (****) Add a twitch to these to fade in/out as needed?
        private bool showLeftArrow = false;
        private bool showRightArrow = false;

        private Vector2 leftArrowPosition = Vector2.Zero;
        private Vector2 rightArrowPosition = Vector2.Zero;
        private Vector2 arrowSize = new Vector2(64.0f, 64.0f);

        #region Accessors

        /// <summary>
        /// Returns the index of the currently selected item.  
        /// May be -1 if none.
        /// </summary>
        public int CurrentIndex
        {
            get { return curIndex; }
        }

        /// <summary>
        /// Delegate to be called when the selection is changed.
        /// </summary>
        public UIPictureListEvent OnChange
        {
            set { onChange = value; }
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
                        HelpOverlay.Push(@"UIGridPictureList");

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
        public UIGridPictureListElement(ParamBlob blob, string label)
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
        public UIGridPictureListElement(float width, float height, float edgeSize, string normalMapName, Color baseColor, string label, Shared.GetFont font, Justification justify, Color textColor)
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
        public UIGridPictureListElement(float width, float height, float edgeSize, string normalMapName, Color baseColor, string label, Shared.GetFont font, Justification justify, Color textColor, Color dropShadowColor, bool invertDropShadow)
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
        /// Version of AddPicture that allows the scale to be tweaked.  Note that this is not fully supported
        /// in the sense that for layout purposes only the scale of the first picture is used.  So if you try
        /// and make every picture a different size you will have problems.
        /// </summary>
        /// <param name="textureName"></param>
        /// <param name="label"></param>
        /// <param name="unselectedScale"></param>
        /// <param name="selectedScale"></param>
        public void AddPicture(string textureName, string label, float unselectedScale, float selectedScale)
        {
            Picture pic = new Picture();

            pic.unselectedScale = unselectedScale;
            pic.selectedScale = selectedScale;

            pic.label = TextHelper.FilterInvalidCharacters(label);
            pic.textureName = textureName;
            pic.Texture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\" + pic.textureName);
            pic.scale = pic.unselectedScale;

            pictures.Add(pic);

            if (pictures.Count == 1)
            {
                pictures[0].scale = pic.selectedScale;
            }
        }
        
        /// <summary>
        /// Add a picture to the list.  Pictures should be added in order and are indexed starting with 0.
        /// </summary>
        /// <param name="filename"></param>
        public void AddPicture(string textureName, string label)
        {
            Picture pic = new Picture();
            pic.label = TextHelper.FilterInvalidCharacters(label);
            pic.textureName = textureName;
            pic.Texture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\" + pic.textureName);
            pic.scale = pic.unselectedScale;

            pictures.Add(pic);

            if (pictures.Count == 1)
            {
                pictures[0].scale = pic.selectedScale;
            }
        }

        /// <summary>
        /// Add a gradient tile to the list.  Tiles should be added in order and are indexed starting with 0.
        /// </summary>
        /// <param name="filename"></param>
        public void AddGradientTile(Vector4[] gradient, string label)
        {
            Picture pic = new Picture();
            pic.gradient = gradient;
            pic.label = TextHelper.FilterInvalidCharacters(label);
            
            pic.scale = pic.unselectedScale;

            pictures.Add(pic);

            if (pictures.Count == 1)
            {
                pictures[0].scale = pic.selectedScale;
            }
        }

        /// <summary>
        /// Returns the gradient assotiated with the "index" picture.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Vector4[] GetGradient(int index)
        {
            return pictures[index].gradient;
        }

        /// <summary>
        /// Searches through the gradients in the list for one that matches
        /// the passed in gradient.  When found this is then set as the 
        /// current selection.
        /// </summary>
        /// <param name="gradient"></param>
        public void SetValue(Vector4[] gradient)
        {
            for (int i = 0; i < pictures.Count; i++)
            {
                float kClose = 0.001f * 0.001f;
                Vector4[] g = pictures[i].gradient;
                if (Vector4.DistanceSquared(g[0], gradient[0]) < kClose &&
                    Vector4.DistanceSquared(g[1], gradient[1]) < kClose &&
                    Vector4.DistanceSquared(g[2], gradient[2]) < kClose &&
                    Vector4.DistanceSquared(g[3], gradient[3]) < kClose &&
                    Vector4.DistanceSquared(g[4], gradient[4]) < kClose)
                {
                    // Match.
                    SetCurrentIndex(i);
                }
            }
        }   // end of SetValue()

        /// <summary>
        /// Sets the currently selected index and refreshes 
        /// everything to take the change into account.
        /// </summary>
        /// <param name="index"></param>
        public void SetCurrentIndex(int index)
        {
            curIndex = index;
            dirty = true;
            RecalcPositions();
            RefreshTexture();
        }   // end of SetDefault()

        public void CallOnChange(int index)
        {
            onChange(index);
        }   // end of CallOnChange()

        /// <summary>
        /// Finds picture with given label and sets it as current.
        /// </summary>
        /// <param name="label"></param>
        public void SetCurrentLabel(string label)
        {
            for (int i = 0; i < pictures.Count; i++)
            {
                if (label == pictures[i].label)
                {
                    SetCurrentIndex(i);
                    return;
                }
            }
            Debug.Assert(false, "Label not found.");
        }

        /// <summary>
        /// Set an optional callback and text string for the element.
        /// </summary>
        /// <param name="onXButton"></param>
        /// <param name="text"></param>
        public void SetXButton(UIPictureListEvent onXButton, string text)
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

                bool changed = false;

                // Handle input changes here.
                if (pad.DPadLeft.WasPressed
                    || pad.DPadLeft.WasRepeatPressed
                    || pad.LeftStickLeft.WasPressed
                    || pad.LeftStickLeft.WasRepeatPressed)
                {
                    curIndex = (curIndex + pictures.Count - 1) % pictures.Count;
                    Foley.PlayClickDown();
                    changed = true;
                }
                if (pad.DPadRight.WasPressed
                    || pad.DPadRight.WasRepeatPressed
                    || pad.LeftStickRight.WasPressed
                    || pad.LeftStickRight.WasRepeatPressed)
                {
                    curIndex = (curIndex + 1) % pictures.Count;
                    Foley.PlayClickUp();
                    changed = true;
                }

                if (pad.ButtonX.WasPressed)
                {
                    if (onXButton != null)
                    {
                        onXButton(curIndex);
                    }
                }

                if (changed || curIndex == -1)
                {
                    RecalcPositions();
                    if (onChange != null)
                    {
                        onChange(curIndex);
                    }
                }

            }

            RefreshTexture();

            base.Update(ref parentMatrix);
        }   // end of UIGridPictureListElement Update()

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

        }   // end of UIGridPictureListElement Render()

        /// <summary>
        /// Recalculates the positions, scales and alpha values for each of the pictures in the list.
        /// </summary>
        private void RecalcPositions()
        {
            int y = 72;
            int margin = 8;
            int arrowWidth = 64;
            int pictureWidth = 64;
            Vector2 picSize = new Vector2(pictureWidth, pictureWidth);
            int widthForPictures = backgroundWidth - margin * 2 - arrowWidth * 2;
            int numVisiblePictures = widthForPictures / pictureWidth;

            // Calc actual width needed for pics.
            widthForPictures = (int)((numVisiblePictures - 1.0f + pictures[0].selectedScale) * pictureWidth);

            if (numVisiblePictures > pictures.Count)
            {
                numVisiblePictures = pictures.Count;
            }

            leftArrowPosition = new Vector2(backgroundWidth / 2 - widthForPictures / 2 - arrowWidth, y);
            rightArrowPosition = new Vector2(backgroundWidth / 2 + widthForPictures / 2, y);

            bool refreshAll = curIndex == -1;
            if (refreshAll)
            {
                curIndex = 0;
            }

            bool leftIndexChanged = false;

            if (curIndex < leftIndex)
            {
                leftIndex = curIndex;
                leftIndexChanged = true;
            }

            if (curIndex >= leftIndex + numVisiblePictures - 1)
            {
                leftIndex = curIndex - numVisiblePictures + 1;
                leftIndexChanged = true;
            }

            // Always assume that the curIndex has changed so update all scales and positions.
            // picPosition is the center of the pic.
            Vector2 picPosition = leftArrowPosition + 0.5f * picSize;
            picPosition.X += arrowWidth - leftIndex * pictureWidth;

            for (int i = 0; i < pictures.Count; i++)
            {
                float scale = i == curIndex ? pictures[0].selectedScale : pictures[0].unselectedScale;
                pictures[i].Scale = scale;

                // Position, adjust for scale.
                // Offset adjusts for scaling and for the fact that we need the
                // upper left hand corner as the position instead of the center.
                Vector2 offset = 0.5f * scale * picSize;
                pictures[i].Position = picPosition - offset;

                // Now adjust scale to calc how far to move for the next pic.
                scale = 1.0f;
                if (i == curIndex || i + 1 == curIndex)
                {
                    scale += (pictures[0].selectedScale - 1.0f) * 0.5f;
                    // Ensure that the left edge of the first pic always 
                    // stays at the same place.
                    if (i == 0 && i == curIndex)
                    {
                        scale += (pictures[0].selectedScale - 1.0f) * 0.5f;
                    }
                }
                picPosition.X += scale * pictureWidth;
            }

            // If the left index has changed, then we need to recalc the positions
            // and alpha values of all the pictures.
            if (leftIndexChanged || refreshAll)
            {
                for (int i = 0; i < pictures.Count; i++)
                {
                    // Alpha
                    if (i < leftIndex || i >= leftIndex + numVisiblePictures)
                    {
                        pictures[i].Alpha = 0.0f;
                    }
                    else 
                    { 
                        pictures[i].Alpha = 1.0f;
                    }
                }
            }

            showLeftArrow = leftIndex > 0;
            showRightArrow = leftIndex + numVisiblePictures < pictures.Count;

            dirty = true;

        }   // end of UIGridPictureListElement RecalPositions().

        /// <summary>
        /// If the state of the element has changed, we may need to re-create the texture.
        /// </summary>
        private void RefreshTexture()
        {
            if (!dirty)
            {
                // Check if any of the owned pictures are dirty.
                for (int i = 0; i < pictures.Count; i++)
                {
                    if (pictures[i].dirty)
                    {
                        dirty = true;
                        pictures[i].dirty = false;
                    }
                }
            }

            if (dirty)
            {
                InGame.SetRenderTarget(diffuse);
                InGame.Clear(Color.Transparent);

                int width = backgroundWidth;
                int height = backgroundHeight;

                ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();

                

                // Render the label text into the texture in the upper left-hand corner.
                int margin = 32;
                int x = 0;
                int y = 12;

                string fancyLabel = label;
                if (pictures[curIndex].label != null)
                {
                    fancyLabel = fancyLabel + " : " + pictures[curIndex].label;
                }
                int textWidth = (int)(Font().MeasureString(fancyLabel).X);

                x = TextHelper.CalcJustificationOffset(margin, width, textWidth, justify);

                SpriteBatch batch = UI2D.Shared.SpriteBatch;
                batch.Begin();
                TextHelper.DrawStringWithShadow(Font, batch, x, y, fancyLabel, textColor, dropShadowColor, invertDropShadow);
                batch.End();


                // Render the arrows.
                if (showLeftArrow)
                {
                    quad.Render(leftArrow, leftArrowPosition, arrowSize, @"TexturedRegularAlpha");
                }
                if (showRightArrow)
                {
                    quad.Render(rightArrow, rightArrowPosition, arrowSize, @"TexturedRegularAlpha");
                }

                // Render the PictureList.
                for(int i=0; i<pictures.Count; i++)
                {
                    if(pictures[i].alpha > 0.0f)
                    {
                        quad.Render(pictures[i].Texture, pictures[i].position, pictures[i].scale * arrowSize, @"TexturedRegularAlpha");
                    }
                }

                // Render help button.
                if (ShowHelpButton)
                {
                    x = width - 54;
                    y = height - 54;
                    Vector2 pos = new Vector2(x, y);
                    Vector2 size = new Vector2(64, 64);
                    quad.Render(ButtonTextures.YButton, pos, size, "TexturedRegularAlpha");
                    x -= 10 + (int)Font().MeasureString(Strings.Localize("editObjectParams.help")).X;
                    batch.Begin();
                    TextHelper.DrawStringWithShadow(Font, batch, x, y, Strings.Localize("editObjectParams.help"), textColor, dropShadowColor, invertDropShadow);
                    batch.End();

                    if (xButtonText != null)
                    {
                        x = width - 54;
                        y = height - 54 - Font().LineSpacing - 6;
                        pos = new Vector2(x, y);
                        size = new Vector2(64, 64);
                        quad.Render(ButtonTextures.XButton, pos, size, "TexturedRegularAlpha");
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
        }   // end of UIGridPictureListElement RefreshTexture()

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

            // Load the arrow textures.
            if (leftArrow == null)
            {
                leftArrow = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\WhiteLeftArrow");
            }
            if (rightArrow == null)
            {
                rightArrow = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\WhiteRightArrow");
            }
        }

        const int dpi = 128;

        public override void InitDeviceResources(GraphicsDevice device)
        {
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

            // Create the diffuse texture.
            backgroundWidth = w;
            backgroundHeight = h;
            if (BokuGame.RequiresPowerOf2)
            {
                w = MyMath.GetNextPowerOfTwo(w);
                h = MyMath.GetNextPowerOfTwo(h);
            }

            CreateRenderTargets(device);

            // Reload any textures.
            for (int i = 0; i < pictures.Count; i++)
            {
                BokuGame.Load(pictures[i], true);
            }

            // Recalc everything and refresh the texture.
            dirty = true;
            RecalcPositions();
            RefreshTexture();

            BokuGame.Load(geometry, true);
        }

        public override void UnloadContent()
        {
            base.UnloadContent();

            ReleaseRenderTargets();

            BokuGame.Release(ref effect);
            BokuGame.Release(ref normalMap);
            BokuGame.Release(ref leftArrow);
            BokuGame.Release(ref rightArrow);

            for (int i = 0; i < pictures.Count; i++)
            {
                BokuGame.Unload(pictures[i]);
            }

            BokuGame.Unload(geometry);
            geometry = null;
        }   // end of UIGridPictureListElement UnloadContent()

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
            int w = (int)(dpi * width);
            int h = (int)(dpi * height);

            if (BokuGame.RequiresPowerOf2)
            {
                w = MyMath.GetNextPowerOfTwo(w);
                h = MyMath.GetNextPowerOfTwo(h);
            }

            diffuse = new RenderTarget2D(device, w, h, false, SurfaceFormat.Color, DepthFormat.None);
            InGame.GetRT("UIGridPictureListElement", diffuse);

            dirty = true;
        }

        private void ReleaseRenderTargets()
        {
            InGame.RelRT("UIGridPictureListElement", diffuse);
            BokuGame.Release(ref diffuse);
        }

    }   // end of class UIGridPictureListElement

}   // end of namespace Boku.UI2D






