// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;

using Boku.Common;
using Boku.Fx;
using Boku.SimWorld.Terra;

namespace Boku.SimWorld.Path
{
    public class RoadStdRenderObj : Road.RenderObj
    {
        #region Members
        protected VertexBuffer vertBuff = null;
        protected IndexBuffer indexBuff = null;

        protected RoadGenerator.RoadVertex[] verts;
        protected Int16[] indices;

        protected int numVerts = 0;
        protected int numIndices = 0;

        protected bool buffersDirty = false;

        protected BoundingSphere sphere;

        protected Vector4 diffuseColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
//        protected Vector4 specularColor = new Vector4(0.8f, 0.8f, 0.8f, 1.0f);
        protected Vector4 specularColor = new Vector4(0.3f, 0.3f, 0.3f, 1.0f);
        protected Vector4 emissiveColor = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
        protected float specularPower = 8.0f;
        protected float shininess = 0.4f;
        protected float wrap = 0.5f;

        protected Texture2D diffuseTex0 = null;
        protected Texture2D diffuseTex1 = null;
        protected Texture2D normalTex0 = null;
        protected Texture2D normalTex1 = null;

        protected Vector4 uvXfm = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

        static protected Effect effect = null;
        #endregion Members

        #region Accessors
        /// <summary>
        /// Bounds for this chunk of geometry.
        /// </summary>
        public BoundingSphere Sphere
        {
            get { return sphere; }
            set { sphere = value; }
        }
        /// <summary>
        /// First diffuse texture.
        /// </summary>
        public Texture2D DiffuseTex0
        {
            get { return diffuseTex0; }
            set { diffuseTex0 = value; }
        }
        /// <summary>
        /// Second diffuse texture.
        /// </summary>
        public Texture2D DiffuseTex1
        {
            get { return diffuseTex1; }
            set { diffuseTex1 = value; }
        }
        /// <summary>
        /// First normal map.
        /// </summary>
        public Texture2D NormalTex0
        {
            get { return normalTex0; }
            set { normalTex0 = value; }
        }
        /// <summary>
        /// Second normal map.
        /// </summary>
        public Texture2D NormalTex1
        {
            get { return normalTex1; }
            set { normalTex1 = value; }
        }
        /// <summary>
        /// Texture2D transform.
        /// </summary>
        public Vector4 UVXfm
        {
            get { return uvXfm; }
            set { uvXfm = value; }
        }
        /// <summary>
        /// Diffuse tint.
        /// </summary>
        public Vector4 DiffuseColor
        {
            get { return diffuseColor; }
            set { diffuseColor = value; }
        }
        /// <summary>
        /// Specular tint.
        /// </summary>
        public Vector4 SpecularColor
        {
            get { return specularColor; }
            set { specularColor = value; }
        }
        /// <summary>
        /// Glow.
        /// </summary>
        public Vector4 EmissiveColor
        {
            get { return emissiveColor; }
            set { emissiveColor = value; }
        }
        /// <summary>
        /// Specular lobe width.
        /// </summary>
        public float SpecularPower
        {
            get { return specularPower; }
            set { specularPower = value; }
        }
        /// <summary>
        /// Shine
        /// </summary>
        public float Shininess
        {
            get { return shininess; }
            set { shininess = value; }
        }
        /// <summary>
        /// Vertex list
        /// </summary>
        public RoadGenerator.RoadVertex[] Verts
        {
            get { return verts; }
            set { DirtyBuffers();  verts = value; }
        }
        /// <summary>
        /// Index list
        /// </summary>
        public Int16[] Indices
        {
            get { return indices; }
            set { DirtyBuffers();  indices = value; }
        }
        /// <summary>
        /// Do we need rebuild
        /// </summary>
        public bool BuffersDirty
        {
            get { return buffersDirty; }
        }
        /// <summary>
        /// Effect to use rendering me.
        /// </summary>
        static public Effect Effect
        {
            get { return effect; }
        }
        #endregion

        #region Public

