#define HIDE_MISSIONS

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
using Boku.Common.Xml;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.SimWorld;

namespace Boku
{
    /// <summary>
    /// The is the main menu for loading levels.  It's a tabbed page where each tab is a 
    /// different catagory of levels.  On each page we then have a list menu of available
    /// levels.  The pages are:
    ///     Missions
    ///     My Worlds
    ///     Starter Worlds
    ///     Downloads
    /// </summary>
    public class OldLoadLevelMenu : GameObject, INeedsDeviceReset
    {
        #region Members

        private static Color backgroundColor = Color.Black;
        private static Color unselectedColor = new Color(68, 77, 68);
        private static Color selectedColor = new Color(192, 120, 0);    // was Color.Orange;

        private static String missionsPath = @"Missions\";
        private static String myWorldsPath = @"MyWorlds\";
        private static String starterWorldsPath = @"StarterWorlds\";
        private static String downloadsPath = @"Downloads\";

        public static OldLoadLevelMenu Instance = null;

        #endregion

        protected class Shared : INeedsDeviceReset
        {
            #region Members

            public OldLoadLevelMenu parent = null;
            public Camera camera = new PerspectiveUICamera();

            // The menus for each of the tabs.
            public UIGrid missionsGrid = null;
            public UIGrid myWorldsGrid = null;
            public UIGrid starterWorldsGrid = null;
            public UIGrid downloadsGrid = null;

            public UIGrid2DTextElement missionsTab = null;
            public UIGrid2DTextElement myWorldsTab = null;
            public UIGrid2DTextElement starterWorldsTab = null;
            public UIGrid2DTextElement downloadsTab = null;
            public UIGrid2DTextElement bottomBar = null;

            public Texture backgroundTexture = null;

            public int numWorlds = -1;      // The number of worlds being displayed in the 
                                            // bottom bar.  When this no longer matches reality 
                                            // then the bottom bar texture must be updated.

            public LevelSort.SortBy curSortOrder = LevelSort.SortBy.UnSorted;
            public bool bottomBarDirty = true;

            // Position and size for buttons on bottom bar.  UI camera coordinates.
            public Vector2 xButtonPosition = new Vector2(-3.25f, -3.425f);
            public Vector2 yButtonPosition = new Vector2(-1.3f, -3.425f);
            public Vector2 buttonSize = new Vector2(0.4f, 0.4f);

            #endregion

            #region Public

