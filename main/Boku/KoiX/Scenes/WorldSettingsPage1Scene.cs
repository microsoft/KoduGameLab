
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
    public class WorldSettingsPage1Scene : BasePageScene
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
        CheckBoxLabelHelp glassWalls;
        CheckBoxLabelHelp showCompass;
        CheckBoxLabelHelp showResourceMeter;
        CheckBoxLabelHelp enableResourceLimiting;
        CheckBoxLabelHelp showVirtualController;

        SliderLabelHelp waveHeight;
        SliderLabelHelp waterStrength;
        SliderLabelHelp minBreeze;
        SliderLabelHelp maxBreeze;
        SliderLabelHelp effectsVolume;
        SliderLabelHelp musicVolume;

        RadioButtonSetLabelHelp startGameWith;
        RadioButtonLabelHelp startWithNothing;
        RadioButtonLabelHelp startWithTitle;
        RadioButtonLabelHelp startWithDesc;
        RadioButtonLabelHelp startWithCountdown;
        RadioButtonLabelHelp startWithDescCountdown;

        GraphicRadioButtonSetLabelHelp cameraMode;
        GraphicRadioButton cameraFree;
        GraphicRadioButton cameraFixedPosition;
        GraphicRadioButton cameraFixedOffset;
        ButtonLabelHelp setCamera;

        CheckBoxLabelHelp startingCamera;
        ButtonLabelHelp setStartingCamera;
        SliderLabelHelp cameraSpring;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public WorldSettingsPage1Scene(string nextLabelId = null, string nextLabelText = null, string prevLabelId = null, string prevLabelText = null)
            : base("WorldSettingsPage1Scene", nextLabelId, nextLabelText, prevLabelId, prevLabelText)
        {
            ThemeSet theme = Theme.CurrentThemeSet;

            titleFont = new SystemFont(theme.TextFontFamily, theme.TextTitleFontSize * 2.0f, System.Drawing.FontStyle.Bold);
            font = new SystemFont(theme.TextFontFamily, theme.TextBaseFontSize * 1.2f, System.Drawing.FontStyle.Regular);

            FontWrapper wrapper = new FontWrapper(null, font);
            GetFont Font = delegate() { return wrapper; };

            title = new Label(fullScreenContentDialog, titleFont, theme.LightTextColor, outlineColor: theme.DarkTextColor, outlineWidth: 2.5f, labelId: "worldSettings.settings");
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
                // Glass walls.
                BaseWidget.Callback onChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null) { Terrain.Current.GlassWalls = cb.Checked; } };
                glassWalls = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "editWorldParams.glassWallsCheckbox", "GlassWalls", column1.Size.X, onChange, theme);
                column1.AddWidget(glassWalls);
            }
            {
                // Show compass.
                BaseWidget.Callback onChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null) { Terrain.Current.ShowCompass = cb.Checked; } };
                showCompass = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "editWorldParams.showCompassCheckbox", "ShowCompass", column1.Size.X, onChange, theme);
                column1.AddWidget(showCompass);
            }
            {
                // Show resource meter.
                BaseWidget.Callback onChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null) { Terrain.Current.ShowResourceMeter = cb.Checked; } };
                showResourceMeter = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "editWorldParams.showResourceMeterCheckbox", "ShowResourceMeter", column1.Size.X, onChange, theme);
                column1.AddWidget(showResourceMeter);
            }
            {
                // Enable resource limiting.
                BaseWidget.Callback onChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null) { Terrain.Current.EnableResourceLimiting = cb.Checked; } };
                enableResourceLimiting = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "editWorldParams.enableResourceLimitingCheckbox", "EnableResourceLimiting", column1.Size.X, onChange, theme);
                column1.AddWidget(enableResourceLimiting);
            }
            {
                // Show virtual controller.
                BaseWidget.Callback onChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null) { InGame.ShowVirtualController = cb.Checked; } };
                showVirtualController = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "editWorldParams.showVirtualController", "ShowVirtualController", column1.Size.X, onChange, theme);
                column1.AddWidget(showVirtualController);
            }

            {
                // Wave height.
                BaseWidget.Callback onChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null) { Terrain.WaveHeight = s.CurValue / 100.0f; } };
                waveHeight = new SliderLabelHelp(fullScreenContentDialog, Font, "editWorldParams.waveHeight", "WaveHeight", column1.Size.X,
                                                minValue: 0.0f, maxValue: 100.0f, increment: 5.00f, numDecimals: 0, curValue: Terrain.WaveHeight * 100.0f,
                                                OnChange: onChange, theme: theme);
                column1.AddWidget(waveHeight);
            }
            {
                // Water strength
                BaseWidget.Callback onChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null) { Terrain.WaterStrength = s.CurValue / 100.0f; } };
                waterStrength = new SliderLabelHelp(fullScreenContentDialog, Font, "editWorldParams.waterStrength", "WaterStrength", column1.Size.X,
                                                minValue: 0.0f, maxValue: 100.0f, increment: 5.00f, numDecimals: 0, curValue: Terrain.WaterStrength * 100.0f,
                                                OnChange: onChange, theme: theme);
                column1.AddWidget(waterStrength);
            }
            {
                // Min breeze.
                minBreeze = new SliderLabelHelp(fullScreenContentDialog, Font, "editWorldParams.windMin", "MinBreeze", column1.Size.X,
                                                minValue: 0.0f, maxValue: 100.0f, increment: 5.00f, numDecimals: 0, curValue: 0,
                                                theme: theme);
                column1.AddWidget(minBreeze);
            }
            {
                // Max breeze.
                BaseWidget.Callback onChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null) { InGame.WindMin = MathHelper.Clamp(s.CurValue, 0, 100) / 100.0f; if (s.TargetValue < minBreeze.TargetValue) minBreeze.TargetValue = s.TargetValue; } };
                maxBreeze = new SliderLabelHelp(fullScreenContentDialog, Font, "editWorldParams.windMax", "MaxBreeze", column1.Size.X,
                                                minValue: 0.0f, maxValue: 100.0f, increment: 5.00f, numDecimals: 0, curValue: 50,
                                                OnChange: onChange, theme: theme);
                column1.AddWidget(maxBreeze);

                // Fix up minBreeze callback.  We couldn't do this above since 
                // the callback references maxBreeze which didn't exist then.
                onChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null) { InGame.WindMin = MathHelper.Clamp(s.CurValue, 0, 100) / 100.0f; if (s.TargetValue > maxBreeze.TargetValue) maxBreeze.TargetValue = s.TargetValue; } };
                minBreeze.SetOnChange(onChange);
            }
            {
                // Effects volume.
                BaseWidget.Callback onChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null) { InGame.LevelFoleyVolume = s.CurValue / 100.0f; } };
                effectsVolume = new SliderLabelHelp(fullScreenContentDialog, Font, "editWorldParams.foleyVolume", "EffectsVolume", column1.Size.X,
                                                minValue: 0.0f, maxValue: 100.0f, increment: 5.00f, numDecimals: 0, curValue: InGame.LevelFoleyVolume * 100.0f,
                                                OnChange: onChange, theme: theme);
                column1.AddWidget(effectsVolume);
            }
            {
                // Music volume.
                BaseWidget.Callback onChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null) { InGame.LevelMusicVolume = s.CurValue / 100.0f; } };
                musicVolume = new SliderLabelHelp(fullScreenContentDialog, Font, "editWorldParams.musicVolume", "MusicVolume", column1.Size.X,
                                                minValue: 0.0f, maxValue: 100.0f, increment: 5.00f, numDecimals: 0, curValue: InGame.LevelMusicVolume * 100.0f,
                                                OnChange: onChange, theme: theme);
                column1.AddWidget(musicVolume);
            }


            {
                // Start game with... (PreGame)
                List<RadioButtonLabelHelp> buttons = new List<RadioButtonLabelHelp>();
                List<RadioButton> siblings = new List<RadioButton>();

                BaseWidget.Callback onChange;

                onChange = delegate(BaseWidget w) { InGame.XmlWorldData.preGame = ""; InGame.IsLevelDirty = true; };
                startWithNothing = new RadioButtonLabelHelp(fullScreenContentDialog, Font, null, column2.Size.X, siblings, "editWorldParams.nothing", OnChange: onChange, theme: theme);
                buttons.Add(startWithNothing);

                onChange = delegate(BaseWidget w) { InGame.XmlWorldData.preGame = "World Title"; InGame.IsLevelDirty = true; };
                startWithTitle = new RadioButtonLabelHelp(fullScreenContentDialog, Font, null, column2.Size.X, siblings, "editWorldParams.levelTitle", OnChange: onChange, theme: theme);
                buttons.Add(startWithTitle);

                onChange = delegate(BaseWidget w) { InGame.XmlWorldData.preGame = "World Description"; InGame.IsLevelDirty = true; };
                startWithDesc = new RadioButtonLabelHelp(fullScreenContentDialog, Font, null, column2.Size.X, siblings, "editWorldParams.levelDesc", OnChange: onChange, theme: theme);
                buttons.Add(startWithDesc);

                onChange = delegate(BaseWidget w) { InGame.XmlWorldData.preGame = "Countdown"; InGame.IsLevelDirty = true; };
                startWithCountdown = new RadioButtonLabelHelp(fullScreenContentDialog, Font, null, column2.Size.X, siblings, "editWorldParams.countdown", OnChange: onChange, theme: theme);
                buttons.Add(startWithCountdown);

                onChange = delegate(BaseWidget w) { InGame.XmlWorldData.preGame = "Description with Countdown"; InGame.IsLevelDirty = true; };
                startWithDescCountdown = new RadioButtonLabelHelp(fullScreenContentDialog, Font, null, column2.Size.X, siblings, "editWorldParams.countdownWithDesc", OnChange: onChange, theme: theme);
                buttons.Add(startWithDescCountdown);

                startGameWith = new RadioButtonSetLabelHelp(fullScreenContentDialog, Font, "PreGame", column2.Size.X, buttons, labelId: "editWorldParams.preGame", OnChange: onChange, theme: theme);
                column2.AddWidget(startGameWith);
            }

            {
                // Camera Mode
                Vector2 displaySize = new Vector2(128, 128);
                List<GraphicRadioButton> siblings = new List<GraphicRadioButton>();

                BaseWidget.Callback onChange;

                onChange = delegate(BaseWidget w)
                {
                    Terrain.Current.FixedCamera = false;
                    Terrain.Current.FixedOffsetCamera = false;
                    InGame.IsLevelDirty = true;
                };
                cameraFree = new GraphicRadioButton(fullScreenContentDialog, siblings, displaySize, textureName: @"Textures\CameraModeFree", labelId: "editWorldParams.cameraModeFree", OnChange: onChange, theme: theme);
                cameraFree.LocalRect = new RectangleF(new Vector2(56, 8), displaySize);

                onChange = delegate(BaseWidget w)
                {
                    Terrain.Current.FixedCamera = false;
                    Terrain.Current.FixedOffsetCamera = false;
                    InGame.inGame.SaveFixedCamera();
                    InGame.IsLevelDirty = true;
                };
                cameraFixedPosition = new GraphicRadioButton(fullScreenContentDialog, siblings, displaySize, textureName: @"Textures\CameraModeFixedPosition", labelId: "editWorldParams.cameraModeFixedPosition", OnChange: onChange, theme: theme);
                cameraFixedPosition.LocalRect = new RectangleF(cameraFree.Position + new Vector2(128 + 16, 0), displaySize);

                onChange = delegate(BaseWidget w)
                {
                    Terrain.Current.FixedCamera = false;
                    Terrain.Current.FixedOffsetCamera = false;
                    Terrain.Current.FixedOffsetCamera = true;
                    Terrain.Current.FixedOffset = InGame.inGame.Camera.EyeOffset;
                    InGame.IsLevelDirty = true;
                };
                cameraFixedOffset = new GraphicRadioButton(fullScreenContentDialog, siblings, displaySize, textureName: @"Textures\CameraModeFixedOffset", labelId: "editWorldParams.cameraModeFixedOffset", OnChange: onChange, theme: theme);
                cameraFixedOffset.LocalRect = new RectangleF(cameraFixedPosition.Position + new Vector2(128 + 16, 0), displaySize);

                cameraMode = new GraphicRadioButtonSetLabelHelp(fullScreenContentDialog, Font, "CameraMode", 600.0f, siblings, labelId: "editWorldParams.cameraMode", OnChange: onChange, theme: theme);
                cameraMode.Margin = new Padding(0, 32, 0, 0);
                column2.AddWidget(cameraMode);
            }
            {
                // Set Camera for Camera Mode
                BaseWidget.Callback onChange = delegate(BaseWidget w)
                {
                    // TODO (****) Fill this in.
                };
                setCamera = new ButtonLabelHelp(fullScreenContentDialog, Font, "editWorldParams.setCamera", null, column1.Size.X, onChange, indent: 226);
                column2.AddWidget(setCamera);
            }

            {
                // Starting Camera.
                BaseWidget.Callback onChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null) { Terrain.Current.ShowCompass = cb.Checked; } };
                startingCamera = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "editWorldParams.startingCameraCheckbox", "StartingCameraPosition", column2.Size.X, onChange, theme);
                startingCamera.Margin = new Padding(0, 30, 0, 0);
                column2.AddWidget(startingCamera);
            }
            {
                // Set Starting Camera
                BaseWidget.Callback onChange = delegate(BaseWidget w)
                {
                    // TODO (****) Fill this in.
                };
                setStartingCamera = new ButtonLabelHelp(fullScreenContentDialog, Font, "editWorldParams.setCamera", null, column1.Size.X, onChange, indent: 226);
                column2.AddWidget(setStartingCamera);
            }
            {
                // Camera Spring
                BaseWidget.Callback onChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null) { InGame.CameraSpringStrength = s.CurValue; } };
                cameraSpring = new SliderLabelHelp(fullScreenContentDialog, Font, "editWorldParams.cameraSpringStrength", "CameraSpringStrength", column2.Size.X,
                                                    minValue: 0.0f, maxValue: 1.0f, increment: 0.1f, numDecimals: 2, curValue: InGame.CameraSpringStrength,
                                                    OnChange: onChange, theme: theme);
                cameraSpring.Margin = new Padding(0, 30, 0, 0);
                column2.AddWidget(cameraSpring);
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

            // This creates an underlying tile.  Note that the vertical padding
            // for the second and third ones is large enough to fit under the
            // next widget effectively grouping them together.

            startGameWith.OutlinePadding = new Padding(8, 8, 8, 8);
            startGameWith.RenderOutline = true;

            cameraMode.OutlinePadding = new Padding(8, 8, 8, 60);
            cameraMode.RenderOutline = true;

            startingCamera.OutlinePadding = new Padding(8, 8, 8, 60);
            startingCamera.RenderOutline = true;

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
            glassWalls.Checked = Terrain.Current.GlassWalls;
            showCompass.Checked = Terrain.Current.ShowCompass;
            showResourceMeter.Checked = Terrain.Current.ShowResourceMeter;
            enableResourceLimiting.Checked = Terrain.Current.EnableResourceLimiting;
            showVirtualController.Checked = InGame.ShowVirtualController;

            waveHeight.CurValue = Terrain.WaveHeight * 100.0f;
            waterStrength.CurValue = Terrain.WaterStrength * 100.0f;
            minBreeze.CurValue = InGame.WindMin * 100.0f;
            maxBreeze.CurValue = InGame.WindMax * 100.0f;
            effectsVolume.CurValue = InGame.LevelFoleyVolume * 100.0f;
            musicVolume.CurValue = InGame.LevelMusicVolume * 100.0f;

            switch(InGame.XmlWorldData.preGame)
            {
                case "World Title":
                    startWithTitle.Selected = true;
                    break;
                case "World Description":
                    startWithDesc.Selected = true;
                    break;
                case "Countdown":
                    startWithCountdown.Selected = true;
                    break;
                case "Description with Countdown":
                    startWithDescCountdown.Selected = true;
                    break;
                case "Nothing":
                default:
                    startWithNothing.Selected = true;
                    break;
            }

            if (Terrain.Current.FixedCamera)
            {
                cameraFixedPosition.Selected = true;
            }
            else if (Terrain.Current.FixedOffsetCamera)
            {
                cameraFixedOffset.Selected = true;
            }
            else
            {
                cameraFree.Selected = true;
            }

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

    }   // end of class WorldSettingsPage1Scene
}   // end of namespace KoiX.Scenes
