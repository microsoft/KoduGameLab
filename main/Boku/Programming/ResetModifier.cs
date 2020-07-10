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


namespace Boku.Programming
{
    public class ResetModifier : Modifier
    {
        [Flags]
        public enum Resets
        {
            None = 0 << 0,
            World = 1 << 0,
            Score = 1 << 1,
            Expression = 1 << 2,
            Glow = 1 << 3,
            Health = 1 << 4
        }

        [XmlAttribute]
        public Resets reset;


        public override ProgrammingElement Clone()
        {
            ResetModifier clone = new ResetModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(ResetModifier clone)
        {
            base.CopyTo(clone);
            clone.reset = this.reset;
        }

        public override void GatherParams(ModifierParams param)
        {
            param.Reset |= this.reset;
        }

    }
}
