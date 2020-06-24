
using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;

using Boku.Fx;
using Boku.Common;
using Boku.Common.Xml;
using Boku.Audio;

namespace Boku.SimWorld.Terra
{
    public class TerrainMaterial
    {
        #region CONSTANTS
        private const ushort defaultMatIdx = 115;
        public const ushort MinMatIdx = 0; //0 IS a VALID material index! (Shown to users as material #26)
        public const ushort MaxMatIdx = (MaxNum - 1);
        public const ushort MaxNum = 121;
        public const ushort EmptyMatIdx = 1 << 12;

        public enum Flags : ushort
        {
            Selection = 1 << 14,
            Fabric    = 1 << 13,
        }
        #endregion

        #region EFFECT_CACHE
        private EffectCache effectCacheColor;
        private EffectCache effectCacheEdit;

        public enum EffectParams
        {
            BotColor = 0,
            BotGloss,
            TopColor,
            TopGloss,
            BotEmissive,
            TopEmissive,
            SpecularPower,
            BotTex,
            TopTex,            
            BotBumpStrength,
            TopBumpStrength,
            BotUVWToUV,
            TopUVWToUV,
        }
        public const int EffectParamsNum = (int)EffectParams.TopUVWToUV;
        public enum EffectTechs
        {
            TerrainDepthPass = InGame.RenderEffect.Normal,
            TerrainEditMode,
            TerrainColorPass,
        }
        public const int EffectTechsNum = (int)EffectTechs.TerrainColorPass;

        /// <summary>
        /// Translate enum into actual effect parameter ref.
        /// </summary>
        private EffectParameter ParameterColor(EffectParams param)
        {
            return effectCacheColor.Parameter((int)param);
        }
        private EffectParameter ParameterEdit(EffectParams param)
        {
            return effectCacheEdit.Parameter((int)param);
        }
        /// <summary>
        /// Translate enum into actual technique ref.
        /// </summary>
        public EffectTechnique TechniqueColor(EffectTechs tech)
        {
            return effectCacheColor.Technique((int)tech);
        }
        public EffectTechnique TechniqueEdit(EffectTechs tech)
        {
            return effectCacheEdit.Technique((int)tech);
        }

        #region FabricRM effect state
        public enum EffectParams_FA
        {
            BotUVWToUV_FA = EffectParamsNum + 1,
            TopUVWToUV_FA,
        }
        public enum EffectTechs_FA
        {
            TerrainDepthPass_FA = EffectTechsNum + 1,
            TerrainEditMode_FA,
            TerrainColorPass_FA,
        }

        /// <summary>
        /// Translate enum into actual effect parameter ref.
        /// </summary>
        private EffectParameter ParameterColor(EffectParams_FA param)
        {
            return effectCacheColor.Parameter((int)param);
        }
        private EffectParameter ParameterEdit(EffectParams_FA param)
        {
            return effectCacheEdit.Parameter((int)param);
        }
        /// <summary>
        /// Translate enum into actual technique ref.
        /// </summary>
        public EffectTechnique TechniqueColor(EffectTechs_FA tech)
        {
            return effectCacheColor.Technique((int)tech);
        }
        public EffectTechnique TechniqueEdit(EffectTechs_FA tech)
        {
            return effectCacheEdit.Technique((int)tech);
        }

        #endregion

        #endregion EFFECT_CACHE

        #region MEMBERS

        private Texture2D[] botTex = new Texture2D[Tile.NumFaces];
        private Texture2D[] topTex = new Texture2D[Tile.NumFaces];

        private Matrix[] botUvwToUv = new Matrix[Tile.NumFaces];
        private Matrix[] topUvwToUv = new Matrix[Tile.NumFaces];
        private Matrix[] uiBotUvwToUv = new Matrix[Tile.NumFaces];
        private Matrix[] uiTopUvwToUv = new Matrix[Tile.NumFaces];

        XmlTerrainMaterialData xmlData = new XmlTerrainMaterialData();

        private static readonly IDictionary<ushort, float> uiPriDict = new Dictionary<ushort, float>(2 * (MaxNum));
        private static readonly IDictionary<ushort, int> usageDict = new Dictionary<ushort, int>(2 * (MaxNum));
        private static readonly TerrainMaterial[] materials = new TerrainMaterial[MaxNum];

        private static SamplerState wrapWrapSampler = null;     // Address U and V use wrap
        private static SamplerState wrapClampSampler = null;    // Address U uses wrap, Address V uses clamp

        #endregion MEMBERS

        #region ACCESSORS
        public static TerrainMaterial[] Materials
        {
            get { return materials; }
        }

        /// <summary>
        /// Texture(s) for the base layer.
        /// </summary>
        public Texture2D[] BotTex
        {
            get { return botTex; }
            private set { botTex = value; }
        }
        /// <summary>
        /// Texture(s) (if any) for the top layer.
        /// </summary>
        public Texture2D[] TopTex
        {
            get { return topTex; }
            private set { topTex = value; }
        }
        /// <summary>
        /// Tint of the base layer.
        /// </summary>
        public Vector4 Color
        {
            get { return xmlData.Color; }
            private set { xmlData.Color = value; }
        }
        /// <summary>
        /// Glossiness of the base layer.
        /// </summary>
        public float Gloss
        {
            get { return xmlData.Gloss; }
            private set { xmlData.Gloss = value; }
        }
        /// <summary>
        /// Self illumination for base layer.
        /// </summary>
        public Vector4 BotEmissive
        {
            get { return xmlData.BotEmissive; }
            private set { xmlData.BotEmissive = value; }
        }
        /// <summary>
        /// Tint of the top layer.
        /// </summary>
        public Vector4 TopColor
        {
            get { return xmlData.TopColor; }
            private set { xmlData.TopColor = value; }
        }
        /// <summary>
        /// Glossiness of the top layer.
        /// </summary>
        public float TopGloss
        {
            get { return xmlData.TopGloss; }
            private set { xmlData.TopGloss = value; }
        }
        /// <summary>
        /// Self illumination for top layer.
        /// </summary>
        public Vector4 TopEmissive
        {
            get { return xmlData.TopEmissive; }
            private set { xmlData.TopEmissive = value; }
        }
        /// <summary>
        /// Whether the top layer is clamped or tiling in the V direction.
        /// </summary>
        public bool TopClamped
        {
            get { return xmlData.TopClamped; }
            private set { xmlData.TopClamped = value; }
        }

        /// <summary>
        /// Whether the bottom layer is clamped or tiling in the V direction.
        /// </summary>
        public bool BotClamped
        {
            get { return xmlData.BotClamped; }
            private set { xmlData.BotClamped = value; }
        }

        /// <summary>
        /// Strength of bump on bottom layer, 1 is normal, higher accentuates bumps.
        /// </summary>
        public float BotBumpStrength
        {
            get { return xmlData.BotBumpStrength; }
            private set { xmlData.BotBumpStrength = value; }
        }

        /// <summary>
        /// Strength of bump on top layer, 1 is normal, higher accentuates bumps.
        /// </summary>
        public float TopBumpStrength
        {
            get { return xmlData.TopBumpStrength; }
            private set { xmlData.TopBumpStrength = value; }
        }

        public float BotScale
        {
            get { return xmlData.BotScale; }
            private set { xmlData.BotScale = value; }
        }

        public float TopScale
        {
            get { return xmlData.TopScale; }
            private set { xmlData.TopScale = value; }
        }

        /// <summary>
        /// Maximum step between adjacent cubes that will be smoothed.
        /// </summary>
        public float Step
        {
            get { return xmlData.Step; }
            set { xmlData.Step = value; }
        }
        /// <summary>
        /// Transform from baked in uvw to current texture uv for bottom layer.
        /// </summary>
        public Matrix[] BotUVWToUV
        {
            get { return botUvwToUv; }
        }

        /// <summary>
        /// Transform from baked in uvw to current texture uv for top layer.
        /// </summary>
        public Matrix[] TopUVWToUV
        {
            get { return topUvwToUv; }
        }

        /// <summary>
        /// Transform from baked in uvw to current texture uv for bottom layer.
        /// Used for UI, to clamp max scale to something sensible on a single cube.
        /// </summary>
        public Matrix[] UIBotUVWToUV
        {
            get { return uiBotUvwToUv; }
        }

        /// <summary>
        /// Transform from baked in uvw to current texture uv for top layer.
        /// Used for UI, to clamp max scale to something sensible on a single cube.
        /// </summary>
        public Matrix[] UITopUVWToUV
        {
            get { return uiTopUvwToUv; }
        }

        /// <summary>
        /// Extension suffix for this material's techniques.
        /// </summary>
        public string TechniqueExt
        {
            get { return xmlData.TechniqueExt; }
            private set { xmlData.TechniqueExt = value; }
        }

        public static IDictionary<ushort, int> Users
        {
            get { return usageDict; }
        }

        public Foley.CollisionSound CollisionSound
        {
            get { return xmlData.CollisionSound; }
            set { xmlData.CollisionSound = value; }
        }

        /// <summary>
        /// This is really a constant, telling the terrain UI which material should
        /// be the default chosen, before the user has been into the material picker.
        /// </summary>
        public static ushort DefaultMatIdx
        {
            get { return defaultMatIdx; }
        }

        #region INTERNAL
        /// <summary>
        /// Where the actual numbers are stored.
        /// </summary>
        private XmlTerrainMaterialData XmlData
        {
            get { return xmlData; }
        }
        #endregion INTERNAL

        #endregion ACCESSORS

        #region PUBLIC
        public static float GetUIPriority(ushort matIdx)
        {
            return uiPriDict[matIdx] 
                    + (IsUsed(matIdx) || HasFlags(matIdx, Flags.Selection) 
                        ? 10000 
                        : 0);
        }
        public static TerrainMaterial Get(int matIdx) { return Get((ushort)matIdx); }
        public static TerrainMaterial Get(ushort matIdx)
        {
            matIdx = GetNonFabric(matIdx);
            matIdx = Unselect(matIdx);

            return Materials[matIdx];
        }
        public static bool IsUsed(ushort matIdx)
        {
            int val;
            if (Users.TryGetValue(matIdx, out val))
                return val > 0;
            else
                return false;
        }
        public static void ResetUsers()
        {
            Users.Clear();
        }
        
        /// <summary>
        /// Checks if a currentl terrain color is valid.
        /// </summary>
        /// <param name="matIdx"></param>
        /// <param name="allowEmpty">Treat empty as valid.  Defaults to true.</param>
        /// <param name="allowSelectionFlag">Treats terrain with selection flag as valid.  Defaults to true.</param>
        /// <returns></returns>
        public static bool IsValid(ushort matIdx, bool allowEmpty = true, bool allowSelectionFlag = true)
        {
            if (matIdx == EmptyMatIdx)
                return allowEmpty;

            matIdx = GetNonFabric(matIdx); //Gets rid of the fabric flag if there is one

            if (allowSelectionFlag)
                matIdx = RemoveFlags(matIdx, Flags.Selection); //An index with the "Selection" flag IS considered VALID

            if (matIdx < TerrainMaterial.MinMatIdx || TerrainMaterial.MaxMatIdx < matIdx)
                return false;

            return true;
        }
        public static bool HasFlags(ushort matIdx, Flags flags)
        {
            return (matIdx & (ushort)flags) == (ushort)flags;
        }
        public static ushort SetFlags(ushort matIdx, Flags flags)
        {
            return (ushort)(matIdx | (ushort)flags);
        }
        public static ushort RemoveFlags(ushort matIdx, Flags flags)
        {
            return (ushort)~(~matIdx | (ushort)flags);
        }
        public static ushort Unselect(ushort matIdx)
        {
            return TerrainMaterial.RemoveFlags(matIdx, Flags.Selection);
        }

        /// <summary>
        /// Static constructor. We don't touch disk here,
        /// just allocating space for a few static members.
        /// </summary>
        static TerrainMaterial()
        {
            // Set all the keys in the dictionaries so we don't get
            // key-not-found errors.
            for (ushort i = 0; i < MaxNum; i++)
            {
                uiPriDict[i] = 0;
                uiPriDict[GetFabric(i)] = 0;
            }

            ResetUsers();

            if (wrapWrapSampler == null)
            {
                wrapWrapSampler = new SamplerState();
                wrapWrapSampler.AddressU = TextureAddressMode.Wrap;
                wrapWrapSampler.AddressV = TextureAddressMode.Wrap;
            }
            if (wrapClampSampler == null)
            {
                wrapClampSampler = new SamplerState();
                wrapClampSampler.AddressU = TextureAddressMode.Wrap;
                wrapClampSampler.AddressV = TextureAddressMode.Wrap;
            }

        }

        /// <summary>
        /// Constructor
        /// </summary>
        public TerrainMaterial()
        {            
        }

        public void Init(XmlTerrainMaterialData data)
        {
            xmlData = data;
        }
        
        public void Setup_FD(bool forUI)
        {
            if (forUI)
            {
                ParameterColor(EffectParams.BotUVWToUV).SetValue(UIBotUVWToUV);
                ParameterColor(EffectParams.TopUVWToUV).SetValue(UITopUVWToUV);
                ParameterEdit(EffectParams.BotUVWToUV).SetValue(UIBotUVWToUV);
                ParameterEdit(EffectParams.TopUVWToUV).SetValue(UITopUVWToUV);
            }
            else
            {
                ParameterColor(EffectParams.BotUVWToUV).SetValue(BotUVWToUV);
                ParameterColor(EffectParams.TopUVWToUV).SetValue(TopUVWToUV);
                ParameterEdit(EffectParams.BotUVWToUV).SetValue(BotUVWToUV);
                ParameterEdit(EffectParams.TopUVWToUV).SetValue(TopUVWToUV);
            }

            Setup(forUI);
        }
        public void SetupTop_FD(bool forUI)
        {
            ParameterColor(EffectParams.BotTex).SetValue(BotTex[(int)Tile.Face.Top]);
            ParameterColor(EffectParams.TopTex).SetValue(TopTex[(int)Tile.Face.Top]);
            ParameterEdit(EffectParams.BotTex).SetValue(BotTex[(int)Tile.Face.Top]);
            ParameterEdit(EffectParams.TopTex).SetValue(TopTex[(int)Tile.Face.Top]);

            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            //Set address mode for texture samplers
            device.SamplerStates[0] = wrapWrapSampler;
            device.SamplerStates[1] = wrapWrapSampler;  //Top face, always wrapped
        }
        public void SetupSides_FD(bool forUI)
        {
            ParameterColor(EffectParams.BotTex).SetValue(BotTex[(int)Tile.Face.Front]);
            ParameterColor(EffectParams.TopTex).SetValue(TopTex[(int)Tile.Face.Front]);
            ParameterEdit(EffectParams.BotTex).SetValue(BotTex[(int)Tile.Face.Front]);
            ParameterEdit(EffectParams.TopTex).SetValue(TopTex[(int)Tile.Face.Front]);

            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            //Set address mode for texture samplers
            device.SamplerStates[0] = BotClamped ? wrapClampSampler : wrapWrapSampler;
            device.SamplerStates[1] = TopClamped ? wrapClampSampler : wrapWrapSampler;
        }

        public static ushort GetNonFabric(ushort matIdx)
        {
            unchecked
            {
                return (ushort)(matIdx & (ushort)(~TerrainMaterial.Flags.Fabric));
            }
        }
        public static ushort GetFabric(ushort matIdx)
        {
            unchecked
            {
                return (ushort)(matIdx | (ushort)TerrainMaterial.Flags.Fabric);
            }
        }

        public static bool IsFabric(ushort matIdx)
        {
            return (matIdx & (ushort)TerrainMaterial.Flags.Fabric) != 0;
        }

        public void Setup_FA(bool forUI)
        {
            if (forUI)
            {
                ParameterEdit(EffectParams_FA.BotUVWToUV_FA).SetValue(UIBotUVWToUV[(int)Tile.Face.Top]);
                ParameterEdit(EffectParams_FA.TopUVWToUV_FA).SetValue(UITopUVWToUV[(int)Tile.Face.Top]);
                ParameterColor(EffectParams_FA.BotUVWToUV_FA).SetValue(UIBotUVWToUV[(int)Tile.Face.Top]);
                ParameterColor(EffectParams_FA.TopUVWToUV_FA).SetValue(UITopUVWToUV[(int)Tile.Face.Top]);
            }
            else
            {
                ParameterEdit(EffectParams_FA.BotUVWToUV_FA).SetValue(BotUVWToUV[(int)Tile.Face.Top]);
                ParameterEdit(EffectParams_FA.TopUVWToUV_FA).SetValue(TopUVWToUV[(int)Tile.Face.Top]);
                ParameterColor(EffectParams_FA.BotUVWToUV_FA).SetValue(BotUVWToUV[(int)Tile.Face.Top]);
                ParameterColor(EffectParams_FA.TopUVWToUV_FA).SetValue(TopUVWToUV[(int)Tile.Face.Top]);
            }

            ParameterEdit(EffectParams.BotTex).SetValue(BotTex[(int)Tile.Face.Top]);
            ParameterEdit(EffectParams.TopTex).SetValue(TopTex[(int)Tile.Face.Top]);
            ParameterColor(EffectParams.BotTex).SetValue(BotTex[(int)Tile.Face.Top]);
            ParameterColor(EffectParams.TopTex).SetValue(TopTex[(int)Tile.Face.Top]);

            GraphicsDevice device = KoiLibrary.GraphicsDevice;
            //Set address mode for texture samplers
            device.SamplerStates[0] = wrapWrapSampler;
            device.SamplerStates[1] = wrapWrapSampler;      // Top face, always wrapped.

            Setup(forUI);
        }

