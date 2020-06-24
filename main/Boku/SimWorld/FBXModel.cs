
//#define KEYBOARD_LOD_HACK

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;

using Boku.Base;
using Boku.Fx;
using Boku.Common;
using TileProcessor;
using Boku.SimWorld.Collision;
using Boku.Common.Xml;

using Boku.Animatics;

namespace Boku.SimWorld
{
    /// <summary>
    /// Low level class for containing a model loaded from an FBX file.
    /// In general this will be what SROs are derived from.
    /// </summary>
    public class FBXModel : ArbitraryComparable, INeedsDeviceReset
    {
        #region Members
        /// A nested list of lists.  The outer list contains
        /// an inner list for each Mesh.  The inner list contains
        /// a PartInfo for each part.
        private List<List<PartInfo>>[] infoLists = null;
        private List<Model> models = new List<Model>();
        private Effect effect = null;

        private Face face = null;   // Face texture for this guy.
        private AnimatorList animators = null;

        private string resourceName = null;

        private List<Matrix[]> restPalettes = new List<Matrix[]>();

        private BoundingBox boundingBox;
        private BoundingSphere boundingSphere;

        private Vector4 diffuseColor = Vector4.One;   // For tinting.
        private float shininess = 1.0f;
        private Vector3 glowEmissiveColor = Vector3.Zero;
        private string techniqueExt = "";

        private bool neverDisplayCollisions = false;

        private static bool batching = false;
        private static int nextScratchPack = 0;
        private static List<RenderPack> scratchPacks = new List<RenderPack>();
        public enum LockLOD
        {
            kAny,
            kLow,
            kHigh
        }
        private static LockLOD lockLowLOD = LockLOD.kAny;

        private List<CollisionPrimitive> collisionPrims = null;

        private XmlGameActor xmlActor = null;

        #region EffectCache
        enum EffectParams
        {
            LocalToModel, // transform from local space to palette space.
            WorldViewProjMatrix, // transform from root space to NDC
            WorldMatrix, // transform from root space to world space
            WorldMatrixInverseTranspose,
            DiffuseTexture,
            DiffuseColor,
            Shininess,
            SpecularColor,
            EmissiveColor,
            SpecularPower,
            LightWrap,
            GlowEmissiveColor,

            MatrixPalette, // transform from palette space to root space
            RestPalette, // alternate (subdued) version of MatrixPalette, for blending
        };
        private EffectCache effectCache = new EffectCache<EffectParams>();
        private EffectParameter Parameter(EffectParams param)
        {
            return effectCache.Parameter((int)param);
        }
        #endregion EffectCache
        #endregion Members

        #region Accessors
        public List<Matrix[]> RestPalettes
        {
            get { return restPalettes; }
            set { restPalettes = value; }
        }
        public static bool Batching
        {
            get { return batching; }
        }
        public static bool PushBatching(bool on)
        {
            bool was = batching;
            batching = on;
            return was;
        }
        public static void PopBatching(bool on) { batching = on; }
        public Effect Effect
        {
            get { return effect; }
        }
        public int NumLODs
        {
            get { return models.Count; }
        }
        public BoundingBox BoundingBox
        {
            get { return boundingBox; }
        }
        public BoundingSphere BoundingSphere
        {
            get { return boundingSphere; }
        }

        /// <summary>
        /// List of collision prims for this model.  These
        /// are generally used by StaticProps.  For other
        /// types of characters will will generally be null.
        /// </summary>
        public List<CollisionPrimitive> CollisionPrims
        {
            get { return collisionPrims; }
        }

        /// <summary>
        /// Diffuse color for tinting non-textured, non-foliage parts.
        /// </summary>
        public Vector4 DiffuseColor
        {
            get { return diffuseColor; }
            set { diffuseColor = value; }
        }
        public Vector4 RenderColor
        {
            get { return DiffuseColor; }
            set { DiffuseColor = value; }
        }
        public float Shininess
        {
            get { return shininess; }
            set { shininess = value; }
        }
        public string TechniqueExt
        {
            get { return techniqueExt; }
            set { techniqueExt = value; }
        }
        public Face Face
        {
            set { face = value; }
            get { return face; }
        }
        /// <summary>
        /// How much "diffuse light" the glow contributes.
        /// </summary>
        public Vector3 GlowEmissiveColor
        {
            set { glowEmissiveColor = value; }
            get { return glowEmissiveColor; }
        }
        /// <summary>
        /// The Animator is a temp cache, where the owning object (which also owns
        /// the real animator) can stash the animator to use when rendering it's 
        /// instance of the FBXModel. Remember this FBXModel is a shared resource.
        /// </summary>
        public AnimatorList Animators
        {
            get { return animators; }
            set { animators = value; }
        }
        public EffectTechnique Technique(InGame.RenderEffect pass, bool textured)
        {
            return effectCache.Technique(pass, textured);
        }
        public bool DisplayCollisions
        {
            get { return !neverDisplayCollisions && InGame.DebugDisplayCollisions; }
        }
        public bool NeverDisplayCollisions
        {
            get { return neverDisplayCollisions; }
            set { neverDisplayCollisions = value; }
        }
        public static LockLOD LockLowLOD
        {
            get { return lockLowLOD; }
            set { lockLowLOD = value; }
        }
        public XmlGameActor XmlActor
        {
            get { return xmlActor; }
            set { xmlActor = value; }
        }
        #endregion Accessors

        #region Public
        public delegate void Setup(FBXModel model);

        public Setup PreRender;


        public FBXModel(string resourceName)
        {
            this.resourceName = resourceName;
        }   // end of FBXModel c'tor

        public virtual Vector4 PartColor(PartInfo partInfo, Vector4 tint)
        {
            if ((partInfo.DiffuseColor.X == 1.0f) 
                &&(partInfo.DiffuseColor.Y == 1.0f)
                &&(partInfo.DiffuseColor.Z == 1.0f))
            {
                return tint;
            }
            return partInfo.DiffuseColor;
        }
        public virtual Texture2D PartTexture(PartInfo partInfo)
        {
            return partInfo.DiffuseTexture;
        }

        public static EffectTechnique FindTechnique(Effect effect, string name, string ext)
        {
            EffectTechnique tech = effect.Techniques[name + ext];
            if (tech == null)
            {
                tech = effect.Techniques[name];
            }
            return tech;
        }

        private void CopyRestPalette(AnimationInstance anim, int lod)
        {
            Matrix[] animPalette = anim.Palette;
            int boneCount = animPalette.Length;

            Matrix[] palette = new Matrix[boneCount];

            for (int i = 0; i < boneCount; ++i)
            {
                palette[i] = animPalette[i];
            }
            if (lod < restPalettes.Count)
            {
                restPalettes[lod] = palette;
            }
            else
            {
                restPalettes.Add(palette);
            }
        }

