// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Content;

namespace Boku.Common
{
    public partial class GamePadInput
    {
        /// <summary>
        /// A child class of GamePadInput which allows you to add a layer
        /// which will allow events to be thrown by GamePadInput.
        /// </summary>
        public class Events
        {
            /// <summary>
            /// Prototype used for GamePadInput.Events events. 
            /// </summary>
            /// <param name="creator">The object which registered for this event set.</param>
            public delegate void InputEvent(Object obj);

            private Object creator;             // The object that created this.
            private PlayerIndex index;          // Which input controller we care about.
            private GamePadInput gamePad = null;

            public event InputEvent AButtonOnPressed;
            public event InputEvent BButtonOnPressed;
            public event InputEvent XButtonOnPressed;
            public event InputEvent YButtonOnPressed;
            public event InputEvent BackButtonOnPressed;
            public event InputEvent StartButtonOnPressed;
            public event InputEvent DPadUpOnPressed;
            public event InputEvent DPadDownOnPressed;
            public event InputEvent DPadLeftOnPressed;
            public event InputEvent DPadRightOnPressed;
            public event InputEvent LeftShoulderOnPressed;
            public event InputEvent RightShoulderOnPressed;
            public event InputEvent LeftStickOnPressed;
            public event InputEvent RightStickOnPressed;
            public event InputEvent LeftTriggerButtonOnPressed;
            public event InputEvent RightTriggerButtonOnPressed;
            public event InputEvent LeftStickLeftOnPressed;
            public event InputEvent LeftStickRightOnPressed;
            public event InputEvent LeftStickUpOnPressed;
            public event InputEvent LeftStickDownOnPressed;
            public event InputEvent RightStickLeftOnPressed;
            public event InputEvent RightStickRightOnPressed;
            public event InputEvent RightStickUpOnPressed;
            public event InputEvent RightStickDownOnPressed;

            public event InputEvent AButtonOnRepeatPressed;
            public event InputEvent BButtonOnRepeatPressed;
            public event InputEvent XButtonOnRepeatPressed;
            public event InputEvent YButtonOnRepeatPressed;
            public event InputEvent BackButtonOnRepeatPressed;
            public event InputEvent StartButtonOnRepeatPressed;
            public event InputEvent DPadUpOnRepeatPressed;
            public event InputEvent DPadDownOnRepeatPressed;
            public event InputEvent DPadLeftOnRepeatPressed;
            public event InputEvent DPadRightOnRepeatPressed;
            public event InputEvent LeftShoulderOnRepeatPressed;
            public event InputEvent RightShoulderOnRepeatPressed;
            public event InputEvent LeftStickOnRepeatPressed;
            public event InputEvent RightStickOnRepeatPressed;
            public event InputEvent LeftTriggerButtonOnRepeatPressed;
            public event InputEvent RightTriggerButtonOnRepeatPressed;
            public event InputEvent LeftStickLeftOnRepeatPressed;
            public event InputEvent LeftStickRightOnRepeatPressed;
            public event InputEvent LeftStickUpOnRepeatPressed;
            public event InputEvent LeftStickDownOnRepeatPressed;
            public event InputEvent RightStickLeftOnRepeatPressed;
            public event InputEvent RightStickRightOnRepeatPressed;
            public event InputEvent RightStickUpOnRepeatPressed;
            public event InputEvent RightStickDownOnRepeatPressed;



            public event InputEvent AButtonOnReleased;
            public event InputEvent BButtonOnReleased;
            public event InputEvent XButtonOnReleased;
            public event InputEvent YButtonOnReleased;
            public event InputEvent BackButtonOnReleased;
            public event InputEvent StartButtonOnReleased;
            public event InputEvent DPadUpOnReleased;
            public event InputEvent DPadDownOnReleased;
            public event InputEvent DPadLeftOnReleased;
            public event InputEvent DPadRightOnReleased;
            public event InputEvent LeftShoulderOnReleased;
            public event InputEvent RightShoulderOnReleased;
            public event InputEvent LeftStickOnReleased;
            public event InputEvent RightStickOnReleased;
            public event InputEvent LeftTriggerButtonOnReleased;
            public event InputEvent RightTriggerButtonOnReleased;
            public event InputEvent LeftStickLeftOnReleased;
            public event InputEvent LeftStickRightOnReleased;
            public event InputEvent LeftStickUpOnReleased;
            public event InputEvent LeftStickDownOnReleased;
            public event InputEvent RightStickLeftOnReleased;
            public event InputEvent RightStickRightOnReleased;
            public event InputEvent RightStickUpOnReleased;
            public event InputEvent RightStickDownOnReleased;

