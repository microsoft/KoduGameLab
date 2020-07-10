// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;

using Boku.Base;
using Boku.Common;

using Boku.SimWorld.Chassis;

namespace Boku.SimWorld.Collision
{
    #region Helper Structs
    /// <summary>
    /// Collision info reporting struct used internal to the collision system.
    /// </summary>
    public struct CollInfo
    {
        /// <summary>
        /// Distance squared travelled before impact
        /// </summary>
        public float DistSq; 
        /// <summary>
        /// sphere center at contact time
        /// </summary>
        public Vector3 Center;
        /// <summary>
        /// Point of contact
        /// </summary>
        public Vector3 Contact; 
        /// <summary>
        /// Normal at point of contact, pointing toward sphere
        /// </summary>
        public Vector3 Normal;
        /// <summary>
        /// Center of other at impact, if it was a mover
        /// </summary>
        public Vector3 Struck;
        /// <summary>
        /// True if they were already in contact at the frame start
        /// </summary>
        public bool Touching;

        /// <summary>
        /// The lowest level primitive hit (for composites)
        /// </summary>
        public CollisionPrimitive LowPrimitive;
        /// <summary>
        /// The highest level composite body.
        /// </summary>
        public CollisionPrimitive TopPrimitive;
    }

    /// <summary>
    /// Collision reporting struct returned to the outside world.
    /// </summary>
    public struct HitInfo
    {
        /// <summary>
        /// The guy getting hit by you
        /// </summary>
        public GameActor Other;

        /// <summary>
        /// A possibly null ref to the other mover. Will be null if the primitive
        /// struck is not a mover.
        /// </summary>
        internal Mover OtherMover;

        /// <summary>
        /// Where your "center" was when you got hit.
        /// </summary>
        public Vector3 Center;

        /// <summary>
        /// Point of contact
        /// </summary>
        public Vector3 Contact;

        /// <summary>
        /// Amount you will have to move to stop colliding with Other.
        /// </summary>
        public Vector3 Offset;

        /// <summary>
        /// Where other guy's center was at time of impact. Will be Zero for non-movers.
        /// </summary>
        public Vector3 Struck;

        /// <summary>
        /// Surface normal at contact point.
        /// </summary>
        public Vector3 Normal;

        /// <summary>
        /// True if they were already in contact at the beginning of the frame.
        /// </summary>
        public bool Touching;

        /// <summary>
        /// True if this is the recipricating second call having 
        /// already been processed by the first of the colliding pair.
        /// </summary>
        public bool Handled;

        /// <summary>
        /// Distance from start to impact point. Used for sorting only, probably
        /// doesn't mean what you think. 
        /// It's actually Vector3.DistanceSquared(StartingPosition, Contact);
        /// Note that that's from the starting center of the sphere to the contact
        /// point on the surface of the ending position sphere.
        /// </summary>
        public float DistSq;

        /// <summary>
        /// Time when this happened.
        /// </summary>
        public double TimeStamp;
    }
    #endregion Helper Structs

    /// <summary>
    /// Collision detection module.
    /// </summary>
    public class CollSys
    {
        #region Members
        private List<CollisionPrimitive> things = new List<CollisionPrimitive>();

        private List<Mover> movers = new List<Mover>();

        private List<CollInfo> hits = new List<CollInfo>();

        private static CollSys Sys = new CollSys();
        #endregion Members

        #region Accessors
        #endregion Accessors

        #region Public
        /// <summary>
        /// Give all primitives a chance to sync up with their transforms.
        /// </summary>
        public static void Update()
        {
            Sys.UpdateTransforms();
            Sys.CollisionCheck();
        }

        /// <summary>
        /// Return hit info for the first thing hit between p0 and p1. Will return multiple
        /// hits if p0 is already in contact with multiple things. Client is responsible
        /// for clearing hits, we only append here.
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="radius"></param>
        /// <param name="hits"></param>
        public static bool TestBest(Vector3 p0, Vector3 p1, float radius, List<HitInfo> hits)
        {
            return Sys.CollisionCheck(p0, p1, new Vector3(radius), 0, false, hits);
        }

