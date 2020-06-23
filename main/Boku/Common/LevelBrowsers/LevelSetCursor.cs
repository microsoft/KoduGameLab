using System;

namespace Boku.Common
{
    /// <summary>
    /// A fairly general-purpose implementation of ILevelSetCursor.
    /// Currently used for browsing local and community levels.
    /// We'll see whether it works for p2p sharing sessions...
    /// </summary>
    public class LevelSetCursor : ILevelSetCursor
    {
        public int QueryPointer { get; set; }

        public ILevelBrowser Browser { get; private set; }

        public ILevelSetQuery Query;

        Guid desiredSelection;

        LevelSetCursorShifted shiftCallback;
        LevelSetCursorJumped jumpCallback;
        LevelSetCursorAddition additionCallback;
        LevelSetCursorRemoval removalCallback;

        int size;
        int totalOpCount;
        int currOpCount;

        public LevelMetadata this[int i]
        {
            get { return Query[QueryPointer + i]; }
        }

        public int Size
        {
            get { return size; }
        }

        public bool Working
        {
            get { return currOpCount > 0; }
        }


        public LevelSetCursor(
            ILevelBrowser browser,
            Guid desiredSelection,
            ILevelSetQuery query,
            LevelSetCursorShifted shiftCallback,
            LevelSetCursorJumped jumpCallback,
            LevelSetCursorAddition additionCallback,
            LevelSetCursorRemoval removalCallback,
            int size)
        {
            Query = query;
            this.Browser = browser;
            this.desiredSelection = desiredSelection;
            this.shiftCallback = shiftCallback;
            this.jumpCallback = jumpCallback;
            this.additionCallback = additionCallback;
            this.removalCallback = removalCallback;
            this.size = size;
        }

        public bool SetDesiredLevel(Guid newDesired)
        {
            int queryCount = Query.Size();
            for (int i = 0; i < queryCount; ++i)
            {
                if (Query[i].WorldId == newDesired)
                {
                    this.desiredSelection = newDesired;
                    QueryPointer = i;
                    return true;
                }
            }

            return false;
        }

        public void SetLevelDownloadState(Guid worldId, LevelMetadata.DownloadStates downloadState)
        {
            int queryCount = Query.Size();
            for (int i = 0; i < queryCount; ++i)
            {
                if (Query[i].WorldId == worldId)
                {
                    Query[i].DownloadState = downloadState;

                    //make sure the local cache is updated with the correct genre for file location
                    if (downloadState == LevelMetadata.DownloadStates.Complete)
                    {
                        Query[i].Genres |= BokuShared.Genres.Downloads;
                    }
                    return;
                }
            }
        }

        public void StartShifting(int desired)
        {
            totalOpCount += 1;
            currOpCount += 1;
            Query.StartShiftingCursor(this, desired);
        }

        public void ShiftComplete(int desired, int actual)
        {
            currOpCount -= 1;
            // Remember the id of the currently selected level, so that we can re-select it
            // when the query sort or filter changes.
            if (Query[QueryPointer] != null)
                desiredSelection = Query[QueryPointer].WorldId;

            shiftCallback(this, desired, actual);
        }

        public void StartJumping(string searchString)
        {
            totalOpCount += 1;
            currOpCount += 1;
            Query.StartJumpingCursor(this, searchString);
        }

        public void JumpComplete()
        {
            currOpCount -= 1;
            // Remember the id of the currently selected level, so that we can re-select it
            // when the query sort or filter changes.
            if (Query[QueryPointer] != null)
                desiredSelection = Query[QueryPointer].WorldId;

            jumpCallback(this);
        }

        internal void LevelAdded(int index, LevelMetadata level)
        {
            if (level.WorldId == desiredSelection && totalOpCount == 0)
            {
                // If we were looking for a specific level and the user hasn't explicitly moved the cursor,
                // move the cursor to point at the identified level.
                totalOpCount += 1;
                QueryPointer = index;
            }
            else if (totalOpCount > 0 && index <= QueryPointer && !Query.IsEmpty)
            {
                // If we want the cursor to remain on the current level, and the new level being added comes
                // before the current selection, this means the current level bumps forward in the query, so
                // adjust the selection to remain on the current level.
                QueryPointer += 1;
            }

            additionCallback(this, index - QueryPointer);
        }

        internal void LevelRemoved(int index)
        {
            if (index >= 0)
            {
                // A single level has been removed from the query.

                // Ensure our query pointer doesn't point past the last element in the query.
                if (QueryPointer > 0 && Query[QueryPointer] == null)
                {
                    QueryPointer -= 1;
                }

                // If the query is now empty, reset some state variables that dictate
                // how we should behave when new levels are added
                if (QueryPointer == 0 && Query[QueryPointer] == null)
                {
                    totalOpCount = 0;
                    desiredSelection = Guid.Empty;
                }

                removalCallback(this, index - QueryPointer);
            }
            else if (index == -1)
            {
                // The query has been cleared. Reset a few state variables and
                // then make the callback with the special value MaxValue to 
                // communicate to the app that it should clear its state as well.
                QueryPointer = 0;
                totalOpCount = 0;
                removalCallback(this, int.MaxValue);
            }
        }
    }
}
