
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

        }   // end of c'tor

        public static BaseMouseEditTool GetInstance()
        {
            if (instance == null)
            {
                instance = new WaterTool();
            }
            return instance;
        }   // end of GetInstance()

        public override void Update()
        {
            if (Active)
            {
                CheckSelectCursor(false);

                SetEditModes(Terrain.EditMode.WaterRaise, Terrain.EditMode.WaterChange, Terrain.EditMode.WaterLower);

                SelectOverlay();
            }

            base.Update();
        }   // end of Update()
        #endregion Public

        #region Internal

        protected override void ProcessPoint()
        {
            bool saveRestore = false;

            if (KoiLibrary.LastTouchedDeviceIsTouch)
            {
                saveRestore = TouchInput.TouchCount > 0 && Boku.InGame.inGame.TouchEdit.HasNonUITouch();
            }
            else
            {
                saveRestore = LowLevelMouseInput.Right.IsPressed || LowLevelMouseInput.Left.IsPressed;
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
            base.ProcessPoint();

            Water.CurrentType = wasType;
        }   // end of ProcessPoint()

        private object timerInstrument = null;

        protected override void OnActivate()
        {
            timerInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.InGameWaterTool);

            base.OnActivate();

            inGame.Cursor3D.Rep = Cursor3D.Visual.Pointy;
            inGame.Cursor3D.Hidden = false;

            inGame.ShowCursor();

        }   // end of OnActivate()

        protected override void OnDeactivate()
        {
            inGame.HideCursor();

            base.OnDeactivate();

            inGame.Cursor3D.Rep = Cursor3D.Visual.Edit;
            inGame.Cursor3D.Hidden = true;

            Instrumentation.StopTimer(timerInstrument);

        }   // end of OnDeactivate()
        #endregion Internal

    }   // class WaterTool

}   // end of namespace Boku.Scenes.InGame.MouseEditTools


