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
using Boku.Common.Xml;
using Boku.Common.ParticleSystem;
using Boku.SimWorld;
using Boku.SimWorld.Chassis;
using Boku.Programming;

namespace Boku
{
    public class Hut : GameActor
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
                    xmlGameActor = XmlGameActor.Deserialize("Hut");
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
        //  Hut
        //

        public Hut()
            : base("hut", new StaticPropChassis(), HutSRO.GetInstance)
        {
            BokuGame.Load(this);

            XmlConstruct();
            InitDefaults();
        }   // end of Hut c'tor

    }   // end of class Hut

    /// <summary>
    /// Inner child rendering object for factory.
    /// </summary>
    public class HutSRO : FBXModel
    {
        private static HutSRO sroInstance = null;

        private HutSRO()
            : base(@"Models\hut")
        {
            TechniqueExt = "WithSkinning";
        }

        /// <summary>
        /// Returns a static, shareable instance of a factory sro.
        /// </summary>
        public static HutSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new HutSRO();
                sroInstance.XmlActor = Hut.XmlActor;
            }
            return sroInstance;
        }
    }

}   // end of namespace Boku
