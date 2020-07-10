// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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
    public partial class Turtle : GameActor
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
                    xmlGameActor = XmlGameActor.Deserialize("Turtle");
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
        //  Turtle
        //

        public Turtle()
            : base("turtle", new HoverChassis(), TurtleSRO.GetInstance)
        {
            // Create the face.
            face = Face.MakeFace(TurtleSRO.GetInstance, XmlActor);
            face.FaceChange += OnFaceChanged;
            preRender += face.SetupForRender;

            BokuGame.Load(this);

            XmlConstruct();
            InitDefaults();
        }   // end of Turtle c'tor

    }   // end of class Turtle

}   // end of namespace Boku


namespace Boku.SimWorld
{
    /// <summary>
    /// Inner child rendering object for hover car.
    /// </summary>
    public class TurtleSRO : FBXModel
    {
        private static TurtleSRO sroInstance = null;

        // c'tor
        private TurtleSRO()
            : base(@"Models\Turtle")
        {
            TechniqueExt = "WithSkinning";
        }   // end of TurtleSRO c'tor

        /// <summary>
        /// Returns a static, shareable instance of a hover car sro.
        /// </summary>
        public static TurtleSRO GetInstance()
        {
            if (sroInstance == null)
            {
                sroInstance = new TurtleSRO();
                sroInstance.XmlActor = Turtle.XmlActor;
            }

            return sroInstance;
        }   // end of TurtleSRO GetInstance()

    }   // end of class TurtleSRO

}   // end of namespace Boku.SimWorld
