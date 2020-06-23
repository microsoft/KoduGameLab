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
    /// Represents a specific projectile type.
    /// </summary>
    public class ProjectileModifier : Modifier
    {
        public enum ProjectileType
        {
            None,
            Missile,
            Blip,
        }

        [XmlAttribute]
        public ProjectileType projectile;


        public override ProgrammingElement Clone()
        {
            ProjectileModifier clone = new ProjectileModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(ProjectileModifier clone)
        {
            base.CopyTo(clone);
            clone.projectile = this.projectile;
        }

    }
}
