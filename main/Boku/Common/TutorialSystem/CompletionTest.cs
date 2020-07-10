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

using KoiX;

using Boku.Base;
using Boku.Fx;
using Boku.Common;
using Boku.Common.Xml;
using Boku.Programming;
using Boku.SimWorld.Terra;

namespace Boku.Common.TutorialSystem
{
    public class CompletionTest
    {
        #region Members

        private string name;    // Name of the test to run.
        private string args;    // Arg string from Xml.

        private string[] argList;   // Args broken up into elements, passed into the test itself.

        #endregion

        #region Accessors

        [XmlAttribute]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [XmlAttribute]
        public string Args
        {
            get { return args; }
            set 
            { 
                args = value;
                char[] separators = { ' ' };
                argList = args.Split(separators);
            }
        }

        #endregion

        #region Public

        /// <summary>
        /// Needed for serialization
        /// </summary>
        public CompletionTest()
        {
        }   // end of c'tor

        public CompletionTest(string name, string args)
        {
            this.name = name;
            this.args = args;

            char[] separators = { ' ', ',' };
            argList = args.Split(separators);

        }   // end of c'tor

        /// <summary>
        /// Returns true if the test passes, false otherwise.
        /// Defaults to true if test is unrecognized.
        /// </summary>
        public bool Evaluate()
        {
            switch(name)
            {
                case "Timer":
                    return Timer();

                case "BotCount":
                    return BotCount();

                case "Kode":
                    return Kode();

                case "Terra":
                    return Terra();

                case "ModeChange":
                    return ModeChange();

                case "CameraMove":
                    return CameraMove();

                default :
                    Debug.Assert(false, "Unrecognized test : " + name);
                    return true;
            }

            //return true;
        }   // end of Evaluate()

        #endregion

        #region Internal

        //
        // Implementation of all tests.
        //

        // Storage needed for Timer.
        private double startTime = double.MaxValue;

