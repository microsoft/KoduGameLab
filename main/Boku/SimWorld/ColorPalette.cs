// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.UI2D;

namespace Boku.SimWorld
{
    /// <summary>
    /// A holder for the object edit color palette information that we
    /// want to persist from frame to frame.  Simplifies things by
    /// assuming that all the objects are square.
    /// </summary>
    public class ColorPalette : INeedsDeviceReset
    {
        private const int numEntries = (int)Classification.Colors.SIZEOF - 1;
        private bool active = false;

        private const float yPosition = 3.0f;       // Normal, on screen postion.
        private const float swatchMargin = 0.0f;    // Margin between color swatches.

        private const float activeSize = 1.0f;      // Size of active elements.
        private const float inactiveSize = 0.7f;    // Size of inactive elements.

        private float currentY = yPosition + activeSize * 2.0f;    // Used to slide the palette on and off the screen.  Init offscreen.

        private Texture2D shadowMask = null;

        private Vector2[] texturePosition = new Vector2[numEntries];
        private float[] textureSize = new float[numEntries];

        private UIGrid2DTextureElement[] buttons = new UIGrid2DTextureElement[numEntries];

        private int prevColor = -1;

        private AABB2D[] buttonHitBoxes = null;

        UiCamera camera;

        bool handlingTouch = false;

        #region Accessors
        /// <summary>
        /// If this flag is set, then the color palette is currently handling the touch, which can signal
        /// to other input handlers to defer their input handling until the touch is complete.
        /// </summary>
        public bool HandlingTouch
        {
            get { return handlingTouch; }
        }

        public int NumEntries
        {
            get { return numEntries; }
        }
        public bool Active
        {
            get { return active; }
            set 
            {
                if (active != value)
                {
                    if (value)
                    {
                        TwitchManager.Set<float> set = delegate(float val, Object param) { currentY = val; };
                        TwitchManager.CreateTwitch<float>(currentY, yPosition, set, 0.2f, TwitchCurve.Shape.EaseInOut);
                    }
                    else
                    {
                        TwitchManager.Set<float> set = delegate(float val, Object param) { currentY = val; };
                        TwitchManager.CreateTwitch<float>(currentY, yPosition + activeSize * 2.0f, set, 0.2f, TwitchCurve.Shape.EaseInOut);
                        // If we're going inactive we definitely shouldn't be handling touch anymore
                        handlingTouch = false;
                    }

                    active = value;
                }
            }
        }
        #endregion

        // c'tor
        public ColorPalette()
        {
            // Create elements for the color buttons.
            // Start with a blob of common parameters.
            UIGridElement.ParamBlob blob = new UIGridElement.ParamBlob();
            blob.width = 1.0f;
            blob.height = 1.0f;
            blob.edgeSize = 0.5f;
            blob.selectedColor = Color.White;
            blob.unselectedColor = Color.White;
            blob.normalMapName = @"QuarterRound4NormalMap";

            int numColors = (int)Classification.Colors.SIZEOF - 1;
            buttonHitBoxes = new AABB2D[numColors];
            for (int i = 0; i < numEntries; i++)
            {
                buttonHitBoxes[i] = new AABB2D();
            }

            for (int i = 0; i < numEntries; i++)
            {
                buttons[i] = new UIGrid2DTextureElement(blob, null);
                buttons[i].NoZ = false; 

                // Set initial size.
                textureSize[i] = inactiveSize;

                buttons[i].BaseColor = Classification.ColorVector4(GetColorFromIndex(i));
            }

            camera = new UiCamera();
        }

        /// <summary>
        /// Based on the size of each element and the margins, calculate its position.
        /// </summary>
        public void CalcPositions(UiCamera camera)
        {
            Vector2 cur = new Vector2(0, currentY);

            // Lay out the textures left to right.
            for (int i = 0; i < numEntries; i++)
            {
                texturePosition[i] = cur;
                cur.X += textureSize[i] + swatchMargin;
            }

            // Calculate the overall width so that we can center the palette on the screen.
            float width = cur.X - swatchMargin;
            float leftEdge = -width / 2.0f;

            // Calc position of each button.
            texturePosition[0].X = leftEdge + textureSize[0] / 2.0f;
            for (int i = 1; i < numEntries; i++)
            {
                texturePosition[i].X = texturePosition[i - 1].X + textureSize[i - 1] / 2.0f + textureSize[i] / 2.0f + swatchMargin;
            }

            // Calculate the hit boxes for each button
            for (int i = 0; i < numEntries; i++)
            {
                // Since the color boxes are perfectly square and of unit size, we can use the scalar size value
                // to determine the button's actual dimensions.
                // Also, the texture position represents the center of the button, so we need to adjust for our
                // actual button position (the top left).
                Vector2 buttonPosition = texturePosition[i] - 
                    new Vector2(textureSize[i] / 2.0f, -textureSize[i] / 2.0f);
                Point topLeftCoord = camera.WorldToScreenCoords(new Vector3(buttonPosition, 0.0f));
                Vector2 textureBottomRightPosition =
                    new Vector2(textureSize[i], -textureSize[i]) + buttonPosition;
                Point bottomRightCoord = camera.WorldToScreenCoords(
                    new Vector3(textureBottomRightPosition, 0.0f));
                buttonHitBoxes[i].Set(
                    new Vector2(topLeftCoord.X, topLeftCoord.Y),
                    new Vector2(bottomRightCoord.X, bottomRightCoord.Y)
                );
            }
        }   // end of ColorPalette CalcPositions()

