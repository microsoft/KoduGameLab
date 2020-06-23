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
    public partial class Mine : GameActor
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
                    xmlGameActor = XmlGameActor.Deserialize("Mine");
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
        //  Mine
        //
        public Mine()
            : base("mine", new HoverChassis(), MineSRO.GetInstance)
        {
            BokuGame.Load(this);

            XmlConstruct();
            InitDefaults();
        }   // end of Mine c'tor

    }   // end of class Mine

}   // end of namespace Boku


namespace Boku.SimWorld
{
    /// <summary>
    /// Inner child rendering object for hover car.
    /// </summary>
    public class MineSRO : FBXModel
    {
        private static MineSRO sroInstance = null;

        // c'tor
        private MineSRO()
            : base(@"Models\mine")
        {
            TechniqueExt = "WithSkinning";
        }   // end of MineSRO c'tor

        /// <summary>
        /// Returns a static, shareable instance of a hover car sro.
        /// </summary>
        public static MineSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new MineSRO();
                sroInstance.XmlActor = Mine.XmlActor;
            }

            return sroInstance;
        }   // end of MineSRO GetInstance()

    }   // end of class MineSRO

}   // end of namespace Boku.SimWorld
