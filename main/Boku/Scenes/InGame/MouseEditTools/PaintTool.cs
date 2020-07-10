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

        /// <summary>
        /// Do the modifications needed to handle sampling terrain materials (with Alt key).
        /// Also sets up the correct editMode.
        /// </summary>
        public void HandleMouseInput()
        {
            if (!KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
            {
                return;
            }

            // In eyedropper mode, show the pointy cursor.
            if (KeyboardInputX.AltIsPressed)
            {
                inGame.Cursor3D.Rep = Cursor3D.Visual.Pointy;
                inGame.Cursor3D.Hidden = false;
            }
            else
            {
                inGame.Cursor3D.Hidden = true;
            }

            if (KeyboardInputX.AltIsPressed)
            {
                // Prevent terrain from being painted when Alt is being used to select material.
                editMode = Terrain.EditMode.Noop;

                // Sample the current terrain.
                if (MouseEdit.TriggerSample())
                {
                    LowLevelMouseInput.Left.IgnoreUntilReleased = true;
                    
                    Vector3 p = MouseEdit.MouseTouchHitInfo.TerrainPosition;
                    Vector2 pos = new Vector2(p.X, p.Y);

                    ushort matIdx = Terrain.GetMaterialType(pos);
                    if (TerrainMaterial.IsValid(matIdx, false, false))
                    {
                        Terrain.CurrentMaterialIndex = matIdx;
                        Foley.PlayCut();
                    }
                }
            } 
            else
            {
                // Set the mode based on whether or not 
                // option keys are pressed.
                editMode = Terrain.EditMode.PaintAndAddMaterial;
                if (KeyboardInputX.ShiftIsPressed)
                {
                    editMode = Terrain.EditMode.PaintMaterial;
                }
                else if (KeyboardInputX.CtrlIsPressed)
                {
                    editMode = Terrain.EditMode.AddAtCenter;
                }
            }
        }   // end of HandleMouseInput()

        /*
        public void HandleTouchInput()
        {
            if (!KoiLibrary.LastTouchedDeviceIsTouch)
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
        */

        public override void Update()
        {
            if (Active)
            {
                CheckSelectCursor(false);

                HandleMouseInput();

                SetEditModes(editMode, Terrain.EditMode.AddAtCenter, Terrain.EditMode.Delete);

                SelectOverlay();
            }

            base.Update();
        }   // end of Update()

        #endregion Public

        #region InputEventHandler
        #endregion

        #region Internal

        private object timerInstrument = null;

        protected override void OnActivate()
        {
            timerInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.InGamePaintTool);
            base.OnActivate();

            // If the location of the cursor is not over any terrain then
            // don't allow the magic brush as the default.
            if (Terrain.GetTerrainHeightFlat(Boku.InGame.inGame.Cursor3D.Position) == 0.0f)
            {
                // By not including the magic brush in the brush set we
                // force the picker to change the current brush to one
                // of the standard brushes if not already.
            }
        }   // end of OnActivate()

        protected override void OnDeactivate()
        {
            base.OnDeactivate();

            Terrain.Current.EndSelection();

            Instrumentation.StopTimer(timerInstrument);
        }   // end of OnDeactivate()
        #endregion Internal

    }   // class PaintTool

}   // end of namespace Boku.Scenes.InGame.MouseEditTools


