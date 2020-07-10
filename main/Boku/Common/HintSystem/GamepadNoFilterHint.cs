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
using Boku.Programming;

namespace Boku.Common.HintSystem
{
    /// <summary>
    /// Displays a warning to the user when they use a gamepad sensor
    /// without any button or stick filters.
    /// </summary>
    public class GamepadNoFilterHint : BaseHint
    {
        private bool editing = false;   // Is the user in the proramming editor?
        private GameActor actor = null; // What actor is the user editing?

        public GamepadNoFilterHint()
        {
            id = "GamepadNoFilterHint";

            toastText = Strings.Localize("toast.gamepadNoFilterToast");
            modalText = Strings.Localize("toast.gamepadNoFilterModal");

            showOnce = false;
        }

        public override bool Update()
        {
            bool activate = false;

            // We need to look for the transition out of editing kode.
            // At that point we then need to scan the for instances of
            // the GamePad sensor being used without a filter.

            bool prevEditing = editing;
            editing = false;
            if (InGame.inGame.Editor.Active == true)
            {
                editing = true;
                actor = InGame.inGame.Editor.GameActor;
            }

            // If we were editing last frame and are not editing
            // this frame then we've found the transition.
            if (prevEditing && !editing)
            {
                // We should only check the kode of the recently edited bot.
                if (actor != null)
                {
                    Brain brain = actor.Brain;
                    for (int t = 0; t < brain.tasks.Count; t++)
                    {
                        Task task = brain.tasks[t];
                        if (task != null)
                        {
                            for (int r = 0; r < task.reflexes.Count; r++)
                            {
                                Reflex reflex = task.reflexes[r] as Reflex;
                                if (reflex != null && reflex.sensorUpid != null)
                                {
                                    if (reflex.sensorUpid.StartsWith("sensor.gamepad"))
                                    {
                                        activate = true;
                                        // Look through the filters.  If we find anything other
                                        // than player.n or not then it must be a button or a stick
                                        // and everything's fine.
                                        for (int i = 0; i < reflex.filterUpids.Length; i++)
                                        {
                                            string id = reflex.filterUpids[i];
                                            if (id != null)
                                            {
                                                if (!id.StartsWith("filter.player") && id != "filter.not")
                                                {
                                                    activate = false;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return activate;
        }

    }   // end of class GamepadNoFilterHint

}   // end of namespace Boku.Common.HintSystem
