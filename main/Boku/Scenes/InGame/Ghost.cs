// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


#define MF_GHOST_IGNORED

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;
using Boku.Common;
using Boku.Fx;
using Boku.SimWorld.Collision;

namespace Boku
{
    /// <summary>
    /// This section of InGame deals with ghosting actors (making them translucent)
    /// when they are in the way, as in when the camera is passing through them.
    /// </summary>
    public partial class InGame : GameObject, INeedsDeviceReset
    {
        #region Child Classes
        private class Ghost
        {
            #region Accessors
            /// <summary>
            /// When the last time I was blocking the view
            /// </summary>
            public double TimeOccluding;
            /// <summary>
            /// When I was first detected to be blocking the view
            /// </summary>
            public double TimeStart;
            /// <summary>
            /// The actor blocking the view
            /// </summary>
            public GameActor Actor;
            /// <summary>
            /// Urgent if the camera is embedded in me, not urgent if
            /// I'm just in the LOS.
            /// </summary>
            public bool Urgent;
            /// <summary>
            /// The time to ramp up the effect.
            /// </summary>
            private static float GhostUp
            {
                get { return 0.22f; }
            }
            /// <summary>
            /// The time to ramp off the effect. Must be greater than GhostUp
            /// </summary>
            public static float Decay
            {
                get { return 0.25f; }
            }
            /// <summary>
            /// Ramp up time for glow flash
            /// </summary>
            private static float GlowUp
            {
                get { return GhostUp * 0.25f; }
            }
            /// <summary>
            /// Ramp down time for glow flash
            /// </summary>
            private static float GlowDown
            {
                get { return GhostUp - GlowUp; }
            }
            /// <summary>
            /// Length of the glow flash
            /// </summary>
            private static float GlowLength
            {
                get { return GlowUp + GlowDown; }
            }
            /// <summary>
            /// Opacity once effect is fully on.
            /// </summary>
            private static float MinOpacity
            {
                get { return 0.2f; }
            }
            /// <summary>
            /// Glow amount when glow peaks.
            /// </summary>
            private static float FullGlow
            {
                get { return 0.25f; }
            }

            /// <summary>
            /// Whether we need to stay in urgent mode.
            /// </summary>
            public bool StillUrgent
            {
                get
                {
                    return (Time.WallClockTotalSeconds - TimeStart) < GlowLength;
                }
            }
            /// <summary>
            /// Current opacity
            /// </summary>
            public float Opacity
            {
                get
                {
                    if (Urgent)
                    {
                        return MinOpacity;
                    }
                    float t = (float)(Time.WallClockTotalSeconds - TimeStart);
                    t /= GhostUp;
                    t = MathHelper.Clamp(t, 0.0f, 1.0f);
                    t = MyMath.SmoothStep(0.0f, 1.0f, t);

                    return 1.0f + t * (MinOpacity - 1.0f);
                }
            }
            /// <summary>
            /// Current glow
            /// </summary>
            public float Glow
            {
                get
                {
                    float t = (float)(Time.WallClockTotalSeconds - TimeStart);
                    float tEnd = (float)(Time.WallClockTotalSeconds - TimeOccluding);
                    if (Decay - tEnd < GlowLength)
                    {
                        t = Decay - tEnd;
                    }
                    if (t <= GhostUp)
                    {
                        t /= GhostUp;
                    }
                    else if (t >= GlowLength - GlowDown)
                    {
                        t = (GlowLength - t) / GlowDown;
                    }
                    else
                    {
                        t = 1.0f;
                    }
                    return t * FullGlow;
                }
            }
            #endregion Accessors
        }

        /// <summary>
        /// Compare ghosts by distance to a reference point. Must set reference point before sort.
        /// </summary>
        private class CompareGhostByDist : IComparer<Ghost>
        {
            public Vector3 RefPos = Vector3.Zero;
            public int Compare(Ghost lhs, Ghost rhs)
            {
                /// Hack check if we're comparing self, to placate Sort's assertion.
                if (lhs == rhs)
                    return 0;

                float lftDist = Vector3.DistanceSquared(RefPos, lhs.Actor.Movement.Position);
                float rgtDist = Vector3.DistanceSquared(RefPos, rhs.Actor.Movement.Position);
                if (lftDist > rgtDist)
                    return -1;
                if (lftDist < rgtDist)
                    return 1;
                return 0;
            }
        }

