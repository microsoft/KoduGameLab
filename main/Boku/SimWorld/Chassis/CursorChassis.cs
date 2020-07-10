// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using System.Diagnostics;

using Boku.Base;
using Boku.Common;
using Boku.SimWorld;
using Boku.SimWorld.Terra;

namespace Boku.SimWorld.Chassis
{
    /// <summary>
    /// Chassis for our cursor.
    /// </summary>
    public class CursorChassis : BaseChassis
    {
        #region Members

        private bool followSlope = false;
        private float scale = 1.0f;
        private Vector3 normal = new Vector3(0.0f, 0.0f, 1.0f);

        #endregion

        #region Accessors

        public override bool SupportsStrafing { get { return false; } }

        #endregion

        #region Public

        public override void InitDefaults()
        {
            base.InitDefaults();

            // values here copied from member initializers above.
            followSlope = false;
            scale = 1.0f;
            normal = new Vector3(0.0f, 0.0f, 1.0f);
        }

        public override void PreCollisionTestUpdate(GameThing actor)
        {
            Movement movement = actor.Movement;
            GameActor.State state = actor.CurrentState;

            Matrix local = Matrix.Identity;

            /// We query the terrain height using an extremely high position, so that the
            /// cursor winds up on top of everything, including any elevated paths.
            Vector3 queryPos = new Vector3(movement.Position.X, movement.Position.Y, float.MaxValue);
            movement.Altitude = EditHeight + Terrain.GetTerrainAndPathHeight(queryPos);

            if (followSlope)
            {
                // Calc world matrix.
                normal = 0.9f * normal + 0.1f * Terrain.GetNormal(movement.Position);
                normal.Normalize();
                Vector3 X = new Vector3(1.0f, 0.0f, 0.0f);
                Vector3 dy = Vector3.Cross(normal, X);
                Vector3 dx = Vector3.Cross(dy, normal);

                dx.Normalize();
                dy.Normalize();

                // dx, dy, normal are now the coordinate vectors which need to be put into the matrix.
                local.Up = dy;
                local.Right = dx;
                local.Forward = -normal;
            }

            // Apply translation.
            local.Translation = movement.Position;

            // Apply the scaling.
            local.M11 *= scale;
            local.M22 *= scale;
            local.M33 *= scale;

            movement.LocalMatrix = local;

        }   // end of CursorChassis PreCollisionTestUpdate()

        public override void PostCollisionTestUpdate(GameThing thing)
        {
            // Nothing here.
        }   // end of CursorChassis PostCollisionTestUpdate()

        public override void CollideWithTerrainWalls(GameThing thing)
        {
            // Nothing to see here, move along.
        }

        public override void CollisionResponse(Movement movement)
        {
        }   // end of CursorChassis CollisionResponse()

        #endregion

    }   // end of class CursorChassis

}   // end of namespace Boku.SimWorld.Chassis
