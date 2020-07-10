// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Common;
using Boku.SimWorld.Terra;

namespace Boku.SimWorld.Path
{
    public class HiWayGen : RoadGenerator
    {
        #region Members
        protected int numSteps = 0;

        // These tables define the profile of the road/wall
        protected float[] widthTable =    { 0.80f, 1.00f, 1.00f, 1.10f, 1.10f, 1.10f, 0.0f };
        protected float[] heightTable =   { 0.25f, 0.25f, 0.30f, 0.30f, 0.00f, 0.00f, 0.0f };
        protected float[] tex0Source =    { 1.00f, 1.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f };
        protected float[] tex1Source = null;
        protected float[] tex2Source = null;
        // For UV source, 0 is horizontal mapping, 1.0 is vertical mapping
        protected float[] uvSource =      { 0.00f, 0.00f, 1.00f, 1.00f, 1.00f, 0.00f, 0.00f };
        protected float centerHeight = 0.25f;

        // These are generated from the above tables
        protected bool[] skipTable = null;
        protected int numSkips = 0;
        private float[] dwTable = null;
        private float[] dhTable = null;
        protected float[] distTable = null;
        protected float maxWidth = 0.0f;
        protected float maxHeight = 0.0f;
        protected float minHeight = 0.0f;

        protected Texture2D diffTex0 = BokuGame.Load<Texture2D>(
                            BokuGame.Settings.MediaPath
                            + @"Textures\Terrain\GroundTextures\alum_plt");
        protected Texture2D diffTex1 = BokuGame.Load<Texture2D>(
                            BokuGame.Settings.MediaPath
                            + @"Textures\Terrain\GroundTextures\RIVROCK1");
        protected Texture2D normTex0 = BokuGame.Load<Texture2D>(
                            BokuGame.Settings.MediaPath
                            + @"Textures\Terrain\GroundTextures\alum_plt_norm");
        protected Texture2D normTex1 = BokuGame.Load<Texture2D>(
                            BokuGame.Settings.MediaPath
                            + @"Textures\Terrain\GroundTextures\RIVROCK1_norm");

        protected Vector4 uvXfm = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

        protected float shininess = 0.4f;
        #endregion Members

        #region Accessors
        /// <summary>
        /// HiWays can be travelled on.
        /// </summary>
        /// <returns></returns>
        public override bool MakesGround
        {
            get { return true; }
        }

        /// <summary>
        /// Return the maximum width
        /// </summary>
        public override float MaxWidth
        {
            get { return maxWidth; }
        }

        /// <summary>
        /// Return the maximum height generated (relative to the terrain).
        /// </summary>
        public override float MaxHeight
        {
            get { return maxHeight; }
        }

        /// <summary>
        /// Return the minimum height generated (relative to the terrain).
        /// </summary>
        public override float MinHeight
        {
            get { return minHeight; }
        }

        /// <summary>
        /// How much this surface reflects environment map.
        /// </summary>
        public float Shininess
        {
            get { return shininess; }
            protected set { shininess = value; }
        }
        #endregion Accessors

        #region Public
        /// <summary>
        /// Constructor
        /// </summary>
        public HiWayGen()
        {
            InitDelTables();

            BokuGame.Load(this);
        }

