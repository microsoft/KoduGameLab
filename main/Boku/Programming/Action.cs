
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;

using Boku.Base;
using Boku.Programming;

namespace Boku.Programming
{
    // Collection of classes derived from BaseAction as well as static methods
    // for allocating and freeing them.

    /// <summary>
    /// Static class for managing allocation and freeing of Actions.
    /// </summary>
    public static class Action
    {
        #region Members

        // Static members for managing BaseActions.  These get created and used every single
        // frame so stashing them really makes sense.
        static public List<Attractor> AttractorFreeList = new List<Attractor>();

        static public List<SpeedAction> SpeedActionFreeList = new List<SpeedAction>();
        static public List<VelocityAction> VelocityActionFreeList = new List<VelocityAction>();
        static public List<TargetLocationAction> TargetLocationActionFreeList = new List<TargetLocationAction>();
        static public List<TurnSpeedAction> TurnSpeedActionFreeList = new List<TurnSpeedAction>();
        static public List<HeadingAction> HeadingActionFreeList = new List<HeadingAction>();
        static public List<VerticalSpeedAction> VerticalRateActionFreeList = new List<VerticalSpeedAction>();
        static public List<AltitudeAction> AltitudeActionFreeList = new List<AltitudeAction>();
        static public List<AvoidAction> AvoidActionFreeList = new List<AvoidAction>();

        #endregion

        #region Public

        static void Free(ref BaseAction action)
        {
            action.Free();
            action = null;
        }   // end of Free()

        /// <summary>
        /// Old-style action.  Should be replaced?  TargetDirectionAction?
        /// 
        /// We still use this for stuff like shooting.
        /// 
        /// WHEN See Apple DO Shoot
        ///     gameThing is the nearest apple
        ///     direction is the direction to shoot
        ///     distance is distance to apple
        /// 
        /// WHEN GamePad AButton DO Shoot
        ///     gameThing is null
        ///     direction is forward
        ///     distance is 1
        ///     
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="direction"></param>
        /// <param name="gameThing"></param>
        /// <param name="reflex"></param>
        /// <param name="canBlend"></param>
        /// <param name="specialInstruction"></param>
        /// <returns></returns>
        static public Attractor AllocAttractor(float distance,
                                                Vector3 direction,
                                                GameThing gameThing, 
                                                Reflex reflex, 
                                                bool canBlend = false, 
                                                BaseAction.SpecialInstruction specialInstruction = BaseAction.SpecialInstruction.None)
        {
            Attractor attractor;

            // Recycle an attractor if possible.  If not, create a new one.
            if (AttractorFreeList.Count > 0)
            {
                attractor = AttractorFreeList[AttractorFreeList.Count - 1];
                AttractorFreeList.RemoveAt(AttractorFreeList.Count - 1);
            }
            else
            {
                attractor = new Attractor();
            }

            // Fill in the data.
            attractor.Init(distance, direction, gameThing, reflex, canBlend, specialInstruction);

            return attractor;
        }   // end of AllocAttractor()


        //
        //
        // Start of new alloc routines.
        //
        //

        /// <summary>
        /// Creates an action for changing the actor's forward speed.
        /// </summary>
        /// <param name="reflex"></param>
        /// <param name="speed">Normalized speed value.  1.0 = full speed.  Useful for speed controlled by stick which may be less than 1.0.</param>
        /// <returns></returns>
        static public SpeedAction AllocSpeedAction(Reflex reflex, float speed)
        {
            SpeedAction action;

            // Recycle action if possible.  If not, create a new one.
            if (SpeedActionFreeList.Count > 0)
            {
                action = SpeedActionFreeList[SpeedActionFreeList.Count - 1];
                SpeedActionFreeList.RemoveAt(SpeedActionFreeList.Count - 1);
            }
            else
            {
                action = new SpeedAction();
            }

            // Fill in the data.
            action.Init(reflex, speed);

            return action;
        }   // end of AllocSpeedAction()

