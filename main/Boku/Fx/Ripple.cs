// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


#define ALLOW_BLOOM

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;

using Boku.Common;
using Boku.Audio;
using Boku.SimWorld.Terra;

namespace Boku.Fx
{
    public static class Ripple
    {
        #region Members
        private static Texture2D texture = null;

        private static int numTiles = 0;
        private static int numTilesReady = 0;
        private static int nextVert = 0;
        private static int nextLocal = 0;
        private static int lastCulled = 0;

        private static double timeOffset = 0;

        private const int kMaxTiles = 5000;
        private const int kMaxVerts = kMaxTiles * 4;
        private const int kMaxTris = kMaxTiles * 2;
        private const int kMaxIndices = kMaxTris * 3;

#if USE_HALFS
        [StructLayout(LayoutKind.Explicit, Size = 32)]
        private struct Vertex
        {
            [FieldOffset(0)]public Vector2 cubeCenter;    // cubecenter
            [FieldOffset(4)]public Vector2 offset;    // offset from cubecenter to position
            [FieldOffset(8)]public Vector4 uv;          // center.xy, radius.z, birth.w 
            [FieldOffset(24)]public Vector2 water;       // height.x, type.y
        };
        private static VertexElement[] elements = new VertexElement[]
        {
            new VertexElement(0, VertexElementFormat.Short2, VertexElementUsage.Position, 0),
            new VertexElement(4, VertexElementFormat.Short2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(8, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1),
            new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 2),
            // Total == 32 bytes
        };
        private const int Stride = 32;
#else // USE_HALFS
        private struct Vertex : IVertexType
        {
            public Vector2 cubeCenter;  // cubecenter
            public Vector2 offset;      // offset from cubecenter to position
            public Vector4 uv;          // center.xy, radius.z, birth.w 
            public Vector2 water;       // height.x, type.y

            static VertexDeclaration decl = null;
            static VertexElement[] elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1),
                new VertexElement(32, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 2),
                // Total == 40 bytes
            };

            public VertexDeclaration VertexDeclaration
            {
                get
                {
                    if (decl == null || decl.IsDisposed)
                    {
                        decl = new VertexDeclaration(elements);
                    }
                    return decl;
                }
            }

            public static int Stride 
            { 
                get { return 40; } 
            }
        };

