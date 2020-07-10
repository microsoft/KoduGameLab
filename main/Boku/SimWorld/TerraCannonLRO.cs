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
    public class TerraCannonSRO : FBXModel
    {
        private static TerraCannonSRO sroInstance = null;

        // c'tor
        private TerraCannonSRO()
            : base(@"Models\terracannon")
        {
            TechniqueExt = "WithSkinning";
        }   // end of TerraCannonSRO c'tor

        /// <summary>
        /// Returns a static, shareable instance of a hover car sro.
        /// </summary>
        public static TerraCannonSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new TerraCannonSRO();
                sroInstance.XmlActor = TerraCannon.XmlActor;
            }

            return sroInstance;
        }   // end of TerraCannonSRO GetInstance()

    }   // end of class TerraCannonSRO

}   // end of namespace Boku.SimWorld
