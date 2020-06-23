#if DEBUG
# define FLUSH_TO_FILE
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.IO;

namespace Boku.Common
{
    /// These enumeration values persist as strings server-side in the instrumentation
    /// database, so it is safe to reorder them as you like. It is NOT safe to rename
    /// them though, as doing so may cause database queries to miss important data.
    public static partial class Instrumentation
    {
        #region Enumerations

        /// <summary>
        /// Events are unique database entries, each with its own comment attached.
        /// </summary>
        public enum EventId
        {
            None,

            // User downloaded a level from the community.
            LevelDownloaded,

            // User uploaded a level to the community.
            LevelUploaded,

            // User loaded a level to play or edit
            LevelLoaded,

            // User saves changes to a level
            LevelSaved,

            // Level that was loaded on exit
            FinalLevel,

            // Logged when the microbit tiles become enabled.
            MicrobitTilesEnabled,

            // User searched levels.
            SearchLevels,

            // Add your event ids above this comment line
            SIZEOF,
        }

        /// <summary>
        /// Counters accumulate an integer value which gets stored as a single entry
        /// in the database.
        /// </summary>
        public enum CounterId
        {
            None,

            // User defined a creatable.
            DefinedCreatable,

            // User inserted an example from a programming help card.
            ProgrammingHelpCardInsertExample,

            // User inserted an example bot from an add item help card.
            AddItemHelpCardInsertExample,

            // Delete an item from the world.
            DeleteItem,

            // Add a new item to the world.
            AddItem,

            // Cut item in world.
            CutItem,

            // Paste item in world.
            PasteItem,

            // Clone item in world.
            CloneItem,

            // Tried to add a new item but ran out of budget.
            AddItemNoBudget,

            // Chose to print the kode
            PrintKode,

            FPS_0to5,
            FPS_5to10,
            FPS_10to15,
            FPS_15to20,
            FPS_20to25,
            FPS_25to30,
            FPS_30to35,
            FPS_35to40,
            FPS_40to45,
            FPS_45to50,
            FPS_50to55,
            FPS_55to60,
            FPS_60to65,
            FPS_65to70,
            FPS_70to75,
            FPS_75to80,
            FPS_80to85,
            FPS_85to90,
            FPS_90to95,
            FPS_95to100,
            FPS_100plus,

            // Records how many microbits are attached to the computer.
            MicrobitCount,

            // Add your counter ids above this comment line
            SIZEOF,
        }

        /// <summary>
        /// Timers accumulate time and support multiple starts/stops.  The total
        /// time across all starts/stops and the number of starts/stops are stored
        /// in a single database entry.
        /// </summary>
        public enum TimerId
        {
            None,

            // Overall running time of the executable session.
            BokuSession,

            //active time spent in Kodu
            ActiveSession,
            
            //time in main menu
            MainMenuTime, 

            // Time spent in the community level browser UI.
            CommunityUI,

            //Programming time, spent creating/editing rules
            ProgrammingTime,

            // Time spent in the local level browser UI.
            LocalStorageUI,

            // Time spent in the sharing session UI.
            // Added 2/2/2009.
            SharingSessionUI,

            // Time spent looking at the Programming Help Cards.
            ProgrammingHelpCards,

            // Time spent looking at the AddItemMenu Help Cards.
            AddItemHelpCards,

            // Time spent looking at the SaveLevel dialog.
            SaveLevelDialog,

            GamePadInputTime,           // time spent with the gamepad
            KeyboardMouseInputTime,     // time spent in mouse mode

            // InGame modes.
            InGameRunSim,                   // The sim is running.
            InGameEditObjectParameters,     // Tweaking an object's params.
            InGameEditWorldParameters,      // Tweaking the world params.
            InGameEditObject,               // Normal edit mode.

            InGameWaterTool,
            InGamePaintTool,
            InGameSpikeyHillyTool,
            InGameSmoothLevelTool,
            InGameRaiseLowerTool,
            InGameDeleteTool,

