
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
    /// Inner child rendering object for yucca3.
    /// </summary>
    public class Yucca3SRO : FBXModel
    {
        private static Yucca3SRO sroInstance = null;

        // c'tor
        private Yucca3SRO() 
            : base(@"Models\Tree_C")
        {
            TechniqueExt = "WithWind";
        }   // end of Yucca3SRO c'tor

        /// <summary>
        /// Returns a static, shareable instance of a yucca3 sro.
        /// </summary>
        public static Yucca3SRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new Yucca3SRO();
                sroInstance.XmlActor = Yucca3.XmlActor;
            }

            return sroInstance;
        }   // end of Yucca3SRO GetInstance()

           
    }   // end of class Yucca3SRO

}   // end of namespace Boku.SimWorld
