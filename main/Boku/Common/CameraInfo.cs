// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


//#define DEBUG_SPEW

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;

namespace Boku.Common
{
    /// <summary>
    /// Static class for holding current camera mode information.
    /// </summary>
    public class CameraInfo
    {
        public enum Modes
        {
            Edit,           // Standard edit mode with user controlled cursor.
            Free = Edit,
            Actor,          // Follow current focus actor.  This only works for a single actor.
            MultiTarget,    // The camera is following multiple targets.
            FixedTarget,    // The user has specified a fixed position and orientation for the camera.
            FixedOffset,    // Camera is at a fixed world space offset from the target actor.
            NumModes,
        }

        #region Members

        // Current camera mode.
        private static Modes mode = Modes.Free;

        // Actor that the camera is currently tracking.
        private static GameActor cameraFocusGameActor = null;

        // Are we in first person?  Caused by user zooming into user controlled bot.
        private static bool firstPersonViaZoom = false;

        // Actually used like a stack except that we only allow a single instance
        // of each actor to be present.
        private static List<GameActor> brainFirstPersonStack = new List<GameActor>();
        private static List<GameActor> brainFollowMeList = new List<GameActor>();       // Bots programmed to have camera follow them.
        private static List<GameActor> brainUserControlledList = new List<GameActor>(); // User controlled bots.
        private static List<GameActor> brainIgnoreMeList = new List<GameActor>();       // Bots programmed to have camera ignore.
        private static List<GameActor> mergedFollowList = new List<GameActor>();        // Union of brainFollowMeList and brainUserControlledList.

        private static bool amFollowingActors;          // True if the camera is following actors as a result of the previous resolve call.
        private static bool wasFollowingActors;         // We need to know what this setting was in the last frame too.
        private static int firstPersonSetFrame;         // Frame when first person camera mode was last set.  Acts like an arbitrator and won't allow mode to change again this frame.
                                                        // Note this is an odd case for an arbitrator since it works across all bots.

        #endregion

        #region Accessors

        /// <summary>
        /// Controls the current camera mode.
        /// </summary>
        public static Modes Mode
        {
            get { return mode; }
            set 
            {
                if (mode != value)
                {
#if DEBUG_SPEW
                    switch(value)
                    {
                        case Modes.Actor:
                            Debug.Print("mode : Actor");
                            break;
                        case Modes.Edit:
                            Debug.Print("mode : Edit");
                            break;
                        case Modes.FixedOffset:
                            Debug.Print("mode : Fixed Offset");
                            break;
                        case Modes.FixedTarget:
                            Debug.Print("mode : Fixed Target");
                            break;
                        case Modes.MultiTarget:
                            Debug.Print("mode : Multi Target");
                            break;
                    }
#endif                    
                    
                    // If we were in follow actor mode and we are transitioning into edit (free) mode
                    // then we need to keep the camera from jumping.  This transition can happen either
                    // because of a bots programming or more likely because a user controlled bot just
                    // got destroyed.
                    if (mode == Modes.Actor && value == Modes.Edit && InGame.inGame.CurrentUpdateMode == InGame.UpdateMode.RunSim)
                    {
                        if (amFollowingActors || wasFollowingActors)
                        {
                            // Prevent the cursor from running away.
                            GamePadInput.GetGamePad0().IgnoreLeftStickUntilZero();
                            GamePadInput.GetGamePad0().IgnoreRightStickUntilZero();

                            if (FirstPersonViaZoom)
                            {
                                // Ease us away form the cursor.
                                InGame.inGame.Camera.DesiredDistance = 10.0f;
                                InGame.inGame.Camera.DesiredPitch = -0.5f;
                                FirstPersonViaZoom = false;
                            }
                        }
                    }

                    mode = value;
                }
            }
        }

        /// <summary>
        /// List of actors we need to keep "in frame" due to their programmed behavior.
        /// </summary>
        public static List<GameActor> ProgrammedFollowList
        {
            get { return brainFollowMeList; }
        }

