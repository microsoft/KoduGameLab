// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;

using KoiX;

using Boku.Base;
using Boku.Common;

namespace Boku.Audio
{
    internal class Sound : Voice
    {
        #region Members
        private string cuename = "";

        private float loudness = 1.0f;

        private float virtualLength = 0.0f;

        List<AudioCue> cues = new List<AudioCue>();

        #endregion Members

        #region Accessors
        /// <summary>
        /// The name of the sound cue I play
        /// </summary>
        public string CueName
        {
            get { return cuename; }
            set { cuename = value; }
        }
        /// <summary>
        /// How loud am I, with 1.0f being normal loudness.
        /// </summary>
        public float Loudness
        {
            get { return loudness; }
            set { loudness = value; }
        }
        /// <summary>
        /// How long before I can repeat.
        /// </summary>
        public float VirtualLength
        {
            get { return virtualLength; }
            set { virtualLength = value; }
        }
        #endregion Accessors

        #region Public
        /// <summary>
        /// Constructor, with parent to attach to and unique id for lookup.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="id"></param>
        public Sound(Group parent, string id)
            : base(parent, id)
        {
        }

        #endregion Public

        #region Internal
        /// <summary>
        /// A leaf has no choice but to select itself.
        /// </summary>
        /// <returns></returns>
        internal override Sound Select()
        {
            return this;
        }

        /// <summary>
        /// Break ties with a cue we've spawned.
        /// </summary>
        /// <param name="cue"></param>
        internal void Detach(AudioCue cue)
        {
            Debug.Assert(cue.Sound == this, "Someone else's cue detaching from me");
            cues.Remove(cue);
            cue.Sound = null;
        }

        /// <summary>
        /// Have I played long enough to restart?
        /// </summary>
        /// <param name="cue"></param>
        /// <returns></returns>
        internal bool Done(AudioCue cue)
        {
            return (VirtualLength > 0.0f)
                && (Time.WallClockTotalSeconds - cue.StartTime >= VirtualLength);
        }

        /// <summary>
        /// True if the emitter is playing a sound through me.
        /// </summary>
        /// <param name="emitter"></param>
        /// <returns></returns>
        internal override bool IsPlaying(GameThing emitter)
        {
            for (int i = 0; i < cues.Count; ++i)
            {
                AudioCue cue = cues[i];

                if (
                    (cue.Emitter == emitter) 
                    || 
                    (!Spatial && (emitter == null))
                    )
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Will actually start playing, but assumes
        /// accompanying bookkeeping has been done. Use Play(list) instead.
        /// </summary>
        internal AudioCue Play(GameThing emitter)
        {
            AudioCue cue = BokuGame.Audio.GetCue(cuename, emitter, Spatial);
            cue.Play();
            cue.Sound = this;
            cues.Add(cue);
            return cue;
        }

        #endregion Internal
    }
}
