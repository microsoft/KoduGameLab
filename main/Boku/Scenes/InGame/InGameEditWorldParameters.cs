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
    /// UpdateObject for InGame -> EditWorldParameters
    /// </summary>
    public partial class InGame : GameObject, INeedsDeviceReset
    {

        protected class EditWorldParametersUpdateObj : BaseEditUpdateObj
        {

            // c'tor
            public EditWorldParametersUpdateObj(InGame parent, ref Shared shared)
                : base(parent, ref shared)
            {
            }   // end of EditWorldParametersUpdateObj c'tor


            /// <summary>
            /// EditWorldParametersUpdateObj Update()
            /// </summary>
            /// <param name="camera"></param>
            public override void Update()
            {
                base.Update();

                if (AuthUI.IsModalActive)
                {
                    return;
                }

                //Because the text editor can show above the edit world parameter we need to update it first so it can steal inputs.
                if( shared.textEditor.Active )
                {
                    shared.textEditor.Update();
                }
                else if (shared.textLineDialog.Active)
                {
                    shared.textLineDialog.Update();
                }
                else
                {
                    // Jump straight to ToolMenu (unless load level menu was up - in which case, let it handle the escape)
                    if (Actions.ToolMenu.WasPressed && !BokuGame.bokuGame.loadLevelMenu.Active && !shared.editWorldParameters.IsInProgrammingTileMode())
                    {
                        Actions.ToolMenu.ClearAllWasPressedState();

                        Deactivate();
                        parent.CurrentUpdateMode = UpdateMode.ToolMenu;
                        return;
                    }

                    if (Actions.MiniHub.WasPressed)
                    {
                        Actions.MiniHub.ClearAllWasPressedState();

                        Deactivate();
                        parent.SwitchToMiniHub();

                        return;
                    }

                    shared.editWorldParameters.Update();

                    // Has the user exited?
                    if (shared.editWorldParameters.Active == false)
                    {
                        GamePadInput.ClearAllWasPressedState();
                        Deactivate();

                        return;
                    }
                }

                // Do the common bits of the Update().
                UpdateCamera();
                UpdateWorld();

            }   // end of EditWorldParametersUpdateObj Update()

            private object timerInstrument = null;

            public override void Activate()
            {
                if (!active)
                {
                    base.Activate();

                    HelpOverlay.Push("EditWorldParameters");
                    parent.cursor3D.Deactivate();
                    shared.editWorldParameters.Activate();

                    timerInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.InGameEditWorldParameters);
                }
            }   // end of EditWorldParametersUpdateObj Activate()

            public override void Deactivate()
            {
                if (active)
                {
                    shared.editWorldParameters.Deactivate(false);
                    HelpOverlay.Pop();

                    if (EditWorldParameters.CameraSetMode)
                    {
                        parent.CurrentUpdateMode = UpdateMode.EditObject;
                    }
                    else if (EditWorldParameters.NextLevelMode)
                    {
                        parent.CurrentUpdateMode = UpdateMode.SelectNextLevel;
                    }
                    else if (shared.editWorldParameters.IsInProgrammingTileMode())
                    {
                        parent.CurrentUpdateMode = UpdateMode.EditObject;
                    }
                    else
                    {
                        parent.CurrentUpdateMode = UpdateMode.ToolMenu;
                    }

                    Instrumentation.StopTimer(timerInstrument);

                    base.Deactivate();
                }
            }

        }   // end of class EditWorldParametersUpdateObj

    }   // end of class InGame

}


