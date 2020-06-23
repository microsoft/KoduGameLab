
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
    public class Bullet : GameActor
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
                    xmlGameActor = XmlGameActor.Deserialize("Bullet");
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
        //  Bullet
        //

        public Bullet()
            : base("bullet", new DynamicPropChassis(), BulletSRO.GetInstance)
        {
            BokuGame.Load(this);

            XmlConstruct();
            InitDefaults();
        }   // end of Bullet c'tor

    }   // end of class Bullet

}   // end of namespace Boku

// putting the SRO in this file because I suspect we are going to be able to do away with
// the SRO classes entirely...they're really nothing but a holder for a string. in some cases they are
// referenced directly, but that will have to go away when we have data driven files.
// So, in optimistic anticipation of that day, I'm putting this here so there is one less file
// to delete later.
namespace Boku.SimWorld
{
    /// <summary>
    /// Inner child rendering object for bullet.
    /// </summary>
    public class BulletSRO : FBXModel
    {
        private static BulletSRO sroInstance = null;

        private BulletSRO()
            : base(@"Models\missile-02")
        {
        }

        /// <summary>
        /// Returns a static, shareable instance.
        /// </summary>
        public static BulletSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new BulletSRO();
                sroInstance.XmlActor = Bullet.XmlActor;
            }
            return sroInstance;
        }
    }
}
