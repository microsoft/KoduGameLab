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
using Boku.Common.Xml;

namespace Boku
{
    public class Daisy : SharedIdle
    {
        #region Members
        private static SharedAnimation classAnim = new SharedAnimation("wind");
        private static XmlGameActor xmlGameActor = null;
        #endregion Members

        #region Accessors
        public static XmlGameActor XmlActor
        {
            get
            {
                if (xmlGameActor == null)
                    xmlGameActor = XmlGameActor.Deserialize("Flower_D");
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
        protected override SharedAnimation SharedAnim
        {
            get { return classAnim; }
        }
        #endregion Accessors

        //
        //  Daisy
        //

        public Daisy()
            : base("flower", new DynamicPropChassis(), DaisySRO.GetInstance)
        {
            BokuGame.Load(this);

            XmlConstruct();
            InitDefaults();
        }   // end of Daisy c'tor

    }   // end of class Daisy

}   // end of namespace Boku
