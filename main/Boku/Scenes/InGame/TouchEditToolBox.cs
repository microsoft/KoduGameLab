
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
using Boku.Scenes.InGame.MouseEditTools;

namespace Boku
{
    /// <summary>
    /// Container for the available terrain editing tools.
    /// </summary>
    public class TouchEditToolBox : INeedsDeviceReset
    {
        #region Members

        private InGame.TouchEditUpdateObj.ToolMode currentMode = InGame.TouchEditUpdateObj.ToolMode.TerrainRaiseLower;
        private BaseMouseEditTool activeTool = null;

        private PerspectiveUICamera camera = new PerspectiveUICamera();

        private CommandMap commandMap = new CommandMap("TouchEditToolBox"); // Placeholder for stack.

        private bool active = false;

        #endregion

        #region Accessors

        /// <summary>
        /// Determines which is the active tool.
        /// </summary>
        public InGame.TouchEditUpdateObj.ToolMode CurrentMode
        {
            get { return currentMode; }
            set
            {
                if (activeTool != null)
                {
                    activeTool.Active = false;
                }
                currentMode = value;
                switch (currentMode)
                {
                    case InGame.TouchEditUpdateObj.ToolMode.EditObject:
                        activeTool = EditObjectsTool.GetInstance();
                        break;
                    case InGame.TouchEditUpdateObj.ToolMode.Paths:
                        activeTool = EditPathsTool.GetInstance();
                        break;
                    case InGame.TouchEditUpdateObj.ToolMode.TerrainPaint:
                        activeTool = PaintTool.GetInstance();
                        break;
                    case InGame.TouchEditUpdateObj.ToolMode.TerrainRaiseLower:
                        activeTool = RaiseLowerTool.GetInstance();
                        break;
                    case InGame.TouchEditUpdateObj.ToolMode.TerrainSpikeyHilly:
                        activeTool = SpikeyHillyTool.GetInstance();
                        break;
                    case InGame.TouchEditUpdateObj.ToolMode.TerrainSmoothLevel:
                        activeTool = SmoothLevelTool.GetInstance();
                        break;
                    case InGame.TouchEditUpdateObj.ToolMode.WaterRaiseLower:
                        activeTool = WaterTool.GetInstance();
                        break;
                    case InGame.TouchEditUpdateObj.ToolMode.DeleteObjects:
                        activeTool = DeleteObjectsTool.GetInstance();
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }
                if (activeTool != null)
                {
                    activeTool.Active = true;
                }
            }
        }

        public bool Active
        {
            get { return active; }
        }

        public BaseMouseEditTool ActiveTool
        {
            get { return activeTool; }
        }

        public EditObjectsTool EditObjectsToolInstance
        {
            get { return (EditObjectsTool)EditObjectsTool.GetInstance(); }
        }

        public EditPathsTool EditPathsToolInstance
        {
            get { return (EditPathsTool)EditPathsTool.GetInstance(); }
        }

        /// <summary>
        /// Give tools access to the MaterialPicker.
        /// </summary>
        public MaterialPicker MaterialPicker
        {
            get { return InGame.inGame.mouseEditUpdateObj.ToolBox.MaterialPicker; }
        }
        /// <summary>
        /// Give tools access to water type selection.
        /// </summary>
        public WaterPicker WaterPicker
        {
            get { return InGame.inGame.mouseEditUpdateObj.ToolBox.WaterPicker; }
        }
        /// <summary>
        /// Give tools access to the BrushPicker.
        /// </summary>
        public BrushPicker BrushPicker
        {
            get { return InGame.inGame.mouseEditUpdateObj.ToolBox.BrushPicker; }
        }

        /// <summary>
        /// Are any of the pickers active?
        /// </summary>
        public bool PickersActive
        {
            get
            {
                return !BrushPicker.Hidden
                        || !MaterialPicker.Hidden
                        || !WaterPicker.Hidden;
            }
        }

        /// <summary>
        /// Are any menus active?
        /// </summary>
        public bool MenusActive
        {
            get
            {
                return EditObjectsToolInstance.MenusActive || EditPathsToolInstance.MenusActive;
            }
        }

        /// <summary>
        /// Are any of the sliders active?
        /// </summary>
        public bool SlidersActive
        {
            get
            {
                return EditObjectsToolInstance.SliderActive || EditPathsToolInstance.SliderActive;
            }
        }

