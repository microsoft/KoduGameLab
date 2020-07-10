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
using Boku.Common.Xml;

namespace KoiX.Scenes
{
    /// <summary>
    /// Page of WorldSettings.  Formerly DNA menu.
    /// </summary>
    public class WorldSettingsPage3Scene : BasePageScene
    {
        #region Members

        Texture2D bkgTexture;

        SystemFont titleFont;
        SystemFont font;

        Label title;

        WidgetSet column1;
        Vector2 columnSize = new Vector2(1600, 850);
        WidgetSet row1;
        WidgetSet row2;
        WidgetSet row3;
        WidgetSet row4;

        // We really don't need these refs.  Once we add them to the column widgetsets
        // that's the only ref we need.  Leaving these here just to make debugging easier.
        List<ScoreSettings> scoreSettings = new List<ScoreSettings>();

        #endregion

        #region Accessors
        #endregion

        #region Public

        public WorldSettingsPage3Scene(string nextLabelId = null, string nextLabelText = null, string prevLabelId = null, string prevLabelText = null)
            : base("WorldSettingsPage3Scene", nextLabelId, nextLabelText, prevLabelId, prevLabelText)
        {
            ThemeSet theme = Theme.CurrentThemeSet;

            titleFont = new SystemFont(theme.TextFontFamily, theme.TextTitleFontSize * 2.0f, System.Drawing.FontStyle.Bold);
            font = new SystemFont(theme.TextFontFamily, theme.TextBaseFontSize * 1.2f, System.Drawing.FontStyle.Regular);

            FontWrapper wrapper = new FontWrapper(null, font);
            GetFont Font = delegate() { return wrapper; };

            title = new Label(fullScreenContentDialog, titleFont, theme.LightTextColor, outlineColor: theme.DarkTextColor, outlineWidth: 2.5f, labelId: "worldSettings.scores");
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

            column1 = new WidgetSet(fullScreenContentDialog, new RectangleF(new Vector2(00, 100), columnSize), Orientation.Vertical, horizontalJustification: Justification.Center, verticalJustification: Justification.Top);
            fullScreenContentDialog.AddWidget(column1);

            {
                int width = 500;
                for (int i = 0; i < (int)Classification.ColorInfo.Count; i++)
                {
                    ScoreSettings ss = new ScoreSettings(fullScreenContentDialog, Font, width, (Classification.Colors)((int)Classification.ColorInfo.First + i), OnChange: null, theme: theme);
                    scoreSettings.Add(ss);
                    ss.Margin = new Padding(64, 64, 64, 64);
                }

            }

            row1 = new WidgetSet(fullScreenContentDialog, new RectangleF(0, 0, 1600, 160), Orientation.Horizontal, horizontalJustification: Justification.Center);
            column1.AddWidget(row1);
            row2 = new WidgetSet(fullScreenContentDialog, new RectangleF(0, 0, 1600, 160), Orientation.Horizontal, horizontalJustification: Justification.Center);
            column1.AddWidget(row2);
            row3 = new WidgetSet(fullScreenContentDialog, new RectangleF(0, 0, 1600, 160), Orientation.Horizontal, horizontalJustification: Justification.Center);
            column1.AddWidget(row3);
            row4 = new WidgetSet(fullScreenContentDialog, new RectangleF(0, 0, 1600, 160), Orientation.Horizontal, horizontalJustification: Justification.Center);
            column1.AddWidget(row4);

            // Add scoreSettings to proper rows.
            row1.AddWidget(scoreSettings[0]);
            row1.AddWidget(scoreSettings[1]);
            row1.AddWidget(scoreSettings[2]);
            row2.AddWidget(scoreSettings[3]);
            row2.AddWidget(scoreSettings[4]);
            row2.AddWidget(scoreSettings[5]);
            row3.AddWidget(scoreSettings[6]);
            row3.AddWidget(scoreSettings[7]);
            row3.AddWidget(scoreSettings[8]);
            row4.AddWidget(scoreSettings[9]);
            row4.AddWidget(scoreSettings[10]);

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
            column1.Position = new Vector2(screenSize.X / 2.0f - 1600 / 2.0f, 100);

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

    }   // end of class WorldSettingsPage3Scene
}   // end of namespace KoiX.Scenes