        /// <summary>
        /// List of actors we need to keep "in frame" due to either programming or user control.
        /// </summary>
        public static List<GameActor> MergedFollowList
        {
            get { return mergedFollowList; }
        }

        public static GameActor CameraFocusGameActor
        {
            get { return cameraFocusGameActor; }
            set
            {
                if (cameraFocusGameActor != value)
                {
                    cameraFocusGameActor = value;
                }
            }
        }

        /// <summary>
        /// Is the camera in first person?  Only valid in follow mode.
        /// This is set to true when the user zooms in enough to trigger
        /// first person mode.
        /// </summary>
        public static bool FirstPersonViaZoom
        {
            get { return firstPersonViaZoom; }
            set
            {
                if (firstPersonViaZoom != value)
                {
                    firstPersonViaZoom = value;

                    // Changing first person mode.
                    if (CameraFocusGameActor != null)
                    {
                        if (value)
                        {
                            AddFirstPerson(CameraFocusGameActor);
                        }
                        else
                        {
                            AddIgnoreMe(CameraFocusGameActor);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Is the camera in first person for any reason.
        /// </summary>
        public static bool FirstPersonActive
        {
            get { return FirstPersonActor != null; }
        }

        /// <summary>
        /// Current first person actor if any.
        /// </summary>
        public static GameActor FirstPersonActor
        {
            get 
            {
                GameActor actor = brainFirstPersonStack.Count == 0 ? null : brainFirstPersonStack[brainFirstPersonStack.Count - 1];
                return actor;
            }
        }

        #endregion

        /// <summary>
        /// Tell the camera to follow an actor.
        /// </summary>
        /// <param name="actor">The actor to follow</param>
        public static void AddFollowMe(GameActor actor)
        {
            // If this first person actor got that way via zooming in rather
            // than via it's programming then we don't want to let the 
            // brain overwrite that state.
            if (FirstPersonViaZoom && actor == FirstPersonActor)
                return;

            // If this first person actor got that way this frame, don't follow.
            // This effectively acts as an arbitrator for the camera.
            if (actor == FirstPersonActor && firstPersonSetFrame == Time.FrameCounter)
                return;

            // Remove this actor from the first person stack if there.
            brainFirstPersonStack.Remove(actor);

            if (!brainFollowMeList.Contains(actor))
            {
                brainFollowMeList.Add(actor);
            }
        }   // end of AddFollowMe()

        /// <summary>
        /// Add an actor to the list of user-controlled actors.
        /// </summary>
        /// <param name="actor">The user controlled actor.</param>
        public static void AddUserControlled(GameActor actor)
        {
            /// If this is first person actor, make it sticky and don't
            /// let user controlled-ness implicitly steal it away.
            if (actor == FirstPersonActor)
                return;

            // Remove this actor from the first person stack if there.
            brainFirstPersonStack.Remove(actor);

            if (!brainUserControlledList.Contains(actor))
            {
                brainUserControlledList.Add(actor);
            }
        }   // end of AddUserControlled()

        /// <summary>
        /// Tell the camera to ignore the actor.
        /// </summary>
        /// <param name="actor">The actor to ignore</param>
        public static void AddIgnoreMe(GameActor actor)
        {
            // Remove this actor from the first person stack if there.
            brainFirstPersonStack.Remove(actor);

            if (!brainIgnoreMeList.Contains(actor))
            {
                brainIgnoreMeList.Add(actor);
            }
        }   // end of AddIgnoreMe()

        /// <summary>
        /// Tell the camera to use this actor for first person camera.
        /// </summary>
        /// <param name="actor">The actor to use.</param>
        public static void AddFirstPerson(GameActor actor)
        {
            // If we've already set the mode this frame, ignore this.
            if (firstPersonSetFrame == Time.FrameCounter)
            {
                return;
            }
            firstPersonSetFrame = Time.FrameCounter;

            // Remove actor from follow list if there.
            brainFollowMeList.Remove(actor);

            // Don't do anything if already at top of stack.
            if (FirstPersonActor != actor)
            {
                // Remove if in stack.
                brainFirstPersonStack.Remove(actor);
                // Add to top of stack.
                brainFirstPersonStack.Add(actor);
            }
        }   // end of FirstPerson()

        /// <summary>
        /// Reset all lists.  Needs to be called on reset.
        /// </summary>
        public static void ResetAllLists()
        {
            brainFirstPersonStack.Clear();
            brainFollowMeList.Clear();
            brainUserControlledList.Clear();
            brainIgnoreMeList.Clear();
            mergedFollowList.Clear();
        }   // end of ResetAllLists()

        /// <summary>
        /// Reset the follow lists used by the camera.  This should be done
        /// each frame before the brains are updated.
        /// </summary>
        public static void ResetIgnoreList()
        {
            brainIgnoreMeList.Clear();

            // Ensure only one bot thinks it's in first person.
            for (int i = 0; i < brainFirstPersonStack.Count - 1; i++)
            {
                if (brainFirstPersonStack[i].FirstPerson)
                {
                    brainFirstPersonStack[i].SetFirstPerson(false);
                }
            }
        }   // end of ResetIgnoreList()

        /// <summary>
        /// Resolves conflicts in the follow lists used by the camera.
        /// An entry in the NeverFollowList overrides one in the FollowList.
        /// Also removes any dead bots.
        /// This should be called after the brain updates but before the 
        /// camera is used.
        /// </summary>
        public static void ResolveFollowLists()
        {
            // An explicit ignore in the bot's programming trumps 
            // an explicit follow or a user controlled follow.
            for (int i = 0; i < brainIgnoreMeList.Count; i++)
            {
                brainFollowMeList.Remove(brainIgnoreMeList[i]);
                brainUserControlledList.Remove(brainIgnoreMeList[i]);
            }

            //
            // Remove any dead bots.
            //
            for (int i = brainFollowMeList.Count - 1; i>=0; i--)
            {
                GameActor actor = brainFollowMeList[i];
                if (!actor.IsAlive())
                {
                    brainFollowMeList.RemoveAt(i);
                }
            }

            for (int i = brainUserControlledList.Count - 1; i >= 0; i--)
            {
                GameActor actor = brainUserControlledList[i];
                if (!actor.IsAlive())
                {
                    brainUserControlledList.RemoveAt(i);
                }
            }

            if (FirstPersonActive)
            {
                /// We want to clear out anyone that has requested first person
                /// but lost out. They can continue to request, but if they requested
                /// at some point in the past but stopped requesting, and the current
                /// first person stops being first person, we want their request to 
                /// have expired. So achieving firstperson-ness is sticky, but requesting
                /// firstperson-ness is not.
                GameActor winner = null;

                for (int i = brainFirstPersonStack.Count - 1; i >= 0; i--)
                {
                    GameActor actor = brainFirstPersonStack[i];
                    if (actor.IsAlive())
                    {
                        winner = actor;
                        break;
                    }
                }

                brainFirstPersonStack.Clear();
                if (winner != null)
                    brainFirstPersonStack.Add(winner);
            }

            // Create the merged follow list.
            mergedFollowList.Clear();
            for (int i = 0; i < brainFollowMeList.Count; i++)
            {
                GameActor actor = brainFollowMeList[i];
                if (!mergedFollowList.Contains(actor))
                {
                    mergedFollowList.Add(actor);
                }
            }
            for (int i = 0; i < brainUserControlledList.Count; i++)
            {
                GameActor actor = brainUserControlledList[i];
                if (!mergedFollowList.Contains(actor))
                {
                    mergedFollowList.Add(actor);
                }
            }

            // Flag whether camera is following any actors.
            wasFollowingActors = amFollowingActors;
            amFollowingActors = mergedFollowList.Count > 0 || brainFirstPersonStack.Count > 0;

            // Clear the user controlled list since it gets updated every frame.
            brainUserControlledList.Clear();

        }   // end of ResolveFollowLists()

    }   // end of class CameraInfo

}   // end of namespace Boku.Common
