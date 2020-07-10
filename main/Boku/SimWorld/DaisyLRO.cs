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
    /// Inner child rendering object for daisy.
    /// </summary>
    public class DaisySRO : FBXModel
    {
        private static DaisySRO sroInstance = null;

        private DaisySRO()
            : base(@"Models\Flower_D")
        {
            TechniqueExt = "WithWind";
        }

        /// <summary>
        /// Returns a static, shareable instance of a daisy sro.
        /// </summary>
        public static DaisySRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new DaisySRO();
                sroInstance.XmlActor = Daisy.XmlActor;
            }
            return sroInstance;
        }
    }
}
