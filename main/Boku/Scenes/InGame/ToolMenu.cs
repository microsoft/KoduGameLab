
//#define MF_HOSE_TESTS

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

#if MF_HOSE_TESTS
using System.Xml;
using System.Xml.Serialization;
#endif // MF_HOSE_TESTS

using Boku.Base;
using Boku.Common;
using Boku.Common.Xml;
using Boku.Fx;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.SimWorld;
using Boku.SimWorld.Terra;
using Boku.Scenes.InGame.Tools;

namespace Boku
{
    /// <summary>
    /// Selector for game editing tools.  Internally this just presents 
    /// a grid with the tool icons on it and then switches InGame modes 
    /// depending on the user's choice.
    /// </summary>
    public class ToolMenu : INeedsDeviceReset
    {
        #region Members

        private Camera camera = new PerspectiveUICamera();

        private ToolMenuUIGrid grid = null;
        private int numTools = 0;       // Count of tools in the list.

        private Texture2D dropShadowTexture = null;

        private Matrix worldMatrix = Matrix.Identity;

        private CommandMap commandMap = new CommandMap("ToolMenu"); // Placeholder for stack.

        private bool active = false;

        private double kPreFadeTime = 2.0;      // # seconds to wait on inaction before fading in trigger button icons.
        private double kFadeTime = 0.5;         // # second to fade up.
        private double lastChangedTime = 0;     // Time when user last changed something.
        private int curIndex = -1;              // Let's us detect when the selection state has changed.

        #endregion

        #region Accessors

        public bool Active
        {
            get { return active; }
        }
        
        #endregion

        #region Public

        // c'tor
        public ToolMenu()
        {
            // Create elements for the grid.
            // Start with a blob of common parameters.
            UIGridElement.ParamBlob blob = new UIGridElement.ParamBlob();
            blob.width = 1.1f;
            blob.height = 1.1f;
            blob.edgeSize = 0.1f;
            Color tileColor = new Color(110, 150, 140, 170);
            blob.selectedColor = tileColor;
            blob.unselectedColor = tileColor;
            blob.textColor = Color.Red; // Shouldn't need this.
            blob.dropShadowColor = Color.Black;
            blob.normalMapName = @"QuarterRound4NormalMap";
            blob.altShader = true;
            blob.ignorePowerOf2 = true;

            // Create and populate grid.
            int maxTools = 20;
            numTools = 0;
            grid = new ToolMenuUIGrid(OnSelect, OnCancel, new Point(maxTools, 1), "ToolMenuGrid");

            UIGrid2DTextureElement e = null;

            // JW - The toolmenu elements are now assigned names so that they can be disabled by the
            // same strings as the Mouse/Touch "ToolBar" elements.
            blob.elementName = Strings.Localize("toolBar.runGame");
            e = new UIGrid2DTextureElement(blob, @"ToolMenu\Play");
            e.Tag = InGame.UpdateMode.RunSim;
            grid.Add(e, numTools++, 0);

            blob.elementName = Strings.Localize("toolBar.home");
            e = new UIGrid2DTextureElement(blob, @"ToolMenu\Home");
            e.Tag = InGame.UpdateMode.MiniHub;
            grid.Add(e, numTools++, 0);

            blob.elementName = Strings.Localize("toolBar.objectEdit");
            e = new UIGrid2DTextureElement(blob, @"ToolMenu\ObjectEdit");
            e.Tag = InGame.UpdateMode.EditObject;
            grid.Add(e, numTools++, 0);

            blob.elementName = Strings.Localize("toolBar.terrainPaint");
            e = new UIGrid2DTextureElement(blob, @"ToolMenu\TerrainMaterial");
            e.Tag = InGame.UpdateMode.TerrainMaterial;
            grid.Add(e, numTools++, 0);

            blob.elementName = Strings.Localize("toolBar.terrainRaiseLower");
            e = new UIGrid2DTextureElement(blob, @"ToolMenu\TerrainUpDown");
            e.Tag = InGame.UpdateMode.TerrainUpDown;
            grid.Add(e, numTools++, 0);

            blob.elementName = Strings.Localize("toolBar.terrainSmoothLevel");
            e = new UIGrid2DTextureElement(blob, @"ToolMenu\TerrainFlatten");
            e.Tag = InGame.UpdateMode.TerrainFlatten;
            grid.Add(e, numTools++, 0);

            blob.elementName = Strings.Localize("toolBar.terrainSpikeyHilly");
            e = new UIGrid2DTextureElement(blob, @"ToolMenu\TerrainRoughHill");
            e.Tag = InGame.UpdateMode.TerrainRoughHill;
            grid.Add(e, numTools++, 0);

            blob.elementName = Strings.Localize("toolBar.waterRaiseLower");
            e = new UIGrid2DTextureElement(blob, @"ToolMenu\TerrainWater");
            e.Tag = InGame.UpdateMode.TerrainWater;
            grid.Add(e, numTools++, 0);

            blob.elementName = Strings.Localize("toolBar.deleteObjects");
            e = new UIGrid2DTextureElement(blob, @"ToolMenu\DeleteObject");
            e.Tag = InGame.UpdateMode.DeleteObjects;
            grid.Add(e, numTools++, 0);

            //e = new UIGrid2DTextureElement(blob, @"ToolMenu\ObjectTweak");
            //e.Object = InGame.UpdateMode.TweakObject;
            //grid.Add(e, numTools++, 0);

            blob.elementName = Strings.Localize("toolBar.worldTweak");
            e = new UIGrid2DTextureElement(blob, @"ToolMenu\WorldSettings");
            e.Tag = InGame.UpdateMode.EditWorldParameters;
            grid.Add(e, numTools++, 0);

            Debug.Assert(numTools <= maxTools, "If this fires, just up maxTools.");

            // Set grid properties.
            grid.Spacing = new Vector2(0.1f, 0.0f);
            grid.Scrolling = false;
            grid.Wrap = false;
            grid.UseLeftStick = false;
            grid.UseTriggers = true;

            Matrix mat = Matrix.Identity;
            mat.Translation = new Vector3(0.0f, -2.2f, -10.0f);
            grid.LocalMatrix = mat;

        }   // end of ToolMenu c'tor