        /// <summary>
        /// Creates an action for changing the actor's velocity.
        /// </summary>
        /// <param name="reflex"></param>
        /// <param name="velocity"></param>
        /// <param name="autoTurn">If true then actor will turn to face acceleration direction.  If false, no implicit rotation will happen.</param>
        /// <returns></returns>
        static public VelocityAction AllocVelocityAction(Reflex reflex, Vector3 velocity, bool autoTurn)
        {
            VelocityAction action;

            // Recycle action if possible.  If not, create a new one.
            if (VelocityActionFreeList.Count > 0)
            {
                action = VelocityActionFreeList[VelocityActionFreeList.Count - 1];
                VelocityActionFreeList.RemoveAt(VelocityActionFreeList.Count - 1);
            }
            else
            {
                action = new VelocityAction();
            }

            // Fill in the data.
            action.Init(reflex, velocity, autoTurn);

            return action;
        }   // end of AllocVelocityAction()

        /// <summary>
        /// Create an action which moves the actor to a specific location.
        /// The actor will slow to a stop upon arriving.
        /// </summary>
        /// <param name="reflex"></param>
        /// <param name="targetLocation"></param>
        /// <param name="autoTurn">If true then actor will turn to face acceleration direction.  If false, no implicit rotation will happen.</param>
        /// <returns></returns>
        static public TargetLocationAction AllocTargetLocationAction(Reflex reflex, Vector3 targetLocation, bool autoTurn)
        {
            TargetLocationAction action;

            // Recycle action if possible.  If not, create a new one.
            if (TargetLocationActionFreeList.Count > 0)
            {
                action = TargetLocationActionFreeList[TargetLocationActionFreeList.Count - 1];
                TargetLocationActionFreeList.RemoveAt(TargetLocationActionFreeList.Count - 1);
            }
            else
            {
                action = new TargetLocationAction();
            }

            // Fill in the data.
            action.Init(reflex, targetLocation, autoTurn);

            return action;
        }   // end of AllocTargetLocationAction()

        /// <summary>
        /// Create an action to cause the actor to spin at a constant rate.
        /// </summary>
        /// <param name="reflex"></param>
        /// <returns></returns>
        static public TurnSpeedAction AllocTurnSpeedAction(Reflex reflex, float turnRate)
        {
            TurnSpeedAction action;

            // Recycle action if possible.  If not, create a new one.
            if (TurnSpeedActionFreeList.Count > 0)
            {
                action = TurnSpeedActionFreeList[TurnSpeedActionFreeList.Count - 1];
                TurnSpeedActionFreeList.RemoveAt(TurnSpeedActionFreeList.Count - 1);
            }
            else
            {
                action = new TurnSpeedAction();
            }

            // Fill in the data.
            action.Init(reflex, turnRate);

            return action;
        }   // end of AllocTurnSpeedAction()

        static public HeadingAction AllocHeadingAction(Reflex reflex, float heading, float rotationRateModifier)
        {
            HeadingAction action;

            // Recycle action if possible.  If not, create a new one.
            if (HeadingActionFreeList.Count > 0)
            {
                action = HeadingActionFreeList[HeadingActionFreeList.Count - 1];
                HeadingActionFreeList.RemoveAt(HeadingActionFreeList.Count - 1);
            }
            else
            {
                action = new HeadingAction();
            }

            // Fill in the data.
            action.Init(reflex, heading, rotationRateModifier);

            return action;
        }   // end of AllocHeadingAction()

        /// <summary>
        /// Creates an action for changing the actor's vertical speed.
        /// </summary>
        /// <param name="reflex"></param>
        /// <param name="VerticalRateModifier">Accumulates Quickly and Slowly effects.</param>
        /// <returns></returns>
        static public VerticalSpeedAction AllocVerticalRateAction(Reflex reflex, float verticalRate)
        {
            VerticalSpeedAction action;

            // Recycle action if possible.  If not, create a new one.
            if (VerticalRateActionFreeList.Count > 0)
            {
                action = VerticalRateActionFreeList[VerticalRateActionFreeList.Count - 1];
                VerticalRateActionFreeList.RemoveAt(VerticalRateActionFreeList.Count - 1);
            }
            else
            {
                action = new VerticalSpeedAction();
            }

            // Fill in the data.
            action.Init(reflex, verticalRate);

            return action;
        }   // end of AllocVerticalRateAction()

        static public AltitudeAction AllocAltitudeAction(Reflex reflex, float altitude)
        {
            AltitudeAction action;

            // Recycle action if possible.  If not, create a new one.
            if (AltitudeActionFreeList.Count > 0)
            {
                action = AltitudeActionFreeList[AltitudeActionFreeList.Count - 1];
                AltitudeActionFreeList.RemoveAt(AltitudeActionFreeList.Count - 1);
            }
            else
            {
                action = new AltitudeAction();
            }

            // Fill in the data.
            action.Init(reflex, altitude);

            return action;
        }   // end of AllocAltitudeAction()

