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
    public class ResourceMeterHint : BaseHint
    {
        private double lastActivationTime = 0;
        private float delayBeforeReactivation = 60.0f;   // Don't display more than once a minute.

        public ResourceMeterHint()
        {
            id = "ResourceMeterHint";

            toastText = Strings.Localize("toast.resourceMeterToast");
            modalText = Strings.Localize("toast.resourceMeterModal");
        }

        public override bool Update()
        {
            bool activate = false;

            // Don't even consider if we've recently shown this hint.
            if (lastActivationTime < Time.WallClockTotalSeconds)
            {
                // Make sure we're in a mode that matters.
                if (InGame.inGame.State != InGame.States.Inactive)
                {
                    // Trigger this hint when we get to 85% full.
                    if (InGame.inGame.FractionFullUnclamped > 0.85f && Terrain.Current.EnableResourceLimiting)
                    {
                        activate = true;
                        lastActivationTime = Time.WallClockTotalSeconds + delayBeforeReactivation;
                    }
                }
            }

            return activate;
        }

    }   // end of class ResourceMeterHint

}   // end of namespace Boku.Common.HintSystem
