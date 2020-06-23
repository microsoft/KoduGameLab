
using System;
using System.Collections;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Common;

namespace Boku.UI2D
{
    /// <summary>
    /// UI Grid element used for 2d terrain editing brushes.  Basically it's just a 
    /// wrapper around the standard 2d texture element adding the brush index.
    /// </summary>
    public class UIGrid2DBrushElement : UIGrid2DTextureElement
    {
        private int brushIndex = 0;

        #region Accessors
        /// <summary>
        /// This is the global index (the one matching the index in the
        /// brush manager) of the brush represented by this element.
        /// </summary>
        public int BrushIndex
        {
            get { return brushIndex; }
        }
        #endregion

        /// <summary>
        /// C'tor
        /// </summary>
        /// <param name="blob">Standard paramters for element.</param>
        /// <param name="diffuseTextureName">Resource name for brush texture.</param>
        /// <param name="brushIndex">Index for brush.</param>
        public UIGrid2DBrushElement(ParamBlob blob, string diffuseTextureName, int brushIndex)
            : base(blob, diffuseTextureName)
        {
            this.brushIndex = brushIndex;
        }   // end of UIGrid2DBrushElement c'tor

        /// <summary>
        /// C'tor
        /// </summary>
        /// <param name="blob">Standard paramters for element.</param>
        /// <param name="diffuseTextureName">Resource name for brush texture.</param>
        /// <param name="overlayTextureName">Resource name for overlay texture.</param>
        /// <param name="brushIndex">Index for brush.</param>
        public UIGrid2DBrushElement(ParamBlob blob, string diffuseTextureName, string overlayTextureName, int brushIndex)
            : base(blob, diffuseTextureName, overlayTextureName)
        {
            this.brushIndex = brushIndex;
        }   // end of UIGrid2DBrushElement c'tor

    }   // end of class UIGrid2DBrushElement

}   // end of namespace Boku.UI2D
