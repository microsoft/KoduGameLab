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
    /// this modifier acts like a parameter and provides a Verb to be used 
    /// as a payload to the actuator
    /// </summary>
    public class PayloadVerbModifier : Modifier
    {
        [XmlAttribute]
        public GameThing.Verbs Verb;

        public override ProgrammingElement Clone()
        {
            PayloadVerbModifier clone = new PayloadVerbModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(PayloadVerbModifier clone)
        {
            base.CopyTo(clone);
            clone.Verb = this.Verb;
        }

        public override void GatherParams(ModifierParams param)
        {
            if (!param.HasVerb)
                param.Verb = this.Verb;
        }

    }
}