            #region Accessors
            public Object Creator
            {
                get { return creator; }
                set { creator = value; }
            }
            public PlayerIndex PlayerIndex
            {
                get { return index; }
            }
            #endregion

            // c'tor
            public Events(Object obj, PlayerIndex index)
            {
                this.creator = obj;
                this.index = index;
                switch (index)
                {
                    case PlayerIndex.One:
                        this.gamePad = gamePad1;
                        break;
                    case PlayerIndex.Two:
                        this.gamePad = gamePad2;
                        break;
                    case PlayerIndex.Three:
                        this.gamePad = gamePad3;
                        break;
                    case PlayerIndex.Four:
                        this.gamePad = gamePad4;
                        break;
                }
            }   // end of Events c'tor

            private static void UpdateCurrent(ArrayList list, ref Events curPadEvents, Object stackTop)
            {
                curPadEvents = null;
                for (int i = 0; i < list.Count; i++)
                {
                    Events events = (Events)list[i];
                    if (events.creator == stackTop)
                    {
                        curPadEvents = events;
                        break;
                    }
                }
            }   // end of Events UpdateCurrent()

            /// <summary>
            /// Looks through the list seeing if any match the stack top.  If so, then 
            /// those should be made current.
            /// </summary>
            /// <param name="stackTop"></param>
            public static void UpdateCurrent(Object stackTop)
            {
                UpdateCurrent(gamePad1.eventsList, ref gamePad1.curEvents, stackTop);
                UpdateCurrent(gamePad2.eventsList, ref gamePad2.curEvents, stackTop);
                UpdateCurrent(gamePad3.eventsList, ref gamePad3.curEvents, stackTop);
                UpdateCurrent(gamePad4.eventsList, ref gamePad4.curEvents, stackTop);
            }   // end of UpdateCurrent()

