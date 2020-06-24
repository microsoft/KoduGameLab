
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Input;
using KoiX.Text;

using Boku.Base;
using Boku.Common;
using Boku.Input;
using Boku.SimWorld;
using Boku.UI;
using Boku.UI2D;
using Boku.Fx;

namespace Boku
{
    /// <summary>
    /// Text input dialog for allowing the user to name files.
    /// </summary>
    public class TextDialog : GameObject, INeedsDeviceReset
    {
        public class Shared
        {
            public TextDialog parent = null;

            public TextBlob descBlob = null;

            public Color textColor = Color.White;
            public Color shadowColor = Color.Black;

            public bool dirty = true;               // Does the texture need refreshing.

            public string curString = "blah bah blah";         // Current input string.
            public int cursorPosition = 0;          // Current cursor position.
                                                    // 0 is before the 1st character, 1 is between 
                                                    // the 1st and 2nd characters, etc.
            public string originalString = null;    // Used to allow Cancel to work.

            public int maxWidth;                    // Max allowed width of text string in pixels.

            static private char[] numbers = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' };

            // c'tor
            public Shared(TextDialog parent)
            {
                this.parent = parent;
            }   // end of Shared c'tor

            public string Prompt
            {
                get { return parent.Prompt; }
            }
            public void ResetString()
            {
                curString = originalString;
                cursorPosition = curString.Length;
            }

            public void IncrementString()
            {
                string text = curString.TrimEnd(numbers);
                string oldNum = curString.Substring(text.Length);
                UInt32 newNum = 0;
                if (oldNum.Length > 0)
                {
                    // Have a number, increment it.
                    try
                    {
                        newNum = UInt32.Parse(oldNum);
                        ++newNum;
                    }
                    catch
                    {
                        // oh well, we'll just use zero.
                    }
                }
                curString = text;
                if (!curString.EndsWith(" "))
                    curString += " ";
                curString += newNum.ToString();
                cursorPosition = curString.Length;
                dirty = true;
            }

        }   // end of class Shared

        protected class UpdateObj : UpdateObject
        {
            protected TextDialog parent = null;
            protected Shared shared = null;

            protected CommandMap commandMap = new CommandMap("TextDialog");

            // c'tor
            public UpdateObj(TextDialog parent, Shared shared)
            {
                this.parent = parent;
                this.shared = shared;
            }   // end of TextDialog UpdateObj c'tor

            public override void Update()
            {
                // Check if we have input focus.
                if (CommandStack.Peek() == commandMap)
                {
                    GamePadInput pad = GamePadInput.GetGamePad0();

                    if (pad.ButtonA.WasPressed && (0 != (parent.Buttons & TextDialogButtons.Accept)))
                    {
                        Accept();
                    }

                    if (pad.ButtonB.WasPressed && (0 != (parent.Buttons & TextDialogButtons.Cancel)))
                    {
                        Cancel();
                    }

                    if (pad.ButtonX.WasPressed && (0 != (parent.Buttons & TextDialogButtons.Discard)))
                    {
                        Discard();
                    }

                    if (pad.ButtonY.WasPressed)
                    {
                        shared.IncrementString();
                    }
                }

                parent.renderObj.RefreshTexture();

            }   // end of UpdateObj Update()

            public void TextInput(char c)
            {
                // Ignore backspace.  (side effect of new WinKeyboard code).
                if (c == 8)
                {
                    return;
                }

                // Ignore enter.  (side effect of new WinKeyboard code).
                if (c == 13)
                {
                    return;
                }

                shared.descBlob.InsertString(new string(c, 1));
                shared.curString = shared.descBlob.RawText;
            } 
            // TODO (****) Clean this up.  We're mixing text as input with text as control here.
            // Probably the right thing to do would be to push/pop the text input callbacks
            // dynamically to mirror the state we're in.

