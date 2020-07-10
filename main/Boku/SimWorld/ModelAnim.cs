// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;

using Boku.Common;

using Boku.Animatics;

namespace Boku.SimWorld
{
    public class ModelAnim
    {
        #region Members
        protected AnimatorList activeAnimation = null;
        protected SimpleController activeController = null;
        protected AnimatorList idleAnimation = null;
        protected SimpleController idleController = null;
        #endregion Members

        #region Accessors
        /// <summary>
        /// Return the (possibly null) animation for the active state.
        /// </summary>
        public AnimatorList ActiveAnimation
        {
            get { return activeAnimation; }
            protected set { activeAnimation = value; }
        }
        /// <summary>
        ///  Return the (possibly null) animation for the idle state.
        /// </summary>
        public AnimatorList IdleAnimation
        {
            get { return idleAnimation != null ? idleAnimation : ActiveAnimation; }
            protected set { idleAnimation = value; }
        }

        #region Internal
        protected SimpleController ActiveController
        {
            get { return activeController; }
            set { activeController = value; }
        }
        protected SimpleController IdleController
        {
            get { return idleController; }
            set { idleController = value; }
        }

        protected bool HasRestPalette
        {
            get { return IdleAnimation != null; }
        }
        #endregion Internal
        #endregion Accessors

        #region Public
        /// <summary>
        /// Construct self from a model and active animation name.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="animName"></param>
        public ModelAnim(FBXModel model, string animName)
        {
            ActiveAnimation = new AnimatorList(model);
            if (ActiveAnimation.NotEmpty)
            {
                AnimationInstance sample = ActiveAnimation.Sample;
                ActiveController = SimpleController.TryMake(sample, animName, sample.FirstAnimationName);

                if (ActiveController != null)
                {
                    ActiveAnimation.ApplyController(ActiveController);

                    IdleAnimation = new AnimatorList(model);
                    Debug.Assert(IdleAnimation.NotEmpty, "Could make active but not idle?");
                    sample = IdleAnimation.Sample;
                    IdleController = SimpleController.TryMake(sample, "idle", null);
                    if (IdleController != null)
                    {
                        IdleAnimation.ApplyController(IdleController);
                    }
                    else
                    {
                        IdleAnimation = null;
                    }
                }
                else
                {
                    ActiveAnimation = null;
                }
            }
        }
        /// <summary>
        /// Set up the model to be rendered using the active animation with idle in rest palette.
        /// </summary>
        /// <param name="model"></param>
        public void SetupActive(FBXModel model)
        {
            model.Animators = ActiveAnimation;
            SetRestPalette(model);
        }
        /// <summary>
        /// Setup the model with idle animation AND idle in rest palette.
        /// </summary>
        /// <param name="model"></param>
        public void SetupIdle(FBXModel model)
        {
            model.Animators = IdleAnimation;
            SetRestPalette(model);
        }
        /// <summary>
        /// Advance the animations.
        /// </summary>
        public void Update()
        {
            if ((ActiveAnimation != null) && (ActiveController != null))
            {
                ActiveController.Update(Time.GameTimeFrameSeconds);
            }
            if ((IdleAnimation != null) && (IdleController != null))
            {
                IdleController.Update(Time.GameTimeFrameSeconds);
            }
        }
        #endregion Public

        #region Internal
        protected void SetRestPalette(FBXModel model)
        {
            if (HasRestPalette)
            {
                model.RestPalettes = IdleAnimation.RestPalettes;
            }
        }
        #endregion Internal
    }
}
