
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
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Geometry;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI;
using KoiX.UI.Dialogs;

using Boku;
using Boku.Base;
using Boku.Common;
using Boku.Common.Sharing;
using Boku.Common.Xml;
using Boku.Web;

using BokuShared;
using BokuShared.Wire;

namespace KoiX.Scenes
{
    /// <summary>
    /// The main scene for loading worlds.  
    /// With online browser, works as Community menu.
    /// With fly-out changes, also works as browser for linking levels.
    /// </summary>
    public class LoadLevelScene : BasePageScene
    {
        #region Members

        LevelBrowserType browserType = LevelBrowserType.Local;

        Texture2D bkgTexture;
        string bkgTextureName;

        SystemFont pageTitleFont;
        SystemFont worldTitleFont;
        SystemFont font;

        Label title;
        string titleLabelId;

        WidgetSet fullSet;              // The full region we use for UI.  Excludes the title and back button.
        WidgetSet upperLeftSet;
        WidgetSet lowerRightSet;

        WidgetSet categoryButtonsSet;   // Contains category buttons.
        Button myWorldsButton;
        Button downloadsButton;
        Button lessonsButton;
        Button samplesButton;
        Button allButton;

        WidgetSet searchSortSet;        // Contains search and sort UI.
        SingleLineTextEditBox searchBox;
        Button sortButton;
        SortDialog sortDialog;

        TextBox titleTextBox;           // Use TextBoxes instead of Labels since TextBoxes
        TextBox dateCreatorTextBox;     // support wrapping to multiple lines.
        TextBox tagsTextBox;

        TextBox statusTextBox;          // Used to display "No worlds" and "Searching" messages.

        Label descriptionLabel;
        ScrollableTextBox descriptionTextBox;

        BrowserWorldsDisplay worldsDisplay;

        FlyoutDialog flyoutDialog;

        GraphicButton leftArrow;            // Buttons for mouse/touch scrolling of world list.
        GraphicButton rightArrow;
        GraphicButton leftBumper;           // Should we be using GraphicButton for this?
        GraphicButton rightBumper;

        int margin = 8;                     // Around button text.


        bool isUserAdmin = false;           // Does the user have admin privileges?
        bool isMyWorld = false;             // Is the current world one of the user's?
        bool isBuiltInWorld = false;        // Is the current world one of start worlds?
        bool isDownload = false;            // Is the current world an unmodified downloaded world?

        bool isDeleteActive = false;        // Is the user allowed to delete this world?

        bool isLinking = false;             // Are we linking levels?

        ILevelBrowser browser = null;
        ILevelSetCursor cursor = null;

        LevelSetFilterByKeywords levelFilter;   // Used when filtering by search string.
        LevelSetSorterBasic levelSorter;        // Controlled by category buttons.

        bool showPagingMessage = false;         // Flashes "fetching" message.

        // Where we save the current state so re-entering the LoadLevelMenu
        // brings us back to where we were when we left.  Note that this is
        // only used for local browsing.  Community browsing is always reset.
        Guid previouSelectedLevelId = Guid.Empty;
        Genres previouShowOnly = Genres.All;
        SortBy previouSortBy = SortBy.Date;
        SortDirection previouSortDirection = SortDirection.Descending;

        // Instrumentation.
        object UiOpenInstrument;

        #endregion

        #region Accessors

        /// <summary>
        /// The current world in focus for this browser.
        /// </summary>
        public LevelMetadata CurWorld
        {
            get
            {
                LevelMetadata cur = null;
                if (cursor != null)
                {
                    cur = cursor[0];
                }
                return cur;
            }
        }

        #endregion


        #region Public