        static public AvoidAction AllocAvoidAction(Reflex reflex, GameActor target)
        {
            AvoidAction action;

            // Recycle action if possible.  If not, create a new one.
            if (AvoidActionFreeList.Count > 0)
            {
                action = AvoidActionFreeList[AvoidActionFreeList.Count - 1];
                AvoidActionFreeList.RemoveAt(AvoidActionFreeList.Count - 1);
            }
            else
            {
                action = new AvoidAction();
            }

            // Fill in the data.
            action.Init(reflex, target);

            return action;
        }   // end of AllocAvoidAction()

        #endregion

        #region Internal 
        
        #endregion

    }   // end of class Action

    /// <summary>
    /// Attractor is a legacy action that is still used by the ClosestSelector.
    /// Not sure if it's worth rewriting or even just renaming.
    /// 
    /// Also referenced by MoveLeftRightSelector and SpinSelector both of
    /// which are archived.
    /// </summary>
    public class Attractor : BaseAction
    {
        public Attractor()
        {
        }

        public override void Free()
        {
            Reset();
            Action.AttractorFreeList.Add(this);
        }

        public override void Apply(GameActor actor)
        {
            // Do nothing for old actions...
        }

        public void Init(float distance, Vector3 value, GameThing gameThing, Reflex reflex, bool canBlend, SpecialInstruction specialInstruction)
        {
            this.Distance = distance;
            this.GameThing = gameThing;
            this.Reflex = reflex;
            this.Value = value;
            this.Used = false;
            this.ActedOn = false;
            this.CanBlend = canBlend;
            this.specialInstruction = specialInstruction;
        }

        void Reset()
        {
            Init(0f, Vector3.Zero, null, null, false, SpecialInstruction.None);
        }

    }   // end of class Attractor()

    //
    //
    // Start of new Action classes.
    //
    //

    /// <summary>
    /// Action which changes the actor's speed.
    /// </summary>
    public class SpeedAction : BaseAction
    {
        public float Speed;

        public SpeedAction()
        {
        }

        public override void Free()
        {
            Reset();
            Action.SpeedActionFreeList.Add(this);
        }

        public override void Apply(GameActor actor)
        {
            // Only set this if we haven't already set it or other, mutually exclusive values.
            if (!actor.DesiredMovement.DesiredVelocity.HasValue && !actor.DesiredMovement.DesiredTargetLocation.HasValue)
            {
                float speedModifier = Reflex.ModifierParams.SpeedModifier;
                float maxSpeed = speedModifier * actor.CalcMaxSpeed();
                float maxAcceleration = speedModifier * actor.CalcMaxAcceleration();

                actor.DesiredMovement.SetDesiredVelocity(actor.Movement.Heading, maxSpeed, maxAcceleration, autoTurn: false);
            }
        }   // end of Apply()

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reflex"></param>
        public void Init(Reflex reflex, float speed = 0)
        {
            Reflex = reflex;
            Speed = speed;
        }

        void Reset()
        {
            Init(null);
        }

    }   // end of class SpeedAction()

    /// <summary>
    /// Action which changes the actor's velocity.
    /// </summary>
    public class VelocityAction : BaseAction
    {
        public Vector3 Velocity;
        public bool AutoTurn;   // Should we also generated a Z axis rotation?  For example:
                                // WHEN Gamepad LeftStick DO Move => Autoturn should be true so hover looks right
                                //      but turning can still be overridden by an explicit turn command.
                                // WHEN Gamepad LeftStick DO Move Left/Right => AutoTurn should be false since we're strafing.
                                //      Note: Currently don't have Left/Right or Strafe commands so this is a bit forward looking.

        public VelocityAction()
        {
        }

        public override void Free()
        {
            Reset();
            Action.VelocityActionFreeList.Add(this);
        }

