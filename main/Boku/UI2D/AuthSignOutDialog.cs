// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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
using Boku.Common.Xml;
using Boku.Fx;
using Boku.UI2D;
using Boku.Input;

using BokuShared;

namespace Boku.UI2D
{
    //
    // This is the dialog that pops up when a user want to sign out.
    // In particular this is triggerd when the user is signed in and
    // they click on the status dialog.
    //
    public class AuthSignOutDialog : INeedsDeviceReset
    {
        #region Members

        private const int margin = 16;

        private Texture2D titleBarTexture;
        private Texture2D dialogBodyTexture;
        private Texture2D checkboxUnlit;
        private Texture2D checkboxLit;
        private Texture2D textBoxTexture;

        private Rectangle titleRect;
        private Rectangle dialogBodyRect;

        private AABB2D checkBoxBox = new AABB2D();

        private Button signOutButton;
        private Button cancelButton;

        private TextBlob blob;

        private bool keepSignedInChecked = false;

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
                        keepSignedInChecked = XmlOptionsData.KeepSignedInOnExit;

                        // Save away existing keyboard event handlers.  We need this in case
                        // this dialog is launched over some other dialog which uses keyboard
                        // input like the SaveLevelDialog.
                        prevOnChar = KeyboardInput.OnChar;
                        prevOnKey = KeyboardInput.OnKey;
#if !NETFX_CORE
                        prevTextInput = BokuGame.bokuGame.winKeyboard.CharacterEntered;
#endif

                        // Now that we've saved them.  Null them out.
                        KeyboardInput.OnKey = null;
#if NETFX_CORE
                        Debug.Assert(false, "Does this work?  Why did we prefer winKeyboard?");
                        KeyboardInput.OnChar = null;
#else
                        BokuGame.bokuGame.winKeyboard.CharacterEntered = null;
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
                }
            }
        }

#endregion

