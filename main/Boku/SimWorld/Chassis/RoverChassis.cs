
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using System.Diagnostics;

using KoiX;

using Boku.Base;
using Boku.Common;
using Boku.SimWorld;
using Boku.SimWorld.Terra;
using Boku.Animatics;
using Boku.Audio;
using Boku.Common.ParticleSystem;

namespace Boku.SimWorld.Chassis
{
    /// <summary>
    /// Chassis used for Mars Rover actor
    /// NOTE: This was created by making a blanket copy of the CycleChassis and then adding some functionality to provide the rotation we wanted.
    /// This still needs a pass with some significant cleanup of functionality from Cycle that we don't want/need.
    /// //TODO: Common base "VehicleChassis" that both Cycle and Rover derive from
    /// </summary>
    public class RoverChassis : VehicleChassis
    {
        #region Members

        //how much we should pitch/roll based on terrain normals
        private float terrainPitchClamped;
        private float terrainPitch;
        private float terrainRollClamped;
        private float terrainRoll;

        /// <summary>
        /// The UNSCALED rover hill climb speed
        /// </summary>
        protected float roverHillClimbSpeed;
        /// <summary>
        /// The UNSCALED rover hill pitch which we start applying slow down
        /// </summary>
        protected float roverHillStartPitch;
        /// <summary>
        /// The UNSCALED rover hill pitch which we slowdown is maxed
        /// </summary>
        protected float roverHillEndPitch;

        #endregion

        #region Accessors

        /// <summary>
        /// The unscaled, the slowest speed of which the rover can climb hills
        /// </summary>
        public float RoverHillClimbSpeed
        {
            get { return roverHillClimbSpeed; }
            set { roverHillClimbSpeed = value; }
        }
        /// <summary>
        /// The unscaled, the min pitch at which we start applying slow down
        /// </summary>
        public float RoverHillStartPitch
        {
            get { return roverHillStartPitch; }
            set { roverHillStartPitch = value; }
        }
        /// <summary>
        /// The unscaled, the end pitch at which the slow down is maxed
        /// </summary>
        public float RoverHillEndPitch
        {
            get { return roverHillEndPitch; }
            set { roverHillEndPitch = value; }
        }

        public float RoverMinPitch
        {
            get { return -3.14f; }
        }

        public float RoverMaxPitch
        {
            get { return 3.14f; }
        }

        public float RoverMinRoll
        {
            get { return -3.14f; }
        }

        public float RoverMaxRoll
        {
            get { return 3.14f; }
        }

        #endregion

        #region Public

        public RoverChassis()
            : base()
        {
        }

        public override void InitDefaults()
        {
            base.InitDefaults();

            // values here copied from member initializers above.
            terrainPitch = 0.0f;
            terrainPitchClamped = 0.0f;
            terrainRoll = 0.0f;
        }

        public float TerrainPitch
        {
            get { return terrainPitch; }
        }

        // Modify the speed of the rover depending on the gradient of the terrain
        public bool ModifyHeading(ref Vector3 heading)
        {
            GameActor actor = Parent as GameActor;

            if (actor == null)
            {
                return false;
            }

            float len = heading.Length();

            Vector3 unit = heading;

            if (unit != Vector3.Zero)
            {
                unit.Normalize();
            }

            float CurrentMultiplier = 1.0f;
            float CurrentPitch = TerrainPitch;
            // if rover is going up hill
            if (CurrentPitch < 0.0f)
            {
                CurrentPitch = Math.Abs(CurrentPitch);
                if (CurrentPitch < RoverHillStartPitch)
                {
                    CurrentMultiplier = 1.0f;
                }
                else if (CurrentPitch > RoverHillEndPitch)
                {
                    CurrentMultiplier = RoverHillClimbSpeed;
                }
                else
                {
                    float PitchRate = (CurrentPitch - RoverHillStartPitch) / (RoverHillEndPitch - RoverHillStartPitch);
                    CurrentMultiplier = MyMath.Lerp(1.0f, RoverHillClimbSpeed, PitchRate);
                }
            }
            heading = unit * len * CurrentMultiplier;

            return true;
        }
        #endregion

        /// <summary>
        /// Allow subclasses to handle special logic for adjusting based on terrain
        /// </summary>
        /// <param name="movement"></param>
        protected override void HandleMovement(Movement movement)
        {
            Vector3 terrainNormalAvg = Vector3.Zero;

            //calculate how much to pitch/roll the model based on terrain
            CalcAverageTerrainNormal(movement, ref terrainNormalAvg);

            //TODO: surely we don't have to recalculate this - should be stored somewhere?
            Matrix localToWorld = Matrix.CreateRotationZ(movement.RotationZ);
            Matrix worldToLocal = Matrix.Invert(localToWorld);

            //move the terrain normal into local space
            terrainNormalAvg = Vector3.TransformNormal(terrainNormalAvg, worldToLocal);

            if (terrainNormalAvg.Z >= 0.001f)
            {
                //how much off vertical
                terrainPitch = (float)Math.Atan2(terrainNormalAvg.X, terrainNormalAvg.Z);
                //how much off horizontal
                terrainRoll = (float)Math.Atan2(terrainNormalAvg.Y, Math.Sqrt(terrainNormalAvg.X * terrainNormalAvg.X + terrainNormalAvg.Z * terrainNormalAvg.Z));

                terrainPitchClamped = MyMath.Clamp<float>(terrainPitch, RoverMinPitch, RoverMaxPitch);
                terrainRollClamped = MyMath.Clamp<float>(terrainRoll, RoverMinRoll, RoverMaxRoll);
            }
        }

        /// <summary>
        /// Allow subclasses to handle special logic for generating rotation of the object
        /// </summary>
        /// <param name="movement"></param>
        protected override Matrix GenerateRotationMatrix(Movement movement)
        {
            //rotation matrix based on direction + terrain
            Matrix local = Matrix.CreateRotationY(terrainPitchClamped) * //rotation due to pitch from terrain in X (rotated around Y)
                                Matrix.CreateRotationX(-terrainRollClamped) * //rotation due to roll from terrain in Y (rotated around X)
                                Matrix.CreateRotationZ(movement.RotationZ); //normal rotation due to direction facing

            //rotates in direction we're facing + accounts for lean based on terrain
            return local;
        }

        /// <summary>
        /// Calculates an average terrain normal based on feelers
        /// </summary>
        /// <param name="movement"></param>
        /// <param name="terrainNormal"></param>
        private void CalcAverageTerrainNormal(Movement movement, ref Vector3 terrainNormal)
        {
            Vector3 prevPosition = movement.PrevPosition;

            // On the first frame or if dead, the feeler data is invalid so ignore.
            if (Feelers[0].prevPosition == Vector3.Zero/* || parent.CurrentState == GameThing.State.Dead*/)
            {
                //terrainAltitude = Terrain.GetHeight(Top(prevPosition));
                terrainNormal = Vector3.UnitZ;
            }
            else
            {
                // Skip the first feeler since we've already got it.
                for (int i = 1; i < Feelers.Count; i++)
                {
                    Vector3 normal = Terrain.GetNormal(Feelers[i].prevPosition);
                    terrainNormal += normal;
                }

                terrainNormal.Normalize();
            }
        }   // end of GetTerrainAltitudeAndNormalFromFeelers()

    }   // end of class RoverChassis

}   // end of namespace Boku.SimWorld.Chassis