        public virtual AnimationInstance MakeAnimator()
        {
            return MakeAnimator(0);
        }
        public virtual AnimationInstance MakeAnimator(int which)
        {
            AnimationInstance result = null;

            if (models[which] != null && models[which].Tag != null)
            {
                result = AnimationInstance.TryMake(models[which]);
                if (result != null)
                {
                    Debug.Assert(which <= restPalettes.Count, "Must copy restpalette's in order [0..nLod]");
                    CopyRestPalette(result, which);
                }
            }
            return result;
        }

        private int CurrentLOD(Camera camera, Matrix rootToWorld)
        {
            Debug.Assert(NumLODs > 0);
            switch (lockLowLOD)
            {
                case LockLOD.kLow:
                    return 0;

                case LockLOD.kHigh:
                    return NumLODs - 1;

                case LockLOD.kAny:
                    {

                        float dist = Vector3.Distance(rootToWorld.Translation, camera.ActualFrom);

#if KEYBOARD_LOD_HACK
                        if (KeyboardInputX.IsPressed(Keys.RightShift))
                            dist = 0;
                        if (KeyboardInputX.IsPressed(Keys.RightControl))
                            dist = Single.MaxValue;
                        if (KeyboardInputX.IsPressed(Keys.F8))
                            XmlGameActor.RebindSurfacesAllActors();
#endif // KEYBOARD_LOD_HACK

                        float kLODSwitch = 30.0f;

                        int lod = dist < kLODSwitch ? 1 : 0;
                        return Math.Min(lod, NumLODs - 1);
                    }
                default:
                    break;
            }
            Debug.Assert(false, "Unknown lod lock");
            return 0;
        }

