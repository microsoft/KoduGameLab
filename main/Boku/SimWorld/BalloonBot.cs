
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
using Boku.SimWorld.Terra;
using Boku.Programming;
using Boku.Common.Xml;

namespace Boku
{
    public class BalloonBot : GameActor
    {
        private static XmlGameActor xmlGameActor = null;
        public static XmlGameActor XmlActor
        {
            get
            {
                if (xmlGameActor == null)
                    xmlGameActor = XmlGameActor.Deserialize("BalloonBot");
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

        //
        //  BalloonBot
        //

        public BalloonBot()
            : base("balloon", new FloatInAirChassis(), BalloonBotSRO.GetInstance)
        {
            // Create the face.
            face = Face.MakeFace(BalloonBotSRO.GetInstance, XmlActor);
            face.FaceChange += OnFaceChanged;
            preRender += face.SetupForRender;

            BokuGame.Load(this);

            XmlConstruct();
            InitDefaults();
        }
    }
}
