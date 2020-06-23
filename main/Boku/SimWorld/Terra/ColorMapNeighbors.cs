using System;
using System.Collections.Generic;
using System.Text;

namespace Boku.SimWorld.Terra
{
    public partial class VirtualMap
    {
        /// <summary>
        /// Simple helper class collecting the neighbors around a colorMap.
        /// </summary>
        public class ColorMapNeighbors
        {
            #region HELPERS
            public enum Dir
            {
                Center,
                North,
                NorthEast,
                East,
                SouthEast,
                South,
                SouthWest,
                West,
                NorthWest
            };
            #endregion HELPERS

            #region MEMBERS
            /// <summary>
            /// Convenience int version of the number of neighbor directions.
            /// </summary>
            const int NumNeighbors = (int)Dir.NorthWest + 1;
            /// <summary>
            /// The collected neighborhood.
            /// </summary>
            private ColorMap[] maps = new ColorMap[NumNeighbors];
            #endregion MEMBERS

            #region PUBLIC
            /// <summary>
            /// Return the (possibly null) neighbor in a given direction.
            /// </summary>
            /// <param name="neighbor"></param>
            /// <returns></returns>
            public ColorMap this[int neighbor]
            {
                get { return maps[neighbor]; }
                set { maps[neighbor] = value; }
            }
            /// <summary>
            /// Return the (possibly null) neighbor in a given direction.
            /// </summary>
            /// <param name="neighbor"></param>
            /// <returns></returns>
            public ColorMap this[Dir neighbor]
            {
                get { return maps[(int)neighbor]; }
                set { maps[(int)neighbor] = value; }
            }

            /// <summary>
            /// Return the color at the given coordinate.
            /// </summary>
            public ushort GetColor(int i, int j)
            {
                var isIUnder = i < 0;
                var isIOver = i >= this[Dir.Center].Size;
                var isJUnder = j < 0;
                var isJOver = j >= this[Dir.Center].Size;

                if (isIUnder || isIOver || isJUnder || isJOver)
                {
                    var empty = TerrainMaterial.EmptyMatIdx;

                    if (isIUnder && isJUnder)
                        if (this[Dir.SouthWest] != null)
                            return this[Dir.SouthWest][this[Dir.SouthWest].Size - 1, this[Dir.SouthWest].Size - 1];
                        else
                            return empty;
                    else if (isIUnder && isJOver)
                        if (this[Dir.NorthWest] != null)
                            return this[Dir.NorthWest][this[Dir.NorthWest].Size - 1, 0];
                        else
                            return empty;
                    else if (isIOver && isJUnder)
                        if (this[Dir.SouthEast] != null)
                            return this[Dir.SouthEast][0, this[Dir.SouthEast].Size - 1];
                        else
                            return empty;
                    else if (isIOver && isJOver)
                        if (this[Dir.NorthEast] != null)
                            return this[Dir.NorthEast][0, 0];
                        else
                            return empty;
                    else if (isIUnder)
                        if (this[Dir.West] != null)
                            return this[Dir.West][this[Dir.West].Size - 1, j];
                        else
                            return empty;
                    else if (isIOver)
                        if (this[Dir.East] != null)
                            return this[Dir.East][0, j];
                        else
                            return empty;
                    else if (isJUnder)
                        if (this[Dir.South] != null)
                            return this[Dir.South][i, this[Dir.South].Size - 1];
                        else
                            return empty;
                    else if (isJOver)
                        if (this[Dir.North] != null)
                            return this[Dir.North][i, 0];
                        else
                            return empty;
                }

                return this[Dir.Center][i, j];
            }
            #endregion PUBLIC
        }
    }
}
