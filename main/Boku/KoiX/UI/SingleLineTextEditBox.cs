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
using Microsoft.Xna.Framework.Input;

using KoiX;
using KoiX.Geometry;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;

using Boku.Audio;

using BokuShared;

namespace KoiX.UI
{
    /// <summary>
    /// This is a text edit box designed for entries which only require a single
    /// line of input, eg creator name, level title, etc.
    /// 
    /// TODO (****) Allow a separate color for default text rather than just ghosted.
    /// </summary>
    public class SingleLineTextEditBox : BaseWidget
    {
        #region Members

        Twitchable<Color> bodyColor;

        Twitchable<Color> outlineColor;
        Twitchable<float> outlineWidth;

        Twitchable<Color> textColor;

        TextEditBoxTheme curTheme;  // Current theme settings based on state.

        TextBlob blob;

        string defaultText;     // This is the text that is showed when the box is empty.
        string prefilledText;   // This is any text that is prefilled into the blob.

        SpriteCamera camera;    // Need to hold on to this ref to handle mouse input.
        Vector2 blobPosition;

        UIState prevCombinedState = UIState.Inactive;

        bool numbersOnly = false;           // Only allow numbers.
        bool maskInput = false;             // Masks the input with dots.  Used for pin number, etc.

        bool showSearchIcon = false;
        Texture2D searchIcon;

        double lastKeypressTime = 0;

        #endregion

        #region Accessors

        /// <summary>
        /// Returns the current text for the box.  If no text has been entered
        /// this returns the default text.
        /// </summary>
        public string CurrentText
        {
            get
            {
                if (string.IsNullOrEmpty(blob.ScrubbedText))
                {
                    return defaultText;
                }
                else
                {
                    return blob.ScrubbedText;
                }
            }
        }

        /// <summary>
        /// Get the text currently in the text box.  Returns
        /// empty string if default is being shown.
        /// </summary>
        public string CurrentTextNoDefault
        {
            get { return blob.ScrubbedText; }
        }

        /// <summary>
        /// Get the height of a single line of text.
        /// </summary>
        public float TotalSpacing
        {
            get { return blob.TotalSpacing; }
        }

        /// <summary>
        /// Raw text in blob.
        /// </summary>
        public string RawText
        {
            get { return blob.RawText; }
            set { blob.RawText = value; }
        }

        /// <summary>
        /// When true, shows the search icon (magnifiying glass)
        /// at the beginning of the text window.
        /// </summary>
        public bool ShowSearchIcon
        {
            get { return showSearchIcon; }
            set { showSearchIcon = value; }
        }

        /// <summary>
        /// Number of seconds since the last keypress was added.  Ignores moving
        /// the cursor and only counts keypresses that change the text string.
        /// </summary>
        public double SecondsSinceLastKeypress
        {
            get { return Time.WallClockTotalSeconds - lastKeypressTime; }
        }

        #endregion

