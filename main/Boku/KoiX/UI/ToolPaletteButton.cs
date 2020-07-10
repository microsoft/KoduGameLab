// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;
using KoiX.Geometry;
using KoiX.Input;

using Boku.Input;

namespace KoiX.UI
{
    public class ToolPaletteButton : GraphicButton
    {
        #region Members
        #endregion

        #region Accessors
        #endregion

        #region Public

        public ToolPaletteButton(BaseDialog parentDialog, RectangleF rect, string textureName, Callback onSelect)
            : base(parentDialog: parentDialog, rect: rect, textureName: textureName, onSelect: onSelect)
        {
            this.localRect = rect;


        }   // end of c'tor

        #endregion

        #region Internal
        #endregion

    }   // end of class ToolPaletteButton

}   // end of namespace KoiX.UI
