
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
    public class PineConeSRO : FBXModel
    {
        private static PineConeSRO sroInstance = null;

        private PineConeSRO()
            : base(@"Models\pinecone")
        {
        }

        /// <summary>
        /// Returns a static, shareable instance of a star sro.
        /// </summary>
        public static PineConeSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new PineConeSRO();
            }
            return sroInstance;
        }
    }
}
