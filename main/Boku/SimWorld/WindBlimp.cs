
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

using KoiX;

using Boku.Base;
using Boku.Common;
using Boku.Common.ParticleSystem;
using Boku.SimWorld;
using Boku.SimWorld.Chassis;
using Boku.SimWorld.Terra;
using Boku.Programming;
using Boku.Common.Xml;

using Boku.Animatics;

namespace Boku
{
    public class WindBlimp : GameActor
    {
        public WindBlimp(string classificationName, BaseChassis chassis, GetModelInstance getModelInstance, StaticActor staticActor)
            : base(classificationName, classificationName, chassis, getModelInstance, getModelInstance, staticActor) { }

        public override void UpdateAnimations()
        {
            base.UpdateAnimations();

            if (idleController != null)
            {
                idleController.Update(Time.GameTimeFrameSeconds);
            }
        }

        /// <summary>
        /// You might well wonder why Steve set this up this way, different from
        /// any other model in the system. I did, so I asked him. He didn't remember.
        /// So now that I've figured out why, I'm writing it down here. It's sort
        /// of like I'm commenting on the code, I'm embedding my comments on nuances
        /// of the code within the code. I may have to patent that idea.
        /// Anyway, the reason is that for other models, they load up their various
        /// animations and blend them together based on things like whether the bot
        /// is moving or shooting or whatever. The blimp only has one animation. That
        /// in itself isn't a problem, because the anim system just puts the idle 
        /// animation in for whatever is missing, and the idle animation is the one
        /// animation the blimp has. But the blimp's idle animation has the propellor
        /// rotating 1080 degrees (3 revs) around a pivot point. Now, if you take two idle
        /// animations that have different phase, and blend the current Matrix from
        /// each by some weights (which is how the different animations are blended),
        /// then you will get garbage. The matrix's scale will grow and shrink as the
        /// weights are changed.
        /// So, the single animation is loaded up here and set to go, and the AnimationSet
        /// functionality is skipped because there is no animation named "idle" within
        /// the data.
        /// </summary>
        SimpleController idleController = null;
        public override void InitDeviceResources(GraphicsDevice device)
        {
            base.InitDeviceResources(device);
            _animators = new AnimatorList(StaticActor.Model);
            /// All animators in the stack should have the same set of animations.
            AnimationInstance animator = _animators.Sample;

            // TODO (****)  Do we need this check???
            if (animator.HasAnimation("full_running"))
            {
                // Add individual animations to blend controller.
                idleController = SimpleController.TryMake(animator, "full_running");
                SetAnimation(idleController);
            }
        }

        public override void UnloadContent()
        {
            base.UnloadContent();
            _animators = null;       // should we dispose()?
            _currentAnim = null;
        }

    }   // end of class WindBlimp

}   // end of namespace Boku