        /// <summary>
        /// Compare ghosts by time last occluding
        /// </summary>
        private class CompareGhostByTime : IComparer<Ghost>
        {
            public int Compare(Ghost lhs, Ghost rhs)
            {
                if (lhs.TimeOccluding < rhs.TimeOccluding)
                    return -1;
                if (lhs.TimeOccluding > rhs.TimeOccluding)
                    return 1;
                return 0;
            }
        }

        /// <summary>
        /// Sort ghosts by actor unique number (ArbitraryComparable)
        /// </summary>
        private class CompareGhostByActor : IComparer<Ghost>
        {
            public int Compare(Ghost lhs, Ghost rhs)
            {
                if (lhs.Actor.UniqueNum < rhs.Actor.UniqueNum)
                    return -1;
                if (lhs.Actor.UniqueNum > rhs.Actor.UniqueNum)
                    return 1;
                return 0;
            }
        }

        /// <summary>
        /// Sort actors by unique number (ArbitraryComparable)
        /// </summary>
        private class CompareActor : IComparer<GameActor>
        {
            public int Compare(GameActor lhs, GameActor rhs)
            {
                if (lhs.UniqueNum < rhs.UniqueNum)
                    return -1;
                if (lhs.UniqueNum > rhs.UniqueNum)
                    return 1;
                return 0;
            }
        }

        /// <summary>
        /// Static sort object for GC
        /// </summary>
        private static CompareGhostByDist _CompareGhostByDist = new CompareGhostByDist();

        /// <summary>
        /// Static sort object for GC
        /// </summary>
        private static CompareGhostByTime _CompareGhostByTime = new CompareGhostByTime();

        /// <summary>
        /// Static sort object for GC
        /// </summary>
        private static CompareGhostByActor _CompareGhostByActor = new CompareGhostByActor();

        /// <summary>
        /// Static sort object for GC
        /// </summary>
        private static CompareActor _CompareActors = new CompareActor();

        #endregion Child Classes

        #region Members

        private List<Ghost> ghostList = new List<Ghost>();

        #region Scratch
        /// <summary>
        /// These are all just scratch lists for GC friendliness. They aren't persistent
        /// in the least, they are just scratch space during update.
        /// </summary>
        private List<Ghost> _scratchGhosts = new List<Ghost>();
        private static List<HitInfo> _scratchLOS = new List<HitInfo>();
        private static List<GameActor> _scratchActors = new List<GameActor>();
        private static List<GameActor> _scratchToAdd = new List<GameActor>();
        private static List<GameActor> _scratchExclude = new List<GameActor>();
        #endregion Scratch

        #endregion Members

        #region Accessors
        #endregion Accessors

        /// The external API. These are private functions, but 
        /// are the only API called from elsewhere for ghosting operations.
        #region Public

        /// <summary>
        /// Reset the system.
        /// </summary>
        private void ClearGhosts()
        {
            for (int i = ghostList.Count - 1; i >= 0; --i)
            {
                UnGhost(i);
            }
            Debug.Assert(ghostList.Count == 0);
        }

        /// <summary>
        /// Render all ghost effects
        /// </summary>
        /// <param name="camera"></param>
        private void RenderGhosts(Camera camera)
        {
            if (ghostList.Count > 0)
            {
                if (ghostList.Count > 1)
                {
                    ghostList.Sort(_CompareGhostByDist);
                }

                RenderEffect was = renderEffects;
                renderEffects = RenderEffect.GhostPass;

                for (int i = 0; i < ghostList.Count; ++i)
                {
                    float opacity = ghostList[i].Opacity;
                    float glow = ghostList[i].Glow;

                    ShaderGlobals.FixExplicitBloom(glow);
                    ShaderGlobals.FixBloomColor(new Vector4(opacity, opacity, opacity, 1.0f));
                    ghostList[i].Actor.RenderObject.Render(camera);
                }
                ShaderGlobals.ReleaseExplicitBloom();
                ShaderGlobals.ReleaseBloomColor();

                renderEffects = was;
            }
        }

