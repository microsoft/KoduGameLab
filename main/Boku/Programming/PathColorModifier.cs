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
    /// this modifier acts like a parameter and provides a color for the selector or actuator
    /// </summary>
    public class PathColorModifier : Modifier
    {
        [XmlAttribute]
        public Classification.Colors color;


        public override ProgrammingElement Clone()
        {
            PathColorModifier clone = new PathColorModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(PathColorModifier clone)
        {
            base.CopyTo(clone);
            clone.color = this.color;
        }

        public override void GatherParams(ModifierParams param)
        {
            if (!param.HasColor)
                param.Color = this.color;
        }

    }
}
