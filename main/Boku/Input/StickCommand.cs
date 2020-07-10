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

using KoiX;
using KoiX.Input;

using Boku.Base;
using Boku.Common;

namespace Boku.Input
{
    public class StickEventArgs : EventArgs
    {
        public StickEventArgs(Vector2 position)
        {
            this.position = position;
        }
        public Vector2 position;
    }
    public delegate void InputCommandStickDelegate(Object sender, StickEventArgs args);

    /// <summary>
    /// This represents the abstraction for game pad stick input command
    /// It exposes a 2d vector for the position of the stick 
    /// 
    /// There is currently no need to use this class outside this module as all
    /// usefull classes are represented below
    /// </summary>
    abstract public class StickCommand : InputCommand
    {
        [XmlAttribute]
        public PlayerIndex playerIndex;

        protected Vector2 position;
        protected enum DigitalPosition
        {
            centered = 0x0000,
            Up = 0x0001,
            UpRight = 0x0011,
            Right = 0x0010,
            DownRight = 0x0110,
            Down = 0x0100,
            DownLeft = 0x1100,
            Left = 0x1000,
            UpLeft = 0x1001,
        }
        protected DigitalPosition digitalPosition;
        // NOTE: do not call Start on this timer, it is being used in a caller update model
        protected Boku.Base.GameTimer timerAutoRepeat = new Boku.Base.GameTimer(Boku.Base.GameTimer.ClockType.WallClock);

        /// <summary>
        /// PositionChange event; when a position update happens that changes the position value
        /// </summary>
        public event InputCommandStickDelegate PositionChange;

        /// <summary>
        /// Press event, when state changes from released to pressed these are triggered
        /// </summary>
        public event InputCommandButtonDelegate UpPress;
        public event InputCommandButtonDelegate UpRightPress;
        public event InputCommandButtonDelegate RightPress;
        public event InputCommandButtonDelegate DownRightPress;
        public event InputCommandButtonDelegate DownPress;
        public event InputCommandButtonDelegate DownLeftPress;
        public event InputCommandButtonDelegate LeftPress;
        public event InputCommandButtonDelegate UpLeftPress;
        public event InputCommandStickDelegate PositionPress;
 
        /// <summary>
        /// RepeatPress event, when state changes auto repeats pressed these are triggered
        /// </summary>
        public event InputCommandButtonDelegate UpRepeatPress;
        public event InputCommandButtonDelegate UpRightRepeatPress;
        public event InputCommandButtonDelegate RightRepeatPress;
        public event InputCommandButtonDelegate DownRightRepeatPress;
        public event InputCommandButtonDelegate DownRepeatPress;
        public event InputCommandButtonDelegate DownLeftRepeatPress;
        public event InputCommandButtonDelegate LeftRepeatPress;
        public event InputCommandButtonDelegate UpLeftRepeatPress;
        public event InputCommandStickDelegate PositionRepeatPress;

        /// <summary>
        /// ReleasePress event, when state changes from pressed to released these are triggered
        /// </summary>
        public event InputCommandButtonDelegate UpReleasePress;
        public event InputCommandButtonDelegate UpRightReleasePress;
        public event InputCommandButtonDelegate RightReleasePress;
        public event InputCommandButtonDelegate DownRightReleasePress;
        public event InputCommandButtonDelegate DownReleasePress;
        public event InputCommandButtonDelegate DownLeftReleasePress;
        public event InputCommandButtonDelegate LeftReleasePress;
        public event InputCommandButtonDelegate UpLeftReleasePress;
        public event InputCommandStickDelegate PositionReleasePress;

        [XmlAttribute]
        public string BindUpPress;
        [XmlAttribute]
        public string BindUpRightPress;
        [XmlAttribute]
        public string BindRightPress;
        [XmlAttribute]
        public string BindDownRightPress;
        [XmlAttribute]
        public string BindDownPress;
        [XmlAttribute]
        public string BindDownLeftPress;
        [XmlAttribute]
        public string BindLeftPress;
        [XmlAttribute]
        public string BindUpLeftPress;
        [XmlAttribute]
        public string BindPositionPress;

