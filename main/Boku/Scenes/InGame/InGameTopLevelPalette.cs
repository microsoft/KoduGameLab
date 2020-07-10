// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
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
using Boku.SimWorld;
using Boku.Common;
using Boku.Programming;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;

namespace Boku
{
    /// <summary>
    /// UpdateObject for InGame -> TopLevelPalette
    /// </summary>
    public partial class InGame : GameObject, INeedsDeviceReset
    {

        public class TopLevelPaletteUpdateObj : BaseEditUpdateObj
        {
            private InGame.UpdateMode prevMode = UpdateMode.RunSim;     // Previously selected mode so we can go back if we need to.
            private InGame.UpdateMode curMode = UpdateMode.RunSim;

            #region Accessors
            /// <summary>
            /// Before activating the TopLevelPalette we need to know what mode is
            /// currently active for 2 reasons.  1) so we can pre-select the right
            /// mode when we start up and 2) so we can back out to the previous mode.
            /// </summary>
            public InGame.UpdateMode CurrentMode
            {
                get { return curMode; }
                set { curMode = prevMode = value; }
            }
            /// Returns the position of the closed palette in pixel coordinates.
            /// Useful for anything that want to try and line up with the palette.
            /// </summary>
            public Vector2 CenterOfClosedPalette
            {
                get { return shared.topLevelPalette.CenterOfClosedPalette; }
            }
            #endregion

            // c'tor
            public TopLevelPaletteUpdateObj(InGame parent, ref Shared shared)
                : base(parent, ref shared)
            {
            }   // end of TopLevelPaletteUpdateObj c'tor


            public void MoveToNextMode()
            {
                curMode = (UpdateMode)(((int)curMode + 1) % (int)UpdateMode.NumModesInPalette);
                shared.topLevelPalette.Select = curMode;
            }

            public void MoveToPreviousMode()
            {
                curMode = (UpdateMode)(((int)curMode + (int)UpdateMode.NumModesInPalette - 1) % (int)UpdateMode.NumModesInPalette);
                shared.topLevelPalette.Select = curMode;
            }


            /// <summary>
            /// TopLevelPaletteUpdateObj Update()
            /// </summary>
            /// <param name="camera"></param>
            public override void Update()
            {
                float secs = Time.WallClockFrameSeconds;

                // Check if we have input focus.  Don't do any input
                // related update if we don't.
                if (CommandStack.Peek() == commandMap)
                {
                    // Grab the current state of the gamepad.
                    GamePadInput pad = GamePadInput.GetGamePad1();

                    // Switch to Mini-Hub?
                    if (pad.Back.WasPressed)
                    {
                        GamePadInput.ClearAllWasPressedState();
                        parent.ResetSim(CurrentLevelFilename());

                        // Needed to make sure that deactivated objects are actually removed from
                        // the list otherwise they may get saved along with the newly activated ones.
                        parent.Refresh(BokuGame.gameListManager.updateList, BokuGame.gameListManager.renderList);

                        parent.SwitchToMiniHub();
                        return;
                    }

                    // The start button should always return to run mode.
                    if (pad.Start.WasPressed)
                    {
                        BokuGame.Audio.GetCue("programming add").Play();
                        parent.CurrentUpdateMode = UpdateMode.RunSim;
                        HelpOverlay.Pop();
                        return;
                    }

                    // Return to previous mode
                    if (pad.ButtonB.WasPressed)
                    {
                        parent.CurrentUpdateMode = UpdateMode.RunSim;
                        HelpOverlay.Pop();
                        return;
                    }
                    
                    // Select current mode.
                    if (pad.ButtonA.WasPressed)
                    {
                        BokuGame.Audio.GetCue("programming add").Play();
                        parent.CurrentUpdateMode = curMode;
                        HelpOverlay.Pop();  // Remove whichever help overlay is active.
                    }

                    // Change selected mode.
                    if (pad.LeftTriggerButton.WasPressed || pad.DPadLeft.WasPressed)
                    {
                        BokuGame.Audio.GetCue("programming click").Play();
                        MoveToPreviousMode();
                        SetHelpOverlay();
                    }
                    if (pad.RightTriggerButton.WasPressed || pad.DPadRight.WasPressed)
                    {
                        BokuGame.Audio.GetCue("programming click").Play();
                        MoveToNextMode();
                        SetHelpOverlay();
                    }

                }   // end if we have input focus.

                UpdateCamera();
                UpdateWorld();
                UpdateEditBrush();

            }   // end of TopLevelPaletteUpdateObj Update()

            private void SetHelpOverlay()
            {
                HelpOverlay.Pop();
                switch (curMode)
                {
                    case UpdateMode.RunSim:
                        HelpOverlay.Push("TopLevelPaletteRunSim");
                        break;
                    case UpdateMode.EditObject:
                        HelpOverlay.Push("TopLevelPaletteEditObject");
                        break;
                    case UpdateMode.ToolBox:
                        HelpOverlay.Push("TopLevelPaletteToolBox");
                        break;
                    case UpdateMode.EditWorldParameters:
                        HelpOverlay.Push("TopLevelPaletteEditWorldParameters");
                        break;
                    case UpdateMode.EditObjectParameters:
                        HelpOverlay.Push("TopLevelPaletteEditObjectParameters");
                        break;
                }
            }   // end of TopLevelPalette SetHelpOverlay()

            public override void Activate()
            {
                base.Activate();

                BokuGame.Audio.GetCue("programming move out").Play();
                shared.topLevelPalette.Open = true;
                shared.topLevelPalette.Select = curMode;

                HelpOverlay.Push("TopLevelPalette");
                SetHelpOverlay();

            }   // end of TopLevelPaletteUpdateObj Activate()

            public override void Deactivate()
            {
                base.Deactivate();

                shared.topLevelPalette.Open = false;
            }   // end of TopLevelPaletteUpdateObj Deactivate()

        }   // end of class TopLevelPaletteUpdateObj

    }   // end of class InGame

}   // end of namespace Boku


