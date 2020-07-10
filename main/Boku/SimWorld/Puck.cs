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
using Boku.Common.ParticleSystem;
using Boku.SimWorld;
using Boku.SimWorld.Chassis;
using Boku.SimWorld.Terra;
using Boku.Programming;
using Boku.Common.Xml;

namespace Boku
{
    public class Puck : GameActor
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
                    xmlGameActor = XmlGameActor.Deserialize("Puck");
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
        #endregion

        //
        //  Puck
        //
        #region Public
        public Puck()
            : base("puck", new PuckChassis(), PuckSRO.GetInstance)
        {
            BokuGame.Load(this);

            XmlConstruct();
            InitDefaults();
        }   // end of Puck c'tor

        #endregion Public

    }   // end of class Puck

}   // end of namespace Boku
