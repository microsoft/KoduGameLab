// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
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
using Boku.SimWorld.Chassis;

namespace Boku.Programming
{
    public enum Directions
    {
        None = 0 << 0,
        Forward = 1 << 0,
        North = 1 << 1,
        East = 1 << 2,
        South = 1 << 3,
        West = 1 << 4,
        Up = 1 << 5,
        Down = 1 << 6,
        Clockwise = 1 << 7,
        Counterclockwise = 1 << 8,
        NorthWest = North | West,
        NorthEast = North | East,
        SouthWest = South | West,
        SouthEast = South | East,
        Exclusive = Forward | Up | Down,
        Cardinal = North | South | East | West,
        Vertical = Up | Down,
        Rotation = Clockwise | Counterclockwise
    }

    /// <summary>
    /// this modifier both acts like a parameter providing a fixed direction 
    /// for the selector or actuator; and modifiers the heading to be the direction.
    /// </summary>
    public class DirectionModifier : Modifier
    {
        [XmlAttribute]
        public Directions direction;

        public static Vector3 GetUnitDirection(Directions direction, GameActor actor)
        {
            Vector3 unit = Vector3.Zero;

            if ((direction & Directions.North) != 0)
                    unit += Vector3.UnitY;
            if ((direction & Directions.South) != 0)
                    unit -= Vector3.UnitY;
            if ((direction & Directions.East) != 0)
                    unit += Vector3.UnitX;
            if ((direction & Directions.West) != 0)
                    unit -= Vector3.UnitX;
            if ((direction & Directions.Up) != 0)
                    unit += Vector3.UnitZ;
            if ((direction & Directions.Down) != 0)
                    unit -= Vector3.UnitZ;
            if (actor != null && (direction & Directions.Forward) != 0)
                    unit += actor.Movement.Facing;

            if (unit != Vector3.Zero)
                unit.Normalize();

            return unit;
        }

        public override ProgrammingElement Clone()
        {
            DirectionModifier clone = new DirectionModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(DirectionModifier clone)
        {
            base.CopyTo(clone);
            clone.direction = this.direction;
        }

        public override void GatherParams(ModifierParams param)
        {
            param.Direction |= this.direction;
        }

        public override bool ModifyHeading(Reflex reflex, GameActor gameActor, ref Vector3 heading)
        {
            float speed = heading.Length();
            if (speed > 0f)
                heading.Normalize();
            Vector3 dir = GetUnitDirection(direction, gameActor);
            heading += dir;
            if (speed > 0f)
                heading *= speed;

            return true;
        }
    }
}
