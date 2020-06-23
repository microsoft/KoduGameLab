
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
    public class PlayerModifier : Modifier
    {
        [XmlAttribute]
        public GamePadSensor.PlayerId playerIndex;

        public override ProgrammingElement Clone()
        {
            PlayerModifier clone = new PlayerModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(PlayerModifier clone)
        {
            base.CopyTo(clone);
            clone.playerIndex = this.playerIndex;
        }

        public override void GatherParams(ModifierParams param)
        {
            if (!param.HasPlayerIndex)
                param.PlayerIndex = this.playerIndex;
        }

    }
}
