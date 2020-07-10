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
    public class CheckBoxTheme : ICloneable
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

        public CheckBoxTheme(ThemeSet theme)
        {
            // Set values which are the same across all states here.
            CornerRadius = theme.ButtonCornerRadius;

        }   // end of c'tor

        public static void InitDefaultValues(ThemeSet theme)
        {
            theme.CheckBoxDisabled = new CheckBoxTheme(theme);
            theme.CheckBoxNormal = new CheckBoxTheme(theme);
            theme.CheckBoxNormalFocused = new CheckBoxTheme(theme);
            theme.CheckBoxSelected = new CheckBoxTheme(theme);
            theme.CheckBoxSelectedFocused = new CheckBoxTheme(theme);

            // Disabled.
            theme.CheckBoxDisabled.BodyColor = theme.DisabledColor;
            theme.CheckBoxDisabled.OutlineColor = theme.DisabledLightColor;
            theme.CheckBoxDisabled.OutlineWidth = theme.BaseOutlineWidth;

            // Normal.
            theme.CheckBoxNormal.BodyColor = theme.DarkTextColor;
            theme.CheckBoxNormal.OutlineColor = theme.LightTextColor;
            theme.CheckBoxNormal.OutlineWidth = theme.BaseOutlineWidth;

            // NormalFocused.
            theme.CheckBoxNormalFocused.BodyColor = theme.DarkTextColor;
            theme.CheckBoxNormalFocused.OutlineColor = theme.FocusColor;
            theme.CheckBoxNormalFocused.OutlineWidth = theme.BaseOutlineWidth * 1.5f;

            // Selected.
            theme.CheckBoxSelected.BodyColor = theme.FocusColor;
            theme.CheckBoxSelected.OutlineColor = theme.LightTextColor;
            theme.CheckBoxSelected.OutlineWidth = theme.BaseOutlineWidth;

            // SelectedFocused.
            theme.CheckBoxSelectedFocused.BodyColor = theme.FocusColor;
            theme.CheckBoxSelectedFocused.OutlineColor = theme.FocusColorMinus10;
            theme.CheckBoxSelectedFocused.OutlineWidth = theme.BaseOutlineWidth * 1.5f;

        }   // end of InitDefaultValues()

        public object Clone()
        {
            CheckBoxTheme clone = MemberwiseClone() as CheckBoxTheme;

            return clone;
        }   // end of Clone()

        #endregion

        #region Internal
        #endregion

    }   // end of class CheckBoxTheme

}   // end of namespace KoiX.UI