        #region EffectCaching
        public enum EffectParams
        {
            WorldViewProjMatrix,
            WorldMatrix,
            DiffuseColor,
            SpecularColor,
            EmissiveColor,
            SpecularPower,
            Shininess,
            LightWrap,
            UVXfm,
            DiffuseTexture0,
            DiffuseTexture1,
            NormalTexture0,
            NormalTexture1,
            ShadowTexture,
            ShadowMask,
            ShadowTextureOffsetScale,
            ShadowMaskOffsetScale,
        };
        static EffectCache effectCache = new EffectCache<EffectParams>();
        static public EffectParameter Parameter(EffectParams param)
        {
            return effectCache.Parameter((int)param);
        }
        #endregion EffectCaching

        /// <summary>
        /// Get technique for rendering me.
        /// </summary>
        static public EffectTechnique Technique
        {
            get { return effectCache.Technique(InGame.inGame.renderEffects, true); }
        }

        /// <summary>
        /// Force a rebuild.
        /// </summary>
        public void DirtyBuffers()
        {
            buffersDirty = true;
        }

        /// <summary>
        /// Load shared graphics resources.
        /// </summary>
        /// <param name="graphics"></param>
        static public void LoadContent(bool immediate)
        {
            if (effect == null)
            {
                effect = KoiLibrary.LoadEffect(@"Shaders\Road");
                ShaderGlobals.RegisterEffect("Road", effect);
            }
        }

        static public void InitDeviceResources(GraphicsDevice device)
        {
            effectCache.Load(effect, "");
        }

        /// <summary>
        /// Free shared resources
        /// </summary>
        static public void UnloadContent()
        {
            DeviceResetX.Release(ref effect);
        }

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        static public void DeviceReset(GraphicsDevice device)
        {
        }

        /// <summary>
        /// Free my owned resources.
        /// </summary>
        public void Clear()
        {
            DeviceResetX.Release(ref vertBuff);
            DeviceResetX.Release(ref indexBuff);
        }

        /// <summary>
        /// Render (batched).
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="road"></param>
        public void Render(Camera camera, Road road)
        {
            if ((NumVertices > 0) && (NumIndices > 0))
            {
                UpdateBuffers();

                road.AddBatch(this);
            }
        }

        private bool wireframe = false;

        /// <summary>
        /// Actually render the batch.
        /// </summary>
        /// <param name="road"></param>
        public void RenderBatch(Road road)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            device.Indices = indexBuff;
            device.SetVertexBuffer(vertBuff);

            if(wireframe)
                device.RasterizerState = SharedX.RasterStateWireframe;

            Vector4 color = DiffuseColor * road.Path.RGBColor;
            Parameter(EffectParams.DiffuseColor).SetValue(color);
            Parameter(EffectParams.SpecularColor).SetValue(SpecularColor);
            Parameter(EffectParams.EmissiveColor).SetValue(EmissiveColor);
            Parameter(EffectParams.SpecularPower).SetValue(SpecularPower);
            Parameter(EffectParams.Shininess).SetValue(Shininess);

            Parameter(EffectParams.UVXfm).SetValue(uvXfm);

            Parameter(EffectParams.DiffuseTexture0).SetValue(diffuseTex0);
            Parameter(EffectParams.DiffuseTexture1).SetValue(diffuseTex1);
#if !NETFX_CORE
            Parameter(EffectParams.NormalTexture0).SetValue(normalTex0);
            Parameter(EffectParams.NormalTexture1).SetValue(normalTex1);
#endif

            Texture2D shadowTexture = InGame.inGame.ShadowCamera.ShadowTexture;

            // Sometimes after multiple device resets we can get here with
            // a bad shadow texture.  At this point we can just set it to
            // null rendering a frame without shadows and then everything 
            // will be back to normal next frame.
            if (shadowTexture != null && (shadowTexture.IsDisposed || shadowTexture.GraphicsDevice.IsDisposed))
            {
                if (!shadowTexture.IsDisposed)
                {
                    DeviceResetX.Release(ref shadowTexture);
                }
                shadowTexture = null;
            }

            Texture2D shadowMask = InGame.inGame.ShadowCamera.ShadowMask;
            Parameter(EffectParams.ShadowTexture).SetValue(shadowTexture);
            Parameter(EffectParams.ShadowMask).SetValue(shadowMask);
            Vector4 offsetScale = InGame.inGame.ShadowCamera.OffsetScale;
            Parameter(EffectParams.ShadowTextureOffsetScale).SetValue(offsetScale);
            offsetScale = InGame.inGame.ShadowCamera.MaskOffsetScale;
            Parameter(EffectParams.ShadowMaskOffsetScale).SetValue(offsetScale);

