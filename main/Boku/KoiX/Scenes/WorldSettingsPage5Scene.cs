
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
using Boku.Common;
using Boku.Common.Xml;
using Boku.SimWorld.Terra;

namespace KoiX.Scenes
{
    /// <summary>
    /// Page of WorldSettings.  Formerly DNA menu.
    /// </summary>
    public class WorldSettingsPage5Scene : BasePageScene
    {
        #region Members

        Texture2D bkgTexture;

        SystemFont titleFont;
        SystemFont font;

        Label title;

        WidgetSet column1;
        WidgetSet column2;
        Vector2 columnSize = new Vector2(600, 850);
        float columnGutter = 100.0f;    // Gap between columns.

        // We really don't need these refs.  Once we add them to the column widgetsets
        // that's the only ref we need.  Leaving these here just to make debugging easier.
        CheckBoxLabelHelp debugPath;
        CheckBoxLabelHelp debugCollision;
        CheckBoxLabelHelp debugSightLines;
        CheckBoxLabelHelp debugCurrentPage;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public WorldSettingsPage5Scene(string nextLabelId = null, string nextLabelText = null, string prevLabelId = null, string prevLabelText = null)
            : base("WorldSettingsPage5Scene", nextLabelId, nextLabelText, prevLabelId, prevLabelText)
        {
            ThemeSet theme = Theme.CurrentThemeSet;

            titleFont = new SystemFont(theme.TextFontFamily, theme.TextTitleFontSize * 2.0f, System.Drawing.FontStyle.Bold);
            font = new SystemFont(theme.TextFontFamily, theme.TextBaseFontSize * 1.2f, System.Drawing.FontStyle.Regular);

            FontWrapper wrapper = new FontWrapper(null, font);
            GetFont Font = delegate() { return wrapper; };

            title = new Label(fullScreenContentDialog, titleFont, theme.LightTextColor, outlineColor: theme.DarkTextColor, outlineWidth: 2.5f, labelId: "worldSettings.debug");
            title.Position = new Vector2(150, 5);
            title.Size = title.CalcMinSize();
            fullScreenContentDialog.AddWidget(title);

            // Version string.  Add to the ContentDialog so that it scales in size with the window.
            {
                // Create a widgetset to hold and align the label.
                WidgetSet set = new WidgetSet(fullScreenContentDialog, RectangleF.EmptyRect, Orientation.Horizontal, horizontalJustification: Justification.Center, verticalJustification: Justification.Bottom);
                set.FitToParentDialog = true;
                fullScreenContentDialog.AddWidget(set);

                string version = Strings.Localize("shareHub.appName") + " " + Program2.ThisVersion.ToString();
                Vector2 size = Font().MeasureString(version);
                Vector2 pos = new Vector2(camera.ScreenSize.X - size.X - 96, camera.ScreenSize.Y - size.Y);
                Label versionLabel = new Label(fullScreenContentDialog, font, theme.LightTextColor, outlineColor: theme.DarkTextColor * 0.5f, outlineWidth: 1.1f, labelText: version);
                versionLabel.Position = pos;
                versionLabel.Size = size;
                versionLabel.Margin = new Padding(8);   // Keep it off the bottom edge a bit.

                set.AddWidget(versionLabel);
            }

            column1 = new WidgetSet(fullScreenContentDialog, new RectangleF(new Vector2(150, 100), columnSize), Orientation.Vertical, horizontalJustification: Justification.Left, verticalJustification: Justification.Top);
            column2 = new WidgetSet(fullScreenContentDialog, new RectangleF(new Vector2(850, 100), columnSize), Orientation.Vertical, horizontalJustification: Justification.Left, verticalJustification: Justification.Top);
            fullScreenContentDialog.AddWidget(column1);
            fullScreenContentDialog.AddWidget(column2);

            {
                // Debug path follow.
                BaseWidget.Callback onChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null) { InGame.DebugPathFollow = cb.Checked; } };
                debugPath = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "editWorldParams.debugPathFollow", "Debug:PathFollowing", column1.Size.X, onChange, theme);
                column1.AddWidget(debugPath);
            }
            {
                // Debug collisions.
                BaseWidget.Callback onChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null) { InGame.DebugDisplayCollisions = cb.Checked; } };
                debugCollision = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "editWorldParams.debugDisplayCollisions", "Debug:DisplayCollisions", column1.Size.X, onChange, theme);
                column1.AddWidget(debugCollision);
            }
            {
                // Debug sight lines.
                BaseWidget.Callback onChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null) { InGame.DebugDisplayLinesOfPerception = cb.Checked; } };
                debugSightLines = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "editWorldParams.debugDisplayLinesOfPerception", "Debug:ShowLinesOfPerception", column1.Size.X, onChange, theme);
                column1.AddWidget(debugSightLines);
            }
            {
                // Debug current programming page.
                BaseWidget.Callback onChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null) { InGame.DebugDisplayCurrentPage = cb.Checked; } };
                debugCurrentPage = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "editWorldParams.debugDisplayCurrentPage", "Debug:ShowCurrentPage", column1.Size.X, onChange, theme);
                column1.AddWidget(debugCurrentPage);
            }


            // Connect navigation links.
            fullScreenContentDialog.CreateTabList();

            // Dpad nav includes all widgets.
            fullScreenContentDialog.CreateDPadLinks();


            // Debug only!!!
            /*
            column1.RenderDebugOutline = true;
            column2.RenderDebugOutline = true;
            column1.SetMarginDebugOnChildren(true);
            */

        }   // end of c'tor

        public override void Update()
        {
            Vector2 screenSize = BokuGame.ScreenSize / camera.Zoom;
            column1.Position = new Vector2(screenSize.X / 2.0f - columnSize.X - columnGutter / 2.0f, 100);
            column2.Position = new Vector2(screenSize.X / 2.0f + columnGutter / 2.0f, 100);

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

            if (rt != null)
            {
                device.SetRenderTarget(null);
            }

            base.Render(rt);
        }   // end of Render()

        public override void Activate(params object[] args)
        {
            // Set all the initial values.
            debugPath.Checked = InGame.DebugPathFollow;
            debugCollision.Checked = InGame.DebugDisplayCollisions;
            debugSightLines.Checked = InGame.DebugDisplayLinesOfPerception;
            debugCurrentPage.Checked = InGame.DebugDisplayCurrentPage;

            base.Activate(args);
        }   // end of Activate()

        #endregion

        #region Internal

        public override void LoadContent()
        {
            if (bkgTexture == null)
            {
                bkgTexture = KoiLibrary.LoadTexture2D(@"Textures\LoadLevel\CommunityBackground");
            }

            base.LoadContent();
        }

        public override void UnloadContent()
        {
            DeviceResetX.Release(ref bkgTexture);

            base.UnloadContent();
        }

        #endregion

    }   // end of class WorldSettingsPage5Scene
}   // end of namespace KoiX.Scenes
