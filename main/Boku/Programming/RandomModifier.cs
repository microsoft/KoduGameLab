
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
    /// this modifier acts like a parameter and provides a score amount to the actuator
    /// </summary>
    public class RandomModifier : Modifier
    {
        public override ProgrammingElement Clone()
        {
            RandomModifier clone = new RandomModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(RandomModifier clone)
        {
            base.CopyTo(clone);
        }

    }
}