        /// <summary>
        /// If the position is on the road, compute the height of the road and return true.
        /// Else return false.
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="pos"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public override bool GetHeight(
            Vector3 p0, 
            Vector3 p1, 
            Vector3 pos, 
            ref float height)
        {
            float distSq = DistanceSqToSection(p0, p1, pos);

            if ((distSq < 0) || (distSq >= MaxWidth * MaxWidth))
            {
                return false;
            }

            if (HeightFromTable(distSq, ref height))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// If the position is on the road, compute the height of the road and return true.
        /// Else return false.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="pos"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public override bool GetHeight(
            Vector3 center, 
            Vector3 pos, 
            ref float height)
        {
            float distSq = (center.X - pos.X) * (center.X - pos.X) + (center.Y - pos.Y) * (center.Y - pos.Y);
            if (HeightFromTable(distSq, ref height))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Generate a new section.
        /// </summary>
        /// <param name="section"></param>
        public override void NewSection(Road.Section section)
        {
            section.RenderObj = null;

            RoadVertex[] verts = GenVerts(section);

            Int16[] indices = GenIndices(section);

            if ((verts.Length > 0) && (indices.Length > 0))
            {
                RoadStdRenderObj ro = new RoadStdRenderObj();

                ro.Verts = verts;
                ro.Indices = indices;
                ro.DiffuseTex0 = diffTex0;
                ro.DiffuseTex1 = diffTex1;
                ro.NormalTex0 = normTex0;
                ro.NormalTex1 = normTex1;
                ro.UVXfm = uvXfm;
                ro.Shininess = Shininess;

                section.RenderObj = ro;
            }
        }

        /// <summary>
        /// Generate a fan to fill out an intersection.
        /// </summary>
        /// <param name="nodeC"></param>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public override bool NewFan(
            Road.Intersection isect, 
            Road.Section first, 
            Road.Section second,
            List<Road.RenderObj> fans)
        {
            if ((first == null) || (second == null))
            {
                return NewFan(isect, new Vector2(1.0f, 0.0f), Math.PI * 2.0, fans);
            }

            WayPoint.Node nodeC = isect.Node;
            WayPoint.Node node0;
            WayPoint.Node node1;
            if (!FindBend(first, second, nodeC, out node0, out node1))
            {
                return false;
            }

            Vector2 center = nodeC.Position2d;
            Vector2 firstEdge = Vector2.Normalize(node0.Position2d - center);
            Vector2 startRadial = new Vector2(-firstEdge.Y, firstEdge.X);

            double rads0 = first.EdgeAngle(nodeC) + Math.PI * 0.5;
            if (rads0 < 0.0)
            {
                rads0 += Math.PI * 2.0;
            }
            double rads1 = second.EdgeAngle(nodeC) - Math.PI * 0.5;
            if (rads1 < 0.0)
            {
                rads1 += Math.PI * 2.0;
            }
            double rads = rads1 - rads0;
            if (rads < 0.0)
            {
                rads += Math.PI * 2.0;
            }
            return NewFan(isect, startRadial, rads, fans);
        }


        #endregion Public

