// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


// Uncomment this to see debug spew about the command "stack".
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

using KoiX;
using KoiX.Input;

using Boku.Common;

namespace Boku.Input
{
    /// <summary>
    /// Represents a focus like method for input
    /// The caller creates a List of InputCommand based objects and
    /// pushes them onto the stack when its activated and pops them when its deactivated
    /// </summary>
    public class CommandStack
    {
        /// <summary>
        /// the stack of command maps
        /// </summary>
        private static List<CommandMap> commandMaps = new List<CommandMap>();
        private static CommandMap commandOverride = null;
        private static InputConstraint inputConstraint = null;

        /// <summary>
        /// Adds the command map to the stack as the top and 
        /// input will be directed to the top map of the stack
        /// </summary>
        /// <param name="commandMap"></param>
        public static void Push(CommandMap commandMap)
        {
            Debug.Assert(commandMap != null);

            //ResetTop();
            CommandMap prevActiveCommandMap = null;
            if (commandMaps.Count > 0)
            {
                prevActiveCommandMap = commandMaps[commandMaps.Count - 1];
            }
                
            commandMaps.Add(commandMap);
            SyncTop(prevActiveCommandMap);

#if DEBUG_SPEW
            Debug.Print("push " + commandMap.name);
            PrintStack();
#endif
        }
        protected static void ResetTop()
        {
            // reset the last top map collection
            if (commandMaps.Count > 0)
            {
                CommandMap activeMap = commandMaps[commandMaps.Count - 1];
            }
        }
        protected static void SyncTop(CommandMap prevActiveCommandMap)
        {
            // sync the new top map collection
            if (commandMaps.Count > 0)
            {
                CommandMap activeMap = commandMaps[commandMaps.Count - 1];
            }
        }
        /// <summary>
        /// Pops the command map from the stack exposing the next one as the active one
        /// This is only needed to be done when the object is no longer activated or
        /// within the running stack of objects
        /// </summary>
        /// <param name="commandMap"></param>
        public static void Pop(CommandMap commandMap)
        {
            if (commandMaps.Count > 0)
            {
                // Currently not needed.  Useful to uncomment for debugging.
                //CommandMap activeMap = commandMaps[commandMaps.Count - 1];

                //commandMaps.Remove(commandMap);
                // We can't just do a Remove here.  If a command map is in 
                // the stack more than once the Remove will take out the 
                // wrong one.  The whole reason we're in this mess is because
                // the command "stack" doesn't always act like a stack.  Hence
                // having to pass in the element that we want to pop.  This
                // should be cleaned up.
                int index = commandMaps.LastIndexOf(commandMap);

                // I have no idea why the programming UI is popping command
                // that aren't on the stack...
                if (index >= 0)
                {
                    commandMaps.RemoveAt(index);
                }

                SyncTop(commandMap);

                GamePadInput.ClearAllWasPressedState();
#if DEBUG_SPEW
                Debug.Print("pop " + commandMap.name);
                PrintStack();
#endif
            }
        }

#if DEBUG_SPEW
        public static void PrintStack()
        {
            Debug.Print("command stack :");
            for (int i = 0; i < commandMaps.Count; i++)
            {
                Debug.Print("  " + i + " " + commandMaps[i].name);
            }
            Debug.Print(" ");
        }
#endif


        /// <summary>
        /// Returns the CommandMap currently on the top of the stack.  Returns null
        /// if nothing is there.  
        /// 
        /// This should be used to verify that you have input focus before looking at 
        /// analog stick or trigger values unless you can guarantee that you have been
        /// called due to an event being triggered rather than through some other path.
        /// Otherwise you may be getting stale values.
        /// </summary>
        /// <returns></returns>
        public static CommandMap Peek()
        {
            if (commandOverride != null)
            {
                return commandOverride;
            }
            return Peek(0);
        }
        /// <summary>
        /// (see above)
        /// Used to ignore the Override and look deeper into the stack
        /// Used by those who are setting the override
        /// </summary>
        /// <param name="depth"></param>
        /// <returns></returns>
        public static CommandMap Peek(int depth)
        {
            if (depth >= 0 && commandMaps.Count > depth)
            {
                CommandMap activeMap = commandMaps[commandMaps.Count - (depth+1)];
                return activeMap;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the depth of the command stack.
        /// </summary>
        /// <returns></returns>
        public static int Depth()
        {
            return commandMaps.Count;
        }

        /// <summary>
        /// Removes the ith element of the stack.
        /// </summary>
        /// <param name="i">index of element to remove.  0 is top of stack, 1 is next element, etc.</param>
        public static void Remove(int i)
        {
            commandMaps.RemoveAt(commandMaps.Count - (i + 1));
        }

        /// <summary>
        /// This is used for override ability and should not be used by any normal UI
        /// Its primary use is for a tutorial
        /// </summary>
        /// <param name="command"></param>
        public static void AttachCommandOverride( CommandMap command )
        {
            Debug.Assert(commandOverride == null);
            commandOverride = command;
        }
        public static void DetachCommandOverride(CommandMap command)
        {
            Debug.Assert(commandOverride == command, "you shouldn't clear someone elses override");
            if (commandOverride == command)
            {
                commandOverride = null;
            }
        }

        /// <summary>
        /// This is used to constrain input use and should not be used by any normal UI
        /// Its primary use is for a tutorial
        /// </summary>
        /// <param name="constraint"></param>
        public static void AttachInputConstraint(InputConstraint constraint)
        {
            Debug.Assert(inputConstraint == null);
            inputConstraint = constraint;
        }
        public static void DetachInputConstraint(InputConstraint constraint)
        {
            Debug.Assert(inputConstraint == constraint, "you shouldn't clear someone elses constraint");
            if (inputConstraint == constraint)
            {
                inputConstraint = null;
            }
        }
        public static InputConstraint Constraint
        {
            get
            {
                return inputConstraint;
            }
        }

        /// <summary>
        /// Called on every update by the root game object
        /// This will update keyboard and gamepad states and 
        /// then update the top command map input commands
        /// </summary>
        public static void Update()
        {
            if (commandOverride != null)
            {
                // update static state
                InputCommand.UpdateState();
            }
            else if (commandMaps.Count > 0)
            {
                // update static state
                InputCommand.UpdateState();

                // now update all commands in the top map collection
                CommandMap activeMap = commandMaps[commandMaps.Count - 1];
            }
        }
    }
}