            // c'tor
            public Shared(OldLoadLevelMenu parent)
            {
                this.parent = parent;
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

                // Create grids for each tabbed page.
                UIGridWorldTile.SharedRenderTarget.ResetAll();
                missionsGrid = CreateGrid(BokuGame.Settings.MediaPath + @"Xml\Levels" + @"\" + missionsPath, @"*.Xml");
                myWorldsGrid = CreateGrid(BokuGame.Settings.MediaPath + @"Xml\Levels" + @"\" + myWorldsPath, @"*.Xml");
                starterWorldsGrid = CreateGrid(BokuGame.Settings.MediaPath + @"Xml\Levels" + @"\" + starterWorldsPath, @"*.Xml");
                downloadsGrid = CreateGrid(BokuGame.Settings.MediaPath + @"Xml\Levels" + @"\" + downloadsPath, @"*.Xml");

                // Create tabs.
                // Create text elements.
                // Start with a blob of common parameters.
                UIGridElement.ParamBlob blob = new UIGridElement.ParamBlob();
                blob.width = 2.0f;
                blob.height = 0.5f;
                blob.edgeSize = 0.25f;
                blob.font = BokuGame.fontBerlinSansFBDemiBold24;
                blob.textColor = Color.White;
                blob.dropShadowColor = Color.Black;
                blob.useDropShadow = true;
                blob.invertDropShadow = false;
                blob.normalMapName = @"QuarterRoundNormalMap";
                blob.justify = UIGrid2DTextElement.Justification.Center;

                blob.selectedColor = selectedColor;
                blob.unselectedColor = unselectedColor;
                missionsTab = new UIGrid2DTextElement(blob, Strings.Instance.loadLevelMenu.missionsTab);
                missionsTab.Position = new Vector3(-3.75f, 3.25f, 0.0f);

                float offset = 0.0f;
#if HIDE_MISSIONS
                // Provide offset to center remaining tabs.
                offset = -0.75f;
#endif 

                blob.selectedColor = selectedColor;
                blob.unselectedColor = unselectedColor;
                myWorldsTab = new UIGrid2DTextElement(blob, Strings.Instance.loadLevelMenu.myWorldsTab);
                myWorldsTab.Position = new Vector3(-1.25f + offset, 3.25f, 0.0f);

                blob.selectedColor = selectedColor;
                blob.unselectedColor = unselectedColor;
                starterWorldsTab = new UIGrid2DTextElement(blob, Strings.Instance.loadLevelMenu.starterWorldsTab);
                starterWorldsTab.Position = new Vector3(1.25f + offset, 3.25f, 0.0f);

                blob.selectedColor = selectedColor;
                blob.unselectedColor = unselectedColor;
                downloadsTab = new UIGrid2DTextElement(blob, Strings.Instance.loadLevelMenu.downloadsTab);
                downloadsTab.Position = new Vector3(3.75f + offset, 3.25f, 0.0f);

                // Create the backdrop.

                // And the bottom bar.
                bottomBar = new UIGrid2DTextElement(8.5f, 0.5f, 0.25f, @"QuarterRoundNormalMap", selectedColor, @"", BokuGame.fontBerlinSansFBDemiBold20, UIGrid2DTextElement.Justification.Center, Color.White, Color.Black, false);
                bottomBar.Position = new Vector3(0.5f, -3.35f, 0.0f);
                bottomBar.SpecularColor = Color.Gray;
            }   // end of Shared c'tor

            /// <summary>
            /// Creates and populates a grid with file elements based on the input path and filter.
            /// </summary>
            /// <param name="path">Path where files reside.</param>
            /// <param name="filter">Filter for valid file names.</param>
            /// <returns>The created grid.  Grids are created in the InActive state.  Will return null if no files found.</returns>
            public UIGrid CreateGrid(String path, String filter)
            {
                // Create a list of files in the path.
                String[] files = null;

                try
                {
#if !XBOX360
                    files = Storage.GetFiles(path, filter, SearchOption.TopDirectoryOnly);
#else
                    files = Storage.GetFiles(path, filter);
#endif
                }
                catch (DirectoryNotFoundException)
                {
                    // The directory will be empty, so no need to get files again
                    // we do this as a convenience and its really not needed.
                }
                
                UIGrid grid = null;

                if (files != null && files.Length > 0)
                {
                    List<string> filteredFiles = new List<string>();

                    // Filter AutoSave.Xml
                    for (int i = 0; i < files.Length; ++i)
                    {
                        if (files[i].ToUpper().Contains("AUTOSAVE"))
                            continue;
                        filteredFiles.Add(files[i]);
                    }

                    files = filteredFiles.ToArray();
                }

                if (files != null && files.Length > 0)
                {
                    // Create and populate grid.
                    grid = new UIGrid(parent.OnSelect, parent.OnCancel, new Point(1, files.Length), "App.LoadLevelMenu");

                    // Set up the blob for info common to all tiles.
                    UIGridWorldTile.ParamBlob blob = new UIGridWorldTile.ParamBlob();
                    blob.width = 8.5f;
                    blob.height = 1.25f;
                    blob.edgeSize = 0.125f;
                    blob.selectedColor = selectedColor;
                    blob.unselectedColor = unselectedColor;
                    blob.textColor = Color.White;
                    blob.dropShadowColor = Color.Black;
                    blob.useDropShadow = true;
                    blob.invertDropShadow = false;
                    blob.normalMapName = @"QuarterRoundNormalMap";

                    int index = 0;
                    for (int i = 0; i < files.Length; i++)
                    {
                        DateTime dateTime = Storage.GetLastWriteTime(files[i]);

                        String filename = files[i].Substring(path.Length);
                        String fullPath = path + filename;

                        XmlWorldData xmlWorldData = XmlWorldData.Load(fullPath);

                        Texture thumb = null;
                        try
                        {
                            string thumbFilename = xmlWorldData.GetThumbFilenameWithoutExtension();
                            thumb = Storage.TextureLoad(path + thumbFilename);
                        }
                        catch { }
                        
                        UIGridWorldTile tile = new UIGridWorldTile(
                            blob,
                            xmlWorldData.id,
                            fullPath, 
                            xmlWorldData.name, 
                            xmlWorldData.description, 
                            xmlWorldData.creator, 
                            dateTime, 
                            xmlWorldData.rating,
                            thumb
                        );
                        grid.Add(tile, 0, index++);
                    }

                    grid.Spacing = new Vector2(0.0f, 0.05f);    // The first number doesn't really matter since we're doing a 1d column.
                    grid.Scrolling = true;
                    grid.SlopOffset = true;
                }

                return grid;

            }   // end of CreateGrid()

            public int FileNameComparison(String f0, String f1)
            {
                return String.Compare(f0, f1);
            }   // end of FileNameComparison()

            /// <summary>
            /// Given a tab value, returns the associated grid.  May return null.
            /// </summary>
            /// <param name="tab"></param>
            /// <returns></returns>
            public UIGrid GetGridFromTab(Tab tab)
            {
                UIGrid result = null;
                switch (tab)
                {
                    case Tab.Missions:
                        result = missionsGrid;
                        break;
                    case Tab.MyWorlds:
                        result = myWorldsGrid;
                        break;
                    case Tab.StarterWorlds:
                        result = starterWorldsGrid;
                        break;
                    case Tab.Downloads:
                        result = downloadsGrid;
                        break;
                }

                return result;
            }   // end of GetGridFromTab()

            public UIGrid GetGridFromCurTab()
            {
                return GetGridFromTab(parent.curTab);
            }   // end of GetGridFromCurTab()

            #endregion

            #region Internal

            /// <summary>
            /// Updates the texture to be rendered to the bottom bar based on the number of worlds and the current sort order.
            /// </summary>
            public void UpdateBottomBarTexture()
            {
                UIGrid grid = GetGridFromCurTab();
                int worldCount = grid != null ? grid.ActualDimensions.Y : 0;
                if (numWorlds != worldCount || bottomBarDirty)
                {
                    numWorlds = worldCount;
                    bottomBarDirty = false;

                    GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

                    // Render into the diffuse texture.
                    RenderTarget2D diffuse = bottomBar.Diffuse;

                    InGame.SetRenderTarget(diffuse);
                    InGame.Clear(Color.TransparentBlack);

                    BitmapFont font = BokuGame.fontBerlinSansFBDemiBold20;
                    
                    // Render the label text into the texture.
                    const int dpi = 128;
                    const float buttonSpacing = 0.1f;   // Magic number gotten through trial and error.  No other reason.
                    int x = diffuse.Width / 2 + (int)((xButtonPosition.X - bottomBar.Position.X + buttonSpacing) * dpi);

                    int h = (int)(dpi * bottomBar.Height);
                    int y = (int)((h - font.LineHeight) / 2.0f) - 2;

                    // Only show delete option for MyWorlds and Downloads.
                    if (parent.curTab == Tab.MyWorlds || parent.curTab == Tab.Downloads)
                    {
                        x += DrawText(Strings.Instance.loadLevelMenu.deleteWorld, x, y, bottomBar.TextColor);
                    }

                    // Sort options.
                    x = diffuse.Width / 2 + (int)((yButtonPosition.X - bottomBar.Position.X + buttonSpacing) * dpi);
                    x += DrawText(Strings.Instance.loadLevelMenu.oldSortBy, x, y, bottomBar.TextColor);

                    if (curSortOrder == LevelSort.SortBy.WorldName)
                    {
                        x += DrawText(Strings.Instance.loadLevelMenu.world, x, y, Color.Yellow);
                    }
                    else
                    {
                        x += DrawText(Strings.Instance.loadLevelMenu.world, x, y, bottomBar.TextColor);
                    }
                    x += DrawText(Strings.Instance.loadLevelMenu.separator, x, y, bottomBar.TextColor);
                    if (curSortOrder == LevelSort.SortBy.Creator)
                    {
                        x += DrawText(Strings.Instance.loadLevelMenu.oldCreator, x, y, Color.Yellow);
                    }
                    else
                    {
                        x += DrawText(Strings.Instance.loadLevelMenu.oldCreator, x, y, bottomBar.TextColor);
                    }
                    x += DrawText(Strings.Instance.loadLevelMenu.separator, x, y, bottomBar.TextColor);
                    if (curSortOrder == LevelSort.SortBy.Rating)
                    {
                        x += DrawText(Strings.Instance.loadLevelMenu.rating, x, y, Color.Yellow);
                    }
                    else
                    {
                        x += DrawText(Strings.Instance.loadLevelMenu.rating, x, y, bottomBar.TextColor);
                    }
                    x += DrawText(Strings.Instance.loadLevelMenu.separator, x, y, bottomBar.TextColor);
                    if (curSortOrder == LevelSort.SortBy.Date)
                    {
                        x += DrawText(Strings.Instance.loadLevelMenu.date, x, y, Color.Yellow);
                    }
                    else
                    {
                        x += DrawText(Strings.Instance.loadLevelMenu.date, x, y, bottomBar.TextColor);
                    }

                    string str = numWorlds.ToString() + (numWorlds == 1 ? Strings.Instance.loadLevelMenu.worldCount : Strings.Instance.loadLevelMenu.worldsCount);
                    x = diffuse.Width - font.MeasureString(str) - 50;
                    x += DrawText(str, x, y, bottomBar.TextColor);

                    // Note:
                    // The button graphics are rendered on the fly in the Render() 
                    // call rather than being rendered into the texture so they 
                    // don't get washed out by the specular highlights.
                    
                    

                    // Restore backbuffer.
                    InGame.RestoreRenderTarget();

                }
            }   // end of UpdateBottomBarTexture()

            /// <summary>
            /// A helper function to make it easier to render the text on the bottom bar while
            /// highlighting sections of it.
            /// </summary>
            /// <returns>The width of the string just rendered.</returns>
            private int DrawText(String str, int x, int y, Color color)
            {
                BitmapFont font = BokuGame.fontBerlinSansFBDemiBold20;

                if (bottomBar.UseDropShadow)
                {
                    TextHelper.DrawStringWithShadow(font, x, y, str, color, bottomBar.DropShadowColor, bottomBar.InvertDropShadow);
                }
                else
                {
                    font.DrawString(x, y, color, str);
                }

                return font.MeasureString(str);
            }   // end of DrawText()

            
            public void LoadGraphicsContent(GraphicsDeviceManager graphics)
            {
                BokuGame.Load(missionsGrid);
                BokuGame.Load(myWorldsGrid);
                BokuGame.Load(starterWorldsGrid);
                BokuGame.Load(downloadsGrid);

                BokuGame.Load(missionsTab);
                BokuGame.Load(myWorldsTab);
                BokuGame.Load(starterWorldsTab);
                BokuGame.Load(downloadsTab);

                BokuGame.Load(bottomBar);

                if (backgroundTexture == null)
                {
                    // Load different frames based on whether we're (approximately) widescreen or not.
                    if (camera.AspectRatio > 1.5f)
                    {
                        backgroundTexture = BokuGame.ContentManager.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\BigBinFrame");
                    }
                    else
                    {
                        backgroundTexture = BokuGame.ContentManager.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\BigBinFrame_800");
                    }
                }

                // Force a redraw of the bottom bar texture.
                bottomBarDirty = true;
            }   // end of LoadLevelMenu Shared LoadGraphicsContent()

            public void UnloadGraphicsContent()
            {
                BokuGame.Unload(missionsGrid);
                BokuGame.Unload(myWorldsGrid);
                BokuGame.Unload(starterWorldsGrid);
                BokuGame.Unload(downloadsGrid);

                BokuGame.Unload(missionsTab);
                BokuGame.Unload(myWorldsTab);
                BokuGame.Unload(starterWorldsTab);
                BokuGame.Unload(downloadsTab);

                BokuGame.Release(ref backgroundTexture);

                BokuGame.Unload(bottomBar);
            }   // end of LoadLevelMenu Shared UnloadGraphicsContent()

            #endregion

        }   // end of class Shared

        protected class UpdateObj : UpdateObject
        {
            #region Members

            private OldLoadLevelMenu parent = null;
            private Shared shared = null;

            #endregion

            #region Public

            public UpdateObj(OldLoadLevelMenu parent, Shared shared)
            {
                this.parent = parent;
                this.shared = shared;
            }

            public override void Update()
            {
                // Our children have input focus but we can still steal away the buttons we care about.
                GamePadInput pad = GamePadInput.GetGamePad1();

                UIGrid curGrid = shared.GetGridFromCurTab();

                bool sortNeeded = false;

                // Do we or our children have input focus?  This is so we can steal some inputs from them.
                if (CommandStack.Peek() == parent.commandMap || (curGrid != null && CommandStack.Peek() == curGrid.commandMap))
                {
                    if (pad.RightShoulder.WasPressed || pad.DPadRight.WasPressed || pad.LeftStickRight.WasPressed)
                    {
                        parent.curTab = (Tab)(((int)parent.curTab + 1) % (int)Tab.NumTabs);
#if HIDE_MISSIONS
                        if (parent.curTab == Tab.Missions)
                        {
                            parent.curTab = (Tab)(((int)parent.curTab + 1) % (int)Tab.NumTabs);
                        }
#endif

                        // Update active state.
                        if (curGrid != null)
                        {
                            curGrid.Active = false;
                        }
                        curGrid = shared.GetGridFromCurTab();
                        if (curGrid != null)
                        {
                            curGrid.Active = true;
                        }

                        sortNeeded = true;
                    }

                    if (pad.LeftShoulder.WasPressed || pad.DPadLeft.WasPressed || pad.LeftStickLeft.WasPressed)
                    {
                        parent.curTab = (Tab)(((int)parent.curTab + (int)Tab.NumTabs - 1) % (int)Tab.NumTabs);
#if HIDE_MISSIONS
                        if (parent.curTab == Tab.Missions)
                        {
                            parent.curTab = (Tab)(((int)parent.curTab + (int)Tab.NumTabs - 1) % (int)Tab.NumTabs);
                        }
#endif


                        // Update active state.
                        if (curGrid != null)
                        {
                            curGrid.Active = false;
                        }
                        curGrid = shared.GetGridFromCurTab();
                        if (curGrid != null)
                        {
                            curGrid.Active = true;
                        }

                        sortNeeded = true;
                    }

                    if (pad.ButtonY.WasPressed)
                    {
                        shared.curSortOrder = (LevelSort.SortBy)(((int)shared.curSortOrder + 1) % (int)LevelSort.SortBy.NumSorts);
                        sortNeeded = true;
                        shared.bottomBarDirty = true;

                        BokuGame.Audio.GetCue("programming add").Play();
                    }

                    if (shared.curSortOrder == LevelSort.SortBy.UnSorted)
                    {
                        shared.curSortOrder = LevelSort.SortBy.Date;
                        sortNeeded = true;
                    }

                    if (sortNeeded && curGrid != null)
                    {
                        LevelSort.SortGrid(curGrid, shared.curSortOrder);
                        sortNeeded = false;
                    }

                    if (pad.ButtonB.WasPressed)
                    {
                        // Back to Main Menu.
                        parent.Deactivate();
                        MainMenu.Instance.Activate();
                    }

                    // Only allow deletions from Downloads or MyWorlds.
                    if (pad.ButtonX.WasPressed && (parent.curTab == Tab.Downloads || parent.curTab == Tab.MyWorlds))
                    {
                        // TODO delete file.  Ask are you sure???

                        // Delete the file.  If curGrid is null then there's no file to delete.
                        if (curGrid != null)
                        {
                            UIGridWorldTile tile = (UIGridWorldTile)curGrid.SelectionElement;
                            String filename = tile.FullPath;
                            if (Storage.Exists(filename))
                            {
                                // NOTE: It is not safe to delete the following files since they could
                                // be shared by multiple worlds:
                                //
                                //  - The "stuff" file
                                //  - The terrain heightmap file
                                //  - The terrain texture select file

                                // Delete world xml file
                                Storage.Delete(filename);
                                // Delete thumbnail image
                                Storage.TextureDelete(Path.GetDirectoryName(filename) + @"\"+ Path.GetFileNameWithoutExtension(filename));

                                if (InGame.CurrentWorldId.ToString() == Path.GetFileNameWithoutExtension(filename))
                                {
                                    // Reset the current world information
                                    InGame.AutoSaved = false;
                                    InGame.XmlWorldData = null;
                                }
                            }

                            // Remove the shared render targets from all elements.
                            UIGridWorldTile.SharedRenderTarget.ResetAll();

                            // Remove the element from the grid.
                            if (curGrid.RemoveAndCollapse(curGrid.SelectionIndex))
                            {
                                // This was last element in grid.  Delete it.

                                // Pop its command map.
                                CommandStack.Pop(curGrid.commandMap);

                                // Remove the grid itself.
                                switch (parent.curTab)
                                {
                                    case Tab.Missions:
                                        shared.missionsGrid = null;
                                        break;
                                    case Tab.MyWorlds:
                                        shared.myWorldsGrid = null;
                                        break;
                                    case Tab.StarterWorlds:
                                        shared.starterWorldsGrid = null;
                                        break;
                                    case Tab.Downloads:
                                        shared.downloadsGrid = null;
                                        break;
                                }
                                curGrid = null;
                            }

                        }   // end if curGrid != null

                   }   // end if 'X' was pressed.
                
                }   // end of if we have input focus

                // If we're not shutting down, update the tabs and the child grids.
                if(parent.pendingState != States.Inactive)
                {
                    Matrix world = Matrix.Identity;

                    // Set the backdrop color and unfade current tab.
                    switch (parent.curTab)
                    {
                        case Tab.Missions:
                            shared.missionsTab.Selected = true;
                            shared.myWorldsTab.Selected = false;
                            shared.starterWorldsTab.Selected = false;
                            shared.downloadsTab.Selected = false;
                            break;
                        case Tab.MyWorlds:
                            shared.missionsTab.Selected = false;
                            shared.myWorldsTab.Selected = true;
                            shared.starterWorldsTab.Selected = false;
                            shared.downloadsTab.Selected = false;
                            break;
                        case Tab.StarterWorlds:
                            shared.missionsTab.Selected = false;
                            shared.myWorldsTab.Selected = false;
                            shared.starterWorldsTab.Selected = true;
                            shared.downloadsTab.Selected = false;
                            break;
                        case Tab.Downloads:
                            shared.missionsTab.Selected = false;
                            shared.myWorldsTab.Selected = false;
                            shared.starterWorldsTab.Selected = false;
                            shared.downloadsTab.Selected = true;
                            break;
                    }

                    shared.missionsTab.Update();
                    shared.myWorldsTab.Update();
                    shared.starterWorldsTab.Update();
                    shared.downloadsTab.Update();

                    shared.UpdateBottomBarTexture();
                    shared.bottomBar.Update();

                    if (shared.missionsGrid != null)
                    {
                        shared.missionsGrid.Update(ref world);
                    }
                    if (shared.myWorldsGrid != null)
                    {
                        shared.myWorldsGrid.Update(ref world);
                    }
                    if (shared.starterWorldsGrid != null)
                    {
                        shared.starterWorldsGrid.Update(ref world);
                    }
                    if (shared.downloadsGrid != null)
                    {
                        shared.downloadsGrid.Update(ref world);
                    }

                    // Make sure the visible elements for the current grid have render targets.
                    if (curGrid != null)
                    {
                        int focus = curGrid.SelectionIndex.Y;
                        int start = Math.Max(focus - 3, 0);
                        int end = Math.Min(focus + 3, curGrid.ActualDimensions.Y - 1);

                        for (int i = start; i <= end; i++)
                        {
                            UIGridWorldTile tile = (UIGridWorldTile)curGrid.Get(0, i);
                            if (tile.SRT == null)
                            {
                                // Get a rendertarget and assign to current tile.
                                UIGridWorldTile.SharedRenderTarget srt = UIGridWorldTile.SharedRenderTarget.Get(curGrid);
                                srt.Grid = curGrid;
                                srt.Index = i;
                                tile.SRT = srt;
                            }
                        }
                    }
                }   // end if not shutting down.
            }   // end of Update()

            #endregion

            #region Internal

            public override void Activate()
            {
                shared.curSortOrder = LevelSort.SortBy.UnSorted;
            }

            public override void Deactivate()
            {
            }

            #endregion

        }   // end of class LoadLevelMenu UpdateObj  

        protected class RenderObj : RenderObject
        {
            #region Members

            private Shared shared;

            #endregion

            #region Public

            public RenderObj(Shared shared)
            {
                this.shared = shared;
            }

            public override void Render(Camera camera)
            {
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

                // Clear the screen & z-buffer.
                InGame.Clear(backgroundColor);

                // Set up params for rendering UI with this camera.
                BokuGame.bokuGame.shaderGlobals.Effect.Parameters["EyeLocation"].SetValue(new Vector4(shared.camera.From, 1.0f));
                BokuGame.bokuGame.shaderGlobals.Effect.Parameters["CameraUp"].SetValue(new Vector4(shared.camera.Up, 1.0f));

                // Render the active grid using the local camera.
                UIGrid curGrid = shared.GetGridFromCurTab();
                if (curGrid != null)
                {
                    curGrid.Render(shared.camera);
                }

                // Render the backdrop/frame on top of the grid.
                ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();
                ssquad.Render(shared.backgroundTexture, new Vector2(0.0f), new Vector2(BokuGame.bokuGame.GraphicsDevice.Viewport.Width - 1.0f, BokuGame.bokuGame.GraphicsDevice.Viewport.Height - 1.0f), @"TexturedRegularAlpha");

                // Render the tabs.
#if !HIDE_MISSIONS
                shared.missionsTab.Render(shared.camera);
#endif
                shared.myWorldsTab.Render(shared.camera);
                shared.starterWorldsTab.Render(shared.camera);
                shared.downloadsTab.Render(shared.camera);

                
                // Render the bottom bar.  Well, actually, instead of rendering the bar
                // we'll just steal the texture from it and render than.
                shared.bottomBar.Render(shared.camera);
                CameraSpaceQuad csquad = CameraSpaceQuad.GetInstance();

                //csquad.Render(shared.camera, shared.bottomBar.Diffuse.GetTexture(), new Vector2(0.0f, -3.35f), new Vector2(9.5f, 0.5f), @"TexturedRegularAlpha");

                // Render the buttons for the bottom bar.
                if (shared.parent.curTab == Tab.MyWorlds || shared.parent.curTab == Tab.Downloads)
                {
                    csquad.Render(shared.camera, ButtonTextures.XButton, shared.xButtonPosition, shared.buttonSize, @"TexturedRegularAlpha");
                }
                csquad.Render(shared.camera, ButtonTextures.YButton, shared.yButtonPosition, shared.buttonSize, @"TexturedRegularAlpha");

            }   // end of LoadLevelMenu RenderObj Render()

            #endregion

            #region Internal

            public override void Activate()
            {
            }

            public override void Deactivate()
            {
            }

            #endregion

        }   // end of class LoadLevelMenu RenderObj     

        #region Members

        // List objects.
        protected Shared shared = null;
        protected RenderObj renderObj = null;
        protected UpdateObj updateObj = null;

        private enum States
        {
            Inactive,
            Active,
        }
        private States state = States.Inactive;
        private States pendingState = States.Inactive;

        private CommandMap commandMap = new CommandMap( "App.LoadLevel" );   // Placeholder for stack.

        protected enum Tab
        {
            Missions,
            MyWorlds,
            StarterWorlds,
            Downloads,
            NumTabs,
        }
#if HIDE_MISSIONS
        private Tab curTab = Tab.MyWorlds;
#else
        private Tab curTab = Tab.Missions;
#endif

        #endregion

        #region Accessors
        public bool Active
        {
            get { return (state == States.Active); }
        }
        #endregion

        #region Public

        // c'tor
        public OldLoadLevelMenu()
        {
            OldLoadLevelMenu.Instance = this;

            shared = new Shared(this);

            // Create the RenderObject and UpdateObject parts of this mode.
            updateObj = new UpdateObj(this, shared);
            renderObj = new RenderObj(shared);

            Init();
        }   // end of LoadLevelMenu c'tor

        private void Init()
        {
            // Create children
        }

        private void UpdateLevelZoneFromTab()
        {
            switch (curTab)
            {
                case Tab.MyWorlds:
                    InGame.CurrentLevelZone = LevelZone.MyWorlds;
                    break;

                case Tab.Downloads:
                    InGame.CurrentLevelZone = LevelZone.Downloads;
                    break;

                case Tab.Missions:
                    InGame.CurrentLevelZone = LevelZone.Missions;
                    break;

                case Tab.StarterWorlds:
                    InGame.CurrentLevelZone = LevelZone.StarterWorlds;
                    break;
            }
        }

        public void OnSelect(UIGrid grid)
        {
            // Shut down the grid.
            grid.Active = false;

            String fullPath = ((UIGridWorldTile)grid.SelectionElement).FullPath;
            // Save this off for use until we've rendered a frame
            InGame.inGame.ThumbNail = ((UIGridWorldTile)grid.SelectionElement).Thumbnail as Texture2D;

            // Load all worlds statically except mine.
            bool staticLoading = !(curTab == Tab.MyWorlds);

            Deactivate();

            UpdateLevelZoneFromTab();
            BokuGame.bokuGame.inGame.ActivateWithNewLevel(fullPath/*, staticLoading*/);

        }   // end of OnSelect

        public void OnCancel(UIGrid grid)
        {
            // Back to Main Menu.
            Deactivate();
            MainMenu.Instance.Activate();
        }

        
        /// <summary>
        /// Set the Downloads tab as active.
        /// </summary>
        public void SetToDownloadsTab()
        {
            curTab = Tab.Downloads;
            InGame.CurrentLevelZone = LevelZone.Downloads;
        }   // end of LoadLevelMenu SetToDownloadsTab()

        #endregion

        #region Internal

        public override bool Refresh(ArrayList updateList, ArrayList renderList)
        {
            bool result = false;

            if (state != pendingState)
            {
                if (pendingState == States.Active)
                {
                    updateList.Add(updateObj);
                    updateObj.Activate();
                    renderList.Add(renderObj);
                    renderObj.Activate();

                    UIGrid grid = shared.GetGridFromCurTab();
                    if (grid != null)
                    {
                        grid.Active = true;
                    }
                }
                else
                {
                    UIGrid grid = shared.GetGridFromCurTab();
                    if (grid != null)
                    {
                        grid.Active = false;
                    }

                    renderObj.Deactivate();
                    renderList.Remove(renderObj);
                    updateObj.Deactivate();
                    updateList.Remove(updateObj);
                }

                state = pendingState;
            }

            return result;
        }

        override public void Activate()
        {
            if (state != States.Active)
            {
                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Push(commandMap);

                // Recreate the MyWorlds and Downloads menus since we may have added new files to them.
                UIGridWorldTile.SharedRenderTarget.ResetAll();
                BokuGame.Unload(shared.myWorldsGrid);
                BokuGame.Unload(shared.downloadsGrid);

                shared.myWorldsGrid = shared.CreateGrid(BokuGame.Settings.MediaPath + @"Xml\Levels" + @"\" + myWorldsPath, @"*.Xml");
                if (shared.myWorldsGrid != null)
                {
                    // Sort and set top item as initial selection.
                    LevelSort.SortGrid(shared.myWorldsGrid, LevelSort.SortBy.Date);
                    shared.myWorldsGrid.SelectionIndex = new Point(0, 0);
                }

                shared.downloadsGrid = shared.CreateGrid(BokuGame.Settings.MediaPath + @"Xml\Levels" + @"\" + downloadsPath, @"*.Xml");
                if (shared.downloadsGrid != null)
                {
                    // Sort and set top item as initial selection.
                    LevelSort.SortGrid(shared.downloadsGrid, LevelSort.SortBy.Date);
                    shared.downloadsGrid.SelectionIndex = new Point(0, 0);
                }

                BokuGame.Load(shared.myWorldsGrid);
                BokuGame.Load(shared.downloadsGrid);

                pendingState = States.Active;
                BokuGame.objectListDirty = true;
            }
        }

        override public void Deactivate()
        {
            if (state != States.Inactive)
            {
                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Pop(commandMap);

                UIGrid grid = shared.GetGridFromCurTab();
                if (grid != null)
                {
                    grid.Active = false;
                }
                
                pendingState = States.Inactive;
                BokuGame.objectListDirty = true;
            }
        }


        public void LoadGraphicsContent(GraphicsDeviceManager graphics)
        {
            BokuGame.Load(shared);
        }   // end of LoadLevelMenu LoadGraphicsContent()

        public void UnloadGraphicsContent()
        {
            BokuGame.Unload(shared);
        }   // end of LoadLevelMenu UnloadGraphicsContent()

        #endregion

    }   // end of class LoadLevelMenu

}   // end of namespace Boku


