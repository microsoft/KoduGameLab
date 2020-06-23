
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
    /// Inner child rendering object for star.
    /// </summary>
    public class StarSRO : FBXModel
    {
        private static StarSRO sroInstance = null;

        private StarSRO()
            : base(@"Models\star")
        {
        }

        /// <summary>
        /// Returns a static, shareable instance of a star sro.
        /// </summary>
        public static StarSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new StarSRO();
                sroInstance.XmlActor = Star.XmlActor;
            }
            return sroInstance;
        }
    } 
}
