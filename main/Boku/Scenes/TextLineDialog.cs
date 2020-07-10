// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;
using Boku.Common;
using Boku.Common.Gesture;
using Boku.UI2D;
using Boku.Fx;
using Point = Microsoft.Xna.Framework.Point;

namespace Boku
{
    /// <summary>
    /// The is a single line text entry dialog.
    /// </summary>
    public class TextLineDialog : GameObject, INeedsDeviceReset
    {
        #region Members

        public TextLineEditor textLineEditor = null;         // Actual editor.

        public AABB2D textBox = new AABB2D();
        public AABB2D fullScreenHitBox = new AABB2D();
        public int headerHeight=42+15;
        public int textBoxHeight = 50;
        public int footerHeight = 62+10;
        public int dialogWidth = 512;
        public string title = "Enter Text";

        public Button cancelButton = null;
        public Button okButton = null;

        public Camera camera = new PerspectiveUICamera();           // Camera for rendering most UI.

        //dialog pieces
        public Texture2D whiteTile;
        public Texture2D whiteTop;
        public Texture2D whiteBottom;
        public Texture2D blackHighlight;

        public delegate void OnDialogDone(bool bCanceled, string text);
        private OnDialogDone _onDialogDoneCallback;
        #endregion

        private int dialogHeight
        {
            get { return (headerHeight + textBoxHeight + footerHeight); }
        }
        public string GetText()
        {
            return textLineEditor.GetText();
        }
        public void SetText(string text)
        {
            textLineEditor.SetText(text);
        }

        public TextLineDialog(string initalText="")
        {
            camera.Resolution = new Point(1280, 720);

            var center = new Vector2(640, 360);
            var min = center - new Vector2(dialogWidth / 2, dialogHeight / 2);
            var max = center + new Vector2(dialogWidth / 2, dialogHeight / 2);

                //Create text control
            var margin = new Vector2(10, 0);//inset text control a bit
            textBox = new AABB2D(min + new Vector2(0, headerHeight) + margin, max - new Vector2(0, footerHeight) - margin);

            textLineEditor = new TextLineEditor(textBox, initalText);

        } 

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="device"></param>
        public void DeviceReset(GraphicsDevice device)
        {
            BokuGame.DeviceReset(textLineEditor, device);
        }


        #region Public

        public void Update()
        {

            HandleGamepadInput();
            HandleMouseInput();
            HandleTouchInput();

            if (textLineEditor.Active)
            {
                textLineEditor.Update();

                //Need to render now because we use render targets to get ui to scale.
                PreRender();

                return; //no further updates
            }

        }   // end of Update()


        private void HandleGamepadInput()
        {
            // Our children have input focus but we can still steal away the buttons we care about.
            GamePadInput pad = GamePadInput.GetGamePad0();

            if (pad.ButtonA.WasPressed)
            {
                pad.ButtonA.ClearAllWasPressedState();
                Accept();

            }
            else if (pad.ButtonB.WasPressed)
            {
                pad.ButtonB.ClearAllWasPressedState();
                Cancel();
            }
        }

        public void HandleMouseInput()
        {
            if (GamePadInput.ActiveMode != GamePadInput.InputMode.KeyboardMouse)
            {
                return;
            }

            Vector2 hit = MouseInput.GetMouseInRtCoords();

            // Cancel
            if (cancelButton.Box.LeftPressed(hit))
            {
                Cancel();
            }

            // ok
            if (okButton.Box.LeftPressed(hit))
            {
                Accept();
            }

            cancelButton.SetHoverState(hit);
            okButton.SetHoverState(hit);

        }

        private void Cancel()
        {
            if (_onDialogDoneCallback != null)
            {
                _onDialogDoneCallback(true, GetText());
            }
            textLineEditor.Deactivate();
            Deactivate();
        }

        private void Accept()
        {
            if (_onDialogDoneCallback != null)
            {
                _onDialogDoneCallback(false, GetText());
            }
            textLineEditor.Deactivate();
            Deactivate();
        }

// end of HandleMouseInput()

        private void HandleTouchInput()
        {
            //if (TouchInput.TouchCount == 0) { return; } // nothing to see here.

            //TouchContact touch = TouchInput.GetOldestTouch();
            //Vector2 touchHit = TouchInput.GetAspectRatioAdjustedPosition(
            //    touch.position,
            //    camera,
            //    true
            //);

            if (TouchInput.TouchCount == 0) { return; } // nothing to see here.

            TouchContact touch = TouchInput.GetOldestTouch();

            Vector2 touchHit = ScreenWarp.ScreenToRT(touch.position);

            if (TouchGestureManager.Get().TapGesture.WasTapped())
            {
                if (okButton.Box.Contains(touchHit))
                {
                    Accept();
                }
                if (cancelButton.Box.Contains(touchHit))
                {
                    Cancel();
                }
                if (textBox.Contains(touchHit))
                {
                    KeyboardInput.ShowOnScreenKeyboard();
                }
            }

        }   // end of HandleTouchInput()

        #endregion

