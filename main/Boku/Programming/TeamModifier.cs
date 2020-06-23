
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
    /// this modifier acts like a parameter and provides the team id to the actuator
    /// </summary>
    public class TeamModifier : Modifier
    {
        public enum Team
        {
            Dynamic, // used by the actuator to signify to use this modifier
            A,
            B,
        }

        [XmlIgnore]
        public Team team;
        // NOTE: the above was listed with XmlAttribute instead of XmlIgnore; but the
        // XML serializer had problems with it and would fail with no real indication why
        // many purmutations were tried to work around the issue but adding the below seems to be
        // the only real solution at the time
        //
        [XmlAttribute]
        public string TeamId
        {
            get
            {
                return this.team.ToString();
            }
            set
            {
                this.team = (Team)Enum.Parse(typeof(Team), value, true);
            }
        }

        public override ProgrammingElement Clone()
        {
            TeamModifier clone = new TeamModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(TeamModifier clone)
        {
            base.CopyTo(clone);
            clone.team = this.team;
        }

        public override bool ProvideParam(out object param)
        {
            param = this.team;
            return true;
        }

    }
}
