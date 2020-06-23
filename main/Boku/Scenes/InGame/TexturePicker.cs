
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


namespace Boku
{
    /// <summary>
    /// Menus of available textures.  Rendered live over the top of the current
    /// world.  We should be able to have this change the textures on the fly.
    /// </summary>
    public class TexturePicker : INeedsDeviceReset
    {
        /// <summary>
        /// Holder of a filename and a scaling factor for each texture.
        /// </summary>
        public class TextureFile
        {
            public string filename = null;
            public string sidename = null;
            public float scale = 1.0f;
            public float gloss = 0.0f;
            public float bumpAmplitude = 0.5f;

            // c'tor
            public TextureFile(string filename, float scale, float gloss, float bumpAmplitude)
            {
                this.filename = filename;
                this.sidename = filename;
                this.scale = scale;
                this.gloss = gloss;
                this.bumpAmplitude = bumpAmplitude;
            }   // end of TextureFile c'tor
            public TextureFile(string filename, string sidename, float scale, float gloss, float bumpAmplitude)
            {
                this.filename = filename;
                this.sidename = sidename;
                this.scale = scale;
                this.gloss = gloss;
                this.bumpAmplitude = bumpAmplitude;
            }   // end of TextureFile c'tor
        }

        private Camera camera = new PerspectiveUICamera();
        private Matrix worldGrid = Matrix.Identity;

        private UIGrid grid0 = null;
        private UIGrid grid1 = null;
        private UIGrid grid2 = null;
        private UIGrid grid3 = null;

        // Saved away values in case the user cancels.
        private int oldIndex0 = 0;
        private int oldIndex1 = 0;
        private int oldIndex2 = 0;
        private int oldIndex3 = 0;

        private int curFocusIndex0 = 0; // The index of the texture in focus
        private int curFocusIndex1 = 0; // The index of the texture in focus
        private int curFocusIndex2 = 0; // The index of the texture in focus
        private int curFocusIndex3 = 0; // The index of the texture in focus

        private UIGrid2DTextureElement backPlate = null;
        private UIGrid2DTextureElement selectionPlate = null;

        private Color backPlateColor = Color.WhiteSmoke;
        private Color selectionPlateColor = Color.Red;

        private CommandMap commandMap = new CommandMap("Sim.Edit.TexturePicker");   // Placeholder for stack.

        private bool active = false;
        private static List<TextureFile> files = null;

        private int activeGrid = 1;

