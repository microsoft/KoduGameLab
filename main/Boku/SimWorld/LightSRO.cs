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
    /// Inner child rendering object for light.
    /// </summary>
    public class LightSRO : FBXModel
    {
        private static LightSRO sroInstance = null;

        // c'tor
        private LightSRO()
            : base(@"Models\wisp")
        {
        }   // end of WispSRO c'tor

        /// <summary>
        /// Returns a static, shareable instance of a light sro.
        /// </summary>
        public static LightSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new LightSRO();
                sroInstance.XmlActor = Light.XmlActor;
            }

            return sroInstance;
        }   // end of LightSRO GetInstance()



    }   // end of class LightSRO

}   // end of namespace Boku.SimWorld