        /// <summary>
        /// Default render method for mesh models.  While this may work for many models
        /// you will likely find that you need to override this to create one specific
        /// for each model type.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="rootToWorld"></param>
        /// <param name="listPartsReplacement">Used to replace the existing InfoList created from the file.  If null, then the default InfoList is used.</param>
        /// <param name="renderPass"></param>
        public virtual void Render(Camera camera, ref Matrix rootToWorld, List<List<PartInfo>> listPartsReplacement)
        {
            if (Batching)
            {
                CollectBatch(camera, ref rootToWorld, listPartsReplacement);
                return;
            }
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            int lod = CurrentLOD(camera, rootToWorld);
            List<List<PartInfo>> meshInfoList = listPartsReplacement == null ? infoLists[lod] : listPartsReplacement;

            if (PreRender != null)
                PreRender(this);

            Model model = models[lod];
            AnimationInstance animator = animators[lod];

            for (int i = 0; i < model.Meshes.Count; i++)
            {
                ModelMesh mesh = model.Meshes[i];
                List<PartInfo> list = meshInfoList[i];

                if (animator != null && animator.Palette != null)
                {
                    Parameter(EffectParams.MatrixPalette).SetValue(animator.Palette);
                    Matrix[] restPalette = RestPalettes[lod];
                    Parameter(EffectParams.RestPalette).SetValue(restPalette);
                }

                BokuGame.bokuGame.shaderGlobals.SetUpWind(rootToWorld);
                Parameter(EffectParams.WorldMatrix).SetValue(rootToWorld);
                Parameter(EffectParams.WorldMatrixInverseTranspose).SetValue(Matrix.Transpose(Matrix.Invert(rootToWorld)));
                Matrix rootToProjMatrix = rootToWorld * camera.ViewProjectionMatrix;
                Parameter(EffectParams.WorldViewProjMatrix).SetValue(rootToProjMatrix);
                Parameter(EffectParams.GlowEmissiveColor).SetValue(GlowEmissiveColor);

                for (int j = 0; j < mesh.MeshParts.Count; j++)
                {
                    ModelMeshPart part = mesh.MeshParts[j];

                    device.Indices = part.IndexBuffer;
                    // Argh.  Note we don't set the Vertex Offset here but we do use it 
                    // in the DrawPrims call.  If you set it in both places then the 
                    // values get summed and the part fails to render.
                    device.SetVertexBuffer(part.VertexBuffer /* , part.VertexOffset */);

                    // Apply part info params.
                    PartInfo partInfo = list[j];

                    Matrix localToModel = LocalToModel(mesh);
                    if (partInfo.Collision)
                    {
                        localToModel = animator != null
                            ? animator.LocalToWorld(mesh.ParentBone.Index)
                            : mesh.ParentBone.Transform;
                    }
                    Parameter(EffectParams.LocalToModel).SetValue(localToModel);

                    if (DisplayCollisions || partInfo.Render)
                    {
                        if (partInfo.SurfaceSet != null)
                        {
                            RenderSurface(part, partInfo, lod);
                        }
                        else
                        {

                            if (InGame.inGame.renderEffects != InGame.RenderEffect.Normal)
                            {
                                Effect.CurrentTechnique = effectCache.Technique(InGame.inGame.renderEffects, false);

                                // Hack to handle ghosting of lights and wisps since they don't have textures.
                                if (InGame.inGame.renderEffects == InGame.RenderEffect.GhostPass)
                                {
                                    VertexElement[] elements = part.VertexBuffer.VertexDeclaration.GetVertexElements();
                                    if (elements.Length == 2)
                                    {
                                        if (Effect.CurrentTechnique.Name.EndsWith("SM3"))
                                        {
                                            Effect.CurrentTechnique = Effect.Techniques["GhostPassNonTextured_SM3"];
                                        }
                                        else
                                        {
                                            Effect.CurrentTechnique = Effect.Techniques["GhostPassNonTextured_SM2"];
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Texture2D diffuseTexture = PartTexture(partInfo);
                                Parameter(EffectParams.DiffuseTexture).SetValue(diffuseTexture);
                                Effect.CurrentTechnique = effectCache.Technique(InGame.inGame.renderEffects, diffuseTexture != null);
                            }

                            Vector4 diffuseColor = PartColor(partInfo, DiffuseColor);

                            Parameter(EffectParams.DiffuseColor).SetValue(new Vector4(diffuseColor.X, diffuseColor.Y, diffuseColor.Z, 1.0f));
                            Parameter(EffectParams.Shininess).SetValue(Shininess * diffuseColor.W);

                            Parameter(EffectParams.SpecularColor).SetValue(partInfo.SpecularColor);
                            Parameter(EffectParams.EmissiveColor).SetValue(partInfo.EmissiveColor);
                            Parameter(EffectParams.SpecularPower).SetValue(partInfo.SpecularPower);

                            // HACKHACK  XNA 4 is much pickier than XNA 3.1 about having valid data for
                            // all vertex shader inputs.  So, if we find a case where we don't have the 
                            // correct inputs, fall back to something simpler.
                            if (Effect.CurrentTechnique.Name.Contains("WithWind"))
                            {
                                VertexElement[] elements = part.VertexBuffer.VertexDeclaration.GetVertexElements();
                                if (elements.Length < 4)
                                {
                                    int index = Effect.CurrentTechnique.Name.IndexOf("WithWind");
                                    string technique = Effect.CurrentTechnique.Name.Remove(index, 8);
                                    Effect.CurrentTechnique = Effect.Techniques[technique];
                                }
                            }

                            // HACKHACK More working around mismatched vertex decls vs shaders.
                            // In this case "WithSkinning" techniques are being used but the vertex decls
                            // don't have any BlendWeights.
                            {
                                string curTechnique = Effect.CurrentTechnique.Name;
                                if (curTechnique.Contains("WithSkinning"))
                                {
                                    VertexElement[] elements = part.VertexBuffer.VertexDeclaration.GetVertexElements();

                                    bool hasBlendWeight = false;
                                    for (int e = 0; e < elements.Length; e++)
                                    {
                                        if (elements[e].VertexElementUsage == VertexElementUsage.BlendWeight)
                                        {
                                            hasBlendWeight = true;
                                            break;
                                        }
                                    }
                                    if (!hasBlendWeight)
                                    {
                                        curTechnique = curTechnique.Replace("WithSkinning", "");
                                        Effect.CurrentTechnique = Effect.Techniques[curTechnique];
                                    }
                                }
                            }

                            try
                            {
                                for (int indexEffectPass = 0; indexEffectPass < Effect.CurrentTechnique.Passes.Count; indexEffectPass++)
                                {
                                    EffectPass pass = Effect.CurrentTechnique.Passes[indexEffectPass];
                                    pass.Apply();
                                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                                                                    part.VertexOffset,
                                                                    0,
                                                                    part.NumVertices,
                                                                    part.StartIndex,
                                                                    part.PrimitiveCount);

                                }   // end loop over each pass.
                            }
                            catch 
                            { 
                                // TODO (scoy) Figure out why we get here.
                            }
                        }
                    }

                }   // end loop over each part.

            }   // end loop over each mesh.

        }   // end of FBXModel Render()

        private void RenderSurface(ModelMeshPart part, PartInfo partInfo, int lod)
        {
            SurfaceSet set = partInfo.SurfaceSet;
            Effect.CurrentTechnique = set.Technique(InGame.inGame.renderEffects);
            set.Setup(DiffuseColor, lod);

            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            for (int indexEffectPass = 0; indexEffectPass < Effect.CurrentTechnique.Passes.Count; indexEffectPass++)
            {
                EffectPass pass = Effect.CurrentTechnique.Passes[indexEffectPass];
                pass.Apply();
                try
                {
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                                                    part.VertexOffset,
                                                    0,
                                                    part.NumVertices,
                                                    part.StartIndex,
                                                    part.PrimitiveCount);
                }
                catch (Exception)
                {
                }

            }   // end loop over each pass.

        }   // end of FBXModel Render()


        #endregion Public

        #region Internal

        private FBXModel()
        {
        }

        private void BindSurfaces()
        {
            for(int i = 0; i < models.Count; ++i)
            {
                BindSurfaces(i);
            }
        }
        private void BindSurfaces(int which)
        {
            Model model = models[which];
            if ((model != null) && 
                (xmlActor != null) 
                && (xmlActor.SurfaceSets != null) 
                && (xmlActor.SurfaceSets.Count > 0))
            {
                for (int i = 0; i < model.Meshes.Count; ++i)
                {
                    ModelMesh mesh = model.Meshes[i];
                    List<PartInfo> list = infoLists[which][i];
                    for (int j = 0; j < mesh.MeshParts.Count; j++)
                    {
                        ModelMeshPart part = mesh.MeshParts[j];

                        list[j].SurfaceSet = xmlActor.FindSet(part.Tag as string);
                        if (list[j].SurfaceSet != null)
                        {
                            list[j].SurfaceSet.Bind(effect);
                        }
                    }
                }
            }
        }
        private void UnBindSurfaces()
        {
            for(int i = 0; i < models.Count; ++i)
            {
                UnBindSurfaces(i);
            }
        }
        private void UnBindSurfaces(int which)
        {
            if (models[which] != null)
            {
                for (int i = 0; i < models[which].Meshes.Count; i++)
                {
                    List<PartInfo> list = infoLists[which][i];
                    for (int j = 0; j < list.Count; j++)
                    {
                        if (list[j].SurfaceSet != null)
                            list[j].SurfaceSet.UnBind();
                        list[j].SurfaceSet = null;
                    }
                }
            }
        }

        public class RenderPack
        {
            /// <summary>
            /// I don't think technique belongs here. I'm not sure we'll get back
            /// as much from getting, for example, all the users of the foliage technique
            /// packed together, as we'll spend on string compares. But we'll see.
            /// </summary>
            public InGame.RenderEffect technique;
            public Texture2D diffuseTexture;
            /// <summary>
            /// In alphabetic sort order
            /// Same model implies same effect
            /// </summary>
            public FBXModel model;
            /// <summary>
            /// Change in lod is essentially a change in model (under the hood).
            /// </summary>
            public int lod;
            /// <summary>
            /// Change mesh is change index buffer
            /// </summary>
            public int meshIdx;
            /// <summary>
            /// change part is change vertex buffer
            /// </summary>
            public int partIdx;

            /// <summary>
            /// These three are sorted simply by bool. We want all the 
            /// things that don't have these adjacent, the things that do
            /// have any of these set don't really matter in order
            /// </summary>
            public List<List<PartInfo>> meshInfoList;
            public AnimationInstance animator;
            public Setup PreRender;

            /// <summary>
            /// We want these last, because we hope to eventually batch together
            /// some or all of the instances differing only in color and transform.
            /// So we will ignore them in the sort.
            /// </summary>
            public Matrix rootToWorld;
            public Vector4 diffuseColor;
            public Vector3 glowEmissiveColor;

            /// <summary>
            /// Save off the camera in case someone moves it before the
            /// queue is flushed.
            /// </summary>
            public Camera camera;

            public SurfaceSet SurfaceSet
            {
                get { return meshInfoList[meshIdx][partIdx].SurfaceSet; }
            }
            public EffectTechnique Technique
            {
                get 
                {
                    SurfaceSet set = SurfaceSet;
                    if (set != null)
                    {
                        return set.Technique(technique);
                    }
                    return model.Technique(technique, diffuseTexture != null); 
                }
            }
        }

        protected static void StartTimer(PerfTimer t)
        {
            //t.Start();
        }
        protected static void StopTimer(PerfTimer t)
        {
            //t.Stop();
        }

        static PerfTimer sortTime = new PerfTimer("sort", 5.0f);
        static PerfTimer collTime = new PerfTimer("coll", 5.0f);
        static PerfTimer rendTime = new PerfTimer("rend", 5.0f);
        static PerfTimer parmTime = new PerfTimer("parm", 5.0f);
        static PerfTimer loopTime = new PerfTimer("loop", 5.0f);


        private static System.Comparison<RenderPack> ComparePacksThing = new Comparison<RenderPack>(ComparePacks);
        public static int ComparePacks(RenderPack lhs, RenderPack rhs)
        {

            if (lhs.technique < rhs.technique)
                return -1;
            if (lhs.technique > rhs.technique)
                return 1;

            if ((lhs.animator == null) && (rhs.animator != null))
                return -1;
            if ((lhs.animator != null) && (rhs.animator == null))
                return 1;
            if (lhs.animator != null)
            {
                Debug.Assert(rhs.animator != null, "By now either both should be or neither");
                if (lhs.animator.id < rhs.animator.id)
                    return -1;
                if (lhs.animator.id > rhs.animator.id)
                    return 1;
            }

            if ((lhs.diffuseTexture == null) && (rhs.diffuseTexture != null))
                return -1;
            if ((lhs.diffuseTexture != null) && (rhs.diffuseTexture == null))
                return 1;

            int cmpMod = lhs.model.CompareTo(rhs.model);
            if (cmpMod < 0)
                return -1;
            if (cmpMod > 0)
                return 1;

            if (lhs.lod < rhs.lod)
                return -1;
            if (lhs.lod > rhs.lod)
                return 1;

            if (lhs.meshIdx < rhs.meshIdx)
                return -1;
            if (lhs.meshIdx > rhs.meshIdx)
                return 1;

            if (lhs.partIdx < rhs.partIdx)
                return -1;
            if (lhs.partIdx > rhs.partIdx)
                return 1;

            if ((lhs.meshInfoList == null) && (rhs.meshInfoList != null))
                return -1;
            if ((lhs.meshInfoList != null) && (rhs.meshInfoList == null))
                return 1;

            if ((lhs.PreRender == null) && (rhs.PreRender != null))
                return -1;
            if ((lhs.PreRender != null) && (rhs.PreRender == null))
                return 1;

            return 0;
        }

        static bool renderWire = false;
        public static void RenderBatches(List<RenderPack> renderBatch)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            if (renderWire)
                device.RasterizerState = SharedX.RasterStateWireframe;
            //StartTimer(sortTime);
            /// sort the list
            /// 
            renderBatch.Sort(ComparePacksThing);

            //StopTimer(sortTime);

            //StartTimer(loopTime);

            int batchBegin = 0;
            while (batchBegin < renderBatch.Count)
            {
                // If we change effect or technique, 
                //      finish up any old effect/technique
                //      start up the new effect/technique
                RenderPack basePack = renderBatch[batchBegin];
                Effect effect = basePack.model.Effect;
                effect.CurrentTechnique = basePack.Technique;

                int batchEnd;

                for (batchEnd = batchBegin;
                    (batchEnd < renderBatch.Count)
                        && (effect.CurrentTechnique == renderBatch[batchEnd].Technique);
                    ++batchEnd)
                {
                }

                Setup lastPreRender = null;
                FBXModel lastModel = null;
                AnimationInstance lastAnim = null;
                int lastMeshIdx = -1;
                int lastPartIdx = -1;
                int lastLod = -1;

                for (int iPass = 0; iPass < effect.CurrentTechnique.Passes.Count; ++iPass)
                {
                    EffectPass pass = effect.CurrentTechnique.Passes[iPass];

                    pass.Apply();

                    for (int iBatch = batchBegin; iBatch < batchEnd; ++iBatch)
                    {
                        RenderPack pack = renderBatch[iBatch];

                        ModelMesh mesh = (ModelMesh)pack.model.models[pack.lod].Meshes[pack.meshIdx];
                        List<PartInfo> list = pack.meshInfoList[pack.meshIdx];
                        ModelMeshPart part = mesh.MeshParts[pack.partIdx];
                        PartInfo partInfo = list[pack.partIdx];

                        if ((pack.PreRender != null) && (pack.PreRender != lastPreRender))
                        {
                            pack.PreRender(pack.model);
                            lastPreRender = pack.PreRender;
                        }


                        // If the model changed
                        //      reset mesh 
                        //      reset part
                        // if the mesh changed
                        //      set index buffer
                        //      reset part
                        bool setVtx = false;
                        if ((iBatch == batchBegin) 
                            || (lastModel != pack.model) 
                            || (lastLod != pack.lod)
                            || (lastMeshIdx != pack.meshIdx))
                        {
                            lastModel = pack.model;
                            lastLod = pack.lod;
                            lastMeshIdx = pack.meshIdx;

                            setVtx = true;
                        }

                        // if the part changed
                        //      reset verts
                        if (setVtx || (lastPartIdx != pack.partIdx))
                        {
                            lastPartIdx = pack.partIdx;

                            device.Indices = part.IndexBuffer;
                            // Argh.  Note we don't set the Vertex Offset here but we do use it 
                            // in the DrawPrims call.  If you set it in both places then the 
                            // values get summed and the part fails to render.
                            device.SetVertexBuffer(part.VertexBuffer /* , part.VertexOffset */);
                        }


                        if (pack.animator != null && pack.animator.Palette != null)
                        {
                            if (pack.animator != lastAnim)
                            {
                                EffectCache effectCache = pack.model.effectCache;
                                effectCache.Parameter((int)EffectParams.MatrixPalette).SetValue(
                                    pack.animator.Palette);
                                if (pack.lod > 0)
                                {
                                    effectCache.Parameter((int)EffectParams.RestPalette).SetValue(
                                        pack.model.restPalettes[pack.lod]);
                                }

                                lastAnim = pack.animator;
                            }
                        }


                        // Render
                        pack.model.RenderBatch(pack);

                    }

                }

                batchBegin = batchEnd;
            }
            //StopTimer(loopTime);

            nextScratchPack = 0;
            renderBatch.Clear();

            // TODO (scoy) Get rid of this???
            if (renderWire)
                device.RasterizerState = RasterizerState.CullCounterClockwise;
        }

