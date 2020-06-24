using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KoiX.UI
{
    /// <summary>
    /// A lot like System.Windows.Forms.Padding but prevents the need
    /// to keep including that.
    /// </summary>
    public struct Padding : IEquatable<Padding>
    {
        public static readonly Padding Empty;

        #region Members

        public int left;
        public int right;
        public int top;
        public int bottom;

        #endregion

        #region Accessors

        public int Left
        {
            get { return left; }
            set { left = value; }
        }

        public int Right
        {
            get { return right; }
            set { right = value; }
        }

        public int Top
        {
            get { return top; }
            set { top = value; }
        }

        public int Bottom
        {
            get { return bottom; }
            set { bottom = value; }
        }

        /// <summary>
        /// Gets sum of left and right padding.
        /// </summary>
        public int Horizontal
        {
            get { return left + right; }
        }

        /// <summary>
        /// Gets sum of top and bottom padding.
        /// </summary>
        public int Vertical
        {
            get { return top + bottom; }
        }

        #endregion

        #region Public

        public Padding(int all)
        {
            left = all;
            right = all;
            top = all;
            bottom = all;
        }   // end of x'tor

        public Padding(int left, int top, int right, int bottom)
        {
            this.top = top;
            this.left = left;
            this.right = right;
            this.bottom = bottom;
        }

        public static bool operator ==(Padding p0, Padding p1)
        {
            return p0.left == p1.left && p0.top == p1.top && p0.right == p1.right && p0.bottom == p1.bottom;
        }

        public static bool operator !=(Padding p0, Padding p1)
        {
            return !(p0 == p1);
        }

        /// <summary>
        /// Not sure why VS insists on overriding this 
        /// since it's totally stupid for value types.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is Padding)
                return this == (Padding)obj;
            return false;
        }

        public bool Equals(Padding other)
        {
            return this == other;
        }

        public static Padding operator +(Padding p0, Padding p1)
        {
            return new Padding(p0.Left + p1.Left, p0.Top + p1.Top, p0.Right + p1.Right, p0.Bottom + p1.Bottom);
        }

        public override int GetHashCode()
        {
            return (top<<0 + bottom<<8 + left<<16 + right<<24).GetHashCode();
        }

        #endregion

        #region Internal
        #endregion

    }   // end of struct Padding

}   // end of namespace KoiX.UI
