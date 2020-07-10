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

namespace Boku.Scenes.InGame.MouseEditTools
{
    public class SmoothLevelTool : BaseMouseEditTool
    {
        #region Members

        private static SmoothLevelTool instance = null;

        #endregion Members

        #region Public

        // c'tor
        public SmoothLevelTool()
        {
            HelpOverlayID = null;
            HelpOverlayStartID = @"SmoothLevelStart";
            HelpOverlayGoingID = @"SmoothLevelGoing";

            RightAudioStart = delegate() { Foley.PlayEarthUp(); };
            MiddleAudioStart = delegate() { Foley.PlayPaint(); };
            LeftAudioStart = delegate() { Foley.PlayEraseLand(); };
            RightAudioEnd = delegate() { Foley.StopEarthUp(); };
            MiddleAudioEnd = delegate() { Foley.StopPaint(); };
            LeftAudioEnd = delegate() { Foley.StopEraseLand(); };

        }   // end of c'tor

        public static BaseMouseEditTool GetInstance()
        {
            if (instance == null)
            {
                instance = new SmoothLevelTool();
            }
            return instance;
        }   // end of GetInstance()

        public override void Update(Camera camera)
        {
            if (Active)
            {
                CheckSelectCursor(false);

                if (!PickerXInUse && !PickerYInUse)
                {
                    if (DebouncePending)
                        return;

                    ProcessTriggers(
                        Terrain.EditMode.Smooth,
                        Terrain.EditMode.Smooth,
                        Terrain.EditMode.Level);

                    SelectOverlay();
                }
            }

            base.Update(camera);
        }   // end of Update()

        #endregion Public

        #region Internal
        private object timerInstrument = null;

        public override void OnActivate()
        {
            timerInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.InGameSmoothLevelTool);
            base.OnActivate();

            PickerX = brushPicker;      // Assign X button to brush picker and activate.

            // If the location of the cursor is not over any terrain then
            // don't allow the magic brush as the default.
            if (Terrain.GetTerrainHeightFlat(Boku.InGame.inGame.Cursor3D.Position) == 0.0f)
            {
                // By not including the magic brush in the brush set we
                // force the picker to change the current brush to one
                // of the standard brushes if not already.
                brushPicker.BrushSet = Brush2DManager.BrushType.Binary;
            }

            brushPicker.BrushSet = Brush2DManager.BrushType.Binary
                | Brush2DManager.BrushType.Selection;
            brushPicker.UseAltOverlay = true;

            inGame.Terrain.LevelHeight = Terrain.GetTerrainHeightFlat(shared.editBrushPosition);

            PickerY = materialPicker;   // Assign Y button to material picker and activate.
        }   // end of OnActivate()

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            Instrumentation.StopTimer(timerInstrument);
        }   // end of OnDeactivate()
        #endregion Internal

    }   // class SmoothLevelTool

}   // end of namespace Boku.Scenes.InGame.MouseEditTools


