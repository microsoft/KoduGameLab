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
    /// This modifier will limit movement along a vector in either direction.
    /// </summary>
    public class ConstraintModifier : Modifier
    {
        [Flags]
        public enum Constraints
        {
            None = 0 << 0,
            Immobile = 1 << 0,
            NorthSouth = 1 << 1,
            EastWest = 1 << 2,
            UpDown = 1 << 3,
        }

        [XmlAttribute]
        public Constraints ConstraintType;

        private Vector3 initialPosition;


        public override ProgrammingElement Clone()
        {
            ConstraintModifier clone = new ConstraintModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(ConstraintModifier clone)
        {
            base.CopyTo(clone);
            clone.ConstraintType = this.ConstraintType;
        }

        public override void Reset(Reflex reflex)
        {
            InitializeFrom(reflex.Task.Brain.GameActor);
        }

        public override void GatherParams(ModifierParams param)
        {
            param.Constraints |= this.ConstraintType;
        }

        internal void InitializeFrom(GameActor actor)
        {
            initialPosition = actor.Movement.Position;
        }

        internal void Constrain(Reflex reflex)
        {
            GameActor actor = reflex.Task.GameActor;

            // Only apply the immobile constraint if the reflex fired. This allows the user
            // to stop the bot based on sensor conditions, such as "gamepad A - move freeze".
            if (reflex.targetSet.AnyAction && 0 != (ConstraintType & Constraints.Immobile))
            {
                actor.Chassis.Constraints |= ConstraintType;
                actor.Chassis.ImmobilePosition = actor.Movement.Position;
            }
            // Always apply other constraint types. This is so that even when the
            // reflex doesn't fire, the constraint will still be applied so that
            // when the bot is bumped by another, it will remain clamped to the
            // constraint axis. If constrain were a top-level verb, we could express
            // this behavior better by saying: "always - limit N/S"
            else if (0 == (ConstraintType & Constraints.Immobile))
            {
                actor.Chassis.Constraints |= ConstraintType;
                actor.Chassis.ConstraintInitialPosition = initialPosition;
            }
        }
    }
}
