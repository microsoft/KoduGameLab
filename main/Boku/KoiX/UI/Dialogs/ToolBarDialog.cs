// If defined, the toolbar is placed at the traditional, bottom of screen, position.
// If not defined, the toolbar is placed vertically along the right edge of the screen.
#define TOOLBAR_AT_BOTTOM

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
using Boku.Scenes.InGame.MouseEditTools;

namespace KoiX.UI.Dialogs
{
    public enum EditModeTools
    {
        Home,
        Play,
        CameraMove,
        EditObject,
        Paths,
        TerrainPaint,
        TerrainRaiseLower,
        TerrainSpikeyHilly,
        TerrainSmoothLevel,
        Water,
        EraseObjects,
        WorldSettings,
        None
    }

    /// <summary>
    /// Non-modal dialog which contains the tool bar for touch and key/mouse input modes.
    /// Note this is not used for gamepad input.  That is handled by GamePadToolBarDialog.
    /// </summary>
    public class ToolBarDialog : BaseDialogNonModal
    {

        #region Members

        static ToolBarDialog instance;  // Used for static calls.

        SpriteCamera camera;

        ToolBarButton homeButton;
        ToolBarButton playButton;
        ToolBarButton cameraMoveButton;
        ToolBarButton editObjectButton;
        ToolBarButton pathsButton;
        ToolBarButton terrainPaintButton;
        ToolBarButton terrainRaiseLowerButton;
        ToolBarButton waterButton;
        ToolBarButton eraseObjectsButton;
        ToolBarButton worldSettingsButton;

        WidgetSet set;

        List<ToolBarButton> siblings;   // The set of buttons.

        ToolBarButton curSelectedToolButton;
        BaseMouseEditTool currentEditTool;

        // When space is pressed, we temporarily move to the camera move tool.  This
        // keeps track of where we return when shift is released.
        ToolBarButton spaceReturnToolButton;

        bool needsBrush = false;    // Is the current tool one which needs to have the brush rendered?

        #endregion

        #region Accessors

        /// <summary>
        /// The tool which is currently recieving input.
        /// </summary>
        BaseMouseEditTool CurrentEditTool
        {
            get { return currentEditTool; }
            set
            {
                if (value != currentEditTool)
                {
                    if (currentEditTool != null)
                    {
                        currentEditTool.Active = false;
                    }
                    currentEditTool = value;
                    if (currentEditTool != null)
                    {
                        // We unregister and then re-register the ToolBarDialog so that
                        // it sits higher in the input stack than the tools.  This way 
                        // the dialog gets first crack at any input and can block input
                        // from going to the tools.  The tools register themselves on
                        // activation.
                        UnregisterForInputEvents();
                        currentEditTool.Active = true;
                        RegisterForInputEvents();

                        // Since the Active state of the buttons didn't change, they haven't
                        // re-registerd themselves for input so do it here.
                        foreach (ToolBarButton b in siblings)
                        {
                            b.RegisterForInputEvents();
                        }
                    }
                }
            }
        }

        public static EditModeTools CurTool
        {
            get { return instance.curSelectedToolButton.Tool; }
        }

        public static SpriteCamera Camera
        {
            get { return instance.camera; }
        }

        public static bool IsActive
        {
            get { return instance.Active; }
        }

        /// <summary>
        /// Returns true if the currenlty selected tools needs to
        /// have the brush rendered.
        /// </summary>
        public static bool NeedsBrush
        {
            get
            {
                // Calling this also updates the needsBrush flag.
                instance.FindCurrentEditTool();
                return instance.needsBrush;
            }
        }

        #endregion

        #region Public