        [XmlAttribute]
        public string BindUpRepeatPress;
        [XmlAttribute]
        public string BindUpRightRepeatPress;
        [XmlAttribute]
        public string BindRightRepeatPress;
        [XmlAttribute]
        public string BindDownRightRepeatPress;
        [XmlAttribute]
        public string BindDownRepeatPress;
        [XmlAttribute]
        public string BindDownLeftRepeatPress;
        [XmlAttribute]
        public string BindLeftRepeatPress;
        [XmlAttribute]
        public string BindUpLeftRepeatPress;
        [XmlAttribute]
        public string BindPositionRepeatPress;
        [XmlAttribute]

        public string BindUpReleasePress;
        [XmlAttribute]
        public string BindUpRightReleasePress;
        [XmlAttribute]
        public string BindRightReleasePress;
        [XmlAttribute]
        public string BindDownRightReleasePress;
        [XmlAttribute]
        public string BindDownReleasePress;
        [XmlAttribute]
        public string BindDownLeftReleasePress;
        [XmlAttribute]
        public string BindLeftReleasePress;
        [XmlAttribute]
        public string BindUpLeftReleasePress;
        [XmlAttribute]
        public string BindPositionReleasePress;

        [XmlAttribute]
        public string BindPositionChange;

        public StickCommand()
        {
            Reset();
            timerAutoRepeat.TimerElapsed += RepeatPress;
        }

        protected void RepeatPress(Boku.Base.GameTimer timer)
        {
            // NOTE: do not call Start on this timer, it is being used in a caller update model
            timerAutoRepeat.Reset(timeAutoRepeat);
            FireRepeatEvent(digitalPosition);
            if (digitalPosition != DigitalPosition.centered && this.PositionRepeatPress != null)
            {
                this.PositionRepeatPress( this, new StickEventArgs( this.position ));
            }
        }

