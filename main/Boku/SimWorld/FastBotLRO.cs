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
    /// Inner child rendering object for fastbot.
    /// </summary>
    public class FastBotSRO : FBXModel
    {
        private static FastBotSRO sroInstance = null;

        // c'tor
        private FastBotSRO()
            : base(@"Models\fastbot")
        {
            TechniqueExt = "WithSkinning";
        }   // end of FastBotSRO c'tor

        /// <summary>
        /// Returns the single, shareable instance.
        /// </summary>
        public static FastBotSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new FastBotSRO();
                sroInstance.XmlActor = FastBot.XmlActor;
            }

            return sroInstance;
        }   // end of FastBotSRO GetInstance()

    }   // end of class FastBotSRO
}   // end of namespace Boku.SimWorld
