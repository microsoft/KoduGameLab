// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.



using System;
using System.Collections;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.Common.ParticleSystem;
using Boku.Common.Xml;
using Boku.Programming;
using Boku.SimWorld;
using Boku.SimWorld.Chassis;
using Boku.SimWorld.Collision;
using Boku.SimWorld.Terra;

namespace Boku
{
    public class CruiseMissile : GameActor
    {
        private static XmlGameActor xmlActor;
        public static XmlGameActor XmlActor
        {
            get 
            {
                if (xmlActor == null)
                    xmlActor = ActorManager.GetActor("CruiseMissile").XmlGameActor;
                return xmlActor;
            }
        }

        public override float Cost
        {
            get
            {
                float cost = base.Cost;
                if (smokeEnabled)
                {
                    const float kSmokeCost = 2.5f;
                    cost += kSmokeCost;
                }
                return cost;
            }
        }

        //
        //  CruiseMissile
        //

        private bool smokeEnabled;

        public CruiseMissile(string classificationName, BaseChassis chassis, GetModelInstance getModelInstance, StaticActor staticActor)
            : base(classificationName, classificationName, chassis, getModelInstance, getModelInstance, staticActor)
        {
        }

        static private CruiseMissile NextAvailable()
        {
            return ActorFactory.Create(ActorManager.GetActor("CruiseMissile")) as CruiseMissile;
        }

        private void Setup(Vector3 position,                      // Starting position.
                                GameActor launcher,                 // Actor that launched this missile.
                                float initialRotation,
                                GameThing.Verbs verbPayload,
                                int damage,
                                MissileChassis.BehaviorFlags behavior,
                                Classification.Colors color,
                                float desiredSpeed,
                                bool wantSmoke)
        {
            smokeEnabled = wantSmoke;

            // Or, we could change the sound to something other than the rumble.
            if (!smokeEnabled)
                XmlActorParams.IdleSoundName = null;

            MissileChassis missileChassis = Chassis as MissileChassis;
            missileChassis.Missile = this;
            missileChassis.Launcher = launcher;
            missileChassis.VerbPayload = verbPayload;
            missileChassis.Damage = damage;
            missileChassis.Behavior = behavior;
            missileChassis.Rotation = initialRotation;
            missileChassis.DesiredSpeed = desiredSpeed;

            Mass = 10.0f;

            Movement.Position = position;

            // Calculate initial missile speed.
            float initialSpeed = desiredSpeed;
            Vector3 gunVel = launcher.Movement.Velocity;
            Vector3 shotVel = new Vector3((float)Math.Cos(initialRotation), (float)Math.Sin(initialRotation), 0f) * initialSpeed;
            if (gunVel.X != 0 || gunVel.Y != 0)
            {
                // Add in some of the launcher's velocity.  Missile will
                // gradually adjust its speed to its desired velocity.
                Vector3 gunVelNorm = Vector3.Normalize(gunVel);
                Vector3 shotVelNorm = Vector3.Normalize(shotVel);
                float dot = Vector3.Dot(gunVelNorm, shotVelNorm);
                Vector3 proj = gunVel * dot;
                initialSpeed = proj.Length() * MyMath.Direction(dot);
            }
            // Don't allow initial speed to be negative because it looks odd.
            missileChassis.Speed = Math.Max(desiredSpeed, initialSpeed);

            classification.Color = color;

            InitSmokeEmitter(Classification.ColorVector4(classification.Color));

            InitMuzzleFlash(Classification.ColorVector4(classification.Color), 2.0f, 8.0f);

            missileChassis.IgnoreGlassWalls = true;
            missileChassis.Feelers.Clear();     // Don't want missiles to hit glass walls.

            // Register for collisions.
            //InGame.inGame.RegisterCollide(this);

        }   // end of Setup()

        /// <summary>
        /// c'tor for use when targetting a point in space rather than an object.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="targetPosition"></param>
        /// <param name="launcher"></param>
        /// <param name="verbPayload"></param>
        /// <param name="trackingMode"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        static public CruiseMissile Create(
                                Vector3 position,                   // Starting position.
                                Vector3 targetPosition,             // Location we're trying to hit.
                                GameActor launcher,                 // Actor that launched this missile.
                                float initialRotation,              // 
                                GameThing.Verbs verbPayload,
                                int damage,
                                MissileChassis.BehaviorFlags behavior,
                                Classification.Colors color,
                                float desiredSpeed,
                                float missileLifetime,
                                bool wantSmoke)
        {
            CruiseMissile cm = NextAvailable();

            cm.Setup(
                position,
                launcher,
                initialRotation,
                verbPayload,
                damage,
                behavior,
                color,
                desiredSpeed,
                wantSmoke);

            //Vector3 forward = Vector3.Normalize(targetPosition - position);
            //Vector3 side = Vector3.Cross(Vector3.UnitZ, forward);
            //if (side.LengthSquared() < 0.01f)
            //{
            //    side = Vector3.Cross(Vector3.UnitY, forward);
            //}
            //side.Normalize();
            //Vector3 up = Vector3.Normalize(Vector3.Cross(forward, side));
            //Matrix l2w = Matrix.Identity;
            //l2w.Right = forward;
            //l2w.Up = up;
            //l2w.Forward = side;
            //l2w.Translation = position;
            //cm.Movement.LocalMatrix = l2w;

            MissileChassis missileChassis = cm.Chassis as MissileChassis;

            Vector3 direction = targetPosition - launcher.WorldCollisionCenter;
            Vector3 delta = Vector3.Normalize(direction);

            float desiredPitch = (float)(Math.Atan2(delta.Z, new Vector2(delta.X, delta.Y).Length()));
            missileChassis.PitchAngle = desiredPitch;

            missileChassis.DeathTime = Time.GameTimeTotalSeconds + missileLifetime * 1.1f;
            missileChassis.TargetPosition = position + delta * desiredSpeed * missileLifetime * 10f;
            missileChassis.TargetObject = null;

            return cm;

        }   // end of CruiseMissile Create

