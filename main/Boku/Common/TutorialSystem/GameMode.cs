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

namespace Boku.Common.TutorialSystem
{
    /// <summary>
    /// Class for handling the tutorial system.  All access to this class should be via static methods.
    /// </summary>
    public partial class TutorialManager
    {
        #region Members

        public enum GameMode
        {
            Unknown = 0,
            InGame,

            MainMenu,
            OptionsMenu,
            HelpScreens,
            LoadLevelMenu,
            CommunityMenu,
            SharingHub,
            SharingSession,

            HomeMenu,

            Programming,        // Will probably need to extend this to have more specific info about the
                                // current mode OR have the completion criteria be able to check whether
                                // the user is on the page handle, reflex handle, reflex etc.  Should also
                                // be able to check reflex # and page #.
            ProgrammingHelp,

            //
            // Mouse modes
            //
            MouseHome,
            MousePlay,
            MouseCameraMove,
            MouseEditObject,
            MouseEditObjectFocus,
            MouseEditMenu,          // Pop-up menu next to actor.
            MousePaths,
            MouseTerrainPaint,
            MouseTerrainUpDown,
            MouseTerrainSmoothLevel,
            MouseTerrainSpikeyHilly,
            MouseWater,
            MouseDeleteObjects,
            MouseWorldSettings,

            //
            // Gamepad modes
            //
            // ToolMenu* is just when the user is in the tool menu.  Actual tool modes are below.
            ToolMenuRunSim,
            ToolMenuHomeMenu,
            ToolMenuEditObject,
            ToolMenuTerrainPaint,
            ToolMenuTerrainUpDown,
            ToolMenuTerrainSmoothLevel,
            ToolMenuTerrainSpikeyHilly,
            ToolMenuWater,
            ToolMenuDeleteObjects,
            ToolMenuWorldSettings,

            GamepadEditObject,
            GamepadEditObjectFocus,
            GamepadTerrainPaint,
            GamepadTerrainUpDown,
            GamepadTerrainSmoothLevel,
            GamepadTerrainSpikeyHilly,
            GamepadWater,
            GamepadDeleteObjects,
            GamepadWorldSettings,

            //
            // Touch modes.
            //
            TouchHome,
            TouchPlay,
            TouchCameraMove,
            TouchEditObject,
            TouchEditObjectFocus,
            TouchEditMenu,          // Pop-up menu next to actor.
            TouchPaths,
            TouchTerrainPaint,
            TouchTerrainUpDown,
            TouchTerrainSmoothLevel,
            TouchTerrainSpikeyHilly,
            TouchWater,
            TouchDeleteObjects,
            TouchWorldSettings,

            // Edit related modes shared by gamepad and mouse.
            MaterialPicker,
            BrushPicker,
            WaterPicker,
            AddItem,
            AddItemHelp,
            ObjectTweak,

            LAST_MODE,

        }   // end of enum GameMode

        private static GameMode curGameMode = GameMode.Unknown;

        #endregion

        #region Accessors

        public static GameMode CurGameMode
        {
            get { return curGameMode; }
        }

        #endregion

        #region Public
                #endregion

        #region Internal

