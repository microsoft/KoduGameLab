
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

using BokuShared;

namespace KoiX.Scenes
{
    /// <summary>
    /// Page of WorldSettings.  Formerly DNA menu.
    /// </summary>
    public class WorldSettingsPage2Scene : BasePageScene
    {
        #region Members

        Texture2D bkgTexture;

        SystemFont titleFont;
        SystemFont font;

        Label title;

        WidgetSet column1;
        Vector2 columnSize = new Vector2(1200, 850);

        WidgetSet bottomSet;    // Used to contain the sky and nextLevel elements.

        // We really don't need these refs.  Once we add them to the column widgetsets
        // that's the only ref we need.  Leaving these here just to make debugging easier.
        GraphicRadioButtonSetLabelHelp sky;
        GraphicRadioButtonSetLabelHelp lightRig;
        LinkedLevelLabelHelp linkedLevel;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public WorldSettingsPage2Scene(string nextLabelId = null, string nextLabelText = null, string prevLabelId = null, string prevLabelText = null)
            : base("WorldSettingsPage2Scene", nextLabelId, nextLabelText, prevLabelId, prevLabelText)
        {
            ThemeSet theme = Theme.CurrentThemeSet;

            titleFont = new SystemFont(theme.TextFontFamily, theme.TextTitleFontSize * 2.0f, System.Drawing.FontStyle.Bold);
            font = new SystemFont(theme.TextFontFamily, theme.TextBaseFontSize * 1.2f, System.Drawing.FontStyle.Regular);

            FontWrapper wrapper = new FontWrapper(null, font);
            GetFont Font = delegate() { return wrapper; };

            title = new Label(fullScreenContentDialog, titleFont, theme.LightTextColor, outlineColor: theme.DarkTextColor, outlineWidth: 2.5f, labelId: "worldSettings.environment");
            title.Position = new Vector2(200, 5);
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

            column1 = new WidgetSet(fullScreenContentDialog, new RectangleF(new Vector2(150, 100), columnSize), Orientation.Vertical, horizontalJustification: Justification.Center, verticalJustification: Justification.Top);
            fullScreenContentDialog.AddWidget(column1);

            {
                // Sky
                Vector2 displaySize = new Vector2(96, 96);
                int margin = 16;
                List<GraphicRadioButton> siblings = new List<GraphicRadioButton>();

                BaseWidget.Callback onChange = null;

                onChange = delegate(BaseWidget w)
                {
                    int index = (int)w.Data;
                    Terrain.SkyIndex = index;
                    InGame.IsLevelDirty = true;
                    InGame.RefreshThumbnail = true;
                };

                Vector2 pos = Vector2.Zero;
                for (int i = 0; i < 21; i++)
                {
                    Texture2D texture = CreateGradientSkyTexture(i);
                    GraphicRadioButton grb = new GraphicRadioButton(fullScreenContentDialog, siblings, displaySize, texture: texture, labelText: (i + 1).ToString(), OnChange: onChange, theme: theme, data: (object)i);
                    grb.LocalRect = new RectangleF(pos, displaySize);
                    if (i == 6 || i == 13)
                    {
                        pos.X = 0;
                        pos.Y += displaySize.Y + margin;
                    }
                    else
                    {
                        pos.X += displaySize.X + margin;
                    }
                }

                float width = 7 * displaySize.X + 6 * margin;
                sky = new GraphicRadioButtonSetLabelHelp(fullScreenContentDialog, Font, "Sky", width, siblings, labelText: Strings.Localize("editWorldParams.skyPictureList"), OnChange: onChange, theme: theme);
                sky.Margin = new Padding(0, 32, 0, 0);
                column1.AddWidget(sky);
            }

            bottomSet = new WidgetSet(fullScreenContentDialog, new RectangleF(0, 0, 1600, 0), Orientation.Horizontal);
            column1.AddWidget(bottomSet);

            {
                // Lighting
                Vector2 displaySize = new Vector2(96, 96);
                int margin = 16;
                List<GraphicRadioButton> siblings = new List<GraphicRadioButton>();

                BaseWidget.Callback onChange = null;

                onChange = delegate(BaseWidget w)
                {
                    GraphicRadioButton b = w as GraphicRadioButton;
                    if (b != null)
                    {
                        string rigName = b.TextureName.Substring(13);       // Skip "Textures\Icon" at beginning of name.
                        rigName = rigName.Substring(0, rigName.Length - 3); // Cut off "Rig" at end.
                        InGame.LightRig = rigName;
                        InGame.IsLevelDirty = true;
                        InGame.RefreshThumbnail = true;
                    }
                };

                Vector2 pos = Vector2.Zero;
                GraphicRadioButton grb = new GraphicRadioButton(fullScreenContentDialog, siblings, displaySize, textureName: @"Textures\IconDayRig", labelId: "lightRigNames.day", OnChange: onChange, theme: theme);
                grb.LocalRect = new RectangleF(pos, displaySize);
                pos.X += displaySize.X + margin;

                grb = new GraphicRadioButton(fullScreenContentDialog, siblings, displaySize, textureName: @"Textures\IconNightRig", labelId: "lightRigNames.night", OnChange: onChange, theme: theme);
                grb.LocalRect = new RectangleF(pos, displaySize);
                pos.X += displaySize.X + margin;

                grb = new GraphicRadioButton(fullScreenContentDialog, siblings, displaySize, textureName: @"Textures\IconSpaceRig", labelId: "lightRigNames.space", OnChange: onChange, theme: theme);
                grb.LocalRect = new RectangleF(pos, displaySize);
                pos.X += displaySize.X + margin;

                grb = new GraphicRadioButton(fullScreenContentDialog, siblings, displaySize, textureName: @"Textures\IconDreamRig", labelId: "lightRigNames.dream", OnChange: onChange, theme: theme);
                grb.LocalRect = new RectangleF(pos, displaySize);
                pos.X = 0;
                pos.Y += displaySize.Y + margin;

                grb = new GraphicRadioButton(fullScreenContentDialog, siblings, displaySize, textureName: @"Textures\IconVenusRig", labelId: "lightRigNames.venus", OnChange: onChange, theme: theme);
                grb.LocalRect = new RectangleF(pos, displaySize);
                pos.X += displaySize.X + margin;

                grb = new GraphicRadioButton(fullScreenContentDialog, siblings, displaySize, textureName: @"Textures\IconMarsRig", labelId: "lightRigNames.mars", OnChange: onChange, theme: theme);
                grb.LocalRect = new RectangleF(pos, displaySize);
                pos.X += displaySize.X + margin;

                grb = new GraphicRadioButton(fullScreenContentDialog, siblings, displaySize, textureName: @"Textures\IconDarkRig", labelId: "lightRigNames.dark", OnChange: onChange, theme: theme);
                grb.LocalRect = new RectangleF(pos, displaySize);
                pos.X += displaySize.X + margin;

                grb = new GraphicRadioButton(fullScreenContentDialog, siblings, displaySize, textureName: @"Textures\IconReallyDarkRig", labelId: "lightRigNames.realdark", OnChange: onChange, theme: theme);
                grb.LocalRect = new RectangleF(pos, displaySize);
                pos.X += displaySize.X + margin;

                float width = 4 * displaySize.X + 3 * margin;
                lightRig = new GraphicRadioButtonSetLabelHelp(fullScreenContentDialog, Font, "Lighting", width, siblings, labelText: Strings.Localize("editWorldParams.lightRigPictureList"), OnChange: onChange, theme: theme);
                lightRig.Margin = new Padding(64);
                bottomSet.AddWidget(lightRig);
            }

            {
                // Linked Level.
                int width = 600;
                linkedLevel = new LinkedLevelLabelHelp(fullScreenContentDialog, Font, width, theme: theme);
                linkedLevel.Margin = new Padding(64);
                linkedLevel.Button.BackgroundColor = theme.FocusColor;  // Force a RoundedRect tile under this and also support focus outlining.
                bottomSet.AddWidget(linkedLevel);

                linkedLevel.Button.CornerRadius = theme.BaseCornerRadius;
                linkedLevel.Button.FocusOutlineWidth = 8.0f;
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
            column1.Position = new Vector2(TargetResolution.X / 2.0f - columnSize.X / 2.0f, 100);

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
            // Set initial sky state.
            foreach (GraphicRadioButton grb in sky.RadioButtons)
            {
                if ((int)grb.Data == Terrain.SkyIndex)
                {
                    grb.Selected = true;
                    // Also start by setting the focus on this button.
                    grb.SetFocus(overrideInactive: true);
                }
                else
                {
                    grb.Selected = false;
                }
            }

            // Set initial light rig state.
            string lightRigTextureName = @"Textures\Icon" + InGame.LightRig + "Rig"; 
            foreach (GraphicRadioButton grb in lightRig.RadioButtons)
            {
                if (grb.TextureName == lightRigTextureName)
                {
                    grb.Selected = true;
                }
                else
                {
                    grb.Selected = false;
                }
            }

            // Set initial linked world state.
            if (InGame.XmlWorldData.LinkedToLevel == null)
            {
                // Set defaults.
                linkedLevel.Title = null;
                linkedLevel.ThumbnailTexture = KoiLibrary.LoadTexture2D(@"Textures\GridElements\NoNextLevel");
            }
            else
            {
                // Get metadata from linked level.  Try from MyWorlds, first.
                string worldFilename = Path.Combine(BokuGame.Settings.MediaPath, BokuGame.MyWorldsPath + InGame.XmlWorldData.LinkedToLevel.ToString() + @".Xml");
                string thumbFilename = Path.Combine(BokuGame.Settings.MediaPath, BokuGame.MyWorldsPath + InGame.XmlWorldData.LinkedToLevel.ToString() + @".dds");
                XmlWorldData level = XmlWorldData.Load(worldFilename, XnaStorageHelper.Instance);

                // Not valid, try Downloads.
                // TODO (****) If user selects a Downloads world (or a built in one) shouldn't we clone it to MyWorlds?
                if(level == null)
                {
                    worldFilename = Path.Combine(BokuGame.Settings.MediaPath, BokuGame.DownloadsPath + InGame.XmlWorldData.LinkedToLevel.ToString() + @".Xml");
                    level = XmlWorldData.Load(worldFilename, XnaStorageHelper.Instance);
                    thumbFilename = Path.Combine(BokuGame.Settings.MediaPath, BokuGame.DownloadsPath + InGame.XmlWorldData.LinkedToLevel.ToString() + @".dds");
                }

                if (level != null)
                {
                    // Get thumbnail and title of target level.
                    linkedLevel.Title = level.name;
                    linkedLevel.ThumbnailTexture = Storage4.TextureLoad(thumbFilename);
                }
            }

            base.Activate(args);
        }   // end of Activate()

        #endregion

        #region Internal

        Texture2D CreateGradientSkyTexture(int index)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            RenderTarget2D rt = new RenderTarget2D(device, 64, 64);
            device.SetRenderTarget(rt);
            Boku.Fx.ScreenSpaceQuad quad = Boku.Fx.ScreenSpaceQuad.GetInstance();
            quad.RenderGradient(Boku.SimWorld.SkyBox.Gradient(index));
            device.SetRenderTarget(null);

            return rt;
        }   // end of CreateGradientSkyTexture()

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

    }   // end of class WorldSettingsPage2Scene
}   // end of namespace KoiX.Scenes
