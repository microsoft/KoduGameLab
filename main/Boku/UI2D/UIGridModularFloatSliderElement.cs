// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Text;

using Boku.Common;
using Boku.Fx;

namespace Boku.UI2D
{
    /// <summary>
    /// An instance of UIElement that uses a 9-grid element for its geometry
    /// and creates a texture on the fly into which the slider and the 
    /// associated text string are rendered.
    /// </summary>
    public class UIGridModularFloatSliderElement : UIGridBaseModularSliderElement
    {
        public delegate void UIFloatSliderEvent(float curValue);

        private UIFloatSliderEvent onChange = null;
        private float minValue = 0.0f;
        private float maxValue = 1.0f;
        private float curValue = 0.0f;
        private float incrementByAmount = 0.1f;
        private int numDecimals = 1;

        private int twitchHandle = -1;

        #region Accessors
        public UIFloatSliderEvent OnChange
        {
            set { onChange = value; }
        }
        /// <summary>
        /// Min value for slider
        /// </summary>
        public float MinValue
        {
            get { return minValue; }
            set { minValue = value; }
        }
        /// <summary>
        /// Max value for slider
        /// </summary>
        public float MaxValue
        {
            get { return maxValue; }
            set { maxValue = value; }
        }
        /// <summary>
        /// Amount to increment/decrement the slider value per step.
        /// </summary>
        public float IncrementByAmount
        {
            get { return incrementByAmount; }
            set { incrementByAmount = value; }
        }
        /// <summary>
        /// Current value for slider.
        /// </summary>
        public float CurrentValue
        {
            get { return curValue; }
            set
            {
                if (curValue != value)
                {
                    curValue = value;
                    dirty = true;
                    onChange(curValue);

                    if (twitchHandle >= 0)
                    {
                        //Kill old twitch to prevent multiple set.
                        TwitchManager.KillTwitch<float>(twitchHandle);
                    }
                    // Create a twitch to change to displayValue to curValue.
                    TwitchManager.Set<float> set = delegate(float val, Object param) { displayValue = val; dirty = true; };
                    twitchHandle = TwitchManager.CreateTwitch<float>(displayValue, curValue, set, 0.1f, TwitchCurve.Shape.EaseOut);
                }
            }
        }

        public float CurrentValueImmediate
        {
            get { return curValue; }
            set
            {
                if (curValue != value)
                {
                    curValue = value;
                    displayValue = value;
                    dirty = true;
                    if (onChange != null)
                    {
                        onChange(curValue);
                    }
                }
            }
        }

        /// <summary>
        /// The number of decimal places to use when displaying the current value.
        /// </summary>
        public int NumberOfDecimalPlaces
        {
            get { return numDecimals; }
            set { numDecimals = value; }
        }

        /// <summary>
        /// Set or cleared by the owning grid to tell this element whether it's the selected element.
        /// </summary>
        public override bool Selected
        {
            get { return selected; }
            set
            {
                if (selected != value)
                {
                    base.Selected = value;
                    if (selected)
                    {
                        if (SetHelpOverlay)
                        {
                            HelpOverlay.Push(@"ModularSlider");
                        }

                        TwitchManager.Set<float> set = delegate(float val, Object param) { dim = val; };
                        TwitchManager.CreateTwitch<float>(dim, 1.0f, set, 0.2f, TwitchCurve.Shape.EaseInOut);
                    }
                    else
                    {
                        if (SetHelpOverlay)
                        {
                            HelpOverlay.Pop();
                        }

                        TwitchManager.Set<float> set = delegate(float val, Object param) { dim = val; };
                        TwitchManager.CreateTwitch<float>(dim, 0.5f, set, 0.2f, TwitchCurve.Shape.EaseInOut);
                    }
                }
            }
        }

        #endregion


