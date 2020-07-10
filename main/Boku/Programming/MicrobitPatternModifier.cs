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
    public class MicrobitPatternModifier : Modifier, IMicrobitTile
    {
        public override ProgrammingElement Clone()
        {
            MicrobitPatternModifier clone = new MicrobitPatternModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(MicrobitPatternModifier clone)
        {
            base.CopyTo(clone);
        }

        public override void GatherParams(ModifierParams param)
        {
        }

    }
}