        public void LoadContent(bool immediate)
        {
            whiteTile=BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\LoadLevel\WhiteTile");
            whiteTop = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\WhiteTop");
            whiteBottom = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\WhiteBottom");
            blackHighlight = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\BlackHighlight");

            {
                GetTexture getTexture = delegate { return ButtonTextures.BButton; };
                cancelButton = new Button(Strings.Localize("textDialog.cancel"), Color.White, getTexture,
                    UI2D.Shared.GetGameFont20);
            }
            {
                GetTexture getTexture = delegate { return ButtonTextures.AButton; };
                okButton = new Button(Strings.Localize("textDialog.ok"), Color.White, getTexture, UI2D.Shared.GetGameFont20);
            }

            BokuGame.Load(textLineEditor, immediate);

        }   // end of LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
            textLineEditor.InitDeviceResources(device);
        }

        public void UnloadContent()
        {
            BokuGame.Release(ref blackHighlight);
            BokuGame.Release(ref whiteBottom);
            BokuGame.Release(ref whiteTop);
            BokuGame.Release(ref whiteTile);

            BokuGame.Unload(textLineEditor);
        }   // end of UnloadContent()

        public void Render()
        {
            RenderTarget2D rt = Shared.RenderTargetDepthStencil1280_720;
            ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();
            // Copy the rendered scene to the backbuffer.
            quad.Render(rt, ScreenWarp.RenderPosition, ScreenWarp.RenderSize, @"TexturedRegularAlpha");
        }

        public void PreRender()
        {
            RenderTarget2D rt = Shared.RenderTargetDepthStencil1280_720;

            Vector2 rtSize = new Vector2(rt.Width, rt.Height);
            ScreenWarp.FitRtToScreen(rtSize);

            // Render the scene to our rendertarget.
            InGame.SetRenderTarget(rt);
            InGame.Clear(Color.Transparent);

            // Set up params for rendering UI with this camera.
            ShaderGlobals.SetCamera(camera);

            ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();

            SpriteBatch batch = Shared.SpriteBatch;

            var center = new Vector2(640, 360);
            var dialogMin = center - new Vector2(dialogWidth / 2, dialogHeight / 2);
            var dialogMax = center + new Vector2(dialogWidth / 2, dialogHeight / 2);


            // Top and bottom of dialog.
            var size = new Vector2(dialogWidth, whiteTop.Height);
            Vector2 pos = dialogMin;
            quad.Render(whiteTop, new Vector4(0, 0, 0, 1), pos, size, "TexturedRegularAlpha");
            pos += new Vector2(0, dialogHeight - 16);
            quad.Render(whiteBottom, new Vector4(0, 0, 0, 1), pos, size, "TexturedRegularAlpha");

            //dialog center
            size = new Vector2(dialogWidth, dialogHeight);
            pos = dialogMin;
            pos.Y += 16;
            size.Y = dialogHeight - 32;
            pos.X += 1;//to match size of top and bottom pieces
            size.X -= 2;//
            quad.Render(new Vector4(0, 0, 0, 1), pos, size);

            //top and bottom highlights
            int highlightMargin = 3;
            pos = dialogMin + new Vector2(highlightMargin, 2);
            size.X = dialogWidth - 2 * highlightMargin;
            size.Y = 30;
            quad.Render(blackHighlight, new Vector4(1, 1, 1, 0.2f), pos, size, "AdditiveBlendWithAlpha");

            pos = dialogMin + new Vector2(highlightMargin, dialogHeight - 60);
            size.X = dialogWidth - 2 * highlightMargin;
            size.Y = 30;
            quad.Render(blackHighlight, new Vector4(1, 1, 1, 0.2f), pos, size, "AdditiveBlendWithAlpha");

            batch.Begin();

            //Dialog title.
            TextHelper.DrawString(Shared.GetGameFont20, title, dialogMin + new Vector2(10, 10 + 5), Color.White);

            //Ok and Cancel buttons
            var buttonSize = cancelButton.GetSize();
            pos = dialogMax - buttonSize - new Vector2(10, 10);
            cancelButton.Render(pos);

            buttonSize = okButton.GetSize();
            pos -= new Vector2(buttonSize.X + 20, 0);
            okButton.Render(pos);

            batch.End();

            // Render text box
            textLineEditor.Render(camera);

            InGame.RestoreRenderTarget();

            InGame.SetViewportToScreen();

        }   // end of Render()

        #region Members
 
        private enum States
        {
            Inactive,
            Active,
        }
        private States state = States.Inactive;

        #endregion

        #region Accessors

        public bool Active
        {
            get { return (state == States.Active); }
        }

        #endregion

        public override bool Refresh(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            Debug.Assert(false, "This object is not designed to be put into any lists.");
            return true;
        }   // end of Refresh()


        override public void Activate()
        {
            if (state != States.Active)
            {
                state = States.Active;
                BokuGame.objectListDirty = true;

            }
        }

        public void Activate(OnDialogDone callback, string initalText, TextLineEditor.ValidateText validateTextCallback=null)
        {
            _onDialogDoneCallback = callback;

            Activate();
            TextLineEditor.OnEditDone editorCallback = delegate(bool canceled, string newText)
            {
                if (_onDialogDoneCallback != null)
                {
                    _onDialogDoneCallback(canceled, newText);
                }
                Deactivate();
            };
            textLineEditor.Activate(editorCallback, initalText, validateTextCallback);
        }
        override public void Deactivate()
        {
            state = States.Inactive;
        }   // End of Deactivate()

    }   // end of class 

}   // end of namespace Boku