        public ToolBarDialog(ThemeSet theme = null)
            : base(theme: theme)
        {
            instance = this;
#if DEBUG
            _name = "ToolBarDialog";
#endif
            // Change ref to what base class has set.
            // Allows us to use theme rather than this.Theme.
            theme = this.ThemeSet;

            float buttonSize = ToolBarButton.SmallSize;
#if TOOLBAR_AT_BOTTOM
            Rectangle = new RectangleF(0, 0, 9 * ToolBarButton.SmallSize + ToolBarButton.LargeSize, ToolBarButton.LargeSize);
#else
            Rectangle = new RectangleF(0, 0, ToolBarButton.LargeSize, 9 * ToolBarButton.SmallSize + ToolBarButton.LargeSize);
#endif

            Focusable = false;  // By turning off focusable we prevent the DialogManager from trying to focus widgets
                                // and also prevent it from processing tabs.  The dialogManager normally uses tabs to 
                                // move to the next focusable widget.  In the case of editing though, we want to reserve
                                // tabs for moving the camera to the next actor.
            RenderBaseTile = false;

            // No auto-alignment for this set.  We'll do it manually in Update().
            set = new WidgetSet(this, Rectangle, Orientation.None);
            set.FitToParentDialog = true;
            AddWidget(set);

            siblings = new List<ToolBarButton>();
            RectangleF rect = new RectangleF(0, 0, buttonSize, buttonSize);

            homeButton = new ToolBarButton(this, siblings, rect, @"Textures\ToolMenu\Home", OnHome, tool: EditModeTools.Home, id: "Home");
            set.AddWidget(homeButton);

            playButton = new ToolBarButton(this, siblings, rect, @"Textures\ToolMenu\Play", OnPlay, tool: EditModeTools.Play, id: "Play");
            set.AddWidget(playButton);

            cameraMoveButton = new ToolBarButton(this, siblings, rect, @"Textures\ToolMenu\Hand", OnMoveCamera, tool: EditModeTools.CameraMove, id: "CameraMove");
            set.AddWidget(cameraMoveButton);

            editObjectButton = new ToolBarButton(this, siblings, rect, @"Textures\ToolMenu\ObjectEdit", OnObjectEdit, tool: EditModeTools.EditObject, id: "EditObject");
            set.AddWidget(editObjectButton);

            pathsButton = new ToolBarButton(this, siblings, rect, @"Textures\ToolMenu\Paths", OnPaths, tool: EditModeTools.Paths, id: "Paths");
            set.AddWidget(pathsButton);

            terrainPaintButton = new ToolBarButton(this, siblings, rect, @"Textures\ToolMenu\TerrainMaterial", OnPaintTerrain, tool: EditModeTools.TerrainPaint, id: "TerrainPaint");
            set.AddWidget(terrainPaintButton);

            terrainRaiseLowerButton = new ToolBarButton(this, siblings, rect, @"Textures\ToolMenu\TerrainUpDown", OnHills, tool: EditModeTools.TerrainRaiseLower, id: "TerrainRaiseLower");
            set.AddWidget(terrainRaiseLowerButton);

            waterButton = new ToolBarButton(this, siblings, rect, @"Textures\ToolMenu\TerrainWater", OnWater, tool: EditModeTools.Water, id: "Water");
            set.AddWidget(waterButton);

            eraseObjectsButton = new ToolBarButton(this, siblings, rect, @"Textures\ToolMenu\DeleteObject", OnErase, tool: EditModeTools.EraseObjects, id: "EraseObjects");
            set.AddWidget(eraseObjectsButton);

            worldSettingsButton = new ToolBarButton(this, siblings, rect, @"Textures\ToolMenu\WorldSettings", OnWorldSettings, tool: EditModeTools.WorldSettings, id: "WorldSettings");
            set.AddWidget(worldSettingsButton);

            cameraMoveButton.Selected = true;

            // Note we don't calculate the TabList or the dPad links since we're not using them.

            instance.curSelectedToolButton = cameraMoveButton;
            CurrentEditTool = null;

            // Start with the camera move tool selected.
            cameraMoveButton.Selected = true;
            curSelectedToolButton = cameraMoveButton;
            CurrentEditTool = null;

        }   // end of c'tor

