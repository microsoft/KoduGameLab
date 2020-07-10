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
    /// Displays a warning to the user when they program a Switch
    /// without telling what page to go to.
    /// </summary>
    public class SwitchNoTargetHint : BaseHint
    {
        private bool editing = false;   // Is the user in the proramming editor?
        private GameActor actor = null; // What actor is the user editing?

        public SwitchNoTargetHint()
        {
            id = "SwitchNoTargetHint";

            toastText = Strings.Localize("toast.switchNoTargetToast");
            modalText = Strings.Localize("toast.switchNoTargetModal");

            showOnce = false;
        }

        public override bool Update()
        {
            bool activate = false;

            // We need to look for the transition out of editing kode.
            // At that point we then need to scan the for instances of
            // the Switch actuator being used without a page modifier.

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
                                if (reflex != null)
                                {
                                    if (reflex.actuatorUpid == "actuator.switchtask")
                                    {
                                        // The only valid modifiers are pages so just look for any to exist.
                                        if (reflex.modifierUpids.Length == 0)
                                        {
                                            activate = true;
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

    }   // end of class SwitchNoTargetHint

}   // end of namespace Boku.Common.HintSystem
