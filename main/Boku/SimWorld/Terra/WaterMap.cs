// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


#region USING
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
#endregion USING

namespace Boku.SimWorld.Terra
{
    class WaterMap
    {
        #region MEMBERS
        private byte[,] map = null;
        private int size = 0;
        #endregion MEMBERS

        #region PUBLIC
        public WaterMap(int size)
        {
            map = new byte[size, size];
            this.size = size;

            Debug.Assert(Water.InvalidLabel == 0, "Counting on default zero on new meaning 'no water'");
        }

        public byte this[int i, int j]
        {
            get { return map[i, j]; }
            set { map[i, j] = value; }
        }

        public Water GetWater(int i, int j)
        {
            return Water.FromLabel(map[i, j]);
        }

        public void Erase(int index)
        {
            byte idx = (byte)index;
            for (int i = 0; i < size; ++i)
            {
                for (int j = 0; j < size; ++j)
                {
                    if (map[i, j] == idx)
                    {
                        map[i, j] = Water.InvalidLabel;
                    }
                }
            }
        }
        #endregion PUBLIC
    }
}
