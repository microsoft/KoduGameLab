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
using Boku.Input;

namespace Boku.Programming
{
    /// <summary>
    /// Used as a Parameter Filter used by the GamePadSensor
    /// 
    /// </summary>
    public class PlayerFilter : Filter
    {
        [XmlAttribute]
        public GamePadSensor.PlayerId playerIndex;

        public override ProgrammingElement Clone()
        {
            PlayerFilter clone = new PlayerFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(PlayerFilter clone)
        {
            base.CopyTo(clone);
            clone.playerIndex = this.playerIndex;
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            return false;
        }

        public override bool MatchAction(Reflex reflex, out object param)
        {
            param = this.playerIndex;
            return true;
        }

    }
}
