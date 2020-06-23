
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
    /// Inner child rendering object for lavender.
    /// </summary>
    public class LavenderSRO : FBXModel
    {
        private static LavenderSRO sroInstance = null;

        private LavenderSRO()
            : base(@"Models\Flower_C")
        {
            TechniqueExt = "WithWind";
        }

        /// <summary>
        /// Returns a static, shareable instance of a star sro.
        /// </summary>
        public static LavenderSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new LavenderSRO();
                sroInstance.XmlActor = Lavender.XmlActor;
            }
            return sroInstance;
        }
    }
}
