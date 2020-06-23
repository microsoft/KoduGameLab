using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Boku.Base;
using Boku.Common;
using Boku.Programming;

namespace Boku.Audio
{
    public partial class Audio
    {
        #region Members

        private Voice taxonomy = new Group(null, Programming.CardSpace.Group.RootGroup);

        private Dictionary<string, Voice> lookup = null;

        private Dictionary<string, CardSpace.Group> groups = null;

        #endregion Members

        #region Accessors
        #endregion Accessors

        #region Public
        /// <summary>
        /// Build up the taxonomy from a list which includes SoundModifiers.
        /// We ignore everything in the list but the SoundModifiers.
        /// </summary>
        /// <param name="groups"></param>
        /// <param name="modifiers"></param>
        /// <returns></returns>
        public bool Build(Dictionary<string, CardSpace.Group> groups, Dictionary<string, Modifier> modifiers)
        {
            this.groups = groups;

            foreach (Modifier mod in modifiers.Values)
            {
                SoundModifier soundMod = mod as SoundModifier;
                if (soundMod != null)
                {
                    AddSoundModifier(soundMod);
                }
            }

            this.groups = null;

            BuildLookup();

            return true;
        }

        /// <summary>
        /// Play sound identified by id (or randomly selected descendent) with 
        /// (possibly null) emitter for 3d effects, and place sound in list cues. 
        /// If I'm already playing a sound in the list,
        /// do nothing and return null.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="emitter"></param>
        /// <param name="cues"></param>
        /// <returns></returns>
        public AudioCue Play(string id, GameThing emitter, List<AudioCue> cues)
        {
            AudioCue cue = null;
            Voice v = Find(id);
            if (v != null)
            {
                if (v.IsAny)
                    v = v.Parent;
                cue = v.Play(emitter, cues);
            }
            return cue;
        }

        /// <summary>
        /// Stop any sounds with given id (or its descendents) that are in the list,
        /// and remove them.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cues"></param>
        /// <returns></returns>
        public bool Stop(string id, List<AudioCue> cues)
        {
            bool stopped = false;
            Voice v = Find(id);
            if (v != null)
            {
                if (v.IsAny)
                    v = v.Parent;
                stopped = v.Stop(cues);
            }
            return stopped;
        }

        public void StopAllSounds(List<AudioCue> cues)
        {
            Voice.ClearImmediate(cues);
        }

        /// <summary>
        /// Is the sound (or descendent) playing from emitter?
        /// Use emitter==null to look for non-spatial sounds.
        /// </summary>
        /// <param name="emitter"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool IsPlaying(GameThing emitter, string id)
        {
            Voice v = Find(id);
            if (v != null)
            {
                if (v.IsAny)
                    v = v.Parent;

                return v.IsPlaying(emitter);
            }
            return false;
        }

        /// <summary>
        /// Check whether the id'd voice is a Spatialized sound,
        /// or if it's a group, if _any_ of its descendents are spatialized.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool IsSpatial(string id)
        {
            Voice v = Find(id);
            if (v != null)
            {
                if (v.IsAny)
                    v = v.Parent;

                return v.Spatial;
            }
            return false;
        }
        #endregion Public

        #region Internal
        /// <summary>
        /// Lookup from dictionary to get a voice from an string id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private Voice Find(string id)
        {
            Debug.Assert(lookup != null, "Can't use find until after build is complete");
            return lookup[id];
        }

        /// <summary>
        /// Build the fast lookup dictionary.
        /// </summary>
        private void BuildLookup()
        {
            lookup = new Dictionary<string, Voice>();
            taxonomy.AddToLookup(lookup);
        }

        /// <summary>
        /// Incrementally build up the tree from a SoundModifier.
        /// </summary>
        /// <param name="soundMod"></param>
        /// <returns></returns>
        private bool AddSoundModifier(SoundModifier soundMod)
        {
            string groupName = soundMod.group;
            string soundId = soundMod.upid;
            string soundName = soundMod.sound;

            Group group = FindOrMakeGroup(groupName);

            Sound sound = AddSound(group, soundId, soundName);
            if (sound != null)
            {
                sound.Spatial = soundMod.spatial;
                sound.VirtualLength = soundMod.vlength;
                sound.Loudness = soundMod.loudness;
            }
            return sound != null;
        }

        /// <summary>
        /// Return a group with the given name, creating it if necessary.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private Group FindOrMakeGroup(string name)
        {
            Voice v = taxonomy.Find(name);
            Group group = v as Group;
            if (group != null)
            {
                return group;
            }
            string parentName = ParentName(name);
            Group parent = FindOrMakeGroup(parentName);

            group = new Group(parent, name);

            return group;
        }

        /// <summary>
        /// Return the name of the parent of the node with childName,
        /// based on the CardSpace groups.
        /// </summary>
        /// <param name="childName"></param>
        /// <returns></returns>
        private string ParentName(string childName)
        {
            CardSpace.Group cardGroup = null;
            CardSpace.Cards.GroupDict.TryGetValue(childName, out cardGroup);
            if (cardGroup != null)
            {
                return cardGroup.group;
            }
            return CardSpace.Group.RootGroup;
        }

        /// <summary>
        /// Assert a bunch of stuff and then add a new Sound to input parent.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="soundId"></param>
        /// <param name="cueName"></param>
        /// <returns></returns>
        private Sound AddSound(Group parent, string soundId, string cueName)
        {
            Debug.Assert(parent != null, "parent group not found or not a group");
            Debug.Assert(taxonomy.Find(soundId) == null, "adding an existing sound");
            if (parent != null)
            {
                Sound s = new Sound(parent, soundId);
                s.CueName = cueName;
                return s;
            }

            return null;
        }
        #endregion Internal

    }
}
