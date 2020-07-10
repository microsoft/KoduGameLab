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
using Boku.SimWorld.Terra;
using Boku.Programming;
using Boku.Common.Xml;

namespace Boku
{
    public class SubBot : GameActor
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
                    xmlGameActor = XmlGameActor.Deserialize("SubBot");
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
        //  SubBot
        //
        public enum MovementModes
        {
            WaterSurface, // surfaced, keep periscope above water
            Water,        // move about in 3d water, brains don't support well due to continued hacks
            WaterFloor,   // keep to the floor of the water
        }

        private MovementModes movementMode = MovementModes.Water;

        #region Accessors
        public MovementModes MovementMode
        {
            get
            {
                return movementMode;
            }
            set
            {
                this.movementMode = value;
            }
        }
        #endregion

        public SubBot()
            : base("sub", new SwimChassis(), SubBotSRO.GetInstance)
        {
            // Create the face.
            face = Face.MakeFace(SubBotSRO.GetInstance, XmlActor);
            face.FaceChange += OnFaceChanged;
            preRender += face.SetupForRender;

            BokuGame.Load(this);

            XmlConstruct();
            InitDefaults();
        }
    }
}
