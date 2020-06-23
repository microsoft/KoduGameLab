
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
    /// Inner child rendering object for WindBlimp.
    /// </summary>
    public class WindBlimpSRO : FBXModel
    {
        private static WindBlimpSRO sroInstance = null;

        // c'tor
        private WindBlimpSRO()
            : base(@"Models\windmachine")
        {
            TechniqueExt = "WithSkinning";
        }   // end of WindBlimpSRO c'tor

        /// <summary>
        /// Returns a static, shareable instance of a WindBlimp sro.
        /// </summary>
        public static WindBlimpSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new WindBlimpSRO();
                sroInstance.XmlActor = WindBlimp.XmlActor;
            }

            return sroInstance;
        }   // end of WindBlimpSRO GetInstance()
    }   // end of class WindBlimpSRO

}   // end of namespace Boku.SimWorld
