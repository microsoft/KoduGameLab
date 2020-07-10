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
    /// Inner child rendering object for big yucca 1.
    /// </summary>
    public class BigYucca1SRO : FBXModel
    {
        private static BigYucca1SRO sroInstance = null;

        // c'tor
        private BigYucca1SRO() 
            : base(@"Models\Tree_D")
        {
            TechniqueExt = "WithWind";
        }   // end of BigYucca1SRO c'tor

        /// <summary>
        /// Returns a static, shareable instance of a big yucca 1 sro.
        /// </summary>
        public static BigYucca1SRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new BigYucca1SRO();
                sroInstance.XmlActor = BigYucca1.XmlActor;
            }

            return sroInstance;
        }   // end of BigYucca1SRO GetInstance()

           
    }   // end of class BigYucca1SRO

}   // end of namespace Boku.SimWorld
