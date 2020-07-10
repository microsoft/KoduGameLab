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
using Boku.SimWorld.Chassis;

namespace KoiX.Scenes
{
    /// <summary>
    /// Page of ObjectSettings.  Formerly DNA menu.
    /// </summary>
    public class ObjectSettingsPage2Scene : BasePageScene
    {
        #region Members

        GameActor actor;

        Texture2D bkgTexture;

        SystemFont titleFont;
        SystemFont font;

        Label title;

        WidgetSet column1;
        WidgetSet column2;
        Vector2 columnSize = new Vector2(600, 850);
        float columnGutter = 100.0f;    // Gap between columns.

        SliderLabelHelp movementSpeedModifier;
        SliderLabelHelp movementAccelerationModifier;
        SliderLabelHelp turningSpeedModifier;
        SliderLabelHelp turningAccelerationModifier;
        SliderLabelHelp verticalSpeedModifier;
        SliderLabelHelp verticalAccelerationModifier;
        CheckBoxLabelHelp immobile;
        
        CheckBoxLabelHelp stayAboveWater;
        SliderLabelHelp bounciness;
        SliderLabelHelp friction;
        CheckBoxLabelHelp debugBarriers;
        CheckBoxLabelHelp debugLOS;
        CheckBoxLabelHelp programmingPage;
        
        #endregion

        #region Accessors

        public GameActor Actor
        {
            get { return actor; }
            set { actor = value; }
        }

        #endregion

        #region Public

