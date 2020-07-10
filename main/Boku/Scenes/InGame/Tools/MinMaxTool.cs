// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.SimWorld;
using Boku.SimWorld.Terra;

namespace Boku.Scenes.InGame.Tools
{
    public class MinMaxTool : BaseTool
    {
        private static MinMaxTool instance = null;

        // c'tor
        public MinMaxTool()
        {
            Description = Strings.Instance.tools.minMaxTool;
            HelpOverlayID = @"MinMaxTool";
            HelpOverlayStartID = @"MinMaxToolStart";
            HelpOverlayGoingID = @"MinMaxToolGoing";
            IconTextureName = @"\UI2D\Tools\MinMaxTool";
        }   // end of c'tor

        public static BaseTool GetInstance()
        {
            if (instance == null)
            {
                instance = new MinMaxTool();
            }
            return instance;
        }   // end of MinMaxTool GetInstance()

        public override void Update()
        {
            if (Active)
            {
                CheckSelectCursor();

                if (!PickerXInUse && !PickerYInUse)
                {
                    if (DebouncePending)
                        return;

                    ProcessTriggers(
                        Terrain.EditMode.Max,
                        Terrain.EditMode.Noop,
                        Terrain.EditMode.Min);

                    SelectOverlay();
                }
            }

            base.Update();
        }   // end of MinMaxTool Update()

        public override void OnActivate()
        {
            base.OnActivate();

            PickerX = brushPicker;      // Assign X button to brush picker and activate.
            brushPicker.BrushSet = Brush2DManager.BrushType.All
                | Brush2DManager.BrushType.StretchedAll
                | Brush2DManager.BrushType.Selection;

        }   // end of MinMaxTool OnActivate()

    }   // class MinMaxTool

}   // end of namespace Boku.Scenes.InGame.Tools


