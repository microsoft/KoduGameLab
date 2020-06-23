
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
    /// Inner child rendering object for pine tree.
    /// </summary>
    public class SmallStoneBuildingSRO : FBXModel
    {
        private static SmallStoneBuildingSRO sroInstance = null;

        private SmallStoneBuildingSRO()
            : base(@"Models\oak")
        {
        }

        /// <summary>
        /// Returns a static, shareable instance of a pine tree sro.
        /// </summary>
        public static SmallStoneBuildingSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new SmallStoneBuildingSRO();
            }
            return sroInstance;
        }
    }
}
