
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
using KoiX.Input;
using KoiX.Text;


namespace KoiX.UI
{
    public partial class Button : BaseButton
    {
        #region Members

        UIState prevCombinedState = UIState.Inactive;   // Note that this is used to detect changes needed for rendering 
                                                        // so it includes flags for selected, focused, and hover as needed.

        protected Label label;
        GamePadInput.Element element = GamePadInput.Element.None;   // Which gamepad button, if any, this Button is bound to.
        Texture2D gamePadTexture;   // If null, doesn't display any different
                                    // when input mode is gamepad.

        ButtonTheme curTheme;       // Colors and sizes for current button state.

        protected SystemFont font;

        // Properties for rendering widget.  Non-underscore version is the twitched
        // version which is used for rendering each frame.  Underscore version is 
        // the value we're twitching toward.

        Twitchable<Color> bodyColor;
        Twitchable<Color> outlineColor;
        Twitchable<float> outlineWidth;
        Twitchable<float> cornerRadius;

        Twitchable<Color> textColor;
        Twitchable<Color> textOutlineColor;
        Twitchable<float> textOutlineWidth;

        ShadowStyle shadow = ShadowStyle.None;
        Twitchable<float> shadowSize;
        Twitchable<Vector2> shadowOffset;
        Twitchable<float> shadowAlpha;

        #endregion

        #region Accessors

        public Color BodyColor
        {
            get { return bodyColor.Value; }
            set { bodyColor.Value = value; }
        }

        public Color _BodyColor
        {
            get { return bodyColor.TargetValue; }
            set { bodyColor.TargetValue = value; }
        }

        public Color OutlineColor
        {
            get { return outlineColor.Value; }
            set { outlineColor.Value = value; }
        }

        public Color _OutlineColor
        {
            get { return outlineColor.TargetValue; }
            set { outlineColor.TargetValue = value; }
        }

        public float OutlineWidth
        {
            get { return outlineWidth.Value; }
            set { outlineWidth.Value = value; }
        }

        public float _OutlineWidth
        {
            get { return outlineWidth.TargetValue; }
            set { outlineWidth.TargetValue = value; }
        }

        public Color TextColor
        {
            get { return textColor.Value; }
            set { textColor.Value = value; }
        }

        public Color _TextColor
        {
            get { return textColor.TargetValue; }
            set { textColor.TargetValue = value; }
        }

        public Color TextOutlineColor
        {
            get { return textOutlineColor.Value; }
            set { textOutlineColor.Value = value; }
        }

        public Color _TextOutlineColor
        {
            get { return textOutlineColor.TargetValue; }
            set { textOutlineColor.TargetValue = value; }
        }

        public float TextOutlineWidth
        {
            get { return textOutlineWidth.Value; }
            set { textOutlineWidth.Value = value; }
        }

        public float _TextOutlineWidth
        {
            get { return textOutlineWidth.TargetValue; }
            set { textOutlineWidth.TargetValue = value; }
        }

        public string FontFamily
        {
            get { return curTheme.FontFamily; }
            set 
            {
                if (curTheme.FontFamily != value)
                {
                    curTheme.FontFamily = value;
                    font = SysFont.GetSystemFont(curTheme.FontFamily, curTheme.FontSize, curTheme.FontStyle);
                    dirty = true;
                }
            }
        }

        public System.Drawing.FontStyle FontStyle
        {
            get { return curTheme.FontStyle; }
            set
            {
                if (curTheme.FontStyle != value)
                {
                    curTheme.FontStyle = value;
                    font = SysFont.GetSystemFont(curTheme.FontFamily, curTheme.FontSize, curTheme.FontStyle);
                    dirty = true;
                }
            }
        }

        public float FontSize
        {
            get { return curTheme.FontSize; }
            set
            {
                if (curTheme.FontSize != value)
                {
                    curTheme.FontSize = value;
                }
            }
        }

        public float CornerRadius
        {
            get { return cornerRadius.Value; }
            set { cornerRadius.Value = value; }
        }

        public float _CornerRadius
        {
            get { return cornerRadius.TargetValue; }
            set { cornerRadius.TargetValue = value; }
        }

        public ShadowStyle Shadow
        {
            get { return shadow; }
            set { shadow = value; }
        }

        public Vector2 ShadowOffset
        {
            get { return shadowOffset.Value; }
            set { shadowOffset.Value = value; }
        }

