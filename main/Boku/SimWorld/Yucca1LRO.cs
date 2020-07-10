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
    /// Inner child rendering object for yucca1.
    /// </summary>
    public class Yucca1SRO : FBXModel
    {
        private static Yucca1SRO sroInstance = null;

        // c'tor
        private Yucca1SRO() 
            : base(@"Models\Tree_A")
        {
            TechniqueExt = "WithWind";
        }   // end of Yucca1SRO c'tor

        /// <summary>
        /// Returns a static, shareable instance of a yucca1 sro.
        /// </summary>
        public static Yucca1SRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new Yucca1SRO();
                sroInstance.XmlActor = Yucca1.XmlActor;
            }

            return sroInstance;
        }   // end of Yucca1SRO GetInstance()
           
    }   // end of class Yucca1SRO

}   // end of namespace Boku.SimWorld
