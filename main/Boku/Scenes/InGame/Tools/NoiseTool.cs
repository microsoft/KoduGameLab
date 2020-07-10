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

using KoiX;
using KoiX.Input;

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
    public class NoiseTool : BaseTool
    {
        #region Members
        private static NoiseTool instance = null;
        #endregion Members

        #region Public
        // c'tor
        public NoiseTool()
        {
            Description = Strings.Localize("tools.noiseTool");
            HelpOverlayID = @"NoiseTool";
            HelpOverlayStartID = @"NoiseToolStart";
            HelpOverlayGoingID = @"NoiseToolGoing";
            IconTextureName = @"\UI2D\Tools\NoiseTool";

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
                instance = new NoiseTool();
            }
            return instance;
        }   // end of NoiseTool GetInstance()

        public override void Update()
        {
            if (Active)
            {
                CheckSelectCursor(false);

                if (DebouncePending)
                    return;

                GamePadInput pad = GamePadInput.GetGamePad0();
                if (RightRate < kSmallRate)
                {
                    /// Reseed everytime they switch modes.
                    Terrain.Reseed();
                }

                UpdateRates();

                ProcessTriggers(
                    Terrain.EditMode.Hill,
                    Terrain.EditMode.Smooth,
                    Terrain.EditMode.Roughen);

                SelectOverlay();
            }

            base.Update();
        }   // end of NoiseTool Update()
        #endregion Public

        #region Internal
        private object timerInstrument = null;
        public override void OnActivate()
        {
            timerInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.InGameSpikeyHillyTool);
            
            base.OnActivate();

            Boku.InGame.inGame.Cursor3D.Hidden = true;

            Terrain.Reseed();

        }   // end of NoiseTool OnActivate()

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            Instrumentation.StopTimer(timerInstrument);
        }   // end of NoiseTool OnDeactivate()
        #endregion Internal

    }   // class NoiseTool

}   // end of namespace Boku.Scenes.InGame.Tools


