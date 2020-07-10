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

using KoiX;
using KoiX.Input;

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
    /// UpdateObject for InGame -> ToolMenu
    /// This is the update object for the main tool menu that 
    /// lets the user transition into the various editing tools.
    /// </summary>
    public partial class InGame : GameObject, INeedsDeviceReset
    {

        protected class ToolMenuUpdateObj : BaseEditUpdateObj
        {

            #region Accessors

            public CommandMap CommandMap
            {
                get { return commandMap; }
            }

            #endregion

            // c'tor
            public ToolMenuUpdateObj(InGame parent, ref Shared shared)
                : base(parent, ref shared)
            {
            }   // end of ToolMenuUpdateObj c'tor


            /// <summary>
            /// ToolMenuUpdateObj Update()
            /// </summary>
            /// <param name="camera"></param>
            public override void Update()
            {
                base.Update();

                // No need to check for input focus or anything.  If
                // we're active then the ToolMenu object has focus.

                shared.ToolMenu.Update();

                // If the ToolMenu is no longer active we're done.
                if (!shared.ToolMenu.Active)
                {
                    return;
                }

                // Do the common bits of the Update().  If our child is active we
                // can temporarily push/pop our commandMap to grab input focus.
                if (shared.ToolMenu.Active)
                {
                    CommandStack.Push(commandMap);
                }
                UpdateCamera(false);
                if (shared.ToolMenu.Active)
                {
                    CommandStack.Pop(commandMap);
                }
                UpdateWorld();
                // TODO (****) Should this only be called for tools that use the edit brush?
                // TODO (****) How do we turn off the edit brush rendering if we don't need it?
                UpdateEditBrush();

                GamePadInput pad = GamePadInput.GetGamePad0();

                // Run!
                if (pad.Back.WasPressed)
                {
                    // TODO (****) Transition to RunSim.
                    // Or is this already done in the base class???
                }

                // MiniHub!
                if (pad.Start.WasPressed)
                {
                    // TODO (****) Transition to MinHub.
                    // Or is this already done in the base class???
                }

                ToolTipManager.Update();
                ThoughtBalloonManager.Update(shared.camera);

            }   // end of ToolMenuUpdateObj Update()

            public override void Activate()
            {
                base.Activate();

                parent.cursor3D.Activate();
                parent.cursor3D.DiffuseColor = new Vector4(0.5f, 0.9f, 0.8f, 0.3f);

                shared.ToolMenu.Activate();
            }   // end of ToolMenuUpdateObj Activate()

            public override void Deactivate()
            {
                base.Deactivate();

                // Probably overkill.
                shared.ToolMenu.Deactivate();
            }

        }   // end of class ToolMenuUpdateObj

    }   // end of class InGame

}   // end of namespace Boku


