
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
    public class TextEditBoxTheme : ICloneable
    {
        #region Members

        public Color BodyColor;

        public Color OutlineColor;
        public float OutlineWidth;

        public float CornerRadius;

        public Color TextColor;

        public ShadowStyle Shadow;
        public Vector2 ShadowOffset;
        public float ShadowSize;
        public float ShadowAlpha;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public TextEditBoxTheme(ThemeSet theme)
        {
            // Set values which are the same across all states here.
            CornerRadius = theme.ButtonCornerRadius;
            TextColor = theme.DarkTextColor;

            Shadow = ShadowStyle.Inner;
            ShadowOffset = new Vector2(3, 3);
            ShadowSize = 9.0f;
            ShadowAlpha = 0.8f;

        }   // end of c'tor

        public static void InitDefaultValues(ThemeSet theme)
        {
            theme.TextEditBoxDisabled = new TextEditBoxTheme(theme);
            theme.TextEditBoxNormal = new TextEditBoxTheme(theme);
            theme.TextEditBoxNormalFocused = new TextEditBoxTheme(theme);

            // Disabled.
            theme.TextEditBoxDisabled.BodyColor = theme.DisabledColor;
            theme.TextEditBoxDisabled.OutlineColor = theme.DisabledLightColor;
            theme.TextEditBoxDisabled.OutlineWidth = theme.BaseOutlineWidth;
            theme.TextEditBoxDisabled.TextColor = theme.DisabledDarkColor;

            // Normal.
            theme.TextEditBoxNormal.BodyColor = theme.LightTextColor;
            theme.TextEditBoxNormal.OutlineColor = theme.DarkTextColor;
            theme.TextEditBoxNormal.OutlineWidth = theme.BaseOutlineWidth;

            // NormalFocused.
            theme.TextEditBoxNormalFocused.BodyColor = theme.LightTextColor;
            theme.TextEditBoxNormalFocused.OutlineColor = theme.FocusColor;
            theme.TextEditBoxNormalFocused.OutlineWidth = theme.BaseOutlineWidth;

        }   // end of InitDefaultValues()

        public object Clone()
        {
            TextEditBoxTheme clone = MemberwiseClone() as TextEditBoxTheme;

            return clone;
        }   // end of Clone()

        #endregion

        #region Internal
        #endregion


    }
}
