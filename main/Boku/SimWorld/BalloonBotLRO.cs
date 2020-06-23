
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
    public class BalloonBotSRO : FBXModel
    {
        private static BalloonBotSRO sroInstance = null;

        // c'tor
        private BalloonBotSRO()
            : base(@"Models\balloon")
        {
            TechniqueExt = "WithSkinning";
        }   // end of BalloonBotSRO c'tor

        /// <summary>
        /// Returns a static, shareable instance of a hover car sro.
        /// </summary>
        public static BalloonBotSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new BalloonBotSRO();
                sroInstance.XmlActor = BalloonBot.XmlActor;
            }

            return sroInstance;
        }   // end of BalloonBotSRO GetInstance()

    }   // end of class BalloonBotSRO

}   // end of namespace Boku.SimWorld
