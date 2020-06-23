#region USING STATEMENTS
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
#endregion


namespace Boku.Programming
{
    public class PronounModifier : Modifier
    {
        public enum Pronouns
        {
            None,
            It,
            Me
        }

        [XmlAttribute]
        public Pronouns Pronoun;

        public override ProgrammingElement Clone()
        {
            PronounModifier clone = new PronounModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(PronounModifier clone)
        {
            base.CopyTo(clone);
            clone.Pronoun = this.Pronoun;
        }

        public override void GatherParams(ModifierParams param)
        {
            if (!param.HasPronoun)
                param.Pronoun = this.Pronoun;
        }

    }
}
