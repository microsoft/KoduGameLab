
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

using Boku.Base;
using Boku.Common;
using Boku.Fx;
using Boku.UI2D;
using Boku.Input;
using BokuShared;

using ExtensionMethods;

namespace Boku.UI2D
{
    // This is the small dialog that sits in the upper-right hand corner of the screen
    // and displays the currently signed-in creator name.  The whole things acts as a
    // button.  When pressed it launches the AuthSignInDialog
    public class AuthStatusDialog : INeedsDeviceReset
    {
        #region Members

        private TextBlob blob;
        private AABB2D hitBox = new AABB2D();

        private Texture2D tileTexture;
        private Rectangle tileRect;

        private bool pressed = false;

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
                }
            }
        }

        #endregion

        #region Public

        /// <summary>
        /// c'tor
        /// </summary>
        public AuthStatusDialog()
        {
            blob = new TextBlob(Shared.GetGameFont20, "", 800);
        }

        public void Update()
        {
            if (active)
            {
                string s = "";
                s += Strings.Localize("auth.creator") + " : " + Auth.CreatorName;
                blob.RawText = s;

                float scale = 0.75f;
                Vector2 size = new Vector2(blob.GetLineWidth(0), tileTexture.Height * scale);
                // Add some extra width to allow the text to be centered horizontally.
                size.X += 32;
                Vector2 upperRightCorner = new Vector2((int)BokuGame.ScreenPosition.X + (int)BokuGame.ScreenSize.X, 0);
                Vector2 pos = upperRightCorner - new Vector2(size.X, 0);
                // Add a bit of a margin from the top of the screen.
                pos += new Vector2(-8, 8);
                pos = pos.Truncate();

                hitBox.Set(pos, pos + size);
                tileRect = new Rectangle((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y);

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
                else if (GamePadInput.ActiveMode == GamePadInput.InputMode.GamePad)
                {
                    // Do nothing.  Since signing in requires keyboard/mouse it 
                    // really doesn't make sense to partially support gamepad.
                }
            }
        }   // end of Update()

        private void HandleTouchInput(TouchContact touch, Vector2 hit)
        {
            if (hitBox.Touched(touch, hit))
            {
                Boku.Common.Gesture.TouchGestureManager.Get().TapGesture.ClearWasTapped();
                AuthUI.ShowSignOutDialog();
            }
        }

        private void HandleMouseInput(Vector2 hit)
        {
            if (hitBox.LeftPressed(hit))
            {
                AuthUI.ShowSignOutDialog();
            }
        }

        public void Render()
        {
            if (active)
            {
                // Render the underlying button texture.
                AuthUI.RenderTile(tileTexture, tileRect);

                Vector2 offset = new Vector2(16, 6);
                if (pressed)
                {
                    offset.Y += 1;
                }
                blob.RenderWithButtons(hitBox.Min + offset, Color.White);
            
            }   // end of active

        }   // end of Render()

        #endregion

        #region Internal

        public void LoadContent(bool immediate)
        {
            tileTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\BlueTextTileWide");
        }

        public void InitDeviceResources(GraphicsDevice device)
        {
        }

        public void UnloadContent()
        {
            BokuGame.Release(ref tileTexture);
        }

        public void DeviceReset(GraphicsDevice device)
        {
        }

        #endregion

    }   // end of class AuthStatusDialog

}   // end of namespace Boku.UI2D
