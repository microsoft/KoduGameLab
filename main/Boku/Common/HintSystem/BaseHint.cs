// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Fx;
using Boku.Common;
using Boku.Common.Xml;
using Boku.Programming;

namespace Boku.Common.HintSystem
{
    /// <summary>
    /// Base class for hints in the hint system.
    /// </summary>
    public abstract class BaseHint
    {

        #region Members

        protected string id = null;
        protected string toastText = null;
        protected string modalText = null;

        protected bool disabled = false;
        protected bool showOnce = true;     // Once show once per session when not disabled.

        #endregion

        #region Accessors

        public string ID
        {
            get { return id; }
        }

        public bool Disabled
        {
            get { return disabled; }
            set { disabled = value; }
        }

        /// <summary>
        /// Should this hint only be shown once per session.  This should generally be true
        /// but in some cases it's false.  An example would be the NoControllerHint which
        /// may need to trigger for several levels during a session.
        /// </summary>
        public bool ShowOnce
        {
            get { return showOnce; }
        }

        /// <summary>
        /// The text to be displayed on the toast for this hint.
        /// </summary>
        public string ToastText
        {
            get { return toastText; }
        }

        /// <summary>
        /// The detailed version of the text for this hint to 
        /// be displayed when modal.
        /// </summary>
        public string ModalText
        {
            get { return modalText; }
        }

        #endregion

        #region Public

        /// <summary>
        /// Test for the conditions that will activate this hint.
        /// </summary>
        /// <returns>Was hint activated?</returns>
        public abstract bool Update();

        #endregion

        #region Internal

        /// <summary>
        /// Helper function which checks the actors in the current world for 
        /// input sensors.
        /// </summary>
        /// <param name="usesGamepad"></param>
        /// <param name="usesMouse"></param>
        /// <param name="usesKeyboard"></param>
        public static void CheckInputUsage(out bool usesGamepad, out bool usesMouse, out bool usesKeyboard)
        {
            usesGamepad = false;
            usesMouse = false;
            usesKeyboard = false;

            // Check for keyboard, mouse or gamepad sensors.
            for (int i = 0; i < InGame.inGame.gameThingList.Count; i++)
            {
                GameActor actor = InGame.inGame.gameThingList[i] as GameActor;

                if (actor != null)
                {
                    Brain brain = actor.Brain;
                    for (int t = 0; t < brain.tasks.Count; t++)
                    {
                        Task task = brain.tasks[t];
                        if (task != null)
                        {
                            for (int r = 0; r < task.reflexes.Count; r++)
                            {
                                Reflex reflex = task.reflexes[r] as Reflex;
                                if (reflex != null && reflex.sensorUpid != null)
                                {
                                    if (reflex.sensorUpid == "sensor.keyboard")
                                    {
                                        usesKeyboard = true;
                                    }
                                    if (reflex.sensorUpid == "sensor.mouse")
                                    {
                                        usesMouse = true;
                                    }
                                    if (reflex.sensorUpid.StartsWith("sensor.gamepad"))
                                    {
                                        usesGamepad = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

        }   // end of CheckInputUsage()

        /// <summary>
        /// Helper function which checks the actors in the current world for 
        /// mouse sensor usage.
        /// </summary>
        /// <param name="leftButton">true if left button filter found (actually this is a 
        /// bit more complicated.  We actually check for usage that would conflict with 
        /// normal camera movement.  If no conflict occurs then we leave this false even 
        /// though there is mouse left button usage.)</param>
        /// <param name="rightButton">true if right button filter found</param>
        /// <param name="hover">true if hover filter found</param>
        /// <returns>True if any mouse sensor found found.</returns>
        public static bool CheckMouseUsage(out bool leftButton, out bool rightButton, out bool hover)
        {
            leftButton = false;
            rightButton = false;
            hover = false;

            bool sensorFound = false;
            
            for (int i = 0; i < InGame.inGame.gameThingList.Count; i++)
            {
                GameActor actor = InGame.inGame.gameThingList[i] as GameActor;

                if (actor != null)
                {
                    Brain brain = actor.Brain;
                    for (int t = 0; t < brain.tasks.Count; t++)
                    {
                        Task task = brain.tasks[t];
                        if (task != null)
                        {
                            for (int r = 0; r < task.reflexes.Count; r++)
                            {
                                Reflex reflex = task.reflexes[r] as Reflex;
                                if (reflex != null && reflex.sensorUpid != null)
                                {
                                    if (reflex.sensorUpid == "sensor.mouse")
                                    {
                                        sensorFound = true;

                                        // We've found a mouse sensor, now look for filter usage.
                                        for (int f = 0; f < reflex.filterUpids.Length; f++)
                                        {
                                            if (reflex.filterUpids[f] == "filter.mouse.button.left")
                                            {
                                                // Only restrict left button use if there are no other filters in the reflex.
                                                // Having filters implies that we're testing for clicking on an object, not the ground.
                                                // Also, look at the RHS of the reflex.
                                                // If "moving toward" then restrict the button.
                                                // If "shooting" then restrict if using the click location as a target.  If using a 
                                                // direction this implies we don't care about the click location and we can let
                                                // the camera drag still work.
                                                if (reflex.filterUpids.Length == 1)
                                                {
                                                    bool shooting = reflex.actuatorUpid == "actuator.shoot2";
                                                    if(shooting)
                                                    {
                                                        foreach (string modUpid in reflex.modifierUpids)
                                                        {
                                                            if(modUpid == "modifier.north"
                                                                || modUpid == "modifier.south"
                                                                || modUpid == "modifier.east"
                                                                || modUpid == "modifier.west"
                                                                || modUpid == "modifier.up"
                                                                || modUpid == "modifier.down"
                                                                || modUpid == "modifier.forward")
                                                            {
                                                                shooting = false;
                                                            }
                                                        }
                                                    }
                                                    if (shooting || reflex.selectorUpid == "selector.towardclosest")
                                                    {
                                                        leftButton = true;
                                                    }
                                                }
                                            }
                                            if (reflex.filterUpids[f] == "filter.mouse.button.right")
                                            {
                                                // Only restrict right button use if there are no other filters in the reflex.
                                                // Having filters implies that we're testing for clicking on an object, not the ground.
                                                // Also, look at the RHS of the reflex.
                                                // If "moving toward" then restrict the button.
                                                // If "shooting" then restrict if using the click location as a target.  If using a 
                                                // direction this implies we don't care about the click location and we can let
                                                // the camera drag still work.
                                                if (reflex.filterUpids.Length == 1)
                                                {
                                                    bool shooting = reflex.actuatorUpid == "actuator.shoot2";
                                                    if (shooting)
                                                    {
                                                        foreach (string modUpid in reflex.modifierUpids)
                                                        {
                                                            if (modUpid == "modifier.north"
                                                                || modUpid == "modifier.south"
                                                                || modUpid == "modifier.east"
                                                                || modUpid == "modifier.west"
                                                                || modUpid == "modifier.up"
                                                                || modUpid == "modifier.down"
                                                                || modUpid == "modifier.forward")
                                                            {
                                                                shooting = false;
                                                            }
                                                        }
                                                    }
                                                    if (shooting || reflex.selectorUpid == "selector.towardclosest")
                                                    {
                                                        rightButton = true;
                                                    }
                                                }
                                            }
                                            if (reflex.filterUpids[f] == "filter.mouse.hover")
                                            {
                                                hover = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return sensorFound;

        }   // end of CheckMouseUsage()

        #endregion

    }   // end of class BaseHint

}   // end of namespace Boku.Common.HintSystem
