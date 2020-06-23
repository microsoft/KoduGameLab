using System;
using BokuShared;

namespace Boku.Common
{
    /// <summary>
    /// A sorter that sorts on one of the values in the SortBy enumeration
    /// in the direction specified by the SortDirection field.
    /// </summary>
    public class LevelSetSorterBasic : ILevelSetSorter
    {
        private SortBy sortBy;
        private SortDirection sortDirection;

        public SortBy SortBy
        {
            get { return sortBy; }
            set
            {
                if (sortBy != value)
                {
                    sortBy = value;
                    Dirty = true;
                }
            }
        }

        public SortDirection SortDirection
        {
            get { return sortDirection; }
            set
            {
                if (sortDirection != value)
                {
                    sortDirection = value;
                    Dirty = true;
                }
            }
        }

        public bool Dirty { get; set; }

        public int Compare(LevelMetadata a, LevelMetadata b)
        {
            int result = 0;

            switch (SortBy)
            {
                case SortBy.Date:
                    result = DateComparison(a, b);
                    break;
                case SortBy.Creator:
                    result = CreatorComparison(a, b);
                    break;
                case SortBy.Name:
                    result = NameComparison(a, b);
                    break;
                case SortBy.Rating:
                    result = RatingComparison(a, b);
                    break;
                case SortBy.Rank:
                    result = 0;//Rank sorting is done server side
                    break;
            }

            if (SortDirection == SortDirection.Ascending)
                result *= -1;

            return result;
        }

        public int JumpCompatibility(LevelMetadata level, string searchString)
        {
            int result = 0;

            switch (SortBy)
            {
                case SortBy.Creator:
                    result = JumpCompatibility(level.Creator, searchString);
                    break;
                case SortBy.Name:
                    result = JumpCompatibility(level.Name, searchString);
                    break;
            }

            return result;
        }

        private int JumpCompatibility(string input, string searchString)
        {
            int result = 0;
            int len = Math.Min(input.Length, searchString.Length);
            searchString = searchString.ToUpper();
            string work = input.Substring(0, len).ToUpper();
            for (int i = 0; i < len; ++i)
            {
                if (work[i] != searchString[i])
                    break;

                result += 1;
            }
            return result;
        }

        private int DateComparison(LevelMetadata a, LevelMetadata b)
        {
            int result = -a.LastWriteTime.CompareTo(b.LastWriteTime);
            if (result == 0)
            {
#if NETFX_CORE
                result = String.Compare(b.Name, a.Name, StringComparison.CurrentCultureIgnoreCase);
#else
                result = String.Compare(b.Name, a.Name, true);
#endif
            }

            return result;
        }
        private int CreatorComparison(LevelMetadata a, LevelMetadata b)
        {
#if NETFX_CORE
            int result = String.Compare(b.Creator, a.Creator, StringComparison.CurrentCultureIgnoreCase);
#else
            int result = String.Compare(b.Creator, a.Creator, true);
#endif
            if (result == 0)
            {
                result = DateComparison(a, b);
            }

            return result;
        }
        private int NameComparison(LevelMetadata a, LevelMetadata b)
        {
#if NETFX_CORE
            int result = String.Compare(b.Name, a.Name, StringComparison.CurrentCultureIgnoreCase);
#else
            int result = String.Compare(b.Name, a.Name, true);
#endif
            if (result == 0)
            {
                result = DateComparison(a, b);
            }

            return result;
        }
        private int RatingComparison(LevelMetadata a, LevelMetadata b)
        {
            int result = a.Rating.CompareTo(b.Rating);
            if (result == 0)
                result = DateComparison(a, b);

            return result;
        }
    }
}
