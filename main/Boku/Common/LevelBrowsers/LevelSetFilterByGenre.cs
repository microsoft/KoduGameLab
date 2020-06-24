using System;
using Boku.Common.Sharing;
using BokuShared;

namespace Boku.Common
{
    /// <summary>
    /// A filter that matches a set of genres.
    /// </summary>
    public class LevelSetFilterByGenre : ILevelSetFilter
    {
        public bool Dirty { get; set; }
        public bool ServerSideMatching { get; set; }//True allows server side to handle matches

        Genres genres;
        public Genres FilterGenres
        {
            get { return genres; }
            set
            {
                if (genres != value)
                {
                    genres = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Based on the current filter's tags, decides whether or not a level matches.
        /// Note that this logic must match the logic on the server side otherwise 
        /// madness ensues.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        virtual public bool Matches(LevelMetadata item)
        {
            //check for server side matching 
            if (ServerSideMatching)
            {
                return true;
            }

            Genres itemGenres = item.Genres;

            if (FilterGenres == Genres.All)
            {
                return true;
            }

            // If all the bucket tags are set, don't bother filtering on MyWorlds.
            Genres filterBuckets = FilterGenres & Genres.Buckets;
            if (filterBuckets != Genres.Buckets)
            {
                // If MyWorlds is set, filter out anything that the user didn't author.
                if ((FilterGenres & Genres.MyWorlds) != 0)
                {
                    // First look for the right name.
                    if (Auth.CreatorName == item.Creator && !string.IsNullOrEmpty(item.Checksum))
                    {
                        if (!Auth.IsValidCreatorChecksum(item.Checksum, item.LastWriteTime))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }

                }
            }

            // If no bucket bits are set, that means the bucket is All.
            if (filterBuckets == 0 || filterBuckets == Genres.Buckets)
            {
                // Must be All, so don't filter.
            }
            else
            {
                // If any bucket is set, filter out anything that doesn't match.
                if ((itemGenres & filterBuckets) == 0)
                {
                    return false;
                }
            }

            // Now that we've filtered on the buckets, we're left with filtering on
            // the individual tags so grab that subset.
            Genres filterTags = FilterGenres & Genres.NonBucket;

            if (filterTags == Genres.NonBucket || filterTags == Genres.None)
            {
                // All set so everything passes.
            }
            else
            {
                // Look for any matches.
                if ((int)(filterTags & itemGenres) == 0)
                {
                    // No match.
                    return false;
                }
            }

            return true;
        }
    }
}
