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
    public class PaintTool : BaseMouseEditTool
    {
        #region Members
        private static PaintTool instance = null;
        #endregion Members

        #region Public
        // c'tor
        public PaintTool()
        {
            HelpOverlayID = @"MouseEditTerrainPaint";
            HelpOverlayMagicBrushID = @"MouseEditTerrainPaintMagicBrush";

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
                instance = new PaintTool();
            }
            return instance;
        }   // end of GetInstance()

        Terrain.EditMode editMode = Terrain.EditMode.AddAtCenter;

        public void HandleMouseInput()
        {
            if (GamePadInput.ActiveMode != GamePadInput.InputMode.KeyboardMouse)
            {
                return;
            }
            // In eyedropper mode, show the pointy cursor.
            if (KeyboardInput.AltIsPressed)
            {
                inGame.Cursor3D.Rep = Cursor3D.Visual.Pointy;
                inGame.Cursor3D.Hidden = false;
            }
            else
            {
                inGame.Cursor3D.Hidden = true;
            }

            if (KeyboardInput.AltIsPressed)
            {
                // Sample the current terrain.
                if (MouseEdit.TriggerSample())
                {
                    MouseInput.Left.IgnoreUntilReleased = true;
                    
                    // Prevent terrain from being painted when Alt is being used to select material.
                    editMode = Terrain.EditMode.Noop;

                    Vector3 p = MouseEdit.HitInfo.TerrainPosition;
                    Vector2 pos = new Vector2(p.X, p.Y);

                    ushort matIdx = Terrain.GetMaterialType(pos);
                    if (TerrainMaterial.IsValid(matIdx, false, false))
                    {
                        Terrain.CurrentMaterialIndex = matIdx;
                        Foley.PlayCut();
                    }
                }
            } 
            else if (!PickerXInUse && !PickerYInUse)
            {
                if (DebouncePending)
                    return;

                if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                {
                    // Set the mode based on whether or not 
                    // option keys are pressed.
                    if (MouseInput.Left.WasPressed)
                    {
                        editMode = Terrain.EditMode.PaintAndAddMaterial;
                        if (KeyboardInput.ShiftIsPressed)
                        {
                            editMode = Terrain.EditMode.PaintMaterial;
                        }
                        else if (KeyboardInput.CtrlIsPressed)
                        {
                            editMode = Terrain.EditMode.AddAtCenter;
                        }
                    }
                }
            }
        }

        public void HandleTouchInput()
        {
            if (GamePadInput.ActiveMode != GamePadInput.InputMode.Touch)
            {
                return;
            }

            inGame.Cursor3D.Rep = Cursor3D.Visual.Pointy;

            if (shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.baPaintMaterial)
            {
                if (TouchInput.TouchCount > 0 && TouchInput.GetTouchContactByIndex(0).phase == TouchPhase.Began)
                {
                    editMode = Terrain.EditMode.PaintAndAddMaterial;
                }
            }
        }

        public override void Update(Camera camera)
        {
            if (Active)
            {
                CheckSelectCursor(false);

                HandleMouseInput();
                HandleTouchInput();

                ProcessTriggers(
                    editMode,
                    Terrain.EditMode.AddAtCenter,
                    Terrain.EditMode.Delete);

                SelectOverlay();
            }

            base.Update(camera);
        }   // end of Update()
        #endregion Public

        #region Internal
        private object timerInstrument = null;

        public override void OnActivate()
        {
            timerInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.InGamePaintTool);
            base.OnActivate();

            PickerX = brushPicker;      // Assign X button to brush picker and activate.

            // If the location of the cursor is not over any terrain then
            // don't allow the magic brush as the default.
            if (Terrain.GetTerrainHeightFlat(Boku.InGame.inGame.Cursor3D.Position) == 0.0f)
            {
                // By not including the magic brush in the brush set we
                // force the picker to change the current brush to one
                // of the standard brushes if not already.
                brushPicker.BrushSet = Brush2DManager.BrushType.Binary
                    | Brush2DManager.BrushType.StretchedBinary;
            }

            brushPicker.BrushSet = Brush2DManager.BrushType.Binary
                | Brush2DManager.BrushType.StretchedBinary
                | Brush2DManager.BrushType.Selection;
            brushPicker.UseAltOverlay = true;

            PickerY = materialPicker;   // Assign Y button to material picker and activate.
        }   // end of OnActivate()

        public override void OnDeactivate()
        {
            base.OnDeactivate();

            Terrain.Current.EndSelection();

            Instrumentation.StopTimer(timerInstrument);
        }   // end of OnDeactivate()
        #endregion Internal

    }   // class PaintTool

}   // end of namespace Boku.Scenes.InGame.MouseEditTools


