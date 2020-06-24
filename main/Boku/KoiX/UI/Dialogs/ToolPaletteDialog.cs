
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;
using KoiX.Input;
using KoiX.Geometry;
using KoiX.Managers;
using KoiX.Text;

using Boku;

namespace KoiX.UI.Dialogs
{
    /// <summary>
    /// Companion to the ToolBarDialog.  This contains the
    /// submenus and settings values needed for the current
    /// tool.
    /// </summary>
    public class ToolPaletteDialog : BaseDialogNonModal
    {
        #region Members

        KoiLibrary.InputDevice inputDevice = KoiLibrary.InputDevice.None;
        EditModeTools curTool = EditModeTools.None;

        WidgetSet set;

        ToolPaletteButton brushTypeButton;
        ToolPaletteButton terrainMaterialButton;
        ToolPaletteButton waterTypeButton;

        SpriteCamera camera;    // Local ref passed to dialogs we launch.

        #endregion

        #region Accessors
        #endregion

        #region Public

        public ToolPaletteDialog(ThemeSet theme = null)
            : base(theme: theme)
        {
#if DEBUG
            _name = "ToolPaletteDialog";
#endif
            // Change ref to what base class has set.
            // Allows us to use theme rather than this.Theme.
            theme = this.ThemeSet;

            rect = new RectangleF(0, 0, 100, 300);

            Focusable = false;  // By turning off focusable we prevent the DialogManager from trying to focus widgets
                                // and also prevent it from processing tabs.  The dialogManager normally uses tabs to 
                                // move to the next focusable widget.  In the case of editing though, we want to reserve
                                // tabs for moving the camera to the next actor.

            set = new WidgetSet(this, rect, Orientation.Vertical, horizontalJustification: Justification.Center, verticalJustification: Justification.Top);
            AddWidget(set);

            int buttonSize = 64;
            RectangleF buttonRect = new RectangleF(0, 0, buttonSize, buttonSize);

            brushTypeButton = new ToolPaletteButton(this, buttonRect, @"Textures\ToolMenu\Brushes", OnBrushType);
            set.AddWidget(brushTypeButton);
            
            terrainMaterialButton = new ToolPaletteButton(this, buttonRect, @"Textures\ToolMenu\TerrainMaterials", OnTerrainMaterial);
            set.AddWidget(terrainMaterialButton);

            waterTypeButton = new ToolPaletteButton(this, buttonRect, @"Textures\ToolMenu\WaterTypes", OnWaterType);
            set.AddWidget(waterTypeButton);

        }   // end of c'tor

        void OnBrushType(BaseWidget w)
        {
            DialogCenter.BrushTypeDialog.Init(curTool);
            DialogManagerX.ShowDialog(DialogCenter.BrushTypeDialog, camera);
        }   // end of OnBrushType()

        void OnTerrainMaterial(BaseWidget w)
        {
            DialogManagerX.ShowDialog(DialogCenter.TerrainMaterialDialog, camera);
        }   // end of OnTerrainMaterial()

        void OnWaterType(BaseWidget w)
        {
            DialogManagerX.ShowDialog(DialogCenter.WaterTypeDialog, camera);
        }   // end of OnWaterType()

