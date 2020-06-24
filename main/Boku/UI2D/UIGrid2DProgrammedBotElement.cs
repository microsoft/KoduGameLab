
using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Text;

using Boku.Common;
using Boku.Fx;
using Boku.Programming;

namespace Boku.UI2D
{
    /// <summary>
    /// Grid element for hold a preprogrammed bot.  The element displays a single, representative 
    /// programming tile and a text description of what the programming does.
    /// 
    /// Note this has mutated into a very different beast form most of the uigrid elements.  This
    /// no longer renders into a texture, rather it renders everything on the fly.  This helps
    /// with the clarity of the text.
    /// </summary>
    public class UIGrid2DProgrammedBotElement : UIGridElement
    {
        #region Members

        private Texture2D tile = null;
        private TextBlob textBlob = null;

        private ActorHelp actorHelp = null; 
        private int exampleIndex = -1;              // Which program from the above ActorHelp this element represents.

        // Properties for the underlying 9-grid geometry.
        private float width;
        private float height;
        private float edgeSize;

        private Vector4 baseColor;          // Color that shows through where the texture is transparent.
        private Vector4 selectedColor;
        private Vector4 unselectedColor;
        private bool selected = false;

        private Vector4 specularColor = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
        private float specularPower = 16.0f;

        private Color textColor;
        private Color dropShadowColor;
        private bool useDropShadow = false;
        private bool invertDropShadow = false;  // Puts the drop shadow above the regular letter instead of below.
        private TextHelper.Justification justify = TextHelper.Justification.Left;
        private bool altShader = false;
        private bool ignorePowerOf2 = false;

        private Vector4 selectedTextColor = new Vector4(0.0f, 1.0f, 0.05f, 1.0f);
        private Vector4 unselectedTextColor = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);

        private float selectedAlpha = 1.0f;
        private float unselectedAlpha = 0.5f;

        #endregion

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
                        {
                            // Create a twitch to change to selected.
                            {
                                TwitchManager.Set<Vector4> set = delegate(Vector4 val, Object param) { textColor = new Color(val); dirty = true;  };
                                TwitchManager.CreateTwitch<Vector4>(textColor.ToVector4(), selectedTextColor, set, 0.15, TwitchCurve.Shape.EaseInOut);
                            }
                            {
                                TwitchManager.Set<float> set = delegate(float val, Object param) { alpha = val; dirty = true; };
                                TwitchManager.CreateTwitch<float>(alpha, selectedAlpha, set, 0.15, TwitchCurve.Shape.EaseInOut);
                            }
                        }
                    }
                    else
                    {
                        // Create a twitch to change to unselected.
                        {
                            TwitchManager.Set<Vector4> set = delegate(Vector4 val, Object param) { textColor = new Color(val); dirty = true; };
                            TwitchManager.CreateTwitch<Vector4>(textColor.ToVector4(), unselectedTextColor, set, 0.15, TwitchCurve.Shape.EaseInOut);
                        }
                        {
                            TwitchManager.Set<float> set = delegate(float val, Object param) { alpha = val; dirty = true; };
                            TwitchManager.CreateTwitch<float>(alpha, unselectedAlpha, set, 0.15, TwitchCurve.Shape.EaseInOut);
                        }
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

        public override Vector2 Size
        {
            get { return new Vector2(width, height); }
            set { /* do nothing, should be removed from base class */ }
        }

        /// <summary>
        /// Uses an alternate shader for rendering.  This shader treats the texture
        /// contents as fully lit and doesn't attenuate the textured pixels by the 
        /// alpha of the object color.
        /// </summary>
        public bool AltShader
        {
            get { return altShader; }
            set { altShader = value; }
        }

        /// <summary>
        /// Access the ActorHelp associated with this element.
        /// </summary>
        public ActorHelp ActorHelp
        {
            get { return actorHelp; }
        }

        public int ExampleIndex
        {
            get { return exampleIndex; }
        }

        /// <summary>
        /// Should be set to true if the texture for this tile is not from a generated rendertarget and should always be stretched fully across the tile.
        /// </summary>
        public bool IgnorePowerOf2
        {
            get { return ignorePowerOf2; }
            set { ignorePowerOf2 = value; }
        }

        #endregion

        #region Public

        // c'tor
        public UIGrid2DProgrammedBotElement(ParamBlob blob, ActorHelp actorHelp, int exampleIndex)
        {
            this.actorHelp = actorHelp;
            this.exampleIndex = exampleIndex;

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

        }

        public override void Update(ref Matrix parentMatrix)
        {
            base.Update(ref parentMatrix);

            if (dirty)
            {
                tile = CardSpace.Cards.CardFaceTexture(actorHelp.programs[exampleIndex].tileUpid);
            }

        }   // end of UIGrid2DProgrammedBotElement Update()

        public override void HandleMouseInput(Vector2 hitUV)
        {
        }   // end of HandleMouseInput()

        public override void HandleTouchInput(TouchContact touch, Vector2 hitUV)
        {
        }   // end of HandleTouchInput()

        public override void Render(Camera camera)
        {
            CameraSpaceQuad quad = CameraSpaceQuad.GetInstance();

            Vector2 pos = new Vector2(worldMatrix.Translation.X, worldMatrix.Translation.Y);
            pos.X -= 3.4f;
            quad.Render(camera, tile, alpha, pos, new Vector2(1.2f, 1.2f), "TexturedRegularAlpha");

            pos.X += 0.8f;
            pos.Y += 0.7f;
            Point loc = camera.WorldToScreenCoords(new Vector3(pos.X, pos.Y, 0.0f));
            pos = new Vector2(loc.X, loc.Y);

            if (textBlob.NumLines == 1)
                pos.Y += textBlob.Font().LineSpacing;
            if (textBlob.NumLines == 2)
                pos.Y += 0.5f * textBlob.Font().LineSpacing;

            textBlob.RenderText(null, pos, textColor, maxLines: 3);

        }   // end of UIGrid2DProgrammedBotElement Render()

        #endregion

        #region Internal

        public override void LoadContent(bool immediate)
        {
        }   // end of UIGrid2DProgrammedBotElement LoadContent()

        public override void InitDeviceResources(GraphicsDevice device)
        {
            // Before we can decide on a size, we need to know how many lines of text we need.
            int labelLineWidth = 530;
            textBlob = new TextBlob(SharedX.GetGameFont24, actorHelp.programs[exampleIndex].description.Trim(), labelLineWidth);

            const int dpi = 96;
            int w = (int)(dpi * width);
            int h = (int)(dpi * height);
            int originalWidth = w;
            int originalHeight = h;

        }   // end of UIGrid2DProgrammedBotElement InitDeviceResources()

        public override void UnloadContent()
        {
            base.UnloadContent();

            tile = null;
        }   // end of UIGrid2DProgrammedBotElement UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public override void DeviceReset(GraphicsDevice device)
        {
        }

        #endregion

    }   // end of class UIGrid2DProgrammedBotElement

}   // end of namespace Boku.UI2D