        public Vector2 _ShadowOffset
        {
            get { return shadowOffset.TargetValue; }
            set { shadowOffset.TargetValue = value; }
        }

        public float ShadowSize
        {
            get { return shadowSize.Value; }
            set { shadowSize.Value = value; }
        }

        public float _ShadowSize
        {
            get { return shadowSize.TargetValue; }
            set { shadowSize.TargetValue = value; }
        }

        public float ShadowAlpha
        {
            get { return shadowAlpha.Value; }
            set { shadowAlpha.Value = value; }
        }

        public float _ShadowAlpha
        {
            get { return shadowAlpha.TargetValue; }
            set { shadowAlpha.TargetValue = value; }
        }

        public string LabelId
        {
            get { return label.LabelId; }
            set { label.LabelId = value; }
        }

        public Label Label
        {
            get { return label; }
        }

        public SystemFont Font
        {
            get { return font; }
        }

        #endregion

        #region Public

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentDialog"></param>
        /// <param name="rect">The overall size of the button.  Does not change.  If the label is too long to fit it is compressed.  If either dimension is 0 then calc minSize and use that.</param>
        /// <param name="labelId"></param>
        /// <param name="labelText"></param>
        /// <param name="OnChange"></param>
        /// <param name="element"></param>
        /// <param name="theme"></param>
        public Button(BaseDialog parentDialog, RectangleF rect, string labelId = null, string labelText = null, GamePadInput.Element element = GamePadInput.Element.None, Callback OnChange = null, ThemeSet theme = null)
            : base(parentDialog, OnChange: OnChange, theme: theme)
        {
            Debug.Assert(labelId == null || labelText == null, "Only one should be set.");
            
            this.localRect = rect;
            this.element = element;

            if (theme == null)
            {
                theme = Theme.CurrentThemeSet;
            }

            prevCombinedState = UIState.Inactive;
            curTheme = theme.ButtonNormal.Clone() as ButtonTheme;

            font = SysFont.GetSystemFont(curTheme.FontFamily, curTheme.FontSize, curTheme.FontStyle);

            // Create all the Twitchables and set initial values.
            bodyColor = new Twitchable<Color>(Theme.TwitchTime, TwitchCurve.Shape.EaseInOut, parent: this, startingValue: curTheme.BodyColor);
            outlineColor = new Twitchable<Color>(Theme.TwitchTime, TwitchCurve.Shape.EaseInOut, parent: this, startingValue: curTheme.OutlineColor);
            outlineWidth = new Twitchable<float>(Theme.TwitchTime, TwitchCurve.Shape.EaseInOut, parent: this, startingValue: curTheme.OutlineWidth);

            textColor = new Twitchable<Color>(Theme.TwitchTime, TwitchCurve.Shape.EaseInOut, parent: this, startingValue: curTheme.TextColor);
            textOutlineColor = new Twitchable<Color>(Theme.TwitchTime, TwitchCurve.Shape.EaseInOut, parent: this, startingValue: curTheme.TextOutlineColor);
            textOutlineWidth = new Twitchable<float>(Theme.TwitchTime, TwitchCurve.Shape.EaseInOut, parent: this, startingValue: curTheme.TextOutlineWidth);

            cornerRadius = new Twitchable<float>(Theme.TwitchTime, TwitchCurve.Shape.EaseInOut, parent: this, startingValue: curTheme.CornerRadius);

            shadow = theme.ButtonNormal.Shadow;
            shadowOffset = new Twitchable<Vector2>(Theme.TwitchTime, TwitchCurve.Shape.EaseInOut, parent: this, startingValue: curTheme.ShadowOffset);
            shadowSize = new Twitchable<float>(Theme.TwitchTime, TwitchCurve.Shape.EaseInOut, parent: this, startingValue: curTheme.ShadowSize);
            shadowAlpha = new Twitchable<float>(Theme.TwitchTime, TwitchCurve.Shape.EaseInOut, parent: this, startingValue: curTheme.ShadowAlpha);

            label = new Label(ParentDialog, font, TextColor, outlineColor: TextOutlineColor, outlineWidth: TextOutlineWidth, labelId: labelId, labelText: labelText);
            label.Padding = new Padding((int)(2 * CornerRadius), 0, (int)(2 * CornerRadius), 0);    // Set padding to keep label from overlapping ends.
            label.Size = label.CalcMinSize();
            
            // Set our size to not force any squishing of the label text.  Use rect as minimum size.
            Size = MyMath.Max(rect.Size, CalcMinSize());

            Recalc(Vector2.Zero);

        }   // end of c'tor

