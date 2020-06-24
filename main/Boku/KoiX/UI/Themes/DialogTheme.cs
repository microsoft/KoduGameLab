
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
    public class DialogTheme : ICloneable
    {
        #region Members

        public Color TileColor;
        public float CornerRadius;
        public Padding Padding;

        public Color OutlineColor;
        public float OutlineWidth;

        public BevelStyle BevelStyle;
        public float BevelWidth;

        public ShadowStyle ShadowStyle;
        public Vector2 ShadowOffset;
        public float ShadowSize;
        public float ShadowAlpha = 1.0f;    // Assumes black shadow.

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

        public DialogTheme(ThemeSet theme)
        {
            // Set values which are the same across all states here.
        }   // end of c'tor

        /// <summary>
        /// Fills in all the Dialog settings in the given theme.
        /// </summary>
        /// <param name="theme"></param>
        public static void InitDefaultValues(ThemeSet theme)
        {
            theme.DialogBodyTileDisabled = new DialogTheme(theme);
            theme.DialogBodyTileNormal = new DialogTheme(theme);
            theme.DialogBodyTileFocused = new DialogTheme(theme);
            theme.DialogTitleTileDisabled = new DialogTheme(theme);
            theme.DialogTitleTileNormal = new DialogTheme(theme);
            theme.DialogTitleTileFocused = new DialogTheme(theme);

            // Disabled, body.
            theme.DialogBodyTileDisabled.TileColor = theme.DisabledColor;
            theme.DialogBodyTileDisabled.CornerRadius = theme.BaseCornerRadius;
            theme.DialogBodyTileDisabled.Padding = theme.BasePadding;

            theme.DialogBodyTileDisabled.OutlineColor = theme.DisabledDarkColor;
            theme.DialogBodyTileDisabled.OutlineWidth = theme.BaseOutlineWidth;

            theme.DialogBodyTileDisabled.BevelStyle = theme.BaseBevelStyle;
            theme.DialogBodyTileDisabled.BevelWidth = theme.BaseBevelWidth;

            theme.DialogBodyTileDisabled.ShadowStyle = ShadowStyle.Outer;
            theme.DialogBodyTileDisabled.ShadowOffset = theme.BaseShadowOffset;
            theme.DialogBodyTileDisabled.ShadowSize = theme.BaseShadowSize;
            theme.DialogBodyTileDisabled.ShadowAlpha = theme.BaseShadowAlpha;

            theme.DialogBodyTileDisabled.TextColor = theme.LightTextColor;
            theme.DialogBodyTileDisabled.TextOutlineColor = theme.DarkTextColor;
            theme.DialogBodyTileDisabled.TextOutlineWidth = 1.0f;
            theme.DialogBodyTileDisabled.FontFamily = theme.TextFontFamily;
            theme.DialogBodyTileDisabled.FontStyle = theme.TextBaseFontStyle;
            theme.DialogBodyTileDisabled.FontSize = theme.TextBaseFontSize;

            // Disabled, title.
            theme.DialogTitleTileDisabled.TileColor = theme.DisabledColor;
            theme.DialogTitleTileDisabled.CornerRadius = theme.BaseCornerRadius;
            theme.DialogTitleTileDisabled.Padding = theme.BasePadding;

            theme.DialogTitleTileDisabled.OutlineColor = theme.DisabledDarkColor;
            theme.DialogTitleTileDisabled.OutlineWidth = theme.BaseOutlineWidth;

            theme.DialogTitleTileDisabled.BevelStyle = theme.BaseBevelStyle;
            theme.DialogTitleTileDisabled.BevelWidth = theme.BaseBevelWidth;

            theme.DialogTitleTileDisabled.ShadowStyle = ShadowStyle.Outer;
            theme.DialogTitleTileDisabled.ShadowOffset = theme.BaseShadowOffset;
            theme.DialogTitleTileDisabled.ShadowSize = theme.BaseShadowSize;
            theme.DialogTitleTileDisabled.ShadowAlpha = theme.BaseShadowAlpha;

            theme.DialogTitleTileDisabled.TextColor = theme.LightTextColor;
            theme.DialogTitleTileDisabled.TextOutlineColor = theme.DarkTextColor;
            theme.DialogTitleTileDisabled.TextOutlineWidth = 1.0f;
            theme.DialogTitleTileDisabled.FontFamily = theme.TextFontFamily;
            theme.DialogTitleTileDisabled.FontStyle = theme.TextBaseFontStyle;
            theme.DialogTitleTileDisabled.FontSize = theme.TextBaseFontSize;

            // Normal, body.
            theme.DialogBodyTileNormal.TileColor = theme.BaseColor;
            theme.DialogBodyTileNormal.CornerRadius = theme.BaseCornerRadius;
            theme.DialogBodyTileNormal.Padding = theme.BasePadding;

            theme.DialogBodyTileNormal.OutlineColor = theme.AccentColor;
            theme.DialogBodyTileNormal.OutlineWidth = theme.BaseOutlineWidth;

            theme.DialogBodyTileNormal.BevelStyle = theme.BaseBevelStyle;
            theme.DialogBodyTileNormal.BevelWidth = theme.BaseBevelWidth;

            theme.DialogBodyTileNormal.ShadowStyle = ShadowStyle.Outer;
            theme.DialogBodyTileNormal.ShadowOffset = theme.BaseShadowOffset;
            theme.DialogBodyTileNormal.ShadowSize = theme.BaseShadowSize;
            theme.DialogBodyTileNormal.ShadowAlpha = theme.BaseShadowAlpha;

            theme.DialogBodyTileNormal.TextColor = theme.LightTextColor;
            theme.DialogBodyTileNormal.TextOutlineColor = theme.DarkTextColor;
            theme.DialogBodyTileNormal.TextOutlineWidth = 1.0f;
            theme.DialogBodyTileNormal.FontFamily = theme.TextFontFamily;
            theme.DialogBodyTileNormal.FontStyle = theme.TextBaseFontStyle;
            theme.DialogBodyTileNormal.FontSize = theme.TextBaseFontSize;

            // Normal, title.
            theme.DialogTitleTileNormal.TileColor = theme.DarkTextColor;
            theme.DialogTitleTileNormal.CornerRadius = theme.BaseCornerRadius;
            theme.DialogTitleTileNormal.Padding = theme.BasePadding;

            theme.DialogTitleTileNormal.OutlineColor = theme.AccentColor;
            theme.DialogTitleTileNormal.OutlineWidth = theme.BaseOutlineWidth;

            theme.DialogTitleTileNormal.BevelStyle = theme.BaseBevelStyle;
            theme.DialogTitleTileNormal.BevelWidth = theme.BaseBevelWidth;

            theme.DialogTitleTileNormal.ShadowStyle = ShadowStyle.Outer;
            theme.DialogTitleTileNormal.ShadowOffset = theme.BaseShadowOffset;
            theme.DialogTitleTileNormal.ShadowSize = theme.BaseShadowSize;
            theme.DialogTitleTileNormal.ShadowAlpha = theme.BaseShadowAlpha;

            theme.DialogTitleTileNormal.TextColor = theme.LightTextColor;
            theme.DialogTitleTileNormal.TextOutlineColor = theme.DarkTextColor;
            theme.DialogTitleTileNormal.TextOutlineWidth = 0.0f;
            theme.DialogTitleTileNormal.FontFamily = theme.TextFontFamily;
            theme.DialogTitleTileNormal.FontStyle = theme.TextBaseFontStyle;
            theme.DialogTitleTileNormal.FontSize = theme.TextTitleFontSize;

            // Focused, body.
            theme.DialogBodyTileFocused.TileColor = theme.BaseColor;
            theme.DialogBodyTileFocused.CornerRadius = theme.BaseCornerRadius;
            theme.DialogBodyTileFocused.Padding = theme.BasePadding;

            theme.DialogBodyTileFocused.OutlineColor = theme.AccentColor;
            theme.DialogBodyTileFocused.OutlineWidth = theme.BaseOutlineWidth;

            theme.DialogBodyTileFocused.BevelStyle = theme.BaseBevelStyle;
            theme.DialogBodyTileFocused.BevelWidth = theme.BaseBevelWidth;

            theme.DialogBodyTileFocused.ShadowStyle = ShadowStyle.Outer;
            theme.DialogBodyTileFocused.ShadowOffset = theme.BaseShadowOffset;
            theme.DialogBodyTileFocused.ShadowSize = theme.BaseShadowSize;
            theme.DialogBodyTileFocused.ShadowAlpha = theme.BaseShadowAlpha;

            theme.DialogBodyTileFocused.TextColor = theme.LightTextColor;
            theme.DialogBodyTileFocused.TextOutlineColor = theme.DarkTextColor;
            theme.DialogBodyTileFocused.TextOutlineWidth = 1.0f;
            theme.DialogBodyTileFocused.FontFamily = theme.TextFontFamily;
            theme.DialogBodyTileFocused.FontStyle = theme.TextBaseFontStyle;
            theme.DialogBodyTileFocused.FontSize = theme.TextBaseFontSize;

            // Focused, title.
            theme.DialogTitleTileFocused.TileColor = theme.DarkTextColor;
            theme.DialogTitleTileFocused.CornerRadius = theme.BaseCornerRadius;
            theme.DialogTitleTileFocused.Padding = theme.BasePadding;

            theme.DialogTitleTileFocused.OutlineColor = theme.AccentColor;
            theme.DialogTitleTileFocused.OutlineWidth = theme.BaseOutlineWidth;

            theme.DialogTitleTileFocused.BevelStyle = theme.BaseBevelStyle;
            theme.DialogTitleTileFocused.BevelWidth = theme.BaseBevelWidth;

            theme.DialogTitleTileFocused.ShadowStyle = ShadowStyle.Outer;
            theme.DialogTitleTileFocused.ShadowOffset = theme.BaseShadowOffset;
            theme.DialogTitleTileFocused.ShadowSize = theme.BaseShadowSize;
            theme.DialogTitleTileFocused.ShadowAlpha = theme.BaseShadowAlpha;

            theme.DialogTitleTileFocused.TextColor = theme.LightTextColor;
            theme.DialogTitleTileFocused.TextOutlineColor = theme.DarkTextColor;
            theme.DialogTitleTileFocused.TextOutlineWidth = 0.0f;
            theme.DialogTitleTileFocused.FontFamily = theme.TextFontFamily;
            theme.DialogTitleTileFocused.FontStyle = theme.TextBaseFontStyle;
            theme.DialogTitleTileFocused.FontSize = theme.TextTitleFontSize;

        }   // end of InitDefaultValues()

        public object Clone()
        {
            DialogTheme clone = MemberwiseClone() as DialogTheme;

            return clone;
        }   // end of Clone()

        #endregion

        #region Internal
        #endregion

    }   // end of class DialogTheme

}   // end of namespace KoiX.UI
