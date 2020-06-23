using System;

namespace Boku.Base
{
    public partial class Classification
    {
        // TODO (****) Make these values part of the Colors enum since
        // they need to be kept in sync.  Having them in a different
        // enum is just asking for trouble.
        public enum ColorInfo
        {
            First = Colors.White,
            Last = Colors.Brown,
            Count = Last - First + 1
        }

        public enum Colors
        {
            NotApplicable, // assumed to occupy the zero slot.
            White,
            Black,
            Grey,
            Red,
            Green,
            Blue,
            Orange,
            Yellow,
            Purple,
            Pink,
            Brown,

            SIZEOF,
            None,
        }
    }
}