            public void newKeyInput(Keys key)
            {

                //if (shared.focus == Shared.InputFocus.Name || shared.focus == Shared.InputFocus.Creator)
                {
                    //
                    // Editing the level name.
                    //
                    bool changed = false;

                    KeyboardInputX.ClearAllWasPressedState(key);

                    switch (key)
                    {
                        case Keys.Enter:
                            Accept();
                            break;

                        case Keys.Escape:
                            Cancel();
                            break;

                        case Keys.Left:
                            if (shared.cursorPosition > 0)
                            {
                                shared.cursorPosition--;
                            }
                            break;
                        case Keys.Right:
                            if (shared.cursorPosition < shared.curString.Length)
                            {
                                shared.cursorPosition++;
                            }
                            break;

                        case Keys.Home:
                            shared.cursorPosition = 0;
                            break;

                        case Keys.End:
                            shared.cursorPosition = shared.curString.Length;
                            break;

                        case Keys.Back:
                            if (shared.curString.Length > 0 && shared.cursorPosition > 0)
                            {
                                shared.curString = shared.curString.Substring(0, shared.cursorPosition - 1) + shared.curString.Substring(shared.cursorPosition);
                                shared.cursorPosition--;
                                changed = true;
                            }
                            break;

                        case Keys.Delete:
                            if (shared.curString.Length > 0 && shared.cursorPosition < shared.curString.Length)
                            {
                                shared.curString = shared.curString.Substring(0, shared.cursorPosition) + shared.curString.Substring(shared.cursorPosition + 1);
                                changed = true;
                            }
                            break;

                    }   // end of switch on special characters.

                    if (changed)
                    {
                        shared.descBlob.RawText = shared.curString;
                        UpdateEditedString();
                    }
                }


            }   // end of UpdateObj KeyInput()

            private void UpdateEditedString()
            {

            }

            public void KeyInput(Keys key)
            {
                newKeyInput(key);
                return;

                //switch (key)
                //{
                //    case Keys.Enter:
                //        Accept();
                //        break;

                //    case Keys.Escape:
                //        Cancel();
                //        break;

                //    case Keys.Left:
                //        if (shared.cursorPosition > 0)
                //        {
                //            shared.cursorPosition--;
                //        }
                //        break;
                //    case Keys.Right:
                //        if (shared.cursorPosition < shared.curString.Length)
                //        {
                //            shared.cursorPosition++;
                //        }
                //        break;

                //    case Keys.Home:
                //        shared.cursorPosition = 0;
                //        break;

                //    case Keys.End:
                //        shared.cursorPosition = shared.curString.Length;
                //        break;

                //    case Keys.Back:
                //        if (shared.curString.Length > 0 && shared.cursorPosition > 0)
                //        {
                //            shared.curString = shared.curString.Substring(0, shared.cursorPosition - 1) + shared.curString.Substring(shared.cursorPosition);
                //            shared.cursorPosition--;
                //        }
                //        break;

                //    case Keys.Delete:
                //        if (shared.curString.Length > 0 && shared.cursorPosition < shared.curString.Length)
                //        {
                //            shared.curString = shared.curString.Substring(0, shared.cursorPosition) + shared.curString.Substring(shared.cursorPosition + 1);
                //        }
                //        break;

                //}
                //shared.dirty = true;
            }   // end of UpdateObj KeyInput()

            public void Accept()
            {
                parent.Button = TextDialogButtons.Accept;

                if (shared.curString.Length > 0)
                {
                    parent.Deactivate();
                }
                else
                {
                    shared.ResetString();
                }

                if (parent.OnButtonPressed != null)
                    parent.OnButtonPressed(parent);
            }

            public void Cancel()
            {
                parent.Button = TextDialogButtons.Cancel;

                // Restore the original string and exit.
                shared.ResetString();
                parent.Deactivate();

                if (parent.OnButtonPressed != null)
                    parent.OnButtonPressed(parent);
            }

