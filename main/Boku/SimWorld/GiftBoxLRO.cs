
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
    /// Inner child rendering object for gift box.
    /// </summary>
    public class GiftBoxSRO : FBXModel
    {
        private static GiftBoxSRO sroInstance = null;

        private GiftBoxSRO()
            : base(@"Models\oak")
        {
        }

        /// <summary>
        /// Returns a static, shareable instance of a gift box sro.
        /// </summary>
        public static GiftBoxSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new GiftBoxSRO();
            }
            return sroInstance;
        }
    }
}