        void OnHome(BaseWidget b)
        {
            SceneManager.SwitchToScene("HomeMenuScene", frameDelay: 1);
            // Refresh the thumbnail during our 1 frame delay.
            InGame.RefreshThumbnail = true;
        }   // end of OnHome()

        void OnPlay(BaseWidget b)
        {
            SceneManager.SwitchToScene("RunSimScene");
            b.Selected = true;
        }   // end of OnPlay()

        void OnMoveCamera(BaseWidget b)
        {
            b.Selected = true;
        }   // end of OnMoveCamera()
        
        void OnObjectEdit(BaseWidget b)
        {
            b.Selected = true;
        }   // end of OnObjectEdit()
        
        void OnPaths(BaseWidget b)
        {
            b.Selected = true;
        }   // end of OnPaths()
        
        void OnPaintTerrain(BaseWidget b)
        {
            b.Selected = true;
        }   // end of OnPaintTerrain()
        
        void OnHills(BaseWidget b)
        {
            b.Selected = true;
        }   // end of OnHills()
        
        void OnWater(BaseWidget b)
        {
            b.Selected = true;
        }   // end of OnWater()
        
        void OnErase(BaseWidget b)
        {
            b.Selected = true;
        }   // end of OnErase()

        void OnWorldSettings(BaseWidget b)
        {
            SceneManager.SwitchToScene("WorldSettingsMenuScene");
            b.Selected = true;
        }   // end of OnWorldSettings()

        public override void Update(SpriteCamera camera)
        {
            // Hold ref to current camera.
            this.camera = camera;

            // If the user has released the space key and we were in a different tool
            // return there.
            if (spaceReturnToolButton != null && !LowLevelKeyboardInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.Space))
            {
                spaceReturnToolButton.Selected = true;
                instance.curSelectedToolButton = spaceReturnToolButton;
                spaceReturnToolButton = null;
            }

#if TOOLBAR_AT_BOTTOM
            // Keep positioned centerd on bottom of screen.
            Vector2 offset = camera.ScreenSize / 2.0f / camera.Zoom;
            rect.Position = new Vector2(-rect.Width / 2.0f, offset.Y - rect.Height - 32);
#else
            // Keep positioned in upper right hand corner in camera space.
            Vector2 offset = camera.ScreenSize / 2.0f / camera.Zoom;
            rect.Position = new Vector2(offset.X - rect.Width, -offset.Y + 32);
#endif

            // Handle sizing for all buttons here.  If the mouse is over one of 
            // the buttons, make it large.  If it's not over any, make the 
            // selected button large.
            ToolBarButton hoveredTool = null;

            CurrentEditTool = FindCurrentEditTool();
            if (CurrentEditTool != null)
            {
                CurrentEditTool.Active = true;
                CurrentEditTool.Update();
            }
            
            // First, figure out our current state.
            foreach (ToolBarButton tbb in siblings)
            {
                if (tbb.Selected)
                {
                    // If we're changing tools and the level is dirty, autosave.
                    if (curSelectedToolButton != tbb && InGame.IsLevelDirty)
                    {
                        // Push current state to undo stack.  (forces autosave)
                        InGame.UnDoStack.Store();
                    }
                    curSelectedToolButton = tbb;
                }
                if (KoiLibrary.InputEventManager.MouseHitObject == tbb)
                {
                    hoveredTool = tbb;
                }
            }

            // Now, apply that info.  Pick the tool to make large.
            ToolBarButton toolButton = curSelectedToolButton;
            if (hoveredTool != null)
            {
                toolButton = hoveredTool;
            }

            // Set size for all tools.
            foreach (ToolBarButton tbb in siblings)
            {
                if (tbb == toolButton)
                {
                    tbb.CurSize = ToolBarButton.LargeSize;
                }
                else
                {
                    tbb.CurSize = ToolBarButton.SmallSize;
                }
            }

