
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
    /// Inner child rendering object for Cloud.
    /// </summary>
    public class CloudSRO : FBXModel
    {
        private static CloudSRO sroInstance = null;

        // c'tor
        private CloudSRO()
            : base(@"Models\cloud")
        {
            TechniqueExt = "Cloud";
            Shininess = 0.2f;
        }   // end of CloudSRO c'tor

        /// <summary>
        /// Returns a static, shareable instance of a Cloud sro.
        /// </summary>
        public static CloudSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new CloudSRO();
                sroInstance.XmlActor = Cloud.XmlActor;
            }

            return sroInstance;
        }   // end of CloudSRO GetInstance()
    }   // end of class CloudSRO

}   // end of namespace Boku.SimWorld
