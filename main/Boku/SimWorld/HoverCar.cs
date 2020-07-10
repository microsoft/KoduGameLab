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
using Boku.Programming;
using Boku.Common.Xml;

namespace Boku
{
    public class HoverCar : GameActor
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
                    xmlGameActor = XmlGameActor.Deserialize("HoverCar");
                return xmlGameActor;
            }
        }
        #endregion Accessors
        //
        //  HoverCar
        //

        public HoverCar()
            : base()
        {
            this.classification = new Classification("fish",
                    Classification.Colors.White,
                    Classification.Shapes.Tube,
                    Classification.Tastes.Salty,
                    Classification.Smells.Pleasant,
                    Classification.Physicalities.Collectable);
            this.classification.audioImpression = Classification.AudioImpression.Noisy;
            this.classification.audioVolume = Classification.AudioVolume.Loud;
            this.classification.emitter = ExpressModifier.Emitters.None;
            this.classification.expression = Face.FaceState.None;

            InitGameActorParams();

            renderObj = new RenderObj(this, HoverCarSRO.GetInstance);

            BokuGame.Load(this);

            // Set holding position after loading model so that BoundingSphere is initialized.
            this.holdingPosition = new Vector3(1.5f, 0.0f, -0.1f);

        }

        protected override void InitGameActorParams()
        {
            this.strengthHeld = 0.99f;
            this.grabRange = 1.1f;
            this.kickRange = 1.1f;
            this.collideWithEnv = true;
            this.collideWithPath = true;
            this.collisionRadius = 0.55f;

            // lastly, base method must be called to create "devices" (touch, sight, hearing parameters).
            base.InitGameActorParams();
        }

        public override Vector3 GlowPosition
        {
            get
            {
                return new Vector3(-0.235f, 0.0f, 0.68f);
            }
        }

        public override void CreateDevices()
        {

            // one eye
            {
                VisualDevice visual = new VisualDevice(0);
                visual.Normal = Vector3.Right; // front
                visual.Arc = 3.4906f; // MathHelper.Pi * 1.11f; // 200
                AddDevice(visual);
            }
        }
    }   
}   
