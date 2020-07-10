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
using Boku.Common.ParticleSystem;
using Boku.SimWorld;
using Boku.SimWorld.Chassis;
using Boku.Programming;
using Boku.Common.Xml;

using Xclna.Xna.Animation;

namespace Boku
{
    public class GateBot : GameActor
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
                    xmlGameActor = XmlGameActor.Deserialize("GateBot");
                return xmlGameActor;
            }
        }
        #endregion Accessors

        //
        //  GateBot
        //
        public enum GateModes
        {
            Open,
            Close,
        }

        private GateModes gateMode = GateModes.Close;

        public GateBot()
            : base()
        {

            this.classification = new Classification("gatebot",
                    Classification.Colors.White,
                    Classification.Shapes.Ball,
                    Classification.Tastes.None,
                    Classification.Smells.None,
                    Classification.Physicalities.Collectable);
            this.classification.audioImpression = Classification.AudioImpression.Noisy;
            this.classification.audioVolume = Classification.AudioVolume.Soft;
            this.classification.emitter = ExpressModifier.Emitters.None;
            this.classification.expression = Face.FaceState.None;

            StaticPropChassis staticChassis = new StaticPropChassis();
            Chassis = staticChassis;
            staticChassis.Mass = 500.0f;
            staticChassis.DefaultEditHeight = 0.0f;

            renderObj = new RenderObj(this, GateBotSRO.GetInstance);
            InitGameActorParams();

            BokuGame.Load(this);

            // Set holding position after loading model so that BoundingSphere is initialized.
            this.holdingPosition = new Vector3(0.6f, 0.0f, 0.6f);

        }   // end of GateBot c'tor

        protected override void InitGameActorParams()
        {
            this.strengthHeld = 0.9f;
            this.grabRange = 2.0f;
            this.kickRange = 2.0f;
            this.collideWithEnv = false;
            this.collideWithPath = false;
            this.collisionRadius = 1.5f;

            fireballEmitter = new FireballEmitter(InGame.inGame.ParticleSystemManager); // Starts in inactive state.

            // lastly, base method must be called to create "devices" (touch, sight, hearing parameters).
            base.InitGameActorParams();
        }

        #region Accessors
        public override Vector3 GlowPosition
        {
            get
            {
                return new Vector3(0.0f, 0.0f, 1.5f);
            }
        }

        public GateModes GateMode
        {
            get
            {
                return this.gateMode;
            }
            set
            {
                this.gateMode = value;
            }

        }
        public RenderObj GateRenderObj
        {
            get { return renderObj as RenderObj; }
        }
        #endregion

        protected override bool IDoDirectObjectVerb(GameThing.Verbs verb, GameThing subject, Effector effector)
        {
            bool supports = false;
            switch (verb)
            {
                case GameThing.Verbs.Open:
                    supports = DoOpen();
                    break;

                case GameThing.Verbs.Close:
                    supports = DoClose();
                    break;

                default:
                    supports = base.IDoDirectObjectVerb(verb, subject, effector);
                    break;
            }
            return supports;
        }


        protected bool DoOpen()
        {
            if (gateMode != GateModes.Open)
            {
                this.gateMode = GateModes.Open;
                AnimOpen();
            }
            return true;
        }

        protected bool DoClose()
        {
            if (gateMode != GateModes.Close)
            {
                this.gateMode = GateModes.Close;
                AnimClose();
            }
            return true;
        }

        protected override void OnNoiseLevelReset(GameTimer timer)
        {
            this.Classification.audioVolume = Classification.AudioVolume.Soft;
        }

        public override void CreateDevices()
        {
            {
                VisualDevice visual = new VisualDevice(0);
                visual.Normal = Vector3.Backward; // up
                visual.Arc = MathHelper.Pi * 1.96f; // 300, small cone downward that it can't see
                AddDevice(visual);
            }
        }

        #region ANIMATION

        private AnimationController _animOpenA;
        private AnimationController _animCloseA;
        private AnimationController _animOpenB;
        private AnimationController _animCloseB;

        private void LoadAnimations(FBXModel sro)
        {
            _animator = sro.MakeAnimator();
            _animOpenA = new AnimationController(null, _animator.Animations["open_A"]);
            _animCloseA = new AnimationController(null, _animator.Animations["close_A"]);
            _animOpenB = new AnimationController(null, _animator.Animations["open_B"]);
            _animCloseB = new AnimationController(null, _animator.Animations["close_B"]);
            // test: just to show that animation is loaded and working
            _animOpenA.SpeedFactor = 0;
            SetAnimation(_animOpenA);
        }

        private void UnloadAnimations()
        {
            _animOpenA = null;
            _animCloseA = null;
            _animOpenB = null;
            _animCloseB = null;
        }

        public void AnimOpen()
        {
            _animOpenB.ElapsedTime = 0;
            _animOpenB.SpeedFactor = .25;
            _animOpenB.IsLooping = false;
            SetAnimation(_animOpenB);
        }

        public void AnimClose()
        {
            _animCloseB.ElapsedTime = 0;
            _animCloseB.SpeedFactor = .25;
            _animCloseB.IsLooping = false;
            SetAnimation(_animCloseB);
        }

        #endregion

        public override void InitDeviceResources(GraphicsDeviceManager graphics)
        {
            base.InitDeviceResources(graphics);
            LoadAnimations(renderObj.SRO);
        }

        public override void UnloadContent()
        {
            UnloadAnimations();
            base.UnloadContent();
        }   // end of GateBot UnloadContent()

    }   // end of class GateBot

}   // end of namespace Boku
