using System;
using System.Collections.Generic;
using System.Diagnostics;

using Boku.Base;

namespace Boku.Audio
{
    internal class Group : Voice
    {
        #region Members
        private List<Voice> children = new List<Voice>();

        private List<Voice> unplayed = new List<Voice>();

        private static Random rnd = new Random();
        #endregion Members

        #region Accessors

        #endregion Accessors

        #region Public

        /// <summary>
        /// Constructor, requires a parent to attach to, and a unique id.
        /// Root has a null parent.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="id"></param>
        public Group(Group parent, string id)
            : base(parent, id)
        {
        }

        /// <summary>
        /// Add a child to my list.
        /// </summary>
        /// <param name="v"></param>
        public void Add(Voice v)
        {
            children.Add(v);
        }

        /// <summary>
        /// Remove a child from my list.
        /// </summary>
        /// <param name="v"></param>
        public void Remove(Voice v)
        {
            children.Remove(v);
        }

        /// <summary>
        /// Search _down_ the hierarchy for a voice with given id.
        /// Once the taxonomy is constructed, you should use Audio.Find() instead.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override Voice Find(string id)
        {
            Voice v = base.Find(id);
            if (v == null)
            {
                for (int i = 0; i < children.Count; ++i)
                {
                    Voice child = children[i];
                    v = child.Find(id);
                    if (v != null)
                        break;
                }
            }
            return v;
        }

        #endregion Public


        #region Internal

        /// <summary>
        /// Select a random child and play it.
        /// </summary>
        internal override Sound Select()
        {
            Voice child = null;
            if (unplayed.Count == 0)
            {
                unplayed.Capacity = children.Count;
                for (int i = 0; i < children.Count; ++i)
                {
                    Voice v = children[i];
                    if (!v.IsAny)
                        unplayed.Add(v);
                }
            }
            Debug.Assert(unplayed.Count > 0, "Empty group?");
            if (unplayed.Count == 1)
            {
                child = unplayed[0];
                unplayed.Clear();
            }
            else
            {
                int idx = rnd.Next(unplayed.Count);
                child = unplayed[idx];
                unplayed.RemoveAt(idx);
            }
            Debug.Assert(child != null, "Null child in children list?");

            return child.Select();
        }

        /// <summary>
        /// Add self and children to the lookup dictionary.
        /// </summary>
        /// <param name="lookup"></param>
        internal override void AddToLookup(Dictionary<string, Voice> lookup)
        {
            base.AddToLookup(lookup);
            
            bool anySpatial = false;
            for (int i = 0; i < children.Count; ++i)
            {
                Voice v = children[i];
                v.AddToLookup(lookup);
                if (!v.IsAny && v.Spatial)
                    anySpatial = true;
            }
            Spatial = anySpatial;
        }


        /// <summary>
        /// True if the given emitter is playing our sound (or a descendent's sound).
        /// </summary>
        /// <param name="emitter"></param>
        /// <returns></returns>
        internal override bool IsPlaying(GameThing emitter)
        {
            for (int i = 0; i < children.Count; ++i)
            {
                Voice child = children[i];
                if (child.IsPlaying(emitter))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion Internal

    }
}