        public ObjectSettingsPage2Scene(string nextLabelId = null, string nextLabelText = null, string prevLabelId = null, string prevLabelText = null)
            : base("ObjectSettingsPage2Scene", nextLabelId, nextLabelText, prevLabelId, prevLabelText)
        {
            ThemeSet theme = Theme.CurrentThemeSet;

            titleFont = new SystemFont(theme.TextFontFamily, theme.TextTitleFontSize * 2.0f, System.Drawing.FontStyle.Bold);
            font = new SystemFont(theme.TextFontFamily, theme.TextBaseFontSize * 1.2f, System.Drawing.FontStyle.Regular);

            FontWrapper wrapper = new FontWrapper(null, font);
            GetFont Font = delegate() { return wrapper; };

            title = new Label(fullScreenContentDialog, titleFont, theme.LightTextColor, outlineColor: theme.DarkTextColor, outlineWidth: 2.5f, labelId: "editObjectParams.titlePage2");
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

            /*
             // TODO (****) fill in here...
            {
                // Show Tool Tips
                BaseWidget.Callback onChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null) { XmlObjectSettingsData.ShowToolTips = cb.Checked; } };
                showToolTips = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "ObjectSettingsParams.showToolTips", "ShowToolTips", column1.Size.X, onChange, theme);
                showToolTips.Checked = XmlObjectSettingsData.ShowToolTips;
                column1.AddWidget(showToolTips);
            }
            */

            // Column1
            {
                {
                    // Movement speed modifier.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.MovementSpeedModifier = s.CurValue; InGame.IsLevelDirty = true; } };
                    movementSpeedModifier = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.movementSpeedMultiplier", "SpeedMultiplier", column1.Size.X,
                                                    minValue: 0.1f, maxValue: 5.0f, increment: 0.1f, numDecimals: 1, curValue: 1.0f,
                                                    OnChange: OnChange, theme: theme);
                    column1.AddWidget(movementSpeedModifier);
                }
                {
                    // Movement acceleration modifier.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.LinearAccelerationModifier = s.CurValue; InGame.IsLevelDirty = true; } };
                    movementAccelerationModifier = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.linearAccelerationMultiplier", "AccelerationMultiplier", column1.Size.X,
                                                    minValue: 0.1f, maxValue: 10.0f, increment: 0.1f, numDecimals: 1, curValue: 1.0f,
                                                    OnChange: OnChange, theme: theme);
                    column1.AddWidget(movementAccelerationModifier);
                }
                {
                    // Turning speed modifier.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.TurningSpeedModifier = s.CurValue; InGame.IsLevelDirty = true; } };
                    turningSpeedModifier = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.turningSpeedMultiplier", "SpeedMultiplier", column1.Size.X,
                                                    minValue: 0.1f, maxValue: 5.0f, increment: 0.1f, numDecimals: 1, curValue: 1.0f,
                                                    OnChange: OnChange, theme: theme);
                    column1.AddWidget(turningSpeedModifier);
                }
                {
                    // Turning acceleration modifier.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.LinearAccelerationModifier = s.CurValue; InGame.IsLevelDirty = true; } };
                    turningAccelerationModifier = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.turningAccelerationMultiplier", "AccelerationMultiplier", column1.Size.X,
                                                    minValue: 0.1f, maxValue: 5.0f, increment: 0.1f, numDecimals: 1, curValue: 1.0f,
                                                    OnChange: OnChange, theme: theme);
                    column1.AddWidget(turningAccelerationModifier);
                }
                {
                    // Vertical speed modifier.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.VerticalSpeedModifier = s.CurValue; InGame.IsLevelDirty = true; } };
                    verticalSpeedModifier = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.verticalSpeedMultiplier", "SpeedMultiplier", column1.Size.X,
                                                    minValue: 0.1f, maxValue: 5.0f, increment: 0.1f, numDecimals: 1, curValue: 1.0f,
                                                    OnChange: OnChange, theme: theme);
                    column1.AddWidget(verticalSpeedModifier);
                }
                {
                    // Vertical acceleration modifier.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.LinearAccelerationModifier = s.CurValue; InGame.IsLevelDirty = true; } };
                    verticalAccelerationModifier = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.verticalAccelerationMultiplier", "AccelerationMultiplier", column1.Size.X,
                                                    minValue: 0.1f, maxValue: 10.0f, increment: 0.1f, numDecimals: 1, curValue: 1.0f,
                                                    OnChange: OnChange, theme: theme);
                    column1.AddWidget(verticalAccelerationModifier);
                }
                {
                    // Immobile.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null && actor != null) { actor.TweakImmobile = cb.Checked; InGame.IsLevelDirty = true; } };
                    immobile = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "editObjectParams.immobile", "Immobile", column1.Size.X, OnChange, theme);
                    column1.AddWidget(immobile);
                }

            }

            // Column2
            {
                {
                    // StayAboveWater.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null && actor != null) { actor.StayAboveWater = cb.Checked; InGame.IsLevelDirty = true; } };
                    stayAboveWater = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "editObjectParams.StayAboveWater", "StayAboveWater", column2.Size.X, OnChange, theme);
                    column2.AddWidget(stayAboveWater);
                }
                {
                    // Bounciness
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.CoefficientOfRestitution = s.CurValue; InGame.IsLevelDirty = true; } };
                    bounciness = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.bounciness", "Bounciness", column2.Size.X,
                                                    minValue: 0.0f, maxValue: 1.0f, increment: 0.05f, numDecimals: 2, curValue: 0.5f,
                                                    OnChange: OnChange, theme: theme);
                    column2.AddWidget(bounciness);
                }
                {
                    // Friction
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.Friction = s.CurValue; InGame.IsLevelDirty = true; } };
                    friction = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.friction", "Friction", column2.Size.X,
                                                    minValue: 0.0f, maxValue: 1.0f, increment: 0.05f, numDecimals: 2, curValue: 0.5f,
                                                    OnChange: OnChange, theme: theme);
                    column2.AddWidget(friction);
                }
                {
                    // DebugBarriers.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null && actor != null) { actor.DisplayLOS = cb.Checked; InGame.IsLevelDirty = true; } };
                    debugBarriers = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "editObjectParams.displayLOS", "Debug:DisplayLineOfSight", column2.Size.X, OnChange, theme);
                    column2.AddWidget(debugBarriers);
                }
                {
                    // DebugLOS.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null && actor != null) { actor.DisplayLOS = cb.Checked; InGame.IsLevelDirty = true; } };
                    debugLOS = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "editObjectParams.displayLOP", "Debug:DrawLinesShowingWhatISeeAndHear", column2.Size.X, OnChange, theme);
                    column2.AddWidget(debugLOS);
                }
                {
                    // Current programming page.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null && actor != null) { actor.DisplayCurrentPage = cb.Checked; InGame.IsLevelDirty = true; } };
                    programmingPage = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "editObjectParams.displayCurrentPage", "Debug:DrawCurrentPage", column2.Size.X, OnChange, theme);
                    column2.AddWidget(programmingPage);
                }

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
            // Actor is null at creation time, so on activation we need to set any actor specific values.
            movementSpeedModifier.TargetValue = Actor.MovementSpeedModifier;
            movementAccelerationModifier.TargetValue = Actor.LinearAccelerationModifier;
            turningSpeedModifier.TargetValue = Actor.TurningSpeedModifier;
            turningAccelerationModifier.TargetValue = Actor.TurningAccelerationModifier;
            verticalSpeedModifier.TargetValue = Actor.VerticalSpeedModifier;
            verticalAccelerationModifier.TargetValue = Actor.VerticalAccelerationModifier;
            immobile.Checked = Actor.TweakImmobile;

            stayAboveWater.Checked = Actor.StayAboveWater;
            bounciness.TargetValue = Actor.CoefficientOfRestitution;
            friction.TargetValue = Actor.Friction;
            debugBarriers.Checked = Actor.DisplayLOS;
            debugLOS.Checked = Actor.DisplayLOP;
            programmingPage.Checked = Actor.DisplayCurrentPage;

            //
            // Based on this bot's capabilites, decide which elements to add..
            //
            bool isFixed = (actor.Chassis == null) || actor.Chassis.FixedPosition;
            bool isDynamic = actor.Chassis is DynamicPropChassis;
            bool isSpin = actor.Chassis is SitAndSpinChassis || (actor.Chassis is DynamicPropChassis && actor.Version >= 1);
            bool noSpin = actor.Chassis is PuckChassis || actor.Chassis is SaucerChassis;
            bool vertical = actor.Chassis is FloatInAirChassis || actor.Chassis is SaucerChassis || actor.Chassis is SwimChassis || actor.Chassis is HoverSwimChassis;
            bool isAlwaysImmobile = actor.Chassis is PipeChassis || isFixed;

            column1.Widgets.Clear();

            if (!isFixed && !isDynamic)
            {
                column1.AddWidget(movementSpeedModifier);
                column1.AddWidget(movementAccelerationModifier);
            }
            if ((!isFixed && !isDynamic && !noSpin) || isSpin)
            {
                column1.AddWidget(turningSpeedModifier);
                column1.AddWidget(turningAccelerationModifier);
            }
            if (vertical)
            {
                column1.AddWidget(verticalSpeedModifier);
                column1.AddWidget(verticalAccelerationModifier);
            }
            if (!isAlwaysImmobile)
            {
                column1.AddWidget(immobile);
            }


            column2.Widgets.Clear();

            if (!(Actor.Chassis is BoatChassis
                || Actor.Chassis is CursorChassis
                || Actor.Chassis is CycleChassis
                || Actor.Chassis is DynamicPropChassis
                || Actor.Chassis is StaticPropChassis
                || Actor.Chassis is SwimChassis
                || Actor.Chassis is RoverChassis))
            {
                column2.AddWidget(stayAboveWater);
            }
            if (isDynamic)
            {
                column2.AddWidget(bounciness);
            }
            if (!isAlwaysImmobile)
            {
                column2.AddWidget(friction);
            }
            column2.AddWidget(debugBarriers);
            column2.AddWidget(debugLOS);
            column2.AddWidget(programmingPage);


            // Connect navigation links.
            fullScreenContentDialog.CreateTabList();

            // Dpad nav includes all widgets.
            fullScreenContentDialog.CreateDPadLinks();

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

    }   // end of class ObjectSettingsPage2Scene
}   // end of namespace KoiX.Scenes
