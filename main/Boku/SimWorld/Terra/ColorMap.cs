// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#region USING
using System;
using System.Collections.Generic;
using System.Diagnostics;

using System.IO;
#endregion USING

namespace Boku.SimWorld.Terra
{
    public partial class VirtualMap
    {
        /// <summary>
        /// Simple helper class for storing the material type for each pixel.
        /// Matches pixel for pixel the heightMap.
        /// </summary>
        public class ColorMap
        {
            #region MEMBERS
            private ushort[,] map = null;
            #endregion MEMBERS

            #region PUBLIC
            public int Size { get; private set; }
            public ColorMap(int size)
            {
                Size = size;
                map = new ushort[size, size];

                //Set color values to the default empty terrain index
                for (int i = 0; i < size; i++)
                    for (int j = 0; j < size; j++)
                        map[i, j] = TerrainMaterial.EmptyMatIdx; //Note: TerrainMaterial.EmptyMatIdx != 0
            }

            public ushort this[int i, int j]
            {
                get 
                {
                    ushort val = map[i, j];
                    return val; 
                }
                set 
                {
                    Debug.Assert(TerrainMaterial.IsValid(value, allowEmpty: true, allowSelectionFlag: true));
                    map[i, j] = value; 
                }
            }

            public void Save(BinaryWriter bw)
            {
                for (int i = 0; i < PixPerMap; ++i)
                {
                    for (int j = 0; j < PixPerMap; ++j)
                    {
                        ushort matIdx = map[i, j];
                        bw.Write(matIdx);
                    }
                }
            }

            public void Load(BinaryReader br, bool use16BitMatIdx)
            {
                for (int i = 0; i < PixPerMap; ++i)
                {
                    for (int j = 0; j < PixPerMap; ++j)
                    {
                        ushort val;

                        if (use16BitMatIdx)
                            val = br.ReadUInt16();
                        else
                            val = br.ReadByte();

                        // If user was editing terrain with a huge brush and saved before
                        // all tiles were updated, some of them may be saved with the 
                        // selection flag in place.  Obviously this doesn't make sense to
                        // have on loading, so clear it out just to be sure.
                        val &= (ushort)~TerrainMaterial.Flags.Selection;
                        
                        // Is the material we read valid? If not, replace it with empty.
                        if (!TerrainMaterial.IsValid(val, true, false))
                        {
                            Debug.Assert(false, "Why are we reading invalid values from this map?");
                            val = TerrainMaterial.EmptyMatIdx;
                        }

                        map[i, j] = val;

                    }
                }
            }
            #endregion PUBLIC
        }
    }
}
