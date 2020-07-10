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
    /// Inner child rendering object for palm1.
    /// </summary>
    public class Palm1SRO : FBXModel
    {
        private static Palm1SRO sroInstance = null;

        // c'tor
        private Palm1SRO() 
            : base(@"Models\palm 1")
        {
            TechniqueExt = "Foliage";
        }   // end of Palm1SRO c'tor

        /// <summary>
        /// Returns a static, shareable instance of a palm1 sro.
        /// </summary>
        public static Palm1SRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new Palm1SRO();
            }

            return sroInstance;
        }   // end of Palm1SRO GetInstance()

        
        
           
    }   // end of class Palm1SRO

}   // end of namespace Boku.SimWorld
