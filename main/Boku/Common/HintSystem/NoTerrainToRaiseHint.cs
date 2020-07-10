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

namespace Boku.Common.HintSystem
{
    public class NoTerrainToRaiseHint : BaseHint
    {
        private bool prevEditingTerrain = false;

        public NoTerrainToRaiseHint()
        {
            id = "NoTerrainToRaiseHint";

            toastText = Strings.Localize("toast.noTerrainToRaiseToast");
            modalText = Strings.Localize("toast.noTerrainToRaiseModal");
        }

        public override bool Update()
        {
            bool activate = false;

            bool editingTerrain = false;

            if (GamePadInput.ActiveMode == GamePadInput.InputMode.GamePad)
            {
                // GamePad mode.
                if (InGame.inGame.CurrentUpdateMode == InGame.UpdateMode.TerrainUpDown
                    || InGame.inGame.CurrentUpdateMode == InGame.UpdateMode.TerrainRoughHill
                    || InGame.inGame.CurrentUpdateMode == InGame.UpdateMode.TerrainFlatten)
                {
                    editingTerrain = true;
                }
            }
            else
            {
                // MouseEdit mode.
                if (InGame.inGame.CurrentUpdateMode == InGame.UpdateMode.MouseEdit)
                {
                    if ((InGame.inGame.mouseEditUpdateObj.ToolBar.CurrentMode == InGame.BaseEditUpdateObj.ToolMode.TerrainRaiseLower &&
                        InGame.inGame.mouseEditUpdateObj.ToolBar.RevertMode == InGame.BaseEditUpdateObj.ToolMode.TerrainRaiseLower)
                        || (InGame.inGame.mouseEditUpdateObj.ToolBar.CurrentMode == InGame.BaseEditUpdateObj.ToolMode.TerrainSmoothLevel &&
                        InGame.inGame.mouseEditUpdateObj.ToolBar.RevertMode == InGame.BaseEditUpdateObj.ToolMode.TerrainSmoothLevel)
                        || (InGame.inGame.mouseEditUpdateObj.ToolBar.CurrentMode == InGame.BaseEditUpdateObj.ToolMode.TerrainSpikeyHilly &&
                        InGame.inGame.mouseEditUpdateObj.ToolBar.RevertMode == InGame.BaseEditUpdateObj.ToolMode.TerrainSpikeyHilly)
                        )
                    {
                        editingTerrain = true;
                    }
                }
            }

            // If we're making the transition from not editing to editing terrain, activate if there is no terrain.
            if (!prevEditingTerrain && editingTerrain)
            {
                if (InGame.inGame.Terrain.MaxHeight == 0.0f)
                {
                    activate = true;
                }
            }

            prevEditingTerrain = editingTerrain;

            return activate;
        }

    }   // end of class NoTerrainToRaiseHint

}   // end of namespace Boku.Common.HintSystem
