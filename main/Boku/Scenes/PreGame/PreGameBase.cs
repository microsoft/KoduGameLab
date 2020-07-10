// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Input;

using Boku.Base;
using Boku.SimWorld;
using Boku.Common;
using Boku.Programming;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;

namespace Boku
{
    public partial class InGame : GameObject, INeedsDeviceReset
    {
        /// <summary>
        /// Base class for PreGame objects.
        /// </summary>
        public class PreGameBase
        {
            #region Members

            protected CommandMap commandMap;

            protected bool active = false;

            #endregion

            #region Accessors
            public bool Active
            {
                get { return active; }
                set 
                {
                    if (active != value)
                    {
                        active = value;
                        if (active)
                        {
                            Activate();
                        }
                        else
                        {
                            // If the user hit a button to skip the pregame, clear this state.
                            GamePadInput.ClearAllWasPressedState();
                            Deactivate();
                        }
                    }
                }
            }
            #endregion

            #region Public

            // c'tor
            public PreGameBase()
            {
                // Just create an empty command map to act as a placeholder on
                // on the command map stack.
                commandMap = new CommandMap("PreGameBase");

            }   // end of PreGameBase c'tor

            public virtual void Update() 
            {
                // Default Update lets pregame run until user kicks it out.
                if (commandMap == CommandStack.Peek() && Active)
                {
                    if (Actions.X.WasPressed)
                    {
                        Actions.X.ClearAllWasPressedState();

                        Active = false;
                    }

                    // Exit pre-game mode.
                    if (KeyboardInputX.WasPressed(Keys.Escape))
                    {
                        KeyboardInputX.ClearAllWasPressedState(Keys.Escape);
                        Active = false;
                    }

                    if (Actions.ToolMenu.WasPressed)
                    {
                        Actions.ToolMenu.ClearAllWasPressedState();

                        Active = false;
                        InGame.inGame.CurrentUpdateMode = UpdateMode.ToolMenu;
                    }

                    if (Actions.MiniHub.WasPressed)
                    {
                        Actions.MiniHub.ClearAllWasPressedState();

                        // Leave active so when we come out of the mini-hub we know to re-start pregame.
                        //Active = false;
                        InGame.inGame.SwitchToMiniHub();
                    }
                }
            }   // end of PreGameBase Update()

            public virtual void Render(Camera camera) 
            {
                // This space intentionally left blank.
            }   // end of PreGameBase Render()

            #endregion

            #region Internal

            protected virtual void Activate()
            {
                CommandStack.Push(commandMap);
                HelpOverlay.Push("PreGame");
            }   // end of PreGameBase Activate()

            protected virtual void Deactivate()
            {
                CommandStack.Pop(commandMap);
                if (HelpOverlay.Peek() == "PreGame")
                {
                    HelpOverlay.Pop();
                }
                // If the pre-game has messed with the clock, restore it.
                Time.ClockRatio = 1.0f;
            }   // end of PreGameBase Deactivate()

            #endregion

        }   // end of class PreGameBase

    }   // end of class InGame

}   // end of namespace Boku


