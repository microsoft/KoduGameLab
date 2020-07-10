// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Microsoft.Xna.Framework;

using Boku.Base;

namespace Boku.Common
{
    /// <summary>
    /// This class is the connection point between brain actions and those
    /// actions being applied by the chassis to doing actual movement.
    /// 
    /// Each value in this class has a nullable type which allows you to tell 
    /// whether or not we have a valid value.  For instance, if a character
    /// is just turning, then the velocity and target location values will
    /// not be valid since they are not being used.
    /// 
    /// In addition to setting the desired values, the brain also need to set
    /// the max speeds and accelerations.  These will be based on the actor's
    /// base values and modified by any quickly/slowly modifiers.  When doing
    /// movement updates, the chassis should only look at these values, not 
    /// the base actor values.
    /// 
    /// Note that the only way to set any of these values is through the Set*()
    /// functions.  This is deliberate since it forces you set set the related
    /// speed and acceleration values when giving a target value.  Don't cheat
    /// or try to work around this.  Do the right thing.
    /// </summary>
    public class DesiredMovement
    {
        #region Members

        // Velocity and Target Location should be mutually exclusive.
        Vector3? desiredVelocity = null;
        Vector3? desiredTargetLocation = null;

        // If true, when responding to a velocity command, the actor
        // will also turn to face the direction of acceleration.
        bool autoTurn = false;

        // Rotation Rate and Rotation Angle should be mutually exclusive.
        float? desiredRotationRateZ = null;
        float? desiredRotationAngleZ = null;

        // Vertical speed and altitude should be mutually exclusive.
        float? desiredVerticalSpeed = null;
        float? desiredAltitude = null;

        Vector3? externalForce = null;

        // List of actors we want to avoid while moving.
        List<GameActor> avoidTargets = new List<GameActor>();

        // Max speed and acceleration values based on actor plus any
        // modifiers on the reflex.  These should always be non-negative
        // numbers even though desired speeds may be negative.
        float maxSpeed;
        float maxLinearAcceleration;
        float maxRotationRate;
        float maxRotationalAcceleration;
        float maxVerticalSpeed;
        float maxVerticalAcceleration;

        #endregion

        #region Accessors

        /// <summary>
        /// This is set to indicate that an actor should be moving
        /// in this direction, at this speed (vector length).
        /// </summary>
        public Vector3? DesiredVelocity
        {
            get { return desiredVelocity; }
            protected set { desiredVelocity = value; }
        }

        /// <summary>
        /// This indicates a location the actor is moving toward.
        /// Should slow down to stop exactly here.
        /// </summary>
        public Vector3? DesiredTargetLocation
        {
            get { return desiredTargetLocation; }
            protected set { desiredTargetLocation = value; }
        }

        /// <summary>
        /// If true, when responding to a velocity command, the actor
        /// will also turn to face the direction of acceleration.
        /// </summary>
        public bool AutoTurn
        {
            get { return autoTurn; }
            protected set { autoTurn = value; }
        }

        /// <summary>
        /// This is how fast we want the actor to be spinning.
        /// </summary>
        public float? DesiredRotationRate
        {
            get { return desiredRotationRateZ; }
            protected set { desiredRotationRateZ = value; }
        }

        /// <summary>
        /// This is the angle we want the actor to spin toward.  Should
        /// slow down to not overshoot.
        /// </summary>
        public float? DesiredRotationAngle
        {
            get { return desiredRotationAngleZ; }
            protected set { desiredRotationAngleZ = value; }
        }

        /// <summary>
        /// Target speed for moving up or down.
        /// </summary>
        public float? DesiredVerticalSpeed
        {
            get { return desiredVerticalSpeed; }
            protected set { desiredVerticalSpeed = value; }
        }

        /// <summary>
        /// Target altitude.  Want to slow and stop at this value.
        /// Note that "altitude" means Z position, not height over
        /// the ground.
        /// </summary>
        public float? DesiredAltitude
        {
            get { return desiredAltitude; }
            protected set { desiredAltitude = value; }
        }

        /// <summary>
        /// List of actors to avoid.
        /// </summary>
        public List<GameActor> AvoidTargets
        {
            get { return avoidTargets; }
        }

        /// <summary>
        /// An external force applied to this actor.  Currently,
        /// only used by fan to push or pull other actors.
        /// </summary>
        public Vector3? ExternalForce
        {
            get { return externalForce; }
            protected set { externalForce = value; }
        }

        /// <summary>
        /// Max linear speed.
        /// </summary>
        public float MaxSpeed
        {
            get { return maxSpeed; }
            protected set { maxSpeed = value; }
        }

