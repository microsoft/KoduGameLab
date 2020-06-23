#if DEBUG
//#define Debug_CountTerrainVerts
#endif

using System;
using System.Collections;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;


namespace Boku.Common
{
    /// <summary>
    /// Using the GameTime class it's not as easy to support slow motion, 
    /// fast forward, pausing etc as it should be.  This class just wraps 
    /// GameTime and gives us a clear differentiation between wall clock time
    /// and the in game simulation time.
    /// 
    /// WallClock values are real world times.
    /// GameTime values may reflect sped up, slowed down or paused game time.
    /// 
    /// All the class accessors are static since there's only one time.
    /// </summary>
    public class Time
    {
        private static double wallClockTotalSeconds = 0.0;      // Total real world seconds since game started.
        private static double gameTimeTotalSeconds = 0.0;       // Total game time seconds since game started.
        private static double instrumentationTotalSeconds = 0.0; //Total active game seconds since game started
        private static float wallClockFrameSeconds = 0.0f;      // Real world time since last call to Update().
        private static float gameTimeFrameSeconds = 0.0f;       // Game time seconds since last call to Update().

        private static float clockRatio = 1.0f;                 // Ratio of game time / real world.  
        // Set to 0 to pause although calling Pause is better since you can then
        // call Resume() without having to remember what speed you were running at.  
        // 0.5 to have game run at half speed.
        // 2.0 to have game run at double speed.
        // Negative values may cause your brain to implode.
        private static float savedRatio = 1.0f;                 // Ratio to return to after a pause.
        private static bool paused = false;
        private static int frameCounter = 0;                    // Total frame counter.
        private static int nonPausedFrameCounter = 0;           // Frame count while not paused.  Allows timers to not re-sync when coming out of pause mode.
        private static float fps = 60.0f;                       // Current frame rate.

        //variables to account for inactivity in the game, used for instrumentation
        private static bool activeGameClock;                    // Indicates if the user is active in the game
        public static double inactiveTimeThreshold = 60.0;      // seconds of allowed inactivity before stopping the clock
        private static double startInactiveTime = 0.0;          // timer that counts the current number of inactive seconds

        private static bool skippingFrames = false;             // On low end machines we can sometimes skip rendering the world
                                                                // to provide better UI responsiveness.
        private const double kSkipStartFrameTime = 0.1;         // If frame time is longer than this, start skipping.
        private const double kSkipStopFrameTime = 0.07;         // If frame time is shorter than this, stop skipping.
        private const int kFrameSkipModValue = 5;
        private static double[] PrevFrames = new double[kFrameSkipModValue];    // Previous N frame times for FrameSkip mode.

        static Stopwatch timer = new Stopwatch();

        #region Accessors

        public static double StartInactiveTime
        {
            set { startInactiveTime = value; }
            get { return startInactiveTime; }
        }

        /// <summary>
        /// Total real world seconds of active time since game started.
        /// </summary>
        public static double InstrumentationTotalSeconds
        {
            get { return instrumentationTotalSeconds; }
        }

        static object sessionTimerInstrument;

        /// <summary>
        /// Indicates if the instrumentation clock needs to be active
        /// </summary>
        public static bool ActiveGameClock
        {
            set
            {
                if (activeGameClock != value)
                {
                    if (value == true)
                    {
                        sessionTimerInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.ActiveSession);
                    }
                    else
                    {
                        Instrumentation.StopTimer(sessionTimerInstrument);
                    }
                }
               activeGameClock = value;
            }
            get { return activeGameClock; }
        }

        /// <summary>
        /// Total real world seconds since game started.
        /// </summary>
        public static double WallClockTotalSeconds
        {
            get { return wallClockTotalSeconds; }
        }
        /// <summary>
        /// Total game time seconds since game started.
        /// </summary>
        public static double GameTimeTotalSeconds
        {
            get { return gameTimeTotalSeconds; }
        }
        /// <summary>
        /// Real world time since last call to Update().
        /// </summary>
        public static float WallClockFrameSeconds
        {
            get { return wallClockFrameSeconds; }
        }
        /// <summary>
        /// Game time seconds since last call to Update().
        /// </summary>
        public static float GameTimeFrameSeconds
        {
            get { return gameTimeFrameSeconds; }
        }
        /// <summary>
        /// Number of frames since starting.
        /// </summary>
        public static int FrameCounter
        {
            get { return frameCounter; }
        }
        /// <summary>
        /// Number of frames not counting those that occur while game is paused.
        /// Used by timers to tell the difference between starting for the first
        /// time and resuming after a pause.
        /// </summary>
        public static int NonPausedFrameCounter
        {
            get { return nonPausedFrameCounter; }
        }
        /// <summary>
        /// Current frame rate in frames per second.
        /// </summary>
        public static float FrameRate
        {
            get { return fps; }
        }

#if DEBUG
        static public string DebugString = "";
#endif 