        public LoadLevelScene(string sceneName, LevelBrowserType browserType, bool isLinking = false)
            : base(sceneName)
        {
            this.browserType = browserType;
            this.isLinking = isLinking;

            Debug.Assert(!isLinking || (isLinking && browserType == LevelBrowserType.Local), "Linking only works with local browser.");

            switch (browserType)
            {
                case LevelBrowserType.Local:
                    bkgTextureName = @"Textures\LoadLevel\LocalBackground";
                    titleLabelId = isLinking ? "loadLevelMenu.linkingTitle" : "loadLevelMenu.localTitle";
                    break;
                case LevelBrowserType.Community:
                    bkgTextureName = @"Textures\LoadLevel\CommunityBackground";
                    titleLabelId = "loadLevelMenu.communityTitle";
                    break;
                default:
                    Debug.Assert(false, "No other types are supported");
                    break;
            }

            ThemeSet theme = Theme.CurrentThemeSet;

            // Overwrite the button theme for our latched buttons.
            // Text color for selected should stay white.
            // Body color should go green.
            theme.ButtonSelected.TextColor = theme.LightTextColor;
            theme.ButtonSelectedFocused.TextColor = theme.LightTextColor;
            theme.ButtonSelectedHover.TextColor = theme.LightTextColor;
            theme.ButtonSelectedFocusedHover.TextColor = theme.LightTextColor;
            theme.ButtonSelected.BodyColor = theme.FocusColor;
            theme.ButtonSelectedFocused.BodyColor = theme.FocusColor;
            theme.ButtonSelectedHover.BodyColor = theme.FocusColor;
            theme.ButtonSelectedFocusedHover.BodyColor = theme.FocusColor;

            pageTitleFont = new SystemFont(theme.TextFontFamily, theme.TextTitleFontSize * 2.0f, System.Drawing.FontStyle.Bold);
            worldTitleFont = new SystemFont(theme.TextFontFamily, theme.TextTitleFontSize * 1.0f, System.Drawing.FontStyle.Bold);
            font = new SystemFont(theme.TextFontFamily, theme.TextBaseFontSize * 1.0f, System.Drawing.FontStyle.Regular);

            FontWrapper wrapperPageTitle = new FontWrapper(null, pageTitleFont);
            GetFont FontPageTitle = delegate() { return wrapperPageTitle; };
            FontWrapper wrapperWorldTitle = new FontWrapper(null, worldTitleFont);
            GetFont FontWorldTitle = delegate() { return wrapperWorldTitle; };
            FontWrapper wrapper = new FontWrapper(null, font);
            GetFont Font = delegate() { return wrapper; };

            title = new Label(fullScreenContentDialog, pageTitleFont, theme.LightTextColor, outlineColor: theme.DarkTextColor, outlineWidth: 2.5f, labelId: titleLabelId);
            title.Position = new Vector2(150, 5);
            title.Size = title.CalcMinSize();
            fullScreenContentDialog.AddWidget(title);

            //
            // WidgetSets
            // 
            fullSet = new WidgetSet(fullScreenContentDialog, new RectangleF(150, 100, 1300, 800), Orientation.None);
            fullSet.IgnoreRectWhenHitTesting = true;    // Allows all browser worlds to be hit.
            fullScreenContentDialog.AddWidget(fullSet);

            upperLeftSet = new WidgetSet(fullScreenContentDialog, new RectangleF(0, 0, 1300, 800), Orientation.Vertical, horizontalJustification: Justification.Left, verticalJustification: Justification.Top);
            fullSet.AddWidget(upperLeftSet);

            lowerRightSet = new WidgetSet(fullScreenContentDialog, new RectangleF(650, 400, 650, 400), Orientation.Vertical, horizontalJustification: Justification.Left, verticalJustification: Justification.Top);
            fullSet.AddWidget(lowerRightSet);

            //
            // Category buttons.
            //
            {
                categoryButtonsSet = new WidgetSet(fullScreenContentDialog, new RectangleF(0, 0, 1300, 10), Orientation.Horizontal, Justification.Left);
                upperLeftSet.AddWidget(categoryButtonsSet);

                myWorldsButton = new Button(fullScreenContentDialog, RectangleF.EmptyRect, labelId: "loadLevelMenu.showMyWorlds", OnChange: OnMyWorlds, theme: theme);
                categoryButtonsSet.AddWidget(myWorldsButton);

                if (browserType != LevelBrowserType.Community)
                {
                    downloadsButton = new Button(fullScreenContentDialog, RectangleF.EmptyRect, labelId: "loadLevelMenu.showDownloads", OnChange: OnDownloads, theme: theme);
                    categoryButtonsSet.AddWidget(downloadsButton);
                }

                lessonsButton = new Button(fullScreenContentDialog, RectangleF.EmptyRect, labelId: "loadLevelMenu.showLessons", OnChange: OnLessons, theme: theme);
                categoryButtonsSet.AddWidget(lessonsButton);

                samplesButton = new Button(fullScreenContentDialog, RectangleF.EmptyRect, labelId: "loadLevelMenu.showsamples", OnChange: OnSamples, theme: theme);
                categoryButtonsSet.AddWidget(samplesButton);

                allButton = new Button(fullScreenContentDialog, RectangleF.EmptyRect, labelId: "loadLevelMenu.showAll", OnChange: OnAll, theme: theme);
                categoryButtonsSet.AddWidget(allButton);

                // Calc size of largest button and use that for all.
                Vector2 maxSize = Vector2.Zero;
                foreach (BaseWidget w in categoryButtonsSet.Widgets)
                {
                    w.Padding = new Padding(margin);    // Make the buttosn a bit "thicker" just to look better.
                    maxSize = MyMath.Max(maxSize, w.CalcMinSize());

                    // Also set any common state we ned for these buttons.
                    (w as Button).Latchable = true;
                    w.Focusable = false;    // For this scene we only want the search box and worlds display to be focusable.
                }
                // Add a bit of margin around the text.
                //maxSize += new Vector2(margin, 0);
                foreach (BaseWidget w in categoryButtonsSet.Widgets)
                {
                    w.Size = maxSize;
                    w.Margin = new Padding(0, 0, margin, 0);    // Adds a horizontal gap between the buttons.
                }
                // Shrink vertical size of set to match button height.
                categoryButtonsSet.Size = new Vector2(categoryButtonsSet.Size.X, maxSize.Y);

                // Since we're treating this as a set of radio buttons, need to have one set.  Start with All.
                allButton.Selected = true;
            }

            //
            // Search and sort.
            //
            {
                searchSortSet = new WidgetSet(fullScreenContentDialog, new RectangleF(0, 0, 1300, 72), Orientation.Horizontal, horizontalJustification: Justification.Left, verticalJustification: Justification.Center);
                upperLeftSet.AddWidget(searchSortSet);

                searchBox = new SingleLineTextEditBox(fullScreenContentDialog, Font, 500, "", "", theme: theme);
                searchBox.ShowSearchIcon = true;
                searchBox.Margin = new Padding(32, 0, 32, 0);
                searchSortSet.AddWidget(searchBox);

                sortButton = new Button(fullScreenContentDialog, RectangleF.EmptyRect, labelId: "loadLevelMenu.sortBy", OnChange: OnSort, element: GamePadInput.Element.YButton, theme: theme);
                sortButton.Margin = new Padding(32, 0, 32, 0);
                // Set size of sort button to max possible size.
                sortButton.Label.LabelText = Strings.Localize("loadLevelMenu.sortBy") + Strings.Localize("loadLevelMenu.sortCreator");
                sortButton.Size = sortButton.CalcMinSize();
                sortButton.Label.LabelText = Strings.Localize("loadLevelMenu.sortBy") + Strings.Localize("loadLevelMenu.sortCreator");
                sortButton.Size = MyMath.Max(sortButton.Size, sortButton.CalcMinSize());
                sortButton.Label.LabelText = Strings.Localize("loadLevelMenu.sortBy") + Strings.Localize("loadLevelMenu.sortDate");
                sortButton.Size = MyMath.Max(sortButton.Size, sortButton.CalcMinSize());
                sortButton.Focusable = false;
                searchSortSet.AddWidget(sortButton);

                RectangleF rect = new RectangleF(new Vector2(-100, -300), new Vector2(360, 260));
                sortDialog = new SortDialog(rect, "loadLevelMenu.sortBy", theme: theme);
                sortDialog.OnDeactivate = OnSortDeactivate;
            }

            //
            // Scroll arrows.  Note these are not in any of the WidgetSets we've created to fit the
            // 1600x1000 UI area.  We want these to be at the edges of the screen, wherever that is.
            //
            {
                leftArrow = new GraphicButton(fullScreenContentDialog, new RectangleF(0, 0, 64, 64), @"Textures\LoadLevel\Arrow_Left", OnLeftArrow, autorepeat: true);
                fullScreenContentDialog.AddWidget(leftArrow);

                rightArrow = new GraphicButton(fullScreenContentDialog, new RectangleF(0, 0, 64, 64), @"Textures\LoadLevel\Arrow_Right", OnRightArrow, autorepeat: true);
                fullScreenContentDialog.AddWidget(rightArrow);
            }

            //
            // Bumpers for selecting catagories.
            //
            {
                leftBumper = new GraphicButton(fullScreenContentDialog, new RectangleF(0, 0, 128, 128), @"Textures\LoadLevel\L_bumper", OnLeftBumper, autorepeat: true);
                fullSet.AddWidget(leftBumper);

                rightBumper = new GraphicButton(fullScreenContentDialog, new RectangleF(0, 0, 128, 128), @"Textures\LoadLevel\R_bumper", OnRightBumper, autorepeat: true);
                fullSet.AddWidget(rightBumper);
            }

            //
            // World tiles.
            //
            {
                worldsDisplay = new BrowserWorldsDisplay(fullScreenContentDialog, browserType, OnLeftArrow, OnRightArrow, OnSelect);
                worldsDisplay.Position = new Vector2(170, 150);
                fullSet.AddWidget(worldsDisplay);
            }

            //
            // Flyout menu.
            //
            {
                flyoutDialog = new FlyoutDialog(this, isLinking: isLinking);
            }

            //
            // Current world title, creator and date.  Tags?
            //
            {
                titleTextBox = new TextBox(fullScreenContentDialog, FontWorldTitle, theme.DarkTextColor, outlineColor: theme.LightTextColor, outlineWidth: 0.75f, displayText: "Title of World");
                titleTextBox.Width = 600;
                fullSet.AddWidget(titleTextBox);

                dateCreatorTextBox = new TextBox(fullScreenContentDialog, Font, theme.DarkTextColor, outlineColor: theme.LightTextColor, outlineWidth: 0.75f, displayText: "Date by Creator");
                dateCreatorTextBox.Width = 600;
                fullSet.AddWidget(dateCreatorTextBox);

                tagsTextBox = new TextBox(fullScreenContentDialog, Font, theme.DarkTextColor, outlineColor: theme.LightTextColor, outlineWidth: 0.75f, displayText: "Tags");
                tagsTextBox.Width = 600;
                fullSet.AddWidget(tagsTextBox);

                titleTextBox.Position = new Vector2(32, 480);
                dateCreatorTextBox.Position = new Vector2(32, 520);
                tagsTextBox.Position = new Vector2(32, 560);
            }

            //
            // Status message.
            //
            {
                statusTextBox = new TextBox(fullScreenContentDialog, FontPageTitle, theme.LightTextColor, outlineColor: theme.DarkTextColor, outlineWidth: 2.5f, displayText: "Status Message");
                statusTextBox.Justification = TextHelper.Justification.Center;
                statusTextBox.Width = 700;
                statusTextBox.Position = new Vector2(270, 180);
                fullSet.AddWidget(statusTextBox);
            }

            //
            // Description box.
            //
            {
                descriptionLabel = new Label(fullScreenContentDialog, font, theme.DarkTextColor, outlineColor: theme.LightTextColor, outlineWidth: 0.75f, labelId: "loadLevelMenu.Description");
                descriptionLabel.Size = descriptionLabel.CalcMinSize();
                lowerRightSet.AddWidget(descriptionLabel);

                RectangleF rect = new RectangleF(0, descriptionLabel.Size.Y, lowerRightSet.Size.X, lowerRightSet.Size.Y - descriptionLabel.Size.Y);
                descriptionTextBox = new ScrollableTextBox(fullScreenContentDialog, rect, font, theme: theme);
                descriptionTextBox.Focusable = false;
                lowerRightSet.AddWidget(descriptionTextBox);
            }

            // Call Recalc for force all the button positions and sizes to be calculated.
            // We need this in order to properly calc the links.
            fullScreenContentDialog.Dirty = true;
            fullScreenContentDialog.Recalc();

            // Skip this and handle manually.
            // Connect navigation links.
            //fullScreenContentDialog.CreateTabList();

            // Dpad nav includes all widgets.
            //fullScreenContentDialog.CreateDPadLinks();



            /*
            // DEBUG ONLY!!!
            (fullScreenContentDialog.WidgetsDEBUGONLY[1] as WidgetSet).RenderDebugOutline = true;
            //(fullScreenContentDialog.WidgetsDEBUGONLY[1] as WidgetSet).SetMarginDebugOnChildren(true);

            upperLeftSet.RenderDebugOutline = true;
            categoryButtonsSet.RenderDebugOutline = true;
            searchSortSet.RenderDebugOutline = true;
            lowerRightSet.RenderDebugOutline = true;
            fullSet.RenderDebugOutline = true;
            */

            //
            //
            // Browsers and supporting objects.
            //
            //

            levelFilter = new LevelSetFilterByKeywords();
            if (browserType == LevelBrowserType.Community)
            {
                levelFilter.ServerSideMatching = true;
            }
            else
            {
                levelFilter.ServerSideMatching = false;
            }

            levelFilter.FilterGenres = Genres.All;
            ApplyCategoryFiltering();

            levelSorter = new LevelSetSorterBasic();
            levelSorter.SortBy = SortBy.Date;
            levelSorter.SortDirection = SortDirection.Descending;



        }   // end of c'tor

        //
        // Callbacks.
        //

        void OnSelect(BaseWidget w)
        {
            // Only launch flyout if we have a valid world.
            if (CurWorld != null)
            {
                // Set position.  In particular, we need to set the height
                // to match the center of the focus tile.  When launched, 
                // the flyout will figure out which buttons to include and
                // then adjust it's position relative to this.
                RectangleF rect = flyoutDialog.Rectangle;
                // Magic numbers just set to look good.
                rect.Position = new Vector2(450, 304) + fullSet.Position - camera.ScreenSize / 2.0f / camera.Zoom;
                flyoutDialog.Rectangle = rect;

                flyoutDialog.LocalBrowser = browserType == LevelBrowserType.Local;
                flyoutDialog.CurrentWorld = CurWorld;

                DialogManagerX.ShowDialog(flyoutDialog, camera);
            }
        }   // end of OnSelect()

        void OnLeftArrow(BaseWidget w)
        {
            PrevWorld();
        }   // end of OnLeftArrow()

