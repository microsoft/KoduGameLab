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

using KoiX;
using KoiX.Text;

using Boku.Common;

namespace Boku.UI2D
{
    /// <summary>
    /// This is a text element which has no background and is rendered 
    /// directly over whatever is under it.  For the selected selected 
    /// state the text is rendered in BOLD with a blurred drop shadow 
    /// under it.
    /// </summary>
    public class UIGrid2DFloatingTextElement : UIGridElement
    {
        #region Members

        private Color unselectedTextColor;
        private Color selectedTextColor;
        private Color dropShadowColor;

        private string label = null;
        private bool selected = false;
        private float width;
        private float height;


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
                    // TODO (****) twitch to change text color?
                }
            }
        }

        /// <summary>
        /// The string associated with this grid element.
        /// </summary>
        public override string Label
        {
            get { return label; }
        }

        public override Vector2 Size
        {
            get { return new Vector2(width, height); }
            set { /* do nothing, should be removed from base class */ }
        }

        public int WidthOfLabelInPixels
        {
            get { return (int)Font().MeasureString(label).X; }
        }

        #endregion

        #region Public

        public UIGrid2DFloatingTextElement(ParamBlob blob, string label)
        {
            this.label = TextHelper.FilterInvalidCharacters(label);

            // blob
            this.Font = blob.Font;
            this.selectedTextColor = blob.selectedColor;
            this.unselectedTextColor = blob.unselectedColor;
            this.dropShadowColor = blob.dropShadowColor;

            
            width = 1.0f;
            height = Font().LineSpacing / 96.0f;

        }   // end of c'tor

        public override void HandleMouseInput(Vector2 hitUV)
        {
        }   // end of HandleMouseInput()


        public override void HandleTouchInput(TouchContact touch, Vector2 hitUV)
        {
        }  // end of HandleTouchInput()

        public override void Render(Camera camera)
        {
            SpriteBatch batch = KoiLibrary.SpriteBatch;
            batch.Begin();

            // Calc position in pixel coords.
            Point pos = camera.WorldToScreenCoords(Vector3.Transform(Position, worldMatrix));

            TextHelper.DrawString(Font, label, new Vector2(pos.X, pos.Y), selected ? selectedTextColor : unselectedTextColor);

            batch.End();
        }   // end of Render()

        /// <summary>
        /// Render the text label at the given position.
        /// </summary>
        /// <param name="position">In pixel coordinates.</param>
        public void RenderAt(SpriteBatch batch, Vector2 position)
        {
            if (selected)
            {
                TextHelper.DrawString(Font, label, position + Vector2.One, dropShadowColor);
                TextHelper.DrawString(Font, label, position, selectedTextColor);
            }
            else
            {
                TextHelper.DrawString(Font, label, position, unselectedTextColor);
            }
        }   // end of RenderAt()

        #endregion

        #region Internal

        public override void LoadContent(bool immediate)
        {
        }   // end of LoadContent()

        public override void InitDeviceResources(GraphicsDevice device)
        {
        }

        public override void UnloadContent()
        {
            base.UnloadContent();
        }   // end of UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public override void DeviceReset(GraphicsDevice device)
        {
        }

        #endregion

    }   // end of class UIGrid2DFloatingTextElement

}   // end of namespace Boku.UI2D