        /// <summary>
        /// Returns a string with the current frame rate and other timing info.
        /// </summary>
        public static string FrameRateString
        {
            get
            {
                string fps = null;
#if DEBUG
#if Debug_CountTerrainVerts
                fps = String.Format("{0:F1} fps ave:{1:####.00}ms verts:{2:####} tris:{3:####}", Time.FrameRate, 1000.0f / Time.FrameRate, Boku.SimWorld.Terra.Terrain.VertCounter_Debug, Boku.SimWorld.Terra.Terrain.TriCounter_Debug);
#else
                fps = String.Format("{0:F1} fps ave:{3:####.00}ms ", Time.FrameRate, minMS, maxMS, 1000.0f / Time.FrameRate) + " " + DebugString;
#endif
#else
                fps = String.Format("{0:F1} fps", Time.FrameRate);
#endif
                return fps;
            }
        }

        /// <summary>
        /// Ratio of game time / real world.  
        /// Set to 0 to pause although setting Paused to true is better since you can then
        /// set it back to false without having to remember what speed you were running at.  
        /// 0.5 to have game run at half speed.
        /// 2.0 to have game run at double speed.
        /// Negative values may cause your brain to implode.
        /// </summary>
        public static float ClockRatio
        {
            get { return clockRatio; }
            set
            {
                savedRatio = clockRatio;
                clockRatio = value;
                paused = clockRatio == 0.0f;
            }
        }
        /// <summary>
        /// Pause/unPause the passing of in-game seconds.
        /// </summary>
        public static bool Paused
        {
            get { return paused; }
            set
            {
                if (value != paused)
                {
                    paused = value;
                    if (paused)
                    {
                        savedRatio = clockRatio;
                        clockRatio = 0.0f;
                        //BokuGame.Audio.PauseGameAudio();
                    }
                    else
                    {
                        clockRatio = savedRatio;
                        //BokuGame.Audio.ResumeGameAudio();
                    }
                }
            }
        }

        /// <summary>
        /// Are we skipping rendering the world in order to provide better
        /// UI responsiveness?  Only works with SM2 and effects off.
        /// </summary>
        public static bool SkippingFrames
        {
            get { return skippingFrames; }
        }

        /// <summary>
        /// When skipping frames we only render the world every Nth frame.  This is N.
        /// </summary>
        public static int FrameSkipModValue
        {
            get { return kFrameSkipModValue; }
        }

        /// <summary>
        /// Should we skip rendering the world this frame?
        /// </summary>
        public static bool SkipThisFrame
        {
            get { return SkippingFrames && ((FrameCounter % kFrameSkipModValue) != 0); }
        }

        #endregion

        // c'tor
        private Time()
        {
        }   // end of Time c'tor

        // Counters for calculating frame rate.
        private static double elapsedTime = 0.0;
        private static int elapsedFrames = 0;
        private static double sampleTime = 0.5;     // Update every half of a second.
        private static double minFrame = 0.0;       // Min frame time during sample period.
        private static double maxFrame = 0.0;       // Max frame time during sample period.
        private static float minMS = 0.0f;
        private static float maxMS = 0.0f;

        // If we received no user input, check if the user is active in the
        // game. If the game is active, and the inactive timer counter is 0.0,
        // start the inactive time counter. Otherwise, check if we have been
        // inactive for a longer period of time than the threshold.
        public static void startInactiveCheck()
        {
            if (Time.ActiveGameClock == true)
            {
                if (Time.StartInactiveTime == 0.0)
                {
                    StartInactiveTime = wallClockTotalSeconds;
                }
                else
                {
                    if (Time.WallClockTotalSeconds - Time.StartInactiveTime > Time.inactiveTimeThreshold)
                    {
                        Time.ActiveGameClock = false;
                    }
                }
            }
        }

        // Called if the game is inactive and we receive user input. Resets the
        // inactive time counter and sets the active flag to true.
        public static void startActiveInstrumentationClock()
        {
            Time.StartInactiveTime = 0.0;
            Time.ActiveGameClock = true;
        }

        // Keep the last frame time so we can deal with the timer wrapping around.
        private static double lastTotalSeconds = 0.0f;
        static int emptyFrames = 2;