        /// <summary>
        /// Check for camera occlusion, building up and tearing down
        /// the ghost list as appropriate.
        /// </summary>
        /// <param name="camera"></param>
        private void CheckGhost(Camera camera)
        {
            PrimeScratchExclude();

            double currentTime = Time.WallClockTotalSeconds;
            CheckInvisible(currentTime);

            // Ignore LOS checks for ghosting if in KeyMouse mode.
            if (GamePadInput.ActiveMode != GamePadInput.InputMode.KeyboardMouse)
            {

                /// Do LOS check and find everything intersecting sphere around
                /// the camera's current position.
                /// 
                CheckCameraSphere(camera);
                MergeNewGhosts(_scratchActors, ghostList, true, currentTime);

                /// Do another LOS check with a smaller sphere, but extruded from
                /// camera position to camera target object position.
                /// Target object is:
                /// 1) Free camera (including edit mode) - cursor
                /// 2) Follow camera, cameraFocusList.Count==1  - cameraFocusList[0]
                /// There is _NO_ target object for the following modes, and this
                /// second LOS check is skipped:
                /// 1) First person
                /// 2) cameraFocusList.Count > 1
                /// 3) Fixed position camera
                if (CheckLOSToTarget(camera))
                {
                    MergeNewGhosts(_scratchActors, ghostList, false, currentTime);
                }
            }

            PruneOldGhosts(currentTime);
        }
        #endregion Public

        /// These are internal to the workings here, they have no place
        /// being called elsewhere by inGame (or anyone else).
        #region Internal

        /// <summary>
        /// Find anything flagged as invisible in the world. When editing,
        /// we'll display them as ghosted too.
        /// </summary>
        /// <param name="camera"></param>
        private void CheckInvisible(double currentTime)
        {
            Debug.Assert(_scratchActors.Count == 0);

            for (int i = 0; i < gameThingList.Count; ++i)
            {
                GameActor actor = gameThingList[i] as GameActor;
                if ((actor != null) && (actor.Invisible
#if MF_GHOST_IGNORED
                                        || actor.Ignored
                                        || actor.Camouflaged
#endif // MF_GHOST_IGNORED
                                        ))
                {
                    _scratchActors.Add(actor);
                }
            }
            MergeNewGhosts(_scratchActors, ghostList, true, currentTime);

            _scratchActors.Clear();
        }

        /// <summary>
        /// Just see what's intersecting the sphere around the camera.
        /// </summary>
        /// <param name="camera"></param>
        private void CheckCameraSphere(Camera camera)
        {
            Debug.Assert(_scratchLOS.Count == 0);
            Debug.Assert(_scratchActors.Count == 0);
            Vector3 camPos = camera.ActualFrom;
            float camRadius = camera.NearClip * 5.0f;
            if (CollSys.TestAll(
                camPos,
                camPos,
                camRadius,
                _scratchLOS))
            {
                ActorsFromHitInfo(_scratchLOS, _scratchActors);
            }
            _scratchLOS.Clear();

        }

        /// <summary>
        /// Put things that should be excluded from ghosting in the exclude list.
        /// </summary>
        private void PrimeScratchExclude()
        {
            _scratchExclude.Clear();
            /// First person actor disappears on its own.
            if (CameraInfo.FirstPersonActor != null)
            {
                _scratchExclude.Add(CameraInfo.FirstPersonActor);
            }
            /// Edit focus object should never ghost.
            GameActor editFocusObject = EditFocusObject;
            if (editFocusObject != null)
            {
                _scratchExclude.Add(editFocusObject);
            }
            /// If we're running the sim, then the camera focus list actors
            /// shouldn't be ghosted, they're in focus.
            if (CurrentUpdateMode == UpdateMode.RunSim)
            {
                for (int i = 0; i < CameraInfo.MergedFollowList.Count; ++i)
                {
                    _scratchExclude.Add(CameraInfo.MergedFollowList[i]);
                }
            }
        }

        /// <summary>
        /// Pull out the edit focus object, if any.
        /// </summary>
        private GameActor EditFocusObject
        {
            get
            {
                EditObjectUpdateObj editObj = updateObj as EditObjectUpdateObj;
                if ((editObj != null) && (editObj.editFocusObject is GameActor))
                {
                    return editObj.editFocusObject as GameActor;
                }
                return null;
            }
        }

