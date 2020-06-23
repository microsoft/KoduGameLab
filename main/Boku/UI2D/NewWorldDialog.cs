
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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
    /// <summary>
    /// Dialog which lets user choose among multiple "new worlds".
    /// </summary>
    public class NewWorldDialog : INeedsDeviceReset
    {
        public class NewWorldLevel
        {
            public string LevelGuid;
            public string ThumbnailName;
            public Texture2D ThumbnailTexture;
            public Color FrameColor = Color.Transparent;
            public AABB2D Box;

            public Rectangle Rectangle 
            {
                get { return Box.Rectangle; }
            }

            public NewWorldLevel(string LevelGuid)
            {
                this.LevelGuid = LevelGuid;
                ThumbnailName = Path.Combine(@"Content\Xml\Levels\BuiltInWorlds", LevelGuid +".dds");

                Box = new AABB2D();

            }   // end of c'tor
        }

        public delegate void OnAction(string level);

        #region Members

        private const int margin = 32;

        private Texture2D titleBarTexture;
        private Texture2D dialogBodyTexture;

        private Rectangle titleRect;
        private Rectangle dialogBodyRect;

        private Button cancelButton;
        private OnAction CancelDelegate;

        private OnAction SelectWorldDelegate;

        private List<NewWorldLevel> levels = new List<NewWorldLevel>();
        private int focusLevelIndex = 0;

        private TextBlob blob;

        private KeyboardInput.KeyboardKeyEvent prevOnKey;
        private KeyboardInput.KeyboardCharEvent prevOnChar;
        private KeyboardInput.KeyboardCharEvent prevTextInput;

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
                    active = value;

                    if (active)
                    {
                        // Save away existing keyboard event handlers.  We need this in case
                        // this dialog is launched over some other dialog which uses keyboard
                        // input like the SaveLevelDialog.
                        prevOnChar = KeyboardInput.OnChar;
                        prevOnKey = KeyboardInput.OnKey;
                        prevTextInput = BokuGame.bokuGame.winKeyboard.CharacterEntered;

                        // Now that we've saved them.  Null them out.
                        KeyboardInput.OnKey = null;
                        BokuGame.bokuGame.winKeyboard.CharacterEntered = null;
                    }
                    else
                    {
                        // Restore keyboard event handlers.
                        KeyboardInput.OnKey = prevOnKey;
                        KeyboardInput.OnChar = prevOnChar;
                        BokuGame.bokuGame.winKeyboard.CharacterEntered = prevTextInput;
                    }
                }
            }
        }

        #endregion

        #region Public

        public NewWorldDialog(OnAction OnSelectWorld, OnAction OnCancel)
        {
            SelectWorldDelegate = OnSelectWorld;
            CancelDelegate = OnCancel;

            blob = new TextBlob(UI2D.Shared.GetGameFont20, "", 400);

            cancelButton = new Button(Strings.Localize("auth.cancel"), Color.White, null, UI2D.Shared.GetGameFont20);

            levels.Add(new NewWorldLevel(@"03a1b038-fd3f-492f-b18c-2a197fe68701"));
            levels.Add(new NewWorldLevel(@"71b3660d-f472-49b2-90c3-2de1758e1f64"));
            levels.Add(new NewWorldLevel(@"be4ca04b-a3cc-4f76-ba7a-84eb917bcf92")); 

        }   // end of c'tor

        public void Update()
        {
            if (active)
            {
                //
                // Input?
                //

                // Touch.
                for (int i = 0; i < TouchInput.TouchCount; i++)
                {
                    TouchContact touch = TouchInput.GetTouchContactByIndex(i);

                    Vector2 touchHit = ScreenWarp.ScreenToRT(touch.position);
                    HandleTouchInput(touch, touchHit);
                }

                // Mouse.  Note, we explicitely look for mouse mode here so that
                // mouse hover doesn't change the focus level when changing it via
                // keyboard or gamepad.
                if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                {
                    Vector2 mouseHit = MouseInput.GetMouseInRtCoords();
                    HandleMouseInput(mouseHit);
                }

                // Keyboard.
                // Arrows are handled via Actions in HandleGamePadInput().
                if (KeyboardInput.WasPressed(Keys.Escape))
                {
                    OnCancel();
                }
                if (KeyboardInput.WasPressed(Keys.Enter))
                {
                    OnSelect();
                }

                // Gamepad.
                HandleGamepadInput();

                // Set frame color to indicate focus.
                for (int i = 0; i < levels.Count; i++)
                {
                    if (focusLevelIndex == i)
                    {
                        levels[i].FrameColor = new Color(0, 255, 13);
                    }
                    else
                    {
                        levels[i].FrameColor = Color.Transparent;
                    }
                }

            }   // end if active.    

        }   // end of Update()

        private void HandleTouchInput(TouchContact touch, Vector2 hit)
        {
            if (cancelButton.Box.Touched(touch, hit))
            {
                Boku.Common.Gesture.TouchGestureManager.Get().TapGesture.ClearWasTapped();
                OnCancel();
            }

            for(int i=0; i<levels.Count; i++)
            {
                if (levels[i].Box.Touched(touch, hit))
                {
                    Boku.Common.Gesture.TouchGestureManager.Get().TapGesture.ClearWasTapped();
                    focusLevelIndex = i;
                    OnSelect();
                }
            }

        }   // end of HandleTouchInput()

        private void HandleMouseInput(Vector2 hit)
        {
            if (cancelButton.Box.LeftPressed(hit))
            {
                OnCancel();
            }

            // Update hover state.
            cancelButton.SetHoverState(hit);

            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i].Box.Contains(hit))
                {
                    focusLevelIndex = i;

                    if (MouseInput.Left.WasPressed)
                    {
                        OnSelect();
                    }
                }
            }

        }   // end of HandleMouseInput()

        private void HandleGamepadInput()
        {
            GamePadInput pad = GamePadInput.GetGamePad0();

            // Select a level.
            if (pad.ButtonA.WasPressed)
            {
                OnSelect();
            }

            // Cancel.
            if (pad.Back.WasPressed)
            {
                OnCancel();
            }

            // Cycle through worlds.
            if (Actions.ComboRight.WasPressedOrRepeat)
            {
                focusLevelIndex = (focusLevelIndex + 1) % levels.Count;
            }
            if (Actions.ComboLeft.WasPressedOrRepeat)
            {
                focusLevelIndex = (focusLevelIndex + levels.Count - 1) % levels.Count;
            }

        }   // end of HandleGamepadInput()

        private void OnSelect()
        {
            Active = false;

            if (SelectWorldDelegate != null)
            {
                SelectWorldDelegate(levels[focusLevelIndex].LevelGuid);
            }
        }   // end of OnSelect()

        private void OnCancel()
        {
            // Just go away!
            Active = false;

            if (CancelDelegate != null)
            {
                CancelDelegate(null);
            }

        }   // end of OnCancel()

        /// <summary>
        /// 
        /// </summary>
        /// <param name="screenSize">If rendering to an RT allows us to know how to center.</param>
        public void Render(Vector2 screenSize)
        {
            if (active)
            {
                titleRect = new Rectangle(0, 0, 512, 72);
                dialogBodyRect = new Rectangle(0, 64, 512, 268);

                // Now that we have the final dialog size, center it on the screen.
                Vector2 pos = screenSize / 2.0f;
                pos.X -= titleRect.Width / 2;
                pos.Y -= (titleRect.Height + dialogBodyRect.Height) / 2;
                titleRect.X = (int)pos.X;
                titleRect.Y = (int)pos.Y;
                dialogBodyRect.X = titleRect.X;
                dialogBodyRect.Y = titleRect.Y + titleRect.Height;

                AuthUI.RenderTile(titleBarTexture, titleRect);
                AuthUI.RenderTile(dialogBodyTexture, dialogBodyRect);

                // Title bar text.
                string str = Strings.Localize("miniHub.emptyLevel");
                blob.RawText = str;
                blob.Font = UI2D.Shared.GetGameFont30Bold;
                blob.Justification = UIGridElement.Justification.Left;
                blob.RenderWithButtons(new Vector2(titleRect.X + 16, titleRect.Y + 6), Color.White, Color.Black, new Vector2(0, 2), maxLines: 1);

                // World tiles.
                // Update rects.
                Rectangle rect = new Rectangle(dialogBodyRect.X + margin, dialogBodyRect.Y + margin, 128, 128);
                foreach (NewWorldLevel level in levels)
                {
                    level.Box.Set(rect);
                    rect.X += 128 + margin;
                }

                foreach (NewWorldLevel level in levels)
                {
                    SpriteBatch batch = UI2D.Shared.SpriteBatch;
                    batch.Begin();
                    {
                        if (level.FrameColor != Color.Transparent)
                        {
                            int frameWidth = 4;
                            Rectangle frameRect = new Rectangle((int)level.Box.Min.X - frameWidth, (int)level.Box.Min.Y - frameWidth, (int)(level.Box.Width + 2 * frameWidth), (int)(level.Box.Height + 2 * frameWidth));
                            batch.Draw(Utils.white, frameRect, level.FrameColor);
                        }

                        batch.Draw(level.ThumbnailTexture, level.Box.Rectangle, Color.White);
                    }
                    batch.End();
                }

                // Buttons.  Fit at bottom of dialog.
                pos = new Vector2(dialogBodyRect.Right, dialogBodyRect.Bottom);
                pos.X -= margin + 6;
                pos.Y -= margin + 8;
                pos -= cancelButton.GetSize();
                cancelButton.Render(pos, useBatch: false);
            }
        }   // end of Render()

        #endregion

        #region Internal

        public void LoadContent(bool immediate)
        {
            titleBarTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\BlueTextTileWide");
            dialogBodyTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\LoadLevel\PopupFrame");

            // Get thumbnails for levels.
            foreach (NewWorldLevel level in levels)
            {
                level.ThumbnailTexture = Storage4.TextureLoad(level.ThumbnailName);
            }
        }

        public void InitDeviceResources(GraphicsDevice device)
        {
        }

        public void UnloadContent()
        {
            BokuGame.Release(ref titleBarTexture);
            BokuGame.Release(ref dialogBodyTexture);
        }

        public void DeviceReset(GraphicsDevice device)
        {
        }

        #endregion

    }   // end of class NewWorldDialog

}   // end of namespace Boku.UI2D
