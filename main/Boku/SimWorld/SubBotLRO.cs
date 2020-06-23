
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
    /// Inner child rendering object for hover car.
    /// </summary>
    public class SubBotSRO : FBXModel
    {
        private static SubBotSRO sroInstance = null;

        // c'tor
        private SubBotSRO()
            : base(@"Models\sub_bot")
        {
            TechniqueExt = "WithSkinning";
        }   // end of SubBotSRO c'tor

        /// <summary>
        /// Returns a static, shareable instance of a hover car sro.
        /// </summary>
        public static SubBotSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new SubBotSRO();
                sroInstance.XmlActor = SubBot.XmlActor;
            }

            return sroInstance;
        }   // end of SubBotSRO GetInstance()


    }   // end of class SubBotSRO

}   // end of namespace Boku.SimWorld