        public override void Recalc(Vector2 parentPosition)
        {
            this.parentPosition = parentPosition;

            base.Recalc(parentPosition);

        }   // end of Recalc()

        public override void Update(SpriteCamera camera, Vector2 parentPosition)
        {
            // Needed to handle focus changes.
            base.Update(camera, parentPosition);

            UpdateState();

            // Did we change input devices?  If so, mark things
            // as dirt so we can reflow/resize them.
            if (KoiLibrary.LastTouchedDeviceChanged)
            {
                dirty = true;
            }

            if (Dirty)
            {
                if (label != null)
                {
                    label.Dirty = true;
                }

                Recalc(parentPosition);
            }

            // We handle the twitching of the text color here in Button so just set it on the label with no Twitch.
            if (label != null)
            {
                // Have label match button's selected state.
                label.Selected = Selected;

                label.TextColor.Value = textColor.TargetValue;
                label.TextOutlineColor.Value = textOutlineColor.TargetValue;
                label.TextOutlineWidth.Value = textOutlineWidth.TargetValue;
                label.Update(camera, parentPosition);
            }

        }   // end of Update()

        /// <summary>
        /// Based on state changes, we need to make changes to the themeSet we're using.
        /// </summary>
        void UpdateState()
        {
            UIState combinedState = CombinedState;
            if (combinedState != prevCombinedState)
            {
                // Set new state params.  Note that dirty flag gets
                // set internally by setting individual values so
                // we don't need to worry about it here.
                switch (combinedState)
                {
                    case UIState.Disabled:
                    case UIState.DisabledSelected:
                        curTheme = theme.ButtonDisabled;
                        break;

                    case UIState.Active:
                        curTheme = theme.ButtonNormal;
                        break;

                    case UIState.ActiveFocused:
                        curTheme = theme.ButtonNormalFocused;
                        break;

                    case UIState.ActiveHover:
                        curTheme = theme.ButtonNormalHover;
                        break;

                    case UIState.ActiveFocusedHover:
                        curTheme = theme.ButtonNormalFocusedHover;
                        break;

                    case UIState.ActiveSelected:
                        curTheme = theme.ButtonSelected;
                        break;

                    case UIState.ActiveSelectedFocused:
                        curTheme = theme.ButtonSelectedFocused;
                        break;

                    case UIState.ActiveSelectedHover:
                        curTheme = theme.ButtonSelectedHover;
                        break;

                    case UIState.ActiveSelectedFocusedHover:
                        curTheme = theme.ButtonSelectedFocusedHover;
                        break;

                    default:
                        // Should only happen on state.None
                        break;

                }   // end of switch

                // Now that we have the new theme, set all the Twitchable values from it.
                // Non-twitchable values we get directly from the theme.
                bodyColor.Value = curTheme.BodyColor;
                cornerRadius.Value = curTheme.CornerRadius;
                outlineColor.Value = curTheme.OutlineColor;
                outlineWidth.Value = curTheme.OutlineWidth;

                shadowOffset.Value = curTheme.ShadowOffset;
                shadowSize.Value = curTheme.ShadowSize;
                shadowAlpha.Value = curTheme.ShadowAlpha;

                textColor.Value = curTheme.TextColor;
                textOutlineColor.Value = curTheme.TextOutlineColor;
                textOutlineWidth.Value = curTheme.TextOutlineWidth;

                prevCombinedState = combinedState;
                dirty = true;
            }
        }   // end of UpdateState()

