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
    /// Inner child rendering object for castle.
    /// </summary>
    public class CastleSRO : FBXModel
    {
        private static CastleSRO sroInstance = null;

        private CastleSRO()
            : base(@"Models\castle_tower")
        {
        }

        /// <summary>
        /// Returns a static, shareable instance of a castle sro.
        /// </summary>
        public static CastleSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new CastleSRO();
                sroInstance.XmlActor = Castle.XmlActor;
            }
            return sroInstance;
        }
    }
}
