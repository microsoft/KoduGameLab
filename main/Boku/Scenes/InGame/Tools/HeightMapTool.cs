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

using Boku.Audio;
using Boku.Base;
using Boku.Common;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.SimWorld;
using Boku.SimWorld.Terra;

namespace Boku.Scenes.InGame.Tools
{
    public class HeightMapTool : BaseTool
    {
        #region Members
        private static HeightMapTool instance = null;
        #endregion Members

        #region Public
        // c'tor
        public HeightMapTool()
        {
            Description = Strings.Localize("tools.heightMapTool");
            HelpOverlayID = @"HeightMapTool";
            HelpOverlayStartID = @"HeightMapToolStart";
            HelpOverlayGoingID = @"HeightMapToolGoing";
            IconTextureName = @"\UI2D\Tools\HeightMapTool";

            RightAudioStart = delegate() { Foley.PlayEarthUp(); };
            MiddleAudioStart = delegate() { Foley.PlayPaint(); };
            LeftAudioStart = delegate() { Foley.PlayEarthDown(); };
            RightAudioEnd = delegate() { Foley.StopEarthUp(); };
            MiddleAudioEnd = delegate() { Foley.StopPaint(); };
            LeftAudioEnd = delegate() { Foley.StopEarthDown(); };

        }   // end of c'tor

        public static BaseTool GetInstance()
        {
            if (instance == null)
            {
                instance = new HeightMapTool();
            }
            return instance;
        }   // end of HeightMapTool GetInstance()

        public override void Update()
        {
            if (Active)
            {
                CheckSelectCursor(false);

                if (!PickerXInUse && !PickerYInUse)
                {
                    if (DebouncePending)
                        return;

                    UpdateRates();

                    ProcessTriggers(
                        Terrain.EditMode.Raise,
                        Terrain.EditMode.Smooth,
                        Terrain.EditMode.Lower);

                    SelectOverlay();
                }
            }

            base.Update();
        }   // end of HeightMapTool Update()
        #endregion Public

        #region Internal
        private object timerInstrument = null;

        public override void OnActivate()
        {
            timerInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.InGameRaiseLowerTool);

            base.OnActivate();

            Boku.InGame.inGame.Cursor3D.Hidden = true;

            PickerX = brushPicker;      // Assign X button to brush picker and activate.
            brushPicker.BrushSet = Brush2DManager.BrushType.All 
                | Brush2DManager.BrushType.StretchedAll
                | Brush2DManager.BrushType.Selection;

        }   // end of HeightMapTool OnActivate()

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            Instrumentation.StopTimer(timerInstrument);

        }   // end of NoiseTool OnDeactivate()
        #endregion Internal

    }   // class HeightMapTool

}   // end of namespace Boku.Scenes.InGame.Tools


