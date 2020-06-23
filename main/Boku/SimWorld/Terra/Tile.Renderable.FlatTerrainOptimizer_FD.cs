
#if DEBUG
#define Debug_CountTerrainVerts
#define Debug_DrawNormalsWithF8
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Common;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Microsoft.Xna.Framework.Input;

namespace Boku.SimWorld.Terra
{
    public partial class Tile
    {
        internal partial class Renderable
        {
            internal class FlatTerrainOptimizer_FD
            {
                //ToDo (DZ): Write a more detailed description of how
                // this algorithm works.
                //Basic explanation:
                // For flat terrain, vertices will be bunched up in
                // groups of four. It is these clusters of four that
                // we optimize. Basically, this algorithm runs through
                // the tile's top vertex buffer, finds all the complete
                // corner sets (positions that have all four verts at the
                // same height), further groups adjacent corner sets, then
                // replaces the old vertex buffer with a reduced one.

                //Note: This algorithm relies on the order in which
                // vertices are added in AddVertices. If this order
                // changes, this algorithm will need some tweaks.
                // This algorithm is (hopefully) temporary. I'm sure
                // there is a much more effecient way to accomplish
                // the same goal. However, this algorithm works (at
                // the time this was written...) and performance isn't
                // a huge concern since this is only run when the
                // terrain is modified (not a per-Update operation).

                /// <summary>
                /// How large the top faces are allowed to be. (Note, if
                /// this is too large then it will interfere with 
                /// per-vertex lighting.)
                /// </summary>
                public const int MaxXLength = 4;
                /// <summary>
                /// How large the top faces are allowed to be. (Note, if
                /// this is too large then it will interfere with 
                /// per-vertex lighting.)
                /// </summary>
                public const int MaxYLength = 4;

                private const int maxXTraversals = MaxXLength - 2;
                private const int maxYTraversals = MaxYLength - 2;

                #region Nested types
                private enum Corners
                {
                    None = 0,
                    FL = 1,
                    FR = 2,
                    BL = 4,
                    BR = 8,
                }
                private struct CornerSet
                {
                    public int?[] cornerIndices;

                    public bool Filled
                    {
                        get
                        {
                            return cornerIndices[0] != null &&
                                   cornerIndices[1] != null &&
                                   cornerIndices[2] != null &&
                                   cornerIndices[3] != null;
                        }
                    }

                    public int iFL { get { return cornerIndices[3].Value - 2; } }
                    public int iFR { get { return cornerIndices[1].Value - 2; } }
                    public int iBL { get { return cornerIndices[2].Value + 2; } }
                    public int iBR { get { return cornerIndices[0].Value + 2; } }

                    public int iB { get { return cornerIndices[2].Value + 1; } }
                    public int iF { get { return cornerIndices[3].Value - 1; } }
                    public int iL { get { return cornerIndices[3].Value + 1; } }
                    public int iR { get { return cornerIndices[1].Value - 1; } }
                }
                #endregion

                private readonly int _numVerts;
                private readonly Terrain.TerrainVertex_FD[] _verts;
                private readonly IDictionary<Vector3, CornerSet> _posToCSs;
                //private readonly HashSet<int> _indicesToRemove; //ToDo (DZ): Hashset doesn't exist on XBox!
                // HashSet isn't available on Xbox so just use a Dictionary w/ empty value.
                private readonly Dictionary<int, int> _indicesToRemove;

                // Make these dictionaries static so that can be shared and aren't constantly allocated/freed.
                static Dictionary<Vector3, CornerSet> _posToCSs_static = new Dictionary<Vector3,CornerSet>();
                static Dictionary<int, int> _indicesToRemove_static = new Dictionary<int, int>();

                public FlatTerrainOptimizer_FD(Terrain.TerrainVertex_FD[] verts, int numVerts)
                {
                    _numVerts = numVerts;
                    _verts = verts;

                    _posToCSs = _posToCSs_static;
                    _posToCSs.Clear();
                    _indicesToRemove = _indicesToRemove_static;
                    _indicesToRemove.Clear();
                }

