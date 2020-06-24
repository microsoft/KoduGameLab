
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

    public class ThemeSet : XmlData<ThemeSet>, ICloneable 
    {
        public Color BaseColor;
        public Color BaseColorPlus10;       // Plus 10% brightness.
        public Color BaseColorMinus10;      // Minus 10% brightness.

        public Color FocusColor;
        public Color FocusColorPlus10;      // Plus 10% brightness.
        public Color FocusColorMinus10;     // Minus 10% brightness.

        public Color AccentColor;           // Alternates with FocusColor.

        public Color BackdropColor;         // Used behind modal dialogs.

        public Color DisabledColor;
        public Color DisabledDarkColor;
        public Color DisabledLightColor;

        public float BaseOutlineWidth;
        public float BaseCornerRadius;
        public float ButtonCornerRadius;
        public BevelStyle BaseBevelStyle = BevelStyle.Slant;
        public float BaseBevelWidth = 4.0f;
        public Padding BasePadding = new Padding(24, 24, 24, 24);

        // Shadows used by dialogs.
        public Vector2 BaseShadowOffset = new Vector2(3.0f, 3.0f);
        public float BaseShadowSize = 12.0f;
        public float BaseShadowAlpha = 1.0f;

        // Text and Fonts.
        public Color LightTextColor;
        public Color DarkTextColor;

        public string TextFontFamily;                           // Font family for all UI text.
        public float TextBaseFontSize;                          // Base size for most text.
        public float TextTitleFontSize;                         // Size for dialg titles.
        public System.Drawing.FontStyle TextBaseFontStyle;

        // Buttons.
        public ButtonTheme ButtonDisabled;
        public ButtonTheme ButtonNormal;
        public ButtonTheme ButtonNormalFocused;
        public ButtonTheme ButtonNormalHover;
        public ButtonTheme ButtonNormalFocusedHover;
        public ButtonTheme ButtonSelected;
        public ButtonTheme ButtonSelectedFocused;
        public ButtonTheme ButtonSelectedHover;
        public ButtonTheme ButtonSelectedFocusedHover;

        // Dialogs.
        public DialogTheme DialogBodyTileDisabled;
        public DialogTheme DialogTitleTileDisabled;
        public DialogTheme DialogBodyTileNormal;
        public DialogTheme DialogTitleTileNormal;
        public DialogTheme DialogBodyTileFocused;
        public DialogTheme DialogTitleTileFocused;

        // CheckBoxes.
        public CheckBoxTheme CheckBoxDisabled;
        public CheckBoxTheme CheckBoxNormal;
        public CheckBoxTheme CheckBoxNormalFocused;
        public CheckBoxTheme CheckBoxSelected;
        public CheckBoxTheme CheckBoxSelectedFocused;

        // RadioButtons.
        public RadioButtonTheme RadioButtonDisabled;
        public RadioButtonTheme RadioButtonNormal;
        public RadioButtonTheme RadioButtonNormalFocused;
        public RadioButtonTheme RadioButtonSelected;
        public RadioButtonTheme RadioButtonSelectedFocused;

        // TextEditBoxes.
        public TextEditBoxTheme TextEditBoxDisabled;
        public TextEditBoxTheme TextEditBoxNormal;
        public TextEditBoxTheme TextEditBoxNormalFocused;

        // PieMenus.
        public PieMenuTheme PieMenuNormal;
        public PieMenuTheme PieMenuNormalFocused;

        // (****) TODO Fonts?

        // MessageDialog.
        public int MessageDialogMinLines;
        public int MessageDialogMaxLines;

        /// <summary>
        /// ThemeSet c'tor.  Should never be called.  Instead clone
        /// the current theme and modify.
        ///     ThemeSet theme = Theme.CurrentThemeSet.Clone() as ThemeSet;
        ///     Here's where 'friend' would be useful.
        /// </summary>
        public ThemeSet()
        {
            // Set default values.  Can be overridden by deserializing a different set
            // and setting it as the currentThemeSet.
            InitDefaultValues();

        }   // end of c'tor

        public void InitDefaultValues()
        {
            BaseColor = new Color(20, 150, 200);  // Blue.
            BaseColorMinus10 = new Color(17, 129, 173);
            BaseColorPlus10 = new Color(22, 167, 224);

            FocusColor = new Color(128, 221, 6);  // Green.
            FocusColorMinus10 = new Color(114, 196, 6);
            FocusColorPlus10 = new Color(143, 247, 7);

            AccentColor = new Color(10, 10, 10);

            BackdropColor = Color.White * 0.5f;

            DisabledColor = new Color(127, 127, 127);
            DisabledDarkColor = new Color(63, 63, 63);
            DisabledLightColor = new Color(192, 192, 192);

            BaseOutlineWidth = 1.5f;
            BaseCornerRadius = 24.0f;
            ButtonCornerRadius = 6.0f;

            // Text.
            TextFontFamily = "Calibri";
            TextBaseFontSize = 24.0f;
            TextTitleFontSize = 30.0f;
            TextBaseFontStyle = FontStyle.Regular;

            LightTextColor = new Color(255, 255, 255);
            DarkTextColor = new Color(0, 0, 0);

            // TODO Need to figure out better naming so these can be shared.
            //DialogInfoFont = SysFont.GetSystemFont(TextFontFamily, 24.0f, FontStyle.Regular);
            //DialogLabelFont = SysFont.GetSystemFont(TextFontFamily, 24.0f, FontStyle.Regular);
            //DialogTextBoxFont = SysFont.GetSystemFont(TextFontFamily, 32.0f, FontStyle.Regular);

            // MessageDialog.
            MessageDialogMinLines = 5;
            MessageDialogMaxLines = 11;

            // Init default values for sub-themes.
            ButtonTheme.InitDefaultValues(this);
            DialogTheme.InitDefaultValues(this);
            CheckBoxTheme.InitDefaultValues(this);
            RadioButtonTheme.InitDefaultValues(this);
            TextEditBoxTheme.InitDefaultValues(this);
            PieMenuTheme.InitDefaultValues(this);

        }   // end of InitDefaultValues()


        public object Clone()
        {
            ThemeSet clone = MemberwiseClone() as ThemeSet;

            // Deep copy the button themes.
            clone.ButtonDisabled = ButtonDisabled.Clone() as ButtonTheme;
            clone.ButtonNormal = ButtonNormal.Clone() as ButtonTheme;
            clone.ButtonNormalFocused = ButtonNormalFocused.Clone() as ButtonTheme;
            clone.ButtonNormalHover = ButtonNormalHover.Clone() as ButtonTheme;
            clone.ButtonNormalFocusedHover = ButtonNormalFocusedHover.Clone() as ButtonTheme;
            clone.ButtonSelected = ButtonSelected.Clone() as ButtonTheme;
            clone.ButtonSelectedFocused = ButtonSelectedFocused.Clone() as ButtonTheme;
            clone.ButtonSelectedHover = ButtonSelectedHover.Clone() as ButtonTheme;
            clone.ButtonSelectedFocusedHover = ButtonSelectedFocusedHover.Clone() as ButtonTheme;

            // Dialogs.
            DialogBodyTileDisabled = DialogBodyTileDisabled.Clone() as DialogTheme;
            DialogTitleTileDisabled = DialogTitleTileDisabled.Clone() as DialogTheme;
            DialogBodyTileNormal = DialogBodyTileNormal.Clone() as DialogTheme;
            DialogTitleTileNormal = DialogTitleTileNormal.Clone() as DialogTheme;
            DialogBodyTileFocused = DialogBodyTileFocused.Clone() as DialogTheme;
            DialogTitleTileFocused = DialogTitleTileFocused.Clone() as DialogTheme;

            // Checkboxes.
            CheckBoxDisabled = CheckBoxDisabled.Clone() as CheckBoxTheme;
            CheckBoxNormal = CheckBoxNormal.Clone() as CheckBoxTheme;
            CheckBoxNormalFocused = CheckBoxNormalFocused.Clone() as CheckBoxTheme;
            CheckBoxSelected = CheckBoxSelected.Clone() as CheckBoxTheme;
            CheckBoxSelectedFocused = CheckBoxSelectedFocused.Clone() as CheckBoxTheme;

            // RadioButtones.
            RadioButtonDisabled = RadioButtonDisabled.Clone() as RadioButtonTheme;
            RadioButtonNormal = RadioButtonNormal.Clone() as RadioButtonTheme;
            RadioButtonNormalFocused = RadioButtonNormalFocused.Clone() as RadioButtonTheme;
            RadioButtonSelected = RadioButtonSelected.Clone() as RadioButtonTheme;
            RadioButtonSelectedFocused = RadioButtonSelectedFocused.Clone() as RadioButtonTheme;

            // TextEditBoxes.
            TextEditBoxDisabled = TextEditBoxDisabled.Clone() as TextEditBoxTheme;
            TextEditBoxNormal = TextEditBoxNormal.Clone() as TextEditBoxTheme;
            TextEditBoxNormalFocused = TextEditBoxNormalFocused.Clone() as TextEditBoxTheme;

            // PieMenus.
            PieMenuNormal = PieMenuNormal.Clone() as PieMenuTheme;
            PieMenuNormalFocused = PieMenuNormalFocused.Clone() as PieMenuTheme;


            return clone;
        }   // end of Clone()

    }   // end of class ThemeSet

}   // end of namespace KoiX.UI
