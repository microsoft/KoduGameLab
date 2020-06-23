
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
    public class HoverCarSRO : FBXModel
    {
        private static HoverCarSRO sroInstance = null;

        // c'tor
        private HoverCarSRO()
            : base(@"Models\fishFinal1_1")
        {
        }   // end of HoverCarSRO c'tor

        /// <summary>
        /// Returns a static, shareable instance of a hover car sro.
        /// </summary>
        public static HoverCarSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new HoverCarSRO();
                sroInstance.XmlActor = HoverCar.XmlActor;
            }

            return sroInstance;
        }   // end of HoverCarSRO GetInstance()

    }   // end of class HoverCarSRO

}   // end of namespace Boku.SimWorld
