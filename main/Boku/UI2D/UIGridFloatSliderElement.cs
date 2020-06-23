
using System;
using System.Collections;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Common;
using Boku.Fx;

namespace Boku.UI2D
{
    /// <summary>
    /// An instance of UIElement that uses a 9-grid element for its geometry
    /// and creates a texture on the fly into which the slider and the 
    /// associated text string are rendered.
    /// </summary>
    public class UIGridFloatSliderElement : UIGridBaseSliderElement
    {
        public delegate void UIFloatSliderEvent(float curValue);

        private UIFloatSliderEvent onChange = null;
        private float minValue = 0.0f;
        private float maxValue = 1.0f;
        private float curValue = 0.5f;
        private float incrementByAmount = 0.1f;
        private int numDecimals = 1;

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
                curValue = value;
                dirty = true;
                onChange(curValue);
                // Create a twitch to change to displayValue to curValue.
                TwitchManager.Set<float> set = delegate(float val, Object param) { displayValue = val; dirty = true; };
                TwitchManager.CreateTwitch<float>(displayValue, curValue, set, 0.1f, TwitchCurve.Shape.EaseInOut);
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
        #endregion


        // c'tor
        /// <summary>
        /// Simple c'tor using a blob to hold the common data.
        /// </summary>
        /// <param name="blob"></param>
        /// <param name="label"></param>
        public UIGridFloatSliderElement(ParamBlob blob, string label)
            : base(blob, label)
        {
        }

        /// <summary>
        /// Long form c'tor for use with no drop shadow.
        /// </summary>
        public UIGridFloatSliderElement(float width, float height, float edgeSize, string normalMapName, Color baseColor, string label, Shared.GetFont font, Justification justify, Color textColor)
            : base(width, height, edgeSize, normalMapName, baseColor, label, font, justify, textColor)
        {
        }

        /// <summary>
        /// Long for c'tor for use with a drop shadow.
        /// </summary>
        public UIGridFloatSliderElement(float width, float height, float edgeSize, string normalMapName, Color baseColor, string label, Shared.GetFont font, Justification justify, Color textColor, Color dropShadowColor, bool invertDropShadow)
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
            float value = CurrentValue + IncrementByAmount;
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
        }   // end of UIGridFloatSliderElement IncrementCurrentValue()

        /// <summary>
        /// Decrements the current slider value by whatever increment the user specified.
        /// </summary>
        /// <returns>True if the value changed.</returns>
        public override bool DecrementCurrentValue()
        {
            bool result = false;
            float value = CurrentValue - IncrementByAmount;
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
        }   // end of UIGridFloatSliderElement DecrementCurrentValue()

    }   // end of class UIGridFloatSliderElement

}   // end of namespace Boku.UI2D






