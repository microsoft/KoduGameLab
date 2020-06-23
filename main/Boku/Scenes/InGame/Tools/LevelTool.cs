
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

using Boku.Base;
using Boku.Common;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.SimWorld;
using Boku.SimWorld.Terra;

namespace Boku.Scenes.InGame.Tools
{
    public class LevelTool : BaseTool
    {
        #region Members
        private static LevelTool instance = null;

        private bool rightTriggered = false;
        #endregion Members

        #region Public
        // c'tor
        public LevelTool()
        {
            Description = Strings.Instance.tools.levelTool;
            HelpOverlayID = @"LevelTool";
            HelpOverlayStartID = @"LevelToolStart";
            HelpOverlayGoingID = @"LevelToolGoing";
            IconTextureName = @"\UI2D\Tools\MinMaxTool";
        }   // end of c'tor

        public static BaseTool GetInstance()
        {
            if (instance == null)
            {
                instance = new LevelTool();
            }
            return instance;
        }   // end of LevelTool GetInstance()

        public override void Update()
        {
            if (Active)
            {
                CheckSelectCursor();

                if (!PickerXInUse && !PickerYInUse)
                {
                    if (DebouncePending)
                        return;

                    CheckLevel();

                    ProcessTriggers(
                        Terrain.EditMode.Level,
                        Terrain.EditMode.LevelSnap,
                        Terrain.EditMode.Noop);

                    SelectOverlay();
                }
            }

            base.Update();
        }   // end of LevelTool Update()

        #endregion Public

        #region Internal
        /// <summary>
        /// Override when to show/hide the 3d cursor.
        /// </summary>
        /// <returns></returns>
        protected override void CheckSelectCursor(bool alwaysShowCursor)
        {
            Brush2DManager.Brush2D brush = Brush2DManager.GetBrush(shared.editBrushIndex);
            if (brush == null)
            {
                inGame.ShowCursor();
            }
        }

        /// <summary>
        /// Override which overlay to be showing.
        /// </summary>
        protected override void SelectOverlay()
        {
            HelpOverlay.Pop();
            string helpStart = HelpOverlayStartID == null ? HelpOverlayID : HelpOverlayStartID;
            string helpGoing = HelpOverlayGoingID == null ? HelpOverlayID : HelpOverlayGoingID;
            if (rightTriggered)
            {
                HelpOverlay.Push(@"LevelToolRightTrig");
            }
            else
            if (StretchGoing)
            {
                HelpOverlay.Push(helpGoing);
            }
            else
            {
                HelpOverlay.Push(helpStart);
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
            GamePadInput pad = GamePadInput.GetGamePad1();
            if (StretchGoing && RightTriggerOn)
            {
                rightTriggered = true;
            }

            aButton = rightTriggered ? Terrain.EditMode.Noop : Terrain.EditMode.LevelSnap;
            base.ProcessStretched(rightMode, aButton, leftMode);

            if (pad.ButtonA.WasPressed)
            {
                rightTriggered = false;
            }
        }

        /// <summary>
        /// Take snapshot of terrain height at appropriate times.
        /// </summary>
        private void CheckLevel()
        {
            GamePadInput pad = GamePadInput.GetGamePad1();
            if (pad.ButtonA.WasPressed || RightWasPressed)
            {
                float height = Terrain.GetTerrainHeight(shared.editBrushStart);
                inGame.Terrain.LevelHeight = height;
            }
            /// If we have a point selected, hide the cursor
            float startToEnd = Vector2.DistanceSquared(shared.editBrushPosition, shared.editBrushStart);
            const float kMaxDist = 0.1f * 0.1f;
            if (pad.ButtonA.IsPressed || RightTriggerOn || (startToEnd > kMaxDist))
            {
                inGame.HideCursor();
            }
            else
            {
                inGame.ShowCursor();
            }
        }

        public override void OnActivate()
        {
            base.OnActivate();

            PickerX = brushPicker;      // Assign X button to brush picker and activate.
            brushPicker.BrushSet = Brush2DManager.BrushType.All
                | Brush2DManager.BrushType.StretchedAll;

        }   // end of LevelTool OnActivate()

        #endregion Internal

    }   // class LevelTool

}   // end of namespace Boku.Scenes.InGame.Tools