        #region Accessors
        public bool Active
        {
            get { return active; }
        }
        /// <summary>
        /// Look up the TextureFile by name. If performance ever became
        /// an issue, we could do a sort on initialization of the FileList,
        /// and then a binary find. Also, caching the last one looked up
        /// and checking it before doing a search would pay off, because it
        /// looks like we lookup the same record repeatedly before moving on
        /// to another record.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static TextureFile GetTextureFile(String name)
        {
            if (files == null)
            {
                InitFileList();
            }

            // Remove any path from the name.
            name = name.Substring(name.LastIndexOf(@"\") + 1);

            for (int i = 0; i < files.Count; i++)
            {
                if (files[i].filename.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                {
                    return files[i];
                }
            }

            return files[0];

            // Shouldn't happen.
            //Debug.Assert(false);
            //return null;
        }
        /// <summary>
        /// Returns the proper scale for a texture.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static float GetTextureScale(String name)
        {
            return GetTextureFile(name).scale;
        }
        /// <summary>
        /// Returns the proper gloss for a texture.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static float GetTextureGloss(String name)
        {
            return GetTextureFile(name).gloss;
        }
        /// <summary>
        /// Returns the proper bump amplitude for a texture.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static float GetTextureBumpAmplitude(String name)
        {
            return GetTextureFile(name).bumpAmplitude;
        }
        #endregion

        // c'tor
        public TexturePicker()
        {
            InitFileList();

            // Create the back plate.
            backPlate = new UIGrid2DTextureElement(7.0f, 1.25f, 0.625f, @"QuarterRound4NormalMap", backPlateColor, null);
            backPlate.Position = new Vector3(0.0f, 0.0f, 0.0f);
            backPlate.NoZ = true;

            // Create the selection plate.
            selectionPlate = new UIGrid2DTextureElement(1.125f, 1.125f, 0.5625f, @"QuarterRound4NormalMap", selectionPlateColor, null);
            selectionPlate.NoZ = true;
            selectionPlate.Position = new Vector3(-2.25f, 0.0f, 0.0f);

            // Create texture elements for the grids.
            // Start with a blob of common parameters.
            UIGridElement.ParamBlob blob = new UIGridElement.ParamBlob();
            blob.width = 1.0f;
            blob.height = 1.0f;
            blob.edgeSize = 0.5f;
            blob.selectedColor = Color.White;
            blob.unselectedColor = Color.LightGray;
            blob.normalMapName = @"QuarterRound4NormalMap";

            // Create and populate grid.
            int numElements = files.Count;
            grid0 = new UIGrid(OnSelect, OnCancel, new Point(1, numElements), "TerrainEdit.TextureGrid0");
            grid1 = new UIGrid(OnSelect, OnCancel, new Point(1, numElements), "TerrainEdit.TextureGrid1");
            grid2 = new UIGrid(OnSelect, OnCancel, new Point(1, numElements), "TerrainEdit.TextureGrid2");
            grid3 = new UIGrid(OnSelect, OnCancel, new Point(1, numElements), "TerrainEdit.TextureGrid3");
            for (int i = 0; i < numElements; i++)
            {
                // Note, we could probably have the grid share elements except that the element positions
                // are set during Update() so unless we interleaved Update() and Render() calls we would
                // end up seeing everything rendered in the wrong place.
                TextureFile file = (TextureFile)files[i];
                UIGrid2DTextureElement e = new UIGrid2DTextureElement(blob, @"Terrain\GroundTextures\" + file.filename);
                e.NoZ = true;
                grid0.Add(e, 0, i);

                file = (TextureFile)files[i];
                e = new UIGrid2DTextureElement(blob, @"Terrain\GroundTextures\" + file.filename);
                e.NoZ = true;
                grid1.Add(e, 0, i);

                file = (TextureFile)files[i];
                e = new UIGrid2DTextureElement(blob, @"Terrain\GroundTextures\" + file.filename);
                e.NoZ = true;
                grid2.Add(e, 0, i);

                file = (TextureFile)files[i];
                e = new UIGrid2DTextureElement(blob, @"Terrain\GroundTextures\" + file.filename);
                e.NoZ = true;
                grid3.Add(e, 0, i);
            }

            // Set grid properties.
            grid0.Spacing = new Vector2(0.0f, 0.25f);     // The first number doesn't really matter since we're doing a 1d column.
            grid0.Scrolling = true;
            grid0.Wrap = false;
            grid0.LocalMatrix = Matrix.CreateTranslation(new Vector3(-2.25f, 0.0f, 0.0f));

            grid1.Spacing = new Vector2(0.0f, 0.25f);     // The first number doesn't really matter since we're doing a 1d column.
            grid1.Scrolling = true;
            grid1.Wrap = false;
            grid1.LocalMatrix = Matrix.CreateTranslation(new Vector3(-0.75f, 0.0f, 0.0f));

            grid2.Spacing = new Vector2(0.0f, 0.25f);     // The first number doesn't really matter since we're doing a 1d column.
            grid2.Scrolling = true;
            grid2.Wrap = false;
            grid2.LocalMatrix = Matrix.CreateTranslation(new Vector3(0.75f, 0.0f, 0.0f));

            grid3.Spacing = new Vector2(0.0f, 0.25f);     // The first number doesn't really matter since we're doing a 1d column.
            grid3.Scrolling = true;
            grid3.Wrap = false;
            grid3.LocalMatrix = Matrix.CreateTranslation(new Vector3(2.25f, 0.0f, 0.0f));

        }   // end of TexturePicker c'tor


        private static void InitFileList()
        {
            if (files == null)
            {
                files = new List<TextureFile>();

                                        // name, scale, gloss, bumpAmplitude
                files.Add(new TextureFile(@"alum_plt", 0.02f, 0.5f, 0.5f));
                files.Add(new TextureFile(@"block1", 0.05f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"Concrete01", 0.1f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"Concrete02", 0.06f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"Concrete03", 0.04f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"dirt_crackeddrysoft_df_", 0.2f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"dirt_earth-n-moss_df_", 0.7f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"dirt_grayrocky-mossy_df_", 0.4f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"dirt_grayrocky_df_", 0.4f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"Terrazzo.Black.1", 0.1f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"StuccoWhite", 0.1f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"StuccoYellow", 0.1f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"FREESAMPLES_22", 0.1f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"FREESAMPLES_23", 0.1f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"FREESAMPLES_28", 0.1f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"FREESAMPLES_38", 0.2f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"FREESAMPLES_49", 0.05f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"FREESAMPLES_52", 0.10f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"FREESAMPLES_53", 0.10f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"FREESAMPLES_78", 0.10f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"VelvetBlack", 0.1f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"Grass", @"dirt_crackeddrysoft_df_", 0.5f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"GRASS_004", @"dirt_crackeddrysoft_df_", 0.2f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"growth_weirdfungus-01_df_", 0.1f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"growth_weirdfungus-02_df_", 0.1f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"holes", 0.1f, 0.5f, 0.5f));
                files.Add(new TextureFile(@"RIVROCK1", 0.1f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"rock2", 0.1f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"Sand", 0.1f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"Asphalt1", 0.1f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"Sitework.Planting.Grass.1", 0.05f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"Sitework.Planting.Grass.Short", 0.05f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"Sitework.Planting.Gravel.Mixed", 0.1f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"Snow", 0.5f, 0.2f, 0.5f));
                files.Add(new TextureFile(@"steelplt", 0.02f, 0.5f, 0.5f));
                files.Add(new TextureFile(@"Stone", 0.4f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"stone_mutedorangeblue_df_", 0.4f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"stone_orangefishfood_df_", 0.5f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"stone_orangehardrough_df_", 0.4f, 0.0f, 0.5f));
                files.Add(new TextureFile(@"simple_white", .1f, 1.0f, 0.0f));
                files.Add(new TextureFile(@"simple_green", .1f, .25f, 0.0f));
                files.Add(new TextureFile(@"simple_sand", .1f, .1f, 0.0f));
                files.Add(new TextureFile(@"simple_clay", .1f, .7f, 0.0f));
            }
        }   // end of TexturePicker InitFileList()


        public void Update()
        {
            if (active)
            {
                GamePadInput pad = GamePadInput.GetGamePad1();

                // Switch active grid?
                bool changed = false;
                if (pad.LeftStickLeft.WasPressed || pad.DPadLeft.WasPressed)
                {
                    activeGrid = (activeGrid + 3) % 4;
                    changed = true;
                    Foley.PlayProgrammingClick();
                }
                if (pad.LeftStickRight.WasPressed || pad.DPadRight.WasPressed)
                {
                    activeGrid = (activeGrid + 1) % 4;
                    changed = true;
                    Foley.PlayProgrammingClick();
                }

                if (changed)
                {
                    Vector3 newPosition = new Vector3(-2.25f + 1.5f * activeGrid, 0.0f, 0.0f);
                    TwitchManager.GetVector3 get = delegate(Object param) { return selectionPlate.Position; };
                    TwitchManager.SetVector3 set = delegate(Vector3 value, Object param) { selectionPlate.Position = value; };
                    TwitchManager.Vector3Twitch twitch = new TwitchManager.Vector3Twitch(get, set, newPosition, 0.25f, TwitchCurve.Shape.OvershootOut);
                    twitch.Start();

                    grid0.Active = false;
                    grid1.Active = false;
                    grid2.Active = false;
                    grid3.Active = false;

                    switch (activeGrid)
                    {
                        case 0: grid0.Active = true; break;
                        case 1: grid1.Active = true; break;
                        case 2: grid2.Active = true; break;
                        case 3: grid3.Active = true; break;
                    }

                    changed = false;
                }

                backPlate.Update();
                selectionPlate.Update();
                grid0.Update(ref worldGrid);
                grid1.Update(ref worldGrid);
                grid2.Update(ref worldGrid);
                grid3.Update(ref worldGrid);

                // See if any of the grids have changed which texture is in focus.
                // If so, change what the terrain sees.
                if (grid0.SelectionIndex.Y != curFocusIndex0)
                {
                    curFocusIndex0 = grid0.SelectionIndex.Y;
                    InGame.inGame.Terrain.ChangeTerrainTexture(0, files[curFocusIndex0]);
                    InGame.inGame.IsLevelDirty = true;
                }
                if (grid1.SelectionIndex.Y != curFocusIndex1)
                {
                    curFocusIndex1 = grid1.SelectionIndex.Y;
                    InGame.inGame.Terrain.ChangeTerrainTexture(1, files[curFocusIndex1]);
                    InGame.inGame.IsLevelDirty = true;
                }
                if (grid2.SelectionIndex.Y != curFocusIndex2)
                {
                    curFocusIndex2 = grid2.SelectionIndex.Y;
                    InGame.inGame.Terrain.ChangeTerrainTexture(2, files[curFocusIndex2]);
                    InGame.inGame.IsLevelDirty = true;
                }
                if (grid3.SelectionIndex.Y != curFocusIndex3)
                {
                    curFocusIndex3 = grid3.SelectionIndex.Y;
                    InGame.inGame.Terrain.ChangeTerrainTexture(3, files[curFocusIndex3]);
                    InGame.inGame.IsLevelDirty = true;
                }

                float outerRowAlpha = 0.3f;
                float innerRowAlpha = 0.7f;

                // Update the alpha values of the visible tiles.
                for (int i = 0; i < grid0.ActualDimensions.Y; i++)
                {
                    UIGrid2DTextureElement e = null;

                    e = grid0.Get(0, i) as UIGrid2DTextureElement;
                    if (e != null)
                    {
                        int delta = Math.Abs(i - grid0.SelectionIndex.Y);
                        if (delta < 2)
                        {
                            e.Alpha = 1.0f;
                        }
                        else if (delta < 3)
                        {
                            e.Alpha = innerRowAlpha;
                        }
                        else
                        {
                            e.Alpha = outerRowAlpha;
                        }
                    }

                    e = grid1.Get(0, i) as UIGrid2DTextureElement;
                    if (e != null)
                    {
                        int delta = Math.Abs(i - grid1.SelectionIndex.Y);
                        if (delta < 2)
                        {
                            e.Alpha = 1.0f;
                        }
                        else if (delta < 3)
                        {
                            e.Alpha = innerRowAlpha;
                        }
                        else
                        {
                            e.Alpha = outerRowAlpha;
                        }
                    }
                    e = grid2.Get(0, i) as UIGrid2DTextureElement;
                    if (e != null)
                    {
                        int delta = Math.Abs(i - grid2.SelectionIndex.Y);
                        if (delta < 2)
                        {
                            e.Alpha = 1.0f;
                        }
                        else if (delta < 3)
                        {
                            e.Alpha = innerRowAlpha;
                        }
                        else
                        {
                            e.Alpha = outerRowAlpha;
                        }
                    }
                    e = grid3.Get(0, i) as UIGrid2DTextureElement;
                    if (e != null)
                    {
                        int delta = Math.Abs(i - grid3.SelectionIndex.Y);
                        if (delta < 2)
                        {
                            e.Alpha = 1.0f;
                        }
                        else if (delta < 3)
                        {
                            e.Alpha = innerRowAlpha;
                        }
                        else
                        {
                            e.Alpha = outerRowAlpha;
                        }
                    }

                }
            }
        }   // end of TexturePicker Update()

        public void Render()
        {
            if (active)
            {
                //InGame.Clear(Color.Pink);

                // Render menu using local camera.
                BokuGame.bokuGame.shaderGlobals.Effect.Parameters["EyeLocation"].SetValue(new Vector4(camera.From, 1.0f));
                BokuGame.bokuGame.shaderGlobals.Effect.Parameters["CameraUp"].SetValue(new Vector4(camera.Up, 1.0f));

                backPlate.Render(camera);
                selectionPlate.Render(camera);

                grid0.Render(camera);
                grid1.Render(camera);
                grid2.Render(camera);
                grid3.Render(camera);
            }

        }   // end of TexturePicker Render()


        public void OnSelect(UIGrid grid)
        {
            //int index = grid.SelectionIndex.Y;

            // Don't need to do anything with the selection 
            // since we've been actively updating it.

            Deactivate();

            // Save the changes.
            string filename = InGame.CurrentLevelFilename(false);
            InGame.XmlWorldData.xmlTerrainData = InGame.inGame.Terrain.XmlTerrainData;

        }   // end of OnSelect

        public void OnCancel(UIGrid grid)
        {
            // From a user point of view it feels odd to have any changes go away when
            // the B button is pressed so have both A and B accept the changes.
            OnSelect(grid);

            //Deactivate();
        }   // end of OnCancel()

        /// <summary>
        /// This looks at the current state of the world, gets the existing
        /// textures and sets them as the defaults for each grid.  It also
        /// saves them away so that the user can cancel and have them restored.
        /// </summary>
        private void SetDefaultTextures()
        {
            String file = null;

            file = InGame.inGame.Terrain.XmlTerrainData.terrain0TextureFilename;
            file = file.Substring(file.LastIndexOf(@"\") + 1);
            oldIndex0 = GetIndexFromName(file);

            file = InGame.inGame.Terrain.XmlTerrainData.terrain1TextureFilename;
            file = file.Substring(file.LastIndexOf(@"\") + 1);
            oldIndex1 = GetIndexFromName(file);

            file = InGame.inGame.Terrain.XmlTerrainData.terrain2TextureFilename;
            file = file.Substring(file.LastIndexOf(@"\") + 1);
            oldIndex2 = GetIndexFromName(file);

            file = InGame.inGame.Terrain.XmlTerrainData.terrain3TextureFilename;
            file = file.Substring(file.LastIndexOf(@"\") + 1);
            oldIndex3 = GetIndexFromName(file);

            grid0.SelectionIndex = new Point(0, oldIndex0);
            grid1.SelectionIndex = new Point(0, oldIndex1);
            grid2.SelectionIndex = new Point(0, oldIndex2);
            grid3.SelectionIndex = new Point(0, oldIndex3);

            curFocusIndex0 = oldIndex0;
            curFocusIndex1 = oldIndex1;
            curFocusIndex2 = oldIndex2;
            curFocusIndex3 = oldIndex3;
        }   // end of TexturePicker SetDefaultTextures()

        private int GetIndexFromName(String name)
        {
            for (int i = 0; i < files.Count; i++)
            {
                TextureFile file = (TextureFile)files[i];
                if (file.filename.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                {
                    return i;
                }
            }

            // Shouldn't happen.
            Debug.Assert(false);
            return 0;
        }   // end of TexturePicker GetIndexFromName()

        public void Activate()
        {
            if (!active)
            {
                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Push(commandMap);
                active = true;
                grid0.Active = true;
                grid1.Active = false;
                grid2.Active = false;
                grid3.Active = false;
                activeGrid = 0;

                grid0.RenderWhenInactive = true;
                grid1.RenderWhenInactive = true;
                grid2.RenderWhenInactive = true;
                grid3.RenderWhenInactive = true;

                SetDefaultTextures();
                selectionPlate.Position = new Vector3(-2.25f, 0.0f, 0.0f);
            }
        }

        public void Deactivate()
        {
            if (active)
            {
                grid0.Active = false;
                grid1.Active = false;
                grid2.Active = false;
                grid3.Active = false;

                grid0.RenderWhenInactive = false;
                grid1.RenderWhenInactive = false;
                grid2.RenderWhenInactive = false;
                grid3.RenderWhenInactive = false;

                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Pop(commandMap);
                active = false;
            }
        }


        public void LoadContent(bool immediate)
        {
            BokuGame.Load(backPlate);
            BokuGame.Load(selectionPlate);
            BokuGame.Load(grid0);
            BokuGame.Load(grid1);
            BokuGame.Load(grid2);
            BokuGame.Load(grid3);
        }   // end of TexturePicker LoadGraphicsContent()

        public void UnloadContent()
        {
            BokuGame.Unload(backPlate);
            BokuGame.Unload(selectionPlate);
            BokuGame.Unload(grid0);
            BokuGame.Unload(grid1);
            BokuGame.Unload(grid2);
            BokuGame.Unload(grid3);
        }   // end of TexturePicker UnloadGraphicsContent()

        public void InitDeviceResources(GraphicsDeviceManager graphics)
        {
            Debug.Assert(false);
        }

        public void DeviceReset(GraphicsDeviceManager graphics)
        {
            Debug.Assert(false);
        }
    }   // end of class TexturePicker

}   // end of namespace Boku


