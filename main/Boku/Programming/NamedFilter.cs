// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Diagnostics;

using System.Xml;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;

namespace Boku.Programming
{
    /// <summary>
    /// Allows filtering based on user-defined names.
    /// </summary>
    public class NamedFilter : Filter
    {
        string name;
        string nonLocalizedName;

        /// <summary>
        /// The name as defined by the user.
        /// </summary>
        [XmlAttribute]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// The non-localized name of the actor.  This is
        /// use to get the correct texture for the tile.
        /// </summary>
        [XmlAttribute]
        public string NonLocalizedName
        {
            get { return nonLocalizedName; }
            set { nonLocalizedName = value; }
        }

        public NamedFilter(string name, string nonLocalizedName)
        {
            this.name = name;
            this.nonLocalizedName = nonLocalizedName;

            // Note that we use both names in the upid.  This allows us to
            // recreate the filters at level load time even though we don't
            // have the actors yet.
            upid = "filter.named." + name + "." + nonLocalizedName;
            label = name;

            description = CardSpace.Localize("filter.named") + " " + name;

            // NamedFilters are not read from CardSpace.xml, so we must assign initialize these fields manually.
            icon = "image_missing";
            group = "group.named";
            helpGroups = new string[] { "objects", "bots" };
            groupObj = CardSpace.Cards.GetGroup(group);

            XmlInputs.Add(SensorOutputType.ActorSet);

            XmlNegOutputs.Add(SensorOutputType.TerrainMaterial);
            XmlNegOutputs.Add(SensorOutputType.WaterMaterial);
            XmlNegOutputs.Add(SensorOutputType.TouchButton);
            XmlNegOutputs.Add(SensorOutputType.Real);

            XmlCategories.Add(BrainCategories.NamedFilter);

            XmlInclusions.Add(BrainCategories.ExplicitSubject);

            XmlExclusions.Add(BrainCategories.ObjectFilter);

            OnLoad();

        }   // end of c'tor

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            bool matched = false;

            if (sensorTarget.GameThing is NullActor)
            {
                return matched; // Always false.
            }

            GameActor actor = sensorTarget.GameThing as GameActor;
            if(actor != null && actor.DisplayName == name)
            {
                matched = true;
            }

            return matched;
        }   // end of MatchTarget()

        public override bool MatchAction(Reflex reflex, out object param)
        {
            param = null;
            return true;
        }

        public override ProgrammingElement Clone()
        {
            NamedFilter clone = new NamedFilter(Name, NonLocalizedName);
            CopyTo(clone);
            return clone;
        }

        static public void RegisterAllNamedFiltersInCardSpace()
        {
            foreach (GameThing thing in InGame.inGame.gameThingList)
            {
                GameActor actor = thing as GameActor;
                if (actor != null && actor.IsUserNamed)
                {
                    RegisterInCardSpace(actor);
                }
            }
        }   // end of RegisterAllNamedFiltersInCardSpace()

        static public void UnregisterAllNamedFiltersInCardSpace()
        {
            // We know that the NamedFilters are at the end of the list
            // so we can use this to be a bit smarter about how we 
            // remove them.
            int count = CardSpace.Cards.FilterDict.Keys.Count;
            string[] keys = new string[count];
            CardSpace.Cards.FilterDict.Keys.CopyTo(keys, 0);
            int i = count - 1;
            while (keys[i].StartsWith("filter.named."))
            {
                CardSpace.Cards.FilterDict.Remove(keys[i]);
                --i;
            }

        }   // end of UnregisterAllNamedFiltersInCardSpace()


        static public NamedFilter RegisterInCardSpace(GameActor actor)
        {
            return RegisterInCardSpace(actor.DisplayName, actor.StaticActor.NonLocalizedName);
        }   // RegisterInCardSpace()

        static public NamedFilter RegisterInCardSpace(string userDefinedName, string nonLocalizedName)
        {
            // Do we already have a filter with this name?
            // If so, don't try and add a new one.
            string filterUPID = "filter.named." + userDefinedName + "." + nonLocalizedName;
            Filter f;
            CardSpace.Cards.FilterDict.TryGetValue(filterUPID, out f);
            if (f != null)
            {
                return f as NamedFilter;
            }

            NamedFilter namedFilter = new NamedFilter(userDefinedName, nonLocalizedName);
            CardSpace.Cards.FilterDict.Add(namedFilter.upid, namedFilter);

            // Look for a filter tile matching this actor's default name. If found, use its icon for this creatable tile.
            StaticActor actor = ActorManager.GetActor(nonLocalizedName);
            string filterName = actor.MenuTextureFile;
            string iconName;
            Filter filter = CardSpace.Cards.GetFilter(filterName);
            if (filter != null)
            {
                iconName = filter.TextureName;
            }
            else
            {
                iconName = namedFilter.icon;
            }

            CardSpace.Cards.CacheCardFace(namedFilter.upid, iconName, namedFilter.label, namedFilter.noLabelIcon);

            return namedFilter;

        }   // end of RegisterInCardSpace()

    }   // end of class NamedFilter

}   // end of namespace Boku.Programming