        void OnRightArrow(BaseWidget w)
        {
            NextWorld();
        }   // end of OnRightArrow()

        void OnLeftBumper(BaseWidget w)
        {
            // Do nothing, this is just for display.
        }   // end of OnLeftBumper()

        void OnRightBumper(BaseWidget w)
        {
            // Do nothing, this is just for display.
        }   // end of OnRightBumper()

        void OnMyWorlds(BaseWidget w)
        {
            Button b = w as Button;
            if (b != null)
            {
                SpoofRadioButton(b);

                levelFilter.FilterGenres = Genres.MyWorlds;
                ApplyCategoryFiltering();
            }
        }   // end of OnMyWorlds()

        void OnDownloads(BaseWidget w)
        {
            Button b = w as Button;
            if (b != null)
            {
                SpoofRadioButton(b);

                levelFilter.FilterGenres = Genres.Downloads;
                ApplyCategoryFiltering();
            }
        }   // end of OnDownloads()

        void OnLessons(BaseWidget w)
        {
            Button b = w as Button;
            if (b != null)
            {
                SpoofRadioButton(b);

                levelFilter.FilterGenres = Genres.Lessons;
                ApplyCategoryFiltering();
            }
        }   // end of OnLessons()

        void OnSamples(BaseWidget w)
        {
            Button b = w as Button;
            if (b != null)
            {
                SpoofRadioButton(b);

                levelFilter.FilterGenres = Genres.SampleWorlds;
                ApplyCategoryFiltering();
            }
        }   // end of OnSamples()

        void OnAll(BaseWidget w)
        {
            Button b = w as Button;
            if (b != null)
            {
                SpoofRadioButton(b);

                levelFilter.FilterGenres = Genres.All;
                ApplyCategoryFiltering();
            }
        }   // end of OnAll()

        void OnSort(BaseWidget w)
        {
            DialogManagerX.ShowDialog(sortDialog, camera);
        }   // end of OnSort()

        /// <summary>
        /// Called when the sort dialog is deactivated.
        /// </summary>
        /// <param name="d"></param>
        void OnSortDeactivate(BaseDialog d)
        {
            if (!sortDialog.Cancelled)
            {
                if (levelSorter.SortBy != sortDialog.SortBy)
                {
                    // Changed sort, update and reset sort direction.  
                    levelSorter.SortBy = sortDialog.SortBy;

                    // Update the button label to match.
                    switch (levelSorter.SortBy)
                    {
                        case SortBy.Date:
                            sortButton.Label.LabelText = Strings.Localize("loadLevelMenu.sortBy") + Strings.Localize("loadLevelMenu.sortDate");
                            levelSorter.SortDirection = SortDirection.Descending;
                            break;
                        case SortBy.Creator:
                            sortButton.Label.LabelText = Strings.Localize("loadLevelMenu.sortBy") + Strings.Localize("loadLevelMenu.sortCreator");
                            levelSorter.SortDirection = SortDirection.Ascending;
                            break;
                        case SortBy.Name:
                            sortButton.Label.LabelText = Strings.Localize("loadLevelMenu.sortBy") + Strings.Localize("loadLevelMenu.sortTitle");
                            levelSorter.SortDirection = SortDirection.Ascending;
                            break;
                    }
                }
                else
                {
                    // Picked same sort so toggle sort direction.
                    if (levelSorter.SortDirection == SortDirection.Descending)
                    {
                        levelSorter.SortDirection = SortDirection.Ascending;
                    }
                    else
                    {
                        levelSorter.SortDirection = SortDirection.Descending;
                    }
                }
            }

            worldsDisplay.ResetTileTransforms();
        }   // end of OnSortDeactivate()

        /// <summary>
        /// Common helper function that allows us to make
        /// a set of normal buttons look like radio buttons.
        /// </summary>
        /// <param name="button"></param>
        void SpoofRadioButton(Button button)
        {
            // Clear all buttons.
            foreach (BaseWidget w in categoryButtonsSet.Widgets)
            {
                Button b = w as Button;
                if (b != null && b != button)
                {
                    if (b.Focusable)
                    {
                        b.ClearFocus();
                    }
                    b.Selected = false; // Since these buttons are latchable, we need to 
                                        // forcably clear the selected state.
                }
            }
            button.Selected = true;     // Needed if navigating via tabs.
            if (button.Focusable)
            {
                button.SetFocus();
            }
        }   // end of SpoofRadioButton()

        //
        // Callbacks called by FlyoutDialog.  This keeps all the functionality 
        // here rather than mixing it both here and in the flyout.
        //
        public void FlyoutOnPlay()
        {
            CheckPlaySelectedLevel(playMode: true);
        }   // end of FlyoutOnPlay()

        public void FlyoutOnEdit()
        {
            CheckPlaySelectedLevel(playMode: false);
        }   // end of FlyoutOnEdit()

        public void FlyoutOnExport()
        {
            if (CurWorld != null)
            {
                // First step is to validate links.
                ValidateLinksForExport();
            }
        }   // end of FlyoutOnExport()

        #region Export

        void ValidateLinksForExport()
        {
            // Always operate on first link in a chain of levels.
            // (if no chain, the level will be the first link)
            LevelMetadata level = CurWorld.FindFirstLink();

            if (level != null)
            {
                // Check if the chosen level has any broken links - if so, 
                // warn the player and give them the option to cancel.
                LevelMetadata brokenLevel = null;
                bool forwardsLinkBroken = false;
                if (level.FindBrokenLink(ref brokenLevel, ref forwardsLinkBroken))
                {
                    Button.Callback OnContinueExport = delegate(BaseWidget w)
                    {
                        // TODO (scoy) This leaves the broken link dialog on-screen while the 
                        // Windows file dialog is being shown.  Should we delay the call to
                        // ShowExportDialog a couple of frames?
                        ShowExportDialog(level);
                    };
                    DialogCenter.BrokenLinkExportDialog.ContinueButton.SetOnChange(OnContinueExport);
                    DialogCenter.BrokenLinkExportDialog.CancelButton.SetOnChange(null);
                    DialogManagerX.ShowDialog(DialogCenter.BrokenLinkExportDialog);
                }
                else
                {
                    // Links are all good.
                    ShowExportDialog(level);
                }
            }
        }   // end of ValidateLinks()


        /// <summary>
        /// Shows system dialog for exporting file.  Returns name chosen to export to.
        /// </summary>
        /// <param name="level"></param>
        /// <returns>Filename we exported to, null if user backs out.</returns>
        string ShowExportDialog(LevelMetadata level)
        {
            // Create new SaveFileDialog.
            System.Windows.Forms.SaveFileDialog DialogSave = new System.Windows.Forms.SaveFileDialog();

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
            if (DialogSave.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ExportLevel(level, DialogSave.FileName, null);
                result = DialogSave.FileName;
            }

            DialogSave.Dispose();
            DialogSave = null;

            // TODO (scoy) Show result?  We used to have a dialog here that would 
            // show the path the level was exported to.  Is this worth doing?
            
            // Copy path to clipboard.
            try { System.Windows.Forms.Clipboard.SetText(result); }
            catch { }

            return result;
        }   // end of ShowExportDialog()

        /// <summary>
        /// Hanldes the actual export of the file.
        /// 
        /// pre: assumes level is at the start of the chain
        /// pre: assumes valid filename is passed in
        /// 
        /// </summary>
        /// <param name="level"></param>
        /// <param name="fileName"></param>
        /// <param name="outStream">May be null.  If not null this is used.  If this is null then fileName is used.</param>
        void ExportLevel(LevelMetadata level, string fileName, Stream outStream)
        {
            // Only the first level in a chain should ever make it this far 
            // (higher up, we determine it's a linked chain and pass in the first level).
            Debug.Assert(level.LinkedFromLevel == null);

            // Ensure valid filename.
            Debug.Assert(fileName != null);

            List<string> levelFiles = new List<string>();
            List<string> stuffFiles = new List<string>();
            List<string> thumbnailFiles = new List<string>();
            List<string> screenshotFiles = new List<string>();
            List<string> terrainFiles = new List<string>();

            do
            {
                string folderName = Utils.FolderNameFromFlags(level.Genres);
                string fullPathToLevelFile = Path.Combine(Storage4.UserLocation, BokuGame.Settings.MediaPath, folderName, level.WorldId.ToString() + ".Xml");

                levelFiles.Add(fullPathToLevelFile);

                //load the xml so we can find the stuff, thumbnail and terrain file paths
                XmlWorldData xml = XmlWorldData.Load(fullPathToLevelFile, XnaStorageHelper.Instance);

                if (xml == null)
                {
                    string message = "Failed to open for export:\n" + fullPathToLevelFile;
                    var result = System.Windows.Forms.MessageBox.Show(message);

                    return;
                }

                string fullPathToStuffFile = Path.Combine(Storage4.UserLocation, BokuGame.Settings.MediaPath, xml.stuffFilename);

                stuffFiles.Add(fullPathToStuffFile);

                string fullPathToThumbnailFile = Path.Combine(Storage4.UserLocation, BokuGame.Settings.MediaPath, folderName, xml.GetImageFilenameWithoutExtension() + ".Dds");
                thumbnailFiles.Add(fullPathToThumbnailFile);

                string fullPathToScreenshotFile = Path.Combine(Storage4.UserLocation, BokuGame.Settings.MediaPath, folderName, xml.GetImageFilenameWithoutExtension() + ".Jpg");
                screenshotFiles.Add(fullPathToScreenshotFile);

                // Only provide terrain file if it is not a builtin terrain.
                // TODO Rethink this.  Maybe put terrain in every file.
                string fullPathToTerrainFile = null;
                string partialPathToTerrainFile = Path.Combine(BokuGame.Settings.MediaPath, xml.xmlTerrainData2.virtualMapFile);
                if (Storage4.FileExists(partialPathToTerrainFile, StorageSource.UserSpace))
                {
                    fullPathToTerrainFile = Path.Combine(Storage4.UserLocation, partialPathToTerrainFile);
                }
                terrainFiles.Add(fullPathToTerrainFile);

                // Traverse down the chain to the next link, until finished.
                level = level.NextLink();

            } while (level != null);

            // We have gathered all of the file information, do the export.
            LevelPackage.ExportLevel(
                levelFiles,
                stuffFiles,
                thumbnailFiles,
                screenshotFiles,
                terrainFiles,
                fileName,
                outStream);
        }   // end of ExportLevel()


