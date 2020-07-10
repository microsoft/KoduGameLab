// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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

namespace KoiX.UI.Dialogs
{
    public class GameOverDialog : BaseDialog
    {
        #region Members

        WidgetSet fullSet;      // Covers full dialog.
        WidgetSet leftSet;      // Holds buttons in left side of dialog.
        WidgetSet rightSet;     // Holds buttons in right side of dialog.

        MainMenuButton homeButton;
        MainMenuButton editButton;
        MainMenuButton restartButton;

        TextBlob blob;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public GameOverDialog(ThemeSet theme = null)
            : base(theme: theme)
        {
#if DEBUG
            _name = "GameOverDialog";
#endif
            // Change ref to what base class has set.
            // Allows us to use theme rather than this.Theme.
            theme = this.ThemeSet;

            Rectangle = new RectangleF(-320, 100, 640, 220);

            // Don't show a backdrop, we want the other victory overlay stuff to show through.
            BackdropColor = Color.Transparent;

            // Create sets.
            fullSet = new WidgetSet(this, rect, Orientation.Horizontal, horizontalJustification: Justification.Full, verticalJustification: Justification.Center);
            fullSet.FitToParentDialog = true;

            leftSet = new WidgetSet(this, new RectangleF(0, 0, 176, 220), Orientation.Vertical, horizontalJustification: Justification.Center, verticalJustification: Justification.Center);
            rightSet = new WidgetSet(this, new RectangleF(135, 0, 464, 220), Orientation.Vertical, horizontalJustification: Justification.Left, verticalJustification: Justification.Center);

            AddWidget(fullSet);
            fullSet.AddWidget(leftSet);
            fullSet.AddWidget(rightSet);

            //
            // Clone the current theme and modify for these buttons.
            //
            {
                theme = Theme.CurrentThemeSet.Clone() as ThemeSet;

                // Change so that focus has dark text and bright green body.
                theme.ButtonNormalFocused.BodyColor = ThemeSet.FocusColor;
                theme.ButtonNormalFocusedHover.BodyColor = ThemeSet.FocusColor;
                theme.ButtonSelectedFocused.BodyColor = ThemeSet.FocusColor;
                theme.ButtonSelectedFocusedHover.BodyColor = ThemeSet.FocusColor;

                theme.ButtonNormal.OutlineWidth = 0;
                theme.ButtonNormalFocused.OutlineWidth = 0;
                theme.ButtonNormalFocusedHover.OutlineWidth = 0;
                theme.ButtonSelectedFocused.OutlineWidth = 0;
                theme.ButtonSelectedFocusedHover.OutlineWidth = 0;

                // Keep the focus color white.  Otherwise the text turns green on focus.
                theme.ButtonNormalFocused.TextColor = ThemeSet.LightTextColor;
                theme.ButtonNormalFocusedHover.TextColor = ThemeSet.LightTextColor;
                theme.ButtonSelectedFocused.TextColor = ThemeSet.LightTextColor;
                theme.ButtonSelectedFocusedHover.TextColor = ThemeSet.LightTextColor;

                // Used oversized font.  This works since we're setting these buttons up
                // to not have any outlines.
                theme.ButtonNormal.FontSize *= 1.5f;
            }

            blob = new TextBlob(SharedX.GetGameFont30Bold, "[home]\n[esc]\n[enter]", 180);
            blob.Justification = TextHelper.Justification.Center;
            blob.LineSpacingAdjustment = 8; // Add a bit of a gap between the buttons.

            float width = 460 - 32;

            homeButton = new MainMenuButton(this, new RectangleF(0, 0, width, 60), labelId: "gameOver.browse", onSelect: OnHomeMenu, theme: theme);
            homeButton.Label.HorizontalJustification = Justification.Center;
            homeButton.Margin = new Padding(16, 0, 0, 0);
            rightSet.AddWidget(homeButton);

            editButton = new MainMenuButton(this, new RectangleF(0, 0, width, 60), labelId: "gameOver.edit", onSelect: OnEditWorld, theme: theme);
            editButton.Label.HorizontalJustification = Justification.Center;
            editButton.Margin = new Padding(16, 0, 0, 0);
            rightSet.AddWidget(editButton);

            restartButton = new MainMenuButton(this, new RectangleF(0, 0, width, 60), labelId: "gameOver.restart", onSelect: OnRestartWorld, theme: theme);
            restartButton.Label.HorizontalJustification = Justification.Center;
            restartButton.Margin = new Padding(16, 0, 0, 0);
            rightSet.AddWidget(restartButton);

            // Call Recalc for force all the button positions and sizes to be calculated.
            // We need this in order to properly calc the links.
            Dirty = true;
            Recalc();

            // Connect navigation links.
            CreateTabList();

            // Dpad nav includes all widgets so webSiteButton will be there.
            CreateDPadLinks();

        }   // end of c'tor

        void OnHomeMenu(BaseWidget b)
        {
            VictoryOverlay.Reset();
            DialogManagerX.KillDialog(this);
            SceneManager.SwitchToScene("HomeMenuScene", frameDelay: 1);
            // Refresh the thumbnail during our 1 frame delay.
            InGame.RefreshThumbnail = true;
        }   // end of OnHomeMenu()

        void OnEditWorld(BaseWidget b)
        {
            VictoryOverlay.Reset();
            DialogManagerX.KillDialog(this);
            SceneManager.SwitchToScene("EditWorldScene");
        }   // end of OnEditWorld()

        void OnRestartWorld(BaseWidget b)
        {
            VictoryOverlay.Reset();
            DialogManagerX.KillDialog(this);
            InGame.inGame.ResetSim(preserveScores: false, removeCreatablesFromScene: true, keepPersistentScores: false);
        }   // end of OnRestartWorld()

        public override void Update(SpriteCamera camera)
        {
            if (KoiLibrary.LastTouchedDeviceIsGamepad)
            {
                blob.RawText = "<start>\n<back>\n<abutton>";
            }
            else
            {
                blob.RawText = "[home]\n[esc]\n[enter]";
            }

            base.Update(camera);
        }   // end of Update()

        public override void Render(SpriteCamera camera)
        {
            if (state != State.Inactive)
            {
                // Cull if possible.
                if (camera.CullTest(Rectangle) == Boku.Common.Frustum.CullResult.TotallyOutside)
                {
                    return;
                }

                SpriteBatch batch = KoiLibrary.SpriteBatch;

                RenderBackdrop();

                if (RenderBaseTile)
                {
                    RoundedRect.Render(camera, Rectangle, cornerRadius.Value, theme.AccentColor,
                                        outlineColor: outlineColor.Value, outlineWidth: outlineWidth.Value,
                                        twoToneSecondColor: bodyColor.Value, twoToneSplitPosition: leftSet.LocalRect.Right, twoToneHorizontalSplit: false,
                                        bevelStyle: bevelStyle, bevelWidth: bevelWidth.Value,
                                        shadowStyle: shadowStyle, shadowOffset: shadowOffset.Value, shadowSize: shadowSize.Value, shadowAttenuation: 0.85f);
                }

                // Render the keycaps over the top.  Offset centers it vertically on the tile.
                blob.RenderText(camera, Rectangle.Position + new Vector2(0, 28), Color.White);

                RenderWidgets(camera);
            }
        }   // end of Render()

        #endregion

        #region Internal
        #endregion

    }   // end of class GameOverDialog

}   // end of namespace KoiX.UI.Dialogs
