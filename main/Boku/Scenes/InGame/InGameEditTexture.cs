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
    /// UpdateObject for InGame -> EditTexture
    /// </summary>
    public partial class InGame : GameObject, INeedsDeviceReset
    {

        protected class EditTextureUpdateObj : BaseEditUpdateObj
        {

            // c'tor
            public EditTextureUpdateObj(InGame parent, ref Shared shared)
                : base(parent, ref shared)
            {
            }   // end of EditTextureUpdateObj c'tor


            /// <summary>
            /// EditTextureUpdateObj Update()
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
                        parent.SwitchToMiniHub();
                        return;
                    }

                    // Cycle through textures.
                    /*
                    if (pad.ButtonB.WasPressed)
                    {
                        Foley.PlayProgrammingClick();
                        shared.editBrushTextureIndex = (shared.editBrushTextureIndex + 1) % 4;  // Move to the right.
                    }
                    */
                    if (pad.ButtonX.WasPressed)
                    {
                        Foley.PlayProgrammingClick();
                        //shared.editBrushTextureIndex = (shared.editBrushTextureIndex + 3) % 4;  // Move to the left.
                        shared.editBrushTextureIndex = (shared.editBrushTextureIndex + 1) % 4;  // Move to the right.
                    }

                    // Texture picker.
                    if (pad.ButtonY.WasPressed)
                    {
                        parent.CurrentUpdateMode = UpdateMode.TexturePicker;
                        return;
                    }

                    // Paint texture?
                    if (pad.ButtonA.WasPressed || (pad.ButtonA.IsPressed && shared.editBrushMoved))
                    {
                        parent.terrain.UpdateSelectTexture(shared.editBrushTextureIndex, shared.editBrushPosition, shared.editBrushRadius, shared.editBrushIndex);
                        shared.textureSelectModified = true;
                        InGame.inGame.IsLevelDirty = true;
                    }


                }   // end if we have input focus.

                // Do the common bits of the Update().
                UpdateCamera();
                UpdateWorld();
                UpdateEditBrush();

            }   // end of EditTextureUpdateObj Update()

            public override void Activate()
            {
                base.Activate();

                HelpOverlay.Push("TextureEdit");
                parent.cursor3D.Deactivate();
            }   // end of EditTextureUpdateObj Activate()

        }   // end of class EditTextureUpdateObj

    }   // end of class InGame

}   // end of namespace Boku


