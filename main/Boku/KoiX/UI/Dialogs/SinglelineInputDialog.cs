
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using KoiX;
using KoiX.Input;
using KoiX.Text;

using Boku;

namespace KoiX.UI.Dialogs
{
    /// <summary>
    /// Simple dialog which allows the imput of a single line of text.
    /// Generally used for giving names to things.
    /// </summary>
    public class SinglelineInputDialog : BaseDialog
    {
        #region Members

        SingleLineTextEditBox textBox;
        Button okButton;
        Button cancelButton;

        #endregion

        #region Acccessors

        public string CurrentText
        {
            get { return textBox.CurrentText; }
        }

        #endregion

        #region Public

        public SinglelineInputDialog(string labelId = null, string labelText = null, string defaultBoxText = "", string prefilledBoxText = "", BaseWidget.Callback OnAccept = null, BaseWidget.Callback OnCancel = null, TextLineEditor.ValidateText Validate = null, ThemeSet theme = null)
            : base()
        {
#if DEBUG
            _name = "SingleLineInputDialog";
#endif
            if (theme == null)
            {
                theme = Theme.CurrentThemeSet;
            }

            // Calc size of dialog.
            // Note that this is the space for eht text in the text box, the actual width include corners.
            int textBoxWidth = 300;
            Vector2 size = rect.Size;
            size.X = textBoxWidth + 2.0f * theme.DialogBodyTileNormal.CornerRadius;
            size.Y = size.X * 0.6f;
            size.X = (int)size.X;
            size.Y = (int)size.Y;
            rect.Size = size;

            // Center on screen.
            Vector2 position = rect.Position;
            position -= rect.Size / 2.0f;
            position.X = (int)position.X;
            position.Y = (int)position.Y;
            rect.Position = position;

            // Font.
            // TODO (****) Should we be getting this from shared?
            SystemFont font = SysFont.GetSystemFont(theme.TextFontFamily, theme.TextBaseFontSize, theme.TextBaseFontStyle);
            FontWrapper wrapper = new FontWrapper(null, font);
            GetFont Font = delegate() { return wrapper; };

            // Note: each of the elements are manually sized and placed.
            // Label.
            Vector2 pos = new Vector2(theme.DialogBodyTileNormal.CornerRadius, 0.5f * theme.DialogBodyTileNormal.CornerRadius);
            Label label = new Label(this, font, theme.DialogTitleTileNormal.TextColor, labelId: labelId, labelText: labelText);
            label.Position = pos;
            label.Size = label.CalcMinSize();
            AddWidget(label);

            // Text box.
            pos.Y += label.Size.Y;
            textBox = new SingleLineTextEditBox(this, Font, textBoxWidth, defaultBoxText, prefilledBoxText, theme: theme);
            textBox.Position = pos;
            AddWidget(textBox);

            // Magic numbers ahead...
            pos.Y += textBox.LocalRect.Height + 12;
            Vector2 buttonSize = new Vector2(100, 32);
            WidgetSet set = new WidgetSet(this, new RectangleF(pos, new Vector2(textBox.LocalRect.Width, buttonSize.Y)), Orientation.Horizontal, horizontalJustification: Justification.Right, verticalJustification: Justification.Center);
            AddWidget(set);
            okButton = new Button(this, new RectangleF(Vector2.Zero, buttonSize), OnChange: OnAccept, element: GamePadInput.Element.AButton, labelId: "textDialog.ok");
            cancelButton = new Button(this, new RectangleF(Vector2.Zero, buttonSize), OnChange: OnCancel, element: GamePadInput.Element.BButton, labelId: "textDialog.cancel");
            set.AddWidget(okButton);
            set.AddWidget(cancelButton);

            rect.Width = textBox.LocalRect.Width + 2.0f * theme.DialogBodyTileNormal.CornerRadius;
            rect.Height = pos.Y + okButton.LocalRect.Height + 0.5f * theme.DialogBodyTileNormal.CornerRadius + 2;

            // Connect navigation links.
            CreateTabList();

            // Dpad nav includes all widgets so webSiteButton will be there.
            CreateDPadLinks();

        }   // end of c'tor

        public override void Activate(params object[] args)
        {
            // Set focus on text box.
            textBox.SetFocus(overrideInactive: true);

            base.Activate(args);
        }   // end of Activate()

        #endregion

        #region InputEventHandler

        public override void RegisterForInputEvents()
        {
            // Just looking at Enter and Esc.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);

            base.RegisterForInputEvents();
        }   // end of RegisterForInputEvents()

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);

            // Accept input.
            if (input.Key == Keys.Enter)
            {
                okButton.OnButtonSelect();
                return true;
            }

            // Cancel.
            if (input.Key == Keys.Escape)
            {
                cancelButton.OnButtonSelect();
                return true;
            }

            return base.ProcessKeyboardEvent(input);
        }   // end of ProcessKeyboardEvent()


        #endregion

        #region Internal
        #endregion


    }   // end of class SinglelineInputDialog
}   // end of namespace KoiX.UI.Dialogs