            /// <summary>
            /// Throws any events TriggerButtoned by the current state of the GamePadInput.
            /// </summary>
            public void Throw()
            {
                // Go through each possible event, throwing as needed.

                //
                // Pressed
                //
                if (gamePad.A.WasPressed && AButtonOnPressed != null)
                {
                    AButtonOnPressed(creator);
                }
                if (gamePad.B.WasPressed && BButtonOnPressed != null)
                {
                    BButtonOnPressed(creator);
                }
                if (gamePad.X.WasPressed && XButtonOnPressed != null)
                {
                    XButtonOnPressed(creator);
                }
                if (gamePad.Y.WasPressed && YButtonOnPressed != null)
                {
                    YButtonOnPressed(creator);
                }
                if (gamePad.Back.WasPressed && BackButtonOnPressed != null)
                {
                    BackButtonOnPressed(creator);
                }
                if (gamePad.Start.WasPressed && StartButtonOnPressed != null)
                {
                    StartButtonOnPressed(creator);
                }
                if (gamePad.DPadUp.WasPressed && DPadUpOnPressed != null)
                {
                    DPadUpOnPressed(creator);
                }
                if (gamePad.DPadDown.WasPressed && DPadDownOnPressed != null)
                {
                    DPadDownOnPressed(creator);
                }
                if (gamePad.DPadLeft.WasPressed && DPadLeftOnPressed != null)
                {
                    DPadLeftOnPressed(creator);
                }
                if (gamePad.DPadRight.WasPressed && DPadRightOnPressed != null)
                {
                    DPadRightOnPressed(creator);
                }
                if (gamePad.LeftShoulder.WasPressed && LeftShoulderOnPressed != null)
                {
                    LeftShoulderOnPressed(creator);
                }
                if (gamePad.RightShoulder.WasPressed && RightShoulderOnPressed != null)
                {
                    RightShoulderOnPressed(creator);
                }
                if (gamePad.LeftStickButton.WasPressed && LeftStickOnPressed != null)
                {
                    LeftStickOnPressed(creator);
                }
                if (gamePad.RightStickButton.WasPressed && RightStickOnPressed != null)
                {
                    RightStickOnPressed(creator);
                }
                if (gamePad.LeftTriggerButton.WasPressed && LeftTriggerButtonOnPressed != null)
                {
                    LeftTriggerButtonOnPressed(creator);
                }
                if (gamePad.RightTriggerButton.WasPressed && RightTriggerButtonOnPressed != null)
                {
                    RightTriggerButtonOnPressed(creator);
                }
                if (gamePad.LeftStickLeft.WasPressed && LeftStickLeftOnPressed != null)
                {
                    LeftStickLeftOnPressed(creator);
                }
                if (gamePad.LeftStickRight.WasPressed && LeftStickRightOnPressed != null)
                {
                    LeftStickRightOnPressed(creator);
                }
                if (gamePad.LeftStickUp.WasPressed && LeftStickUpOnPressed != null)
                {
                    LeftStickUpOnPressed(creator);
                }
                if (gamePad.LeftStickDown.WasPressed && LeftStickDownOnPressed != null)
                {
                    LeftStickDownOnPressed(creator);
                }
                if (gamePad.RightStickLeft.WasPressed && RightStickLeftOnPressed != null)
                {
                    RightStickLeftOnPressed(creator);
                }
                if (gamePad.RightStickRight.WasPressed && RightStickRightOnPressed != null)
                {
                    RightStickRightOnPressed(creator);
                }
                if (gamePad.RightStickUp.WasPressed && RightStickUpOnPressed != null)
                {
                    RightStickUpOnPressed(creator);
                }
                if (gamePad.RightStickDown.WasPressed && RightStickDownOnPressed != null)
                {
                    RightStickDownOnPressed(creator);
                }

                //
                // RepeatPressed
                //
                if (gamePad.A.WasRepeatPressed && AButtonOnRepeatPressed != null)
                {
                    AButtonOnRepeatPressed(creator);
                }
                if (gamePad.B.WasRepeatPressed && BButtonOnRepeatPressed != null)
                {
                    BButtonOnRepeatPressed(creator);
                }
                if (gamePad.X.WasRepeatPressed && XButtonOnRepeatPressed != null)
                {
                    XButtonOnRepeatPressed(creator);
                }
                if (gamePad.Y.WasRepeatPressed && YButtonOnRepeatPressed != null)
                {
                    YButtonOnRepeatPressed(creator);
                }
                if (gamePad.Back.WasRepeatPressed && BackButtonOnRepeatPressed != null)
                {
                    BackButtonOnRepeatPressed(creator);
                }
                if (gamePad.Start.WasRepeatPressed && StartButtonOnRepeatPressed != null)
                {
                    StartButtonOnRepeatPressed(creator);
                }
                if (gamePad.DPadUp.WasRepeatPressed && DPadUpOnRepeatPressed != null)
                {
                    DPadUpOnRepeatPressed(creator);
                }
                if (gamePad.DPadDown.WasRepeatPressed && DPadDownOnRepeatPressed != null)
                {
                    DPadDownOnRepeatPressed(creator);
                }
                if (gamePad.DPadLeft.WasRepeatPressed && DPadLeftOnRepeatPressed != null)
                {
                    DPadLeftOnRepeatPressed(creator);
                }
                if (gamePad.DPadRight.WasRepeatPressed && DPadRightOnRepeatPressed != null)
                {
                    DPadRightOnRepeatPressed(creator);
                }
                if (gamePad.LeftShoulder.WasRepeatPressed && LeftShoulderOnRepeatPressed != null)
                {
                    LeftShoulderOnRepeatPressed(creator);
                }
                if (gamePad.RightShoulder.WasRepeatPressed && RightShoulderOnRepeatPressed != null)
                {
                    RightShoulderOnRepeatPressed(creator);
                }
                if (gamePad.LeftStickButton.WasRepeatPressed && LeftStickOnRepeatPressed != null)
                {
                    LeftStickOnRepeatPressed(creator);
                }
                if (gamePad.RightStickButton.WasRepeatPressed && RightStickOnRepeatPressed != null)
                {
                    RightStickOnRepeatPressed(creator);
                }
                if (gamePad.LeftTriggerButton.WasRepeatPressed && LeftTriggerButtonOnRepeatPressed != null)
                {
                    LeftTriggerButtonOnRepeatPressed(creator);
                }
                if (gamePad.RightTriggerButton.WasRepeatPressed && RightTriggerButtonOnRepeatPressed != null)
                {
                    RightTriggerButtonOnRepeatPressed(creator);
                }
                if (gamePad.LeftStickLeft.WasRepeatPressed && LeftStickLeftOnRepeatPressed != null)
                {
                    LeftStickLeftOnRepeatPressed(creator);
                }
                if (gamePad.LeftStickRight.WasRepeatPressed && LeftStickRightOnRepeatPressed != null)
                {
                    LeftStickRightOnRepeatPressed(creator);
                }
                if (gamePad.LeftStickUp.WasRepeatPressed && LeftStickUpOnRepeatPressed != null)
                {
                    LeftStickUpOnRepeatPressed(creator);
                }
                if (gamePad.LeftStickDown.WasRepeatPressed && LeftStickDownOnRepeatPressed != null)
                {
                    LeftStickDownOnRepeatPressed(creator);
                }
                if (gamePad.RightStickLeft.WasRepeatPressed && RightStickLeftOnRepeatPressed != null)
                {
                    RightStickLeftOnRepeatPressed(creator);
                }
                if (gamePad.RightStickRight.WasRepeatPressed && RightStickRightOnRepeatPressed != null)
                {
                    RightStickRightOnRepeatPressed(creator);
                }
                if (gamePad.RightStickUp.WasRepeatPressed && RightStickUpOnRepeatPressed != null)
                {
                    RightStickUpOnRepeatPressed(creator);
                }
                if (gamePad.RightStickDown.WasRepeatPressed && RightStickDownOnRepeatPressed != null)
                {
                    RightStickDownOnRepeatPressed(creator);
                }

                //
                // Released.
                //
                if (gamePad.A.WasReleased && AButtonOnReleased != null)
                {
                    AButtonOnReleased(creator);
                }
                if (gamePad.B.WasReleased && BButtonOnReleased != null)
                {
                    BButtonOnReleased(creator);
                }
                if (gamePad.X.WasReleased && XButtonOnReleased != null)
                {
                    XButtonOnReleased(creator);
                }
                if (gamePad.Y.WasReleased && YButtonOnReleased != null)
                {
                    YButtonOnReleased(creator);
                }
                if (gamePad.Back.WasReleased && BackButtonOnReleased != null)
                {
                    BackButtonOnReleased(creator);
                }
                if (gamePad.Start.WasReleased && StartButtonOnReleased != null)
                {
                    StartButtonOnReleased(creator);
                }
                if (gamePad.DPadUp.WasReleased && DPadUpOnReleased != null)
                {
                    DPadUpOnReleased(creator);
                }
                if (gamePad.DPadDown.WasReleased && DPadDownOnReleased != null)
                {
                    DPadDownOnReleased(creator);
                }
                if (gamePad.DPadLeft.WasReleased && DPadLeftOnReleased != null)
                {
                    DPadLeftOnReleased(creator);
                }
                if (gamePad.DPadRight.WasReleased && DPadRightOnReleased != null)
                {
                    DPadRightOnReleased(creator);
                }
                if (gamePad.LeftShoulder.WasReleased && LeftShoulderOnReleased != null)
                {
                    LeftShoulderOnReleased(creator);
                }
                if (gamePad.RightShoulder.WasReleased && RightShoulderOnReleased != null)
                {
                    RightShoulderOnReleased(creator);
                }
                if (gamePad.LeftStickButton.WasReleased && LeftStickOnReleased != null)
                {
                    LeftStickOnReleased(creator);
                }
                if (gamePad.RightStickButton.WasReleased && RightStickOnReleased != null)
                {
                    RightStickOnReleased(creator);
                }
                if (gamePad.LeftTriggerButton.WasReleased && LeftTriggerButtonOnReleased != null)
                {
                    LeftTriggerButtonOnReleased(creator);
                }
                if (gamePad.RightTriggerButton.WasReleased && RightTriggerButtonOnReleased != null)
                {
                    RightTriggerButtonOnReleased(creator);
                }
                if (gamePad.LeftStickLeft.WasReleased && LeftStickLeftOnReleased != null)
                {
                    LeftStickLeftOnReleased(creator);
                }
                if (gamePad.LeftStickRight.WasReleased && LeftStickRightOnReleased != null)
                {
                    LeftStickRightOnReleased(creator);
                }
                if (gamePad.LeftStickUp.WasReleased && LeftStickUpOnReleased != null)
                {
                    LeftStickUpOnReleased(creator);
                }
                if (gamePad.LeftStickDown.WasReleased && LeftStickDownOnReleased != null)
                {
                    LeftStickDownOnReleased(creator);
                }
                if (gamePad.RightStickLeft.WasReleased && RightStickLeftOnReleased != null)
                {
                    RightStickLeftOnReleased(creator);
                }
                if (gamePad.RightStickRight.WasReleased && RightStickRightOnReleased != null)
                {
                    RightStickRightOnReleased(creator);
                }
                if (gamePad.RightStickUp.WasReleased && RightStickUpOnReleased != null)
                {
                    RightStickUpOnReleased(creator);
                }
                if (gamePad.RightStickDown.WasReleased && RightStickDownOnReleased != null)
                {
                    RightStickDownOnReleased(creator);
                }

            }   // end of Events Throw

        }   // end of class Events

    }   // end of class GamePadInput


}   // end of namespace Boku.Common