            public void Discard()
            {
                parent.Button = TextDialogButtons.Discard;

                // Restore the original string and exit.
                shared.ResetString();
                parent.Deactivate();

                if (parent.OnButtonPressed != null)
                    parent.OnButtonPressed(parent);
            }

            public override void Activate()
            {
                CommandStack.Push(commandMap);

                shared.descBlob = new TextBlob(SharedX.GetGameFont24, shared.curString, 650);
                //shared.Justification = blob.justify;
                //KeyboardInputX.OnChar = TextInput;
#if NETFX_CORE
                Debug.Assert(false, "Does this work?  Why did we prefer winKeyboard?");
                KeyboardInputX.OnChar = TextInput;
#else
                BokuGame.bokuGame.winKeyboard.CharacterEntered = TextInput;
#endif
                //KeyboardInputX.OnKey = KeyInput;
            }

            public override void Deactivate()
            {
                CommandStack.Pop(commandMap);

                //KeyboardInputX.OnChar = null;
                //KeyboardInputX.OnKey = null;
#if !NETFX_CORE
                BokuGame.bokuGame.winKeyboard.CharacterEntered = null;
#endif
            }

        }   // end of class TextDialog UpdateObj  

        protected class RenderObj : RenderObject, INeedsDeviceReset
        {
            private Shared shared;

            private static Effect effect = null;

            private RenderTarget2D diffuse = null;
            private Texture2D background = null;      // Backgtround image.

            private float width = 512;      // Size of dialog in pixels.
            private float height = 256;

            private int margin = 24;        // Margin for text in pixels.

            public int backgroundWidth;             // Size of tile without power of 2 adjustments.
            public int backgroundHeight;

            private Vector2 pos;

            public RenderObj(Shared shared)
            {
                this.shared = shared;

                // Calc max width for text string.
                shared.maxWidth = (int)(width - 2.0f * margin);

                // Center box on screen.
                pos = BokuGame.ScreenSize;
                pos = (pos - new Vector2(width, height)) * 0.5f;

            }

            /// <summary>
            /// Rendering call.
            /// </summary>
            /// <param name="camera">Ignored.</param>
            public override void Render(Camera camera)
            {
                ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();

                ssquad.Render(diffuse, pos, new Vector2(width, height), @"TexturedRegularAlpha");

                shared.descBlob.RenderText(null, pos, shared.textColor);
            }   // end of Render()

