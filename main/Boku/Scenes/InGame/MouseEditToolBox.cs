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
    public class MouseEditToolBox : INeedsDeviceReset
    {
        #region Members

        private InGame.BaseEditUpdateObj.ToolMode currentMode = InGame.BaseEditUpdateObj.ToolMode.TerrainRaiseLower;
        private BaseMouseEditTool activeTool = null;

        private MaterialPicker materialPicker = null;
        private WaterPicker waterPicker = null;
        private BrushPicker brushPicker = null;

        private PerspectiveUICamera camera = new PerspectiveUICamera();

        private CommandMap commandMap = new CommandMap("MouseEditToolBox"); // Placeholder for stack.

        private bool active = false;

        #endregion

        #region Accessors

        /// <summary>
        /// Determines which is the active tool.
        /// </summary>
        public InGame.BaseEditUpdateObj.ToolMode CurrentMode
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
                    case InGame.BaseEditUpdateObj.ToolMode.EditObject:
                        activeTool = EditObjectsTool.GetInstance();
                        break;
                    case InGame.BaseEditUpdateObj.ToolMode.Paths:
                        activeTool = EditPathsTool.GetInstance();
                        break;
                    case InGame.BaseEditUpdateObj.ToolMode.TerrainPaint:
                        activeTool = PaintTool.GetInstance();
                        break;
                    case InGame.BaseEditUpdateObj.ToolMode.TerrainRaiseLower:
                        activeTool = RaiseLowerTool.GetInstance();
                        break;
                    case InGame.BaseEditUpdateObj.ToolMode.TerrainSpikeyHilly:
                        activeTool = SpikeyHillyTool.GetInstance();
                        break;
                    case InGame.BaseEditUpdateObj.ToolMode.TerrainSmoothLevel:
                        activeTool = SmoothLevelTool.GetInstance();
                        break;
                    case InGame.BaseEditUpdateObj.ToolMode.WaterRaiseLower:
                        activeTool = WaterTool.GetInstance();
                        break;
                    case InGame.BaseEditUpdateObj.ToolMode.DeleteObjects:
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
            get { return materialPicker; }
        }
        /// <summary>
        /// Give tools access to water type selection.
        /// </summary>
        public WaterPicker WaterPicker
        {
            get { return waterPicker; }
        }
        /// <summary>
        /// Give tools access to the BrushPicker.
        /// </summary>
        public BrushPicker BrushPicker
        {
            get { return brushPicker; }
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
        public bool EditngTerrain
        {
            get
            {
                bool result = false;

                if (Active)
                {
                    if (CurrentMode == InGame.BaseEditUpdateObj.ToolMode.TerrainPaint
                        || CurrentMode == InGame.BaseEditUpdateObj.ToolMode.TerrainRaiseLower
                        || CurrentMode == InGame.BaseEditUpdateObj.ToolMode.TerrainSpikeyHilly
                        || CurrentMode == InGame.BaseEditUpdateObj.ToolMode.TerrainSmoothLevel
                        || CurrentMode == InGame.BaseEditUpdateObj.ToolMode.WaterRaiseLower)
                    {
                        result = true;
                    }
                }

                return result;
            }
        }

        #endregion

        // c'tor
        public MouseEditToolBox()
        {
            // Create the tool helpers.
            materialPicker = new MaterialPicker(
                delegate(int index) { Terrain.CurrentMaterialIndex = Terrain.UISlotToMatIndex(index); },
                delegate() { return Terrain.MaterialIndexToUISlot(Terrain.CurrentMaterialIndex); }
                );
            waterPicker = new WaterPicker(
                delegate(int index) { Water.CurrentType = index; },
                delegate() { return Water.CurrentType; }
                );
            brushPicker = new BrushPicker();
        }   // end of MouseEditToolBox c'tor

        /// <summary>
        /// Update for the toolbox and its tools.
        /// </summary>
        /// <param name="hovering">Is the user hovering over the toolbar?  If so, don't update the active tool.</param>
        public void Update(bool hovering)
        {
            if (active)
            {
                if (AuthUI.IsModalActive)
                {
                    return;
                }

                // Keep camera in sync with window size.
                camera.Resolution = new Point((int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);
                camera.Update();

                if (!hovering)
                {
                    // Update the active tool.
                    activeTool.Update(camera);
                }

                // Update the tool add-ons.
                brushPicker.Update(camera);
                materialPicker.Update(camera);
                waterPicker.Update(camera);

            }   // end if active

        }   // end of MouseEditToolBox Update()

        public void Render()
        {
            if (active)
            {
                // Render the tool add-ons.
                materialPicker.Render(camera);
                waterPicker.Render(camera);
                brushPicker.Render(camera);

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
            brushPicker.Hidden = true;
            materialPicker.Hidden = true;
            waterPicker.Hidden = true;
        }


        public void LoadContent(bool immediate)
        {
            BokuGame.Load(brushPicker, immediate);
            BokuGame.Load(materialPicker, immediate);
            BokuGame.Load(waterPicker, immediate);

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
            BokuGame.Unload(brushPicker);
            BokuGame.Unload(materialPicker);
            BokuGame.Unload(waterPicker);

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
            camera.Resolution = new Point(BokuGame.bokuGame.GraphicsDevice.Viewport.Width, BokuGame.bokuGame.GraphicsDevice.Viewport.Height);

            BokuGame.DeviceReset(brushPicker, device);
            BokuGame.DeviceReset(materialPicker, device);
            BokuGame.DeviceReset(waterPicker, device);

            BokuGame.DeviceReset(EditObjectsToolInstance, device);
            BokuGame.DeviceReset(EditPathsToolInstance, device);
        }

    }   // end of class MouseEditToolBox

}   // end of namespace Boku


