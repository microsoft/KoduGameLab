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
    /// Inner child rendering object for CruiseMissile.
    /// </summary>
    public class CruiseMissileSRO : FBXModel
    {
        private static CruiseMissileSRO sroInstance = null;


        // c'tor
        private CruiseMissileSRO()
            : base(@"Models\missile-02")
        {
        }   // end of CruiseMissileSRO c'tor

        /// <summary>
        /// Returns a static, shareable instance of a CruiseMissile sro.
        /// </summary>
        public static CruiseMissileSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new CruiseMissileSRO();
                sroInstance.XmlActor = CruiseMissile.XmlActor;
            }

            return sroInstance;
        }   // end of CruiseMissileSRO GetInstance()


    }   // end of class CruiseMissileSRO

}   // end of namespace Boku.SimWorld
