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
    /// Inner child rendering object for factory.
    /// </summary>
    public class FactorySRO : FBXModel
    {
        private static FactorySRO sroInstance = null;

        private FactorySRO()
            : base(@"Models\factory")
        {
            TechniqueExt = "WithSkinning";
        }

        /// <summary>
        /// Returns a static, shareable instance of a factory sro.
        /// </summary>
        public static FactorySRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new FactorySRO();
                sroInstance.XmlActor = Factory.XmlActor;
            }
            return sroInstance;
        }
    }
}
