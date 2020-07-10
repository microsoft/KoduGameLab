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
    public class CreatableModifier : Modifier
    {
        private Guid creatableId;

        [XmlAttribute]
        public Guid CreatableId
        {
            get { return creatableId; }
            set { creatableId = value; }
        }

        public CreatableModifier(Guid creatableId, string label)
        {
            this.CreatableId = creatableId;
            this.upid = "modifier.creatable." + creatableId.ToString();
            this.label = label;

            // CreatableModifiers are not read from CardSpace.xml, so we must assign initialize these fields manually.
            this.icon = "image_missing";
            this.group = "group.creatables";
            this.helpGroups = new string[] { "objects", "bots" };
            this.groupObj = CardSpace.Cards.GetGroup(this.group);

            this.XmlCategories.Add(BrainCategories.CreatableModifier);
            this.XmlInclusions.Add(BrainCategories.DoCreate);
            this.XmlInclusions.Add(BrainCategories.DoLaunch);
            this.XmlExclusions.Add(BrainCategories.ObjectModifier);
            this.OnLoad();
        }

        public override ProgrammingElement Clone()
        {
            CreatableModifier clone = new CreatableModifier(CreatableId, label);
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(CreatableModifier clone)
        {
            base.CopyTo(clone);
            // do not assign creatableId field here. it is passed in the constructor.
        }

        public override void GatherParams(ModifierParams param)
        {
            if (!param.HasCreatableId)
                param.CreatableId = CreatableId;
        }
    }
}
