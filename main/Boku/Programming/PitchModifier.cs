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
    public class PitchModifier : Modifier
    {
        public enum PitchDirections
        {
            None,
            Up,
            Down
        }

        [XmlAttribute]
        public PitchDirections direction;

        public override ProgrammingElement Clone()
        {
            PitchModifier clone = new PitchModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(PitchModifier clone)
        {
            base.CopyTo(clone);
            clone.direction = this.direction;
        }

        public override void GatherParams(ModifierParams param)
        {
            if (!param.HasPitch)
                param.Pitch = this.direction;
        }

        public override bool ModifyHeading(Reflex reflex, GameActor gameActor, ref Vector3 heading)
        {
            switch (direction)
            {
                case PitchDirections.Up:
                    heading.Y = Math.Abs(heading.Y);
                    break;

                case PitchDirections.Down:
                    heading.Y = -Math.Abs(heading.Y);
                    break;
            }

            return true;
        }
    }
}
