// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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
using Boku.SimWorld;
using Boku.SimWorld.Chassis;
using Boku.SimWorld.Terra;
using Boku.Programming;
using Boku.Common.Xml;

namespace Boku
{
    public class Sputnik : GameActor
    {
        #region Members
        private static XmlGameActor xmlGameActor = null;
        #endregion Members

        #region Accessors
        public static XmlGameActor XmlActor
        {
            get
            {
                if (xmlGameActor == null)
                    xmlGameActor = XmlGameActor.Deserialize("Sputnik");
                return xmlGameActor;
            }
        }
        /// <summary>
        /// Return the shared paramaterization for this type of actor.
        /// </summary>
        public override XmlGameActor XmlActorParams
        {
            get { return XmlActor; }
        }
        #endregion Accessors
        //
        //  Sputnik
        //

        public Sputnik()
            : base("sputnik", new HoverChassis(), SputnikSRO.GetInstance)
        {
            BokuGame.Load(this);

            XmlConstruct();
            InitDefaults();
        }
    }
}


namespace Boku.SimWorld
{
    /// <summary>
    /// Inner child rendering object for hover car.
    /// </summary>
    public class SputnikSRO : FBXModel
    {
        private static SputnikSRO sroInstance = null;

        // c'tor
        private SputnikSRO()
            : base(@"Models\satellite")
        {
            TechniqueExt = "WithSkinning";
        }   // end of SputnikSRO c'tor

        /// <summary>
        /// Returns a static, shareable instance of a hover car sro.
        /// </summary>
        public static SputnikSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new SputnikSRO();
                sroInstance.XmlActor = Sputnik.XmlActor;
            }

            return sroInstance;
        }   // end of SputnikSRO GetInstance()

    }   // end of class SputnikSRO

}   // end of namespace Boku.SimWorld
