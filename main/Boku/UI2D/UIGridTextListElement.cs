
using System;
using System.Collections;
using System.Collections.Generic;

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
    /// and creates a texture on the fly into which the text strings are rendered.
    /// This is effectively a replacement for the standard radio buttons 
    /// designed to work with a controller as input rather than a mouse.
    /// </summary>
    public class UIGridTextListElement : UIGridElement
    {
        public delegate void UITextListEvent(string text);

        private UITextListEvent onChange = null;

        private static Effect effect = null;

        private RenderTarget2D diffuse = null;
        private Texture2D normalMap = null;
        private string normalMapName = null;

        private static Texture2D checkbox = null;
        private static Texture2D checkmark = null;

        private int curIndex = 0;   // The currently selected option.

        const int dpi = 128;

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

        private static Point margin = new Point(32, 12);    // Margin used by individual TextLines.

        private TextLine title = null;
        private List<TextLine> textList = null;     // The list of options.

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
                        HelpOverlay.Push(@"UIGridTextList");

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
        /// Delegate to be called when the selection is changed.
        /// </summary>
        public UITextListEvent OnChange
        {
            set { onChange = value; }
        }

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

        /// <summary>
        /// Margin used for each text line in pixels.
        /// </summary>
        public static Point Margin
        {
            get { return margin; }
            set { margin = value; }
        }

        public static Texture2D Checkbox
        {
            get { return checkbox; }
        }
        public static Texture2D Checkmark
        {
            get { return checkmark; }
        }
        #endregion

        // c'tor
        /// <summary>
        /// Simple c'tor using a blob to hold the common data.
        /// </summary>
        /// <param name="blob"></param>
        /// <param name="label"></param>
        public UIGridTextListElement(ParamBlob blob, string label)
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

            title = new TextLine(this, label, Font, false);
            textList = new List<TextLine>();
        }

        /// <summary>
        /// Adds a new text string entry to the list.
        /// </summary>
        /// <param name="text"></param>
        public void AddText(string text)
        {
            

            TextLine line = new TextLine(this, TextHelper.FilterInvalidCharacters(text), Font, true);
            textList.Add(line);

            // Grow size for each line of text.
            height += (Font().LineSpacing + Margin.Y) / (float)dpi;
            // Note the 64 in the following line is the width of the checkbox
            // except I can't get it directly from the checkbox since it's 
            // not loaded yet.
            float lineWidth = ((int)(Font().MeasureString(text).X) + Margin.X * 2 + 64) / (float)dpi;
            width = MathHelper.Max(width, lineWidth);

            RecalcPositions();
        }   // end of UIGridTextListElement AddText()

        public override void Update(ref Matrix parentMatrix)
        {
            // Check for input but only if selected.
            if (selected && textList.Count > 1)
            {
                GamePadInput pad = GamePadInput.GetGamePad0();

                bool changed = false;

                // Handle input changes here.
                if (pad.ButtonA.WasPressed || pad.ButtonA.WasRepeatPressed)
                {
                    do
                    {
                        ++curIndex;
                        if (curIndex >= textList.Count)
                        {
                            curIndex = 0;
                        }

                        Foley.PlayClick();
                        changed = true;
                    } while (textList[curIndex].Hidden);
                }

                if (changed)
                {
                    onChange(textList[curIndex].Text);
                    dirty = true;
                    //RecalcPositions();
                }

            }

            RefreshTexture();

            base.Update(ref parentMatrix);
        }   // end of UIGridTextListElement Update()

        public void RecalcPositions()
        {
            

            int dy = Font().LineSpacing;
            int y = Margin.Y + dy;
            for (int i = 0; i < textList.Count; i++)
            {
                if (!textList[i].Hidden)
                {
                    textList[i].Position = new Vector2(0.0f, y);
                    y += dy;
                }
            }
            dirty = true;
        }   // end of UIGridTextListElement RecalcPositions()

        /// <summary>
        /// Mark the text line as hidden.
        /// </summary>
        /// <param name="index"></param>
        public void Hide(int index)
        {
            textList[index].Hidden = true;
            RecalcPositions();
        }   // end of Hide()

        /// <summary>
        /// Un-hide the text line.
        /// </summary>
        /// <param name="index"></param>
        public void Show(int index)
        {
            textList[index].Hidden = false;
            RecalcPositions();
        }   // end of Show()

        public void RefreshTexture()
        {
            if (dirty)
            {
                InGame.SetRenderTarget(diffuse);
                InGame.Clear(Color.Transparent);

                ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();

                // Title.
                quad.Render(title.Texture, title.Position, title.Size, @"TexturedRegularAlpha");

                // The list.  Render these bottom to top so if they overlap we're at least 
                // seeing the selected on unobscured.
                for (int i = textList.Count - 1; i >= 0; i--)
                {
                    if (!textList[i].Hidden)
                    {
                        TextLine line = textList[i];
                        quad.Render(line.Texture, line.Position, line.Size, @"TexturedRegularAlpha");
                    }
                }

                // Render the checkmark.
                Vector2 offset = new Vector2(Margin.X + 2, 5);
                Vector2 size = new Vector2(40.0f, 40.0f);

                quad.Render(Checkmark, textList[curIndex].Position + offset, size, @"TexturedRegularAlpha");

                // Render help button.
                if (ShowHelpButton)
                {
                    int x = (int)width*dpi - 54;
                    int y = (int)textList[textList.Count - 1].Position.Y;   // Align with bottom line of text.
                    Vector2 pos = new Vector2(x, y);
                    size = new Vector2(64, 64);
                    quad.Render(ButtonTextures.YButton, pos, size, "TexturedRegularAlpha");
                    x -= 10 + (int)Font().MeasureString(Strings.Localize("editObjectParams.help")).X;

                    SpriteBatch batch = UI2D.Shared.SpriteBatch;
                    batch.Begin();
                    TextHelper.DrawStringWithShadow(Font, batch, x, y, Strings.Localize("editObjectParams.help"), textColor, dropShadowColor, invertDropShadow);
                    batch.End();
                }

                // Restore backbuffer.
                InGame.RestoreRenderTarget();

                dirty = false;
            }
        }   // end of UIGridTextListElement RefreshTexture()

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

        }   // end of UIGridTextListElement Render()

        /// <summary>
        /// This sets the current index on the matching text line.  If no
        /// matching line is found, the current index is not changed.
        /// </summary>
        /// <param name="text"></param>
        public void SetValue(string text)
        {
            for (int i = 0; i < textList.Count; i++)
            {
                if (textList[i].Text == text)
                {
                    curIndex = i;
                    Dirty = true;
                    break;
                }
            }
        }   // end of UIGridTextListElement SetValue()

        public override void LoadContent(bool immediate)
        {
            // Init the effect.
            if (effect == null)
            {
                effect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\UI2D");
                ShaderGlobals.RegisterEffect("UI2D", effect);
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

            // Load the normal map texture.
            if (normalMapName != null)
            {
                normalMap = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\" + normalMapName);
            }
        }

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

            // Create the diffuse texture.  Leave it null if we have no text to render.
            int originalWidth = w;
            int originalHeight = h;

            if (diffuse == null)
            {
                CreateRenderTargets(device);
            }

            BokuGame.Load(title, true);

            foreach (TextLine line in textList)
            {
                BokuGame.Load(line, true);
            }

            BokuGame.Load(geometry, true);

            dirty = true;
        }

        public override void UnloadContent()
        {
            base.UnloadContent();

            title.UnloadContent();
            foreach (TextLine line in textList)
            {
                line.UnloadContent();
            }

            ReleaseRenderTargets();

            BokuGame.Release(ref effect);
            BokuGame.Release(ref normalMap);
            BokuGame.Release(ref checkbox);
            BokuGame.Release(ref checkmark);

            BokuGame.Unload(geometry);
            geometry = null;
        }   // end of UIGridTextListElement UnloadContent()

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

            diffuse = new RenderTarget2D(
                device,
                w, h,
                false,      // Mipmaps
                SurfaceFormat.Color,
                DepthFormat.None);
            InGame.GetRT("UIGridTextListElement", diffuse);

            dirty = true;
            RefreshTexture();
        }

        private void ReleaseRenderTargets()
        {
            InGame.RelRT("UIGridTextListElement", diffuse);
            BokuGame.Release(ref diffuse);
        }

    }   // end of class UIGridTextListElement

}   // end of namespace Boku.UI2D






