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

using KoiX;
using KoiX.Geometry;
using KoiX.Input;

using Boku.Input;


namespace KoiX.UI
{
    /// <summary>
    /// Simple button which uses a texture for its appearence.
    /// 
    /// This also means that it can be used in a situation
    /// where it makes sense to press and hold so autorepeat
    /// is optionally enabled.
    /// </summary>
    public class GraphicButton : BaseButton
    {
        #region Members

        protected Texture2D texture;
        protected string textureName;

        bool autorepeat = false;

        double touchTime = 0;   // Used to calc autorepeat.

        // Using RoundedRect to display the tile.
        // For now, just make visible the elements that are needed instead
        // of trying to expose them all up front.
        float cornerRadius = 0.0f;
        float focusOutlineWidth = 0.0f;
        Color backgroundColor = Color.Transparent;

        #endregion

        #region Accessors

        public Texture2D Texture
        {
            get { return texture; }
            set
            {
                texture = value;
                textureName = null;
            }
        }

        public float CornerRadius
        {
            get { return cornerRadius; }
            set { cornerRadius = value; }
        }

        /// <summary>
        /// Width for outline when element is in focus.
        /// </summary>
        public float FocusOutlineWidth
        {
            get { return focusOutlineWidth; }
            set { focusOutlineWidth = value; }
        }

        /// <summary>
        /// If BackgroundColor is Transparent, we
        /// don't render the underlying tile.
        /// </summary>
        public Color BackgroundColor
        {
            get { return backgroundColor; }
            set { backgroundColor = value; }
        }

        #endregion

        #region Public

        public GraphicButton(BaseDialog parentDialog, RectangleF rect, string textureName, Callback onSelect, bool autorepeat = false, string id = null, object data = null)
            : base(parentDialog, onSelect, id: id, data: data)
        {
            this.localRect = rect;
            this.textureName = textureName;
            this.autorepeat = autorepeat;
        }

        public GraphicButton(BaseDialog parentDialog, RectangleF rect, Texture2D texture, Callback onSelect, bool autorepeat = false, string id = null, object data = null)
            : base(parentDialog, onSelect, id: id, data: data)
        {
            this.localRect = rect;
            this.texture = texture;
            this.autorepeat = autorepeat;
        }

        public override void Recalc(Vector2 parentPosition)
        {
            base.Recalc(parentPosition);
        }

        public override void Update(SpriteCamera camera, Vector2 parentPosition)
        {
            base.Update(camera, parentPosition);

            if (Dirty)
            {
                Recalc(parentPosition);
            }
        }   // end of Update()

        public override void Render(SpriteCamera camera, Vector2 parentPosition)
        {
            if (alpha.Value > 0)
            {
                RectangleF renderRect = localRect;
                renderRect.Position += parentPosition;

                if (backgroundColor == Color.Transparent)
                {
                    SpriteBatch batch = KoiLibrary.SpriteBatch;

                    Matrix viewMatrix = camera != null ? camera.ViewMatrix : Matrix.Identity;
                    batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, samplerState: null, depthStencilState: null, rasterizerState: null, effect: null, transformMatrix: viewMatrix);
                    {
                        batch.Draw(texture, renderRect.ToRectangle(), Color.White);
                    }
                    batch.End();
                }
                else
                {
                    if (InFocus)
                    {
                        RoundedRect.Render(camera, renderRect, cornerRadius, backgroundColor);
                        renderRect.Inflate(-focusOutlineWidth);
                    }

                    RoundedRect.Render(camera, renderRect, cornerRadius - focusOutlineWidth, backgroundColor, texture: texture);
                }

            }
        }   // end of Render()

