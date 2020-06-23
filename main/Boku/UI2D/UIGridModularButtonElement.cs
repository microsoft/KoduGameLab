
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
    /// An instance of UIElement that uses a 9-grid element for its geometry.
    /// This is the new, modular version call so because of the way the
    /// parts fit together.
    /// 
    /// The "button" element is designed to display several buttons with
    /// labels, kind of like a dialog box.  Version 1.0 will probably be
    /// kind of limited in that it's aimed at controlling the display of
    /// the Change History.
    /// </summary>
    public class UIGridModularButtonElement : UIGridElement
    {
        public delegate void UIButtonElementEvent();

        private static Effect effect = null;

        private RenderTarget2D diffuse = null;

        private string normalMapName = null;
        private Texture2D normalMap = null;

        private Texture2D checkboxWhite = null;
        private Texture2D blackSquare = null;

        private UIButtonElementEvent onXButton = null;
        private UIButtonElementEvent onAButton = null;

        private string label = null;
        private string xButtonText = null;
        private string aButtonText = null;
        private AABB2D xButtonBox = new AABB2D();
        private AABB2D aButtonBox = new AABB2D();

        // Properties for the underlying 9-grid geometry.
        private float width;
        private float height;
        private float edgeSize;

        private Base9Grid geometry = null;

        private bool selected = false;

        private Vector4 specularColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        private float specularPower = 8.0f;

        private Color textColor;
        private Color dropShadowColor;
        private bool useDropShadow = false;
        private bool invertDropShadow = false;  // Puts the drop shadow above the regular letter instead of below.
        private Justification justify = Justification.Left;

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
                        HelpOverlay.Push("ModularButtonElement");

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
        public UIGridModularButtonElement(ParamBlob blob, string label, string aText, UIButtonElementEvent onA, string xText, UIButtonElementEvent onX)
        {
            this.label = label;
            this.aButtonText = aText;
            this.xButtonText = xText;
            this.onAButton = onA;
            this.onXButton = onX;

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

        public override void Update(ref Matrix parentMatrix)
        {
            // Check for input but only if selected.
            if (selected)
            {
                GamePadInput pad = GamePadInput.GetGamePad0();

                if (Actions.Select.WasPressed)
                {
                    Actions.Select.ClearAllWasPressedState();

                    if (onAButton != null)
                    {
                        onAButton();
                        Foley.PlayPressA();
                    }
                }

                if (Actions.X.WasPressed)
                {
                    Actions.X.ClearAllWasPressedState();

                    if (onXButton != null)
                    {
                        onXButton();
                        Foley.PlayCut();
                    }
                }
            }

            RefreshTexture();

            base.Update(ref parentMatrix);
        }   // end of UIGridModularButtonElement Update()

        /// <summary>
        /// Test mouse input against buttons.  hitUV is in 
        /// local UV coords for button.  Full range is 0..1
        /// in either coordinate.
        /// </summary>
        /// <param name="hitUV"></param>
        public override void HandleMouseInput(Vector2 hitUV)
        {
            if (onXButton != null)
            {
                if (xButtonBox.LeftPressed(hitUV))
                {
                    onXButton();
                    Foley.PlayCut();
                }
            }

            if (onAButton != null)
            {
                if (aButtonBox.LeftPressed(hitUV))
                {
                    onAButton();
                    Foley.PlayPressA();
                }
            }

        }   // end of HandleMouseInput()



        public override void HandleTouchInput(TouchContact touch, Vector2 hitUV)
        {
            if (onXButton != null)
            {
                if (xButtonBox.Touched(touch, hitUV))
                {
                    onXButton();
                    Foley.PlayCut();
                }
            }

            if (onAButton != null)
            {
                if (aButtonBox.Touched(touch, hitUV))
                {
                    onAButton();
                    Foley.PlayPressA();
                }
            }
        }   // end of HandleTouchInput()


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

        }   // end of UIGridModularButtonElement Render()

        /// <summary>
        /// If the state of the element has changed, we may need to re-create the texture.
        /// </summary>
        public void RefreshTexture()
        {
            // dirty = true;  // Debug only.

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

                // Render the black box.
                position = Vector2.Zero;
                size.X = size.Y;
                quad.Render(blackSquare, position, size, "TexturedRegularAlpha");

                // Disable writing to alpha channel.
                // This prevents transparent fringing around the text.
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
                device.BlendState = UI2D.Shared.BlendStateColorWriteRGB;

                // Render the label text into the texture.
                int margin = 16;
                position.X = (int)size.X + margin;

                string text = label + "\n";
                if (aButtonText != null)
                    text += "    <A> " + aButtonText + "\n";
                if (xButtonText != null)
                    text += "    <X> " + xButtonText;
 
                TextBlob blob = new TextBlob(Font, text, w - (int)position.X - margin);

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

                // Restore default blending.
                device.BlendState = BlendState.AlphaBlend;

                int line = blob.NumLines - 1;   // Which line in the text has the button.

                // Calc bounding boxes in UV space for A and X buttons/labels.
                if (onXButton != null)
                {
                    Vector2 min = new Vector2(position.X / w, (position.Y + line * blob.TotalSpacing) / h);
                    Vector2 max = min + new Vector2((float)blob.GetLineWidth(line) / w, (float)blob.TotalSpacing / h);
                    xButtonBox.Set(min, max);
                    --line;
                }

                if (onAButton != null)
                {
                    Vector2 min = new Vector2(position.X / w, (position.Y + line * blob.TotalSpacing) / h);
                    Vector2 max = min + new Vector2((float)blob.GetLineWidth(line) / w, (float)blob.TotalSpacing / h);
                    aButtonBox.Set(min, max);
                }

                // DEBUG Show hit box for a button as overlay.
                /*
                if (onAButton != null)
                {
                    position = new Vector2(diffuse.Width * aButtonBox.Min.X, diffuse.Height * aButtonBox.Min.Y);
                    size = new Vector2(diffuse.Width * (aButtonBox.Size.X), diffuse.Height * aButtonBox.Size.Y);
                    quad.Render(new Vector4(1, 0, 0, 0.5f), position, size);
                }
                if (onXButton != null)
                {
                    position = new Vector2(diffuse.Width * xButtonBox.Min.X, diffuse.Height * xButtonBox.Min.Y);
                    size = new Vector2(diffuse.Width * (xButtonBox.Size.X), diffuse.Height * xButtonBox.Size.Y);
                    quad.Render(new Vector4(0, 1, 0, 0.5f), position, size);
                }
                */

                // Restore backbuffer.
                InGame.RestoreRenderTarget();

                dirty = false;
            }
        }   // end of UIGridModularButtonElement Render()

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
            if (blackSquare == null)
            {
                blackSquare = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\BlackSquare");
            }

        }   // end of UIGridModularButtonElement LoadContent()

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
            BokuGame.Release(ref blackSquare);

            BokuGame.Unload(geometry);
            geometry = null;
        }   // end of UIGridModularButtonElement UnloadContent()

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
            InGame.GetRT("UIGridModularButtonElement", diffuse);

            // Refresh the texture.
            dirty = true;
            RefreshTexture();
        }

        private void ReleaseRenderTargets()
        {
            InGame.RelRT("UIGridModularButtonElement", diffuse);
            BokuGame.Release(ref diffuse);
        }

    }   // end of class UIGridModularButtonElement

}   // end of namespace Boku.UI2D






