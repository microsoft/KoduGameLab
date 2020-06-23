using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

using Boku.Base;

namespace Boku.Common
{
    /// <summary>
    /// Override class that doesn't have to handle motion. It only moves
    /// when moved by the editor.
    /// </summary>
    public class FixedMovement : Movement
    {

        #region Public
        /// <summary>
        /// Position at the beginning of the frame.
        /// </summary>
        public override Vector3 PrevPosition
        {
            get { return Position; }
        }
        /// <summary>
        /// Velocity at the beginning of the frame.
        /// </summary>
        public override Vector3 PrevVelocity
        {
            get { return Velocity; }
        }
        /// <summary>
        /// Facing direciton at the beginning of the frame.
        /// </summary>
        public override Vector3 PrevFacingDirection
        {
            get { return Facing; }
        }

        public override void SetPreviousPositionVelocity()
        {
        }

        public FixedMovement(GameThing parent)
            : base(parent)
        {
        }

        #endregion Public
    }
}
