
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
    /// <summary>
    /// Contains the settings for a single button state.
    /// </summary>
    public class ButtonTheme : ICloneable
    {
        #region Members

        public Color BodyColor;
        public float CornerRadius;

        public Color OutlineColor;
        public float OutlineWidth;

        public BevelStyle BevelStyle = BevelStyle.None;

        public ShadowStyle Shadow;
        public Vector2 ShadowOffset;
        public float ShadowSize;
        public float ShadowAlpha = 1.0f;    // Assumes black shadow.

        public Vector2 DefaultSize;

        public Color TextColor;
        public Color TextOutlineColor;
        public float TextOutlineWidth = 0;

        public string FontFamily;
        public System.Drawing.FontStyle FontStyle = System.Drawing.FontStyle.Regular;
        public float FontSize;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public ButtonTheme(ThemeSet theme)
        {
            // Set values which are the same across all states here.
        }   // end of c'tor

        /// <summary>
        /// Fills in all the Button settings in the given theme.
        /// </summary>
        /// <param name="theme"></param>
        public static void InitDefaultValues(ThemeSet theme)
        {
            theme.ButtonDisabled = new ButtonTheme(theme);
            theme.ButtonNormal = new ButtonTheme(theme);
            theme.ButtonNormalFocused = new ButtonTheme(theme);
            theme.ButtonNormalHover = new ButtonTheme(theme);
            theme.ButtonNormalFocusedHover = new ButtonTheme(theme);
            theme.ButtonSelected = new ButtonTheme(theme);
            theme.ButtonSelectedFocused = new ButtonTheme(theme);
            theme.ButtonSelectedHover = new ButtonTheme(theme);
            theme.ButtonSelectedFocusedHover = new ButtonTheme(theme);

            // Disabled.
            theme.ButtonDisabled.BodyColor = theme.DisabledColor;
            theme.ButtonDisabled.OutlineColor = theme.DisabledDarkColor;
            theme.ButtonDisabled.OutlineWidth = theme.BaseOutlineWidth;
            theme.ButtonDisabled.CornerRadius = theme.ButtonCornerRadius;

            theme.ButtonDisabled.TextColor = theme.DisabledDarkColor;
            theme.ButtonDisabled.TextOutlineColor = theme.DarkTextColor;
            theme.ButtonDisabled.TextOutlineWidth = 1.0f;
            theme.ButtonDisabled.FontFamily = theme.TextFontFamily;
            theme.ButtonDisabled.FontStyle = theme.TextBaseFontStyle;
            theme.ButtonDisabled.FontSize = theme.TextBaseFontSize;

            theme.ButtonDisabled.Shadow = ShadowStyle.None;
            theme.ButtonDisabled.ShadowOffset = Vector2.Zero;
            theme.ButtonDisabled.ShadowSize = 0;
            
            theme.ButtonDisabled.DefaultSize = new Vector2(18.0f * theme.ButtonCornerRadius, 6.0f * theme.ButtonCornerRadius);
                
            // Normal.
            theme.ButtonNormal.BodyColor = theme.BaseColorPlus10;
            theme.ButtonNormal.OutlineColor = theme.AccentColor;
            theme.ButtonNormal.OutlineWidth = theme.BaseOutlineWidth;
            theme.ButtonNormal.CornerRadius = theme.ButtonCornerRadius;

            theme.ButtonNormal.TextColor = theme.LightTextColor;
            theme.ButtonNormal.TextOutlineColor = theme.DarkTextColor;
            theme.ButtonNormal.TextOutlineWidth = 1.0f;
            theme.ButtonNormal.FontFamily = theme.TextFontFamily;
            theme.ButtonNormal.FontStyle = theme.TextBaseFontStyle;
            theme.ButtonNormal.FontSize = theme.TextBaseFontSize;

            theme.ButtonNormal.Shadow = ShadowStyle.None;
            theme.ButtonNormal.ShadowOffset = Vector2.Zero;
            theme.ButtonNormal.ShadowSize = 0;
            theme.ButtonNormal.ShadowAlpha = 1.0f;

            theme.ButtonNormal.DefaultSize = new Vector2(18.0f * theme.ButtonCornerRadius, 6.0f * theme.ButtonCornerRadius);

            // NormalFocused.
            theme.ButtonNormalFocused = theme.ButtonNormal.Clone() as ButtonTheme;
            theme.ButtonNormalFocused.OutlineColor = theme.FocusColor;
            theme.ButtonNormalFocused.OutlineWidth = theme.BaseOutlineWidth + 1.0f;

            // NormalHover
            theme.ButtonNormalHover = theme.ButtonNormal.Clone() as ButtonTheme;
            theme.ButtonNormalHover.TextColor = theme.FocusColor;

            // NormalFocusedHover
            theme.ButtonNormalFocusedHover = theme.ButtonNormalFocused.Clone() as ButtonTheme;
            theme.ButtonNormalFocusedHover.TextColor = theme.FocusColor;

            // Selected.
            theme.ButtonSelected = theme.ButtonNormal.Clone() as ButtonTheme;
            theme.ButtonSelected.BodyColor = theme.BaseColorMinus10;
            // Subtle inner shadow for Selected.
            theme.ButtonSelected.Shadow = ShadowStyle.Inner;
            theme.ButtonSelected.ShadowOffset = new Vector2(2, 3);
            theme.ButtonSelected.ShadowSize = 4.0f;
            theme.ButtonSelected.ShadowAlpha = 0.5f;

            // SelectedFocused.
            theme.ButtonSelectedFocused = theme.ButtonSelected.Clone() as ButtonTheme;
            theme.ButtonSelectedFocused.OutlineColor = theme.FocusColor;
            theme.ButtonSelectedFocused.OutlineWidth = theme.BaseOutlineWidth + 1.0f;

            // SelectedHover.
            theme.ButtonSelectedHover = theme.ButtonSelected.Clone() as ButtonTheme;
            theme.ButtonSelectedHover.TextColor = theme.FocusColor;

            // SelectedFocusedHover.
            theme.ButtonSelectedFocusedHover = theme.ButtonSelectedFocused.Clone() as ButtonTheme;
            theme.ButtonSelectedFocusedHover.TextColor = theme.FocusColor;

        }   // end of InitDefaults()

        public object Clone()
        {
            ButtonTheme clone = MemberwiseClone() as ButtonTheme;

            return clone;
        }   // end of Clone()

        #endregion

        #region Internal
        #endregion

    }   // end of class ButtonTheme
}   // end of namespace KoiX.UI
