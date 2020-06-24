
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

namespace KoiX.Scenes
{
    /// <summary>
    /// Page of options.  Formerly DNA menu.
    /// </summary>
    public class OptionsPage1Scene : BasePageScene
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
        CheckBoxLabelHelp showToolTips;
        CheckBoxLabelHelp showHints;
        ButtonLabelHelp restoreDisabledHints;
        CheckBoxLabelHelp showFramerate;
        CheckBoxLabelHelp modalToolMenu;
        
        /*
        CheckBoxLabelHelp invertYAxis;
        CheckBoxLabelHelp invertXAxis;
        CheckBoxLabelHelp invertCamY;
        CheckBoxLabelHelp invertCamX;
        */

        CheckBoxLabelHelp checkForUpdates;
        CheckBoxLabelHelp sendInstrumentation;
        CheckBoxLabelHelp showIntroVideo;
        CheckBoxLabelHelp showTutorialDebug;
        
        SliderLabelHelp terrainSpeed;
        SliderLabelHelp uiVolume;
        SliderLabelHelp foleyVolume;
        SliderLabelHelp musicVolume;
        
        ButtonLabelHelp showCodeOfConduct;
        ButtonLabelHelp showPrivacyStatement;
        ButtonLabelHelp showEULA;


        #endregion

        #region Accessors
        #endregion

        #region Public

