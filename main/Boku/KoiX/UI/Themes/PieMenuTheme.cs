
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
    public class PieMenuTheme : ICloneable
    {
        #region Members

        public float MinRadius;     // Overall pie menu size.
        public float MaxRadius;

        public Color BodyColor;

        public Color OutlineColor;
        public float OutlineWidth;

        public Color TextColor;
        public Color TextOutlineColor;

        public BevelStyle BevelStyle;
        public float BevelWidth;

        public ShadowStyle Shadow;
        public Vector2 ShadowOffset;
        public float ShadowSize;
        public float ShadowAlpha;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public PieMenuTheme(ThemeSet theme)
        {
            // Set values which are the same across all states here.
            TextColor = theme.DarkTextColor;
            TextOutlineColor = theme.LightTextColor;

            theme.DialogBodyTileDisabled.BevelStyle = theme.BaseBevelStyle;
            theme.DialogBodyTileDisabled.BevelWidth = theme.BaseBevelWidth;

            Shadow = ShadowStyle.Outer;
            ShadowOffset = new Vector2(6, 6);
            ShadowSize = 10.0f;
            ShadowAlpha = 0.8f;

        }   // end of c'tor

        public static void InitDefaultValues(ThemeSet theme)
        {
            theme.PieMenuNormal = new PieMenuTheme(theme);
            theme.PieMenuNormalFocused = new PieMenuTheme(theme);

            // Normal.
            theme.PieMenuNormal.BodyColor = theme.BaseColor;
            theme.PieMenuNormal.OutlineColor = theme.FocusColor;
            theme.PieMenuNormal.OutlineWidth = 3.0f;

            // NormalFocused.
            theme.PieMenuNormalFocused.BodyColor = theme.FocusColor;
            theme.PieMenuNormalFocused.OutlineColor = theme.FocusColor;
            theme.PieMenuNormalFocused.OutlineWidth = 3.0f;

        }   // end of InitDefaultValues()

        public object Clone()
        {
            PieMenuTheme clone = MemberwiseClone() as PieMenuTheme;

            return clone;
        }   // end of Clone()

        #endregion

        #region Internal
        #endregion


    }
}
