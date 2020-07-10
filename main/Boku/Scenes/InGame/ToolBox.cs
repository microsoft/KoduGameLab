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
using Boku.Scenes.InGame.Tools;

namespace Boku
{
    /// <summary>
    /// Container for the available terrain editing tools.
    /// </summary>
    public class ToolBox : INeedsDeviceReset
    {
        #region Members

        private InGame.UpdateMode currentMode = InGame.UpdateMode.TerrainUpDown;
        private BaseTool activeTool = null;

        private MaterialPicker materialPicker = null;
        private WaterPicker waterPicker = null;
        private BrushPicker brushPicker = null;

        private PerspectiveUICamera camera = new PerspectiveUICamera();

        private CommandMap commandMap = new CommandMap("ToolBox"); // Placeholder for stack.

        private bool active = false;

        #endregion

        #region Accessors

        /// <summary>
        /// Determines which is the active tool.
        /// </summary>
        public InGame.UpdateMode CurrentMode
        {
            get { return currentMode; }
            set 
            { 
                if(activeTool != null)
                {
                    activeTool.Active = false;
                }
                currentMode = value; 
                switch(currentMode)
                {
                    case InGame.UpdateMode.TerrainUpDown:
                        activeTool = HeightMapTool.GetInstance();
                        break;
                    case InGame.UpdateMode.TerrainMaterial:
                        activeTool = AddTool.GetInstance();
                        break;
                    case InGame.UpdateMode.TerrainWater:
                        activeTool = WaterAdd.GetInstance();
                        break;
                    case InGame.UpdateMode.TerrainFlatten:
                        activeTool = RoadLevelTool.GetInstance();
                        break;
                    case InGame.UpdateMode.TerrainRoughHill:
                        activeTool = NoiseTool.GetInstance();
                        break;
                    case InGame.UpdateMode.DeleteObjects:
                        activeTool = DelObjTool.GetInstance();
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }
                if(activeTool != null)
                {
                    activeTool.Active = true;
                }
            }
        }

        public bool Active
        {
            get { return active; }
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

        public bool PickersActive
        {
            get 
            { 
                return !BrushPicker.Hidden 
                        || !MaterialPicker.Hidden 
                        || !WaterPicker.Hidden; 
            }
        }

        public PerspectiveUICamera Camera
        {
            get { return camera; }
        }

        #endregion

        // c'tor
        public ToolBox()
        {
            // Create the tool helpers.
            materialPicker = new MaterialPicker(
                delegate(int index) { Terrain.CurrentMaterialIndex = (ushort)Terrain.UISlotToMatIndex(index); },
                delegate() { return Terrain.MaterialIndexToUISlot(Terrain.CurrentMaterialIndex); }
                );
            waterPicker = new WaterPicker(
                delegate(int index) { Water.CurrentType = index; },
                delegate() { return Water.CurrentType; }
                );
            brushPicker = new BrushPicker();
        }   // end of ToolBox c'tor

        public void Update()
        {
            if(active)
            {
                // Update the active tool.
                activeTool.Update();

                // Update the tool add-ons.
                brushPicker.Update(camera);
                materialPicker.Update(camera);
                waterPicker.Update(camera);

                // Keep camera in sync with window size.
                camera.Resolution = new Point((int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);
                camera.Update();

            }   // end if active

        }   // end of ToolBox Update()

        public void Render()
        {
            if (active)
            {
                // Render the tool add-ons.
                materialPicker.Render(camera);
                waterPicker.Render(camera);
                brushPicker.Render(camera);
            }

        }   // end of ToolBox Render()


        public void RestartCurrentTool()
        {
            if (activeTool != null)
            {
                activeTool.Starting = true;
            }
        }   // end of ToolBox RestartCurrentTool()

        public void Activate()
        {
            if (!active)
            {
                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Push(commandMap);
                HelpOverlay.Push(@"ToolBox");
                active = true;
                if(activeTool != null)
                {
                    activeTool.Active = true;
                }
            }
        }

        public void Deactivate()
        {
            if (active)
            {
                if(activeTool != null)
                {
                    activeTool.Active = false;
                }

                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Pop(commandMap);
                HelpOverlay.Pop();
                active = false;
            }
        }


        public void LoadContent(bool immediate)
        {
            BokuGame.Load(brushPicker, immediate);
            BokuGame.Load(materialPicker, immediate);
            BokuGame.Load(waterPicker, immediate);
        }   // end of ToolBox LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
            // Ensure that camera matches window dimensions.
            camera.Resolution = new Point((int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);
        }

        public void UnloadContent()
        {
            BokuGame.Unload(brushPicker);
            BokuGame.Unload(materialPicker);
            BokuGame.Unload(waterPicker);
        }   // end of ToolBox UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
            BokuGame.DeviceReset(brushPicker, device);
            BokuGame.DeviceReset(materialPicker, device);
            BokuGame.DeviceReset(waterPicker, device);
        }

    }   // end of class ToolBox

}   // end of namespace Boku


