// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Text;

namespace KoiX
{
    /// <summary>
    /// Base class which can be used to give objects a unique runtime id number
    /// which makes comparing objects much easier.
    /// </summary>
    public class ArbitraryComparable
    {
        static int nextNum = 0;
        int uniqueNum;

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

    }   // end of class ArbitraryComparable

}   // end of namespace KoiX
