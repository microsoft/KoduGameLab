// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Boku.Common
{
    /// <summary>
    /// Tutorial is a non-instantiable(is that a word?) class which consists of
    /// static values and methods designed to support writing C# tutorials.
    /// </summary>
    public class SimTutorialHook
    {
        private static bool disableObjectDelete = false;
        private static bool disableObjectMove = false;
        private static bool disableObjectAdd = false;
        private static bool disableObjectProgramming = false;

        #region Accessors
        /// <summary>
        /// Prevents objects from being deleted in ObjectEdit mode.
        /// </summary>
        public static bool DisableObjectDelete
        {
            get { return disableObjectDelete; }
            set { disableObjectDelete = value; }
        }
        /// <summary>
        /// Prevents objects from being moved in ObjectEdit mode by preventing them from being selected.
        /// </summary>
        public static bool DisableObjectMove
        {
            get { return disableObjectMove; }
            set { disableObjectMove = value; }
        }
        /// <summary>
        /// Prevents objects from being added via the Add Item menu.
        /// </summary>
        public static bool DisableObjectAdd
        {
            get { return disableObjectAdd; }
            set { disableObjectAdd = value; }
        }
        /// <summary>
        /// Prevents objects from having their programming edited.
        /// </summary>
        public static bool DisableObjectProgramming
        {
            get { return disableObjectProgramming; }
            set { disableObjectProgramming = value; }
        }

        /// <summary>
        /// Returns true if InGame is active.
        /// </summary>
        public static bool InGameIsActive
        {
            get { return InGame.inGame.State != InGame.States.Inactive; }
        }
        #endregion


        // c'tor, private since we don't want anyone to create one.
        private SimTutorialHook()
        {
        }

        /// <summary>
        /// Resests all values to their default (non-tutorial) state.
        /// </summary>
        public static void Reset()
        {
            disableObjectDelete = false;
            disableObjectMove = false;
            disableObjectAdd = false;
            disableObjectProgramming = false;

        }   // end of Tutorial.Reset()

        /// <summary>
        /// Navigates to ToolMenu from anywhere in edit mode.
        /// </summary>
        /// <returns>true if successful, false if unable to complete.</returns>
        public static bool NavigateToToolMenu()
        {
            bool result = false;

            if (InGameIsActive)
            {
                InGame.inGame.CurrentUpdateMode = InGame.UpdateMode.ToolMenu;
                result = true;
            }

            return result;
        }   // end of Tutorial.NavigateToToolMenu()

        /// <summary>
        /// Navigates to RunSim mode from anywhere in edit mode.
        /// </summary>
        /// <returns>true if successful, false if unable to complete.</returns>
        public static bool NavigateToRunSimMode()
        {
            bool result = false;

            if (InGameIsActive)
            {
                InGame.inGame.CurrentUpdateMode = InGame.UpdateMode.RunSim;
                result = true;
            }
            
            return result;
        }   // end of Tutorial.NavigateToRunSimMode()

        /// <summary>
        /// Navigates to ObjectEdit mode from anywhere in edit mode.
        /// </summary>
        /// <returns>true if successful, false if unable to complete.</returns>
        public static bool NavigateToObjectEditMode()
        {
            bool result = false;

            if (InGameIsActive)
            {
                InGame.inGame.CurrentUpdateMode = InGame.UpdateMode.EditObject;
                result = true;
            }

            return result;
        }   // end of Tutorial.NavigateToObjectEditMode()

        /// <summary>
        /// Navigates to TexturePaint mode from anywhere in edit mode.
        /// </summary>
        /// <returns>true if successful, false if unable to complete.</returns>
        public static bool NavigateToTexturePaintMode()
        {
            bool result = false;

            if (InGameIsActive)
            {
                /// TODO (scoy) This whole function looks like it should go away.  (along with the rest of the tutorial stuff?)
                ///InGame.inGame.CurrentUpdateMode = InGame.UpdateMode.EditTexture;
                result = true;
            }

            return result;
        }   // end of Tutorial.NavigateToTexturePainMode()

        /// <summary>
        /// Navigates to HeightMapEdit mode from anywhere in edit mode.
        /// </summary>
        /// <returns>true if successful, false if unable to complete.</returns>
        public static bool NavigateToHeightMapEditMode()
        {
            bool result = false;

            if (InGameIsActive)
            {
                /// TODO (scoy) This whole function looks like it should go away.  (along with the rest of the tutorial stuff?)
                ///InGame.inGame.CurrentUpdateMode = InGame.UpdateMode.EditHeightMap;
                result = true;
            }

            return result;
        }   // end of Tutorial.NavigateToHeightMapEditMode()

    }   // end of class Tutorial

}   // end of namespace Boku.Common