            InGameLoadSave,                 // ...
            InGame,                         // In game, edit or sim mode
            MiniHubTime,                    // Home Menu

         //   InGameToolBox,                  // Top level wrapper for all world editing tools.

            // Add your timer ids above this comment line
            SIZEOF,
        }

        /// <summary>
        /// Data items are just generic name/value pairs, anything you want to store.
        /// Name field is max 255 characters. Value field is dynamic in size, growable
        /// up to 1GB characters in size.
        /// </summary>
        public enum DataItemId
        {
            None,

            // Boku's version number
            BokuVersion,

            //Update Code - Used to show provenance of install (MS or open)
            UpdateCode,

            // OS version
            OperatingSystem,

            // Video hardware
            GraphicsAdapter,

            // Screen resolution
            ScreenResolution,

            // Settings.Xml file
            SettingsXml,

            // Uniquely identifies an installation of Kodu on a machine.
            // Added 02/18/2009, Build 1.0.0.72
            InstallationUniqueId,

            // Localization setting
            Language,

            // Add your data ids above this comment line
            SIZEOF,
        }

        #endregion
    }


    /// <summary>
    /// Instrumentation API.  There are three instruments defined:
    /// Events, Counters, and Timers.
    /// </summary>
    public static partial class Instrumentation
    {
        #region Public
        
        //The list of currently active timers.
        public static Dictionary<TimerId, object> activeTimers = new Dictionary<TimerId, object>();

        public static void recordFrameRate(float fps)
        {
            int bucket = (int)fps / 5;
            switch(bucket){
                case 0:
                    IncrementCounter(CounterId.FPS_0to5);
                    break;
                case 1:
                    IncrementCounter(CounterId.FPS_5to10);
                    break;
                case 2:
                    IncrementCounter(CounterId.FPS_10to15);
                    break;
                case 3:
                    IncrementCounter(CounterId.FPS_15to20);
                    break;
                case 4:
                    IncrementCounter(CounterId.FPS_20to25);
                    break;
                case 5:
                    IncrementCounter(CounterId.FPS_25to30);
                    break;
                case 6:
                    IncrementCounter(CounterId.FPS_30to35);
                    break;
                case 7:
                    IncrementCounter(CounterId.FPS_35to40);
                    break;
                case 8:
                    IncrementCounter(CounterId.FPS_40to45);
                    break;
                case 9:
                    IncrementCounter(CounterId.FPS_45to50);
                    break;
                case 10:
                    IncrementCounter(CounterId.FPS_50to55);
                    break;
                case 11:
                    IncrementCounter(CounterId.FPS_55to60);
                    break;
                case 12:
                    IncrementCounter(CounterId.FPS_60to65);
                    break;
                case 13:
                    IncrementCounter(CounterId.FPS_65to70);
                    break;
                case 14:
                    IncrementCounter(CounterId.FPS_70to75);
                    break;
                case 15:
                    IncrementCounter(CounterId.FPS_75to80);
                    break;
                case 16:
                    IncrementCounter(CounterId.FPS_80to85);
                    break;
                case 17:
                    IncrementCounter(CounterId.FPS_85to90);
                    break;
                case 18:
                    IncrementCounter(CounterId.FPS_90to95);
                    break;
                case 19:
                    IncrementCounter(CounterId.FPS_95to100);
                    break;
                default:
                    IncrementCounter(CounterId.FPS_100plus);
                    break;
            }
        }

        /// <summary>
        /// Records an event.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="comment"></param>
        public static void RecordEvent(EventId id, string comment)
        {
            if (!Program2.SiteOptions.Instrumentation)
                return;

            if (instruments.events[(int)id] == null)
                instruments.events[(int)id] = new List<Event>();

            Event evt = new Event();
            evt.Id = id;
            evt.Comment = comment;
            instruments.events[(int)id].Add(evt);
        }

