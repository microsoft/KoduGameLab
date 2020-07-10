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
    public class WaterTerrainHint : BaseHint
    {
        private bool prevEditingWater = false;

        public WaterTerrainHint()
        {
            id = "WaterTerrainHint";

            toastText = Strings.Localize("toast.waterTerrainToast");
            modalText = Strings.Localize("toast.waterTerrainModal");
        }

        public override bool Update()
        {
            bool activate = false;

            bool editingWater = false;

            if(GamePadInput.ActiveMode == GamePadInput.InputMode.GamePad)
            {
                // GamePad mode.
                if(InGame.inGame.CurrentUpdateMode == InGame.UpdateMode.TerrainWater)
                {
                    
                    editingWater = true;
                }
            }
            else
            {
                // MouseEdit mode.
                if(InGame.inGame.CurrentUpdateMode == InGame.UpdateMode.MouseEdit)
                {
                    if(InGame.inGame.mouseEditUpdateObj.ToolBar.CurrentMode == InGame.BaseEditUpdateObj.ToolMode.WaterRaiseLower &&
                        InGame.inGame.mouseEditUpdateObj.ToolBar.RevertMode == InGame.BaseEditUpdateObj.ToolMode.WaterRaiseLower)
                    {
                        editingWater = true;
                    }
                }
            }

            // If we're making the transition from not editing to editing water, activate if terrain is flat.
            if(!prevEditingWater && editingWater)
            {
                if (InGame.inGame.Terrain.MaxHeight < 0.25f)
                {
                    activate = true;
                }
            }

            prevEditingWater = editingWater;

            return activate;
        }

    }   // end of class WaterTerrainHint

}   // end of namespace Boku.Common.HintSystem
