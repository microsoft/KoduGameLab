
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

using Boku.Audio;

namespace KoiX.UI
{
    using Keys = Microsoft.Xna.Framework.Input.Keys;

    public class Slider : BaseWidget
    {
        #region Members

        float minValue;
        float maxValue;
        float increment;
        int numDecimals = -1;               // How many places after the decimal point to display.
        string numDecimalsString = "";      // Format string for above value.

        // Cached values for CurrentValueString.  This just 
        // reduces the amount of per-frame string manipulation 
        // and GC exercise.
        float cachedCurValue = float.MinValue;
        string cachedCurValueString = "";

        Twitchable<float> curValue;

        SpriteCamera camera;    // Local ref used when transforming touch -> object space.

        #endregion

        #region Accessors

        /// <summary>
        /// Current value of the slider.  Note that if you set this, the 
        /// slider uses a twitch to change the value.  If you want to 
        /// immediately jump to a new value, use TargetValue.
        /// </summary>
        public float CurValue
        {
            get { return curValue.Value; }
            set 
            {
                float val = ValidateValue(value);

                if (curValue.Value != val)
                {
                    if (val > curValue.TargetValue)
                    {
                        Foley.PlayClickUp();
                    }
                    else if (val < curValue.TargetValue)
                    {
                        Foley.PlayClickDown();
                    }

                    curValue.Value = val;
                    OnChange();
                }
            }
        }

        /// <summary>
        /// Returns a formatted string with the current slider value.
        /// </summary>
        /// <returns></returns>
        public string CurrentValueString
        {
            get
            {
                if (CurValue != cachedCurValue)
                {
                    cachedCurValue = CurValue;
                    cachedCurValueString = cachedCurValue.ToString(numDecimalsString);
                }

                return cachedCurValueString;
            }
        }   // end of CurrentValueString()

        /// <summary>
        /// The value the slider is animating towards.  Mostly useful
        /// for setting initial value since this won't invoke a twitch.
        /// On the other hand, starting with a twitch may look kind of cool.
        /// </summary>
        public float TargetValue
        {
            get { return curValue.TargetValue; }
            set 
            {
                if (curValue.TargetValue != value)
                {
                    curValue.TargetValue = value;
                    OnChange();
                }
            }
        }

        public int NumDecimals
        {
            get { return numDecimals; }
            set
            {
                if (value != numDecimals)
                {
                    numDecimals = value;
                    numDecimalsString = "F" + numDecimals.ToString();
                }
            }
        }

        #endregion

        #region Public

        public Slider(BaseDialog parentDialog, RectangleF rect, float minValue, float maxValue, float increment, int numDecimals, Callback OnChange = null, ThemeSet theme = null, string id = null, object data = null)
            : base(parentDialog, OnChange: OnChange, theme: theme, id: id, data: data)
        {
            Debug.Assert(minValue < maxValue, "Max should be larger than min.");
            Debug.Assert(maxValue - minValue > increment, "Increment needs to be smaller.");
            // TODO (****) Due to floating point magic, this doesn't work.  Rethink.
            //Debug.Assert((maxValue - minValue) / increment == (int)((maxValue - minValue) / increment), "Max and min should be an integer number of increments apart.");
            Debug.Assert(numDecimals >= 0, "A negative number of decimalplaces to display really doesn't make sense, does it?");

            LocalRect = rect;
            this.minValue = minValue;
            this.maxValue = maxValue;
            this.increment = increment;
            NumDecimals = numDecimals;

            curValue = new Twitchable<float>(Theme.QuickTwitchTime, TwitchCurve.Shape.EaseOut, startingValue: minValue);

        }   // end of c'tor

        public override void Update(SpriteCamera camera, Vector2 parentPosition)
        {
            // TODO (****) should we just do this in the base widget class?
            this.camera = camera;

            base.Update(camera, parentPosition);
        }   // end of Update()

        public override void Render(SpriteCamera camera, Vector2 parentPosition)
        {
            RectangleF rect = localRect;
            rect.Position += parentPosition;
            float radius = rect.Height / 2.0f;
            RoundedRect.Render(camera, rect, radius, ThemeSet.DarkTextColor);

            // Adjust so filled part is smaller than trough.
            float outline = 1.2f;
            rect.Inflate(-outline);
            radius -= outline;

            // Based on CurValue, calc size of filled part.
            float fraction = (CurValue - minValue) / (maxValue - minValue);
            rect.Width = 2.0f * radius + fraction * (rect.Width - 2.0f * radius);
            RoundedRect.Render(camera, rect, radius, ThemeSet.FocusColorPlus10, 
                                bevelStyle: BevelStyle.Round, bevelWidth: radius);

            base.Render(camera, parentPosition);
        }   // end of Render()

