
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using KoiX;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI;

using Boku;
using Boku.Base;
using Boku.Common;

namespace KoiX.UI.Dialogs
{
    // Dialog with a single slider, OK, and Cancel buttons.
    public class SliderDialog : BaseDialog
    {
        #region Members

        SliderLabelHelp slh;

        Button okButton;
        Button cancelButton;

        protected WidgetSet fullSet;    // Covers full dialog.
        protected WidgetSet buttonSet;  // Covers buttons at bottom.

        Vector2 size = new Vector2(512, 224);
        int margin = 32;

        float initialValue; // Used if user hits cancel.

        #endregion

        #region Accessors
        #endregion

        #region Public

        public SliderDialog(Vector2 position, string labelId, string helpId, float minValue, float maxValue, float increment, int numDecimals, float curValue, BaseWidget.Callback OnChange = null, ThemeSet theme = null)
            : base(theme: theme)
        {
#if DEBUG
            _name = "SliderDialog";
#endif
            initialValue = curValue;
            BackdropColor = Color.Transparent;

            Rectangle = new RectangleF(position, size);

            // Allow theme to be used lcoally.
            theme = this.theme;

            float width = size.X - 2.0f * margin;

            SystemFont font = new SystemFont(theme.TextFontFamily, theme.TextBaseFontSize * 1.2f, System.Drawing.FontStyle.Regular);
            FontWrapper wrapper = new FontWrapper(null, font);
            GetFont Font = delegate() { return wrapper; };

            slh = new SliderLabelHelp(this, Font, labelId, helpId, width, minValue, maxValue, increment, numDecimals, curValue, OnChange, theme);
            slh.Margin = new Padding(margin, margin, margin, margin);

            // Create buttons.
            okButton = new Button(this, new RectangleF(), OnChange: OnAccept, theme: this.theme, labelId: "auth.ok");
            okButton.Size = okButton.CalcMinSize() + new Vector2(margin, 0);  // Match button size to label, with a bit of margin.
            okButton.Label.Size = okButton.Size;                              // Make label same size so it gets centered correctly.

            cancelButton = new Button(this, new RectangleF(), OnChange: OnCancel, theme: this.theme, labelId: "auth.cancel");
            cancelButton.Size = cancelButton.CalcMinSize() + new Vector2(margin, 0);    // Match button size to label, with a bit of margin.
            cancelButton.Label.Size = cancelButton.Size;                                // Make label same size so it gets centered correctly.

            // Create sets.
            fullSet = new WidgetSet(this, rect, Orientation.Vertical, verticalJustification: Justification.Top);
            fullSet.FitToParentDialog = true;

            buttonSet = new WidgetSet(this, new RectangleF(Vector2.Zero, new Vector2(size.X - 2 * margin, okButton.Size.Y)), Orientation.Horizontal, horizontalJustification: Justification.Right);
            buttonSet.AddWidget(okButton);
            buttonSet.AddWidget(cancelButton);

            AddWidget(fullSet);
            fullSet.AddWidget(slh);
            fullSet.AddWidget(buttonSet);

            /*
            fullSet.RenderDebugOutline = true;
            slh.RenderDebugOutline = true;
            slh.RenderMarginDebug = true;
            buttonSet.RenderDebugOutline = true;
            buttonSet.RenderMarginDebug = true;
            */

            // Call Recalc for force all the button positions and sizes to be calculated.
            // We need this in order to properly calc the links.
            Dirty = true;
            Recalc();

            // Connect navigation links.
            CreateTabList();

            // Dpad nav includes all widgets so webSiteButton will be there.
            CreateDPadLinks();

        }   // end of c'tor

        void OnAccept(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);
        }   // end of OnAccept()

        void OnCancel(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);
        
            // Restore starting value.
            slh.TargetValue = initialValue;
            slh.OnChange();

        }   // end of OnCancel()

        public override void Update(SpriteCamera camera)
        {
            dirty = true;
            Recalc();

            base.Update(camera);
        }

        #endregion

        #region Internal
        #endregion

    }   // end of class SliderDialog
}   // end of namespace KoiX.UI.Dialogs
