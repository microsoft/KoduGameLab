// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;
using Boku.Common;
using Boku.Animatics;

namespace Boku.SimWorld
{
    public class SharedIdle : GameActor
    {
        #region Members
        protected class SharedAnimation
        {
            #region Members
            public AnimatorList ActiveAnimator = null;
            public SimpleController ActiveController = null;
            public AnimatorList IdleAnimator = null;
            public SimpleController IdleController = null;
            public long LastFrame = -1;
            public string ActiveName = "";
            public string IdleName = "";
            #endregion Members

            public SharedAnimation(string activeName, string idleName)
            {
                ActiveName = activeName;
                IdleName = idleName;
            }
            public SharedAnimation(string activeName)
            {
                ActiveName = activeName;
                IdleName = "idle";
            }
        };

        protected readonly SharedAnimation sharedAnim;
        protected FBXModel fbxSRO = null;
        #endregion Members

        #region Accessors
        protected SharedAnimation SharedAnim
        {
            get { return sharedAnim; }
        }

        private FBXModel FBXSRO
        {
            get { return fbxSRO; }
        }
        #endregion Accessors

        #region Public

        public SharedIdle(
            string classificationName,
            BaseChassis chassis,
            GetModelInstance getModelInstance,
            StaticActor staticActor)
            : base(classificationName, classificationName, chassis, getModelInstance, getModelInstance, staticActor)
        {
            if (XmlActorParams.SharedIdleData != null)
                sharedAnim = new SharedAnimation(XmlActorParams.SharedIdleData.ActiveAnimationName);
            else
                sharedAnim = new SharedAnimation("");

            fbxSRO = getModelInstance() as FBXModel;
            Debug.Assert(fbxSRO != null, "Wrong model type?");
        }

        #endregion Public


        #region Animation

        /// <summary>
        /// Set bot's animations based on current movement.
        /// </summary>
        public override void UpdateAnimations()
        {
            if (SharedAnim.ActiveAnimator == null)
            {
                LoadAnimations(FBXSRO);
            }
            _animators = SharedAnim.ActiveAnimator;
            if (SharedAnim.LastFrame != Time.FrameCounter)
            {
                SharedAnim.LastFrame = Time.FrameCounter;

                long ticks = (long)(Time.GameTimeFrameSeconds * 10000000.0);

                SharedAnim.ActiveController.Update(ticks);

                if (SharedAnim.IdleAnimator != null)
                {
                    SharedAnim.IdleController.Update(ticks);

                    Debug.Assert(FBXSRO.NumLODs == SharedAnim.IdleAnimator.Count);
                    for (int lod = 0; lod < FBXSRO.NumLODs; ++lod)
                    {
                        FBXSRO.RestPalettes[lod] = SharedAnim.IdleAnimator[lod].Palette;
                    }
                }
                /// We don't call the base class update animations, because
                /// we're handling all animation here, and the base class will
                /// only interfere.
            }
        }

        /// <summary>
        /// Load up our animations and bind to the given model.
        /// </summary>
        /// <param name="sro"></param>
        private void LoadAnimations(FBXModel sro)
        {
            Debug.Assert(SharedAnim != null, "Must set SharedAnim to class's shared animation in constructor.");
            if (SharedAnim.ActiveAnimator == null)
            {
                SharedAnim.ActiveAnimator = new AnimatorList(sro);
            }
            _animators = SharedAnim.ActiveAnimator;
            AnimatorList animList = SharedAnim.ActiveAnimator;
            AnimationInstance animator = animList.Sample;
            if (animator != null && animator.HasAnimation(SharedAnim.ActiveName))
            {
                if (SharedAnim.ActiveController == null)
                {
                    SharedAnim.ActiveController = SimpleController.TryMake(animator, SharedAnim.ActiveName);
                    AnimActive();
                }
            }
            else
            {
                animList.ApplyController(null);
            }
            if (SharedAnim.IdleAnimator == null)
            {
                SharedAnim.IdleAnimator = new AnimatorList(sro);
                AnimationInstance sharedIdle = SharedAnim.IdleAnimator.Sample;
                if ((sharedIdle != null) && sharedIdle.HasAnimation(SharedAnim.IdleName))
                {
                    SharedAnim.IdleController = SimpleController.TryMake(
                        sharedIdle,
                        SharedAnim.IdleName);
                }
                if (SharedAnim.IdleController != null)
                {
                    AnimIdle();
                }
                else
                {
                    SharedAnim.IdleAnimator = null;
                }
            }
        }

        private void UnloadAnimations()
        {
            SharedAnim.ActiveAnimator = null;
            SharedAnim.ActiveController = null;
            SharedAnim.IdleAnimator = null;
            SharedAnim.IdleController = null;
        }

        public void AnimActive()
        {
            SharedAnim.ActiveController.CurrentTicks = 0;
            SharedAnim.ActiveController.Speed = 1.0;
            SharedAnim.ActiveController.Loop = true;
            SharedAnim.ActiveAnimator.ApplyController(SharedAnim.ActiveController);
        }

        private void AnimIdle()
        {
            SharedAnim.IdleController.CurrentTicks = 0;
            SharedAnim.IdleController.Speed = 1.0;
            SharedAnim.IdleController.Loop = true;
            SharedAnim.IdleAnimator.ApplyController(SharedAnim.IdleController);
        }

        /// <summary>
        /// Make a no-op animation set, we'll handle animations internally.
        /// </summary>
        protected override void InitAnimationSet()
        {
            animationSet = new AnimationSet();
        }

        public override void LoadContent(bool immediate)
        {
            base.LoadContent(immediate);
        }

        public override void InitDeviceResources(GraphicsDevice device)
        {
            base.InitDeviceResources(device);
        }

        public override void UnloadContent()
        {
            UnloadAnimations();
            base.UnloadContent();
        }

        #endregion
    }
}
