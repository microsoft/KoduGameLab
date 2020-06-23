
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
    public class Yucca3 : SharedIdle
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
                    xmlGameActor = XmlGameActor.Deserialize("Tree_C");
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
        //  Yucca3
        //

        #region Public
        public Yucca3()
            : base("tree", new StaticPropChassis(), Yucca3SRO.GetInstance)
        {
            BokuGame.Load(this);

            XmlConstruct();
            InitDefaults();
        }   // end of Yucca3 c'tor

        #endregion Public

    }   // end of class Yucca3

}   // end of namespace Boku