        // c'tor
        /// <summary>
        /// Simple c'tor using a blob to hold the common data.
        /// </summary>
        /// <param name="blob"></param>
        /// <param name="label"></param>
        public UIGridModularFloatSliderElement(ParamBlob blob, string label)
            : base(blob, label)
        {
        }

        /// <summary>
        /// Long form c'tor for use with no drop shadow.
        /// </summary>
        public UIGridModularFloatSliderElement(float width, float height, float edgeSize, string normalMapName, Color baseColor, String label, GetFont font, TextHelper.Justification justify, Color textColor)
            : base(width, height, edgeSize, normalMapName, baseColor, label, font, justify, textColor)
        {
        }

        /// <summary>
        /// Long for c'tor for use with a drop shadow.
        /// </summary>
        public UIGridModularFloatSliderElement(float width, float height, float edgeSize, string normalMapName, Color baseColor, String label, GetFont font, TextHelper.Justification justify, Color textColor, Color dropShadowColor, bool invertDropShadow)
            : base(width, height, edgeSize, normalMapName, baseColor, label, font, justify, textColor, dropShadowColor, invertDropShadow)
        {
        }

        /// <summary>
        /// Returns 0..1 indicating how full the slider should be rendered.
        /// </summary>
        /// <returns></returns>
        public override float GetSliderPercentage()
        {
            return (displayValue - MinValue) / (MaxValue - MinValue);
        }   // end of UIGridIntegerSlider GetSliderPercentage()

        /// <summary>
        /// Returns a count of the number of stops the slider has based
        /// on the min value, max value and increment.
        /// </summary>
        /// <returns></returns>
        public override float GetNumStops()
        {
            return (MaxValue - MinValue) / IncrementByAmount;
        }

        /// <summary>
        /// Sets the slider to the closest increment to the given percentage value.
        /// </summary>
        /// <param name="value">True if this action caused the value to change.</param>
        public override bool SetSliderPercentage(float value)
        {
            float cur = CurrentValue;

            // The addition of 0.5f forces rounding since (int) truncates.
            int numStops = (int)((maxValue - minValue) / incrementByAmount + 0.5f);
            int step = (int)(value * numStops + 0.5f);
            CurrentValue = MinValue + step * IncrementByAmount;

            return cur != CurrentValue;
        }   // end of SetSliderPercentage()

        /// <summary>
        /// Returns the current value formatted properly for overlaying on the slider.
        /// </summary>
        /// <returns></returns>
        public override string GetFormattedValue()
        {
            return displayValue.ToString("F" + numDecimals.ToString());
        }   // end of UIGridIntegerSlider GetFormattedValue()

        /// <summary>
        /// Increments the current slider value by whatever increment the user specified.
        /// </summary>
        /// <returns>True if the value changed.</returns>
        public override bool IncrementCurrentValue()
        {
            bool result = false;

            float value = FastScrolling ?
                CurrentValue + (IncrementByAmount * FastScrollScalar) :
                CurrentValue + IncrementByAmount;

            if (value > MaxValue)
            {
                value = MaxValue;
            }
            if (value != CurrentValue)
            {
                CurrentValue = value;
                result = true;
            }

            return result;

        }   // end of UIGridModularFloatSliderElement IncrementCurrentValue()

        /// <summary>
        /// Decrements the current slider value by whatever increment the user specified.
        /// </summary>
        /// <returns>True if the value changed.</returns>
        public override bool DecrementCurrentValue()
        {
            bool result = false;

            float value = FastScrolling ?
                CurrentValue - (IncrementByAmount * FastScrollScalar) :
                CurrentValue - IncrementByAmount;

            if (value < MinValue)
            {
                value = MinValue;
            }
            if (value != CurrentValue)
            {
                CurrentValue = value;
                result = true;
            }

            return result;

        }   // end of UIGridModularFloatSliderElement DecrementCurrentValue()

    }   // end of class UIGridModularFloatSliderElement

}   // end of namespace Boku.UI2D






