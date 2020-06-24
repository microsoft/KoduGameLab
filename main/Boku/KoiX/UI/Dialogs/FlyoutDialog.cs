
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
using KoiX.Managers;
using KoiX.Scenes;
using KoiX.Text;
using KoiX.UI;

using Boku;
using Boku.Common;

using BokuShared;

namespace KoiX.UI.Dialogs
{
    using Keys = Microsoft.Xna.Framework.Input.Keys;

    /// <summary>
    /// Flyout menu for use in LoadLevelMenu.
    /// </summary>
    public class FlyoutDialog : BaseDialog
    {
        #region Members

        LoadLevelScene loadLevelScene;  // Parent scene we are a part of.

        WidgetSet buttonSet;

        FlyoutButton playButton;
        FlyoutButton editButton;
        FlyoutButton exportButton;
        FlyoutButton shareButton;
        FlyoutButton editTagsButton;
        FlyoutButton deleteButton;
        FlyoutButton downloadButton;
        FlyoutButton reportButton;
        FlyoutButton linkButton;

        /// <summary>
        /// TODO (****) These should actually be passed as args via Activate() but
        /// the plumbing to pass the args through the DialogManager isn't in place
        /// yet, or even really thought out.
        /// </summary>
        bool localBrowser = true;
        bool isLinking = false;   // Linking levels?
        LevelMetadata currentWorld = null;

        int buttonWidth = 300;
        int buttonHeight = 44;

        #endregion

        #region Accessors

        public LevelMetadata CurrentWorld
        {
            get { return currentWorld; }
            set { currentWorld = value; }
        }

        public bool LocalBrowser
        {
            get { return localBrowser; }
            set { localBrowser = value; }
        }

        #endregion

        #region Public

        public FlyoutDialog(LoadLevelScene loadLevelScene, bool isLinking)
        {
#if DEBUG
            _name = "FlyOutDialog";
#endif

            this.loadLevelScene = loadLevelScene;
            this.isLinking = isLinking;

            // Don't want backdrop for this menu.
            BackdropColor = Color.Transparent;

            //
            // Clone the current theme and modify for these buttons.
            //
            {
                theme = Theme.CurrentThemeSet.Clone() as ThemeSet;

                // Base changes.
                theme.DialogBodyTileFocused.CornerRadius = 12.0f;
                theme.DialogBodyTileFocused.BevelStyle = Geometry.BevelStyle.None;
                theme.DialogBodyTileFocused.OutlineWidth = 0;
                theme.DialogBodyTileFocused.Padding = new Padding(0);   // No padding on dialog, put padding into full set.
                theme.DialogBodyTileFocused.ShadowStyle = Geometry.ShadowStyle.None;
                theme.DialogBodyTileFocused.TileColor = theme.BaseColorPlus10;

                // Change so that focus has dark text and bright green body.
                theme.ButtonNormal.CornerRadius = 0;
                theme.ButtonNormalFocused.CornerRadius = 0;
                theme.ButtonNormalFocusedHover.CornerRadius = 0;
                theme.ButtonSelectedFocused.CornerRadius = 0;
                theme.ButtonSelectedFocusedHover.CornerRadius = 0;

                theme.ButtonNormal.OutlineWidth = 0;
                theme.ButtonNormalFocused.OutlineWidth = 0;
                theme.ButtonNormalFocusedHover.OutlineWidth = 0;
                theme.ButtonSelectedFocused.OutlineWidth = 0;
                theme.ButtonSelectedFocusedHover.OutlineWidth = 0;

                theme.ButtonNormalFocused.BodyColor = ThemeSet.FocusColor;
                theme.ButtonNormalFocusedHover.BodyColor = ThemeSet.FocusColor;
                theme.ButtonSelectedFocused.BodyColor = ThemeSet.FocusColor;
                theme.ButtonSelectedFocusedHover.BodyColor = ThemeSet.FocusColor;

                theme.ButtonNormalFocused.TextColor = ThemeSet.DarkTextColor;
                theme.ButtonNormalFocusedHover.TextColor = ThemeSet.DarkTextColor;
                theme.ButtonSelectedFocused.TextColor = ThemeSet.DarkTextColor;
                theme.ButtonSelectedFocusedHover.TextColor = ThemeSet.DarkTextColor;

                theme.ButtonNormalFocused.TextOutlineColor = ThemeSet.LightTextColor;
                theme.ButtonNormalFocusedHover.TextOutlineColor = ThemeSet.LightTextColor;
                theme.ButtonSelectedFocused.TextOutlineColor = ThemeSet.LightTextColor;
                theme.ButtonSelectedFocusedHover.TextOutlineColor = ThemeSet.LightTextColor;
            }

            buttonSet = new WidgetSet(this, new RectangleF(), Orientation.Vertical, verticalJustification: Justification.Top);
            buttonSet.Padding = new Padding(0, 12, 0, 12);
            AddWidget(buttonSet);

            playButton = new FlyoutButton(this, new RectangleF(0, 0, buttonWidth, buttonHeight), labelId: "loadLevelMenu.playLevel", onSelect: OnPlay, theme: theme);

            editButton = new FlyoutButton(this, new RectangleF(0, 0, buttonWidth, buttonHeight), labelId: "loadLevelMenu.editLevel", onSelect: OnEdit, theme: theme);

            exportButton = new FlyoutButton(this, new RectangleF(0, 0, buttonWidth, buttonHeight), labelId: "loadLevelMenu.export", onSelect: OnExport, theme: theme);

            shareButton = new FlyoutButton(this, new RectangleF(0, 0, buttonWidth, buttonHeight), labelId: "loadLevelMenu.share", onSelect: OnShare, theme: theme);

            editTagsButton = new FlyoutButton(this, new RectangleF(0, 0, buttonWidth, buttonHeight), labelId: "loadLevelMenu.editTags", onSelect: OnEditTags, theme: theme);

            deleteButton = new FlyoutButton(this, new RectangleF(0, 0, buttonWidth, buttonHeight), labelId: "loadLevelMenu.delete", onSelect: OnDelete, theme: theme);
            
            downloadButton = new FlyoutButton(this, new RectangleF(0, 0, buttonWidth, buttonHeight), labelId: "loadLevelMenu.download", onSelect: OnDownload, theme: theme);

            reportButton = new FlyoutButton(this, new RectangleF(0, 0, buttonWidth, buttonHeight), labelId: "loadLevelMenu.reportAbuse", onSelect: OnReport, theme: theme);

            linkButton = new FlyoutButton(this, new RectangleF(0, 0, buttonWidth, buttonHeight), labelId: "loadLevelMenu.link", onSelect: OnLink, theme: theme);

            //Setup();

        }   // end of c'tor

