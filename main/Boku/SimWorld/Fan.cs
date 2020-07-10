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

using Boku.Audio;
using Boku.Base;
using Boku.Common;
using Boku.SimWorld;
using Boku.SimWorld.Chassis;
using Boku.Programming;
using Boku.Common.Xml;
using Boku.Common.ParticleSystem;

namespace Boku
{
    public class Fan : GameActor
    {
        #region Members

        FanEmitter pushEmitter = null;
        FanEmitter pullEmitter = null;

        bool pushing;
        bool pushingReverse;
        bool wasPushing;

        #endregion

        #region Accessors

        // Pushing and PushingReverse are set per frame.
        // so they need to be cleared at the beginning of each
        // frame. Currently this is done in UpdateActuators().
        // At this same time, wasPushing should be set to pushing.


        public bool Pushing
        {
            get { return pushing; }
            set { pushing = value; }
        }

        public bool PushingReverse
        {
            get { return pushingReverse; }
            set { pushingReverse = value; }
        }

        public bool WasPushing
        {
            get { return wasPushing; }
            set { wasPushing = value; }
        }


        #endregion

        //
        //  Fan
        //

        public Fan(string classificationName, BaseChassis chassis, GetModelInstance getModelInstance, StaticActor staticActor)
            : base(classificationName, classificationName, chassis, getModelInstance, getModelInstance, staticActor)
        {
            CreateEmitter();

        }   // end of Fan c'tor

        public override void PreBrainUpdate()
        {
            wasPushing = pushing;
            pushing = false;
            pushingReverse = false;
            
            base.PreBrainUpdate();
        }   // end of PreBrainUpdate()

        public override void PostBrainUpdate()
        {
            // Check if the pushing state has changed and play associated sounds.
            if (!Mute && !WasPushing && Pushing)
            {
                Foley.PlayFanStart(this);
                Foley.PlayFanLoop(this);
            }
            if (wasPushing && !Pushing)
            {
                Foley.StopFanLoop(this);
                if (!Mute)
                {
                    Foley.PlayFanStop(this);
                }
            }

            base.PostBrainUpdate();
        }   // end of PostBrainUpdate()

        protected override bool DoPush(BaseAction effector, GameThing directObject, bool quiet, bool reverse)
        {
            Pushing = true;
            PushingReverse = reverse;

            return base.DoPush(effector, directObject, quiet, reverse);
        }   // end of DoPush()

        public override void Pause()
        {
            // When pausing, stop fan.
            if (Pushing)
            {
                Pushing = false;
                Foley.StopFanLoop(this);
                Foley.PlayFanStop(this);
            }

            base.Pause();
        }   // end of Pause()

        protected override void UpdateAttachments()
        {
            // Edit mode will always emit fan vfx
            if (pushEmitter != null && Pushing && !PushingReverse)
            {
                pushEmitter.Emitting = true;
                AnimationSet.IdleController.Speed = 1.0f;
            }
            else
            {
                pushEmitter.Emitting = false;
            }

            if (pullEmitter != null && Pushing && PushingReverse)
            {
                pullEmitter.Emitting = true;
                AnimationSet.IdleController.Speed = -1.0f;
            }
            else
            {
                pullEmitter.Emitting = false;
            }

            if (!pushEmitter.Emitting && !pullEmitter.Emitting)
            {
                AnimationSet.IdleController.Speed = 0.0f;
            }

            base.UpdateAttachments();
        }

        protected void CreateEmitter()
        {
            //push emitter
            pushEmitter = new FanEmitter(InGame.inGame.ParticleSystemManager);
            pushEmitter.Active = true;
            pushEmitter.AttachTo = this;
            pushEmitter.Forwards = true;
            pushEmitter.AddToManager();

            AddEmitter(pushEmitter, Vector3.Zero);

            //pull emitter
            pullEmitter = new FanEmitter(InGame.inGame.ParticleSystemManager);
            pullEmitter.Active = true;
            pullEmitter.AttachTo = this;
            pullEmitter.Forwards = false;
            pullEmitter.AddToManager();

            //pull emitter position
            Vector3 position = Vector3.Normalize(this.Movement.Facing) * (this as GameActor).FinalPushRange * 0.9f;

            AddEmitter(pullEmitter, position);
        }

        public override void Deactivate()
        {
            base.Deactivate();

            if (pushEmitter != null)
            {
                pushEmitter.Active = false;
                pushEmitter.Emitting = false;
            }

            if (pullEmitter != null)
            {
                pullEmitter.Active = false;
                pullEmitter.Emitting = false;
            }
        }

    }   // end of class Fan

}   // end of namespace Boku