        private static int extraScratch = 50;
        protected static RenderPack NextPack()
        {
            if (nextScratchPack >= scratchPacks.Count)
            {
                scratchPacks.Capacity += extraScratch;
                for (int i = 0; i < extraScratch; ++i)
                {
                    RenderPack pack = new RenderPack();
                    scratchPacks.Add(pack);
                }
                Debug.Assert(nextScratchPack < scratchPacks.Count);
            }
            return scratchPacks[nextScratchPack++];
        }

        protected virtual void CollectBatch(Camera camera, ref Matrix rootToWorld, List<List<PartInfo>> listPartsReplacement)
        {
            StartTimer(collTime);
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            int lod = CurrentLOD(camera, rootToWorld);
            List<List<PartInfo>> meshInfoList = listPartsReplacement == null ? infoLists[lod] : listPartsReplacement;

            Model model = models[lod];
            for (int i = 0; i < model.Meshes.Count; i++)
            {
                ModelMesh mesh = model.Meshes[i];
                List<PartInfo> list = meshInfoList[i];

                for (int j = 0; j < mesh.MeshParts.Count; j++)
                {
                    ModelMeshPart part = mesh.MeshParts[j];
                    PartInfo partInfo = list[j];

                    if (DisplayCollisions || partInfo.Render)
                    {

                        RenderPack pack = NextPack();
                        pack.model = this;
                        pack.technique = InGame.inGame.renderEffects;
                        pack.diffuseTexture = PartTexture(partInfo);
                        pack.lod = lod;
                        pack.meshIdx = i;
                        pack.partIdx = j;
                        pack.meshInfoList = meshInfoList;
                        pack.animator = Animators != null ? Animators[lod] : null;
                        pack.PreRender = PreRender;
                        pack.rootToWorld = rootToWorld;
                        pack.diffuseColor = DiffuseColor;
                        pack.glowEmissiveColor = GlowEmissiveColor;
                        pack.camera = camera;

                        InGame.inGame.AddBatch(pack);
                    }

                }   // end loop over each part.

            }   // end loop over each mesh.

            StopTimer(collTime);
        }   // end of FBXModel CollectBatch()

