// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
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
using Boku.SimWorld;
using Boku.SimWorld.Terra;

namespace Boku.Programming
{
    /// <summary>
    /// this selector will pick a random location to wander to based upon properties.  
    /// Once it gets near the location or gets stalled it will pick another.
    /// </summary>
    public class WanderSelector : Selector
    {
        double maxTimeOnTarget = 6.0f;  // How long before we time out and pick a new target?

        private Vector3 wanderTarget;
        private Vector3 wanderDir;
        private float wanderDist;
        private double wanderTimeout;   // Time in GameTime seconds when this wander target should be invalidated.
                                        // This is to prevent an actor from getting stuck trying to reach someplace
                                        // it can't get to.
        
        private bool pickNewTarget = false;
        private float wanderTargetHit;
        private float wanderRangeMin;

        [XmlAttribute]
        public float wanderArcMin;
        [XmlAttribute]
        public float wanderArcRange;
        [XmlAttribute]
        public float WanderRangeMin
        {
            get
            {
                return this.wanderRangeMin;
            }
            set
            {
                this.wanderRangeMin = value;
                this.wanderTargetHit = MathHelper.Max(this.wanderRangeMin / 2.0f, 2.0f);
            }
        }

        [XmlAttribute]
        public float wanderRangeRange;
        [XmlAttribute]
        public float strength;

