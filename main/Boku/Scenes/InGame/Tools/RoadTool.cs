// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Audio;
//using Microsoft.Xna.Framework.Content;
//using Microsoft.Xna.Framework.Graphics;
//using Microsoft.Xna.Framework.Input;
//using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.SimWorld;
using Boku.SimWorld.Terra;

namespace Boku.Scenes.InGame.Tools
{
    public class RoadTool : BaseTool
    {
        #region Members
        private static RoadTool instance = null;

        private bool rightTriggered = false;
        #endregion Members

        #region Public
        // c'tor
        public RoadTool()
        {
            Description = Strings.Instance.tools.roadTool;
            HelpOverlayID = @"RoadTool";
            HelpOverlayStartID = @"RoadToolStart";
            HelpOverlayGoingID = @"RoadToolGoing";
            IconTextureName = @"\UI2D\Tools\RoadTool";

        }   // end of c'tor

        public static BaseTool GetInstance()
        {
            if (instance == null)
            {
                instance = new RoadTool();
            }
            return instance;
        }   // end of RoadTool GetInstance()

        public override void Update()
        {
            if (Active)
            {
                CheckSelectCursor();

                if (DebouncePending)
                    return;

                CheckLevel();

                ProcessTriggers(
                    Terrain.EditMode.Road,
                    Terrain.EditMode.RoadSnap,
                    Terrain.EditMode.Smooth);

                SelectOverlay();
            }

            base.Update();
        }   // end of RoadTool Update()
        #endregion Public

        #region Internal
        protected override void SelectOverlay()
        {
            if (StretchGoing && rightTriggered)
            {
                HelpOverlay.Pop();
                HelpOverlay.Push(@"RoadToolRightTrig");
            }
            else
            {
                base.SelectOverlay();
            }
        }

        /// <summary>
        /// If we're using the right trigger, then null out the a button action.
        /// It will still advance the cursor, but we don't want to do a road snap over it.
        /// </summary>
        /// <param name="rightMode"></param>
        /// <param name="aButton"></param>
        /// <param name="leftMode"></param>
        protected override void ProcessStretched(
            Terrain.EditMode rightMode,
            Terrain.EditMode aButton,
            Terrain.EditMode leftMode)
        {
            GamePadInput pad = GamePadInput.GetGamePad1();
            if (StretchGoing && RightTriggerOn)
            {
                rightTriggered = true;
            }

            aButton = rightTriggered ? Terrain.EditMode.Noop : Terrain.EditMode.RoadSnap;
            base.ProcessStretched(Terrain.EditMode.Road, aButton, Terrain.EditMode.Smooth);

            if (pad.ButtonA.WasPressed)
            {
                rightTriggered = false;
            }
        }

        /// <summary>
        /// Grab snapshot terrain heights as appropriate.
        /// </summary>
        private void CheckLevel()
        {
            GamePadInput pad = GamePadInput.GetGamePad1();
            if (pad.ButtonA.WasPressed)
            {
                inGame.Terrain.LevelStart
                    = Terrain.GetTerrainHeight(shared.editBrushStart);
            }
            if(shared.editBrushMoved)
            {
                inGame.Terrain.LevelHeight
                    = Terrain.GetTerrainHeight(shared.editBrushPosition);
            }
        }

        public override void OnActivate()
        {
            base.OnActivate();

            PickerX = brushPicker;
            brushPicker.BrushSet = Brush2DManager.BrushType.StretchedAll;

        }   // end of HeightMapTool OnActivate()

        public override void OnDeactivate()
        {
            base.OnDeactivate();
        }   // end of RoadTool OnDeactivate()
        #endregion Internal
    }   // class RoadTool

}   // end of namespace Boku.Scenes.InGame.Tools


