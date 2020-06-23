
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
    public class Yucca1 : SharedIdle
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
                    xmlGameActor = XmlGameActor.Deserialize("Tree_A");
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
        //  Yucca1
        //
        public Yucca1()
            : base("tree", new StaticPropChassis(), Yucca1SRO.GetInstance)
        {
            BokuGame.Load(this);

            XmlConstruct();
            InitDefaults();
        }   // end of Yucca1 c'tor

    }   // end of class Yucca1

}   // end of namespace Boku
