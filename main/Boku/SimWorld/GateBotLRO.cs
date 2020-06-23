
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

using Xclna.Xna.Animation;

namespace Boku.SimWorld
{
    /// <summary>
    /// Inner child rendering object for GateBot.
    /// </summary>
    public class GateBotSRO : FBXModel
    {
        private static GateBotSRO sroInstance = null;

        // c'tor
        private GateBotSRO() 
            : base(@"Models\gate")
        {
        }   // end of GateBotSRO c'tor

        /// <summary>
        /// Returns a static, shareable instance of a GateBot sro.
        /// </summary>
        public static GateBotSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new GateBotSRO();
                sroInstance.XmlActor = GateBot.XmlActor;
            }

            return sroInstance;
        }   // end of GateBotSRO GetInstance()

    }   // end of class GateBotSRO

}   // end of namespace Boku.SimWorld