        public static void Update()
        {
            // This seems stupid but for some reason it stops the UI from getting stuck at startup.
            if (emptyFrames > 0)
            {
                --emptyFrames;
                if (emptyFrames == 0)
                {
                    timer.Start();
                }
            }

            double seconds = timer.Elapsed.TotalSeconds;
            double delta = seconds - lastTotalSeconds;
            lastTotalSeconds = seconds;

            // If we get a glitch, just use the time from last frame.
            // This should be fixed with the refresh.
            if (delta < 0.0f)
            {
                delta = wallClockFrameSeconds;
            }

            // Check if we're in lores mode.  If so, also check if we should be skipping frames.
            if (BokuSettings.Settings.PostEffects == false && BokuGame.HiDefProfile == false)
            {
                int index = FrameCounter % PrevFrames.Length;
                PrevFrames[index] = delta;

                // Find the max delta in PrevFrames and use that to control 
                // going in and out of frame skip mode. 
                // If this is the frame after the one where we always render the full world,
                // check the frame time and decide whether or not to turn skipping on or off.
                double maxDelta = PrevFrames[0];
                for (int i = 1; i < PrevFrames.Length; i++)
                {
                    maxDelta = Math.Max(maxDelta, PrevFrames[i]);
                }
                /*
                if (maxDelta > kSkipStartFrameTime)
                    skippingFrames = true;
                else if (maxDelta < kSkipStopFrameTime)
                    skippingFrames = false;
                */
                /*
                skippingFrames = true;

                if ((FrameCounter % 100) == 0)
                {
                    Debug.Print("---");
                    Debug.Print(PrevFrames[0].ToString());
                    Debug.Print(PrevFrames[1].ToString());
                    Debug.Print(PrevFrames[2].ToString());
                    Debug.Print(PrevFrames[3].ToString());
                    Debug.Print(PrevFrames[4].ToString());
                }
                */

                // If we're in skipping frames mode, use the average of the last
                // N frames since we're only rendering every Nth frame.
                if (skippingFrames)
                {
                    delta = 0;
                    for (int i = 0; i < kFrameSkipModValue; i++)
                    {
                        delta += PrevFrames[i];
                    }
                    delta /= kFrameSkipModValue;
                }
            }

            // Debugging aid, if we're in the debugger then things jump 
            // around too much so use a minimum of 1/5 of a second.
            delta = Math.Min(1.0 / 5.0, delta);

            wallClockFrameSeconds = (float)delta;
            wallClockTotalSeconds += delta;

            //wallClockSecondsInt = (int)wallClockTotalSeconds * 1;
            //if (wallClockSecondsInt % 10 == 0)
            //{
            //    Instrumentation.recordFrameRate(fps);
            //}

            // This check is used to make sure we only update the instrumentation
            // timer when the game is active.
            if (ActiveGameClock == true)
            {
                instrumentationTotalSeconds += delta;
            }
            //else //for debugging only
            //{
            //    Console.WriteLine("Wall Clock " + wallClockTotalSeconds);
            //    Console.WriteLine("Instrumentation Clock " + instrumentationTotalSeconds);
            //}

            // Calc fps.
            elapsedTime += wallClockFrameSeconds;
            ++elapsedFrames;

            minFrame = Math.Min(minFrame, delta);
            maxFrame = Math.Max(maxFrame, delta);

            if (elapsedTime > sampleTime)
            {
                fps = (float)(elapsedFrames / elapsedTime);

                elapsedTime = 0.0;
                elapsedFrames = 0;

                minMS = (float)(minFrame * 1000.0);
                maxMS = (float)(maxFrame * 1000.0);
                minFrame = delta;
                maxFrame = delta;

                //for recording frame rate, sample every time fps is updated,
                // as long as the game is active (i.e., do not sample during
                // idle time) and in RunSim mode
                if (ActiveGameClock == true
                    && InGame.inGame != null
                    && InGame.inGame.CurrentUpdateMode == InGame.UpdateMode.RunSim
                    && InGame.inGame.State == InGame.States.Active)
                {
                    Instrumentation.recordFrameRate(fps);
                }
            }

            delta *= clockRatio;
            gameTimeFrameSeconds = (float)delta;
            gameTimeTotalSeconds += delta;

            // Prevent dividing by 0 on the first frame.
            if (wallClockFrameSeconds < 0.0001f)
            {
                wallClockFrameSeconds = 1.0f / 60.0f;
            }

            ++frameCounter;
            if (!Paused)
            {
                ++nonPausedFrameCounter;
            }

        }   // end of Time Update()

    }   // end of class Time

}   // end of namespace BokuCommon




