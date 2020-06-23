
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Audio;
using Boku.Base;
using Boku.Common;
using Boku.Common.Xml;
using Boku.Fx;
using Boku.UI2D;
using Boku.Input;

using BokuShared;
using ExtensionMethods;

namespace Boku.UI2D
{
    // This is the dialog that pops up when a user is expected to sign in.
    //
    public class AuthSignInDialog : INeedsDeviceReset
    {
        #region Members

        private const int margin = 16;
        private const int desiredCreatorNameSpace = 353;    // In English results in a 512 pixle wide dialog.

        private int dialogWidth = 512;

        private Texture2D titleBarTexture;
        private Texture2D dialogBodyTexture;
        private Texture2D checkboxUnlit;
        private Texture2D checkboxLit;
        private Texture2D helpIcon;
        private Texture2D textBoxTexture;

        private Rectangle titleRect;
        private Rectangle dialogBodyRect;
        private Rectangle helpRect;

        private AABB2D helpBox = new AABB2D();
        private AABB2D creatorBox = new AABB2D();
        private AABB2D pinBox = new AABB2D();
        private AABB2D checkBoxBox = new AABB2D();

        private Button okButton;
        private Button cancelButton;

        private TextBlob blob;          // Blob used for random bits of text.
        // We use specific blobs for these since we need to keep the cursor position fixed.
        private TextBlob creatorBlob;   // Blob used to edit creator name.
        private TextBlob pinBlob;       // Blob used to edit pin.

        private bool editingCreator = false;
        private bool editingPin = false;
        private bool keepSignedInChecked = false;

        private KeyboardInput.KeyboardKeyEvent prevOnKey;
        private KeyboardInput.KeyboardCharEvent prevOnChar;
        private KeyboardInput.KeyboardCharEvent prevTextInput;

        private ScrollableTextDisplay scrollableTextDisplay;

        private bool newUserMode = false;    // Set when we want this to look like a new user dialog.
        private bool active = false;

        #endregion

        #region Accessors

        public bool Active
        {
            get { return active; }
            set
            {
                if (active != value)
                {
                    // On activate.
                    if (value)
                    {
                        // Pre-fill blobs so we can force the cursor to the end.
                        creatorBlob.RawText = Auth.DefaultCreatorName;
                        creatorBlob.End();
                        pinBlob.RawText = Auth.DefaultCreatorPin;
                        pinBlob.End();

                        EditingCreator = false;
                        EditingPin = false;

                        keepSignedInChecked = XmlOptionsData.KeepSignedInOnExit;
                        newUserMode = false;

                        // Save away existing keyboard event handlers.  We need this in case
                        // this dialog is launched over some other dialog which uses keyboard
                        // input like the SaveLevelDialog.
                        prevOnChar = KeyboardInput.OnChar;
                        prevOnKey = KeyboardInput.OnKey;
#if !NETFX_CORE
                        prevTextInput = BokuGame.bokuGame.winKeyboard.CharacterEntered;
#endif

                        KeyboardInput.OnKey = KeyInput;
#if NETFX_CORE
                        Debug.Assert(false, "Does this work?  Why did we prefer winKeyboard?");
                        KeyboardInput.OnChar = TextInput;
#else
                        BokuGame.bokuGame.winKeyboard.CharacterEntered = TextInput;
#endif
                    }
                    else
                    {
                        // Restore keyboard event handlers.
                        KeyboardInput.OnKey = prevOnKey;
                        KeyboardInput.OnChar = prevOnChar;
#if !NETFX_CORE
                        BokuGame.bokuGame.winKeyboard.CharacterEntered = prevTextInput;
#endif
                    }

                    active = value;
                }
            }
        }

        private bool EditingCreator
        {
            get { return editingCreator; }
            set
            {
                if (editingCreator != value)
                {
                    editingCreator = value;
                    creatorBlob.End();
                    if (editingCreator)
                    {
                        EditingPin = false;
                        if (creatorBlob.ScrubbedText == Auth.DefaultCreatorName)
                        {
                            creatorBlob.RawText = "";
                        }
                        KeyboardInput.ShowOnScreenKeyboard();
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(creatorBlob.ScrubbedText))
                        {
                            creatorBlob.RawText = Auth.DefaultCreatorName;
                        }
                    }
                }
            }
        }

