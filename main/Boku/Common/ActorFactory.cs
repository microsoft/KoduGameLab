// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Programming;

namespace Boku.Common
{
    public static class ActorFactory
    {
        static Dictionary<StaticActor, Stack<GameActor>> recycleBin
            = new Dictionary<StaticActor, Stack<GameActor>>();

        static Dictionary<Guid, Stack<GameActor>> creatableBin
            = new Dictionary<Guid, Stack<GameActor>>();

        static bool enabled = true;

        /// <summary>
        /// Explicit control over when recycling is enabled, so we can use it on
        /// entering edit mode and reloading.
        /// </summary>
        public static bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        /// <summary>
        /// Flush all held recyclables.
        /// </summary>
        public static void Clear()
        {
            recycleBin.Clear();
            creatableBin.Clear();
        }

        /// <summary>
        /// Flush all held recyclable creatables.
        /// </summary>
        public static void ClearCreatables()
        {
            creatableBin.Clear();
        }

        /// <summary>
        /// Flush all creatables linked to input id.
        /// </summary>
        /// <param name="creatableId"></param>
        public static void ClearCreatable(Guid creatableId)
        {
            creatableBin.Remove(creatableId);
        }

        /// <summary>
        /// Create a new actor of the specified type, reusing recycled instance whenever possible.
        /// </summary>
        /// <param name="gameActorType"></param>
        /// <returns></returns>
        public static GameActor Create(StaticActor staticActor)
        {
            Stack<GameActor> freeActors;
            recycleBin.TryGetValue(staticActor, out freeActors);

            GameActor actor;
            if (freeActors != null && freeActors.Count > 0)
            {
                actor = freeActors.Pop();
            }
            else
            {
                actor = staticActor.CreateNewInstance();
            }

            if (actor != null)
            {
                // Default to actor name.
                actor.DisplayName = actor.StaticActor.LocalizedName;

                actor.InRecycleBin = false;
                actor.FactoryCreated = true;

                // For this call we ignore the current state so we know that the 
                // subsystems will get reset.  Without this the smoke emitters 
                // don't turn on until the level has been run and reset.
                actor.ResetState(ignoreCurrentState: true);
            }

            return actor;
        }

        /// <summary>
        /// If the src is a creatable actor,
        ///   If we have a recycled copy sitting around
        ///     return the recycled copy
        ///   else
        ///     get a new actor of the same type and clone src's brain
        /// Else
        ///   Return a new blank copy of the appropriate type
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static GameActor Create(GameActor src)
        {
            Debug.Assert(src != null, "Cloning a null source actor");
            GameActor dst = null;

            if (Enabled && (src.CreatableId != Guid.Empty))
            {
                Stack<GameActor> freeActors = null;
                if (creatableBin.TryGetValue(src.CreatableId, out freeActors)
                    &&(freeActors != null)
                    &&(freeActors.Count > 0))
                {
                    dst = freeActors.Pop();
                }
                else
                {
                    dst = Create(src.StaticActor);
                    dst.Brain = Brain.DeepCopy(src.Brain);
                }
                if (dst != null)
                {
                    dst.Brain.Wipe();
                    dst.InRecycleBin = false;
                    dst.FactoryCreated = true;
                }
            }
            else
            {
                dst = Create(src.StaticActor);
                /// If we are Enabled, then this is during runtime, and
                /// the only non-creatable things getting created have
                /// no brains programmed. But if we are enabled, we may 
                /// be cloning a programmed bot, in which case we need 
                /// to clone it's brain.
                /// The perfect test would be something like:
                ///    if(src.Brain.NotEmpty)
                ///       dst.Brain = Brain.DeepCopy(src.Brain);
                /// But we don't currently have such a test. This is close
                /// enough because at runsim we know we don't need a brain copy,
                /// and at edit time we don't care about a little superfluous GC.
                if (!Enabled)
                {
                    dst.Brain = Brain.DeepCopy(src.Brain);
                    dst.Brain.Wipe();
                }
            }

            // May have user defined name.  Copy it.
            dst.DisplayName = src.DisplayName;

            return dst;
        }

        /// <summary>
        /// Return the actor to the factory. Returns true if the actor was accepted back into the factory.
        /// </summary>
        /// <param name="actor"></param>
        /// <returns></returns>
        public static bool Recycle(GameActor actor)
        {
            if (actor == null)
                return false;

            Debug.Assert(actor.CurrentState == GameThing.State.Inactive);

            // Due to complicated issues including cut/paste, we only recycle deactivated objects during simulation run.
            if (!Enabled)
            {
                return false;
            }

            // We may not recycle creatable masters. Though they are inactive, they are held in the creatable list during gameplay.
            if (actor.Creatable)
            {
                return false;
            }

            /// Try to recycle it as a creatable clone.
            if (RecycleCreatable(actor))
            {
                return true;
            }

            Stack<GameActor> freeActors;

            if (!recycleBin.TryGetValue(actor.StaticActor, out freeActors))
            {
                freeActors = new Stack<GameActor>();
                recycleBin.Add(actor.StaticActor, freeActors);
            }

            Debug.Assert(!freeActors.Contains(actor));
            freeActors.Push(actor);

            actor.InitDefaults(false);

            /// MF_BRAIN_TRASH
            /// If we made it here, then this actor is not a creatable, but it might have a brain.
            /// We'd like to give it an empty brain if it has a non-empty brain, else leave it
            /// alone.
            if (!actor.Brain.IsEmpty)
            {
                actor.Brain = Brain.CreateEmpty();
            }
            actor.InRecycleBin = true;

            actor.Version = GameThing.CurrentVersion;

            return true;
        }

        /// <summary>
        /// Sync this changed brain down to any creatable clones currently
        /// in recycle storage.
        /// </summary>
        /// <param name="creatableId"></param>
        /// <param name="copy"></param>
        public static void CopyBrains(Guid creatableId, MemoryStream memStream, XmlSerializer serializer)
        {
            Stack<GameActor> freeActors = null;

            if (creatableBin.TryGetValue(creatableId, out freeActors)
                && (freeActors != null))
            {
                foreach (GameActor curr in freeActors)
                {
                    memStream.Position = 0;
                    curr.Brain = (Brain)serializer.Deserialize(memStream);
                }
            }
        }

        /// <summary>
        /// If this actor is a creatable, bin him up by creatableId.
        /// </summary>
        /// <param name="actor"></param>
        /// <returns>true if binned by ID, else false</returns>
        private static bool RecycleCreatable(GameActor actor)
        {
            if (actor.CreatableId != Guid.Empty)
            {
                Stack<GameActor> freeActors = null;
                if (!creatableBin.TryGetValue(actor.CreatableId, out freeActors))
                {
                    freeActors = new Stack<GameActor>();
                    creatableBin.Add(actor.CreatableId, freeActors);
                }

                freeActors.Push(actor);

                /// Stash away the creatable ID, it will get reset
                /// in InitDefaults.
                Guid creatableId = actor.CreatableId;

                actor.InitDefaults(false);
                actor.Brain.Reset(0, true, true);
                actor.InRecycleBin = true;
                actor.CreatableId = creatableId;

                actor.Version = GameThing.CurrentVersion;

                return true;
            }
            return false;
        }

    }
}
