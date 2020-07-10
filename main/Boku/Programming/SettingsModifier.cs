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
    /// modifier that just exists as a placeholder.  Actual value is 
    /// extracted when scores are accumulated.
    /// 
    /// Used for MaxHealth, BlipDamage, MissileDamage... (so far)
    /// </summary>
    public class SettingsModifier : Modifier
    {
        [XmlAttribute]
        public int points;

        [XmlIgnore]
        public string name = null;  // Filled during OnLoad() in Cardspace.

        public override ProgrammingElement Clone()
        {
            SettingsModifier clone = new SettingsModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(SettingsModifier clone)
        {
            base.CopyTo(clone);
            clone.points = this.points;
            clone.name = name;
        }

        public override void GatherParams(ModifierParams param)
        {
            // WTF?  HasPoints is hard-coded to always return true.  Why does it do this and what was it meant to do?
            if (!param.HasPoints)
            {
                param.Points = 0;
            }
        }

    }   // end of class SettingsModifier

}   // end of namespace Boku.Programming
