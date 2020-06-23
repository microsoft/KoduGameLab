using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Common;

namespace Boku.Base
{
    public delegate void TimerEvent(GameTimer timer);

    /// <summary>
    /// This represents a simple timer
    /// construct it with the type of clock and number of seconds it should time out
    ///
    /// Call Start to start the timer and run it automatically
    /// Attach an event to the TimerElapsed, it will get called when time has elapsed
    /// Elapsed will return true if the time has elapsed
    /// 
    /// example:
    ///     GameTimer timer = new GameTimer( GameTimer.ClockType.WallClock. 3.0f );
    ///     timer.TimerElapsed += MyEventHandler;
    ///     timer.Start();
    /// 
    /// Advanced:  This supports a owner update called method; which is used when the owner
    /// wants to control the life of timer and call its update routine.  This is not a normal use
    /// and is supported for a few special cases in the inputcommand work.
    /// 
    /// </summary>
    public class GameTimer
    {
        protected enum State
        {
            Running,
            Paused,
            Elapsed,
        }
        protected State state = State.Paused;

        /// <summary>
        /// Start time for current timer.
        /// </summary>
        protected double startTime = double.NegativeInfinity;

        /// <summary>
        /// Timeout duration for this timer.
        /// </summary>
        protected double seconds = 1.0;

        /// <summary>
        /// Defines if the clock is real time or game time
        /// </summary>
        public enum ClockType
        {
            GameClock, // running at sim time, not real world time
            WallClock, // running at real world time
        }
        protected ClockType clock = ClockType.GameClock;
        /// <summary>
        /// Event fired when timer has elapsed
        /// </summary>
        public event TimerEvent TimerElapsed;

        /// <summary>
        /// Constructor for a GameTimer
        /// </summary>
        /// <param name="clock">Real Time or Game Time</param>
        /// <param name="seconds">Seconds to time out on</param>
        public GameTimer(ClockType clock, double seconds)
        {
            this.clock = clock;
            this.seconds = seconds;
            if (this.seconds == 0)
            {
                this.seconds = 1.0f;
            }
        }
        /// <summary>
        /// Constructor for a GameTimer 
        /// </summary>
        /// <param name="clock">Real Time or Game Time</param>
        public GameTimer(ClockType clock)
        {
            this.clock = clock;
            this.seconds = double.NegativeInfinity;
        }
        /// <summary>
        /// Update the GameTimer and process events
        /// Called by the TimerManager by defualt
        /// 
        /// Advanced: With the owner called update model, the owner must call this explicitly in its update
        /// </summary>
        public bool Update()
        {
            Debug.Assert(this.seconds >= 0.0);

            double elapsedTime = 0;
            if (this.clock == ClockType.WallClock)
            {
                elapsedTime = Time.WallClockTotalSeconds - startTime;
            }
            else
            {
                elapsedTime = Time.GameTimeTotalSeconds - startTime;
            }

            bool elapsed = elapsedTime >= seconds;

            if (elapsed)
            {
                this.state = State.Elapsed;
                GameTimerManager.DetachTimer(this);

                if (TimerElapsed != null)
                {
                    TimerElapsed(this);
                }
            }
            return elapsed;
        }
        /// <summary>
        /// Resets the game timer to the time given
        /// 
        /// Advanced: If using owner called update it also sets the state to allow it to run
        /// </summary>
        /// <param name="seconds"></param>
        public void Reset(double seconds)
        {
            startTime = clock == ClockType.WallClock ? Time.WallClockTotalSeconds : Time.GameTimeTotalSeconds;
            this.seconds = seconds;
            if (this.seconds == 0)
            {
                this.seconds = 1.0f;
            }
            this.state = State.Running;
        }
        /// <summary>
        /// Advanced: primarily used for owner called update model to stop the timer
        /// </summary>
        public void Clear()
        {
            this.seconds = double.NegativeInfinity;
            this.state = State.Elapsed;
        }
        /// <summary>
        /// Starts the timer
        /// 
        /// Advanced: should not be called for owner called update model
        /// </summary>
        public void Start()
        {
            this.state = State.Running;

            // If starting for the first time, just grab the current time.
            startTime = clock == ClockType.WallClock ? Time.WallClockTotalSeconds : Time.GameTimeTotalSeconds;            
            
            GameTimerManager.AttachTimer(this);
        }

        /// <summary>
        /// Restarts the timer but resets the start time based on the previous start time.
        /// This prevents errors from accumulating since timeout values don't sync perfectly
        /// with frame times.
        /// </summary>
        /// <param name="sync">If sync is true, we want to restart the timer in such a way as to take the previous start time into account.  If sync is false, we want to just start from "now".</param>
        public void ReStart(bool sync)
        {
            this.state = State.Running;

            // Need to adjust start time so that it is in range (curTime - seconds, curTime]
            double curTime = clock == ClockType.WallClock ? Time.WallClockTotalSeconds : Time.GameTimeTotalSeconds;

            if (sync)
            {
                while (startTime <= curTime)
                {
                    startTime += seconds;
                }
                startTime -= seconds;
            }
            else
            {
                startTime = curTime;
            }

            GameTimerManager.AttachTimer(this);
        }

        /// <summary>
        /// Stops the timer
        /// 
        /// Advanced: should not be called for owner called update model
        /// </summary>
        public void Stop()
        {
            this.state = State.Paused;
            GameTimerManager.DetachTimer(this);
        }
        /// <summary>
        /// Check if the timer has elapsed
        /// </summary>
        public bool Elapsed
        {
            get
            {
                return this.state == State.Elapsed;
            }
        }
        /// <summary>
        /// Check if the timer is running
        /// Note that it may have naturally elapsed
        /// </summary>
        public bool Running
        {
            get
            {
                return this.state == State.Running;
            }
        }
    }

    /// <summary>
    /// Manages all GameTimers
    /// Used by the GameTimer and Games main update loop
    /// 
    /// Advanced: except not using the owner called udpate model
    /// </summary>
    public class GameTimerManager
    {
        protected static List<GameTimer> timers = new List<GameTimer>();

        public static void AttachTimer( GameTimer gameTimer )
        {
            if (!timers.Contains(gameTimer))
            {
                timers.Add(gameTimer);
            }
        }
        public static void DetachTimer(GameTimer gameTimer)
        {
            if (timers.Contains(gameTimer))
            {
                timers.Remove(gameTimer);
            }
        }

        public static void Update()
        {
            int indexTimer = 0;
            int countTimers = timers.Count; // cache this as to not run new ones this cycle
            while (indexTimer < countTimers)
            {
                GameTimer gameTimer = timers[indexTimer] as GameTimer;
                if (gameTimer.Update())
                {
                    countTimers--; // one less now
                }
                else
                {
                    indexTimer++;
                }
            }
        }
    }
}