        void OnPlay(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);
            loadLevelScene.FlyoutOnPlay();
        }

        void OnEdit(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);
            loadLevelScene.FlyoutOnEdit();
        }

        void OnExport(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);
            loadLevelScene.FlyoutOnExport();
        }

        void OnShare(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);
            loadLevelScene.FlyoutOnShare();
        }

        void OnEditTags(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);
            loadLevelScene.FlyoutOnEditTags();
        }

        void OnDelete(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);
            loadLevelScene.FlyoutOnDelete();
        }

        void OnDownload(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);
            loadLevelScene.FlyoutOnDownload();
        }

        void OnReport(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);
            loadLevelScene.FlyoutOnReport();
        }

        void OnLink(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);
            loadLevelScene.FlyoutOnLink();
        }

        /// <summary>
        /// Sets up the menu based on the current browser.
        /// </summary>
        void Setup()
        {
            // Start with a clean set.
            // TODO (****) Should this be a method so Widgets is not exposed?
            buttonSet.Widgets.Clear();
            buttonSet.FitToParentDialog = true;

            // Based on current state, add needed buttons.
            if (LocalBrowser)
            {
                if (isLinking)
                {
                    buttonSet.AddWidget(linkButton);
                }
                else
                {
                    buttonSet.AddWidget(playButton);
                    buttonSet.AddWidget(editButton);
                    buttonSet.AddWidget(exportButton);
                    // Can only share your own worlds.
                    if (CurrentWorld != null && CurrentWorld.Creator == Auth.CreatorName)
                    {
                        buttonSet.AddWidget(shareButton);
                    }
                    // Don't allow changing tags or deleting of built-in worlds.
                    if (CurrentWorld != null && (CurrentWorld.Genres & BokuShared.Genres.BuiltInWorlds) == 0)
                    {
                        buttonSet.AddWidget(editTagsButton);
                        buttonSet.AddWidget(deleteButton);
                    }
                }
            }
            else
            {
                // Community.
                if (CurrentWorld != null && CurrentWorld.DownloadState == LevelMetadata.DownloadStates.Complete)
                {
                    buttonSet.AddWidget(playButton);
                    buttonSet.AddWidget(editButton);
                }
                else
                {
                    buttonSet.AddWidget(downloadButton);
                }

                // Can only delete your own worlds.  Guest is not applicable.  Note this is for
                // deleting on server side.  User can delete the local version from local menu.
                if (CurrentWorld.Creator != Auth.DefaultCreatorName
                    && Auth.IsValidCreatorChecksum(CurrentWorld.Checksum, CurrentWorld.LastSaveTime))
                {
                    buttonSet.AddWidget(deleteButton);
                }

                buttonSet.AddWidget(reportButton);
            }

            // Center flyout vertically relative to focus tile.
            int height = buttonSet.Widgets.Count * buttonHeight + 24;
            rect = new RectangleF(rect.X, rect.Y - height/2.0f, 300, height);

            // Call Recalc for force all the button positions and sizes to be calculated.
            // We need this in order to properly calc the links.
            Dirty = true;
            Recalc();

            // Connect navigation links.
            CreateTabList();

            // Dpad nav includes all widgets so webSiteButton will be there.
            CreateDPadLinks();
        }   // end of Setup()

        public override void Update(SpriteCamera camera)
        {
            base.Update(camera);
        }   // end of Update()

        public override void Activate(params object[] args)
        {
            Debug.Assert(CurrentWorld != null, "This shouldn't be launched if null.");

            // Decide which menu options to show based on current world and browser.
            Setup();

            // Prevent buttons from grabbing hover focus while flying out.
            foreach (BaseWidget w in buttonSet.Widgets)
            {
                w.Hoverable = false;
            }

            // Trigger fly-out.
            {
                Vector2 targetPosition = rect.Position;
                rect.Position = rect.Position + new Vector2(-200, 0);
                TwitchManager.Set<Vector2> set = delegate(Vector2 val, Object param) { rect.Position = val; };
                TwitchManager.CreateTwitch<Vector2>(rect.Position, targetPosition, set, 0.2f, TwitchCurve.Shape.OvershootOut, onComplete: FlyOutDone);
            }

            base.Activate(args);
        }   // end of Activate()

        public override void RegisterForInputEvents()
        {
            // Call base register first.  By putting the child widgets on the input stacks
            // first, we can then put ourselves on and have priority.
            base.RegisterForInputEvents();
            
            // Allow dialog to be cancelled with taps or clicks outside of its rect.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftDown);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.GamePad);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Tap);
        }

        public override bool ProcessMouseLeftDownEvent(MouseInput input)
        {
            Debug.Assert(Active);

            // If user clicks outside of flyout, treat that as a cancel.
            if (!Rectangle.Contains(input.Position))
            {
                ShutDown();
                return true;
            }

            return base.ProcessMouseLeftDownEvent(input);
        }

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);

            if (input.Key == Keys.Escape)
            {
                ShutDown();
                return true;
            }

            return base.ProcessKeyboardEvent(input);
        }   // end of ProcessKeyboardEvent()

        public override bool ProcessGamePadEvent(GamePadInput pad)
        {
            Debug.Assert(Active);

            if (pad.Back.WasPressed || pad.ButtonB.WasPressed)
            {
                ShutDown();
                return true;
            }

            return base.ProcessGamePadEvent(pad);
        }   // end of ProcessGamePadEvent()

        public override bool ProcessTouchTapEvent(TapGestureEventArgs gesture)
        {
            Debug.Assert(Active);

            // If user taps outside of flyout, treat that as a cancel.
            if (!Rectangle.Contains(gesture.Position))
            {
                ShutDown();
                return true;
            }

            return base.ProcessTouchTapEvent(gesture);
        }

        #endregion

        #region Internal

        void ShutDown()
        {
            // Trigger fly-in.
            {
                Vector2 targetPosition = rect.Position + new Vector2(-200, 0);
                TwitchManager.Set<Vector2> set = delegate(Vector2 val, Object param) { rect.Position = val; };
                TwitchManager.CreateTwitch<Vector2>(rect.Position, targetPosition, set, 0.1f, TwitchCurve.Shape.OvershootIn, onComplete: FlyInDone);
            }
        }   // end of ShutDown()

        /// <summary>
        /// Callback that occurs after fly-in animation is processed.
        /// </summary>
        /// <param name="param"></param>
        void FlyInDone(Object param)
        {
            DialogManagerX.KillDialog(this);
        }

        /// <summary>
        /// Callback that occurs after fly-out animation is processed.
        /// </summary>
        /// <param name="param"></param>
        void FlyOutDone(Object param)
        {
            foreach (BaseWidget w in buttonSet.Widgets)
            {
                w.Hoverable = true;
            }
        }   

        #endregion

    }   // end of class FlyOutDialog

}   // end of namespace KoiX.UI.Dialogs