        #region Public

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentDialog"></param>
        /// <param name="rect"></param>
        /// <param name="textWidth">Width in pixels of widget.  Text space is this minus corners.</param>
        /// <param name="defaultText">This is the text that is showed when the box is empty.</param>
        /// <param name="prefilledText">This is any text that is prefilled into the blob.</param>
        public SingleLineTextEditBox(BaseDialog parentDialog, GetFont Font, int textWidth, string defaultText, string prefilledText, Callback OnChange = null, ThemeSet theme = null, string id = null, object data = null, int maxCharacters = int.MaxValue, bool numbersOnly = false, bool maskInput = false)
            : base(parentDialog, OnChange: OnChange, theme: theme, id: id, data: data)
        {
            Debug.Assert(theme != null);

            this.theme = theme;
            curTheme = theme.TextEditBoxNormal;

            this.numbersOnly = numbersOnly;
            this.maskInput = maskInput;

            this.defaultText = defaultText ?? "";
            this.prefilledText = prefilledText ?? "";

            this.onChange = OnChange;

            this.localRect = new RectangleF();
            localRect.Size = new Vector2(textWidth, Font().LineSpacing);

            bodyColor = new Twitchable<Color>(Theme.QuickTwitchTime, TwitchCurve.Shape.EaseInOut, this, startingValue: curTheme.BodyColor);

            outlineColor = new Twitchable<Color>(Theme.QuickTwitchTime, TwitchCurve.Shape.EaseInOut, this, startingValue: curTheme.OutlineColor);
            outlineWidth = new Twitchable<float>(Theme.QuickTwitchTime, TwitchCurve.Shape.EaseInOut, this, startingValue: curTheme.OutlineWidth);

            textColor = new Twitchable<Color>(Theme.QuickTwitchTime, TwitchCurve.Shape.EaseInOut, this, startingValue: curTheme.TextColor);

            blob = new TextBlob(Font, prefilledText, (int)(textWidth - 2.0f * curTheme.CornerRadius));

            // Switching to SingleLineMode has a couple of effects.  
            // First, line feeds are ignored.  Second, the length 
            // of the input string is limited to the width of the
            // text space.
            blob.SingleLineMode = true;
        }   // end of c'tor

        public override void Recalc(Vector2 parentPosition)
        {
            base.Recalc(parentPosition);
        }   // end of Recalc();

        public override void Update(SpriteCamera camera, Vector2 parentPosition)
        {
            base.Update(camera, parentPosition);

            if (Active)
            {
                // Needed to handle focus changes.
                base.Update(camera, parentPosition);

                UIState combinedState = CombinedState;
                if (combinedState != prevCombinedState)
                {
                    // Set new state params.  Note that dirty flag gets
                    // set internally by setting individual values so
                    // we don't need to worry about it here.
                    switch (combinedState)
                    {
                        case UIState.Disabled:
                            curTheme = theme.TextEditBoxDisabled;
                            break;

                        case UIState.Active:
                        case UIState.ActiveHover:
                        case UIState.ActiveSelected:
                        case UIState.ActiveSelectedHover:
                            curTheme = theme.TextEditBoxNormal;
                            break;

                        case UIState.ActiveFocused:
                        case UIState.ActiveFocusedHover:
                        case UIState.ActiveSelectedFocused:
                        case UIState.ActiveSelectedFocusedHover:
                            curTheme = theme.TextEditBoxNormalFocused;
                            break;

                        default:
                            // Should only happen on state.None
                            break;

                    }   // end of switch

                    // Now that we have the new theme, set all the Twitchable values from it.
                    // Non-twitchable values we get directly from the theme.
                    bodyColor.Value = curTheme.BodyColor;
                    outlineColor.Value = curTheme.OutlineColor;
                    outlineWidth.Value = curTheme.OutlineWidth;

                    textColor.Value = curTheme.TextColor;

                    prevCombinedState = combinedState;
                    dirty = true;
                    this.camera = camera;
                }   // end if state changed

            }   // end if Active
        }   // end of Update()