            // Finally, based on sizes, set position of all buttons.
#if TOOLBAR_AT_BOTTOM
            float center = set.LocalRect.Center.Y;
            float x = 0;
            foreach (ToolBarButton tbb in siblings)
            {
                tbb.Position = new Vector2(x, center - tbb.CurSize / 2.0f);
                x += tbb.CurSize;
            }
#else
            float center = set.LocalRect.Center.X;
            float y = 0;
            foreach (ToolBarButton tbb in siblings)
            {
                tbb.Position = new Vector2(center - tbb.CurSize / 2.0f, y);
                y += tbb.CurSize;
            }
#endif

            base.Update(camera);
        }   // end of Update()

        public override void Render(SpriteCamera camera)
        {
            base.Render(camera);
        }   // end of Render()

        public override void RegisterForInputEvents()
        {
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);

            base.RegisterForInputEvents();
        }   // end of RegisterForInputEvents()

        public override void Activate(params object[] args)
        {
            // If an edit tool is already selected, leave it.  If it's some
            // other button then default to camera move tool.
            if (curSelectedToolButton == playButton ||
                curSelectedToolButton == homeButton ||
                curSelectedToolButton == worldSettingsButton)
            {
                ResetToCameraMove();
            }          
  
            base.Activate(args);
        }   // end of Activate()

        public override void Deactivate()
        {
            // Deactivate current tool, if any.
            if (CurrentEditTool != null)
            {
                CurrentEditTool.Active = false;
            }

            base.Deactivate();
        }   // end of Deactivate()

        /// <summary>
        /// Force toolbar back to CameraMove tool.
        /// </summary>
        public static void ResetToCameraMove()
        {
            if (instance != null)
            {
                instance.cameraMoveButton.Selected = true;
                instance.curSelectedToolButton = instance.cameraMoveButton;
                instance.CurrentEditTool = null;
            }
        }   // end of ResetToCameraMove()

        #endregion

        #region InputEventHandler

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            if (Active)
            {
                if (KeyboardInputX.WasPressed(Microsoft.Xna.Framework.Input.Keys.Space) && !instance.cameraMoveButton.Selected)
                {
                    spaceReturnToolButton = instance.curSelectedToolButton;
                    ResetToCameraMove();
                }
            }

            return base.ProcessKeyboardEvent(input);
        }   // end of ProcessKeyboardEvent()

        #endregion

        #region Internal

        BaseMouseEditTool FindCurrentEditTool()
        {
            BaseMouseEditTool tool = null;

            needsBrush = false;

            switch (CurTool)
            {
                case EditModeTools.Play:
                    tool = null;
                    break;
                case EditModeTools.Home:
                    tool = null;
                    break;
                case EditModeTools.CameraMove:
                    break;
                case EditModeTools.EditObject:
                    tool = EditObjectsTool.GetInstance();
                    break;
                case EditModeTools.Paths:
                    tool = EditPathsTool.GetInstance();
                    break;
                case EditModeTools.TerrainPaint:
                    tool = PaintTool.GetInstance();
                    needsBrush = true;
                    break;
                case EditModeTools.TerrainRaiseLower:
                    tool = RaiseLowerTool.GetInstance();
                    needsBrush = true;
                    break;
                case EditModeTools.TerrainSpikeyHilly:
                    tool = SpikeyHillyTool.GetInstance();
                    needsBrush = true;
                    break;
                case EditModeTools.TerrainSmoothLevel:
                    tool = SmoothLevelTool.GetInstance();
                    needsBrush = true;
                    break;
                case EditModeTools.Water:
                    tool = WaterTool.GetInstance();
                    break;
                case EditModeTools.EraseObjects:
                    tool = DeleteObjectsTool.GetInstance();
                    needsBrush = true;
                    break;
                case EditModeTools.WorldSettings:
                    break;

            }

            return tool;
        }   // end of FindCurrentEditTool()

        #endregion

    }   // end of class TooBarDialog

}   // end of namespace KoiX.UI.Dialogs
