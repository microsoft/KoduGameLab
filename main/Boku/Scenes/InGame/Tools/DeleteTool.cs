
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
    public class DeleteTool : BaseTool
    {
        #region Members
        private static DeleteTool instance = null;
        #endregion Members

        #region Public
        // c'tor
        public DeleteTool()
        {
            Description = Strings.Instance.tools.deleteTool;
            HelpOverlayID = @"DeleteTool";
            HelpOverlayStartID = @"DeleteToolStart";
            HelpOverlayGoingID = @"DeleteToolGoing";
            IconTextureName = @"\UI2D\Tools\DeleteTool";

        }   // end of c'tor

        public static BaseTool GetInstance()
        {
            if (instance == null)
            {
                instance = new DeleteTool();
            }
            return instance;
        }   // end of DeleteTool GetInstance()

        public override void Update()
        {
            if (Active && !PickerXInUse && !PickerYInUse)
            {
                CheckSelectCursor();

                if(DebouncePending)
                    return;

                ProcessTriggers(
                    Terrain.EditMode.Delete,
                    Terrain.EditMode.Delete,
                    Terrain.EditMode.Noop);
                // 'A' or RightTrigger button deletes the terrain under the brush.

                SelectOverlay();
            }

            base.Update();
        }   // end of DeleteTool Update()
        #endregion Public

        #region Internal
        public override void OnActivate()
        {
            base.OnActivate();

            PickerX = brushPicker;      // Assign X button to brush picker and activate.
            brushPicker.BrushSet = Brush2DManager.BrushType.Binary
                | Brush2DManager.BrushType.StretchedBinary
                | Brush2DManager.BrushType.Selection;

        }   // end of HeightMapTool OnActivate()
        #endregion Internal

    }   // class DeleteTool

}   // end of namespace Boku.Scenes.InGame.Tools