        public override void Render(SpriteCamera camera, Vector2 parentPosition)
        {
            if (Alpha > 0)
            {
                Vector2 pos = Position + parentPosition;

                // Ensure the gamePadTexture is good.
                if (gamePadTexture == null || gamePadTexture.IsDisposed || gamePadTexture.GraphicsDevice.IsDisposed)
                {
                    gamePadTexture = Textures.Get(element);
                }

                if (KoiLibrary.LastTouchedDeviceIsGamepad && gamePadTexture != null)
                {
                    // Render appropriate for gamepad input.
                    // TODO (****) Also do a version for R-to-L languages.

                    SpriteBatch batch = KoiLibrary.SpriteBatch;
                    // The 0.875 is just a scaling of 7/8 since that looks more like a visual match for the regular button.
                    Rectangle destRect = new Rectangle((int)pos.X, (int)pos.Y, (int)(localRect.Height * 0.875f), (int)(localRect.Height * 0.875f));

                    batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, samplerState: null, depthStencilState: null, rasterizerState: null, effect: null, transformMatrix: camera.ViewMatrix);
                    {
                        batch.Draw(gamePadTexture, destRect, Color.White * Alpha);
                    }
                    batch.End();

                    pos.X = destRect.Left;
                    label.HorizontalJustification = Justification.Left;
                    // Add text label for gamepad icon.  Rect width for text is full button width minus icon width.
                    RectangleF labelRect = label.LocalRect;
                    labelRect.Width = LocalRect.Width - destRect.Width;
                    label.LocalRect = labelRect;
                    label.Render(camera, pos + new Vector2((int)(localRect.Height * 0.75f), 0));
                }
                else
                {
                    Vector2 minSize = CalcMinSize();
                    if (minSize.X != LocalRect.Size.X)
                    {
                    }

                    // Render for mouse.
                    RoundedRect.Render(camera, pos, LocalRect.Size, CornerRadius, BodyColor * Alpha,
                                        outlineWidth: OutlineWidth, outlineColor: OutlineColor * Alpha,
                                        shadowStyle: shadow, shadowOffset: ShadowOffset, shadowSize: ShadowSize,
                                        bevelStyle: curTheme.BevelStyle, bevelWidth: CornerRadius);
                    if (label != null)
                    {
                        // Match label size to button size.
                        label.Size = Size;
                        label.HorizontalJustification = Justification.Center;
                        label.Render(camera, pos);
                    }
                }
            }   // end if Alpha > 0.

            base.Render(camera, parentPosition);
        }   // end of Render()

        public override void Activate(params object[] args)
        {
            if (label != null)
            {
                label.Activate(args);
            }

            base.Activate(args);
        }   // end of Activate()

        public override void Deactivate()
        {
            if (label != null)
            {
                label.Deactivate();
            }

            // Force any state change to be applied.  This turns out to be
            // useful for buttons on dialogs that pop up a lot (help, etc.)
            // If not here, when the dialog displays, the button then starts
            // transitioning from Selected to Normal and we see the color change.
            //
            // TODO (****) Would it be better to instantly force the proper theme 
            // settings in Activate?  Need to be sure not to mess up latchable
            // buttons.  (Do we expect these to be able to keep state over an
            // Activate/Deactivate cycle?)
            UpdateState();

            base.Deactivate();
        }   // end of Deactivate()

        /// <summary>
        /// Returns the min size of the button based on the label.
        /// Does not account for margin around button.
        /// </summary>
        /// <returns></returns>
        public override Vector2 CalcMinSize()
        {
            // Start with size of text.
            Vector2 size = label.CalcMinSize();

            // Add any padding.
            size = size.Ceiling();
            size.X += Padding.Horizontal;
            size.Y += Padding.Vertical;

            // If we have a valid element, add in the size of the element.
            if (element != GamePadInput.Element.None)
            {
                size.X += size.Y * 0.875f;
            }

            return size;
        }   // end of CalcMinSize()

        #endregion

        #region InputEventHandler

        public override void RegisterForInputEvents()
        {
            // Call base register first.  By putting the child widgets on the input stacks
            // first we can then put oursleves on and have priority.
            base.RegisterForInputEvents();

            // BaseButton already responds to A if the button is in focus, but if we also
            // specify a gamepad element, we want to be able to respond to that even when
            // not in focus.
            if (element != GamePadInput.Element.None)
            {
                KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.GamePad);
            }

        }   // end of RegisterForInputEvents()

        public override bool ProcessGamePadEvent(GamePadInput pad)
        {
            Debug.Assert(Active);

            if (element != GamePadInput.Element.None)
            {
                GamePadInput.Button button = pad.GetButton(element);
                if (button != null && button.WasPressed)
                {
                    OnButtonSelect();
                    return true;
                }
            }

            return base.ProcessGamePadEvent(pad);
        }

        #endregion


        #region Internal
        #endregion


    }   // end of class Button

}   // end of namespace KoiX.UI