        /// <summary>
        /// Determine whether we need to do an LOS check, false if
        /// the sphere test around the camera is enough.
        /// </summary>
        /// <returns></returns>
        private bool NeedCheckLOSToTarget()
        {
            if (CameraInfo.FirstPersonActive)
            {
                return false;
            }
            if (CameraInfo.Mode == CameraInfo.Modes.FixedTarget)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determine whether we're just checking for the cursor, or 
        /// checking LOS to actors in the scene.
        /// </summary>
        private bool CursorOnlyCheck
        {
            get
            {
                return (CameraInfo.MergedFollowList.Count == 0)
                    || (CurrentUpdateMode != UpdateMode.RunSim);
            }
        }

        /// <summary>
        /// Clamp a ray to a max length.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="at"></param>
        /// <param name="maxDist"></param>
        /// <returns></returns>
        private Vector3 LimitRay(Vector3 from, Vector3 at, float maxDist)
        {
            at -= from;
            float length = at.Length();
            at /= length;
            length = Math.Min(length, maxDist);
            at *= length;
            at += from;

            return at;
        }

        /// <summary>
        /// Do ray casts from the camera to appropriate focus objects.
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        private bool CheckLOSToTarget(Camera camera)
        {
            Debug.Assert(_scratchLOS.Count == 0);
            Debug.Assert(_scratchActors.Count == 0);
            if (NeedCheckLOSToTarget())
            {
                if (CursorOnlyCheck)
                {
                    /// Single check from camera to cursor.
                    Vector3 targetPos = shared.CursorPosition;
                    GameActor editFocusObject = EditFocusObject;
                    if (editFocusObject != null)
                    {
                        targetPos = editFocusObject.WorldCollisionCenter;
                    }

                    // We want to limit the ghosting to objects that are no more than MaxCursorRay units
                    // from the camera.  This prevents distant object from being ghosted since they're
                    // probably too small to matter.
                    // We also want to prevetn ghosting if the bot is too close to the cursor.  Otherwise
                    // the ghosting looks like the bot is being selected.

                    float MaxCursorRay = 20.0f;
                    float MinCursorRay = 5.0f;
                    Vector3 cameraFrom = camera.ActualFrom;
                    targetPos = LimitRay(cameraFrom, targetPos, MaxCursorRay);
                    targetPos -= MinCursorRay * camera.ViewDir;

                    float camRadius = 0.1f;

                    CheckLOSToTarget(cameraFrom, targetPos, camRadius);
                }
                else
                {
                    /// A check from camera to each focus object.
                    for (int i = 0; i < CameraInfo.MergedFollowList.Count; ++i)
                    {
                        Vector3 targetPos = CameraInfo.MergedFollowList[i].WorldCollisionCenter;
                        float MaxCursorRay = 20.0f;
                        targetPos = LimitRay(camera.ActualFrom, targetPos, MaxCursorRay);

                        float camRadius = 0.1f;

                        CheckLOSToTarget(camera.ActualFrom, targetPos, camRadius);
                    }
                }
            }
            return _scratchActors.Count > 0;
        }

        /// <summary>
        /// Do the single check from camera to the fixed position.
        /// Hit actors put in _scratchActors.
        /// </summary>
        /// <param name="cameraPos"></param>
        /// <param name="targetPos"></param>
        /// <param name="radius"></param>
        private void CheckLOSToTarget(Vector3 cameraPos, Vector3 targetPos, float radius)
        {
            Debug.Assert(_scratchLOS.Count == 0);
            if (CollSys.TestAll(
                cameraPos,
                targetPos,
                radius,
                _scratchLOS))
            {
                ActorsFromHitInfo(_scratchLOS, _scratchActors);
            }
            _scratchLOS.Clear();
        }

        /// <summary>
        /// Pull eligible actors from the hitInfo list and put them in dst.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        private void ActorsFromHitInfo(List<HitInfo> src, List<GameActor> dst)
        {
            /// Don't worry about duplicates, we'll prune them later.
            for (int i = 0; i < src.Count; ++i)
            {
                if ((_scratchExclude == null) || !_scratchExclude.Contains(src[i].Other))
                {
                    dst.Add(src[i].Other);
                }
            }
        }

        /// Now that we have the list of things needing ghosting this frame,
        /// go through both lists, newList and ghostList.
        /// If an object is in both
        ///     refresh its time
        /// else if it is in newList
        ///     add to ghostList
        /// else (only in ghostList) if it has timed out
        ///     remove from ghostList
        
        /// <summary>
        /// Merge the new list into the ghost list, eliminating duplicates and 
        /// refreshing existing ghosts.
        /// </summary>
        /// <param name="newList"></param>
        /// <param name="ghosts"></param>
        /// <param name="currentTime"></param>
        private void MergeNewGhosts(List<GameActor> newList, List<Ghost> ghosts, bool urgent, double currentTime)
        {
            if (ghosts.Count > 1)
            {
                ghosts.Sort(_CompareGhostByActor);
            }
            if (newList.Count > 1)
            {
                newList.Sort(_CompareActors);
                PruneDuplicates(newList);
            }

            Debug.Assert(_scratchToAdd.Count == 0);
            int iGhost = 0;
            for (int i = 0; i < newList.Count; ++i)
            {
                while ((iGhost < ghostList.Count)
                    && (ghostList[iGhost].Actor.UniqueNum < _scratchActors[i].UniqueNum))
                {
                    ++iGhost;
                }

                if ((iGhost < ghostList.Count) && (ghostList[iGhost].Actor == _scratchActors[i]))
                {
                    /// Found it, refresh
                    ghostList[iGhost].TimeOccluding = currentTime;
                    ghostList[iGhost].Urgent = ghostList[iGhost].StillUrgent || urgent;
                }
                else
                {
                    _scratchToAdd.Add(newList[i]);
                }
            }
            newList.Clear();
            for (int i = 0; i < _scratchToAdd.Count; ++i)
            {
                AddGhost(_scratchToAdd[i], urgent, currentTime);
            }
            _scratchToAdd.Clear();
        }

        /// <summary>
        /// Prune all duplicate actors out of the list.
        /// </summary>
        /// <param name="list"></param>
        private void PruneDuplicates(List<GameActor> list)
        {
            if (list.Count > 1)
            {
                int dst = 1;
                int lastID = list[0].UniqueNum;
                for (int src = 1; src < list.Count; ++src)
                {
                    if (list[src].UniqueNum != lastID)
                    {
                        list[dst] = list[src];
                        lastID = list[dst].UniqueNum;
                        ++dst;
                    }
                }
                if (dst < list.Count)
                {
                    list.RemoveRange(dst, list.Count - dst);
                }
            }
        }

        /// <summary>
        /// Prune out expired ghosts from the ghostList.
        /// </summary>
        /// <param name="currentTime"></param>
        private void PruneOldGhosts(double currentTime)
        {
            double oldest = currentTime - Ghost.Decay;
            for(int i = ghostList.Count - 1; i >= 0; --i)
            {
                if(ghostList[i].TimeOccluding < oldest)
                {
                    UnGhost(i);
                }
                else if ((ghostList[i].Actor.CurrentState == GameThing.State.Inactive)
                    || (!ghostList[i].Actor.Invisible 
#if MF_GHOST_IGNORED
                        && !ghostList[i].Actor.Ignored
                        && !ghostList[i].Actor.Camouflaged
#endif // MF_GHOST_IGNORED
                        && !ghostList[i].Actor.Visible))
                {
                    UnGhost(i);
                }
            }
        }

        /// <summary>
        /// Get a ghost from the scratch list, to avoid GC.
        /// </summary>
        /// <returns></returns>
        private Ghost ScratchGhost()
        {
            Ghost ret = null;
            if (_scratchGhosts.Count > 0)
            {
                ret = _scratchGhosts[_scratchGhosts.Count - 1];
                _scratchGhosts.RemoveAt(_scratchGhosts.Count - 1);
            }
            else
            {
                ret = new Ghost();
            }
            return ret;
        }

        /// <summary>
        /// Set up the actor as a ghost, pulling from the regular scene render
        /// and embedding in our ghost list.
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="urgent"></param>
        /// <param name="currentTime"></param>
        private void AddGhost(GameActor actor, bool urgent, double currentTime)
        {
            if (actor.Invisible 
#if MF_GHOST_IGNORED
                || actor.Ignored
                || actor.Camouflaged
#endif // MF_GHOST_IGNORED
                || actor.Visible)
            {
                Ghost ghost = ScratchGhost();
                ghost.TimeOccluding = currentTime;
                ghost.TimeStart = currentTime;
                ghost.Actor = actor;
                ghost.Urgent = urgent;

                actor.DisableEmitters();

                /// Remove from our renderlist
                renderObj.renderList.Remove(actor.RenderObject);

                ghostList.Add(ghost);
            }
        }

        /// <summary>
        /// Pull actor out of our ghost list and reinstate in the scene's regular render..
        /// </summary>
        /// <param name="idx"></param>
        private void UnGhost(int idx)
        {
            Ghost ghost = ghostList[idx];

            GameActor actor = ghost.Actor;
            if (actor.Visible && (actor.CurrentState != GameThing.State.Inactive))
            {
                renderObj.renderList.Add(actor.RenderObject);
                // Don't enable emitters for dead or squashed things.
                if (actor.CurrentState != GameThing.State.Dead && actor.CurrentState != GameThing.State.Squashed)
                {
                    actor.EnableEmitters();
                }
            }

            _scratchGhosts.Add(ghost);
            ghostList.RemoveAt(idx);
        }

        #endregion Internal

    }
}