        /// <summary>
        /// Generates a user-friendly filename for exported file.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="withExtension"></param>
        /// <returns></returns>
        string GenerateDefaultFileName(LevelMetadata level, bool withExtension)
        {
            string fileName = "";
            if (level.PreviousLink() == null && level.NextLink() == null)
            {
                fileName = LevelPackage.CreateExportFilenameWithoutExtension(level.Name, level.Creator);
            }
            else
            {
                fileName = LevelPackage.CreateExportFilenameWithoutExtension(level. Name, level.Creator);
            }

            if (withExtension)
            {
                fileName += ".Kodu2";
            }

            return fileName;
        }   // end of GenerateDefaultFileName()

        #endregion Export

        public void FlyoutOnShare()
        {
            if (CurWorld != null)
            {
                LevelMetadata level = CurWorld;

                // If part of a linked chain, go to the beginning.
                level = level.FindFirstLink();

                string folderName = Utils.FolderNameFromFlags(level.Genres);
                string fullPath = BokuGame.Settings.MediaPath + folderName + level.WorldId.ToString() + @".Xml";

                // Try to share the level.  Display an error if it fails.
                if (!Community.Async_Ping(Callback_Ping, fullPath))
                {
                    DialogCenter.NoCommunityDialog.ContinueButton.SetOnChange(null);
                    DialogManagerX.ShowDialog(DialogCenter.NoCommunityDialog);
                }

            }
        }   // end of FlyoutOnShare()

        #region Share

        /// <summary>
        /// Callback that results from testing whether or not the community server is active.
        /// </summary>
        /// <param name="resultObj"></param>
        void Callback_Ping(AsyncResult resultObj)
        {
            AsyncResult result = (AsyncResult)resultObj;

            if (result.Success)
            {
                // Read it back from disk and start uploading it to the community.
                BokuShared.Wire.WorldPacket packet = XmlDataHelper.ReadWorldPacketFromDisk(result.Param as string);

                LevelMetadata level = XmlDataHelper.LoadMetadataByGenre(packet.Info.WorldId, (BokuShared.Genres)packet.Info.Genres);

                UploadWorldData(packet, level);

                // TODO (scoy) Show success message?
            }
            else
            {
                // TODO (scoy) Show error message?  This used to fail if not properly signed in.  No reason
                // for it to fail now, but...
            }
        }   // end of Callback_Ping()

        void UploadWorldData(WorldPacket packet, LevelMetadata level)
        {
            DialogCenter.CommunityUploadFailedDialog.ContinueButton.SetOnChange(null);

            if (packet == null)
            {
                // Failed to load level and create packet.
                DialogManagerX.ShowDialog(DialogCenter.CommunityUploadFailedDialog);
            }
            else if (0 == Community.Async_PutWorldData(packet, Callback_PutWorldData, level))
            {
                // Failed to send packet to community.
                // I don't think we need to show the error here since it looks like
                // it gets shown at the end of Callback_PutWorldData().
                //DialogManagerX.ShowDialog(DialogCenter.CommunityUploadFailedDialog);
            }
        }

        void Callback_PutWorldData(AsyncResult result)
        {
            LevelMetadata uploadedLevel = result.Param as LevelMetadata;

            if (result.Success && uploadedLevel != null && uploadedLevel.LinkedToLevel != null)
            {
                LevelMetadata nextLevel = uploadedLevel.NextLink();

                if (nextLevel != null)
                {
                    string folderName = Utils.FolderNameFromFlags(nextLevel.Genres);
                    string fullPath = BokuGame.Settings.MediaPath + folderName + nextLevel.WorldId.ToString() + @".Xml";

                    // Read it back from disk and start uploading it to the community.
                    BokuShared.Wire.WorldPacket packet = XmlDataHelper.ReadWorldPacketFromDisk(fullPath);

                    UploadWorldData(packet, nextLevel);

                    return;
                }
            }

            if (result.Success)
            {
                DialogCenter.CommunityUploadSuccessDialog.ContinueButton.SetOnChange(null);
                DialogManagerX.ShowDialog(DialogCenter.CommunityUploadSuccessDialog);
            }
            else
            {
                DialogCenter.CommunityUploadFailedDialog.ContinueButton.SetOnChange(null);
                DialogManagerX.ShowDialog(DialogCenter.CommunityUploadFailedDialog);
            }

        }   // end of Callback_PutWorldData()

        #endregion Share

        /// <summary>
        /// Launch the TagDialog.
        /// </summary>
        public void FlyoutOnEditTags()
        {
            if (CurWorld != null)
            {
                // TODO (scoy) Need to plumb args all the way through DialogManager.
                // On question is, do we want to re-apply the args if the dialog was
                // suspended and then re-activated?  Should the dialog's Activate()
                // method handle this so it can do the right thing?
                DialogCenter.TagsDialog.Genres = CurWorld.Genres;
                DialogCenter.TagsDialog.OnDeactivate = OnEditTagsDone;
                DialogManagerX.ShowDialog(DialogCenter.TagsDialog, camera);
            }
        }   // end of FlyoutOnEditTags()

        /// <summary>
        /// When TagsDialog is done, copy results back.
        /// </summary>
        /// <param name="d"></param>
        void OnEditTagsDone(BaseDialog d)
        {
            // Write changes out.
            CurWorld.Genres = DialogCenter.TagsDialog.Genres;
            XmlDataHelper.UpdateWorldMetadata(CurWorld);

            // Clear this just in case TagsDialog is launched 
            // from elsewhere without an OnDeactivate callback.
            DialogCenter.TagsDialog.OnDeactivate = null;
        }   // end of OnEditTagsDOne()

        public void FlyoutOnDelete()
        {
            if (isDeleteActive)
            {
                if (CurWorld.PreviousLink() != null || CurWorld.NextLink() != null)
                {
                    // Delete linked level.  Note this only warns the user about breaking
                    // the links, it doesn't delete or fix up the other levels.
                    Button.Callback OnDelete = delegate(BaseWidget w)
                    {
                        DeleteSelectedLevel();
                    };
                    DialogCenter.DeleteConfirmLinkedDialog.DeleteButton.SetOnChange(OnDelete);
                    DialogCenter.DeleteConfirmLinkedDialog.CancelButton.SetOnChange(null);
                    DialogManagerX.ShowDialog(DialogCenter.DeleteConfirmLinkedDialog);
                }
                else
                {
                    // Delete level with no links.
                    Button.Callback OnDelete = delegate(BaseWidget w)
                    {
                        DeleteSelectedLevel();
                    };
                    DialogCenter.DeleteConfirmDialog.DeleteButton.SetOnChange(OnDelete);
                    DialogCenter.DeleteConfirmDialog.CancelButton.SetOnChange(null);
                    DialogManagerX.ShowDialog(DialogCenter.DeleteConfirmDialog);
                }
            }
        }   // end of FlyoutOnDelete()

        #region Delete

        void DeleteSelectedLevel()
        {
            // Issue the delete command.
            if (browserType == LevelBrowserType.Community)
            {
                // Send command to delete level on Community site.
                LevelMetadata info = CurWorld;
                if (info != null)
                {
                    // Double check that user is ok to delete.
                    if (Auth.IsValidCreatorChecksum(info.Checksum, info.LastSaveTime))
                    {
                        // Delete this world.
                        bool deleted = DeleteCurrentWorld();

                    }
                }
            }
            else
            {
                // Delete locally.
                bool deleted = DeleteCurrentWorld();
            }

            // Move the worlds display to fill in the hole.
            //worldsDisplay.ResetTileTransforms(twitch: true);

        }   // end DeleteSelectedLevel()

        /// <summary>
        /// Deletes the current world.  If local then from the local 
        /// machine.  If in the community site then sends a message 
        /// to the server to delete the world.
        /// </summary>
        public bool DeleteCurrentWorld()
        {
            bool result = browser.StartDeletingLevel(
                CurWorld.WorldId,
                CurWorld.Genres & Genres.Virtual,
                DeleteCallback,
                CurWorld.Browser);

            return result;
        }   // end of DeleteCurrentWorld();

