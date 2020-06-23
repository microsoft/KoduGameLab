
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

namespace Boku.Common.HintSystem
{
    public class QuietMainMenuHint : BaseHint
    {
        private double activationTime = 0;
        private float delayBeforeActivation = 30.0f;    // Wait 30 seconds before displaying.

        public QuietMainMenuHint()
        {
            id = "QuietMainMenuHint";

            toastText = Strings.Localize("toast.quietMainMenuToast");
            modalText = Strings.Localize("toast.quietMainMenuModal");
        }

        public override bool Update()
        {
            bool activate = false;

            if (activationTime == 0)
            {
                activationTime = Time.WallClockTotalSeconds + delayBeforeActivation;
            }

            // Keep resetting the start time if we're not in the MainMenu.
            if (!BokuGame.bokuGame.mainMenu.Active || BokuGame.bokuGame.mainMenu.OptionsActive)
            {
                activationTime = Time.WallClockTotalSeconds + delayBeforeActivation;
            }

            if (Time.WallClockTotalSeconds > activationTime)
            {
                activate = true;
                // Reset time.
                activationTime = Time.WallClockTotalSeconds + 2.0f * delayBeforeActivation;
            }

            return activate;
        }

    }   // end of class QuietMainMenuHint

}   // end of namespace Boku.Common.HintSystem