                public Terrain.TerrainVertex_FD[] Optimize()
                {
                    for (int i = 0; i < _numVerts; i++)
                    {
                        Terrain.TerrainVertex_FD v = _verts[i];
                        Vector3 pos = GetPos(v);
                        int corner = (int)((v.faceAndCorner & 0xff00) >> 8);

                        if (_posToCSs.ContainsKey(pos))
                        {
                            //Debug.Assert(_posToCSs[pos].cornerIndices[corner] == null);

                            _posToCSs[pos].cornerIndices[corner] = i;
                        }
                        else
                        {
                            CornerSet cS = new CornerSet();
                            cS.cornerIndices = new int?[4];

                            cS.cornerIndices[corner] = i;

                            _posToCSs.Add(pos, cS);
                        }
                    }

                    // Doesn't seem to be used...
                    //var toEmpty = new HashSet<CornerSet>();
                    List<Terrain.TerrainVertex_FD> toAdd = new List<Terrain.TerrainVertex_FD>();

                    foreach (CornerSet cS in _posToCSs.Values)
                    {
                        if (!IsRemoved(cS, Corners.None) && IsFlatAndFilled(cS))
                        {
                            MarkForRemove(cS);

                            //Traverse up the Y dir
                            CornerSet bCS = cS;
                            CornerSet nBCS = GetCornerSet(cS.iB);
                            int bTraversals = 0;
                            while (!IsRemoved(nBCS, Corners.FL | Corners.FR) && IsFlatAndFilled(nBCS) && bTraversals < maxYTraversals)
                            {
                                bTraversals++;
                                bCS = nBCS;
                                MarkForRemove(bCS);
                                nBCS = GetCornerSet(nBCS.iB);
                            }

                            //Traverse up the X dir
                            CornerSet rCS = cS;
                            CornerSet brCS = bCS;
                            CornerSet nRCS = GetCornerSet(rCS.iR);
                            int rTraversals = 0;
                            while (!IsRemoved(nRCS, Corners.FL | Corners.BL) && IsFlatAndFilled(nRCS) && rTraversals < maxXTraversals)
                            {
                                rTraversals++;
                                List<CornerSet> tenativeToEmpty = new List<CornerSet>();
                                bool bTraversalsCompleted = true;
                                CornerSet nBRCS = nRCS;
                                nBCS = nRCS;
                                for (int i = 0; i < bTraversals; i++)
                                {
                                    nBCS = GetCornerSet(nBCS.iB);
                                    if (!IsRemoved(nBCS, Corners.FL | Corners.FR | Corners.BL) && IsFlatAndFilled(nBCS))
                                    {
                                        tenativeToEmpty.Add(nBCS);
                                        nBRCS = nBCS;
                                    }
                                    else
                                    {
                                        bTraversalsCompleted = false;
                                        break;
                                    }
                                }

                                if (bTraversalsCompleted)
                                {
                                    brCS = nBRCS;
                                    rCS = nRCS;
                                    MarkForRemove(rCS);
                                    for (int i = 0; i < tenativeToEmpty.Count; i++)
                                        MarkForRemove(tenativeToEmpty[i]);
                                    nRCS = GetCornerSet(nRCS.iR);
                                }
                                else
                                    break;
                            }

                            Terrain.TerrainVertex_FD vFL = _verts[cS.iFL];
                            Terrain.TerrainVertex_FD vBL = _verts[bCS.iBL];
                            Terrain.TerrainVertex_FD vBR = _verts[brCS.iBR];
                            Terrain.TerrainVertex_FD vFR = _verts[rCS.iFR];

                            toAdd.Add(vFL);
                            toAdd.Add(vFR);
                            toAdd.Add(vBR);
                            toAdd.Add(vBL);
                        }
                    }

                    for (int i = 0; i < _numVerts; i++)
                    {
                        if (!_indicesToRemove.ContainsKey(i))
                        {
                            toAdd.Add(_verts[i]);
                        }
                    }

                    Terrain.TerrainVertex_FD[] result = toAdd.ToArray();

                    return result;
                }

                void MarkForRemove(CornerSet cS)
                {
                    Debug.Assert(cS.cornerIndices[0].HasValue);
                    {
                        var c0 = cS.cornerIndices[0].Value;

                        _indicesToRemove[c0] = 0;
                        _indicesToRemove[c0 + 1] = 0;
                        _indicesToRemove[c0 + 2] = 0;
                        _indicesToRemove[c0 + 3] = 0;
                    }
                    Debug.Assert(cS.cornerIndices[1].HasValue);
                    {
                        var c1 = cS.cornerIndices[1].Value;

                        _indicesToRemove[c1] = 0;
                        _indicesToRemove[c1 - 1] = 0;
                        _indicesToRemove[c1 - 2] = 0;
                        _indicesToRemove[c1 - 3] = 0;
                    }
                    Debug.Assert(cS.cornerIndices[2].HasValue);
                    {
                        var c2 = cS.cornerIndices[2].Value;

                        _indicesToRemove[c2] = 0;
                        _indicesToRemove[c2 - 1] = 0;
                        _indicesToRemove[c2 + 1] = 0;
                        _indicesToRemove[c2 + 2] = 0;
                    }
                    Debug.Assert(cS.cornerIndices[3].HasValue);
                    {
                        var c3 = cS.cornerIndices[3].Value;

                        _indicesToRemove[c3] = 0;
                        _indicesToRemove[c3 - 1] = 0;
                        _indicesToRemove[c3 - 2] = 0;
                        _indicesToRemove[c3 + 1] = 0;
                    }
                }