#region Public

        public AuthSignOutDialog()
        {
            blob = new TextBlob(UI2D.Shared.GetGameFont20, "", 400);

            signOutButton = new Button(Strings.Localize("auth.signOut"), Color.White, null, UI2D.Shared.GetGameFont20);
            cancelButton = new Button(Strings.Localize("auth.cancel"), Color.White, null, UI2D.Shared.GetGameFont20);

        }   // end of c'tor

        public void Update()
        {
            if (active)
            {
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
            if (checkBoxBox.Touched(touch, hit))
            {
                Boku.Common.Gesture.TouchGestureManager.Get().TapGesture.ClearWasTapped();
                keepSignedInChecked = !keepSignedInChecked;
            }

            // Buttons
            if (signOutButton.Box.Touched(touch, hit))
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
            if (checkBoxBox.LeftPressed(hit))
            {
                keepSignedInChecked = !keepSignedInChecked;
            }

            // Buttons
            if (signOutButton.Box.LeftPressed(hit))
            {
                // Save results.
                OnAccept();
            }
            if (cancelButton.Box.LeftPressed(hit))
            {
                OnCancel();
            }

            // Update hover state.
            signOutButton.SetHoverState(hit);
            cancelButton.SetHoverState(hit);

        }   // end of HandleMouseInput()

        private void OnAccept()
        {
            // Note that setting this also forces the creator name and idHash
            // to be saved from Auth.
            XmlOptionsData.KeepSignedInOnExit = keepSignedInChecked;

            Active = false;

            // Activate the sign-in dialog.
            AuthUI.ShowSignInDialog();

        }   // end of OnAccept()

        private void OnCancel()
        {
            // Even if we cancel, we may have changed this.
            XmlOptionsData.KeepSignedInOnExit = keepSignedInChecked;

            // Just go away!
            Active = false;

            // Restart status dialog.
            AuthUI.ShowStatusDialog();
        }   // end of OnCancel()

        public void Render()
        {
            if (active)
            {
                titleRect = new Rectangle(0, 0, 512, 72);
                dialogBodyRect = new Rectangle(0, 64, 512, 320);

                // Now that we have the final dialog size, center it on the screen.
                Vector2 pos = BokuGame.ScreenSize / 2.0f;
                pos.X -= titleRect.Width / 2;
                pos.Y -= (titleRect.Height + dialogBodyRect.Height) / 2;
                titleRect.X = (int)pos.X;
                titleRect.Y = (int)pos.Y;
                dialogBodyRect.X = titleRect.X;
                dialogBodyRect.Y = titleRect.Y + titleRect.Height;

                AuthUI.RenderTile(titleBarTexture, titleRect);
                AuthUI.RenderTile(dialogBodyTexture, dialogBodyRect);

                // Title bar text.
                string str = Strings.Localize("auth.signOutTitle");
                blob.RawText = str;
                blob.Font = UI2D.Shared.GetGameFont30Bold;
                blob.Justification = UIGridElement.Justification.Left;
                blob.RenderWithButtons(new Vector2(titleRect.X + margin, titleRect.Y + 6), Color.White, Color.Black, new Vector2(0, 2), maxLines: 1);

                // Text box labels.
                int verticalBoxSpacing = blob.TotalSpacing + 4;
                blob.Font = UI2D.Shared.GetGameFont24;
                blob.Justification = UIGridElement.Justification.Left;
                str = Strings.Localize("auth.currentlySignedInAs") + Auth.CreatorName;

                blob.Width = dialogBodyRect.Width - 2 * margin;
                pos = new Vector2(dialogBodyRect.X + margin, dialogBodyRect.Y + margin);
                blob.RawText = str;
                blob.RenderWithButtons(pos, Color.White);

                // Buttons.  Fit at bottom of dialog.
                pos = new Vector2(dialogBodyRect.Right, dialogBodyRect.Bottom);
                pos.X -= margin;
                pos.Y -= margin;
                pos -= cancelButton.GetSize();
                cancelButton.Render(pos, useBatch: false);
                pos.X -= margin;
                pos.X -= signOutButton.GetSize().X;
                signOutButton.Render(pos, useBatch: false);

                // Keep signed in checkbox.
                // Position vertically just above buttons.
                pos.X = dialogBodyRect.X + margin;
                pos.Y = signOutButton.Box.Min.Y;
                pos.X += 32;    // Adjust for checkbox.
                blob.RawText = Strings.Localize("auth.keepSignedIn");
                blob.Width = dialogBodyRect.Width - 2 * margin - 32;
                pos.Y -= blob.NumLines * blob.TotalSpacing;
                Rectangle checkboxRect = new Rectangle((int)pos.X - 32, (int)pos.Y + 4, 32, 32);
                checkBoxBox.Set(checkboxRect);
                AuthUI.RenderTile(keepSignedInChecked ? checkboxLit : checkboxUnlit, checkboxRect);
                blob.RenderWithButtons(pos, Color.White);
            }
        }   // end of Render()

#endregion

#region Internal

        public void LoadContent(bool immediate)
        {
            titleBarTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\BlueTextTileWide");
            dialogBodyTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\LoadLevel\PopupFrame");
            checkboxUnlit = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\CheckboxUnlit");
            checkboxLit = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\CheckboxLit");
            textBoxTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\WhiteTile");
        }

        public void InitDeviceResources(GraphicsDevice device)
        {
        }

        public void UnloadContent()
        {
            BokuGame.Release(ref titleBarTexture);
            BokuGame.Release(ref dialogBodyTexture);
            BokuGame.Release(ref checkboxUnlit);
            BokuGame.Release(ref checkboxLit);
            BokuGame.Release(ref textBoxTexture);
        }

        public void DeviceReset(GraphicsDevice device)
        {
        }

#endregion

    }   // end of class AuthSignOutDialog

}   // end of namespace Boku.UI2D
