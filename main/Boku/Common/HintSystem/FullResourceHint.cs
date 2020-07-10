// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Fx;
using Boku.Common;
using Boku.Common.Xml;
using Boku.SimWorld.Terra;

namespace Boku.Common.HintSystem
{
    public class FullResourceHint : BaseHint
    {
        private double lastActivationTime = 0;
        private float delayBeforeReactivation = 60.0f;   // Don't display more than once a minute.

        public FullResourceHint()
        {
            id = "FullResourceHint";

            toastText = Strings.Localize("toast.fullResourceToast");
            modalText = Strings.Localize("toast.fullResourceModal");
        }

        public override bool Update()
        {
            bool activate = false;

            // Don't even consider if we've recently shown this hint.
            if (lastActivationTime < Time.WallClockTotalSeconds)
            {
                // Trigger this hint when we get over 100% full.
                if (InGame.inGame.FractionFullUnclamped > 1.0f && Terrain.Current.EnableResourceLimiting)
                {
                    activate = true;
                    lastActivationTime = Time.WallClockTotalSeconds + delayBeforeReactivation;
                }
            }

            return activate;
        }

    }   // end of class FullResourceHint

}   // end of namespace Boku.Common.HintSystem