        public override void RegisterForInputEvents()
        {
            base.RegisterForInputEvents();

            if (autorepeat)
            {
                // Autorepeat is handled here so unregister for in the base class.  This
                // prevent duplicate calls to OnSelect().
                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.Tap);

                // Unregister for these inputs since they assume having focus.
                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.Keyboard);
                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.GamePad);
            }

        }   // end of RegisterForInputEvents()

        #endregion

        #region Internal

        public override bool ProcessMouseLeftDownEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseFocusObject == null)
            {
                if (KoiLibrary.InputEventManager.MouseHitObject == this)
                {
                    // Claim mouse focus as ours.
                    KoiLibrary.InputEventManager.MouseFocusObject = this;

                    // Register to get left up events.
                    KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftUp);
                    if (autorepeat)
                    {
                        // If autorepeat is set, trigger immediately.
                        OnButtonSelect();
                        KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MousePosition);
                    }

                    // Change state.
                    Selected = true;

                    return true;
                }
            }

            return false;
        }   // end of ProcessMouseLeftDownEvent()

        public override bool ProcessMouseLeftUpEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseFocusObject == this)
            {
                // Release mouse focus.
                if (KoiLibrary.InputEventManager.MouseFocusObject == this)
                {
                    KoiLibrary.InputEventManager.MouseFocusObject = null;
                }

                // Stop getting move and up events.
                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MousePosition);
                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MouseLeftUp);

                Selected = false;

                // If we're a non-autorepeat button, then trigger on release assuming
                // the mouse is still over us.
                if (!autorepeat && KoiLibrary.InputEventManager.MouseHitObject == this)
                {
                    OnButtonSelect();
                }

                return true;
            }

            return false;
        }   // end of ProcessMouseLeftUpEvent()

        public override bool ProcessMousePositionEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseFocusObject == this)
            {
                if (KoiLibrary.InputEventManager.MouseFocusObject == this)
                {
                    if(autorepeat && KoiLibrary.InputEventManager.MouseHitObject == this && LowLevelMouseInput.Left.WasRepeatPressed)
                    {
                        OnButtonSelect();
                    }

                    return true;
                }
            }

            return false;
        }   // end of ProcessMousePositionEvent()

        public override bool ProcessTouchEvent(List<TouchSample> touchSampleList)
        {
            /*
            if (KoiLibrary.InputEventManager.TouchFocusObject == null || KoiLibrary.InputEventManager.TouchHitObject == null)
            {
                Debug.Print("...");
            }
            else
            {
                Debug.Print("focus " + KoiLibrary.InputEventManager.TouchFocusObject.UniqueNum.ToString());
                Debug.Print("hit " + KoiLibrary.InputEventManager.TouchHitObject.UniqueNum.ToString());
                Debug.Print("  " + Time.FrameCounter.ToString());
                Debug.Print("  " + Time.WallClockTotalSeconds.ToString());
                Debug.Print("  " + touchTime.ToString());
            }
            */

            Debug.Assert(Active);

            bool result = false;

            // We're not calling the base version of this so we need to set TouchFocusObject here.
            for (int i = 0; i < touchSampleList.Count; i++)
            {
                TouchSample ts = touchSampleList[i];

                if(KoiLibrary.InputEventManager.TouchHitObject == this
                    && ts.State == Microsoft.Xna.Framework.Input.Touch.TouchLocationState.Pressed)
                {
                    KoiLibrary.InputEventManager.TouchFocusObject = this;
                    result = true;
                }

                if (ts.State == Microsoft.Xna.Framework.Input.Touch.TouchLocationState.Released)
                {
                    if (KoiLibrary.InputEventManager.TouchFocusObject == this)
                    {
                        KoiLibrary.InputEventManager.TouchFocusObject = null;
                        result = true;
                    }
                }
            }   // end of loop over samples.


            if (KoiLibrary.InputEventManager.TouchFocusObject == this)
            {
                // Note, this will trigger immediately since touchTime is 0 and
                // then will start keeping track of autorepeat times.
                if (KoiLibrary.InputEventManager.TouchHitObject == this && Time.WallClockTotalSeconds > touchTime)
                {
                    OnButtonSelect();

                    // Calc new touch time.  Use longer delay at first, and then shorter delay after.
                    touchTime = Time.WallClockTotalSeconds + (touchTime == 0 ? GamePadInput.Button.AutoRepeatDelay : 1.0f / GamePadInput.Button.AutoRepeatRate);
                }
            }
            else
            {
                touchTime = 0;  // Reset since we don't have focus.
            }

            return result;

        }   // end of ProcessTouchEvent()

        public override void LoadContent()
        {
            if (DeviceResetX.NeedsLoad(texture))
            {
                texture = KoiLibrary.LoadTexture2D(textureName);
            }
        }

        public override void UnloadContent()
        {
            DeviceResetX.Release(ref texture);
        }

        #endregion
    }   // end of class GraphicButton

}   // end of namespace KoiX.UI