        public OptionsPage1Scene(string nextLabelId = null, string nextLabelText = null, string prevLabelId = null, string prevLabelText = null)
            : base("OptionsPage1Scene", nextLabelId, nextLabelText, prevLabelId, prevLabelText)
        {
            ThemeSet theme = Theme.CurrentThemeSet;

            titleFont = new SystemFont(theme.TextFontFamily, theme.TextTitleFontSize * 2.0f, System.Drawing.FontStyle.Bold);
            font = new SystemFont(theme.TextFontFamily, theme.TextBaseFontSize * 1.2f, System.Drawing.FontStyle.Regular);

            FontWrapper wrapper = new FontWrapper(null, font);
            GetFont Font = delegate() { return wrapper; };

            title = new Label(fullScreenContentDialog, titleFont, theme.LightTextColor, outlineColor: theme.DarkTextColor, outlineWidth: 2.5f, labelId: "optionsParams.options");
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
                // Show Tool Tips
                BaseWidget.Callback onChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null) { XmlOptionsData.ShowToolTips = cb.Checked; } };
                showToolTips = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "optionsParams.showToolTips", "ShowToolTips", column1.Size.X, onChange, theme);
                showToolTips.Checked = XmlOptionsData.ShowToolTips;
                column1.AddWidget(showToolTips);
            }
            {
                // Show Hints
                BaseWidget.Callback onChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null) { XmlOptionsData.ShowHints = cb.Checked; } };
                showHints = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "optionsParams.showHints", "ShowHints", column1.Size.X, onChange, theme);
                showHints.Checked = XmlOptionsData.ShowHints;
                column1.AddWidget(showHints);
            }
            {
                // Restore Disabled Hints
                BaseWidget.Callback onChange = delegate(BaseWidget w) { Button b = w as Button; if (b != null) { XmlOptionsData.RestoreDisabledHints(); } };
                restoreDisabledHints = new ButtonLabelHelp(fullScreenContentDialog, Font, "optionsParams.restoreDisabledHints", "RestoreDisabledHints", column1.Size.X, onChange);
                column1.AddWidget(restoreDisabledHints);
            }
            {
                // Show Framerate
                BaseWidget.Callback onChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null) { XmlOptionsData.ShowFramerate = cb.Checked; } };
                showFramerate = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "optionsParams.showFramerate", "ShowFramerate", column1.Size.X, onChange, theme);
                showFramerate.Checked = XmlOptionsData.ShowFramerate;
                column1.AddWidget(showFramerate);
            }
            {
                // Modal Tool Menu
                BaseWidget.Callback onChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null) { XmlOptionsData.ModalToolMenu = cb.Checked; } };
                modalToolMenu = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "optionsParams.ModalToolMenu", "ModalToolMenu", column1.Size.X, onChange, theme);
                modalToolMenu.Checked = XmlOptionsData.ModalToolMenu;
                column1.AddWidget(modalToolMenu);
            }

            /*
            {
                // Invert Y Axis
                BaseWidget.Callback onChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null) { PlayerIndex lastTouched = GamePadInput.RealToLogical(GamePadInput.LastTouched); GamePadInput.SetInvertYAxis(lastTouched, cb.Checked); } };
                invertYAxis = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "optionsParams.invertYAxis", "InvertYAxis", column1.Size.X, onChange, theme);
                {
                    PlayerIndex lastTouched = GamePadInput.RealToLogical(GamePadInput.LastTouched);
                    invertYAxis.Checked = GamePadInput.InvertYAxis(lastTouched);
                }
                column1.AddWidget(invertYAxis);
            }
            {
                // Invert X Axis
                BaseWidget.Callback onChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null) { PlayerIndex lastTouched = GamePadInput.RealToLogical(GamePadInput.LastTouched); GamePadInput.SetInvertXAxis(lastTouched, cb.Checked); } };
                invertXAxis = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "optionsParams.invertXAxis", "InvertXAxis", column1.Size.X, onChange, theme);
                {
                    PlayerIndex lastTouched = GamePadInput.RealToLogical(GamePadInput.LastTouched);
                    invertXAxis.Checked = GamePadInput.InvertXAxis(lastTouched);
                }
                column1.AddWidget(invertXAxis);
            }
            {
                // Invert Camera Y
                BaseWidget.Callback onChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null) { PlayerIndex lastTouched = GamePadInput.RealToLogical(GamePadInput.LastTouched); GamePadInput.SetInvertCamY(lastTouched, cb.Checked); } };
                invertCamY = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "optionsParams.invertCamY", "InvertCamY", column1.Size.X, onChange, theme);
                invertCamY.Checked = GamePadInput.InvertCamY();
                column1.AddWidget(invertCamY);
            }
            {
                // Invert Camera X
                BaseWidget.Callback onChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null) { PlayerIndex lastTouched = GamePadInput.RealToLogical(GamePadInput.LastTouched); GamePadInput.SetInvertCamX(lastTouched, cb.Checked); } };
                invertCamX = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "optionsParams.invertCamX", "InvertCamX", column1.Size.X, onChange, theme);
                invertCamX.Checked = GamePadInput.InvertCamX();
                column1.AddWidget(invertCamX);
            }
            */

            {
                // Check For Updates
                BaseWidget.Callback onChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null) { XmlOptionsData.CheckForUpdates = cb.Checked; } };
                checkForUpdates = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "optionsParams.checkForUpdates", "CheckForUpdates", column2.Size.X, onChange, theme);
                checkForUpdates.Checked = XmlOptionsData.CheckForUpdates;
                column1.AddWidget(checkForUpdates);
            }
            {
                // Send Instrumentation.
                BaseWidget.Callback onChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null) { XmlOptionsData.CheckForUpdates = cb.Checked; } };
                sendInstrumentation = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "optionsParams.sendInstrumentation", "SendInstrumentation", column2.Size.X, onChange, theme);
                sendInstrumentation.Checked = XmlOptionsData.SendInstrumentation;
                column1.AddWidget(sendInstrumentation);
            }
            {
                // Show Intro Video
                BaseWidget.Callback onChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null) { XmlOptionsData.ShowIntroVideo = cb.Checked; } };
                showIntroVideo = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "optionsParams.showIntroVideo", "ShowIntroVideo", column2.Size.X, onChange, theme);
                showIntroVideo.Checked = XmlOptionsData.ShowIntroVideo;
                column1.AddWidget(showIntroVideo);
            }
            {
                // Show Tutorial Debug
                BaseWidget.Callback onChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null) { XmlOptionsData.ShowTutorialDebug = cb.Checked; } };
                showTutorialDebug = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "optionsParams.showTutorialDebug", "ShowTutorialDebug", column2.Size.X, onChange, theme);
                showTutorialDebug.Checked = XmlOptionsData.ShowTutorialDebug;
                column1.AddWidget(showTutorialDebug);
            }
            
            {
                // Terrain Speed
                BaseWidget.Callback onChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null) { XmlOptionsData.TerrainSpeed = s.CurValue; } };
                terrainSpeed = new SliderLabelHelp(fullScreenContentDialog, Font, "optionsParams.terrainSpeed", "TerrainSpeed", column2.Size.X,
                                                    minValue: 0.25f, maxValue: 4.0f, increment: 0.25f, numDecimals: 2, curValue: XmlOptionsData.TerrainSpeed, 
                                                    OnChange: onChange, theme: theme);
                column2.AddWidget(terrainSpeed);
            }

            {
                // UI Volume
                BaseWidget.Callback onChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null) { XmlOptionsData.UIVolume = s.CurValue / 100.0f; } };
                uiVolume = new SliderLabelHelp(fullScreenContentDialog, Font, "optionsParams.uiVolume", "UIVolume", column2.Size.X,
                                                minValue: 0.0f, maxValue: 100.0f, increment: 5.00f, numDecimals: 0, curValue: XmlOptionsData.UIVolume * 100.0f,
                                                OnChange: onChange, theme: theme);
                column2.AddWidget(uiVolume);
            }
            {
                // Foley Volume
                BaseWidget.Callback onChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null) { XmlOptionsData.FoleyVolume = s.CurValue / 100.0f; } };
                foleyVolume = new SliderLabelHelp(fullScreenContentDialog, Font, "optionsParams.foleyVolume", "EffectsVolume", column2.Size.X,
                                                    minValue: 0.0f, maxValue: 100.0f, increment: 5.00f, numDecimals: 0, curValue: XmlOptionsData.FoleyVolume * 100.0f,
                                                    OnChange: onChange, theme: theme);
                column2.AddWidget(foleyVolume);
            }
            {
                // Music Volume
                BaseWidget.Callback onChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null) { XmlOptionsData.MusicVolume = s.CurValue / 100.0f; } };
                musicVolume = new SliderLabelHelp(fullScreenContentDialog, Font, "optionsParams.musicVolume", "MusicVolume", column2.Size.X,
                                                    minValue: 0.0f, maxValue: 100.0f, increment: 5.00f, numDecimals: 0, curValue: XmlOptionsData.MusicVolume * 100.0f,
                                                    OnChange: onChange, theme: theme);
                column2.AddWidget(musicVolume);
            }

            {
                // Show Code of Conduct
                BaseWidget.Callback onChange = delegate(BaseWidget w) 
                {
                    Stream stream = Storage4.OpenRead(BokuGame.Settings.MediaPath + @"Text\Kodu_Game_Lab_Code_of_Conduct.txt", StorageSource.TitleSpace);
                    StreamReader reader = new StreamReader(stream);
                    string content = reader.ReadToEnd();
                    reader.Close();

                    TextDialog textDialog = SharedX.TextDialog;

                    Debug.Assert(textDialog.Active == false);

                    textDialog.TitleId = "optionsParams.viewCodeOfConduct";
                    textDialog.BodyText = content;
                    DialogManagerX.ShowDialog(textDialog);
                };
                showCodeOfConduct = new ButtonLabelHelp(fullScreenContentDialog, Font, "optionsParams.viewCodeOfConduct", null, column1.Size.X, onChange);

                // Put some space between this element and the sliders above it.
                Padding margin = showCodeOfConduct.Margin;
                margin.Top += 16;
                showCodeOfConduct.Margin = margin;  

                column2.AddWidget(showCodeOfConduct);
            }
            {
                // Show Privacy Statement
                BaseWidget.Callback onChange = delegate(BaseWidget w)
                {
                    Process.Start(Program2.SiteOptions.KGLUrl + @"/Link/PrivacyStatement");
                };
                showPrivacyStatement = new ButtonLabelHelp(fullScreenContentDialog, Font, "optionsParams.viewPrivacyStatement", null, column1.Size.X, onChange);
                column2.AddWidget(showPrivacyStatement);
            }
            {
                // Show EULA
                BaseWidget.Callback onChange = delegate(BaseWidget w)
                {
                    Stream stream = Storage4.OpenRead(BokuGame.Settings.MediaPath + @"Text\Kodu_Game_Lab_EULA.txt", StorageSource.TitleSpace);
                    StreamReader reader = new StreamReader(stream);
                    string content = reader.ReadToEnd();
                    reader.Close();

                    TextDialog textDialog = SharedX.TextDialog;

                    Debug.Assert(textDialog.Active == false);

                    textDialog.TitleId = "optionsParams.viewEULA";
                    textDialog.BodyText = content;
                    DialogManagerX.ShowDialog(textDialog);
                };
                showEULA = new ButtonLabelHelp(fullScreenContentDialog, Font, "optionsParams.viewEULA", null, column1.Size.X, onChange);
                column2.AddWidget(showEULA);
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

    }   // end of class OptionsPage1Scene
}   // end of namespace KoiX.Scenes
