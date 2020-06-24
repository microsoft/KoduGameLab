
using System;
using System.Collections;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;

using Boku.Common;
using Boku.Fx;
using Boku.Programming;

namespace Boku.UI2D
{
    /// <summary>
    /// An instance of UIElement that shows a 5x5 gird of LEDs
    /// for programming patterns into the Microbit.
    /// </summary>
    public class UIGrid2DLEDArrayElement : UIGridElement
    {
        #region Members

        static Effect effect = null;

        Texture2D normalMap = null;
        string normalMapName = null;
        Texture2D diffuse = null;
        string diffuseTextureName = null;
        Texture2D white = null;

        // Properties for the underlying 9-grid geometry.
        float width;
        float height;
        float edgeSize;
        bool noZ = false;
        bool ignorePowerOf2 = false;

        Base9Grid geometry = null;

        Vector4 baseColor;          // Color that shows through where the texture is transparent.
        Vector4 selectedColor;
        Vector4 unselectedColor;
        bool selected = false;

        Vector4 specularColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        float specularPower = 32.0f;

        bool altShader = false;
        bool altPreMultAlphaShader = false;
        bool greyFlatShader = false;

        int focusIndex = 0; // 0..255 value of the LED that's currently in focus.
        bool[] leds;

        public AABB2D[] ledHitBoxes;

        #endregion

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
                        // Create a twitch to change to selected color
                        TwitchManager.Set<Vector4> set = delegate(Vector4 val, Object param) { baseColor = val; };
                        TwitchManager.CreateTwitch<Vector4>(baseColor, selectedColor, set, 0.15, TwitchCurve.Shape.EaseInOut);

