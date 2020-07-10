// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


// Uncomment the following line to cause fake presence 
// events to be fed to the sharing presence display.
//#define PRESENCE_DEBUG

// Uncomment to help figure out crashing during export.
//#define EXPORT_DEBUG_HACK

// Until we get all the new auth stuf figured out, hide likes and comments.
#define HIDE_LIKES

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
#if NETFX_CORE
    using Windows.Storage;
    using Windows.Storage.Pickers;
    using Windows.Storage.Streams;
using Windows.System;
#else
using System.Windows.Forms;
#endif

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
#if !NETFX_CORE
using Microsoft.Xna.Framework.Net;
#endif

using KoiX;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;

using Boku.Base;
using Boku.Common;
using Boku.Common.Gesture;
using Boku.Common.Sharing;
using Boku.Common.Xml;
using Boku.UI2D;
using Boku.Input;
using Boku.Web;
using Boku.Fx;
using Boku.Programming;

using Boku.Audio;
using BokuShared;
using BokuShared.Wire;

namespace Boku
{
    /// <summary>
    /// The is the main menu for loading levels.
    /// </summary>
    public class LoadLevelMenu : GameObject, INeedsDeviceReset
    {
        public class Shared : INeedsDeviceReset
        {
            #region Members

            public int scrollOpCount;

            public bool isUserAdmin = false;            // Does the user have admin privileges?
            public bool isMyWorld = false;              // Is the current world one of the user's?
            public bool isBuiltInWorld = false;         // Is the current world one of start worlds?
            public bool isDownload = false;             // Is the current world an unmodified downloaded world?

            public bool isDeleteActive = false;         // Is the user allowed to delete this world?

            public string userName = null;              // The name of the current user.  Used to decided whether or 
                                                        // not she may delete a file from the community.

            public bool isAttaching = false;            // Are we in attach-level mode?

            public TextLineEditor textLineEditor = null;         // Editor for search.

            public AABB2D searchBox = new AABB2D(new Vector2(195, 110), new Vector2(195+375, 110+50));
            public AABB2D fullScreenHitBox = new AABB2D();

            public LoadLevelMenu parent = null;
            public Camera camera = new PerspectiveUICamera();           // Camera for rendering most UI.
            public Camera dialogCamera = new PerspectiveUICamera();     // Camera for rendering dialogs (not on rendertarget).

            // The menu grids.
            public LoadLevelMenuUIGrid levelGrid = null;    // List of level tiles.
            public UIGrid bucketsGrid = null;               // Buckets (level categories)
            public ModularCheckboxList sortList = null;     // "sort by" menu.
            public LoadLevelPopup popup = null;             // Popup displayed when <A> is pressed.
            public string sortListDisplay = Strings.Localize("loadLevelMenu.sortDate");            // The sort string displayed to the user.

            // Text color for button label.
            public Color sortColor = new Color(191, 191, 191);
            // Color targetted by the twitch.  Used for comparisons so
            // we know whether or not to start a new twitch.
            public Color sortTargetColor = new Color(191, 191, 191);

            // Color constants.
            public Color lightTextColor = new Color(191, 191, 191);
            public Color hoverTextColor = new Color(50, 255, 50);

            public TagPicker tagPicker = null;

            // Mouse hit boxes for opening show and sort menus.
            public AABB2D sortBox = new AABB2D();
            public AABB2D backBox = new AABB2D();   // Back button for popup menu.

            public AABB2D likesBox = new AABB2D();              // Hitbox for num likes.
            public AABB2D downloadsBox = new AABB2D();          // Hitbox for num downloads.

            public ILevelBrowser mainBrowser = null;
            public ILevelBrowser altBrowser = null;         // On the Sharing page we also need to be able to toggle between 
            // using the SharingBrowser and the LocalBrowser.  We use this
            // as a place to hang the currently unused one.

            public ILevelBrowser remoteBrowser = null;
            public LocalLevelBrowser localBrowser = null;
            public CommunityLevelBrowser srvBrowser = null;

            public ILevelSetCursor mainCursor = null;
            public ILevelSetCursor altCursor = null;        // See above.

            public LevelSetFilterByKeywords levelFilter;
            public LevelSetSorterBasic levelSorter;
            public bool showPagingMessage;

            // Bumpers for buckets selection.
            public Vector2 leftBumperPosition = Vector2.Zero;
            public Vector2 rightBumperPosition = Vector2.Zero;
            public AABB2D leftBumperBox = new AABB2D();
            public AABB2D rightBumperBox = new AABB2D();

            // Arrows for scrolling levels w/ mouse.
            public AABB2D arrowLeftBox = new AABB2D();
            public AABB2D arrowRightBox = new AABB2D();
            public CommunityShareMenu communityShareMenu = new CommunityShareMenu();

            #endregion

            #region Accessors

            public LevelMetadata CurWorld
            {
                get
                {
                    LevelMetadata cur = null;
                    if (mainCursor != null)
                    {
                        cur = mainCursor[0];
                    }
                    return cur;
                }
            }

            public bool IsExportEnabled
            {
                get
                {
                    return true;
                }
            }

            #endregion

            #region Public

