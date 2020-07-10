// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Microsoft.Xna.Framework;

using KoiX;

using Boku.Base;
using Boku.Common;
using Boku.SimWorld;
using Boku.SimWorld.Terra;
using Boku.Animatics;

namespace Boku.SimWorld.Chassis
{
    /// <summary>
    /// Chassis used for things that sit and spin, e.g. StickBoy.
    /// </summary>
    public class SitAndSpinChassis : BaseChassis
    {
        public const float DefaultMaxRotationalAcceleration = 0.1f;
        public const float DefaultMaxRotationRate = 1.0f;

        #region Accessors

        public override bool SupportsStrafing { get { return false; } }

        #endregion

        #region Public

        public SitAndSpinChassis()
        {
            fixedPosition = true;
            MaxRotationalAcceleration = DefaultMaxRotationalAcceleration;   // radians / sec^2
            MaxRotationRate = DefaultMaxRotationRate;                       // radians / sec around Z axis
        }

        public override void PreCollisionTestUpdate(GameThing thing)
        {
            Movement movement = thing.Movement;
            movement.Velocity = Vector3.Zero;

            GameActor.State state = thing.CurrentState;

            float secs = Time.GameTimeFrameSeconds;

            ApplyDesiredRotation(movement, Parent.DesiredMovement);

            // Adjust actor's height to follow the terrain.  This doesn't seem 
            // like it should need doing but collisions can change the height 
            // when we don't want it changed.
            movement.Altitude = MyMath.Lerp(movement.Altitude, EditHeight + Terrain.GetTerrainAndPathHeight(movement.Position), 0.1f * 30.0f * secs);

            Moving = false;
        }   // end of PreCollisionTestUpdate()

        public override void SetLoopedAnimationWeights(AnimationSet anims, Movement movement, DesiredMovement desiredMovement)
        {
            anims.IdleWeight = anims.IsOpen ? 1.0f : 0.0f;
            anims.ForwardWeight = 0.0f;
            anims.BackwardsWeight = 0.0f;
            anims.RightWeight = 0.0f;
            anims.LeftWeight = 0.0f;
        }   // end of SetLoopedAnimationWeights()

        #endregion

    }   // end of class SitAndSpinChassis

}   // end of namespace Boku.SimWorld.Chassis