        /// <summary>
        /// Increments a counter.
        /// </summary>
        /// <param name="id"></param>
        public static void IncrementCounter(CounterId id)
        {
            if (!Program2.SiteOptions.Instrumentation)
                return;

            if (instruments.counters[(int)id] == null)
                instruments.counters[(int)id] = new Counter();

            instruments.counters[(int)id].Id = id;
            instruments.counters[(int)id].Count += 1;
        }

        /// <summary>
        /// Set the counter's value.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        public static void SetCounter(CounterId id, int value)
        {
            if (!Program2.SiteOptions.Instrumentation)
                return;

            if (instruments.counters[(int)id] == null)
                instruments.counters[(int)id] = new Counter();

            instruments.counters[(int)id].Id = id;
            instruments.counters[(int)id].Count = value;
        }

        /// <summary>
        /// Starts a timer.  Timers are not stored until they are stopped (via StopTimer).
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static object StartTimer(TimerId id)
        {
            if (!Program2.SiteOptions.Instrumentation)
                return null;

            ActiveTimer timer = new ActiveTimer();
            timer.id = id;

            // For some reason we occasionally get duplicate entries.
            // Assert in debug mode but don't let it crash.
            Debug.Assert(!activeTimers.ContainsKey(timer.id), "Why are we adding a duplicate key?");

            // Remove timer if already in dictionary.  This shouldn't
            // happen but prevents crashes in release mode.
            activeTimers.Remove(timer.id);

            activeTimers.Add(timer.id, timer);

            //use the instrumentation clock which only represents active time
            timer.startTime = Time.InstrumentationTotalSeconds;//Time.WallClockTotalSeconds;
            return timer;
        }

        
        /// <summary>
        /// Stops a timer.
        /// </summary>
        /// <param name="timerObj"></param>
        public static void StopTimer(object timerObj)
        {
            if (!Program2.SiteOptions.Instrumentation)
                return;

            ActiveTimer timer = timerObj as ActiveTimer;

            if (instruments.timers[(int)timer.id] == null)
            {
                instruments.timers[(int)timer.id] = new Timer();
                instruments.timers[(int)timer.id].Id = timer.id;
            }

            if(activeTimers.ContainsKey(timer.id)) {
                activeTimers.Remove(timer.id);
            }

            //use the instrumentation clock which only represents active time
            instruments.timers[(int)timer.id].TotalTime += (Time.InstrumentationTotalSeconds - timer.startTime);
            instruments.timers[(int)timer.id].Count += 1;
        }

        /*
         * When the user does not exit through the menu, then not all
         * timers get stopped. This method is called to stop all remaining
         * timers. Without it, we are missing quite a bit of timer information
         * upon exit. 
         */
        public static void StopAllTimers()
        {
            foreach(KeyValuePair<TimerId, object> pair in activeTimers) {
                //StopTimer(pair.Value);
                ActiveTimer timer = pair.Value as ActiveTimer;
                if (instruments.timers[(int)timer.id] == null)
                {
                    instruments.timers[(int)timer.id] = new Timer();
                    instruments.timers[(int)timer.id].Id = timer.id;
                }
                //use the instrumentation clock which only represents active time
                instruments.timers[(int)timer.id].TotalTime += (Time.InstrumentationTotalSeconds - timer.startTime);
                instruments.timers[(int)timer.id].Count += 1;
            }
            activeTimers = new Dictionary<TimerId, object>();
        }

        public static void RecordDataItem(DataItemId id, string value)
        {
            if (!Program2.SiteOptions.Instrumentation)
                return;

            if (instruments.dataItems[(int)id] == null)
                instruments.dataItems[(int)id] = new List<DataItem>();

            DataItem item = new DataItem();
            item.Id = id;
            item.Value = value;
            instruments.dataItems[(int)id].Add(item);
        }

