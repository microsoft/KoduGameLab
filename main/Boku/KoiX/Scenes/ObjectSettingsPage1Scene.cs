
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
    public class ObjectSettingsPage1Scene : BasePageScene
    {
        #region Members

        GameActor actor;    // Actor whose settings we are changing.

        Texture2D bkgTexture;

        SystemFont titleFont;
        SystemFont font;

        Label title;

        WidgetSet topSet;   // Contains the radio buttons for bot color.
        Vector2 topSize = new Vector2(1600, 128);

        WidgetSet column1;
        WidgetSet column2;
        Vector2 columnSize = new Vector2(600, 850 - 128);   // Note the 128 is the topSet vertical size.
        float columnGutter = 100.0f;                        // Gap between columns.

        // We really don't need these refs.  Once we add them to the column widgetsets
        // that's the only ref we need.  Leaving these here just to make debugging easier.
        SingleLineInputHelp rename;
        CheckBoxLabelHelp creatable;
        SliderLabelHelp maxCreatables;
        SliderLabelHelp size;
        SliderLabelHelp holdDistance;
        SliderLabelHelp glowStrength;
        SliderLabelHelp glowLightStrength;
        SliderLabelHelp glowSelfLighting;

        CheckBoxLabelHelp mute;
        SliderLabelHelp hearingDistance;
        SliderLabelHelp nearByDistance;
        SliderLabelHelp farDistance;
        SliderLabelHelp kickStrength;
        SliderLabelHelp kickRate;
        SliderLabelHelp pushRange;
        SliderLabelHelp pushStrength;

        #endregion

        #region Accessors

        public GameActor Actor
        {
            get { return actor; }
            set { actor = value; }
        }

        #endregion

        #region Public

        public ObjectSettingsPage1Scene(string nextLabelId = null, string nextLabelText = null, string prevLabelId = null, string prevLabelText = null)
            : base("ObjectSettingsPage1Scene", nextLabelId, nextLabelText, prevLabelId, prevLabelText)
        {
            ThemeSet theme = Theme.CurrentThemeSet;

            titleFont = new SystemFont(theme.TextFontFamily, theme.TextTitleFontSize * 2.0f, System.Drawing.FontStyle.Bold);
            font = new SystemFont(theme.TextFontFamily, theme.TextBaseFontSize * 1.2f, System.Drawing.FontStyle.Regular);

            FontWrapper wrapper = new FontWrapper(null, font);
            GetFont Font = delegate() { return wrapper; };

            title = new Label(fullScreenContentDialog, titleFont, theme.LightTextColor, outlineColor: theme.DarkTextColor, outlineWidth: 2.5f, labelId: "editObjectParams.titlePage1");
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

            topSet = new WidgetSet(fullScreenContentDialog, new RectangleF(100, 100, topSize.X, topSize.Y), Orientation.Horizontal);    // Default sot center justification in both directions.
            column1 = new WidgetSet(fullScreenContentDialog, new RectangleF(new Vector2(150, topSize.Y + 100), columnSize), Orientation.Vertical, horizontalJustification: Justification.Left, verticalJustification: Justification.Top);
            column2 = new WidgetSet(fullScreenContentDialog, new RectangleF(new Vector2(850, topSize.Y + 100), columnSize), Orientation.Vertical, horizontalJustification: Justification.Left, verticalJustification: Justification.Top);
            fullScreenContentDialog.AddWidget(topSet);
            fullScreenContentDialog.AddWidget(column1);
            fullScreenContentDialog.AddWidget(column2);

            // TopSet
            {
                List<ColorRadioButton> siblings = new List<ColorRadioButton>();

                BaseWidget.Callback OnChange = null;

                OnChange = delegate(BaseWidget w)
                {
                    ColorRadioButton b = w as ColorRadioButton;
                    Debug.Assert(b != null, "If this is null, something is broken.");
                    if (b != null)
                    {
                        Debug.Assert(b.ClassColor != Classification.Colors.None, "Why do we have a button with an invalid ClassColor?");
                        Debug.Assert(b.ClassColor != Classification.Colors.NotApplicable, "Why do we have a button with an invalid ClassColor?");

                        actor.SetColor(b.ClassColor);

                        InGame.RefreshThumbnail = true;
                        InGame.IsLevelDirty = true;
                    }
                };

                float radius = 48.0f;
                int margin = 16;
                for (int i = (int)Classification.ColorInfo.First; i <= (int)Classification.ColorInfo.Last; i++)
                {
                    ColorRadioButton button = new ColorRadioButton(fullScreenContentDialog, siblings, radius, (Classification.Colors)i, OnChange: OnChange, theme: theme);
                    button.Margin = new Padding(margin, 0, margin, 0);  // Add horizontal margin.
                    topSet.AddWidget(button);
                }
            }

            // Column1
            {
                {
                    // Rename.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w)
                    {
                        string newName = rename.Text;

                        if (newName.Length > 0)
                        {
                            newName = TextHelper.FilterURLs(newName);
                            newName = TextHelper.FilterEmail(newName);

                            Actor.DisplayName = newName;
                            Boku.Programming.NamedFilter.RegisterInCardSpace(Actor);
                            Boku.InGame.IsLevelDirty = true;
                        }
                    };
                    rename = new SingleLineInputHelp(fullScreenContentDialog, Font, defaultTextId: null, helpId: "Rename", width: column1.Size.X, OnChange: OnChange, theme: theme);
                    column1.AddWidget(rename);

                    // Add a bit of a gap above and below this widget.
                    rename.Margin = new Padding(0, 8, 0, 6);
                }
                {
                    // Creatable.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null && actor != null) { actor.Creatable = cb.Checked; InGame.IsLevelDirty = true; } };
                    creatable = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "editObjectParams.creatable", "Creatable", column1.Size.X, OnChange, theme);
                    creatable.Margin = new Padding(0, 0, 0, 0);
                    column1.AddWidget(creatable);
                }
                {
                    // Max creatables.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.MaxCreated = (int)s.CurValue; InGame.IsLevelDirty = true; } };
                    maxCreatables = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.maxCreated", "MaxCreated", column1.Size.X,
                                                    minValue: 0.0f, maxValue: 1000.0f, increment: 1.00f, numDecimals: 0, curValue: 100,
                                                    OnChange: OnChange, theme: theme);
                    column1.AddWidget(maxCreatables);
                }
                {
                    // Size.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.ReScale = s.CurValue; InGame.IsLevelDirty = true; } };
                    size = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.reScale", "ReScale", column1.Size.X,
                                                    minValue: 0.2f, maxValue: 4.0f, increment: 0.1f, numDecimals: 1, curValue: 1.0f,
                                                    OnChange: OnChange, theme: theme);
                    column1.AddWidget(size);
                }
                {
                    // Hold distance.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.HoldDistance = s.CurValue; InGame.IsLevelDirty = true; } };
                    holdDistance = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.holdDistance", "HoldDistance", column1.Size.X,
                                                    minValue: 1.0f, maxValue: 5.0f, increment: 0.1f, numDecimals: 1, curValue: 1.0f,
                                                    OnChange: OnChange, theme: theme);
                    column1.AddWidget(holdDistance);
                }
                {
                    // Glow Strength.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.GlowAmt = s.CurValue; InGame.IsLevelDirty = true; } };
                    glowStrength = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.glowAmt", "GlowStrength", column1.Size.X,
                                                    minValue: 0.0f, maxValue: 1.0f, increment: 0.1f, numDecimals: 1, curValue: 0.0f,
                                                    OnChange: OnChange, theme: theme);
                    column1.AddWidget(glowStrength);
                }
                {
                    // Glow Light Strength.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.GlowLights = s.CurValue; InGame.IsLevelDirty = true; } };
                    glowLightStrength = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.glowLights", "GlowLightStrength", column1.Size.X,
                                                    minValue: 0.0f, maxValue: 1.0f, increment: 0.1f, numDecimals: 1, curValue: 0.0f,
                                                    OnChange: OnChange, theme: theme);
                    column1.AddWidget(glowLightStrength);
                }
                {
                    // Glow Self Lighting.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.GlowEmission = s.CurValue; InGame.IsLevelDirty = true; } };
                    glowSelfLighting = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.glowEmission", "GlowEmission", column1.Size.X,
                                                    minValue: 0.0f, maxValue: 1.0f, increment: 0.1f, numDecimals: 1, curValue: 0.0f,
                                                    OnChange: OnChange, theme: theme);
                    column1.AddWidget(glowSelfLighting);
                }

            }

            // Column2
            {
                {
                    // Mute.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { CheckBox cb = w as CheckBox; if (cb != null && actor != null) { actor.Mute = cb.Checked; InGame.IsLevelDirty = true; } };
                    mute = new CheckBoxLabelHelp(fullScreenContentDialog, Font, "editObjectParams.mute", "Mute", column2.Size.X, OnChange, theme);
                    column2.AddWidget(mute);
                }
                {
                    // Hearing distance.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.Hearing = s.CurValue / 100.0f; InGame.IsLevelDirty = true; } };
                    hearingDistance = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.hearing", "Hearing", column2.Size.X,
                                                    minValue: 0.0f, maxValue: 100.0f, increment: 0.5f, numDecimals: 1, curValue: 100.0f,
                                                    OnChange: OnChange, theme: theme);
                    column2.AddWidget(hearingDistance);
                }
                {
                    // Near by distance.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.NearByDistance = s.CurValue; InGame.IsLevelDirty = true; } };
                    nearByDistance = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.nearByDistance", "NearByDistance", column2.Size.X,
                                                    minValue: 0.0f, maxValue: 100.0f, increment: 0.5f, numDecimals: 1, curValue: 5.0f,
                                                    OnChange: OnChange, theme: theme);
                    column2.AddWidget(nearByDistance);
                }
                {
                    // Far distance.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.FarAwayDistance = s.CurValue; InGame.IsLevelDirty = true; } };
                    farDistance = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.farAwayDistance", "FarAwayDistance", column2.Size.X,
                                                    minValue: 0.0f, maxValue: 100.0f, increment: 0.5f, numDecimals: 1, curValue: 25.0f,
                                                    OnChange: OnChange, theme: theme);
                    column2.AddWidget(farDistance);
                }
                {
                    // Kick strength.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.KickStrength = s.CurValue; InGame.IsLevelDirty = true; } };
                    kickStrength = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.kickStrength", "KickStrength", column2.Size.X,
                                                    minValue: 1.0f, maxValue: 20.0f, increment: 1.0f, numDecimals: 0, curValue: 5.0f,
                                                    OnChange: OnChange, theme: theme);
                    column2.AddWidget(kickStrength);
                }
                {
                    // Kick rate.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.KickRate = s.CurValue; InGame.IsLevelDirty = true; } };
                    kickRate = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.kickRate", "KickRate", column2.Size.X,
                                                    minValue: 1.0f, maxValue: 10.0f, increment: 1.0f, numDecimals: 0, curValue: 5.0f,
                                                    OnChange: OnChange, theme: theme);
                    column2.AddWidget(kickRate);
                }
                {
                    // Push Range.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.PushRange = s.CurValue; InGame.IsLevelDirty = true; } };
                    pushRange = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.pushrange", "PushRange", column2.Size.X,
                                                    minValue: 1.0f, maxValue: 100.0f, increment: 5.0f, numDecimals: 0, curValue: 100.0f,
                                                    OnChange: OnChange, theme: theme);
                    column2.AddWidget(pushRange);
                }
                {
                    // Push Strength.
                    BaseWidget.Callback OnChange = delegate(BaseWidget w) { Slider s = w as Slider; if (s != null && actor != null) { actor.PushStrength = s.CurValue; InGame.IsLevelDirty = true; } };
                    pushStrength = new SliderLabelHelp(fullScreenContentDialog, Font, "editObjectParams.pushstrength", "PushStrength", column2.Size.X,
                                                    minValue: 1.0f, maxValue: 150.0f, increment: 5.0f, numDecimals: 0, curValue: 100.0f,
                                                    OnChange: OnChange, theme: theme);
                    column2.AddWidget(pushStrength);
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
            column1.Position = new Vector2(screenSize.X / 2.0f - columnSize.X - columnGutter / 2.0f, topSize.Y + 100).Truncate();
            column2.Position = new Vector2(screenSize.X / 2.0f + columnGutter / 2.0f, topSize.Y + 100).Truncate();

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

            // Set color matching actor's current color.
            foreach (BaseWidget w in topSet.Widgets)
            {
                ColorRadioButton b = w as ColorRadioButton;
                if (b != null)
                {
                    if (b.ClassColor == Actor.ClassColor)
                    {
                        b.Selected = true;
                        break;
                    }
                }
            }

            rename.Text = Actor.DisplayName;
            creatable.Checked = Actor.Creatable;
            maxCreatables.TargetValue = Actor.MaxCreated;
            size.TargetValue = Actor.ReScale;
            holdDistance.TargetValue = Actor.HoldDistance;
            glowStrength.TargetValue = Actor.GlowAmt;
            glowLightStrength.TargetValue = Actor.GlowLights;
            glowSelfLighting.TargetValue = Actor.GlowEmission;

            mute.Checked = Actor.Mute;
            hearingDistance.TargetValue = Actor.Hearing * 100.0f;
            nearByDistance.TargetValue = Actor.NearByDistance;
            farDistance.TargetValue = Actor.FarAwayDistance;
            kickStrength.TargetValue = Actor.KickStrength;
            kickRate.TargetValue = Actor.KickRate;
            pushRange.TargetValue = Actor.PushRange;
            pushStrength.TargetValue = Actor.PushStrength;

            if (Actor is Fan)
            {
                column2.AddWidget(pushRange);
                column2.AddWidget(pushStrength);
            }
            else
            {
                column2.Widgets.Remove(pushRange);
                column2.Widgets.Remove(pushStrength);
            }

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

    }   // end of class ObjectSettingsPage1Scene
}   // end of namespace KoiX.Scenes
