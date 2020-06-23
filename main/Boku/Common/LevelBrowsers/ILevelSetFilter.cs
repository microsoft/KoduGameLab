using System;

namespace Boku.Common
{
    /// <summary>
    /// Base class for classes that filter levels in a query.
    /// Levels that pass the filter belong in the query.
    /// </summary>
    public interface ILevelSetFilter
    {
        bool Dirty { get; set; }
        bool ServerSideMatching { get; set; }//True allows server side to handle matches
        bool Matches(LevelMetadata item);

    }
}