        /// <summary>
        /// Return a 2D vector position of the stick between -1.0 and 1.0
        /// </summary>
        [XmlIgnore]
        public Vector2 Position
        {
            get
            {
                return position;
            }
            set
            {
                if (value != this.position)
                {
                    DigitalPosition digitalPositionNew = CalcDigitalPosition(value);
                    if (digitalPositionNew != digitalPosition)
                    {
                        if (digitalPositionNew == DigitalPosition.centered)
                        {
                            timerAutoRepeat.Clear();
                        }
                        else
                        {
                            // NOTE: do not call Start on this timer, it is being used in a caller update model
                            timerAutoRepeat.Reset(timeFirstAutoRepeat);
                        }
                        // fire release event if needed
                        FireReleaseEvent(digitalPosition);
                        if (digitalPositionNew == DigitalPosition.centered && this.PositionReleasePress != null)
                        {
                            this.PositionReleasePress(this, new StickEventArgs(this.position));
                        }

                        // fire pressed event if needed
                        FirePressedEvent(digitalPositionNew);

                        if (digitalPosition == DigitalPosition.centered && this.PositionPress != null)
                        {
                            this.PositionPress(this, new StickEventArgs(this.position));
                        }

                        digitalPosition = digitalPositionNew;
                    }

                    position = value;
                    if (PositionChange != null)
                    {
                        PositionChange(this, new StickEventArgs(this.position));
                    }
                }
            }
        }
        override public void Reset()
        {
            // do not set property Position as it will trigger events
            position = Vector2.Zero;
            digitalPosition = DigitalPosition.centered;
            timerAutoRepeat.Clear();
        }
        override public void Update()
        {
            if (GamePadInput.GetGamePad(this.playerIndex).IsConnected)
            {
                Position = StickValue();
                
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
            position = StickValue();
            digitalPosition = CalcDigitalPosition(position);
        }
        abstract protected Vector2 StickValue();

        protected void FirePressedEvent(DigitalPosition digpos)
        {
            
            switch (digpos)
            {
                case DigitalPosition.centered:
                    break;
                case DigitalPosition.Up:
                    if (UpPress != null)
                    {
                        UpPress(this, new EventArgs());
                    }
                    break;
                case DigitalPosition.UpRight:
                    if (UpRightPress != null)
                    {
                        UpRightPress(this, new EventArgs());
                    }
                    break;
                case DigitalPosition.Right:
                    if (RightPress != null)
                    {
                        RightPress(this, new EventArgs());
                    }
                    break;
                case DigitalPosition.DownRight:
                    if (DownRightPress != null)
                    {
                        DownRightPress(this, new EventArgs());
                    }
                    break;
                case DigitalPosition.Down:
                    if (DownPress != null)
                    {
                        DownPress(this, new EventArgs());
                    }
                    break;
                case DigitalPosition.DownLeft:
                    if (DownLeftPress != null)
                    {
                        DownLeftPress(this, new EventArgs());
                    }
                    break;
                case DigitalPosition.Left:
                    if (LeftPress != null)
                    {
                        LeftPress(this, new EventArgs());
                    }
                    break;
                case DigitalPosition.UpLeft:
                    if (UpLeftPress != null)
                    {
                        UpLeftPress(this, new EventArgs());
                    }
                    break;
            }
            
        }
        protected void FireRepeatEvent(DigitalPosition digpos)
        {
            switch (digpos)
            {
                case DigitalPosition.centered:
                    break;
                case DigitalPosition.Up:
                    if (UpRepeatPress != null)
                    {
                        UpRepeatPress(this, new EventArgs());
                    }
                    break;
                case DigitalPosition.UpRight:
                    if (UpRightRepeatPress != null)
                    {
                        UpRightRepeatPress(this, new EventArgs());
                    }
                    break;
                case DigitalPosition.Right:
                    if (RightRepeatPress != null)
                    {
                        RightRepeatPress(this, new EventArgs());
                    }
                    break;
                case DigitalPosition.DownRight:
                    if (DownRightRepeatPress != null)
                    {
                        DownRightRepeatPress(this, new EventArgs());
                    }
                    break;
                case DigitalPosition.Down:
                    if (DownRepeatPress != null)
                    {
                        DownRepeatPress(this, new EventArgs());
                    }
                    break;
                case DigitalPosition.DownLeft:
                    if (DownLeftRepeatPress != null)
                    {
                        DownLeftRepeatPress(this, new EventArgs());
                    }
                    break;
                case DigitalPosition.Left:
                    if (LeftRepeatPress != null)
                    {
                        LeftRepeatPress(this, new EventArgs());
                    }
                    break;
                case DigitalPosition.UpLeft:
                    if (UpLeftRepeatPress != null)
                    {
                        UpLeftRepeatPress(this, new EventArgs());
                    }
                    break;
            }

        }
        protected void FireReleaseEvent(DigitalPosition digpos)
        {
            switch (digpos)
            {
                case DigitalPosition.centered:
                    break;
                case DigitalPosition.Up:
                    if (UpReleasePress != null)
                    {
                        UpReleasePress(this, new EventArgs());
                    }
                    break;
                case DigitalPosition.UpRight:
                    if (UpRightReleasePress != null)
                    {
                        UpRightReleasePress(this, new EventArgs());
                    }
                    break;
                case DigitalPosition.Right:
                    if (RightReleasePress != null)
                    {
                        RightReleasePress(this, new EventArgs());
                    }
                    break;
                case DigitalPosition.DownRight:
                    if (DownRightReleasePress != null)
                    {
                        DownRightReleasePress(this, new EventArgs());
                    }
                    break;
                case DigitalPosition.Down:
                    if (DownReleasePress != null)
                    {
                        DownReleasePress(this, new EventArgs());
                    }
                    break;
                case DigitalPosition.DownLeft:
                    if (DownLeftReleasePress != null)
                    {
                        DownLeftReleasePress(this, new EventArgs());
                    }
                    break;
                case DigitalPosition.Left:
                    if (LeftReleasePress != null)
                    {
                        LeftReleasePress(this, new EventArgs());
                    }
                    break;
                case DigitalPosition.UpLeft:
                    if (UpLeftReleasePress != null)
                    {
                        UpLeftReleasePress(this, new EventArgs());
                    }
                    break;
            }
            
        }
        protected const double valueEdge = 0.6;
        protected DigitalPosition CalcDigitalPosition(Vector2 value)
        {
            DigitalPosition digpos = DigitalPosition.centered;
            if (value.X < -valueEdge)
            {
                digpos = digpos | DigitalPosition.Left;
            }
            else if (value.X > valueEdge)
            {
                digpos = digpos | DigitalPosition.Right;
            }
            if (value.Y < -valueEdge)
            {
                digpos = digpos | DigitalPosition.Down;
            }
            else if (value.Y > valueEdge)
            {
                digpos = digpos | DigitalPosition.Up;
            }
            return digpos;
        }
    }
}