        public override void Render(SpriteCamera camera, Vector2 parentPosition)
        {
            if(Alpha > 0)
            {
                SpriteBatch batch = KoiLibrary.SpriteBatch;

                Vector2 pos = LocalRect.Position + parentPosition;

                // Render box.
                RoundedRect.Render(camera, pos, LocalRect.Size, curTheme.CornerRadius, bodyColor.Value * Alpha,
                                    outlineWidth: outlineWidth.Value, outlineColor: outlineColor.Value * Alpha,
                                    shadowStyle:curTheme.Shadow, shadowOffset:curTheme.ShadowOffset, shadowSize:curTheme.ShadowSize, shadowAttenuation: curTheme.ShadowAlpha);

                if (showSearchIcon)
                {
                    int margin = (int)(curTheme.CornerRadius);
                    int iconSize = (int)(localRect.Size.Y);
                    
                    Rectangle rect = new Rectangle((int)pos.X + margin, (int)pos.Y, iconSize, iconSize);

                    batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, samplerState: null, depthStencilState: null, rasterizerState: null, effect: null, transformMatrix: camera.ViewMatrix);
                    batch.Draw(searchIcon, rect, Color.White);
                    batch.End();

                    pos.X += iconSize + 2 * margin;
                    blob.Width = (int)(localRect.Size.X - 4 * margin - iconSize);
                }

                blobPosition = pos + new Vector2(curTheme.CornerRadius, 0);
                blob.Justification = TextHelper.Justification.Left;

                if (string.IsNullOrEmpty(blob.ScrubbedText))
                {
                    // No blob text so render default text.  
                    // Be sure to put the cursor at the beginning but only if inFocus.
                    blob.RawText = defaultText;
                    blob.SetCursorPosition(0, 0);
                    blob.RenderText(camera, blobPosition, textColor.Value * 0.5f, renderCursor: InFocus, maxLines: 1);
                    blob.RawText = "";  // Need to clear this otherwise it won't be empty next frame.
                }
                else
                {
                    // Render blob text.
                    if (maskInput)
                    {
                        // Hack to hide pin numbers.  Only show last number unless Guest pin.
                        string rawText = blob.ScrubbedText;  // Save existing string.
                        if (blob.ScrubbedText != Auth.DefaultCreatorPin)
                        {
                            string str = "";
                            if (blob.ScrubbedText.Length > 0)
                            {
                                for (int i = 0; i < blob.ScrubbedText.Length - 1; i++)
                                {
                                    str += '•';
                                }
                                // If in focus, mask all but the last character.
                                // If not in focus, mask all characters.
                                str += InFocus ? blob.ScrubbedText[blob.ScrubbedText.Length - 1] : '•';
                                blob.RawText = str;
                            }
                        }
                        blob.RenderText(camera, blobPosition, blob.ScrubbedText == Auth.DefaultCreatorPin ? textColor.Value * 0.5f : textColor.Value, renderCursor: InFocus);
                        // Restore
                        blob.RawText = rawText;
                    }
                    else
                    {
                        // Just render with no masking.
                        blob.RenderText(camera, blobPosition, textColor.Value, renderCursor: InFocus, maxLines: 1);
                    }
                }
            
            }   // end if Alpha > 0

        }   // end of Render()

        public override void Activate(params object[] args)
        {
            base.Activate(args);
        }

        public override void Deactivate()
        {
            // Release keyboard focus if we have it.
            // Actually, since we never grab it we shouldn't have it but just to be safe...
            if (KoiLibrary.InputEventManager.KeyboardFocusObject == this)
            {
                KoiLibrary.InputEventManager.KeyboardFocusObject = null;
            }

            base.Deactivate();
        }

