
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
    public class PaintMaterialTool : BaseTool
    {
        #region Members
        private static PaintMaterialTool instance = null;
        #endregion Members

        #region Public
        // c'tor
        public PaintMaterialTool()
        {
            Description = Strings.Instance.tools.paintMaterialTool;
            HelpOverlayID = @"PaintMaterialTool";
            HelpOverlayStartID = @"PaintMaterialToolStart";
            HelpOverlayGoingID = @"PaintMaterialToolGoing";
            IconTextureName = @"\UI2D\Tools\PaintMaterialTool";

        }   // end of c'tor

        public static BaseTool GetInstance()
        {
            if (instance == null)
            {
                instance = new PaintMaterialTool();
            }
            return instance;
        }   // end of PaintMaterialTool GetInstance()

        public override void Update()
        {
            if (Active && !PickerXInUse && !PickerYInUse)
            {
                CheckSelectCursor();

                if (DebouncePending)
                    return;

                ProcessTriggers(
                    Terrain.EditMode.PaintMaterial,
                    Terrain.EditMode.PaintMaterial,
                    Terrain.EditMode.Noop);

                SelectOverlay();
            }

            base.Update();
        }   // end of PaintMaterialTool Update()
        #endregion Public

        #region Internal
        public override void OnActivate()
        {
            base.OnActivate();

            PickerX = brushPicker;      // Assign X button to brush picker and activate.
            brushPicker.BrushSet = Brush2DManager.BrushType.Binary
                | Brush2DManager.BrushType.StretchedBinary
                | Brush2DManager.BrushType.Selection;

            PickerY = materialPicker;   // Assign Y button to material picker and activate.

            inGame.Terrain.ContainSelection = false;
        }   // end of HeightMapTool OnActivate()

        public override void OnDeactivate()
        {
            inGame.Terrain.ContainSelection = true;
            base.OnDeactivate();
        }
        #endregion Internal

    }   // class PaintMaterialTool

}   // end of namespace Boku.Scenes.InGame.Tools


