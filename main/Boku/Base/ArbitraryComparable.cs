using System;
using System.Collections.Generic;
using System.Text;

namespace Boku
{
    public class ArbitraryComparable
    {
        static int nextNum = 0;
        public int uniqueNum;

        public int UniqueNum
        {
            get { return uniqueNum; }
        }

        public ArbitraryComparable()
        {
            uniqueNum = nextNum++;
        }
        public int CompareTo(Object other)
        {
            ArbitraryComparable otherObj = other as ArbitraryComparable;
            if (otherObj == null)
            {
                return 1;
            }
            if (uniqueNum < otherObj.uniqueNum)
            {
                return -1;
            }
            if (uniqueNum > otherObj.uniqueNum)
            {
                return 1;
            }
            return 0;
        }

        public override int GetHashCode()
        {
            return UniqueNum;
        }
    }
}