        /// <summary>
        /// Attempts to get a new color selection from the touch input. If no color is picked through
        /// this method, then Colors.None will be returned.
        /// </summary>
        /// <returns></returns>
        public Classification.Colors GetColorFromTouch()
        {
            if(!active) { return Classification.Colors.None;}

            TouchContact touch = TouchInput.GetOldestTouch();
            if (touch != null)
            {
                for (int i = 0; i < numEntries; i++)
                {
                    if (buttonHitBoxes[i].Contains(touch.position))
                    {
                        if (touch.phase == TouchPhase.Began)
                        {
                            handlingTouch = true;
                        }

                        // If we're hitting boxes but we aren't flagged as the handler, then we won't
                        // report a hit, so we can allow the touch to fall through to other handlers.
                        if (handlingTouch)
                        {
                            return GetColorFromIndex(i);
                        }
                        else
                        {
                            return Classification.Colors.None;
                        }
                    }
                }
            }
            else
            {
                handlingTouch = false;
            }
            return Classification.Colors.None;
        }

        /// <summary>
        /// Renders the waypoint color palette.
        /// </summary>
        /// <param name="activeColor">Active color in 0..6 range</param>
        public void Render(int activeColor)
        {
            // First, check if the active color has changed.  If so, create twitches to smoothly change scales.
            if (activeColor != prevColor)
            {
                if (prevColor != -1)
                {
                    int index = prevColor;
                    TwitchManager.Set<float> set = delegate(float val, Object param) { textureSize[index] = val; };
                    TwitchManager.CreateTwitch<float>(textureSize[index], inactiveSize, set, 0.2f, TwitchCurve.Shape.EaseInOut);
                }
                {
                    int index = activeColor;
                    TwitchManager.Set<float> set = delegate(float val, Object param) { textureSize[index] = val; };
                    TwitchManager.CreateTwitch<float>(textureSize[index], activeSize, set, 0.2f, TwitchCurve.Shape.EaseInOut);
                }

                prevColor = activeColor;
            }

            camera.Resolution = new Point((int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);
            camera.Dirty = true;
            camera.Recalc();

            // Update positions.
            CalcPositions(camera);

            // Set up params for rendering UI with this camera.
            Fx.ShaderGlobals.SetCamera(camera);

            for (int i = 0; i < numEntries; i++)
            {
                buttons[i].Scale = textureSize[i];
                buttons[i].Position = new Vector3(texturePosition[i], 2.0f);
                buttons[i].Update();
                buttons[i].Render(camera);
            }

        }   // end of ColorPalette Render()


        public static Classification.Colors GetColorFromIndex(int colorIndex)
        {
            Classification.Colors color = Classification.Colors.Red;

            switch (colorIndex)
            {
                case 0:
                    color = Classification.Colors.Black;
                    break;
                case 1:
                    color = Classification.Colors.Grey;
                    break;
                case 2:
                    color = Classification.Colors.White;
                    break;
                case 3:
                    color = Classification.Colors.Red;
                    break;
                case 4:
                    color = Classification.Colors.Orange;
                    break;
                case 5:
                    color = Classification.Colors.Yellow;
                    break;
                case 6:
                    color = Classification.Colors.Green;
                    break;
                case 7:
                    color = Classification.Colors.Blue;
                    break;
                case 8:
                    color = Classification.Colors.Purple;
                    break;
                case 9:
                    color = Classification.Colors.Pink;
                    break;
                case 10:
                    color = Classification.Colors.Brown;
                    break;
            }

            return color;

        }   // end of ColorPalette GetColorFromIndex()

        public static int GetIndexFromColor(Classification.Colors color)
        {
            int index = 0;

            switch (color)
            {
                case Classification.Colors.Black:
                    index = 0;
                    break;
                case Classification.Colors.Grey:
                    index = 1;
                    break;
                case Classification.Colors.White:
                    index = 2;
                    break;
                case Classification.Colors.Red:
                    index = 3;
                    break;
                case Classification.Colors.Orange:
                    index = 4;
                    break;
                case Classification.Colors.Yellow:
                    index = 5;
                    break;
                case Classification.Colors.Green:
                    index = 6;
                    break;
                case Classification.Colors.Blue:
                    index = 7;
                    break;
                case Classification.Colors.Purple:
                    index = 8;
                    break;
                case Classification.Colors.Pink:
                    index = 9;
                    break;
                case Classification.Colors.Brown:
                    index = 10;
                    break;
            }

            return index;

        }   // end of ColorPalette GetIndexFromColor()


        public void LoadContent(bool immediate)
        {
            if (shadowMask == null)
            {
                shadowMask = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Terrain\TexturePaletteMask");
            }

            for (int i = 0; i < numEntries; i++)
            {
                BokuGame.Load(buttons[i], immediate);
            }
        }

        public void InitDeviceResources(GraphicsDevice device)
        {
        }

        public void UnloadContent()
        {
            BokuGame.Release(ref shadowMask);

            for (int i = 0; i < numEntries; i++)
            {
                BokuGame.Unload(buttons[i]);
            }
        }

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
            for (int i = 0; i < numEntries; ++i)
            {
                BokuGame.DeviceReset(buttons[i], device);
            }
        }

    }   // end of class ColorPalette

}   // end of namespace Boku.SimWorld