        /// <summary>
        /// Max linear acceleration.
        /// </summary>
        public float MaxLinearAcceleration
        {
            get { return maxLinearAcceleration; }
            protected set { maxLinearAcceleration = value; }
        }

        /// <summary>
        /// Max rotation rate.
        /// </summary>
        public float MaxRotationRate
        {
            get { return maxRotationRate; }
            protected set { maxRotationRate = value; }
        }

        /// <summary>
        /// Max rotational acceleration.
        /// </summary>
        public float MaxRotationalAcceleration
        {
            get { return maxRotationalAcceleration; }
            protected set { maxRotationalAcceleration = value; }
        }

        /// <summary>
        /// Max vertical speed.
        /// </summary>
        public float MaxVerticalSpeed
        {
            get { return maxVerticalSpeed; }
            protected set { maxVerticalSpeed = value; }
        }

        /// <summary>
        /// Max vertical acceleration.
        /// </summary>
        public float MaxVerticalAcceleration
        {
            get { return maxVerticalAcceleration; }
            protected set { maxVerticalAcceleration = value; }
        }

        /// <summary>
        /// Is this bot just coasting?  True if none of the Desired* values are set.
        /// Use to determine when to add friction.
        /// This includes velocity, rotation, and altitude values.
        /// 
        /// TODO (****) Probably doesn't help much but we could have all the Coasting
        /// bools cleared in Reset() and then accumulated as the Set* methods are called.
        /// This would remove the need for all the .HasValue testing.
        /// </summary>
        public bool Coasting
        {
            get
            {
                bool coasting = true;

                coasting = true
                    && !desiredVelocity.HasValue
                    && !desiredTargetLocation.HasValue 
                    && !desiredRotationRateZ.HasValue 
                    && !desiredRotationAngleZ.HasValue 
                    && !desiredVerticalSpeed.HasValue
                    && !desiredAltitude.HasValue;

                return coasting;
            }
        }

        /// <summary>
        /// Like Coasting but only looks at horizontal movement.
        /// Does not include rotation.  Rotation was left out on
        /// purpose so that mouse-look works correctly.  We want
        /// coasting to be true even if rotation is happening so
        /// that friction takes affect and stop the movement.
        /// 
        /// True if we have user input for horizontal movement.
        /// False if no user input for horizontal movement.
        /// </summary>
        public bool CoastingHorizontally
        {
            get
            {
                bool coastingHorizontally = true;

                coastingHorizontally = true
                    && !desiredVelocity.HasValue
                    && !desiredTargetLocation.HasValue;

                return coastingHorizontally;
            }
        }

        /// <summary>
        /// Like Coasting but only looks at vertical movement.
        /// True if we have user input for vertical movement.
        /// False if no user input for vertical movement.
        /// </summary>
        public bool CoastingVertically
        {
            get
            {
                bool coastingVertically = true;

                coastingVertically = true
                    && !desiredTargetLocation.HasValue  // Having a target location will also affect Z.
                    && !desiredVerticalSpeed.HasValue
                    && !desiredAltitude.HasValue;

                return coastingVertically;
            }
        }

        #endregion

        #region Public

        /// <summary>
        /// Set the desired velocity for the bot along with max speed 
        /// and acceleration limits it can use to achieve that velocity.
        /// </summary>
        /// <param name="velocity"></param>
        /// <param name="maxSpeed"></param>
        /// <param name="maxLinearAcceleration"></param>
        /// <param name="autoTurn"></param>
        public void SetDesiredVelocity(Vector3 velocity, float maxSpeed, float maxLinearAcceleration, bool autoTurn)
        {
            Debug.Assert(maxLinearAcceleration > 0, "Acceleration limits should be positive.");
            Debug.Assert(maxSpeed >= 0, "Max speed should be non-negative.");
            DesiredVelocity = velocity;
            MaxSpeed = maxSpeed;
            MaxLinearAcceleration = maxLinearAcceleration;
            AutoTurn = autoTurn;
        }   // end of SetDesiredVelocity()

        /// <summary>
        /// Set the desired target location for the bot along with max speed 
        /// and acceleration limits it can use to get there.  Chassis should
        /// slow to a stop at this point.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="maxSpeed"></param>
        /// <param name="maxLinearAcceleration"></param>
        /// <param name="autoTurn"></param>
        public void SetDesiredTargetLocation(Vector3 target, float maxSpeed, float maxLinearAcceleration, bool autoTurn)
        {
            Debug.Assert(maxLinearAcceleration > 0, "Acceleration limits should be positive.");
            Debug.Assert(maxSpeed >= 0, "Max speed should be non-negative.");
            DesiredTargetLocation = target;
            MaxSpeed = maxSpeed;
            MaxLinearAcceleration = maxLinearAcceleration;
            AutoTurn = autoTurn;
        }   // end of SetDesiredTargetLocation()

