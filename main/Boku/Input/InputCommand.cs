// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Xml;
using System.Xml.Serialization;
using System.Collections;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Common;

namespace Boku.Input
{
    /// <summary>
    /// Represents the base abstraction for input command
    /// An input command is an object that represents one hardware input element
    /// An example would be a single keyboard key ("enter") or a game pad button ("start")
    /// 
    /// There is currently no need to use this class outside this module as all
    /// usefull classes are represented below
    /// </summary>
    abstract public class InputCommand
    {
        protected const double timeAutoRepeat = 1.0 / 15.0;     // Was 8.0
        protected const double timeFirstAutoRepeat = 0.3;       // Was 0.33
        
        /// <summary>
        /// Temporary per update frame storage of key states (Game pad state is handled in GamepadInput)
        /// 
        /// </summary>
        protected static KeyboardState keyState;
        protected static KeyboardState[] chatpadStates = new KeyboardState[(int)PlayerIndex.Four + 1];

        /// <summary>
        /// This will update the per frame storage of key and game pad states
        /// For use by derived classes
        /// </summary>
        public static void UpdateState()
        {
            keyState = Keyboard.GetState();
            /*
            for (int iPlayer = (int)PlayerIndex.One; iPlayer <= (int)PlayerIndex.Four; iPlayer++)
            {
                chatpadStates[iPlayer] = Keyboard.GetState(GamePadInput.LogicalToReal((PlayerIndex)iPlayer));
            }
            */
        }
        [XmlAttribute]
        public string id;

        /// <summary>
        /// This will be called by the InputCommandStack to update
        /// specifics for the input command type
        /// </summary>
        abstract public void Update();

        /// <summary>
        /// This will be called by the InputCommandStack to reset
        /// specifics for the input command type
        /// This happens usually when a new command stack is pushed so that
        /// the old command stack can be set to a known state
        /// </summary>
        abstract public void Reset();

        /// <summary>
        /// This will be called by the InputCommmandStack to sync 
        /// to the current state for the input command type
        /// This happens when a new command stack is pushed so that
        /// the new stack will represent the current state and correctly call events
        /// 
        /// </summary>
        abstract public void Sync( );
    }

}