        public override void Update(SpriteCamera camera)
        {
            this.camera = camera;

            // Keep positioned on left edge of screen.
            Vector2 offset = camera.ScreenSize / 2.0f / camera.Zoom;
            rect.Position = new Vector2(-offset.X, 0);


            bool needsRefresh = false;
            // Did the current tool change?
            if (curTool != ToolBarDialog.CurTool)
            {
                curTool = ToolBarDialog.CurTool;
                needsRefresh = true;
            }
            // Did the input device change?  Note that we
            // don't count switching between keyboard and 
            // mouse as a real switch needing a refresh.
            if (KoiLibrary.LastTouchedDevice != inputDevice)
            {
                switch(KoiLibrary.LastTouchedDevice)
                {
                    case KoiLibrary.InputDevice.Gamepad:
                    case KoiLibrary.InputDevice.Touch:
                        needsRefresh = true;
                        break;
                    case KoiLibrary.InputDevice.Mouse:
                        if (inputDevice != KoiLibrary.InputDevice.Keyboard)
                        {
                            needsRefresh = true;
                        }
                        break;
                    case KoiLibrary.InputDevice.Keyboard:
                        if (inputDevice != KoiLibrary.InputDevice.Mouse)
                        {
                            needsRefresh = true;
                        }
                        break;
                }
                inputDevice = KoiLibrary.LastTouchedDevice;
            }

            if (needsRefresh)
            {
                ClearAllButtons();
                SetUpButtons();
                needsRefresh = false;
            }


            base.Update(camera);
        }   // end of Update()

        public override void Activate(params object[] args)
        {
            base.Activate(args);
        }

        public override void Deactivate()
        {
            ClearAllButtons();
            base.Deactivate();
        }

        #endregion

        #region Internal

        /// <summary>
        /// Based on the current input mode and tool selection, 
        /// init the correct set of buttons for the palette.
        /// </summary>
        void SetUpButtons()
        {
            if (inputDevice == KoiLibrary.InputDevice.Touch)
            {
                switch(curTool)
                {
                    case EditModeTools.Home:
                        // Nothing to do here.
                        break;
                    case EditModeTools.Play:
                        // Nothing to do here.
                        break;
                    case EditModeTools.CameraMove:
                        break;
                    case EditModeTools.EditObject:
                        break;
                    case EditModeTools.Paths:
                        break;
                    case EditModeTools.TerrainPaint:
                        set.AddWidget(brushTypeButton);
                        set.AddWidget(terrainMaterialButton);
                        break;
                    case EditModeTools.TerrainRaiseLower:
                        set.AddWidget(brushTypeButton);
                        break;
                    case EditModeTools.Water:
                        set.AddWidget(waterTypeButton);
                        break;
                    case EditModeTools.EraseObjects:
                        set.AddWidget(brushTypeButton);
                        break;
                    case EditModeTools.WorldSettings:
                        // Nothing to do here.
                        break;
                }
            }

            if (inputDevice == KoiLibrary.InputDevice.Keyboard || inputDevice == KoiLibrary.InputDevice.Mouse)
            {
                switch (curTool)
                {
                    case EditModeTools.Home:
                        // Nothing to do here.
                        break;
                    case EditModeTools.Play:
                        // Nothing to do here.
                        break;
                    case EditModeTools.CameraMove:
                        break;
                    case EditModeTools.EditObject:
                        break;
                    case EditModeTools.Paths:
                        break;
                    case EditModeTools.TerrainPaint:
                        set.AddWidget(brushTypeButton);
                        set.AddWidget(terrainMaterialButton);
                        break;
                    case EditModeTools.TerrainRaiseLower:
                        set.AddWidget(brushTypeButton);
                        break;
                    case EditModeTools.Water:
                        set.AddWidget(waterTypeButton);
                        break;
                    case EditModeTools.EraseObjects:
                        set.AddWidget(brushTypeButton);
                        break;
                    case EditModeTools.WorldSettings:
                        // Nothing to do here.
                        break;
                }
            }

            // Note, we add nothing if in gamepad mode.

            // Activate the buttons we selected.
            set.Activate();

        }   // end of SetUpButtons()

        /// <summary>
        /// Removes and deactivates all the buttons currently in
        /// this dialog.
        /// </summary>
        void ClearAllButtons()
        {
            while (set.Widgets.Count > 0)
            {
                set.Widgets[set.Widgets.Count - 1].Deactivate();
                set.Widgets.RemoveAt(set.Widgets.Count - 1);
            }
        }   // end of ClearAllButtons()

        #endregion

    }   // end of class ToolPaletteDialog

}   // namespace KoiX.UI.Dialogs
