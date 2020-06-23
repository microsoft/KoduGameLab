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

using Boku.Base;

namespace Boku.Input
{
    public delegate void InputCommandButtonDelegate( Object sender, EventArgs args);


    /// <summary>
    /// This represents an intermediate abstraction specific to push buttons
    /// It will manage two states (wasPressed and wasReleased) for this input command
    /// 
    /// There is currently no need to use this class outside this module as all
    /// usefull classes are represented below
    /// </summary>
    abstract public class ButtonCommand : InputCommand
    {
        protected enum ButtonCommandState
        {
            Unknown,
            Press,
            Repeat,
            ReleasePress,
        }
        /// <summary>
        /// Current pressed state of this command
        /// </summary>
        protected ButtonCommandState state;
        // NOTE: do not call Start on this timer, it is being used in a caller update model
        protected Boku.Base.GameTimer timerAutoRepeat = new Boku.Base.GameTimer(Boku.Base.GameTimer.ClockType.WallClock);

        /// <summary>
        /// Press event, when state changes from released to pressed these are triggered
        /// </summary>
        public event InputCommandButtonDelegate Press;
        /// <summary>
        /// RepeatPress event, when the state continues to be pressed, called at a repeat interval
        /// </summary>
        public event InputCommandButtonDelegate RepeatPress;
        /// <summary>
        /// ReleasePress event, when state changes from pressed to released these are triggered
        /// </summary>
        public event InputCommandButtonDelegate ReleasePress;

        protected ButtonCommand()
        {
            Reset();
            timerAutoRepeat.TimerElapsed += OnRepeatPress;
        }

        protected void OnRepeatPress(Boku.Base.GameTimer timer)
        {
            // NOTE: do not call Start on this timer, it is being used in a caller update model
            timerAutoRepeat.Reset(timeAutoRepeat);
            if (RepeatPress != null)
            {
                RepeatPress(this, new System.EventArgs());
            }
            this.state = ButtonCommandState.Repeat;
        }

        override public void Reset()
        {
            state = ButtonCommandState.Unknown;
            timerAutoRepeat.Clear();
        }

        [XmlAttribute]
        public string BindPress;
        [XmlAttribute]
        public string BindRepeatPress;
        [XmlAttribute]
        public string BindReleasePress;

        /// <summary>
        /// Is the button currently being pressed down
        /// </summary>
        [XmlIgnore]
        public bool Pressed
        {
            get
            {
                return (state == ButtonCommandState.Press || state == ButtonCommandState.Repeat);
            }
            protected set
            {
                if (value)
                {
                    if (state == ButtonCommandState.ReleasePress)
                    {
                        // NOTE: do not call Start on this timer, it is being used in a caller update model
                        timerAutoRepeat.Reset(timeFirstAutoRepeat);
                        if (Press != null)
                        {
                            Press( this, new EventArgs() );
                            /* asyncronous event example; really not interesting for input
                            foreach (InputCommandButtonDelegate sync in Press.GetInvocationList())
                            {
                                sync.BeginInvoke( this,
                                        new EventArgs(),
                                        new AsyncCallback(PressInvokeCompleted),
                                        sync);
                            }
                             */
                        }
                    }
                    state = ButtonCommandState.Press;
                }
                else
                {
                    if (state != ButtonCommandState.ReleasePress)
                    {
                        timerAutoRepeat.Clear();
                        if (ReleasePress != null)
                        {
                            ReleasePress(this, new System.EventArgs());
                        }
                    }
                    state = ButtonCommandState.ReleasePress;
                }
            }
        }
            /* asyncronous event example; really not interesting for input
            private void PressInvokeCompleted(IAsyncResult result)
            {
                InputCommandButtonDelegate e = (InputCommandButtonDelegate)result.AsyncState;
                e.EndInvoke(result);
            }
             */ 
    }
}
