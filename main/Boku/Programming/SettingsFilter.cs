// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
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
using Boku.Input;

namespace Boku.Programming
{
    /// <summary>
    /// Filter that just exists as a placeholder.  Actual value is 
    /// extracted when scores are accumulated.
    /// 
    /// Used for MaxHealth, BlipDamage, MissileDamage... (so far)
    /// 
    /// </summary>
    public class SettingsFilter : Filter
    {
        [XmlIgnore]
        public string name = null;  // Filled during OnLoad() in Cardspace.

        public override ProgrammingElement Clone()
        {
            SettingsFilter clone = new SettingsFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(SettingsFilter clone)
        {
            base.CopyTo(clone);
            clone.name = name;
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            return false;
        }

        public override bool MatchAction(Reflex reflex, out object param)
        {
            param = null;
            return true;
        }

    }   // end of class SettingsFilter

}   // end of namespace Boku.Programming
