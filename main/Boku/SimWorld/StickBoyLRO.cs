
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
    /// Inner child rendering object for StickBoy.
    /// </summary>
    public class StickBoySRO : FBXModel
    {
        private static StickBoySRO sroInstance = null;

        // c'tor
        private StickBoySRO() 
            : base(@"Models\stick_boy")
        {
            TechniqueExt = "WithSkinning";
        }   // end of StickBoySRO c'tor

        /// <summary>
        /// Returns a static, shareable instance of a StickBoy sro.
        /// </summary>
        public static StickBoySRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new StickBoySRO();
                sroInstance.XmlActor = StickBoy.XmlActor;
            }

            return sroInstance;
        }   // end of StickBoySRO GetInstance()

    }   // end of class StickBoySRO

}   // end of namespace Boku.SimWorld