        /// <summary>
        /// Allows a timer to be used to wait for n seconds.  Returns true after the time has passed.
        /// This is a slightly strange test since it actually has some persistent state.
        /// 
        /// args: a simple number (may be floating point) representing the number of seconds to wait.
        /// </summary>
        private bool Timer()
        {
            try
            {
                double curTime = Time.WallClockTotalSeconds;
                double tics = double.Parse(argList[0]);

                // Init if needed.
                if (startTime == double.MaxValue)
                {
                    startTime = curTime;
                }

                double elapsed = curTime - startTime;
                if (elapsed > tics)
                {
                    // Reset start time.
                    startTime = double.MaxValue;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                Debug.Assert(false);

                // Default to returning true on error.
                return true;
            }
        }   // end of Timer()

        // Storage needed for BotCount
        private int startCount = -1;

        /// <summary>
        /// Allows the counting of specified bot types.
        /// args:   0) bot type, uses standard bot name
        ///         1) count, number of this bot type to look for (optional, default is any)
        ///         2) min max delta (optional, modifies count "2 min" triggers on a minimum of 2
        ///                                                    "2 max" triggers on a maximum of 2
        ///                                                    "2 delta" triggers on a change of 2)
        /// Examples:
        ///     "apple" -- triggers if any apple exists.
        ///     "apple 2" -- triggers if exactly 2 apples exist.
        ///     "apple 4 min" -- triggers if 4 or more apples exist.
        ///     "apple 2 delta" -- triggers when 3 apples have been added.
        /// 
        /// </summary>
        /// <returns></returns>
        private bool BotCount()
        {
            try
            {
                // Fail if no world is loaded.
                if (InGame.XmlWorldData == null)
                    return false;

                int curCount = 0;
                for (int i = 0; i < InGame.inGame.gameThingList.Count; i++)
                {
                    GameActor actor = InGame.inGame.gameThingList[i] as GameActor;
                    if (actor != null)
                    {
                        if (actor.StaticActor.NonLocalizedName.StartsWith(argList[0]))
                        {
                            ++curCount;
                        }
                    }
                }

                // Check 'any' case.
                if(argList.Length == 1)
                {
                    return curCount != 0;
                }
                else
                {
                    // Get count from arg list.
                    int count = int.Parse(argList[1]);

                    // Check exact count case.
                    if(argList.Length == 2)
                    {
                        return curCount == count;
                    }

                    // Check relative cases.
                    switch(argList[2])
                    {
                        case "min":
                            return curCount >= count;
                        case "max":
                            return curCount <= count;
                        case "delta":
                            // Init?
                            if(startCount == -1)
                            {
                                startCount = curCount;
                            }
                            if(curCount == startCount + count)
                            {
                                startCount = -1;
                                return true;
                            }
                            return false;
                    }
                }

                return false;
            }
            catch
            {
                Debug.Assert(false);

                // Default to returning true on error.
                return true;
            }
        }   // end of BotCount()


        /// <summary>
        /// Allows the testing of specific kode sequences.
        /// args:   (optional) page n - specifies the page to look on.  Otherwise all pages are checked.
        ///         (optional) reflex n - specifies the reflex to test.  Otherwise all reflexes are tested.
        ///         (optional) indent n - specifies the required indent level of the reflex.  Otherwise all levels are valid.
        ///         list of 0 or more tile upids.
        ///         Groups of tiles can be OR'd together using paraentheses to add flexibility.  For instance 
        ///             (filter.ArrowKeys,filter.WASDKeys) will trigger if either tile is present.
        ///         
        ///         returns true if ALL the tiles are found on the specified page/reflex.
        /// 
        /// Examples:
        ///     NOTE:  and  allow the greater-than and less-than symbols to be used in Xml.
        ///     "sensor.eyes" -- triggers if the "see" sensor is used anywhere in this bot's programming.
        ///     "sensor.eyes filter.fruit actuator.movement selector.towardclosest" -- triggers when "see apple move toward" is programmed.
        ///     "reflex 1 actuator.movement selector.wander" -- triggers when "move wander" is programmed in the first refex.
        ///     "page 2 reflex 3 actuator.switchtask modifier.taskc -- triggers when the 3rd reflex of page 2 is "switch page3".
        ///     "reflex 2 indent 1 actuator.movement selector.wander" -- triggers when "move wander" is programmed and indented 1 level.
        /// 
        /// </summary>
        /// <returns></returns>
        private bool Kode()
        {
            try
            {
                int targetPage = -1;
                int targetReflex = -1;
                int targetIndent = -1;

                int index = 0;

                bool argFound = true;

                do
                {
                    switch (argList[index])
                    {
                        case "page":
                            targetPage = int.Parse(argList[index + 1]);
                            index += 2;
                            break;
                        case "reflex":
                            targetReflex = int.Parse(argList[index + 1]);
                            index += 2;
                            break;
                        case "indent":
                            targetIndent = int.Parse(argList[index + 1]);
                            index += 2;
                            break;
                        default:
                            argFound = false;
                            break;
                    }
                } while (argFound);

                // Find current bot's programming.
                GameActor actor = InGame.inGame.Editor.GameActor;
                if (actor != null)
                {
                    Brain brain = actor.Brain;
                    for (int t = 0; t < brain.tasks.Count; t++)
                    {
                        // Are we looking for this page?
                        if (targetPage == -1 || targetPage - 1 == t)
                        {
                            Task task = brain.tasks[t];
                            if (task != null)
                            {
                                for (int r = 0; r < task.reflexes.Count; r++)
                                {
                                    // Are we looking for this reflex?
                                    if (targetReflex == -1 || targetReflex - 1 == r)
                                    {
                                        Reflex reflex = task.reflexes[r] as Reflex;
                                        if (reflex != null)
                                        {
                                            // Are we looking for this indent level?
                                            if (targetIndent == -1 || targetIndent == reflex.Indentation)
                                            {
                                                bool reflexMatches = true;
                                                for (int i = index; i < argList.Length; i++)
                                                {
                                                    // We may have multiple tiles OR'd together, split them apart.
                                                    char[] orChars = { '(', ',',')' };
                                                    string[] sensorList = argList[i].Split(orChars);
                                                    bool tileExists = false;
                                                    foreach (string tile in sensorList)
                                                    {
                                                        if (reflex.Data.HasTile(tile))
                                                        {
                                                            tileExists = true;
                                                            break;
                                                        }
                                                    }

                                                    if (!tileExists)
                                                    {
                                                        reflexMatches = false;
                                                        break;
                                                    }
                                                }
                                                
                                                // We found all the tiles listed.
                                                if (reflexMatches)
                                                {
                                                    return true;
                                                }

                                            }   // end if correct indent level.
                                        }   // end if reflex != null.
                                    }   // end if correct reflex.
                                }   // end of loop over reflexes.
                            }   // end if task != null.
                        }   // end of page is the one we're looking for.
                    }   // end of loop over tasks.
                }   // end if actor != null.


                return false;
            }
            catch
            {
                Debug.Assert(false);

                // Default to returning true on error.
                return true;
            }
        }   // end of Kode()

        // Storage needed for Terra
        private bool firstTime = true;
        private int paintCounter = -1;
        private int addCounter = -1;
        private int deleteCounter = -1;
        private int raiseCounter = -1;
        private int lowerCounter = -1;
        private int smoothCounter = -1;
        private int waterCounter = -1;

        /// <summary>
        /// Allows testing to detect the usage of terrain tools.
        /// As with other tests, having more than one arg indicates that you want
        /// each of the triggers to fire, not just one.
        /// 
        /// args:   paint -- triggers when terrain material is changed.
        ///         add -- triggers when terrain is added
        ///         delete -- triggers when terrain is deleted
        ///         raise -- triggers when terrain is raised
        ///         lower -- triggers when terrain is lowered
        ///         smooth -- triggers when terrain is smoothed
        ///         water -- triggers when water is added
        /// </summary>
        /// <returns></returns>
        private bool Terra()
        {
            try
            {
                if (firstTime)
                {
                    // Sync counters.
                    paintCounter = Terrain.paintCounter;
                    addCounter = Terrain.addCounter;
                    deleteCounter = Terrain.deleteCounter;
                    raiseCounter = Terrain.raiseCounter;
                    lowerCounter = Terrain.lowerCounter;
                    smoothCounter = Terrain.smoothCounter;
                    waterCounter = Terrain.waterCounter;

                    firstTime = false;
                }

                bool passed = true;
                foreach (string arg in argList)
                {
                    switch (arg)
                    {
                        case "paint":
                            passed &= Terrain.paintCounter > paintCounter;
                            break;
                        case "add":
                            passed &= Terrain.addCounter > addCounter;
                            break;
                        case "delete":
                            passed &= Terrain.deleteCounter > deleteCounter;
                            break;
                        case "raise":
                            passed &= Terrain.raiseCounter > raiseCounter;
                            break;
                        case "lower":
                            passed &= Terrain.lowerCounter > lowerCounter;
                            break;
                        case "smooth":
                            passed &= Terrain.smoothCounter > smoothCounter;
                            break;
                        case "water":
                            passed &= Terrain.waterCounter > waterCounter;
                            break;
                    }
                }

                if (passed)
                {
                    // Reset for next use.
                    firstTime = true;
                }

                return passed;
            }
            catch
            {
                Debug.Assert(false);

                // Default to returning true on error.
                return true;
            }
        }   // end of Terrain

        /// <summary>
        /// Allows testing for mode change.
        /// args:   list of modes to test for
        ///         triggers when in a mode not on the list
        ///         
        /// Generally this is used to detect when the user has left a mode.  For instance
        /// it can be used to give info to the user about the material picker and will 
        /// trigger once the user makes a selection and leaves the material picker.
        /// </summary>
        /// <returns></returns>
        public bool ModeChange()
        {
            try
            {
                foreach (string arg in argList)
                {
                    if (arg == TutorialManager.CurGameMode.ToString())
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                Debug.Assert(false);

                // Default to returning true on error.
                return true;
            }
        }   // end of ModeChage()

        /// <summary>
        /// Forces the camera to move to a specific character.
        /// args:   Name of character to move to.  This needs to be the name that
        ///         appears over the character when in focus in the editor.
        ///         
        /// This should be used with a target mode of MosueEditObject, GamepadEditObject or MouseCameraMove.
        /// </summary>
        /// <returns></returns>
        public bool CameraMove()
        {
            try
            {
                // Since actor names have spaces we need to use the full args string.
                for (int i = 0; i < InGame.inGame.gameThingList.Count; i++)
                {
#if NETFX_CORE
                    if (string.Compare(args, InGame.inGame.gameThingList[i].CreatableName, StringComparison.CurrentCultureIgnoreCase) == 0)
#else
                    if (string.Compare(args, InGame.inGame.gameThingList[i].DisplayNameNumber, ignoreCase: true) == 0)
#endif
                    {
                        Vector3 pos = InGame.inGame.gameThingList[i].Movement.Position;
                        pos.Z = InGame.inGame.shared.CursorPosition.Z;
                        InGame.inGame.shared.CursorPosition = pos;
                        break;
                    }
                }

                // Always return true.  If we can't find the actor we're in trouble anyway.
                return true;
            }
            catch
            {
                Debug.Assert(false);

                // Default to returning true on error.
                return true;
            }
        }   // end of CameraMove()

        private bool Settings()
        {
            try
            {

                return false;
            }
            catch
            {
                Debug.Assert(false);

                // Default to returning true on error.
                return true;
            }
        }   // end of Settings()



        private bool Blank()
        {
            try
            {

                return false;
            }
            catch
            {
                Debug.Assert(false);

                // Default to returning true on error.
                return true;
            }
        }

        #endregion

    }   // end of class CompletionTest

}   // end of namespace Boku.Common.TutorialSystem
