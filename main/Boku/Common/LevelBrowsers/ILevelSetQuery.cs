using System;

namespace Boku.Common
{
    /// <summary>
    /// Represents a query on a level browser.
    /// A query has a level filter, a level sorter, and any number of attached cursors.
    /// </summary>
    public interface ILevelSetQuery
    {
        LevelMetadata this[int i] { get; }
        ILevelSetFilter Filter { get; }
        ILevelSetSorter Sorter { get; }
        bool IsEmpty { get; }

        void AddCursor(ILevelSetCursor cursor);
        int RemoveCursor(ILevelSetCursor cursor);   // return the number of cursors attached after the removal
        void StartShiftingCursor(ILevelSetCursor cursor, int amount);
        void StartJumpingCursor(ILevelSetCursor cursor, string searchString);
        void Clear();

        void NotifyFetchComplete();

        void LevelAdded(LevelMetadata level);
        void LevelRemoved(LevelMetadata level);

        int Size();
    }

    /// <summary>
    /// Query Callbacks.
    /// </summary>
    public delegate void LevelSetQueryEvent(ILevelSetQuery query);
}
