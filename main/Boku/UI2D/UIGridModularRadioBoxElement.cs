// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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
    /// and creates a texture on the fly into which the text strings are rendered.
    /// This is effectively a replacement for the standard radio buttons 
    /// designed to work with a controller as input rather than a mouse.
    /// </summary>
    public class UIGridModularRadioBoxElement : UIGridElement
    {
        public class ListEntry
        {
            public string text = null;
            public string Key = null;
            public AABB2D Box = new AABB2D();

            public string Text
            {
                get { return text; }
                set
                {
                    text = TextHelper.FilterInvalidCharacters(value);
                    //Debug.Assert(text.Equals(value), "Consider adding the missing glyphs or changing the text.");
                }
            }

            public ListEntry(string text)
            {
                this.Text = text;
            }

            public ListEntry(string text, string key)
            {
                this.Text = text;
                this.Key = key;
            }
        }

        public delegate void UIModularRadioBoxEvent(ListEntry entry);

        private UIModularRadioBoxEvent onChange = null;
        private UIModularRadioBoxEvent onSelection = null;

        private static Effect effect = null;

        private RenderTarget2D diffuse = null;
        private Texture2D normalMap = null;
        private string normalMapName = null;

        private static Texture2D radioWhite = null;
        private static Texture2D radioBlack = null;
        private static Texture2D middleBlack = null;
        private static Texture2D indicatorLit = null;
        private static Texture2D indicatorUnlit = null;

        private int curIndex = 0;   // The currently selected option.

        const int dpi = 128;

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

        private static Point margin = new Point(32, 12);    // Margin used by individual TextLines.

        private int numColumns = 1;             // Number of columns to use to display entries.  Should be either 1 or 2.

        private bool showIndicators = true;     // Set to false for use as a menu.

        private string title = null;
        private List<ListEntry> list = null;

        #region Accessors
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
                        HelpOverlay.Push("ModularRadioBox");

                        TwitchManager.Set<float> set = delegate(float val, Object param) { dim = val; };
                        TwitchManager.CreateTwitch<float>(dim, 1.0f, set, 0.2f, TwitchCurve.Shape.EaseInOut);
                    }
                    else
                    {
                        if (HelpOverlay.Peek() == "ModularRadioBox")
                        {
                            HelpOverlay.Pop();
                        }

                        TwitchManager.Set<float> set = delegate(float val, Object param) { dim = val; };
                        TwitchManager.CreateTwitch<float>(dim, 0.5f, set, 0.2f, TwitchCurve.Shape.EaseInOut);
                    }
                }
            }
        }

        public int CurIndex
        {
            get { return curIndex; }
            set
            {
                if (curIndex != value)
                {
                    curIndex = value;
                    dirty = true;
                }
            }
        }

        public string CurString
        {
            get { return list[curIndex].Text; }
        }

        public string CurKey
        {
            get { return list[curIndex].Key; }
        }

        /// <summary>
        /// Delegate to be called when the selection is changed.
        /// </summary>
        public UIModularRadioBoxEvent OnChange
        {
            set { onChange = value; }
        }

        /// <summary>
        /// Delegate to be called when a selection is made, regardless of change.
        /// </summary>
        public UIModularRadioBoxEvent OnSelection
        {
            set { onSelection = value; }
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

        public int NumColumns
        {
            get { return numColumns; }
            set
            {
                if (value == 1 || value == 2)
                {
                    numColumns = value;
                }
                else
                {
                    Debug.Assert(false, "We don't support anything but 1 or 2.");
                }
            }
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

        /// <summary>
        /// Determines whether or not radio button indicators are rendered.
        /// Should be set to false for use as a menu.
        /// </summary>
        public bool ShowIndicators
        {
            get { return showIndicators; }
            set { showIndicators = value; }
        }
        #endregion

        // c'tor
        /// <summary>
        /// Simple c'tor using a blob to hold the common data.
        /// </summary>
        /// <param name="blob"></param>
        /// <param name="label"></param>
        public UIGridModularRadioBoxElement(ParamBlob blob, string label)
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

            title = label;
            list = new List<ListEntry>();
        }

        /// <summary>
        /// Adds a new text string entry to the list.
        /// </summary>
        /// <param name="text"></param>
        public void AddText(string text)
        {
            list.Add(new ListEntry(text));
        }   // end of UIGridModularRadioBoxElement AddText()

        /// <summary>
        /// Adds a new text string entry with key to the list.
        /// </summary>
        /// <param name="text"></param>
        public void AddText(string text, string key)
        {
            list.Add(new ListEntry(text, key));
        }   // end of UIGridModularRadioBoxElement AddText()

        public override void Update(ref Matrix parentMatrix)
        {
            // Check for input but only if selected.
            if (selected && list.Count > 1)
            {
                GamePadInput pad = GamePadInput.GetGamePad0();

                bool changed = false;

                // Handle input changes here.
                if (Actions.Select.WasPressedOrRepeat)
                {
                    Actions.Select.ClearAllWasPressedState();

                    ++curIndex;
                    if (curIndex >= list.Count)
                    {
                        curIndex = 0;
                    }

                    Foley.PlayPressA();
                    changed = true;
                }

                if (changed)
                {
                    if (null != onSelection)
                    {
                        onSelection(list[curIndex]);
                    }

                    if (null != onChange)
                    {
                        onChange(list[curIndex]);
                    }

                    dirty = true;
                    //RecalcPositions();
                }

            }

            RefreshTexture();

            base.Update(ref parentMatrix);
        }   // end of UIGridModularRadioBoxElement Update()

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
                Vector2 size = new Vector2(w, radioWhite.Height);
                quad.Render(radioWhite, position, size, "TexturedNoAlpha");

                // And the black parts.
                position.Y = 70;
                size.Y = h - 70;
                quad.Render(middleBlack, position, size, "TexturedRegularAlpha");
                position.Y = 64;
                size.Y = radioBlack.Height;
                quad.Render(radioBlack, position, size, "TexturedRegularAlpha");

                // Disable writing to alpha channel.
                // This prevents transparent fringing around the text.
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
                device.BlendState = UI2D.Shared.BlendStateColorWriteRGB;

                // Render the label and value text into the texture.
                int margin = 0;
                position.X = 0;
                position.Y = (int)((64 - Font().LineSpacing) / 2.0f);
                int textWidth = (int)(Font().MeasureString(label).X);

                justify = Justification.Center;
                position.X = TextHelper.CalcJustificationOffset(margin, w, textWidth, justify);

                Color labelColor = new Color(127, 127, 127);
                Color valueColor = new Color(140, 200, 63);
                Color shadowColor = new Color(0, 0, 0, 20);
                Vector2 shadowOffset = new Vector2(0, 6);
                Color entryColor = new Color(200, 200, 200);
                Color selectedColor = new Color(0, 255, 12);

                SpriteBatch batch = UI2D.Shared.SpriteBatch;
                batch.Begin();

                // Title.
                TextHelper.DrawString(Font, label, position + shadowOffset, shadowColor);
                TextHelper.DrawString(Font, label, position, labelColor);

                // Entries.
                UI2D.Shared.GetFont entryFont = UI2D.Shared.GetGameFont18Bold;

                if (numColumns == 1)
                {
                    position.Y = 70;
                    Vector2 min = Vector2.Zero;
                    Vector2 max = Vector2.One;

                    for (int i = 0; i < list.Count; i++)
                    {
                        position.X = (512 - entryFont().MeasureString(list[i].Text).X) / 2;
                        TextHelper.DrawString(entryFont, list[i].Text, position, curIndex == i ? selectedColor : entryColor);

                        if (showIndicators)
                        {
                            int vert = 5;
                            if (curIndex == i)
                            {
                                quad.Render(indicatorLit, new Vector2(30, position.Y + vert), new Vector2(indicatorLit.Width, indicatorLit.Height), "TexturedRegularAlpha");
                                quad.Render(indicatorLit, new Vector2(512 - 30 - indicatorLit.Width, position.Y + vert), new Vector2(indicatorLit.Width, indicatorLit.Height), "TexturedRegularAlpha");
                            }
                            else
                            {
                                quad.Render(indicatorUnlit, new Vector2(30, position.Y + vert), new Vector2(indicatorUnlit.Width, indicatorUnlit.Height), "TexturedRegularAlpha");
                                quad.Render(indicatorUnlit, new Vector2(512 - 30 - indicatorLit.Width, position.Y + vert), new Vector2(indicatorUnlit.Width, indicatorUnlit.Height), "TexturedRegularAlpha");
                            }
                        }

                        min.Y = position.Y / h;

                        position.Y += entryFont().LineSpacing + 4;

                        max.Y = position.Y / h;
                        if (list[i].Box == null)
                        {
                            list[i].Box = new AABB2D(min, max);
                        }
                        else
                        {
                            list[i].Box.Set(min, max);
                        }
                    }
                }
                else if (numColumns == 2)
                {
                    // Probably not as general as we'd like but I think this only
                    // get used for the language options so just do what we need
                    // to get all the languages to fit.
                    // For column 0, left justify.
                    // For column 1, right justify.
                    for (int column = 0; column < numColumns; column++)
                    {
                        position.Y = 70;

                        int width = 512 / numColumns;
                        int center = width / 2 + column * width;
                        int itemsPerColumn = (list.Count + (numColumns - 1)) / numColumns;
                        int minIndex = column * itemsPerColumn;
                        int maxIndex = Math.Min(list.Count, (column + 1) * itemsPerColumn);

                        // Min/max values ine 0..1 range across rt.
                        Vector2 min = new Vector2((center - width / 2.0f)/w, 0);
                        Vector2 max = new Vector2((center + width / 2.0f) / w, 1);

                        for (int i = minIndex; i < maxIndex; i++)
                        {
                            position.X = (int)(center - (entryFont().MeasureString(list[i].Text).X) / 2);
                            int edgeMargin = 8;
                            if (column == 0)
                            {
                                position.X = edgeMargin;
                            }
                            else
                            {
                                position.X = (int)(512 - edgeMargin - entryFont().MeasureString(list[i].Text).X);
                            }
                            TextHelper.DrawString(entryFont, list[i].Text, position, curIndex == i ? selectedColor : entryColor);

                            min.Y = position.Y / h;

                            position.Y += entryFont().LineSpacing + 4;

                            max.Y = position.Y / h;
                            if (list[i].Box == null)
                            {
                                list[i].Box = new AABB2D(min, max);
                            }
                            else
                            {
                                list[i].Box.Set(min, max);
                            }
                        }
                    }
                }
                else
                {
                    Debug.Assert(false, "numColumns must be 1 or 2, nothing else is supported.");
                }


                batch.End();

                // Restore default blend state.
                device.BlendState = BlendState.AlphaBlend;

                // Restore backbuffer.
                InGame.RestoreRenderTarget();

                dirty = false;
            }
        }   // end of UIGridModularRadioBoxElement RefreshTexture()

        public override void HandleMouseInput(Vector2 hitUV)
        {
            // See if we hit an element.
            int hit = -1;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Box.Contains(hitUV))
                {
                    hit = i;
                    break;
                }
            }

            if (hit != -1)
            {
                if (MouseInput.Left.WasPressed)
                {
                    MouseInput.ClickedOnObject = list[hit];
                }
                if (MouseInput.Left.WasReleased && MouseInput.ClickedOnObject == list[hit])
                {
                    //Play selection sound
                    Foley.PlayPressA();


                    bool indexChanged = hit != curIndex;
                    CurIndex = hit;

                    if (null != onSelection)
                    {
                        onSelection(list[curIndex]);
                    }

                    if (indexChanged)
                    {
                        if (null != onChange)
                        {
                            onChange(list[curIndex]);
                        }

                        dirty = true;
                    }
                }
            }
        }   // end of HandleMouseInput()

        public override void HandleTouchInput(TouchContact touch, Vector2 hitUV)
        {
            // See if we hit an element.
            int hit = -1;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Box.Contains(hitUV))
                {
                    hit = i;
                    break;
                }
            }

            if (hit != -1)
            {
                if (TouchInput.WasTouched)
                {
                    touch.TouchedObject = list[hit];
                }
                if (TouchInput.WasReleased && touch.TouchedObject == list[hit])
                {
                    //Play sound.
                    Foley.PlayPressA();

                    bool indexChanged = hit != curIndex;
                    CurIndex = hit;

                    if (null != onSelection)
                    {
                        onSelection(list[curIndex]);
                    }

                    if (indexChanged)
                    {
                        if (null != onChange)
                        {
                            onChange(list[curIndex]);
                        }

                        dirty = true;
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

            effect.Parameters["Alpha"].SetValue(Alpha);
            effect.Parameters["DiffuseColor"].SetValue(new Vector4(dim, dim, dim, 1.0f));
            effect.Parameters["SpecularColor"].SetValue(specularColor);
            effect.Parameters["SpecularPower"].SetValue(specularPower);

            effect.Parameters["NormalMap"].SetValue(normalMap);

            geometry.Render(effect);

        }   // end of UIGridModularRadioBoxElement Render()

        /// <summary>
        /// This sets the current index on the matching text line.  If no
        /// matching line is found, the current index is not changed.
        /// </summary>
        /// <param name="text"></param>
        public void SetValue(string text)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Text == text)
                {
                    curIndex = i;
                    Dirty = true;
                    break;
                }
            }
        }   // end of UIGridModularRadioBoxElement SetValue()

        /// <summary>
        /// This sets the current index on the matching key value.  If no
        /// matching line is found, the current index is not changed.
        /// Note: comparison ignores case.
        /// </summary>
        /// <param name="text"></param>
        public void SetValueByKey(string key)
        {
            for (int i = 0; i < list.Count; i++)
            {
#if NETFX_CORE
                if(string.Compare(list[i].Key, key, StringComparison.OrdinalIgnoreCase) == 0)
#else
                if (string.Compare(list[i].Key, key, true) == 0)
#endif
                {
                    curIndex = i;
                    Dirty = true;
                    break;
                }
            }
        }   // end of UIGridModularRadioBoxElement SetValueByKey()

        public override void LoadContent(bool immediate)
        {
            // Init the effect.
            if (effect == null)
            {
                effect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\UI2D");
                ShaderGlobals.RegisterEffect("UI2D", effect);
            }

            // Load the check textures.
            if (radioWhite == null)
            {
                radioWhite = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\SliderWhite");
            }
            if (radioBlack == null)
            {
                radioBlack = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\RadioBoxBlack");
            }
            if (middleBlack == null)
            {
                middleBlack = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\MiddleBlack");
            }
            if (indicatorLit == null)
            {
                indicatorLit = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\IndicatorLit");
            }
            if (indicatorUnlit == null)
            {
                indicatorUnlit = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\IndicatorUnlit");
            }

            // Load the normal map texture.
            if (normalMapName != null)
            {
                normalMap = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\" + normalMapName);
            }
        }

        private void GetWH(out int w, out int h)
        {
            UI2D.Shared.GetFont entryFont = UI2D.Shared.GetGameFont18Bold;

            w = 512;
            // Note: the Count + (numColumns - 1) is there to ensure we get the right height
            // when the count is not an exact multiple of numColumns.
            h = 76 + (entryFont().LineSpacing + 4) * ((list.Count + (numColumns - 1)) / numColumns);
        }

        public override void InitDeviceResources(GraphicsDevice device)
        {
            int w, h;
            GetWH(out w, out h);

            height = h * width / w;
            // Create the geometry.
            geometry = new Base9Grid(width, height, edgeSize);

            // Create the diffuse texture.  Leave it null if we have no text to render.
            if (diffuse == null)
            {
                CreateRenderTargets(device);
            }

            BokuGame.Load(geometry, true);

            dirty = true;
        }

        public override void UnloadContent()
        {
            base.UnloadContent();

            ReleaseRenderTargets();

            BokuGame.Release(ref effect);
            BokuGame.Release(ref normalMap);
            BokuGame.Release(ref radioWhite);
            BokuGame.Release(ref radioBlack);
            BokuGame.Release(ref middleBlack);
            BokuGame.Release(ref indicatorLit);
            BokuGame.Release(ref indicatorUnlit);

            BokuGame.Unload(geometry);
            geometry = null;
        }   // end of UIGridModularRadioBoxElement UnloadContent()

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
            int w, h;
            GetWH(out w, out h);

            diffuse = new RenderTarget2D(
                device,
                w, h,
                false,      // mipmaps
                SurfaceFormat.Color,
                DepthFormat.None);

            InGame.GetRT("UIGridModularRadioBoxElement", diffuse);

            dirty = true;
            RefreshTexture();
        }

        private void ReleaseRenderTargets()
        {
            InGame.RelRT("UIGridModularRadioBoxElement", diffuse);
            BokuGame.Release(ref diffuse);
        }

    }   // end of class UIGridModularRadioBoxElement

}   // end of namespace Boku.UI2D