            effect.CurrentTechnique.Passes[0].Apply();

            device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                                            0,
                                            0,
                                            NumVertices,
                                            0,
                                            NumTriangles);

            if (wireframe)
                device.RasterizerState = RasterizerState.CullCounterClockwise;
        }

        public void Finish(Road.Section section)
        {
            AABB box = AABB.EmptyBox();

            bool stretchUp = section.Road.Generator.StretchUp;
            int cnt = Verts.Length;
            for(int i = 0; i < cnt; ++i)
            {
                if (stretchUp)
                {
                    float minHeight = section.Road.Generator.MinHeight;
                    float maxHeight = section.Road.Generator.MaxHeight;
                    float t = (Verts[i].pos.Z - minHeight) / (maxHeight - minHeight);
                    float baseHeight = section.BaseHeight(Verts[i].pos);
                    float terrHeight = Terrain.GetTerrainHeightFlat(Verts[i].pos);
                    Verts[i].pos.Z = terrHeight + t * (Verts[i].pos.Z + baseHeight - terrHeight);
                }
                else
                {
                    if (Verts[i].pos.Z >= 0.0f)
                    {
                        Verts[i].pos.Z += section.BaseHeight(Verts[i].pos);
                    }
                    else
                    {
                        Verts[i].pos.Z = Terrain.GetTerrainHeightFlat(Verts[i].pos);
                    }
                }
                box.Union(Verts[i].pos);
            }
            sphere = box.MakeSphere();
        }

        public void Finish(Road.Intersection isect)
        {
            AABB box = AABB.EmptyBox();

            int cnt = Verts.Length;
            bool stretchUp = isect.Road.Generator.StretchUpEnd;
            for (int i = 0; i < cnt; ++i)
            {
                if (stretchUp)
                {
                    float minHeight = isect.Road.Generator.MinHeight;
                    float maxHeight = isect.Road.Generator.MaxHeight;
                    float t = (Verts[i].pos.Z - minHeight) / (maxHeight - minHeight);
                    float baseHeight = isect.BaseHeight(Verts[i].pos);
                    float terrHeight = Terrain.GetTerrainHeightFlat(Verts[i].pos);
                    Verts[i].pos.Z = terrHeight + t * (Verts[i].pos.Z + baseHeight - terrHeight);
                }
                else
                {
                    if (Verts[i].pos.Z >= 0.0f)
                    {
                        Verts[i].pos.Z += isect.BaseHeight(Verts[i].pos);
                    }
                    else
                    {
                        Verts[i].pos.Z = Terrain.GetTerrainHeightFlat(Verts[i].pos);
                    }
                }
                box.Union(Verts[i].pos);
            }
            sphere = box.MakeSphere();
        }

        #endregion Public

        #region Internal

        protected bool MakeVertexBuffer()
        {
            DeviceResetX.Release(ref vertBuff);
            int numVerts = NumVertices;
            if (numVerts > 0)
            {
                GraphicsDevice device = KoiLibrary.GraphicsDevice;

                vertBuff = new VertexBuffer(device, 
                    typeof(RoadGenerator.RoadVertex), 
                    numVerts, 
                    BufferUsage.WriteOnly);

                vertBuff.SetData<RoadGenerator.RoadVertex>(Verts);
            }

            return numVerts > 0;
        }
        protected bool MakeIndexBuffer()
        {
            DeviceResetX.Release(ref indexBuff);
            int numIndices = NumIndices;
            
            if (numIndices > 0)
            {
                GraphicsDevice device = KoiLibrary.GraphicsDevice;

                indexBuff = new IndexBuffer(device, IndexElementSize.SixteenBits, numIndices, BufferUsage.WriteOnly);

                indexBuff.SetData<Int16>(Indices);
            }

            return numIndices > 0;
        }
        protected int NumVertices
        {
            get { return Verts != null ? Verts.Length : 0; }
        }

        protected int NumIndices
        {
            get { return Indices != null ? Indices.Length : 0; }
        }

        protected int NumTriangles
        {
            get { return NumIndices / 3; }
        }

        protected void UpdateBuffers()
        {
            if (BuffersDirty)
            {
                MakeVertexBuffer();
                MakeIndexBuffer();
                buffersDirty = false;
            }
        }

        #endregion Internal
    }
}
