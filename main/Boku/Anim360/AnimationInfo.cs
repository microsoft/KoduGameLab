/*
 * AnimationInfo.cs
 * Copyright (c) 2006 David Astle
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be included
 * in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework.Graphics;

namespace Xclna.Xna.Animation
{
    /// <summary>
    /// A collection of BoneKeyFrames that represents an animation track.
    /// </summary>
    public class BoneKeyframeCollection : ReadOnlyCollection<BoneKeyframe>
    {
        #region Member Variables
        // The name of the bone represented by this animation track
        private string boneName;
        // Duration of the track
        private long duration;
        #endregion

        #region Constructors
        // Only allow creation from inside the library (only in AnimationReader)
        internal BoneKeyframeCollection(string boneName,
            IList<BoneKeyframe> list) : base(list)
        {
            this.boneName = boneName;
            duration = list[list.Count - 1].Time;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the duration of the animation track.
        /// </summary>
        public long Duration
        {
            get { return duration; }
        }

        /// <summary>
        /// Gets the name of the bone associated with the animation track.
        /// </summary>
        public string BoneName
        { get { return boneName; } }
        #endregion

        #region Methods
        /// <summary>
        /// Gets the index in the track at the given time.
        /// </summary>
        /// <param name="ticks">The time for which the index is found.</param>
        /// <returns>The index in the track at the given time.</returns>
        public int GetIndexByTime(long ticks)
        {
            // Since the animation is usually interpolated to 60 fps, this will
            // almost always be the index to return
            int firstFrameIndexToCheck = (int)(ticks / Util.TICKS_PER_60FPS);
            // Do out of bounds checking
            if (firstFrameIndexToCheck >= base.Count)
                firstFrameIndexToCheck = base.Count - 1;
            // Increment the index until the time at the next index is greater than the
            // specified time
            while (firstFrameIndexToCheck < base.Count - 1
                    && base[firstFrameIndexToCheck+1].Time < ticks)
            {
                ++firstFrameIndexToCheck;
            }
            // Decrement the index till the time at the index is not greater than the
            // specified time
            while (firstFrameIndexToCheck >= 0 && base[firstFrameIndexToCheck].Time >
                ticks)
            {
                --firstFrameIndexToCheck;
            }

            return firstFrameIndexToCheck;
        }

        #endregion
    }

    /// <summary>
    /// Represents a keyframe in an animation track.
    /// </summary>
    public struct BoneKeyframe
    {
        /// <summary>
        /// Creats a new BoneKeyframe.
        /// </summary>
        /// <param name="transform">The transform for the keyframe.</param>
        /// <param name="time">The time in ticks for the keyframe.</param>
        public BoneKeyframe(Matrix transform, long time)
        {
            this.Transform = transform;
            this.Time = time;
        }
        /// <summary>
        /// The transform for the keyframe.
        /// </summary>
        public readonly Matrix Transform;
        /// <summary>
        /// The time for the keyframe.
        /// </summary>
        public readonly long    Time;

    }

    /// <summary>
    /// A collection of animation channels or tracks, which are sections of an
    /// animation that run for one bone.
    /// </summary>
    public class AnimationChannelCollection : ReadOnlyCollection<BoneKeyframeCollection>
    {
        // Allow quick access to channels by BoneName
        private Dictionary<string, BoneKeyframeCollection> dict =
            new Dictionary<string, BoneKeyframeCollection>();

        // The bones affected by the tracks contained in this collection
        private ReadOnlyCollection<string> affectedBones;

        // This immutable data structure should not be created by the library user
        internal AnimationChannelCollection(IList<BoneKeyframeCollection> channels)
            : base(channels)
        {
            // Find the affected bones
            List<string> affected = new List<string>();
            foreach (BoneKeyframeCollection frames in channels)
            {
                dict.Add(frames.BoneName, frames);
                affected.Add(frames.BoneName);
            }
            affectedBones = new ReadOnlyCollection<string>(affected);

        }

        /// <summary>
        /// Gets the BoneKeyframeCollection that is associated with the given bone.
        /// </summary>
        /// <param name="boneName">The name of the bone that contains a track in this
        /// AnimationChannelCollection.</param>
        /// <returns>The track associated with the given bone.</returns>
        public BoneKeyframeCollection this[string boneName]
        {
            get { return dict[boneName]; }
        }

        // See AnimationInfo's equivalent method for documentation
        internal bool AffectsBone(string boneName)
        { return dict.ContainsKey(boneName); }

        // See AnimationInfo's equivalent method for documentation
        internal ReadOnlyCollection<string> AffectedBones
        {
            get { return affectedBones; }
        }
    }

    /// <summary>
    /// Contains information about an animation.
    /// </summary>
    public class AnimationInfo
    {
        private long duration = 0;
        private string animationName;

        // The bone animation tracks
        private AnimationChannelCollection boneAnimations;
        
        // Internal because it should only be created by the AnimationReader
        internal AnimationInfo(string animationName, AnimationChannelCollection 
            anims)
        {
            this.animationName = animationName;
            boneAnimations = anims;
            foreach (BoneKeyframeCollection channel in anims)
            {
                if (channel.Duration > duration)
                    duration = channel.Duration;
            }
        }

        /// <summary>
        /// Gets a collection of channels that represent the bone animation
        /// tracks for this animation.
        /// </summary>
        public AnimationChannelCollection AnimationChannels
        { get { return boneAnimations; } }


        /// <summary>
        /// Gets a collection of bones that have tracks in this animation.
        /// </summary>
        public ReadOnlyCollection<string> AffectedBones
        { get { return boneAnimations.AffectedBones; } }

        /// <summary>
        /// Gets the total duration of this animation in ticks.
        /// </summary>
        public long Duration
        {
            get { return duration; }
        }

        /// <summary>
        /// Gets the name of the animation.
        /// </summary>
        public string Name
        {
            get { return animationName; }
        }


        /// <summary>
        /// Returns true if the animation contains any tracks that affect the given
        /// bone.
        /// </summary>
        /// <param name="boneName">The bone to test for track information.</param>
        /// <returns>True if the animation contains any tracks that affect the given
        /// bone.</returns>
        public bool AffectsBone(string boneName)
        { return boneAnimations.AffectsBone(boneName); }
    }

    /// <summary>
    /// A collection of AnimationInfo objects.
    /// </summary>
    public class AnimationInfoCollection : SortedList<string, AnimationInfo>
    {
        // New instances should only be created by the AnimationReader
        internal AnimationInfoCollection()
        {
        }

        /// <summary>
        /// Gets a collection of animations stored in the model.
        /// </summary>
        /// <param name="model">The model that contains the animations.</param>
        /// <returns>The animations stored in the model.</returns>
        public static AnimationInfoCollection FromModel(Model model)
        {
            // Grab the tag that was set in the processor; this is a dictionary so that users can extend
            // the processor and pass their own data into the program without messing up the animation data
            Dictionary<string, object> modelTagData = (Dictionary<string, object>)model.Tag;
            if (modelTagData == null || !modelTagData.ContainsKey("Animations"))
            {
                return new AnimationInfoCollection();
            }
            else
            {
                AnimationInfoCollection animations = (AnimationInfoCollection)modelTagData["Animations"];
                return animations;
            }
        }

        /// <summary>
        /// Gets the AnimationInfo object at the given index.
        /// </summary>
        /// <param name="index">The index of the AnimationInfo object.</param>
        /// <returns>The AnimationInfo object at the given index.</returns>
        public AnimationInfo this[int index]
        {
            get
            {
                return this.Values[index];
            }
        }

    }




}
