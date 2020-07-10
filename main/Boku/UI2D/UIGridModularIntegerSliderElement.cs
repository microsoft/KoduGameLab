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
    public class UIGridModularIntegerSliderElement : UIGridBaseModularSliderElement
    {
        public delegate void UIIntegerSliderEvent(int curValue);

        private UIIntegerSliderEvent onChange = null;
        private int minValue = 1;
        private int maxValue = 10;
        private int curValue = 1;
        private int incrementByAmount = 1;

        #region Accessors
        public UIIntegerSliderEvent OnChange
        {
            set { onChange = value; }
        }
        /// <summary>
        /// Min value for slider
        /// </summary>
        public int MinValue
        {
            get { return minValue; }
            set { minValue = value; }
        }
        /// <summary>
        /// Max value for slider
        /// </summary>
        public int MaxValue
        {
            get { return maxValue; }
            set { maxValue = value; }
        }
        /// <summary>
        /// Amount to increment/decrement the slider value per step.
        /// </summary>
        public int IncrementByAmount
        {
            get { return incrementByAmount; }
            set { incrementByAmount = value; }
        }
        /// <summary>
        /// Current value for slider.
        /// </summary>
        public int CurrentValue
        {
            get { return curValue; }
            set
            {
                if (curValue != value)
                {
                    curValue = value;
                    dirty = true;
                    onChange(curValue);
                    // Create a twitch to change to displayValue to curValue.
                    TwitchManager.Set<float> set = delegate(float val, Object param) { displayValue = val; dirty = true; };
                    TwitchManager.CreateTwitch<float>(displayValue, curValue, set, 0.15f, TwitchCurve.Shape.EaseInOut);
                }
            }
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
                    selected = value;
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
        public UIGridModularIntegerSliderElement(ParamBlob blob, string label)
            : base(blob, label)
        {
        }

        /// <summary>
        /// Long form c'tor for use with no drop shadow.
        /// </summary>
        public UIGridModularIntegerSliderElement(float width, float height, float edgeSize, string normalMapName, Color baseColor, String label, GetFont font, TextHelper.Justification justify, Color textColor)
            : base(width, height, edgeSize, normalMapName, baseColor, label, font, justify, textColor)
        {
        }

        /// <summary>
        /// Long for c'tor for use with a drop shadow.
        /// </summary>
        public UIGridModularIntegerSliderElement(float width, float height, float edgeSize, string normalMapName, Color baseColor, String label, GetFont font, TextHelper.Justification justify, Color textColor, Color dropShadowColor, bool invertDropShadow)
            : base(width, height, edgeSize, normalMapName, baseColor, label, font, justify, textColor, dropShadowColor, invertDropShadow)
        {
        }

        /// <summary>
        /// Returns 0..1 indicating how full the slider should be rendered.
        /// </summary>
        /// <returns></returns>
        public override float GetSliderPercentage()
        {
            return (float)(displayValue - MinValue) / (float)(MaxValue - MinValue);
        }   // end of UIGridModularIntegerSlider GetSliderPercentage()

        /// <summary>
        /// Returns a count of the number of stops the slider has based
        /// on the min value, max value and increment.
        /// </summary>
        /// <returns></returns>
        public override float GetNumStops()
        {
            return (MaxValue - MinValue) / (float)IncrementByAmount;
        }

        /// <summary>
        /// Sets the slider to the closest increment to the given percentage value.
        /// </summary>
        /// <param name="value">True if this action caused the value to change.</param>
        public override bool SetSliderPercentage(float value)
        {
            int cur = CurrentValue;

            int numStops = (int)((maxValue - minValue) / incrementByAmount);
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
            return CurrentValue.ToString();
        }   // end of UIGridModularIntegerSlider GetFormattedValue()

        /// <summary>
        /// Increments the current slider value by whatever increment the user specified.
        /// </summary>
        /// <returns>True if the value changed.</returns>
        public override bool IncrementCurrentValue()
        {
            bool result = false;

            int value = FastScrolling ?
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
        }   // end of UIGridModularIntegerSliderElement IncrementCurrentValue()

        /// <summary>
        /// Decrements the current slider value by whatever increment the user specified.
        /// </summary>
        /// <returns>True if the value changed.</returns>
        public override bool DecrementCurrentValue()
        {
            bool result = false;

            int value = FastScrolling ?
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
        }   // end of UIGridModularIntegerSliderElement DecrementCurrentValue()

    }   // end of class UIGridModularIntegerSliderElement

}   // end of namespace Boku.UI2D






