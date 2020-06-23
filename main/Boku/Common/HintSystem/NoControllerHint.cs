
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
    /// <summary>
    /// Displays a warning to the user when loading a level that has exclusively
    /// gamepad input if the user doesn't have a gamepad connected.
    /// </summary>
    public class NoControllerHint : BaseHint
    {
        private XmlWorldData data = null;

        public NoControllerHint()
        {
            id = "NoControllerHint";

            toastText = Strings.Localize("toast.noControllerToast");
            modalText = Strings.Localize("toast.noControllerModal");

            showOnce = false;
        }

        public override bool Update()
        {
            bool activate = false;

            if (GamePadInput.NoControllers)
            {
                // Activate when world data changes.  This implies that a new world was loaded.
                if (data != InGame.XmlWorldData)
                {
                    data = InGame.XmlWorldData;

                    bool usesGamepad = false;
                    bool usesKeyboard = false;
                    bool usesMouse = false;

                    CheckInputUsage(out usesGamepad, out usesMouse, out usesKeyboard);

                    // Only trigger if the level uses gamepad input but doesn't use keyboard input.
                    if (usesGamepad && !(usesKeyboard || usesMouse) )
                    {
                        activate = true;
                    }
                }

            }

            return activate;
        }

    }   // end of class NoControllerHint

}   // end of namespace Boku.Common.HintSystem
