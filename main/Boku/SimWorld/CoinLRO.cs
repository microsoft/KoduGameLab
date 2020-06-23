
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
    /// Inner child rendering object for coin.
    /// </summary>
    public class CoinSRO : FBXModel
    {
        private static CoinSRO sroInstance = null;

        private CoinSRO() 
            : base(@"Models\coin")
        {
        }   

        /// <summary>
        /// Returns a static, shareable instance of a star sro.
        /// </summary>
        public static CoinSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new CoinSRO();
                sroInstance.XmlActor = Coin.XmlActor;
            }
            return sroInstance;
        }   
    } 
} 
