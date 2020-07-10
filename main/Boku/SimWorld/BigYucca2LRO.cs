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
    /// Inner child rendering object for big yucca 2.
    /// </summary>
    public class BigYucca2SRO : FBXModel
    {
        private static BigYucca2SRO sroInstance = null;

        // c'tor
        private BigYucca2SRO() 
            : base(@"Models\big_yucca_2")
        {
            TechniqueExt = "Foliage";
        }   // end of BigYucca2SRO c'tor

        /// <summary>
        /// Returns a static, shareable instance of a big yucca 2 sro.
        /// </summary>
        public static BigYucca2SRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new BigYucca2SRO();
            }

            return sroInstance;
        }   // end of BigYucca2SRO GetInstance()
    }   // end of class BigYucca2SRO

}   // end of namespace Boku.SimWorld