        public void Update()
        {
            if (active)
            {
                // If we're modal, allow the left stick to control the grid.
                if (XmlOptionsData.ModalToolMenu)
                {
                    grid.UseLeftStick = true;
                }
                else
                {
                    grid.UseLeftStick = false;
                }

                Matrix mat = Matrix.Identity;
                mat.Translation = new Vector3(0.0f, -2.2f, -10.0f);
                grid.LocalMatrix = mat;

                grid.Update(ref worldMatrix);

                GamePadInput pad = GamePadInput.GetGamePad0();

                if (pad.Back.WasPressed)
                {
                    Deactivate();
                    InGame.inGame.CurrentUpdateMode = InGame.UpdateMode.RunSim;
                    return;
                }

                if (pad.Start.WasPressed)
                {
                    Deactivate();
                    InGame.inGame.SwitchToMiniHub();
                    return;
                }

#if MF_HOSE_TESTS
                TestXml2();
#endif // MF_HOSE_TESTS

                // Undo - Redo
                if (Actions.Redo.WasPressed)
                {
                    Actions.Redo.ClearAllWasPressedState();

                    InGame.UnDoStack.ReDo();
                }
                if (Actions.Undo.WasPressed)
                {
                    Actions.Undo.ClearAllWasPressedState();

                    InGame.UnDoStack.UnDo();
                }

                if (curIndex != grid.SelectionIndex.X)
                {
                    curIndex = grid.SelectionIndex.X;
                    lastChangedTime = Time.WallClockTotalSeconds;
                }

                // Make sure camera has correct resolution.
                camera.Resolution = new Point((int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);
                camera.Update();

            }   // end if active

        }   // end of ToolMenu Update()

#if MF_HOSE_TESTS
        private void TestUndo()
        {
            GamePadInput pad = GamePadInput.GetGamePad0();
            if (pad.ButtonY.WasPressed)
            {
                doTest = 0;
                pad.ButtonY.ClearAllWasPressedState();
            }
            if (doTest >= 0)
            {
                {
                    XmlGameActor testXml = XmlGameActor.Deserialize("BalloonBot");
                    if (testXml == null)
                    {
                        doTest = 0;
                    }
                }

                bool doUndo = BokuGame.bokuGame.rnd.NextDouble() > 0.5f;
                if (doUndo)
                {
                    InGame.UnDoStack.UnDo();
                }
                else
                {
                    InGame.UnDoStack.ReDo();
                }
                {
                    XmlGameActor testXml = XmlGameActor.Deserialize("BalloonBot");
                    if (testXml == null)
                    {
                        doTest = 0;
                    }
                }
            }
        }

        private void TestXml()
        {
            GamePadInput pad = GamePadInput.GetGamePad0();
            if (pad.ButtonY.WasPressed)
            {
                doTest = 0;
            }
            if (doTest >= 0)
            {
                if (!Storage4.FileExists(BokuGame.Settings.MediaPath + @"Xml\OptionsData.Xml"))
                {
                    doTest = 0;
                }
                XmlOptionsData.UIVolume = XmlOptionsData.UIVolume;
                if (!Storage4.FileExists(BokuGame.Settings.MediaPath + @"Xml\OptionsData.Xml"))
                {
                    doTest = 0;
                }
                ++testsDone;
            }
        }

        public class XmlHoseItem
        {
            public int item = 0;

            public XmlHoseItem(int x)
            {
                item = x;
            }
        }
        public class XmlHose
        {
            public bool hoser = false;

            public List<XmlHoseItem> list = new List<XmlHoseItem>();

            public XmlHose()
            {
                list.Add(new XmlHoseItem(1));
                list.Add(new XmlHoseItem(2));
            }

        }
        private void TestXml2()
        {
            GamePadInput pad = GamePadInput.GetGamePad0();
            if (pad.ButtonY.WasPressed)
            {
                doTest = 0;
            }
            if (doTest >= 0)
            {
                if (!Storage4.FileExists(BokuGame.Settings.MediaPath + @"Xml\OptionsData.Xml"))
                {
                    doTest = 0; /// This breakpoint isn't ever hit, OptionsData.xml exists here
                }
                Stream hoseStream = Storage4.OpenWrite(@"Xml\Hoser.xml");

                XmlHose hoser = new XmlHose();
                hoser.hoser = true;

                XmlSerializer serializer = new XmlSerializer(typeof(XmlHose));
                serializer.Serialize(hoseStream, hoser);

                Storage4.Close(hoseStream);
                if (!Storage4.FileExists(BokuGame.Settings.MediaPath + @"Xml\OptionsData.Xml"))
                {
                    doTest = 0; /// THis breakpoint is hit on failure, after the flush
                                /// OptionsData.xml is not longer there.
                }
                ++testsDone;
            }
        }

        private void TestStorage()
        {
            GamePadInput pad = GamePadInput.GetGamePad0();
            if (pad.ButtonY.WasPressed)
            {
                doTest = 0;
            }
            if (doTest >= 0)
            {
                string fileName = "Test0.dat";
                if (!Storage4.FileExists(fileName))
                {
                    doTest = 0;
                }

                Stream writeStream = Storage4.OpenWrite(fileName);
                if (writeStream == null)
                {
                    doTest = 0;
                }
                byte[] writeBytes = new byte[10000];
                writeStream.Write(writeBytes, 0, writeBytes.Length);
                Storage4.Close(writeStream);

                if (!Storage4.FileExists(fileName))
                {
                    doTest = 0;
                }

                ++testsDone;
            }
        }
        static int doTest = -1;
        static int testsDone = 0;
#endif // MF_HOSE_TESTS

        public void Render()
        {
            if (active)
            {
                // Render menu using local camera.
                Fx.ShaderGlobals.SetCamera(camera);

                // Darken the background to emphasize to the user that they need to pick a tool.
                ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();
                ssquad.Render(dropShadowTexture, new Vector4(0, 0, 0, 0.9f), new Vector2(0, 0.6f * BokuGame.ScreenSize.Y), new Vector2(BokuGame.ScreenSize.X, 0.4f * BokuGame.ScreenSize.Y), "TexturedRegularAlpha");

                grid.Render(camera);

                // Render reticule around selected tools tile.
                CameraSpaceQuad csquad = CameraSpaceQuad.GetInstance();
                UIGrid2DTextureElement e = (UIGrid2DTextureElement)grid.SelectionElement;
                Vector2 position = new Vector2(e.Position.X, e.Position.Y);
                position.X += grid.WorldMatrix.Translation.X;
                position.Y += grid.WorldMatrix.Translation.Y;
                position.Y -= 0.14f;    // No clue.  Nedd to figure this out.
                Vector2 size = 2.0f * new Vector2(e.Size.X, e.Size.Y);
                float alpha = 1.0f;
                csquad.Render(camera, BasePicker.reticuleTexture, alpha, position, size, @"AdditiveBlend");

                // Don't bother with trigger icons if we're modal.
                if (!XmlOptionsData.ModalToolMenu)
                {
                    // Trigger icons?
                    double curTime = Time.WallClockTotalSeconds;
                    double dTime = curTime - lastChangedTime;
                    if (dTime > kPreFadeTime)
                    {
                        dTime -= kPreFadeTime;

                        float triggerAlpha = Math.Min((float)(dTime / kFadeTime), 1.0f);
                        Vector2 offset = size * 0.4f;
                        size *= 0.4f;
                        // Note the 12/64 in the positioning accounts for the fact that the 
                        // button textures only use the upper 40x40 out of the 64x64 space they allocate.
                        // The 12 is actually (64-40)/2.
                        csquad.Render(camera, ButtonTextures.RightTrigger, triggerAlpha, position + offset + size * 12.0f / 64.0f, size, @"TexturedRegularAlpha");
                        offset.X = -offset.X;
                        csquad.Render(camera, ButtonTextures.LeftTrigger, triggerAlpha, position + offset + size * 12.0f / 64.0f, size, @"TexturedRegularAlpha");
                    }
                }

            }

        }   // end of ToolMenu Render()

        #endregion

        #region Internal

        public void OnSelect(UIGrid grid)
        {
            Deactivate();

            // Need to special case MiniHub since it's not a normal "mode" of InGame.
            if ((InGame.UpdateMode)grid.SelectionElement.Tag == InGame.UpdateMode.MiniHub)
            {
                InGame.inGame.SwitchToMiniHub();
                return;
            }

            // Transition to tool the user chose.
            InGame.inGame.CurrentUpdateMode = (InGame.UpdateMode)grid.SelectionElement.Tag;

            GamePadInput.IgnoreUntilReleased(Buttons.A);

        }   // end of OnSelect

        public void OnCancel(UIGrid grid)
        {
            Deactivate();

            // Transition to RunSim.
            InGame.inGame.CurrentUpdateMode = InGame.UpdateMode.RunSim;

        }   // end of OnCancel()

        public void Activate()
        {
            if (!active)
            {
                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Push(commandMap);
                HelpOverlay.Push(@"ToolMenu");
                active = true;
                grid.Active = true;
                grid.RenderWhenInactive = false;

                // Never allow the Play button to be the default.
                if (grid.SelectionIndex.X == 0)
                {
                    // Default to object edit.
                    grid.SelectionIndex = new Point(2, 0);
                }

                // Reset all the tiles to the origin so they spring out nicely.
                for (int i = 0; i < grid.ActualDimensions.X; i++)
                {
                    UIGrid2DTextureElement e = (UIGrid2DTextureElement)grid.Get(i, 0);
                    e.Position = Vector3.Zero;
                }
                grid.Dirty = true;
                // Force the grid to update the positions before getting rendered.
                grid.Update(ref worldMatrix);

                lastChangedTime = Time.WallClockTotalSeconds;
                curIndex = grid.SelectionIndex.X;

                InGame.inGame.StopAllSounds();
            }
        }

        public void Deactivate()
        {
            if (active)
            {
                grid.Active = false;

                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Pop(commandMap);
                HelpOverlay.Pop();

                InGame.inGame.RenderWorldAsThumbnail = false;

                active = false;
            }
        }


        public void LoadContent(bool immediate)
        {
            BokuGame.Load(grid, immediate);

            if (dropShadowTexture == null)
            {
                dropShadowTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\ToolMEnu\DropShadow");
            }

        }   // end of ToolMenu LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
            // Ensure that camera matches window dimensions.
            camera.Resolution = new Point((int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);
        }

        public void UnloadContent()
        {
            BokuGame.Unload(grid);

            BokuGame.Release(ref dropShadowTexture);

        }   // end of ToolMenu UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
            BokuGame.DeviceReset(grid, device);
        }

        /// <summary>
        /// Sets the visibility of an element in the grid by element name
        /// </summary>
        /// <param name="name">string name for the element sought</param>
        /// <param name="isVisible">visible setting</param>
        public bool SetVisible(string name, bool isVisible)
        {
            bool result = grid.SetVisible(name, isVisible);
            if (result)
            {
                grid.Dirty = true;
            }
            return result;
        }

        /// <summary>
        /// Sets the visibility of all tools
        /// </summary>
        public void SetAllToolsVisible(bool isVisible)
        {
            grid.SetAllVisible(isVisible);
            grid.Dirty = true;
        }

        #endregion

    }   // end of class ToolMenu

}   // end of namespace Boku