        /// <summary>
        /// Set the desired rotation rate for the bot along with max rotation 
        /// and acceleration limits it can use to achieve that rate.
        /// </summary>
        /// <param name="rotationRate"></param>
        /// <param name="maxRotationRate"></param>
        /// <param name="maxRotationalAcceleration"></param>
        public void SetDesiredRotationRate(float rotationRate, float maxRotationRate, float maxRotationalAcceleration)
        {
            Debug.Assert(maxRotationalAcceleration > 0, "Acceleration limits should be positive.");
            Debug.Assert(maxRotationRate >= 0, "Max rate should be non-negative.");
            DesiredRotationRate = rotationRate;
            MaxRotationRate = maxRotationRate;
            MaxRotationalAcceleration = maxRotationalAcceleration;
        }   // end of SetDesiredRotationRate()

        /// <summary>
        /// Set the desired rotation angle for the bot along with max rotation 
        /// and acceleration limits it can use to achieve that angle.  Actor
        /// should slow to stop rotatiing at this angle without overshooting.
        /// </summary>
        /// <param name="rotationAngle"></param>
        /// <param name="maxRotationRate"></param>
        /// <param name="maxRotationalAcceleration"></param>
        public void SetDesiredRotationAngle(float rotationAngle, float maxRotationRate, float maxRotationalAcceleration)
        {
            Debug.Assert(maxRotationalAcceleration > 0, "Acceleration limits should be positive.");
            Debug.Assert(maxRotationRate >= 0, "Max rate should be non-negative.");
            DesiredRotationAngle = rotationAngle;
            MaxRotationRate = maxRotationRate;
            MaxRotationalAcceleration = maxRotationalAcceleration;
        }   // end of SetDesiredRotationAngle()

        /// <summary>
        /// Set the desired vertical speed for the bot along with max speed 
        /// and acceleration limits it can use to achieve that speed.
        /// </summary>
        /// <param name="verticalSpeed"></param>
        /// <param name="maxVerticalSpeed"></param>
        /// <param name="maxVerticalAcceleration"></param>
        public void SetDesiredVerticalSpeed(float verticalSpeed, float maxVerticalSpeed, float maxVerticalAcceleration)
        {
            Debug.Assert(maxVerticalAcceleration > 0, "Acceleration limits should be positive.");
            Debug.Assert(maxVerticalSpeed >= 0, "Max speed should be non-negative.");
            DesiredVerticalSpeed = verticalSpeed;
            MaxVerticalSpeed = maxVerticalSpeed;
            MaxVerticalAcceleration = maxVerticalAcceleration;
        }   // end of SetDesiredVerticalSpeed()

        /// <summary>
        /// Sets the target altitude for the bot.
        /// </summary>
        /// <param name="altitude"></param>
        /// <param name="maxVerticalSpeed"></param>
        /// <param name="maxVerticalAcceleration"></param>
        public void SetDesiredAltitude(float altitude, float maxVerticalSpeed, float maxVerticalAcceleration)
        {
            Debug.Assert(maxVerticalAcceleration > 0, "Acceleration limits should be positive.");
            Debug.Assert(maxVerticalSpeed >= 0, "Max speed should be non-negative.");
            DesiredAltitude = altitude;
            MaxVerticalSpeed = maxVerticalSpeed;
            MaxVerticalAcceleration = maxVerticalAcceleration;
        }   // end of SetDesiredAltitude()

        /// <summary>
        /// Sets a new location to avoid this frame.  May be multiple of these.
        /// </summary>
        /// <param name="location"></param>
        public void SetAvoidTarget(GameActor target)
        {
            avoidTargets.Add(target);
        }   // end of SetAvoidTarget()

        /// <summary>
        /// Adds an external force to this actor.  External forces are 
        /// summed together and applied as a single, aggregate force.
        /// Currently, this is only used by fan's push/pull.
        /// </summary>
        /// <param name="force"></param>
        public void SetExternalForce(Vector3 force)
        {
            externalForce = force + externalForce.GetValueOrDefault();
        }   // end of SetExternalForce()

        /// <summary>
        /// Resets all Desired* values to invalid.
        /// </summary>
        public void Reset()
        {
            desiredVelocity = null;
            desiredTargetLocation = null;

            desiredRotationRateZ = null;
            desiredRotationAngleZ = null;

            desiredVerticalSpeed = null;
            desiredAltitude = null;

            avoidTargets.Clear();

            externalForce = null;

        }   // end of Reset()

        #endregion

        #region Internal
        #endregion

    }   // end of class DesiredMovement



}   // end of namespace Boku.Common
