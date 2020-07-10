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
using Boku.Common;
using Boku.Common.Localization;
using Boku.Common.Xml;

namespace KoiX.Scenes
{
    /// <summary>
    /// Page of options.  Formerly DNA menu.
    /// </summary>
    public class OptionsPage2Scene : BasePageScene
    {
        #region Members

        Texture2D bkgTexture;

        SystemFont titleFont;
        SystemFont font;

        Label title;

        WidgetSet column1;
        WidgetSet column2;
        WidgetSet column3;
        int numActiveColumns = 1;
        Vector2 columnSize = new Vector2(500, 850);
        float columnGutter = 25.0f;     // Gap between columns.
        int maxEntriesPerColumn = 15;   // 15 is based on what fits decently on the screen.  If we get enough
                                        // languages that 3 columns no longer works (>45 languages) then we 
                                        // can increase this to 16 by removing the version number display.
                                        // Beyond, shrinking the margin around the widgets or even shrinking
                                        // the widgets themselves.  Not too worried about this...  640k should
                                        // be enough for anyone.

        // List containing all the buttons for the set.  Note we don't need
        // to explicitly add the radioButtons to this list.  They add themselves
        // in their c'tors.
        List<RadioButton> buttonSet;
        List<RadioButtonLabelHelp> widgetList;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public OptionsPage2Scene(string nextLabelId = null, string nextLabelText = null, string prevLabelId = null, string prevLabelText = null)
            : base("OptionsPage2Scene", nextLabelId, nextLabelText, prevLabelId, prevLabelText)
        {
            ThemeSet theme = Theme.CurrentThemeSet;

            titleFont = new SystemFont(theme.TextFontFamily, theme.TextTitleFontSize * 2.0f, System.Drawing.FontStyle.Bold);
            font = new SystemFont(theme.TextFontFamily, theme.TextBaseFontSize * 1.2f, System.Drawing.FontStyle.Regular);

            FontWrapper wrapper = new FontWrapper(null, font);
            GetFont Font = delegate() { return wrapper; };

            title = new Label(fullScreenContentDialog, titleFont, theme.LightTextColor, outlineColor: theme.DarkTextColor, outlineWidth: 2.5f, labelId: "optionsParams.Language");
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

            column1 = new WidgetSet(fullScreenContentDialog, new RectangleF(new Vector2(150, 100), new Vector2(550, 850)), Orientation.Vertical, horizontalJustification: Justification.Left, verticalJustification: Justification.Top);
            column2 = new WidgetSet(fullScreenContentDialog, new RectangleF(new Vector2(900, 100), new Vector2(550, 850)), Orientation.Vertical, horizontalJustification: Justification.Left, verticalJustification: Justification.Top);
            column3 = new WidgetSet(fullScreenContentDialog, new RectangleF(new Vector2(1900, 100), new Vector2(550, 850)), Orientation.Vertical, horizontalJustification: Justification.Left, verticalJustification: Justification.Top);
            fullScreenContentDialog.AddWidget(column1);
            fullScreenContentDialog.AddWidget(column2);
            fullScreenContentDialog.AddWidget(column3);

            float width = columnSize.X;

            buttonSet = new List<RadioButton>();
            widgetList = new List<RadioButtonLabelHelp>();

            // Start by gettting and sorting the full list of supported languages.
            IEnumerable<LocalizationResourceManager.SupportedLanguage> langs = LocalizationResourceManager.SupportedLanguages;

            // Copy to a List so we can sort.
            List<LocalizationResourceManager.SupportedLanguage> languageList = new List<LocalizationResourceManager.SupportedLanguage>();
            foreach (LocalizationResourceManager.SupportedLanguage lang in langs)
            {
                languageList.Add(lang);
            }
            languageList.Sort(LanguageSortComp);

            // Create a radioButton for each language.
            foreach (LocalizationResourceManager.SupportedLanguage lang in languageList)
            {
                string labelText = "";

                if (lang.NameInEnglish.Equals("hebrew", StringComparison.InvariantCultureIgnoreCase))
                {
                    // RtoL code seems to have trouble with NSM characters 0x05b0 and 0x05b4.
                    // Strip them out.
                    string native = "";
                    char[] a = lang.NameInNative.ToCharArray();
                    foreach (char c in a)
                    {
                        if (c != 0x05b0 && c != 0x05b4)
                        {
                            native += c;
                        }
                    }

                    labelText = lang.NameInEnglish + " : " + native;
                }
                else
                {
                    labelText = lang.NameInEnglish + " : " + lang.NameInNative;
                }

                // Create the RadioButton triple. Note that we pass in the lang as the data blob allowing us to associate this RadioButton with the language.
                RadioButtonLabelHelp rblh = new RadioButtonLabelHelp(fullScreenContentDialog, Font, null, width, siblings: buttonSet, labelText: labelText, OnChange: OnChange, theme: theme, data: lang);
                widgetList.Add(rblh);
            }   // end of loop over languages.

            // Split the language list into 1, 2, or 3 columns depending on the count.  If we need more
            // than this then we need to re-think the layout.
            // Decide how many columns we need.
            numActiveColumns = (int)Math.Ceiling(widgetList.Count / (float)maxEntriesPerColumn);
            switch (numActiveColumns)
            {
                case 1:
                    {
                        for (int i = 0; i < widgetList.Count; i++)
                        {
                            column1.AddWidget(widgetList[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        // Figure out how many go into the first column.
                        // The +1 forces rounding up.
                        int half = (int)((widgetList.Count + 1) / 2.0f);
                        for (int i = 0; i < half; i++)
                        {
                            column1.AddWidget(widgetList[i]);
                        }
                        for (int i = half; i < widgetList.Count; i++)
                        {
                            column2.AddWidget(widgetList[i]);
                        }
                    }
                    break;

                case 3:
                    {
                        // Figure out how many go into the first column.
                        // The +2 forces rounding up.
                        int third = (int)((widgetList.Count + 2) / 3.0f);
                        for (int i = 0; i < third; i++)
                        {
                            column1.AddWidget(widgetList[i]);
                        }
                        int half = (int)((widgetList.Count - third + 1) / 2.0f);
                        for (int i = third; i < third + half; i++)
                        {
                            column2.AddWidget(widgetList[i]);
                        }
                        for (int i = third + half; i < widgetList.Count; i++)
                        {
                            column3.AddWidget(widgetList[i]);
                        }
                    }
                    break;

                default:
                    Debug.Assert(false, "Hmm, didn't plan for this case, did you?");
                    break;
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

        void OnChange(BaseWidget w) 
        { 
            RadioButton rb = w as RadioButton; 
            if (rb != null) 
            {
                rb.SetFocus();  // Move focus to this widget.  Not needed for mouse but helps touch.

                LocalizationResourceManager.SupportedLanguage lang = rb.Data as LocalizationResourceManager.SupportedLanguage;

                Debug.Assert(lang != null);

                if(lang != null)
                {
                    XmlOptionsData.Language = lang.Language;

                    // Trigger a language reload if fast enough, else only do when leaving page.
                }

            } 
        }

        public override void Update()
        {
            Vector2 screenSize = BokuGame.ScreenSize / camera.Zoom;

            switch (numActiveColumns)
            {
                case 1:
                    column1.Position = new Vector2(screenSize.X / 2.0f - columnSize.X / 2.0f, 100);
                    break;

                case 2:
                    column1.Position = new Vector2(screenSize.X / 2.0f - columnSize.X - columnGutter / 2.0f, 100);
                    column2.Position = new Vector2(screenSize.X / 2.0f + columnGutter / 2.0f, 100);
                    break;

                case 3:
                    column1.Position = new Vector2(screenSize.X / 2.0f - columnSize.X * 1.5f - columnGutter * 2.0f, 100);
                    column2.Position = new Vector2(screenSize.X / 2.0f - columnSize.X / 2.0f, 100);
                    column3.Position = new Vector2(screenSize.X / 2.0f + columnSize.X / 2.0f + columnGutter, 100);
                    break;

                default:
                    Debug.Assert(false, "Hmm, didn't plan for this case, did you?");
                    break;
            }

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
            Debug.Assert(!Active, "Why are we activating something that's already active?");

            base.Activate(args);

            // Find currently langauge and set matching radio button.
            // Note that we do this after base activation.  This way the radio buttons
            // for the language chocie are already active before we select and set focus.
            string curLang = XmlOptionsData.Language;
            foreach (RadioButtonLabelHelp rb in widgetList)
            {
                LocalizationResourceManager.SupportedLanguage lang = rb.Data as LocalizationResourceManager.SupportedLanguage;
                Debug.Assert(lang != null);
                if (curLang.Equals(lang.Language, StringComparison.InvariantCultureIgnoreCase))
                {
                    rb.Selected = true;
                    rb.SetFocus();
                    break;
                }
            }

        }   // end of Activate()

        #endregion

        #region Internal

        /// <summary>
        /// Comparison used when sorting languages.
        /// Note this assumes we never get a null input.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private int LanguageSortComp(LocalizationResourceManager.SupportedLanguage a, LocalizationResourceManager.SupportedLanguage b)
        {
            return a.NameInEnglish.CompareTo(b.NameInEnglish);
        }

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

    }   // end of class OptionsPage2Scene
}   // end of namespace KoiX.Scenes
