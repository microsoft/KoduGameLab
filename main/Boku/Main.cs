// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Storage;

namespace Boku
{
    /// <summary>
    /// Something to wrap around everything.
    /// </summary>
    public class Main
    {
        #region Members
        #endregion

        #region Accessors
        #endregion

        #region Public

        /// <summary>
        /// c'tor
        /// </summary>
        public Main()
        {
        }   // end of c'tor

        public void Update()
        {
        }   // end of Update()

        public void Render()
        {
            GraphicsDevice device = XNAControl.Device;

            device.Clear(Color.DarkSlateGray);
        }   // end of Render

        #endregion

        #region Internal

        #endregion
    }   // end of class Main
}   // end of namespace Boku
