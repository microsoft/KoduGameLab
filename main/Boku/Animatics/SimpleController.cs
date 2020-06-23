using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;


namespace Boku.Animatics
{
    public class SimpleController : BaseController
    {
        #region Members
        private double speed = 1.0f;
        private bool loop = true;
        private bool oneMore = false;
        private readonly long duration;

        private readonly string animName;

        private static Random rnd = new Random();
        #endregion Members

        #region Accessors
        /// <summary>
        /// Whether this animation currently loops.
        /// </summary>
        public bool Loop
        {
            get { return loop; }
            set { loop = value; }
        }
        /// <summary>
        /// If true, finish this loop, do the next and then stop.
        /// </summary>
        public bool OneMoreLoop
        {
            get { return oneMore; }
            set 
            {
                oneMore = value;
                if (oneMore)
                    loop = false;
            }
        }
        /// <summary>
        /// Speed scalar for this animation. 
        /// </summary>
        public double Speed
        {
            get { return speed; }
            set { speed = value; }
        }
        /// <summary>
        /// Length of this animation in ticks.
        /// </summary>
        public long Duration
        {
            get { return duration; }
        }
        /// <summary>
        /// Is the animation at the end.
        /// </summary>
        public bool AtEnd
        {
            get { return CurrentTicks == Duration; }
        }
        /// <summary>
        /// Name of this animation.
        /// </summary>
        public string AnimName
        {
            get { return animName; }
        }
        #endregion Accessors

        #region Public

        /// <summary>
        /// Try to make a simple controller of the animation of given name 
        /// in the container inst. May return null (e.g. if the named animation doesn't exist).
        /// </summary>
        /// <param name="inst"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static SimpleController TryMake(AnimationInstance inst, string name)
        {
            return TryMake(inst, name, null);
        }

        /// <summary>
        /// Try to make a simple controller of animation from inst named "name". If that's
        /// not found, try "backup". Will return null if neither is found.
        /// </summary>
        /// <param name="inst"></param>
        /// <param name="name"></param>
        /// <param name="backup"></param>
        /// <returns></returns>
        public static SimpleController TryMake(AnimationInstance inst, string name, string backup)
        {
            Animation anim = inst.FindAnimation(name);
            if ((anim == null) && (backup != null))
                anim = inst.FindAnimation(backup);

            return anim != null ? new SimpleController(inst, anim) : null;
        }

        /// <summary>
        /// Advance the clock by ticks. No heavy lifting, just update internal clock state.
        /// </summary>
        /// <param name="advTicks"></param>
        public override void Update(long time)
        {
            Debug.Assert((Weight >= -kWeightTol) && (Weight <= 1.0f + kWeightTol), 
                "Do we need to clamp this on input?");

            AdvanceTime(time);
        }

        /// <summary>
        /// Compute the localToParent values we know about, leaving the rest alone.
        /// localToParent has already been populated with default values.
        /// </summary>
        /// <param name="inst"></param>
        /// <param name="localToParent"></param>
        internal override void GetTransforms(AnimationInstance inst, Matrix[] localToParent)
        {
            Animation animation = inst.FindAnimation(animName);

            Debug.Assert(Weight > 0);
            if (animation != null)
            {
                BoneKeys[] keyList = animation.KeysList;

                int keyIdx = keyList[0].IndexAtTime(CurrentTicks);
                for (int i = 0; i < keyList.Length; ++i)
                {
                    int boneIdx = animation.KeyIndexToBoneIndex(i);
                    localToParent[boneIdx] = keyList[i].AtIndex(keyIdx);
                }
            }
        }

        /// <summary>
        /// Reset to the beginning of the animation.
        /// </summary>
        public void SetToBegin()
        {
            CurrentTicks = 0;
        }
        /// <summary>
        /// Advance to the end of the animation.
        /// </summary>
        public void SetToEnd()
        {
            CurrentTicks = duration;
        }
        /// <summary>
        /// Set self to some random place in the middle of the animation.
        /// </summary>
        public void SetToRandom()
        {
            CurrentTicks = rnd.Next((int)duration);
        }
        /// <summary>
        /// Align self as an even multiple of ticks.
        /// </summary>
        /// <param name="ticks"></param>
        public void Align(long ticks)
        {
            int leftOver = (int)(CurrentTicks % ticks);
            CurrentTicks -= leftOver;
        }
        #endregion Public

        #region Internal
        /// <summary>
        /// Internal constructor from an instance and an animation within that instance.
        /// </summary>
        /// <param name="inst"></param>
        /// <param name="anim"></param>
        internal SimpleController(AnimationInstance inst, Animation anim)
        {
            animName = anim.Name;
            duration = anim.Duration;
        }

        /// <summary>
        /// Advance the time by advTicks, doing proper loop or clamp.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        private bool AdvanceTime(long advTicks)
        {
            long oldTicks = CurrentTicks;
            long advance = (long)(advTicks * speed);

            CurrentTicks += advance;

            if (Loop || OneMoreLoop)
            {
                if ((CurrentTicks > duration) || (CurrentTicks < 0))
                {
                    OneMoreLoop = false;
                    CurrentTicks = duration > 0 ? CurrentTicks % duration : 0;
                    if (CurrentTicks < 0)
                    {
                        CurrentTicks += duration;
                    }
                }
            }
            else
            {
                if (CurrentTicks < 0)
                    CurrentTicks = 0;
                else if (CurrentTicks >= duration)
                    CurrentTicks = duration;
            }
            Debug.Assert((CurrentTicks >= 0) && (CurrentTicks <= duration));

            return (advTicks == 0) || (oldTicks != CurrentTicks);
        }

        #endregion Internal
    }
}
