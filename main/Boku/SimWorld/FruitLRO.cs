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
    /// Inner child rendering object for Fruit.
    /// </summary>
    public class FruitSRO : FBXModel
    {
        private static FruitSRO sroInstance = null;

        // c'tor
        private FruitSRO() 
            : base(@"Models\Fruit")
        {
        }   // end of FruitSRO c'tor

        /// <summary>
        /// Returns a static, shareable instance of a Fruit sro.
        /// </summary>
        public static FruitSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new FruitSRO();
                sroInstance.XmlActor = Fruit.XmlActor;
            }

            return sroInstance;
        }   // end of FruitSRO GetInstance()
           
    }   // end of class FruitSRO

}   // end of namespace Boku.SimWorld