        public PerspectiveUICamera Camera
        {
            get { return camera; }
        }

        /// <summary>
        /// True if in an editing mode that is changing the height map or materials.
        /// </summary>
        public bool EditingTerrain
        {
            get
            {
                bool result = false;

                if (Active)
                {
                    if (CurrentMode == InGame.TouchEditUpdateObj.ToolMode.TerrainPaint
                        || CurrentMode == InGame.TouchEditUpdateObj.ToolMode.TerrainRaiseLower
                        || CurrentMode == InGame.TouchEditUpdateObj.ToolMode.TerrainSpikeyHilly
                        || CurrentMode == InGame.TouchEditUpdateObj.ToolMode.TerrainSmoothLevel
                        || CurrentMode == InGame.TouchEditUpdateObj.ToolMode.WaterRaiseLower
                        || CurrentMode == InGame.TouchEditUpdateObj.ToolMode.DeleteObjects)
                    {
                        result = true;
                    }
                }

                return result;
            }
        }

        #endregion

        // c'tor
        public TouchEditToolBox()
        {           
        }   // end of MouseEditToolBox c'tor

        /// <summary>
        /// Update for the toolbox and its tools.
        /// </summary>
        /// <param name="hovering">Is the user hovering over the toolbar?  If so, don't update the active tool.</param>
        public void Update(bool hovering)
        {
            if (active)
            {
                if (!hovering)
                {
                    // Update the active tool.
                    activeTool.Update(camera);
                }

                // Update the tool add-ons.
                BrushPicker.Update(camera);
                MaterialPicker.Update(camera);
                WaterPicker.Update(camera);

                // Keeep camera in sync with window size.
                camera.Resolution = new Point((int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);
                camera.Update();

            }   // end if active

        }   // end of MouseEditToolBox Update()

        public bool IsTouchOverMenuButton(TouchContact touch)
        {
            if( null != touch )
            {
                if( !MaterialPicker.Hidden ||
                    !WaterPicker.Hidden ||
                    !BrushPicker.Hidden )
                {
                    return true;
                }
            }

            return false;
        }

        public void Render()
        {
            if (active)
            {
                // Render the tool add-ons.
                MaterialPicker.Render(camera);
                WaterPicker.Render(camera);
                BrushPicker.Render(camera);

                // We need to render this since the slider input device may be active.
                EditObjectsToolInstance.Render(camera);
                EditPathsToolInstance.Render(camera);
            }

        }   // end of MouseEditToolBox Render()


        public void RestartCurrentTool()
        {
            if (activeTool != null)
            {
                activeTool.Starting = true;
            }
        }   // end of MouseEditToolBox RestartCurrentTool()

        public void Activate()
        {
            if (!active)
            {
                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Push(commandMap);
                active = true;

                if (activeTool != null)
                {
                    activeTool.Active = true;
                }
            }
        }

        public void Deactivate()
        {
            if (active)
            {
                if (activeTool != null)
                {
                    activeTool.Active = false;
                }

                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Pop(commandMap);
                active = false;
            }

            // Ensure that all of the pickers have been deactivated.
            BrushPicker.Hidden = true;
            MaterialPicker.Hidden = true;
            WaterPicker.Hidden = true;
        }


        public void LoadContent(bool immediate)
        {

            BokuGame.Load(EditObjectsToolInstance, immediate);
            BokuGame.Load(EditPathsToolInstance, immediate);
        }   // end of MouseEditToolBox LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
            // Ensure that camera matches window dimensions.
            camera.Resolution = new Point((int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);

            EditObjectsToolInstance.InitDeviceResources(device);
            EditPathsToolInstance.InitDeviceResources(device);
        }

        public void UnloadContent()
        {

            BokuGame.Unload(EditObjectsToolInstance);
            BokuGame.Unload(EditPathsToolInstance);
        }   // end of MouseEditToolBox UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
            // Ensure that camera matches window dimensions.
            camera.Resolution = new Point((int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);

            BokuGame.DeviceReset(EditObjectsToolInstance, device);
            BokuGame.DeviceReset(EditPathsToolInstance, device);
        }

    }   // end of class MouseEditToolBox

}   // end of namespace Boku


