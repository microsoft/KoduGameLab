// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;
using KoiX.Geometry;
using KoiX.Text;

namespace KoiX.UI
{
    using FontStyle = System.Drawing.FontStyle;

    /// <summary>
    /// Colors, sizes, styles etc for UI elements.
    /// 
    /// This class is basically just a public wrapper around the current ThemeSet.
    /// The ThemeSet has the actual values for the current theme.
    /// </summary>
    public class Theme
    {
        static ThemeSet currentThemeSet = null;

        public static float TwitchTime = 0.2f;          // Time used by most things.
        public static float QuickTwitchTime = 0.05f;    // Time used for smaller changes.
        public static float GoldenRatio = 1.618f;

        #region Members
        #endregion

        #region Accessors

        public static ThemeSet CurrentThemeSet
        {
            get { return currentThemeSet; }
        }

        #endregion

        #region Public

        public static void Init()
        {
            currentThemeSet = new ThemeSet();
        }   // end of Init()

        #endregion

        /*
        //
        // Base UI colors from Irina's design exploration.
        //
        static Color UIBlue = new Color(20, 150, 200);
        static Color UIGreen = new Color(128, 221, 6);

        // Slight color varients used to indicate pressed or pressable state.
        // +- 10% brightness in HSB space.
        public static Color UIBlueLight = new Color(22, 167, 224);
        public static Color UIBlueDark = new Color(17, 129, 173);
        public static Color UIGreenLight = new Color(143, 247, 7);
        public static Color UIGreenDark = new Color(114, 97, 77);

        public static Color UIBase = UIBlue;
        public static Color UISelected = UIGreen;

        public static Color TextBlack = new Color(27, 29, 28);
        public static Color TextWhite = new Color(250, 250, 250);

        public static string TextFontFamily = "Calibri";
        public static float TextBaseFontSize = 18.0f;

        public static float TextOutlineWidthHelpOverlay = 4.0f; // Outline width for HeloOverlay text.
        public static float TextOutlineWidthTiles = 8.0f;       // Outline width used on tiles.

        public static Color BaseColor = UIBlue;
        public static Color BaseColorLight = UIBlueLight;
        public static Color BaseColorDark = UIBlueDark;

        public static Color AccentColor = new Color(255, 153, 0);

        public static Color OutlineColor = new Color(20, 20, 20);
        public static Color OutlineColorFocused = UIGreenLight;

        // Dialogs and Widgets
        public static ShadowStyle DialogShadowStyle = ShadowStyle.Outer;
        public static float DialogBodyRadius = 32.0f;
        public static Color DialogColorFocused = BaseColor;
        public static Color DialogColorNotFocused = BaseColorDark;
        public static SystemFont DialogLabelFont = SysFont.GetSystemFont(TextFontFamily, 24.0f, FontStyle.Regular);
        public static SystemFont DialogTextBoxFont = SysFont.GetSystemFont(TextFontFamily, 32.0f, FontStyle.Regular);

        // Labels
        public static Color LabelColor = new Color(240, 250, 220);

        // Buttons
        public static float ButtonFontSize = 16.0f;
        public static SystemFont ButtonFont = SysFont.GetSystemFont(TextFontFamily, 16.0f, FontStyle.Regular);

        public static ShadowStyle ButtonShadow = ShadowStyle.None;
        public static ShadowStyle ButtonShadowDisabled = ShadowStyle.None;
        public static ShadowStyle ButtonShadowFocused = ShadowStyle.None;
        public static ShadowStyle ButtonShadowSelected = ShadowStyle.Inner;

        public static float ButtonRadius = 16.0f;
        public static float ButtonRadiusHover = 16.0f;
        public static float ButtonRadiusDisabled = 16.0f;
        public static float ButtonRadiusFocused = 16.0f;
        public static float ButtonRadiusSelected = 16.0f;

        public static Color ButtonBodyColor = BaseColorLight;
        public static Color ButtonBodyColorHover = BaseColorLight;
        public static Color ButtonBodyColorDisabled = BaseColorLight;
        public static Color ButtonBodyColorFocused = BaseColorLight;
        public static Color ButtonBodyColorSelected = BaseColor;

        public static Color ButtonOutlineColor = OutlineColor;
        public static Color ButtonOutlineColorHover = OutlineColorFocused;
        public static Color ButtonOutlineColorDisabled = OutlineColor;
        public static Color ButtonOutlineColorFocused = OutlineColorFocused;
        public static Color ButtonOutlineColorSelected = OutlineColor;

        // These work best if they are whole numbers.
        public static float ButtonOutlineWidth = 2.0f;
        public static float ButtonOutlineWidthHover = 2.0f;
        public static float ButtonOutlineWidthDisabled = 2.0f;
        public static float ButtonOutlineWidthFocused = 2.0f;
        public static float ButtonOutlineWidthSelected = 3.0f;

        public static Color ButtonLabelColor = LabelColor;
        public static Color ButtonLabelColorHover = OutlineColorFocused;
        public static Color ButtonLabelColorDisabled = TextColorDisabled;
        public static Color ButtonLabelColorFocused = LabelColor;
        public static Color ButtonLabelColorSelected = LabelColor;


        // Text box
        public static float TextBoxRadius = 16.0f;
        public static int SingleLineTextEditBoxWidth = 300;
        public static Color TextColor = OutlineColor;   // new Color(66, 66, 66);
        public static Color TextColorDisabled = new Color(112, 114, 104);
        public static Color TextBoxColor = new Color(233, 233, 233);
        public static ShadowStyle TextBoxShadow = ShadowStyle.Inner;
        public static Color TextBoxOutlineColor = new Color(66, 66, 66);
        public static float TextBoxOutlineWidth = 0.0f;
        public static Color TextBoxOutlineColorFocused = OutlineColorFocused;
        public static float TextBoxOutlineWidthFocused = 1.5f;
        */



    }   // end of class Theme

}   // end of namespace KoiX.UI

