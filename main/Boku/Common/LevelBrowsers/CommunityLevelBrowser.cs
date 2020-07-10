// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

using Boku.Base;
using Boku.Web;
using Boku.Common;
using Boku.Common.Xml;

using BokuShared;

namespace Boku.Common
{
    //helper class for containing info regarding a download for an item that may not be loaded in the current 
    //browser - used when linked level dependencies are donwloaded and we can't guarantee the item is on the 
    //current page
    class OffPageLevelInfo
    {
        public Guid WorldId;
        public LevelDownloadOffPageCompleteEvent downloadCallback;
    }

    /// <summary>
    /// An ILevelBrowser for browsing levels on the community server
    /// </summary>
    public class CommunityLevelBrowser : ILevelBrowser
    {
        #region Private

        object synch = new object();
        public object Synch { get { return synch; } }

        List<ILevelSetQuery> queries = new List<ILevelSetQuery>();
        List<LevelMetadata> allLevels = new List<LevelMetadata>();
        List<LevelMetadata> queuedThumbnailLoads = new List<LevelMetadata>();
        int thumbnailLoadOpCount;

        class LevelBrowserState
        {
            public LevelMetadata level;
            public LevelDownloadCompleteEvent downloadCallback;
            public ThumbnailDownloadCompleteEvent thumbnailCallback;
        }

        #endregion Private

        #region Public

        public bool Working
        {
            get { return pagingOpCount > 0; }
        }


        /// <summary>
        /// Start deleting a level from the community server.  You must have adequate permissions or the server will deny your request.
        /// </summary>
        /// <param name="worldId"></param>
        /// <param name="callback"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public bool StartDeletingLevel(
            Guid worldId,
            Genres bucket,
            BokuAsyncCallback callback,
            object param)
        {
            int index = IndexOf(worldId);
            if (index >= 0)
            {
                LevelMetadata level = allLevels[index];
                allLevels.RemoveAt(index);
                LevelRemoved(level);
            }

            return 0 != Web.Community.Async_DelWorldData2(
                worldId,
                Auth.Pin,
                callback,
                param);
        }

        public void Update()
        {
            lock (Synch)
            {
                foreach (LevelSetQuery query in queries)
                {
                    query.Update();
                }
            }

            if (thumbnailLoadOpCount == 0 && queuedThumbnailLoads.Count > 0)
            {
                // Pull from the end of the list to service newer requests first.
                LevelMetadata level = queuedThumbnailLoads[queuedThumbnailLoads.Count - 1];
                queuedThumbnailLoads.RemoveAt(queuedThumbnailLoads.Count - 1);
                thumbnailLoadOpCount += 1;
                Web.Community.Async_GetThumbnail(
                    level.WorldId,
                    level.Thumbnail,
                    GotThumbnail,
                    level);
            }
        }

        public void Shutdown()
        {
            Web.Community.Async_AbortAll();
        }

