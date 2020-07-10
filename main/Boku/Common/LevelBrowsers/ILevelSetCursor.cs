// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Boku.Common
{
    /// <summary>
    /// Represents a sliding window over a query
    /// </summary>
    public interface ILevelSetCursor
    {
        LevelMetadata this[int i] { get; }              // Accesses elements in the cursor's window. Negative indexes are valid.
        int QueryPointer { get; set; }                  // internal state maintained by associated query object. DO NOT MODIFY.
        int Size { get; }                               // The width of the cursor window.
        ILevelBrowser Browser { get; }                  // The browser this cursor belongs to.
        bool Working { get; }                           // Returns true if cursor shift is in progress.

        void StartShifting(int desired);
        void ShiftComplete(int desired, int actual);

        void StartJumping(string searchString);
        void JumpComplete();

        bool SetDesiredLevel(Guid newDesired);
        void SetLevelDownloadState(Guid worldId, LevelMetadata.DownloadStates downloadState);
    }

    /// <summary>
    /// Cursor Callbacks
    /// </summary>
    public delegate void LevelSetCursorShifted(ILevelSetCursor cursor, int desired, int actual);
    public delegate void LevelSetCursorJumped(ILevelSetCursor cursor);
    public delegate void LevelSetCursorAddition(ILevelSetCursor cursor, int index);
    public delegate void LevelSetCursorRemoval(ILevelSetCursor cursor, int index);
}
