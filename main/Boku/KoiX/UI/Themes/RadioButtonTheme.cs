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

namespace KoiX.UI
{
    public class RadioButtonTheme : ICloneable
    {
        #region Members

        public Color BodyColor;

        public Color OutlineColor;
        public float OutlineWidth;

        public float CornerRadius;

        public Vector2 DefaultSize = new Vector2(32, 32);

        #endregion

        #region Accessors
        #endregion

        #region Public

        public RadioButtonTheme(ThemeSet theme)
        {
            // Set values which are the same across all states here.

            // FOrce radius to half size of button to ensure it's round.
            CornerRadius = DefaultSize.X / 2.0f;

        }   // end of c'tor

        public static void InitDefaultValues(ThemeSet theme)
        {
            theme.RadioButtonDisabled = new RadioButtonTheme(theme);
            theme.RadioButtonNormal = new RadioButtonTheme(theme);
            theme.RadioButtonNormalFocused = new RadioButtonTheme(theme);
            theme.RadioButtonSelected = new RadioButtonTheme(theme);
            theme.RadioButtonSelectedFocused = new RadioButtonTheme(theme);

            // Disabled.
            theme.RadioButtonDisabled.BodyColor = theme.DisabledColor;
            theme.RadioButtonDisabled.OutlineColor = theme.DisabledLightColor;
            theme.RadioButtonDisabled.OutlineWidth = theme.BaseOutlineWidth;

            // Normal.
            theme.RadioButtonNormal.BodyColor = theme.DarkTextColor;
            theme.RadioButtonNormal.OutlineColor = theme.LightTextColor;
            theme.RadioButtonNormal.OutlineWidth = theme.BaseOutlineWidth;

            // NormalFocused.
            theme.RadioButtonNormalFocused.BodyColor = theme.DarkTextColor;
            theme.RadioButtonNormalFocused.OutlineColor = theme.LightTextColor;    // Assumes surround is FocusColor.
            theme.RadioButtonNormalFocused.OutlineWidth = theme.BaseOutlineWidth;

            // Selected.
            theme.RadioButtonSelected.BodyColor = theme.FocusColor;
            theme.RadioButtonSelected.OutlineColor = theme.LightTextColor;
            theme.RadioButtonSelected.OutlineWidth = theme.BaseOutlineWidth;

            // SelectedFocused.
            theme.RadioButtonSelectedFocused.BodyColor = theme.FocusColor;
            theme.RadioButtonSelectedFocused.OutlineColor = theme.LightTextColor;
            theme.RadioButtonSelectedFocused.OutlineWidth = theme.BaseOutlineWidth;

        }   // end of InitDefaultValues()

        public object Clone()
        {
            RadioButtonTheme clone = MemberwiseClone() as RadioButtonTheme;

            return clone;
        }   // end of Clone()

        #endregion

        #region Internal
        #endregion

    }   // end of class RadioButtonTheme

}   // end of namespace KoiX.UI