        #region Internal
        /// <summary>
        /// Interpolate the height based on the distance squared from the center of the road.
        /// </summary>
        /// <param name="distSq"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        protected bool HeightFromTable(float distSq, ref float height)
        {
            bool ret = false;
            if (distSq <= MaxWidth * MaxWidth)
            {
                float dist = (float)Math.Sqrt(distSq);

                if (dist < widthTable[0])
                {
                    float h = dist / widthTable[0];
                    h = centerHeight + h * (heightTable[0] - centerHeight);
                    if (h > height)
                    {
                        ret = true;
                        height = h;
                    }
                }

                // find all width pairs that bracket dist, and return the max
                // interpolated height for them all.
                for (int i = 1; i < widthTable.Length; ++i)
                {
                    float dback = dist - widthTable[i - 1];

                    if ((dback >= 0.0f) && (widthTable[i] - dist > 0.0f))
                    {
                        float h = dback / (widthTable[i] - widthTable[i - 1]);
                        h = heightTable[i - 1] + h * (heightTable[i] - heightTable[i - 1]);

                        if (h > height)
                        {
                            height = h;
                            ret = true;
                        }
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// The number of vertices transverse to the direction of road travel.
        /// </summary>
        protected int VertsPerStep
        {
            get { return widthTable.Length * 2 + 1; }
        }

        protected bool FloatSame(float a, float b)
        {
            return Math.Abs(a - b) <= 0.001f;
        }

        /// <summary>
        /// Initialize table of which quads are degenerate.
        /// </summary>
        protected void InitSkipTable()
        {
            int tabLen = widthTable.Length;

            skipTable = new bool[tabLen];
            numSkips = 0;

            skipTable[0] = FloatSame(widthTable[0], 0.0f) && FloatSame(heightTable[0], centerHeight);
            if (skipTable[0])
            {
                ++numSkips;
            }
            maxWidth = widthTable[0];
            maxHeight = heightTable[0];
            minHeight = heightTable[0];

            for (int iTab = 1; iTab < tabLen; ++iTab)
            {
                maxWidth = Math.Max(maxWidth, widthTable[iTab]);
                maxHeight = Math.Max(maxHeight, heightTable[iTab]);
                minHeight = Math.Min(minHeight, heightTable[iTab]);

                skipTable[iTab] = FloatSame(widthTable[iTab], widthTable[iTab - 1])
                                && FloatSame(heightTable[iTab], heightTable[iTab - 1]);

                if (skipTable[iTab])
                {
                    ++numSkips;
                }
            }

        }

        /// <summary>
        ///  Initialize tables of change in width and height.
        /// </summary>
        protected void InitDelTables()
        {
            int tabLen = widthTable.Length;

            InitSkipTable();

            dwTable = new float[tabLen];
            dwTable[0] = widthTable[1] - 0.0f;
            dwTable[tabLen - 1] = widthTable[tabLen - 1] - widthTable[tabLen - 2];

            dhTable = new float[tabLen];
            dhTable[0] = heightTable[1] - centerHeight;
            dhTable[tabLen - 1] = heightTable[tabLen - 1] - heightTable[tabLen - 2];

            for (int iTab = 1; iTab < tabLen - 1; ++iTab)
            {
                dwTable[iTab] = widthTable[iTab + 1] - widthTable[iTab - 1];
                dhTable[iTab] = heightTable[iTab + 1] - heightTable[iTab - 1];
            }

            distTable = new float[tabLen];
            Vector2 del = new Vector2(widthTable[0],
                                        heightTable[0] - centerHeight);
            distTable[0] = del.Length();
            for (int iTab = 1; iTab < tabLen; ++iTab)
            {
                del.X = widthTable[iTab] - widthTable[iTab - 1];
                del.Y = heightTable[iTab] - heightTable[iTab - 1];
                distTable[iTab] = del.Length();
                distTable[iTab] += distTable[iTab - 1];
            }

            // sanity check our (currently unused) other source tables
            if ((tex1Source == null) || (tex1Source.Length != tex0Source.Length))
            {
                // will default to fill in with zeros
                tex1Source = new float[tex0Source.Length];
            }
            if ((tex2Source == null) || (tex2Source.Length != tex0Source.Length))
            {
                // will default to fill in with zeros
                tex2Source = new float[tex0Source.Length];
            }
        }

        protected Color FromVector4(Vector4 v4)
        {
            return new Color(v4);
        }

        protected Color TexSelect(int iVert)
        {
            return FromVector4(new Vector4(
                            tex0Source[iVert],
                            tex1Source[iVert],
                            tex2Source[iVert],
                            uvSource[iVert]));
        }

        /// <summary>
        /// Compute the normal for a vertex. Normals are based on the terrain
        /// normal for horizontal areas, and based on the profile for vertical areas.
        /// </summary>
        /// <param name="terrNormal"></param>
        /// <param name="normAxis"></param>
        /// <param name="iVert"></param>
        /// <returns></returns>
        protected Vector3 VtxNormal(Vector3 terrNormal, Vector2 normAxis, int iVert)
        {
            Vector3 normal = new Vector3(normAxis.X,
                                         normAxis.Y,
                                         0.0f);

            float dw = Math.Abs(dwTable[iVert]);
            float dh = Math.Abs(dhTable[iVert]);

            normal *= -dhTable[iVert];
            normal.Z = dw;
            normal.Normalize();

            if (dw < dh)
            {
                normal = normal + dw / dh * (terrNormal - normal);
            }
            else
            {
                Debug.Assert(Math.Abs(dh) <= Math.Abs(dw));
                normal = terrNormal + dh / dw * (normal - terrNormal);
            }

            if (dwTable[iVert] < 0)
            {
                normal.Z = -normal.Z;
            }

            return Vector3.Normalize(normal);
        }

        /// <summary>
        /// Generate the vertex data for a (straight) section of road.
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        protected RoadVertex[] GenVerts(Road.Section section)
        {
            Vector2 p0 = section.Edge.Node0.Position2d;
            Vector2 p1 = section.Edge.Node1.Position2d;

            Vector2 axis = p1 - p0;
            float len = axis.Length();
            axis /= len;

            Vector2 norm = new Vector2(-axis.Y, axis.X);

            float step = Road.Step;
            numSteps = Math.Max(1, (int)Math.Ceiling(len / step));
            step = len / (float)numSteps;

            int vertsPerHalfStep = VertsPerStep / 2;

            int baseVertex = 0;
            RoadVertex[] verts = new RoadVertex[(numSteps + 1) * VertsPerStep];
            for (int iStep = 0; iStep <= numSteps; ++iStep)
            {
                Vector2 posCen = p0 + axis * step * iStep;

                Vector3 terrNormal = Vector3.UnitZ;
                // line it up with direction of travel by subtracting out component
                // along norm.
                //vtxNormal.X -= vtxNormal.X * norm.X;
                //vtxNormal.Y -= vtxNormal.Y * norm.Y;
                //vtxNormal.Normalize();

                // Negative side
                for (int iVert = vertsPerHalfStep-1; iVert >= 0; --iVert)
                {
                    float width = widthTable[iVert];
                    float height = heightTable[iVert];

                    Vector2 pos = new Vector2(
                        posCen.X - norm.X * width,
                        posCen.Y - norm.Y * width);

                    //float baseHeight = section.BaseHeight(pos);
                    //height += baseHeight;

                    verts[baseVertex].pos.X = pos.X;
                    verts[baseVertex].pos.Y = pos.Y;
                    verts[baseVertex].pos.Z = height;

                    verts[baseVertex].norm = VtxNormal(terrNormal, -norm, iVert);

                    verts[baseVertex].uv.X = step * iStep;
                    verts[baseVertex].uv.Y = distTable[iVert];
                    verts[baseVertex].texSelect = TexSelect(iVert);

                    ++baseVertex;
                }

                // Center
                float centerBaseHeight = 0.0f; // section.BaseHeight(posCen);
                float heightAtCenter = centerHeight + centerBaseHeight;
                verts[baseVertex].pos.X = posCen.X;
                verts[baseVertex].pos.Y = posCen.Y;
                verts[baseVertex].pos.Z = heightAtCenter;
                verts[baseVertex].norm = terrNormal;
                verts[baseVertex].uv.X = step * iStep;
                verts[baseVertex].uv.Y = 0.0f;
                verts[baseVertex].texSelect = TexSelect(0);

                ++baseVertex;

                // Positive side
                for (int iVert = 0; iVert < vertsPerHalfStep; ++iVert)
                {
                    float width = widthTable[iVert];
                    float height = heightTable[iVert];

                    Vector2 pos = new Vector2(
                        posCen.X + norm.X * width,
                        posCen.Y + norm.Y * width);

                    //float baseHeight = section.BaseHeight(pos);
                    //height += baseHeight;

                    verts[baseVertex].pos.X = pos.X;
                    verts[baseVertex].pos.Y = pos.Y;
                    verts[baseVertex].pos.Z = height;

                    verts[baseVertex].norm = VtxNormal(terrNormal, norm, iVert);

                    verts[baseVertex].uv.X = step * iStep;
                    verts[baseVertex].uv.Y = distTable[iVert];
                    verts[baseVertex].texSelect = TexSelect(iVert);

                    ++baseVertex;
                }
            }
            return verts;
        }

        /// <summary>
        /// Lookup whether this quad would be degenerate.
        /// </summary>
        /// <param name="iQuad"></param>
        /// <param name="quadsPerSide"></param>
        /// <returns></returns>
        protected bool SkipQuad(int iQuad, int quadsPerSide)
        {
            int skipIdx = iQuad >= quadsPerSide
                        ? iQuad - quadsPerSide
                        : quadsPerSide - iQuad - 1;
            
            return skipTable[skipIdx];
        }

        /// <summary>
        /// Generate connectivity for a straight section of road.
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        protected Int16[] GenIndices(Road.Section section)
        {
            int vertRowBase = 0;
            int nextRow = VertsPerStep; // 3 per row
            int nextCol = 1; // the next one over
            int quadsPerStep = VertsPerStep - 1;
            int quadsPerSide = quadsPerStep / 2;
            int numTris = numSteps * (quadsPerStep - numSkips * 2) * 2;
            Int16[] indices = new Int16[numTris * 3];
            int idx = 0;
            for (int iStep = 0; iStep < numSteps; ++iStep)
            {
                if ((iStep & 1) != 0)
                {
                    for (int iQuad = 0; iQuad < quadsPerStep; iQuad += 2)
                    {
                        int vertBase = vertRowBase + iQuad;

                        if (!SkipQuad(iQuad, quadsPerSide))
                        {
                            indices[idx++] = ((Int16)vertBase);
                            indices[idx++] = ((Int16)(vertBase + nextCol));
                            indices[idx++] = ((Int16)(vertBase + nextRow));

                            indices[idx++] = ((Int16)(vertBase + nextRow));
                            indices[idx++] = ((Int16)(vertBase + nextCol));
                            indices[idx++] = ((Int16)(vertBase + nextRow + nextCol));
                        }

                        ++vertBase;

                        if (!SkipQuad(iQuad + 1, quadsPerSide))
                        {
                            indices[idx++] = ((Int16)(vertBase + nextCol));
                            indices[idx++] = ((Int16)(vertBase + nextCol + nextRow));
                            indices[idx++] = ((Int16)(vertBase));

                            indices[idx++] = ((Int16)(vertBase));
                            indices[idx++] = ((Int16)(vertBase + nextCol + nextRow));
                            indices[idx++] = ((Int16)(vertBase + nextRow));
                        }
                    }
                }
                else
                {
                    for (int iQuad = 0; iQuad < quadsPerStep; iQuad += 2)
                    {
                        int vertBase = vertRowBase + iQuad;

                        if (!SkipQuad(iQuad, quadsPerSide))
                        {
                            indices[idx++] = ((Int16)(vertBase + nextCol));
                            indices[idx++] = ((Int16)(vertBase + nextCol + nextRow));
                            indices[idx++] = ((Int16)(vertBase));

                            indices[idx++] = ((Int16)(vertBase));
                            indices[idx++] = ((Int16)(vertBase + nextCol + nextRow));
                            indices[idx++] = ((Int16)(vertBase + nextRow));
                        }

                        ++vertBase;

                        if (!SkipQuad(iQuad + 1, quadsPerSide))
                        {
                            indices[idx++] = ((Int16)vertBase);
                            indices[idx++] = ((Int16)(vertBase + nextCol));
                            indices[idx++] = ((Int16)(vertBase + nextRow));

                            indices[idx++] = ((Int16)(vertBase + nextRow));
                            indices[idx++] = ((Int16)(vertBase + nextCol));
                            indices[idx++] = ((Int16)(vertBase + nextRow + nextCol));
                        }
                    }
                }

                vertRowBase += VertsPerStep;
            }
            return indices;
        }

        protected virtual bool NewFan(
            Road.Intersection isect, 
            Vector2 startRadial, 
            double rads,
            List<Road.RenderObj> fans)
        {
            Road road = isect.Road;
            int numRadials = (int)Math.Ceiling(rads / road.RadStep);
            double radStep = rads / numRadials;

            Vector2 center = isect.Node.Position2d;

            int vertsPerRadial = VertsPerStep / 2;
            int numVerts = (numRadials + 1) * vertsPerRadial + 1;
            RoadVertex[] verts = new RoadVertex[numVerts];

            Vector3 terrNormal = Vector3.UnitZ;

            AABB box = AABB.EmptyBox();

            int iVert = 0;
            for (int iRadial = 0; iRadial <= numRadials; ++iRadial)
            {
                double ang = radStep * iRadial;
                float cos = (float)Math.Cos(ang);
                float sin = (float)Math.Sin(ang);

                Vector2 radial = new Vector2(startRadial.X * cos - startRadial.Y * sin,
                                             startRadial.X * sin + startRadial.Y * cos);

                for (int iOut = 0; iOut < vertsPerRadial; ++iOut)
                {
                    float width = widthTable[iOut];
                    float height = heightTable[iOut];

                    float arc = (float)(ang * width);

                    RoadVertex vtx = new RoadVertex();
                    Vector2 pos = center + width * radial;

                    //float baseHeight = isect.BaseHeight(pos);
                    //height += baseHeight;

                    vtx.pos.X = pos.X;
                    vtx.pos.Y = pos.Y;
                    vtx.pos.Z = height;

                    box.Union(vtx.pos);

                    vtx.norm = VtxNormal(terrNormal, radial, iOut);

                    vtx.uv.X = arc;
                    vtx.uv.Y = distTable[iOut];
                    vtx.texSelect = TexSelect(iOut);

                    verts[iVert++] = vtx;
                }
            }

            float centerBaseHeight = 0.0f; //  isect.BaseHeight(center);
            float heightAtCenter = centerHeight + centerBaseHeight;

            RoadVertex last = new RoadVertex();
            last.pos.X = center.X;
            last.pos.Y = center.Y;
            last.pos.Z = heightAtCenter;

            box.Union(last.pos);

            last.norm = terrNormal;

            last.uv.X = 0.0f;
            last.uv.Y = 0.0f;
            last.texSelect = TexSelect(0);

            int centerIdx = iVert++;
            verts[centerIdx] = last;

            Debug.Assert(iVert == numVerts);

            // First step per radial is a single tri, 
            // then the rest of the steps are quads
            int quadsPerRadial = vertsPerRadial - 1;

            int numTris = 0;
            if( !skipTable[0])
            {
                numTris += numRadials;

                numTris += numRadials * (quadsPerRadial - numSkips) * 2;
            }
            else
            {
                numTris += numRadials * (quadsPerRadial - (numSkips - 1)) * 2;
            }
            Int16[] indices = new Int16[numTris * 3];

            int nextRad = vertsPerRadial;
            int nextOut = 1;
            int baseVtx = 0;
            int idx = 0;
            for (int iRadial = 0; iRadial < numRadials; ++iRadial)
            {
                if (!skipTable[0])
                {
                    indices[idx++] = (Int16)(baseVtx);
                    indices[idx++] = (Int16)(centerIdx);
                    indices[idx++] = (Int16)(baseVtx + nextRad);
                }

                for (int iOut = 0; iOut < quadsPerRadial; iOut++)
                {
                    if (!skipTable[iOut + 1])
                    {
                        indices[idx++] = (Int16)(baseVtx + iOut + nextRad);
                        indices[idx++] = (Int16)(baseVtx + iOut + nextRad + nextOut);
                        indices[idx++] = (Int16)(baseVtx + iOut);

                        indices[idx++] = (Int16)(baseVtx + iOut);
                        indices[idx++] = (Int16)(baseVtx + iOut + nextRad + nextOut);
                        indices[idx++] = (Int16)(baseVtx + iOut + nextOut);
                    }
                }
                baseVtx += nextRad;
            }
            Debug.Assert(idx == numTris * 3);

            RoadStdRenderObj ro = null;
            if ((verts.Length > 0) && (indices.Length > 0))
            {
                ro = new RoadStdRenderObj();

                ro.Verts = verts;
                ro.Indices = indices;
                ro.DiffuseTex0 = diffTex0;
                ro.DiffuseTex1 = diffTex1;
                ro.NormalTex0 = normTex0;
                ro.NormalTex1 = normTex1;
                ro.UVXfm = uvXfm;
                ro.Shininess = Shininess;
                ro.Sphere = box.MakeSphere();

                fans.Add(ro);

                return true;
            }
            return false;
        }

        /// <summary>
        /// Load textures.
        /// </summary>
        /// <param name="graphics"></param>
        public override void LoadContent(bool immediate)
        {
            if (diffTex0 == null)
            {
                diffTex0 = BokuGame.Load<Texture2D>(
                                    BokuGame.Settings.MediaPath
                                    + @"Textures\Terrain\GroundTextures\alum_plt");
            }
            if (diffTex1 == null)
            {
                diffTex1 = BokuGame.Load<Texture2D>(
                                    BokuGame.Settings.MediaPath
                                    + @"Textures\Terrain\GroundTextures\RIVROCK1");
            }
            if (normTex0 == null)
            {
                normTex0 = BokuGame.Load<Texture2D>(
                            BokuGame.Settings.MediaPath
                            + @"Textures\Terrain\GroundTextures\alum_plt_norm");
            }
            if (normTex1 == null)
            {
                normTex1 = BokuGame.Load<Texture2D>(
                            BokuGame.Settings.MediaPath
                            + @"Textures\Terrain\GroundTextures\RIVROCK1_norm");
            }

            base.LoadContent(immediate);
        }

        /// <summary>
        /// Dump graphics resources (textures).
        /// </summary>
        public override void UnloadContent()
        {
            BokuGame.Release(ref diffTex0);
            BokuGame.Release(ref diffTex1);
            BokuGame.Release(ref normTex0);
            BokuGame.Release(ref normTex1);
            base.UnloadContent();
        }

        #endregion Internal

    }
}
