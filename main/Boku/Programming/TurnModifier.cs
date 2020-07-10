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
    public class TurnModifier : Modifier
    {
        public enum TurnDirections
        {
            None,
            Left,
            Right,
            Toward,
            Forward
        }

        [XmlAttribute]
        public TurnDirections direction;

        public override ProgrammingElement Clone()
        {
            TurnModifier clone = new TurnModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(TurnModifier clone)
        {
            base.CopyTo(clone);
            clone.direction = this.direction;
        }

        public override void GatherParams(ModifierParams param)
        {
            if (!param.HasTurn)
                param.Turn = this.direction;
        }

        public override bool ModifyHeading(Reflex reflex, GameActor gameActor, ref Vector3 heading)
        {
            switch (direction)
            {
                case TurnDirections.Left:
                    heading.X = -Math.Abs(heading.X);
                    break;

                case TurnDirections.Right:
                    heading.X = Math.Abs(heading.X);
                    break;

                case TurnDirections.Toward:
                    // This case is handled by the TurnSelector because we don't have enough information here.
                    break;

                case TurnDirections.Forward:
                    // This case is handled by the TurnSelector because we don't have enough information here.
                    break;
            }

            return true;
        }

    }
}