            // c'tor
            public Shared(LoadLevelMenu parent)
            {
                this.parent = parent;

                // We're rendering this whole UI to a 1280x720 rendertarget and
                // then cropping it as needed for 4:3 display.
                camera.Resolution = new Point(1280, 720);
                dialogCamera.Resolution = new Point((int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);

                // Create tag picker.
                tagPicker = new TagPicker();
                tagPicker.OnExit = OnTagPickerExit;
                tagPicker.WorldMatrix = Matrix.CreateTranslation(-2.5f, 0.0f, 0.0f);

                levelFilter = new LevelSetFilterByKeywords();
                if (parent.OriginalBrowserType == LevelBrowserType.Community)
                {
                    levelFilter.ServerSideMatching = true;
                }
                else
                {
                    levelFilter.ServerSideMatching = false;
                }

                levelFilter.FilterGenres = Genres.All;
                levelSorter = new LevelSetSorterBasic();
                levelSorter.SortBy = SortBy.Date;
                levelSorter.SortDirection = SortDirection.Descending;

                textLineEditor = new TextLineEditor(searchBox, levelFilter.SearchString, @"Textures\UI2D\SearchIcon");


                // Create level grid.
                levelGrid = new LoadLevelMenuUIGrid(
                    parent.OnSelect,
                    parent.OnCancel,
                    parent.OnMoveLeft,
                    parent.OnMoveRight,
                    @"App.LoadLevelMenu.LevelGrid");
                Matrix mat = Matrix.CreateTranslation(-3.2f, 0.65f, 0.0f);
                levelGrid.LocalMatrix = mat;
                levelGrid.RenderWhenInactive = true;
                levelGrid.UseTriggers = true;
                levelGrid.UseMouseScrollWheel = true;

                // Create buckets grid.
                SetUpBuckets();

            }   // end of Shared c'tor

            private void OnTagPickerExit(ModularCheckboxList picker)
            {
                int tags = tagPicker.GetTags();

                // Apply new tags to the current world. Be careful
                // to preserve the vritual tags.
                Genres virt = CurWorld.Genres & Genres.Virtual;
                CurWorld.Genres = virt | (Genres)tags;

                // Write out changed metadata.
                XmlDataHelper.UpdateWorldMetadata(CurWorld);

                // TODO Touch browser to re-filter?

            }   // end of OnTagPickerChange()

            private void OnChange(UIGrid grid)
            {
                if (popup != null)
                {
                    popup.Active = false;
                }
            }

            private void SetUpBuckets()
            {
                bucketsGrid = new UIGrid(BucketOnSelect, BucketOnCancel, new Point(6, 0), "App.LoadLevelMenu.BucketsGrid");
                bucketsGrid.AlwaysReadInput = true;
                bucketsGrid.UseDPad = false;
                bucketsGrid.UseLeftStick = false;
                bucketsGrid.UseShoulders = true;
                bucketsGrid.UseKeyboard = false;
                bucketsGrid.UseTab = true;
                bucketsGrid.Wrap = true;
                bucketsGrid.RenderWhenInactive = true;
                bucketsGrid.Spacing = new Vector2(0.1f, 0.0f);
                bucketsGrid.MovedLeft = OnChange;
                bucketsGrid.MovedRight = OnChange;

                UIGridElement.ParamBlob blob = new UIGridElement.ParamBlob();
                blob.width = 1.5f;
                blob.height = 0.5f;
                blob.Font = KoiX.SharedX.GetGameFont20;
                blob.unselectedTextColor = Color.White;
                blob.selectedTextColor = new Color(12, 255, 0);
                blob.justify = TextHelper.Justification.Center;

                UIGridModularTextElement e = null;
                int index = 0;

                if (parent.OriginalBrowserType == LevelBrowserType.Local)
                {
                    e = new UIGridModularTextElement(blob, Strings.Localize("loadLevelMenu.showMyWorlds"));
                    bucketsGrid.Add(e, index++, 0);
                    e = new UIGridModularTextElement(blob, Strings.Localize("loadLevelMenu.showDownloads"));
                    bucketsGrid.Add(e, index++, 0);
                    e = new UIGridModularTextElement(blob, Strings.Localize("loadLevelMenu.showLessons"));
                    bucketsGrid.Add(e, index++, 0);
                    e = new UIGridModularTextElement(blob, Strings.Localize("loadLevelMenu.showSamples"));
                    bucketsGrid.Add(e, index++, 0);
                    e = new UIGridModularTextElement(blob, Strings.Localize("loadLevelMenu.showAll"));
                    bucketsGrid.Add(e, index++, 0);
                    bucketsGrid.SelectionIndex = new Point(4, 0);   // Default to "All".

                    Matrix mat = Matrix.CreateTranslation(-0.8f, 3.0f, 0.0f);
                    bucketsGrid.LocalMatrix = mat;

                    leftBumperPosition = new Vector2(90, 32);
                    rightBumperPosition = new Vector2(944, 32);

                }
                else if (parent.OriginalBrowserType == LevelBrowserType.Community)
                {
                    e = new UIGridModularTextElement(blob, Strings.Localize("loadLevelMenu.showMyWorlds"));
                    bucketsGrid.Add(e, index++, 0);
                    e = new UIGridModularTextElement(blob, Strings.Localize("loadLevelMenu.showLessons"));
                    bucketsGrid.Add(e, index++, 0);
                    e = new UIGridModularTextElement(blob, Strings.Localize("loadLevelMenu.showSamples"));
                    bucketsGrid.Add(e, index++, 0);
                    e = new UIGridModularTextElement(blob, Strings.Localize("loadLevelMenu.showAll"));
                    bucketsGrid.Add(e, index++, 0);
                    bucketsGrid.SelectionIndex = new Point(3, 0);   // Default to "All".

                    Matrix mat = Matrix.CreateTranslation(-1.6f, 3.0f, 0.0f);
                    bucketsGrid.LocalMatrix = mat;

                    leftBumperPosition = new Vector2(90, 32);
                    rightBumperPosition = new Vector2(791, 32);

                }
                else if (parent.OriginalBrowserType == LevelBrowserType.Sharing)
                {
                    e = new UIGridModularTextElement(blob, Strings.Localize("loadLevelMenu.showSharing"));
                    bucketsGrid.Add(e, index++, 0);
                    e = new UIGridModularTextElement(blob, Strings.Localize("loadLevelMenu.showMyWorlds"));
                    bucketsGrid.Add(e, index++, 0);
                    e = new UIGridModularTextElement(blob, Strings.Localize("loadLevelMenu.showDownloads"));
                    bucketsGrid.Add(e, index++, 0);
                    bucketsGrid.SelectionIndex = new Point(0, 0);   // Default to "Sharing".

                    Matrix mat = Matrix.CreateTranslation(-2.4f, 3.0f, 0.0f);
                    bucketsGrid.LocalMatrix = mat;

                    leftBumperPosition = new Vector2(90, 32);
                    rightBumperPosition = new Vector2(636, 32);

                }
                else
                {
                    Debug.Assert(false, "Unrecognized browser type");
                }

                // Bounding boxes for mouse hit test.
                Vector2 min = leftBumperPosition;
                Vector2 max = leftBumperPosition + new Vector2(96, 72);
                leftBumperBox.Set(min, max);
                min = rightBumperPosition;
                max = rightBumperPosition + new Vector2(96, 72);
                rightBumperBox.Set(min, max);

            }   // end of SetUpBuckets()

            private void BucketOnSelect(UIGrid grid)
            {
            }   // end of BucketOnSelect()

            private void BucketOnCancel(UIGrid grid)
            {
                // Do nothing.
            }   // end of BucketOnCancel()

            /// <summary>
            /// Makes sure that the menu selections match the actual state.
            /// </summary>
            public void SetAuxMenuDefaultSelections()
            {
                //
                // SortBy list
                //

                int i = sortList.GetIndex(Strings.Localize("loadLevelMenu.sortDate"));
                sortList.GetItem(i).Check = levelSorter.SortBy == SortBy.Date;
                if (sortList.GetItem(i).Check)
                {
                    sortListDisplay = sortList.GetItem(i).Text;
                    sortList.CurIndex = i;
                }

                i = sortList.GetIndex(Strings.Localize("loadLevelMenu.sortCreator"));
                sortList.GetItem(i).Check = levelSorter.SortBy == SortBy.Creator;
                if (sortList.GetItem(i).Check)
                {
                    sortListDisplay = sortList.GetItem(i).Text;
                    sortList.CurIndex = i;
                }

                i = sortList.GetIndex(Strings.Localize("loadLevelMenu.sortTitle"));
                sortList.GetItem(i).Check = levelSorter.SortBy == SortBy.Name;
                if (sortList.GetItem(i).Check)
                {
                    sortListDisplay = sortList.GetItem(i).Text;
                    sortList.CurIndex = i;
                }

                // If we're on the community browser, also allow sorting by rating.
                if (parent.OriginalBrowserType == LevelBrowserType.Community)
                {
                    i = sortList.GetIndex(Strings.Localize("loadLevelMenu.sortRank"));
                    sortList.GetItem(i).Check = levelSorter.SortBy == SortBy.Rank;
                    if (sortList.GetItem(i).Check)
                    {
                        sortListDisplay = sortList.GetItem(i).Text;
                        sortList.CurIndex = i;
                    }
                }


            }   // end of SetAuxMenuDefaultSelections()

            #endregion

            #region Internal

            /// <summary>
            /// Sets up the SortBy 
            /// </summary>
            public void SetUpAuxMenus()
            {
                UIGridElement.ParamBlob blob = new UIGridElement.ParamBlob();

                blob.textColor = new Color(30, 30, 30);
                blob.dropShadowColor = Color.Black;
                blob.Font = SharedX.GetGameFont20;
                blob.selectedColor = Color.White;
                blob.unselectedColor = new Color(30, 30, 30);

                // Sort By menu.
                sortList = new ModularCheckboxList();
                sortList.AllExclusive = true;
                sortList.OnExit = null;
                sortList.OnChange = ListOnExit;     // We want to exit when we change anything.
                sortList.WorldMatrix = Matrix.CreateTranslation(1.25f, 2.0f, 0.0f);

                sortList.AddItem(Strings.Localize("loadLevelMenu.sortDate"), true);
                sortList.AddItem(Strings.Localize("loadLevelMenu.sortCreator"), false);
                sortList.AddItem(Strings.Localize("loadLevelMenu.sortTitle"), false);
                if (parent.OriginalBrowserType == LevelBrowserType.Community)
                {
                    sortList.AddItem(Strings.Localize("loadLevelMenu.sortRank"), false);
                }

                SetAuxMenuDefaultSelections();

                popup = new LoadLevelPopup();
                popup.Position = new Vector2(310, 185);
                popup.Size = new Vector2(310, 230);

            }   // end of SetUpAuxMenus()

            /// <summary>
            /// Depending on the browser type and the current state, decide which
            /// options we want to expose.
            /// NOTE: This is called every frame.
            /// </summary>
            public void SetUpPopup(bool setupSubMenusToo)
            {
                popup.ClearAllItems();

                if (parent.OriginalBrowserType == LevelBrowserType.Sharing)
                {
                    UIGridModularTextElement e = (UIGridModularTextElement)bucketsGrid.SelectionElement;

                    if (e.Label == Strings.Localize("loadLevelMenu.showSharing"))
                    {
                        // If we're looking at other people's levels, we can download or play them.
                        // Only allow them to be played if they've completed downloading.
                        if (parent.shared.CurWorld != null)
                        {
                            if (parent.shared.CurWorld.DownloadState == LevelMetadata.DownloadStates.None || parent.shared.CurWorld.DownloadState == LevelMetadata.DownloadStates.Failed)
                            {
                                popup.AddItem(Strings.Localize("loadLevelMenu.download"), PopupOnDownload);
                            }
                            else if (parent.shared.CurWorld.DownloadState == LevelMetadata.DownloadStates.Complete)
                            {
                                popup.AddItem(Strings.Localize("loadLevelMenu.playLevel"), PopupOnPlay);

                                if (IsExportEnabled && 0 == (parent.shared.CurWorld.Genres & Genres.BuiltInWorlds))
                                {
                                    popup.AddItem(Strings.Localize("loadLevelMenu.export"), PopupOnExport);
                                }
                            }
                        }
                    }
                    else
                    {
                        // If we're looking at our own, we can toggle the sharing, change tags or delete.
                        popup.AddItem(Strings.Localize("loadLevelMenu.playLevel"), PopupOnPlay);

                        if (IsExportEnabled && (parent.shared.CurWorld != null) && 0 == (parent.shared.CurWorld.Genres & Genres.BuiltInWorlds))
                        {
                            popup.AddItem(Strings.Localize("loadLevelMenu.editLevel"), PopupOnEdit);
                            popup.AddItem(Strings.Localize("loadLevelMenu.export"), PopupOnExport);
                        }

                        if (parent.shared.isDeleteActive)
                        {
                            popup.AddItem(Strings.Localize("loadLevelMenu.editTags"), PopupOnChangeTags);
                            popup.AddItem(Strings.Localize("loadLevelMenu.delete"), PopupOnDelete);
                        }
                    }
                }
                else if (parent.OriginalBrowserType == LevelBrowserType.Local)
                {
                    if (isAttaching)
                    {
                        popup.AddItem(Strings.Localize("loadLevelMenu.attach"), PopupOnAttach);
                    }
                    else
                    {
                        popup.AddItem(Strings.Localize("loadLevelMenu.playLevel"), PopupOnPlay);

                        // Video output hack.
                        if (Auth.CreatorName == "scoy6" || Auth.CreatorName == "scoy")
                        {
                            popup.AddItem(Strings.Localize("loadLevelMenu.videoOut"), PopupOnVideoOut);
                        }

                        if (IsExportEnabled && (parent.shared.CurWorld != null) && 0 == (parent.shared.CurWorld.Genres & Genres.BuiltInWorlds))
                        {
                            popup.AddItem(Strings.Localize("loadLevelMenu.editLevel"), PopupOnEdit);
                            popup.AddItem(Strings.Localize("loadLevelMenu.export"), PopupOnExport);
                        }

                        // Share with community.  No sharing of downloaded files since we expect that they are already uploaded.
                        // No sharing of built in worlds since everyone already has those also.
                        if (Program2.SiteOptions.CommunityEnabled
                            && (parent.shared.CurWorld != null)
                            && (parent.shared.CurWorld.Genres & Genres.Downloads) == 0
                            && (parent.shared.CurWorld.Genres & Genres.BuiltInWorlds) == 0)
                        {
                            popup.AddItem(Strings.Localize("loadLevelMenu.share"), PopupOnCommunityShare);
                        }

                        if (parent.shared.isDeleteActive)
                        {
                            popup.AddItem(Strings.Localize("loadLevelMenu.editTags"), PopupOnChangeTags);
                            popup.AddItem(Strings.Localize("loadLevelMenu.delete"), PopupOnDelete);
                        }
                    }
                }
                else if (parent.OriginalBrowserType == LevelBrowserType.Community)
                {
                    //
                    // Community browser.
                    //
                    if (parent.shared.CurWorld.DownloadState == LevelMetadata.DownloadStates.None)
                    {
                        popup.AddItem(Strings.Localize("loadLevelMenu.download"), PopupOnDownload);
                    }
                    else
                    {
                        popup.AddItem(Strings.Localize("loadLevelMenu.playLevel"), PopupOnPlay);
                        /*
                        // Don't bother to show Export.  It's valid but clutters up the UI.
                        if (IsExportEnabled && 0 == (parent.shared.CurWorld.Genres & Genres.BuiltInWorlds))
                        {
                            popup.AddItem(Strings.Localize("loadLevelMenu.export"), PopupOnExport);
                        }
                        */
                    }

                    if (parent.shared.CurWorld != null && parent.shared.CurWorld.DownloadState == LevelMetadata.DownloadStates.Complete)
                    {
                        popup.AddItem(Strings.Localize("loadLevelMenu.editTags"), PopupOnChangeTags);
                    }

                    if (parent.shared.isDeleteActive)
                    {
                        popup.AddItem(Strings.Localize("loadLevelMenu.delete"), PopupOnDelete);
                    }

#if !HIDE_LIKES
#if !NETFX_CORE
                    // Not available for Win8 version since we haven't fixed the auth issue.

                    // Likes (will need to auth first)
                    popup.AddItem(Strings.Localize("loadLevelMenu.like"), PopupOnLike);

                    // Comments (can read but needs auth to leave a new comment)
                    popup.AddItem(Strings.Localize("loadLevelMenu.comments"), PopupOnComments);
#endif
#endif

                    // Always allow abuse reporting.  No longer allow un-reporting.
                    if (true)
                    {
                        if (parent.shared.CurWorld.FlaggedByMe)
                        {
                            //popup.AddItem(Strings.Localize("loadLevelMenu.unReportAbuse"), PopupOnReportAbuse);
                        }
                        else
                        {
                            popup.AddItem(Strings.Localize("loadLevelMenu.reportAbuse"), PopupOnReportAbuse);
                        }
                    }
                }
                else
                {
                    Debug.Assert(false, "Unrecognized browser type");
                }
            }   // end of SetUpPopup()


            private void AttachSelectedLevel()
            {
                LevelMetadata targetNextLevel = parent.shared.CurWorld;
                if (targetNextLevel != null)
                {
                    //TODO: look for broken links first?  Or assume no broken links - they should be resolved when level loads?

                    LevelMetadata currentLevel = LevelMetadata.CreateFromXml(InGame.XmlWorldData);

                    //don't search forward, we're going to be linking from this level elsewhere
                    currentLevel.LinkedToLevel = null;

                    LevelMetadata brokenLevel = null;
                    bool brokenForwards = false;
                    if (currentLevel.FindBrokenLink(ref brokenLevel, ref brokenForwards))
                    {
                        Debug.WriteLine("ERROR: broken link found, can't attach. ");
                        parent.ShowBrokenLinkOnAttachDialog();
                        return;
                    }

                    //start at the beginning and traverse the chain forward to the current level
                    LevelMetadata nextLink = currentLevel.FindFirstLink();

                    //make sure the new link doesn't form a loop
                    bool cycleFound = false;
                    while (nextLink != null && nextLink.WorldId != currentLevel.WorldId)
                    {
                        //does a link in our past match what we're trying to link to?  if so, don't allow it
                        if (nextLink.WorldId == targetNextLevel.WorldId)
                        {
                            cycleFound = true;
                            break;
                        }
                        nextLink = nextLink.NextLink();
                    }

                    //check if we're creating a cycle (or self-linking)
                    if (cycleFound || currentLevel.WorldId == targetNextLevel.WorldId)
                    {
                        Debug.WriteLine("ERROR: Cycle found, unable to create new link");
                        parent.ShowSimpleDialog("loadLevelMenu.levelChainHasCycleMessage");
                        return;
                    }

                    //check if target next world had a level attached (and was local - downloads/built-ins will be copied so they don't count)
                    if (targetNextLevel.LinkedFromLevel != null &&
                        XmlDataHelper.CheckWorldExistsByGenre((Guid)targetNextLevel.LinkedFromLevel, Genres.MyWorlds))
                    {
                        Debug.WriteLine("WARNING: Linking to a level that has already been linked to - inform the user.");
                        parent.ShowTargetAlreadyLinkedDialog(targetNextLevel.WorldId);
                        return;
                    }

                    //we're good, update the actual link in the xml (won't take effect until a save occurs)
                    InGame.XmlWorldData.LinkedToLevel = targetNextLevel.WorldId;

                    parent.ReturnToPreviousMenu();
                }
            }

            public void PopupOnAttach()
            {
                popup.Active = false;

                if (levelGrid.SelectionElement != null)
                {
                    AttachSelectedLevel();
                }
                else
                {
                    // SelectionElement is null which implies that the grid has not yet been populated so 
                    // don't let the user make a choice.  Note that they can still exit if they choose.

                    // Reactivate the grid and return.
                    levelGrid.Active = true;
                }
            }   // end of PopupOnAttach()

            /// <summary>
            /// Exports the selected level.
            /// </summary>
            /// <returns>Filename exported to or null if not exported.</returns>
            private string ExportSelectedLevel()
            {
                //always operate on first link in a chain of levels (if no chain, the level will be the first link)
                LevelMetadata level = parent.shared.CurWorld.FindFirstLink();

                if (level != null)
                {
                    //check if the chosen level has any broken links - if so, warn the player
                    LevelMetadata brokenLevel = null;
                    bool forwardsLinkBroken = false;
                    if (level.FindBrokenLink(ref brokenLevel, ref forwardsLinkBroken))
                    {
                        parent.ShowBrokenLevelExportWarning();

                        return null;
                    }

                    string exportedFilename = null;
#if NETFX_CORE
                    // Show WinRT happy save file dialog.
                    PickSaveFile(level);

#else
                    exportedFilename = ShowExportDialog(level);
#endif

                    return exportedFilename;

                }   // end if level not null

                return null;

            }   // end of ExportSelectedLevel()

#if NETFX_CORE
            private async void PickSaveFile(LevelMetadata level)
            {
                FileSavePicker savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("Kodu Game Lab World", new List<string>() { ".Kodu2" });
                savePicker.DefaultFileExtension = ".Kodu2";
                // TODO (****) where do we get/generate this?

                // Set the default filename.
                string folderName = Utils.FolderNameFromFlags(level.Genres);
                string pathToLevelFile = Path.Combine(BokuGame.Settings.MediaPath, folderName, level.WorldId.ToString() + ".Xml");
                XmlWorldData xml = XmlWorldData.Load(pathToLevelFile, XnaStorageHelper.Instance);

                savePicker.SuggestedFileName = GenerateDefaultFileName(level, true);
                StorageFile savedItem = await savePicker.PickSaveFileAsync();

                if (savedItem != null)
                {
                    IRandomAccessStream stream = await savedItem.OpenAsync(FileAccessMode.ReadWrite);
                    Stream outStream = stream.AsStreamForWrite();

                    // Store the file.
                    string filePath = savedItem.Path;
                    ExportLevel(level, filePath, outStream);

                    //await stream.FlushAsync();
                    //stream.Dispose();
                }
            }
#endif

#if !NETFX_CORE

            /// <summary>
            /// Shows dialog for exporting file.  Returns name chosen to export to.
            /// </summary>
            /// <param name="level"></param>
            /// <returns>Filename we exported to, null if user backs out.</returns>
            private string ShowExportDialog(LevelMetadata level)
            {
                // Create new SaveFileDialog.
                SaveFileDialog DialogSave = new SaveFileDialog();

                // Default file extension.
                DialogSave.DefaultExt = "Kodu2";

                // Available file extensions.
                DialogSave.Filter = "Kodu files (*.Kodu2)|*.Kodu2";

                // Adds a extension if the user does not.
                DialogSave.AddExtension = true;

                // Restores the selected directory, next time this is activated.
                DialogSave.RestoreDirectory = true;

                if (level.PreviousLink() != null || level.NextLink() != null)
                {
                    // Dialog title.
                    DialogSave.Title = Strings.Localize("loadLevelMenu.exportMultiDialogTitle");
                }
                else
                {
                    // Dialog title.
                    DialogSave.Title = Strings.Localize("loadLevelMenu.exportDialogTitle");
                }

                // Startup directory
                DialogSave.InitialDirectory = @"c:/My Documents/Kodu";

                // Set the default filename.
                DialogSave.FileName = GenerateDefaultFileName(level, true);

                string result = null;

                // Show the dialog and process the result.
                if (DialogSave.ShowDialog() == DialogResult.OK)
                {
                    ExportLevel(level, DialogSave.FileName, null);
                    result = DialogSave.FileName;
                }

                DialogSave.Dispose();
                DialogSave = null;

                return result;
            }   // end of ShowExportDialog()
#endif

            private void Callback_ExportSelectedLevel(AsyncOperation op)
            {
                LevelMetadata level = op.Param0 as LevelMetadata;

                //Note: this code path didn't offer the user a chance to set the file name?  preserving this behaviour for now...
                string fileNameWithoutExtension = GenerateDefaultFileName(level, false);

                // If this filename is already in use, add a unique digit on the end.
#if NETFX_CORE
                string fileName = String.Format("{0}.kodu2", fileNameWithoutExtension);
#else
                string fileName = String.Format("{0}.kodu2", fileNameWithoutExtension);
#endif
                int rev = 1;
                while (true)
                {
#if NETFX_CORE
                    if (!Storage4.FileExists(LevelPackage.ExportsPath + fileName, StorageSource.UserSpace))
#else
                    if (!File.Exists(LevelPackage.ExportsPath + fileName))
#endif
                    {
                        break;
                    }

#if NETFX_CORE
                    fileName = String.Format("{0} ({1}).kodu2", fileNameWithoutExtension, rev);
#else
                    fileName = String.Format("{0} ({1}).kodu2", fileNameWithoutExtension, rev);
#endif
                    rev += 1;
                }

                ExportLevel(level, Path.Combine(LevelPackage.ExportsPath, fileName), null);

                InGame.EndMessage(parent.blockingOpMessage.Render, null);
            }

            //pre: assumes level is at the start of the chain
            //pre: assumes valid filename is passed in
            /// <summary>
            /// 
            /// </summary>
            /// <param name="level"></param>
            /// <param name="fileName"></param>
            /// <param name="outStream">May be null.  If not null this is used.  If this is null then fileName is used.</param>
            private void ExportLevel(LevelMetadata level, string fileName, Stream outStream)
            {
                //only the first level in a chain should ever make it this far 
                //(higher up, we determine it's a package and pass in the first level)
                Debug.Assert(level.LinkedFromLevel == null);

                //ensure valid filename
                Debug.Assert(fileName != null);

                List<string> levelFiles = new List<string>();
                List<string> stuffFiles = new List<string>();
                List<string> thumbnailFiles = new List<string>();
                List<string> screenshotFiles = new List<string>();
                List<string> terrainFiles = new List<string>();

                do
                {
                    string folderName = Utils.FolderNameFromFlags(level.Genres);

#if NETFX_CORE
                    string fullPathToLevelFile = Path.Combine(BokuGame.Settings.MediaPath, folderName, level.WorldId.ToString() + ".Xml");
#else
                    string fullPathToLevelFile = Path.Combine(Storage4.UserLocation, BokuGame.Settings.MediaPath, folderName, level.WorldId.ToString() + ".Xml");
#endif

#if EXPORT_DEBUG_HACK
                    {
                        string message = "Main File\n" +
                                        "UserLocation : " + Storage4.UserLocation + "\n" +
                                        "MediaPath : " + BokuGame.Settings.MediaPath + "\n" +
                                        "folderName : " + folderName + "\n" +
                                        "fileName : " + level.WorldId.ToString() + ".Xml\n\n" +
                                        "fullPath : " + fullPathToLevelFile;
                        var result = System.Windows.Forms.MessageBox.Show(message);
                    }
#endif

                    levelFiles.Add(fullPathToLevelFile);

                    //load the xml so we can find the stuff, thumbnail and terrain file paths
                    XmlWorldData xml = XmlWorldData.Load(fullPathToLevelFile, XnaStorageHelper.Instance);

#if !NETFX_CORE
                    if (xml == null)
                    {
                        string message = "Failed to open for export:\n" + fullPathToLevelFile;
                        var result = System.Windows.Forms.MessageBox.Show(message);

                        return;
                    }
#endif

#if NETFX_CORE
                    string fullPathToStuffFile = Path.Combine(BokuGame.Settings.MediaPath, xml.stuffFilename);
#else
                    string fullPathToStuffFile = Path.Combine(Storage4.UserLocation, BokuGame.Settings.MediaPath, xml.stuffFilename);
#endif

                    stuffFiles.Add(fullPathToStuffFile);

#if NETFX_CORE
                    string fullPathToThumbnailFile = Path.Combine(BokuGame.Settings.MediaPath, folderName, xml.GetImageFilenameWithoutExtension() + ".Dds");
#else
                    string fullPathToThumbnailFile = Path.Combine(Storage4.UserLocation, BokuGame.Settings.MediaPath, folderName, xml.GetImageFilenameWithoutExtension() + ".Dds");
#endif
                    thumbnailFiles.Add(fullPathToThumbnailFile);

#if NETFX_CORE
                    string fullPathToScreenshotFile = Path.Combine(BokuGame.Settings.MediaPath, folderName, xml.GetImageFilenameWithoutExtension() + ".Jpg");
#else
                    string fullPathToScreenshotFile = Path.Combine(Storage4.UserLocation, BokuGame.Settings.MediaPath, folderName, xml.GetImageFilenameWithoutExtension() + ".Jpg");
#endif
                    screenshotFiles.Add(fullPathToScreenshotFile);


                    // Only provide terrain file if it is not a builtin terrain.
                    // TODO Rethink this.  Maybe put terrain in every file.
                    string fullPathToTerrainFile = null;
                    string partialPathToTerrainFile = Path.Combine(BokuGame.Settings.MediaPath, xml.xmlTerrainData2.virtualMapFile);
                    if (Storage4.FileExists(partialPathToTerrainFile, StorageSource.UserSpace))
                    {
#if NETFX_CORE
                        fullPathToTerrainFile = partialPathToTerrainFile;
#else
                        fullPathToTerrainFile = Path.Combine(Storage4.UserLocation, partialPathToTerrainFile);
#endif
                    }
                    terrainFiles.Add(fullPathToTerrainFile);

                    //traverse down the chain to the next link, until finished
                    level = level.NextLink();

                } while (level != null);

                //we have gathered all of the file information, do the export
                LevelPackage.ExportLevel(
                    levelFiles,
                    stuffFiles,
                    thumbnailFiles,
                    screenshotFiles,
                    terrainFiles,
                    fileName,
                    outStream);
            }   // end of ExportLevel()


            private string GenerateDefaultFileName(LevelMetadata level, bool withExtension)
            {
                string fileName = "";
                if (level.PreviousLink()==null && level.NextLink()==null)
                {
                    fileName = LevelPackage.CreateExportFilenameWithoutExtension(level.Name, level.Creator);
                }
                else
                {
                    // TODO (****) I hate having [Package] prepended to the file names.  Why bother?
                    string packageName = Strings.Localize("loadLevelMenu.exportPackageString") + level.Name;
                    fileName = LevelPackage.CreateExportFilenameWithoutExtension(packageName, level.Creator);
                }

                if (withExtension)
                {
                    fileName += ".Kodu2";
                }

                return fileName;
            }

            private void CheckPlaySelectedLevel(bool bEditMode)
            {
                LevelMetadata level = parent.shared.CurWorld;

                //for downloads, make sure we're reading the full xml - linked level info won't be in metadata from the community server
                if (level != null && level.Genres == Genres.Downloads)
                {
                    level = XmlDataHelper.LoadMetadataByGenre(level.WorldId, Genres.Downloads);
                }

                if (level != null)
                {
                    LevelMetadata brokenLevel = null;
                    bool forwardLinkBroken = false;

                    //check if the level has a broken link
                    if (level.FindBrokenLink(ref brokenLevel, ref forwardLinkBroken))
                    {
                        parent.ShowBrokenLinkDialog( bEditMode );
                    }
                    //check if this isn't the first level in a list (edit mode always loads the level selected)
                    else if (level.LinkedFromLevel != null && !bEditMode)
                    {
                        parent.ShowLevelNotFirstDialog();
                    }
                    else
                    {
                        PlaySelectedLevel(false, bEditMode);
                    }
                }
            }

            internal void PlaySelectedLevel(bool playFirst, bool bEditMode)
            {
                LevelMetadata level = parent.shared.CurWorld;

                if (level != null)
                {
                    if (playFirst)
                    {
                        //if it's a community level, try to reload it to make sure we have the metadata for links
                        if (level.Browser is CommunityLevelBrowser &&
                            XmlDataHelper.CheckWorldExistsByGenre(level.WorldId, BokuShared.Genres.Downloads))
                        {
                            level = XmlDataHelper.LoadMetadataByGenre(level.WorldId, BokuShared.Genres.Downloads);
                        }
                        //use the first level in the chain
                        level = level.FindFirstLink();

                        //update the cursor/level grid to be pointing at the level we actually loaded
                        mainCursor.SetDesiredLevel(level.WorldId);
                        levelGrid.Reload(mainCursor);
                    }

                    PlayLevel(level, bEditMode);
                }
            }

            private void PlayLevel(LevelMetadata level, bool bEditMode)
            {
                if (level != null)
                {
                    // Put up "loading" message.
                    parent.blockingOpMessage.Text = Strings.Localize("loadLevelMenu.loadingLevelMessage");
                    InGame.AddMessage(parent.blockingOpMessage.Render, null);

                    // Shut down the grids.
                    levelGrid.Active = false;
                    bucketsGrid.Active = false;

                    // Save this off for use until we've rendered a frame
                    InGame.inGame.ThumbNail = ((UIGridLevelElement)levelGrid.SelectionElement).Thumbnail as Texture2D;

                    string folderName = Utils.FolderNameFromFlags(level.Genres);
                    string fullPath = BokuGame.Settings.MediaPath + folderName + level.WorldId.ToString() + @".Xml";

                    // Queue the play operation to happen in the next frame so that we can render the "please wait" message first.
                    FrameDelayedOperation op = null;

                    if (bEditMode)
                    {
                        op = new FrameDelayedOperation(Callback_EditSelectedLevel, fullPath, null);
                    }
                    else
                    {
                        op = new FrameDelayedOperation(Callback_PlaySelectedLevel, fullPath, null);
                    }

                    AsyncOps.Enqueue(op);
                }
            }

            public void Callback_EditSelectedLevel(AsyncOperation op)
            {
                InGame.EndMessage(parent.blockingOpMessage.Render, null);

                string fullPath = op.Param0 as string;

                if (BokuGame.bokuGame.inGame.LoadLevelAndRun(fullPath, keepPersistentScores: false, newWorld: false, andRun: false))
                {
                    parent.Deactivate();
                    InGame.inGame.Activate();
                }

                SceneManager.SwitchToScene("EditWorldScene");

                // If we were loading from an unproven string, our asynchronous attempt is now done,
                // whether we succeeded or failed.
                parent.LoadingFromString = false;
                Time.Paused = false;
            }

            public void Callback_PlaySelectedLevel(AsyncOperation op)
            {
                InGame.EndMessage(parent.blockingOpMessage.Render, null);

                string fullPath = op.Param0 as string;

                if (BokuGame.bokuGame.inGame.LoadLevelAndRun(fullPath, keepPersistentScores: false, newWorld: false, andRun: true))
                {
                    parent.Deactivate();
                }
                else
                {
                    SceneManager.SwitchToScene("RunSimScene");
                }

                // If we were loading from an unproven string, our asynchronous attempt is now done,
                // whether we succeeded or failed.
                parent.LoadingFromString = false;
                Time.Paused = false;
            }

            public void Callback_PlayLinkedLevel(AsyncOperation op)
            {
                InGame.EndMessage(parent.blockingOpMessage.Render, null);

                string fullPath = op.Param0 as string;

                if (BokuGame.bokuGame.inGame.LoadLevelAndRun(fullPath, keepPersistentScores: true, newWorld: false, andRun: true))
                {
                    parent.Deactivate();
                }
                else
                {
                    SceneManager.SwitchToScene("RunSimScene");
                }

                // If we were loading from an unproven string, our asynchronous attempt is now done,
                // whether we succeeded or failed.
                parent.LoadingFromString = false;
                Time.Paused = false;
            }

            public void PopupOnExport()
            {
                popup.Active = false;

                if (levelGrid.SelectionElement != null)
                {
                    string exportedFilename = ExportSelectedLevel();
                    if (exportedFilename != null)
                    {
                        // For WinRT we will show a dialog even though we are fullscreen.
#if !NETFX_CORE
                        // Only display message on fullscreen.  On windowed we 
                        // use the SaveDialog instead.
                        // TODO (****) *** Aren't we always windowed now?
                        //if (Boku.BokuGame.Graphics.IsFullScreen)
                        {
                            if (parent.shared.CurWorld.LinkedFromLevel != null || parent.shared.CurWorld.LinkedToLevel != null)
                            {
                                parent.ShowLinkedLevelExportedDialog();
                            }
                            else
                            {
                                parent.ShowLevelExportedDialog(exportedFilename);
                            }
                        }
#endif
                    }
                }
            }

            public void PopupOnCommunityShare()
            {
                popup.Active = false;
                // Acknowledges upload?
                communityShareMenu.Activate(parent.shared.CurWorld);

            }


            /// <summary>
            /// Video output hack.
            /// </summary>
            public void PopupOnVideoOut()
            {
                popup.Active = false;

                if (levelGrid.SelectionElement != null)
                {
                    string levelFilePath = null;
                    string stuffFilePath = null;
                    string thumbPath = null;
                    string terrainPath = null;

                    LevelMetadata level = parent.shared.CurWorld;

                    // If we're asking for Empty World, give all levels instead.
                    if (level.WorldId.ToString() == "dbe9e4ff-de06-49c0-8cc8-2251502dbd3d")
                    {
                        // Create a list of filenames including oth my files and downloaded files.
                        string folderName = Utils.FolderNameFromFlags(Genres.MyWorlds);
                        string[] myFilenames = Storage4.GetFiles("Content\\" + folderName, StorageSource.All);

                        folderName = Utils.FolderNameFromFlags(Genres.Downloads);
                        string[] downloadFilenames = Storage4.GetFiles("Content\\" + folderName, StorageSource.All);

                        string[] filenames = new string[myFilenames.Length + downloadFilenames.Length];

                        for (int i = 0; i < myFilenames.Length; i++)
                        {
                            filenames[i] = myFilenames[i];
                        }
                        for (int i = 0; i < downloadFilenames.Length; i++)
                        {
                            filenames[i + myFilenames.Length] = downloadFilenames[i];
                        }

                        // Files to pass to video out.
                        List<string> files = new List<string>();

                        if (filenames != null)
                        {
                            for (int i = 0; i < filenames.Length; i++)
                            {
                                // Always start with Xml files, ignore .Dds
                                if (filenames[i].EndsWith(".Xml"))
                                {
                                    try
                                    {
                                        levelFilePath = filenames[i].Substring(8);
                                        XmlWorldData xml = XmlWorldData.Load(filenames[i], XnaStorageHelper.Instance);

                                        stuffFilePath = xml.stuffFilename;

                                        thumbPath = folderName + xml.GetImageFilenameWithoutExtension() + ".Dds";

                                        terrainPath = xml.xmlTerrainData2.virtualMapFile;

                                        files.Add(levelFilePath);
                                        files.Add(stuffFilePath);
                                        files.Add(thumbPath);
                                        files.Add(terrainPath);
                                    }
                                    catch (Exception e)
                                    {
                                        // If we failed somewhere, ignore.
                                        if (e != null)
                                        {
                                        }
                                    }
                                }
                            }

                            // Shut down the LoadLevelMenu
                            parent.Deactivate();

                            // Kick off video output.
                            VideoOutput.Instance.Activate(files);
                        }
                    }
                    else
                    {
                        string folderName = Utils.FolderNameFromFlags(level.Genres);

                        levelFilePath = folderName + level.WorldId.ToString() + ".Xml";

                        if (Storage4.FileExists(BokuGame.Settings.MediaPath + levelFilePath, StorageSource.All))
                        {
                            // Shut down the LoadLevelMenu
                            parent.Deactivate();

                            XmlWorldData xml = XmlWorldData.Load(BokuGame.Settings.MediaPath + levelFilePath, XnaStorageHelper.Instance);

                            stuffFilePath = xml.stuffFilename;

                            thumbPath = folderName + xml.GetImageFilenameWithoutExtension() + ".Dds";

                            terrainPath = xml.xmlTerrainData2.virtualMapFile;

                            List<string> files = new List<string>();
                            files.Add(levelFilePath);
                            files.Add(stuffFilePath);
                            files.Add(thumbPath);
                            files.Add(terrainPath);

                            VideoOutput.Instance.Activate(files);

                        }   // end if level exists
                    }

                }
            }

            public void PopupOnPlay()
            {
                PopupOnPlayOrEdit(false);
            }

            public void PopupOnEdit()
            {
                PopupOnPlayOrEdit(true);
            }


            private void PopupOnPlayOrEdit(bool bEditMode)
            {
                popup.Active = false;

                if (levelGrid.SelectionElement != null)
                {
                    CheckPlaySelectedLevel( bEditMode );
                }
                else
                {
                    // SelectionElement is null which implies that the grid has not yet been populated so 
                    // don't let the user make a choice.  Note that they can still exit if they choose.

                    // Reactivate the grid and return.
                    levelGrid.Active = true;
                }

            }   // end of PopupOnPlay()

            internal void DeleteSelectedLevel(ModularMessageDialog dialog)
            {
                // User chose "delete"

                // Deactivate dialog.
                if (dialog != null)
                    dialog.Deactivate();

                // Activate "please wait" messaage
                parent.blockingOpMessage.Text = Strings.Localize("loadLevelMenu.deletingLevelMessage");
                InGame.AddMessage(parent.blockingOpMessage.Render, null);

                // Queue the delete operation to happen in the next frame so that we can render the "please wait" message first.
                FrameDelayedOperation op = new FrameDelayedOperation(Callback_DeleteSelectedLevel, null, null);
                AsyncOps.Enqueue(op);
            }

            private void Callback_DeleteSelectedLevel(AsyncOperation op)
            {
                InGame.EndMessage(parent.blockingOpMessage.Render, null);

                // Issue the delete command.
                if (parent.OriginalBrowserType == LevelBrowserType.Community)
                {
                    // Send command to delete level on Community site.
                    LevelMetadata info = parent.shared.CurWorld;
                    if (info != null)
                    {
                        // Double check that user is ok to delete.
                        if (Auth.IsValidCreatorChecksum(info.Checksum, info.LastSaveTime))
                        {
                            // Delete this world.
                            bool deleted = parent.updateObj.DeleteCurrentWorld();

                        }
                    }
                }
                else
                {
                    // Delete locally.
                    bool deleted = parent.updateObj.DeleteCurrentWorld();
                }

                // Reactivate the grid.
                parent.shared.levelGrid.Active = true;
            }

            internal void ReportAbuseSelectedLevel(ModularMessageDialog dialog)
            {
                // Deactivate dialog.
                if (dialog != null)
                {
                    dialog.Deactivate();
                }

                if (parent.shared.CurWorld.FlaggedByMe)
                {
                    // If already flagged, unflag.
                    // Send the report message.
                    Community.Async_FlagLevel(parent.shared.CurWorld.WorldId, false, null, null);

                    parent.shared.CurWorld.FlaggedByMe = false;
                }
                else
                {
                    // Send the report message.
                    Community.Async_FlagLevel(parent.shared.CurWorld.WorldId, true, null, null);

                    parent.shared.CurWorld.FlaggedByMe = true;
                }
            }


            public void PopupOnDelete()
            {
                popup.Active = false;

                if (parent.shared.isDeleteActive)
                {
                    if (parent.shared.CurWorld.PreviousLink() != null || parent.shared.CurWorld.NextLink() != null)
                    {
                        parent.ShowDeleteLinkedConfirmDialog();
                    }
                    else
                    {
                        parent.ShowDeleteConfirmDialog();
                    }
                }
            }   // end of PopupOnDelete()

            public void PopupOnDownload()
            {
                popup.Active = false;

                StartDownloadingWorld(parent.shared.CurWorld);
                ++parent.shared.CurWorld.Downloads;
            }   // end of PopupOnDownload()

            public void PopupOnChangeTags()
            {
                popup.Active = false;

                tagPicker.Active = true;
                tagPicker.SetTags((int)parent.shared.CurWorld.Genres);
            }   // end of PopupOnChangeTags()

            public void PopupOnReportAbuse()
            {
                popup.Active = false;

                if(parent.shared.CurWorld.FlaggedByMe)
                {
                    // Already flagged by me.  Just call the handler to unflag.  No need to confirmation.
                    ReportAbuseSelectedLevel(null);
                }
                else
                {
                    parent.ShowReportAbuseDialog();
                }

            }   // end of PopupOnReportAbuse()

            public void PopupOnLike()
            {
                popup.Active = false;
            }   // end of PopupOnLike()

            internal bool StartDownloadingWorld(LevelMetadata world)
            {
                bool started = remoteBrowser.StartDownloadingWorld(world, parent.WorldDownloadComplete);

                if (started)
                {
                    world.Genres |= Genres.Downloads;
                }

                return started;
            }

            //helper function that walks a level chain backwards to the first link, downloading links it finds missing
            internal bool StartDownloadToFirstLink(Guid worldId)
            {
                //early out if the level already exists
                if (XmlDataHelper.CheckWorldExistsByGenre(worldId, Genres.Downloads))
                {
                    LevelMetadata levelData = XmlDataHelper.LoadMetadataByGenre(worldId, Genres.Downloads);

                    if (levelData.LinkedFromLevel != null)
                    {
                        return StartDownloadToFirstLink((Guid)levelData.LinkedFromLevel);
                    }
                    else
                    {
                        //base case - no previous links, start walking forward
                        return StartDownloadToLastLink(worldId);
                    }
                }
                return remoteBrowser.StartDownloadingOffPageWorld(worldId, parent.BackwardLinkDownloadComplete);
            }

            //helper function that walks a level chain forwards to the last link, downloading links it finds missing
            internal bool StartDownloadToLastLink(Guid worldId)
            {
                //early out if the level already exists
                if (XmlDataHelper.CheckWorldExistsByGenre(worldId, Genres.Downloads))
                {
                    LevelMetadata levelData = XmlDataHelper.LoadMetadataByGenre(worldId, Genres.Downloads);

                    if (levelData.LinkedToLevel != null)
                    {
                        return StartDownloadToLastLink((Guid)levelData.LinkedToLevel);
                    }
                    else
                    {
                        parent.ShowSimpleDialog("loadLevelMenu.confirmLinkedDownloadMessage");
                        //base case - we're done! at the last link while walking forward
                        return true;
                    }
                }
                return remoteBrowser.StartDownloadingOffPageWorld(worldId, parent.ForwardLinkDownloadComplete);
            }

            public void ListOnExit(ModularCheckboxList list)
            {
                if (list == sortList)
                {
                    int i = sortList.GetIndex(Strings.Localize("loadLevelMenu.sortDate"));
                    if (i != -1 && sortList.GetItem(i).Check)
                    {
                        // If already using this sort, toggle the direction.
                        if (levelSorter.SortBy == SortBy.Date)
                        {
                            levelSorter.SortDirection = levelSorter.SortDirection == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
                        }
                        else
                        {
                            levelSorter.SortDirection = SortDirection.Descending;
                        }
                        levelSorter.SortBy = SortBy.Date;
                        sortListDisplay = Strings.Localize("loadLevelMenu.sortDate");
                    }

                    i = sortList.GetIndex(Strings.Localize("loadLevelMenu.sortCreator"));
                    if (i != -1 && sortList.GetItem(i).Check)
                    {
                        // If already using this sort, toggle the direction.
                        if (levelSorter.SortBy == SortBy.Creator)
                        {
                            levelSorter.SortDirection = levelSorter.SortDirection == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
                        }
                        else
                        {
                            levelSorter.SortDirection = SortDirection.Ascending;
                        }
                        levelSorter.SortBy = SortBy.Creator;
                        sortListDisplay = Strings.Localize("loadLevelMenu.sortCreator");
                    }

                    i = sortList.GetIndex(Strings.Localize("loadLevelMenu.sortTitle"));
                    if (i != -1 && sortList.GetItem(i).Check)
                    {
                        // If already using this sort, toggle the direction.
                        if (levelSorter.SortBy == SortBy.Name)
                        {
                            levelSorter.SortDirection = levelSorter.SortDirection == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
                        }
                        else
                        {
                            levelSorter.SortDirection = SortDirection.Ascending;
                        }
                        levelSorter.SortBy = SortBy.Name;
                        sortListDisplay = Strings.Localize("loadLevelMenu.sortTitle");
                    }

                    i = sortList.GetIndex(Strings.Localize("loadLevelMenu.sortRank"));
                    if (i != -1 && sortList.GetItem(i).Check)
                    {
                        // If already using this sort, toggle the direction.
                        if (levelSorter.SortBy == SortBy.Rank)
                        {
                            levelSorter.SortDirection = levelSorter.SortDirection == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
                        }
                        else
                        {
                            levelSorter.SortDirection = SortDirection.Ascending;
                        }
                        levelSorter.SortBy = SortBy.Rank;
                        sortListDisplay = Strings.Localize("loadLevelMenu.sortRank");
                    }

                    list.Deactivate();
                }

                if (levelFilter.Dirty || levelSorter.Dirty)
                {
                    if (parent.OriginalBrowserType == LevelBrowserType.Community)
                    {
                        // The community browser can't pivot on the current selection when the query changes.
                        mainBrowser.Reset();
                    }
                }

            }   // end of ListOnExit

            public void AuxOnCancel(UIGrid grid)
            {
                // Shut down the grid.
                grid.Active = false;

                SetAuxMenuDefaultSelections();
            }   // end of AuxOnCancel()


            public void StartFetchingThumbnails(ILevelSetCursor cursor)
            {
                // Most recent thumbnail requests are serviced first, so walk from left
                // to right from the outer edges toward the selected level, requesting
                // thumbnails for each. Thumbnails should come back in roughly the opposite
                // order they were requested, with some variation due to the asynchronous
                // nature of the transaction.
                int min = -LoadLevelMenuUIGrid.kFront;
                int max = LoadLevelMenuUIGrid.kWidth - LoadLevelMenuUIGrid.kFront;

                int low = -30;
                int high = 30;

                while (low < high)
                {
                    low += 1;
                    high -= 1;

                    if (low >= min)
                        cursor.Browser.StartDownloadingThumbnail(cursor[low], GotThumbnail, false);
                    if (high <= max)
                        cursor.Browser.StartDownloadingThumbnail(cursor[high], GotThumbnail, false);
                }
            }

            private void GotThumbnail(LevelMetadata level)
            {
                for (int i = 0; i < LoadLevelMenuUIGrid.kWidth; ++i)
                {
                    UIGridLevelElement elem = parent.shared.levelGrid.Get(i);
                    if (elem != null && elem.Level == level)
                    {
                        elem.Dirty = true;
                        break;
                    }
                }
            }

            public void LevelAdded(ILevelSetCursor cursor, int index)
            {
                StartFetchingThumbnails(cursor);

                // Adjust UI if this is the visible browser.
                if (cursor.Browser == mainBrowser)
                {
                    levelGrid.SplitAt(index);
                    levelGrid.Reload(cursor);
                }
            }

            public void LevelRemoved(ILevelSetCursor cursor, int index)
            {
                // Adjust UI if this is the visible browser.
                if (cursor.Browser == mainBrowser)
                {
                    if (index == int.MaxValue)
                    {
                        // int.MaxValue is a special value indicating the entire query has been cleared.
                        levelGrid.UnloadInstanceContent();
                    }
                    else
                    {
                        levelGrid.Remove(index);
                        levelGrid.Reload(cursor);
                    }
                }
            }

            public void CursorShifted(ILevelSetCursor cursor, int desired, int actual)
            {
                StartFetchingThumbnails(cursor);

                if (cursor.Browser == mainBrowser)
                {
                    levelGrid.Shift(actual);
                    levelGrid.Reload(cursor);
                    scrollOpCount -= 1;
                }
            }

            public void CursorJumped(ILevelSetCursor cursor)
            {
                StartFetchingThumbnails(cursor);

                if (cursor.Browser == mainBrowser)
                {
                    levelGrid.Reload(cursor);
                    scrollOpCount -= 1;
                }
            }

            public void LoadContent(bool immediate)
            {
                BokuGame.Load(levelGrid, immediate);
                BokuGame.Load(bucketsGrid, immediate);
                BokuGame.Load(sortList, immediate);
                BokuGame.Load(popup, immediate);
                BokuGame.Load(tagPicker, immediate);
                BokuGame.Load(textLineEditor, immediate);

            }   // end of LoadLevelMenu Shared LoadContent()

            public void InitDeviceResources(GraphicsDevice device)
            {
                if (mainCursor != null)
                    levelGrid.Reload(mainCursor);
                tagPicker.InitDeviceResources(device);
                textLineEditor.InitDeviceResources(device);

            }

            public void UnloadContent()
            {
                BokuGame.Unload(levelGrid);
                BokuGame.Unload(bucketsGrid);
                BokuGame.Unload(sortList);
                BokuGame.Unload(popup);
                BokuGame.Unload(tagPicker);
                BokuGame.Unload(textLineEditor);
            }   // end of LoadLevelMenu Shared UnloadContent()

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
                BokuGame.DeviceReset(levelGrid, device);
                BokuGame.DeviceReset(bucketsGrid, device);
                BokuGame.DeviceReset(sortList, device);
                BokuGame.DeviceReset(popup, device);
                BokuGame.DeviceReset(tagPicker, device);
                BokuGame.DeviceReset(textLineEditor, device);
            }