        /// <summary>
        /// Return hit info for ALL collisions between p0 and p1, sorted from soonest to latest.
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="radius"></param>
        /// <param name="hits"></param>
        /// <returns></returns>
        public static bool TestAll(Vector3 p0, Vector3 p1, float radius, List<HitInfo> hits)
        {
            return Sys.CollisionCheck(p0, p1, new Vector3(radius), 0, true, hits);
        }

        /// <summary>
        /// Register a dynamic mover for collision detection.
        /// </summary>
        /// <param name="owner"></param>
        public static void RegisterMover(GameActor owner)
        {
            Mover mover = new Mover();
            mover.Radius = owner.CollisionRadius;
            mover.SetOwner(owner);
            Sys.Register(mover);
        }

        public static void RefreshMoverCollision(GameActor owner)
        {
            Sys.Refresh(owner);
        }
        /// <summary>
        /// Register a static scene element for getting hit.
        /// </summary>
        /// <param name="owner"></param>
        public static void RegisterBlocker(GameActor owner)
        {
            CollisionPrimitive prim = owner.SRO.CollisionPrim(owner);
            if (prim != null)
            {
                Sys.Register(prim);
            }
            else
            {
                // We're looking for collision prims here but haven't found any.
                // What should happen here?
            }
        }
        /// <summary>
        /// Unregister the mover so it won't get tested any more.
        /// </summary>
        /// <param name="owner"></param>
        public static void UnregisterMover(GameActor owner)
        {
            Sys.Unregister(owner, true);
        }
        /// <summary>
        /// Unregister a blocker so it won't block movers any more.
        /// </summary>
        /// <param name="owner"></param>
        public static void UnregisterBlocker(GameActor owner)
        {
            Sys.Unregister(owner, false);
        }

        #endregion Public

        #region Internal

        private CollSys()
        {
        }

