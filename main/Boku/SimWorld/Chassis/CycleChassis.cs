// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Microsoft.Xna.Framework;

using KoiX;

using Boku.Audio;
using Boku.Base;
using Boku.Common;
using Boku.Common.ParticleSystem;
using Boku.SimWorld;
using Boku.SimWorld.Terra;
using Boku.Animatics;

namespace Boku.SimWorld.Chassis
{
    /// <summary>
    /// Chassis used for single wheeled bots, e.g. the FastBot.
    /// </summary>
    public class CycleChassis : VehicleChassis
    {
        #region Members
        private const float DefaultLeanRatio = 0.4f;
        private const float DefaultMaxLean = 1.0f;

        private float leanRatio = DefaultLeanRatio;         // Ratio between lean and rotation rate.
        private float maxLean = DefaultMaxLean;             // Limit for lean angle.

        private float lean = 0.0f;                          // Rotation around the X axis.

        #endregion

        #region Public

        public CycleChassis()
            : base()
        {

        }

        /// <summary>
        /// Resets member field that change during runtime back to their initial default values
        /// as they were before customizations applied by a specific actor.
        /// </summary>
        public override void InitDefaults()
        {
            base.InitDefaults();

            // values here copied from member initializers above.
            lean = 0.0f;
        }
        #endregion

        #region Internal

        /// <summary>
        /// Allow subclasses to handle special logic for adjusting based on terrain
        /// </summary>
        /// <param name="movement"></param>
        protected override void HandleMovement(Movement movement)
        {
            lean = MyMath.Lerp(lean, -leanRatio * movement.RotationZRate * movement.Speed / (MaxSpeed * TurningSpeedModifier), 0.1f);
            lean = MathHelper.Clamp(lean, -maxLean, maxLean);
        }

        /// <summary>
        /// Allow subclasses to handle special logic for generating rotation of the object
        /// </summary>
        /// <param name="movement"></param>
        protected override Matrix GenerateRotationMatrix(Movement movement)
        {
            // Create the local matrix.
            Matrix local = Matrix.CreateRotationX(lean) *
                            Matrix.CreateRotationZ(movement.RotationZ);

            //rotates in direction we're facing + accounts for lean based on terrain
            return local;
        }
        #endregion Internal

    }   // end of class CycleChassis

}   // end of namespace Boku.SimWorld.Chassis
