
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Fx;
using Boku.SimWorld;
using Boku.SimWorld.Terra;

namespace Boku.Common.ParticleSystem
{
    /// <summary>
    /// Emitter for rendering floating particles using Perlin noise to control the animation.
    /// While this isn't enforced, the assumption is that there is only one of these and 
    /// that it is attached to the terrain since the particle set needs to be updated every
    /// time the terrain is changed.
    /// </summary>
    public class WaterParticleEmitter : BaseEmitter, INeedsDeviceReset
    {
        private Effect effect = null;
        private VertexBuffer vbuf = null;
        private IndexBuffer ibuf = null;
        private Texture2D texture = null;
        private Texture2D noiseTexture = null;

        private int numSprites = 0;
        private int maxSprites = 10000;         // Maxes out at 10k or so because of using 16 bit indices.


        private Vector2 noiseCenter;
        private float noiseRadius;
        private float theta;
        private float dt;
        private Vector2 baseUV;

        public struct Vertex : IVertexType
        {
            public Vector3 position;
            public Vector2 texCoord;
            public Vector3 state;

            static VertexDeclaration decl = null;
            static VertexElement[] elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),              // position
                new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),    // texture UVs
                new VertexElement(20, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 1),    // rotation, radius, alpha
                // size == 32
            };

            public Vertex(Vector3 pos, Vector2 tex, float rotation, float radius, float alpha)
            {
                position = pos;
                texCoord = tex;
                state = new Vector3(rotation, radius, alpha);
            }   // end of Vertex c'tor

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

        }   // end of Vertex

        private Vector4 color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        private float minRadius = 0.03f;
        private float maxRadius = 0.03f;
        private float alpha = 1.0f;

        private EffectTechnique technique = null;

        private float defaultDensity = 0.2f;

        #region EFFECT_CACHE
        private enum EffectParams
        {
            DiffuseColor,
            WaterColor,
            DiffuseTexture,
            NoiseTexture,
            EyeLocation,
            CameraUp,
            WorldMatrix,
            WorldViewProjMatrix,
            BaseUV,
            Amplitude,
            Sync,
        };
        private EffectCache effectCache = new EffectCache<EffectParams>();
        private EffectParameter Parameter(EffectParams param)
        {
            return effectCache.Parameter((int)param);
        }
        #endregion EFFECT_CACHE

        #region Accessors
        /// <summary>
        /// Color used to attenuate particles when they're rendered.
        /// </summary>
        public Vector4 Color
        {
            get { return color; }
            set { color = value; }
        }
        /// <summary>
        /// Minimum radius for particles.
        /// </summary>
        public float MinRadius
        {
            get { return minRadius; }
            set { maxRadius = value; }
        }
        /// <summary>
        /// Maximum radius for particles.
        /// </summary>
        public float MaxRadius
        {
            get { return maxRadius; }
            set { maxRadius = value; }
        }
        /// <summary>
        /// Alpha value for particles.
        /// </summary>
        public float Alpha
        {
            get { return alpha; }
            set { alpha = value; }
        }
        public bool ListIsEmpty
        {
            get { return particleList.Count == 0; }
        }
        public EffectTechnique Technique
        {
            get { return technique; }
        }

        #endregion

        // c'tor
        public WaterParticleEmitter(ParticleSystemManager manager)
            : base(manager)
        {
            Persistent = true; // Tell manager not to clear me.
            Usage = Use.Never; // Don't auto draw me, wait for explicit render.
            BokuGame.Load(this);
        }   // end of c'tor


        protected void Init(GraphicsDevice device)
        {
            if (BokuSettings.Settings.PreferReach)
                return;

            int numVertices = 4 * maxSprites;
            int numIndices = 6 * maxSprites;

            if (vbuf == null)
            {
                vbuf = new VertexBuffer(device, typeof(Vertex), numVertices, BufferUsage.None);
            }

            if (ibuf == null)
            {
                ibuf = new IndexBuffer(device, IndexElementSize.SixteenBits, numIndices, BufferUsage.None);
            }

            // Load the textures.
            if (texture == null)
            {
                texture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\WaterParticle1");
            }
            if (noiseTexture == null)
            {
                Texture2D src = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\SmoothNoise");

                noiseTexture = new Texture2D(device,
                    src.Width, src.Height,
                    false, // Mipmaps
                    SurfaceFormat.Vector4);

                Color[] tmpColor = new Color[src.Width * src.Height];
                Vector4[] tmpVector4 = new Vector4[src.Width * src.Height];

                src.GetData<Color>(tmpColor);

                int sz = src.Width * src.Height;
                for (int i = 0; i < sz; ++i)
                    tmpVector4[i] = tmpColor[i].ToVector4();

                noiseTexture.SetData<Vector4>(tmpVector4);
                BokuGame.Release(ref src);
            }

            // Load the effect.
            if (effect == null)
            {
                effect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\NoiseParticle2D");

                technique = effect.Techniques[@"TexturedColorPassNormalAlpha"];

                effectCache.Load(effect);
            }

        }   // end of WaterParticleEmitter Init()

        public void InitParticles(Terrain terrain)
        {
            if (BokuSettings.Settings.PreferReach || !Active)
                return;

            if (ibuf == null || vbuf == null)
            {
                Init(BokuGame.bokuGame.GraphicsDevice);
            }

            // Calc approximate volume of water that we need to seed with particles.  Sample along the
            // grid so that this is faster and assume no particles in the top or bottom meter of water.
            float columnArea = terrain.CubeSize * terrain.CubeSize;
            float volume = 0.0f;

            /// Waves go strictly down from the water height (waterHeight is height at crest),
            /// and we also reserve a meter at the bottom that we don't fill. 
            float extraHeight = 1.0f + terrain.XmlWorldData.waveHeight * 2.0f;

            Vector2 pos = Vector2.Zero;
            float waterHeight = 0.0f;
            float terrainHeight = 0.0f;
            VirtualMap.WaterIterator waterIter = terrain.IterateWater();
            for (waterIter.Begin(); waterIter.More(); waterIter.Next())
            {
                waterIter.Current(ref pos, ref waterHeight, ref terrainHeight);
                float depth = waterHeight - terrainHeight;
                if (depth > extraHeight)
                {
                    volume += (depth - extraHeight) * columnArea;
                }
            }

            // Calc how many particles we want.
            numSprites = (int)MathHelper.Min((int)(volume * defaultDensity), maxSprites);
            float actualDensity = numSprites / volume;

            Random rnd = new Random();

            // Create particles.
            if (numSprites > 0)
            {
                int numVertices = 4 * maxSprites;
                int numIndices = 6 * maxSprites;

                Vertex[] localVerts = new Vertex[numVertices];
                ushort[] localIndices = new ushort[numIndices];

                // Pre-fill UV coords for local vertices and set index buffer.
                int vert = 0;
                int index = 0;
                for (int s = 0; s < numSprites; s++)
                {
                    localIndices[index++] = (ushort)(vert + 0);
                    localIndices[index++] = (ushort)(vert + 1);
                    localIndices[index++] = (ushort)(vert + 2);
                    localIndices[index++] = (ushort)(vert + 0);
                    localIndices[index++] = (ushort)(vert + 2);
                    localIndices[index++] = (ushort)(vert + 3);

                    vert += 4;
                }

                // Now scan the height map again figuring out how many sprites to generate in each column of water.
                // For each sprite, generate 4 vertices.
                float fraction = 0.0f;
                int numCreated = 0;
                vert = 0;
                float gridSpacing = terrain.CubeSize;
                for (waterIter.Begin(); waterIter.More(); waterIter.Next())
                {
                    waterIter.Current(ref pos, ref waterHeight, ref terrainHeight);
                    float depth = waterHeight - terrainHeight;
                    if (depth > extraHeight)
                    {
                        float columnHeight = depth - extraHeight;
                        fraction += actualDensity * columnHeight * columnArea;

                        while (fraction > 1.0f && numCreated < numSprites)
                        {
                            // Create a new particle in this column of water.
                            Vector3 position = new Vector3(
                                pos.X + gridSpacing * (float)(rnd.NextDouble() - 0.5), 
                                pos.Y + gridSpacing * (float)(rnd.NextDouble() - 0.5), 
                                terrainHeight + 1.0f + columnHeight * (float)rnd.NextDouble());
                            float radius = minRadius + (float)rnd.NextDouble() * (maxRadius - minRadius);

                            float alpha = 1.0f;
                            localVerts[vert++] = new Vertex(position, new Vector2(0, 0), 0.0f, radius, alpha);
                            localVerts[vert++] = new Vertex(position, new Vector2(0, 1), 0.0f, radius, alpha);
                            localVerts[vert++] = new Vertex(position, new Vector2(1, 1), 0.0f, radius, alpha);
                            localVerts[vert++] = new Vertex(position, new Vector2(1, 0), 0.0f, radius, alpha);

                            fraction -= 1.0f;
                            numCreated++;
                        }
                    }
                }
                //float gridSpacingX = (terrain.Max.X - terrain.Min.X) / heightMap.Size.X;
                //float gridSpacingY = (terrain.Max.Y - terrain.Min.Y) / heightMap.Size.Y;
                //foreach (Tile tile in terrain.Tiles)
                //{
                //    if (tile.Hidden)
                //        continue;

                //    // Calc the offset for the current tile.
                //    int offsetX = tile.X * (Tile.TileSize - 1);
                //    int offsetY = tile.Y * (Tile.TileSize - 1);

                //    for (int i = 1; i < Tile.TileSize - 2; i++)
                //    {
                //        for (int j = 1; j < Tile.TileSize - 2; j++)
                //        {
                //            float depth = heightMap.GetHeight(offsetX + i, offsetY + j);
                //            if (depth < -2.0f)
                //            {
                //                float columHeight = -depth - 2.0f;
                //                fraction += actualDensity * columHeight * columnArea;

                //                while (fraction > 1.0f && numCreated < numSprites)
                //                {
                //                    // Create a new particle in this column of water.
                //                    Vector3 position = tile.Position + new Vector3(gridSpacingX * i + (float)(rnd.NextDouble() - 0.5), gridSpacingY * j + (float)(rnd.NextDouble() - 0.5), -1.0f - columHeight * (float)rnd.NextDouble());
                //                    float radius = minRadius + (float)rnd.NextDouble() * (maxRadius - minRadius);

                //                    float alpha = 1.0f;
                //                    localVerts[vert++] = new Vertex(position, new Vector2(0, 0), 0.0f, radius, alpha);
                //                    localVerts[vert++] = new Vertex(position, new Vector2(0, 1), 0.0f, radius, alpha);
                //                    localVerts[vert++] = new Vertex(position, new Vector2(1, 1), 0.0f, radius, alpha);
                //                    localVerts[vert++] = new Vertex(position, new Vector2(1, 0), 0.0f, radius, alpha);

                //                    fraction -= 1.0f;
                //                    numCreated++;
                //                }
                //            }
                //        }
                //    }
                //}   // end of loop over tiles

                // Copy local versions to the buffers.
                // First ensure that the buffers are not already set on the device.
                BokuGame.bokuGame.GraphicsDevice.SetVertexBuffer(null);
                BokuGame.bokuGame.GraphicsDevice.Indices = null;
                ibuf.SetData<ushort>(localIndices);
                vbuf.SetData<Vertex>(localVerts);
            }

            noiseCenter = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble());
            noiseRadius = 10.0f + (float)rnd.NextDouble();
            theta = MathHelper.TwoPi * (float)rnd.NextDouble();
            dt = 0.004f;

        }   // end of WaterParticleEmitter InitParticles()

        public override void Update()
        {
            if (BokuSettings.Settings.PreferReach)
                return;

            theta += dt * Time.GameTimeFrameSeconds;
            if (theta > MathHelper.TwoPi)
            {
                theta -= MathHelper.TwoPi;
            }

            baseUV = noiseCenter + noiseRadius * new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta));

        }   // end of WaterParticleEmitter Update()

        public override void Render(Camera camera)
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            if (BokuSettings.Settings.PreferReach)
                return;

            if (InGame.inGame.renderEffects != InGame.RenderEffect.Normal)
                return;

            /// Don't render while we're editing the water.
            if (Terrain.WaterBusy)
                return;

            if (active && numSprites > 0)
            {
                effect.CurrentTechnique = technique;

                // Set up common rendering values.
                Parameter(EffectParams.DiffuseColor).SetValue(Color);
                Parameter(EffectParams.WaterColor).SetValue(new Vector4(0.15f, 0.55f, 0.82f, 10.0f));

                // Set up world matrix.
                Matrix worldMatrix = Matrix.Identity;
                Matrix worldViewProjMatrix = worldMatrix * camera.ViewProjectionMatrix;

                Parameter(EffectParams.DiffuseTexture).SetValue(texture);
                Parameter(EffectParams.NoiseTexture).SetValue(noiseTexture);

                Parameter(EffectParams.EyeLocation).SetValue(camera.ActualFrom);
                Parameter(EffectParams.CameraUp).SetValue(camera.ViewUp);

                Parameter(EffectParams.WorldMatrix).SetValue(worldMatrix);
                Parameter(EffectParams.WorldViewProjMatrix).SetValue(worldViewProjMatrix);

                Parameter(EffectParams.BaseUV).SetValue(baseUV);
                Parameter(EffectParams.Amplitude).SetValue(1.0f);
                Parameter(EffectParams.Sync).SetValue(0.01f);

                device.SetVertexBuffer(vbuf);
                device.Indices = ibuf;

                // Render all passes.
                for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
                {
                    EffectPass pass = effect.CurrentTechnique.Passes[i];
                    pass.Apply();
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, numSprites * 4, 0, numSprites * 2);
                }
            }   // end of if active

            // HACK This prevents (I hope) the error where the system still thinks a Vector4
            // texture is live and complains about Linear sampling.
            try
            {
                // Yes this sucks that we have to hard-code these values.  For some
                // reason TextureCollection doesn't have a Count nor is it emmunerable.
                for (int i = 0; i < 15; i++)
                {
                    device.Textures[i] = null;
                }
#if !NETFX_CORE
                for (int i = 0; i < 4; i++)
                {
                    device.VertexTextures[i] = null;
                }
#endif
            }
            catch
            {
            }

        }   // end of WaterParticleEmitter Render()



        public void LoadContent(bool immediate)
        {
            Init(BokuGame.bokuGame.GraphicsDevice);
        }

        public void InitDeviceResources(GraphicsDevice device)
        {
            Init(device);
        }

        public void UnloadContent()
        {
            technique = null;
            BokuGame.Release(ref vbuf);
            BokuGame.Release(ref ibuf);
            BokuGame.Release(ref effect);
            BokuGame.Release(ref texture);
            BokuGame.Release(ref noiseTexture);
        }

        public void DeviceReset(GraphicsDevice device)
        {
            UnloadContent();
            Init(BokuGame.bokuGame.GraphicsDevice);
        }

    }   // end of class WaterParticleEmitter

}   // end of namespace Boku.Common
