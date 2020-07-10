// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Boku.SimWorld.Terra
{
    public partial class VirtualMap
    {
        /// <summary>
        /// Simple helper class collecting the neighbors around a heightMap.
        /// </summary>
        public class HeightMapNeighbors
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
            private HeightMap[] maps = new HeightMap[NumNeighbors];
            #endregion MEMBERS

            #region PUBLIC
            /// <summary>
            /// Return the (possibly null) neighbor in a given direction.
            /// </summary>
            /// <param name="neighbor"></param>
            /// <returns></returns>
            public HeightMap this[int neighbor]
            {
                get { return maps[neighbor]; }
                set { maps[neighbor] = value; }
            }
            /// <summary>
            /// Return the (possibly null) neighbor in a given direction.
            /// </summary>
            /// <param name="neighbor"></param>
            /// <returns></returns>
            public HeightMap this[Dir neighbor]
            {
                get { return maps[(int)neighbor]; }
                set { maps[(int)neighbor] = value; }
            }

            /// <summary>
            /// Return the height at the given coordinate.
            /// </summary>
            public float GetHeight(int i, int j)
            {
                var isIUnder = i < 0;
                var isIOver = i >= this[Dir.Center].Size.X;
                var isJUnder = j < 0;
                var isJOver = j >= this[Dir.Center].Size.Y;

                if (isIUnder || isIOver || isJUnder || isJOver)
                {
                    if (isIUnder && isJUnder)
                        if (this[Dir.SouthWest] != null)
                            return this[Dir.SouthWest].GetHeight(this[Dir.SouthWest].Size.X - 1, this[Dir.SouthWest].Size.Y - 1);
                        else
                            return 0;
                    else if (isIUnder && isJOver)
                        if (this[Dir.NorthWest] != null)
                            return this[Dir.NorthWest].GetHeight(this[Dir.NorthWest].Size.X - 1, 0);
                        else
                            return 0;
                    else if (isIOver && isJUnder)
                        if (this[Dir.SouthEast] != null)
                            return this[Dir.SouthEast].GetHeight(0, this[Dir.SouthEast].Size.Y - 1);
                        else
                            return 0;
                    else if (isIOver && isJOver)
                        if (this[Dir.NorthEast] != null)
                            return this[Dir.NorthEast].GetHeight(0, 0);
                        else
                            return 0;
                    else if (isIUnder)
                        if (this[Dir.West] != null)
                            return this[Dir.West].GetHeight(this[Dir.West].Size.X - 1, j);
                        else
                            return 0;
                    else if (isIOver)
                        if (this[Dir.East] != null)
                            return this[Dir.East].GetHeight(0, j);
                        else
                            return 0;
                    else if (isJUnder)
                        if (this[Dir.South] != null)
                            return this[Dir.South].GetHeight(i, this[Dir.South].Size.Y - 1);
                        else
                            return 0;
                    else if (isJOver)
                        if (this[Dir.North] != null)
                            return this[Dir.North].GetHeight(i, 0);
                        else
                            return 0;
                }

                return this[Dir.Center].GetHeight(i, j);
            }
            #endregion PUBLIC

            #region INTERNAL
            #endregion INTERNAL
        }
    }
}
