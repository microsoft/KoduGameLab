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
    public class RaiseLowerTool : BaseMouseEditTool
    {
        #region Members
        private static RaiseLowerTool instance = null;
        #endregion Members

        #region Public
        // c'tor
        public RaiseLowerTool()
        {
            HelpOverlayID = null;
            HelpOverlayStartID = @"RaiseLowerStart";
            HelpOverlayGoingID = @"RaiseLowerGoing";

            RightAudioStart = delegate() { Foley.PlayEarthUp(); };
            MiddleAudioStart = delegate() { Foley.PlayPaint(); };
            LeftAudioStart = delegate() { Foley.PlayEarthDown(); };
            RightAudioEnd = delegate() { Foley.StopEarthUp(); };
            MiddleAudioEnd = delegate() { Foley.StopPaint(); };
            LeftAudioEnd = delegate() { Foley.StopEarthDown(); };

        }   // end of c'tor

        public static BaseMouseEditTool GetInstance()
        {
            if (instance == null)
            {
                instance = new RaiseLowerTool();
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
                        Terrain.EditMode.Raise,
                        Terrain.EditMode.Smooth,
                        Terrain.EditMode.Lower);

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
            timerInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.InGameRaiseLowerTool);

            base.OnActivate();

            PickerX = brushPicker;      // Assign X button to brush picker and activate.
            brushPicker.BrushSet = Brush2DManager.BrushType.All
                | Brush2DManager.BrushType.Selection;

        }   // end of OnActivate()

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            Instrumentation.StopTimer(timerInstrument);

        }   // end of OnDeactivate()
        #endregion Internal

    }   // class RaiseLowerTool

}   // end of namespace Boku.Scenes.InGame.MouseEditTools