        protected virtual void RenderSurfaceBatch(RenderPack pack, SurfaceSet set)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;
            Camera camera = pack.camera;

            List<List<PartInfo>> meshInfoList = pack.meshInfoList;

            //StartTimer(parmTime);

            ModelMesh mesh = models[pack.lod].Meshes[pack.meshIdx];
            List<PartInfo> list = meshInfoList[pack.meshIdx];

            // Apply part info params.
            PartInfo partInfo = list[pack.partIdx];

            /// Could provide a convenience func FBXModel.Parameter(EffectParams which)
            /// that just passes through { return effectCache.Parameter((int)which); }
            /// but I'm worried that the overhead of a non-inlined function call might
            /// actually add up to something in this case.
            Matrix localToModel = LocalToModel(mesh);
            if (partInfo.Collision)
            {
                AnimationInstance animator = pack.animator;
                localToModel = animator != null
                    ? animator.LocalToWorld(mesh.ParentBone.Index)
                    : mesh.ParentBone.Transform;
            }
            Parameter(EffectParams.LocalToModel).SetValue(localToModel);

            BokuGame.bokuGame.shaderGlobals.SetUpWind(pack.rootToWorld);
            Parameter(EffectParams.WorldMatrix).SetValue(pack.rootToWorld);
            Parameter(EffectParams.WorldMatrixInverseTranspose).SetValue(Matrix.Transpose(Matrix.Invert(pack.rootToWorld)));

            Matrix rootToProjMatrix = pack.rootToWorld * camera.ViewProjectionMatrix;
            Parameter(EffectParams.WorldViewProjMatrix).SetValue(rootToProjMatrix);


            ModelMeshPart part = mesh.MeshParts[pack.partIdx];

            Texture2D diffuseTexture = PartTexture(partInfo);
            Parameter(EffectParams.DiffuseTexture).SetValue(diffuseTexture);
            Parameter(EffectParams.GlowEmissiveColor).SetValue(pack.glowEmissiveColor);

            // HACKHACK  XNA 4 is much pickier than XNA 3.1 about having valid data for
            // all vertex shader inputs.  So, if we find a case where we don't have the 
            // correct inputs, fall back to something simpler.
            if (Effect.CurrentTechnique.Name.Contains("WithWind"))
            {
                VertexElement[] elements = part.VertexBuffer.VertexDeclaration.GetVertexElements();
                if (elements.Length < 4)
                {
                    int index = Effect.CurrentTechnique.Name.IndexOf("WithWind");
                    string technique = Effect.CurrentTechnique.Name.Remove(index, 8);
                    Effect.CurrentTechnique = Effect.Techniques[technique];
                }
            }

            set.Setup(pack.diffuseColor, pack.lod);
            //StopTimer(parmTime);

            //StartTimer(rendTime);
            Effect.CurrentTechnique.Passes[0].Apply();

            int primCount = part.PrimitiveCount;
            //primCount = Math.Min(1, primCount);