        private bool EditingPin
        {
            get { return editingPin; }
            set
            {
                if (editingPin != value)
                {
                    editingPin = value;
                    pinBlob.End();
                    if (editingPin)
                    {
                        EditingCreator = false;
                        if (pinBlob.ScrubbedText == Auth.DefaultCreatorPin)
                        {
                            pinBlob.RawText = "";
                        }
                        KeyboardInput.ShowOnScreenKeyboard();
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(pinBlob.ScrubbedText))
                        {
                            pinBlob.RawText = Auth.DefaultCreatorPin;
                        }
                    }
                }
            }
        }

#endregion

#region Public

        public AuthSignInDialog()
        {
            blob = new TextBlob(UI2D.Shared.GetGameFont20, "", 400);
            creatorBlob = new TextBlob(UI2D.Shared.GetGameFont20, Auth.DefaultCreatorName, 400);
            pinBlob = new TextBlob(UI2D.Shared.GetGameFont20, Auth.DefaultCreatorPin, 400);

            okButton = new Button(Strings.Localize("auth.ok"), Color.White, null, UI2D.Shared.GetGameFont20);
            cancelButton = new Button(Strings.Localize("auth.cancel"), Color.White, null, UI2D.Shared.GetGameFont20);

            scrollableTextDisplay = new ScrollableTextDisplay();
        }   // end of c'tor

        public void Update()
        {
            if (active)
            {
                scrollableTextDisplay.Update(null);
                if (scrollableTextDisplay.Active)
                {
                    return;
                }

                //
                // Input?
                //
                if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                {
                    for (int i = 0; i < TouchInput.TouchCount; i++)
                    {
                        TouchContact touch = TouchInput.GetTouchContactByIndex(i);

                        Vector2 touchHit = touch.position;
                        HandleTouchInput(touch, touchHit);
                    }
                }
                else if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                {
                    Vector2 hit = MouseInput.PositionVec;
                    HandleMouseInput(hit);
                }
            }

        }   // end of Update()

        private void HandleTouchInput(TouchContact touch, Vector2 hit)
        {
            if (helpBox.Touched(touch, hit))
            {
                Boku.Common.Gesture.TouchGestureManager.Get().TapGesture.ClearWasTapped();
                OnHelp();
            }
            if (creatorBox.Touched(touch, hit))
            {
                Boku.Common.Gesture.TouchGestureManager.Get().TapGesture.ClearWasTapped();
                EditingCreator = true;
            }
            if (pinBox.Touched(touch, hit))
            {
                Boku.Common.Gesture.TouchGestureManager.Get().TapGesture.ClearWasTapped();
                EditingPin = true;
            }
            if (checkBoxBox.Touched(touch, hit))
            {
                Boku.Common.Gesture.TouchGestureManager.Get().TapGesture.ClearWasTapped();
                keepSignedInChecked = !keepSignedInChecked;
                EditingCreator = false;
                EditingPin = false;
            }

            // Buttons
            if (okButton.Box.Touched(touch, hit))
            {
                Boku.Common.Gesture.TouchGestureManager.Get().TapGesture.ClearWasTapped();
                OnAccept();
            }
            if (cancelButton.Box.Touched(touch, hit))
            {
                Boku.Common.Gesture.TouchGestureManager.Get().TapGesture.ClearWasTapped();
                OnCancel();
            }

        }   // end of HandleTouchInput()

        private void HandleMouseInput(Vector2 hit)
        {
            if (helpBox.LeftPressed(hit))
            {
                OnHelp();
            }
            if (creatorBox.LeftPressed(hit))
            {
                EditingCreator = true;
            }
            if (pinBox.LeftPressed(hit))
            {
                EditingPin = true;
            }
            if (checkBoxBox.LeftPressed(hit))
            {
                keepSignedInChecked = !keepSignedInChecked;
                EditingCreator = false;
                EditingPin = false;
            }

            // Buttons
            if (okButton.Box.LeftPressed(hit))
            {
                // Save results.
                OnAccept();
            }
            if (cancelButton.Box.LeftPressed(hit))
            {
                OnCancel();
            }

            // Update hover state.
            okButton.SetHoverState(hit);
            cancelButton.SetHoverState(hit);

        }   // end of HandleMouseInput()

        private void OnAccept()
        {
            // If the current creator name or pin isn't valid, don't allow the user to click OK.
            // Skip this check if we have Guest signed in.
            if (creatorBlob.ScrubbedText != Auth.DefaultCreatorName && pinBlob.ScrubbedText != Auth.DefaultCreatorPin)
            {
                if (!Auth.IsPinValid(pinBlob.ScrubbedText) || string.IsNullOrWhiteSpace(creatorBlob.ScrubbedText))
                {
                    Foley.PlayNoBudget();
                    return;
                }
            }

            string newCreatorName = creatorBlob.ScrubbedText;
            string newPin = pinBlob.ScrubbedText;
            string newIdHash = Auth.CreateIdHash(newCreatorName, newPin);

            if (!newUserMode)
            {
                bool previouslySeenHash = true;     // TODO (v-chph) Put test here.  Note we should always return true for guest ( Auth.DefaultCreatorHash ) without having to ping the server.
                
                if (previouslySeenHash)
                {
                    // We've seen this name before.
                    // Update Auth with the new values.
                    Auth.SetCreator(newCreatorName, newIdHash);
                }
                else
                {
                    // We haven't seen this name before so change dialog to New User.
                    newUserMode = true;
                    return; // Don't deactivate.
                }
            }

            // We've seen this name before.
            // Update Auth with the new values.
            Auth.SetCreator(newCreatorName, newIdHash);

            // Note that setting this also forces the creator name and idHash
            // to be saved from Auth.
            XmlOptionsData.KeepSignedInOnExit = keepSignedInChecked;

            // Done, we can exit now.
            Active = false;

            newUserMode = false;

            // Restart status dialog.
            AuthUI.ShowStatusDialog();

        }   // end of OnAccept()

        private void OnCancel()
        {
            // Even if we cancel we may have changed this so save the state.
            XmlOptionsData.KeepSignedInOnExit = keepSignedInChecked;

            // Just go away!
            Active = false;

            // Restart status dialog.
            AuthUI.ShowStatusDialog();

        }   // end of OnCancel()

        private void OnHelp()
        {
            scrollableTextDisplay.Activate(null, Strings.Localize("auth.helpText"), UIGridElement.Justification.Left, useBackgroundThumbnail: false, useRtCoords: false);
        }   // end of OnHelp()

        public void Render()
        {
            if (active)
            {
                titleRect = new Rectangle(0, 0, dialogWidth, 72);
                dialogBodyRect = new Rectangle(0, 64, dialogWidth, 320);

                // Now that we have the final dialog size, center it on the screen.
                Vector2 pos = BokuGame.ScreenSize / 2.0f;
                pos.X -= titleRect.Width / 2;
                pos.Y -= (titleRect.Height + dialogBodyRect.Height) / 2;
                titleRect.X = (int)pos.X;
                titleRect.Y = (int)pos.Y;
                dialogBodyRect.X = titleRect.X;
                dialogBodyRect.Y = titleRect.Y + titleRect.Height;

                int padding = 4;
                helpRect = new Rectangle(titleRect.X + titleRect.Width - titleRect.Height - margin, titleRect.Y + padding, titleRect.Height - 2 * padding, titleRect.Height - 2 * padding);

                AuthUI.RenderTile(titleBarTexture, titleRect);
                AuthUI.RenderTile(dialogBodyTexture, dialogBodyRect);

                // Title bar help icon.
                AuthUI.RenderTile(helpIcon, helpRect);
                helpBox.Set(helpRect);

                // Title bar text.
                string str = newUserMode ? Strings.Localize("auth.newUserTitle") : Strings.Localize("auth.signInTitle");
                blob.RawText = str;
                blob.Font = UI2D.Shared.GetGameFont30Bold;
                blob.Justification = UIGridElement.Justification.Left;
                blob.RenderWithButtons(new Vector2(titleRect.X + margin, titleRect.Y + 6), Color.White, Color.Black, new Vector2(0, 2), maxLines: 1);

                // Text box labels.
                int verticalBoxSpacing = blob.TotalSpacing + 4;
                blob.Font = UI2D.Shared.GetGameFont24;
                blob.Justification = UIGridElement.Justification.Right;
                string creatorString = Strings.Localize("auth.creator");
                string pinString = Strings.Localize("auth.pin");

                int posX = (int)Math.Max(blob.Font().MeasureString(creatorString).X, blob.Font().MeasureString(pinString).X);
                posX += margin;
                blob.Width = dialogBodyRect.Width - posX - 2 * margin;
                pos = new Vector2(dialogBodyRect.X + posX - blob.Width, dialogBodyRect.Y + margin);
                blob.RawText = creatorString;
                blob.RenderWithButtons(pos, Color.White);
                pos.Y += verticalBoxSpacing;
                blob.RawText = pinString;
                blob.RenderWithButtons(pos, Color.White);
                
                // Text boxes.
                // Creator.
                creatorBlob.Justification = UIGridElement.Justification.Left;
                pos.Y -= verticalBoxSpacing;
                pos.X = dialogBodyRect.X + posX + margin;
                int creatorBoxWidth = dialogBodyRect.Width - posX - 2 * margin;
                Rectangle creatorTextBoxRect = new Rectangle((int)pos.X, (int)pos.Y, creatorBoxWidth, 40);
                creatorBox.Set(creatorTextBoxRect);
                // If editing, put a focus highlight around it.
                if (EditingCreator)
                {
                    AuthUI.RenderTile(textBoxTexture, creatorTextBoxRect, AuthUI.FocusColor);
                    creatorTextBoxRect = creatorTextBoxRect.Shrink(2);

                    // Also display warning about picking a good creator name.
                    Vector2 warningPos = new Vector2(dialogBodyRect.Left + 8, pinBox.Rectangle.Bottom);
                    int prevWidth = blob.Width;
                    blob.Width = dialogBodyRect.Width - 16;
                    blob.Justification = UIGridElement.Justification.Left;
                    str = Strings.Localize("auth.helpTextShort");
                    blob.RawText = str;
                    blob.Font = UI2D.Shared.GetGameFont15_75;
                    blob.RenderWithButtons(warningPos, AuthUI.ErrorColor);
                    blob.Font = UI2D.Shared.GetGameFont24;
                    blob.Width = prevWidth;
                    blob.Justification = UIGridElement.Justification.Right;
                }
                AuthUI.RenderTile(textBoxTexture, creatorTextBoxRect);
                creatorBlob.RenderWithButtons(pos, creatorBlob.ScrubbedText == "Guest" ? Color.Gray : Color.Black, renderCursor: EditingCreator);

                // Pin
                pos.Y += verticalBoxSpacing;
                // Use regular blob for measurement.
                blob.Justification = UIGridElement.Justification.Left;
                int pinBoxWidth = (int)pinBlob.Font().MeasureString("0000").X;  // Assumes 0s are max width characters.
                Rectangle pinTextBoxRect = new Rectangle((int)pos.X, (int)pos.Y, pinBoxWidth, 40);
                pinBox.Set(pinTextBoxRect);
                if (EditingPin)
                {
                    AuthUI.RenderTile(textBoxTexture, pinTextBoxRect, AuthUI.FocusColor);
                    pinTextBoxRect = pinTextBoxRect.Shrink(2);
                }
                AuthUI.RenderTile(textBoxTexture, pinTextBoxRect);

                // Hack to hide pin numbers.  Only show last number unless Guest pin.
                string rawText = pinBlob.ScrubbedText;  // Save existing string.
                if (pinBlob.ScrubbedText != Auth.DefaultCreatorPin)
                {
                    str = "";
                    if (pinBlob.ScrubbedText.Length > 0)
                    {
                        for (int i = 0; i < pinBlob.ScrubbedText.Length - 1; i++)
                        {
                            str += '•';
                        }
                        str += pinBlob.ScrubbedText[pinBlob.ScrubbedText.Length - 1];
                        pinBlob.RawText = str;
                    }
                }
                pinBlob.RenderWithButtons(pos, pinBlob.ScrubbedText == Auth.DefaultCreatorPin ? Color.Gray : Color.Black, renderCursor: EditingPin);
                // Restore
                pinBlob.RawText = rawText;

                // Pin warnings.
                if (pinBlob.ScrubbedText != Auth.DefaultCreatorPin && !Auth.IsPinValid(pinBlob.ScrubbedText))
                {
                    pos.X += pinTextBoxRect.Width + 8;
                    str = Strings.Localize("auth.pinError");
                    if (pinBlob.ScrubbedText.Length != 4)
                    {
                        str += "\n" + Strings.Localize("auth.pinTooShort");
                    }
                    else
                    {
                        str += "\n" + Strings.Localize("auth.pinTooSimple");
                    }
                    blob.RawText = str;
                    blob.RenderWithButtons(pos, AuthUI.ErrorColor);
                }

                // Buttons.  Fit at bottom of dialog.
                pos = new Vector2(dialogBodyRect.Right, dialogBodyRect.Bottom);
                pos.X -= margin;
                pos.Y -= margin;
                pos -= cancelButton.GetSize();
                cancelButton.Render(pos, useBatch:false);
                pos.X -= margin;
                pos.X -= okButton.GetSize().X;
                okButton.Render(pos, useBatch: false);

                // Keep signed in checkbox.
                // Position vertically just above buttons.
                pos.X = dialogBodyRect.X + margin;
                pos.Y = okButton.Box.Min.Y + 32;
                pos.X += 32;    // Adjust for checkbox.
                blob.RawText = Strings.Localize("auth.keepSignedIn");
                blob.Width = dialogBodyRect.Width - 2 * margin - 32;
                pos.Y -= blob.NumLines * blob.TotalSpacing;
                Rectangle checkboxRect = new Rectangle((int)pos.X - 32, (int)pos.Y + 4, 32, 32);
                checkBoxBox.Set(checkboxRect);
                AuthUI.RenderTile(keepSignedInChecked ? checkboxLit : checkboxUnlit, checkboxRect);
                blob.RenderWithButtons(pos, Color.White);

                // Adjust dialog size to CreatorName box is the desired size.
                // This keeps the entry box the same size regardless of the length
                // of "Creator" in whatever language is being used.
                dialogWidth += desiredCreatorNameSpace - (int)creatorBox.Width;

                scrollableTextDisplay.Render();
            }

            VirtualKeyboard.Render();

        }   // end of Render()

