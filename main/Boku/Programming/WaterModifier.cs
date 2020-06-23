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
    /// Represents a water type.
    /// </summary>
    public class WaterModifier : Modifier
    {
        public override ProgrammingElement Clone()
        {
            WaterModifier clone = new WaterModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(WaterModifier clone)
        {
            base.CopyTo(clone);
        }

    }
}
