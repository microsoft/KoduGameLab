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
    /// Inner child rendering object for hover car.
    /// </summary>
    public class SwimFishSRO : FBXModel
    {
        private static SwimFishSRO sroInstance = null;

        // c'tor
        private SwimFishSRO()
            : base(@"Models\FishNew_0")
        {
            TechniqueExt = "WithFlex";
        }   // end of SwimFishSRO c'tor

        /// <summary>
        /// Returns a static, shareable instance of a hover car sro.
        /// </summary>
        public static SwimFishSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new SwimFishSRO();
                sroInstance.XmlActor = SwimFish.XmlActor;
            }

            return sroInstance;
        }   // end of SwimFishSRO GetInstance()

    }   // end of class SwimFish

}   // end of namespace Boku.SimWorld
