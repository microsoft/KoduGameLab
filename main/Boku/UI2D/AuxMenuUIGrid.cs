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

using KoiX;

using Boku.Base;
using Boku.Common;
using Boku.Fx;
using Boku.UI2D;
using Boku.Input;

namespace Boku.UI2D
{
    /// <summary>
    /// A specialized version of the a UIGrid designed to be used with the 
    /// aux menus on the load level menu.  The aux menus are those labelled 
    /// "Sort By" and "Show Only".  The main differrentiating factor for 
    /// this menu is the fact that the menu renders its own background while
    /// the elements are just rendered as text over the top of the menu.
    /// </summary>
    public class AuxMenuUIGrid : UIGrid, INeedsDeviceReset
    {
        #region Members

        private Effect effect = null;
        private string backgroundName = null;
        private Texture2D backgroundTexture = null;

        // Properties for the underlying 9-grid geometry.
        private float width;
        private float height;
        private float edgeSize;
        private Base9Grid geometry = null;

        private float lineHeight = 28.0f;
        private Vector2 lowerLeftCorner = Vector2.Zero;

        private SpriteBatch batch = null;

        #endregion

        #region Accessors

        /// <summary>
        /// Where we want to place the lower left corner of the menu.
        /// This is in UICamera space coordinates.
        /// </summary>
        public Vector2 LowerLeftCorner
        {
            set { lowerLeftCorner = value; }
        }

        #endregion

        #region Public

        public AuxMenuUIGrid(
            UIGridEvent onSelect,
            UIGridEvent onCancel,
            Point maxDimensions,
            string backgroundName,
            string uiMode)
            : base(onSelect, onCancel, maxDimensions, uiMode)
        {
            this.backgroundName = backgroundName;

            edgeSize = 64.0f / 96.0f;
            height = (maxDimensions.Y * lineHeight) / 96.0f + 2.0f * edgeSize;
            float minWidth = 3.0f;
            width = Math.Max(2.0f * edgeSize, minWidth);

            geometry = new Base9Grid(width, height, edgeSize);

        }   // end of c'tor

        public override void Render(Camera camera)
        {
            if (active || renderWhenInactive)
            {
                // Update the size if needed.
                float w = width;
                for (int i = 0; i < ActualDimensions.Y; i++)
                {
                    UIGrid2DFloatingTextElement e = (UIGrid2DFloatingTextElement)grid[0, i];
                    w = Math.Max(w, e.WidthOfLabelInPixels);
                }
                w /= 96.0f; // pixels to inches.
                w += 2.0f * edgeSize;
                if (w > width)
                {
                    width = w;
                    BokuGame.Unload(geometry);

                    geometry = new Base9Grid(width, height, edgeSize);
                    BokuGame.Load(geometry);
                }

                UpdateSelectionFocus();

                effect.CurrentTechnique = effect.Techniques["TexturedRegularAlpha"];

                effect.Parameters["DiffuseTexture"].SetValue(backgroundTexture);

                // Calc position from lowerLeftCorner.
                Vector3 trans = new Vector3(lowerLeftCorner.X + (width - edgeSize) / 2.0f, lowerLeftCorner.Y + (height - edgeSize) / 2.0f, 0.0f);
                worldMatrix.Translation = trans;

                effect.Parameters["WorldMatrix"].SetValue(worldMatrix);
                effect.Parameters["WorldViewProjMatrix"].SetValue(worldMatrix * camera.ViewProjectionMatrix);

                effect.Parameters["Alpha"].SetValue(1.0f);
                effect.Parameters["DiffuseColor"].SetValue(Vector4.One);

                geometry.Render(effect);

                //
                // Render the menu elements.
                //
                Vector3 pos = worldMatrix.Translation + new Vector3(-(width / 2.0f - edgeSize), height / 2.0f - edgeSize, 0.0f);
                Point pixelCoord = camera.WorldToScreenCoords(pos);
                Vector2 position = new Vector2(pixelCoord.X, pixelCoord.Y);
                batch.Begin();

                for (int i = 0; i < ActualDimensions.Y; i++)
                {
                    UIGrid2DFloatingTextElement e = (UIGrid2DFloatingTextElement)grid[0, i];
                    e.RenderAt(batch, position);

                    position.Y += lineHeight;
                }

                batch.End();
            }
            //base.Render(camera);
        }

        #endregion

        #region Internal

        public new void LoadContent(bool immediate)
        {
            // Init the effect.
            if (effect == null)
            {
                effect = KoiLibrary.LoadEffect(@"Shaders\UI2D");
                ShaderGlobals.RegisterEffect("UI2D", effect);
            }

            if (backgroundTexture == null)
            {
                backgroundTexture = KoiLibrary.LoadTexture2D(backgroundName);
            }

            base.LoadContent(immediate);
        }   // end of LoadContent()

        public new void InitDeviceResources(GraphicsDevice device)
        {
            batch = KoiLibrary.SpriteBatch;

            BokuGame.Load(geometry, true);
        }

        public new void UnloadContent()
        {
            DeviceResetX.Release(ref backgroundTexture);
            DeviceResetX.Release(ref effect);

            batch = null;

            BokuGame.Unload(geometry);

            base.UnloadContent();
        }   // end of UnloadContent()

        #endregion

    }   // end of class AuxMenuUIGrid

}   // end of namespace Boku.Ui2d
