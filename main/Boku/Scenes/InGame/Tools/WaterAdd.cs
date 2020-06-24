
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
    public class WaterAdd : BaseTool
    {
        #region Members
        private static WaterAdd instance = null;
        #endregion Members

        #region Public
        // c'tor
        public WaterAdd()
        {
            Description = Strings.Localize("tools.waterAdd");
            HelpOverlayID = @"WaterAdd";
            IconTextureName = @"\UI2D\Tools\WaterAdd";

            RightAudioStart = delegate() { Foley.PlayRaiseWater(); };
            MiddleAudioStart = delegate() { };
            LeftAudioStart = delegate() { Foley.PlayLowerWater(); };
            RightAudioEnd = delegate() { Foley.StopRaiseWater(); };
            MiddleAudioEnd = delegate() { };
            LeftAudioEnd = delegate() { Foley.StopLowerWater(); };

            // We don't want to see any brush rendered for this tool.
            prevBrushIndex = -1;
        }   // end of c'tor

        public static BaseTool GetInstance()
        {
            if (instance == null)
            {
                instance = new WaterAdd();
            }
            return instance;
        }   // end of WaterAdd GetInstance()

        public override void Update()
        {
            if (Active)
            {
                CheckSelectCursor(true);

                if (DebouncePending)
                    return;

                UpdateRates();

                ProcessTriggers(
                    Terrain.EditMode.WaterRaise,
                    Terrain.EditMode.WaterChange,
                    Terrain.EditMode.WaterLower);

                SelectOverlay();
            }

            base.Update();
        }   // end of WaterAdd Update()
        #endregion Public

        #region Internal
        protected override void ProcessPoint(
            Terrain.EditMode rightMode,
            Terrain.EditMode aButton,
            Terrain.EditMode leftMode)
        {
            bool saveRestore = (RightRate > kSmallRate) || (LeftRate > kSmallRate);

            int wasType = Water.CurrentType;
            if (saveRestore)
            {
                int waterType = Terrain.GetWaterType(shared.editBrushPosition);
                if (waterType != Water.InvalidType)
                {
                    Water.CurrentType = waterType;
                }
            }
            base.ProcessPoint(rightMode, aButton, leftMode);

            Water.CurrentType = wasType;
        }

        private object timerInstrument = null;

        public override void OnActivate()
        {
            timerInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.InGameWaterTool);
            
            base.OnActivate();

            inGame.ShowCursor();
        }   // end of HeightMapTool OnActivate()

        public override void OnDeactivate()
        {
            inGame.HideCursor();

            base.OnDeactivate();

            Instrumentation.StopTimer(timerInstrument);
        }   // end of WaterAdd OnDeactivate()
        #endregion Internal

    }   // class WaterAdd

}   // end of namespace Boku.Scenes.InGame.Tools


