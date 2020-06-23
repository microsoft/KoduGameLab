using System;
using System.Collections.Generic;

namespace Boku.Common
{
    /// <summary>
    /// A fairly general-purpose implementation of ILevelSetQuery.
    /// Currently used for browsing local and community levels.
    /// We'll see whether it works for p2p sharing sessions...
    /// </summary>
    public class LevelSetQuery : ILevelSetQuery
    {
        object synch = new object();
        public object Synch { get { return synch; } }

        ILevelSetFilter filter;
        ILevelSetSorter sorter;
        ILevelBrowser browser;

        LevelSetQueryEvent notifyFetchingCallback;
        LevelSetQueryEvent notifyFetchCompleteCallback;

        List<LevelSetCursor> cursors = new List<LevelSetCursor>();
        List<LevelMetadata> levels = new List<LevelMetadata>();
        List<LevelMetadata> additions = new List<LevelMetadata>();
        List<LevelMetadata> removals = new List<LevelMetadata>();

        bool notifyCursors;

        public LevelMetadata this[int i]
        {
            get
            {
                lock (Synch)
                {
                    if (i >= 0 && i < levels.Count)
                    {
                        return levels[i];
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        public bool IsEmpty
        {
            get { lock (Synch) { return levels.Count == 0; } }
        }

        public ILevelSetFilter Filter
        {
            get { return filter; }
        }

        public ILevelSetSorter Sorter
        {
            get { return sorter; }
        }

        public LevelSetQuery(
            ILevelSetSorter sorter,
            ILevelSetFilter filter,
            ILevelBrowser browser,
            LevelSetQueryEvent notifyFetchingCallback,
            LevelSetQueryEvent notifyFetchCompleteCallback)
        {
            this.sorter = sorter;
            this.filter = filter;
            this.browser = browser;
            this.notifyFetchingCallback = notifyFetchingCallback;
            this.notifyFetchCompleteCallback = notifyFetchCompleteCallback;
        }

        public void AddCursor(ILevelSetCursor cursor)
        {
            cursors.Add((LevelSetCursor)cursor);
        }

        public int RemoveCursor(ILevelSetCursor cursor)
        {
            cursors.Remove((LevelSetCursor)cursor);
            return cursors.Count;
        }

        public void LevelAdded(LevelMetadata level)
        {
            if (filter.Matches(level))
            {
                lock (Synch)
                {
                    additions.Add(level);
                    notifyCursors = true;
                }
            }
        }

        public void LevelRemoved(LevelMetadata level)
        {
            bool matches = filter.Matches(level);
            if (matches)
            {
                lock (Synch)
                {
                    removals.Add(level);
                    notifyCursors = true;
                }
            }
        }

        public void Update()
        {
            if (filter.Dirty || sorter.Dirty)
            {
                lock (Synch)
                {
                    Clear();
                    browser.LoadQuery(this);
                    filter.Dirty = false;
                    sorter.Dirty = false;
                }
            }

            if (notifyCursors)
            {
                lock (Synch)
                {
                    notifyCursors = false;

                    for (int i = 0; i < removals.Count; ++i)
                    {
                        LevelMetadata level = removals[i];
                        RemoveLevel_Synched(level);
                    }

                    for (int i = 0; i < additions.Count; ++i)
                    {
                        LevelMetadata level = additions[i];
                        AddLevel_Synched(level);
                    }

                    removals.Clear();
                    additions.Clear();
                }
            }

            lock (Synch)
            {
                for (int i = 0; i < cursors.Count; ++i)
                {
                    ILevelSetCursor cursor = cursors[i];

                    // This is really here just for the community browser, so that it
                    // will automatically fetch more levels from the server when the
                    // user gets close to the end of the current set.
                    if ((levels.Count - cursor.QueryPointer) < 8)
                    {
                        if (browser.StartFetchingMore(this))
                            notifyFetchingCallback(this);
                        break;
                    }
                }
            }
        }

        public void RemoveLevel_Synched(LevelMetadata level)
        {
            int index = levels.IndexOf(level);

            if (index >= 0)
            {
                levels.RemoveAt(index);
                NotifyLevelRemoved_Synched(index);
            }
        }

        public void AddLevel_Synched(LevelMetadata level)
        {
            if (levels.Contains(level))
                return;

            if (filter.Matches(level))
            {
                int index = -1;
                for (int i = 0; i < levels.Count; ++i)
                {
                    if (sorter.Compare(levels[i], level) > 0)
                    {
                        index = i;
                        break;
                    }
                }

                if (index == -1)
                {
                    index = levels.Count;
                }

                levels.Insert(index, level);
                NotifyLevelAdded_Synched(index, level);
            }
        }

        public void NotifyLevelRemoved_Synched(int index)
        {
            for (int i = 0; i < cursors.Count; ++i)
            {
                LevelSetCursor cursor = cursors[i];
                cursor.LevelRemoved(index);
            }
        }

        public void NotifyLevelAdded_Synched(int index, LevelMetadata level)
        {
            for (int i = 0; i < cursors.Count; ++i)
            {
                LevelSetCursor cursor = cursors[i];
                cursor.LevelAdded(index, level);
            }
        }

        public void StartShiftingCursor(ILevelSetCursor cursor, int desired)
        {
            int actual;

            lock (Synch)
            {
                int origQueryPointer = cursor.QueryPointer;
                cursor.QueryPointer = MyMath.Clamp(cursor.QueryPointer + desired, 0, levels.Count - 1);
                actual = cursor.QueryPointer - origQueryPointer;
            }

            cursor.ShiftComplete(desired, actual);
        }

        public void StartJumpingCursor(ILevelSetCursor cursor, string searchString)
        {
            if (searchString.Length > 0)
            {
                lock (Synch)
                {
                    int minCompatibility = 0;
                    for (int i = 0; i < levels.Count; ++i)
                    {
                        int compatibility = sorter.JumpCompatibility(levels[i], searchString);
                        if (compatibility > minCompatibility)
                        {
                            cursor.QueryPointer = i;
                            minCompatibility = compatibility;
                        }
                    }
                }
            }

            cursor.JumpComplete();


        }

        public void Clear()
        {
            lock (Synch)
            {
                levels.Clear();
                NotifyLevelRemoved_Synched(-1);
            }
        }

        public void NotifyFetchComplete()
        {
            notifyFetchCompleteCallback(this);
        }

        public int Size()
        {
            return levels.Count;        
        }
    }
}
