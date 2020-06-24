
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
    public class RoadLevelTool : BaseTool
    {
        #region Members
        private static RoadLevelTool instance = null;

        private Vector2 endSamplePosition = Vector2.Zero;
        #endregion Members

        #region Accessors

        #endregion Accessors

        #region Public
        // c'tor
        public RoadLevelTool()
        {
            Description = Strings.Localize("tools.levelTool");
            HelpOverlayID = @"LevelTool";
            HelpOverlayStartID = @"LevelToolStart";
            HelpOverlayGoingID = @"LevelToolGoing";
            IconTextureName = @"\UI2D\Tools\MinMaxTool";

            RightAudioStart = delegate() { Foley.PlayEarthUp(); };
            MiddleAudioStart = delegate() { Foley.PlayEarthDown(); };
            LeftAudioStart = delegate() { Foley.PlayPaint(); };
            RightAudioEnd = delegate() { Foley.StopEarthUp(); };
            MiddleAudioEnd = delegate() { Foley.StopEarthDown(); };
            LeftAudioEnd = delegate() { Foley.StopPaint(); };
        }   // end of c'tor

        public static BaseTool GetInstance()
        {
            if (instance == null)
            {
                instance = new RoadLevelTool();
            }
            return instance;
        }   // end of RoadLevelTool GetInstance()

        public override void Update()
        {
            if (Active)
            {
                CheckSelectCursor(true);

                if (DebouncePending)
                    return;

                UpdateRates();

                CheckLevel();

                ProcessTriggers(
                    Terrain.EditMode.Level,
                    Terrain.EditMode.Smooth,
                    Terrain.EditMode.Smooth);

                SelectOverlay();
            }

            base.Update();
        }   // end of RoadLevelTool Update()

        #endregion Public

        #region Internal

        /// <summary>
        /// Take snapshot of terrain height at appropriate times.
        /// </summary>
        protected virtual void CheckLevel()
        {
            if (!InStretchMode)
            {
                GamePadInput pad = GamePadInput.GetGamePad0();
                if (pad.ButtonA.WasPressed || RightWasPressed || LeftWasPressed)
                {
                    shared.editBrushStart = shared.editBrushPosition;
                    float height = Terrain.GetTerrainHeightFlat(shared.editBrushStart);
                    inGame.Terrain.LevelHeight = height;
                }
                /// If we have a point selected, hide the cursor
                if (pad.ButtonA.IsPressed || RightTriggerOn)
                {
                    inGame.HideCursor();
                }
                else
                {
                    inGame.ShowCursor();
                }
            }
        }

        /// <summary>
        /// Override to make the A button do a snap in Stretch mode, instead of the usual
        /// blend in other modes.
        /// </summary>
        /// <param name="rightMode"></param>
        /// <param name="aButton"></param>
        /// <param name="leftMode"></param>
        /// <summary>
        /// If we're using the right trigger, then null out the a button action.
        /// It will still advance the cursor, but we don't want to do a road snap over it.
        /// If we're not doing right trigger, make a button instant.
        /// </summary>
        /// <param name="rightMode"></param>
        /// <param name="aButton"></param>
        /// <param name="leftMode"></param>
        protected override void ProcessStretched(
            Terrain.EditMode rightMode,
            Terrain.EditMode aButton,
            Terrain.EditMode leftMode)
        {
            rightMode = Terrain.EditMode.Road;
            aButton = Terrain.EditMode.RoadSnap;
            
            base.ProcessStretched(rightMode, aButton, leftMode);

        }

        private object timerInstrument = null;
        public override void OnActivate()
        {
            timerInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.InGameSmoothLevelTool);
            base.OnActivate();

            Boku.InGame.inGame.Cursor3D.Hidden = true;

            endSamplePosition = shared.editBrushPosition;
            inGame.Terrain.LevelHeight
                = Terrain.GetTerrainHeightFlat(shared.editBrushPosition);

        }   // end of RoadLevelTool OnActivate()

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            Instrumentation.StopTimer(timerInstrument);
        }   // end of RoadLevelTool OnDeactivate()

        #endregion Internal

    }   // class RoadLevelTool

}   // end of namespace Boku.Scenes.InGame.Tools


