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
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI;

using Boku.Common;

namespace KoiX.UI.Dialogs
{
    /// <summary>
    /// Flexible message dialog.
    /// 
    /// Can show optional title bar at top.
    /// 
    /// Can show multiple buttons.  Access is given to the buttons
    /// so that OnSelect methods may be connected to them.
    /// </summary>
    public class MessageDialog : BaseDialogWithTitle
    {
        [Flags]
        public enum ButtonSet
        {
            None = 0,
            Ok = 1,
            Continue = 2,
            Yes = 4,
            No = 8,
            Delete = 16,
            Overwrite = 32,
            IncrementAndSave = 64,
            Cancel = 128,
        }

        #region Members

        string messageId;   // String fed to localization.
        string messageText; // String actually displayed.

        TextBox textBox;

        // Which buttons we expose.
        ButtonSet buttons = ButtonSet.None;

        Button okButton;
        Button continueButton;
        Button yesButton;
        Button noButton;
        Button deleteButton;
        Button cancelButton;
        Button overwriteButton;
        Button incrementAndSaveButton;

        GetFont Font;

        #endregion

        #region Accessors

        public Button OkButton
        {
            get { return okButton; }
        }

        public Button ContinueButton
        {
            get { return continueButton; }
        }

        public Button YesButton
        {
            get { return yesButton; }
        }

        public Button NoButton
        {
            get { return noButton; }
        }

        public Button DeleteButton
        {
            get { return deleteButton; }
        }

        public Button CancelButton
        {
            get { return cancelButton; }
        }

        public Button OverwriteButton
        {
            get { return overwriteButton; }
        }

        public Button IncrementAndSaveButton
        {
            get { return incrementAndSaveButton; }
        }

        #endregion

        #region Public

        /// <summary>
        /// Simple c'tor for just displaying text with an OK button that dismisses the dialog.
        /// </summary>
        /// <param name="messageId"></param>
        public MessageDialog(string titleId = null, string titleText = null, string messageId = null, string messageText = null, ButtonSet buttons = ButtonSet.Continue, ThemeSet theme = null)
            : base(rect: RectangleF.EmptyRect, titleId: titleId, titleText: titleText, theme:theme)
        {
#if DEBUG
            _name = "MessageDialog : " + titleId;
#endif

            // Change ref to what base class has set.
            // Allows us to use theme rather than this.Theme.
            theme = this.ThemeSet;
            this.buttons = buttons;

            Debug.Assert(messageId == null || messageText == null, "Exactly one must be non-null");
            Debug.Assert(messageId != null || messageText != null, "Exactly one must be non-null");

            this.messageId = messageId;
            this.messageText = messageText;

            Font = SharedX.GetGameFont24;
            textBox = new TextBox(this, Font, color: theme.LightTextColor, textId: messageId, displayText: messageText);

            bodySet.AddWidget(textBox);

            Recalc();

            CreateTabList();
            CreateDPadLinks();

        }   // end of c'tor

