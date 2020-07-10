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
    public class SpikeyHillyTool : BaseMouseEditTool
    {
        #region Members
        private static SpikeyHillyTool instance = null;
        #endregion Members

        #region Public
        // c'tor
        public SpikeyHillyTool()
        {
            HelpOverlayID = null;
            HelpOverlayStartID = @"SpikeyHillyStart";
            HelpOverlayGoingID = @"SpikeyHillyGoing";

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
                instance = new SpikeyHillyTool();
            }
            return instance;
        }   // end of GetInstance()

        public override void Update()
        {
            if (Active)
            {
                CheckSelectCursor(false);

                SetEditModes(Terrain.EditMode.Roughen, Terrain.EditMode.Smooth, Terrain.EditMode.Hill);

                ProcessTriggers(
                    Terrain.EditMode.Roughen,
                    Terrain.EditMode.Smooth,
                    Terrain.EditMode.Hill);

                SelectOverlay();
            }

            base.Update();
        }   // end of Update()
        #endregion Public

        #region Internal

        private object timerInstrument = null;
        protected override void OnActivate()
        {
            timerInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.InGameSpikeyHillyTool);
            base.OnActivate();

            // If the location of the cursor is not over any terrain then
            // don't allow the magic brush as the default.
            if (Terrain.GetTerrainHeightFlat(Boku.InGame.inGame.Cursor3D.Position) == 0.0f)
            {
                // By not including the magic brush in the brush set we
                // force the picker to change the current brush to one
                // of the standard brushes if not already.
            }
        }   // end of OnActivate()

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            Instrumentation.StopTimer(timerInstrument);
        }   // end of OnDeactivate()
        #endregion Internal

    }   // class SpikeyHillyTool

}   // end of namespace Boku.Scenes.InGame.MouseEditTools