                bool IsRemoved(CornerSet cS, Corners cornersToIgnore)
                {
                    if ((cornersToIgnore & Corners.BR) == 0)
                    {
                        if (cS.cornerIndices[0].HasValue)
                        {
                            var c0 = cS.cornerIndices[0].Value;
                            if (_indicesToRemove.ContainsKey(c0)) return true;
                            if (_indicesToRemove.ContainsKey(c0 + 1)) return true;
                            if (_indicesToRemove.ContainsKey(c0 + 2)) return true;
                            if (_indicesToRemove.ContainsKey(c0 + 3)) return true;
                        }
                    }

                    if ((cornersToIgnore & Corners.FR) == 0)
                    {
                        if (cS.cornerIndices[1].HasValue)
                        {
                            var c1 = cS.cornerIndices[1].Value;

                            if (_indicesToRemove.ContainsKey(c1)) return true;
                            if (_indicesToRemove.ContainsKey(c1 - 1)) return true;
                            if (_indicesToRemove.ContainsKey(c1 - 2)) return true;
                            if (_indicesToRemove.ContainsKey(c1 - 3)) return true;
                        }
                    }

                    if ((cornersToIgnore & Corners.BL) == 0)
                    {
                        if (cS.cornerIndices[2].HasValue)
                        {
                            var c2 = cS.cornerIndices[2].Value;

                            if (_indicesToRemove.ContainsKey(c2)) return true;
                            if (_indicesToRemove.ContainsKey(c2 - 1)) return true;
                            if (_indicesToRemove.ContainsKey(c2 + 1)) return true;
                            if (_indicesToRemove.ContainsKey(c2 + 2)) return true;
                        }
                    }

                    if ((cornersToIgnore & Corners.FL) == 0)
                    {
                        if (cS.cornerIndices[3].HasValue)
                        {
                            var c3 = cS.cornerIndices[3].Value;

                            if (_indicesToRemove.ContainsKey(c3)) return true;
                            if (_indicesToRemove.ContainsKey(c3 - 1)) return true;
                            if (_indicesToRemove.ContainsKey(c3 - 2)) return true;
                            if (_indicesToRemove.ContainsKey(c3 + 1)) return true;
                        }
                    }

                    return false;
                }

                bool IsFlatAndFilled(CornerSet cS)
                {
                    if (!cS.Filled)
                        return false;

                    var z = GetPos(_verts[cS.cornerIndices[0].Value]).Z;

                    var zFL = GetPos(_verts[cS.iFL]).Z;
                    if (z != zFL) return false;
                    var zFR = GetPos(_verts[cS.iFR]).Z;
                    if (z != zFR) return false;
                    var zBL = GetPos(_verts[cS.iBL]).Z;
                    if (z != zBL) return false;
                    var zBR = GetPos(_verts[cS.iBR]).Z;
                    if (z != zBR) return false;
                    var zF = GetPos(_verts[cS.iF]).Z;
                    if (z != zF) return false;
                    var zB = GetPos(_verts[cS.iB]).Z;
                    if (z != zB) return false;
                    var zL = GetPos(_verts[cS.iL]).Z;
                    if (z != zL) return false;
                    var zR = GetPos(_verts[cS.iR]).Z;
                    if (z != zR) return false;

                    return true;
                }

                CornerSet GetCornerSet(int index)
                {
                    return _posToCSs[GetPos(_verts[index])];
                }

                Vector3 GetPos(Terrain.TerrainVertex_FD vert)
                {
                    var pos4 = vert.positionAndZDiff;
                    return new Vector3(pos4.X, pos4.Y, pos4.Z);
                }
            }   // end of class FlatTerrainOptimizer_FD

        }   // end of class Renderable
    
    }   // end of class Tile

}   // end of namespace Boku.SimWorld.Terra