        /// <summary>
        /// Flushes stored instrument readings to the server.
        /// </summary>
        /// <param name="callback"></param>
        public static bool Flush(SendOrPostCallback callback)
        {
            if (!Program2.SiteOptions.Instrumentation)
                return false;

            FlushState state = new FlushState();
            state.instruments = instruments;
            state.callback = callback;

#if FLUSH_TO_FILE
            {
                try
                {
                    Stream s = Storage4.OpenWrite("instruments.txt");
                    using (StreamWriter writer = new StreamWriter(s))
                    {
                        // Write events
                        for (int i = 0; i < (int)EventId.SIZEOF; ++i)
                        {
                            List<Event> l = instruments.events[i];
                            if (l == null)
                                continue;
                            foreach (Event q in l)
                            {
                                writer.WriteLine(String.Format("Event: {0}, {1}", q.Id, q.Comment));
                            }
                        }
                        // Write counters
                        for (int i = 0; i < (int)CounterId.SIZEOF; ++i)
                        {
                            Counter q = instruments.counters[i];
                            if (q == null)
                                continue;
                            writer.WriteLine(String.Format("Counter: {0}, {1}", q.Id, q.Count));
                        }
                        // Write timers
                        for (int i = 0; i < (int)TimerId.SIZEOF; ++i)
                        {
                            Timer q = instruments.timers[i];
                            if (q == null)
                                continue;
                            writer.WriteLine(String.Format("Timer: {0}, {1}, {2}", q.Id, q.TotalTime, q.Count));
                        }
                        // Write data items
                        for (int i = 0; i < (int)DataItemId.SIZEOF; ++i)
                        {
                            List<DataItem> l = instruments.dataItems[i];
                            if (l == null)
                                continue;
                            foreach (DataItem q in l)
                            {
                                writer.WriteLine(String.Format("DataItem: {0}, {1}", q.Id, q.Value));
                            }
                        }
                        writer.Flush();
                        writer.Close();
                    }
                    s.Close();
                }
                catch
                {
                }
            }
#endif

            Boku.Web.Trans.Instrumentation trans = new Boku.Web.Trans.Instrumentation(
                instruments,
                Flush_Callback,
                state);

            // Make a new, empty set of instruments.
            instruments = new Instruments();

            return trans.Send();
            //return false;
        }

        #endregion
    }


    /// Internal machinations of the Instrumentation class.
    public static partial class Instrumentation
    {
        #region Internal

        internal class Event
        {
            public EventId Id = EventId.None;
            public string Comment = String.Empty;
        }

        internal class Timer
        {
            public TimerId Id = TimerId.None;
            public double TotalTime = 0.0;
            public int Count = 0;
        }

        internal class Counter
        {
            public CounterId Id = CounterId.None;
            public int Count = 0;
        }

        internal class DataItem
        {
            public DataItemId Id = DataItemId.None;
            public string Value = String.Empty;
        }

        internal class Instruments
        {
            public List<Event>[] events = new List<Event>[(int)EventId.SIZEOF];
            public Timer[] timers = new Timer[(int)TimerId.SIZEOF];
            public Counter[] counters = new Counter[(int)CounterId.SIZEOF];
            public List<DataItem>[] dataItems = new List<DataItem>[(int)DataItemId.SIZEOF];
        }

        class ActiveTimer
        {
            public TimerId id = TimerId.None;
            public double startTime = 0.0;
        }

        static Instruments instruments = new Instruments();

        class FlushState
        {
            public Instruments instruments;
            public SendOrPostCallback callback;
        }

        static void Flush_Callback(object param)
        {
            Boku.Web.Trans.Instrumentation.Result result = (Boku.Web.Trans.Instrumentation.Result)param;

            FlushState state = (FlushState)result.userState;

            if (!result.success)
            {
                // Write instruments to disk for retry later?
            }

            if (state.callback != null)
                state.callback(null);
        }

        #endregion
    }
}
