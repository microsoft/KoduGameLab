
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
using Boku.Programming;
using Boku.Common.Xml;

namespace Boku
{
    public class FastBot : GameActor
    {
        //
        //  FastBot
        //
        private static XmlGameActor xmlGameActor = null;

        #region Accessors
        public static XmlGameActor XmlActor
        {
            get
            {
                if (xmlGameActor == null)
                    xmlGameActor = XmlGameActor.Deserialize("FastBot");
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

        public FastBot()
            : base("cycle", new CycleChassis(), FastBotSRO.GetInstance)
        {
            // Create the face.
            face = Face.MakeFace(FastBotSRO.GetInstance, XmlActor);
            face.FaceChange += OnFaceChanged;
            preRender += face.SetupForRender;

            BokuGame.Load(this);

            XmlConstruct();
            InitDefaults();
        }   // end of c'tor

    }   // end of class FastBot
}