                        // Also change grey state.
                        TwitchGrey(0.0f, 0.15f, TwitchCurve.Shape.EaseInOut);
                    }
                    else
                    {
                        // Create a twitch to change to unselected color.
                        TwitchManager.Set<Vector4> set = delegate(Vector4 val, Object param) { baseColor = val; };
                        TwitchManager.CreateTwitch<Vector4>(baseColor, unselectedColor, set, 0.15, TwitchCurve.Shape.EaseInOut);

                        // Also change grey state.
                        TwitchGrey(1.0f, 0.15f, TwitchCurve.Shape.EaseInOut);
                    }
                    selected = value;
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
        public Vector4 BaseColor
        {
            set { baseColor = value; }
            get { return baseColor; }
        }
        public Texture2D DiffuseTexture
        {
            get { return diffuse; }
            set { diffuse = value; }
        }
        public float Width
        {
            get { return width; }
        }
        public float Height
        {
            get { return height; }
        }
        public bool NoZ
        {
            get { return noZ; }
            set { noZ = value; }
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
        /// Uses an alternate shader for rendering.  This shader treats the texture
        /// contents as fully lit and doesn't attenuate the textured pixels by the 
        /// alpha of the object color.  Assumes pre-multiplied alpha in the texture.
        /// </summary>
        public bool AltPreMultAlphaShader
        {
            get { return altPreMultAlphaShader; }
            set { altPreMultAlphaShader = value; }
        }

        /// <summary>
        /// Should be set to true if the texture for this tile is not from a generated rendertarget and should always be stretched fully across the tile.
        /// </summary>
        public bool IgnorePowerOf2
        {
            get { return ignorePowerOf2; }
            set { ignorePowerOf2 = value; }
        }

        public bool[] LEDs
        {
            get { return leds; }
            set
            {
                // Need to copy values, not just the reference so if we cancel 
                // out we haven't overwritten the original values.
                for (int i = 0; i < 25; i++)
                {
                    leds[i] = value[i];
                }
            }
        }

        /// <summary>
        /// Accessor for the on state of the LED currently in focus.
        /// </summary>
        public bool FocusLEDOn
        {
            get { return leds[focusIndex]; }
            set { leds[focusIndex] = value; }
        }

        public int FocusLEDIndex
        {
            get { return focusIndex; }
            set
            {
                focusIndex = value;
                focusIndex = Math.Max(focusIndex, 0);
                focusIndex = Math.Min(focusIndex, 24);
            }
        }

        #endregion

        // c'tor
        /// <summary>
        /// Simple c'tor using a blob to hold the common data.
        /// </summary>
        /// <param name="blob"></param>
        /// <param name="label"></param>
        public UIGrid2DLEDArrayElement(ParamBlob blob)
        {
            this.diffuseTextureName = @"TextEditor\LEDArrayBkg";

            // blob
            this.width = blob.width;
            this.height = blob.height;
            this.edgeSize = blob.edgeSize;
            this.selectedColor = blob.selectedColor.ToVector4();
            this.unselectedColor = blob.unselectedColor.ToVector4();
            this.baseColor = unselectedColor;
            this.altShader = blob.altShader;
            this.altPreMultAlphaShader = blob.altPreMultAlphaShader;
            this.greyFlatShader = blob.greyFlatShader;
            this.ignorePowerOf2 = blob.ignorePowerOf2;

            this.normalMapName = blob.normalMapName;
            this.elementName = blob.elementName;

            leds = new bool[25];
            ledHitBoxes = new AABB2D[25];
            for (int i = 0; i < 25; i++)
            {
                ledHitBoxes[i] = new AABB2D();
            }
        }   // end of c'tor

        public void Update()
        {
            Matrix parentMatrix = Matrix.Identity;

            base.Update(ref parentMatrix);
        }   // end of UIGrid2DLEDArray Update()

        public override void HandleMouseInput(Vector2 UV)
        {
        }   // end of HandleMouseInput()

        public override void HandleTouchInput(TouchContact touch, Vector2 UV)
        {
        }   // end of HandleTouchInput()

        public override void Render(Camera camera)
        {
            effect.CurrentTechnique = effect.Techniques["NormalMappedWithEnv"];
            effect.Parameters["DiffuseTexture"].SetValue(diffuse);

            effect.Parameters["WorldMatrix"].SetValue(worldMatrix);
            effect.Parameters["WorldViewProjMatrix"].SetValue(worldMatrix * camera.ViewProjectionMatrix);

            effect.Parameters["Alpha"].SetValue(Alpha);
            effect.Parameters["DiffuseColor"].SetValue(new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            effect.Parameters["SpecularColor"].SetValue(specularColor);
            effect.Parameters["SpecularPower"].SetValue(specularPower);

            effect.Parameters["NormalMap"].SetValue(normalMap);

            // Render the base tile.
            geometry.Render(effect);

            // Add the LEDs.
            CameraSpaceQuad csquad = CameraSpaceQuad.GetInstance();

            Vector2 size = new Vector2(0.2f, 0.2f);
            Vector2 innerFocusSize = new Vector2(0.24f, 0.24f);
            Vector2 outerFocusSize = new Vector2(0.28f, 0.28f);
            Vector2 margin = new Vector2(0.125f, 0.125f);
            // Postion of first LED.
            Vector2 basePosition = new Vector2(Position.X, Position.Y);
            basePosition.X = basePosition.X - 2.0f * size.X - 2.0f * margin.X;
            basePosition.Y = basePosition.Y + 2.0f * size.Y + 2.0f * margin.Y;
            for (int i = 0; i < 25; i++)
            {
                Vector2 pos = basePosition;
                pos.X += (i % 5) * (size.X + margin.X);
                pos.Y -= (int)(i / 5) * (size.Y + margin.Y);
                Vector4 color = leds[i] ? new Vector4(1, 0, 0, 1) : new Vector4(0, 0, 0, 1);
                if (i == focusIndex)
                {
                    // Highlight around in-focus LED.
                    csquad.Render(camera, white, new Vector4(1, 0, 0, 1), 1.0f, pos, outerFocusSize, "TexturedRegularAlpha");
                    csquad.Render(camera, white, new Vector4(1, 1, 1, 1), 1.0f, pos, innerFocusSize, "TexturedRegularAlpha");
                }
                csquad.Render(camera, white, color, 1.0f, pos, size, "TexturedRegularAlpha");

                // Also set hit box in camera space.
                ledHitBoxes[i].Set(pos - size, pos + size);
            }

        }   // end of UIGrid2DLEDArray Render()

        public override void LoadContent(bool immediate)
        {
            // Init the effect.
            if (effect == null)
            {
                effect = KoiLibrary.LoadEffect(@"Shaders\UI2D");
                ShaderGlobals.RegisterEffect("UI2D", effect);
            }

            // Load the normal map texture.
            if (normalMapName != null)
            {
                normalMap = KoiLibrary.LoadTexture2D(@"Textures\UI2D\" + normalMapName);
            }

            // Load the diffuse texture.
            if (diffuseTextureName != null)
            {
                diffuse = KoiLibrary.LoadTexture2D(@"Textures\" + diffuseTextureName);
            }

            if (white == null)
            {
                white = KoiLibrary.LoadTexture2D(@"Textures\White");
            }

        }   // end of UIGrid2DLEDArray LoadContent()

        public override void InitDeviceResources(GraphicsDevice device)
        {
            const int dpi = 96;
            int w = (int)(dpi * width);
            int h = (int)(dpi * height);

            // Create the geometry.
            if (BokuGame.RequiresPowerOf2 && !IgnorePowerOf2)
            {
                float u = (float)w / (float)MyMath.GetNextPowerOfTwo(w);
                float v = (float)h / (float)MyMath.GetNextPowerOfTwo(h);
                geometry = new Base9Grid(width, height, edgeSize, u, v);
            }
            else
            {
                geometry = new Base9Grid(width, height, edgeSize);
            }

            BokuGame.Load(geometry, true);
        }

        public override void UnloadContent()
        {
            base.UnloadContent();

            DeviceResetX.Release(ref effect);
            DeviceResetX.Release(ref normalMap);
            DeviceResetX.Release(ref diffuse);
            DeviceResetX.Release(ref white);

            BokuGame.Unload(geometry);
            geometry = null;
        }   // end of UIGrid2DLEDArray UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public override void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class UIGrid2DLEDArrayElement

}   // end of namespace Boku.UI2D






