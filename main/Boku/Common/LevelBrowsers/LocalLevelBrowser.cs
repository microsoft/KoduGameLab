using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Diagnostics;

using Boku.Base;
using Boku.Common.Xml;
using Boku.Common.Sharing;

#if NETFX_CORE
    using System.Threading.Tasks;
    using Windows.Foundation;
    using Windows.System.Threading;
#endif

using BokuShared;


namespace Boku.Common
{
    /// <summary>
    /// An ILevelBrowser for browsing levels on the local system.
    /// </summary>
    public partial class LocalLevelBrowser : ILevelBrowser
    {
        #region Private

        bool running;
        AutoResetEvent signal = new AutoResetEvent(false);

        object synch = new object();
        public object Synch { get { return synch; } }

        class LevelBrowserState
        {
            public LevelMetadata level;
            public ThumbnailDownloadCompleteEvent thumbnailCallback;
        }

        #endregion Private

        #region Public

        public bool Working
        {
#if NETFX_CORE
            get { return working; }
#else
            get { return thread != null; }
#endif
        }

        public StorageSource Sources = StorageSource.All;

        public void Update()
        {
            lock (Synch)
            {
                for (int i = 0; i < queries.Count; ++i)
                {
                    LevelSetQuery query = queries[i] as LevelSetQuery;
                    query.Update();
                }

                for (int i = 0; i < thumbnailCompletions.Count; ++i)
                {
                    LevelMetadata level = thumbnailCompletions[i];
                    LevelBrowserState state = (LevelBrowserState)level.BrowserState;

                    CompleteThumbnail(level);

                    if (state.thumbnailCallback != null)
                        state.thumbnailCallback(level);
                    state.thumbnailCallback = null;
                }

                thumbnailCompletions.Clear();
            }
        }

        public void Shutdown()
        {
            running = false;
            signal.Set();

            ShutdownInternal();
        }

        #endregion
    }

    #region Private

    /// <summary>
    /// Internal machinations of the LocalLevelBrowser class
    /// </summary>
    public partial class LocalLevelBrowser
    {
        List<LevelMetadata> allLevels = new List<LevelMetadata>();
        List<LevelMetadata> thumbnailQueue = new List<LevelMetadata>();
        List<LevelMetadata> thumbnailCompletions = new List<LevelMetadata>();

#if NETFX_CORE
        bool working = false;   // Mirrors thread's null/non-null condition 
#else
        Thread thread;
#endif

        public LocalLevelBrowser()
            : this(StorageSource.All)
        {
        }

        public LocalLevelBrowser(StorageSource sources)
        {
            this.Sources = sources;
            StartInitialize();
        }

        /// <summary>
        /// Starts the initialization thread, which populates the datasources.
        /// </summary>
        private void StartInitialize()
        {
            running = true;
#if NETFX_CORE
            // Run ReadLevelsProc using the ThreadPool
            Task.Factory.StartNew(ReadLevelsProc);
            working = true;
#else
            thread = new Thread(new ThreadStart(ReadLevelsProc));
            thread.Start();
#endif
        }

        void ReadLevelsProc2()
        {
        }

        /// <summary>
        /// This is the worker thread that loads the datasources from disk.
        /// </summary>
        private void ReadLevelsProc()
        {
            ReadDataSource("BuiltInWorlds", Genres.BuiltInWorlds, StorageSource.TitleSpace);
            ReadDataSource("MyWorlds", Genres.MyWorlds, StorageSource.UserSpace);
            ReadDataSource("Downloads", Genres.Downloads, StorageSource.UserSpace);

#if NETFX_CORE
            working = false;
#else
            thread = null;
#endif

            for (; ; )
            {
                if (!running || !BokuGame.Running)
                    break;

                // Wait for a wake-up signal
#if NETFX_CORE
                if (!signal.WaitOne(10))
                    continue;
#else
                if (!signal.WaitOne(10, false))
                    continue;
#endif

                // Process all queued thumbnail load requests.
                for (; ; )
                {
                    lock (Synch)
                    {
                        if (thumbnailQueue.Count == 0 || !running || !BokuGame.Running)
                            break;

                        LevelMetadata level = thumbnailQueue[thumbnailQueue.Count - 1];
                        thumbnailQueue.RemoveAt(thumbnailQueue.Count - 1);

                        try
                        {
                            string texFilename = BokuGame.Settings.MediaPath + Utils.FolderNameFromFlags(level.Genres) + level.WorldId.ToString();
                            Stream texStream = OpenTextureStream(texFilename);
                            if (texStream != null)
                            {
                                level.ThumbnailBytes = new byte[texStream.Length];
                                texStream.Read(level.ThumbnailBytes, 0, (int)texStream.Length);
                                Storage4.Close(texStream);
                                thumbnailCompletions.Add(level);
                            }
                        }
                        catch { }
                    }

                    // Let the main thread have the cpu so it can deliver the thumbnail to the level.
#if NETFX_CORE
                    {
                        System.Threading.Tasks.Task delayTask = System.Threading.Tasks.Task.Delay(1);
                        delayTask.ConfigureAwait(false);
                        delayTask.Wait();
                    }
#else
                    Thread.Sleep(1);
#endif
                }
            }
        }

