// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Boku.Input
{
    public class MicroBitDisplayFrame
    {
        public MicroBitImage Image { get; private set; }
        public float Duration { get; private set; }
        public int Brightness { get; private set; }

        public MicroBitDisplayFrame(bool[] LEDs, float duration, int brightness)
        {
            Image = new MicroBitImage(LEDs);
            Duration = duration;
            Brightness = brightness;
        }
    }
}