            #endregion

        }   // end of class Shared

        protected class UpdateObj : UpdateObject
        {
            #region Members

            private LoadLevelMenu parent = null;
            private Shared shared = null;

            #endregion

            #region Public

            public UpdateObj(LoadLevelMenu parent, Shared shared)
            {
                this.parent = parent;
                this.shared = shared;
            }

            public override void Update()
            {
#if PRESENCE_DEBUG
                if (parent.OriginalBrowserType == LevelBrowserType.Sharing)
                {
                    parent.FeedFakeSharingEvents();
                }
#endif
                // We need to do this ever frame instead of just at activation 
                // time since deactivation of the previous scene and activation 
                // of this scene don't always happen in that order.
                AuthUI.ShowStatusDialog(DialogManagerX.CurrentFocusDialogCamera);

                shared.dialogCamera.Resolution = new Point((int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);
                shared.dialogCamera.Update();

                parent.sharingCloseSessionConfirmMessage.Update();
                parent.sharingLeaveSessionConfirmMessage.Update();
                parent.sharingSessionEndedMessage.Update();

                if (shared.mainBrowser != null)
                {
                    shared.mainBrowser.Update();
                }

                if (shared.altBrowser != null)
                {
                    shared.altBrowser.Update();
                }

                // Don't update level grid if modal dialog is showing.
                if (ModularMessageDialogManager.Instance.IsDialogActive())
                    return;
                if (parent.sharingCloseSessionConfirmMessage.Active)
                    return;
                if (parent.sharingLeaveSessionConfirmMessage.Active)
                    return;
                if (parent.sharingSessionEndedMessage.Active)
                    return;

                if (shared.communityShareMenu.Active)
                {
                    shared.communityShareMenu.Update();
                    return;
                }

                if (AuthUI.IsModalActive)
                {
                    return;
                }

                LevelMetadata info = shared.CurWorld;

                HandleGamepadInput();
                HandleMouseInput();
                HandleTouchInput();

                // Note that no input goes to the levelGrid while the popup (or the sort list) is active.
                // TODO (****) Should this be rethought to send input to those items first instead of 
                // updating the main list first?
                if (shared.levelGrid != null && shared.popup.Active == false && shared.sortList.Active == false && parent.pendingState != States.Inactive)
                {
                    Matrix world = Matrix.Identity;
                    shared.levelGrid.Update(ref world);
                }

                if (shared.textLineEditor.Active)
                {
                    shared.textLineEditor.Update();
                    if (shared.textLineEditor.GetSecondsSinceLastKeypress() > 1.0
                        && shared.levelFilter.SearchString != shared.textLineEditor.GetText())
                    {
                        var text = shared.textLineEditor.GetText();
                        Instrumentation.RecordEvent(Instrumentation.EventId.SearchLevels, text);
                        shared.levelFilter.SearchString = text;// shared.textLineEditor.GetText();
                        shared.mainBrowser.Reset();
                    }

                    // The level info comes in asynchronously.  So check 
                    // it and split the description text when it arrives.
                    SplitText();

                    return; //no further updates
                }

                // If we're not shutting down, update the tabs and the child grids.
                if (parent.pendingState != States.Inactive)
                {
                    Matrix world = Matrix.Identity;

                    // Need to update the lists before the grids otherwise 
                    // the grid will steal input.
                    if (shared.sortList != null)
                    {
                        shared.sortList.Update(shared.camera, ref world);
                    }
                    if (shared.popup != null)
                    {
                        shared.popup.Update(shared.camera, ref world);
                    }
                    if (shared.tagPicker != null)
                    {
                        shared.tagPicker.Update(shared.camera, ref world);
                    }

                    if (shared.bucketsGrid != null)
                    {
                        shared.bucketsGrid.Update(ref world);

                        // Mouse input.  Buckets don't get any mouse input if tagPicker or lists are active.
                        bool tagPickerActive = shared.tagPicker != null && shared.tagPicker.Active;
                        bool sortListActive = shared.sortList != null && shared.sortList.Active;
                        if (!tagPickerActive && !sortListActive)
                        {
                            // Click on a category, make that the focus.
                            for (int i = 0; i < shared.bucketsGrid.ActualDimensions.X; i++)
                            {
                                UIGridElement e = shared.bucketsGrid.Get(i, 0);

                                // No need to reselect the already selected group.
                                if (e == shared.bucketsGrid.SelectionElement)
                                    continue;

                                Matrix mat = Matrix.Invert(e.WorldMatrix);
                                Vector2 hitUV = LowLevelMouseInput.GetHitUV(shared.camera, ref mat, e.Size.X, e.Size.Y, true);

                                if (hitUV.X >= 0 && hitUV.X < 1 && hitUV.Y >= 0 && hitUV.Y < 1)
                                {
                                    if (LowLevelMouseInput.Left.WasPressed)
                                    {
                                        MouseInput.ClickedOnObject = e;
                                    }
                                    if (LowLevelMouseInput.Left.WasReleased && MouseInput.ClickedOnObject == e)
                                    {
                                        // We hit an element, so bring it into focus.
                                        shared.bucketsGrid.SelectionIndex = new Point(i, 0);
                                    }
                                }
                            }

                            // Click on arrows at end, inc/dec focus.
                            {
                                Vector2 hit = LowLevelMouseInput.GetMouseInRtCoords();

                                if (shared.leftBumperBox.LeftPressed(hit))
                                {
                                    shared.bucketsGrid.MoveLeft();
                                }
                                if (shared.rightBumperBox.LeftPressed(hit))
                                {
                                    shared.bucketsGrid.MoveRight();
                                }
                            }
                        }

                        // If we're on the Sharing page, see if we need to switch browser types.  We tell we're
                        // on the sharing page since altBrowser is not null.
                        if (shared.altBrowser != null)
                        {
                            // If the current selection is Sharing then use the sharing browser, else use the local browser.
                            if (shared.bucketsGrid.SelectionElement.Label == Strings.Localize("loadLevelMenu.showSharing"))
                            {
                                // Swap browser and cursor.
                                ILevelBrowser tmpBrowser = shared.mainBrowser;
                                shared.mainBrowser = shared.altBrowser;
                                shared.altBrowser = tmpBrowser;
                                ILevelSetCursor tmpCursor = shared.mainCursor;
                                shared.mainCursor = shared.altCursor;
                                shared.altCursor = tmpCursor;
                                parent.CurrentBrowserType = LevelBrowserType.Sharing;
                            }
                        }
                        ApplyBucketFiltering();
                    }
                    //                    if (shared.levelGrid != null)
                    //                    {
                    //                        shared.levelGrid.Update(ref world);
                    //                    }

                    // Look at the world that is currently in focus and set 
                    // the appropriate shared bools so we know where we are.
                    // Start by turning off everything so we've got a known state.
                    shared.isDownload = false;
                    shared.isMyWorld = false;
                    shared.isBuiltInWorld = false;

                    // Get user name.
                    shared.userName = Auth.CreatorName;
                    shared.isUserAdmin = Boku.Web.Community.UserLevel == UserLevel.DomainAdmin ||
                                        Boku.Web.Community.UserLevel == UserLevel.GlobalAdmin;

                    if (info != null)
                    {
                        shared.isMyWorld = (info.Genres & Genres.MyWorlds) != 0;
                        shared.isBuiltInWorld = (info.Genres & Genres.BuiltInWorlds) != 0;
                        shared.isDownload = (info.Genres & Genres.Downloads) != 0;

                        // Users may delete their own worlds or worlds they've downloaded.  
                        shared.isDeleteActive =
                            //(info.Creator == shared.userName && shared.isMyWorld) ||
                            shared.isMyWorld ||
                            // Only allow deleting of downloads from LoadLevelMenu, not Community.
                            (shared.isDownload && parent.CurrentBrowserType != LevelBrowserType.Community);

                        // If in Community page, users may also delete their own worlds.
                        if (parent.CurrentBrowserType == LevelBrowserType.Community)
                        {
                            // Matching id?  Guest not allowed.
                            if (info.Creator != Auth.DefaultCreatorName && Auth.IsValidCreatorChecksum(info.Checksum, info.LastSaveTime))
                            {
                                shared.isDeleteActive = true;
                            }
                        }

                        // Cannot delete levels from sharing sessions.
                        shared.isDeleteActive &= (parent.CurrentBrowserType != LevelBrowserType.Sharing);
                    }

                    // The level info comes in asynchronously.  So check 
                    // it and split the description text when it arrives.
                    SplitText();

                }   // end if not shutting down.

            }   // end of Update()

            public bool LevelGridFocused()
            {
                return (!shared.sortList.Active && !shared.popup.Active &&
                        !shared.tagPicker.Active && !shared.textLineEditor.Active);
            }

            private void HandleGamepadInput()
            {
                if (LevelGridFocused())
                {
                    // Our children have input focus but we can still steal away the buttons we care about.
                    GamePadInput pad = GamePadInput.GetGamePad0();

                    if (Actions.Select.WasPressed)
                    {
                        Actions.Select.ClearAllWasPressedState();

                        if (!shared.levelGrid.NoValidLevels)
                        {
                            shared.SetUpPopup(true);
                            if (shared.popup.NumItems > 0)
                            {
                                shared.popup.Active = true;
                                Foley.PlayPressA();
                            }
                        }
                    }
                    else if (Actions.Cancel.WasPressed)
                    {
                        Actions.Cancel.ClearAllWasPressedState();
                        parent.ReturnToPreviousMenu();
                    }
                    // Y -> activate SortBy menu.
                    if (Actions.SortBy.WasPressed)
                    {
                        Actions.SortBy.ClearAllWasPressedState();
                        shared.sortList.Activate(useRtCoords: true);
                        return;
                    }
                }
            }

            public void HandleMouseInput()
            {
                if (!KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
                {
                    //Defocus search box if not in KeyboardMouse mode
                    if (shared.textLineEditor.Active)
                    {
                        shared.textLineEditor.Deactivate();
                        shared.levelGrid.IgnoreInput = false;
                        shared.levelGrid.Active = true;
                    }

                    return;
                }

                Vector2 hit = LowLevelMouseInput.GetMouseInRtCoords();

                //Focus or defocus search box
                shared.fullScreenHitBox.Set(Vector2.Zero,BokuGame.ScreenSize);
                if (LowLevelMouseInput.Left.WasPressed && shared.searchBox.Contains(hit))
                {
                    //make sure popups are deactived
                    shared.sortList.Deactivate();
                    shared.popup.Active = false;
                    TextLineEditor.OnEditDone callback = delegate(bool canceled, string newText)
                    {
                        if (!canceled && shared.levelFilter.SearchString != newText)
                        {
                            Instrumentation.RecordEvent(Instrumentation.EventId.SearchLevels, newText);

                            shared.levelFilter.SearchString = newText;
                            shared.mainBrowser.Reset();
                        }
                        shared.textLineEditor.Deactivate();
                        shared.levelGrid.IgnoreInput = false;
                        shared.levelGrid.Active = true;
                    };
                    //Focus on search box
                    shared.textLineEditor.Activate(callback, shared.levelFilter.SearchString);
                    shared.levelGrid.IgnoreInput = true;
                }
                else if (shared.textLineEditor.Active)
                {
                    //Deactivate if click outside search box
                    if (LowLevelMouseInput.Left.WasPressed && shared.fullScreenHitBox.Contains(hit))
                    {
                        shared.textLineEditor.Deactivate();
                        shared.levelGrid.IgnoreInput = false;
                        shared.levelGrid.Active = true;
                    }
                }

                if (LevelGridFocused())
                {
                    // Mouse input on grid.
                    // Hit the in-focus tile, then open popup.
                    // Hit another tile, then bring that one to focus.  Note because of overlap of
                    // the tiles we should do this center-out.

                    // In-Focus tile.
                    UIGridElement e = shared.levelGrid.SelectionElement;
                    if (e != null)
                    {
                        Matrix invWorld = Matrix.Invert(e.WorldMatrix);
                        Vector2 hitUV = LowLevelMouseInput.GetHitUV(shared.camera, ref invWorld, e.Size.X, e.Size.Y, true);

                        if (hitUV.X > 0 && hitUV.X < 1 && hitUV.Y > 0 && hitUV.Y < 1)
                        {
                            if (LowLevelMouseInput.Left.WasPressed)
                            {
                                MouseInput.ClickedOnObject = e;
                            }
                            if (LowLevelMouseInput.Left.WasReleased && MouseInput.ClickedOnObject == e)
                            {
                                if (!shared.levelGrid.NoValidLevels)
                                {
                                    shared.SetUpPopup(true);
                                    if (shared.popup.NumItems > 0)
                                    {
                                        shared.popup.Active = true;
                                        Foley.PlayPressA();
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Check non-focus tiles.
                            bool moved = false;
                            // Left side first.
                            for (int i = shared.levelGrid.SelectionIndex.X - 1; i >= 0 && !moved; i--)
                            {
                                e = shared.levelGrid.Get(i, 0);
                                if (e != null)
                                {
                                    invWorld = Matrix.Invert(e.WorldMatrix);
                                    hitUV = LowLevelMouseInput.GetHitUV(shared.camera, ref invWorld, e.Size.X, e.Size.Y, true);

                                    if (hitUV.X > 0 && hitUV.X < 1 && hitUV.Y > 0 && hitUV.Y < 1)
                                    {
                                        if (LowLevelMouseInput.Left.WasPressed)
                                        {
                                            MouseInput.ClickedOnObject = e;
                                        }
                                        if (LowLevelMouseInput.Left.WasReleased && MouseInput.ClickedOnObject == e)
                                        {
                                            int steps = shared.levelGrid.SelectionIndex.X - i;
                                            while (steps > 0)
                                            {
                                                shared.levelGrid.MoveLeft();
                                                --steps;
                                            }
                                        }
                                        // Found a hit, no need to continue.
                                        moved = true;
                                    }
                                }
                            }   // end of loop over element on left side of focus.

                            // Right side
                            for (int i = shared.levelGrid.SelectionIndex.X + 1;
                                 i < shared.levelGrid.ActualDimensions.X && !moved; i++)
                            {
                                e = shared.levelGrid.Get(i, 0);
                                if (e != null)
                                {
                                    invWorld = Matrix.Invert(e.WorldMatrix);
                                    hitUV = LowLevelMouseInput.GetHitUV(shared.camera, ref invWorld, e.Size.X, e.Size.Y, true);

                                    if (hitUV.X > 0 && hitUV.X < 1 && hitUV.Y > 0 && hitUV.Y < 1)
                                    {
                                        if (LowLevelMouseInput.Left.WasPressed)
                                        {
                                            MouseInput.ClickedOnObject = e;
                                        }
                                        if (LowLevelMouseInput.Left.WasReleased && MouseInput.ClickedOnObject == e)
                                        {
                                            int steps = i - shared.levelGrid.SelectionIndex.X;
                                            while (steps > 0)
                                            {
                                                shared.levelGrid.MoveRight();
                                                --steps;
                                            }
                                        }
                                        // Found a hit, no need to continue.
                                        moved = true;
                                    }
                                }
                            }   // end of loop over element on right side of focus.
                        }   // end of check over non-focus tiles

                        // Check for edges of screen.
                        if (LowLevelMouseInput.AtWindowLeft())
                        {
                            shared.levelGrid.MoveLeft();
                        }
                        if (LowLevelMouseInput.AtWindowRight())
                        {
                            shared.levelGrid.MoveRight();
                        }

                        // Check for clicking on scroll arrows.
                        if (shared.arrowLeftBox.LeftPressed(hit))
                        {
                            shared.levelGrid.MoveLeft();
                        }
                        if (shared.arrowRightBox.LeftPressed(hit))
                        {
                            shared.levelGrid.MoveRight();
                        }


                    }

                    // Mouse input for activating show or sort menus.
                    bool activateSort = false;

                    if (shared.sortBox.LeftPressed(hit))
                    {
                        activateSort = true;
                    }
                    if (activateSort)
                    {
                        Actions.SortBy.ClearAllWasPressedState();
                        shared.sortList.Activate(useRtCoords: true);
                    }

                    // Handle buckets grid
                    // Click on a category, make that the focus.
                    for (int i = 0; i < shared.bucketsGrid.ActualDimensions.X; i++)
                    {
                        e = shared.bucketsGrid.Get(i, 0);

                        // No need to reselect the already selected group.
                        if (e == shared.bucketsGrid.SelectionElement)
                        {
                            continue;
                        }

                        Matrix mat = Matrix.Invert(e.WorldMatrix);
                        Vector2 hitUV = LowLevelMouseInput.GetHitUV(shared.camera, ref mat, e.Size.X, e.Size.Y, true);
                        if (hitUV.X >= 0 && hitUV.X < 1 && hitUV.Y >= 0 && hitUV.Y < 1)
                        {
                            if (LowLevelMouseInput.Left.WasPressed)
                            {
                                MouseInput.ClickedOnObject = e;
                            }
                            if (LowLevelMouseInput.Left.WasReleased && MouseInput.ClickedOnObject == e)
                            {
                                // We hit an element, so bring it into focus.
                                shared.bucketsGrid.SelectionIndex = new Point(i, 0);
                                shared.mainBrowser.Reset();
                            }
                        }

                        if (shared.leftBumperBox.LeftPressed(hit))
                        {
                            shared.bucketsGrid.MoveLeft();
                        }
                        if (shared.rightBumperBox.LeftPressed(hit))
                        {
                            shared.bucketsGrid.MoveRight();
                        }
                    }
                }

                if (shared.popup.Active)
                {
                    if (shared.backBox.LeftPressed(hit))
                    {
                        shared.popup.Active = false;
                    }
                }



                // Check for hover and adjust text color to match.
                Color newColor;



                newColor = shared.sortBox.Contains(hit) ? shared.hoverTextColor : shared.lightTextColor;
                if (newColor != shared.sortTargetColor)
                {
                    shared.sortTargetColor = newColor;
                    Vector3 curColor = new Vector3(shared.sortColor.R / 255.0f,
                        shared.sortColor.G / 255.0f, shared.sortColor.B / 255.0f);
                    Vector3 destColor = new Vector3(newColor.R / 255.0f,
                        newColor.G / 255.0f, newColor.B / 255.0f);

                    TwitchManager.Set<Vector3> set = delegate(Vector3 value, Object param)
                    {
                        shared.sortColor.R = (byte)(value.X * 255.0f + 0.5f);
                        shared.sortColor.G = (byte)(value.Y * 255.0f + 0.5f);
                        shared.sortColor.B = (byte)(value.Z * 255.0f + 0.5f);
                    };
                    TwitchManager.CreateTwitch<Vector3>(curColor, destColor, set, 0.1f,
                        TwitchCurve.Shape.EaseOut);
                }

#if !HIDE_LIKES
                if (shared.likesBox.LeftPressed(hit))
                {
#if NETFX_CORE
                    Debug.Assert(false, "Figure out how to launch IE from a URL in WinRT.");
#else
                    try
                    {
                        shared.PopupOnLike();
                    }
                    catch { }
#endif
                }

                if (shared.commentsBox.LeftPressed(hit))
                {
                    try
                    {
#if NETFX_CORE
                        Launcher.LaunchUriAsync(new Uri(shared.CurWorld.Permalink));
#else
                        shared.PopupOnComments();
#endif
                    }
                    catch { }
                }
#endif

                if (shared.downloadsBox.LeftPressed(hit))
                {
                    try
                    {
#if NETFX_CORE
                        Launcher.LaunchUriAsync(new Uri(shared.CurWorld.Permalink));
#else
                        shared.PopupOnDownload();
#endif
                    }
                    catch { }
                }

                // Finally, see if user clicked on exit text at bottom of screen.
                if (LowLevelMouseInput.Left.WasPressed)
                {
                    Point mousePos = new Point(LowLevelMouseInput.Position.X - (int)BokuGame.ScreenPosition.X, LowLevelMouseInput.Position.Y - (int)BokuGame.ScreenPosition.Y);
                    if (HelpOverlay.MouseHitBottomText(mousePos))
                    {
                        parent.ReturnToPreviousMenu();
                    }
                }

            }   // end of HandleMouseInput()

            private bool TouchedLevelIndex(int levelIndex)
            {
                TouchContact touch = TouchInput.GetOldestTouch();
                if (touch == null) { return false; }

                UIGridElement element = shared.levelGrid.Get(levelIndex);
                if (element == null) { return false; }

                Matrix invWorld = Matrix.Invert(element.WorldMatrix);
                Vector2 touchHitUV = TouchInput.GetHitUV(
                    touch.position,
                    shared.camera,
                    ref invWorld,
                    element.Size.X,
                    element.Size.Y,
                    true
                );

                if ((touchHitUV.X > 0) && (touchHitUV.X < 1) &&
                    (touchHitUV.Y > 0) && (touchHitUV.Y < 1))
                {
                    return true;
                }

                return false;
            }

            private int GetLevelIndexFromTouch()
            {
                TouchContact touch = TouchInput.GetOldestTouch();
                if (touch == null) { return -1; }

                UIGridElement focusElement = shared.levelGrid.SelectionElement;
                if (focusElement == null)
                {
                    return -1;
                }

                Vector2 touchHitUV = Vector2.Zero;

                // We need to perform the hit tests in a specific order due to the overlapping
                // nature of the grid's display. The order we'll use is:
                // - focus element
                // - left neighbor of focus to the far left
                // - right neighbor of focus to the far right
                if (TouchedLevelIndex(shared.levelGrid.SelectionIndex.X))
                {
                    return shared.levelGrid.SelectionIndex.X;
                }
                for (int i = shared.levelGrid.SelectionIndex.X - 1; i >= 0; i--)
                {
                    if (TouchedLevelIndex(i))
                    {
                        return i;
                    }
                }
                for (int i = shared.levelGrid.SelectionIndex.X + 1; i < shared.levelGrid.ActualDimensions.X; i++)
                {
                    if (TouchedLevelIndex(i))
                    {
                        return i;
                    }
                }
                return -1;
            }

            private void HandleBucketGridTouch()
            {
                if (TouchInput.TouchCount == 0) { return; } // nothing to see here.
                TouchContact touch = TouchInput.GetOldestTouch();

                for (int i = 0; i < shared.bucketsGrid.ActualDimensions.X; i++)
                {
                    UIGridElement element = shared.bucketsGrid.Get(i, 0);
                    if (element == null) { continue; }

                    Matrix invMatrix = Matrix.Invert(element.WorldMatrix);

                    Vector2 touchHitUV = TouchInput.GetHitUV(
                                        touch.position,
                                        shared.camera,
                                        ref invMatrix,
                                        element.Size.X,
                                        element.Size.Y,
                                        true);

                    if ((touchHitUV.X >= 0) && (touchHitUV.X < 1) &&
                        (touchHitUV.Y >= 0) && (touchHitUV.Y < 1))
                    {
                        if (touch.phase == TouchPhase.Began)
                        {
                            touch.TouchedObject = element;
                        }
                        else if (touch.phase == TouchPhase.Ended)
                        {
                            if (touch.TouchedObject == element)
                            {
                                shared.bucketsGrid.SelectionIndex = new Point(i, 0);
                                touch.TouchedObject = null;
                                shared.mainBrowser.Reset();
                                return;
                            }
                        }
                    }
                }
            }

            private void HandleTouchInput()
            {
                if (TouchInput.TouchCount == 0) { return; } // nothing to see here.

                TouchContact touch = TouchInput.GetOldestTouch();

                Vector2 touchHit = ScreenWarp.ScreenToRT(touch.position);

                if (TouchGestureManager.Get().TapGesture.WasTapped())
                {
                    //Focus or defocus search box
                    shared.fullScreenHitBox.Set(Vector2.Zero, BokuGame.ScreenSize);
                    if (shared.searchBox.Contains(touchHit))
                    {
                        //make sure popups are deactived
                        shared.sortList.Deactivate();
                        shared.popup.Active = false;
                        KeyboardInputX.ShowOnScreenKeyboard();
                        TextLineEditor.OnEditDone callback = delegate(bool canceled, string newText)
                        {
                            if (!canceled && shared.levelFilter.SearchString != newText)
                            {
                                Instrumentation.RecordEvent(Instrumentation.EventId.SearchLevels, newText);
                                shared.levelFilter.SearchString = newText;
                                shared.mainBrowser.Reset();
                            }
                            //KeyboardInputX
                            shared.textLineEditor.Deactivate();
                            shared.levelGrid.IgnoreInput = false;
                            shared.levelGrid.Active = true;
                        };
                        //Focus on search box
                        shared.textLineEditor.Activate(callback, shared.levelFilter.SearchString);
                        shared.levelGrid.IgnoreInput = true;
                    }
                    else if (shared.textLineEditor.Active)
                    {
                        //Deactivate if click outside search box
                        if (shared.fullScreenHitBox.Contains(touchHit))
                        {
                            shared.textLineEditor.Deactivate();
                            shared.levelGrid.IgnoreInput = false;
                            shared.levelGrid.Active = true;
                        }
                    }
                }

                if (!LevelGridFocused()) { return; } // input not handled here.

                // Check level grid elements for hits, accounting for overlapping level hit boxes.
                int levelIndex = GetLevelIndexFromTouch();

                if (levelIndex == -1)
                {
                    // Didn't hit any level grid elements.. check menu boxes
                    if (shared.sortBox.Touched(touch, touchHit))
                    {
                        shared.sortList.Activate(useRtCoords: true);
                    }
                    else
                    {
                        // Check bucket grid
                        HandleBucketGridTouch();
                    }
                }
                else
                {
                    //// hit a level!
                    UIGridElement levelElement = shared.levelGrid.Get(levelIndex, 0);
                    if (touch.phase == TouchPhase.Began)
                    {
                        touch.TouchedObject = levelElement;
                    }
                    else if (touch.phase == TouchPhase.Ended)
                    {
                        if (TouchGestureManager.Get().TapGesture.WasTapped())
                        {
                            // Note: Only a tap-ending will trigger these behaviors.
                            if (touch.TouchedObject == levelElement)
                            {
                                if (!shared.levelGrid.NoValidLevels)
                                {
                                    if (levelElement == shared.levelGrid.SelectionElement)
                                    {
                                        // Hit the focus level
                                        touch.TouchedObject = shared.popup;
                                        shared.popup.Active = true;
                                        Foley.PlayPressA();
                                    }
                                    else
                                    {
                                        // Hit non-focus level. Make it the focus
                                        int numSteps = levelIndex - shared.levelGrid.SelectionIndex.X;
                                        if (numSteps > 0)
                                        {
                                            // Moving right..
                                            while (numSteps > 0)
                                            {
                                                shared.levelGrid.MoveRight();
                                                --numSteps;
                                            }
                                        }
                                        else if(numSteps < 0)
                                        {
                                            // Moving left..
                                            numSteps = Math.Abs(numSteps);
                                            while (numSteps > 0)
                                            {
                                                shared.levelGrid.MoveLeft();
                                                --numSteps;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (!shared.popup.Active)
                        {
                            touch.TouchedObject = null;
                        }
                    }
                }

                if (TouchGestureManager.Get().SwipeGesture.WasSwiped())
                {
                    if (TouchGestureManager.Get().SwipeGesture.SwipeDirection == Boku.Programming.Directions.South)
                    {
                        parent.ReturnToPreviousMenu();
                    }
                }

                if ((touch.TouchedObject == null) || (touch.TouchedObject as UIGridLevelElement != null))
                {
                    //// We've either hit nothing, or we've hit a level element. Either way, dragging occurs.
                    shared.levelGrid.HandleTouchInput(shared.camera);
                }

                if (TouchGestureManager.Get().TapGesture.WasTapped())
                {
                    //tapping overlay text from load level menu acts as back button
                    if (HelpOverlay.MouseHitBottomText(
                        TouchInput.GetAsPoint(TouchGestureManager.Get().TapGesture.Position)))
                    {
                        parent.ReturnToPreviousMenu();
                    }
                }

                // Test for a tap on these buttons.
                if (TouchGestureManager.Get().TapGesture.WasTapped())
                {
                    Vector2 hit = TouchGestureManager.Get().TapGesture.Position;

                    hit = LowLevelMouseInput.AdjustHitPosition(hit, shared.camera, true, false);

                    if (shared.likesBox.Contains(hit))
                    {
                        if (!string.IsNullOrEmpty(shared.CurWorld.Permalink))
                        {
                            try
                            {
#if NETFX_CORE
                                Launcher.LaunchUriAsync(new Uri(shared.CurWorld.Permalink));
#else
                                //Process.Start(shared.CurWorld.Permalink);

                                shared.PopupOnLike();
#endif
                            }
                            catch { }
                        }
                    }

                    if (shared.downloadsBox.Contains(hit))
                    {
                        if (!string.IsNullOrEmpty(shared.CurWorld.Permalink))
                        {
                            try
                            {
#if NETFX_CORE
                                Launcher.LaunchUriAsync(new Uri(shared.CurWorld.Permalink));
#else
                                //Process.Start(shared.CurWorld.Permalink);
                                shared.PopupOnDownload();
#endif
                            }
                            catch { }
                        }
                    }

                }

            }   // end of HandleTouchInput()

            /// <summary>
            /// Changes the level filtering depending on the currently selected bucket.
            /// </summary>
            public void ApplyBucketFiltering()
            {
                Genres cur = shared.levelFilter.FilterGenres;

                // Remove any previous filtering.
                cur = (Genres)((int)shared.levelFilter.FilterGenres
                          & ~(int)Genres.MyWorlds
                          & ~(int)Genres.Buckets
                          );

                // Apply the current one.
                UIGridModularTextElement e = (UIGridModularTextElement)shared.bucketsGrid.SelectionElement;

                if (e.Label == Strings.Localize("loadLevelMenu.showAll"))
                {
                    cur |= Genres.Buckets | Genres.MyWorlds;

                    // Only apply All if nothing else is applied.
                    if (cur == Genres.None)
                    {
                        cur = (Genres)((int)cur | (int)Genres.All);
                    }
                }
                if (e.Label == Strings.Localize("loadLevelMenu.showMyWorlds"))
                {
                    cur = (Genres)((int)cur | (int)Genres.MyWorlds);
                }
                if (e.Label == Strings.Localize("loadLevelMenu.showDownloads"))
                {
                    cur = (Genres)((int)cur | (int)Genres.Downloads);
                }
                if (e.Label == Strings.Localize("loadLevelMenu.showLessons"))
                {
                    cur = (Genres)((int)cur | (int)Genres.Lessons);
                }
                if (e.Label == Strings.Localize("loadLevelMenu.showSamples"))
                {
                    cur = (Genres)((int)cur | (int)Genres.SampleWorlds);
                }
                if (e.Label == Strings.Localize("loadLevelMenu.showStarterWorlds"))
                {
                    cur = (Genres)((int)cur | (int)Genres.StarterWorlds);
                }

                shared.levelFilter.FilterGenres = cur;

            }   // end of ApplyBucketFiltering()

            private void SplitText()
            {
                for (int i = 0; i < LoadLevelMenuUIGrid.kWidth; i++)
                {
                    UIGridLevelElement e = shared.levelGrid[i];
                    if (e == null)
                        continue;

                    LevelMetadata info = e.Level;

                    if (info != null && info.UIDescBlob == null)
                    {
                        string description = null;
                        description += info.Description;
                        // We need to limit the width on the sharing browser more so that 
                        // we have room for the presence information to fit.
                        int maxWidth = parent.OriginalBrowserType == LevelBrowserType.Sharing ? 460 : 900;
                        info.UIDescBlob = new TextBlob(parent.renderObj.FontSmall, description, maxWidth);
                        info.UIDescBlob.Justification = info.DescJustification;

                        if (info.Name != null)
                        {
                            TextHelper.SplitMessage(info.Name, 400, parent.renderObj.FontSmall, false, info.UIName);
                        }
                    }
                }
            }   // end of SplitText()

            #endregion

            #region Internal

            /// <summary>
            /// Deletes the current world.  If local then from the local 
            /// machine.  If in the community site then sends a message 
            /// to the server to delete the world.
            /// </summary>
            public bool DeleteCurrentWorld()
            {
                return shared.CurWorld.Browser.StartDeletingLevel(
                    shared.CurWorld.WorldId,
                    shared.CurWorld.Genres & Genres.Virtual,
                    DeleteCallback,
                    shared.CurWorld.Browser);
            }


            //
            // Callbacks for Async server calls.
            //

            public void DeleteCallback(AsyncResult result)
            {
                shared.StartFetchingThumbnails(shared.mainCursor);

                // We have two browsers?
                if (shared.altBrowser != null)
                {
                    // This is the browser we deleted the level from.
                    ILevelBrowser browser = result.Param as ILevelBrowser;

                    // This is the other browser.
                    ILevelBrowser otherBrowser = (browser == shared.mainBrowser) ? shared.altBrowser : shared.mainBrowser;
                }

            }   // end of DeleteCallback()

            public void GetWorldDataCallback(AsyncResult result)
            {
                LevelMetadata level = (LevelMetadata)result.Param;

                if (result.Success)
                {
                    AsyncResult_GetWorldData data = result as AsyncResult_GetWorldData;
                    if (data != null)
                    {
                        if (XmlDataHelper.WriteWorldPacketToDisk(data.World))
                        {
                            level.DownloadState = LevelMetadata.DownloadStates.Complete;
                        }
                        else
                        {
                            level.DownloadState = LevelMetadata.DownloadStates.Failed;
                        }
                    }
                    else
                    {
                        level.DownloadState = LevelMetadata.DownloadStates.Failed;
                    }
                }
                else
                {
                    level.DownloadState = LevelMetadata.DownloadStates.Failed;
                    // TODO (****) Give an error.
                }
            }   // end of GetWorldDataCallback()


            public override void Activate()
            {
                shared.levelGrid.LoadContent(true);
            }

            public override void Deactivate()
            {
                shared.levelGrid.UnloadContent();
            }

            void Callback_OpenMainMenu(AsyncOperation op)
            {
                parent.ActivateMenu(ReturnTo.MainMenu);
            }

            #endregion

        }   // end of class LoadLevelMenu UpdateObj  

        protected class RenderObj : RenderObject, INeedsDeviceReset
        {
            #region Members

            private LoadLevelMenu parent = null;
            private Shared shared = null;

            public Texture2D whiteTile = null;
            public Texture2D blackTile = null;  // Button shapes under Show and Sort options.
            public Texture2D blueTile = null;   // Button shapes under likes and comments.
            public Texture2D blueTileWide = null;
            public Texture2D blueTileWide2 = null;
            public Texture2D smileyTexture = null;
            public Texture2D commentTexture = null;
            public Texture2D downloadsTexture = null;
            public Texture2D localBackground = null;
            public Texture2D communityBackground = null;
            public Texture2D sharingBackground = null;
            public Texture2D auxMenuShadow = null;
            public Texture2D leftBumper = null;
            public Texture2D rightBumper = null;
            public Texture2D corner = null;

            public Texture2D arrowLeft = null;
            public Texture2D arrowRight = null;

            public bool auxMenusActive = false;         // Used to trigger changes in the shadow under the lists.
            public float auxMenuShadowAlpha = 0.0f;     // Shadow oapcity used when sort or show only list is active.

            public GetFont FontSmall = KoiX.SharedX.GetGameFont15_75;
            public GetFont FontLarge = KoiX.SharedX.GetGameFont20;
            public TextBlob blob = new TextBlob(KoiX.SharedX.GetGameFont15_75, "", 500);

            private UIGridLevelElement prevFocusElement = null;     // Used to detect when the focus element changes so we can start fading in the A button.
            private float AButtonAlpha = 1.0f;

            private float xOffset = 0.0f;   // Offset calculated to map rendered image into final display.
            // We save the X value off to the side to be used when rendering
            // the left/right arrows in mouse mode.  This allows us to be
            // sure that they end up at the edges of the screen.

            #endregion

            #region Public

            public RenderObj(LoadLevelMenu parent, Shared shared)
            {
                this.parent = parent;
                this.shared = shared;
            }
            int counter = 0;    // Adds hysteresis to "searching" / "no match" messaging to prevent flashing.
            public override void Render(Camera camera)
            {
                // Ensure the help overlay texture is up to date.
                HelpOverlay.RefreshTexture();

                GraphicsDevice device = KoiLibrary.GraphicsDevice;

                RenderTarget2D rt = SharedX.RenderTargetDepthStencil1280_720;

                Vector2 screenSize = BokuGame.ScreenSize;
                Vector2 rtSize = new Vector2(rt.Width, rt.Height);
                ScreenWarp.FitRtToScreen(rtSize);

                // World info.
                LevelMetadata info = shared.CurWorld;

                // Render the scene to our rendertarget.
                InGame.SetRenderTarget(rt);

                // Needed to clear the depth buffer so we don't see a silhouette 
                // of the main menu and greeter Kodu.
                device.Clear(Color.Transparent);

                // Set up params for rendering UI with this camera.
                BokuGame.bokuGame.shaderGlobals.SetCamera(shared.camera);

                ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();

                // Copy the background to the rt.
                string title = null;
                Texture2D bkg = null;
                if (parent.CurrentBrowserType == LevelBrowserType.Community)
                {
                    title = Strings.Localize("loadLevelMenu.communityTitle");
                    bkg = communityBackground;
                }
                else if (shared.altBrowser != null)
                {
                    title = Strings.Localize("loadLevelMenu.sharingTitle");
                    bkg = sharingBackground;
                }
                else
                {
                    title = Strings.Localize("loadLevelMenu.localTitle");
                    bkg = localBackground;
                }

                quad.Render(bkg, Vector2.Zero, rtSize, "TexturedNoAlpha");


                Color darkTextColor = new Color(40, 40, 40);
                Color greyTextColor = new Color(127, 127, 127);
                Color lightTextColor = new Color(255, 255, 255);
                Color greenTextColor = new Color(12, 255, 0);
                Color blueText = new Color(12, 150, 209);


                SpriteBatch batch = KoiLibrary.SpriteBatch;

                Vector2 pos = Vector2.Zero;

                // White tile for X and Y buttons.
                quad.Render(whiteTile, new Vector4(1.0f, 1.0f, 1.0f, 0.2f), new Vector2(185, 100), new Vector2(600, 70), "TexturedRegularAlpha");

                // Render buttons.
                string yText = Strings.Localize("loadLevelMenu.sortBy") + " " + shared.sortListDisplay;

                batch.Begin();

                // Y button
                pos.Y = 113;
                pos.X = 185 + 600 - 64 - FontLarge().MeasureString(yText).X;
                Vector2 min = pos;
                // Render button if in key/mouse mode.
                bool km = KoiLibrary.LastTouchedDeviceIsKeyboardMouse;
                Vector2 buttonSize = new Vector2(52.0f, 52.0f);
                if (km)
                {
                    buttonSize.X = 48 + FontLarge().MeasureString(Strings.Localize("loadLevelMenu.sortBy") + " ").X;
                    quad.Render(blackTile, pos - new Vector2(4.0f, 4.0f), buttonSize, "TexturedRegularAlpha");
                }
                quad.Render(ButtonTextures.YButton, pos + new Vector2(4, 4), new Vector2(52, 52), "TexturedRegularAlpha");
                float maxLength = pos.X;    // Save for trimming X button text.
                pos.X += 64;
                TextHelper.DrawString(FontLarge, Strings.Localize("loadLevelMenu.sortBy"), pos + new Vector2(-20, 3), km ? shared.sortColor : new Color(255, 255, 60));
                pos.X += FontLarge().MeasureString(Strings.Localize("loadLevelMenu.sortBy") + "  ").X;
                TextHelper.DrawString(FontLarge, shared.sortListDisplay, pos + new Vector2(-20, 3), darkTextColor);
                Vector2 max = pos + new Vector2(-20 + FontLarge().MeasureString(Strings.Localize("loadLevelMenu.sortBy") + "  ").X, 3 + FontLarge().LineSpacing);
                shared.sortBox.Set(min, max);

                //
                // Add text.
                //

                // Browser screen title.
                GetFont Font = SharedX.GetGameFont30Bold;
                Vector2 position = new Vector2(192, 0);
                TextHelper.DrawString(Font, title, position, new Color(255, 255, 255, 200));

                batch.End();

                // Description block.
                if (info != null)
                {
                    int maxWidth = parent.OriginalBrowserType == LevelBrowserType.Sharing ? 460 : 900;
                    pos.X = 190;
                    pos.Y = 426;

                    batch.Begin();

                    // Level title.
                    string titleStr = TextHelper.FilterInvalidCharacters(info.Name);
                    titleStr = TextHelper.AddEllipsis(FontLarge, titleStr, maxWidth);
                    Vector2 titlePosition = pos;
                    pos.Y += FontLarge().LineSpacing;

                    // Date and creator name.
                    Vector2 datePosition = pos;
                    DateTime localWriteTime = info.LastWriteTime.ToLocalTime();
#if NETFX_CORE
                    string dateStr = localWriteTime.ToString() + " " + localWriteTime.ToString();
#else
                    string dateStr = localWriteTime.ToShortDateString() + " " + localWriteTime.ToShortTimeString();
#endif

                    if (info.Creator != null)
                    {
                        dateStr += " " + Strings.Localize("loadLevelMenu.authoredBy") + " " + TextHelper.FilterInvalidCharacters(info.Creator);
                    }
                    dateStr = TextHelper.AddEllipsis(FontLarge, dateStr, maxWidth);
                    pos.Y += FontLarge().LineSpacing;

                    batch.End();

                    // Display #likes, #comments, #downloads and socl and KoduGameLab buttons.
                    // Only if on Community page.
                    if (parent.OriginalBrowserType == LevelBrowserType.Community)
                    {
                        Vector2 buttonPos = pos;

                        string numString = "";
                        Vector2 strSize;
                        Vector2 iconSize;
#if !HIDE_LIKES
                        // Likes.
                        numString = info.NumLikes.ToString();
                        strSize = FontSmall().MeasureString(numString);
                        iconSize = new Vector2(26, 26);
                        buttonSize = new Vector2(32, 32);
                        buttonSize.X += strSize.X;
                        quad.Render(blueTile, buttonPos, buttonSize, "TexturedRegularAlpha");
                        shared.likesBox.Set(buttonPos, buttonPos + buttonSize);
                        // Don't render number if 0.
                        if (info.NumLikes == 0)
                        {
                            quad.Render(smileyTexture, buttonPos + new Vector2(15, 3), iconSize, "TexturedRegularAlpha");
                        }
                        else
                        {
                            quad.Render(smileyTexture, buttonPos + new Vector2(3, 3), iconSize, "TexturedRegularAlpha");
                            blob.RawText = numString;
                            blob.RenderWithButtons(buttonPos + new Vector2(22, 3), Color.White);
                        }
                        buttonPos.X += buttonSize.X + 8.0f;

                        // Comments.
                        numString = "   " + info.NumComments.ToString();
                        strSize = FontSmall().MeasureString(numString);
                        iconSize = new Vector2(24, 24);
                        buttonSize = new Vector2(32, 32);
                        buttonSize.X += strSize.X;
                        quad.Render(blueTile, buttonPos, buttonSize, "TexturedRegularAlpha");
                        shared.commentsBox.Set(buttonPos, buttonPos + buttonSize);
                        // Don't render number if 0.
                        if (info.NumComments == 0)
                        {
                            quad.Render(commentTexture, buttonPos + new Vector2(17, 5), iconSize, "TexturedRegularAlpha");
                        }
                        else
                        {
                            quad.Render(commentTexture, buttonPos + new Vector2(4, 5), iconSize, "TexturedRegularAlpha");
                            blob.RawText = numString;
                            blob.RenderWithButtons(buttonPos + new Vector2(22, 3), Color.White);
                        }
                        buttonPos.X += buttonSize.X + 8.0f;
#endif
                        // Downloads.
                        numString = "   " + info.Downloads.ToString();
                        strSize = FontSmall().MeasureString(numString);
                        iconSize = new Vector2(30, 30); // Larger than normal since the image is smaller.
                        buttonSize = new Vector2(32, 32);
                        buttonSize.X += strSize.X;
                        quad.Render(blueTile, buttonPos, buttonSize, "TexturedRegularAlpha");
                        shared.downloadsBox.Set(buttonPos, buttonPos + buttonSize);
                        // Don't render number if 0.
                        if (info.Downloads == 0)
                        {
                            quad.Render(downloadsTexture, buttonPos + new Vector2(14, 4), iconSize, "TexturedRegularAlpha");
                        }
                        else
                        {
                            quad.Render(downloadsTexture, buttonPos + new Vector2(4, 4), iconSize, "TexturedRegularAlpha");
                            blob.RawText = numString;
                            blob.RenderText(null, buttonPos + new Vector2(22, 3), Color.White);
                        }
                        buttonPos.X += buttonSize.X + 8.0f;

                        pos.Y += FontLarge().LineSpacing;
                    }

                    // Tags.
                    int bits = (int)info.Genres;
                    string tags = null;
                    bool first = true;
                    for (int shift = 1; shift < 32; shift++)
                    {
                        int i = 1 << shift;
                        if ((i & bits) != 0)
                        {
                            if ((Genres)i == Genres.StarterWorlds)
                                continue;

                            if (first)
                            {
                                tags = Strings.GetGenreName(i);
                                first = false;
                            }
                            else
                            {
                                tags += ", " + Strings.GetGenreName(i);
                            }
                        }
                    }
                    if (tags != null)
                    {
                        blob.RawText = Strings.Localize("loadLevelMenu.tags") + ": " + tags;
                        blob.RenderText(null, pos, darkTextColor);
                        pos.Y += FontLarge().LineSpacing * blob.NumLines;
                    }

                    // Calc size of box around level info.
                    //
                    // Title.
                    int boxWidth = (int)FontLarge().MeasureString(titleStr).X;
                    // Date/creator.
                    boxWidth = Math.Max(boxWidth, (int)FontSmall().MeasureString(dateStr).X);
                    // Tags.
                    if (tags != null)
                    {
                        for (int i = 0; i < blob.NumLines; i++)
                        {
                            boxWidth = Math.Max(boxWidth, blob.GetLineWidth(i));
                        }
                    }
                    // Description.
                    if (info.UIDescBlob != null)
                    {
                        int maxLines = 7;
                        for (int i = 0; i < maxLines; i++)
                        {
                            boxWidth = Math.Max(boxWidth, info.UIDescBlob.GetLineWidth(i));
                        }
                    }

                    // Add corners around level info.
                    pos.X = 178;
                    pos.Y = 422;
                    // Pad width.
                    boxWidth += 16;
                    int boxHeight = (int)(datePosition.Y - pos.Y);
                    if (tags != null)
                    {
                        boxHeight += (blob.NumLines - 1) * blob.TotalSpacing;
                        boxHeight += FontLarge().LineSpacing;
                    }

                    int maxDescLines = blob == null ? 7 : 8 - blob.NumLines;

                    if (info.UIDescBlob != null)
                    {
                        int numLines = Math.Min(maxDescLines, info.UIDescBlob.NumLines);
                        boxHeight += numLines * info.UIDescBlob.TotalSpacing;
                    }
                    // Pad height.
                    boxHeight += 42;

                    /*
                    // Disable corner rendering since we now allow user to justify their description text.
                    Vector4 tint = new Vector4(1, 1, 1, 0.6f);
                    quad.Render(corner, tint, pos, new Vector2(corner.Width, corner.Height), "TexturedRegularAlpha");
                    quad.Render(corner, tint, pos + new Vector2(boxWidth, 0), new Vector2(-corner.Width, corner.Height), "TexturedRegularAlpha");
                    quad.Render(corner, tint, pos + new Vector2(0, boxHeight), new Vector2(corner.Width, -corner.Height), "TexturedRegularAlpha");
                    quad.Render(corner, tint, pos + new Vector2(boxWidth, boxHeight), new Vector2(-corner.Width, -corner.Height), "TexturedRegularAlpha");
                    */

                    pos = datePosition;
                    // Skip an extra space to account for likes/comments/etc buttons.
                    pos.Y += 2.0f * FontLarge().LineSpacing;
                    if (tags != null)
                    {
                        // We've already rendered the Tags text, just skip the space for the like if it's not null.
                        //tagsBlob.RenderWithButtons(pos, darkTextColor, false);
                        pos.Y += FontLarge().LineSpacing * blob.NumLines;
                    }

                    batch.Begin();
                    // Draw Title
                    TextHelper.DrawString(FontLarge, titleStr, titlePosition, blueText);
                    // Draw date/time/creator
                    TextHelper.DrawString(FontSmall, dateStr, datePosition, blueText);
                    batch.End();

                    // Description.
                    if (info.UIDescBlob != null)
                    {
                        info.UIDescBlob.RenderText(null, pos, darkTextColor, maxLines: maxDescLines);
                    }
                }

                batch.Begin();

                if (shared.showPagingMessage)
                {
                    string message = "Fetching...";
                    Font = SharedX.GetGameFont24;
                    int textWidth = (int)Font().MeasureString(message).X;
                    int screenWidth = KoiLibrary.GraphicsDevice.Viewport.Width;
                    int textX = (screenWidth - textWidth) / 2;
                    int textY = KoiLibrary.GraphicsDevice.Viewport.TitleSafeArea.Top;
                    TextHelper.DrawString(Font, message, new Vector2(textX, textY), lightTextColor);
                }

                // Check if we have no valid levels.
                if (shared.levelGrid.NoValidLevels)
                {
                    string str = String.Empty;


                    if (counter > 0)
                        --counter;

                    if (shared.mainBrowser != null && shared.mainBrowser.Working)
                    {
                        counter = 5;
                    }

                    if (shared.mainBrowser != null && (shared.mainBrowser.Working || counter > 0))
                    {
                        // Busy.
                        str = Strings.Localize("loadLevelMenu.searching");
                    }
                    else
                    {
                        if (shared.levelFilter.FilterGenres == Genres.All)
                        {
                            // Must be none.
                            str = Strings.Localize("loadLevelMenu.noLevels");
                        }
                        else
                        {
                            // None that match filter.
                            str = Strings.Localize("loadLevelMenu.noMatch");
                        }
                    }

                    pos = new Vector2(0, 200);
                    pos.X = (rt.Width - FontLarge().MeasureString(Strings.Localize("loadLevelMenu.noMatch")).X) / 2.0f;
                    TextHelper.DrawString(FontLarge, str, pos, darkTextColor);
                }

                batch.End();

                // Render the buckets grid.
                if (shared.bucketsGrid != null)
                {
                    if (KoiLibrary.LastTouchedDeviceIsGamepad)
                    {
                        quad.Render(leftBumper, shared.leftBumperPosition, new Vector2(96, 96), "TexturedRegularAlpha");
                        quad.Render(rightBumper, shared.rightBumperPosition, new Vector2(96, 96), "TexturedRegularAlpha");
                    }

                    shared.bucketsGrid.Render(shared.camera);
                }

                // Render the file list grid.
                if (shared.levelGrid != null)
                {
                    shared.levelGrid.Render(shared.camera);
                }

                // A Button, needs to be rendered on top of the current tile.
                if (!shared.levelGrid.NoValidLevels && shared.levelGrid.SelectionElement != null)
                {
                    batch.Begin();

                    pos = new Vector2(390, 340);

                    // Recalc position to put A button on lower right hand corner of selected tile.
                    UIGridLevelElement focusElement = (UIGridLevelElement)shared.levelGrid.SelectionElement;
                    if (focusElement != prevFocusElement && focusElement.Title != "" && (prevFocusElement == null || prevFocusElement.Title != ""))
                    {
                        prevFocusElement = focusElement;
                        // Start a twitch to fade in the A button
                        TwitchAButtonAlpha();
                    }

                    if (focusElement.Title == "")
                    {
                        focusElement = prevFocusElement;
                    }

                    Vector3 position3d = focusElement.Orientation.position;
                    position3d += shared.levelGrid.WorldMatrix.Translation;

                    Point point = shared.camera.WorldToScreenCoords(position3d);
                    pos = new Vector2(point.X, point.Y);
                    float scale = focusElement.Orientation.scale;
                    pos += scale / 1.2f * new Vector2(68, 76);

                    if (shared.popup.Active)
                    {
                        /*
                        quad.Render(ButtonTextures.BButton, pos + new Vector2(4, 4), new Vector2(52, 52), "TexturedRegularAlpha");
                        pos.X += 64;
                        TextHelper.DrawString(FontLarge, Strings.Localize("loadLevelMenu.back"), pos + new Vector2(-20, 3), new Color(245, 0, 22));
                        */
                    }
                    else
                    {
                        shared.SetUpPopup(false);

                        if (shared.popup.NumItems > 0 && KoiLibrary.LastTouchedDeviceIsGamepad)
                        {
                            quad.Render(ButtonTextures.AButton, new Vector4(1, 1, 1, AButtonAlpha), pos + new Vector2(4, 4), new Vector2(52, 52), "TexturedRegularAlpha");
                            //pos.X += 64;
                            //TextHelper.DrawString(FontLarge, Strings.Localize("loadLevelMenu.actions"), pos + new Vector2(-20, 3), new Color(11, 161, 65));
                        }
                    }
                    batch.End();
                }

                // Render the scroll arrows if in mouse mode.
                if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
                {
                    pos = new Vector2(xOffset + 22, 260);
                    Vector2 size = new Vector2(arrowLeft.Width, arrowLeft.Height);

                    quad.Render(arrowLeft, pos, size, "TexturedRegularAlpha");
                    shared.arrowLeftBox.Set(pos, pos + size);

                    pos.X = 1280 - xOffset - 22 - size.X;
                    quad.Render(arrowRight, pos, size, "TexturedRegularAlpha");
                    shared.arrowRightBox.Set(pos, pos + size);
                }

                // Render the aux menus if active.  If active we also need to render the shadow underneath.
                RenderAuxMenuShadow(rtSize);

                // Render search box
                shared.textLineEditor.Render(camera);

                shared.sortList.Render(shared.camera);
                shared.popup.Render(shared.camera);

                // If we've got the popup active, rerender the in-focus level tile.
                if (shared.popup.Active
                    || ModularMessageDialogManager.Instance.IsDialogActive())
                {
                    UIGridLevelElement e = (UIGridLevelElement)shared.levelGrid.SelectionElement;
                    if (e != null)
                    {
                        e.Render(shared.camera);

                        if (KoiLibrary.LastTouchedDeviceIsGamepad)
                        {
                            // If the popup is active, render <B> Back under it.
                            batch.Begin();
                            pos = new Vector2(450, 390);
                            quad.Render(ButtonTextures.BButton, pos + new Vector2(4, 4), new Vector2(52, 52), "TexturedRegularAlpha");
                            pos.X += 64;
                            TextHelper.DrawString(FontLarge, Strings.Localize("loadLevelMenu.back"), pos + new Vector2(-20, 3), new Color(245, 0, 22));
                            batch.End();

                            // Set mouse hit box for <B> back.
                            min = pos + new Vector2(-60, -4);
                            max = shared.backBox.Min + new Vector2(64 - 20 + FontLarge().MeasureString(Strings.Localize("loadLevelMenu.back")).X, 64 + 3);
                            shared.backBox.Set(min, max);
                        }
                    }
                }

                // Render the tag picker after the in-focus tile to ensure that it's on top.
                shared.tagPicker.Render(shared.camera);

                InGame.RestoreRenderTarget();

                InGame.Clear(new Color(20, 20, 20));
                InGame.SetViewportToScreen();

                // Copy the rendered scene to the backbuffer.
                quad.Render(rt, ScreenWarp.RenderPosition, ScreenWarp.RenderSize, @"TexturedNoAlpha");

                // Set up offset for mouse scroll arrows.
                xOffset = -position.X * 720.0f / screenSize.Y;
                xOffset = MathHelper.Max(0, xOffset);

                // Slip the help overlay under any message dialogs.
                HelpOverlay.Render();

                // Message dialog boxes only render if active.
                parent.sharingCloseSessionConfirmMessage.Render();
                parent.sharingLeaveSessionConfirmMessage.Render();
                parent.sharingSessionEndedMessage.Render();

                shared.communityShareMenu.Render();

                InGame.RenderMessages();

            }   // end of LoadLevelMenu RenderObj Render()

            #endregion

            #region Internal

            /// <summary>
            /// Inits A Button alpha to 0 and twitches it to 1.0f.
            /// </summary>
            private void TwitchAButtonAlpha()
            {
                AButtonAlpha = 0.0f;
                TwitchManager.Set<float> set = delegate(float val, Object param) { AButtonAlpha = val; };
                TwitchManager.CreateTwitch<float>(0.0f, 1.0f, set, 0.4f, TwitchCurve.Shape.EaseInOut);
            }   // end of TwitchAButtonAlpha()

            /// <summary>
            /// Checks for any transitions in the aux menu state, adjusts the shadow alpha
            /// accordingly and then renders the shadow if needed.
            /// </summary>
            private void RenderAuxMenuShadow(Vector2 rtSize)
            {
                if (auxMenusActive)
                {
                    if (!parent.shared.sortList.Active
                        && !parent.shared.popup.Active
                        && !parent.shared.tagPicker.Active
                        && !ModularMessageDialogManager.Instance.IsDialogActive())
                    {
                        auxMenusActive = false;

                        //auxMenuShadowAlpha = 0.0f;
                        TwitchManager.Set<float> set = delegate(float val, Object param) { auxMenuShadowAlpha = val; };
                        TwitchManager.CreateTwitch<float>(auxMenuShadowAlpha, 0.0f, set, 0.2f, TwitchCurve.Shape.EaseInOut);
                    }
                }
                else
                {
                    if (parent.shared.sortList.Active
                        || parent.shared.popup.Active
                        || parent.shared.tagPicker.Active
                        || ModularMessageDialogManager.Instance.IsDialogActive())
                    {
                        auxMenusActive = true;

                        //auxMenuShadowAlpha = 0.8f;
                        TwitchManager.Set<float> set = delegate(float val, Object param) { auxMenuShadowAlpha = val; };
                        TwitchManager.CreateTwitch<float>(auxMenuShadowAlpha, 0.8f, set, 0.2f, TwitchCurve.Shape.EaseInOut);
                    }
                }

                if (auxMenuShadowAlpha > 0.0f)
                {
                    ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();
                    quad.Render(auxMenuShadow, new Vector4(1.0f, 1.0f, 1.0f, auxMenuShadowAlpha), Vector2.Zero, rtSize, @"TexturedRegularAlpha");
                }
            }   // end of RenderAuxMenuShadow()


            public override void Activate()
            {
            }

            public override void Deactivate()
            {
            }

            /// <summary>
            /// Helper function to save some typing...
            /// </summary>
            /// <param name="tex"></param>
            /// <param name="path"></param>
            public void LoadTexture(ref Texture tex, string path)
            {
                if (tex == null)
                {
                    tex = BokuGame.Load<Texture>(BokuGame.Settings.MediaPath + path);
                }
            }   // end of LoadTexture()

            /// <summary>
            /// Helper function to save some typing...
            /// </summary>
            /// <param name="tex"></param>
            /// <param name="path"></param>
            public void LoadTexture(ref Texture2D tex, string path)
            {
                if (tex == null)
                {
                    tex = KoiLibrary.LoadTexture2D(path);
                }
            }   // end of LoadTexture()

            public void LoadContent(bool immediate)
            {
                LoadTexture(ref localBackground, @"Textures\LoadLevel\LocalBackground");
                LoadTexture(ref communityBackground, @"Textures\LoadLevel\CommunityBackground");
                LoadTexture(ref sharingBackground, @"Textures\LoadLevel\SharingBackground");

                LoadTexture(ref whiteTile, @"Textures\LoadLevel\WhiteTile");
                LoadTexture(ref blackTile, @"Textures\GridElements\BlackTextTile");
                LoadTexture(ref blueTile, @"Textures\GridElements\BlueTextTile");
                LoadTexture(ref blueTileWide, @"Textures\GridElements\BlueTextTileWide");
                LoadTexture(ref blueTileWide2, @"Textures\GridElements\BlueTextTileWide2");

                LoadTexture(ref smileyTexture, @"Textures\LoadLevel\Smiley");
                LoadTexture(ref commentTexture, @"Textures\LoadLevel\Comment");
                LoadTexture(ref downloadsTexture, @"Textures\LoadLevel\Downloads");

                LoadTexture(ref auxMenuShadow, @"Textures\LoadLevel\AuxMenuShadow");

                LoadTexture(ref leftBumper, @"Textures\LoadLevel\L_bumper");
                LoadTexture(ref rightBumper, @"Textures\LoadLevel\R_bumper");

                LoadTexture(ref corner, @"Textures\LoadLevel\Corner");

                LoadTexture(ref arrowLeft, @"Textures\LoadLevel\Arrow_Left");
                LoadTexture(ref arrowRight, @"Textures\LoadLevel\Arrow_Right");
            }

            public void InitDeviceResources(GraphicsDevice device)
            {
                parent.shared.SetUpAuxMenus();         // This needs to be done after the renderObj is loaded since it uses its font.
                BokuGame.Load(parent.shared, true);    // This needs to be done after the aux menus are set up.
            }

            public void UnloadContent()
            {
                DeviceResetX.Release(ref localBackground);
                DeviceResetX.Release(ref communityBackground);
                DeviceResetX.Release(ref sharingBackground);

                DeviceResetX.Release(ref whiteTile);
                DeviceResetX.Release(ref blackTile);
                DeviceResetX.Release(ref blueTile);
                DeviceResetX.Release(ref blueTileWide);
                DeviceResetX.Release(ref blueTileWide2);
                DeviceResetX.Release(ref smileyTexture);
                DeviceResetX.Release(ref commentTexture);
                DeviceResetX.Release(ref downloadsTexture);
                DeviceResetX.Release(ref auxMenuShadow);
                DeviceResetX.Release(ref leftBumper);
                DeviceResetX.Release(ref rightBumper);
                DeviceResetX.Release(ref corner);
                DeviceResetX.Release(ref arrowLeft);
                DeviceResetX.Release(ref arrowRight);
            }   // end of LoadLevelMenu RenderObj UnloadContent()

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
            }

            #endregion

        }   // end of class LoadLevelMenu RenderObj     

        #region Members

        // List objects.
        public Shared shared = null;
        protected RenderObj renderObj = null;
        protected UpdateObj updateObj = null;

        // Instrumentation
        private object UiOpenInstrument;

        public enum ReturnTo
        {
            MainMenu,
            MiniHub,
            ShareHub,
            EditWorldParameters,
            Editor
        }

        private enum States
        {
            Inactive,
            Active,
        }
        private States state = States.Inactive;
        private States pendingState = States.Inactive;

        private CommandMap commandMap = new CommandMap("App.LoadLevelMenu");   // Placeholder for stack.

        private ReturnTo returnToMenu = ReturnTo.MainMenu;

        protected ModularMessageDialog sharingCloseSessionConfirmMessage;
        protected ModularMessageDialog sharingLeaveSessionConfirmMessage;
        protected ModularMessageDialog sharingSessionEndedMessage;

        protected SimpleMessage blockingOpMessage;

        private bool loadingFromString = false;

        #endregion

        #region Accessors

        /// <summary>
        /// Flag telling us that we're attempting an asynchronous load of a level file.
        /// No one else should attempt a load from string while this load is being attempted.
        /// </summary>
        public bool LoadingFromString
        {
            set { loadingFromString = value; }
            get { return loadingFromString; }
        }

        public bool Active
        {
            get { return (state == States.Active); }
        }

        /// <summary>
        /// Flag telling us that if user cancel out of menu we need to
        /// return to the MainMenu rather than the MiniHub.
        /// TODO This should be an arg to Activate().  Make it so once
        /// we get rid of the shared/updateObj/renderObj stuff.
        /// </summary>
        public ReturnTo ReturnToMenu
        {
            get { return returnToMenu; }
            set { returnToMenu = value; }
        }

        #endregion

        #region Public

        /// <summary>
        /// These are the different supported views of the local level browser.
        /// </summary>
        public enum LocalLevelModes
        {
            General,
            Lessons,
            SIZEOF
        }

        LocalLevelModes localLevelMode;

        /// <summary>
        /// The currently active view of the local level browser. NOTE: This should be
        /// set just before the call to loadLevelMenu.Activate(), and not set at any
        /// other time. It should be considered a parameter to loadLevelMenu.Activate().
        /// </summary>
        public LocalLevelModes LocalLevelMode
        {
            get { return localLevelMode; }
            set
            {
                localLevelMode = value;
                switch (localLevelMode)
                {
                    // Reset "lessons" filter in case the user changed it last time the UI was viewed.
                    case LocalLevelModes.Lessons:
                        savedUIStates[(int)localLevelMode].showOnly = Genres.Lessons;
                        break;
                }
            }
        }

        /// <summary>
        /// Each LoadLevelMenu has its own attached level browser. This field
        /// returns which browser is in use on an instance of a LoadLevelMenu.
        /// </summary>
        public LevelBrowserType CurrentBrowserType;

        /// <summary>
        ///  This field returns the browser type this load level menu was created
        ///  as.  The Sharing page actually has 2 browsers and switches between them.
        ///  The above vlaue changes as they switch.
        /// </summary>
        public LevelBrowserType OriginalBrowserType;

        /// <summary>
        /// We support different views of the local level browser. Each view's
        /// UI state is saved to this structure so that it can be restored later.
        /// </summary>
        public class SavedUIState
        {
            public Guid selectedLevelId = Guid.Empty;
            public Genres showOnly = Genres.All;
            public SortBy sortBy = SortBy.Date;
            public SortDirection sortDirection = SortDirection.Descending;
        }

        // The set of local level browser stored UI states.
        SavedUIState[] savedUIStates;

        /// <summary>
        /// Shortcut to access the stored UI state for the current view of the local level browser.
        /// </summary>
        public SavedUIState RememberedState
        {
            get
            {
                Debug.Assert(CurrentBrowserType == LevelBrowserType.Local);
                return savedUIStates[(int)LocalLevelMode];
            }
        }


        // c'tor
        public LoadLevelMenu(LevelBrowserType browserType)
        {
            this.CurrentBrowserType = browserType;
            this.OriginalBrowserType = browserType;

            if (OriginalBrowserType == LevelBrowserType.Local)
            {
                // Initialize UI state storage for each of the local level browser views.
                savedUIStates = new SavedUIState[(int)LocalLevelModes.SIZEOF];
                for (int i = 0; i < (int)LocalLevelModes.SIZEOF; ++i)
                    savedUIStates[i] = new SavedUIState();
            }

            shared = new Shared(this);

            // Create the RenderObject and UpdateObject parts of this mode.
            updateObj = new UpdateObj(this, shared);
            renderObj = new RenderObj(this, shared);

            {
                sharingCloseSessionConfirmMessage = new ModularMessageDialog(
                    Strings.Localize("loadLevelMenu.sharingCloseSessionConfirmMessage"),
                    null, Strings.Localize("textDialog.ok"),
                    null, Strings.Localize("textDialog.cancel"),
                    null, null,
                    null, null);

                sharingLeaveSessionConfirmMessage = new ModularMessageDialog(
                    Strings.Localize("loadLevelMenu.sharingLeaveSessionConfirmMessage"),
                    null, Strings.Localize("textDialog.ok"),
                    null, Strings.Localize("textDialog.cancel"),
                    null, null,
                    null, null);

                sharingSessionEndedMessage = new ModularMessageDialog(
                    Strings.Localize("loadLevelMenu.sharingSessionEndedMessageFmt"),
                    null, Strings.Localize("textDialog.ok"),
                    null, null,
                    null, null,
                    null, null);

            }

        }   // end of LoadLevelMenu c'tor

        /// <summary>
        /// Sends a "like" to Socl by the current user for the current level.
        /// </summary>
        public void LikeLevel(bool liked)
        {
            var packet = new PostLikePacket();
            packet.Liked = liked;
            packet.PartitionKey = shared.CurWorld.PartitionKey;
            packet.RowKey = shared.CurWorld.RowKey;
            packet.UserId = 0;
            if (0 == Web.Community.Async_PostLike(packet, Callback_PostLikeByWorldId, null))
            {
                // TODO: Handle Error
            }
        }   // end of LikeLevel()

        public void Callback_PostLikeByWorldId(AsyncResult result)
        {
            // If successful, locally increment the number of likes
            // to provide some level of feedback to the user.
            // Don't do this if they've already like this.
            if (result.Success && !shared.CurWorld.LikedByThisUser)
            {
                ++shared.CurWorld.NumLikes;
                shared.CurWorld.LikedByThisUser = true;
            }
        }   // end of Callback_PutWorldData()

        /// <summary>
        /// Sends a "like" to the KoduDB by the current user for the current level.
        /// </summary>
        public void LikeLevelByWorldId(bool liked)
        {
            var packet = new PostLikeByWorldIdPacket();
            packet.Liked = liked;
            packet.UserId = 0;
            packet.WorldId = shared.CurWorld.WorldId;
            if (0 == Web.Community.Async_PostLikeByWorldId(packet, Callback_PostLikeByWorldId, null))
            {
                // TODO: Handle Error
            }
        }   // end of LikeLevelByWorldId()

        //helper functions to display dialogs
        public void ShowLevelExportedDialog(string exportedFilename)
        {
            string text = Strings.Localize("textDialog.levelExported") + exportedFilename;
            string labelA = Strings.Localize("textDialog.continue");
            ModularMessageDialogManager.Instance.AddDialog(text, ReturnToLevelGrid, labelA);
        }

        public void ShowLinkedLevelExportedDialog()
        {
            string text = Strings.Localize("textDialog.linkedLevelExport");
            string labelA = Strings.Localize("textDialog.continue");
            ModularMessageDialogManager.Instance.AddDialog(text, ReturnToLevelGrid, labelA);
        }

        public void ShowDeleteLinkedConfirmDialog()
        {
            string text = Strings.Localize("textDialog.deleteLinkedPrompt");
            string labelA = Strings.Localize("textDialog.delete");
            string labelB = Strings.Localize("textDialog.cancel");
            ModularMessageDialogManager.Instance.AddDialog(text, shared.DeleteSelectedLevel, labelA,
                                                                 ReturnToLevelGrid, labelB);
        }

        public void ShowDeleteConfirmDialog()
        {
            string text = Strings.Localize("textDialog.deletePrompt");
            string labelA = Strings.Localize("textDialog.delete");
            string labelB = Strings.Localize("textDialog.cancel");
            ModularMessageDialogManager.Instance.AddDialog(text, shared.DeleteSelectedLevel, labelA,
                                                                 ReturnToLevelGrid, labelB);
        }

        public void ShowReportAbuseDialog()
        {
            string text = Strings.Localize("textDialog.reportAbusePrompt");
            string labelA = Strings.Localize("textDialog.ok");
            string labelB = Strings.Localize("textDialog.cancel");
            ModularMessageDialogManager.Instance.AddDialog(text, shared.ReportAbuseSelectedLevel, labelA,
                                                                 ReturnToLevelGrid, labelB);
        }

        public void ShowLevelNotFirstDialog()
        {
            //handler for "yes" - load first level
            ModularMessageDialog.ButtonHandler handlerA = delegate(ModularMessageDialog dialog)
            {
                //close the dialog
                dialog.Deactivate();

                //if edit mode was desired, we wouldn't even prompt this, can assume we're playing
                shared.PlaySelectedLevel(true, false);
            };

            //handler for "no" - load selected level
            ModularMessageDialog.ButtonHandler handlerB = delegate(ModularMessageDialog dialog)
            {
                //close the dialog
                dialog.Deactivate();

                // User chose "no"
                //if edit mode was desired, we wouldn't even prompt this, can assume we're playing
                shared.PlaySelectedLevel(false, false);
            };

            string text = Strings.Localize("loadLevelMenu.levelNotFirstMessage");
            string labelA = Strings.Localize("textDialog.yes");
            string labelB = Strings.Localize("textDialog.no");
            ModularMessageDialogManager.Instance.AddDialog(text, handlerA, labelA, handlerB, labelB);
        }

        public void ShowBrokenLinkDialog( bool bEditMode )
        {
            //handler for "continue" - user wants to play anyway
            ModularMessageDialog.ButtonHandler handlerA = delegate(ModularMessageDialog dialog)
            {
                //close the dialog
                dialog.Deactivate();

                shared.PlaySelectedLevel(false, bEditMode);
            };

            string text = Strings.Localize("loadLevelMenu.levelLinksBrokenMessage");
            string labelA = Strings.Localize("textDialog.continue");
            string labelB = Strings.Localize("textDialog.back");
            ModularMessageDialogManager.Instance.AddDialog(text, handlerA, labelA, ReturnToLevelGrid, labelB);
        }

        public void ShowBrokenLinkOnAttachDialog()
        {
            //TODO: need a "Yes" that still does the attach
            string text = Strings.Localize("loadLevelMenu.levelLinksBrokenMessage");
            string labelA = Strings.Localize("textDialog.ok");
            ModularMessageDialogManager.Instance.AddDialog(text, ReturnToLevelGrid, labelA);
        }

        public void ShowSimpleDialog(string messageKey)
        {
            string text = Strings.Localize(messageKey);
            string labelA = Strings.Localize("textDialog.ok");
            ModularMessageDialogManager.Instance.AddDialog(text, ReturnToLevelGrid, labelA);
        }


        public void ShowTargetAlreadyLinkedDialog(Guid targetLink)
        {
            //handler for "continue" - user wants to play anyway
            ModularMessageDialog.ButtonHandler handlerA = delegate(ModularMessageDialog dialog)
            {
                //close the dialog
                dialog.Deactivate();

                //we're good, update the actual link in the xml (won't take effect until a save occurs)
                InGame.XmlWorldData.LinkedToLevel = targetLink;

                ReturnToPreviousMenu();
            };

            string text = Strings.Localize("loadLevelMenu.targetAlreadyLinkedMessage");
            string labelA = Strings.Localize("textDialog.continue");
            string labelB = Strings.Localize("textDialog.cancel");
            ModularMessageDialogManager.Instance.AddDialog(text, handlerA, labelA, ReturnToLevelGrid, labelB);
        }

        public void ShowBrokenLevelExportWarning()
        {
            ShowWarning(Strings.Localize("loadLevelMenu.brokenLevelExportMessage"));
        }

        public void ShowBrokenLevelShareWarning()
        {
            ShowWarning(Strings.Localize("loadLevelMenu.brokenLevelShareMessage"));
        }

        private void ShowWarning( string text )
        {
            //handler for "continue" - user wants to play anyway
            ModularMessageDialog.ButtonHandler handlerA = delegate(ModularMessageDialog dialog)
            {
                //close the dialog
                dialog.Deactivate();
            };

            if (null == text)
            {
                text = "";
            }
            string labelA = Strings.Localize("textDialog.ok");
            ModularMessageDialogManager.Instance.AddDialog(text, handlerA, labelA);
        }

        //Common dialog callbacks
        public void ReturnToLevelGrid(ModularMessageDialog dialog)
        {
            //close the dialog
            dialog.Deactivate();

            //reactive the grid
            shared.levelGrid.Active = true;
        }



        public void OnSelect(UIGrid grid)
        {
            // Pressing A should activate the popup.
            //Debug.Assert(false, "This should no longer be called.  But it actually is if you select a world with the gamepad...");

        }   // end of OnSelect

        public void OnCancel(UIGrid grid)
        {
            // Gets called when PopOut menu is cancelled via B button on controller.
            ReturnToPreviousMenu();
        }

        public void OnMoveLeft(UIGrid grid)
        {
            shared.scrollOpCount += 1;
            shared.mainCursor.StartShifting(-1);
        }

        public void OnMoveRight(UIGrid grid)
        {
            shared.scrollOpCount += 1;
            shared.mainCursor.StartShifting(1);
        }

        public override bool Refresh(List<UpdateObject> updateList, List<RenderObject> renderList)
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

                    if (shared.bucketsGrid != null)
                    {
                        shared.bucketsGrid.Active = true;
                    }
                    if (shared.levelGrid != null)
                    {
                        shared.levelGrid.Active = true;
                    }

                    if (shared.mainBrowser != null)
                    {
                        shared.mainBrowser.Reset();
                    }

                    if (shared.altBrowser != null)
                    {
                        shared.altBrowser.Reset();
                    }
                }
                else
                {
                    if (shared.levelGrid != null)
                    {
                        shared.levelGrid.Active = false;
                        shared.levelGrid.UnloadInstanceContent();
                    }
                    if (shared.bucketsGrid != null)
                    {
                        shared.bucketsGrid.Active = false;
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

        #endregion

        #region Internal

        override public void Activate()
        {
            if (state != States.Active)
            {
                sharingCloseSessionConfirmMessage.Deactivate();
                sharingLeaveSessionConfirmMessage.Deactivate();
                sharingSessionEndedMessage.Deactivate();

                shared.levelGrid.Clear();

                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Push(commandMap);
                HelpOverlay.Push("LoadLevelMenu");

                if (OriginalBrowserType == LevelBrowserType.Community)
                {
                    shared.mainBrowser = shared.remoteBrowser = shared.srvBrowser = new CommunityLevelBrowser();
                    UiOpenInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.CommunityUI);
                }
                else if (OriginalBrowserType == LevelBrowserType.Local)
                {
                    // If we tried to import a level but it's from a newer version
                    // tell the user that a new version is available.
                    if (!LevelPackage.ImportAllLevels(null))
                    {
                        //GamePadInput.CreateNewerVersionDialog();
                    }

                    shared.remoteBrowser = null;
                    shared.mainBrowser = shared.localBrowser = new LocalLevelBrowser();
                    UiOpenInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.LocalStorageUI);
                }
                else if (OriginalBrowserType == LevelBrowserType.Sharing)
                {
                    Debug.Assert(false, "Should not get here since we no longer have a Sharing browser.");
                }

                Guid desiredSelection = Guid.Empty;

                if (OriginalBrowserType == LevelBrowserType.Local)
                {
                    // Try to restore the selection to the level previously selected.
                    desiredSelection = RememberedState.selectedLevelId;
                    shared.levelFilter.FilterGenres = RememberedState.showOnly;
                    shared.levelSorter.SortBy = RememberedState.sortBy;
                    shared.levelSorter.SortDirection = RememberedState.sortDirection;
                }
                else
                {
                    // Reset browsing state for sharing and community browsers.
                    desiredSelection = Guid.Empty;
                    shared.levelFilter.FilterGenres = Genres.All;
                    shared.levelSorter.SortBy = SortBy.Date;
                    shared.levelSorter.SortDirection = SortDirection.Descending;
                }

                shared.SetAuxMenuDefaultSelections();

                if (shared.mainBrowser != null)
                {
                    shared.mainCursor = shared.mainBrowser.OpenCursor(
                        desiredSelection,
                        shared.levelSorter,
                        shared.levelFilter,
                        CursorFetchingCallback,
                        CursorFetchCompleteCallback,
                        CursorShiftedCallback,
                        CursorJumpedCallback,
                        CursorAdditionCallback,
                        CursorRemovalCallback,
                        LoadLevelMenuUIGrid.kWidth);
                }

                if (shared.altBrowser != null)
                {
                    shared.altCursor = shared.altBrowser.OpenCursor(
                        desiredSelection,
                        shared.levelSorter,
                        shared.levelFilter,
                        CursorFetchingCallback,
                        CursorFetchCompleteCallback,
                        CursorShiftedCallback,
                        CursorJumpedCallback,
                        CursorAdditionCallback,
                        CursorRemovalCallback,
                        LoadLevelMenuUIGrid.kWidth);
                }

                pendingState = States.Active;
                BokuGame.objectListDirty = true;

                AuthUI.ShowStatusDialog(DialogManagerX.CurrentFocusDialogCamera);
            }
        }

        public void ActivateAttaching()
        {
            if (state != States.Active)
            {
                shared.isAttaching = true;
                shared.levelGrid.Clear();

                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Push(commandMap);
                HelpOverlay.Push("LoadLevelMenu");

                // If we tried to import a level but it's from a newer version
                // tell the user that a new version is available.
                if (!LevelPackage.ImportAllLevels(null))
                {
                    //GamePadInput.CreateNewerVersionDialog();
                }

                shared.mainBrowser = new LocalLevelBrowser();
                UiOpenInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.LocalStorageUI);

                Guid desiredSelection = Guid.Empty;

                if (InGame.XmlWorldData!=null && InGame.XmlWorldData.LinkedToLevel!=null)
                {
                    // Try to return to the selection to the level previously stored in the reflex data
                    desiredSelection = (Guid)InGame.XmlWorldData.LinkedToLevel;
                    shared.levelFilter.FilterGenres = Genres.All;
                }
                else
                {
                    desiredSelection = RememberedState.selectedLevelId;
                    shared.levelFilter.FilterGenres = RememberedState.showOnly;
                }

                shared.levelSorter.SortBy = RememberedState.sortBy;
                shared.levelSorter.SortDirection = RememberedState.sortDirection;

                shared.SetAuxMenuDefaultSelections();

                if (shared.mainBrowser != null)
                {
                    shared.mainCursor = shared.mainBrowser.OpenCursor(
                        desiredSelection,
                        shared.levelSorter,
                        shared.levelFilter,
                        CursorFetchingCallback,
                        CursorFetchCompleteCallback,
                        CursorShiftedCallback,
                        CursorJumpedCallback,
                        CursorAdditionCallback,
                        CursorRemovalCallback,
                        LoadLevelMenuUIGrid.kWidth);
                }

                pendingState = States.Active;
                BokuGame.objectListDirty = true;
            }
        }

        private void CursorAdditionCallback(ILevelSetCursor cursor, int index)
        {
            shared.LevelAdded(cursor, index);
        }

        private void CursorRemovalCallback(ILevelSetCursor cursor, int index)
        {
            shared.LevelRemoved(cursor, index);
        }

        private void CursorFetchingCallback(ILevelSetQuery query)
        {
            shared.showPagingMessage = true;
        }

        private void CursorFetchCompleteCallback(ILevelSetQuery query)
        {
            shared.showPagingMessage = false;
        }

        private void CursorShiftedCallback(ILevelSetCursor cursor, int desired, int actual)
        {
            shared.CursorShifted(cursor, desired, actual);
        }

        private void CursorJumpedCallback(ILevelSetCursor cursor)
        {
            shared.CursorJumped(cursor);
        }

        //helper function that will re-load to ensure the linked level ids and set the downloads genre
        //pre: level exists and was successfully downloaded
        private LevelMetadata ProcessDownloadedLevel(Guid worldId)
        {
            if (!XmlDataHelper.CheckWorldExistsByGenre(worldId, Genres.Downloads))
            {
                return null;
            }

            LevelMetadata localLevel = XmlDataHelper.LoadMetadataByGenre(worldId, Genres.Downloads);

            localLevel.Genres |= Genres.Downloads;
            localLevel.Genres &= ~Genres.Favorite;

            //make sure we save the added downloads genre flag
            XmlDataHelper.UpdateWorldMetadata(localLevel);

            if (shared.localBrowser != null)
            {
                shared.localBrowser.AddLevel(localLevel);
            }

            localLevel.DownloadState = LevelMetadata.DownloadStates.Complete;
            localLevel.Downloads++;
            return localLevel;
        }

        private void BackwardLinkDownloadComplete(WorldDataPacket packet, byte[] thumbnailBytes, Guid worldId)
        {
            if (!XmlDataHelper.WriteWorldDataPacketToDisk(packet, thumbnailBytes, DateTime.Now))
            {
                Debug.WriteLine("Failed walking links backward on world id {0}", worldId.ToString());
                shared.mainCursor.SetLevelDownloadState(worldId, LevelMetadata.DownloadStates.Failed);
            }
            else
            {
                shared.mainCursor.SetLevelDownloadState(worldId, LevelMetadata.DownloadStates.Complete);

                //this will re-load to ensure the linked level ids are loaded - metadata from download doesn't contain them
                LevelMetadata localLevel = ProcessDownloadedLevel(worldId);

                if (localLevel == null)
                {
                    shared.mainCursor.SetLevelDownloadState(worldId, LevelMetadata.DownloadStates.Failed);
                }
                else if (localLevel.LinkedFromLevel != null)
                {
                    shared.StartDownloadToFirstLink((Guid)localLevel.LinkedFromLevel);
                }
                else if (localLevel.LinkedToLevel != null)
                {
                    //base case - hit the last link, time to walk forward
                    shared.StartDownloadToLastLink((Guid)localLevel.LinkedToLevel);
                }
            }
        }

        private void ForwardLinkDownloadComplete(WorldDataPacket packet, byte[] thumbnailBytes, Guid worldId)
        {
            if (!XmlDataHelper.WriteWorldDataPacketToDisk(packet, thumbnailBytes, DateTime.Now))
            {
                shared.mainCursor.SetLevelDownloadState(worldId, LevelMetadata.DownloadStates.Failed);
                Debug.WriteLine("Failed walking links forward on world id {0}", worldId.ToString());
            }
            else
            {
                shared.mainCursor.SetLevelDownloadState(worldId, LevelMetadata.DownloadStates.Complete);

                //this will re-load to ensure the linked level ids are loaded - metadata from download doesn't contain them
                LevelMetadata localLevel = ProcessDownloadedLevel(worldId);

                if (localLevel == null)
                {
                    shared.mainCursor.SetLevelDownloadState(worldId, LevelMetadata.DownloadStates.Failed);
                }
                else if (localLevel.LinkedFromLevel != null)
                {
                    shared.StartDownloadToLastLink((Guid)localLevel.LinkedFromLevel);
                }
                else
                {
                    //we're done - show the message indicating linked levels were downloaded
                    ShowSimpleDialog("loadLevelMenu.confirmLinkedDownloadMessage");
                }
            }
        }



        private void WorldDownloadComplete(WorldDataPacket packet, byte[] thumbnailBytes, LevelMetadata level)
        {
            if (!XmlDataHelper.WriteWorldDataPacketToDisk(packet, thumbnailBytes, level.LastWriteTime))
            {
                level.DownloadState = LevelMetadata.DownloadStates.Failed;
            }
            else
            {
                //this will re-load to ensure the linked level ids are loaded - metadata from download doesn't contain them
                LevelMetadata localLevel = ProcessDownloadedLevel(level.WorldId);

                if (localLevel == null)
                {
                    shared.mainCursor.SetLevelDownloadState(level.WorldId, LevelMetadata.DownloadStates.Failed);
                }
                else if (localLevel.LinkedFromLevel != null)
                {
                    shared.StartDownloadToFirstLink((Guid)localLevel.LinkedFromLevel);
                }
                else if (localLevel.LinkedToLevel != null)
                {
                    shared.StartDownloadToLastLink((Guid)localLevel.LinkedToLevel);
                }
            }
        }

        override public void Deactivate()
        {
            if (state != States.Inactive)
            {
                // if we were attaching, we aren't anymore
                shared.isAttaching = false;

                sharingCloseSessionConfirmMessage.Deactivate();
                sharingLeaveSessionConfirmMessage.Deactivate();
                sharingSessionEndedMessage.Deactivate();

                if (shared.popup != null)
                {
                    shared.popup.Active = false;
                }


                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Pop(commandMap);
                HelpOverlay.Pop();

                // Remember previous state BEFORE clearing it.
                if (OriginalBrowserType == LevelBrowserType.Local)
                {
                    // Remember the browser settings for this view of the local level menu.
                    RememberedState.selectedLevelId = shared.levelGrid.CurrentLevelId;
                    RememberedState.showOnly = shared.levelFilter.FilterGenres;
                    RememberedState.sortBy = shared.levelSorter.SortBy;
                    RememberedState.sortDirection = shared.levelSorter.SortDirection;
                }

                if (shared.levelGrid != null)
                {
                    shared.levelGrid.Active = false;
                    shared.levelGrid.Clear();
                }
                if (shared.bucketsGrid != null)
                {
                    shared.bucketsGrid.Active = false;
                }


                //
                // Shut down all the browser instances.
                //
                if (shared.mainBrowser != null)
                {
                    shared.mainBrowser.CloseCursor(ref shared.mainCursor);
                    shared.mainBrowser.CloseCursor(ref shared.altCursor);
                    shared.mainBrowser.Shutdown();
                    shared.mainBrowser = null;
                }

                if (shared.altBrowser != null)
                {
                    shared.altBrowser.CloseCursor(ref shared.mainCursor);
                    shared.altBrowser.CloseCursor(ref shared.altCursor);
                    shared.altBrowser.Shutdown();
                    shared.altBrowser = null;
                }

                if (shared.remoteBrowser != null)
                {
                    shared.remoteBrowser.Shutdown();
                    shared.remoteBrowser = null;
                }
                if (shared.localBrowser != null)
                {
                    shared.localBrowser.Shutdown();
                    shared.localBrowser = null;
                }
                if (shared.srvBrowser != null)
                {
                    shared.srvBrowser.Shutdown();
                    shared.srvBrowser = null;
                }

                Instrumentation.StopTimer(UiOpenInstrument);

                pendingState = States.Inactive;
                BokuGame.objectListDirty = true;

                AuthUI.HideAllDialogs();
            }
        }   // End of Deactivate()

        public void ReturnToPreviousMenu()
        {
            Foley.PlayBack();
            ActivateMenu(ReturnToMenu);
        }

        public void ActivateMenu(ReturnTo whichMenu)
        {
            Deactivate();

            switch (whichMenu)
            {
                case ReturnTo.MainMenu:
                    SceneManager.SwitchToScene("MainMenuScene");
                    break;

                case ReturnTo.MiniHub:
                    InGame.inGame.SwitchToMiniHub();
                    break;

                case ReturnTo.EditWorldParameters:
                    SceneManager.SwitchToScene("WorldSettingsMenuScene");
                    break;

                case ReturnTo.Editor:
                    SceneManager.SwitchToScene("EditWorldScene");
                    break;
            }
        }

        public void LoadContent(bool immediate)
        {
            BokuGame.Load(renderObj, immediate);
            BokuGame.Load(sharingCloseSessionConfirmMessage, immediate);
            BokuGame.Load(sharingLeaveSessionConfirmMessage, immediate);
            BokuGame.Load(sharingSessionEndedMessage, immediate);

        }   // end of LoadLevelMenu LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
            // TODO (****) *** How does this get scaled if window changes size???
            Point deviceSize = new Point(device.Viewport.Width, device.Viewport.Height);

            blockingOpMessage = new SimpleMessage();
            blockingOpMessage.Center = new Point(deviceSize.X / 2, deviceSize.Y / 2);

            Texture2D messageTexture = KoiLibrary.LoadTexture2D(@"Textures\Terrain\WaitPicture");
            blockingOpMessage.AddTexture(messageTexture);
            blockingOpMessage.Size = new Point(deviceSize.Y / 3, deviceSize.Y / 3);

            blockingOpMessage.Text = "Text Unset";
            blockingOpMessage.Font = SharedX.GetGameFont18Bold;
            blockingOpMessage.TextCenter = new Point(
                blockingOpMessage.Center.X,
                blockingOpMessage.Center.Y + blockingOpMessage.Size.Y / 2);
        }

        public void UnloadContent()
        {
            BokuGame.Unload(shared);
            BokuGame.Unload(renderObj);
            BokuGame.Unload(sharingCloseSessionConfirmMessage);
            BokuGame.Unload(sharingLeaveSessionConfirmMessage);
            BokuGame.Unload(sharingSessionEndedMessage);
        }   // end of LoadLevelMenu UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="device"></param>
        public void DeviceReset(GraphicsDevice device)
        {
            BokuGame.DeviceReset(shared, device);
            BokuGame.DeviceReset(renderObj, device);
            BokuGame.DeviceReset(sharingCloseSessionConfirmMessage, device);
            BokuGame.DeviceReset(sharingLeaveSessionConfirmMessage, device);
            BokuGame.DeviceReset(sharingSessionEndedMessage, device);
        }

        public bool SkipToLevel(Guid levelId, Genres genres)
        {
            string bucket = Utils.FolderNameFromFlags(genres);
            string fullPath = BokuGame.Settings.MediaPath + bucket + levelId.ToString() + @".Xml";

            //attempt to play the level
            if (!PlayLevelFromPath(fullPath))
            {
                return false;
            }
            return true;
        }

        private bool PlayLevelFromPath(string fullPath)
        {
            // Console.WriteLine("Trying to load level from string: " + fullPath);
            if (!Storage4.FileExists(fullPath, StorageSource.All))
            {
                // Console.WriteLine("Load failed on {0}! This string will be ignored for the rest of the game.", fullPath);
                return false;
            }

            LoadingFromString = true;

            blockingOpMessage.Text = Strings.Localize("loadLevelMenu.loadingLevelMessage");
            InGame.AddMessage(blockingOpMessage.Render, null);

            // Shut down the grids, if active.
            shared.levelGrid.Active = false;
            shared.bucketsGrid.Active = false;

            // Queue the play operation to happen in the next frame so that we can render the "please wait" message first.
            FrameDelayedOperation op =
                new FrameDelayedOperation(shared.Callback_PlayLinkedLevel, fullPath, null);
            AsyncOps.Enqueue(op);

            Time.Paused = true;
            return true;
        }

        /// <summary>
        /// Converts byte array to Base64 while also doing character
        /// substitutions to ensure that the resutling string is 
        /// valid to use in a URL.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ToBase64String(params byte[] bytes)
        {
            var base64String = Convert.ToBase64String(bytes);
            base64String = base64String.Replace('/', '_');
            base64String = base64String.Replace('+', '-');
            return base64String;
        }

        #endregion

    }   // end of class LoadLevelMenu

}   // end of namespace Boku
