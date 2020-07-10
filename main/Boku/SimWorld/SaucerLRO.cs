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
    public class SaucerSRO : FBXModel
    {
        private static SaucerSRO sroInstance = null;

        // c'tor
        private SaucerSRO() 
            : base(@"Models\saucer")
        {
            TechniqueExt = "WithSkinning";
        }   // end of SaucerSRO c'tor

        /// <summary>
        /// Returns a static, shareable instance of a saucer sro.
        /// </summary>
        public static SaucerSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new SaucerSRO();
                sroInstance.XmlActor = Saucer.XmlActor;
            }

            return sroInstance;
        }   // end of SaucerSRO GetInstance()

        
           
    }   // end of class SaucerSRO

}   // end of namespace Boku.SimWorld