        /// <summary>
        /// Set parameters to effect for render
        /// </summary>
        public void Setup(bool forUI)
        {
            ParameterEdit(EffectParams.BotBumpStrength).SetValue(
                    new Vector4(BotBumpStrength * 2.0f,
                                BotBumpStrength * -1.0f,
                                2.0f,
                                -1.0f));
            ParameterEdit(EffectParams.TopBumpStrength).SetValue(
                    new Vector4(TopBumpStrength * 2.0f,
                                TopBumpStrength * -1.0f,
                                2.0f,
                                -1.0f));
            ParameterEdit(EffectParams.BotColor).SetValue(Color);
            ParameterEdit(EffectParams.BotGloss).SetValue(Gloss);
            ParameterEdit(EffectParams.BotEmissive).SetValue(BotEmissive);
            ParameterEdit(EffectParams.TopColor).SetValue(TopColor);
            ParameterEdit(EffectParams.TopGloss).SetValue(TopGloss);
            ParameterEdit(EffectParams.TopEmissive).SetValue(TopEmissive);
            ParameterEdit(EffectParams.SpecularPower).SetValue(5.0f);

            ParameterColor(EffectParams.BotBumpStrength).SetValue(
            new Vector4(BotBumpStrength * 2.0f,
                        BotBumpStrength * -1.0f,
                        2.0f,
                        -1.0f));
            ParameterColor(EffectParams.TopBumpStrength).SetValue(
                    new Vector4(TopBumpStrength * 2.0f,
                                TopBumpStrength * -1.0f,
                                2.0f,
                                -1.0f));
            ParameterColor(EffectParams.BotColor).SetValue(Color);
            ParameterColor(EffectParams.BotGloss).SetValue(Gloss);
            ParameterColor(EffectParams.BotEmissive).SetValue(BotEmissive);
            ParameterColor(EffectParams.TopColor).SetValue(TopColor);
            ParameterColor(EffectParams.TopGloss).SetValue(TopGloss);
            ParameterColor(EffectParams.TopEmissive).SetValue(TopEmissive);
            ParameterColor(EffectParams.SpecularPower).SetValue(5.0f);
        }

        /// <summary>
        /// This whole function is just a hack to hardcode materials, rather
        /// than load them from XML.
        /// </summary>
        public void Init(ushort matIdx)
        {
            if (xmlData.BotBump == null)
            {
                /// Temp hack to prime up with some plausible materials. These
                /// will eventually be specified in XML.
                xmlData.BotBump = new string[Tile.NumFaces];
                xmlData.TopBump = new string[Tile.NumFaces];
                xmlData.BotDiffuse = new string[Tile.NumFaces];
                xmlData.TopDiffuse = new string[Tile.NumFaces];

                if (!BokuGame.HiDefProfile)
                {
                    InitSM2(matIdx);
                }
                else
                {
                    InitSM3(matIdx);
                }

                if (xmlData.BotDiffuse[0] == null)
                {
                    xmlData.BotDiffuse[0] = @"Textures\white";
                    xmlData.BotDiffuse[1] = @"Textures\white";
                    xmlData.TopDiffuse[0] = @"Textures\white";
                    xmlData.TopDiffuse[1] = @"Textures\white";
                }
                if (xmlData.BotDiffuse[1] == null)
                {
                    xmlData.BotDiffuse[1] = xmlData.BotDiffuse[0];
                }
                if (xmlData.TopDiffuse[1] == null)
                {
                    xmlData.TopDiffuse[1] = xmlData.TopDiffuse[0];
                }
                if (xmlData.BotBump[1] == null)
                {
                    xmlData.BotBump[1] = xmlData.BotBump[0];
                }
                if (xmlData.TopBump[1] == null)
                {
                    xmlData.TopBump[1] = xmlData.TopBump[0];
                }
                for (int i = 2; i < Tile.NumFaces; ++i)
                {
                    if (xmlData.BotBump[i] == null)
                    {
                        xmlData.BotBump[i] = xmlData.BotBump[1];
                    }
                    if (xmlData.TopBump[i] == null)
                    {
                        xmlData.TopBump[i] = xmlData.TopBump[1];
                    }
                    if (xmlData.BotDiffuse[i] == null)
                    {
                        xmlData.BotDiffuse[i] = xmlData.BotDiffuse[1];
                    }
                    if (xmlData.TopDiffuse[i] == null)
                    {
                        xmlData.TopDiffuse[i] = xmlData.TopDiffuse[1];
                    }
                }
            }
        }

        /// <summary>
        /// Initialize from effect
        /// </summary>
        /// <param name="device"></param>
        /// <param name="effect"></param>
        /// <param name="map"></param>
        public void LoadColor(GraphicsDevice device, Effect effect, ushort matIdx)
        {
            if (!BokuGame.HiDefProfile)
                LoadTexturesSM2();
            else
                LoadTexturesSM3();

            effectCacheColor = new EffectCacheWithTechs(
                new Type[]
                    { 
                        typeof(EffectParams), 
                        typeof(EffectParams_FA)
                    },
                new Type[]
                    { 
                        typeof(EffectTechs),
                        typeof(EffectTechs_FA)
                    });

            effectCacheColor.Load(effect, TechniqueExt);

            MakeTransforms();
        }
        public void LoadEdit(GraphicsDevice device, Effect effect, ushort matIdx)
        {
            if (!BokuGame.HiDefProfile)
                LoadTexturesSM2();
            else
                LoadTexturesSM3();

            effectCacheEdit = new EffectCacheWithTechs(
                new Type[]
                    { 
                        typeof(EffectParams) ,
                        typeof(EffectParams_FA)
                    },
                new Type[]
                    { 
                        typeof(EffectTechs),
                        typeof(EffectTechs_FA)
                    });

            effectCacheEdit.Load(effect, TechniqueExt);

            MakeTransforms();
        }

        /// <summary>
        /// Release device resources
        /// </summary>
        public void Dispose()
        {
            for (int i = 0; i < Tile.NumFaces; ++i)
            {
                if (botTex[i] != null)
                {
                    botTex[i].Dispose();
                    botTex[i] = null;
                }

                if (topTex[i] != null)
                {
                    topTex[i].Dispose();
                    topTex[i] = null;
                }
            }
        }

        #endregion PUBLIC

        #region INTERNAL

        private static Matrix MakeTransform(Tile.Face face, float zScale, float mult)
        {
            Matrix xfm = Matrix.Identity;
            switch (face)
            {
                case Tile.Face.Top:
                    xfm = new Matrix(
                        1.0f * mult, 0.0f, 0.0f, 0.0f,
                        0.0f, -1.0f * mult, 0.0f, 0.0f,
                        0.0f, 0.0f, 0.0f, 0.0f,
                        1.0f * mult, 1.0f * mult, 0.0f, 1.0f);
                    break;
                case Tile.Face.Front:
                    xfm = new Matrix(
                        1.0f * mult, 0.0f, 0.0f, 0.0f,
                        0.0f, 0.0f, 0.0f, 0.0f,
                        0.0f, zScale, 0.0f, 0.0f,
                        1.0f * mult, 0.0f, 0.0f, 1.0f);
                    break;
                case Tile.Face.Back:
                    xfm = new Matrix(
                        -1.0f * mult, 0.0f, 0.0f, 0.0f,
                        0.0f, 0.0f, 0.0f, 0.0f,
                        0.0f, zScale, 0.0f, 0.0f,
                        1.0f * mult, 0.0f, 0.0f, 1.0f);
                    break;
                case Tile.Face.Left:
                    xfm = new Matrix(
                        0.0f, 0.0f, 0.0f, 0.0f,
                        -1.0f * mult, 0.0f, 0.0f, 0.0f,
                        0.0f, zScale, 0.0f, 0.0f,
                        1.0f * mult, 0.0f, 0.0f, 1.0f);
                    break;
                case Tile.Face.Right:
                    xfm = new Matrix(
                        0.0f, 0.0f, 0.0f, 0.0f,
                        1.0f * mult, 0.0f, 0.0f, 0.0f,
                        0.0f, zScale, 0.0f, 0.0f,
                        1.0f * mult, 0.0f, 0.0f, 1.0f);
                    break;
                default:
                    Debug.Assert(false, "Unknown face type");
                    break;
            }
            return xfm;
        }
        /// <summary>
        /// Set up the mapping from vertex uvw to texture uv
        /// for each orientation
        /// </summary>
        /// <param name="scale"></param>
        /// <param name="size"></param>
        private void MakeTransforms()
        {
            /// Default is to make the bumpmap square. So
            /// one tiling horizontally (or square width) should be one 
            /// tiling vertically as well. If the topdownsquare isn't a square
            /// but a rectangle, split the difference.
            float botZScale = BotClamped ? -1.0f : -1.0f / BotScale;
            float botMult = 1.0f / BotScale;

            float topZScale = TopClamped ? -1.0f : -1.0f / TopScale;
            float topMult = 1.0f / TopScale;

            float maxUIScale = 1.0f;
            float uiBotScale = Math.Min(BotScale, maxUIScale);
            float uiTopScale = Math.Min(TopScale, maxUIScale);

            float uiBotZScale = BotClamped ? -1.0f : -1.0f / uiBotScale;
            float uiBotMult = 1.0f / uiBotScale;

            float uiTopZScale = TopClamped ? -1.0f : -1.0f / uiTopScale;
            float uiTopMult = 1.0f / uiTopScale;

            for (int i = 0; i < Tile.NumFaces; ++i)
            {
                botUvwToUv[i] = MakeTransform((Tile.Face)i, botZScale, botMult);

                topUvwToUv[i] = MakeTransform((Tile.Face)i, topZScale, topMult);

                uiBotUvwToUv[i] = MakeTransform((Tile.Face)i, uiBotZScale, uiBotMult);

                uiTopUvwToUv[i] = MakeTransform((Tile.Face)i, uiTopZScale, uiTopMult);
            }

        }

        protected Texture2D LoadTex(string name)
        {
            Texture2D tex = null;
            if (!string.IsNullOrEmpty(name))
            {
                string texName = name;
                tex = KoiLibrary.LoadTexture2D(texName);
            }
            return tex;
        }
        protected void LoadTexturesSM2()
        {
            for (int i = 0; i < Tile.NumFaces; ++i)
            {
                botTex[i] = LoadTex(xmlData.BotDiffuse[i]);
                topTex[i] = LoadTex(xmlData.TopDiffuse[i]);
            }
        }

        protected void LoadTexturesSM3()
        {
            for (int i = 0; i < Tile.NumFaces; ++i)
            {
                botTex[i] = LoadTex(xmlData.BotBump[i]);
                topTex[i] = LoadTex(xmlData.TopBump[i]);
            }
        }

        #region INIT_HACKERY

        protected void InitSM2(ushort matIdx)
        {
            switch (matIdx)
            {
                #region Print Styles
                ///
                /// Print styles begin
                case 28:
                    Color = Vector4.One;
                    Gloss = 0.3f;
                    BotScale = 30.0f;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\D_01_2";
                    xmlData.BotDiffuse[1]
                        = @"Textures\DistortionField_desat";
                    uiPriDict[matIdx] = 200f;
                    xmlData.CollisionSound = Foley.CollisionSound.grass;
                    break;

                case 31:
                    Color = Vector4.One;
                    Gloss = 0.3f;
                    BotScale = 30.0f;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\D_02_2";
                    xmlData.BotDiffuse[1]
                        = @"Textures\DistortionField_desat";
                    uiPriDict[matIdx] = 200f;
                    xmlData.CollisionSound = Foley.CollisionSound.grass;
                    break;

                case 32:
                    Color = Vector4.One;
                    Gloss = 0.3f;
                    BotScale = 30.0f;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\D_03_2";
                    xmlData.BotDiffuse[1]
                        = @"Textures\DistortionField_desat";

                    uiPriDict[matIdx] = 200f;
                    xmlData.CollisionSound = Foley.CollisionSound.grass;
                    break;

                case 33:
                    Color = Vector4.One;
                    Gloss = 1.0f;
                    BotScale = 30.0f;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\D_04_2";
                    xmlData.BotDiffuse[1]
                        = @"Textures\DistortionField_desat";

                    uiPriDict[matIdx] = 200f;
                    xmlData.CollisionSound = Foley.CollisionSound.grass;
                    break;

                case 34:
                    Color = Vector4.One;
                    Gloss = 1.0f;
                    BotScale = 30.0f;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\D_05_2";
                    xmlData.BotDiffuse[1]
                        = @"Textures\DistortionField_desat";

                    uiPriDict[matIdx] = 200f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;
                case 35:
                    Color = Vector4.One;
                    Gloss = 1.0f;
                    BotScale = 30.0f;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\D_06_2";
                    xmlData.BotDiffuse[1]
                        = @"Textures\DistortionField_desat";

                    uiPriDict[matIdx] = 200f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;
                case 36:
                    Color = Vector4.One;
                    Gloss = 5.0f;
                    BotScale = 30.0f;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\D_07_2";
                    xmlData.BotDiffuse[1]
                        = @"Textures\DistortionField_desat";

                    uiPriDict[matIdx] = 200f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 37:
                    Color = Vector4.One;
                    Gloss = 1.0f;
                    BotScale = 30.0f;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\D_08_2";
                    xmlData.BotDiffuse[1]
                        = @"Textures\DistortionField_desat";

                    uiPriDict[matIdx] = 200f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;
                case 38:
                    Color = Vector4.One;
                    Gloss = 1.0f;
                    BotScale = 30.0f;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\D_09_2";
                    xmlData.BotDiffuse[1]
                        = @"Textures\DistortionField_desat";

                    uiPriDict[matIdx] = 200f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;
                case 39:
                    Color = Vector4.One;
                    Gloss = 1.0f;
                    BotScale = 30.0f;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\D_10_2";
                    xmlData.BotDiffuse[1]
                        = @"Textures\DistortionField_desat";

                    uiPriDict[matIdx] = 200f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 40:
                    Color = Vector4.One;
                    Gloss = 5.0f;
                    BotScale = 30.0f;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\D_11_2";
                    xmlData.BotDiffuse[1]
                        = @"Textures\DistortionField_desat";

                    uiPriDict[matIdx] = 200f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;
                case 41:
                    Color = Vector4.One;
                    Gloss = 0.2f;
                    BotScale = 30.0f;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\D_12_2";
                    xmlData.BotDiffuse[1]
                        = @"Textures\DistortionField_desat";

                    uiPriDict[matIdx] = 200f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;
                case 42:
                    Color = Vector4.One;
                    Gloss = 0.2f;
                    BotScale = 30.0f;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\D_13_2";
                    xmlData.BotDiffuse[1]
                        = @"Textures\DistortionField_desat";

                    uiPriDict[matIdx] = 200f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;
                #endregion Print Styles

                ///
                /// Print styles end
                /// One-offs styles begin
                /// 

                #region One Offs
                case 15:
                    xmlData.BotDiffuse[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\alphabet_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\alphabet_desat";

                    Color = new Vector4(197f / 256f, 183f / 256f, 162f / 256f, 1.0f);
                    Gloss = .3f;
                    BotScale = 4.0f;

                    uiPriDict[matIdx] = 150f;
                    xmlData.CollisionSound = Foley.CollisionSound.grass;
                    break;

                #endregion One Offs

                ///
                /// One-offs styles end
                /// Realistics styles begin
                /// 

                #region Realistics
                case 1:

                    Color = new Vector4(0.9f, 0.65f, 0.04f, 1.0f);
                    Gloss = 0.4f;
                    TopColor = new Vector4(0.5f, 0.8f, 0.2f, 1.0f);
                    TopGloss = 0.1f;
                    TechniqueExt = "Masked";
                    Step = 0.1f;
                    TopScale = 10.0f;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\Grass";
                    xmlData.BotDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\dirt_crackeddrysoft_df_border";
                    xmlData.TopDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\Grass";
                    xmlData.TopDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\Grass_side";
                    xmlData.CollisionSound = Foley.CollisionSound.grass;
                    uiPriDict[matIdx] = 50f;

                    break;

                case 3:
                    Color = new Vector4(213f / 256f, 140f / 256f, 76f / 256f, 1.0f);
                    BotScale = 10.0f;
                    Gloss = 1.0f;
                    TopColor = new Vector4(0.76f, 0.7f, 0.6f, 1.0f);
                    TopGloss = 0.2f;
                    TechniqueExt = "Masked";
                    TopClamped = true;
                    TopScale = 10.0f;
                    Step = 0.35f;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\rock2 wash";
                    xmlData.BotDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\rock2 wash";
                    xmlData.TopDiffuse[0]
                        = @"Textures\BlackTransp";
                    xmlData.TopDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\cracked_diff";
                    xmlData.CollisionSound = Foley.CollisionSound.rock;
                    uiPriDict[matIdx] = 50f;
                    break;

                case 27:
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\rock2 wash";
                    xmlData.BotDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\rock2 wash";
                    Color = new Vector4(213f / 256f, 140f / 256f, 76f / 256f, 1.0f);
                    BotScale = 10.0f;
                    Gloss = 0.2f;
                    xmlData.TopDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\alphafade_white";
                    xmlData.TopDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\alphafade_white";
                    TopColor = new Vector4(0.76f, 0.7f, 0.6f, 1.0f);
                    TopGloss = 1.0f;
                    TechniqueExt = "Masked";
                    TopClamped = false;
                    TopScale = 1.0f;
                    Step = 0.0f;

                    xmlData.CollisionSound = Foley.CollisionSound.rock;
                    uiPriDict[matIdx] = 50f;
                    break;
                case 30:
                    Step = 0.25f;

                    xmlData.BotDiffuse[0]
                        = @"Textures\Black";
                    xmlData.BotDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\lava_side_2";

                    BotEmissive = Vector4.One;
                    Color = Vector4.UnitW;
                    Gloss = 0;
                    BotEmissive = new Vector4(235f / 256f, 130f / 256f, 18f / 256f, .75f);
                    Color = new Vector4(23.5f / 256f, 13.0f / 256f, 1.8f / 256f, 1.0f); // .5f);       // orange with high glow
                    Gloss = 0f;
                    BotBumpStrength = 1.0f;

                    xmlData.CollisionSound = Foley.CollisionSound.dirt;
                    uiPriDict[matIdx] = 50f;
                    break;

                case 29:                     // flat-top chamfer, brown, with top noise
                    xmlData.BotDiffuse[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\rocktop_no_bevel_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-neg-y_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Back]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-y_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Left]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-neg-x_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Right]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-x_desat";
                    BotBumpStrength = 1.0f;
                    Color = new Vector4(156f / 256f, 110f / 256f, 69f / 256f, 1.0f);
                    Gloss = .01f;
                    Step = 0.5f;

                    xmlData.CollisionSound = Foley.CollisionSound.dirt;
                    uiPriDict[matIdx] = 50f;
                    break;

                case 116:                     // moon domes
                    xmlData.BotDiffuse[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\moon_dome_2";
                    xmlData.BotDiffuse[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-neg-y_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Back]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-y_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Left]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-neg-x_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Right]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-x_desat";

