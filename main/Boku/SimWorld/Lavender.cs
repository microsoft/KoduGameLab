
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
using Boku.Programming;
using Boku.Common.Xml;

namespace Boku
{
    public class Lavender : SharedIdle
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
                    xmlGameActor = XmlGameActor.Deserialize("Flower_C");
                return xmlGameActor;
            }
        }
        protected override SharedAnimation SharedAnim
        {
            get { return classAnim; }
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
        //  Lavender
        //

        public Lavender()
            : base("flower", new DynamicPropChassis(), LavenderSRO.GetInstance)
        {
            BokuGame.Load(this);

            XmlConstruct();
            InitDefaults();
        }   // end of Lavender c'tor

    }   // end of class Lavender

}   // end of namespace Boku