            /// <summary>
            /// If the text being displayed has changed, we need to refresh the texture.
            /// Note this requires changing the rendertarget so this should no be called
            /// during the normal rendering loop.
            /// </summary>
            public void RefreshTexture()
            {
                if (shared.dirty)
                {
                    GraphicsDevice device = KoiLibrary.GraphicsDevice;

                    SpriteBatch batch = KoiLibrary.SpriteBatch;
                    GetFont Font20 = KoiX.SharedX.GetGameFont20;
                    GetFont Font24 = KoiX.SharedX.GetGameFont24;

                    InGame.SetRenderTarget(diffuse);
                    InGame.Clear(Color.Transparent);

                    TextBlob blob = new TextBlob(KoiX.SharedX.GetGameFont20, shared.Prompt, (int)(diffuse.Width - 2.0f * margin));

                    // Render the backdrop.
                    ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();
                    ssquad.Render(background, Vector2.Zero, new Vector2(512, 256), @"TexturedPreMultAlpha");

                    // Render the prompt.
                    if (shared.Prompt != null && shared.Prompt.Length > 0)
                    {
                        // Render the prompt text into the texture.
                        Vector2 promptPos = new Vector2(margin, margin);
                        int textWidth = (int)Font20().MeasureString(shared.Prompt).X;

                        blob.RenderText(null, promptPos, shared.textColor, shared.shadowColor, new Vector2(1, 1), maxLines: 3);

                        //TextHelper.DrawStringWithShadow(Font20, batch, x, y, shared.Prompt, shared.textColor, shared.shadowColor, false);
                    }


                    batch.Begin();

                    Vector2 glyphSize = new Vector2(64f, 64f);

                    // Pre-measure the button strip so we can align it.

                    int stripWidth = 0;
                    if (0 != (shared.parent.Buttons & TextDialogButtons.Accept))
                    {
                        string text = shared.parent.ButtonText(TextDialogButtons.Accept);
                        stripWidth += ((int)glyphSize.X * 7 / 6) + (int)Font20().MeasureString(text).X;
                    }
                    if (0 != (shared.parent.Buttons & TextDialogButtons.Discard))
                    {
                        string text = shared.parent.ButtonText(TextDialogButtons.Discard);
                        stripWidth += ((int)glyphSize.X * 7 / 6) + (int)Font20().MeasureString(text).X;
                    }
                    if (0 != (shared.parent.Buttons & TextDialogButtons.Cancel))
                    {
                        string text = shared.parent.ButtonText(TextDialogButtons.Cancel);
                        stripWidth += ((int)glyphSize.X * 7 / 6) + (int)Font20().MeasureString(text).X;
                    }

                    // Render the buttons and the text that goes with them.

                    Point position = new Point();
                    position.X = (backgroundWidth - stripWidth) / 2;
                    position.Y = backgroundHeight - margin - 40;
                    ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();

                    if (0 != (shared.parent.Buttons & TextDialogButtons.Accept))
                    {
                        string text = shared.parent.ButtonText(TextDialogButtons.Accept);
                        quad.Render(ButtonTextures.AButton, new Vector2(position.X, position.Y - 2), glyphSize, @"TexturedRegularAlpha");
                        position.X += (int)glyphSize.X * 2 / 3;
                        TextHelper.DrawStringWithShadow(Font20, batch, position.X, position.Y, text, Color.White, Color.Black, false);
                        position.X += (int)glyphSize.X / 2 + (int)Font20().MeasureString(text).X;
                    }

                    if (0 != (shared.parent.Buttons & TextDialogButtons.Discard))
                    {
                        string text = shared.parent.ButtonText(TextDialogButtons.Discard);
                        quad.Render(ButtonTextures.XButton, new Vector2(position.X, position.Y - 2), glyphSize, @"TexturedRegularAlpha");
                        position.X += (int)glyphSize.X * 2 / 3;
                        TextHelper.DrawStringWithShadow(Font20, batch, position.X, position.Y, text, Color.White, Color.Black, false);
                        position.X += (int)glyphSize.X / 2 + (int)Font20().MeasureString(text).X;
                    }

                    if (0 != (shared.parent.Buttons & TextDialogButtons.Cancel))
                    {
                        string text = shared.parent.ButtonText(TextDialogButtons.Cancel);
                        quad.Render(ButtonTextures.BButton, new Vector2(position.X, position.Y - 2), glyphSize, @"TexturedRegularAlpha");
                        position.X += (int)glyphSize.X * 2 / 3;
                        TextHelper.DrawStringWithShadow(Font20, batch, position.X, position.Y, text, Color.White, Color.Black, false);
                        position.X += (int)glyphSize.X / 2 + (int)Font20().MeasureString(text).X;
                    }

                    // Render the user text.
                    // Position user text in the vertical center of the tile.
                    position.X = margin;
                    position.Y = margin + blob.NumLines * blob.TotalSpacing;
                    position.Y += (backgroundHeight - position.Y) / 2 - Font24().LineSpacing - Font24().LineSpacing / 2;

                    // Calc the cursor position.
                    string tmpText = shared.curString.Substring(0, shared.cursorPosition);
                    int width = (int)Font24().MeasureString(tmpText).X;

                    float cursorHeight = Font24().LineSpacing + 4.0f;
                    Vector2 cursorTop = new Vector2(position.X + width, position.Y);
                    Vector2 cursorBottom = cursorTop;
                    cursorBottom.Y += cursorHeight;

                    // Render the user text with cursor.
                    Vector2 pos = new Vector2(position.X, position.Y);
                    TextHelper.DrawString(Font24, shared.curString, pos, Color.Black);
                    Utils.Draw2DLine(cursorTop, cursorBottom, Color.Black.ToVector4());
                    pos += new Vector2(1, -1);
                    TextHelper.DrawString(Font24, shared.curString, pos, Color.White);
                    cursorTop += new Vector2(1.0f, -1.0f);
                    cursorBottom += new Vector2(1.0f, -1.0f);
                    Utils.Draw2DLine(cursorTop, cursorBottom, Color.White.ToVector4());

                    batch.End();

                    // Restore backbuffer.
                    InGame.RestoreRenderTarget();

                    //shared.dirty = false;
                }
            }   // end of TextDialog RenderObj RefreshTexture()

