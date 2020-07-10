// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Boku.Base;
using Boku.Common;

namespace Boku.Audio
{
    /// <summary>
    /// A node in the Audio taxonomy.
    /// </summary>
    public abstract class Voice
    {
        #region Members

        private Group parent = null;

        private readonly string id;

        private bool isAny = false;

        private bool spatial = true;

        #endregion Members

        #region Accessors
        /// <summary>
        /// Unique identifier for lookup up this voice.
        /// </summary>
        public string ID
        {
            get { return id; }
        }
        /// <summary>
        /// Whether this node represents selecting an entire group (specifically my parent).
        /// </summary>
        public bool IsAny
        {
            get { return isAny; }
            private set { isAny = value; }
        }
        /// <summary>
        /// Is this Voice (or any of my children) spatialized.
        /// </summary>
        public bool Spatial
        {
            get { return spatial; }
            set { spatial = value; }
        }
        /// <summary>
        /// My parent.
        /// </summary>
        internal Group Parent
        {
            get { return parent; }
        }
        #endregion Accessors

        #region Public

        /// <summary>
        /// Does input id match ours?
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool Is(string id)
        {
            return id == this.id;
        }

        /// <summary>
        /// Build with immediate attachment to parent, and identifier.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="id"></param>
        internal Voice(Group parent, string id)
        {
            this.id = id;
            this.parent = parent;
            if(parent != null)
                parent.Add(this);

            IsAny = id.EndsWith(".any");
        }

        /// <summary>
        /// Search _down_ the hierarchy for a voice with given id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual Voice Find(string id)
        {
            if (id == this.id)
            {
                return this;
            }
            return null;
        }

        /// <summary>
        /// Am I the group or descendent of the group?
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public bool BelongsTo(Voice group)
        {
            if (this == group)
            {
                return true;
            }
            if (parent != null)
            {
                return parent.BelongsTo(group);
            }
            return false;
        }

        /// <summary>
        /// Is the Voice v this or a child of this?
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public virtual bool Contains(Voice v)
        {
            return v.BelongsTo(this);
        }

        /// <summary>
        /// If nothing in the list is me (or descendent) already playing,
        /// Play myself and add myself to the list.
        /// Returns the new audiocue if it started one, else null.
        /// </summary>
        /// <param name="list"></param>
        public virtual AudioCue Play(GameThing emitter, List<AudioCue> list)
        {
            for (int i = 0; i < list.Count; ++i)
            {
                AudioCue cue = list[i];
                Sound v = cue.Sound;
                if ((v != null) && v.BelongsTo(this))
                {
                    /// Already playing, just bail.
                    return null;
                }
            }
            /// Start a random child playing
            Sound leaf = Select();

            AudioCue toplay = leaf.Play(emitter);
            if (emitter != null)
            {
                toplay.OnComplete += emitter.OnAudioCueComplete;
            }

            /// Add leaf to list
            list.Add(toplay);

            return toplay;
        }

        /// <summary>
        /// If anything(s) in the list are me (or descendent),
        /// Stop it and remove it from the list.
        /// </summary>
        /// <param name="list"></param>
        public virtual bool Stop(List<AudioCue> list)
        {
            bool stopped = false;
            for (int i = list.Count - 1; i >= 0; --i)
            {
                AudioCue cue = list[i];
                Sound v = cue.Sound;
                if ((v != null) && v.BelongsTo(this))
                {
                    /// Stop v from playing
                    cue.StopImmediate();

                    list.RemoveAt(i);

                    stopped = true;
                }
            }
            return stopped;
        }

        /// <summary>
        /// Helper for stopping (as authored) and clearing out an entire list.
        /// </summary>
        /// <param name="list"></param>
        public static void Clear(List<AudioCue> list)
        {
            for (int i = 0; i < list.Count; ++i)
            {
                AudioCue cue = list[i];
                cue.Stop();
            }
            list.Clear();
        }
        /// <summary>
        /// Helper for immediately stopping and clearing out an entire list.
        /// </summary>
        /// <param name="list"></param>
        public static void ClearImmediate(List<AudioCue> list)
        {
            for (int i = 0; i < list.Count; ++i)
            {
                AudioCue cue = list[i];
                cue.StopImmediate();
            }
            list.Clear();
        }

        #endregion Public

        #region Internal

        /// <summary>
        /// Add myself and any children to a lookup dictionery.
        /// </summary>
        /// <param name="lookup"></param>
        internal virtual void AddToLookup(Dictionary<string, Voice> lookup)
        {
            lookup.Add(ID, this);
        }

        /// <summary>
        /// Is the emitter playing a sound through me (or a descendent)?
        /// </summary>
        /// <param name="emitter"></param>
        /// <returns></returns>
        internal abstract bool IsPlaying(GameThing emitter);

        /// <summary>
        /// Pick a sound to play.
        /// </summary>
        /// <returns></returns>
        internal abstract Sound Select();
        #endregion Internal
    }
}
