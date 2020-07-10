// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Boku.Base;
using BokuShared;
using BokuShared.Wire;

namespace Boku.Common
{
    public enum LevelBrowserType
    {
        Local,
        Community,
        Sharing,
    }

    /// <summary>
    /// Browser Callbacks.
    /// </summary>    
    public delegate void LevelDownloadCompleteEvent(WorldDataPacket packet, byte[] thumbnailBytes, LevelMetadata level);
    public delegate void LevelDownloadOffPageCompleteEvent(WorldDataPacket packet, byte[] thumbnailBytes, Guid guid);
    public delegate void ThumbnailDownloadCompleteEvent(LevelMetadata level);

    /// <summary>
    /// Interface describing the API for paging through level metadata.
    /// </summary>
    public interface ILevelBrowser
    {
        bool Working { get; }

        ILevelSetCursor OpenCursor(
            Guid desiredSelection,
            ILevelSetSorter sorter,
            ILevelSetFilter filter,
            LevelSetQueryEvent notifyFetchingCallback,
            LevelSetQueryEvent notifyFetchCompleteCallback,
            LevelSetCursorShifted shiftCallback,
            LevelSetCursorJumped jumpCallback,
            LevelSetCursorAddition additionCallback,
            LevelSetCursorRemoval removalCallback,
            int size);

        void CloseCursor(ref ILevelSetCursor cursor);

        void LoadQuery(ILevelSetQuery query);

        bool StartFetchingMore(ILevelSetQuery query);

        void StartDownloadingThumbnail(LevelMetadata level, ThumbnailDownloadCompleteEvent callback, bool lowPriority);

        bool StartDownloadingWorld(LevelMetadata level, LevelDownloadCompleteEvent callback);

        bool StartDownloadingOffPageWorld(Guid worldId, LevelDownloadOffPageCompleteEvent callback);

        void MetadataUpdated(LevelMetadata level);

        /// <summary>
        /// Start deleting a level. May complete synchronously or asynchronously.
        /// </summary>
        /// <param name="worldId">The level's id</param>
        /// <param name="callback">Optional callback. Receives an AsyncResult argument.</param>
        /// <param name="param">User-supplied parameter, returned in callback.</param>
        /// <returns></returns>
        bool StartDeletingLevel(
            Guid worldId,
            Genres bucket,
            BokuAsyncCallback callback,
            object param);

        /// <summary>
        /// Some implementations may need to be updated each loop to drive asynchronous operations.
        /// </summary>
        void Update();

        void Shutdown();

        void Reset();

        /// <summary>
        /// Add a level to the browser.
        /// </summary>
        /// <param name="level"></param>
        void AddLevel(LevelMetadata level);

        /// <summary>
        /// Remove a level from the browser.
        /// </summary>
        /// <param name="level"></param>
        void RemoveLevel(LevelMetadata level);
    }
}