            public override void Activate()
            {
            }

            public override void Deactivate()
            {
            }


            public void LoadContent(bool immediate)
            {
                // Init the effect.
                if (effect == null)
                {
                    effect = KoiLibrary.LoadEffect(@"Shaders\UI2D");
                    ShaderGlobals.RegisterEffect("UI2D", effect);
                }

                // Load the background texture.
                if (background == null)
                {
                    background = KoiLibrary.LoadTexture2D(@"Textures\MessageBox\TextDialogBackground");
                }

            }   // end of TextDialog RenderObj LoadContent();

            public void InitDeviceResources(GraphicsDevice device)
            {
                CreateRenderTargets(device);

            }   // end of InitDeviceResources()

            public void UnloadContent()
            {
                ReleaseRenderTargets();
                DeviceResetX.Release(ref effect);
                DeviceResetX.Release(ref background);
            }

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
                // TODO (****) This could probably work just by saying dirty = true
                // With XNA4 rendertargets seem to survive, they just lose their content.
                ReleaseRenderTargets();
                CreateRenderTargets(device);
            }

            private void CreateRenderTargets(GraphicsDevice device)
            {
                int w = (int)width;
                int h = (int)height;

                // Create the diffuse texture.
                backgroundWidth = w;
                backgroundHeight = h;
                if (BokuGame.RequiresPowerOf2)
                {
                    w = MyMath.GetNextPowerOfTwo(w);
                    h = MyMath.GetNextPowerOfTwo(h);
                }
                if (diffuse == null)
                {
                    diffuse = new RenderTarget2D(device,
                        w, h,
                        false,                      // Mipmaps
                        SurfaceFormat.Color,
                        DepthFormat.None);
                    SharedX.GetRT("TextDialog", diffuse);
                }

                shared.dirty = true;
            }

            private void ReleaseRenderTargets()
            {
                SharedX.RelRT("TextDialog", diffuse);
                DeviceResetX.Release(ref diffuse);
            }

        }   // end of class TextDialog RenderObj     


        // delegates
        public delegate void TextDialogButtonHandler(TextDialog dialog);
        public TextDialogButtonHandler OnButtonPressed;

        // List objects.
        public Shared shared = null;
        private RenderObj renderObj = null;
        private UpdateObj updateObj = null;
        private string prompt;

        private enum States
        {
            Inactive,
            Active,
        }
        private States state = States.Inactive;
        private States pendingState = States.Inactive;

        [Flags]
        public enum TextDialogButtons
        {
            None = 0,
            Accept = 1 << 0,
            Cancel = 1 << 1,
            Discard = 1 << 2,
            SIZEOF = 3
        }

        private TextDialogButtons buttons;
        private string[] buttonText = new string[(int)TextDialogButtons.SIZEOF];

        private TextDialogButtons button = TextDialogButtons.None;

        #region Accessors

        /// <summary>
        /// The button the user pressed
        /// </summary>
        public TextDialogButtons Button
        {
            get { return button; }
            set { button = value; }
        }
        public bool Active
        {
            get { return state == States.Active; }
        }
        /// <summary>
        /// Totally bogus call that let's you check the pending state.
        /// Needed because of the delay between having Activate() called
        /// and the state actually showing up as active.
        /// </summary>
        public bool PendingActive
        {
            get { return pendingState == States.Active; }
        }