        private void UpdateTransforms()
        {
            foreach (CollisionPrimitive prim in things)
            {
                prim.UpdateTransforms();
            }

            foreach (Mover mover in movers)
            {
                mover.UpdateTransforms();
            }
        }
        private Vector3 MakeOffset(HitInfo hitInfo, float radius)
        {
            if (Vector3.DistanceSquared(hitInfo.Center, hitInfo.Contact) 
                < radius * radius)
            {
                Vector3 newCenter = hitInfo.Contact + hitInfo.Normal * radius;
                return (newCenter - hitInfo.Center) * 1.05f;
            }
            return Vector3.Zero;
        }
        /// <summary>
        /// Fill in a hitInfo struct from the input.
        /// </summary>
        /// <param name="best"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        private HitInfo MakeHitScratch(CollInfo best, float radius)
        {
            GameActor other = best.TopPrimitive.Owner;
            HitInfo hitScratch = new HitInfo();
            hitScratch.Contact = best.Contact;
            hitScratch.Center = best.Center;
            hitScratch.Normal = Vector3.Normalize(best.Normal);
            hitScratch.DistSq = best.DistSq;
            hitScratch.Other = other;
            hitScratch.Struck = best.Struck;
            hitScratch.OtherMover = best.TopPrimitive as Mover;

            hitScratch.Offset = MakeOffset(hitScratch, radius);

            hitScratch.Touching = best.Touching;
            hitScratch.Handled = false;
            hitScratch.TimeStamp = Time.GameTimeTotalSeconds;

            return hitScratch;
        }
        private void AdjustDelta(Mover mover, HitInfo hitInfo)
        {
            if (mover != null)
            {
                /// We'll only allow a change in position if we are moving into each other.
                if (Vector3.Dot(mover.Delta, hitInfo.Contact - mover.Center) >= 0)
                {
                    GameActor owner = mover.Owner;
                    // If this is a fixed position, don't allow it to move.
                    // This is either due to being a static prop chassis or
                    // having the immobile flag set.
                    bool mobile = !(owner.Chassis.FixedPosition || owner.TweakImmobile || owner.TweakImmobileNoRot);
                    if (mobile)
                    {
                        // Seagrass hack, don't have collisions with seagrass affect position of mover.
                        if (hitInfo.Other.Classification.name != "seagrass")
                        {
                            Vector3 endPosition = hitInfo.Center + hitInfo.Offset;

                            mover.Delta = endPosition - mover.Center;

                            owner.Movement.Position += mover.Delta;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Flag used to tell if the LastCloneThing ref should be nulled.
        /// </summary>
        private bool clearLastClonedThing = true;

        /// <summary>
        /// Give the actor hitting the other and the other a chance
        /// to respond to impact.
        /// </summary>
        /// <param name="mover"></param>
        /// <param name="hitInfo"></param>
        private void ApplyCollision(Mover mover, HitInfo hitInfo)
        {
            Debug.Assert(hitInfo.Other != null);
            if (mover.Owner.ActorHoldingThis == hitInfo.Other)
            {
                /// I'm touching what's holding me.
                return;
            }
            if (hitInfo.Other.ActorHoldingThis == mover.Owner)
            {
                /// I'm touching what I'm holding.
                return;
            }

            if (InGame.inGame.IsPickedUp(mover.Owner) && InGame.inGame.LastClonedThing == hitInfo.Other)
            {
                // Don't collide the selected object with the recent clone.
                clearLastClonedThing = false;
                return;
            }

            mover.Owner.ApplyCollisions(ref hitInfo);
            AdjustDelta(mover, hitInfo);

            GameActor other = hitInfo.Other;

            // The ordering of tests can change so we need to check if either of 
            // the acotrs involved are missiles.
            if (mover.Owner is CruiseMissile || other is CruiseMissile)
            {
                // Actor has been hit by cruise missile.
                // Yes, it kind of sucks to have this here but none of the collision
                // information is stored on the Actor so we have to use it while we have it.
                // Note that we don't allow missiles to hit their launcher.
                if (other is CruiseMissile)
                {
                    MissileChassis mc = other.Chassis as MissileChassis;
                    if (mc != null && mc.Launcher != mover.Owner)
                    {
                        mc.HitTarget(mover.Owner, hitInfo);
                    }
                }
                else
                {
                    MissileChassis mc = mover.Owner.Chassis as MissileChassis;
                    if (mc != null && mc.Launcher != other)
                    {
                        mc.HitTarget(other, hitInfo);
                    }
                }
            }
            else
            {
                // Normal path that happens when two actors bump.
                hitInfo.Center = hitInfo.Struck;
                hitInfo.Normal = -hitInfo.Normal;
                hitInfo.Other = mover.Owner;
                hitInfo.Offset = MakeOffset(hitInfo, other.CollisionRadius);
                other.ApplyCollisions(ref hitInfo);
                AdjustDelta(hitInfo.OtherMover, hitInfo);
            }

        }   // end of ApplyCollision()

        /// <summary>
        /// Run collision checks for all registered movers.
        /// </summary>
        private void CollisionCheck()
        {
            float dt = Time.GameTimeFrameSeconds;
            clearLastClonedThing = true;    // Init to true.  Will get set to false if
                                            // selected obj collides w/ recent clone.

            /// These are structs, no real allocation done here.
            
            // Loop over all the movers comparing each one against all
            // the remaining ones in the list AND any non-moving props.
            // NOTE It may seem strange that this loop ends at movers.Count
            // rather than movers.Count-1.  After all, for the last mover
            // in the list there is no one to collide with.  BUT further 
            // down we do another round of collision testing for actors
            // with TouchCushions which are used to force bump events
            // between hovering bots and low things like apples.  For
            // that to work, this loop must cover all the movers in the list.
            for (int first = 0; first < movers.Count; ++first)
            {
                Mover mover = movers[first];
                mover.Owner.ClearHits();
                if (mover.Owner.Ignored)
                {
                    continue;
                }

                // Get squashed radii for mover.
                Vector3 moverRadii = new Vector3(mover.Radius) * mover.Owner.SquashScale;

                Vector3 start = mover.Center;
                Vector3 end = start + mover.Delta;

                hitInfo_ScratchList.Clear();

                // Do "real" collision test to determine if things collide.  These collisions are
                // then applied to both the physics and the Bumped sensor.
                if (CollisionCheck(start, end, moverRadii, first + 1, false, hitInfo_ScratchList))
                {
                    for (int i = 0; i < hitInfo_ScratchList.Count; ++i)
                    {
                        ApplyCollision(mover, hitInfo_ScratchList[i]);
                    }
                }


                // Do another round of collision testing but this time only for movers with
                // a non-zero TouchCushion.  Note that we have to test against all other
                // movers, not just the ones after us in the list.  These results are only
                // applied to the Bumped sensor, not the physics.
                hitInfo_ScratchList.Clear();
                if (mover.TouchCushion > 0.0f)
                {
                    float maxHeight = start.Z - mover.Radius * 0.5f;
                    end = start;
                    end.Z -= mover.TouchCushion;
                    if (CollisionCheck(start, end, moverRadii, first + 1, true, hitInfo_ScratchList))
                    {
                        for (int i = 0; i < hitInfo_ScratchList.Count; ++i)
                        {
                            HitInfo hit = hitInfo_ScratchList[i];
                            if ((hit.Other != mover.Owner)
                                && ((hit.OtherMover == null)
                                    || (hit.OtherMover.Center.Z + hit.OtherMover.Radius < maxHeight)))
                            {
                                mover.Owner.AddTouched(hit.Other,
                                    hit.Other.Movement.Position - mover.Owner.Movement.Position,
                                    Vector3.Distance(hit.Center, hit.Contact));
                                break;
                            }
                        }
                    }
                }

                hitInfo_ScratchList.Clear();
            }

            // If this is still set to true that means that the selected obj and 
            // the most recent clone did not collide so it's safe to clear the ref.
            if(clearLastClonedThing)
            {
                InGame.inGame.LastClonedThing = null;
            }
        }
        private static List<HitInfo> hitInfo_ScratchList = new List<HitInfo>();

        /// <summary>
        /// Start, end and radii are passed in from a mover.  This method tests
        /// this mover's swept ellipsoid against all the non-moving things as
        /// well as all the other movers which appear after it in the list.
        /// The "first" param is the index of the next item in the list.
        /// 
        /// The listAll param seems to determine whether we just keep the nearest hit
        /// or keep them all.  It appears that listAll=false is used for physics collision
        /// testing while listAll=true is used for WHEN Bumped testing.  But, of course,
        /// this is just a guess on my part since it's not documented.  Argh.
        /// </summary>
        /// <param name="startPos0">start position of swept sphere</param>
        /// <param name="endPos0">end position of swept sphere</param>
        /// <param name="radii0">Radii of swept ellipsoid.</param>
        /// <param name="first">This is the index of the first mover to check for collisions with.  Start, end, and radii come from the previous mover.  TODO (****) Change numbering so this is more clear.</param>
        /// <param name="listAll">When true, all hits are listed.  When false, only the nearest hit is listed.</param>
        /// <param name="hits"></param>
        /// <returns></returns>
        private bool CollisionCheck(Vector3 startPos0, Vector3 endPos0, Vector3 radii0, int first, bool listAll, List<HitInfo> hits)
        {
            /// These are structs, no real allocation done here.
            CollInfo curCollPrim = new CollInfo();
            curCollPrim.DistSq = Single.MaxValue;

            CollInfo best = new CollInfo();
            best.DistSq = Single.MaxValue;
            bool hit = false;

            // Test the current swept ellipsoid against all the non-moving things.
            List<CollisionPrimitive> relevants = things;
            for (int ithing = 0; ithing < relevants.Count; ++ithing)
            {
                CollisionPrimitive thing = relevants[ithing];

                if (thing.Owner.Ignored)
                {
                    continue;
                }

                // Note we used the Z part of the radii.  So, on squashed movers we
                // might get some intersection.
                // TODO (****) Actually figure out how to test a swept ellipsoid against
                // the Primitive based collision shapes.
                if (thing.Collide(startPos0, endPos0, radii0.Z, ref curCollPrim))
                {
                    if (listAll)
                    {
                        HitInfo hitScratch = MakeHitScratch(curCollPrim, radii0.X);
                        hits.Add(hitScratch);
                        curCollPrim.DistSq = Single.MaxValue;
                    }
                    else
                    if (curCollPrim.DistSq <= best.DistSq)
                    {
                        hit = true;
                        best = curCollPrim;
                        endPos0 = best.Center;

                        if (best.Touching)
                        {
                            HitInfo hitScratch = MakeHitScratch(best, radii0.X);
                            hits.Add(hitScratch);
                        }
                    }
                }
            }

            // Test the current mover against the remaining movers in the list.
            // Note that in the case of hit testing blips this index may be -1.
            int curMoverIndex = first - 1;

            // If listAll is true, then we're testing for bump so test the full
            // list since we have to worry about TouchCushions.
            if (listAll)
            {
                first = 0;
            }
            for (int second = first; second < movers.Count; ++second)
            {
                Mover sphere = movers[second];

                // Don't test against self.
                if (second == curMoverIndex)
                {
                    continue;
                }

                if (sphere.Owner.Ignored)
                {
                    continue;
                }

                // Don't collide missiles with their launcher.  Note that this assumes
                // that the launcher is always in the list _ahead_ of the missile.
                MissileChassis mc = movers[second].Owner.Chassis as MissileChassis;
                if (mc != null && curMoverIndex >= 0 && mc.Launcher == movers[curMoverIndex].Owner)
                {
                    continue;
                }

                Vector3 radii1 = sphere.Radius * sphere.Owner.SquashScale;

                Vector3 hitContactPosition = Vector3.Zero;
                Vector3 hitNormal = Vector3.UnitZ;
                float hitT = -1;
                bool hitTest = SweptPrims.SweptEllipsoidSweptEllipsoid(startPos0, endPos0, radii0, sphere.Center, sphere.Center + sphere.Delta, sphere.Radius * sphere.Owner.SquashScale, ref hitContactPosition, ref hitNormal, ref hitT);

                if (hitTest)
                {
                    Vector3 posAtCollision = MyMath.Lerp(startPos0, endPos0, hitT);
                    // Already touching?
                    if (hitT <= 0)
                    {
                        sphere.SetCollPrimTouching(startPos0, posAtCollision, hitContactPosition, -hitNormal, sphere.Center + hitT * sphere.Delta, ref curCollPrim);
                    }
                    else
                    {
                        sphere.SetCollPrim(startPos0, posAtCollision, hitContactPosition, -hitNormal, sphere.Center + hitT * sphere.Delta, ref curCollPrim);
                    }

                    if (listAll)
                    {
                        HitInfo hitScratch = MakeHitScratch(curCollPrim, radii0.X);
                        hits.Add(hitScratch);
                        curCollPrim.DistSq = Single.MaxValue;
                    }
                    else
                        if (curCollPrim.DistSq <= best.DistSq)
                        {
                            best = curCollPrim;
                            hit = true;

                            if (best.Touching)
                            {
                                HitInfo hitScratch = MakeHitScratch(best, radii0.X);
                                hits.Add(hitScratch);
                            }
                        }

                }   // end if hitTest

            }   // end of loop over other movers.

            if (hit)
            {
                Debug.Assert(!listAll, "Hit shouldn't get set when listing all hits");
                /// If it's touching, we've already applied, otherwise
                /// do it here.
                if (!best.Touching)
                {
                    HitInfo hitScratch = MakeHitScratch(best, radii0.X);
                    hits.Add(hitScratch);
                }
            }
            if (listAll && (hits.Count > 1))
            {
                hits.Sort(comparer);
            }
            return hits.Count > 0;
        }

        private class CompareHits : IComparer<HitInfo>
        {
            public int Compare(HitInfo lhs, HitInfo rhs)
            {
                if (lhs.DistSq < rhs.DistSq)
                    return -1;
                if (lhs.DistSq > rhs.DistSq)
                    return 1;
                return 0;
            }
        }
        private static CompareHits comparer = new CompareHits();

        #region Bookkeeping

        private void Register(Mover mover)
        {
            movers.Add(mover);
        }

        private void Register(CollisionPrimitive prim)
        {
            things.Add(prim);
        }

        private void Register(List<CollisionPrimitive> prims)
        {
            foreach (CollisionPrimitive prim in prims)
            {
                Register(prim);
            }
        }

        private void Unregister(GameActor owner, bool moving)
        {
            if (moving)
            {
                for (int i = movers.Count - 1; i >= 0; --i)
                {
                    if (movers[i].Owner == owner)
                    {
                        movers.RemoveAt(i);
                    }
                }
            }
            else
            {
                for (int i = things.Count - 1; i >= 0; --i)
                {
                    if (things[i].Owner == owner)
                    {
                        things.RemoveAt(i);
                    }
                }
            }
        }

        private void Refresh(GameActor owner)
        {
            foreach (Mover m in movers)
            {
                if (m.Owner == owner)
                {
                    m.Radius = owner.CollisionRadius;
                    m.UpdateTransforms();
                }
            }
        }
        #endregion Bookkeeping

        #endregion Internal
    }
}
