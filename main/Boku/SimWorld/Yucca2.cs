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
    public class Yucca2 : SharedIdle
    {
        #region Members
        private static SharedAnimation classAnim = new SharedAnimation("wind");
        private static XmlGameActor xmlGameActor = null;
        #endregion Members

        #region Accessors
        protected override SharedAnimation SharedAnim
        {
            get { return classAnim; }
        }
        public static XmlGameActor XmlActor
        {
            get
            {
                if (xmlGameActor == null)
                    xmlGameActor = XmlGameActor.Deserialize("Tree_B");
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
        //  Yucca2
        //

        public Yucca2()
            : base("tree", new StaticPropChassis(), Yucca2SRO.GetInstance)
        {
            BokuGame.Load(this);

            XmlConstruct();
            InitDefaults();
        }   // end of Yucca2 c'tor

    }   // end of class Yucca2

}   // end of namespace Boku