        /// <summary>
        /// Reads a directory, building the list of level metadata available for browsing.
        /// </summary>
        /// <param name="dataSource"></param>
        /// <returns></returns>
        private void ReadDataSource(string folder, Genres tag, StorageSource sources)
        {
            string path = Path.Combine(BokuGame.Settings.MediaPath, BokuGame.LevelsPath, folder);
            string[] files = null;

            try
            {
#if NETFX_CORE
                files = Storage4.GetFiles(path, @"*.Xml", sources);
#else
                files = Storage4.GetFiles(path, @"*.Xml", sources, SearchOption.TopDirectoryOnly);
#endif
            }
            catch { }

            if (files != null)
            {
                // Filter out AutoSave.Xml
                List<string> filteredFiles = new List<string>();
                for (int i = 0; i < files.Length; ++i)
                {
                    if (files[i].ToUpper().Contains("AUTOSAVE"))
                        continue;
                    filteredFiles.Add(files[i]);
                }
                files = filteredFiles.ToArray();

                // Load level metadata records
                for (int i = 0; running && i < files.Length; i++)
                {
                    try
                    {
                        string filename = Path.GetFileName(files[i]);
                        string fullPath = Path.Combine(path, filename);
                        XmlWorldData xml = XmlWorldData.Load(fullPath, XnaStorageHelper.Instance, (int)sources);
                        if (xml == null)
                            continue;

                        LevelMetadata level = new LevelMetadata();
                        level.FromXml(xml);

                        level.Genres |= tag;

                        LevelBrowserState state = new LevelBrowserState();
                        state.level = level;
                        level.BrowserState = state;

                        // Force the creator name of built-ins to "Microsoft"
                        if ((level.Genres & Genres.BuiltInWorlds) != 0)
                        {
                            level.Creator = "Kodu Team";
                        }

                        AddLevel(level);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                }
            }
        }
    }


    public partial class LocalLevelBrowser
    {
        List<ILevelSetQuery> queries = new List<ILevelSetQuery>();

        public ILevelSetCursor OpenCursor(
            Guid desiredSelection,
            ILevelSetSorter sorter,
            ILevelSetFilter filter,
            LevelSetQueryEvent notifyFetchingCallback,
            LevelSetQueryEvent notifyFetchCompleteCallback,
            LevelSetCursorShifted shiftCallback,
            LevelSetCursorJumped jumpCallback,
            LevelSetCursorAddition additionCallback,
            LevelSetCursorRemoval removalCallback,
            int size)
        {
            LevelSetCursor cursor;

            lock (Synch)
            {
                ILevelSetQuery query = new LevelSetQuery(
                    sorter, 
                    filter, 
                    this, 
                    notifyFetchingCallback, 
                    notifyFetchCompleteCallback);

                queries.Add(query);

                cursor = new LevelSetCursor(
                    this,
                    desiredSelection,
                    query,
                    shiftCallback,
                    jumpCallback,
                    additionCallback,
                    removalCallback,
                    size);
                
                query.AddCursor(cursor);
            }

            return cursor;
        }

        public void CloseCursor(ref ILevelSetCursor icursor)
        {
            LevelSetCursor cursor = icursor as LevelSetCursor;

            if (cursor != null)
            {
                lock (Synch)
                {
                    int index = queries.IndexOf(cursor.Query);

                    if (index >= 0)
                    {
                        ILevelSetQuery query = queries[index];
                        if (0 == query.RemoveCursor(cursor))
                        {
                            queries.RemoveAt(index);
                        }
                    }
                }

                icursor = null;
            }
        }

        public void LoadQuery(ILevelSetQuery query)
        {
            lock (Synch)
            {
                for (int i = 0; i < allLevels.Count; ++i)
                {
                    LevelMetadata level = allLevels[i];
                    query.LevelAdded(level);
                }
            }
        }

        public bool StartFetchingMore(ILevelSetQuery query)
        {
            return false;
        }

        public void StartDownloadingThumbnail(LevelMetadata level, ThumbnailDownloadCompleteEvent callback, bool lowPriority)
        {
            if (level == null)
                return;

            lock (Synch)
            {
                if (!level.Thumbnail.IsLoaded && !level.Thumbnail.Loading)
                {
                    LevelBrowserState state = (LevelBrowserState)level.BrowserState;

                    if (state != null)
                    {
                        state.thumbnailCallback = callback;

                        thumbnailQueue.Remove(level);
                        thumbnailQueue.Add(level);
                        signal.Set();
                    }
                }

                // Keep the number of requests down to a max of 20 in case we're quickly paging to the end of the set.
                while (thumbnailQueue.Count > 20)
                {
                    LevelBrowserState state = (LevelBrowserState)thumbnailQueue[0].BrowserState;
                    state.thumbnailCallback = null;
                    thumbnailQueue.RemoveAt(0);
                }
            }
        }

        public bool StartDownloadingWorld(LevelMetadata level, LevelDownloadCompleteEvent callback)
        {
            return false;
        }

        public bool StartDownloadingOffPageWorld(Guid worldId, LevelDownloadOffPageCompleteEvent callback)
        {
            return false;
        }

        public void MetadataUpdated(LevelMetadata level)
        {
        }

        public void Reset()
        {
        }

        public void AddLevel(LevelMetadata level)
        {
            try
            {
                lock (Synch)
                {
                    level.Browser = this;
                    LevelBrowserState state = new LevelBrowserState();
                    state.level = level;
                    level.BrowserState = state;

                    allLevels.Add(level);
                    LevelAdded_Synched(level);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        public void RemoveLevel(LevelMetadata level)
        {
            try
            {
                lock (Synch)
                {
                    for (int i = allLevels.Count - 1; i >= 0; --i)
                    {
                        if (allLevels[i].WorldId == level.WorldId)
                        {
                            level = allLevels[i];
                            allLevels.RemoveAt(i);
                            LevelRemoved_Synched(level);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
    }

    #endregion Private
}
