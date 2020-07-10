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
    public class HillTool : BaseTool
    {
        #region Members
        private static HillTool instance = null;
        #endregion Members

        #region Public
        // c'tor
        public HillTool()
        {
            Description = Strings.Instance.tools.hillTool;
            HelpOverlayID = @"HillTool";
            HelpOverlayStartID = @"HillToolStart";
            HelpOverlayGoingID = @"HillToolGoing";
            IconTextureName = @"\UI2D\Tools\HillTool";
            prevBrushIndex = Brush2DManager.NumBrushes - 1;
        }   // end of c'tor

        public static BaseTool GetInstance()
        {
            if (instance == null)
            {
                instance = new HillTool();
            }
            return instance;
        }   // end of HillTool GetInstance()

        public override void Update()
        {
            if (Active && !PickerXInUse && !PickerYInUse)
            {
                CheckSelectCursor();

                if (DebouncePending)
                    return;

                GamePadInput pad = GamePadInput.GetGamePad1();
                if (LeftRate < kSmallRate)
                {
                    /// Reseed everytime they switch modes.
                    Terrain.Reseed();
                }

                ProcessTriggers(
                    Terrain.EditMode.Hill,
                    Terrain.EditMode.Noop,
                    Terrain.EditMode.Smooth);

                SelectOverlay();
            }

            base.Update();
        }   // end of HillTool Update()
        #endregion Public

        #region Internal
        public override void OnActivate()
        {
            base.OnActivate();

            Terrain.Reseed();

            PickerX = brushPicker;      // Assign X button to brush picker and activate.
            brushPicker.BrushSet = Brush2DManager.BrushType.All
                | Brush2DManager.BrushType.StretchedAll
                | Brush2DManager.BrushType.Selection;

        }   // end of HillTool OnActivate()

        public override void OnDeactivate()
        {
            base.OnDeactivate();
        }   // end of HillTool OnDeactivate()
        #endregion Internal

    }   // class HillTool

}   // end of namespace Boku.Scenes.InGame.Tools


