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
using Boku.SimWorld;
using Boku.SimWorld.Path;
using Boku.SimWorld.Terra;
using Boku.Common;
using Boku.Programming;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.Fx;

namespace Boku
{
    using CreatableMap = Dictionary<Guid, GameActor>;
    
    /// <summary>
    /// This chunk of the InGame class is responsible for managing the list of creatables.
    /// </summary>
    public partial class InGame : GameObject, INeedsDeviceReset
    {
        #region Private members
        private CreatableMap creatables = new CreatableMap();
        #endregion

        #region Public Methods
        public void NewCreatable(GameActor actor)
        {
            actor.CreatableId = Guid.NewGuid();
            RegisterCreatable(actor);
        }

        public void RegisterCreatable(GameActor actor)
        {
            Debug.Assert(actor.CreatableId != null && actor.CreatableId != Guid.Empty && actor.Creatable);

            if (!creatables.ContainsKey(actor.CreatableId))
                creatables.Add(actor.CreatableId, actor);

            RegisterCardSpace(actor);
            UnRegisterCollide(actor);

            if (actor.CreatableAura == null)
            {
                actor.CreatableAura = MakeAura(actor);
            }
        }

        public void UnregisterCreatable(GameActor actor)
        {
            // Let callers blindly cast object to GameActor, we'll worry about whether it wasn't one.
            if (actor == null)
                return;

            Guid creatableId = actor.CreatableId;

            /// Purge any recyclable copies we have lying around.
            ActorFactory.ClearCreatable(creatableId);

            GameActor creatable;
            if (!creatables.TryGetValue(creatableId, out creatable))
                return;

            // Turn clones of this creatable into individuals.
            List<GameActor> cloneList = new List<GameActor>();
            GetClones(creatableId, cloneList);
            foreach (GameActor curr in cloneList)
            {
                curr.CreatableId = Guid.Empty;
                curr.ClearCount();
            }

            // Remove the actor from the set of creatables.
            creatables.Remove(creatableId);
            actor.CreatableId = Guid.Empty;
            actor.CreatableAura = null;
            RegisterCollide(actor);

            // Remove from create menu
            UnregisterCardSpace(creatableId);
        }

        public void UnregisterAllCreatables()
        {
            GameActor[] actors = new GameActor[creatables.Values.Count];
            creatables.Values.CopyTo(actors, 0);
            for (int i = 0; i < actors.Length; ++i)
                UnregisterCreatable(actors[i]);

            /// Purge any creatable recyclables we have lying around.
            ActorFactory.ClearCreatables();
        }

        public bool IsCreatableRegistered(Guid creatableId)
        {
            return creatables.ContainsKey(creatableId);
        }

        public void GetClones(Guid creatableId, List<GameActor> cloneList)
        {
            foreach (object obj in gameThingList)
            {
                GameActor curr = obj as GameActor;

                if (curr == null)
                    continue;

                if (curr.Creatable)
                    continue;

                if (curr.CreatableId != creatableId)
                    continue;

                cloneList.Add(curr);
            }
        }

        public GameActor GetCreatable(Guid creatableId)
        {
            GameActor creatable;
            if (!creatables.TryGetValue(creatableId, out creatable))
                return null;
            return creatable;
        }

        public void AddCreatablesToScene()
        {
            CreatableMap.Enumerator iter = creatables.GetEnumerator();

            while (iter.MoveNext())
                AddCreatableToScene(iter.Current.Value);
        }

        public void RemoveCreatablesFromScene()
        {
            CreatableMap.Enumerator iter = creatables.GetEnumerator();

            while (iter.MoveNext())
                RemoveCreatableFromScene(iter.Current.Value);
        }
        #endregion

        #region Private Methods
        private bool CreatableLinesNeeded
        {
            get
            {
                return (currentUpdateMode == UpdateMode.EditObject)
                || (currentUpdateMode == UpdateMode.EditObjectParameters)
                || (currentUpdateMode == UpdateMode.TweakObject)
                || (currentUpdateMode == UpdateMode.ToolMenu);
            }
        }
        private void RegisterCardSpace(GameActor actor)
        {
            UnregisterCardSpace(actor.CreatableId);
            CreatableModifier modifier = new CreatableModifier(actor.CreatableId, actor.DisplayNameNumber);
            CardSpace.Cards.ModifierDict.Add(modifier.upid, modifier);

            // Look for a filter tile matching this actor type. If found, use its icon for this creatable tile.
            string filterName = actor.StaticActor.MenuTextureFile;
            string iconName;
            Filter filter = CardSpace.Cards.GetFilter(filterName);
            if (filter != null)
                iconName = filter.TextureName;
            else
                iconName = modifier.icon;

            CardSpace.Cards.CacheCardFace(modifier.upid, iconName, modifier.label, modifier.noLabelIcon);
        }

        private void RegisterPlaceholderCardSpace(Guid creatableId)
        {
            UnregisterCardSpace(creatableId);
            CreatableModifier modifier = new CreatableModifier(creatableId, "placeholder");
            CardSpace.Cards.ModifierDict.Add(modifier.upid, modifier);
            CardSpace.Cards.CacheCardFace(modifier.upid, modifier.icon, modifier.label, modifier.noLabelIcon);
        }
        
        private void UnregisterCardSpace(Guid creatableId)
        {
            Modifier existing = CardSpace.Cards.RemoveModifier("modifier.creatable." + creatableId.ToString());
            if (existing != null)
            {
                CardSpace.Cards.UncacheCardFace(existing.upid);
            }
        }

        private void AddCreatableToScene(GameActor actor)
        {
            if (!gameThingList.Contains(actor))
                actor = (GameActor)AddThing(actor);

            if (actor != null)
            {
                if (actor.CreatableAura == null)
                {
                    actor.CreatableAura = MakeAura(actor); // may be null.
                }

                actor.Pause();
            }
        }

        private void RemoveCreatableFromScene(GameActor actor)
        {
            // Will remove itself from gameThingList when deactivated in Refresh method.
            actor.Deactivate();
            actor.CreatableAura = null;
        }

        private void ClearCreatables()
        {
            RemoveCreatablesFromScene();
            creatables.Clear();
        }
        #endregion

    }
}