            try
            {
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                                                part.VertexOffset,
                                                0,
                                                part.NumVertices,
                                                part.StartIndex,
                                                primCount);
            }
            catch(Exception)
            {
            }
            //StopTimer(rendTime);

        }
        protected virtual void RenderBatch(RenderPack pack)
        {
            SurfaceSet set = pack.SurfaceSet;
            if (set != null)
            {
                RenderSurfaceBatch(pack, set);
                return;
            }

            // Wisps don't have shadows...
            // TODO (scoy) This should probably be handled elsewhere.  Just not sure where.
            if (Effect.CurrentTechnique.Name.Equals("ShadowPass") && resourceName.Contains("wisp"))
            {
                return;
            }

            GraphicsDevice device = KoiLibrary.GraphicsDevice;
            Camera camera = pack.camera;

            List<List<PartInfo>> meshInfoList = pack.meshInfoList;

            //StartTimer(parmTime);

            ModelMesh mesh = models[pack.lod].Meshes[pack.meshIdx];
            List<PartInfo> list = meshInfoList[pack.meshIdx];

            // Apply part info params.
            PartInfo partInfo = list[pack.partIdx];

            /// Could provide a convenience func FBXModel.Parameter(EffectParams which)
            /// that just passes through { return effectCache.Parameter((int)which); }
            /// but I'm worried that the overhead of a non-inlined function call might
            /// actually add up to something in this case.
            Matrix localToModel = LocalToModel(mesh);
            if (partInfo.Collision)
            {
                AnimationInstance animator = pack.animator;
                localToModel = animator != null
                    ? animator.LocalToWorld(mesh.ParentBone.Index)
                    : mesh.ParentBone.Transform;
            }
            Parameter(EffectParams.LocalToModel).SetValue(localToModel);

            BokuGame.bokuGame.shaderGlobals.SetUpWind(pack.rootToWorld);
            Parameter(EffectParams.WorldMatrix).SetValue(pack.rootToWorld);
            Parameter(EffectParams.WorldMatrixInverseTranspose).SetValue(Matrix.Transpose(Matrix.Invert(pack.rootToWorld)));

            Matrix rootToProjMatrix = pack.rootToWorld * camera.ViewProjectionMatrix;
            Parameter(EffectParams.WorldViewProjMatrix).SetValue(rootToProjMatrix);


            ModelMeshPart part = mesh.MeshParts[pack.partIdx];

            Texture2D diffuseTexture = PartTexture(partInfo);
            Parameter(EffectParams.DiffuseTexture).SetValue(diffuseTexture);
            Parameter(EffectParams.GlowEmissiveColor).SetValue(GlowEmissiveColor);

            Vector4 diffuseColor = PartColor(partInfo, pack.diffuseColor);

            Parameter(EffectParams.DiffuseColor).SetValue(new Vector4(diffuseColor.X, diffuseColor.Y, diffuseColor.Z, 1.0f));
            Parameter(EffectParams.Shininess).SetValue(Shininess * diffuseColor.W);

            Parameter(EffectParams.SpecularColor).SetValue(partInfo.SpecularColor);
            Parameter(EffectParams.EmissiveColor).SetValue(partInfo.EmissiveColor);
            Parameter(EffectParams.SpecularPower).SetValue(partInfo.SpecularPower);

            // HACKHACK  XNA 4 is much pickier than XNA 3.1 about having valid data for
            // all vertex shader inputs.  So, if we find a case where we don't have the 
            // correct inputs, fall back to something simpler.
            if (Effect.CurrentTechnique.Name.Contains("WithWind"))
            {
                VertexElement[] elements = part.VertexBuffer.VertexDeclaration.GetVertexElements();
                if (elements.Length < 4)
                {
                    int index = Effect.CurrentTechnique.Name.IndexOf("WithWind");
                    string technique = Effect.CurrentTechnique.Name.Remove(index, 8);
                    Effect.CurrentTechnique = Effect.Techniques[technique];
                }
            }

            // HACKHACK More working around mismatched vertex decls vs shaders.
            // In this case "WithSkinning" techniques are being used but the vertex decls
            // don't have any BlendWeights.
            {
                string curTechnique = Effect.CurrentTechnique.Name;
                if (curTechnique.Contains("WithSkinning"))
                {
                    VertexElement[] elements = part.VertexBuffer.VertexDeclaration.GetVertexElements();

                    bool hasBlendWeight = false;
                    for (int e = 0; e < elements.Length; e++)
                    {
                        if (elements[e].VertexElementUsage == VertexElementUsage.BlendWeight)
                        {
                            hasBlendWeight = true;
                            break;
                        }
                    }
                    if (!hasBlendWeight)
                    {
                        curTechnique = curTechnique.Replace("WithSkinning", "");
                        Effect.CurrentTechnique = Effect.Techniques[curTechnique];
                    }
                }
            }


            //StopTimer(parmTime);

            //StartTimer(rendTime);
            Effect.CurrentTechnique.Passes[0].Apply();

            try
            {
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                                                part.VertexOffset,
                                                0,
                                                part.NumVertices,
                                                part.StartIndex,
                                                part.PrimitiveCount);
            }
            catch (Exception)
            {
            }

            //StopTimer(rendTime);

        }   // end of FBXModel RenderBatch()

        protected virtual Matrix LocalToModel(ModelMesh mesh)
        {
            return mesh.ParentBone.Transform;
        }

        private void CalcBoundingBox()
        {
            // Start with the first mesh.
            boundingBox = CalcMeshBoundingBox(models[0].Meshes[0]);

            // Grow based on additional meshes.
            for (int i = 1; i < models[0].Meshes.Count; i++)
            {
                boundingBox = BoundingBox.CreateMerged(boundingBox, CalcMeshBoundingBox(models[0].Meshes[0]));
            }
        }   // end of FBXModel CalcBoundingBox()

        private void CalcBoundingSphere()
        {
            // Start with the first mesh.
            boundingSphere = CalcMeshBoundingSphere(models[0].Meshes[0]);

            // Grow based on additional meshes.
            for (int i = 1; i < models[0].Meshes.Count; i++)
            {
                boundingSphere = BoundingSphere.CreateMerged(boundingSphere, CalcMeshBoundingSphere(models[0].Meshes[i]));
            }
        }   // end of FBXModel CalcBoundingSphere()

        private BoundingSphere CalcMeshBoundingSphere(ModelMesh mesh)
        {
            Matrix transform = CalcMeshTransform(mesh);

            Vector3 center = Vector3.Transform(mesh.BoundingSphere.Center, transform);
            Vector3 radius = Vector3.TransformNormal(new Vector3(mesh.BoundingSphere.Radius, 0.0f, 0.0f), transform);

            BoundingSphere sphere = new BoundingSphere(center, radius.Length());

            return sphere;
        }   // end of FBXModel CalcMeshBoundingSphere()

        private BoundingBox CalcMeshBoundingBox(ModelMesh mesh)
        {
            Matrix transform = CalcMeshTransform(mesh);
            BoundingBox box;

            UIMeshData data = mesh.Tag as UIMeshData;
            if (data != null)
            {
                box = new BoundingBox(data.bBox.Min, data.bBox.Max);

                // Transform the box.
                Vector3[] points = box.GetCorners();
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = Vector3.Transform(points[i], transform);
                }
                box = BoundingBox.CreateFromPoints(points);
            }
            else
            {
                box = new BoundingBox(Vector3.Zero, Vector3.Zero);
            }

            return box;
        }   // end of FBXModel CalcMeshBoundingBox()
            
        private Matrix CalcMeshTransform(ModelMesh mesh)
        {
            Matrix transform = mesh.ParentBone.Transform;
            ModelBone parentBone = mesh.ParentBone.Parent;

            while (parentBone != null)
            {
                transform *= parentBone.Transform;
                parentBone = parentBone.Parent;
            }

            return transform;
        }   // end of FBXModel CalcMeshTransform()

        public void LoadContent(bool immediate)
        {
            // Init the effect.
            if (effect == null)
            {
                effect = KoiLibrary.LoadEffect(@"Shaders\Standard");
                ShaderGlobals.RegisterEffect("Standard", effect);

                effectCache.Load(Effect, TechniqueExt);
            }

            // Load the model.
            if (models.Count == 0)
            {
                string modelName = resourceName;
                string lowName = modelName + "Low";
                try
                {
                    Model m = BokuGame.Load<Model>(BokuGame.Settings.MediaPath + lowName);
                    if (m != null)
                        models.Add(m);
                }
                catch (ContentLoadException)
                {
                    /// Guess we don't have one.
                }
                if (!BokuSettings.Settings.PreferReach || (models.Count == 0))
                {
                    try
                    {
                        Model m = BokuGame.Load<Model>(BokuGame.Settings.MediaPath + resourceName);
                        if (m != null)
                            models.Add(m);
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            if (infoLists == null)
            {
                infoLists = new List<List<PartInfo>>[2];
                for (int i = 0; i < models.Count; ++i)
                {
                    /// 3DSMax puts uncontrollable garbage in the root transform.
                    /// We know we always want the root transform to be identity,
                    /// so just enforce that here.
                    models[i].Root.Transform = Matrix.Identity;

                    infoLists[i] = BuildInfoList(models[i], i);
                }

                CalcBoundingBox();
                CalcBoundingSphere();

                BindSurfaces();                
            }
        }
        private List<List<PartInfo>> BuildInfoList(Model model, int lod)
        {
            /// This AnimationInstance is created just to get the initial
            /// bone poses out to the collision primitives, then discarded. 
            /// Each collision primitive instance will
            /// eventually get its own copy (if it's animated).
            AnimationInstance tempAnim = AnimationInstance.TryMake(model);

            // Extract the material info for each part of each mesh.
            List<List<PartInfo>> newList = new List<List<PartInfo>>();

            for (int i = 0; i < model.Meshes.Count; i++)
            {
                ModelMesh mesh = model.Meshes[i];
                List<PartInfo> partInfoList = new List<PartInfo>(mesh.MeshParts.Count);
                newList.Add(partInfoList);

                bool collisionOnly = InitCollisionPrim(tempAnim, mesh, lod);
                for (int j = 0; j < mesh.MeshParts.Count; j++)
                {
                    ModelMeshPart part = mesh.MeshParts[j];
                    PartInfo partInfo = new PartInfo();
                    partInfo.InitFromPart(part);
                    partInfoList.Add(partInfo);

                    if (collisionOnly)
                    {
                        partInfo.Collision = true;
                        partInfo.Render = false;
                    }
                }

            }
            return newList;
        }

        public void InitDeviceResources(GraphicsDevice device)
        {
        }

        public void UnloadContent()
        {
            DeviceResetX.Release(ref effect);
            UnBindSurfaces();
            for (int i = 0; i < models.Count; ++i)
            {
                models[i] = null;
            }
            models = new List<Model>();

            infoLists = null;
            effectCache.UnLoad();
            restPalettes = new List<Matrix[]>();
        }   // end of FBXModel UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
        }

        /// <summary>
        /// Build and hand off a collision primitive encapsulating our geometry.
        /// Will return null if we don't have any collision geometry.
        /// </summary>
        /// <param name="owner"></param>
        /// <returns></returns>
        public CollisionPrimitive CollisionPrim(GameActor owner)
        {
            Compound prim = null;

            if ((collisionPrims != null) && (collisionPrims.Count > 0))
            {
                prim = new Compound();
                prim.Build(owner, collisionPrims);
                prim.DebugName = owner.GetType().ToString() + owner.uniqueNum;
            }
            else
            {
                /*
                // This models doesn't have any collision prims so just set up
                // a single sphere to match its bounding sphere.
                if (collisionPrims == null)
                {
                    collisionPrims = new List<Primitive>();
                }
                for (int i = 0; i < models.Count; i++)
                {
                    for (int j = 0; j < models[0].Meshes.Count; j++)
                    {
                        Primitive sphere = new Ellipsoid();
                        sphere.SetOwner(owner);
                        SetupPrim(models[i].Meshes[j], sphere);
                        collisionPrims.Add(sphere);
                    }
                }
                prim = new Compound();
                prim.Build(owner, collisionPrims);
                prim.DebugName = owner.GetType().ToString() + owner.uniqueNum;
                */
            }

            return prim;
        }
        protected CollisionPrimitive NewPrim(string name)
        {
            CollisionPrimitive prim = null;
            if (name.StartsWith("SCP_CYL"))
            {
                prim = new Cylinder();
            }
            else if (name.StartsWith("SCP_BOX"))
            {
                prim = new Collision.Slab();
            }
            else if (name.StartsWith("SCP_ELL"))
            {
                prim = new Ellipsoid();
            }

            return prim;
        }
        protected bool InitCollisionPrim(AnimationInstance tempAnim, ModelMesh mesh, int lod)
        {
            CollisionPrimitive prim = NewPrim(mesh.Name);
            // Only pull collision info out of the highest LOD file we have.
            // But flag it as collision only, because a lot of the lower LOD
            // versions also have collision prims, even though they shouldn't.
            // Actually, they should.  When running in Reach mode we don't even
            // load the high LOD models so we don't get collision prims unless
            // the low LOD models have them.
            if (lod == NumLODs - 1)
            {
                if (prim != null)
                {
                    if (SetupPrim(mesh, prim))
                    {
                        prim.SetBone(tempAnim, mesh.ParentBone);

                        if (collisionPrims == null)
                        {
                            collisionPrims = new List<CollisionPrimitive>();
                        }
                        collisionPrims.Add(prim);
                    }
                }
            }
            return prim != null;
        }

        protected bool SetupPrim(ModelMesh mesh, CollisionPrimitive prim)
        {
            UIMeshData data = mesh.Tag as UIMeshData;
            if (data != null)
            {
                Vector3 min = new Vector3(data.bBox.Min.X, data.bBox.Min.Y, data.bBox.Min.Z);
                Vector3 max = new Vector3(data.bBox.Max.X, data.bBox.Max.Y, data.bBox.Max.Z);

                prim.DebugName = mesh.Name;
                prim.Make(min, max);

                /*
                // Uncomment to generate code to fit into hacked switch statement below.
                // Note this must be run in Reach mode to get correct values.
                string name = resourceName + mesh.Name;
                name = name.Replace('\\', '-');
                Debug.Print("case \"" + name + "\":");
                Debug.Print("min = new Vector3(" + min.X.ToString() + "f, " + min.Y.ToString() + "f, " + min.Z.ToString() + "f);");
                Debug.Print("max = new Vector3(" + max.X.ToString() + "f, " + max.Y.ToString() + "f, " + max.Z.ToString() + "f);");
                Debug.Print("break;");
                */

                return true;
            }
            else
            {
                // HACKHACK TODO (scoy) Try to figure out why WinRT build can't get Tag info.
                // Seem to be a problem with the MG loader.
                // In the meantime, uncomment the code above, run in debug mode, and copy/paste 
                // the debug output into the switch statement below.
                Vector3 min = Vector3.Zero;
                Vector3 max = Vector3.Zero;
                string name = resourceName + mesh.Name;
                name = name.Replace('\\', '-');
                
                switch (name)
                {
                    case "Models-Tree_DSCP_CYL_TRUNK":
                        min = new Vector3(-1f, -1f, 0.05571938f);
                        max = new Vector3(1f, 1f, 4.055719f);
                        break;
                    case "Models-Tree_DSCP_ELL_LEFT":
                        min = new Vector3(-2f, -2.2f, -1.44f);
                        max = new Vector3(2f, 2.2f, 1.44f);
                        break;
                    case "Models-Tree_DSCP_ELL_RIGHT":
                        min = new Vector3(-2.07611f, -1.660888f, -1.453277f);
                        max = new Vector3(2.07611f, 1.660888f, 1.453277f);
                        break;
                    case "Models-Tree_DSCP_ELL_MAIN":
                        min = new Vector3(-3.6f, -3.2f, -2.4f);
                        max = new Vector3(3.6f, 3.2f, 2.4f);
                        break;
                    case "Models-Tree_DSCP_CYL_BRANCH":
                        min = new Vector3(-0.1f, -0.1f, -0.2f);
                        max = new Vector3(0.1f, 0.1f, 0.2f);
                        break;
                    case "Models-Tree_DSCP_CYL_BRANCH_SM":
                        min = new Vector3(-0.1f, -0.1f, -0.2f);
                        max = new Vector3(0.1f, 0.1f, 0.2f);
                        break;
                    case "Models-castle_towerSCP_ELL_LOWER":
                        min = new Vector3(-1.316537f, -1.306917f, -3.160463f);
                        max = new Vector3(1.316537f, 1.306917f, 3.160463f);
                        break;
                    case "Models-castle_towerSCP_CYL_TURRET":
                        min = new Vector3(-1f, -0.9848078f, -0.9554202f);
                        max = new Vector3(1f, 0.9848078f, 1f);
                        break;
                    case "Models-factorySCP_ELL":
                        min = new Vector3(-3.268652f, -3.372126f, -2.988695f);
                        max = new Vector3(3.268652f, 3.372126f, 2.988695f);
                        break;
                    case "Models-factorySCP_CYL4":
                        min = new Vector3(-1.624051f, -1.624051f, -0.4603433f);
                        max = new Vector3(1.624051f, 1.624051f, 0.4603433f);
                        break;
                    case "Models-factorySCP_CYL3":
                        min = new Vector3(-0.6014375f, -0.6014376f, -1.710882f);
                        max = new Vector3(0.6014375f, 0.6014376f, 1.710882f);
                        break;
                    case "Models-factorySCP_CYL2":
                        min = new Vector3(-0.5049981f, -0.5049982f, -1.36386f);
                        max = new Vector3(0.5049981f, 0.5049982f, 1.36386f);
                        break;
                    case "Models-factorySCP_CYL1":
                        min = new Vector3(-0.5979427f, -0.5979428f, -3.523174f);
                        max = new Vector3(0.5979427f, 0.5979428f, 3.523174f);
                        break;
                    case "Models-factorySCP_CYL":
                        min = new Vector3(-0.5979427f, -0.5979428f, -3.543462f);
                        max = new Vector3(0.5979427f, 0.5979428f, 3.543462f);
                        break;
                    case "Models-factorySCP_BOX":
                        min = new Vector3(-0.580174f, -1.334388f, -3.738417f);
                        max = new Vector3(0.580174f, 1.334388f, 3.738417f);
                        break;
                    case "Models-hutSCP_ELL_HUT":
                        min = new Vector3(-0.4f, -0.4f, -0.4f);
                        max = new Vector3(0.4f, 0.4f, 0.4f);
                        break;
                    case "Models-hutSCP_BOX_DOOR":
                        min = new Vector3(-0.4f, -0.4f, -0.4f);
                        max = new Vector3(0.4f, 0.4f, 0.4f);
                        break;
                    case "Models-stick_boySCP_CYL1":
                        min = new Vector3(-0.04235264f, -0.04235265f, -0.1297408f);
                        max = new Vector3(0.04235264f, 0.04235265f, 0.1297408f);
                        break;
                    case "Models-stick_boySCP_ELL":
                        min = new Vector3(-0.1694106f, -0.1694106f, -0.2594815f);
                        max = new Vector3(0.1694106f, 0.1694106f, 0.2594815f);
                        break;
                    case "Models-stick_boySCP_CYL":
                        min = new Vector3(-0.3322958f, -0.3322958f, -0.06042531f);
                        max = new Vector3(0.3322958f, 0.3322958f, 0.06042531f);
                        break;
                    case "Models-PipeStraightSCP_CYL_LOWER":
                        min = new Vector3(-0.5049838f, -0.4973121f, 0f);
                        max = new Vector3(0.5049838f, 0.497312f, 3.901193f);
                        break;
                    case "Models-PipeCrossSCP_CYL_LOWER001":
                        min = new Vector3(-0.5049838f, -0.4973121f, 0f);
                        max = new Vector3(0.5049838f, 0.497312f, 4.020609f);
                        break;
                    case "Models-PipeCrossSCP_CYL_LOWER":
                        min = new Vector3(-0.5049838f, -0.4973121f, 0f);
                        max = new Vector3(0.5049838f, 0.497312f, 4.020609f);
                        break;
                    case "Models-PipeCornerSCP_CYL_LOWER001":
                        min = new Vector3(-0.5048829f, -0.4972126f, 0f);
                        max = new Vector3(0.5048829f, 0.4972126f, 2.50017f);
                        break;
                    case "Models-PipeCornerSCP_CYL_LOWER":
                        min = new Vector3(-0.5049838f, -0.4973121f, 0f);
                        max = new Vector3(0.5049838f, 0.497312f, 2.5062f);
                        break;
                    case "Models-Tree_ASCP_BOX":
                        min = new Vector3(-0.5112625f, -0.3147269f, -1.137442f);
                        max = new Vector3(0.5112625f, 0.3147269f, 1.137442f);
                        break;
                    case "Models-Tree_ASCP_CYL1":
                        min = new Vector3(-0.2843902f, -0.2843903f, -0.8650908f);
                        max = new Vector3(0.2843902f, 0.2843903f, 0.8650908f);
                        break;
                    case "Models-Tree_ASCP_CYL":
                        min = new Vector3(-0.2843902f, -0.2843903f, -0.8650908f);
                        max = new Vector3(0.2843902f, 0.2843903f, 0.8650908f);
                        break;
                    case "Models-Tree_ASCP_ELL":
                        min = new Vector3(-1.743022f, -1.722249f, -1.821472f);
                        max = new Vector3(1.743022f, 1.722249f, 1.821472f);
                        break;
                    case "Models-Tree_ASCP_ELL1":
                        min = new Vector3(-1.151036f, -1.07284f, -2.693944f);
                        max = new Vector3(1.151036f, 1.07284f, 2.693944f);
                        break;
                    case "Models-Tree_BSCP_ELL1":
                        min = new Vector3(-0.6796957f, -0.661716f, -1.718881f);
                        max = new Vector3(0.6796957f, 0.661716f, 1.718881f);
                        break;
                    case "Models-Tree_BSCP_ELL":
                        min = new Vector3(-1.0056f, -1.0056f, -0.8893347f);
                        max = new Vector3(1.0056f, 1.0056f, 0.8893347f);
                        break;
                    case "Models-Tree_BSCP_CYL":
                        min = new Vector3(-0.3274529f, -0.3274529f, -1.271516f);
                        max = new Vector3(0.3274529f, 0.3274529f, 1.271516f);
                        break;
                    case "Models-Tree_CSCP_BOX":
                        min = new Vector3(-0.5225759f, -0.2330127f, -1.594361f);
                        max = new Vector3(0.5225759f, 0.2330127f, 1.594361f);
                        break;
                    case "Models-Tree_CSCP_CYL":
                        min = new Vector3(-0.2292376f, -0.2292377f, -1.606869f);
                        max = new Vector3(0.2292376f, 0.2292377f, 1.606869f);
                        break;
                    case "Models-Tree_CSCP_ELL":
                        min = new Vector3(-0.8263509f, -0.5958376f, -0.8263509f);
                        max = new Vector3(0.8263509f, 0.5958376f, 0.8263509f);
                        break;
                    case "Models-Tree_CSCP_ELL1":
                        min = new Vector3(-3.187076f, -2.591478f, -1.615843f);
                        max = new Vector3(3.187076f, 2.591478f, 1.615843f);
                        break;
                    case "Models-Tree_CSCP_ELL2":
                        min = new Vector3(-1.848369f, -1.502947f, -1.382351f);
                        max = new Vector3(1.848369f, 1.502947f, 1.382351f);
                        break;
                        
                    default:
                        Debug.Assert(false, "Did we add a new model?  If so, we need to add entry to this switch statement.");
                        return false;
                }
                

                prim.DebugName = mesh.Name;
                prim.Make(min, max);
               
                return true;
            }
        }
        #endregion Internal

    }   // end of FBXModel

}   // end of namespace Boku.SimWorld



