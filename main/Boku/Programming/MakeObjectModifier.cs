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
    public class MakeObjectModifier : Modifier
    {
        public enum MakeObjects
        {
            None,
            IceBerg,
            Rock,
            RockLowValue,
            RockHighValue,
            Fruit,
            RedFruit, // depricated
            GreenFruit, // depricated
        }
        [XmlAttribute]
        public MakeObjects item;

        public override ProgrammingElement Clone()
        {
            MakeObjectModifier clone = new MakeObjectModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(MakeObjectModifier clone)
        {
            base.CopyTo(clone);
            clone.item = this.item;
        }

        public override void GatherParams(ModifierParams param)
        {
            if (!param.HasMake)
                param.Make = this.item;
        }

    }
}
