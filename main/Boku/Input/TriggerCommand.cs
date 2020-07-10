// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using KoiX;
using KoiX.Input;

using Boku.Base;
using Boku.Common;

namespace Boku.Input
{
    public class TriggerEventArgs : EventArgs
    {
        public TriggerEventArgs(float position)
        {
            this.position = position;
        }
        public float position;
    }
    public delegate void InputCommandTriggerDelegate(Object sender, TriggerEventArgs args);

    /// <summary>
    /// This represents the abstraction for the game pad trigger input command
    /// It exposes a float value for the position of the trigger
    /// 
    /// 
    /// </summary>
    abstract public class TriggerCommand : InputCommand
    {
        [XmlAttribute]
        public PlayerIndex playerIndex;

        protected float position;
        protected bool digitalPosition;

        // NOTE: do not call Start on this timer, it is being used in a caller update model
        protected Boku.Base.GameTimer timerAutoRepeat = new Boku.Base.GameTimer(Boku.Base.GameTimer.ClockType.WallClock);

        /// <summary>
        /// PositionChange event; when a position update happens that changes the position value
        /// </summary>
        public event InputCommandTriggerDelegate PositionChange;

        /// <summary>
        /// Press event, when state changes from released to pressed these are triggered
        /// </summary>
        public event InputCommandButtonDelegate Press;
        /// <summary>
        /// RepeatPress event, when state changes auto repeats pressed these are triggered
        /// </summary>
        public event InputCommandButtonDelegate RepeatPress;
        /// <summary>
        /// ReleasePress event, when state changes from pressed to released these are triggered
        /// </summary>
        public event InputCommandButtonDelegate ReleasePress;

        public TriggerCommand()
        {
            Reset();
            timerAutoRepeat.TimerElapsed += OnRepeatPress;
        }

        protected void OnRepeatPress(Boku.Base.GameTimer timer)
        {
            // NOTE: do not call Start on this timer, it is being used in a caller update model
            timerAutoRepeat.Reset(timeAutoRepeat);
            FireRepeatEvent(digitalPosition);
        }

        [XmlAttribute]
        public string BindPress;
        [XmlAttribute]
        public string BindRepeatPress;
        [XmlAttribute]
        public string BindReleasePress;

        [XmlAttribute]
        public string BindPositionChange;

        /// <summary>
        /// Return a float value of the position of the trigger between 0.0 and 1.0
        /// </summary>
        [XmlIgnore]
        public float Position
        {
            get
            {
                return position;
            }
            set
            {
                if (value != position)
                {
                    bool digitalPositionNew = CalcDigitalPosition(value);
                    if (digitalPositionNew != digitalPosition)
                    {
                        if (digitalPositionNew)
                        {
                            // NOTE: do not call Start on this timer, it is being used in a caller update model
                            timerAutoRepeat.Reset(timeFirstAutoRepeat);
                        }
                        else
                        {
                            timerAutoRepeat.Clear();
                        }
                        // fire release event if needed
                        FireReleaseEvent(digitalPosition);
                        // fire pressed event if needed
                        FirePressedEvent(digitalPositionNew);
                        digitalPosition = digitalPositionNew;
                    }
                    position = value;
                    if (PositionChange != null)
                    {
                        PositionChange( this, new TriggerEventArgs( position ));
                    }
                }
            }
        }
        override public void Reset()
        {
            position = 0.0f;
            digitalPosition = false;
            timerAutoRepeat.Clear();
        }
        override public void Update()
        {
            if ( GamePadInput.GetGamePad(this.playerIndex).IsConnected )
            {
                Position = TriggerValue();
                
                if (timerAutoRepeat.Running)
                {
                    timerAutoRepeat.Update();
                }
            }
            else
            {
                Reset();
            }
        }

        override public void Sync()
        {
            timerAutoRepeat.Clear();
            // do not set property Position as it will trigger events
            position = TriggerValue();
            digitalPosition = CalcDigitalPosition(position);
        }

        abstract protected float TriggerValue();
        protected void FirePressedEvent(bool digpos)
        {
            if (digpos)
            {
                if (Press != null)
                {
                    Press(this, new EventArgs());
                }
            }
        }
        protected void FireRepeatEvent(bool digpos)
        {
            if (digpos)
            {
                if (RepeatPress != null)
                {
                    RepeatPress(this, new EventArgs());
                }
            }
        }
        protected void FireReleaseEvent(bool digpos)
        {
            if (digpos)
            {
                if (ReleasePress != null)
                {
                    ReleasePress(this, new EventArgs());
                }
            }
        }
        protected const double valueEdge = 0.6;
        protected bool CalcDigitalPosition(float value)
        {
            bool digpos = false;
            if (value > valueEdge)
            {
                digpos = true;
            }
            
            return digpos;
        }
        
    }
}

