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

namespace KoiX
{
    public interface IDeviceResetX
    {
        /// <summary>
        /// Called to tell an object to load any device dependent parts of itself.
        /// This call should be passed on to any children.
        /// For static singleton objects "abstract" and "static" don't play well 
        /// together.  The solution is just to implement these methods but NOT to
        /// derive from this class.
        /// </summary>
        void LoadContent();

        /// <summary>
        /// Called to tell an object to remove/delete any device dependent parts.
        /// </summary>
        void UnloadContent();

        /// <summary>
        /// Called on device reset.  Object should re-allocate / rebuild any device dependent content.
        /// If an object wants this it should register for this.
        /// </summary>
        void DeviceResetHandler(object sender, EventArgs e);


    }   // end of interface IDeviceReset
}   // end of namespace Koi