#endif // USE_HALFS

        private static DynamicVertexBuffer verts = null;
        private static IndexBuffer indices = null;
        private static Vertex[] localVerts = new Vertex[kMaxVerts];
        private static float[] deathTimes = new float[kMaxTiles];

        public static float WaveSpeed
        {
            get { return 0.75f; }
        }

        #region Parameter Caching
        enum EffectParams
        {
            RippleTex,
            CurrentTime, // yes, I know this is just a float.
            WorldViewProjMatrix,

            WaveCycle,      // Where in the wave cycle we are.
            WaveHeight,		// Max wave amplitude
            WaveCenter,		// Epicenter of our single wave source
            InverseWaveLength, // 2 * PI / WaveLength;
            HalfCube,       // Half the terrain cubesize
            WaveSpeed,      // How fast the ripples travel outward.
            RotRate,        // How fast do the ripples spin.
            Tint,           // Overall tint, including alpha
            UVAge,          // botScale.x, botOffset.y, topScale.z, topOffset.w
            UpDownAgeBot,   // upS.x, upO.y, dnS.x, dnS.y
            UpDownAgeMid,   // upS.x, upO.y, dnS.x, dnS.y
            UpDownAgeTop,   // upS.x, upO.y, dnS.x, dnS.y
        };
        private static EffectCache effectCache = new EffectCache<EffectParams>();
        private static EffectParameter Parameter(EffectParams param)
        {
            return effectCache.Parameter((int)param);
        }
        private static Effect effect = null;
        #endregion Parameter Caching
        #endregion Members

        #region Accessors
        #endregion Accessors

        #region Public
        public static void Clear()
        {
            numTiles = 0;
            numTilesReady = 0;
            nextLocal = 0;
            nextVert = 0;
            lastCulled = 0;
            timeOffset = Time.GameTimeTotalSeconds;

            for (int i = 0; i < deathTimes.Length; ++i)
            {
                deathTimes[i] = 0;
            }
        }
        public static void Update()
        {
            FlushLocal();
            CullDead();
        }

        private static void CullDead()
        {
            if (freeze)
                return;

            Debug.Assert(numTiles == numTilesReady);

            float currentTime = (float)(Time.GameTimeTotalSeconds - timeOffset);
            int nextTile = nextVert / 4;
            while ((numTiles > 0) && (deathTimes[lastCulled] < currentTime))
            {
                Debug.Assert(lastCulled != nextTile);

                lastCulled = (lastCulled + 1) % kMaxTiles;
                --numTiles;
                --numTilesReady;
            }
        }

        private static void FlushLocal()
        {
            if (nextLocal > 0)
            {
                int numLocal = nextLocal;
                int numTop = kMaxVerts - nextVert;
                if (numTop > numLocal)
                    numTop = numLocal;

                if (numTop > 0)
                {
                    int firstTop = nextVert;
                    int numTopTiles = numTop / 4;
                    int deathBase = nextVert / 4;

                    verts.SetData<Vertex>(
                        firstTop * Vertex.Stride,
                        localVerts,
                        0,
                        numTop,
                        Vertex.Stride);
                }

                int numBot = numLocal - numTop;
                if (numBot > 0)
                {
                    int numBotTiles = numBot / 4;

                    verts.SetData<Vertex>(
                        0,
                        localVerts,
                        numTop,
                        numBot,
                        Vertex.Stride);
                }
                nextVert = (nextVert + numLocal) % kMaxVerts;

                numTilesReady += numLocal / 4;
                nextLocal = 0;
            }
        }
        private static float LifeSpanFromRadius(float radius)
        {
            return radius / WaveSpeed;
        }
        private static void SetupWater()
        {
            /// parms => cycle.x, waveheight.y, invWaveLength.z
            float waveCycle = (float)Time.GameTimeTotalSeconds;
            float waveHeight = Terrain.WaveHeight;
            const double WaveLength = 15.0;
            float invWaveLength = (float)(2.0 * Math.PI / WaveLength);

            Vector2 waveCenter = new Vector2(127.0f, 600.0f);

            Vector2 waveSpeed = new Vector2(WaveSpeed, 1.0f / WaveSpeed);

            Vector2 rotRate = new Vector2((float)(Math.PI * 2.0 / 3.0), (float)(-Math.PI * 2.0 / 1.8));

            Parameter(EffectParams.WaveCycle).SetValue(waveCycle);
            Parameter(EffectParams.WaveHeight).SetValue(waveHeight);
            Parameter(EffectParams.InverseWaveLength).SetValue(invWaveLength);
            Parameter(EffectParams.WaveCenter).SetValue(waveCenter);
            Parameter(EffectParams.WaveSpeed).SetValue(waveSpeed);
            Parameter(EffectParams.RotRate).SetValue(rotRate);

            float halfCube = Terrain.Current.CubeSize * 0.5f;
            Parameter(EffectParams.HalfCube).SetValue(halfCube);
            Parameter(EffectParams.RippleTex).SetValue(texture);

            float currentTime = (float)(Time.GameTimeTotalSeconds - timeOffset);
            Parameter(EffectParams.CurrentTime).SetValue(currentTime);

            Vector4 tint = new Vector4(1.0f, 1.0f, 1.0f, 0.6f);
            tint *= ShaderGlobals.ParticleTint(false);
            Parameter(EffectParams.Tint).SetValue(tint);

            SetupAges();
        }

        private static void SetupAges()
        {
            float T0 = 0;
            /// when the bot is fully ramped up
            float T1 = 0.25f;
            /// when the middle starts ramping up and the bot starts ramping down
            float T2 = 0.3f;
            /// when the middle is fully ramped up, the bot is fully off, and the top starts ramping up
            float T3 = 0.5f;
            /// When the middle is fully down, and the top is fully up
            float T4 = 0.6f;
            /// When the top is fully down
            float T5 = 1.0f;

            Parameter(EffectParams.UVAge).SetValue(new Vector4(
                1 / (T5 - T0), -T0 / (T5 - T0), // scale and offset for bot uv
                1 / (T5 - T0), -T0 / (T5 - T0) // scale and offset for top uv
                ));

            Parameter(EffectParams.UpDownAgeBot).SetValue(new Vector4(
                1 / (T1 - T0), -T0 / (T3 - T0), // scale and offset for up on bot
                -1 / (T3 - T2), T3 / (T3 - T2) // scale and offset for down on bot
                ));

            Parameter(EffectParams.UpDownAgeMid).SetValue(new Vector4(
                1 / (T3 - T2), -T2 / (T3 - T2), // scale and offset for up on mid
                -1 / (T4 - T3), T4 / (T4 - T3) // scale and offset for down on mid
                ));

            //Parameter(EffectParams.UpDownAgeTop).SetValue(new Vector4(
            //    1 / (T4 - T3), -T3 / (T4 - T3), // scale and offset for up on top
            //    -1 / (T5 - T4), T5 / (T5 - T4) // scale and offset for down on top
            //    ));

            Parameter(EffectParams.UpDownAgeTop).SetValue(new Vector4(
                1 / (T1 - T0), -T0 / (T1 - T0), // scale and offset for up on top
                -1 / (T5 - T4), T5 / (T5 - T4) // scale and offset for down on top
                ));

        }

        public static void Render(Camera camera)
        {
            if (numTilesReady > 0)
            {
                GraphicsDevice device = KoiLibrary.GraphicsDevice;

                Debug.Assert(InGame.inGame.renderEffects == InGame.RenderEffect.Normal);

                Parameter(EffectParams.WorldViewProjMatrix).SetValue(camera.ViewProjectionMatrix);

                SetupWater();

#if !ALLOW_BLOOM
                ShaderGlobals.FixExplicitBloom(0);
#endif // !ALLOW_BLOOM

#if !LLLLLL
                //device.RasterizerState = Shared.RasterStateWireframe;


                device.Indices = indices;
                device.SetVertexBuffer(verts);

                EffectTechnique tech = effect.CurrentTechnique;
                int numPasses = tech.Passes.Count;
                for (int i = 0; i < numPasses; ++i)
                {
                    EffectPass pass = tech.Passes[i];
                    pass.Apply();

                    int firstVert = lastCulled * 4;
                    int lastVert = nextVert;
                    if (firstVert < lastVert)
                    {
                        int numVerts = lastVert - firstVert;
                        int firstTri = firstVert / 4 * 2;
                        int numTris = numVerts * 2 / 4;

                        int firstIndex = firstTri * 3;

                        device.DrawIndexedPrimitives(
                            PrimitiveType.TriangleList,
                            0,
                            firstVert,
                            numVerts,
                            firstIndex,
                            numTris);
                    }
                    else
                    {
                        int firstTopTri = firstVert / 4 * 2;
                        int numTopVerts = kMaxVerts - firstVert;
                        int numTopTris = numTopVerts * 2 / 4;

                        int firstTopIndex = firstTopTri * 3;

                        if (numTopTris > 0)
                        {
                            device.DrawIndexedPrimitives(
                                PrimitiveType.TriangleList,
                                0,
                                firstVert,
                                numTopVerts,
                                firstTopIndex,
                                numTopTris);
                        }

                        int firstBotVert = 0;
                        int numBotVerts = lastVert;
                        int numBotTris = numBotVerts / 4 * 2;

                        int firstBotIndex = 0;

                        if (numBotTris > 0)
                        {
                            device.DrawIndexedPrimitives(
                                PrimitiveType.TriangleList,
                                0,
                                firstBotVert,
                                numBotVerts,
                                firstBotIndex,
                                numBotTris);
                        }

                    }
                }

                //device.RasterizerState = RasterizerState.CullCounterClockwise;

#if !ALLOW_BLOOM
                ShaderGlobals.ReleaseExplicitBloom();
#endif // !ALLOW_BLOOM

#endif /// LLLLLL
            }
        }

        private static bool freeze = false;
        public static bool Add(Vector3 pos, float radius)
        {
            if (freeze)
                return false;

            if (radius <= 0)
                return false;

            Vector2 pos2 = new Vector2(pos.X, pos.Y);

            Water water = Terrain.GetWater(pos2);

            if (water != null)
            {
                /// Figure how many tiles this will cover
                Point nn = Terrain.WorldToVirtualIndex(new Vector2(pos.X - radius, pos.Y - radius));
                Point pp = Terrain.WorldToVirtualIndex(new Vector2(pos.X + radius, pos.Y + radius));

                _tilePointsScratch.Clear();
                for (int i = nn.X; i <= pp.X; ++i)
                {
                    for (int j = nn.Y; j <= pp.Y; ++j)
                    {
                        Point here = new Point(i, j);
                        Water w = Terrain.GetWater(here);
                        if (w == water)
                        {
                            _tilePointsScratch.Add(here);
                        }
                    }
                }

                /// If we have enough room...
                if (numTiles + _tilePointsScratch.Count < kMaxTiles)
                {
                    float life = LifeSpanFromRadius(radius);
                    float currentTime = (float)(Time.GameTimeTotalSeconds - timeOffset);
                    float death = currentTime + life;

                    int tileBase = (nextVert + nextLocal) / 4;

                    int cnt = _tilePointsScratch.Count;
                    for (int i = 0; i < cnt; ++i)
                    {
                        MakeVerts(water, pos2, radius, nextLocal, _tilePointsScratch[i]);

                        deathTimes[(tileBase + i) % kMaxTiles] = death;

                        nextLocal += 4;
                        Debug.Assert(nextLocal <= kMaxVerts);
                    }
                    numTiles += cnt;
                    return true;
                }
            }
            return false;
        }
        private static List<Point> _tilePointsScratch = new List<Point>();
        #endregion Public

        #region Internal

        private static void MakeVerts(Water water, Vector2 center, float radius, int vtxIdx, Point tile)
        {
            Vector2 cubeCenter2 = Terrain.VirtualIndexToWorld(tile);

            Vector3 cubeCenter = new Vector3(cubeCenter2, Terrain.GetTerrainHeightFlat(cubeCenter2));

            float birthTime = (float)(Time.GameTimeTotalSeconds - timeOffset);

            //float uvScale = 0.5f / radius;
            //Vector2 uvOffset = new Vector2(0.5f - center.X * uvScale.X, 0.5f - center.Y * uvScale.Y);
            
            SetVert(
                water.BaseHeight,
                water.Type,
                vtxIdx++,
                cubeCenter,
                new Vector2(-1, -1),
                center,
                radius,
                birthTime);
            SetVert(
                water.BaseHeight,
                water.Type,
                vtxIdx++,
                cubeCenter,
                new Vector2(+1, -1),
                center,
                radius,
                birthTime);
            SetVert(
                water.BaseHeight,
                water.Type,
                vtxIdx++,
                cubeCenter,
                new Vector2(-1, +1),
                center,
                radius,
                birthTime);
            SetVert(
                water.BaseHeight,
                water.Type,
                vtxIdx++,
                cubeCenter,
                new Vector2(+1, +1),
                center,
                radius,
                birthTime);
        }

        /// <summary>
        /// Setup a single vertex.
        /// </summary>
        /// <param name="vtxIdx"></param>
        /// <param name="pos"></param>
        /// <param name="birthTime"></param>
        /// <param name="uvScale"></param>
        /// <param name="uvOffset"></param>
        private static void SetVert(
            float baseHeight,
            int waterType,
            int vtxIdx, 
            Vector3 cubeCenter, 
            Vector2 offset,
            Vector2 center, 
            float radius, 
            float birthTime)
        {
            localVerts[vtxIdx].cubeCenter = new Vector2(cubeCenter.X, cubeCenter.Y);
            localVerts[vtxIdx].offset = offset;
            localVerts[vtxIdx].uv = new Vector4(
                center.X,
                center.Y,
                1.0f / radius,
                birthTime);
            // The 0.01f pushes the ripple effect up slightly to prevent z-fighting
            // with the surface of the water.
            localVerts[vtxIdx].water = new Vector2(baseHeight, cubeCenter.Z + 0.01f);
        }

        public static void Load(GraphicsDevice device)
        {
            if (effect == null)
            {
                effect = KoiLibrary.LoadEffect(@"Shaders\Ripple");
                ShaderGlobals.RegisterEffect("Ripple", effect);
                effectCache.Load(effect);
            }
            if (texture == null)
            {
                texture = KoiLibrary.LoadTexture2D(@"Textures\BullsEye1");
            }
            if (verts == null)
            {
                verts = new DynamicVertexBuffer(device, typeof(Vertex), kMaxVerts, BufferUsage.WriteOnly);
            }
            if (indices == null)
            {
                indices = new IndexBuffer(device, typeof(Int16), kMaxIndices, BufferUsage.WriteOnly);
                Int16[] localIdx = new Int16[kMaxIndices];
                for (int i = 0; i < kMaxTiles; ++i)
                {
                    localIdx[i * 6 + 0] = (short)(i * 4 + 0);
                    localIdx[i * 6 + 1] = (short)(i * 4 + 1);
                    localIdx[i * 6 + 2] = (short)(i * 4 + 2);

                    localIdx[i * 6 + 3] = (short)(i * 4 + 2);
                    localIdx[i * 6 + 4] = (short)(i * 4 + 1);
                    localIdx[i * 6 + 5] = (short)(i * 4 + 3);
                }
                indices.SetData<Int16>(localIdx);
            }
        }

        public static void Unload()
        {
            if (effect != null)
            {
                DeviceResetX.Release(ref effect);
                effectCache.UnLoad();
            }
            DeviceResetX.Release(ref texture);
            DeviceResetX.Release(ref verts);
            DeviceResetX.Release(ref indices);
        }

        #endregion Internal
    };
};