        /// <summary>
        /// Where all the params are calculated.  We put this in a seperate function
        /// so that it can be called either by the c'tor or by Update();
        /// </summary>
        public override void Recalc()
        {
            float aspect = Theme.GoldenRatio;
            int margin = 16;

            Vector2 buttonSize = theme.ButtonNormal.DefaultSize;

            // Set up the buttons first since they can define a min width for the dialog.
            // Create set to hold buttons.  By default have this the full size of the dialog.
            // Use the button's Margin to adjust the spacing.
            if (buttonSet == null)
            {
                buttonSet = new WidgetSet(this, new RectangleF(), Orientation.Horizontal, horizontalJustification: Justification.Right, verticalJustification: Justification.Bottom);
                buttonSet.FitToParentDialog = true;

                // Add set to dialog's list of widgets so update/render/hit test etc all work.
                AddWidget(buttonSet);
            }

            // Create buttons, add to set.
            //
            // NOTE: The ordering of the buttons in this section 
            // determines the ordering of the buttons on the dialogs.
            //
            int buttonSetWidth = 2 * margin + 4;
            buttonSet.Padding = new Padding(margin);
            buttonSet.VerticalJustification = Justification.Bottom;
            if ((buttons & ButtonSet.Ok) != 0 && okButton == null)
            {
                okButton = new Button(this, new RectangleF(Vector2.Zero, buttonSize), OnChange: null, element: GamePadInput.Element.AButton, labelId: "textDialog.ok");
                okButton.KillParentDialogOnSelect = true;
                buttonSet.AddWidget(okButton);
            }
            if ((buttons & ButtonSet.Continue) != 0 && continueButton == null)
            {
                continueButton = new Button(this, new RectangleF(Vector2.Zero, buttonSize), OnChange: null, element: GamePadInput.Element.AButton, labelId: "textDialog.continue");
                continueButton.KillParentDialogOnSelect = true;
                buttonSet.AddWidget(continueButton);
            }
            if ((buttons & ButtonSet.Yes) != 0 && yesButton == null)
            {
                yesButton = new Button(this, new RectangleF(Vector2.Zero, buttonSize), OnChange: null, element: GamePadInput.Element.AButton, labelId: "textDialog.yes");
                yesButton.KillParentDialogOnSelect = true;
                buttonSet.AddWidget(yesButton);
            }
            if ((buttons & ButtonSet.No) != 0 && noButton == null)
            {
                noButton = new Button(this, new RectangleF(Vector2.Zero, buttonSize), OnChange: null, element: GamePadInput.Element.BButton, labelId: "textDialog.no");
                noButton.KillParentDialogOnSelect = true;
                buttonSet.AddWidget(noButton);
            }
            if ((buttons & ButtonSet.Delete) != 0 && deleteButton == null)
            {
                deleteButton = new Button(this, new RectangleF(Vector2.Zero, buttonSize), OnChange: null, element: GamePadInput.Element.AButton, labelId: "textDialog.Delete");
                deleteButton.KillParentDialogOnSelect = true;
                buttonSet.AddWidget(deleteButton);
            }
            if ((buttons & ButtonSet.Overwrite) != 0 && overwriteButton == null)
            {
                overwriteButton = new Button(this, new RectangleF(Vector2.Zero, buttonSize), OnChange: null, element: GamePadInput.Element.AButton, labelId: "saveLevelDialog.overwrite");
                overwriteButton.KillParentDialogOnSelect = true;
                buttonSet.AddWidget(overwriteButton);
            }
            if ((buttons & ButtonSet.IncrementAndSave) != 0 && incrementAndSaveButton == null)
            {
                incrementAndSaveButton = new Button(this, new RectangleF(Vector2.Zero, buttonSize), OnChange: null, element: GamePadInput.Element.YButton, labelId: "saveLevelDialog.incrementVersion");
                incrementAndSaveButton.KillParentDialogOnSelect = true;
                buttonSet.AddWidget(incrementAndSaveButton);
            }
            if ((buttons & ButtonSet.Cancel) != 0 && cancelButton == null)
            {
                cancelButton = new Button(this, new RectangleF(Vector2.Zero, buttonSize), OnChange: null, element: GamePadInput.Element.BButton, labelId: "textDialog.cancel");
                cancelButton.KillParentDialogOnSelect = true;
                buttonSet.AddWidget(cancelButton);
            }

            if (okButton != null)
            {
                buttonSetWidth += (int)Math.Ceiling(okButton.CalcMinSize().X);
            }
            if (continueButton != null)
            {
                buttonSetWidth += (int)Math.Ceiling(continueButton.CalcMinSize().X);
            }
            if (yesButton != null)
            {
                buttonSetWidth += (int)Math.Ceiling(yesButton.CalcMinSize().X);
            }
            if (noButton != null)
            {
                buttonSetWidth += (int)Math.Ceiling(noButton.CalcMinSize().X);
            }
            if (deleteButton != null)
            {
                buttonSetWidth += (int)Math.Ceiling(deleteButton.CalcMinSize().X);
            }
            if (overwriteButton != null)
            {
                buttonSetWidth += (int)Math.Ceiling(overwriteButton.CalcMinSize().X);
            }
            if (incrementAndSaveButton != null)
            {
                buttonSetWidth += (int)Math.Ceiling(incrementAndSaveButton.CalcMinSize().X);
            }
            if (cancelButton != null)
            {
                buttonSetWidth += (int)Math.Ceiling(cancelButton.CalcMinSize().X);
            }

            //
            // Set up text display.  Based on the input text, determine how
            // big to make the text region so everything looks decent.
            //

            int minWidth = Math.Max(100, buttonSetWidth);
            // We have a margin around the text.  32 pixels horizintally and 16 vertically.
            textBox.Position = new Vector2(2 * margin, margin);

            // Start with a single line for the message text.  Grow this
            // as needed to fit all the text.
            int numLines = 0;   // Number of lines in text region.
            Vector2 textSize;
            float height;       // Overall dialog size.
            float width;
            do
            {
                ++numLines; 

                textSize.Y = numLines * textBox.TotalSpacing;
                height = titleSet.LocalRect.Height + buttonSet.LocalRect.Height + textSize.Y + 2 * margin;
                width = aspect * height;

                width = Math.Max(minWidth, width);

                textBox.Width = (int)(width - 4 * margin);

                textSize.X = width;

            } while (textBox.NumLines > numLines);

            textBox.LocalRect.SetSize(textSize);
            bodySet.LocalRect.SetSize(textSize + new Vector2(4 * margin, 2 * margin));
            buttonSet.LocalRect.SetSize(new Vector2(textSize.X, 0));

            // Actual size needs to be big enough to enclose text message, buttons, and then
            // adjust for dialog padding.

            // Set size of dialog and center on screen.
            rect.Size = new Vector2(width, height);
            rect.Position = -rect.Size / 2.0f;

            if (buttonSetWidth == width)
            {
                buttonSet.HorizontalJustification = Justification.Center;
            }
            else
            {
                buttonSet.HorizontalJustification = Justification.Right;
            }

            base.Recalc();

        }   // end of Recalc()

        public void OnAccept(BaseWidget w)
        {
            bool succeeded = DialogManagerX.KillDialog(this);
            Debug.Assert(succeeded, "Why didn't this die?");
        }

        public override void Update(SpriteCamera camera)
        {
            // Update Theme elements.  If any of them changes the dirty flag
            // should be set.

            // If anything has changed, recalc the layout.  Note that
            // the dirty flag is cleared in the base Update call.
            if (Dirty)
            {
                Recalc();
            }

            base.Update(camera);
        }
        
        #endregion

        #region Internal
        #endregion

    }   // end of class MessageDialogWithTitle

}   // end of namespace KoiX.UI.Dialogs
