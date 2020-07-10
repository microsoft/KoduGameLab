// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


#if DEBUG
#define DO_PERF_TIMING
#endif

using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;


namespace Boku.Common
{
    /// <summary>
    /// Primarily for internal use this is a class that will hopefully make it 
    /// easier to measue perf and understand where the hot spots are in the code.
    /// </summary>
    public class PerfTimer
    {
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

#if DO_PERF_TIMING
        private bool valid = true;
        private string label;
        private double wallClockStart = 0.0;
        private float reportRate = 0.00000001f;        // How many seconds between reports.
        private int samples = 0;

        private long totalTime = 0;
        private long startTime = 0;
        private long freq = 0;

        private long minSample = long.MaxValue;
        private long maxSample = long.MinValue;
#endif

        // c'tor
        public PerfTimer(string label)
        {
#if DO_PERF_TIMING
            this.label = label;

            Reset();
#endif
        }   // end of PerfTimer c'tor

        public PerfTimer(string label, float reportRate)
        {
#if DO_PERF_TIMING
            this.label = label;
            this.reportRate = reportRate;

            Reset();
#endif
        }   // end of PerfTimer c'tor

        /// <summary>
        /// Start the timer.
        /// </summary>
        public void Start()
        {
#if DO_PERF_TIMING
            if (valid)
            {
                QueryPerformanceCounter(out startTime);
            }
#endif
        }   // end of PerfTimer Start()

        /// <summary>
        /// Stop the timer, outputting accumulated info if enough time
        /// has passed as determined by the reportRate.
        /// </summary>
        public void Stop(string sampleLabel = default(string))
        {
#if DO_PERF_TIMING
            if (valid)
            {
                long stopTime = 0;
                QueryPerformanceCounter(out stopTime);
                long delta = stopTime - startTime;
                //delta -= 7500L;     // Remove overhead of timer functions.  Note this is
                // minimum overhead as opposed to average or maximum.
                totalTime += delta;
                ++samples;

                minSample = Math.Min(minSample, delta);
                maxSample = Math.Max(maxSample, delta);

                startTime = stopTime;

                double now = (double)stopTime / (double)freq;
                if (now - wallClockStart > reportRate)
                {
                    double ns = 1000 * 1000 * (double)totalTime / (double)freq / (double)samples;
                    double min = 1000 * 1000 * (double)minSample / (double)freq;
                    double max = 1000 * 1000 * (double)maxSample / (double)freq;
                    minSample = freq;
                    maxSample = 0;
#if NETFX_CORE
                    Debug.WriteLine(label + " : " + ns.ToString("f2") + "ns");
                    //Debug.WriteLine("    min : " + minSample.ToString());
                    //Debug.WriteLine("    max : " + maxSample.ToString());
#else
                    if (ns > 1000 * 1000 || true)
                    {
                        Debug.Print(label + " : " + sampleLabel + " : " + (ns/1000.0/1000.0).ToString("f2") + "sec");
                    }
                    else
                    {
                        Debug.Print(label + " : " + sampleLabel + " : " + ns.ToString("f2") + "ns");
                    }
                    //Debug.Print("    min : " + minSample.ToString());
                    //Debug.Print("    max : " + maxSample.ToString());
#endif
                    //Console.WriteLine(label + " : " + ns.ToString("f2") + "ns" + "    min : " + min.ToString("f2") + "    max : " + max.ToString("f2"));

                    Reset();
                }
            }
#endif
        }   // end of PerfTimer Stop()

        private void Reset()
        {
#if DO_PERF_TIMING
            if (QueryPerformanceFrequency(out freq) == false)
            {
                // High-performance counter not supported.
                valid = false;
            }

            long curTime = 0;
            QueryPerformanceCounter(out curTime);
            wallClockStart = (double)curTime / (double)freq;

            samples = 0;
            totalTime = 0;

            //minSample = long.MaxValue;
            //maxSample = long.MinValue;
#endif
        }   // end of PerfTimer Reset()

    }   // end of class PerfTimer

}   // end of namespace Boku.Common


