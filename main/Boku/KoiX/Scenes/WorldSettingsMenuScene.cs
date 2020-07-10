// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI;

using Boku;

namespace KoiX.Scenes
{
    /// <summary>
    /// This is the scene activated when the user chooses WorldSettings edit tool.
    /// Right now we just kind of skip forward to the first page of the WorldSettings.
    /// Eventually, if we have alot more WorldSettings pages then this page will be a
    /// menu allowing you to skip to the section you care about.
    /// </summary>
    public class WorldSettingsMenuScene : BasePageScene
    {
        #region Members

        Texture2D bkgTexture;

        WorldSettingsPage1Scene page1;
        WorldSettingsPage2Scene page2;
        WorldSettingsPage3Scene page3;
        WorldSettingsPage4Scene page4;
        WorldSettingsPage5Scene page5;

        WidgetSet column1;
        Vector2 columnSize = new Vector2(1600, 800);

        Button page1Button;
        Button page2Button;
        Button page3Button;
        Button page4Button;
        Button page5Button;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public WorldSettingsMenuScene()
            : base("WorldSettingsMenuScene", nextLabelId: "worldSettings.settings")
        {
            // Point all the individual pages back to this menu page.
            string backTarget = "WorldSettingsMenuScene";

            backButton.TargetScene = "EditWorldScene";
            NextTargetScene = "WorldSettingsPage1Scene";

            page1 = new WorldSettingsPage1Scene(nextLabelId: "worldSettings.environment", prevLabelId: "worldSettings.menu");
            page1.BackTargetScene = backTarget;
            page1.NextTargetScene = "WorldSettingsPage2Scene";
            page1.PrevTargetScene = "WorldSettingsMenuScene";

            page2 = new WorldSettingsPage2Scene(nextLabelId: "worldSettings.scores", prevLabelId: "worldSettings.settings");
            page2.BackTargetScene = backTarget;
            page2.NextTargetScene = "WorldSettingsPage3Scene";
            page2.PrevTargetScene = "WorldSettingsPage1Scene";

            page3 = new WorldSettingsPage3Scene(nextLabelId: "worldSettings.buttons", prevLabelId: "worldSettings.environment");
            page3.BackTargetScene = backTarget;
            page3.NextTargetScene = "WorldSettingsPage4Scene";
            page3.PrevTargetScene = "WorldSettingsPage2Scene";

            page4 = new WorldSettingsPage4Scene(nextLabelId: "worldSettings.debug", prevLabelId: "worldSettings.scores");
            page4.BackTargetScene = backTarget;
            page4.NextTargetScene = "WorldSettingsPage5Scene";
            page4.PrevTargetScene = "WorldSettingsPage3Scene";

            page5 = new WorldSettingsPage5Scene(nextLabelId: null, prevLabelId: "worldSettings.buttons");
            page5.BackTargetScene = backTarget;
            page5.NextTargetScene = "";
            page5.PrevTargetScene = "WorldSettingsPage4Scene";

            //
            // Clone the current theme and modify for these buttons.
            //
            ThemeSet theme;
            {
                theme = Theme.CurrentThemeSet.Clone() as ThemeSet;

                // Change so that focus has dark text and bright green body.
                theme.ButtonNormalFocused.BodyColor = theme.FocusColor;
                theme.ButtonNormalFocusedHover.BodyColor = theme.FocusColor;
                theme.ButtonSelectedFocused.BodyColor = theme.FocusColor;
                theme.ButtonSelectedFocusedHover.BodyColor = theme.FocusColor;

                theme.ButtonNormalFocused.TextColor = theme.DarkTextColor;
                theme.ButtonNormalFocusedHover.TextColor = theme.DarkTextColor;
                theme.ButtonSelectedFocused.TextColor = theme.DarkTextColor;
                theme.ButtonSelectedFocusedHover.TextColor = theme.DarkTextColor;

                theme.ButtonNormalFocused.TextOutlineColor = theme.LightTextColor;
                theme.ButtonNormalFocusedHover.TextOutlineColor = theme.LightTextColor;
                theme.ButtonSelectedFocused.TextOutlineColor = theme.LightTextColor;
                theme.ButtonSelectedFocusedHover.TextOutlineColor = theme.LightTextColor;
            }

            column1 = new WidgetSet(fullScreenContentDialog, new RectangleF(new Vector2(150, 100), columnSize), Orientation.Vertical, horizontalJustification: Justification.Center, verticalJustification: Justification.Center);
            fullScreenContentDialog.AddWidget(column1);

            Padding margin = new Padding(16);
            RectangleF buttonRect = new RectangleF(0, 0, 400, 64);
            {
                BaseWidget.Callback onChange = delegate(BaseWidget w)
                {
                    SceneManager.SwitchToScene(page1);
                };
                page1Button = new Button(fullScreenContentDialog, buttonRect, labelId: "worldSettings.settings", OnChange: onChange, theme: theme);
                page1Button.Margin = margin;
                column1.AddWidget(page1Button);
            }
            {
                BaseWidget.Callback onChange = delegate(BaseWidget w)
                {
                    SceneManager.SwitchToScene(page2);
                };
                page2Button = new Button(fullScreenContentDialog, buttonRect, labelId: "worldSettings.environment", OnChange: onChange, theme: theme);
                page2Button.Margin = margin;
                column1.AddWidget(page2Button);
            }
            {
                BaseWidget.Callback onChange = delegate(BaseWidget w)
                {
                    SceneManager.SwitchToScene(page3);
                };
                page3Button = new Button(fullScreenContentDialog, buttonRect, labelId: "worldSettings.scores", OnChange: onChange, theme: theme);
                page3Button.Margin = margin;
                column1.AddWidget(page3Button);
            }
            {
                BaseWidget.Callback onChange = delegate(BaseWidget w)
                {
                    SceneManager.SwitchToScene(page4);
                };
                page4Button = new Button(fullScreenContentDialog, buttonRect, labelId: "worldSettings.buttons", OnChange: onChange, theme: theme);
                page4Button.Margin = margin;
                column1.AddWidget(page4Button);
            }
            {
                BaseWidget.Callback onChange = delegate(BaseWidget w)
                {
                    SceneManager.SwitchToScene(page5);
                };
                page5Button = new Button(fullScreenContentDialog, buttonRect, labelId: "worldSettings.debug", OnChange: onChange, theme: theme);
                page5Button.Margin = margin;
                column1.AddWidget(page5Button);
            }


            // Connect navigation links.
            fullScreenContentDialog.CreateTabList();

            // Dpad nav includes all widgets.
            fullScreenContentDialog.CreateDPadLinks();

        }   // end of c'tor

        public override void Update()
        {
            if (Active)
            {
                Vector2 screenSize = BokuGame.ScreenSize / camera.Zoom;
                column1.Position = new Vector2(screenSize.X / 2.0f - 1600 / 2.0f, 100);
            }

            // Keep the nav buttons happy.
            base.Update();

        }   // end of Update()

        public override void Render(RenderTarget2D rt)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            if (rt != null)
            {
                device.SetRenderTarget(rt);
            }

            RenderBackgroundStretched(bkgTexture);

            if (rt != null)
            {
                device.SetRenderTarget(null);
            }
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

    }   // endof class WorldSettingsMenuScene
}   // end of namespace KoiX.Scenes
