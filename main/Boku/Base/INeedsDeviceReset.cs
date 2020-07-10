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

namespace Boku.Base
{
    /// <summary>
    /// Interface called by the ContentLoader to both load content from disk
    /// and allocate graphics device resources. You should never call this
    /// API directly, not even when passing to children. See documentation
    /// below for more information.
    /// </summary>
    public interface INeedsDeviceReset
    {
        /// <summary>
        /// Load content from disk.
        /// 
        /// Never call this function directly, not even for child objects.
        /// NOTE: It IS NOT SAFE to access the graphics device from within
        /// this function.
        ///
        /// To pass this call to children, call: 
        /// 
        ///     // You must pass the 'immediate' parameter unchanged.
        ///     BokuGame.Load(childObj, immediate);
        /// 
        /// </summary>
        void LoadContent(bool immediate);

        /// <summary>
        /// Allocate device resources.
        /// 
        /// Never call this function directly, not even for child objects.
        /// NOTE: It IS SAFE to access the graphics device from within this
        /// function.
        /// 
        /// No need to do anything special to pass this call to children.
        /// The ContentLoader will automatically call this method on the
        /// child objects you queued for load in LoadContent.
        /// 
        /// </summary>
        /// <param name="graphics"></param>
        void InitDeviceResources(GraphicsDevice device);

        /// <summary>
        /// Release assets and graphics device resources.
        /// 
        /// To unload a child object, call:
        /// 
        ///     BokuGame.Unload(childObj);
        /// 
        /// To release an asset or device state, call:
        /// 
        ///     // Or you could just set it to null.
        ///     BokuGame.Release(ref asset);
        /// 
        /// </summary>
        void UnloadContent();

        /// <summary>
        /// Recreate things that get lost upon device reset, typically render targets.
        /// You must manually pass this call to children where appropriate.
        /// </summary>
        void DeviceReset(GraphicsDevice device);

    }   // end of class INeedsDeviceReset

}   // end of namespace Boku.Base