        public void Reset()
        {
            lock (Synch)
            {
                pagingFirst = 0;
                pagingEndReached = false;

                allLevels.Clear();

                foreach (ILevelSetQuery query in queries)
                {
                    query.Clear();
                }
            }
        }

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
                foreach (LevelMetadata level in allLevels)
                {
                    query.LevelAdded(level);
                }
            }
        }

        #region Paging State Variables

        int pagingOpCount;
        int pagingFirst = 0;
        const int kPagingPageSize = 30;
        bool pagingEndReached;

        #endregion

        public bool StartFetchingMore(ILevelSetQuery query)
        {
            if (pagingOpCount == 0 && !pagingEndReached)
            {
                // This is a bit of a hack/limitation. For the moment, the community server only
                // supports filtering by genre and sorting on the basic fields. For this limitation
                // to be removed, we must support all sorters and filters on the server side, and
                // send them up with every query.
                LevelSetSorterBasic basicSorter = query.Sorter as LevelSetSorterBasic;
                LevelSetFilterByKeywords filter = query.Filter as LevelSetFilterByKeywords;
                if (String.IsNullOrEmpty(filter.SearchString)
                    || filter.SearchString.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).Length < 1)
                {
                    //if no keywords do old style display
                    if (0 != Web.Community.Async_GetPageOfLevels(
                        filter != null ? filter.FilterGenres : Genres.All,
                        basicSorter != null ? basicSorter.SortBy : SortBy.Date,
                        basicSorter != null ? basicSorter.SortDirection : SortDirection.Descending,
                        pagingFirst,
                        kPagingPageSize,
                        FetchComplete,
                        query))
                    {
                        pagingOpCount += 1;
                        return true;
                    }
                }
                else
                {
                    //use keyword search version
                    if (0 != Web.Community.Async_GetSearchPageOfLevels(
                        filter != null ? filter.FilterGenres : Genres.All,
                        filter.SearchString,
                        basicSorter != null ? basicSorter.SortBy : SortBy.Date,
                        basicSorter != null ? basicSorter.SortDirection : SortDirection.Descending,
                        pagingFirst,
                        kPagingPageSize,
                        FetchComplete,
                        query))
                    {
                        pagingOpCount += 1;
                        return true;
                    }
                }
            }
            return false;
        }

        public void StartDownloadingThumbnail(LevelMetadata level, ThumbnailDownloadCompleteEvent callback, bool lowPriority)
        {
            if (level != null && !level.Thumbnail.IsLoaded && !level.Thumbnail.Loading)
            {
                LevelBrowserState state = (LevelBrowserState)level.BrowserState;
                state.thumbnailCallback = callback;

                // If it already exists in the queue, move it to the end.
                queuedThumbnailLoads.Remove(level);
                queuedThumbnailLoads.Add(level);
            }

            // Only keep a max of 20 pending thumbnail loads, that way of we're just scrolling
            // to the end of the list, we can discard many of these requests as they pass out of view.
            while (queuedThumbnailLoads.Count > 20)
            {
                LevelBrowserState state = (LevelBrowserState)queuedThumbnailLoads[0].BrowserState;
                state.thumbnailCallback = null;
                queuedThumbnailLoads.RemoveAt(0);
            }
        }

        public bool StartDownloadingWorld(LevelMetadata level, LevelDownloadCompleteEvent callback)
        {
            LevelBrowserState state = (LevelBrowserState)level.BrowserState;
            state.downloadCallback = callback;
            level.DownloadState = LevelMetadata.DownloadStates.InProgress;
            return 0 != Web.Community.Async_GetWorldData(level.WorldId, GetWorldDataCallback, level);
        }

        //similiar to StartDownloadingWorld, but operates assuming we can't rely on the current browser page to contain the level
        //all world references will be through Guids instead of LevelMetadata until the download completes
        public bool StartDownloadingOffPageWorld(Guid worldId, LevelDownloadOffPageCompleteEvent callback)
        {            
            OffPageLevelInfo downloadInfo = new OffPageLevelInfo();
            downloadInfo.WorldId = worldId;
            downloadInfo.downloadCallback = callback;

            return 0 != Web.Community.Async_GetWorldData(worldId, GetOffPageWorldDataCallback, downloadInfo);
        }

        public void MetadataUpdated(LevelMetadata level)
        {
        }

        private void GetOffPageWorldDataCallback(AsyncResult result)
        {
            OffPageLevelInfo downloadInfo = (OffPageLevelInfo)result.Param;

            if (result.Success)
            {
                AsyncResult_GetWorldData data = result as AsyncResult_GetWorldData;
                if (data != null)
                {
                    data.World.Data.WorldId = data.World.Info.WorldId;
                    downloadInfo.downloadCallback(data.World.Data, data.World.Info.ThumbnailBytes, downloadInfo.WorldId);
                }
                else
                {
                    downloadInfo.downloadCallback(null, null, downloadInfo.WorldId);
                }
            }
            else
            {
                downloadInfo.downloadCallback(null, null, downloadInfo.WorldId);
            }
        }

        private void GetWorldDataCallback(AsyncResult result)
        {
            LevelMetadata level = (LevelMetadata)result.Param;
            LevelBrowserState state = (LevelBrowserState)level.BrowserState;

            if (result.Success)
            {
                AsyncResult_GetWorldData data = result as AsyncResult_GetWorldData;
                if (data != null)
                {
                    data.World.Data.WorldId = data.World.Info.WorldId;
                    level.DownloadState = LevelMetadata.DownloadStates.Complete;
                }
                else
                {
                    level.DownloadState = LevelMetadata.DownloadStates.Failed;
                }

                state.downloadCallback(data.World.Data, data.World.Info.ThumbnailBytes, level);
            }
            else
            {
                level.DownloadState = LevelMetadata.DownloadStates.Failed;
                state.downloadCallback(null, null, level);
            }
        }


        private int IndexOf(Guid worldId)
        {
            int index = -1;
            foreach (LevelMetadata level in allLevels)
            {
                index += 1;
                if (level.WorldId == worldId)
                    return index;
            }
            return -1;
        }

        private void FetchComplete(AsyncResult ar)
        {
            AsyncResult_GetPageOfLevels result = (AsyncResult_GetPageOfLevels)ar;

            if (result.Success)
            {
                int count = 0;
                foreach (LevelMetadata level in result.Page.Listing)
                {
                    if (IndexOf(level.WorldId) == -1)
                    {
                        LevelBrowserState state = new LevelBrowserState();
                        state.level = level;
                        level.BrowserState = state;

                        level.Browser = this;
                        allLevels.Add(level);
                        LevelAdded(level);
                        count += 1;
                    }
                }

                if (result.Page.First >= result.Page.Total)
                    pagingEndReached = true;

                pagingFirst += count;
            }
            else
            {
                // An error occured, stop trying.
                pagingEndReached = true;
            }

            ILevelSetQuery query = (ILevelSetQuery)ar.Param;

            query.NotifyFetchComplete();

            pagingOpCount -= 1;
        }

        private void GotThumbnail(AsyncResult ar)
        {
            AsyncResult_Thumbnail result = (AsyncResult_Thumbnail)ar;
            LevelMetadata level = (LevelMetadata)result.Param;
            LevelBrowserState state = (LevelBrowserState)level.BrowserState;

            if (result.Success)
            {
                MemoryStream stream = new MemoryStream(result.ThumbnailBytes);
                level.Thumbnail.Texture = Storage4.TextureLoad(stream);
                level.Thumbnail.Loading = false;
            }
            else
            {
                // TODO: Set thumbnail to use missing icon.
            }

            if (state.thumbnailCallback != null)
                state.thumbnailCallback(level);
            state.thumbnailCallback = null;

            thumbnailLoadOpCount -= 1;
        }

        private void LevelAdded(LevelMetadata level)
        {
            if (IsAlreadyDownloaded(level))
            {
                level.DownloadState = LevelMetadata.DownloadStates.Complete;
                level.Genres |= Genres.Downloads;
            }

            foreach (LevelSetQuery query in queries)
            {
                query.LevelAdded(level);
            }
        }

        private void LevelRemoved(LevelMetadata level)
        {
            foreach (LevelSetQuery query in queries)
            {
                query.LevelRemoved(level);
            }
        }

        private bool IsAlreadyDownloaded(LevelMetadata level)
        {
            string filename = BokuGame.Settings.MediaPath + BokuGame.DownloadsPath + level.WorldId.ToString() + @".Xml";
            
            if (Storage4.FileExists(filename, StorageSource.UserSpace))
            {
                XmlWorldData xml = XmlWorldData.Load(filename, XnaStorageHelper.Instance);
                if (xml != null)
                {
                    LevelMetadata local = LevelMetadata.CreateFromXml(xml);

                    return (
                        local.WorldId == level.WorldId &&
                        local.Creator == level.Creator &&
                        local.LastWriteTime >= level.LastWriteTime);
                }
            }

            return false;
        }

        #endregion

        public void AddLevel(LevelMetadata level)
        {
#if NETFX_CORE
            Debug.Assert(false);
#else
            Debug.Fail("not supported");
#endif
        }

        public void RemoveLevel(LevelMetadata level)
        {
#if NETFX_CORE
            Debug.Assert(false);
#else
            Debug.Fail("not supported");
#endif
        }
    }
}