        /// <summary>
        /// Totally bogus call that let's you check the pending state.
        /// Needed because of the delay between having Activate() called
        /// and the state actually showing up as active.
        /// </summary>
        public bool Focused
        {
            get { return Active || PendingActive; }
        }

        /// <summary>
        /// The text string being edited.  May be set to a value to "prefill" the dialog.
        /// </summary>
        public string UserText
        {
            get { return shared.curString; }
            set 
            { 
                shared.curString = value;
                shared.originalString = value;
                // Position the cursor at the end of the current string.
                shared.cursorPosition = shared.curString.Length;
            }
        }
        /// <summary>
        /// The label on the dialog.
        /// </summary>
        public string Prompt
        {
            get { return prompt; }
            set { prompt = value; }
        }

        /// <summary>
        /// The text foreground color.
        /// </summary>
        public Color TextColor
        {
            get { return shared.textColor; }
            set { shared.textColor = value; }
        }
        
        /// <summary>
        /// The shadow color for the text.
        /// </summary>
        public Color ShadowColor
        {
            get { return shared.shadowColor; }
            set { shared.shadowColor = value; }
        }
        
        public TextDialogButtons Buttons
        {
            get { return buttons; }
        }
        
        #endregion 

        #region Public


        // c'tor
        public TextDialog(Color color, TextDialogButtons buttons)
        {
            this.buttons = buttons;

            // Create sub objects
            shared = new Shared(this);

            // Create the RenderObject and UpdateObject parts of this mode.
            updateObj = new UpdateObj(this, shared);
            renderObj = new RenderObj(shared);

            // Set default button labels
            SetButtonText(TextDialogButtons.Accept, Strings.Localize("textDialog.accept"));
            SetButtonText(TextDialogButtons.Cancel, Strings.Localize("textDialog.cancel"));
            SetButtonText(TextDialogButtons.Discard, Strings.Localize("textDialog.discard"));
        }   // end of TextDialog c'tor

        public string ButtonText(TextDialogButtons button)
        {
            return buttonText[MyMath.HighBitPos((int)button)];
        }

        public void SetButtonText(TextDialogButtons button, string text)
        {
            buttonText[MyMath.HighBitPos((int)button)] = text;
            shared.dirty = true;
        }

        #endregion

        #region Internal

        public override bool Refresh(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;

            if (state != pendingState)
            {
                if (pendingState == States.Active)
                {
                    updateList.Add(updateObj);
                    updateObj.Activate();
                    renderList.Add(renderObj);
                    renderObj.Activate();
                }
                else
                {
                    renderObj.Deactivate();
                    renderList.Remove(renderObj);
                    updateObj.Deactivate();
                    updateList.Remove(updateObj);
                }

                state = pendingState;
            }

            return result;
        }

        override public void Activate()
        {
            if (state != States.Active)
            {
                pendingState = States.Active;
                BokuGame.objectListDirty = true;

                // There should be no entry for "TextDialog" so this effectively 
                // disables the help overlay while the dialog is active.
                HelpOverlay.Push("TextDialog");

                // Force the texture to be re-rendered.
                shared.dirty = true;
            }
        }

        override public void Deactivate()
        {
            if (state != States.Inactive)
            {
                HelpOverlay.Pop();

                pendingState = States.Inactive;
                BokuGame.objectListDirty = true;
            }
        }


        public void LoadContent(bool immediate)
        {
            BokuGame.Load(renderObj, immediate);

        }   // end of TextDialog LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
        }

        public void UnloadContent()
        {
            BokuGame.Unload(renderObj);
        }   // end of TextDialog UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
            BokuGame.DeviceReset(renderObj, device);
        }

        #endregion

    }   // end of class TextDialog

}   // end of namespace Boku

