// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KoiX;

using Boku.Input;

namespace Boku.Common.HintSystem
{
#if !NETFX_CORE
    class MicrobitNeedsResetHint : BaseHint
    {
        private double updateTime = 0;

        public MicrobitNeedsResetHint()
        {
            id = "MicrobitNeedsResetHint";

            toastText = Strings.Localize("toast.microbitNeedsResetHintToast");
            modalText = null;

            showOnce = false;
        }

        public override bool Update()
        {
            bool needFlash = false;

            if (MicrobitManager.Microbits.Count > 0)
            {
                foreach (var bit in MicrobitManager.Microbits.Values)
                {
                    // If the bit is stuck in the flashed state for more than a second,
                    // it most likely needs to load the flash into active memory. This
                    // is a manual operation that must be done by the user pushing the
                    // reset button on the back of the bit. The hint controlled by this
                    // object notifies the user of this, and tells them how to do it.
                    if (bit.Status == Microbit.EDeviceStatus.FLASHING)
                    {
                        needFlash = true;
                        break;
                    }
                }
            }

            if (!needFlash)
            {
                // Record the current time. Once we need to flash a bit, we'll
                // use this timestamp to delay the display of the hint by one
                // second.
                updateTime = Time.WallClockTotalSeconds;
                return false;
            }
            else 
            {
                const double delayBeforeActivation = 1.0;
                // Show the hint if we need to flash a bit and its been more
                // than a second.
                return (Time.WallClockTotalSeconds - updateTime) > delayBeforeActivation;
            }
        }
    }
#endif
}