        public override void Apply(GameActor actor)
        {
            // Only set this if we haven't already set it or other, mutually exclusive values.
            if (!actor.DesiredMovement.DesiredVelocity.HasValue && !actor.DesiredMovement.DesiredTargetLocation.HasValue)
            {
                float speedModifier = Reflex.ModifierParams.SpeedModifier;
                float maxSpeed = speedModifier * actor.CalcMaxSpeed();
                float maxAcceleration = speedModifier * actor.CalcMaxAcceleration();

                actor.DesiredMovement.SetDesiredVelocity(Velocity, maxSpeed, maxAcceleration, AutoTurn);
            }
        }   // end of Apply()

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reflex"></param>
        /// <param name="velocity"></param>
        /// <param name="autoTurn"></param>
        public void Init(Reflex reflex, Vector3 velocity = default(Vector3), bool autoTurn = false)
        {
            Reflex = reflex;
            Velocity = velocity;
            AutoTurn = autoTurn;
        }

        void Reset()
        {
            Init(null);
        }

    }   // end of class VelocityAction()

    /// <summary>
    /// Action which changes the actor's target location.  This is
    /// the point in the world the actor is trying to get to.
    /// </summary>
    public class TargetLocationAction : BaseAction
    {
        public Vector3 TargetLocation;
        public bool AutoTurn;   // Should we also generated a Z axis rotation?  For example:
                                // WHEN Gamepad LeftStick DO Move => Autoturn should be true so hover looks right
                                //      but turning can still be overridden by an explicit turn command.
                                // WHEN Gamepad LeftStick DO Move Left/Right => AutoTurn should be false since we're strafing.
                                //      Note: Currently don't have Left/Right or Strafe commands so this is a bit forward looking.

        public TargetLocationAction()
        {
        }

        public override void Free()
        {
            Reset();
            Action.TargetLocationActionFreeList.Add(this);
        }

        public override void Apply(GameActor actor)
        {
            // Only set this if we haven't already set it or other, mutually exclusive values.
            if (!actor.DesiredMovement.DesiredTargetLocation.HasValue && !actor.DesiredMovement.DesiredVelocity.HasValue)
            {
                float speedModifier = Reflex.ModifierParams.SpeedModifier;
                float maxSpeed = speedModifier * actor.CalcMaxSpeed();
                float maxAcceleration = speedModifier * actor.CalcMaxAcceleration();

                actor.DesiredMovement.SetDesiredTargetLocation(TargetLocation, maxSpeed, maxAcceleration, AutoTurn);
            }
        }

        public void Init(Reflex reflex, Vector3 targetLocation = default(Vector3), bool autoTurn = false)
        {
            Reflex = reflex;
            TargetLocation = targetLocation;
            AutoTurn = autoTurn;
        }

        void Reset()
        {
            Init(null);
        }

    }   // end of class TargetLocationAction()

    /// <summary>
    /// Action which changes the actor's turn rate.  This is
    /// just spinning with no end heading in mind.
    /// </summary>
    public class TurnSpeedAction : BaseAction
    {
        public float TurnRate;

        public TurnSpeedAction()
        {
        }

        public override void Free()
        {
            Reset();
            Action.TurnSpeedActionFreeList.Add(this);
        }

        public override void Apply(GameActor actor)
        {
            // Only set this if we haven't already set it or other, mutually exclusive values.
            if (!actor.DesiredMovement.DesiredRotationRate.HasValue && !actor.DesiredMovement.DesiredRotationAngle.HasValue)
            {
                float speedModifier = Reflex.ModifierParams.SpeedModifier;
                float turnSpeed = speedModifier * actor.CalcTurnSpeed();
                float turnAcceleration = (float)Math.Abs(speedModifier) * actor.CalcTurnAcceleration();

                actor.DesiredMovement.SetDesiredRotationRate(speedModifier * TurnRate, turnSpeed, turnAcceleration);
            }
        }   // end of Apply()

        public void Init(Reflex reflex, float turnRate = 0)
        {
            Reflex = reflex;
            TurnRate = turnRate;
        }

        void Reset()
        {
            Init(null);
        }

    }   // end of class TurnSpeedAction()

    /// <summary>
    /// Action which changes the actor's heading.  The heading
    /// is the direction (Z rotation) the actor is facing.
    /// </summary>
    public class HeadingAction : BaseAction
    {
        public float Heading;
        public float RotationRateModifier;  // Rate attenuation based on stick or trigger input.

        public HeadingAction()
        {
        }

        public override void Free()
        {
            Reset();
            Action.HeadingActionFreeList.Add(this);
        }