        /// <summary>
        /// Sets curGameMode to the current mode, if it can be figured out.
        /// If not, sets it to GameMode.Unknown.
        /// </summary>
        private static void SetGameMode()
        {
            // Note, this is going to be a little ad-hoc...

            // TODO What about dialogs on top of these modes?

            if (BokuGame.bokuGame.mainMenu.Active)
            {
                curGameMode = BokuGame.bokuGame.mainMenu.OptionsActive ? GameMode.OptionsMenu : GameMode.MainMenu;
                return;
            }

            if (BokuGame.bokuGame.helpScreens.Active)
            {
                curGameMode = GameMode.HelpScreens;
                return;
            }

            if (BokuGame.bokuGame.miniHub.Active)
            {
                curGameMode = GameMode.HomeMenu;
                return;
            }

            if (BokuGame.bokuGame.loadLevelMenu.Active)
            {
                curGameMode = GameMode.LoadLevelMenu;
                return;
            }

            if (BokuGame.bokuGame.community.Active)
            {
                curGameMode = GameMode.CommunityMenu;
            }

            if (BokuGame.bokuGame.sharingScreen.Active)
            {
                curGameMode = GameMode.SharingSession;
                return;
            }

            if (InGame.inGame.Editor.Active)
            {
                curGameMode = InGame.inGame.shared.programmingHelpCard.Active ? GameMode.ProgrammingHelp : GameMode.Programming;
                return;
            }

            if (BokuGame.bokuGame.inGame.State != InGame.States.Inactive)
            {
                // RunSim mode?
                if (BokuGame.bokuGame.inGame.CurrentUpdateMode == InGame.UpdateMode.RunSim)
                {
                    curGameMode = GameMode.InGame;
                    return;
                }

                if (BokuGame.bokuGame.inGame.CurrentUpdateMode == InGame.UpdateMode.EditObjectParameters)
                {
                    curGameMode = GameMode.ObjectTweak;
                    return;
                }

                // If the AddItemMenu is active.
                if (InGame.inGame.shared.UIShim.Active)
                {
                    if (InGame.inGame.shared.addItemHelpCard.Active)
                    {
                        curGameMode = GameMode.AddItemHelp;
                        return;
                    }

                    curGameMode = GameMode.AddItem;
                    return;
                }

                //
                // Mouse Edit
                //
                if (BokuGame.bokuGame.inGame.CurrentUpdateMode == InGame.UpdateMode.MouseEdit)
                {
                    // If we're just hovering, leave the curGameMode alone unless it's unknown.
                    if (InGame.inGame.mouseEditUpdateObj.ToolBar.Hovering && curGameMode != GameMode.Unknown)
                    {
                        return;
                    }

                    // If any of the pickers are active:
                    if (InGame.inGame.mouseEditUpdateObj.PickersActive)
                    {
                        if (InGame.inGame.mouseEditUpdateObj.ToolBox.BrushPicker.Active && !InGame.inGame.mouseEditUpdateObj.ToolBox.BrushPicker.Hidden)
                        {
                            curGameMode = GameMode.BrushPicker;
                            return;
                        }
                        if (InGame.inGame.mouseEditUpdateObj.ToolBox.MaterialPicker.Active && !InGame.inGame.mouseEditUpdateObj.ToolBox.MaterialPicker.Hidden)
                        {
                            curGameMode = GameMode.MaterialPicker;
                            return;
                        }
                        if (InGame.inGame.mouseEditUpdateObj.ToolBox.WaterPicker.Active && !InGame.inGame.mouseEditUpdateObj.ToolBox.WaterPicker.Hidden)
                        {
                            curGameMode = GameMode.WaterPicker;
                            return;
                        }
                    }
                }

                if (BokuGame.bokuGame.inGame.CurrentUpdateMode == InGame.UpdateMode.TouchEdit)
                {
                    // If we're just hovering, leave the curGameMode alone unless it's unknown.
                    if (InGame.inGame.touchEditUpdateObj.ToolBar.Hovering && curGameMode != GameMode.Unknown)
                    {
                        return;
                    }

                    // If any of the pickers are active:
                    if (InGame.inGame.touchEditUpdateObj.PickersActive)
                    {
                        if (InGame.inGame.touchEditUpdateObj.ToolBox.BrushPicker.Active && !InGame.inGame.touchEditUpdateObj.ToolBox.BrushPicker.Hidden)
                        {
                            curGameMode = GameMode.BrushPicker;
                            return;
                        }
                        if (InGame.inGame.touchEditUpdateObj.ToolBox.MaterialPicker.Active && !InGame.inGame.touchEditUpdateObj.ToolBox.MaterialPicker.Hidden)
                        {
                            curGameMode = GameMode.MaterialPicker;
                            return;
                        }
                        if (InGame.inGame.touchEditUpdateObj.ToolBox.WaterPicker.Active && !InGame.inGame.touchEditUpdateObj.ToolBox.WaterPicker.Hidden)
                        {
                            curGameMode = GameMode.WaterPicker;
                            return;
                        }
                    }
                }


                if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                {
                    switch (HelpOverlay.Peek())
                    {
                        case "MouseEditHome":
                            curGameMode = GameMode.MouseHome;
                            break;
                        case "MouseEditPlay":
                            curGameMode = GameMode.MousePlay;
                            break;
                        case "MouseEditCameraMove":
                            curGameMode = GameMode.MouseCameraMove;
                            break;
                        case "MouseEditEditObject":
                            curGameMode = GameMode.MouseEditObject;
                            break;
                        case "MouseEditEditObjectFocus":
                        case "MouseEditEditObjectFocusTree":
                            curGameMode = GameMode.MouseEditObjectFocus;
                            break;
                        case "MouseEditMenu":
                            curGameMode = GameMode.MouseEditMenu;
                            break;
                        case "MouseEditPaths":
                        case "MouseEditPathsFocusNode":
                        case "MouseEditPathsFocusEdge":
                        case "MouseEditPathsFocusPath":
                        case "MouseEditPathsDeleteNode":
                        case "MouseEditPathsDeletePath":
                            curGameMode = GameMode.MousePaths;
                            break;
                        case "MouseEditTerrainPaint":
                        case "MouseEditTerrainDelete":
                        case "MouseEditTerrainPaintSmooth":
                        case "MouseEditTerrainPaintCubic":
                            curGameMode = GameMode.MouseTerrainPaint;
                            break;
                        case "MouseEditTerrainRaiseLower":
                        case "MouseEditTerrainLowering":
                        case "MouseEditTerrainRaising":
                            curGameMode = GameMode.MouseTerrainUpDown;
                            break;
                        case "MouseEditTerrainSmoothLevel":
                        case "MouseEditTerrainSmoothLevel_ToggleSmooth":
                        case "MouseEditTerrainSmoothLevel_ToggleFlatten":
                            curGameMode = GameMode.MouseTerrainSmoothLevel;
                            break;
                        case "MouseEditTerrainSpikeyHilly":
                        case "MouseEditTerrainSpikeyHilly_ToggleHilly":
                        case "MouseEditTerrainSpikeyHilly_ToggleSpikey":
                        case "MouseEditTerrainSpikeyHilly_ToggleFlatten":
                            curGameMode = GameMode.MouseTerrainSpikeyHilly;
                            break;
                        case "MouseEditWaterRaiseLower":
                        case "MouseEditWaterRaiseLower_ToggleRaise":
                        case "MouseEditWaterRaiseLower_ToggleLower":
                            curGameMode = GameMode.MouseWater;
                            break;
                        case "MouseEditDeleteObjects":
                        case "MouseEditDeleteObjects_Deleting":
                            curGameMode = GameMode.MouseDeleteObjects;
                            break;
                        case "MouseEditWorldTweak":
                            curGameMode = GameMode.MouseWorldSettings;
                            break;

                        case null:
                            // don't change game mode.
                            break;

                        default:
                            // Could be in WorldSettings...
                            if (HelpOverlay.Peek(1) == "EditWorldParameters" || HelpOverlay.Peek(2) == "EditWorldParameters")
                            {
                                curGameMode = GameMode.MouseWorldSettings;
                            }
                            else
                            {
                                //Debug.Assert(false, "Must be missing a mode...");
                                curGameMode = GameMode.Unknown;
                            }
                            break;
                    }

                    return;
                }

                if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                {
                    switch (HelpOverlay.Peek())
                    {
                        case "MouseEditHome":
                            curGameMode = GameMode.TouchHome;
                            break;
                        case "MouseEditPlay":
                            curGameMode = GameMode.TouchPlay;
                            break;
                        case "MouseEditCameraMove":
                            curGameMode = GameMode.TouchCameraMove;
                            break;
                        case "MouseEditEditObject":
                            curGameMode = GameMode.TouchEditObject;
                            break;
                        case "MouseEditEditObjectFocus":
                        case "MouseEditEditObjectFocusTree":
                            curGameMode = GameMode.TouchEditObjectFocus;
                            break;
                        case "MouseEditMenu":
                            curGameMode = GameMode.TouchEditMenu;
                            break;
                        case "MouseEditPaths":
                        case "MouseEditPathsFocusNode":
                        case "MouseEditPathsFocusEdge":
                        case "MouseEditPathsFocusPath":
                        case "MouseEditPathsDeleteNode":
                        case "MouseEditPathsDeletePath":
                            curGameMode = GameMode.TouchPaths;
                            break;
                        case "MouseEditTerrainPaint":
                        case "MouseEditTerrainDelete":
                        case "MouseEditTerrainPaintSmooth":
                        case "MouseEditTerrainPaintCubic":
                            curGameMode = GameMode.TouchTerrainPaint;
                            break;
                        case "MouseEditTerrainRaiseLower":
                        case "MouseEditTerrainLowering":
                        case "MouseEditTerrainRaising":
                            curGameMode = GameMode.TouchTerrainUpDown;
                            break;
                        case "MouseEditTerrainSmoothLevel":
                        case "MouseEditTerrainSmoothLevel_ToggleSmooth":
                        case "MouseEditTerrainSmoothLevel_ToggleFlatten":
                            curGameMode = GameMode.TouchTerrainSmoothLevel;
                            break;
                        case "MouseEditTerrainSpikeyHilly":
                        case "MouseEditTerrainSpikeyHilly_ToggleHilly":
                        case "MouseEditTerrainSpikeyHilly_ToggleSpikey":
                        case "MouseEditTerrainSpikeyHilly_ToggleFlatten":
                            curGameMode = GameMode.TouchTerrainSpikeyHilly;
                            break;
                        case "MouseEditWaterRaiseLower":
                        case "MouseEditWaterRaiseLower_ToggleRaise":
                        case "MouseEditWaterRaiseLower_ToggleLower":
                            curGameMode = GameMode.TouchWater;
                            break;
                        case "MouseEditDeleteObjects":
                        case "MouseEditDeleteObjects_Deleting":
                            curGameMode = GameMode.TouchDeleteObjects;
                            break;
                        case "MouseEditWorldTweak":
                            curGameMode = GameMode.TouchWorldSettings;
                            break;

                        case null:
                            // don't change game mode.
                            break;

                        default:
                            // Could be in WorldSettings...
                            if (HelpOverlay.Peek(1) == "EditWorldParameters" || HelpOverlay.Peek(2) == "EditWorldParameters")
                            {
                                curGameMode = GameMode.TouchWorldSettings;
                            }
                            else
                            {
                                //Debug.Assert(false, "Must be missing a mode...");
                                curGameMode = GameMode.Unknown;
                            }
                            break;
                    }

                    return;
                }

                //
                // Gamepad Edit
                //

                // In ToolMenu.
                if (InGame.inGame.CurrentUpdateMode == InGame.UpdateMode.ToolMenu)
                {
                    switch (HelpOverlay.Peek())
                    {
                        case "ToolMenuRunSim":
                            curGameMode = GameMode.ToolMenuRunSim;
                            break;
                        case "ToolMenuHomeMenu":
                            curGameMode = GameMode.ToolMenuHomeMenu;
                            break;
                        case "ToolMenuObjectEdit":
                            curGameMode = GameMode.ToolMenuEditObject;
                            break;
                        case "ToolMenuTerrainSmoothLevel":
                            curGameMode = GameMode.ToolMenuTerrainSmoothLevel;
                            break;
                        case "ToolMenuTerrainMaterial":
                            curGameMode = GameMode.ToolMenuTerrainPaint;
                            break;
                        case "ToolMenuTerrainSpikeyHilly":
                            curGameMode = GameMode.ToolMenuTerrainSpikeyHilly;
                            break;
                        case "ToolMenuTerrainUpDown":
                            curGameMode = GameMode.ToolMenuTerrainUpDown;
                            break;
                        case "ToolMenuTerrainWater":
                            curGameMode = GameMode.ToolMenuWater;
                            break;
                        case "ToolMenuDeleteObjects":
                            curGameMode = GameMode.ToolMenuDeleteObjects;
                            break;
                        case "ToolMenuWorldSettings":
                            curGameMode = GameMode.ToolMenuWorldSettings;
                            break;

                        case null:
                            // don't change game mode.
                            break;

                        default:
                            //Debug.Assert(false, "Must be missing a mode...");
                            curGameMode = GameMode.Unknown;
                            break;
                    }

                    return;
                }

                // Not in the toolmenu so one of the tools must be active.
                switch (InGame.inGame.CurrentUpdateMode)
                {
                    case InGame.UpdateMode.EditObject:
                        curGameMode = InGame.inGame.ActiveActor == null ? GameMode.GamepadEditObject : GameMode.GamepadEditObjectFocus;
                        return;
                    case InGame.UpdateMode.TerrainFlatten:
                        curGameMode = GameMode.GamepadTerrainSmoothLevel;
                        if (!InGame.inGame.shared.ToolBox.BrushPicker.Hidden)
                        {
                            curGameMode = GameMode.BrushPicker;
                        }
                        return;
                    case InGame.UpdateMode.TerrainMaterial:
                        curGameMode = GameMode.GamepadTerrainPaint;
                        if (!InGame.inGame.shared.ToolBox.BrushPicker.Hidden)
                        {
                            curGameMode = GameMode.BrushPicker;
                        }
                        if (!InGame.inGame.shared.ToolBox.MaterialPicker.Hidden)
                        {
                            curGameMode = GameMode.MaterialPicker;
                        }
                        return;
                    case InGame.UpdateMode.TerrainRoughHill:
                        curGameMode = GameMode.GamepadTerrainSpikeyHilly;
                        if (!InGame.inGame.shared.ToolBox.BrushPicker.Hidden)
                        {
                            curGameMode = GameMode.BrushPicker;
                        }
                        return;
                    case InGame.UpdateMode.TerrainUpDown:
                        curGameMode = GameMode.GamepadTerrainUpDown;
                        if (!InGame.inGame.shared.ToolBox.BrushPicker.Hidden)
                        {
                            curGameMode = GameMode.BrushPicker;
                        }
                        return;
                    case InGame.UpdateMode.TerrainWater:
                        curGameMode = GameMode.GamepadWater;
                        if (!InGame.inGame.shared.ToolBox.WaterPicker.Hidden)
                        {
                            curGameMode = GameMode.WaterPicker;
                        }
                        return;
                    case InGame.UpdateMode.DeleteObjects:
                        curGameMode = GameMode.GamepadDeleteObjects;
                        if (!InGame.inGame.shared.ToolBox.BrushPicker.Hidden)
                        {
                            curGameMode = GameMode.BrushPicker;
                        }
                        return;
                    case InGame.UpdateMode.EditWorldParameters:
                        // We need to check input mode in case user swapped input devices while active.
                        curGameMode = GamePadInput.ActiveMode == GamePadInput.InputMode.GamePad ? GameMode.GamepadWorldSettings : GameMode.MouseWorldSettings;
                        return;
                }

            }

            // TODO Put an assert here to catch any we've missed.

            curGameMode = GameMode.Unknown;
        }   // end of SetGameMode()

        #endregion

    }   // end of class TutorialManager

}   // end of namespace Boku.Common.TutorialSystem