                    BotScale = 4.0f;
                    Color = new Vector4(100f / 256f, 100f / 156f, 206f / 256f, 1.0f);
                    Gloss = .1f;
                    Step = 0.5f;
                    xmlData.CollisionSound = Boku.Audio.Foley.CollisionSound.rock;
                    uiPriDict[matIdx] = 50f;
                    break;
                case 117:                     // snow over dirt
                    Step = 0.25f;

                    // bottom layer is the dirt (was lava)
                    xmlData.BotDiffuse[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\slate_2";
                    xmlData.BotDiffuse[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\dark_bot_2";

                    Color = new Vector4(230f / 256f, 230f / 256f, 250f / 256f, 1.0f); // slightly blue white

                    Gloss = 0.5f;
                    BotScale = 4f;


                    xmlData.CollisionSound = Boku.Audio.Foley.CollisionSound.dirt;
                    uiPriDict[matIdx] = 50f;
                    break;

                case 118:                     // concrete block
                    xmlData.BotDiffuse[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\concrete_2";
                    BotBumpStrength = 2.0f;
                    BotScale = 5.0f;
                    //                    Color = new Vector4(228f / 256f, 178f / 256f, 124f / 256f, 1.0f);
                    Color = new Vector4(178f / 256f, 178f / 256f, 178f / 256f, 1.0f);
                    Gloss = .19f;
                    Step = 0.0f;

                    xmlData.CollisionSound = Boku.Audio.Foley.CollisionSound.rock;
                    uiPriDict[matIdx] = 50f;
                    break;
                case 119:                     // chunky cell rock
                    xmlData.BotBump[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\celstone_2";

                    BotScale = 1.0f;
                    // flesh version                    Color = new Vector4(228f / 256f, 178f / 256f, 124f / 256f, 1.0f);
                    Color = new Vector4(91f / 256f, 99f / 256f, 136f / 256f, 1.0f);
                    Gloss = .19f;
                    Step = 0.0f;

                    xmlData.CollisionSound = Boku.Audio.Foley.CollisionSound.rock;
                    uiPriDict[matIdx] = 50f;
                    break;

                case 120:                     // multibox test
                    xmlData.BotBump[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\bubbles_2";

                    BotScale = 1f;
                    Color = new Vector4(35f / 256f, 35f / 256f, 37f / 256f, 1.0f);
                    Gloss = .8f;
                    Step = 0.00f;

                    xmlData.CollisionSound = Boku.Audio.Foley.CollisionSound.rock;
                    uiPriDict[matIdx] = 50f;
                    break;

                case 115: // dirt cubes with grass on top
                    Step = 0.25f;
                    Color = new Vector4(91f / 256f, 142f / 256f, 54f / 256f, 1.0f);

                    // bottom layer is the dirt (was lava)
                    xmlData.BotDiffuse[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\slate_2";
                    xmlData.BotDiffuse[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\dark_bot_2";

                    xmlData.CollisionSound = Boku.Audio.Foley.CollisionSound.grass;
                    uiPriDict[matIdx] = 50f;
                    break;


                #endregion Realistics


                ///
                /// Realistics styles end
                /// Metal styles begin
                /// 

                #region Metals
                case 0:
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\alum_plt_desat";
                    xmlData.BotDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\alum_plt_side";
                    Color = new Vector4(0.7f, 0.7f, 0.8f, 1.0f);
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    uiPriDict[matIdx] = 40f;
                    break;

                case 6:
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\RIVROCK1_desat";
                    xmlData.BotDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\alum_plt_side";
                    Color = new Vector4(0.8f, 0.8f, 0.8f, 1.0f);
                    Gloss = 0.5f;

                    xmlData.CollisionSound = Foley.CollisionSound.rock;
                    uiPriDict[matIdx] = 40f;
                    break;
                case 7:
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\RIVROCK1_desat";
                    xmlData.BotDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\alum_plt_side";
                    Color = new Vector4(0.3f, 0.5f, 0.3f, 1.0f);
                    Gloss = 0.3f;

                    xmlData.CollisionSound = Foley.CollisionSound.rock;
                    uiPriDict[matIdx] = 40f;
                    break;
                #endregion Metals

                ///
                /// Metal styles end
                /// Decorated single cube (dotted) styles begin
                /// 

                #region Decorated Singles
                case 2:
                    Color = new Vector4(0.3f, 0.1f, 0.1f, 1.0f);
                    Gloss = 0.0f;
                    BotEmissive = new Vector4(0.8f, 0.2f, 0.2f, 0.75f);
                    TopColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
                    TopGloss = 1.0f;
                    TechniqueExt = "Masked";
                    TopClamped = false;
                    uiPriDict[matIdx] = 5f;
                    xmlData.BotDiffuse[0]
                        = @"Textures\white";
                    xmlData.BotDiffuse[1]
                        = @"Textures\white";
                    xmlData.TopDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\circle_diff";
                    xmlData.TopDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\circle_diff";

                    xmlData.CollisionSound = Foley.CollisionSound.plasticSoft;
                    uiPriDict[matIdx] = 30f;
                    break;

                case 4:
                    Color = new Vector4(0.1f, 0.15f, 0.1f, 1.0f);
                    Gloss = 0.4f;
                    TopEmissive = new Vector4(0.4f, 0.1f, 0.1f, 0.85f);
                    TopColor = new Vector4(0.8f, 0.5f, 0.5f, 1.0f);
                    TopGloss = 1.0f;
                    TechniqueExt = "Masked";
                    TopClamped = false;
                    xmlData.BotDiffuse[0]
                        = @"Textures\white";
                    xmlData.BotDiffuse[1]
                        = @"Textures\white";
                    xmlData.TopDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\circle_diff";
                    xmlData.TopDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\circle_diff";

                    xmlData.CollisionSound = Foley.CollisionSound.plasticSoft;
                    uiPriDict[matIdx] = 30f;
                    break;

                // Black plastic with glowing red dots
                case 21:
                    Color = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
                    Gloss = 0.0f;
                    BotEmissive = new Vector4(0.8f, 0.2f, 0.2f, 0.3f);
                    TopColor = new Vector4(0.1f, 0.1f, 0.1f, 1.0f);
                    TopGloss = 1.0f;
                    TechniqueExt = "Masked";
                    TopClamped = false;
                    xmlData.BotDiffuse[0]
                        = @"Textures\white";
                    xmlData.BotDiffuse[1]
                        = @"Textures\white";
                    xmlData.TopDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\circle_diff";
                    xmlData.TopDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\circle_diff";

                    xmlData.CollisionSound = Foley.CollisionSound.plasticSoft;
                    uiPriDict[matIdx] = 30f;
                    break;

                case 44:
                    Gloss = 1.0f;
                    BotScale = 1.0f;
                    Color = Vector4.One;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\C_01_2";

                    uiPriDict[matIdx] = 30f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 45:
                    Gloss = 0.3f;
                    BotScale = 1.0f;
                    Color = Vector4.One;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\C_02_2";

                    uiPriDict[matIdx] = 30f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 46:
                    Color = Vector4.One;
                    Gloss = 1.0f;
                    Color = Vector4.One;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\C_03_2";

                    uiPriDict[matIdx] = 30f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 47:
                    Gloss = 1.0f;
                    Color = Vector4.One;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\C_04_2";
                    uiPriDict[matIdx] = 30f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 48:
                    Gloss = 1.0f;
                    Color = Vector4.One;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\C_05_2";

                    uiPriDict[matIdx] = 30f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 49:
                    Gloss = 1.0f;
                    Color = Vector4.One;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\C_06_2";

                    uiPriDict[matIdx] = 30f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 50:
                    Gloss = 1.0f;
                    Color = Vector4.One;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\C_07_2";

                    uiPriDict[matIdx] = 30f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 51:
                    Gloss = 1.0f;
                    Color = Vector4.One;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\C_08_2";

                    uiPriDict[matIdx] = 30f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 52:
                    Gloss = 1.0f;
                    Color = Vector4.One;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\C_09_2";

                    uiPriDict[matIdx] = 30f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 53:
                    Gloss = 1.0f;
                    Color = Vector4.One;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\C_10_2";

                    uiPriDict[matIdx] = 30f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 54:
                    Gloss = 1.0f;
                    Color = Vector4.One;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\C_11_2";

                    uiPriDict[matIdx] = 30f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 55:
                    Gloss = 1.0f;
                    Color = Vector4.One;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\C_12_2";

                    uiPriDict[matIdx] = 30f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 56:
                    Gloss = 1.0f;
                    Color = Vector4.One;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\C_13_2";

                    uiPriDict[matIdx] = 30f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                #endregion Decorated Singles

                ///
                /// Decorated single cube (dotted) styles end
                /// Generic plastic block styles begin
                /// 
                #region Plastics

                case 25:                                                        // flat-top chamfer box - black
                    FlatTopChamfer_SM2(
                        PaletteEntry(Palette.Black),
                        Foley.CollisionSound.metalHard);
                    uiPriDict[matIdx] = 27f;
                    break;

                #region Chamfer Box
                case 43:
                    ChamferBox_SM2(
                        PaletteEntry(Palette.DarkRed),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 26f;
                    break;
                case 11:
                    ChamferBox_SM2(
                        PaletteEntry(Palette.Red),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 25f;
                    break;

                case 58:
                    ChamferBox_SM2(
                        PaletteEntry(Palette.Orange),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 24f;
                    break;

                case 59:
                    ChamferBox_SM2(
                        PaletteEntry(Palette.Yellow),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 23f;
                    break;

                case 10:
                    ChamferBox_SM2(
                        PaletteEntry(Palette.Brown),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 22f;
                    break;

                case 5:
                    ChamferBox_SM2(
                        PaletteEntry(Palette.Tan),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 21f;
                    break;

                case 9:
                    ChamferBox_SM2(
                        PaletteEntry(Palette.Green),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 20f;
                    break;

                case 60:
                    ChamferBox_SM2(
                        PaletteEntry(Palette.PaleGreen),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 19f;
                    break;


                case 61:
                    ChamferBox_SM2(
                        PaletteEntry(Palette.Blue),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 19f;
                    break;

                case 62:
                    ChamferBox_SM2(
                        PaletteEntry(Palette.PaleBlue),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 19f;
                    break;


                case 63:
                    ChamferBox_SM2(
                        PaletteEntry(Palette.Purple),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 19f;
                    break;

                case 64:
                    ChamferBox_SM2(
                        PaletteEntry(Palette.Pink),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 19f;
                    break;

                case 65:
                    ChamferBox_SM2(
                        PaletteEntry(Palette.White),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 19f;
                    break;

                case 66:
                    ChamferBox_SM2(
                        PaletteEntry(Palette.Grey),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 19f;
                    break;

                #endregion Chamfer Box

                #region Muffin

                case 67:
                    Muffin_SM2(
                        PaletteEntry(Palette.DarkRed),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 19f;
                    break;

                case 68:
                    Muffin_SM2(
                        PaletteEntry(Palette.Red),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 19f;
                    break;

                case 69:
                    Muffin_SM2(
                        PaletteEntry(Palette.Orange),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 19f;
                    break;

                case 70:
                    Muffin_SM2(
                        PaletteEntry(Palette.Yellow),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 19f;
                    break;

                case 71:
                    Muffin_SM2(
                        PaletteEntry(Palette.Brown),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 19f;
                    break;

                case 72:
                    Muffin_SM2(
                        PaletteEntry(Palette.Tan),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 19f;
                    break;

                case 8:
                    Muffin_SM2(
                        PaletteEntry(Palette.Green),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 18f;
                    break;

                case 73:
                    Muffin_SM2(
                        PaletteEntry(Palette.PaleGreen),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 18f;
                    break;

                case 74:
                    Muffin_SM2(
                        PaletteEntry(Palette.Blue),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 18f;
                    break;

                case 75:
                    Muffin_SM2(
                        PaletteEntry(Palette.PaleBlue),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 18f;
                    break;

                case 76:
                    Muffin_SM2(
                        PaletteEntry(Palette.Purple),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 18f;
                    break;

                case 77:
                    Muffin_SM2(
                        PaletteEntry(Palette.Pink),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 18f;
                    break;

                case 78:
                    Muffin_SM2(
                        PaletteEntry(Palette.White),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 18f;
                    break;

                case 79:
                    Muffin_SM2(
                        PaletteEntry(Palette.Grey),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 18f;
                    break;

                #endregion Muffin

                #region Hewn

                case 80:
                    Hewn_SM2(
                        PaletteEntry(Palette.DarkRed),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 18f;
                    break;

                case 81:
                    Hewn_SM2(
                        PaletteEntry(Palette.Red),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 18f;
                    break;

                case 82:
                    Hewn_SM2(
                        PaletteEntry(Palette.Orange),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 18f;
                    break;

                case 83:
                    Hewn_SM2(
                        PaletteEntry(Palette.Yellow),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 18f;
                    break;

                case 12:
                    Hewn_SM2(
                        PaletteEntry(Palette.Brown),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 17f;
                    break;

                case 84:
                    Hewn_SM2(
                        PaletteEntry(Palette.Tan),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 17f;
                    break;

                case 85:
                    Hewn_SM2(
                        PaletteEntry(Palette.Green),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 17f;
                    break;

                case 86:
                    Hewn_SM2(
                        PaletteEntry(Palette.PaleGreen),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 17f;
                    break;

                case 87:
                    Hewn_SM2(
                        PaletteEntry(Palette.Blue),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 17f;
                    break;

                case 88:
                    Hewn_SM2(
                        PaletteEntry(Palette.PaleBlue),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 17f;
                    break;

                case 89:
                    Hewn_SM2(
                        PaletteEntry(Palette.Purple),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 17f;
                    break;

                case 90:
                    Hewn_SM2(
                        PaletteEntry(Palette.Pink),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 17f;
                    break;

                case 91:
                    Hewn_SM2(
                        PaletteEntry(Palette.White),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 17f;
                    break;

                case 92:
                    Hewn_SM2(
                        PaletteEntry(Palette.Black),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 17f;
                    break;
                #endregion Hewn

                #region Bubble
                case 93:                     // bubble turf
                    BubbleTurf_SM2(
                        PaletteEntry(Palette.DarkRed),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 94:                     // bubble turf
                    BubbleTurf_SM2(
                        PaletteEntry(Palette.Red),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 95:                     // bubble turf
                    BubbleTurf_SM2(
                        PaletteEntry(Palette.Orange),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 96:                     // bubble turf
                    BubbleTurf_SM2(
                        PaletteEntry(Palette.Yellow),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 97:                     // bubble turf
                    BubbleTurf_SM2(
                        PaletteEntry(Palette.Brown),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 98:                     // bubble turf
                    BubbleTurf_SM2(
                        PaletteEntry(Palette.Tan),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 99:                     // bubble turf
                    BubbleTurf_SM2(
                        PaletteEntry(Palette.Green),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 100:                     // bubble turf
                    BubbleTurf_SM2(
                        PaletteEntry(Palette.PaleGreen),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 101:                     // bubble turf
                    BubbleTurf_SM2(
                        PaletteEntry(Palette.Blue),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 102:                     // bubble turf
                    BubbleTurf_SM2(
                        PaletteEntry(Palette.PaleBlue),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 103:                     // bubble turf
                    BubbleTurf_SM2(
                        PaletteEntry(Palette.Purple),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 104:                     // bubble turf
                    BubbleTurf_SM2(
                        PaletteEntry(Palette.Pink),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 105:                     // bubble turf
                    BubbleTurf_SM2(
                        PaletteEntry(Palette.White),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 106:                     // bubble turf
                    BubbleTurf_SM2(
                        PaletteEntry(Palette.Grey),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                #endregion Bubble

                #region FlatTop Chamfer
                case 107:                                                        // flat-top chamfer box - brown
                    FlatTopChamfer_SM2(
                        PaletteEntry(Palette.DarkRed),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 108:                                                        // flat-top chamfer box - brown
                    FlatTopChamfer_SM2(
                        PaletteEntry(Palette.Red),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 109:                                                        // flat-top chamfer box - brown
                    FlatTopChamfer_SM2(
                        PaletteEntry(Palette.Orange),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 110:                                                        // flat-top chamfer box - brown
                    FlatTopChamfer_SM2(
                        PaletteEntry(Palette.Yellow),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 24:                                                        // flat-top chamfer box - brown
                    FlatTopChamfer_SM2(
                        PaletteEntry(Palette.Brown),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 15f;
                    break;


                case 111:                                                        // flat-top chamfer box - brown
                    FlatTopChamfer_SM2(
                        PaletteEntry(Palette.Tan),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 15f;
                    break;

                case 23:                                                        // flat-top chamfer box - green
                    FlatTopChamfer_SM2(
                        PaletteEntry(Palette.Green),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 14f;
                    break;
                case 26:                     // side overhang, sameless top and left/right
                    FlatTopChamfer_SM2(
                        PaletteEntry(Palette.PaleGreen),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 13f;
                    break;

                case 112:                                                        // flat-top chamfer box - white
                    FlatTopChamfer_SM2(
                        PaletteEntry(Palette.Blue),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 13f;
                    break;

                case 13:                                                        // flat-top chamfer box - white
                    FlatTopChamfer_SM2(
                        PaletteEntry(Palette.PaleBlue),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 12f;
                    break;

                case 14:                                                        // flat-top chamfer box - white
                    FlatTopChamfer_SM2(
                        PaletteEntry(Palette.Purple),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 12f;
                    break;

                case 17:                                                        // flat-top chamfer box - white
                    FlatTopChamfer_SM2(
                        PaletteEntry(Palette.Pink),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 12f;
                    break;

                case 57:                                                        // flat-top chamfer box - white
                    FlatTopChamfer_SM2(
                        PaletteEntry(Palette.White),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 12f;
                    break;

                case 22:                                                        // flat-top chamfer box - white
                    FlatTopChamfer_SM2(
                        PaletteEntry(Palette.Grey),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 11f;
                    break;
                #endregion FlatTop Chamfer

                #endregion Plastics

                ///
                /// Generic plastic block styles end
                /// Glowing cubes styles begin
                /// 
                #region Glowing
                // Glowing orange (yellow plus diffuse red)
                case 16:
                    GlowCube_SM2(new Vector4(1f, 0f, 0f, 1.0f),
                        new Vector4(.5f, .5f, 0f, 0.8f));
                    break;
                // Glowing blue 
                case 18:
                    GlowCube_SM2(new Vector4(84f / 256f, 197f / 256f, 256f, 1.0f),
                        new Vector4(42f / 256f, 96f / 256f, 128f, 0.9f));
                    break;
                // Glowing red
                case 19:
                    GlowCube_SM2(new Vector4(1f, .1f, 0f, 1.0f),
                        new Vector4(0.5f, .05f, 0f, 0.9f));
                    break;
                // Glowing purple
                case 20:
                    GlowCube_SM2(new Vector4(0, 0, 1.0f, 1.0f),
                        new Vector4(.4f, 0f, 0f, 0.75f));
                    break;

                // Glowing white
                case 113:
                    GlowCube_SM2(new Vector4(0.5f, 0.5f, 0.5f, 1.0f),
                        new Vector4(0.0f, 0.1f, 0.5f, 0.75f));
                    break;

                case 114:
                    GlowCube_SM2(new Vector4(0.5f, 0, 0, 1.0f),
                        new Vector4(0, 0.5f, 0, 0.75f));
                    break;

                #endregion Glowing

                ///
                /// Glowing cubes styles end
                /// 

                //ToDo (DZ): Do we need this?
                //case TerrainMaterial.MaxNum:
                //    xmlData.BotDiffuse[(int)Tile.Face.Top]
                //        = @"Textures\white";
                //    xmlData.BotDiffuse[1] = xmlData.BotDiffuse[0];
                //    Color = new Vector4(1.0f, 1.0f, 0.5f, 1.0f);
                //    Gloss = 0.5f;
                //    uiPriDict[matIdx] = Single.MinValue; // Ensure this stays at the end of the queue.
                //    break;
                default:
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\RIVROCK1_desat";
                    xmlData.BotDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\alum_plt_side";
                    Color = new Vector4(0.3f, 0.3f, 0.3f, 1.0f);
                    Gloss = 1.0f;
                    uiPriDict[matIdx] = -1.0f;
                    break;
            }
        }

        /// <summary>
        /// Load up some plausible SM2 materials.
        /// Eventually these will come from xml.
        /// </summary>
        /// <param name="matIdx"></param>
        protected void InitSM2_0(ushort matIdx)
        {
            switch (matIdx)
            {
                case 0:
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\alum_plt_desat";
                    xmlData.BotDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\alum_plt_side";
                    Color = new Vector4(0.7f, 0.7f, 0.8f, 1.0f);
                    break;
                case 1:
                    Color = new Vector4(0.9f, 0.65f, 0.04f, 1.0f);
                    Gloss = 0.4f;
                    TopColor = new Vector4(0.5f, 0.8f, 0.2f, 1.0f);
                    TopGloss = 0.1f;
                    TechniqueExt = "Masked";
                    Step = 0.1f;
                    TopScale = 10.0f;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\Grass";
                    xmlData.BotDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\dirt_crackeddrysoft_df_border";
                    xmlData.TopDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\Grass";
                    xmlData.TopDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\Grass_side";
                    break;
                case 2:
                    Color = new Vector4(0.3f, 0.1f, 0.1f, 1.0f);
                    Gloss = 0.0f;
                    BotEmissive = new Vector4(0.8f, 0.2f, 0.2f, 0.75f);
                    TopColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
                    TopGloss = 1.0f;
                    TechniqueExt = "Masked";
                    TopClamped = false;
                    uiPriDict[matIdx] = 5f;
                    xmlData.BotDiffuse[0]
                        = @"Textures\white";
                    xmlData.BotDiffuse[1]
                        = @"Textures\white";
                    xmlData.TopDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\circle_diff";
                    xmlData.TopDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\circle_diff";
                    break;
                case 3:
                    Color = new Vector4(213f / 256f, 140f / 256f, 76f / 256f, 1.0f);
                    BotScale = 10.0f;
                    Gloss = 1.0f;
                    TopColor = new Vector4(0.76f, 0.7f, 0.6f, 1.0f);
                    TopGloss = 0.2f;
                    TechniqueExt = "Masked";
                    TopClamped = true;
                    TopScale = 10.0f;
                    Step = 0.35f;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\rock2 wash";
                    xmlData.BotDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\rock2 wash";
                    xmlData.TopDiffuse[0]
                        = @"Textures\BlackTransp";
                    xmlData.TopDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\cracked_diff";
                    break;
                case 4:
                    Color = new Vector4(0.1f, 0.15f, 0.1f, 1.0f);
                    Gloss = 0.4f;
                    TopEmissive = new Vector4(0.4f, 0.1f, 0.1f, 0.85f);
                    TopColor = new Vector4(0.8f, 0.5f, 0.5f, 1.0f);
                    TopGloss = 1.0f;
                    TechniqueExt = "Masked";
                    TopClamped = false;
                    xmlData.BotDiffuse[0]
                        = @"Textures\white";
                    xmlData.BotDiffuse[1]
                        = @"Textures\white";
                    xmlData.TopDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\circle_diff";
                    xmlData.TopDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\circle_diff";
                    break;
                case 5:                     // trying new bump maps
                    xmlData.BotDiffuse[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\chamfer_box_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\chamfer_box_desat";
                    Color = new Vector4(142f / 256f, 121f / 256f, 98f / 256f, 1.0f);
                    Gloss = .1f;
                    // BotBumpStrength = 2.0f;
                    break;
                case 6:
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\RIVROCK1_desat";
                    xmlData.BotDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\alum_plt_side";
                    Color = new Vector4(0.8f, 0.8f, 0.8f, 1.0f);
                    Gloss = 0.5f;
                    break;
                case 7:
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\RIVROCK1_desat";
                    xmlData.BotDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\alum_plt_side";
                    Color = new Vector4(0.3f, 0.5f, 0.3f, 1.0f);
                    Gloss = 0.3f;
                    break;
                case 8:
                    xmlData.BotDiffuse[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\muffin-norm-pos-z_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\muffin-norm-neg-y_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Back]
                        = @"Textures\Terrain\GroundTextures\muffin-norm-pos-y_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Left]
                        = @"Textures\Terrain\GroundTextures\muffin-norm-neg-x_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Right]
                        = @"Textures\Terrain\GroundTextures\muffin-norm-pos-x_desat";

                    Color = new Vector4(114f / 256f, 181f / 256f, 66f / 256f, 1.0f);
                    Gloss = .1f;
                    BotBumpStrength = 1.0f;
                    Step = 0.25f;
                    break;
                case 9:
                    xmlData.BotDiffuse[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\chamfer_box_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\chamfer_box_desat";
                    Color = new Vector4(114f / 256f, 181f / 256f, 66f / 256f, 1.0f);
                    Gloss = .0f;
                    BotBumpStrength = 1.0f;
                    Step = 0.25f;
                    break;
                case 10:
                    xmlData.BotDiffuse[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\chamfer_box_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\chamfer_box_desat";
                    Color = new Vector4(152f / 256f, 101f / 256f, 13f / 256f, 1.0f);
                    Gloss = .0f;
                    BotBumpStrength = 1.0f;
                    break;
                case 11:                     // trying new bump maps
                    xmlData.BotDiffuse[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\chamfer_box_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\chamfer_box_desat";
                    Color = new Vector4(229f / 256f, 157f / 256f, 221f / 256f, 1.0f);
                    Gloss = .0f;
                    BotBumpStrength = 1.0f;
                    break;
                case 12:                     // trying new bump maps
                    xmlData.BotDiffuse[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\hewn-pos-z_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\hewn-neg-y_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Back]
                        = @"Textures\Terrain\GroundTextures\hewn-pos-y_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Right]
                        = @"Textures\Terrain\GroundTextures\hewn-pos-x_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Left]
                        = @"Textures\Terrain\GroundTextures\hewn-neg-x_desat";
                    Color = new Vector4(142f / 256f, 121f / 256f, 98f / 256f, 1.0f);
                    Gloss = .5f;
                    BotBumpStrength = 1.0f;
                    break;
                case 14:                     // bubble turf
                    xmlData.BotDiffuse[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\bubble-norm-pos-z_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\bubble-norm-neg-y_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Back]
                        = @"Textures\Terrain\GroundTextures\bubble-norm-pos-y_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Left]
                        = @"Textures\Terrain\GroundTextures\bubble-norm-neg-x_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Right]
                        = @"Textures\Terrain\GroundTextures\bubble-norm-pos-x_desat";

                    Color = new Vector4(114f / 256f, 181f / 256f, 66f / 256f, 1.0f);
                    Gloss = .1f;
                    BotBumpStrength = 1.0f;
                    break;
                case 15:                     // bubble turf
                    xmlData.BotDiffuse[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\alphabet_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\alphabet_desat";

                    Color = new Vector4(197f / 256f, 183f / 256f, 162f / 256f, 1.0f);
                    Gloss = .3f;
                    BotScale = 4.0f;
                    uiPriDict[matIdx] = 3f;
                    break;
                // Glowing yellow plastic with red shadows
                case 16:
                    xmlData.BotDiffuse[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\chamfer_box_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\chamfer_box_desat";
                    Gloss = .5f;
                    this.BotEmissive = new Vector4(.5f, .5f, 0f, 0.8f);
                    //                        this.TopEmissive = new Vector3(248f / 256f, 241f / 256f, 60f / 256f);
                    Color = new Vector4(1f, 0f, 0f, 1.0f);
                    //                        Color = new Vector4(248f / 256f, 241f / 256f, 60f / 256f, 1.0f);
                    break;
                // Glowing blue 
                case 18:
                    xmlData.BotDiffuse[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\chamfer_box_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\chamfer_box_desat";
                    Gloss = .2f;
                    this.BotEmissive = new Vector4(42f / 256f, 96f / 256f, 128f, 0.9f);
                    Color = new Vector4(84f / 256f, 197f / 256f, 256f, 1.0f);
                    break;
                // Glowing red
                case 19:
                    xmlData.BotDiffuse[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\chamfer_box_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\chamfer_box_desat";
                    Gloss = .2f;
                    this.BotEmissive = new Vector4(0.5f, .05f, 0f, 0.9f);
                    Color = new Vector4(1f, .1f, 0f, 1.0f);
                    break;
                // Glowing pink plastic
                case 20:
                    xmlData.BotDiffuse[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\chamfer_box_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\chamfer_box_desat";
                    Gloss = 1f;
                    this.BotEmissive = new Vector4(.4f, 0f, 0f, 0.75f);
                    //                        this.TopEmissive = new Vector3(248f / 256f, 241f / 256f, 60f / 256f);
                    Color = new Vector4(0f, 0f, 1f, 1.0f);
                    //                        Color = new Vector4(248f / 256f, 241f / 256f, 60f / 256f, 1.0f);
                    break;
                case 21:
                    Color = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
                    Gloss = 0.0f;
                    BotEmissive = new Vector4(0.8f, 0.2f, 0.2f, 0.3f);
                    TopColor = new Vector4(0.1f, 0.1f, 0.1f, 1.0f);
                    TopGloss = 1.0f;
                    TechniqueExt = "Masked";
                    TopClamped = false;
                    xmlData.BotDiffuse[0]
                        = @"Textures\white";
                    xmlData.BotDiffuse[1]
                        = @"Textures\white";
                    xmlData.TopDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\circle_diff";
                    xmlData.TopDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\circle_diff";
                    break;
                case 22:                                                        // flat-top chamfer box - white
                    xmlData.BotDiffuse[(int)Tile.Face.Top]
                        = @"Textures\white";
                    xmlData.BotDiffuse[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-neg-y_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Back]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-y_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Left]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-neg-x_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Right]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-x_desat";

                    Color = new Vector4(220f / 256f, 220f / 256f, 220f / 256f, 1.0f);
                    Gloss = .15f;
                    Step = 0.5f;
                    break;
                case 23:                                                        // flat-top chamfer box - green
                    xmlData.BotDiffuse[(int)Tile.Face.Top]
                        = @"Textures\white";
                    xmlData.BotDiffuse[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-neg-y_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Back]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-y_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Left]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-neg-x_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Right]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-x_desat";

                    Color = new Vector4(114f / 256f, 181f / 256f, 66f / 256f, 1.0f);
                    Gloss = .05f;
                    Step = 0.5f;
                    break;
                case 24:                                                        // flat-top chamfer box - brown
                    xmlData.BotDiffuse[(int)Tile.Face.Top]
                        = @"Textures\white";
                    xmlData.BotDiffuse[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-neg-y_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Back]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-y_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Left]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-neg-x_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Right]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-x_desat";

                    Color = new Vector4(156f / 256f, 110f / 256f, 69f / 256f, 1.0f);
                    Gloss = .05f;
                    Step = 0.5f;
                    break;
                case 25:                                                        // flat-top chamfer box - black
                    xmlData.BotDiffuse[(int)Tile.Face.Top]
                        = @"Textures\white";
                    xmlData.BotDiffuse[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-neg-y_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Back]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-y_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Left]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-neg-x_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Right]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-x_desat";

                    Color = new Vector4(20f / 256f, 20f / 256f, 20f / 256f, 1.0f);
                    Gloss = .15f;
                    Step = 0.5f;
                    break;
                case 26:                     // side overhang, sameless top and left/right
                    xmlData.BotDiffuse[(int)Tile.Face.Top]
                        = @"Textures\white";
                    xmlData.BotDiffuse[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\overhang-side-normal_desat";

                    Color = new Vector4(114f / 256f, 181f / 256f, 66f / 256f, 1.0f);
                    Gloss = .05f;
                    Step = 1.0f;
                    break;
                case 27:
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\rock2 wash";
                    xmlData.BotDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\rock2 wash";
                    Color = new Vector4(213f / 256f, 140f / 256f, 76f / 256f, 1.0f);
                    BotScale = 10.0f;
                    Gloss = 0.2f;
                    xmlData.TopDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\alphafade_white";
                    xmlData.TopDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\alphafade_white";
                    TopColor = new Vector4(0.76f, 0.7f, 0.6f, 1.0f);
                    TopGloss = 1.0f;
                    TechniqueExt = "Masked";
                    TopClamped = false;
                    TopScale = 1.0f;
                    Step = 0.0f;
                    uiPriDict[matIdx] = 4f;
                    break;
                case 28:
                    Color = new Vector4(111.0f / 255.9f, 254.0f / 255.9f, 171.0f / 255.9f, 1.0f);
                    Gloss = 0.3f;
                    TopColor = new Vector4(58.0f / 255.9f, 230.0f / 255.9f, 191.0f / 255.9f, 1.0f);
                    TopGloss = 0.1f;
                    TechniqueExt = "Masked";
                    TopScale = 30.0f;
                    BotScale = 5.0f;
                    TopClamped = false;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\op_terrain_pattern_sm2";
                    xmlData.BotDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\op_terrain_pattern_sm2";
                    xmlData.TopDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\op_terrain_pattern_sm2";
                    xmlData.TopDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\op_terrain_pattern_sm2";
                    uiPriDict[matIdx] = 200f;
                    break;
                case 29:                     // flat-top chamfer, brown, with top noise
                    xmlData.BotDiffuse[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\rocktop_no_bevel_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-neg-y_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Back]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-y_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Left]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-neg-x_desat";
                    xmlData.BotDiffuse[(int)Tile.Face.Right]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-x_desat";
                    BotBumpStrength = 1.0f;
                    Color = new Vector4(156f / 256f, 110f / 256f, 69f / 256f, 1.0f);
                    Gloss = .01f;
                    Step = 0.5f;
                    break;


                //ToDo (DZ): Do we need this?
                //case TerrainMaterial.MaxNum:
                //    xmlData.BotDiffuse[(int)Tile.Face.Top]
                //        = @"Textures\white";
                //    xmlData.BotDiffuse[1] = xmlData.BotDiffuse[0];
                //    Color = new Vector4(1.0f, 1.0f, 0.5f, 1.0f);
                //    Gloss = 0.5f;
                //    uiPriDict[matIdx] = Single.MinValue; // Ensure this stays at the end of the queue.
                //    break;
                default:
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\RIVROCK1_desat";
                    xmlData.BotDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\alum_plt_side";
                    Color = new Vector4(0.3f, 0.3f, 0.3f, 1.0f);
                    Gloss = 1.0f;
                    uiPriDict[matIdx] = -1.0f;
                    break;
            }
        }
        private void ChamferBox_SM2(Vector4 color, Foley.CollisionSound sound)
        {
            xmlData.BotDiffuse[(int)Tile.Face.Top]
                = @"Textures\Terrain\GroundTextures\chamfer_box_desat";
            xmlData.BotDiffuse[(int)Tile.Face.Front]
                = @"Textures\Terrain\GroundTextures\chamfer_box_desat";

            Step = 0.25f;
            Gloss = .0f;
            BotBumpStrength = 1.0f;

            Color = color;
            xmlData.CollisionSound = sound;
        }
        private void Muffin_SM2(Vector4 color, Foley.CollisionSound sound)
        {
            xmlData.BotDiffuse[(int)Tile.Face.Top]
                = @"Textures\Terrain\GroundTextures\muffin-norm-pos-z_desat";
            xmlData.BotDiffuse[(int)Tile.Face.Front]
                = @"Textures\Terrain\GroundTextures\muffin-norm-neg-y_desat";
            xmlData.BotDiffuse[(int)Tile.Face.Back]
                = @"Textures\Terrain\GroundTextures\muffin-norm-pos-y_desat";
            xmlData.BotDiffuse[(int)Tile.Face.Left]
                = @"Textures\Terrain\GroundTextures\muffin-norm-neg-x_desat";
            xmlData.BotDiffuse[(int)Tile.Face.Right]
                = @"Textures\Terrain\GroundTextures\muffin-norm-pos-x_desat";

            Gloss = .1f;
            BotBumpStrength = 1.0f;
            Step = 0.25f;

            Color = color;
            xmlData.CollisionSound = sound;
        }
        private void Hewn_SM2(Vector4 color, Foley.CollisionSound sound)
        {
            xmlData.BotDiffuse[(int)Tile.Face.Top]
                = @"Textures\Terrain\GroundTextures\hewn-pos-z_desat";
            xmlData.BotDiffuse[(int)Tile.Face.Front]
                = @"Textures\Terrain\GroundTextures\hewn-neg-y_desat";
            xmlData.BotDiffuse[(int)Tile.Face.Back]
                = @"Textures\Terrain\GroundTextures\hewn-pos-y_desat";
            xmlData.BotDiffuse[(int)Tile.Face.Right]
                = @"Textures\Terrain\GroundTextures\hewn-pos-x_desat";
            xmlData.BotDiffuse[(int)Tile.Face.Left]
                = @"Textures\Terrain\GroundTextures\hewn-neg-x_desat";

            Color = color;
            Gloss = .5f;
            BotBumpStrength = 1.0f;
            xmlData.CollisionSound = sound;
        }
        private void BubbleTurf_SM2(Vector4 color, Foley.CollisionSound sound)
        {
            xmlData.BotDiffuse[(int)Tile.Face.Top]
                = @"Textures\Terrain\GroundTextures\bubble-norm-pos-z_desat";
            xmlData.BotDiffuse[(int)Tile.Face.Front]
                = @"Textures\Terrain\GroundTextures\bubble-norm-neg-y_desat";
            xmlData.BotDiffuse[(int)Tile.Face.Back]
                = @"Textures\Terrain\GroundTextures\bubble-norm-pos-y_desat";
            xmlData.BotDiffuse[(int)Tile.Face.Left]
                = @"Textures\Terrain\GroundTextures\bubble-norm-neg-x_desat";
            xmlData.BotDiffuse[(int)Tile.Face.Right]
                = @"Textures\Terrain\GroundTextures\bubble-norm-pos-x_desat";

            Gloss = .1f;
            BotBumpStrength = 1.0f;

            Color = color;
            xmlData.CollisionSound = sound;
        }
        private void FlatTopChamfer_SM2(Vector4 color, Foley.CollisionSound sound)
        {
            xmlData.BotDiffuse[(int)Tile.Face.Top]
                = @"Textures\white";
            xmlData.BotDiffuse[(int)Tile.Face.Front]
                = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-neg-y_desat";
            xmlData.BotDiffuse[(int)Tile.Face.Back]
                = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-y_desat";
            xmlData.BotDiffuse[(int)Tile.Face.Left]
                = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-neg-x_desat";
            xmlData.BotDiffuse[(int)Tile.Face.Right]
                = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-x_desat";

            BotBumpStrength = 1.0f;
            Gloss = .01f;
            Step = 0.5f;

            Color = color;
            xmlData.CollisionSound = sound;
        }
        private void GlowCube_SM2(Vector4 color, Vector4 emissive)
        {
            xmlData.BotDiffuse[(int)Tile.Face.Top]
                = @"Textures\Terrain\GroundTextures\chamfer_box_desat";
            xmlData.BotDiffuse[(int)Tile.Face.Front]
                = @"Textures\Terrain\GroundTextures\chamfer_box_desat";
            Gloss = 1f;
            this.BotEmissive = emissive;
            Color = color;
            xmlData.CollisionSound = Foley.CollisionSound.plasticSoft;
        }

        /// <summary>
        /// Load up some default plausible SM3 materials.
        /// Eventually these will come from XML.
        /// </summary>
        /// <param name="matIdx"></param>
        protected void InitSM3(ushort matIdx)
        {
            switch (matIdx)
            {
                #region Print Styles
                ///
                /// Print styles begin
                case 28:
                    Color = new Vector4(111.0f / 255.9f, 254.0f / 255.9f, 171.0f / 255.9f, 1.0f);
                    Gloss = 0.3f;
                    TopColor = new Vector4(58.0f / 255.9f, 230.0f / 255.9f, 191.0f / 255.9f, 1.0f);
                    TopGloss = 0.1f;
                    TechniqueExt = "Masked";
                    TopScale = 30.0f;
                    BotScale = 5.0f;
                    BotBumpStrength = 0.5f;
                    TopBumpStrength = 0.15f;
                    TopClamped = false;
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\grass_norm_top";
                    xmlData.BotBump[1]
                        = @"Textures\EggDetail1";
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\op_terrain_pattern";
                    xmlData.TopBump[1]
                        = @"Textures\EggDetail1";
                    uiPriDict[matIdx] = 200f;
                    xmlData.CollisionSound = Foley.CollisionSound.grass;
                    break;

                case 31:
                    Color = new Vector4(55.0f / 255.9f, 194.0f / 255.9f, 213.0f / 255.9f, 1.0f);
                    Gloss = 0.1f;
                    TopColor = new Vector4(204.0f / 255.9f, 255.0f / 255.9f, 222.0f / 255.9f, 1.0f);
                    TopGloss = 0.8f;
                    TechniqueExt = "Masked";
                    TopScale = 30.0f;
                    BotScale = 20.0f;
                    BotBumpStrength = 0.25f;
                    TopBumpStrength = 1.0f;
                    TopClamped = false;
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\B_Chrome";
                    xmlData.BotBump[1]
                        = @"Textures\EggDetail1";
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\op_terrain_pattern3";
                    xmlData.TopBump[1]
                        = @"Textures\EggDetail1";
                    uiPriDict[matIdx] = 200f;
                    xmlData.CollisionSound = Foley.CollisionSound.grass;
                    break;

                case 32:
                    Color = new Vector4(250.0f / 255.9f, 103.0f / 255.9f, 74.0f / 255.9f, 1.0f);
                    Gloss = 0.3f;
                    TopColor = new Vector4(251.0f / 255.9f, 187.0f / 255.9f, 76.0f / 255.9f, 1.0f);
                    TopGloss = 0.1f;
                    TechniqueExt = "Masked";
                    TopScale = 30.0f;
                    BotScale = 20.0f;
                    BotBumpStrength = 0.25f;
                    TopBumpStrength = 1.0f;
                    TopClamped = false;
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\B_NotePaper";
                    xmlData.BotBump[1]
                        = @"Textures\DistortionField";
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\op_terrain_pattern2";
                    xmlData.TopBump[1]
                        = @"Textures\DistortionField";
                    uiPriDict[matIdx] = 200f;
                    xmlData.CollisionSound = Foley.CollisionSound.grass;
                    break;

                case 33:
                    Color = new Vector4(233.0f / 255.9f, 248.0f / 255.9f, 107.0f / 255.9f, 1.0f);
                    Gloss = 1.0f;
                    TopColor = new Vector4(249.0f / 255.9f, 139.0f / 255.9f, 214.0f / 255.9f, 1.0f);
                    TopGloss = 0.1f;
                    TechniqueExt = "Masked";
                    TopScale = 30.0f;
                    BotScale = 20.0f;
                    BotBumpStrength = 0.05f;
                    TopBumpStrength = 1.0f;
                    TopClamped = false;
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\B_Crystal";
                    xmlData.BotBump[1]
                        = @"Textures\DistortionWake";
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\op_terrain_pattern3";
                    xmlData.TopBump[1]
                        = @"Textures\DistortionWake";
                    uiPriDict[matIdx] = 200f;
                    xmlData.CollisionSound = Foley.CollisionSound.grass;
                    break;

                case 34:
                    Color = new Vector4(240.0f / 255.9f, 90.0f / 255.9f, 40.0f / 255.9f, 1.0f);
                    Gloss = 1.0f;
                    TopColor = new Vector4(246.0f / 255.9f, 147.0f / 255.9f, 30.0f / 255.9f, 1.0f);
                    TopGloss = 0.1f;
                    TechniqueExt = "Masked";
                    BotScale = 20.0f;
                    TopScale = 30.0f;
                    BotBumpStrength = 0.1f;
                    TopBumpStrength = 0.5f;
                    TopClamped = false;
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\B_NotePaper";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\B_NotePaper";
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\C_Floral_Note";
                    xmlData.TopBump[1]
                        = @"Textures\Terrain\GroundTextures\B_NotePaper";
                    uiPriDict[matIdx] = 200f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;
                case 35:
                    Color = new Vector4(240.0f / 255.9f, 90.0f / 255.9f, 40.0f / 255.9f, 1.0f);
                    Gloss = 1.0f;
                    TopColor = new Vector4(217.0f / 255.9f, 28.0f / 255.9f, 92.0f / 255.9f, 1.0f);
                    TopGloss = 0.1f;
                    TechniqueExt = "Masked";
                    BotScale = 20.0f;
                    TopScale = 30.0f;
                    BotBumpStrength = 0.1f;
                    TopBumpStrength = 0.5f;
                    TopClamped = false;
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\B_NotePaper";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\B_NotePaper";
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\C_Floral_Chrome";
                    xmlData.TopBump[1]
                        = @"Textures\BlackTransp";
                    uiPriDict[matIdx] = 200f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;
                case 36:
                    Color = new Vector4(0.0f / 255.9f, 23.0f / 255.9f, 31.0f / 255.9f, 1.0f);
                    Gloss = 5.0f;
                    TopColor = new Vector4(144.0f / 255.9f, 214.0f / 255.9f, 226.0f / 255.9f, 1.0f);
                    TopGloss = 0.1f;
                    TechniqueExt = "Masked";
                    BotScale = 20.0f;
                    TopScale = 30.0f;
                    BotBumpStrength = 0.1f;
                    TopBumpStrength = 0.1f;
                    TopClamped = false;
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\B_NotePaper";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\B_NotePaper";
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\C_Floral_Note";
                    xmlData.TopBump[1]
                        = @"Textures\Terrain\GroundTextures\B_NotePaper";
                    uiPriDict[matIdx] = 200f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 37:
                    Color = new Vector4(2.0f / 255.9f, 171.0f / 255.9f, 236.0f / 255.9f, 1.0f);
                    Gloss = 5.0f;
                    TopColor = new Vector4(218.0f / 255.9f, 238.0f / 255.9f, 229.0f / 255.9f, 1.0f);
                    TopGloss = 0.1f;
                    TechniqueExt = "Masked";
                    BotScale = 20.0f;
                    TopScale = 30.0f;
                    BotBumpStrength = 0.1f;
                    TopBumpStrength = 0.5f;
                    TopClamped = false;
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\B_SmallCubes";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\B_SmallCubes";
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\C_Honeycomb_Cube";
                    xmlData.TopBump[1]
                        = @"Textures\Terrain\GroundTextures\B_SmallCubes";
                    uiPriDict[matIdx] = 200f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;
                case 38:
                    Color = new Vector4(255.9f / 255.9f, 255.9f / 255.9f, 255.9f / 255.9f, 1.0f);
                    Gloss = 1.0f;
                    TopColor = Vector4.UnitW;
                    TopEmissive = new Vector4(255.0f / 255.9f, 241.0f / 255.9f, 0.0f / 255.9f, 0.7f);
                    TopGloss = 0.8f;
                    TechniqueExt = "Masked";
                    BotScale = 20.0f;
                    TopScale = 30.0f;
                    BotBumpStrength = 0.1f;
                    TopBumpStrength = 0.5f;
                    TopClamped = false;
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\B_NotePaper";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\B_NotePaper";
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\C_Honeycomb_Note";
                    xmlData.TopBump[1]
                        = @"Textures\BlackTransp";
                    uiPriDict[matIdx] = 200f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;
                case 39:
                    Color = new Vector4(204.0f / 255.9f, 255.9f / 255.9f, 222.0f / 255.9f, 1.0f);
                    Gloss = 1.0f;
                    TopColor = new Vector4(0.0f / 255.9f, 173.0f / 255.9f, 239.0f / 255.9f, 1.0f);
                    TopGloss = 0.1f;
                    TechniqueExt = "Masked";
                    BotScale = 20.0f;
                    TopScale = 30.0f;
                    BotBumpStrength = 0.1f;
                    TopBumpStrength = 0.1f;
                    TopClamped = false;
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\B_NotePaper";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\B_NotePaper";
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\C_Honeycomb_Cube";
                    xmlData.TopBump[1]
                        = @"Textures\Terrain\GroundTextures\B_NotePaper";
                    uiPriDict[matIdx] = 200f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 40:
                    Color = new Vector4(2.0f / 255.9f, 171.0f / 255.9f, 236.0f / 255.9f, 1.0f);
                    Gloss = 5.0f;
                    TopColor = new Vector4(218.0f / 255.9f, 238.0f / 255.9f, 229.0f / 255.9f, 1.0f);
                    TopGloss = 0.1f;
                    TechniqueExt = "Masked";
                    BotScale = 20.0f;
                    TopScale = 30.0f;
                    BotBumpStrength = 0.1f;
                    TopBumpStrength = 0.5f;
                    TopClamped = false;
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\B_Chrome";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\B_Chrome";
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\C_Jagged_Chrome";
                    xmlData.TopBump[1]
                        = @"Textures\Terrain\GroundTextures\B_Chrome";
                    uiPriDict[matIdx] = 200f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;
                case 41:
                    Color = new Vector4(38.0f / 255.9f, 41.0f / 255.9f, 122.0f / 255.9f, 1.0f);
                    Gloss = 1.0f;
                    TopColor = new Vector4(191.0f / 255.9f, 30.0f / 255.9f, 46.0f / 255.9f, 1.0f);
                    TopGloss = 0.1f;
                    TechniqueExt = "Masked";
                    BotScale = 20.0f;
                    TopScale = 30.0f;
                    BotBumpStrength = 0.1f;
                    TopBumpStrength = 0.1f;
                    TopClamped = false;
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\B_Crystal";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\B_Crystal";
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\C_Jagged_Crystal";
                    xmlData.TopBump[1]
                        = @"Textures\Terrain\GroundTextures\B_Crystal";
                    uiPriDict[matIdx] = 200f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;
                case 42:
                    Color = new Vector4(204.0f / 255.9f, 255.9f / 255.9f, 222.0f / 255.9f, 1.0f);
                    Gloss = 1.0f;
                    TopColor = new Vector4(0.0f / 255.9f, 173.0f / 255.9f, 239.0f / 255.9f, 1.0f);
                    TopGloss = 0.1f;
                    TechniqueExt = "Masked";
                    BotScale = 20.0f;
                    TopScale = 30.0f;
                    BotBumpStrength = 0.1f;
                    TopBumpStrength = 0.1f;
                    TopClamped = false;
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\B_Crystal";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\B_Crystal";
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\C_Jagged_Crystal";
                    xmlData.TopBump[1]
                        = @"Textures\Terrain\GroundTextures\C_Jagged_Crystal";
                    uiPriDict[matIdx] = 200f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;
                #endregion Print Styles

                ///
                /// Print styles end
                /// One-offs styles begin
                /// 

                #region One Offs
                case 15: 
                    xmlData.BotBump[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\alphabet_norm";
                    xmlData.BotBump[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\alphabet_norm";

                    Color = new Vector4(197f / 256f, 183f / 256f, 162f / 256f, 1.0f);
                    Gloss = .3f;
                    BotScale = 4.0f;
                    uiPriDict[matIdx] = 150f;
                    xmlData.CollisionSound = Foley.CollisionSound.grass;
                    break;

                #endregion One Offs

                ///
                /// One-offs styles end
                /// Realistics styles begin
                /// 

                #region Realistics
                case 1:
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\grass_norm_top";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\dirt_norm";
                    Color = new Vector4(0.9f, 0.65f, 0.04f, 1.0f);
                    Gloss = 0.4f;
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\grass_norm_top";
                    xmlData.TopBump[1]
                        = @"Textures\Terrain\GroundTextures\grass_norm_side2";
                    TopColor = new Vector4(0.5f, 0.8f, 0.2f, 1.0f);
                    TopGloss = 0.1f;
                    TechniqueExt = "Masked";
                    Step = 0.1f;
                    TopScale = 10.0f;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\Grass";
                    xmlData.BotDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\dirt_crackeddrysoft_df_border";
                    xmlData.TopDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\Grass";
                    xmlData.TopDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\Grass_side";
                    xmlData.CollisionSound = Foley.CollisionSound.grass;
                    uiPriDict[matIdx] = 50f;
                    break;

                case 3:
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\rock2_norm";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\rock2_norm";
                    Color = new Vector4(213f / 256f, 140f / 256f, 76f / 256f, 1.0f);
                    BotScale = 10.0f;
                    Gloss = 1.0f;
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\rock2_norm";
                    xmlData.TopBump[1]
                        = @"Textures\Terrain\GroundTextures\cracked_norm";
                    TopColor = new Vector4(0.76f, 0.7f, 0.6f, 1.0f);
                    TopGloss = 0.2f;
                    TechniqueExt = "Masked";
                    TopClamped = true;
                    TopScale = 10.0f;
                    Step = 0.35f;
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\rock2";
                    xmlData.BotDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\rock2";
                    xmlData.TopDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\rock2";
                    xmlData.TopDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\cracked_diff";
                    xmlData.CollisionSound = Foley.CollisionSound.rock;
                    uiPriDict[matIdx] = 50f;
                    break;

                case 27:
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\rock2_norm";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\rock2_norm";
                    Color = new Vector4(213f / 256f, 140f / 256f, 76f / 256f, 1.0f);
                    BotScale = 10.0f;
                    Gloss = 0.2f;
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\alphafade_norm";
                    xmlData.TopBump[1]
                        = @"Textures\Terrain\GroundTextures\alphafade_norm";
                    TopColor = new Vector4(0.76f, 0.7f, 0.6f, 1.0f);
                    TopGloss = 1.0f;
                    TechniqueExt = "Masked";
                    TopClamped = false;
                    TopScale = 1.0f;
                    Step = 0.0f;
                    xmlData.CollisionSound = Foley.CollisionSound.rock;
                    uiPriDict[matIdx] = 50f;
                    break;
                case 30:
                    // Dark lava with delicious glowing orange underbelly
                    // both layers will use the same bump for now -
                    Step = 0.25f;


                    // bottom layer is the lava
                    xmlData.BotBump[(int)Tile.Face.Top]
                        = @"Textures\white";
                    xmlData.BotBump[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\lava-norm-neg-y";
                    xmlData.BotBump[(int)Tile.Face.Back]
                        = @"Textures\Terrain\GroundTextures\lava-norm-pos-y";
                    xmlData.BotBump[(int)Tile.Face.Left]
                        = @"Textures\Terrain\GroundTextures\lava-norm-neg-x";
                    xmlData.BotBump[(int)Tile.Face.Right]
                        = @"Textures\Terrain\GroundTextures\lava-norm-pos-x";
                    BotEmissive = new Vector4(235f / 256f, 130f / 256f, 18f / 256f, .75f);
                    Color = new Vector4(23.5f / 256f, 13.0f / 256f, 1.8f / 256f, 1.0f); // .5f);       // orange with high glow
                    Gloss = 0f;
                    BotBumpStrength = 1.0f;


                    TechniqueExt = "Masked";

                    //// Top layer is the black hardened lava
                    xmlData.TopBump[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\lava-pos-z";
                    xmlData.TopBump[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\lava-neg-y";
                    xmlData.TopBump[(int)Tile.Face.Back]
                        = @"Textures\Terrain\GroundTextures\lava-pos-y";
                    xmlData.TopBump[(int)Tile.Face.Left]
                        = @"Textures\Terrain\GroundTextures\lava-neg-x";
                    xmlData.TopBump[(int)Tile.Face.Right]
                        = @"Textures\Terrain\GroundTextures\lava-pos-x";

                    TopColor = new Vector4(64f / 256f, 64f / 256f, 64f / 256f, 1.0f);           // 50% gray
                    TopGloss = .5f;                                                             // kind of shiny
                    TopBumpStrength = 1.0f;
                    xmlData.CollisionSound = Foley.CollisionSound.dirt;
                    uiPriDict[matIdx] = 50f;
                    break;

                case 29:                     // flat-top chamfer, brown, with top noise
                    xmlData.BotBump[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\rocktop_no_bevel";
                    xmlData.BotBump[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-neg-y";
                    xmlData.BotBump[(int)Tile.Face.Back]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-y";
                    xmlData.BotBump[(int)Tile.Face.Left]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-neg-x";
                    xmlData.BotBump[(int)Tile.Face.Right]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-x";
                    BotBumpStrength = 1.0f;
                    Color = new Vector4(156f / 256f, 110f / 256f, 69f / 256f, 1.0f);
                    Gloss = .01f;
                    Step = 0.5f;
                    xmlData.CollisionSound = Foley.CollisionSound.dirt;
                    uiPriDict[matIdx] = 50f;
                    break;

                case 116:                     // moon domes
                    xmlData.BotBump[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\moon_dome_normal_256";
                    xmlData.BotBump[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-neg-y";
                    xmlData.BotBump[(int)Tile.Face.Back]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-y";
                    xmlData.BotBump[(int)Tile.Face.Left]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-neg-x";
                    xmlData.BotBump[(int)Tile.Face.Right]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-x";
                    BotBumpStrength = 2.0f;
                    BotScale = 4.0f;
                    Color = new Vector4(100f / 256f, 100f / 156f, 206f / 256f, 1.0f);
                    Gloss = .1f;
                    Step = 0.5f;
                    xmlData.CollisionSound = Boku.Audio.Foley.CollisionSound.rock;
                    uiPriDict[matIdx] = 50f;
                    break;
                case 117:                     // snow over dirt
                    Step = 0.25f;

                    // bottom layer is the dirt (was lava)
                    xmlData.BotBump[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\slate_bump";
                    xmlData.BotBump[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-neg-y";
                    xmlData.BotBump[(int)Tile.Face.Back]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-y";
                    xmlData.BotBump[(int)Tile.Face.Left]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-neg-x";
                    xmlData.BotBump[(int)Tile.Face.Right]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-x";

                    Color = new Vector4(156f / 256f, 110f / 256f, 69f / 256f, 1.0f);

                    Gloss = 0.0f;
                    BotBumpStrength = 1.0f;

                    TechniqueExt = "Masked";

                    //// Top layer is the grass (was black hardened lava)
                    xmlData.TopBump[(int)Tile.Face.Top]
                                           = @"Textures\Terrain\GroundTextures\slate_bump";

                    // using lava bump map which has alpha channel to control layer compositing
                    xmlData.TopBump[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\lava-neg-y";
                    xmlData.TopBump[(int)Tile.Face.Back]
                        = @"Textures\Terrain\GroundTextures\lava-pos-y";
                    xmlData.TopBump[(int)Tile.Face.Left]
                        = @"Textures\Terrain\GroundTextures\lava-neg-x";
                    xmlData.TopBump[(int)Tile.Face.Right]
                        = @"Textures\Terrain\GroundTextures\lava-pos-x";

                    TopColor = new Vector4(230f / 256f, 230f / 256f, 250f / 256f, 1.0f); // slightly blue white
                    //                    Color = new Vector4(0.0f, 0.0f, 1.0f, 0.0f); // blue
                    TopScale = 4f;

                    TopGloss = .5f;
                    TopBumpStrength = 3.0f;
                    xmlData.CollisionSound = Boku.Audio.Foley.CollisionSound.dirt;
                    uiPriDict[matIdx] = 50f;
                    break;

                case 118:                     // concrete block
                    xmlData.BotBump[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\conbloc-pos-z";
                    xmlData.BotBump[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\conbloc-neg-y";
                    xmlData.BotBump[(int)Tile.Face.Back]
                        = @"Textures\Terrain\GroundTextures\conbloc-pos-y";
                    xmlData.BotBump[(int)Tile.Face.Left]
                        = @"Textures\Terrain\GroundTextures\conbloc-neg-x";
                    xmlData.BotBump[(int)Tile.Face.Right]
                        = @"Textures\Terrain\GroundTextures\conbloc-pos-x";
                    BotBumpStrength = 2.0f;
                    BotScale = 1.0f;
                    //                    Color = new Vector4(228f / 256f, 178f / 256f, 124f / 256f, 1.0f);
                    Color = new Vector4(178f / 256f, 178f / 256f, 178f / 256f, 1.0f);
                    Gloss = .19f;
                    Step = 0.0f;
                    xmlData.CollisionSound = Boku.Audio.Foley.CollisionSound.rock;
                    uiPriDict[matIdx] = 50f;
                    break;
                case 119:                     // chunky cell rock
                    xmlData.BotBump[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\celstoneL-norm-pos-z";
                    xmlData.BotBump[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\celstoneL-norm-neg-y";
                    xmlData.BotBump[(int)Tile.Face.Back]
                        = @"Textures\Terrain\GroundTextures\celstoneL-norm-pos-y";
                    xmlData.BotBump[(int)Tile.Face.Left]
                        = @"Textures\Terrain\GroundTextures\celstoneL-norm-neg-x";
                    xmlData.BotBump[(int)Tile.Face.Right]
                        = @"Textures\Terrain\GroundTextures\celstoneL-norm-pos-x";
                    BotBumpStrength = 2.0f;
                    BotScale = 1.0f;
                    // flesh version                    Color = new Vector4(228f / 256f, 178f / 256f, 124f / 256f, 1.0f);
                    Color = new Vector4(91f / 256f, 99f / 256f, 136f / 256f, 1.0f);
                    Gloss = .19f;
                    Step = 0.0f;
                    xmlData.CollisionSound = Boku.Audio.Foley.CollisionSound.rock;
                    uiPriDict[matIdx] = 50f;
                    break;

                case 120:                     // multibox test
                    xmlData.BotBump[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\knobble-pos-z";
                    xmlData.BotBump[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\knobble-neg-y";
                    xmlData.BotBump[(int)Tile.Face.Back]
                        = @"Textures\Terrain\GroundTextures\knobble-pos-y";
                    xmlData.BotBump[(int)Tile.Face.Left]
                        = @"Textures\Terrain\GroundTextures\knobble-neg-x";
                    xmlData.BotBump[(int)Tile.Face.Right]
                        = @"Textures\Terrain\GroundTextures\knobble-pos-x";
                    BotBumpStrength = 2.0f;
                    BotScale = 1f;
                    Color = new Vector4(35f / 256f, 35f / 256f, 37f / 256f, 1.0f);
                    Gloss = .8f;
                    Step = 0.00f;
                    xmlData.CollisionSound = Boku.Audio.Foley.CollisionSound.rock;
                    uiPriDict[matIdx] = 50f;
                    break;

                case 115: // dirt cubes with grass on top
                    Step = 0.25f;

                    // bottom layer is the dirt (was lava)
                    xmlData.BotBump[(int)Tile.Face.Top]
                        = @"Textures\Terrain\GroundTextures\slate_bump";
                    xmlData.BotBump[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-neg-y";
                    xmlData.BotBump[(int)Tile.Face.Back]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-y";
                    xmlData.BotBump[(int)Tile.Face.Left]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-neg-x";
                    xmlData.BotBump[(int)Tile.Face.Right]
                        = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-x";

                    Color = new Vector4(156f / 256f, 110f / 256f, 69f / 256f, 1.0f);

                    Gloss = 0.0f;
                    BotBumpStrength = 1.0f;

                    TechniqueExt = "Masked";

                    //// Top layer is the grass (was black hardened lava)
                    xmlData.TopBump[(int)Tile.Face.Top] = @"Textures\Terrain\GroundTextures\slate_bump";

                    // using lava bump map which has alpha channel to control layer compositing
                    xmlData.TopBump[(int)Tile.Face.Front]
                        = @"Textures\Terrain\GroundTextures\lava-neg-y";
                    xmlData.TopBump[(int)Tile.Face.Back]
                        = @"Textures\Terrain\GroundTextures\lava-pos-y";
                    xmlData.TopBump[(int)Tile.Face.Left]
                        = @"Textures\Terrain\GroundTextures\lava-neg-x";
                    xmlData.TopBump[(int)Tile.Face.Right]
                        = @"Textures\Terrain\GroundTextures\lava-pos-x";

                    TopColor = new Vector4(114f / 256f, 181f / 256f, 66f / 256f, 1.0f); // clay green
                    TopScale = 8f;

                    TopGloss = .05f;                                                             // kind of shiny
                    TopBumpStrength = 2.0f;
                    xmlData.CollisionSound = Boku.Audio.Foley.CollisionSound.grass;
                    uiPriDict[matIdx] = 50f;
                    break;


                #endregion Realistics


                ///
                /// Realistics styles end
                /// Metal styles begin
                /// 

                #region Metals
                case 0:
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\alum_plt_norm_top";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\alum_plt_norm_side";
                    xmlData.BotDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\alum_plt";
                    xmlData.BotDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\alum_plt";
                    Color = new Vector4(0.7f, 0.7f, 0.8f, 1.0f);
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    uiPriDict[matIdx] = 40f;
                    break;

                case 6:
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\RIVROCK1_norm1";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\alum_plt_norm_side";
                    Color = new Vector4(0.8f, 0.8f, 0.8f, 1.0f);
                    Gloss = 1.0f;
                    xmlData.CollisionSound = Foley.CollisionSound.rock;
                    uiPriDict[matIdx] = 40f;
                    break;
                case 7:
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\RIVROCK1_norm1";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\alum_plt_norm_side";
                    Color = new Vector4(0.3f, 0.5f, 0.3f, 1.0f);
                    Gloss = 0.3f;
                    xmlData.CollisionSound = Foley.CollisionSound.rock;
                    uiPriDict[matIdx] = 40f;
                    break;
                #endregion Metals

                ///
                /// Metal styles end
                /// Decorated single cube (dotted) styles begin
                /// 

                #region Decorated Singles
                case 2:
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\blob_norm";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\blob_norm";
                    Color = new Vector4(0.3f, 0.1f, 0.1f, 1.0f);
                    Gloss = 0.4f;
                    BotEmissive = new Vector4(0.8f, 0.2f, 0.2f, 0.75f);
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\circle_norm";
                    xmlData.TopBump[1]
                        = @"Textures\Terrain\GroundTextures\circle_norm";
                    TopColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
                    TopGloss = 1.0f;
                    TechniqueExt = "Masked";
                    TopClamped = false;
                    xmlData.BotDiffuse[0]
                        = @"Textures\white";
                    xmlData.BotDiffuse[1]
                        = @"Textures\white";
                    xmlData.TopDiffuse[0]
                        = @"Textures\Terrain\GroundTextures\circle_diff";
                    xmlData.TopDiffuse[1]
                        = @"Textures\Terrain\GroundTextures\circle_diff";
                    xmlData.CollisionSound = Foley.CollisionSound.plasticSoft;
                    uiPriDict[matIdx] = 30f;
                    break;

                case 4:
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\blob_norm";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\blob_norm";
                    Color = new Vector4(0.1f, 0.15f, 0.1f, 1.0f);
                    Gloss = 0.4f;
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\circle_norm";
                    xmlData.TopBump[1]
                        = @"Textures\Terrain\GroundTextures\circle_norm";
                    TopEmissive = new Vector4(0.4f, 0.1f, 0.1f, 0.85f);
                    TopColor = new Vector4(0.8f, 0.5f, 0.5f, 1.0f);
                    TopGloss = 1.0f;
                    TechniqueExt = "Masked";
                    TopClamped = false;
                    xmlData.CollisionSound = Foley.CollisionSound.plasticSoft;
                    uiPriDict[matIdx] = 30f;
                    break;

                // Black plastic with glowing red dots
                case 21:
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\blob_norm";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\blob_norm";
                    Color = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
                    Gloss = 0.0f;
                    BotEmissive = new Vector4(0.8f, 0.2f, 0.2f, 0.3f);
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\circle_norm";
                    xmlData.TopBump[1]
                        = @"Textures\Terrain\GroundTextures\circle_norm";
                    TopColor = new Vector4(0.1f, 0.1f, 0.1f, 1.0f);
                    TopGloss = 1.0f;
                    TechniqueExt = "Masked";
                    TopClamped = false;
                    xmlData.CollisionSound = Foley.CollisionSound.plasticSoft;
                    uiPriDict[matIdx] = 30f;
                    break;

                case 44:
                    Color = new Vector4(150.0f / 255.9f, 31.9f / 255.9f, 35.0f / 255.9f, 1.0f);
                    Gloss = 1.0f;
                    TopColor = new Vector4(246.0f / 255.9f, 226.0f / 255.9f, 160.0f / 255.9f, 1.0f);
                    TopGloss = 0.1f;
                    TechniqueExt = "Masked";
                    BotScale = 1.0f;
                    TopScale = 1.0f;
                    BotBumpStrength = 0.0f;
                    TopBumpStrength = 0.25f;
                    TopClamped = false;
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\Blue128x128";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\Blue128x128";
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\C_01_Cubes";
                    xmlData.TopBump[1]
                        = @"Textures\Terrain\GroundTextures\C_01_Cubes";
                    uiPriDict[matIdx] = 30f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 45:
                    Color = new Vector4(58.0f / 255.9f, 88.9f / 255.9f, 92.0f / 255.9f, 1.0f);
                    Gloss = 0.3f;
                    TopColor = new Vector4(174.0f / 255.9f, 56.0f / 255.9f, 2.0f / 255.9f, 1.0f);
                    TopGloss = 1.0f;
                    TechniqueExt = "Masked";
                    BotScale = 1.0f;
                    TopScale = 1.0f;
                    BotBumpStrength = 0.0f;
                    TopBumpStrength = 0.25f;
                    TopClamped = false;
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\Blue128x128";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\Blue128x128";
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\C_02_Canvas";
                    xmlData.TopBump[1]
                        = @"Textures\Terrain\GroundTextures\C_02_Canvas";
                    uiPriDict[matIdx] = 30f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 46:
                    Color = new Vector4(69.0f / 255.9f, 189.0f / 255.9f, 149.0f / 255.9f, 1.0f);
                    Gloss = 1.0f;
                    TopColor = new Vector4(90.0f / 255.9f, 203.0f / 255.9f, 247.0f / 255.9f, 1.0f);
                    TopGloss = 0.1f;
                    TechniqueExt = "Masked";
                    BotScale = 30.0f;
                    TopScale = 1.0f;
                    BotBumpStrength = 0.25f;
                    TopBumpStrength = 0.25f;
                    TopClamped = false;
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\B_Canvas";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\B_Canvas";
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\C_03_Bevel";
                    xmlData.TopBump[1]
                        = @"Textures\Terrain\GroundTextures\C_03_Bevel";
                    uiPriDict[matIdx] = 30f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 47:
                    Color = new Vector4(108.0f / 255.9f, 28.0f / 255.9f, 121.0f / 255.9f, 1.0f);
                    Gloss = 1.0f;
                    TopColor = new Vector4(127.0f / 255.9f, 43.0f / 255.9f, 142.0f / 255.9f, 1.0f);
                    TopGloss = 1.0f;
                    TechniqueExt = "Masked";
                    BotScale = 1.0f;
                    TopScale = 1.0f;
                    BotBumpStrength = 0.25f;
                    TopBumpStrength = 0.25f;
                    TopClamped = false;
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\B_Chrome";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\B_Chrome";
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\C_04_Chrome";
                    xmlData.TopBump[1]
                        = @"Textures\Terrain\GroundTextures\C_04_Chrome";
                    uiPriDict[matIdx] = 30f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 48:
                    Color = new Vector4(97.0f / 255.9f, 76.0f / 255.9f, 78.0f / 255.9f, 1.0f);
                    Gloss = 1.0f;
                    TopColor = new Vector4(137.0f / 255.9f, 185.0f / 255.9f, 153.0f / 255.9f, 1.0f);
                    TopGloss = 1.0f;
                    TechniqueExt = "Masked";
                    BotScale = 20.0f;
                    TopScale = 1.0f;
                    BotBumpStrength = 0.25f;
                    TopBumpStrength = 0.25f;
                    TopClamped = false;
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\B_Stone";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\B_Stone";
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\C_05_Stone";
                    xmlData.TopBump[1]
                        = @"Textures\Terrain\GroundTextures\C_05_Stone";
                    uiPriDict[matIdx] = 30f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 49:
                    Color = new Vector4(254.0f / 255.9f, 186.0f / 255.9f, 16.0f / 255.9f, 1.0f);
                    Gloss = 1.0f;
                    TopColor = new Vector4(240.0f / 255.9f, 240.0f / 255.9f, 240.0f / 255.9f, 1.0f);
                    TopGloss = 1.0f;
                    TechniqueExt = "Masked";
                    BotScale = 1.0f;
                    TopScale = 1.0f;
                    BotBumpStrength = 0.25f;
                    TopBumpStrength = 0.25f;
                    TopClamped = false;
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\B_Craquel";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\B_Craquel";
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\C_06_Craquel";
                    xmlData.TopBump[1]
                        = @"Textures\Terrain\GroundTextures\C_06_Craquel";
                    uiPriDict[matIdx] = 30f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 50:
                    Color = new Vector4(137.0f / 255.9f, 176.0f / 255.9f, 186.0f / 255.9f, 1.0f);
                    Gloss = 1.0f;
                    TopColor = new Vector4(217.0f / 255.9f, 214.0f / 255.9f, 184.0f / 255.9f, 1.0f);
                    TopGloss = 1.0f;
                    TechniqueExt = "Masked";
                    BotScale = 20.0f;
                    TopScale = 1.0f;
                    BotBumpStrength = 0.25f;
                    TopBumpStrength = 0.25f;
                    TopClamped = false;
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\B_Craquel2";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\B_Craquel2";
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\C_07_Blurry";
                    xmlData.TopBump[1]
                        = @"Textures\Terrain\GroundTextures\C_07_Blurry";
                    uiPriDict[matIdx] = 30f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 51:
                    Color = new Vector4(254.0f / 255.9f, 186.0f / 255.9f, 16.0f / 255.9f, 1.0f);
                    Gloss = 1.0f;
                    TopColor = new Vector4(240.0f / 255.9f, 240.0f / 255.9f, 240.0f / 255.9f, 1.0f);
                    TopGloss = 1.0f;
                    TechniqueExt = "Masked";
                    BotScale = 30.0f;
                    TopScale = 1.0f;
                    BotBumpStrength = 0.25f;
                    TopBumpStrength = 0.25f;
                    TopClamped = false;
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\B_Craquel2";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\B_Craquel2";
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\C_08_Bevel";
                    xmlData.TopBump[1]
                        = @"Textures\Terrain\GroundTextures\C_08_Bevel";
                    uiPriDict[matIdx] = 30f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 52:
                    Color = new Vector4(131.0f / 255.9f, 156.0f / 255.9f, 126.0f / 255.9f, 1.0f);
                    Gloss = 1.0f;
                    TopColor = new Vector4(154.0f / 255.9f, 199.0f / 255.9f, 189.0f / 255.9f, 1.0f);
                    TopGloss = 1.0f;
                    TechniqueExt = "Masked";
                    BotScale = 20.0f;
                    TopScale = 1.0f;
                    BotBumpStrength = 0.25f;
                    TopBumpStrength = 0.25f;
                    TopClamped = false;
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\B_Craquel2";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\B_Craquel2";
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\C_09_Craquel2";
                    xmlData.TopBump[1]
                        = @"Textures\Terrain\GroundTextures\C_09_Craquel2";
                    uiPriDict[matIdx] = 30f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 53:
                    Color = new Vector4(224.0f / 255.9f, 232.0f / 255.9f, 44.0f / 255.9f, 1.0f);
                    Gloss = 1.0f;
                    TopColor = new Vector4(156.0f / 255.9f, 222.0f / 255.9f, 71.0f / 255.9f, 1.0f);
                    TopGloss = 1.0f;
                    TechniqueExt = "Masked";
                    BotScale = 30.0f;
                    TopScale = 1.0f;
                    BotBumpStrength = 0.25f;
                    TopBumpStrength = 0.25f;
                    TopClamped = false;
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\B_NotePaper";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\B_NotePaper";
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\C_10_Note";
                    xmlData.TopBump[1]
                        = @"Textures\Terrain\GroundTextures\C_10_Note";
                    uiPriDict[matIdx] = 30f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 54:
                    Color = new Vector4(238.0f / 255.9f, 255.0f / 255.9f, 235.0f / 255.9f, 1.0f);
                    Gloss = 1.0f;
                    TopColor = new Vector4(194.0f / 255.9f, 255.0f / 255.9f, 197.0f / 255.9f, 1.0f);
                    TopGloss = 1.0f;
                    TechniqueExt = "Masked";
                    BotScale = 20.0f;
                    TopScale = 1.0f;
                    BotBumpStrength = 0.25f;
                    TopBumpStrength = 0.25f;
                    TopClamped = false;
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\B_Canvas";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\B_Canvas";
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\C_11_Canvas";
                    xmlData.TopBump[1]
                        = @"Textures\Terrain\GroundTextures\C_11_Canvas";
                    uiPriDict[matIdx] = 30f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 55:
                    Color = new Vector4(67.0f / 255.9f, 38.0f / 255.9f, 17.0f / 255.9f, 1.0f);
                    Gloss = 1.0f;
                    TopColor = new Vector4(238.0f / 255.9f, 200.0f / 255.9f, 108.0f / 255.9f, 1.0f);
                    TopGloss = 1.0f;
                    TechniqueExt = "Masked";
                    BotScale = 30.0f;
                    TopScale = 1.0f;
                    BotBumpStrength = 0.25f;
                    TopBumpStrength = 0.25f;
                    TopClamped = false;
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\B_Craquel";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\B_Craquel";
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\C_12_Craquel";
                    xmlData.TopBump[1]
                        = @"Textures\Terrain\GroundTextures\C_12_Craquel";
                    uiPriDict[matIdx] = 30f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                case 56:
                    Color = new Vector4(150.0f / 255.9f, 31.0f / 255.9f, 35.0f / 255.9f, 1.0f);
                    Gloss = 1.0f;
                    TopColor = new Vector4(238.0f / 255.9f, 255.0f / 255.9f, 235.0f / 255.9f, 1.0f);
                    TopGloss = 1.0f;
                    TechniqueExt = "Masked";
                    BotScale = 30.0f;
                    TopScale = 15.0f;
                    BotBumpStrength = 0.25f;
                    TopBumpStrength = 0.25f;
                    TopClamped = false;
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\B_Chrome";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\B_Chrome";
                    xmlData.TopBump[0]
                        = @"Textures\Terrain\GroundTextures\C_01_StarZNote";
                    xmlData.TopBump[1]
                        = @"Textures\Terrain\GroundTextures\C_01_StarZNote";
                    uiPriDict[matIdx] = 30f;
                    xmlData.CollisionSound = Foley.CollisionSound.metalHard;
                    break;

                #endregion Decorated Singles

                ///
                /// Decorated single cube (dotted) styles end
                /// Generic plastic block styles begin
                /// 
                #region Plastics

                case 25:                                                        // flat-top chamfer box - black
                    FlatTopChamfer_SM3(
                        PaletteEntry(Palette.Black),
                        Foley.CollisionSound.metalHard);
                    uiPriDict[matIdx] = 27f;
                    break;

                #region Chamfer Box
                case 43:
                    ChamferBox_SM3(
                        PaletteEntry(Palette.DarkRed),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 26f;
                    break;
                case 11:
                    ChamferBox_SM3(
                        PaletteEntry(Palette.Red),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 25f;
                    break;

                case 58:
                    ChamferBox_SM3(
                        PaletteEntry(Palette.Orange),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 24f;
                    break;

                case 59:
                    ChamferBox_SM3(
                        PaletteEntry(Palette.Yellow),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 23f;
                    break;

                case 10:
                    ChamferBox_SM3(
                        PaletteEntry(Palette.Brown),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 22f;
                    break;

                case 5:
                    ChamferBox_SM3(
                        PaletteEntry(Palette.Tan),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 21f;
                    break;

                case 9:
                    ChamferBox_SM3(
                        PaletteEntry(Palette.Green),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 20f;
                    break;

                case 60:
                    ChamferBox_SM3(
                        PaletteEntry(Palette.PaleGreen),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 19f;
                    break;


                case 61:
                    ChamferBox_SM3(
                        PaletteEntry(Palette.Blue),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 19f;
                    break;

                case 62:
                    ChamferBox_SM3(
                        PaletteEntry(Palette.PaleBlue),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 19f;
                    break;


                case 63:
                    ChamferBox_SM3(
                        PaletteEntry(Palette.Purple),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 19f;
                    break;

                case 64:
                    ChamferBox_SM3(
                        PaletteEntry(Palette.Pink),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 19f;
                    break;

                case 65:
                    ChamferBox_SM3(
                        PaletteEntry(Palette.White),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 19f;
                    break;

                case 66:
                    ChamferBox_SM3(
                        PaletteEntry(Palette.Grey),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 19f;
                    break;

                #endregion Chamfer Box

                #region Muffin

                case 67:
                    Muffin_SM3(
                        PaletteEntry(Palette.DarkRed),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 19f;
                    break;

                case 68:
                    Muffin_SM3(
                        PaletteEntry(Palette.Red),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 19f;
                    break;

                case 69:
                    Muffin_SM3(
                        PaletteEntry(Palette.Orange),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 19f;
                    break;

                case 70:
                    Muffin_SM3(
                        PaletteEntry(Palette.Yellow),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 19f;
                    break;

                case 71:
                    Muffin_SM3(
                        PaletteEntry(Palette.Brown),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 19f;
                    break;

                case 72:
                    Muffin_SM3(
                        PaletteEntry(Palette.Tan),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 19f;
                    break;

                case 8:
                    Muffin_SM3(
                        PaletteEntry(Palette.Green),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 18f;
                    break;

                case 73:
                    Muffin_SM3(
                        PaletteEntry(Palette.PaleGreen),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 18f;
                    break;

                case 74:
                    Muffin_SM3(
                        PaletteEntry(Palette.Blue),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 18f;
                    break;

                case 75:
                    Muffin_SM3(
                        PaletteEntry(Palette.PaleBlue),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 18f;
                    break;

                case 76:
                    Muffin_SM3(
                        PaletteEntry(Palette.Purple),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 18f;
                    break;

                case 77:
                    Muffin_SM3(
                        PaletteEntry(Palette.Pink),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 18f;
                    break;

                case 78:
                    Muffin_SM3(
                        PaletteEntry(Palette.White),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 18f;
                    break;

                case 79:
                    Muffin_SM3(
                        PaletteEntry(Palette.Grey),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 18f;
                    break;

                #endregion Muffin

                #region Hewn

                case 80:                     
                    Hewn_SM3(
                        PaletteEntry(Palette.DarkRed),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 18f;
                    break;

                case 81:
                    Hewn_SM3(
                        PaletteEntry(Palette.Red),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 18f;
                    break;

                case 82:
                    Hewn_SM3(
                        PaletteEntry(Palette.Orange),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 18f;
                    break;

                case 83:
                    Hewn_SM3(
                        PaletteEntry(Palette.Yellow),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 18f;
                    break;

                case 12:
                    Hewn_SM3(
                        PaletteEntry(Palette.Brown),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 17f;
                    break;

                case 84:
                    Hewn_SM3(
                        PaletteEntry(Palette.Tan),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 17f;
                    break;

                case 85:
                    Hewn_SM3(
                        PaletteEntry(Palette.Green),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 17f;
                    break;

                case 86:
                    Hewn_SM3(
                        PaletteEntry(Palette.PaleGreen),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 17f;
                    break;

                case 87:
                    Hewn_SM3(
                        PaletteEntry(Palette.Blue),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 17f;
                    break;

                case 88:
                    Hewn_SM3(
                        PaletteEntry(Palette.PaleBlue),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 17f;
                    break;

                case 89:
                    Hewn_SM3(
                        PaletteEntry(Palette.Purple),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 17f;
                    break;

                case 90:
                    Hewn_SM3(
                        PaletteEntry(Palette.Pink),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 17f;
                    break;

                case 91:
                    Hewn_SM3(
                        PaletteEntry(Palette.White),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 17f;
                    break;

                case 92:
                    Hewn_SM3(
                        PaletteEntry(Palette.Black),
                        Foley.CollisionSound.dirt);
                    uiPriDict[matIdx] = 17f;
                    break;
                #endregion Hewn

                #region Bubble
                case 93:                     // bubble turf
                    BubbleTurf_SM3(
                        PaletteEntry(Palette.DarkRed), 
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 94:                     // bubble turf
                    BubbleTurf_SM3(
                        PaletteEntry(Palette.Red),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 95:                     // bubble turf
                    BubbleTurf_SM3(
                        PaletteEntry(Palette.Orange),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 96:                     // bubble turf
                    BubbleTurf_SM3(
                        PaletteEntry(Palette.Yellow),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 97:                     // bubble turf
                    BubbleTurf_SM3(
                        PaletteEntry(Palette.Brown),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 98:                     // bubble turf
                    BubbleTurf_SM3(
                        PaletteEntry(Palette.Tan),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 99:                     // bubble turf
                    BubbleTurf_SM3(
                        PaletteEntry(Palette.Green),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 100:                     // bubble turf
                    BubbleTurf_SM3(
                        PaletteEntry(Palette.PaleGreen),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 101:                     // bubble turf
                    BubbleTurf_SM3(
                        PaletteEntry(Palette.Blue),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 102:                     // bubble turf
                    BubbleTurf_SM3(
                        PaletteEntry(Palette.PaleBlue),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 103:                     // bubble turf
                    BubbleTurf_SM3(
                        PaletteEntry(Palette.Purple),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 104:                     // bubble turf
                    BubbleTurf_SM3(
                        PaletteEntry(Palette.Pink),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 105:                     // bubble turf
                    BubbleTurf_SM3(
                        PaletteEntry(Palette.White),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 106:                     // bubble turf
                    BubbleTurf_SM3(
                        PaletteEntry(Palette.Grey),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 16f;
                    break;

                #endregion Bubble

                #region FlatTop Chamfer
                case 107:                                                        // flat-top chamfer box - brown
                    FlatTopChamfer_SM3(
                        PaletteEntry(Palette.DarkRed),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 108:                                                        // flat-top chamfer box - brown
                    FlatTopChamfer_SM3(
                        PaletteEntry(Palette.Red),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 109:                                                        // flat-top chamfer box - brown
                    FlatTopChamfer_SM3(
                        PaletteEntry(Palette.Orange),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 110:                                                        // flat-top chamfer box - brown
                    FlatTopChamfer_SM3(
                        PaletteEntry(Palette.Yellow),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 16f;
                    break;

                case 24:                                                        // flat-top chamfer box - brown
                    FlatTopChamfer_SM3(
                        PaletteEntry(Palette.Brown),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 15f;
                    break;


                case 111:                                                        // flat-top chamfer box - brown
                    FlatTopChamfer_SM3(
                        PaletteEntry(Palette.Tan),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 15f;
                    break;

                case 23:                                                        // flat-top chamfer box - green
                    FlatTopChamfer_SM3(
                        PaletteEntry(Palette.Green),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 14f;
                    break;
                case 26:                     // side overhang, sameless top and left/right
                    FlatTopChamfer_SM3(
                        PaletteEntry(Palette.PaleGreen),
                        Foley.CollisionSound.grass);
                    uiPriDict[matIdx] = 13f;
                    break;

                case 112:                                                        // flat-top chamfer box - white
                    FlatTopChamfer_SM3(
                        PaletteEntry(Palette.Blue),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 13f;
                    break;

                case 13:                                                        // flat-top chamfer box - white
                    FlatTopChamfer_SM3(
                        PaletteEntry(Palette.PaleBlue),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 12f;
                    break;

                case 14:                                                        // flat-top chamfer box - white
                    FlatTopChamfer_SM3(
                        PaletteEntry(Palette.Purple),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 12f;
                    break;

                case 17:                                                        // flat-top chamfer box - white
                    FlatTopChamfer_SM3(
                        PaletteEntry(Palette.Pink),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 12f;
                    break;

                case 57:                                                        // flat-top chamfer box - white
                    FlatTopChamfer_SM3(
                        PaletteEntry(Palette.White),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 12f;
                    break;

                case 22:                                                        // flat-top chamfer box - white
                    FlatTopChamfer_SM3(
                        PaletteEntry(Palette.Grey),
                        Foley.CollisionSound.plasticSoft);
                    uiPriDict[matIdx] = 11f;
                    break;
                #endregion FlatTop Chamfer

                #endregion Plastics

                ///
                /// Generic plastic block styles end
                /// Glowing cubes styles begin
                /// 
                #region Glowing
                    // Glowing orange (yellow plus diffuse red)
                case 16:
                    GlowCube_SM3(new Vector4(1f, 0f, 0f, 1.0f),
                        new Vector4(.5f, .5f, 0f, 0.8f));
                    break;
                // Glowing blue 
                case 18:
                    GlowCube_SM3(new Vector4(84f / 256f, 197f / 256f, 256f, 1.0f),
                        new Vector4(42f / 256f, 96f / 256f, 128f, 0.9f));
                    break;
                // Glowing red
                case 19:
                    GlowCube_SM3(new Vector4(1f, .1f, 0f, 1.0f), 
                        new Vector4(0.5f, .05f, 0f, 0.9f));
                    break;
                // Glowing purple
                case 20:
                    GlowCube_SM3(new Vector4(0, 0, 1.0f, 1.0f), 
                        new Vector4(.4f, 0f, 0f, 0.75f));
                    break;

                // Glowing white
                case 113:
                    GlowCube_SM3(new Vector4(0.5f, 0.5f, 0.5f, 1.0f),
                        new Vector4(0.0f, 0.1f, 0.5f, 0.75f));
                    break;

                case 114:
                    GlowCube_SM3(new Vector4(0.5f, 0, 0, 1.0f),
                        new Vector4(0, 0.5f, 0, 0.75f));
                    break;

                #endregion Glowing

                ///
                /// Glowing cubes styles end
                /// 

                //ToDo (DZ): Do we need this?
                //case TerrainMaterial.MaxNum:
                //    xmlData.BotBump[(int)Tile.Face.Top]
                //        = @"Textures\Terrain\GroundTextures\Blue128x128";
                //    xmlData.BotBump[1] = xmlData.BotBump[0];
                //    Color = new Vector4(1.0f, 1.0f, 0.5f, 1.0f);
                //    Gloss = 0.5f;
                //    uiPriDict[matIdx] = Single.MinValue; // Ensure this stays at the end of the queue.
                //    xmlData.CollisionSound = Foley.CollisionSound.plasticSoft;
                //    break;
                default:
                    xmlData.BotBump[0]
                        = @"Textures\Terrain\GroundTextures\RIVROCK1_norm1";
                    xmlData.BotBump[1]
                        = @"Textures\Terrain\GroundTextures\alum_plt_norm_side";
                    Color = new Vector4(0.3f, 0.3f, 0.3f, 1.0f);
                    Gloss = 1.0f;
                    uiPriDict[matIdx] = -1.0f;
                    break;
            }
        }
        private void ChamferBox_SM3(Vector4 color, Foley.CollisionSound sound)
        {
            xmlData.BotBump[(int)Tile.Face.Top]
                = @"Textures\Terrain\GroundTextures\chamfer_box_normal";
            xmlData.BotBump[(int)Tile.Face.Front]
                = @"Textures\Terrain\GroundTextures\chamfer_box_normal";
            Gloss = .0f;
            BotBumpStrength = 1.0f;

            Color = color;
            xmlData.CollisionSound = sound;
        }
        private void Muffin_SM3(Vector4 color, Foley.CollisionSound sound)
        {
            xmlData.BotBump[(int)Tile.Face.Top]
                = @"Textures\Terrain\GroundTextures\muffin-norm-pos-z";
            xmlData.BotBump[(int)Tile.Face.Front]
                = @"Textures\Terrain\GroundTextures\muffin-norm-neg-y";
            xmlData.BotBump[(int)Tile.Face.Back]
                = @"Textures\Terrain\GroundTextures\muffin-norm-pos-y";
            xmlData.BotBump[(int)Tile.Face.Left]
                = @"Textures\Terrain\GroundTextures\muffin-norm-neg-x";
            xmlData.BotBump[(int)Tile.Face.Right]
                = @"Textures\Terrain\GroundTextures\muffin-norm-pos-x";

            Gloss = .1f;
            BotBumpStrength = 1.0f;
            Step = 0.25f;

            Color = color;
            xmlData.CollisionSound = sound;
        }
        private void Hewn_SM3(Vector4 color, Foley.CollisionSound sound)
        {
            xmlData.BotBump[(int)Tile.Face.Top]
                = @"Textures\Terrain\GroundTextures\hewn-pos-z";
            xmlData.BotBump[(int)Tile.Face.Front]
                = @"Textures\Terrain\GroundTextures\hewn-neg-y";
            xmlData.BotBump[(int)Tile.Face.Back]
                = @"Textures\Terrain\GroundTextures\hewn-pos-y";
            xmlData.BotBump[(int)Tile.Face.Right]
                = @"Textures\Terrain\GroundTextures\hewn-pos-x";
            xmlData.BotBump[(int)Tile.Face.Left]
                = @"Textures\Terrain\GroundTextures\hewn-neg-x";
            Color = color;
            Gloss = .5f;
            BotBumpStrength = 1.0f;
            xmlData.CollisionSound = sound;
        }
        private void BubbleTurf_SM3(Vector4 color, Foley.CollisionSound sound)
        {
            xmlData.BotBump[(int)Tile.Face.Top]
                = @"Textures\Terrain\GroundTextures\bubble-norm-pos-z";
            xmlData.BotBump[(int)Tile.Face.Front]
                = @"Textures\Terrain\GroundTextures\bubble-norm-neg-y";
            xmlData.BotBump[(int)Tile.Face.Back]
                = @"Textures\Terrain\GroundTextures\bubble-norm-pos-y";
            xmlData.BotBump[(int)Tile.Face.Left]
                = @"Textures\Terrain\GroundTextures\bubble-norm-neg-x";
            xmlData.BotBump[(int)Tile.Face.Right]
                = @"Textures\Terrain\GroundTextures\bubble-norm-pos-x";

            Gloss = .1f;
            BotBumpStrength = 1.0f;

            Color = color;
            xmlData.CollisionSound = sound;
        }
        private void FlatTopChamfer_SM3(Vector4 color, Foley.CollisionSound sound)
        {
            xmlData.BotBump[(int)Tile.Face.Top]
                = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-z";
            xmlData.BotBump[(int)Tile.Face.Front]
                = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-neg-y";
            xmlData.BotBump[(int)Tile.Face.Back]
                = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-y";
            xmlData.BotBump[(int)Tile.Face.Left]
                = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-neg-x";
            xmlData.BotBump[(int)Tile.Face.Right]
                = @"Textures\Terrain\GroundTextures\flat-top-chamfer-norm-pos-x";

            Gloss = 0.05f;
            Step = 0.5f;

            Color = color;
            xmlData.CollisionSound = sound;
        }
        private void GlowCube_SM3(Vector4 color, Vector4 emissive)
        {
            xmlData.BotBump[(int)Tile.Face.Top]
                = @"Textures\Terrain\GroundTextures\chamfer_box_normal";
            xmlData.BotBump[(int)Tile.Face.Front]
                = @"Textures\Terrain\GroundTextures\chamfer_box_normal";
            Gloss = 1f;
            BotEmissive = emissive;
            Color = color;
            xmlData.CollisionSound = Foley.CollisionSound.plasticSoft;
        }
        private static Vector4 PaletteEntry(Palette which)
        {
            return BasicPalette[(int)which];
        }
        private enum Palette
        {
            DarkRed,
            Red,
            Orange,
            Yellow,
            Brown,
            Tan,
            Green,
            PaleGreen,
            Blue,
            PaleBlue,
            Purple,
            Pink,
            White,
            Grey,
            Black,

            Count
        }

        private static Vector4[] BasicPalette = new Vector4[(int)Palette.Count]
            { 
                new Vector4(123.0f / 255.9f, 3.0f / 255.9f, 25.0f / 255.9f, 1.0f),
                new Vector4(214.0f / 255.9f, 6.0f / 255.9f, 34.0f / 255.9f, 1.0f),
                new Vector4(255.0f / 255.9f, 173.0f / 255.9f, 51.0f / 255.9f, 1.0f),
                new Vector4(255.0f / 255.9f, 236.0f / 255.9f, 80.0f / 255.9f, 1.0f),
                new Vector4(140.0f / 255.9f, 86.0f / 255.9f, 11.0f / 255.9f, 1.0f),
                new Vector4(255.0f / 255.9f, 227.0f / 255.9f, 178.0f / 255.9f, 1.0f),
                new Vector4(114.0f / 255.9f, 181.0f / 255.9f, 66.0f / 255.9f, 1.0f),
                new Vector4(141.0f / 255.9f, 244.0f / 255.9f, 146.0f / 255.9f, 1.0f),
                new Vector4(5.0f / 255.9f, 22.0f / 255.9f, 154.0f / 255.9f, 1.0f),
                new Vector4(165.0f / 255.9f, 227.0f / 255.9f, 251.0f / 255.9f, 1.0f),
                new Vector4(100.0f / 255.9f, 3.0f / 255.9f, 150.0f / 255.9f, 1.0f),
                new Vector4(248.0f / 255.9f, 171.0f / 255.9f, 236.0f / 255.9f, 1.0f),
                new Vector4(252.0f / 255.9f, 252.0f / 255.9f, 252.0f / 255.9f, 1.0f),
                new Vector4(182.0f / 255.9f, 182.0f / 255.9f, 182.0f / 255.9f, 1.0f),
                new Vector4(20.0f / 255.9f, 20.0f / 255.9f, 20.0f / 255.9f, 1.0f)
            };
        #endregion INIT_HACKERY

        #endregion INTERNAL
    }
}