        public override void Apply(GameActor actor)
        {
            // Only set this if we haven't already set it or other, mutually exclusive values.
            if (!actor.DesiredMovement.DesiredRotationAngle.HasValue && !actor.DesiredMovement.DesiredRotationRate.HasValue)
            {
                float speedModifier = Reflex.ModifierParams.SpeedModifier;
                float turnSpeed = speedModifier * actor.CalcTurnSpeed();
                float turnAcceleration = speedModifier * actor.CalcTurnAcceleration();

                actor.DesiredMovement.SetDesiredRotationAngle(Heading, turnSpeed * RotationRateModifier, turnAcceleration);
            }
        }

        public void Init(Reflex reflex, float heading = 0, float rotationRateModifier = 1)
        {
            Reflex = reflex;
            Heading = heading;
            RotationRateModifier = rotationRateModifier;
        }

        void Reset()
        {
            Init(null);
        }

    }   // end of class HeadingAction()

    /// <summary>
    /// Action which changes the actor's vertical rate.
    /// This is the speed going up or down.
    /// </summary>
    public class VerticalSpeedAction : BaseAction
    {
        public float Speed;     // Speed attenuation caused by stick/trigger inputs.

        public VerticalSpeedAction()
        {
        }

        public override void Free()
        {
            Reset();
            Action.VerticalRateActionFreeList.Add(this);
        }

        public override void Apply(GameActor actor)
        {
            // Only set this if we haven't already set it or other, mutually exclusive values.
            if (!actor.DesiredMovement.DesiredVerticalSpeed.HasValue && !actor.DesiredMovement.DesiredAltitude.HasValue)
            {
                float speedModifier = Reflex.ModifierParams.SpeedModifier;
                float maxSpeed = Speed * speedModifier * actor.CalcMaxVerticalSpeed();
                float maxAcceleration = speedModifier * actor.CalcMaxVerticalAcceleration();

                actor.DesiredMovement.SetDesiredVerticalSpeed(maxSpeed, Math.Abs(maxSpeed), maxAcceleration);
            }
        }   // end of Apply()

        public void Init(Reflex reflex, float speed = 0)
        {
            Reflex = reflex;
            Speed = speed;
        }

        void Reset()
        {
            Init(null);
        }

    }   // end of class VerticalSpeedAction()

    /// <summary>
    /// Action which changes the actor's altitude.  Altitude is
    /// the Z position of the actor, not the height above ground.
    /// </summary>
    public class AltitudeAction : BaseAction
    {
        public float Altitude;

        public AltitudeAction()
        {
        }

        public override void Free()
        {
            Reset();
            Action.AltitudeActionFreeList.Add(this);
        }

        public override void Apply(GameActor actor)
        {
            // Only set this if we haven't already set it or other, mutually exclusive values.
            if (!actor.DesiredMovement.DesiredAltitude.HasValue && !actor.DesiredMovement.DesiredVerticalSpeed.HasValue)
            {
                float speedModifier = Reflex.ModifierParams.SpeedModifier;
                float maxSpeed = speedModifier * actor.CalcMaxVerticalSpeed();
                float maxAcceleration = speedModifier * actor.CalcMaxVerticalAcceleration();

                actor.DesiredMovement.SetDesiredAltitude(Altitude, maxSpeed, maxAcceleration);
            }
        }   // end of Apply()

        public void Init(Reflex reflex, float altitude = 0)
        {
            Reflex = reflex;
            Altitude = altitude;
        }

        void Reset()
        {
            Init(null);
        }

    }   // end of class AltitudeAction()

    /// <summary>
    /// Action which causes a character to avoid a location.  Kept mostly
    /// for back compat.  I'm not really sure if this is used much if at
    /// all since it really doesn't work all that well.
    /// </summary>
    public class AvoidAction : BaseAction
    {
        public GameActor Target;

        public AvoidAction()
        {
        }

        public override void Free()
        {
            Reset();
            Action.AvoidActionFreeList.Add(this);
        }

        public override void Apply(GameActor actor)
        {
            float speedModifier = Reflex.ModifierParams.SpeedModifier;
            float maxSpeed = speedModifier * actor.CalcMaxVerticalSpeed();
            float maxAcceleration = speedModifier * actor.CalcMaxVerticalAcceleration();

            actor.DesiredMovement.SetAvoidTarget(Target);
        }   // end of Apply()

        public void Init(Reflex reflex, GameActor target)
        {
            Reflex = reflex;
            Target = target;
        }

        void Reset()
        {
            Init(null, null);
        }

    }   // end of class AvoidAction()

}   // end of namespace Boku.Programming
