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
    /// Inner child rendering object for flower.
    /// </summary>
    public class FlowerSRO : FBXModel
    {
        private static FlowerSRO sroInstance = null;

        private FlowerSRO()
            : base(@"Models\Flower_B")
        {
            TechniqueExt = "WithWind";
        }

        /// <summary>
        /// Returns a static, shareable instance of a star sro.
        /// </summary>
        public static FlowerSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new FlowerSRO();
                sroInstance.XmlActor = Flower.XmlActor;
            }
            return sroInstance;
        }
    }
}
