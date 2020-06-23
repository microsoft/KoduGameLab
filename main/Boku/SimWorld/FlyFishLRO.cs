
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
    public class FlyFishSRO : FBXModel
    {
        private static FlyFishSRO sroInstance = null;

        // c'tor
        private FlyFishSRO()
            : base(@"Models\puffer")
        {
            TechniqueExt = "WithSkinning";
        }   // end of FlyFishSRO c'tor

        /// <summary>
        /// Returns a static, shareable instance of a hover car sro.
        /// </summary>
        public static FlyFishSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new FlyFishSRO();
                sroInstance.XmlActor = FlyFish.XmlActor;
            }

            return sroInstance;
        }   // end of FlyFishSRO GetInstance()

    }   // end of class FlyFishSRO

}   // end of namespace Boku.SimWorld
