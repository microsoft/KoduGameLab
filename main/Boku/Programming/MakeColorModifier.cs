
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
    public class MakeColorModifier : Modifier
    {
        [XmlAttribute]
        public Classification.Colors color;

        public override ProgrammingElement Clone()
        {
            MakeColorModifier clone = new MakeColorModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(MakeColorModifier clone)
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