        public override void RegisterForInputEvents()
        {
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftDown);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Touch);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.GamePad);

        }   // end of RegisterForInputEvents()

        #endregion

        #region InputEventHandler

        public override bool ProcessMouseLeftDownEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseFocusObject == null)
            {
                if (KoiLibrary.InputEventManager.MouseHitObject == this)
                {
                    // Claim mouse focus as ours.
                    KoiLibrary.InputEventManager.MouseFocusObject = this;

                    // Register to get move and left up events.
                    KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseMove);
                    KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftUp);

                    // Move the slider to where the initial press was.  Otherwise we
                    // don't see any movement until the mouse is moved.
                    float fraction = HitToFraction(input.Position);
                    CurValue = minValue + (maxValue - minValue) * fraction;

                    return true;
                }
            }

            return false;
        }   // end of ProcessMouseLeftDownEvent()

        public override bool ProcessMouseMoveEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseFocusObject == this)
            {
                float fraction = HitToFraction(input.Position);
                CurValue = minValue + (maxValue - minValue) * fraction;

                return true;
            }

            return base.ProcessMouseMoveEvent(input);
        }   // end of ProcessMouseMoveEvent()

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
                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MouseMove);
                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MouseLeftUp);

                return true;
            }
            return false;
        }   // end of ProcessMouseLeftUpEvent()

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);

            if (InFocus && !input.Modifier)
            {
                switch (input.Key)
                {
                    case Keys.Left:
                        CurValue -= increment;
                        return true;

                    case Keys.Right:
                        CurValue += increment;
                        return true;
                }
            }

            return base.ProcessKeyboardEvent(input);
        }   // end of ProcessKeyboardEvent()

        /// <summary>
        /// For sliders we'll just use any touch within the hit box 
        /// as a value to set the slider to.
        /// </summary>
        /// <param name="touchSampleList"></param>
        /// <returns></returns>
        public override bool ProcessTouchEvent(List<TouchSample> touchSampleList)
        {
            Debug.Assert(Active);

            for (int i = 0; i < touchSampleList.Count; i++)
            {
                TouchSample ts = touchSampleList[i];

                // Change focus on Pressed.
                if (ts.State == Microsoft.Xna.Framework.Input.Touch.TouchLocationState.Pressed)
                {
                    // If on us, claim focus.
                    if (ts.HitObject == this)
                    {
                        // Grab focus.
                        SetFocus();
                        KoiLibrary.InputEventManager.TouchFocusObject = this;

                        // Consume sample.
                        touchSampleList.RemoveAt(i);
                        --i;
                        continue;
                    }
                }

                if (KoiLibrary.InputEventManager.TouchFocusObject == this)
                {
                    float fraction = HitToFraction(ts.Position);
                    CurValue = minValue + (maxValue - minValue) * fraction;

                    // Always release focus on Released.
                    if (ts.State == Microsoft.Xna.Framework.Input.Touch.TouchLocationState.Released)
                    {
                        KoiLibrary.InputEventManager.TouchFocusObject = null;
                    }

                    // Consume the sample.
                    touchSampleList.RemoveAt(i);
                    --i;
                }

            }   // end of loop over touch samples.

            // Have we consumed all the samples?
            if (touchSampleList.Count == 0)
            {
                return true;
            }

            return base.ProcessTouchEvent(touchSampleList);
        }   // end of ProcessTouchEvent()

        public override bool ProcessGamePadEvent(GamePadInput pad)
        {
            Debug.Assert(Active);

            if (InFocus)
            {
                if (pad.DPadLeft.WasPressedOrRepeat || pad.LeftStickLeft.WasPressedOrRepeat)
                {
                    pad.DPadLeft.ClearAllWasPressedState();
                    pad.LeftStickLeft.ClearAllWasPressedState();
                    CurValue -= increment;
                    return true;
                }
                if (pad.DPadRight.WasPressedOrRepeat || pad.LeftStickRight.WasPressedOrRepeat)
                {
                    pad.DPadRight.ClearAllWasPressedState();
                    pad.LeftStickRight.ClearAllWasPressedState();
                    CurValue += increment;
                    return true;
                }
            }

            return base.ProcessGamePadEvent(pad);
        }

        #endregion

        #region Internal

        /// <summary>
        /// Takes a hit in screen space, converts it into camera 
        /// space, and then converts that into a normalized, 0..1 
        /// fraction of the slider value.
        /// </summary>
        /// <param name="hit"></param>
        /// <returns></returns>
        float HitToFraction(Vector2 screenHit)
        {
            // Translate hit into local coords.
            // Screen -> Camera.
            Vector2 hit = camera.ScreenToCamera(screenHit);
            // Camera -> Dialog.
            hit -= parentDialog.Rectangle.Position;
            // Dialog -> parent widget.
            hit -= parentPosition;

            // Calc new fraction.
            float radius = localRect.Height / 2.0f;
            float width = localRect.Width - 2.0f * radius;   // Calc slider width without rounded ends.
            float fraction = (hit.X - radius) / width;
            fraction = MathHelper.Clamp(fraction, 0, 1);
            
            return fraction;
        }   // end of HitToFraction()

        /// <summary>
        /// Awkward name.  Take the input value and returns
        /// the nearest valid value for this slider.  The result
        /// will be in range and equal to the min value plus
        /// some integer number of increments.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        float ValidateValue(float val)
        {
            // Round input to nearest increment and clamp into valid range.
            val = MathHelper.Clamp(val, minValue, maxValue);
            val = (val - minValue) / increment;
            val = minValue + increment * (float)Math.Round(val);
            val = MathHelper.Clamp(val, minValue, maxValue);

            return val;
        }   // ValidateValue()
        
        #endregion

    }   // end of class Slider

}   // end of namespace KoiX.UI
