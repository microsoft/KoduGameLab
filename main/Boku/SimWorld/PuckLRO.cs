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
    /// Inner child rendering object for puck.
    /// </summary>
    public class PuckSRO : FBXModel
    {
        private static PuckSRO sroInstance = null;

        // c'tor
        private PuckSRO()
            : base(@"Models\puck")
        {
        }   // end of PuckSRO c'tor

        /// <summary>
        /// Returns a static, shareable instance of a puck sro.
        /// </summary>
        public static PuckSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new PuckSRO();
                sroInstance.XmlActor = Puck.XmlActor;
            }

            return sroInstance;
        }   // end of PuckSRO GetInstance()



    }   // end of class PuckSRO

}   // end of namespace Boku.SimWorld