        public WanderSelector()
        {
        }
        public override ProgrammingElement Clone()
        {
            WanderSelector clone = new WanderSelector();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(WanderSelector clone)
        {
            base.CopyTo(clone);
            clone.wanderArcMin = this.wanderArcMin;
            clone.wanderArcRange = this.wanderArcRange;
            clone.wanderRangeMin = this.wanderRangeMin;
            clone.wanderRangeRange = this.wanderRangeRange;
            clone.wanderTargetHit = this.wanderTargetHit;
            clone.strength = this.strength;
        }

        public override void Reset(Reflex reflex)
        {
            ClearActionSet(actionSet);
            this.pickNewTarget = true;
            base.Reset(reflex);
        }

        public override ActionSet ComposeActionSet(Reflex reflex, GameActor gameActor)
        {
            ClearActionSet(actionSet);
            UpdateCanBlend(reflex);

            if (!reflex.targetSet.AnyAction)
                return actionSet;

            // Don't wander if we are immobilized. The effect is the bot randomly turning in place, which looks broken.
            ConstraintModifier constraintMod = reflex.GetModifierByType(typeof(ConstraintModifier)) as ConstraintModifier;
            if (constraintMod != null && constraintMod.ConstraintType == ConstraintModifier.Constraints.Immobile)
            {
                // We still need to add an attractor to the movement set so that the reflex will be considered acted on.
                // TODO (****) is thre a better way to indicate this?
                actionSet.AddActionTarget(Action.AllocTargetLocationAction(reflex, gameActor.Movement.Position, autoTurn: true));
                return actionSet;
            }

            // Check if this target has timed out.
            if (Time.GameTimeTotalSeconds > this.wanderTimeout)
            {
                this.pickNewTarget = true;
            }

            // Calculate a vector toward target in 2d.
            Vector2 wanderTarget2d = new Vector2(this.wanderTarget.X, this.wanderTarget.Y);
            Vector2 value2d = wanderTarget2d - new Vector2(gameActor.Movement.Position.X, gameActor.Movement.Position.Y);

            float distance = Vector3.Dot(wanderDir, gameActor.Movement.Position) - wanderDist + gameActor.BoundingSphere.Radius;
            if (distance > 0)
            {
                this.pickNewTarget = true;
            }

            if (this.pickNewTarget)
            {
                this.pickNewTarget = false;
                // we need to pick a random wander target
                this.wanderTarget = RandomTargetLocation(gameActor);
                this.wanderTimeout = Time.GameTimeTotalSeconds + maxTimeOnTarget;
            }

            actionSet.AddActionTarget(Action.AllocTargetLocationAction(reflex, wanderTarget, autoTurn: true));
                
            /// For debugging, uncomment this line and enable Debug: Display Line of Sight
            /// on the character in question.
            gameActor.AddLOSLine(gameActor.Movement.Position, 
                new Vector3(wanderTarget.X, wanderTarget.Y, gameActor.Movement.Position.Z), 
                new Vector4(0.0f, 1.0f, 0.0f, 1.0f));
            
            return actionSet;
        }
        
        public override void Used(bool newUse)
        {
            if (newUse)
                pickNewTarget = true;
        }

        private Vector3 RandomTargetLocation(GameActor gameActor)
        {
            Debug.Assert(gameActor != null);

            Vector3 location3d;

            // Find a random spot for the wander target.  Take into account whether or not we've
            // stalled and the movement domain of this actor.
            Vector3 collisionNormal = Vector3.Zero;
            int count = 13; // Prevent this from looping forever.  On the off chance that we get n in a row
                            // bad locations we shouldn't worry since the timeout will prevent the actor
                            // from getting stuck.
            bool validLocation = false;
            do
            {
                float newRotation;
                // If we timed out or if we're taking too many attempts to get a valid target, assume we're bouncing 
                // against a barrier and use a wider angle to pick the next target location.
                float angleWidthModifier = 1.0f;
                if (count < 8 || Time.GameTimeTotalSeconds > this.wanderTimeout)
                {
                    angleWidthModifier = 2.0f;
                }
                newRotation = angleWidthModifier * (float)BokuGame.bokuGame.rnd.NextDouble() * wanderArcRange + wanderArcMin;

                float rangeMin = wanderRangeMin;
                float rangeDelta = wanderRangeRange;

                float wanderDistance = (float)BokuGame.bokuGame.rnd.NextDouble() * rangeDelta + rangeMin;

                if (gameActor.Chassis.HasFacingDirection)
                {
                    newRotation += gameActor.Movement.RotationZ;
                }
                else
                {
                    Vector3 dir = gameActor.Movement.Velocity;
                    dir.Z = 0.0f;
                    dir.Normalize();
                    if (!float.IsNaN(dir.X))
                    {
                        float rotation = (float)Math.Acos(dir.X);
                        if (dir.Y < 0.0f)
                        {
                            rotation = MathHelper.TwoPi - rotation;
                        }
                        newRotation += rotation;
                    }
                }

                // cap it within 0-pi*2;
                newRotation = (newRotation + MathHelper.TwoPi) % (MathHelper.TwoPi);
                Vector3 newHeading = new Vector3((float)Math.Cos(newRotation), (float)Math.Sin(newRotation), 0.0f);

                location3d = newHeading * wanderDistance + gameActor.Movement.Position;

                GameActor.BlockedInfo blockInfo = new GameActor.BlockedInfo();

                bool blocked = gameActor.Blocked(location3d, ref blockInfo);

                if (blocked)
                {
                    gameActor.AddLOSLine(gameActor.Movement.Position, blockInfo.Contact, new Vector4(1.0f, 0, 0, 1.0f));

                    float avert = gameActor.Chassis.SafeAversionDistance();
                    float closest = gameActor.BoundingSphere.Radius + avert;
                    if (Vector3.DistanceSquared(gameActor.WorldCollisionCenter, blockInfo.Contact) > closest * closest)
                    {
                        // We have room to move in that direction
                        Vector3 dir = blockInfo.Contact - gameActor.WorldCollisionCenter;
                        float length = dir.Length();
                        Debug.Assert(length > 0);
                        dir /= length;
                        length -= closest;
                        Debug.Assert(length > 0);
                        dir *= length;
                        location3d = gameActor.Movement.Position + dir;

                        blocked = false;
                    }
                }
                if (!blocked)
                {

                    // Actors can have different domains within which they move.  Try and ensure that
                    // the point picked for wandering is valid for this actor's domain.
                    switch (gameActor.Domain)
                    {
                        case GameActor.MovementDomain.Air:
                            validLocation = CheckAirBounds(location3d);
                            break;
                        case GameActor.MovementDomain.Land:
                            {
                                // Test for ground with no water.
                                float terrainAltitude = Terrain.GetTerrainAndPathHeight(location3d);
                                if (terrainAltitude > 0.0f && Terrain.GetWaterBase(location3d) == 0.0f)
                                {
                                    validLocation = true;
                                    location3d.Z = terrainAltitude;
                                }
                            }
                            break;
                        case GameActor.MovementDomain.Water:
                            {
                                // Test for water and set location somewhere between surface and ground.
                                float waterAltitude = Terrain.GetWaterBase(location3d);
                                if (Terrain.GetWaterBase(location3d) > 0.0f)
                                {
                                    validLocation = true;
                                    float terrainAltitude = Terrain.GetTerrainAndPathHeight(location3d);
                                    location3d.Z = terrainAltitude + (waterAltitude - terrainAltitude) * (float)BokuGame.bokuGame.rnd.NextDouble();
                                }
                            }
                            break;
                    }
                }

                --count;
            } while (count > 0 && !validLocation);

            if (!validLocation)
            {
                location3d.Z = Terrain.GetTerrainAndPathHeight(location3d);
            }
            wanderDir = location3d - gameActor.Movement.Position;
            wanderDir.Z = 0;
            if (wanderDir.LengthSquared() > 0)
                wanderDir.Normalize();
            wanderDist = Vector3.Dot(location3d, wanderDir);

            return location3d;
        }

        private bool CheckAirBounds(Vector3 position)
        {
            AABB bounds = InGame.TotalBounds;
            Vector3 min = bounds.Min;
            Vector3 max = bounds.Max;
            float kBorder = 5.0f;

            if (position.X < min.X - kBorder)
                return false;
            if (position.X > max.X + kBorder)
                return false;

            if (position.Y < min.Y - kBorder)
                return false;
            if (position.Y > max.Y + kBorder)
                return false;

            return true;
        }

        /// <summary>
        /// Function not ready for prime time, checking in commented out. ***.
        /// </summary>
        /// <param name="gameActor"></param>
        /// <returns></returns>
        private bool CheckBlockage(GameActor gameActor)
        {
            //Vector3 lookAhead = gameActor.Movement.Velocity;
            //lookAhead.Z = 0;
            //float lookTime = gameActor.Chassis.SafeStoppingTime(lookAhead) * 3.0f + 0.5f;
            //lookAhead *= lookTime;
            //if (lookAhead.LengthSquared() > 0.0001f)
            //{
            //    GameActor.BlockedInfo blockInfo = new GameActor.BlockedInfo();
            //    if (gameActor.Blocked(gameActor.WorldCollisionCenter + lookAhead, ref blockInfo))
            //    {
            //        pickNewTarget = true;

            //        float hitDist = Vector3.Distance(gameActor.WorldCollisionCenter, blockInfo.Contact);
            //        float lookDist = lookAhead.Length();

            //        /// If we're within a third of hitting, we need to panic and just stop.
            //        //if (hitDist < lookDist * 0.33f)
            //        {
            //            return true;
            //        }
            //    }
            //}
            return false;
        }

    }
}
