
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
    /// UpdateObject for InGame -> EditHeightMap
    /// </summary>
    public partial class InGame : GameObject, INeedsDeviceReset
    {

        protected class EditHeightMapUpdateObj : BaseEditUpdateObj
        {
            private bool starting = false;

            // c'tor
            public EditHeightMapUpdateObj(InGame parent, ref Shared shared)
                : base(parent, ref shared)
            {
            }   // end of EditHeightMapUpdateObj c'tor


            /// <summary>
            /// EditHeightMapUpdateObj Update()
            /// </summary>
            /// <param name="camera"></param>
            public override void Update()
            {
                float secs = Time.WallClockFrameSeconds;

                /// Do the common bits of the Update().
                /// Do these first, as they are input to any editing below.
                UpdateCamera();
                UpdateWorld();
                UpdateEditBrush();

                // Check if we have input focus.  Don't do any input
                // related update if we don't.
                if (CommandStack.Peek() == commandMap)
                {
                    // Grab the current state of the gamepad.
                    GamePadInput pad = GamePadInput.GetGamePad1();

                    // Switch to Mini-Hub?
                    if (pad.Back.WasPressed)
                    {
                        parent.SwitchToMiniHub();
                        return;
                    }

                    /// Debounce. Make sure the button press that brought
                    /// us up is released before using it to make changes. 
                    if (starting)
                    {
                        if (pad.ButtonY.IsPressed
                            || pad.ButtonA.IsPressed
                            || pad.ButtonX.IsPressed)
                        {
                            return;
                        }
                        starting = false;
                    }

                    int modeShift = shared.editBrushBase;
                    // Up
                    if (pad.ButtonY.IsPressed)
                    {
                        InGame.inGame.terrain.RenderToHeightMap(
                            shared.editBrushIndex, 
                            shared.editBrushPosition, 
                            shared.editBrushRadius, 
                            0 + modeShift);
                        shared.heightMapModified = true;
                        InGame.inGame.IsLevelDirty = true;
                    }

                    // Smooth
                    if (pad.ButtonX.IsPressed)
                    {
                        InGame.inGame.terrain.RenderToHeightMap(
                            shared.editBrushIndex, 
                            shared.editBrushPosition, 
                            shared.editBrushRadius, 
                            2 + modeShift);
                        shared.heightMapModified = true;
                        InGame.inGame.IsLevelDirty = true;
                    }

                    // Down
                    if (pad.ButtonA.IsPressed)
                    {
                        InGame.inGame.terrain.RenderToHeightMap(
                            shared.editBrushIndex, 
                            shared.editBrushPosition, 
                            shared.editBrushRadius, 1 + modeShift);
                        shared.heightMapModified = true;
                        InGame.inGame.IsLevelDirty = true;
                    }

                }   // end if we have input focus.

            }   // end of EditHeightMapUpdateObj Update()

            public override void Activate()
            {
                base.Activate();

                HelpOverlay.Push("HeightMapEdit");
                parent.cursor3D.Deactivate();

                starting = true;
            }   // end of EditHeightMapUpdateObj Activate()

        }   // end of class EditHeightMapUpdateObj



    }   // end of class InGame

}   // end of namespace Boku


