
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
    /// Inner child rendering object for yucca2.
    /// </summary>
    public class Yucca2SRO : FBXModel
    {
        private static Yucca2SRO sroInstance = null;

        // c'tor
        private Yucca2SRO() 
            : base(@"Models\Tree_B")
        {
            TechniqueExt = "WithWind";
        }   // end of Yucca2SRO c'tor

        /// <summary>
        /// Returns a static, shareable instance of a yucca2 sro.
        /// </summary>
        public static Yucca2SRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new Yucca2SRO();
                sroInstance.XmlActor = Yucca2.XmlActor;
            }

            return sroInstance;
        }   // end of Yucca2SRO GetInstance()

           
    }   // end of class Yucca2SRO

}   // end of namespace Boku.SimWorld
