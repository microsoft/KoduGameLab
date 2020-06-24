
//#define TOGGLE_DIRTMAP_HACK

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Boku.Fx
{
    public class SurfaceSet : BokuShared.XmlData<SurfaceSet>
    {
        #region Members
        private string name = "";

        private string techniqueExt = "";

        private string[] names = new string[8];

        private string bumpDetailName = "";
        private Texture2D bumpDetail = null;

        private string dirtMapName = "";
        private Texture2D dirtMap = null;
        
        /// <summary>
        /// Don't load/save the surfaces, we'll look them up from the global
        /// dictionary.
        /// </summary>
        private Surface[] surfaces = new Surface[8];

        private static Texture2D whiteMap = null;
        private static Texture2D checkerMap = null;

#if !SURFACE_EDITOR
        #region Parameter Caching
        /// <summary>
        /// Notice that these are tightly packed and get pretty ugly. Minimizing the 
        /// number of constant registers is worth the ugliness though, and no one should
        /// really care, because to the outside world the parameters are expanded to their
        /// components. Packing is restricted to the Setup and within the shaders.
        /// </summary>
        enum EffectParams
        {
            Diffuse3_Bloom1,
            Emissive3_Wrap1,
            SpecCol3_Pow1,
            Aniso2_EnvInt1_Unused1,
            Bump_Tile1_Int1_Unused2,
            BumpDetail,
            DirtMap,
        };
        private EffectCache effectCache = new EffectCache<EffectParams>();
        private EffectParameter Parameter(EffectParams param)
        {
            return effectCache.Parameter((int)param);
        }
        private Effect effect = null;
        public EffectTechnique Technique(InGame.RenderEffect pass)
        {
            return effectCache.Technique(pass, false);
        }
        #endregion Parameter Caching

#endif
        #endregion Members

        #region Accessors
        /// <summary>
        /// The identifying name for this set.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        /// <summary>
        /// Technique extension for this section
        /// </summary>
        public string TechniqueExt
        {
            get { return techniqueExt; }
            set { techniqueExt = value; }
        }
        /// <summary>
        /// The names of the surfaces (which provides a mapping to indices) in this set.
        /// </summary>
        public string[] SurfaceNames
        {
            get { return names; }
            set { names = value; }
        }
        /// <summary>
        /// Detail map for surface texture.
        /// </summary>
        public Texture2D BumpDetail
        {
            get { return bumpDetail; }
        }
        /// <summary>
        /// Name of the detail map for surface texture.
        /// </summary>
        public string BumpDetailName
        {
            get { return bumpDetailName; }
            set 
            { 
                bumpDetailName = value;
                if (bumpDetail != null)
                {
                    bumpDetail = null;
                    LoadTextures();
                }
            }
        }
        /// <summary>
        /// Dark map for self occlusion and grime.
        /// </summary>
        public Texture2D DirtMap
        {
            get { return dirtMap; }
        }
        /// <summary>
        /// Name of dark map.
        /// </summary>
        public string DirtMapName
        {
            get { return dirtMapName; }
            set
            {
                dirtMapName = value;
                if (dirtMap != null)
                {
                    dirtMap = null;
                    LoadTextures();
                }
            }
        }
        #endregion Accessors

        #region Public

        /// <summary>
        /// Convert a Vector3 color to a local surface index. Colors are [0..1].
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static int Index(Vector3 color)
        {
            Debug.Assert((color.X >= 0.0f) && (color.X <= 1.0f));
            Debug.Assert((color.Y >= 0.0f) && (color.Y <= 1.0f));
            Debug.Assert((color.Z >= 0.0f) && (color.Z <= 1.0f));

            Vector3 scale = new Vector3(4.1f, 2.1f, 1.1f);
            Vector3 dot = Vector3.One;
            float findex = Vector3.Dot(dot, color * scale);
            return (int)findex;
        }

        /// <summary>
        /// Bind surface names to surfaces using the input dictionary.
        /// </summary>
        /// <param name="dict"></param>
        public void Bind(SurfaceDict dict)
        {
            for (int i = 0; i < SurfaceNames.Length; ++i)
            {
                surfaces[i] = dict.Surface(SurfaceNames[i]);
            }
        }

#if !SURFACE_EDITOR

        /// <summary>
        /// Push our current values to the shaders
        /// </summary>
        public void Setup(Vector4 tint4, int lod)
        {
            Vector3 tint = new Vector3(tint4.X, tint4.Y, tint4.Z);
            for (int i = 0; i < 8; ++i)
            {
                Surface surf = Surface(i);
                if (surf != null)
                {
                    Diffuse3_Bloom1_Scratch[i] = Vector4.Clamp(
                        new Vector4(surf.Diffuse + surf.Tintable * tint, 1.0f - surf.Bloom),
                        Vector4.Zero,
                        Vector4.One);

                    Emissive3_Wrap1_Scratch[i] = new Vector4(
                        surf.Emissive + surf.TintedEmissive * tint, surf.Wrap);

                    SpecCol3_Pow1_Scratch[i] = new Vector4(
                        surf.SpecularColor, surf.SpecularPower);

                    Aniso2_EnvInt1_Unused1_Scratch[i] = new Vector4(
                        surf.Aniso.X, surf.Aniso.Y, surf.EnvMapIntensity, 0.0f);

                    Bump_Tile1_Int1_Unused2_Scratch[i] = new Vector4(
                        surf.BumpScale, surf.BumpIntensity, 0.0f, 0.0f);
                }
                else
                {
                    Diffuse3_Bloom1_Scratch[i] = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
                    Emissive3_Wrap1_Scratch[i] = Vector4.Zero;
                    SpecCol3_Pow1_Scratch[i] = Vector4.Zero;
                    Aniso2_EnvInt1_Unused1_Scratch[i] = Vector4.Zero;
                    Bump_Tile1_Int1_Unused2_Scratch[i] = Vector4.Zero;
                }
            }

            Parameter(EffectParams.Diffuse3_Bloom1).SetValue(
                Diffuse3_Bloom1_Scratch);

            Parameter(EffectParams.Emissive3_Wrap1).SetValue(
                Emissive3_Wrap1_Scratch);

            Parameter(EffectParams.SpecCol3_Pow1).SetValue(
                SpecCol3_Pow1_Scratch);

            Parameter(EffectParams.Aniso2_EnvInt1_Unused1).SetValue(
                Aniso2_EnvInt1_Unused1_Scratch);

            Parameter(EffectParams.Bump_Tile1_Int1_Unused2).SetValue(
                Bump_Tile1_Int1_Unused2_Scratch);

            // TODO (****) Why is this null???
            if (Parameter(EffectParams.BumpDetail) != null)
            {
                Parameter(EffectParams.BumpDetail).SetValue(BumpDetail);
            }

            Texture2D dmap = DirtMap;
            if (lod == 0)
            {
                dmap = whiteMap;
            }
#if TOGGLE_DIRTMAP_HACK
            if (KeyboardInputX.IsPressed(Keys.LeftShift))
            {
                dmap = whiteMap;
            }
            else if (KeyboardInputX.IsPressed(Keys.LeftControl))
            {
                dmap = checkerMap;
            }
#endif // TOGGLE_DIRTMAP_HACK

            // TODO (****) Why is this ever null?
            if (Parameter(EffectParams.DirtMap) != null)
            {
                Parameter(EffectParams.DirtMap).SetValue(dmap);
            }
        }
        /// <summary>
        /// Some scratch space for compacting parameters before shoving to shaderland.
        /// </summary>
        private Vector4[] Diffuse3_Bloom1_Scratch = new Vector4[8];
        /// <summary>
        /// Some scratch space for compacting parameters before shoving to shaderland.
        /// </summary>
        private Vector4[] Emissive3_Wrap1_Scratch = new Vector4[8];
        /// <summary>
        /// Some scratch space for compacting parameters before shoving to shaderland.
        /// </summary>
        private Vector4[] SpecCol3_Pow1_Scratch = new Vector4[8];
        /// <summary>
        /// Some scratch space for compacting parameters before shoving to shaderland.
        /// </summary>
        private Vector4[] Aniso2_EnvInt1_Unused1_Scratch = new Vector4[8];
        /// <summary>
        /// Some scratch space for compacting parameters before shoving to shaderland.
        /// </summary>
        private Vector4[] Bump_Tile1_Int1_Unused2_Scratch = new Vector4[8];

#endif

        #endregion Public

        #region Internal

#if !SURFACE_EDITOR
        public void Bind(Effect effect)
        {
            this.effect = effect;
            effectCache.Load(effect, TechniqueExt + "_SURF");

            LoadTextures();
        }

        public void UnBind()
        {
            effectCache.UnLoad();

            UnLoadTextures();
        }

        protected override bool OnLoad()
        {
            Bind(BokuGame.bokuGame.Surfaces);

            return true;
        }

        private void LoadTextures()
        {
            if ((bumpDetail == null) && !string.IsNullOrEmpty(bumpDetailName))
            {
                bumpDetail = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + bumpDetailName);
            }
            if ((dirtMap == null) && !string.IsNullOrEmpty(dirtMapName))
            {
                dirtMap = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + dirtMapName);
            }

            if (whiteMap == null)
            {
                whiteMap = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\White");
            }
            if (checkerMap == null)
            {
                checkerMap = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Checker");
            }
        }
#else
        private void LoadTextures()
        {
            // surface editor doesn't load the textures
        }
#endif

        private void UnLoadTextures()
        {
            bumpDetail = null;
            dirtMap = null;
            whiteMap = null;
            checkerMap = null;
        }

        /// <summary>
        /// Retrieve a specific surface. You shouldn't really do this, you should get
        /// the name and then look it up from the global dictionary in SurfaceDict.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private Surface Surface(int i)
        {
            return surfaces[i];
        }

        #endregion Internal
    }
}