        public override void RegisterForInputEvents()
        {
            // Register for LeftDown so we can set cursor position.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftDown);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Tap);
            // Register to get keyboard input. 
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);          // Control keys.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.WinFormsKeyboard);  // Text.
        }

        public override void UnregisterForInputEvents()
        {
            KoiLibrary.InputEventManager.UnregisterForAllEvents(this);
        }

        #endregion

        #region InputEventHandler

        public override bool ProcessMouseLeftDownEvent(MouseInput input)
        {
            Debug.Assert(Active);

            // Was left down on us and has nobody else claimed focus?
            if (KoiLibrary.InputEventManager.MouseFocusObject == null && KoiLibrary.InputEventManager.MouseHitObject == this)
            {
                // Claim focus.
                KoiLibrary.InputEventManager.MouseFocusObject = this;

                // Register for up event.
                KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftUp);

                return true;
            }

            return false;
        }   // end of ProcessMouseLeftDownEvent()

        public override bool ProcessMouseLeftUpEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseFocusObject == this)
            {
                KoiLibrary.InputEventManager.MouseFocusObject = null;

                // Stop getting up events.
                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MouseLeftUp);

                // If mouse is still over us, set cursor position and set this widget to focused.
                if (KoiLibrary.InputEventManager.MouseHitObject == this)
                {
                    if(camera != null)
                    {
                        Vector2 pos = camera.ScreenToCamera(input.Position);
                        pos -= blobPosition;
                        blob.SetCursorToMousePosition(pos);
                    }

                    // Move focus to us.
                    SetFocus();
                }

                return true;
            }

            return false;
        }   // end of ProcessMouseLeftUpEvent()

        public override bool ProcessTouchTapEvent(TapGestureEventArgs gesture)
        {
            Debug.Assert(Active);

            // Did this gesture hit us?
            if (gesture.HitObject == this)
            {
                // Set cursor position if we can.
                if (camera != null)
                {
                    Vector2 pos = camera.ScreenToCamera(gesture.Position);
                    pos -= blobPosition;
                    blob.SetCursorToMousePosition(pos);
                }

                // Move focus to us.
                SetFocus();

                return true;
            }

            return base.ProcessTouchTapEvent(gesture);
        }

        public override bool ProcessWinFormsKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);

            if (InFocus)
            {
                switch(input.Key)
                {
                    default:
                        if (input.AsciiChar == 8)
                        {
                            return false;
                        }
                        else
                        {
                            char c = input.AsciiChar;
                            // Ignore control characters.
                            if (!char.IsControl(c))
                            {
                                if (numbersOnly && !char.IsDigit(c))
                                {
                                    Foley.PlayNoBudget();
                                }
                                else
                                {
                                    if(!blob.InsertString(input.AsciiChar.ToString()))
                                    {
                                        Foley.PlayNoBudget();
                                    }
                                    lastKeypressTime = Time.WallClockTotalSeconds;
                                    OnChange();
                                }
                            }
                        }
                        break;
                }

                return true;
            }

            return base.ProcessWinFormsKeyboardEvent(input);
        }   // end of ProcessWinFormsKeyboardEvent()

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);

            if (InFocus)
            {
                bool result = true;

                switch (input.Key)
                {
                    case Keys.Right:
                        blob.CursorRight();
                        break;
                    case Keys.Left:
                        blob.CursorLeft();
                        break;

                    case Keys.Up:
                        // Ignore for single line.
                        //blob.CursorUp();
                        return false;
                    case Keys.Down:
                        // Ignore for single line.
                        //blob.CursorDown();
                        return false;

                    case Keys.Back:
                        blob.Backspace();
                        lastKeypressTime = Time.WallClockTotalSeconds;
                        OnChange();
                        break;
                    case Keys.Delete:
                        blob.Delete();
                        lastKeypressTime = Time.WallClockTotalSeconds;
                        OnChange();
                        break;
                    case Keys.Home:
                        blob.Home();
                        break;
                    case Keys.End:
                        blob.End();
                        break;

                    case Keys.Enter:
                    case Keys.Escape:
                    case Keys.Tab:
                        // Do nothing.  These handled at dialog level.
                        //Debug.Assert(false, "Shouldn't the dialog get these first?");
                        return false;

                    default:
                        // Ignore all the "regular" keys since they get picked up by the other input handler.
                        break;
                }

                return result;
            }

            return base.ProcessKeyboardEvent(input);
        }   // end of ProcessKeyboardEvent()

        #endregion

        #region Internal

        public override void LoadContent()
        {
            if (searchIcon == null)
            {
                searchIcon = KoiLibrary.LoadTexture2D(@"Textures\UI2D\SearchIcon");
            }

            base.LoadContent();
        }

        public override void UnloadContent()
        {
            DeviceResetX.Release(ref searchIcon);

            base.UnloadContent();
        }

        #endregion

    }   // end of class SingleLineTextEditBox

}   // end of namespace KoiX.UI
