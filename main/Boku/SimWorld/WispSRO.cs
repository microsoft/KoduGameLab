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

namespace Boku.SimWorld
{
    /// <summary>
    /// Inner child rendering object for saucer.
    /// </summary>
    public class WispSRO : FBXModel
    {
        private static WispSRO sroInstance = null;

        // c'tor
        private WispSRO()
            : base(@"Models\wisp")
        {
        }   // end of SaucerSRO c'tor

        /// <summary>
        /// Returns a static, shareable instance of a saucer sro.
        /// </summary>
        public static WispSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new WispSRO();
                sroInstance.XmlActor = Wisp.XmlActor;
            }

            return sroInstance;
        }   // end of SaucerSRO GetInstance()



    }   // end of class SaucerSRO

}   // end of namespace Boku.SimWorld
