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
    /// this modifier acts like a parameter and provides the object type to the actuator
    /// </summary>
    public class ObjectModifier : Modifier
    {
        public enum ModifierObjects
        {
            None,
            InkJet,
            IceBerg,
            Rock,
            RockLowValue,
            RockHighValue,
            Fruit,
            Star,
            Coin,
            Heart,
            Ammo,
            SoccerBall,
        }
        [XmlAttribute]
        public ModifierObjects item;

        
        public override ProgrammingElement Clone()
        {
            ObjectModifier clone = new ObjectModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(ObjectModifier clone)
        {
            base.CopyTo(clone);
            clone.item = this.item;
        }

        public override void GatherParams(ModifierParams param)
        {
            if (!param.HasItem)
                param.Item = this.item;
        }

    }
}
