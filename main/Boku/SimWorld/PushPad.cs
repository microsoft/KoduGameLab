// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.Common.ParticleSystem;
using Boku.SimWorld;
using Boku.SimWorld.Chassis;
using Boku.SimWorld.Terra;
using Boku.Programming;
using Boku.Common.Xml;

namespace Boku
{
    public partial class PushPad : GameActor
    {
        //
        //  PushPad
        //
        #region Members
        private static XmlGameActor xmlGameActor = null;
        #endregion Members

        #region Accessors
        public static XmlGameActor XmlActor
        {
            get
            {
                if (xmlGameActor == null)
                    xmlGameActor = XmlGameActor.Deserialize("PushPad");
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

        public PushPad()
            : base("pushpad", new HoverChassis(), PushPadSRO.GetInstance)
        {
            BokuGame.Load(this);

            XmlConstruct();
            InitDefaults();
        }   // end of PushPad c'tor

    }   // end of class PushPad

}   // end of namespace Boku


namespace Boku.SimWorld
{
    /// <summary>
    /// Inner child rendering object for hover car.
    /// </summary>
    public class PushPadSRO : FBXModel
    {
        private static PushPadSRO sroInstance = null;

        // c'tor
        private PushPadSRO()
            : base(@"Models\PushPad")
        {
            TechniqueExt = "WithSkinning";
        }   // end of PushPadSRO c'tor

        /// <summary>
        /// Returns a static, shareable instance of a hover car sro.
        /// </summary>
        public static PushPadSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new PushPadSRO();
                sroInstance.XmlActor = PushPad.XmlActor;
            }

            return sroInstance;
        }   // end of PushPadSRO GetInstance()

    }   // end of class PushPadSRO

}   // end of namespace Boku.SimWorld