        void DeleteCallback(AsyncResult result)
        {
            // TODO (scoy) This feels like overkill.  Need to understand
            // how cursors handle deletions better.  Seems to act different
            // depending on whether it's local or community.

            // Call to Update needed for local.  Without this, the deleted
            // level is still in the list of worlds.  It is no longer
            // displayed but still takes up a space in the display.
            browser.Update();

            // Needed at all?
            StartFetchingThumbnails(cursor);

            worldsDisplay.Reload(cursor);
            worldsDisplay.ResetTileTransforms(twitch: true);
        }   // end of DeleteCallback()

        #endregion Delete

        public void FlyoutOnDownload()
        {
            browser.StartDownloadingWorld(CurWorld, WorldDownloadComplete);
        }   // end of FlyoutOnDownload()

        #region Download

        void WorldDownloadComplete(WorldDataPacket packet, byte[] thumbnailBytes, LevelMetadata level)
        {
            if (!XmlDataHelper.WriteWorldDataPacketToDisk(packet, thumbnailBytes, level.LastWriteTime))
            {
                level.DownloadState = LevelMetadata.DownloadStates.Failed;
            }
            else
            {
                // This will re-load to ensure the linked level ids are loaded - metadata from download doesn't contain them.
                LevelMetadata localLevel = ProcessDownloadedLevel(level.WorldId);

                if (localLevel == null)
                {
                    cursor.SetLevelDownloadState(level.WorldId, LevelMetadata.DownloadStates.Failed);
                }
                else if (localLevel.LinkedFromLevel != null)
                {
                    StartDownloadToFirstLink((Guid)localLevel.LinkedFromLevel);
                }
                else if (localLevel.LinkedToLevel != null)
                {
                    StartDownloadToLastLink((Guid)localLevel.LinkedToLevel);
                }
            }
        }   // end of WorldDownloadComplete()

        /// <summary>
        /// Helper function that will re-load to ensure the linked level ids and set the downloads genre.
        /// pre: level exists and was successfully downloaded
        /// 
        /// </summary>
        /// <param name="worldId"></param>
        /// <returns></returns>
        LevelMetadata ProcessDownloadedLevel(Guid worldId)
        {
            if (!XmlDataHelper.CheckWorldExistsByGenre(worldId, Genres.Downloads))
            {
                return null;
            }

            LevelMetadata localLevel = XmlDataHelper.LoadMetadataByGenre(worldId, Genres.Downloads);

            localLevel.Genres |= Genres.Downloads;
            localLevel.Genres &= ~Genres.Favorite;

            // Make sure we save the added downloads genre flag.
            XmlDataHelper.UpdateWorldMetadata(localLevel);

            localLevel.DownloadState = LevelMetadata.DownloadStates.Complete;
            localLevel.Downloads++;

            return localLevel;
        }   // end of ProcessDownloadedLevel()

        void BackwardLinkDownloadComplete(WorldDataPacket packet, byte[] thumbnailBytes, Guid worldId)
        {
            if (!XmlDataHelper.WriteWorldDataPacketToDisk(packet, thumbnailBytes, DateTime.Now))
            {
                Debug.WriteLine("Failed walking links backward on world id {0}", worldId.ToString());
                cursor.SetLevelDownloadState(worldId, LevelMetadata.DownloadStates.Failed);
            }
            else
            {
                cursor.SetLevelDownloadState(worldId, LevelMetadata.DownloadStates.Complete);

                //this will re-load to ensure the linked level ids are loaded - metadata from download doesn't contain them
                LevelMetadata localLevel = ProcessDownloadedLevel(worldId);

                if (localLevel == null)
                {
                    cursor.SetLevelDownloadState(worldId, LevelMetadata.DownloadStates.Failed);
                }
                else if (localLevel.LinkedFromLevel != null)
                {
                    StartDownloadToFirstLink((Guid)localLevel.LinkedFromLevel);
                }
                else if (localLevel.LinkedToLevel != null)
                {
                    //base case - hit the last link, time to walk forward
                    StartDownloadToLastLink((Guid)localLevel.LinkedToLevel);
                }
            }
        }   // end of BackwardLinkDownloadComplete()

        void ForwardLinkDownloadComplete(WorldDataPacket packet, byte[] thumbnailBytes, Guid worldId)
        {
            if (!XmlDataHelper.WriteWorldDataPacketToDisk(packet, thumbnailBytes, DateTime.Now))
            {
                cursor.SetLevelDownloadState(worldId, LevelMetadata.DownloadStates.Failed);
                Debug.WriteLine("Failed walking links forward on world id {0}", worldId.ToString());
            }
            else
            {
                cursor.SetLevelDownloadState(worldId, LevelMetadata.DownloadStates.Complete);

                //this will re-load to ensure the linked level ids are loaded - metadata from download doesn't contain them
                LevelMetadata localLevel = ProcessDownloadedLevel(worldId);

                if (localLevel == null)
                {
                    cursor.SetLevelDownloadState(worldId, LevelMetadata.DownloadStates.Failed);
                }
                else if (localLevel.LinkedFromLevel != null)
                {
                    StartDownloadToLastLink((Guid)localLevel.LinkedFromLevel);
                }
                else
                {
                    DialogCenter.DownloadLinkedSuccessDialog.ContinueButton.SetOnChange(null);
                    DialogManagerX.ShowDialog(DialogCenter.DownloadLinkedSuccessDialog);
                }
            }
        }   // end of ForwardLinkDownloadComplete()

        /// <summary>
        /// Helper function that walks a level chain backwards to
        /// the first link, downloading links it finds missing.
        /// </summary>
        /// <param name="worldId"></param>
        /// <returns></returns>
        bool StartDownloadToFirstLink(Guid worldId)
        {
            // Early out if the level already exists.
            if (XmlDataHelper.CheckWorldExistsByGenre(worldId, Genres.Downloads))
            {
                LevelMetadata levelData = XmlDataHelper.LoadMetadataByGenre(worldId, Genres.Downloads);

                if (levelData.LinkedFromLevel != null)
                {
                    return StartDownloadToFirstLink((Guid)levelData.LinkedFromLevel);
                }
                else
                {
                    // Base case - no previous links, start walking forward.
                    return StartDownloadToLastLink(worldId);
                }
            }
            return browser.StartDownloadingOffPageWorld(worldId, BackwardLinkDownloadComplete);
        }   // end of StartDownloadToFirstLink()

        /// <summary>
        /// Helper function that walks a level chain forwards to
        /// the last link, downloading links it finds missing.
        /// </summary>
        /// <param name="worldId"></param>
        /// <returns></returns>
        bool StartDownloadToLastLink(Guid worldId)
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
                    DialogCenter.DownloadLinkedSuccessDialog.ContinueButton.SetOnChange(null);
                    DialogManagerX.ShowDialog(DialogCenter.DownloadLinkedSuccessDialog);

