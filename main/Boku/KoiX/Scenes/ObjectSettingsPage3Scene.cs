
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
    /// Page of ObjectSettings.  Formerly DNA menu.
    /// </summary>
    public class ObjectSettingsPage3Scene : BasePageScene
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

        // We really don't need these refs.  Once we add them to the column widgetsets
        // that's the only ref we need.  Leaving these here just to make debugging easier.

        SliderLabelHelp blipDamage;
        SliderLabelHelp blipReloadTime;
        SliderLabelHelp blipRange;
        SliderLabelHelp blipSpeed;
        SliderLabelHelp blipsInAir;

        CheckBoxLabelHelp showHitPoints;
        SliderLabelHelp maxHitPoints;
        CheckBoxLabelHelp invulnerable;


        SliderLabelHelp missileDamage;
        SliderLabelHelp missileReloadTime;
        SliderLabelHelp missileRange;
        SliderLabelHelp missileSpeed;
        SliderLabelHelp missilesInAir;
        CheckBoxLabelHelp showMissileTrails;

        CheckBoxLabelHelp shieldEffects;
        CheckBoxLabelHelp invisible;
        CheckBoxLabelHelp ghost;
        CheckBoxLabelHelp camouflaged;

        #endregion

        #region Accessors

        public GameActor Actor
        {
            get { return actor; }
            set { actor = value; }
        }

        #endregion

        #region Public

        public ObjectSettingsPage3Scene(string nextLabelId = null, string nextLabelText = null, string prevLabelId = null, string prevLabelText = null)
            : base("ObjectSettingsPage3Scene", nextLabelId, nextLabelText, prevLabelId, prevLabelText)
        {
            ThemeSet theme = Theme.CurrentThemeSet;

            titleFont = new SystemFont(theme.TextFontFamily, theme.TextTitleFontSize * 2.0f, System.Drawing.FontStyle.Bold);
            font = new SystemFont(theme.TextFontFamily, theme.TextBaseFontSize * 1.2f, System.Drawing.FontStyle.Regular);

            FontWrapper wrapper = new FontWrapper(null, font);
            GetFont Font = delegate() { return wrapper; };

            title = new Label(fullScreenContentDialog, titleFont, theme.LightTextColor, outlineColor: theme.DarkTextColor, outlineWidth: 2.5f, labelId: "editObjectParams.titlePage3");
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
             // TODO (scoy) fill in here...
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
                    // BlipDamage.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.BlipDamage = (int)s.CurValue; InGame.IsLevelDirty = true; } };
                    blipDamage = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.blipDamage", "BlipDamage", column1.Size.X,
                                                    minValue: -500, maxValue: 500, increment: 1, numDecimals: 0, curValue: 10,
                                                    OnChange: OnChange, theme: theme);
                    column1.AddWidget(blipDamage);
                }
                {
                    // BlipReloadTime.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.BlipReloadTime = s.CurValue; InGame.IsLevelDirty = true; } };
                    blipReloadTime = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.blipReloadTime", "BlipReloadTime", column1.Size.X,
                                                    minValue: 0.05f, maxValue: 3.0f, increment: 0.05f, numDecimals: 2, curValue: 0.1f,
                                                    OnChange: OnChange, theme: theme);
                    column1.AddWidget(blipReloadTime);
                }
                {
                    // BlipsInAir
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.BlipsInAir = (int)s.CurValue; InGame.IsLevelDirty = true; } };
                    blipsInAir = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.blipDamage", "BlipDamage", column1.Size.X,
                                                    minValue: 0, maxValue: 200, increment: 1, numDecimals: 0, curValue: 100,
                                                    OnChange: OnChange, theme: theme);
                    column1.AddWidget(blipsInAir);
                }
                {
                    // BlipRange.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.BlipRange = s.CurValue; InGame.IsLevelDirty = true; } };
                    blipRange = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.blipRange", "BlipRange", column1.Size.X,
                                                    minValue: 10.0f, maxValue: 100.0f, increment: 5.0f, numDecimals: 0, curValue: 0.1f,
                                                    OnChange: OnChange, theme: theme);
                    column1.AddWidget(blipRange);
                }
                {
                    // BlipSpeed.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.BlipSpeed = (int)s.CurValue; InGame.IsLevelDirty = true; } };
                    blipSpeed = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.blipSpeed", "BlipSpeed", column1.Size.X,
                                                    minValue: 5.0f, maxValue: 100.0f, increment: 5.0f, numDecimals: 0, curValue: 0.1f,
                                                    OnChange: OnChange, theme: theme);
                    column1.AddWidget(blipSpeed);
                }

                {
                    // ShowHitPoints.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null && actor != null) { actor.ShowHitPoints = cb.Checked; InGame.IsLevelDirty = true; } };
                    showHitPoints = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "editObjectParams.showHitPoints", "ShowHitPoints", column1.Size.X, OnChange, theme);
                    column1.AddWidget(showHitPoints);
                }
                {
                    // MaxHitPoints.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.MaxHitPoints = (int)s.CurValue; InGame.IsLevelDirty = true; } };
                    maxHitPoints = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.maxHitPoints", "MaxHitPoints", column1.Size.X,
                                                    minValue: 0, maxValue: 1000, increment: 5, numDecimals: 0, curValue: 100,
                                                    OnChange: OnChange, theme: theme);
                    column1.AddWidget(maxHitPoints);
                }
                {
                    // Invulnerable
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null && actor != null) { actor.Invulnerable = cb.Checked; InGame.IsLevelDirty = true; } };
                    invulnerable = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "editObjectParams.invulnerable", "Invulnerable", column1.Size.X, OnChange, theme);
                    column1.AddWidget(invulnerable);
                }

            }

            // Column2
            {
                {
                    // MissileDamage.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.MissileDamage = (int)s.CurValue; InGame.IsLevelDirty = true; } };
                    missileDamage = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.missileDamage", "MissileDamage", column2.Size.X,
                                                    minValue: -500, maxValue: 500, increment: 1, numDecimals: 0, curValue: 10,
                                                    OnChange: OnChange, theme: theme);
                    column2.AddWidget(missileDamage);
                }
                {
                    // MissileReloadTime.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.MissileReloadTime = s.CurValue; InGame.IsLevelDirty = true; } };
                    missileReloadTime = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.missileReloadTime", "MissileReloadTime", column2.Size.X,
                                                    minValue: 0.5f, maxValue: 5.0f, increment: 0.5f, numDecimals: 1, curValue: 0.1f,
                                                    OnChange: OnChange, theme: theme);
                    column2.AddWidget(missileReloadTime);
                }
                {
                    // MissilesInAir
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.MissilesInAir = (int)s.CurValue; InGame.IsLevelDirty = true; } };
                    missilesInAir = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.missileDamage", "MissileDamage", column2.Size.X,
                                                    minValue: 0, maxValue: 10, increment: 1, numDecimals: 0, curValue: 100,
                                                    OnChange: OnChange, theme: theme);
                    column2.AddWidget(missilesInAir);
                }
                {
                    // MissileRange.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.MissileRange = s.CurValue; InGame.IsLevelDirty = true; } };
                    missileRange = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.missileRange", "MissileRange", column2.Size.X,
                                                    minValue: 10.0f, maxValue: 100.0f, increment: 5.0f, numDecimals: 0, curValue: 0.1f,
                                                    OnChange: OnChange, theme: theme);
                    column2.AddWidget(missileRange);
                }
                {
                    // MissileSpeed.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.MissileSpeed = (int)s.CurValue; InGame.IsLevelDirty = true; } };
                    missileSpeed = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.missileSpeed", "MissileSpeed", column2.Size.X,
                                                    minValue: 1.0f, maxValue: 20.0f, increment: 1.0f, numDecimals: 0, curValue: 0.1f,
                                                    OnChange: OnChange, theme: theme);
                    column2.AddWidget(missileSpeed);
                }
                {
                    // ShowMissileTrails.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null && actor != null) { actor.MissileTrails = cb.Checked; InGame.IsLevelDirty = true; } };
                    showMissileTrails = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "editObjectParams.missileTrails", "MissileSmoke", column2.Size.X, OnChange, theme);
                    column2.AddWidget(showMissileTrails);
                }

                {
                    // ShieldEffects.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null && actor != null) { actor.ShieldEffects = cb.Checked; InGame.IsLevelDirty = true; } };
                    shieldEffects = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "editObjectParams.shieldEffects", "ShieldEffects", column2.Size.X, OnChange, theme);
                    column2.AddWidget(shieldEffects);
                }
                {
                    // Invisible.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null && actor != null) { actor.Invisible = cb.Checked; InGame.IsLevelDirty = true; } };
                    invisible = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "editObjectParams.invisible", "Invisible", column2.Size.X, OnChange, theme);
                    column2.AddWidget(invisible);
                }
                {
                    // Ghost.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null && actor != null) { actor.Ignored = cb.Checked; InGame.IsLevelDirty = true; } };
                    ghost = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "editObjectParams.ignored", "Ignored", column2.Size.X, OnChange, theme);
                    column2.AddWidget(ghost);
                }
                {
                    // Camouflage.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null && actor != null) { actor.Camouflaged = cb.Checked; InGame.IsLevelDirty = true; } };
                    camouflaged = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "editObjectParams.camouflaged", "Camouflaged", column2.Size.X, OnChange, theme);
                    column2.AddWidget(camouflaged);
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
            blipDamage.TargetValue = Actor.BlipDamage;
            blipReloadTime.TargetValue = Actor.BlipReloadTime;
            blipRange.TargetValue = Actor.BlipRange;
            blipSpeed.TargetValue = Actor.BlipSpeed;
            blipsInAir.TargetValue = Actor.BlipsInAir;

            showHitPoints.Checked = Actor.ShowHitPoints;
            maxHitPoints.TargetValue = Actor.MaxHitPoints;
            invulnerable.Checked = Actor.Invulnerable;

            missileDamage.TargetValue = Actor.MissileDamage;
            missileReloadTime.TargetValue = Actor.MissileReloadTime;
            missileRange.TargetValue = Actor.MissileRange;
            missileSpeed.TargetValue = Actor.MissileSpeed;
            missilesInAir.TargetValue = Actor.MissilesInAir;
            showMissileTrails.Checked = Actor.MissileTrails;

            shieldEffects.Checked = Actor.ShieldEffects;
            invisible.Checked = Actor.Invisible;
            ghost.Checked = Actor.Ignored;
            camouflaged.Checked = Actor.Camouflaged;


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

    }   // end of class ObjectSettingsPage3Scene
}   // end of namespace KoiX.Scenes
