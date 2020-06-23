
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
    public class PineTreeSRO : FBXModel
    {
        private static PineTreeSRO sroInstance = null;

        private PineTreeSRO()
            : base(@"Models\pine")
        {
        }

        /// <summary>
        /// Returns a static, shareable instance of a pine tree sro.
        /// </summary>
        public static PineTreeSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new PineTreeSRO();
            }
            return sroInstance;
        }
    }
}
