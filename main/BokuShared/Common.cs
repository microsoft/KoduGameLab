using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace BokuShared
{
    /// <summary>
    /// Indicates which genres a level belongs to, if any. Also used to
    /// filter community searches.
    /// </summary>
    /// WARNING: These values are stored in locally saved level files and
    /// in community databases. You MUST NOT REORDER, DELETE OR RENUMBER
    /// any of these values unless you simultaneously update all Boku
    /// clients, locally stored levels and community databases. It is safe
    /// to rename a genre name, but you must also update its localization
    /// information in both Strings.xml and Strings.cs (which may be the
    /// better place to perform the rename anyway). It is safe to append
    /// new values to this enum (again, synchronizing the localizations
    /// information).
    [Flags]
    public enum Genres : int
    {
        None = 0 << 0,
        Action = 1 << 1,
        Adventure = 1 << 2,
        Puzzle = 1 << 3,
        Racing = 1 << 4,
        RPG = 1 << 5,
        Shooter = 1 << 6,
        Sports = 1 << 7,
        Strategy = 1 << 8,
        Multiplayer = 1 << 9,
        Lessons = 1 << 10,
        SampleWorlds = 1 << 11,
        StarterWorlds = 1 << 12,    // Retired!!!
        FinishedWorlds = 1 << 13,
        Favorite = 1 << 14,         // Cleared on download.
        Keyboard = 1 << 15,
        Controller = 1 << 16,
        Touch = 1 << 17,
        Last = 1 << 18,             // Renumber as needed to ensure this is one bit beyond the last of the normal genres.

        // The three local "storage bins"
        MyWorlds = 1 << 29,         // Virtual
        BuiltInWorlds = 1 << 30,    // Virtual
        Downloads = 1 << 31,        // Virtual

        // The 'special' tags are stripped when the save dialog is opened and must be explicitly set each save.
        Special = SampleWorlds | StarterWorlds | Lessons | FinishedWorlds,

        // The 'buckets' group are used by the load level menu.  They are treated 
        // differently (as an AND rather than an OR) when filtering.
        // Even though MyWorlds should normally be here, it is taken care of in
        // the filtering since it also requires comparing the user's name to the creator's.
        Buckets = Downloads | Lessons | SampleWorlds,
        NonBucket = Action | Adventure | Puzzle | Racing | RPG | Shooter | Sports | Strategy | Multiplayer | FinishedWorlds | BuiltInWorlds | Favorite | Keyboard | Controller,

        LocalBins = MyWorlds | BuiltInWorlds | Downloads,
        SharableBins = MyWorlds | Downloads,
        Virtual = LocalBins,
        All = -1,
    }

    /// <summary>
    /// Indicates the field by which you'd like to sort your level list.
    /// </summary>
    /// WARNING: This value is sent as a string to the community server.
    /// You MUST NOT RENAME any of these values without simultaneously
    /// updating ALL Boku clients and community web services, and database
    /// stored procedures. It is safe to append to this enum and to reorder
    /// it.
    public enum SortBy
    {
        Date,
        Creator,
        Rating,
        Name,
        Rank,
    }

    /// <summary>
    /// Indicates the direction you'd like to sort your level list.
    /// </summary>
    /// WARNING: This value is sent as a string to the community server.
    /// You MUST NOT RENAME any of these values without simultaneously
    /// updating ALL Boku clients and community web services, and database
    /// stored procedures. It is safe to append to this enum and to reorder
    /// it.
    public enum SortDirection
    {
        Ascending,
        Descending,
    }

    /// <summary>
    /// Indicates a user's vote on a level.
    /// </summary>
    /// WARNING: This value is sent as a string to the community server.
    /// You MUST NOT RENAME any of these values without simultaneously
    /// updating ALL Boku clients and community web services, and database
    /// stored procedures. It is safe to append to this enum and to reorder
    /// it.
    public enum Vote
    {
        None,
        Up,
        Down,
    }

    /// <summary>
    /// Specifies a user's priveledge level.
    /// </summary>
    /// WARNING: This setting is stored by value in community databases.
    /// You MUST NOT RENUMBER any of these values without also simultaneously
    /// replacing all Boku clients, community servers, and server databases.
    /// It is safe to append to this enum.
    public enum UserLevel
    {
        Banned = -1,
        User = 0,
        DomainAdmin = 1,
        GlobalAdmin = 2,
    }
}