                    // base case - we're done! at the last link while walking forward
                    return true;
                }
            }
            return browser.StartDownloadingOffPageWorld(worldId, ForwardLinkDownloadComplete);
        }   // end of StartDownloadToLastLink()

        #endregion Download

        public void FlyoutOnReport()
        {
            if (CurWorld.FlaggedByMe)
            {
                // Already flagged by me.  Just call the handler to unflag.  No need to confirmation.
                ReportAbuseSelectedLevel();
            }
            else
            {
                Button.Callback OnOk = delegate(BaseWidget w)
                {
                    ReportAbuseSelectedLevel();
                };
                DialogCenter.ReportAbuseDialog.OkButton.SetOnChange(OnOk);
                DialogCenter.ReportAbuseDialog.CancelButton.SetOnChange(null);

                DialogManagerX.ShowDialog(DialogCenter.ReportAbuseDialog);

            }
        }   // end of FlyoutOnReport()

        #region ReportAbuse

        void ReportAbuseSelectedLevel()
        {
            if (CurWorld.FlaggedByMe)
            {
                // If already flagged, unflag.
                // Send the report message.
                Community.Async_FlagLevel(CurWorld.WorldId, false, null, null);

                CurWorld.FlaggedByMe = false;
            }
            else
            {
                // Send the report message.
                Community.Async_FlagLevel(CurWorld.WorldId, true, null, null);

                CurWorld.FlaggedByMe = true;
            }
        }   // end of ReportAbuseSelectedLevel()
        
        #endregion ReportAbuse

        public void FlyoutOnLink()
        {
            // At this point we're acting just to select a world to link to.
            // We can assume that there is a currently edited world and we
            // want to link to this world.  So, we need to:

            // Check if the selected world already has a back link.  That would mean 
            // that it is already part of a chain.  Adding it to the current chain 
            // would break the old chain.  Warn user.
            if (CurWorld.LinkedFromLevel.HasValue)
            {
                Button.Callback OnContinueLinking = delegate(BaseWidget w)
                {
                    ApplyLink();
                };
                Button.Callback OnCancel = delegate(BaseWidget w)
                {
                    // Nothing to do here.  This let's control go back
                    // to the LoadLevelScene where the user can choose
                    // a different level to link.
                };

                DialogCenter.TargetAlreadyLinkedDialog.ContinueButton.SetOnChange(OnContinueLinking);
                DialogCenter.TargetAlreadyLinkedDialog.CancelButton.SetOnChange(OnCancel);

                // TODO (scoy) Camera or no camera?  Does this control scaling of dialog?
                // Figure this out, document, and check every other call.
                DialogManagerX.ShowDialog(DialogCenter.TargetAlreadyLinkedDialog);
            }
            else
            {
                // No conflict, just link.
                ApplyLink();
            }

        }   // end of FlyoutOnLink()

        /// <summary>
        /// Helper function of eliminate duplicate code.
        /// Links from the currently loaded world (InGame.XmlWorldData) to the
        /// level in focus in this dialog. (CurWorld)
        /// 
        /// TODO (scoy) Realistically, this could be buggy.  In particular, the
        /// target level is assumed to not be in either Downloads or BuiltInGames.
        /// If it is, it needs to be cloned into MyWorlds.  Best option, however,
        /// is to rethink folders so everything shares the same folder and we 
        /// don't have these arbitrary differences.
        /// </summary>
        void ApplyLink()
        {
            if (InGame.XmlWorldData != null)
            {
                // Do we already have a link from this world to a different world?
                // If so, clear it from this world and back link from other world.
                if (InGame.XmlWorldData.LinkedToLevel.HasValue)
                {
                    string filename = Path.Combine(Storage4.UserLocation, BokuGame.Settings.MediaPath, BokuGame.MyWorldsPath, InGame.XmlWorldData.LinkedToLevel.Value.ToString() + ".Xml");
                    XmlWorldData level = XmlWorldData.Load(filename, XnaStorageHelper.Instance);
                    if (level != null)
                    {
                        level.LinkedFromLevel = null;
                        level.Save(filename, XnaStorageHelper.Instance);
                        InGame.XmlWorldData.LinkedToLevel = null;
                        InGame.IsLevelDirty = true;
                    }
                }

                // Update the selected world's back link.
                CurWorld.LinkedFromLevel = InGame.XmlWorldData.id;
                // Update it's file, too.
                {
                    string filename = Path.Combine(Storage4.UserLocation, BokuGame.Settings.MediaPath, BokuGame.MyWorldsPath, CurWorld.WorldId.ToString() + ".Xml");
                    XmlWorldData level = XmlWorldData.Load(filename, XnaStorageHelper.Instance);
                    if (level != null)
                    {
                        level.LinkedFromLevel = InGame.XmlWorldData.id;
                        level.Save(filename, XnaStorageHelper.Instance);

                        // Update current edit world's forward link.
                        // Note that we're only doing this if target link also worked.
                        InGame.XmlWorldData.LinkedToLevel = CurWorld.WorldId;
                        InGame.IsLevelDirty = true;
                    }
                }

            }
            else
            {
                // Does it make sense to actually get here?
                Debug.Assert(false);
            }

            // And finally, switch back to ProgrammingScene (or WorldSettingsScene) without resetting it.
            backButton.OnButtonSelect();
        }   // end of ApplyLink()


        public override void Update()
        {
            // Keep size fixed (gets scaled by camera zoom to always fit).
            // Adjust horizontal position to stay centered.
            int x = (int)((camera.ScreenSize.X / camera.Zoom - fullSet.Size.X) / 2.0f);
            fullSet.Position = new Vector2(x, fullSet.Position.Y);

            // Keep SortDialog positioned relative to button.
            if (!sortDialog.Inactive)
            {
                // Get non-local position.
                Vector2 pos = sortButton.Position + sortButton.ParentPosition;
                // Offset by 1/2 size of button.
                pos += sortButton.LocalRect.Size / 2.0f;
                // Offset by camera position.
                Vector2 offset = camera.ScreenSize * 0.5f / camera.Zoom;
                pos = pos - offset;
                pos.TruncateToPoint();

                // Set new position.
                RectangleF rect = sortDialog.Rectangle;
                rect.SetPosition(pos);
                sortDialog.Rectangle = rect;
            }

            // Keep scroll arrows at edges of screen.  Also, enable/disable depending on input mode.
            {
                Vector2 size = camera.ScreenSize / camera.Zoom;
                leftArrow.Position = new Vector2(margin, 480);
                rightArrow.Position = new Vector2(size.X - rightArrow.Size.X - margin, 480);

                leftBumper.Position = new Vector2(-128, -20);
                // For the right bumper we want it to sit just to the right
                // of the last button which is always the All button.
                rightBumper.Position = new Vector2(allButton.LocalRect.Right, -20);

                // Don't change button state if modal dialog is active since Activate/Deactivate
                // will make changes to the InputEventHandler lists.
                if (!DialogManagerX.ModalDialogIsActive)
                {
                    if (KoiLibrary.LastTouchedDeviceIsGamepad)
                    {
                        leftArrow.Deactivate();
                        rightArrow.Deactivate();
                        leftArrow.Alpha = 0;
                        rightArrow.Alpha = 0;

                        leftBumper.Activate();
                        rightBumper.Activate();
                        leftBumper.Alpha = 1;
                        rightBumper.Alpha = 1;
                    }
                    else
                    {
                        leftArrow.Activate();
                        rightArrow.Activate();
                        leftArrow.Alpha = 1;
                        rightArrow.Alpha = 1;

                        leftBumper.Deactivate();
                        rightBumper.Deactivate();
                        leftBumper.Alpha = 0;
                        rightBumper.Alpha = 0;
                    }
                }
            }

            if (levelFilter.Dirty || levelSorter.Dirty)
            {
                if (browserType == LevelBrowserType.Community)
                {
                    // The community browser can't pivot on the current selection when the query changes.
                    browser.Reset();
                }
            }

            if (levelFilter.Dirty)
            {
                // Nothing to do here any more since adding ResetTileTransforms()
                // below.  Hope it stays this way...
            }

            if (browser != null)
            {
                browser.Update();
                worldsDisplay.ResetTileTransforms(twitch: true);
            }
            LevelMetadata info = CurWorld;

            // Update current level display.  This is a no-op if it hasn't changed.
            if (info != null)
            {
                titleTextBox.RawText = info.Name;

                DateTime localWriteTime = info.LastWriteTime.ToLocalTime();
                string dateStr = localWriteTime.ToShortDateString() + " " + localWriteTime.ToShortTimeString();
                if (info.Creator != null)
                {
                    dateStr += " " + Strings.Localize("loadLevelMenu.authoredBy") + " " + TextHelper.FilterInvalidCharacters(info.Creator);
                }
                dateCreatorTextBox.RawText = dateStr;
                dateCreatorTextBox.Position = titleTextBox.Position + new Vector2(0, titleTextBox.CalcMinSize().Y + margin);

                descriptionTextBox.BodyText = info.Description;
                descriptionTextBox.Justification = info.DescJustification;

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
                    tagsTextBox.RawText = Strings.Localize("loadLevelMenu.tags") + ": " + tags;
                    tagsTextBox.Position = dateCreatorTextBox.Position + new Vector2(0, dateCreatorTextBox.CalcMinSize().Y + margin);
                }
                else
                {
                    tagsTextBox.RawText = "";
                }

            }
            else
            {
                titleTextBox.RawText = "";
                dateCreatorTextBox.RawText = "";
                tagsTextBox.RawText = "";
                descriptionTextBox.BodyText = "";
            }

            // Is the user currently entering stuff in to the search box?
            if (//searchBox.InFocus && 
                searchBox.SecondsSinceLastKeypress > 1.0 &&
                levelFilter.SearchString != searchBox.CurrentText)
            {
                string text = searchBox.CurrentText;
                Instrumentation.RecordEvent(Instrumentation.EventId.SearchLevels, text);
                levelFilter.SearchString = text;
                browser.Reset();
            }

            // Look at the world that is currently in focus and set 
            // the appropriate shared bools so we know where we are.
            // Start by turning off everything so we've got a known state.
            isDownload = false;
            isMyWorld = false;
            isBuiltInWorld = false;

            // Get user name.
            isUserAdmin = Boku.Web.Community.UserLevel == UserLevel.DomainAdmin ||
                            Boku.Web.Community.UserLevel == UserLevel.GlobalAdmin;

            if (info != null)
            {
                isMyWorld = (info.Genres & Genres.MyWorlds) != 0;
                isBuiltInWorld = (info.Genres & Genres.BuiltInWorlds) != 0;
                isDownload = (info.Genres & Genres.Downloads) != 0;

                // Users may delete their own worlds or worlds they've downloaded.  
                isDeleteActive =
                    //(info.Creator == Auth.CreatorName && isMyWorld) ||
                    isMyWorld ||
                    // Only allow deleting of downloads from LoadLevelMenu, not Community.
                    (isDownload && browserType != LevelBrowserType.Community);

                // If in Community page, users may also delete their own worlds.
                if (browserType == LevelBrowserType.Community)
                {
                    // Matching id?  Guest not allowed.
                    if (info.Creator != Auth.DefaultCreatorName && Auth.IsValidCreatorChecksum(info.Checksum, info.LastSaveTime))
                    {
                        isDeleteActive = true;
                    }
                }
            }

            // Keeps status message up to date.
            if (showPagingMessage)
            {
                statusTextBox.RawText = Strings.Localize("loadLevelMenu.searching");
            }
            else if (worldsDisplay.NoValidLevels)
            {
                // Don't let the message flash on first load.  Delay half a second.
                // This just looks better than having the flash.
                if (Time.WallClockTotalSeconds - SceneManager.SceneStartTime > 0.5)
                {
                    if (levelFilter.FilterGenres == Genres.All)
                    {
                        statusTextBox.RawText = Strings.Localize("loadLevelMenu.noLevels");
                    }
                    else
                    {
                        statusTextBox.RawText = Strings.Localize("loadLevelMenu.noMatch");
                    }
                }
            }
            else
            {
                statusTextBox.RawText = "";
            }

            // We render this after the normal rendering so 
            // that it layers correctly with the flyout dialog.
            worldsDisplay.RenderFocusWorld = !flyoutDialog.Active;

            base.Update();
        }   // end of Update()

        public override void Render(RenderTarget2D rt)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;
            SpriteBatch batch = KoiLibrary.SpriteBatch;

            if (rt != null)
            {
                device.SetRenderTarget(rt);
            }

            RenderBackgroundStretched(bkgTexture);

            // Render world title, creator, date, etc.

            if (showPagingMessage)
            {
                // Do this here???
            }

            if (rt != null)
            {
                device.SetRenderTarget(null);
            }

            base.Render(rt);

        }   // end of Render()

        public override void PostDialogRender(RenderTarget2D rt)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;
            SpriteBatch batch = KoiLibrary.SpriteBatch;

            if (rt != null)
            {
                device.SetRenderTarget(rt);
            }

            // We render this after the normal rendering so 
            // that it layers correctly with the flyout dialog.
            if (!worldsDisplay.RenderFocusWorld)
            {
                worldsDisplay.RenderFocusTile(camera, browserType);
            }

            if (rt != null)
            {
                device.SetRenderTarget(null);
            }

            base.PostDialogRender(rt);
        }   // end of PostDialogRender()

        public override void Activate(params object[] args)
        {
            if (!Active)
            {
                if (browserType == LevelBrowserType.Local)
                {
                    // If we tried to import a level but it's from a newer version
                    // tell the user that a new version is available.
                    if (!LevelPackage.ImportAllLevels(null))
                    {
                        DialogManagerX.ShowDialog(DialogCenter.ImportNeedsNewerVersionDialog);
                    }

                    browser = new LocalLevelBrowser();
                    UiOpenInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.LocalStorageUI);
                }
                else if(browserType == LevelBrowserType.Community)
                {
                    browser = new CommunityLevelBrowser();
                    UiOpenInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.CommunityUI);
                }

                Guid desiredSelection = Guid.Empty;
                
                // Restore previous state if local.
                if (browserType == LevelBrowserType.Local)
                {
                    desiredSelection = previouSelectedLevelId;

                    levelFilter.FilterGenres = previouShowOnly;
                    ApplyCategoryFiltering();

                    levelSorter.SortBy = previouSortBy;
                    levelSorter.SortDirection = previouSortDirection;
                }

                cursor = browser.OpenCursor(desiredSelection,
                                            levelSorter,
                                            levelFilter,
                                            CursorFetchingCallback,
                                            CursorFetchCompleteCallback,
                                            CursorShiftedCallback,
                                            CursorJumpedCallback,
                                            CursorAdditionCallback,
                                            CursorRemovalCallback,
                                            BrowserWorldsDisplay.numTiles);

                base.Activate(args);

                // Set focus to worlds display.
                worldsDisplay.SetFocus();

            }   // end if not already active.

        }   // end of Activate()

        public override void Deactivate()
        {
            // Remember previous state for when we come back.
            previouSelectedLevelId = CurWorld == null ? Guid.Empty : CurWorld.WorldId;
            previouShowOnly = levelFilter.FilterGenres;
            previouSortBy = levelSorter.SortBy;
            previouSortDirection = levelSorter.SortDirection;

            base.Deactivate();
        }

        //
        // Cursor callbacks.
        //

        void CursorAdditionCallback(ILevelSetCursor cursor, int index)
        {
            LevelAdded(cursor, index);
        }

        void CursorRemovalCallback(ILevelSetCursor cursor, int index)
        {
            LevelRemoved(cursor, index);
        }

        void CursorFetchingCallback(ILevelSetQuery query)
        {
            showPagingMessage = true;
        }

        void CursorFetchCompleteCallback(ILevelSetQuery query)
        {
            showPagingMessage = false;
        }

        void CursorShiftedCallback(ILevelSetCursor cursor, int desired, int actual)
        {
            CursorShifted(cursor, desired, actual);
        }

        void CursorJumpedCallback(ILevelSetCursor cursor)
        {
            CursorJumped(cursor);
        }

        public void StartFetchingThumbnails(ILevelSetCursor cursor)
        {
            // Most recent thumbnail requests are serviced first, so walk from left
            // to right from the outer edges toward the selected level, requesting
            // thumbnails for each. Thumbnails should come back in roughly the opposite
            // order they were requested, with some variation due to the asynchronous
            // nature of the transaction.
            int min = -BrowserWorldsDisplay.front;
            int max = BrowserWorldsDisplay.numTiles - BrowserWorldsDisplay.front;

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
        }   // end of StartFetchingThumbnails()

        private void GotThumbnail(LevelMetadata level)
        {
            for (int i = 0; i < BrowserWorldsDisplay.numTiles; ++i)
            {
                BrowserWorldsDisplay.Tile elem = worldsDisplay.Get(i);
                if (elem != null && elem.Level == level)
                {
                    elem.RtDirty = true;
                    break;
                }
            }
        }   // end of GotThumbnail()



        void LevelAdded(ILevelSetCursor cursor, int index)
        {
            StartFetchingThumbnails(cursor);

            // Adjust UI if this is the visible browser.
            if (cursor.Browser == browser)
            {
                worldsDisplay.Reload(cursor);
            }
        }

        void LevelRemoved(ILevelSetCursor cursor, int index)
        {
            // Adjust UI if this is the visible browser.
            if (cursor.Browser == browser)
            {
                if (index == int.MaxValue)
                {
                    // int.MaxValue is a special value indicating the entire query has been cleared.
                    //worldsDisplay.UnloadInstanceContent();
                }
                else
                {
                    worldsDisplay.Remove(index);
                }
                worldsDisplay.Reload(cursor);
            }
        }

        void CursorShifted(ILevelSetCursor cursor, int desired, int actual)
        {
            StartFetchingThumbnails(cursor);

            if (cursor.Browser == browser)
            {
                worldsDisplay.Shift(actual);
                worldsDisplay.Reload(cursor);
            }
        }

        void CursorJumped(ILevelSetCursor cursor)
        {
            StartFetchingThumbnails(cursor);

            if (cursor.Browser == browser)
            {
                worldsDisplay.Reload(cursor);
            }
        }

        public override void RegisterForEvents()
        {
            base.RegisterForEvents();

            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftDown);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseWheel);

            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Tap);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.OnePointDrag);

            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);          // Control keys.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.WinFormsKeyboard);  // Text.

            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.GamePad);

        }   // end of RegisterForEvents()

        #endregion

        #region Internal

        //
        // Keyboard
        //

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);

            switch(input.Key)
            {
                case Keys.Tab:
                    // If currently in the search box, break out.
                    // Else, cycle through categories.
                    if (searchBox.InFocus)
                    {
                        worldsDisplay.SetFocus();
                    }
                    else
                    {
                        if (input.Shift)
                        {
                            PrevCategory();
                        }
                        else
                        {
                            NextCategory();
                        }
                    }
                    return true;

                case Keys.Enter:
                    OnSelect(worldsDisplay.CurFocusTile);
                    return true;

                case Keys.Right:
                    NextWorld();
                    return true;

                case Keys.Left:
                    PrevWorld();
                    return true;

                case Keys.Y:
                    // Not sure if we should do this.  The 'Y' button is a 
                    // holdover from when the gamepad icons stayed on the screen
                    // all the time and the Y button brought up the sort dialog.
                    DialogManagerX.ShowDialog(sortDialog, camera);
                    return true;

                default:
                    break;
            }

            return base.ProcessKeyboardEvent(input);
        }   // end of ProcessKeyboardEvent()

        public override bool ProcessWinFormsKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);

            switch(input.Key)
            {
                default:
                    break;
            }

            return base.ProcessWinFormsKeyboardEvent(input);
        }   // end of ProcessWinFormsKeyboardEvent()

        //
        // Mouse
        //

        public override bool ProcessMouseWheelEvent(MouseInput input)
        {
            Debug.Assert(Active);

            // Don't test for hit.  Note that the TextDialog used for Descriptions
            // needs to be looked at first in order for this to work.  
            //if (KoiLibrary.InputEventManager.MouseHitObject == this)
            {
                if (input.E.Delta > 0)
                {
                    PrevWorld();
                }
                else
                {
                    NextWorld();
                }

                return true;
            }

        }   // end of ProcessMouseWheelEvent()

        public override bool ProcessMouseLeftDownEvent(MouseInput input)
        {
            Debug.Assert(Active);

            return base.ProcessMouseLeftDownEvent(input);
        }   // end of ProcessMouseLeftDownEvent()

        public override bool ProcessMouseMoveEvent(MouseInput input)
        {
            Debug.Assert(Active);

            return base.ProcessMouseMoveEvent(input);
        }   // end of ProcessMouseMoveEvent()

        public override bool ProcessMouseLeftUpEvent(MouseInput input)
        {
            Debug.Assert(Active);

            return base.ProcessMouseLeftUpEvent(input);
        }   // end of ProcessMouseLeftUpEvent()

        //
        // Touch
        //

        public override bool ProcessTouchTapEvent(TapGestureEventArgs gesture)
        {
            Debug.Assert(Active);

            return base.ProcessTouchTapEvent(gesture);
        }   // end of ProcessTouchTapEvent()

        public override bool ProcessTouchOnePointDragEvent(OnePointDragGestureEventArgs gesture)
        {
            Debug.Assert(Active);

            return base.ProcessTouchOnePointDragEvent(gesture);
        }   // end of ProcessTouchOnePointDragEvent()

        //
        // Gamepad
        //

        public override bool ProcessGamePadEvent(GamePadInput pad)
        {
            Debug.Assert(Active);

            // Categories
            if (pad.LeftShoulder.WasPressedOrRepeat)
            {
                PrevCategory();
                return true;
            }
            if (pad.RightShoulder.WasPressedOrRepeat)
            {
                NextCategory();
                return true;
            }

            return base.ProcessGamePadEvent(pad);
        }   // end of ProcessGamePadEvent()


        //
        // Actions 
        //

        /// <summary>
        /// Tap or click on a world.
        /// If focus, then launch fly-out menu.
        /// If not focus, then bring to focus.
        /// </summary>
        void SelectWorld()
        {
        }

        /// <summary>
        /// Scrolls to the next world.
        /// </summary>
        void NextWorld()
        {
            cursor.StartShifting(1);
        }   // end of NextWorld()

        /// <summary>
        /// Scrolls to the previous world.
        /// </summary>
        void PrevWorld()
        {
            cursor.StartShifting(-1);            
        }   // end of PrevWorld()

        void NextCategory()
        {
            int i = CurCategoryIndex();
            i = (i + 1) % categoryButtonsSet.Widgets.Count;
            (categoryButtonsSet.Widgets[i] as Button).OnButtonSelect();
        }   // end of NextCategory

        void PrevCategory()
        {
            int i = CurCategoryIndex();
            i = (i + categoryButtonsSet.Widgets.Count - 1) % categoryButtonsSet.Widgets.Count;
            (categoryButtonsSet.Widgets[i] as Button).OnButtonSelect();
        }   // end of PrevCategory()

        /// <summary>
        /// Find the index of the currently selected category button.
        /// </summary>
        /// <returns></returns>
        int CurCategoryIndex()
        {
            int result = 0;
            for (int i = 0; i < categoryButtonsSet.Widgets.Count; i++)
            {
                Button b = categoryButtonsSet.Widgets[i] as Button;
                if (b.Selected)
                {
                    result = i;
                    break;
                }
            }

            return result;
        }   // end of CurCategoryIndex()

        /// <summary>
        /// Changes the level filtering depending on the currently selected category.
        /// It seems like you should be able to just set the catergory on the filter
        /// but the bit-level interpretation is more complication than that.
        /// </summary>
        public void ApplyCategoryFiltering()
        {
            Genres cur = levelFilter.FilterGenres;

            // Remove any previous filtering.
            cur = (Genres)((int)levelFilter.FilterGenres
                      & ~(int)Genres.MyWorlds
                      & ~(int)Genres.Buckets
                      );

            if (allButton.Selected)
            {
                cur |= Genres.Buckets | Genres.MyWorlds;

                // Only apply All if nothing else is applied.
                if (cur == Genres.None)
                {
                    cur = (Genres)((int)cur | (int)Genres.All);
                }
            }
            if (myWorldsButton.Selected)
            {
                cur = (Genres)((int)cur | (int)Genres.MyWorlds);
            }
            if (downloadsButton != null && downloadsButton.Selected)
            {
                cur = (Genres)((int)cur | (int)Genres.Downloads);
            }
            if (lessonsButton.Selected)
            {
                cur = (Genres)((int)cur | (int)Genres.Lessons);
            }
            if (samplesButton.Selected)
            {
                cur = (Genres)((int)cur | (int)Genres.SampleWorlds);
            }

            levelFilter.FilterGenres = cur;

        }   // end of ApplyCategoryFiltering()


        //
        // Internal functions which react to user input.
        //

        /// <summary>
        /// The user has selected a level.
        /// Does this level have broken links, if so, let the user know.
        /// Is this level part of a chain, if so, let use run from first if they want.
        /// else
        /// Just run the level.
        /// </summary>
        /// <param name="playMode">Go into play mode (as opposed to edit mode)</param>
        void CheckPlaySelectedLevel(bool playMode)
        {
            LevelMetadata level = CurWorld;

            // For downloads, make sure we're reading the full xml.
            // Linked level info won't be in metadata from the community server.
            if (level != null && level.Genres == Genres.Downloads)
            {
                level = XmlDataHelper.LoadMetadataByGenre(level.WorldId, Genres.Downloads);
            }

            if (level != null)
            {
                LevelMetadata brokenLevel = null;
                bool forwardLinkBroken = false;

                // Check if the level has a broken link.
                if (level.FindBrokenLink(ref brokenLevel, ref forwardLinkBroken))
                {
                    Button.Callback OnLoadAnyway = delegate(BaseWidget w)
                    {
                        FindFirstLevel();
                        PlayLevel(CurWorld, playMode);
                    };
                    DialogCenter.BrokenLinkDialog.ContinueButton.SetOnChange(OnLoadAnyway);
                    DialogCenter.BrokenLinkDialog.CancelButton.SetOnChange(null);
                    DialogManagerX.ShowDialog(DialogCenter.BrokenLinkDialog);
                }
                // Check if this isn't the first level in a list (edit mode always loads the level selected)
                else if (level.LinkedFromLevel != null && playMode)
                {
                    Button.Callback OnLoadFirstLevel = delegate(BaseWidget w)
                    {
                        FindFirstLevel();
                        PlayLevel(CurWorld, playMode: playMode);
                    };
                    Button.Callback OnLoadThisLevel = delegate(BaseWidget w) 
                    { 
                        PlayLevel(CurWorld, playMode: playMode);
                    };
                    DialogCenter.BrokenLinkDialog.YesButton.SetOnChange(OnLoadFirstLevel);
                    DialogCenter.BrokenLinkDialog.NoButton.SetOnChange(OnLoadThisLevel);
                    DialogManagerX.ShowDialog(DialogCenter.BrokenLinkDialog);
                }
                else
                {
                    PlayLevel(CurWorld, playMode: playMode);
                }
            }
        }   // end of CheckPlaySelectedLevel()

        /// <summary>
        /// Starts with the current level and walks up the chain of links 
        /// to find the first level in the chain.  
        /// Then sets curso to that level which updates CurWorld.
        /// </summary>
        void FindFirstLevel()
        {
            LevelMetadata level = CurWorld;

            // If it's a community level, try to reload it to make sure we have the metadata for links.
            if (level.Browser is CommunityLevelBrowser &&
                XmlDataHelper.CheckWorldExistsByGenre(level.WorldId, BokuShared.Genres.Downloads))
            {
                level = XmlDataHelper.LoadMetadataByGenre(level.WorldId, BokuShared.Genres.Downloads);
            }
            // Use the first level in the chain.
            level = level.FindFirstLink();

            // Update the cursor to point at the level we actually loaded.
            cursor.SetDesiredLevel(level.WorldId);
            worldsDisplay.Reload(cursor);

        }   // end of FindFirstLevel()

        /// <summary>
        /// Loads a level for either edit or play.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="playMode"></param>
        void PlayLevel(LevelMetadata level, bool playMode)
        {
            if (level != null)
            {
                // Put up "loading" message.
                DialogManagerX.ShowDialog(DialogCenter.LoadingLevelWaitDialog, camera);
                DialogCenter.LoadingLevelWaitDialog.Center = new Vector2(0, -280);

                // Save this off for use until we've rendered a frame
                InGame.inGame.ThumbNail = worldsDisplay.FocusWorldThumbnail;

                string folderName = Utils.FolderNameFromFlags(level.Genres);
                string fullPath = BokuGame.Settings.MediaPath + folderName + level.WorldId.ToString() + @".Xml";

                // Queue the play operation to happen in the next frame so that we can render the "please wait" message first.
                FrameDelayedOperation op = null;

                bool? play = playMode;
                op = new FrameDelayedOperation(Callback_PlayEditSelectedLevel, param0: fullPath, param1: play);

                AsyncOps.Enqueue(op);
            }
        }   // end of PlayLevel()

        public void Callback_PlayEditSelectedLevel(AsyncOperation op)
        {
            DialogManagerX.KillDialog(DialogCenter.LoadingLevelWaitDialog);

            string fullPath = op.Param0 as string;
            bool? playMode = op.Param1 as bool?;

            if (BokuGame.bokuGame.inGame.LoadLevelAndRun(fullPath, keepPersistentScores: false, newWorld: false, andRun: playMode.Value))
            {
                // Now that we've successfully loaded the selected level, run or edit it as needed.
                if (playMode.Value)
                {
                    SceneManager.SwitchToScene("RunSimScene");
                }
                else
                {
                    SceneManager.SwitchToScene("EditWorldScene");
                }
            }
            else
            {
                // Load didn't work, just stay here.
                // TODO (scoy) Should we have some kinbd of error here?  What could
                // fail to get us here?
                Debug.Assert(false);
            }

            Time.Paused = false;
        }   // end of Callback_EditSelectedLevel()

        public override void LoadContent()
        {
            if (bkgTexture == null)
            {
                bkgTexture = KoiLibrary.LoadTexture2D(bkgTextureName);
            }

            base.LoadContent();
        }

        public override void UnloadContent()
        {
            DeviceResetX.Release(ref bkgTexture);

            base.UnloadContent();
        }

        #endregion

    }   // end of class LoadLevelLocalScene

}   // end of namespace KoiX.Scenes
