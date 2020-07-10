// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;

namespace Boku.SimWorld
{
    /// <summary>
    /// </summary>
    public class Skirt : INeedsDeviceReset
    {
        private VertexBuffer[] vbuf = null;
        private int stride = 0;
        // unused 1/10/2008 mattmac // private int index = -1;

        private Terrain.SkirtVertex[][] vertices = null;

        #region Accessors
        public VertexBuffer[] VBuf
        {
            get { return vbuf; }
        }
        public int Stride
        {
            get { return stride; }
        }
        public int NumTriangles
        {
            get { return (Tile.TileSize - 1) * 2; }
        }
        public int NumVertices
        {
            get { return Tile.TileSize * 2; }
        }
        #endregion

        /// <summary>
        /// Create a new skirt object.
        /// </summary>
        /// <param name="heightMap">The current heightMap.</param>
        /// <param name="offset">The offset into the heightmap for this tile.</param>
        public Skirt(HeightMap heightMap, Point offset)
        {
            stride = System.Runtime.InteropServices.Marshal.SizeOf(new Terrain.SkirtVertex());

            // Allocate local vertex array.
            vertices = new Terrain.SkirtVertex[4][];
            for(int i=0; i<4; i++)
            {
                vertices[i] = new Terrain.SkirtVertex[NumVertices];
            }

            vbuf = new VertexBuffer[4];

            Update(heightMap, offset);
        }

        /// <summary>
        /// Updates the current skirt based on the heightmap data.
        /// </summary>
        /// <param name="heightMap"></param>
        public void Update(HeightMap heightMap, Point offset)
        {
            int width = Tile.TileSize;
            int height = Tile.TileSize;

            float scaleX = heightMap.Scale.X / (float)(heightMap.Size.X - 1);
            float scaleY = heightMap.Scale.Y / (float)(heightMap.Size.Y - 1);

            //
            // Skirt at Y==0
            //
            // Set the vertex postions and normals.
            for (int i = 0; i < width; i++)
            {
                float x = (offset.X + i) * scaleX;
                float y = offset.Y * scaleY;
                float z = heightMap.GetHeight(i + offset.X, offset.Y);
                vertices[0][i * 2 + 0].position = new Vector3(x, y, z);
                vertices[0][i * 2 + 0].normal = new Vector3(0.0f, -1.0f, 0.0f);
                vertices[0][i * 2 + 0].uv = new Vector2(x * 0.01f, 0.0f);
                vertices[0][i * 2 + 1].position = new Vector3(x, y, -heightMap.Scale.Z);
                vertices[0][i * 2 + 1].normal = new Vector3(0.0f, -1.0f, 0.0f);
                vertices[0][i * 2 + 1].uv = new Vector2(x * 0.01f, (z / heightMap.Scale.Z + 3.0f) * 0.25f);
            }

            //
            // Skirt at Y==max
            //
            // Set the vertex postions and normals.
            for (int i = 0; i < width; i++)
            {
                float x = (offset.X + i) * scaleX;
                float y = (Tile.TileSize - 1 + offset.Y) * scaleY;
                float z = heightMap.GetHeight(i + offset.X, Tile.TileSize - 1 + offset.Y);
                vertices[1][i * 2 + 0].position = new Vector3(x, y, z);
                vertices[1][i * 2 + 0].normal = new Vector3(0.0f, 1.0f, 0.0f);
                vertices[1][i * 2 + 0].uv = new Vector2(x * 0.01f, 0.0f);
                vertices[1][i * 2 + 1].position = new Vector3(x, y, -heightMap.Scale.Z);
                vertices[1][i * 2 + 1].normal = new Vector3(0.0f, 1.0f, 0.0f);
                vertices[1][i * 2 + 1].uv = new Vector2(x * 0.01f, (z / heightMap.Scale.Z + 3.0f) * 0.25f);
            }

            //
            // Skirt at X==0
            //
            // Set the vertex postions and normals.
            for (int j = 0; j < height; j++)
            {
                float x = offset.X * scaleX;
                float y = (offset.Y + j) * scaleY;
                float z = heightMap.GetHeight(offset.X, j + offset.Y);
                vertices[2][j * 2 + 0].position = new Vector3(x, y, z);
                vertices[2][j * 2 + 0].normal = new Vector3(-1.0f, 0.0f, 0.0f);
                vertices[2][j * 2 + 0].uv = new Vector2(y * 0.01f, 0.0f);
                vertices[2][j * 2 + 1].position = new Vector3(x, y, -heightMap.Scale.Z);
                vertices[2][j * 2 + 1].normal = new Vector3(-1.0f, 0.0f, 0.0f);
                vertices[2][j * 2 + 1].uv = new Vector2(y * 0.01f, (z / heightMap.Scale.Z + 3.0f) * 0.25f);
            }

            //
            // Skirt at X==max
            //
            // Set the vertex postions and normals.
            for (int j = 0; j < height; j++)
            {
                float x = (Tile.TileSize - 1 + offset.X) * scaleX;
                float y = (offset.Y + j) * scaleY;
                float z = heightMap.GetHeight(Tile.TileSize - 1 + offset.X, j + offset.Y);
                vertices[3][j * 2 + 0].position = new Vector3(x, y, z);
                vertices[3][j * 2 + 0].normal = new Vector3(1.0f, 0.0f, 0.0f);
                vertices[3][j * 2 + 0].uv = new Vector2(y * 0.01f, 0.0f);
                vertices[3][j * 2 + 1].position = new Vector3(x, y, -heightMap.Scale.Z);
                vertices[3][j * 2 + 1].normal = new Vector3(1.0f, 0.0f, 0.0f);
                vertices[3][j * 2 + 1].uv = new Vector2(y * 0.01f, (z / heightMap.Scale.Z + 3.0f) * 0.25f);
            }

        }   // end of Skirt Update()


        public void LoadGraphicsContent(GraphicsDeviceManager graphics)
        {
            GraphicsDevice device = graphics.GraphicsDevice;

            // Init vertex buffers.
            for (int i = 0; i < 4; i++)
            {
                vbuf[i] = new VertexBuffer(device, typeof(Terrain.SkirtVertex), NumVertices, BufferUsage.WriteOnly);

            // Copy local data to vertex buffer.
#if !XBOX360
            vbuf[i].SetData<Terrain.SkirtVertex>(vertices[i], 0, NumVertices ); //, SetDataOptions.Discard);
#else
            vbuf[i].SetData<Terrain.SkirtVertex>(vertices[i], 0, NumVertices ); // , SetDataOptions.None);
#endif
        }

        }   // end of Skirt LoadGraphicsContent()

        public void UnloadGraphicsContent()
        {
            for (int i = 0; i < 4; i++)
            {
                BokuGame.Release(ref vbuf[i]);
            }
        }   // end of Skirt UnloadGraphicsContent()

    }   // end of class Skirt

}   // end of namespace Boku.SimWorld



