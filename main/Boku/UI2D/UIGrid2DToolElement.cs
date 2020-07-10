// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.Fx;
using Boku.UI2D;
using Boku.Scenes.InGame.Tools;

namespace Boku.UI2D
{
    /// <summary>
    /// A wrapper for the tools held by the toolbox.  This handles the dispaly of the icon and text
    /// while also providing an interface for the update and render calls.
    /// </summary>
    public class UIGrid2DToolElement : UIGrid2DTextureElement
    {
        private BaseTool tool = null;

        private UIGrid parent = null;
        private string description = null;  // The text string to display next to the icon.
        private int descLength = 0;

        private Color textColor = Color.White;
        private Color shadowColor = Color.Black;

        private float iconAlpha = 1.0f;     // Alpha value for rendering icon.
        private float descAlpha = 1.0f;     // Alpha value for rendering description.

        // Texture2D to hold text description string.
        TextLine textLine = null;

        #region Accessors
        public override bool Selected
        {
            get
            {
                return base.Selected;
            }
            set
            {
                if (base.Selected != value)
                {
                    base.Selected = value;

                    if (value)
                    {
                        tool.Active = true;
                    }
                    else
                    {
                        tool.Active = false;
                    }

                }
            }
        }
        public BaseTool Tool
        {
            get { return tool; }
        }
        #endregion

        // c'tor
        public UIGrid2DToolElement(ParamBlob blob, UIGrid parent, BaseTool tool)
            : base(blob, tool.IconTextureName)
        {
            this.tool = tool;
            this.parent = parent;
            this.description = tool.Description;

            NoZ = true;

            
            descLength = (int)(Font().MeasureString(description).X);

            textLine = new TextLine(this, description, Font, false);
        }   // end of c'tor

        /// <summary>
        /// Sets the alpha values used to render the parts of the tool element.
        /// </summary>
        /// <param name="iconAlpha">Alpha value for icon.</param>
        /// <param name="descAlpha">Alpha value for text description.</param>
        public void SetAlpha(float iconAlpha, float descAlpha)
        {
            this.iconAlpha = iconAlpha;
            this.descAlpha = descAlpha;
        }   // end of UIGrid2DToolElement SetAlpha()

        public override void Update(ref Matrix parentMatrix)
        {
            base.Update(ref parentMatrix);

            tool.Update();
        }   // end of UIGrid2DToolElement Update()

        public override void Render(Camera camera)
        {
            // Render the icon.
            base.Alpha = iconAlpha;
            base.Render(camera);

            // Render the text.
            ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();
            // Convert position to screenspace.
            Point pixels = camera.WorldToScreenCoords(Position + parent.WorldMatrix.Translation);
            Vector2 pos = new Vector2(pixels.X, pixels.Y);
            // Calc offset to get text to align correctly with the icon.  Basically this is
            // half the icon size in pixels at 96dpi.
            float offset = base.Size.Y * 0.5f * 96;
            pos.Y -= offset;
            Vector4 color = new Vector4(1.0f, 1.0f, 1.0f, descAlpha);
            quad.Render(textLine.Texture, color, pos, textLine.Size, @"TexturedRegularAlpha");
        }   // end of UIGrid2DToolElement Render()

        public override void LoadContent(bool immediate)
        {
            BokuGame.Load(textLine, immediate);
            base.LoadContent(immediate);
        }   // end of UIGrid2DToolElement LoadContent()

        public override void UnloadContent()
        {
            base.UnloadContent();

            BokuGame.Unload(textLine);
            base.UnloadContent();
        }   // end of UIGrid2DToolElement UnloadContent()

    }   // end of class UIGrid2DToolElement

}   // end of namespace Boku.Ui2d