        /// <summary>
        /// c'tor to use when shooting at a particular object
        /// </summary>
        /// <param name="position"></param>
        /// <param name="targetPosition"></param>
        /// <param name="targetThing"></param>
        /// <param name="launcher"></param>
        /// <param name="verbPayload"></param>
        /// <param name="trackingMode"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        static public CruiseMissile Create(
                                Vector3 position,                    // Starting position.
                                GameThing targetThing,              // Object we're trying to hit.
                                GameActor launcher,                 // Actor that launched this missile.
                                float initialRotation,              // 
                                GameThing.Verbs verbPayload,
                                int damage,
                                MissileChassis.BehaviorFlags behavior,
                                Classification.Colors color,
                                float desiredSpeed,
                                float missileLifetime,
                                bool wantSmoke)
        {
            CruiseMissile cm = NextAvailable();

            cm.Setup(
                position,
                launcher,
                initialRotation,
                verbPayload,
                damage,
                behavior,
                color,
                desiredSpeed,
                wantSmoke);

            MissileChassis missileChassis = cm.Chassis as MissileChassis;

            // Set initial speed taking into account the velocity of the launcher.
            Vector3 missileVelocity = new Vector3((float)Math.Cos(initialRotation), (float)Math.Sin(initialRotation), 0);
            float dot = Vector3.Dot(missileVelocity, launcher.Movement.Velocity);
            missileChassis.Speed = Math.Max(desiredSpeed, dot);

            missileChassis.DeathTime = Time.GameTimeTotalSeconds + missileLifetime;
            missileChassis.TargetPosition = targetThing.WorldCollisionCenter;
            missileChassis.TargetObject = targetThing;

            return cm;
        }   // end of CruiseMissile Create

        #region Accessors
        public const float kCollisionRadius = 0.35f;

        public GameActor Launcher
        {
            get
            {
                GameActor actor = null;
                MissileChassis c = Chassis as MissileChassis;
                if (c != null)
                {
                    actor = c.Launcher;
                }
                return actor;
            }
        }

        #endregion

        private void InitSmokeEmitter(Vector4 color)
        {
            attachments.Clear();

            if (!smokeEnabled)
                return;

            SharedSmokeSource smokeSource = new SharedSmokeSource(InGame.inGame.ParticleSystemManager);
            smokeSource.Position = Movement.Position;
            smokeSource.ResetPreviousPosition();
            smokeSource.LinearEmission = true;
            smokeSource.Active = true;
            smokeSource.Emitting = false;
            smokeSource.Color = color;
            smokeSource.PositionJitter = 0.03f;
            smokeSource.StartRadius = 0.1f;
            smokeSource.EndRadius = 1.0f;
            smokeSource.StartAlpha = 0.4f;
            smokeSource.MinLifetime = .5f;
            smokeSource.MaxLifetime = 1.5f;
            smokeSource.InitFlash(2.4f, 0.5f);
            smokeSource.MaxRotationRate = 3.0f;
            smokeSource.EmissionRate = 5.0f;
            smokeSource.Velocity = Vector3.Zero;
            smokeSource.Acceleration = new Vector3(0.0f, 0.0f, 0.2f);   // Have smoke float up a bit.
            smokeSource.Usage = BaseEmitter.Use.Regular;

            AddEmitter(smokeSource, Vector3.Zero);

            // And the optional heat wash versions
            if (Fx.DistortionManager.PartyEnabled)
            {
                smokeSource = new SharedSmokeSource(InGame.inGame.ParticleSystemManager);
                smokeSource.Position = Movement.Position;
                smokeSource.ResetPreviousPosition();
                smokeSource.LinearEmission = true;
                smokeSource.Active = true;
                smokeSource.Emitting = false;
                smokeSource.Color = new Vector4(0.85f, 0.85f, 0.95f, 1.0f);
                smokeSource.PositionJitter = 0.03f;
                smokeSource.StartRadius = 0.2f;
                smokeSource.EndRadius = 1.0f;
                smokeSource.StartAlpha = 0.9f;
                smokeSource.MinLifetime = 0.25f;
                smokeSource.MaxLifetime = 0.5f;
                smokeSource.MaxRotationRate = 3.0f;
                smokeSource.EmissionRate = 2.0f;
                smokeSource.Velocity = Vector3.Zero;
                smokeSource.Acceleration = new Vector3(0.0f, 0.0f, 0.2f);   // Have smoke float up a bit.
                smokeSource.Usage = BaseEmitter.Use.Distort;

                AddEmitter(smokeSource, new Vector3(-1.0f, 0.0f, 0.0f));

            }
        }   // end of CruiseMissile InitSmokeEmitter()

        public override void SetColor(Classification.Colors color)
        {
            // Set the body color.
            base.SetColor(color);

            // Also change the smoke color.
            if (smokeEnabled)
            {
                OffsetEmitter oe = attachments[0] as OffsetEmitter;
                SharedSmokeSource sss = oe.Emitter as SharedSmokeSource;
                sss.Color = Classification.ColorVector4(color);
            }

        }   // end of SetColor()

        public override void Deactivate()
        {
            // Strangley, this can end up null somehow if the missile gets squashed.
            // On reset, this will throw if no protected.  Not exactly sure what is
            // setting launcher to null though.
            // TODO (****) figure out and fix.  (or just refactor all off the missile stuff)
            if (Launcher != null)
            {
                --Launcher.NumMissilesInAir;
            }
            base.Deactivate();
        }

    }   // end of class CruiseMissile

}   // end of namespace Boku


