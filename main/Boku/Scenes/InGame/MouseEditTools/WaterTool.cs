
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
    public class WaterTool : BaseMouseEditTool
    {
        #region Members
        private static WaterTool instance = null;
        #endregion Members

        #region Public
        // c'tor
        public WaterTool()
        {
            HelpOverlayID = @"MouseEditWaterRaiseLower";

            RightAudioStart = delegate() { Foley.PlayRaiseWater(); };
            MiddleAudioStart = delegate() { };
            LeftAudioStart = delegate() { Foley.PlayLowerWater(); };
            RightAudioEnd = delegate() { Foley.StopRaiseWater(); };
            MiddleAudioEnd = delegate() { };
            LeftAudioEnd = delegate() { Foley.StopLowerWater(); };

            // We don't want to see any brush rendered for this tool.
            prevBrushIndex = -1;

        }   // end of c'tor

        public static BaseMouseEditTool GetInstance()
        {
            if (instance == null)
            {
                instance = new WaterTool();
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
                        Terrain.EditMode.WaterRaise,
                        Terrain.EditMode.WaterChange,
                        Terrain.EditMode.WaterLower);

                    SelectOverlay();
                }
            }

            base.Update(camera);
        }   // end of Update()
        #endregion Public

        #region Internal

        protected override void ProcessPoint(
            Terrain.EditMode rightMode,
            Terrain.EditMode aButton,
            Terrain.EditMode leftMode)
        {
            bool saveRestore = false;

            if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
            {
                saveRestore = TouchInput.TouchCount > 0 && Boku.InGame.inGame.TouchEdit.HasNonUITouch();
            }
            else
            {
                saveRestore = MouseInput.Right.IsPressed || MouseInput.Left.IsPressed;
            }

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

            PickerY = waterPicker;

            inGame.ShowCursor();

        }   // end of OnActivate()

        public override void OnDeactivate()
        {
            inGame.HideCursor();

            base.OnDeactivate();

            Instrumentation.StopTimer(timerInstrument);

        }   // end of OnDeactivate()
        #endregion Internal

    }   // class WaterTool

}   // end of namespace Boku.Scenes.InGame.MouseEditTools


