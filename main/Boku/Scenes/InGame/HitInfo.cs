// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using KoiX;
using KoiX.Input;

using Boku.Base;
using Boku.Common;
using Boku.Fx;
using Boku.SimWorld.Collision;
using Boku.SimWorld.Terra;
using Boku.SimWorld;

namespace Boku
{
    /// <summary>
    /// Class containing info about what is hit at the current mouse/touch
    /// pixel including info on actors and terrain.
    /// </summary>
    public class HitInfo
    {
        #region Members
        /// <summary>
        /// Actor under mouse.
        /// </summary>
        GameActor actorHit = null;
        /// <summary>
        /// LOS ray hit on actorHit's collision bounds.
        /// </summary>
        Vector3 actorPosition = Vector3.Zero;

        /// <summary>
        /// TODO (****) No clue if this is last actor or last frame or ...
        /// Only used by touch.
        /// </summary>
        Vector3 lastTouchEditPos = Vector3.Zero;

        /// <summary>
        /// Position of hit with terrain.
        /// </summary>
        Vector3 terrainPosition = Vector3.Zero;
        /// <summary>
        /// Whether terrain is under mouse.
        /// </summary>
        bool terrainHit = false;
        /// <summary>
        /// True if no terrain under mouse, but ray hits zero plane.
        /// </summary>
        bool zeroPlaneHit = false;

        /// <summary>
        /// The material on the terrain where the mouse LOS hits. Undefined if !terrainHit.
        /// </summary>
        ushort terrainMaterial = 0;

        /// <summary>
        /// If we're dragging around this bot via it's anchor this
        /// will be the vertical offset from the anchor to the bot.
        /// </summary>
        float verticalOffset = 0.0f;

        #endregion Members

        #region Accessors

        /// <summary>
        /// Is there an unoccluded actor under the mouse?
        /// </summary>
        public bool HaveActor
        {
            get { return actorHit != null; }
        }

        /// <summary>
        /// Return any unoccluded actor under the mouse. Must be
        /// closer than any other other actor under mouse, and closer than terrain.
        /// </summary>
        public GameActor ActorHit
        {
            get { return actorHit; }
            internal set { actorHit = value; }
        }

        /// <summary>
        /// Where the mouse LOS ray hits the ActorHit's collision hull. For
        /// mobile bots, that's a sphere, for other bots (e.g. factory) it's 
        /// a set of collision primitives. Undefined if !HaveActor.
        /// </summary>
        public Vector3 ActorPosition
        {
            get
            {
                Vector3 pos = actorPosition;

                /*
                // Not needed here.  Want to snap the actor's position, not the position of the ray intersect.
                if (InGame.inGame.SnapToGrid)
                {
                    // Snap the Actor's position to the center of a terrain cube.
                    pos = InGame.inGame.SnapPosition(pos);
                }
                */

                return pos;
            }
            internal set { actorPosition = value; }
        }

        /// <summary>
        /// Whether current mouse LOS ray hits the terrain
        /// </summary>
        public bool TerrainHit
        {
            get { return terrainHit; }
            internal set { terrainHit = value; }
        }

        /// <summary>
        /// True if current mouse LOS hits no terrain, but does cross the 
        /// zero height plane.
        /// </summary>
        public bool ZeroPlaneHit
        {
            get { return zeroPlaneHit; }
            internal set { zeroPlaneHit = value; }
        }

        /// <summary>
        /// Where the ray hits terrain (or zero plane, check flags).
        /// </summary>
        public Vector3 TerrainPosition
        {
            get
            {
                Vector3 pos = terrainPosition;

                if (InGame.inGame.SnapToGrid)
                {
                    // Snap the position of the hit to the center of the terrain cube.
                    pos = InGame.SnapPosition(pos);
                }

                return pos;
            }
            internal set { terrainPosition = value; }
        }

        /// <summary>
        /// What terrain material is under the mouse. Undefined if !TerrainHit.
        /// </summary>
        public ushort TerrainMaterial
        {
            get { return terrainMaterial; }
            internal set { terrainMaterial = value; }
        }

        /// <summary>
        /// If we're dragging around this bot via it's anchor this
        /// will be the vertical offset from the anchor to the bot.
        /// </summary>
        public float VerticalOffset
        {
            get { return verticalOffset; }
            set { verticalOffset = value; }
        }

        public Vector3 LastTouchEditPos
        {
            get { return lastTouchEditPos; }
            set { lastTouchEditPos = value; }

        }

        #endregion Accessors

        #region Public
        #endregion Public

        #region Internal
        /// <summary>
        /// Reset before doing another test.
        /// </summary>
        internal void Clear()
        {
            actorHit = null;
            actorPosition = Vector3.Zero;
            terrainHit = false;
            terrainPosition = Vector3.Zero;
        }
        #endregion Internal

    }   // end of class HitInfo

}   // end of namespace Boku