#endregion

#region Internal

        // Used to accumulate values when user is inputting special characters using the Alt key.
        string specialChar = null;

        public void TextInput(char c)
        {
            // Handle special character input.
            if (KeyboardInput.AltWasPressed)
            {
                specialChar = null;
            }
            if (KeyboardInput.AltIsPressed)
            {
                // accumulate keystrokes
                specialChar += c;
                return;
            }

            // Grab the tab and use it for cycling through the focus options.
            if (c == '\t')
            {
                ToggleEditTarget();
                KeyboardInput.ClearAllWasPressedState(Keys.Tab);

                return;
            }

            if (EditingCreator || EditingPin)
            {
                // Ignore enter.
                if (c == 13)
                {
                    return;
                }

                string str = new string(c, 1);
                str = TextHelper.FilterInvalidCharacters(str);

                if (!string.IsNullOrEmpty(str))
                {
                    // Check if we've gotten too long.
                    if (EditingCreator)
                    {
                        creatorBlob.InsertString(str);

                        int width = creatorBlob.GetLineWidth(0);
                        if (width >= creatorBox.Width)
                        {
                            // Bzzzt!
                            Foley.PlayNoBudget();
                            for (int i = 0; i < str.Length; i++)
                            {
                                creatorBlob.Backspace();
                            }
                        }
                        else
                        {
                            Foley.PlayClickDown();
                        }
                    }
                    else if (EditingPin)
                    {
                        // With the pin, max out at 4 digits and only allow digits.
                        if (pinBlob.ScrubbedText.Length > 3 || !char.IsDigit(str[0]))
                        {
                            Foley.PlayNoBudget();
                        }
                        else
                        {
                            pinBlob.InsertString(str);
                        }
                    }
                }
            }
        }   // end of TextInput()

        // TODO (****) Clean this up.  We're mixing text as input with text as control here.
        // Probably the right thing to do would be to push/pop the text input callbacks
        // dynamically to mirror the state we're in.

        public void KeyInput(Keys key)
        {
            if (EditingCreator || EditingPin)
            {
                KeyboardInput.ClearAllWasPressedState(key);

                TextBlob curBlob = EditingCreator ? creatorBlob : pinBlob;
                //string curString = EditingCreator ? curCreator : curPin;
                //int curLength = curString.Length;

                switch (key)
                {
                    case Keys.Enter:
                        Foley.PlayClickDown();
                        OnAccept();
                        break;

                    case Keys.Escape:
                        Foley.PlayClickDown();
                        OnCancel();
                        break;

                    case Keys.Left:
                        curBlob.CursorLeft();
                        Foley.PlayClickDown();
                        break;
                    case Keys.Right:
                        curBlob.CursorRight();
                        Foley.PlayClickDown();
                        break;

                    case Keys.Home:
                        Foley.PlayClickDown();
                        creatorBlob.Home();
                        break;

                    case Keys.End:
                        Foley.PlayClickDown();
                        curBlob.End();
                        break;

                    case Keys.Back:
                        curBlob.Backspace();
                        Foley.PlayClickDown();
                        break;

                    case Keys.Delete:
                        curBlob.Delete();
                        break;

                    case Keys.Tab:
                        ToggleEditTarget();
                        break;

                }   // end of switch on special characters.

            }
        }   // end of KeyInput()

        /// <summary>
        /// Toggles the in-focus text box between Creator and Pin.
        /// Triggered by tab key.  Depending on which keyboard we're
        /// using, the tab may come in differently.
        /// </summary>
        void ToggleEditTarget()
        {
            // If one of the text boxes has focus, toggle to the other.
            if (EditingCreator)
            {
                EditingPin = true;
            }
            else if (EditingPin)
            {
                EditingCreator = true;
            }
            // If neither has focus, focus on the creator.
            if (!EditingCreator && !EditingPin)
            {
                EditingCreator = true;
            }

        }   // end of ToggleEditTarget()

        public void LoadContent(bool immediate)
        {
            titleBarTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\BlueTextTileWide");
            dialogBodyTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\LoadLevel\PopupFrame");
            checkboxUnlit = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\CheckboxUnlit");
            checkboxLit = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\CheckboxLit");
            helpIcon = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\Help");
            textBoxTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\WhiteTile");

            scrollableTextDisplay.LoadContent(immediate);
        }

        public void InitDeviceResources(GraphicsDevice device)
        {
            scrollableTextDisplay.InitDeviceResources(device);
        }

        public void UnloadContent()
        {
            BokuGame.Release(ref titleBarTexture);
            BokuGame.Release(ref dialogBodyTexture);
            BokuGame.Release(ref checkboxUnlit);
            BokuGame.Release(ref checkboxLit);
            BokuGame.Release(ref helpIcon);
            BokuGame.Release(ref textBoxTexture);

            scrollableTextDisplay.UnloadContent();
        }

        public void DeviceReset(GraphicsDevice device)
        {
        }

#endregion

    }   // end of class AuthSignInDialog

}   // end of namespace Boku.UI2D
