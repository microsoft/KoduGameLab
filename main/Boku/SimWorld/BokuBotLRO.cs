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
    /// Inner child rendering object for BokuBot.
    /// </summary>
    public class BokuBotSRO : FBXModel
    {
        private static BokuBotSRO sroInstance = null;

        // c'tor
        private BokuBotSRO()
            : base(@"Models\Boku")
        {
//            this.Technique = "NonTexturedColorPassWithSkinning";
            TechniqueExt = "WithSkinning";
//            TechniqueExt = "Face";
        }   // end of BokuBotSRO c'tor


        /// <summary>
        /// Returns a static, shareable instance of a BokuBot sro.
        /// </summary>
        public static BokuBotSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new BokuBotSRO();
                sroInstance.XmlActor = BokuBot.XmlActor;
            }

            return sroInstance;
        }   // end of BokuBotSRO GetInstance()

    }   // end of class BokuBotSRO
}   // end of namespace Boku.SimWorld
