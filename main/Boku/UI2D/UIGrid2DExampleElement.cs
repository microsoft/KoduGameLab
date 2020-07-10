// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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
    /// Element used for examples in programming help.  They display a single reflex and 
    /// some text describing the reflex.
    /// </summary>
    public class UIGrid2DExampleElement : UIGridElement
    {
        #region Members

        private ExamplePage example = null;

        public List<Texture2D> tiles = null;    // The tiles on this example.

        private Texture2D selectedBackground = null;
        private Texture2D unselectedBackground = null;

        private float width;
        private float height;

        private bool selected = false;

        private Color textColor = new Color(127, 127, 127);

        private TextBlob descBlob = null;

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
                        // Create a twitch to change to selected.
                        TwitchManager.Set<float> set = delegate(float val, Object param) { alpha = val; };
                        TwitchManager.CreateTwitch<float>(alpha, 1.0f, set, 0.15, TwitchCurve.Shape.EaseInOut);
                    }
                    else
                    {
                        // Create a twitch to change to unselected.
                        TwitchManager.Set<float> set = delegate(float val, Object param) { alpha = val; };
                        TwitchManager.CreateTwitch<float>(alpha, 0.0f, set, 0.15, TwitchCurve.Shape.EaseInOut);
                    }
                    selected = value;
                }
            }
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
        /// Access the Example associated with this element.
        /// </summary>
        public ExamplePage Example
        {
            get { return example; }
        }

        #endregion

        #region Public

        // c'tor
        /// <summary>
        /// Simple c'tor using a blob to hold the common data.
        /// </summary>
        /// <param name="blob"></param>
        /// <param name="label"></param>
        public UIGrid2DExampleElement(ParamBlob blob, ExamplePage example)
        {
            this.example = example;

            // blob
            this.width = blob.width;
            this.height = blob.height;
            this.Font = blob.Font;
            this.textColor = blob.textColor;

            descBlob = new TextBlob(SharedX.GetGameFont24, example.description.Trim(), 650);
            descBlob.Justification = blob.justify;

            height = 1.1f + descBlob.NumLines * descBlob.TotalSpacing / 96.0f;

            alpha = 0.0f;   // Init to 0 since we're using it for selection.
        }

        public override void  Update(ref Matrix parentMatrix)
        {
            base.Update(ref parentMatrix);
        }   // end of UIGrid2DExampleElement Update()

        public override void HandleMouseInput(Vector2 hitUV)
        {
        }   // end of HandleMouseInput()


        public override void HandleTouchInput(TouchContact touch, Vector2 hitUV)
        {
        }   // end of HandleTouchInput()

        public override void Render(Camera camera)
        {
            CameraSpaceQuad quad = CameraSpaceQuad.GetInstance();

            Vector2 size = new Vector2(1.0f, 1.0f);
            Vector2 pos = new Vector2(worldMatrix.Translation.X, worldMatrix.Translation.Y);
            //pos.Y += height / 2.0f;
            float gap = 0.1f;

            if (camera.Frustum.CullTest(worldMatrix.Translation, 1.2f) == Frustum.CullResult.TotallyOutside)
            {
                return;
            }

            // Render the reflex.

            if (tiles != null)
            {
                for (int i = 0; i < tiles.Count; i++)
                {
                    // Not fully selected?
                    if (alpha < 1.0f)
                    {
                        quad.Render(camera, unselectedBackground, pos + new Vector2(i * (size.X + gap), 0), size, @"TexturedRegularAlpha");
                    }

                    // Not fully unselected?
                    if (alpha > 0.0f)
                    {
                        quad.Render(camera, selectedBackground, alpha, pos + new Vector2(i * (size.X + gap), 0), size, @"TexturedRegularAlpha");
                    }

                    quad.Render(camera, tiles[i], pos + new Vector2(i * (size.X + gap), 0), size, @"TexturedRegularAlpha");

                }
            }

            // And the description.
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            //device.BlendState = Shared.BlendStateColorWriteRGB;

            pos.Y -= size.Y / 2.0f;
            Point pixelCoord = camera.WorldToScreenCoords(new Vector3(pos.X, pos.Y, 0.0f));
            pos = new Vector2(pixelCoord.X, pixelCoord.Y);
            descBlob.RenderText(null, pos, textColor);

            //device.BlendState = BlendState.AlphaBlend;


        }   // end of UIGrid2DExampleElement Render()

        #endregion

        #region Internal

        public override void LoadContent(bool immediate)
        {
            // Load the normal map texture.
            if (selectedBackground == null)
            {
                selectedBackground = KoiLibrary.LoadTexture2D(@"Textures\HelpCard\GreenSquare");
            }
            if (unselectedBackground == null)
            {
                unselectedBackground = KoiLibrary.LoadTexture2D(@"Textures\HelpCard\GreySquare");
            }

            if (tiles == null)
            {
                tiles = new List<Texture2D>();

                // For now, we're assuming there's only one reflex.
                ReflexData reflex = example.reflexes[0];

                // Sensor
                if (reflex.Sensor != null)
                {
                    tiles.Add(CardSpace.Cards.CardFaceTexture(reflex.sensorUpid));
                }

                // Filters
                if (reflex.Filters != null)
                {
                    for (int i = 0; i < reflex.Filters.Count; i++)
                    {
                        tiles.Add(CardSpace.Cards.CardFaceTexture(reflex.filterUpids[i]));
                    }
                }

                // Actuator
                if (reflex.Actuator != null)
                {
                    tiles.Add(CardSpace.Cards.CardFaceTexture(reflex.actuatorUpid));
                }

                // Selector
                if (reflex.Selector != null)
                {
                    tiles.Add(CardSpace.Cards.CardFaceTexture(reflex.selectorUpid));
                }

                // Modifiers
                if (reflex.Modifiers != null)
                {
                    for (int i = 0; i < reflex.Modifiers.Count; i++)
                    {
                        tiles.Add(CardSpace.Cards.CardFaceTexture(reflex.modifierUpids[i]));
                    }
                }

            }

        }   // end of UIGrid2DExampleElement LoadContent()

        public override void InitDeviceResources(GraphicsDevice device)
        {
        }   // end of UIGrid2DExampleElement InitDeviceResources()

        public override void UnloadContent()
        {
            base.UnloadContent();

            DeviceResetX.Release(ref selectedBackground);
            DeviceResetX.Release(ref unselectedBackground);

            for(int i=0; i<tiles.Count; i++)
            {
                tiles[i] = null;
            }
            tiles.Clear();
            tiles = null;
        }   // end of UIGrid2DExampleElement UnloadContent()

        #endregion

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public override void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class UIGrid2DExampleElement

}   // end of namespace Boku.UI2D






