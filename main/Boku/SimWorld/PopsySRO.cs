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
    /// Inner child rendering object for popsy.
    /// </summary>
    public class PopsySRO : FBXModel
    {
        private static PopsySRO sroInstance = null;

        private PopsySRO()
            : base(@"Models\Flower_A")
        {
            TechniqueExt = "WithWind";
        }

        /// <summary>
        /// Returns a static, shareable instance of a popsy sro.
        /// </summary>
        public static PopsySRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new PopsySRO();
                sroInstance.XmlActor = Popsy.XmlActor;
            }
            return sroInstance;
        }
    }
}
