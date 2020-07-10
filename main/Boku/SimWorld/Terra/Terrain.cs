// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


#if DEBUG
#define Debug_ToggleRenderWireWithF9
#define Debug_ToggleSideFacesWithF7
#define Debug_ToggleTopFaceWithF6
#define Debug_CountTerrainVerts
#endif

#define MF_WARPED

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.InteropServices;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Input;

using Boku.Base;
using Boku.Fx;
using Boku.Common;
using Boku.Common.ParticleSystem;
using Boku.Common.Xml;
using Boku.UI2D;
using Boku.SimWorld.Path;

namespace Boku.SimWorld.Terra
{
    public class Terrain : INeedsDeviceReset
    {
        /// <summary>
        /// The available terrain rendering methods. Each method
        /// uses its own VertexDecleration and Effect. See their
        /// respective "remarks" for a detailed description of
        /// their purpose and usage notes.
        /// </summary>
        public enum RenderMethods
        {
            /// <remarks>
            /// See the "Terrain_FD.fx" file's comments for a
            /// description of the FewerDraws code-path.
            /// </remarks>
            FewerDraws = 2,
        }
        public static RenderMethods RenderMethod
        {
            get;
            private set;
        }

        #region FewerDraws render method state

        //Vertex decleration
        public struct TerrainVertex_FD : IVertexType
        {
            public Vector4 positionAndZDiff;
            public UInt32 faceAndCorner;

            static VertexDeclaration decl;
            static VertexElement[] elements = 
            {
                new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Position, 0),
                new VertexElement(16, VertexElementFormat.Byte4, VertexElementUsage.TextureCoordinate, 1),
            };

            public TerrainVertex_FD(Vector3 position, float topZ, int face, int corner)
            {
                this.positionAndZDiff = new Vector4(position.X, position.Y, position.Z, position.Z - topZ);
                this.faceAndCorner = ((uint)corner << 8) + (uint)face;
            }

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

        }

        //EffectParameters
        public enum EffectParams_FD
        {
            Normals = EffectParamsLength,
            BumpToWorld,
            Inversion,
        }
        public EffectParameter ParameterColor(EffectParams_FD param)
        {
            return effectCacheColor.Parameter((int)param);
        }
        public EffectParameter ParameterEdit(EffectParams_FD param)
        {
            return effectCacheEdit.Parameter((int)param);
        }

        //Parameter helpers
        public void SetGlobalParams_FD()
        {
            ParameterColor(EffectParams_FD.Inversion).SetValue(Inversion);
            ParameterColor(EffectParams_FD.Normals).SetValue(Tile.FaceNormals);
            ParameterColor(EffectParams_FD.BumpToWorld).SetValue(BumpToWorld);
            ParameterEdit(EffectParams_FD.Inversion).SetValue(Inversion);
            ParameterEdit(EffectParams_FD.Normals).SetValue(Tile.FaceNormals);
            ParameterEdit(EffectParams_FD.BumpToWorld).SetValue(BumpToWorld);
        }
        public void SetMaterialParams_FD(ushort matIdx, bool forUI)
        {
            TerrainMaterial.Get(matIdx).Setup_FD(forUI);
        }
        public void SetTopParams_FD(ushort matIdx, bool forUI)
        {
            TerrainMaterial.Get(matIdx).SetupTop_FD(forUI);
        }
        public void SetSideParams_FD(ushort matIdx, bool forUI)
        {
            TerrainMaterial.Get(matIdx).SetupSides_FD(forUI);
        }
        #endregion

        
        #region Fabric render method state

        //Vertex decleration
        [StructLayout(LayoutKind.Explicit, Size = TerrainVertex_FA.Stride)]
        public struct TerrainVertex_FA : IVertexType
        {
            [FieldOffset(0)]
            public Vector4 positionAndNormalZ;
            [FieldOffset(16)]
            public Vector2 normalXY;

            public const int Stride = 24;
            public static VertexDeclaration decl = null;
            private readonly static VertexElement[] elements = 
            {
                new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Position, 0),
                new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.Normal, 0),
            };

            public TerrainVertex_FA(Vector3 position, Vector3 normal)
            {
                this.positionAndNormalZ = new Vector4(position.X, position.Y, position.Z, normal.Z);
                this.normalXY = new Vector2(normal.X, normal.Y);
            }

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
        }

        //EffectParameters
        public enum EffectParams_FA
        {
            // The "10" is to account for FD or LM specific params.
            // This will need to be increased if more params are 
            // added to the FD or LM methods. 
            // ToDo (DZ): We may need to rethink this effect cache 
            // thing... It seems very fragile to have this "10" 
            // constant.
            Inversion_FA = EffectParamsLength + 10,
            BumpToWorld_FA,
        }
        public EffectParameter ParameterColor(EffectParams_FA param)
        {
            return effectCacheColor.Parameter((int)param);
        }
        public EffectParameter ParameterEdit(EffectParams_FA param)
        {
            return effectCacheEdit.Parameter((int)param);
        }

        //Parameter helpers
        public void SetGlobalParams_FA()
        {
            ParameterColor(EffectParams_FA.Inversion_FA).SetValue(Inversion[(int)Tile.Face.Top]);
            ParameterColor(EffectParams_FA.BumpToWorld_FA).SetValue(BumpToWorld[(int)Tile.Face.Top]);
            ParameterEdit(EffectParams_FA.Inversion_FA).SetValue(Inversion[(int)Tile.Face.Top]);
            ParameterEdit(EffectParams_FA.BumpToWorld_FA).SetValue(BumpToWorld[(int)Tile.Face.Top]);
        }
        public void SetMaterialParams_FA(ushort matIdx, bool forUI)
        {
            TerrainMaterial.Get(matIdx).Setup_FA(forUI);
        }

        #endregion

        public enum EditMode
        {
            Noop,
            PaintMaterial,          // Changes the material of the terrain under the brush.
            PaintAndAddMaterial,    // Paints existing material and adds if none is there.
            MaterialReplace,        // Repaint contiguous material region under cursor with current mat.  // No longer used?
            Raise,
            Lower,
            Delete,
            Smooth,
            Roughen,
            Min,                    // No longer used?
            Max,                    // No longer used?
            AddAtZero,              // Adds terrain using the current material at minimum height.
            AddAtMax,               // Adds terrain using the current material at the max height under the brush.
            AddAtCenter,            // Adds terrain using the current material at the height of the center of the brush.
            WaterRaise,
            WaterLower,
            WaterChange,
            Road,
            RoadSnap,
            Level,
            LevelSnap,              // No longer used?
            Hill,

            RaiseVol,               // No longer used?
            LowerVol,               // No longer used?
            LevelVol                // No longer used?
        };

        // Counters used by the tutorial system.  Note that the tutorial system may
        // reset these at any time so they can't be relied upon for anything else.
        public static int paintCounter = 0;
        public static int addCounter = 0;
        public static int deleteCounter = 0;
        public static int raiseCounter = 0;
        public static int lowerCounter = 0;
        public static int smoothCounter = 0;
        public static int waterCounter = 0;

        private static Effect effectColor = null;
        private static Effect effectEdit = null;
        /// <summary>
        /// Effect parameters common to all RenderMethods
        /// </summary>
        public enum EffectParams
        {
            EditBrushTexture,
            EditBrushStart,
            EditBrushStartToEnd,
            EditBrushRadius,
            EditBrushToParam,
            EditBrushScaleOff,
            InvCubeSize,
            ShadowTexture,
            ShadowMask,
            ShadowTextureOffsetScale,
            ShadowMaskOffsetScale,
            VSIndex,
            PSIndex,
            WorldViewProjMatrix,
            WorldMatrix,
            LightWrap,
            WarpCenter,
        }
        private const int EffectParamsLength = 17;
        private enum EffectTechs
        {
            TerrainDepthPass = InGame.RenderEffect.Normal,
            TerrainEditMode,
            TerrainColorPass,
            PreCursorPass,
        }
        public EffectParameter ParameterColor(EffectParams param)
        {
            return effectCacheColor.Parameter((int)param);
        }
        public EffectParameter ParameterEdit(EffectParams param)
        {
            return effectCacheEdit.Parameter((int)param);
        }
        private static EffectCache effectCacheColor;
        private static EffectCache effectCacheEdit;

        private static void LoadEffect()
        {
            RenderMethod = (RenderMethods)BokuSettings.Settings.TerrainRenderMethod;
            RenderMethod = RenderMethods.FewerDraws;

            switch (RenderMethod)
            {
                case RenderMethods.FewerDraws:
                    effectCacheColor = new EffectCacheWithTechs(new Type[] 
                                                            { 
                                                                typeof(EffectParams), 
                                                                typeof(EffectParams_FD), 
                                                                typeof(EffectParams_FA)
                                                            },
                                                           new Type[] { typeof(EffectTechs) });
                    effectCacheEdit = new EffectCacheWithTechs(new Type[] 
                                                            { 
                                                                typeof(EffectParams), 
                                                                typeof(EffectParams_FD), 
                                                                typeof(EffectParams_FA)
                                                            },
                                                           new Type[] { typeof(EffectTechs) });
                    effectColor = KoiLibrary.LoadEffect(@"Shaders\Terrain_FD_Color");
                    ShaderGlobals.RegisterEffect("Terrain_FD_Color", effectColor);
                    effectEdit = KoiLibrary.LoadEffect(@"Shaders\Terrain_FD_Edit");
                    ShaderGlobals.RegisterEffect("Terrain_FD_Edit", effectEdit);
                    break;
                default:
                    Debug.Assert(false, "Invalid TerrainRenderMethod value in BokuSettings? (Perhaps the render method you are trying to use has its code path disabled!)");
                    throw new NotImplementedException("The terrain RenderMethod provided doesn't exist on this build!");
                //break;
            }

            effectCacheColor.Load(effectColor);
            effectCacheEdit.Load(effectEdit);
        }
        private static void UnLoadEffect()
        {
            DeviceResetX.Release(ref effectColor);
            DeviceResetX.Release(ref effectEdit);

            if (null != effectCacheColor)
            {
                effectCacheColor.UnLoad();
            }
            if (null != effectCacheEdit)
            {
                effectCacheEdit.UnLoad();
            }
        }

        private XmlWorldData xmlWorldData = null;

        private int runTimeSkyIndex;

        private bool skyTransitioning;
        private int skyTwitchId;
        private int transitionToSkyIndex;
        private float transitionSkyAmount;

        private static int[] uiSlotToMatIdx = new int[TerrainMaterial.MaxNum];//ToDo(DZ): Do we want to change this array to ushorts?
        private static int[] matIdxToUISlot = new int[TerrainMaterial.MaxNum];
        private static int[] matIdxToLabel = new int[TerrainMaterial.MaxNum];

        private static Matrix[] bumpToWorld = MakeTransforms();
        private float[] inversion = new float[Tile.NumFaces];

        // Editing support.
        private EditFilter editFilter = null;
        private EditPalette editPalette = null;
        private int lastEdit = 0;

        private bool editing = false;
        private bool waterDirty = false;
        private static ushort currentMatIdx = TerrainMaterial.DefaultMatIdx;
        private float volumeScale = 0.5f;

        private VirtualMap virtualMap = new VirtualMap();

        private static float cost = 0.0f;

        // Height at center of brush.  Used for AddAtCenter().
        private float brushCenterHeight = 0.0f;

        /// This is static so we only create it once and then just share
        /// it while updating it as the terrain changes.
        private static WaterParticleEmitter waterParticleEmitter = null;

        #region Debug
#if Debug_CountTerrainVerts
        public static int VertCounter_Debug = 0;
        public static int TriCounter_Debug = 0;
#endif
        #endregion

        #region Accessors
        public bool AllowCaching
        {
            get
            {
                return false ;
            }
        }
        public float MinHeight
        {
            get
            {
                return InGame.inGame.SnapToGrid ? 0.5f : VirtualMap.MinHeight;
            }
        }
        public float MaxHeight
        {
            get { return VirtualMap.LandMax.Z; }
        }
        public Effect EffectColor
        {
            get { return effectColor; }
        }
        public Effect EffectEdit
        {
            get { return effectEdit; }
        }
        /// <summary>
        /// The index of the current material for painting.
        /// </summary>
        public static ushort CurrentMaterialIndex
        {
            get { return currentMatIdx; }
            set { currentMatIdx = value; }
        }
        public int LastEdit
        {
            get { return lastEdit; }
        }
        /// <summary>
        /// Maximum of all water body base heights in the level.
        /// </summary>
        public static float MaxWaterHeight
        {
            get { return Current != null ? Current.VirtualMap.MaxWaterHeight : 0.0f; }
        }
        /// <summary>
        /// Bounds of current renderable terrain geometry.
        /// </summary>
        public static Vector3 Min
        {
            get { return Current != null ? Current.VirtualMap.LandMin : Vector3.Zero; }
        }
        /// <summary>
        /// Bounds of current renderable terrain geometry.
        /// </summary>
        public static Vector3 Max
        {
            get { return Current != null ? Current.VirtualMap.LandMax : Vector3.Zero; }
        }
        /// <summary>
        /// 2d bounds of current renderable terrain geometry.
        /// </summary>
        public static Vector2 Min2D
        {
            get
            {
                Vector3 min3d = Min;
                return new Vector2(min3d.X, min3d.Y);
            }
        }
        /// <summary>
        /// 2d bounds of current renderable terrain geometry.
        /// </summary>
        public static Vector2 Max2D
        {
            get
            {
                Vector3 max3d = Max;
                return new Vector2(max3d.X, max3d.Y);
            }
        }
        private VirtualMap VirtualMap
        {
            get { return virtualMap; }
            set { virtualMap = value; }
        }
        public float CubeSize
        {
            get { return VirtualMap.CubeSize; }
        }
        public WaterParticleEmitter WaterParticleEmitter
        {
            get { return waterParticleEmitter; }
        }
        public bool Editing
        {
            get { return editing; }
            set
            {
                editing = value;
                if (editing)
                {
                    lastEdit = Time.FrameCounter;
                }
            }
        }

        public bool WaterDirty
        {
            get { return waterDirty; }
            set { waterDirty = value; }
        }

        /// <summary>
        /// For each face, a current highlit amount.
        /// </summary>
        public float[] Inversion
        {
            get { return inversion; }
        }
        /// <summary>
        /// Transform a normal from surface space to world space
        /// </summary>
        public static Matrix[] BumpToWorld
        {
            get { return bumpToWorld; }
        }

        /// <summary>
        /// An estimate of the cost per vertex normalized to the cost of a 
        /// standard bot being 1.0f.
        /// </summary>
        static public float CostPerVertex
        {
            //ToDo (DZ): We need to recalibrate this number for PCs.
            // Also, we may need to calibrate this on a per-render
            // method and/or a per-shader model version basis.
            get { return 1.0f / (32 * 32) / 7.0f; }
        }


        //
        // XmlWorldData accessors
        //

        /// <summary>
        /// Level specific world params.
        /// </summary>
        public XmlWorldData XmlWorldData
        {
            get { return xmlWorldData; }
        }

        /// <summary>
        /// Guid for this world.
        /// </summary>
        public Guid WorldID
        {
            get { return xmlWorldData.id; }
        }

        /// <summary>
        /// Name of current world.
        /// </summary>
        public string Name
        {
            get { return xmlWorldData.name; }
        }

        /// <summary>
        /// Description of current world.
        /// </summary>
        public string Description
        {
            get { return xmlWorldData.description; }
        }

        /// <summary>
        /// Creator of current world.
        /// </summary>
        public string Creator
        {
            get { return xmlWorldData.creator; }
        }

        /// <summary>
        /// Rating for current world.
        /// </summary>
        public float Rating
        {
            get { return xmlWorldData.rating; }
        }

        /// <summary>
        /// Does the world has "glass walls" which prevent objects from falling off the edge of the world?
        /// </summary>
        public bool GlassWalls
        {
            get { return xmlWorldData.glassWalls; }
            set { xmlWorldData.glassWalls = value; }
        }

        /// <summary>
        /// Is there a user specified, fixed camera for this world?
        /// </summary>
        public bool FixedCamera
        {
            get { return xmlWorldData.fixedCamera; }
            set { xmlWorldData.fixedCamera = value; }
        }

        /// <summary>
        /// The look-from position of the fixed camera.
        /// </summary>
        public Vector3 FixedCameraFrom
        {
            get { return xmlWorldData.FixedCameraFrom; }
            set { xmlWorldData.FixedCameraFrom = value; }
        }
        /// <summary>
        /// The look-at position of the fixed camera.
        /// </summary>
        public Vector3 FixedCameraAt
        {
            get { return xmlWorldData.FixedCameraAt; }
            set { xmlWorldData.FixedCameraAt = value; }
        }

        /// <summary>
        /// The rotation around the vertical axis of the fixed camera.
        /// </summary>
        public float FixedCameraRotation
        {
            get { return xmlWorldData.FixedCameraRotation; }
            set { xmlWorldData.FixedCameraRotation = value; }
        }

        /// <summary>
        /// The pitch of the fixed camera.
        /// </summary>
        public float FixedCameraPitch
        {
            get { return xmlWorldData.FixedCameraPitch; }
            set { xmlWorldData.FixedCameraPitch = value; }
        }

        /// <summary>
        /// The distance of the fixed camera to the viewing target.
        /// </summary>
        public float FixedCameraDistance
        {
            get { return xmlWorldData.FixedCameraDistance; }
            set { xmlWorldData.FixedCameraDistance = value; }
        }

        /// <summary>
        /// Is there a user specified, fixed offset for the camera in this world?
        /// </summary>
        public bool FixedOffsetCamera
        {
            get { return xmlWorldData.fixedOffsetCamera; }
            set { xmlWorldData.fixedOffsetCamera = value; }
        }

        /// <summary>
        /// The offset value (in world space) used for the fixed offset camera.
        /// </summary>
        public Vector3 FixedOffset
        {
            get { return xmlWorldData.fixedOffset; }
            set { xmlWorldData.fixedOffset = value; }
        }

        /// <summary>
        /// Display the compass during run sim.
        /// </summary>
        public bool ShowCompass
        {
            get { return xmlWorldData.ShowCompass; }
            set { xmlWorldData.ShowCompass = value; }
        }

        /// <summary>
        /// Display the resource meter during run sim.
        /// </summary>
        public bool ShowResourceMeter
        {
            get { return xmlWorldData.ShowResourceMeter; }
            set { xmlWorldData.ShowResourceMeter = value; }
        }

        /// <summary>
        /// Enables resrouce limiting during edit and run.
        /// </summary>
        public bool EnableResourceLimiting
        {
            get { return xmlWorldData.EnableResourceLimiting; }
            set { xmlWorldData.EnableResourceLimiting = value; }
        }

        public int RunTimeSkyIndex
        {
            get { return runTimeSkyIndex;  }
            set { runTimeSkyIndex = value; }
        }

        public bool SkyTransitioning
        {
            get { return skyTransitioning; }
        }

        public int TransitionToSkyIndex
        {
            get { return transitionToSkyIndex; }
        }

        public float TransitionSkyAmount
        {
            get { return transitionSkyAmount; }
        }

        /// <summary>
        /// Array of colors and weights which define the sky gradient.
        /// </summary>
        public static int SkyIndex
        {
            get { return Current.xmlWorldData.SkyIndex; }
            set { Current.xmlWorldData.SkyIndex = value; 
                  Current.runTimeSkyIndex = value;
                }
        }

        /// <summary>
        /// Nominal amplitude of waves.
        /// </summary>
        public static float WaveHeight
        {
            get
            {
                return Current != null ? Current.XmlWorldData.waveHeight : 0.2f;
            }
            set
            {
                if (Current != null)
                {
                    Current.XmlWorldData.waveHeight = value;
                }
            }
        }
        /// <summary>
        /// How strong the water distortion effect is..
        /// </summary>
        public static float WaterStrength
        {
            get
            {
                return Current != null ? Current.XmlWorldData.waterStrength : 0.2f;
            }
            set
            {
                if (Current != null)
                {
                    Current.XmlWorldData.waterStrength = value;
                }
            }
        }
        /// <summary>
        /// True if there is currently a backlog of water modifications to process.
        /// </summary>
        public static bool WaterBusy
        {
            get { return Current != null ? Current.VirtualMap.WaterBacklog : false; }
        }

        public static int TerraQueue
        {
            get { return Current != null ? Current.VirtualMap.TerraQueue : 0; }
        }

        /// <summary>
        /// Return an estimate of the total cost of the current terrain.
        /// </summary>
        public static float TotalCost
        {
            get { return cost; }
            internal set { cost = value; }
        }

        #endregion

        // c'tor
        public Terrain(XmlWorldData xmlWorldData)
        {
            Init(xmlWorldData);

            // Finish the initialization with a call to LoadContent.  For most objects we
            // don't have to do this explicitly but since the terrain object is not yet created
            // when the first round of calls to LoadGraphicsObject goes through we do it this way.
            BokuGame.Load(this);
        }   // end of Terrain c'tor

        public void Update(Camera camera)
        {
            UpdateHighLight(Time.WallClockTotalSeconds);

            VirtualMap.Update(false);

            WaterParticleEmitter.Active = InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.ToolBox;

            PostBusy();
        }   // end of Terrain Update();

        public void TransitionToSky(int newSkyIndex, float transitionTime)
        {
            //check if another transition was running, if so, don't start another
            if (skyTransitioning)
            {
                //already transitioning to this sky? if so, let it finish
                if (newSkyIndex == transitionToSkyIndex)
                {
                    return;
                }
                StopSkyTransition();
            }

            SkyBox.InitiateTransition();
            transitionToSkyIndex = newSkyIndex;
            transitionSkyAmount = 0.0f;
            skyTransitioning = true;

            //since we're moving from 0.0 to 1.0, use the input as the lerp value (even though it won't technically be linear)
            TwitchManager.Set<float> skyLerp = delegate(float value, Object param)
            {
                transitionSkyAmount = value;
            };

            TwitchCompleteEvent skyLerpComplete = delegate(Object param)
            {
                RunTimeSkyIndex = newSkyIndex;
                skyTransitioning = false;
            };

            TwitchCompleteEvent skyLerpTerminated = delegate(Object param)
            {
                //if still in the sim, update the sky to the target before we start transitioning somewhere else
                //ideally, we'd be able to smoothly blend the sky like we do with water color, but that will require more work
                if (InGame.inGame.CurrentUpdateMode == InGame.UpdateMode.RunSim)
                {
                    RunTimeSkyIndex = newSkyIndex;
                }
            };

            skyTwitchId = TwitchManager.CreateTwitch<float>(0.0f, 1.0f, skyLerp, transitionTime, TwitchCurve.Shape.EaseIn, newSkyIndex, skyLerpComplete, skyLerpTerminated, true);        
        }

        /// <summary>
        /// Cancels any pending sky transitions
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void StopSkyTransition()
        {
            if (skyTransitioning && skyTwitchId >= 0)
            {
                TwitchManager.KillTwitch<float>(skyTwitchId);
                skyTransitioning = false;
                skyTwitchId = -1;
            }
        }

        public static ushort UISlotToMatIndex(int uiSlot)
        {
            ushort matIdx = (ushort)uiSlotToMatIdx[uiSlot];

            return matIdx;
        }
        public static int MaterialIndexToUISlot(ushort matIdx)
        {
            matIdx = TerrainMaterial.GetNonFabric(matIdx);

            return matIdxToUISlot[matIdx];
        }
        public static int MaterialIndexToLabel(ushort matIdx)
        {
            matIdx = TerrainMaterial.GetNonFabric(matIdx);

            return matIdxToLabel[matIdx];
        }

        /// <summary>
        /// Input t is in range [0..1.0). We smooth step from 0=>1 over interval [0..0.5]
        /// and then back down 1=>0 over [0.5=>1)
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private float InversionRamp(double t)
        {
            t = t * 2.0 - 1.0; // [-1..1]
            t = Math.Abs(t); // [0..1]
            t = 1.0 - t;

            return (float)(3.0 * t * t - 2.0 * t * t * t);
        }
        private void UpdateHighLight(double secs)
        {
            double speed = 1.33;
            secs *= speed;

            for (int i = 0; i < Tile.NumFaces; ++i)
            {
                Inversion[i] = 0.0f;
            }

            float mod3 = (float)(secs - ((double)((int)(secs / 2.0)) * 2.0));
            if ((mod3 >= 0) && (mod3 < 1.0))
            {
                Inversion[(int)Tile.Face.Left] = InversionRamp(mod3);
                Inversion[(int)Tile.Face.Right] = Inversion[(int)Tile.Face.Left];

                Inversion[(int)Tile.Face.Top] = Inversion[(int)Tile.Face.Left];
            }
            else
            {
                Inversion[(int)Tile.Face.Back] = InversionRamp(mod3 - 1.0);
                Inversion[(int)Tile.Face.Front] = Inversion[(int)Tile.Face.Back];

                Inversion[(int)Tile.Face.Top] = Inversion[(int)Tile.Face.Back];
            }
        }

        /// <summary>
        /// Returns true if the input material is currently the magic brush selected material.
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static bool IsSelectedMaterial(TerrainMaterial mat)
        {
            if (Current != null)
            {
                ushort selectedMatIdx = Current.VirtualMap.SelectedMatIdx;
                if ((TerrainMaterial.IsValid(selectedMatIdx, false, false))
                    && (TerrainMaterial.Get(selectedMatIdx) == mat))
                {
                    return true;
                }
            }
            return false;
        }

        private static void CheckMaterials()
        {
            MaterialInfo.InvalidInfo.Reset();
            for (ushort i = 0; i < TerrainMaterial.MaxNum; ++i)
            {
                if (TerrainMaterial.Materials[i] == null)
                {
                    TerrainMaterial.Materials[i] = new TerrainMaterial();
                }
            }
        }
        public XmlTerrainData2 Convert(XmlTerrainData terrainData)
        {
            // Load the HeightMap
            Point size = new Point(terrainData.size.X, terrainData.size.Y);
            Vector3 scale = terrainData.scale;
            HeightMap heightMap = new HeightMap(
                BokuGame.Settings.MediaPath + terrainData.heightMapFilename,
                size,
                scale);

            /// Convert to new form by dicing it up.
            VirtualMap.InitFromSingle(heightMap);

            XmlTerrainData2 terrainData2 = new XmlTerrainData2();

            terrainData2.virtualMapFile = terrainData.heightMapFilename + "vm";
            terrainData2.cubeSize = VirtualMap.CubeSize;

            SaveHeight(BokuGame.Settings.MediaPath + terrainData2.virtualMapFile);

            return terrainData2;
        }
        public void Init(XmlWorldData xmlWorldData)
        {
            CheckMaterials();
            this.xmlWorldData = xmlWorldData;
            if (xmlWorldData.xmlTerrainData != null)
            {
                xmlWorldData.xmlTerrainData2 = Convert(xmlWorldData.xmlTerrainData);
                xmlWorldData.xmlTerrainData = null;
            }

            //give the map one last update to clear out any operations in progress
            VirtualMap.Update(true);

            VirtualMap.Load(
                BokuGame.Settings.MediaPath + xmlWorldData.xmlTerrainData2.virtualMapFile,
                xmlWorldData.xmlTerrainData2.cubeSize);

            VirtualMap.InitWater(xmlWorldData.xmlTerrainData2.waters);

            // ...and the water particles.
            if (waterParticleEmitter == null)
            {
                waterParticleEmitter = new WaterParticleEmitter(InGame.inGame.ParticleSystemManager);
            }

            // Update the water particles.
            RebuildWater();

            // Initialize runtime sky index
            runTimeSkyIndex = xmlWorldData.SkyIndex;

        }   // end of Terrain Init()

        /// <summary>
        /// Save off the height data
        /// </summary>
        /// <param name="path"></param>
        public void SaveHeight(string path)
        {
            if (xmlWorldData.xmlTerrainData2 != null)
            {
                xmlWorldData.xmlTerrainData2.waters = Water.CopyWaters();
            }

            VirtualMap.Save(path);
        }

        public static float GetWaterHeightAndNormal(Vector3 position, ref Vector3 normal)
        {
            return GetWaterHeightAndNormal(new Vector2(position.X, position.Y), ref normal);
        }

        public static float GetWaterHeightAndNormal(Vector2 position, ref Vector3 normal)
        {
            float height = 0.0f;
            if (Current != null)
            {
                Vector3 normalDir = Vector3.UnitZ;

                Water water = Current.VirtualMap.GetWater(position);
                if (water != null)
                {
                    float offset = GetCycleOffset(position, ref normalDir);
                    offset -= (float)Time.GameTimeTotalSeconds;
                    height = Terrain.WaveHeight * (float)(Math.Sin(offset) - 1.0);
                    height += water.BaseHeight;

                    Vector3 up = new Vector3(0.0f, 0.0f, 1.0f);
                    normal = up - 0.5f * Terrain.WaveHeight * normalDir * (float)Math.Cos(offset);
                    normal.Normalize();
                }
            }

            return height;

        }   // end of GetWaterHeightAndNormal()

        /// <summary>
        /// Get the world height of the water (including waves) at position.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static float GetWaterHeight(Vector3 pos)
        {
            return GetWaterHeight(new Vector2(pos.X, pos.Y));
        }
        /// <summary>
        /// Get the world height of the water (including waves) at position.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static float GetWaterHeight(Vector2 position)
        {
            float height = 0.0f;
            if (Current != null)
            {
                Vector3 normalDir = Vector3.UnitZ;

                Water water = Current.VirtualMap.GetWater(position);
                height = GetWaterHeight(water, position);
            }
            return height;
        }
        /// <summary>
        /// Get the height including waves for this water body at given position.
        /// Validity of position for this water body's extent is not checked.
        /// </summary>
        /// <param name="water"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static float GetWaterHeight(Water water, Vector3 position)
        {
            return GetWaterHeight(water, new Vector2(position.X, position.Y));
        }
        /// <summary>
        /// Get the height including waves for this water body at given position.
        /// Validity of position for this water body's extent is not checked.
        /// </summary>
        /// <param name="water"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static float GetWaterHeight(Water water, Vector2 position)
        {
            float height = 0.0f;
            if (water != null)
            {
                Vector3 normalDir = Vector3.Zero;
                float offset = GetCycleOffset(position, ref normalDir);
                offset -= (float)Time.GameTimeTotalSeconds;
                height = Terrain.WaveHeight * (float)(Math.Sin(offset) - 1.0);
                height += water.BaseHeight;
            }
            return height;
        }
        /// <summary>
        /// Get the world height of the water (w/o waves) at position.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static float GetWaterBase(Vector3 pos)
        {
            return GetWaterBase(new Vector2(pos.X, pos.Y));
        }
        /// <summary>
        /// Get the world height of the water (w/o waves) at position.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static float GetWaterBase(Vector2 position)
        {
            float height = 0.0f;
            if (Current != null)
            {
                Water water = Current.VirtualMap.GetWater(position);
                if (water != null)
                {
                    height = water.BaseHeight;
                }
            }
            return height;
        }
        /// <summary>
        /// Return the type of water at given position, or Water.InvalidType if there's no water there.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static int GetWaterType(Vector3 pos)
        {
            return GetWaterType(new Vector2(pos.X, pos.Y));
        }
        /// <summary>
        /// Return the type of water at given position, or Water.InvalidType if there's no water there.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static int GetWaterType(Vector2 pos)
        {
            if (Current != null)
            {
                Water water = Current.VirtualMap.GetWater(pos);
                if (water != null)
                {
                    return water.Type;
                }
            }
            return Water.InvalidType;
        }
        /// <summary>
        /// Return whether the 2d input position is covered by water.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static bool IsWater(Vector2 position)
        {
            if (Current != null)
            {
                return Current.VirtualMap.IsWater(position);
            }
            return false;
        }
        /// <summary>
        /// Return whether the 2d input position is covered by water.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static bool IsWater(Vector3 pos)
        {
            return IsWater(new Vector2(pos.X, pos.Y));
        }

        /// <summary>
        /// Return whether the 3d position is under a water surface. (DC only, ignores waves.)
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static bool InWater(Vector3 pos)
        {
            float waterHeight = GetWaterBase(pos);

            return (waterHeight > 0.0f) && (waterHeight > pos.Z);
        }

        /// <summary>
        /// Return the water body at the given world position. May be null.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static Water GetWater(Vector2 pos)
        {
            Debug.Assert(Current != null);
            return Current.VirtualMap.GetWater(pos);
        }

        /// <summary>
        ///  Return the water body at the given virtual index. May be null.
        /// </summary>
        /// <param name="virtPos"></param>
        /// <returns></returns>
        public static Water GetWater(Point virtPos)
        {
            Debug.Assert(Current != null);
            return Current.VirtualMap.GetWater(virtPos);
        }

        /// <summary>
        /// Return the (short) material index at given position. If no terrain
        /// there, return the empty material index. 
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static ushort GetMaterialType(Vector2 pos)
        {
            if (Current == null)
            {
                return TerrainMaterial.EmptyMatIdx;
            }

            VirtualMap vMap = Current.VirtualMap;
            Point virtIndex = vMap.WorldToVirtualIndex(pos);
            
            var matIdx = vMap.GetColor(virtIndex.X, virtIndex.Y, TerrainMaterial.EmptyMatIdx);

            matIdx = TerrainMaterial.RemoveFlags(matIdx, TerrainMaterial.Flags.Selection);

            return matIdx;
        }

        /// <summary>
        /// Return the current terrain instance.
        /// </summary>
        public static Terrain Current
        {
            get { return InGame.inGame.Terrain; }
        }
        /// <summary>
        /// Returns the offset into the sine wave cycle for this position used for modelling waves.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="normal">A vector orthogonal to the wave front used to calculate the modified normal.</param>
        /// <returns></returns>
        public static float GetCycleOffset(Vector2 position, ref Vector3 normal)
        {
            Vector2 WaveSource = new Vector2(127.0f, 600.0f);
            const float wavelength = 15.0f;

            Vector2 dir = position - WaveSource;

            float cycleOffset = dir.Length() / wavelength * 2.0f * (float)Math.PI;
            dir.Normalize();
            normal = new Vector3(dir.X, dir.Y, 0.0f);

            return cycleOffset;
        }   // end of GetCycleOffset()

        /// <summary>
        /// Return the height of the terrain, ignoring anything on the terrain (e.g. roads)
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static float GetTerrainHeightFlat(Vector2 pos)
        {
            return Current != null ? Current.VirtualMap.GetHeightFlat(pos) : 0.0f;
        }
        /// <summary>
        /// Return the height of the terrain, ignoring anything on the terrain (e.g. roads)
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static float GetTerrainHeightFlat(Vector3 pos)
        {
            return GetTerrainHeightFlat(new Vector2(pos.X, pos.Y));
        }
        /// <summary>
        /// Return the height of the terrain, ignoring anything on the terrain (e.g. roads)
        /// Also returns terrain and water material info for that spot.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="matInfo"></param>
        /// <returns></returns>
        public static float GetTerrainHeightFlat(Vector2 pos, ref MaterialInfo matInfo)
        {
            return Current != null ? Current.VirtualMap.GetHeightAndMaterial(pos, out matInfo) : 0.0f;
        }
        /// <summary>
        /// Return the height of the terrain, ignoring anything on the terrain (e.g. roads)
        /// Also returns terrain and water material info for that spot.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="matInfo"></param>
        /// <returns></returns>
        public static float GetTerrainHeightFlat(Vector3 pos, ref MaterialInfo matInfo)
        {
            return GetTerrainHeightFlat(new Vector2(pos.X, pos.Y), ref matInfo);
        }
        /// <summary>
        /// Return the height of the terrain, ignoring anything on the terrain (e.g. roads)
        /// Gets the smoothed (interpolated) height.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static float GetTerrainHeight(Vector2 pos)
        {
            return Current != null ? Current.VirtualMap.GetHeight(pos) : 0.0f;
        }
        /// <summary>
        /// Return the height of the terrain, ignoring anything on the terrain (e.g. roads)
        /// Gets the smoothed (interpolated) height.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static float GetTerrainHeight(Vector3 pos)
        {
            return GetTerrainHeight(new Vector2(pos.X, pos.Y));
        }

        /// <summary>
        /// Gets the smoothed (interpolated) height.
        /// Updates matInfo.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="matInfo"></param>
        /// <returns></returns>
        public static float GetTerrainHeight(Vector3 pos, ref MaterialInfo matInfo)
        {
            return GetTerrainHeight(new Vector2(pos.X, pos.Y), ref matInfo);
        }

        /// <summary>
        /// Gets the smoothed (interpolated) height.
        /// Updates matInfo.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="matInfo"></param>
        /// <returns></returns>
        public static float GetTerrainHeight(Vector2 pos, ref MaterialInfo matInfo)
        {
            // The following returns the flat height which we ignore.  We still need
            // the matInfo struct filled out though.
            GetTerrainHeightFlat(pos, ref matInfo);

            // Get the smoothed height.
            float height = Current != null ? Current.VirtualMap.GetHeight(pos) : 0.0f;

            return height;
        }

        /// <summary>
        /// Returns the height of the heightmap including any roads/walls at the given position.
        /// Version of GetTerrainAndPathHeight() which doesn't take overPath arg.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static float GetTerrainAndPathHeight(Vector3 pos)
        {
            bool overPath = false;
            return GetTerrainAndPathHeight(pos, ref overPath);
        }

        /// <summary>
        /// Returns the height of the heightmap including any roads/walls at the given position.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="overPath">Are we currently over a path.</param>
        /// <returns></returns>
        public static float GetTerrainAndPathHeight(Vector3 pos, ref bool overPath)
        {
            ushort material = GetMaterialType(new Vector2(pos.X, pos.Y));
            bool fabric = (material & (int)TerrainMaterial.Flags.Fabric) != 0;

            float terrainHeight = 0;

            if (fabric)
            {
                terrainHeight = GetTerrainHeight(pos);
            }
            else
            {
                terrainHeight = GetTerrainHeightFlat(pos);
            }

            float wayHeight = 0.0f;
            if (WayPoint.GetHeight(pos, ref wayHeight))
            {
                overPath = wayHeight > terrainHeight;
                terrainHeight = Math.Max(terrainHeight, wayHeight);
            }
            else
            {
                overPath = false;
            }


            // TODO (****) No clue why we limit the height here.  In what case is this the right
            // thing to do?
            // Commented this out since it was messing with setting the initial InsideGlassWalls value.
            // That code looks for a height of 0.
            /*
            if ((Current != null) && (InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.RunSim))
            {
                terrainHeight = Math.Max(terrainHeight, Current.MinHeight);
            }
            */

            return terrainHeight;
        }
        /// <summary>
        /// Returns the height and normal of the heightmap including any roads/walls at the given position.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static float GetTerrainAndPathHeight(Vector3 pos, out Vector3 normal)
        {
            float terrainHeight = GetTerrainHeightFlat(pos);
            normal = Vector3.UnitZ;
            float wayHeight = 0.0f;
            Vector3 wayNormal = Vector3.Zero;
            if (WayPoint.GetHeightAndNormal(pos, ref wayHeight, ref wayNormal))
            {
                if (wayHeight >= terrainHeight)
                {
                    terrainHeight = wayHeight;
                    normal = wayNormal;
                }
            }
            if ((Current != null) && (InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.RunSim))
            {
                terrainHeight = Math.Max(terrainHeight, Current.MinHeight);
            }

            return terrainHeight;
        }

        /// <summary>
        /// Helper struct, for returning the types of terrain and water at query position.
        /// </summary>
        public struct MaterialInfo
        {
            public ushort TerrainType;                  // Terrain type with the fabric and selected flags removed.
            public int WaterType;
            public bool IsFabric;                       // Is the underlying terrain type fabric?
            public Classification.Colors PathColor;     // If over a path, what is the color?

            /// <summary>
            /// True if there is no terrain here.
            /// </summary>
            public bool NoTerrain
            {
                get { return TerrainType == TerrainMaterial.EmptyMatIdx; }
            }
            /// <summary>
            /// True if there is no water here.
            /// </summary>
            public bool NoWater
            {
                get { return WaterType == Terra.Water.InvalidType; }
            }

            /// <summary>
            /// Reset to terrain-less/water-less state.
            /// </summary>
            public void Reset()
            {
                ResetTerrain();
                ResetWater();
                ResetPathColor();
            }
            /// <summary>
            /// Reset terrain to no-terrain.
            /// </summary>
            public void ResetTerrain()
            {
                TerrainType = TerrainMaterial.EmptyMatIdx;
            }
            /// <summary>
            /// Reset water to no-water.
            /// </summary>
            public void ResetWater()
            {
                WaterType = Terra.Water.InvalidType;
            }

            public void ResetPathColor()
            {
                PathColor = Classification.Colors.NotApplicable;
            }

            public static MaterialInfo InvalidInfo;
        };
        /// <summary>
        /// Helper class for keeping track of what types are in a set. This is a very naive
        /// implementation, because it's only expected to have about 5 or less types in the list.
        /// A more sophisticated implementation will be straightforward, but would currently be
        /// a net waste.
        /// </summary>
        public class TypeList
        {
            #region Members
            private List<int> types = new List<int>();
            #endregion Members

            #region Public
            /// <summary>
            /// Constructor
            /// </summary>
            public TypeList()
            {
            }

            /// <summary>
            /// Is there anything in it?
            /// </summary>
            public bool Empty
            {
                get { return types.Count == 0; }
            }

            /// <summary>
            /// Is this specific type in it?
            /// </summary>
            /// <param name="type"></param>
            /// <returns></returns>
            public bool HasType(int type)
            {
                for (int i = 0; i < types.Count; ++i)
                {
                    if (types[i] == type)
                        return true;
                }
                return false;
            }

            /// <summary>
            /// Is there any type that meets the provided validity test?
            /// </summary>
            public bool HasAnyValid(Predicate<int> validityTester)
            {
                for (int i = 0; i < types.Count; ++i)
                {
                    if (validityTester(types[i]))
                        return true;
                }
                return false;
            }

            /// <summary>
            /// Add a type to the set.
            /// </summary>
            /// <param name="type"></param>
            public void AddType(int type)
            {
                types.Add(type);
            }

            /// <summary>
            /// Clear all types.
            /// </summary>
            public void Clear()
            {
                types.Clear();
            }
            #endregion Public
        }
        /// <summary>
        /// Return height of heightmap including any roads/walls at given position.
        /// Also includes terrain and water material at that spot, but not path specific info.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="matInfo"></param>
        /// <returns></returns>
        public static float GetTerrainAndPathHeight(Vector3 pos, ref MaterialInfo matInfo)
        {
            float terrainHeight = 0;

            if (matInfo.IsFabric)
            {
                terrainHeight = GetTerrainHeight(pos, ref matInfo);
            }
            else
            {
                terrainHeight = GetTerrainHeightFlat(pos, ref matInfo);
            }

            float wayHeight = 0.0f;
            Classification.Colors color = Classification.Colors.NotApplicable;
            if (WayPoint.GetHeight(pos, ref wayHeight, ref color))
            {
                terrainHeight = Math.Max(terrainHeight, wayHeight);
                matInfo.PathColor = color;
            }
            if ((Current != null) && (InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.RunSim))
            {
                terrainHeight = Math.Max(terrainHeight, Current.MinHeight);
            }

            return terrainHeight;
        }

        /// <summary>
        /// Returns the interpolated height of the heightmap at the given position.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static float GetHeight(Vector3 pos)
        {
            float height = GetTerrainHeight(pos);
            float wayHeight = 0.0f;
            if (WayPoint.GetHeight(pos, ref wayHeight))
            {
                height = wayHeight;
            }
            if ((Current != null) && (InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.RunSim))
            {
                height = Math.Max(height, Current.MinHeight);
            }
            return height;
        }   // end of Terrain RenderObj GetHeight()

        /// <summary>
        /// Return the interpolated normal at given position.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static Vector3 GetNormal(Vector3 pos)
        {
            if (Current != null)
            {
                Vector3 normal = Vector3.Zero;
                float height = 0.0f;
                if (WayPoint.GetHeightAndNormal(pos, ref height, ref normal))
                {
                    return normal;
                }
                return Current.VirtualMap.GetNormal(pos);
            }
            return Vector3.UnitZ;
        }

        public static Vector3 GetTerrainNormal(Vector3 pos)
        {
            return GetTerrainNormal(new Vector2(pos.X, pos.Y));
        }
        public static Vector3 GetTerrainNormal(Vector2 pos)
        {
            return Current != null ? Current.VirtualMap.GetNormal(pos) : Vector3.UnitZ;
        }

        /// <summary>
        /// Simple struct for returning LOS blockage information.
        /// </summary>
        public struct HitBlock
        {
            /// <summary>
            /// Where the hit occurred.
            /// </summary>
            public Vector3 Position;
            /// <summary>
            /// Surface normal at the hit.
            /// </summary>
            public Vector3 Normal;
            /// <summary>
            /// Height at hit.
            /// </summary>
            public float BlockHeight;
            /// <summary>
            /// Min height over span of ray before hit.
            /// </summary>
            public float Min;
            /// <summary>
            /// Max height over span of ray before hit.
            /// </summary>
            public float Max;
            /// <summary>
            /// True if the ray crosses over a path
            /// </summary>
            public bool CrossesPath;
            /// <summary>
            /// Tallest step up taken
            /// </summary>
            public float HiStep;
            /// <summary>
            /// Biggest single step drop
            /// </summary>
            public float LoStep;
            /// <summary>
            /// The deepest water encountered.
            /// </summary>
            public float MaxWater;
            /// <summary>
            /// True if we started on land
            /// </summary>
            public bool LandStart;

            /// <summary>
            /// Helper function to stuff data.
            /// </summary>
            /// <param name="hw"></param>
            /// <param name="prevH"></param>
            /// <param name="pos"></param>
            public void Absorb(Vector2 hw, Vector2 prevH, Vector3 pos)
            {
                Min = Math.Min(hw.X, Min);
                Max = Math.Max(hw.X, Max);

                float stepH = hw.X - prevH.X;
                HiStep = Math.Max(HiStep, stepH);
                LoStep = Math.Min(LoStep, stepH);

                MaxWater = Math.Max(MaxWater, hw.Y);
            }
        }
        /// <summary>
        /// Do ray test from p0 TO p1, looking for crossings into cells with 
        /// height LEqual minMaxH.X or height GEqual minMaxH.Y.
        /// Returns true if such a crossing is found, and sets
        /// norm to be the normal of said crossing (norm.Z will be zero),
        /// and hHit to be the height of blocking cell.
        /// To ignore end of world crossings, set minMaxH.X == -1, to catch end of world
        /// crossings set minMaxH.X == 0.0f.
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="minMaxH"></param>
        /// <param name="norm"></param>
        /// <param name="hHit"></param>
        /// <returns></returns>
        public static bool Blocked(Vector3 src, Vector3 dst, Vector2 minMaxH, Vector4 maxStep, ref HitBlock hitBlock, float curActorHeight)
        {
            if (Current != null)
            {
                if ((src - dst) == Vector3.Zero)
                {
                    // No movement, not blocked.
                    return false;
                }

                WayPoint.ResetAbstain();

                Vector3 wallPos = Vector3.Zero;
                bool hitWall = false;
                if (WayPoint.Blocked(src, dst, ref hitBlock))
                {
                    dst = hitBlock.Position;
                    hitWall = true;
                    wallPos = hitBlock.Position;
                }

                bool hitLand = false;
                bool testPaths = hitBlock.CrossesPath;
                while (true)
                {
                    hitLand = false;
                    hitBlock.CrossesPath = false;

                    if (Current.VirtualMap.Blocked(src, dst, minMaxH, maxStep, ref hitBlock, curActorHeight))
                    {
                        /// Note that we just trashed any value hitBlock.Position
                        /// might have had from WayPoint.Blocked().
                        hitLand = true;
                        if (hitBlock.BlockHeight > 0.0f)
                        {
                            break;
                        }
                        src = hitBlock.Position;
                    }
                    else
                    {
                        break;
                    }
                    if (!testPaths || !WayPoint.LeavesRoad(src, dst, ref hitBlock))
                    //if (!WayPoint.LeavesRoad(src, dst, ref hitBlock))
                    {
                        hitLand = !hitBlock.CrossesPath;
                        break;
                    }
                    src = hitBlock.Position;
                }
                if (hitWall && !hitLand)
                {
                    hitBlock.Position = wallPos;
                }

                return hitWall || hitLand;
            }

            hitBlock.Min = hitBlock.Max = 0.0f;
            return false;
        }

#if false
        /// <summary>
        /// Presumes that src is not on land, will search from src to dst
        /// for the first land (including paths) position.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="hitBlock"></param>
        /// <returns></returns>
        public static bool FindLand(Vector3 src, Vector3 dst, ref HitBlock hitBlock)
        {
            float minHeight = 0.0f;
            Vector2 minMaxH = new Vector2(-1.0f, minHeight);
            float srcZ = src.Z;
            src.Z = dst.Z = minMaxH.Y;
            bool found = Blocked(src, dst, minMaxH, ref hitBlock);
            if(found)
            {
                hitBlock.Position.Z = srcZ;
            }
            return found;
        }

        /// <summary>
        /// Presumes src is on land (or path), searches toward dst for the
        /// end of the world.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="hitBlock"></param>
        /// <returns></returns>
        public static bool FindEdgeOfWorld(Vector3 src, Vector3 dst, ref HitBlock hitBlock)
        {
            Vector2 minMaxH = new Vector2(0.0f, Single.MaxValue);
            float srcZ = src.Z;
            src.Z = dst.Z = minMaxH.Y;
            bool found = Blocked(src, dst, minMaxH, ref hitBlock);
            if (found)
            {
                hitBlock.Position.Z = srcZ;
            }
            return found;
        }

        /// <summary>
        /// Ignores end of world conditions, and just searches for blocking
        /// walls between src and dst. A wall (or cliff) is considered blocking
        /// if it is higher than src.Z.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="hitBlock"></param>
        /// <returns></returns>
        public static bool FindWall(Vector3 src, Vector3 dst, ref HitBlock hitBlock)
        {
            Vector2 minMaxH = new Vector2(-1.0f, src.Z);
            dst.Z = src.Z;
            return Blocked(src, dst, minMaxH, ref hitBlock);
        }

        /// <summary>
        /// Search from src to dst for the first blocking wall. 
        /// A wall is considered blocking if it is higher than src.Z.
        /// The edge of the world is treated as an infinitely high wall.
        /// If src is off the edge of the world, src is returned as the 
        /// blocking position.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="hitBlock"></param>
        /// <returns></returns>
        public static bool FindWallOrEdge(Vector3 src, Vector3 dst, ref HitBlock hitBlock)
        {
            Vector2 minMaxH = new Vector2(0.0f, src.Z);
            dst.Z = src.Z;
            return Blocked(src, dst, minMaxH, ref hitBlock);
        }
#endif

        /// <summary>
        /// Test whether or not two points are visible
        /// to each other, checking terrain and paths.
        /// Returns true if visible, false if not.
        /// </summary>
        public static bool Visible(Vector3 src, Vector3 dst)
        {
            if (Current != null)
            {
                Vector3 hitPoint = Vector3.Zero;

                if (Current.VirtualMap.LOSCheck(src, dst, ref hitPoint))
                {
                    return false;
                }
                HitBlock hitBlock = new HitBlock();
                if (WayPoint.Blocked(src, dst, ref hitBlock))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Perform an LOS check from src to dst looking at terrain and path blockers (but
        /// not objects).
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="hitPoint"></param>
        /// <returns></returns>
        public static bool LOSCheckTerrainAndPath(Vector3 src, Vector3 dst, ref Vector3 hitPoint)
        {
            bool hitSomething = false;
            if (Current != null)
            {
                if (Current.VirtualMap.LOSCheck(src, dst, ref hitPoint))
                {
                    dst = hitPoint;
                    hitSomething = true;
                }
                HitBlock hitBlock = new HitBlock();
                if (WayPoint.Blocked(src, dst, ref hitBlock))
                {
                    /// Because we shorten dst toward src on a terrain hit,
                    /// a waypoint hit must be before the terrain hit.
                    hitSomething = true;
                    hitPoint = hitBlock.Position;
                }
            }
            return hitSomething;
        }

        /// <summary>
        /// Convert the world space position to coordinates into
        /// the virtual heightmap underlying the world.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static Point WorldToVirtualIndex(Vector3 pos)
        {
            return WorldToVirtualIndex(new Vector2(pos.X, pos.Y));
        }
        /// <summary>
        /// Convert the world space position to coordinates into
        /// the virtual heightmap underlying the world.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static Point WorldToVirtualIndex(Vector2 pos)
        {
            Debug.Assert(Current != null);
            return Current.VirtualMap.WorldToVirtualIndex(pos);
        }

        /// <summary>
        /// Get the world space center of the cube with the
        /// given virtual coordinate.
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public static Vector2 VirtualIndexToWorld(Point idx)
        {
            Debug.Assert(Current != null);
            return Current.VirtualMap.VirtualIndexToWorld(idx.X, idx.Y);
        }

        /// <summary>
        /// Renders the terrain displaying the edit brush.
        /// </summary>
        public void RenderEditMode(
                                    Camera camera,
                                    Texture2D shadowTexture,
                                    Vector2 brushPosition,
                                    Vector2 brushStart,
                                    float brushRadius)
        {
            Brush2DManager.Brush2D brush = Brush2DManager.GetActiveBrush();
            Debug.Assert(brush != null, "Shouldn't be entering RenderEditMode without a valid brush");

            if (!brush.IsLinear)
            {
                brushStart = brushPosition;
            }

            ParameterEdit(EffectParams.EditBrushTexture).SetValue(brush.Texture);
            ParameterEdit(EffectParams.EditBrushStart).SetValue(brushStart);
            ParameterEdit(EffectParams.EditBrushStartToEnd).SetValue(brushPosition - brushStart);
            ParameterEdit(EffectParams.EditBrushRadius).SetValue(brushRadius);
            Vector2 toParam = brushPosition - brushStart;
            float lenSq = toParam.LengthSquared();
            if (lenSq > 0.0f)
                toParam /= toParam.LengthSquared();
            ParameterEdit(EffectParams.EditBrushToParam).SetValue(toParam);
            /// Note this nasty scaling, because brushRadius is really brushDiameter.
            Vector2 scaleOff = new Vector2(0.5f / (brushRadius * 0.5f), 0.5f);
            ParameterEdit(EffectParams.EditBrushScaleOff).SetValue(scaleOff);

            bool editMode = brush.Shape != Brush2DManager.BrushShape.Magic;

            if (editMode)
            {
                PreRenderCursor(camera, brushPosition, brushStart, brushRadius, brush);
            }

            Render(camera, false, editMode);
        }   // end of Terrain RenderEditMode()

        private CursorVertex[] cursorVerts = new CursorVertex[4];
        /// <summary>
        /// Render the cursor onto the zero plane, so you can see where it
        /// is when there's no terrain for it to highlight.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="brushPosition"></param>
        /// <param name="brushRadius"></param>
        /// <param name="brush"></param>
        private void PreRenderCursor(Camera camera,
            Vector2 brushPosition,
            Vector2 brushStart,
            float brushRadius,
            Brush2DManager.Brush2D brush)
        {
            float radius = brushRadius * 0.5f;
            Vector2 brushMin = new Vector2(
                Math.Min(brushPosition.X, brushStart.X) - radius,
                Math.Min(brushPosition.Y, brushStart.Y) - radius);
            Vector2 brushMax = new Vector2(
                Math.Max(brushPosition.X, brushStart.X) + radius,
                Math.Max(brushPosition.Y, brushStart.Y) + radius);

            cursorVerts[0].position = new Vector3(
                brushMin.X,
                brushMin.Y,
                0.0f);
            cursorVerts[0].corner = new Vector2(0.0f, 0.0f);

            cursorVerts[1].position = new Vector3(
                brushMax.X,
                brushMin.Y,
                0.0f);
            cursorVerts[1].corner = new Vector2(1.0f, 0.0f);

            cursorVerts[2].position = new Vector3(
                brushMin.X,
                brushMax.Y,
                0.0f);
            cursorVerts[2].corner = new Vector2(0.0f, 1.0f);

            cursorVerts[3].position = new Vector3(
                brushMax.X,
                brushMax.Y,
                0.0f);
            cursorVerts[3].corner = new Vector2(1.0f, 1.0f);

            effectEdit.CurrentTechnique = effectCacheEdit.Technique((int)EffectTechs.PreCursorPass);

            Matrix viewMatrix = camera.ViewMatrix;
            Matrix projMatrix = camera.ProjectionMatrix;
            Matrix worldMatrix = Matrix.Identity;

            Matrix worldViewProjMatrix = worldMatrix * viewMatrix * projMatrix;
            ParameterEdit(EffectParams.WorldViewProjMatrix).SetValue(worldViewProjMatrix);

            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            foreach (EffectPass pass in effectEdit.CurrentTechnique.Passes)
            {
                pass.Apply();

                device.DrawUserPrimitives<CursorVertex>(
                    PrimitiveType.TriangleStrip,
                    cursorVerts,
                    0,
                    2);

            }

        }   // end of PreRenderCUrsor()

        private bool RenderWire
        {
            get
            {
#if Debug_ToggleRenderWireWithF9
                if (Keyboard.GetState().IsKeyDown(Keys.F9))
                    return true;
                else
#endif
                    return false;
            }
        }

        /// <summary>
        /// Simple message for when we are processing (asynchronously) terrain.
        /// </summary>
        private static SimpleMessage busyMessage = null;
        private bool busyActive = false;
        /// <summary>
        /// Start displaying the busy message.
        /// </summary>
        /// <param name="label"></param>
        private void BeginBusyMessage(string label)
        {
            busyMessage.Text = label;
            InGame.AddMessage(busyMessage.Render, null);
        }
        /// <summary>
        /// Hide the busy message.
        /// </summary>
        private void EndBusyMessage()
        {
            InGame.EndMessage(busyMessage.Render, null);
        }
        /// <summary>
        /// Turn on or off the busy message with specified label.
        /// </summary>
        /// <param name="on"></param>
        /// <param name="label"></param>
        private void PostBusy()
        {
            bool on = WaterBusy;
            if (busyActive)
            {
                // If we're already showing busy, keep showing busy until queue is empty.
                on = on || (TerraQueue > 0);
            }
            else
            {
                // If we're not showing busy, only show if queue > 10.
                on = on || (TerraQueue > 10);
            }
            if (on)
            {
                string label = null;

                if (!busyActive || (busyMessage.Text != label))
                {
                    BeginBusyMessage(label);
                    busyActive = true;
                }
            }
            else if (!on && busyActive)
            {
                EndBusyMessage();
                busyActive = false;
            }
        }

        /// <summary>
        /// Internal version of render that requires the right technique
        /// to have been already set.
        /// </summary>
        public void Render(Camera camera, bool effects, bool editMode)
        {
            //Is there anything to do?
            if (VirtualMap.Empty)
                return;

            VirtualMap.CullCheck(camera);

            var originalEditModeFlag = editMode;

            var device = KoiLibrary.GraphicsDevice;

            //Render wireframe?
            if (RenderWire)
                device.RasterizerState = SharedX.RasterStateWireframe;

            //Set shadow params
            ParameterColor(EffectParams.ShadowTexture).SetValue(InGame.inGame.ShadowCamera.ShadowTexture);
            ParameterColor(EffectParams.ShadowMask).SetValue(InGame.inGame.ShadowCamera.ShadowMask);
            ParameterColor(EffectParams.ShadowTextureOffsetScale).SetValue(InGame.inGame.ShadowCamera.OffsetScale);
            ParameterColor(EffectParams.ShadowMaskOffsetScale).SetValue(InGame.inGame.ShadowCamera.MaskOffsetScale);
            ParameterEdit(EffectParams.ShadowTexture).SetValue(InGame.inGame.ShadowCamera.ShadowTexture);
            ParameterEdit(EffectParams.ShadowMask).SetValue(InGame.inGame.ShadowCamera.ShadowMask);
            ParameterEdit(EffectParams.ShadowTextureOffsetScale).SetValue(InGame.inGame.ShadowCamera.OffsetScale);
            ParameterEdit(EffectParams.ShadowMaskOffsetScale).SetValue(InGame.inGame.ShadowCamera.MaskOffsetScale);

            //Set WorldViewProjMatrix param
            Matrix viewMatrix = camera.ViewMatrix;
            Matrix projMatrix = camera.ProjectionMatrix;
            Matrix worldMatrix = Matrix.Identity;
            Matrix worldViewProjMatrix = worldMatrix * viewMatrix * projMatrix;
            ParameterColor(EffectParams.WorldViewProjMatrix).SetValue(worldViewProjMatrix);
            ParameterColor(EffectParams.WorldMatrix).SetValue(worldMatrix);
            ParameterEdit(EffectParams.WorldViewProjMatrix).SetValue(worldViewProjMatrix);
            ParameterEdit(EffectParams.WorldMatrix).SetValue(worldMatrix);

            //Set WarpCenter
            float worldRadius = (Max - Min).Length() * 0.5f;
            float cameraRadius = (camera.ActualFrom - (Max + Min) * 0.5f).Length();
            float range = Math.Max(worldRadius, cameraRadius) * 2.0f;
            Vector4 warpCenter = new Vector4(
                camera.ActualFrom,
                1.0f / range
                );
            ParameterColor(EffectParams.WarpCenter).SetValue(warpCenter);
            ParameterEdit(EffectParams.WarpCenter).SetValue(warpCenter);

            //Set cube size params
            var cubeSize = VirtualMap.CubeSize;
            var halfCS = cubeSize * 0.5f;
            ParameterColor(EffectParams.InvCubeSize).SetValue(new Vector3(cubeSize, 1.0f / cubeSize, halfCS));
            ParameterEdit(EffectParams.InvCubeSize).SetValue(new Vector3(cubeSize, 1.0f / cubeSize, halfCS));

            //Calculate the max face dot
            float fov = camera.AspectRatio >= 1.0f ? camera.Fov * camera.AspectRatio : camera.Fov;
            float maxFaceDot = (float)Math.Cos((Math.PI - fov) / 2.0);

#if NETFX_CORE
                // Note: Indexing into shaders doesn't work with MG.  Apparently it
                // was some hack done in XNA related to the Effect code they used.
                // Anyway, instead of using this indexing we need to pick and set 
                // the right technique which we do further down from here.
#else
            if (BokuSettings.Settings.PreferReach)
            {
                //Select the VS based on the number of point-lights
                var lightNum = Luz.Count;
                if (lightNum > 6)
                {
                    ParameterColor(EffectParams.VSIndex).SetValue(4);
                    ParameterEdit(EffectParams.VSIndex).SetValue(4);
                }
                else if (lightNum > 4)
                {
                    ParameterColor(EffectParams.VSIndex).SetValue(3);
                    ParameterEdit(EffectParams.VSIndex).SetValue(3);
                }
                else if (lightNum > 2)
                {
                    ParameterColor(EffectParams.VSIndex).SetValue(2);
                    ParameterEdit(EffectParams.VSIndex).SetValue(2);
                }
                else if (lightNum > 0)
                {
                    ParameterColor(EffectParams.VSIndex).SetValue(1);
                    ParameterEdit(EffectParams.VSIndex).SetValue(1);
                }
                else
                {
                    ParameterColor(EffectParams.VSIndex).SetValue(0);
                    ParameterEdit(EffectParams.VSIndex).SetValue(0);
                }

                //Select the PS
                ParameterColor(EffectParams.PSIndex).SetValue(0);
                ParameterEdit(EffectParams.PSIndex).SetValue(0);
            }
            else // Shader Model v3
            {
                //SM3 only uses one VS
                ParameterColor(EffectParams.VSIndex).SetValue(5);
                ParameterEdit(EffectParams.VSIndex).SetValue(5);

                //Select the PS
                ParameterColor(EffectParams.PSIndex).SetValue(2);
                ParameterEdit(EffectParams.PSIndex).SetValue(2);
            }
#endif

#if Debug_CountTerrainVerts
            VertCounter_Debug = 0;
            TriCounter_Debug = 0;
#endif

            #region Cube rendering
            //For the FewerDraws render method, set parameters that are independent
            //of the face here
            if (RenderMethod == RenderMethods.FewerDraws)
            {
                SetGlobalParams_FD();
            }

            //Each material is rendered separately
            for (int matIter = TerrainMaterial.MaxMatIdx; matIter >= -1; --matIter)
            {
                ushort matIdx;
                ushort renderIdx;

                if (matIter == -1)//Render the selection material on the last iteration (this is why matIter != matIdx). It must happen on the last iteration because we set special effect params
                {
                    var selectedIdx = VirtualMap.SelectedMatIdx;
                    renderIdx = TerrainMaterial.SetFlags(selectedIdx, TerrainMaterial.Flags.Selection);
                    if (TerrainMaterial.IsValid(selectedIdx, false, false) 
                        && !TerrainMaterial.IsFabric(selectedIdx) 
                        && TerrainMaterial.IsUsed(renderIdx))
                    {
                        // This is the selection material. It will always be the last material rendered.
                        matIdx = selectedIdx;
                        ParameterColor(EffectParams.EditBrushScaleOff).SetValue(new Vector2(0.0f, 0.5f));
                        ParameterEdit(EffectParams.EditBrushScaleOff).SetValue(new Vector2(0.0f, 0.5f));
                        editMode = true;
                    }
                    else
                        continue;
                }
                else
                {
                    renderIdx = matIdx = (ushort)matIter;
                    if (!TerrainMaterial.IsUsed(matIdx))
                        continue;
                }

                TerrainMaterial mat = TerrainMaterial.Get(matIdx);

                Effect effect = editMode ? effectEdit : effectColor;

                if (editMode)
                {
#if NETFX_CORE
                    int lightNum = Luz.Count;
                    if (lightNum > 6)
                    {
                        effect.CurrentTechnique = effect.Techniques["TerrainEditColorPass_L10_FD_SM2"];
                    }
                    else if (lightNum > 4)
                    {
                        effect.CurrentTechnique = effect.Techniques["TerrainEditColorPass_L6_FD_SM2"];
                    }
                    else if (lightNum > 2)
                    {
                        effect.CurrentTechnique = effect.Techniques["TerrainEditColorPass_L4_FD_SM2"];
                    }
                    else if (lightNum > 0)
                    {
                        effect.CurrentTechnique = effect.Techniques["TerrainEditColorPass_L2_FD_SM2"];
                    }
                    else
                    {
                        effect.CurrentTechnique = effect.Techniques["TerrainEditColorPass_L0_FD_SM2"];
                    }
#else
                    effect.CurrentTechnique = mat.TechniqueEdit(TerrainMaterial.EffectTechs.TerrainEditMode);
#endif
                }
                else
                {
                    if (effects)
                    {
                        effect.CurrentTechnique = mat.TechniqueColor(TerrainMaterial.EffectTechs.TerrainDepthPass);
                    }
                    else
                    {
#if NETFX_CORE
                        int lightNum = Luz.Count;
                        if (lightNum > 6)
                        {
                            effect.CurrentTechnique = effect.Techniques["TerrainColorPass_L10_FD_SM2"];
                        }
                        else if (lightNum > 4)
                        {
                            effect.CurrentTechnique = effect.Techniques["TerrainColorPass_L6_FD_SM2"];
                        }
                        else if (lightNum > 2)
                        {
                            effect.CurrentTechnique = effect.Techniques["TerrainColorPass_L4_FD_SM2"];
                        }
                        else if (lightNum > 0)
                        {
                            effect.CurrentTechnique = effect.Techniques["TerrainColorPass_L2_FD_SM2"];
                        }
                        else
                        {
                            effect.CurrentTechnique = effect.Techniques["TerrainColorPass_L0_FD_SM2"];
                        }
#else
                        effect.CurrentTechnique = mat.TechniqueColor(TerrainMaterial.EffectTechs.TerrainColorPass);
#endif
                    }
                }

                //For FewerDraws render method, we'll set all the parameters that are per-material here.
                if (RenderMethod == RenderMethods.FewerDraws)
                {
                    SetMaterialParams_FD(matIdx, false);
                }

                for (int iPass = 0; iPass < effect.CurrentTechnique.Passes.Count; iPass++)
                {
                    var pass = effect.CurrentTechnique.Passes[iPass];

                    pass.Apply();

                    if (RenderMethod == RenderMethods.FewerDraws)
                    {
                        device.Indices = Tile.IndexBuffer_FD();

                        #region Debug_ToggleTopFaceWithF6: Skip Top for FewerDrawsRM
#if Debug_ToggleTopFaceWithF6
                        if (!KeyboardInputX.IsPressed(Keys.F6))
#endif
                        #endregion
                        {
                            SetTopParams_FD(matIdx, false);
                            pass.Apply();

                            VirtualMap.Render_FD(device, camera, renderIdx, false);
                        }

                        #region Debug_ToggleSideFacesWithF7: Skip sides for FewerDrawsRM
#if Debug_ToggleSideFacesWithF7
                        if (!KeyboardInputX.IsPressed(Keys.F7))
#endif
                        #endregion
                        {
                            SetSideParams_FD(matIdx, false);
                            pass.Apply();

                            VirtualMap.Render_FD(device, camera, renderIdx, true);
                        }
                    }

                }
            }
            #endregion
            #region Fabric rendering

            editMode = originalEditModeFlag;

            SetGlobalParams_FA();

            for (int matIter = TerrainMaterial.MaxMatIdx; matIter >= -1; --matIter)
            {
                ushort matIdx;
                ushort renderIdx;

                if (matIter == -1)//Render the selection material on the last iteration (this is why matIter != matIdx)
                {
                    var selectedIdx = VirtualMap.SelectedMatIdx;
                    renderIdx = TerrainMaterial.SetFlags(selectedIdx, TerrainMaterial.Flags.Selection);
                    if (TerrainMaterial.IsValid(selectedIdx, false, false)
                        && TerrainMaterial.IsFabric(selectedIdx) 
                        && TerrainMaterial.IsUsed(renderIdx))
                    {
                        // This is the selection material. It will always be the last material rendered.
                        matIdx = selectedIdx;
                        ParameterColor(EffectParams.EditBrushScaleOff).SetValue(new Vector2(0.0f, 0.5f));
                        ParameterEdit(EffectParams.EditBrushScaleOff).SetValue(new Vector2(0.0f, 0.5f));
                        editMode = true;
                    }
                    else
                        continue;
                }
                else
                {
                    renderIdx = matIdx = TerrainMaterial.GetFabric((ushort)matIter);
                    if (!TerrainMaterial.IsUsed(matIdx))
                        continue;
                }


                TerrainMaterial mat = TerrainMaterial.Get(matIdx);

                Effect effect = editMode ? effectEdit : effectColor;

                if (editMode)
                {
#if NETFX_CORE
                    int lightNum = Luz.Count;
                    if (lightNum > 6)
                    {
                        effect.CurrentTechnique = effect.Techniques["TerrainEditColorPass_L10_FA_SM2"];
                    }
                    else if (lightNum > 4)
                    {
                        effect.CurrentTechnique = effect.Techniques["TerrainEditColorPass_L6_FA_SM2"];
                    }
                    else if (lightNum > 2)
                    {
                        effect.CurrentTechnique = effect.Techniques["TerrainEditColorPass_L4_FA_SM2"];
                    }
                    else if (lightNum > 0)
                    {
                        effect.CurrentTechnique = effect.Techniques["TerrainEditColorPass_L2_FA_SM2"];
                    }
                    else
                    {
                        effect.CurrentTechnique = effect.Techniques["TerrainEditColorPass_L0_FA_SM2"];
                    }
#else
                    effect.CurrentTechnique = mat.TechniqueEdit(TerrainMaterial.EffectTechs_FA.TerrainEditMode_FA);
#endif
                }
                else
                {
                    if (effects)
                    {
                        effect.CurrentTechnique = mat.TechniqueColor(TerrainMaterial.EffectTechs_FA.TerrainDepthPass_FA);
                    }
                    else
                    {
#if NETFX_CORE
                        int lightNum = Luz.Count;
                        if (lightNum > 6)
                        {
                            effect.CurrentTechnique = effect.Techniques["TerrainColorPass_L10_FA_SM2"];
                        }
                        else if (lightNum > 4)
                        {
                            effect.CurrentTechnique = effect.Techniques["TerrainColorPass_L6_FA_SM2"];
                        }
                        else if (lightNum > 2)
                        {
                            effect.CurrentTechnique = effect.Techniques["TerrainColorPass_L4_FA_SM2"];
                        }
                        else if (lightNum > 0)
                        {
                            effect.CurrentTechnique = effect.Techniques["TerrainColorPass_L2_FA_SM2"];
                        }
                        else
                        {
                            effect.CurrentTechnique = effect.Techniques["TerrainColorPass_L0_FA_SM2"];
                        }
#else
                        effect.CurrentTechnique = mat.TechniqueColor(TerrainMaterial.EffectTechs_FA.TerrainColorPass_FA);
#endif
                    }
                }

                SetMaterialParams_FA(matIdx, false);

                for (int iPass = 0; iPass < effect.CurrentTechnique.Passes.Count; iPass++)
                {
                    var pass = effect.CurrentTechnique.Passes[iPass];

                    pass.Apply();

                    VirtualMap.Render_FA(device, camera, renderIdx);
                }
            }

            #endregion

            if (RenderWire)
            {
                device.RasterizerState = RasterizerState.CullCounterClockwise;
            }

        }   // end of Terrain Render()

        private Matrix AxisAngle(Vector3 axis, float angle)
        {
            angle = (float)Math.IEEERemainder(angle, 2 * Math.PI);
            Matrix reference = Matrix.CreateFromAxisAngle(axis, angle);

            Matrix axang = Matrix.Identity;
            float cosang = (float)Math.Cos(angle);
            float sinang = (float)Math.Sin(angle);
            axang.M11 = (1 - cosang) * axis.X * axis.X + cosang;
            axang.M21 = (1 - cosang) * axis.X * axis.Y - sinang * axis.Z;
            axang.M31 = (1 - cosang) * axis.X * axis.Z + sinang * axis.Y;

            axang.M12 = (1 - cosang) * axis.X * axis.Y + sinang * axis.Z;
            axang.M22 = (1 - cosang) * axis.Y * axis.Y + cosang;
            axang.M32 = (1 - cosang) * axis.Y * axis.Z - sinang * axis.X;

            axang.M13 = (1 - cosang) * axis.X * axis.Z - sinang * axis.Y;
            axang.M23 = (1 - cosang) * axis.Y * axis.Z + sinang * axis.X;
            axang.M33 = (1 - cosang) * axis.Z * axis.Z + cosang;

            return axang;
        }

        /// <summary>
        /// Setup to render a face of a water cube. Assumes technique is single pass.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="baseHeight"></param>
        /// <param name="type"></param>
        /// <param name="face"></param>
        /// <param name="localToWorld"></param>
        /// <param name="camera"></param>
        public void PreRenderWaterCube(
            GraphicsDevice device,
            float baseHeight,
            int type,
            int face,
            Matrix localToWorld,
            Camera camera)
        {
            VirtualMap.PreRenderWaterCube(
                device,
                baseHeight,
                type,
                face,
                localToWorld,
                camera);
        }
        /// <summary>
        /// Finish up render of water cube face.
        /// </summary>
        public void PostRenderWaterCube()
        {
            VirtualMap.PostRenderWaterCube();
        }

        public void RenderWater(Camera camera, bool effects)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            WaterParticleEmitter.Render(camera);

            VirtualMap.RenderWater(device, camera, effects);
        }   // end of Terrain RenderWater()

        private static Vector2 WorldToTile(Vector2 worldPos, Tile tile)
        {
            return worldPos - tile.Min;
        }
        private static Vector2 TileToWorld(Vector2 tilePos, Tile tile)
        {
            return tilePos + tile.Min;
        }
        public void RenderToHeightMap(
                                        Vector2 editBrushStart,
                                        Vector2 editBrushEnd,
                                        float editBrushRadius,
                                        EditMode editMode,
                                        float editSpeed)
        {
            if (editMode == EditMode.Noop)
                return;

            brushCenterHeight = Math.Max(GetTerrainHeight(editBrushStart), MinHeight);

            Vector2 editMin = new Vector2(
                Math.Min(editBrushStart.X, editBrushEnd.X),
                Math.Min(editBrushStart.Y, editBrushEnd.Y));
            Vector2 editMax = new Vector2(
                Math.Max(editBrushStart.X, editBrushEnd.X),
                Math.Max(editBrushStart.Y, editBrushEnd.Y));
            Vector2 worldMin = new Vector2(
                editMin.X - editBrushRadius * 0.5f,
                editMin.Y - editBrushRadius * 0.5f);
            Vector2 worldMax = new Vector2(
                editMax.X + editBrushRadius * 0.5f,
                editMax.Y + editBrushRadius * 0.5f);

            VirtualMap.ExpandMaps(worldMin, worldMax);

            Rectangle tileRect = VirtualMap.MapsTouched(
                worldMin,
                worldMax);

            /// If this is an add mode, make sure there exist tiles in the entire
            /// region that we can add to. Otherwise, read/write operations on non-existing
            /// tiles are no-ops, so it's best to leave the tiles null.
            if (EditModeIsAdd(editMode))
            {
                if (InGame.inGame.OverBudget)
                {
                    return;
                }

                for (int j = 0; j < tileRect.Height; ++j)
                {
                    for (int i = 0; i < tileRect.Width; ++i)
                    {
                        Point tileIdx = new Point(i + tileRect.X, j + tileRect.Y);

                        VirtualMap.EnsureMap(tileIdx);

                    }
                }
            }
            RenderToHeightMap(
                VirtualMap,
                editBrushStart,
                editBrushEnd,
                editBrushRadius,
                editMode,
                editSpeed);

        }

        /// <summary>
        ///  Return whether this edit mode is capable of adding new terrain. Most are not.
        /// </summary>
        /// <param name="editMode"></param>
        /// <returns></returns>
        public static bool EditModeIsAdd(EditMode editMode)
        {
            return (editMode == EditMode.AddAtMax)
                || (editMode == EditMode.AddAtZero)
                || (editMode == EditMode.AddAtCenter)
                || (editMode == EditMode.PaintAndAddMaterial);
        }

        public void RenderToHeightMap(
                                        Vector2 editBrushPosition,
                                        float editBrushRadius,
                                        EditMode editMode,
                                        float editSpeed)
        {
            if (editMode == EditMode.Noop)
                return;

            Vector2 worldMin = new Vector2(
                editBrushPosition.X - editBrushRadius * 0.5f,
                editBrushPosition.Y - editBrushRadius * 0.5f);
            Vector2 worldMax = new Vector2(
                editBrushPosition.X + editBrushRadius * 0.5f,
                editBrushPosition.Y + editBrushRadius * 0.5f);

            VirtualMap.ExpandMaps(worldMin, worldMax);

            Rectangle tileRect = VirtualMap.MapsTouched(
                worldMin,
                worldMax);

            /// If this is an add mode, make sure there exist tiles in the entire
            /// region that we can add to. Otherwise, read/write operations on non-existing
            /// tiles are no-ops, so it's best to leave the tiles null.
            if (EditModeIsAdd(editMode))
            {
                if (InGame.inGame.OverBudget)
                    return;

                for (int j = 0; j < tileRect.Height; ++j)
                {
                    for (int i = 0; i < tileRect.Width; ++i)
                    {
                        Point tileIdx = new Point(i + tileRect.X, j + tileRect.Y);

                        VirtualMap.EnsureMap(tileIdx);

                    }
                }
            }
            RenderToHeightMap(
                VirtualMap,
                editBrushPosition,
                editBrushRadius,
                editMode,
                editSpeed);

        }

        private void CheckMinMax(int i, int j, ref Point minDone, ref Point maxDone)
        {
            minDone.X = i < minDone.X ? i : minDone.X;
            minDone.Y = j < minDone.Y ? j : minDone.Y;

            maxDone.X = i > maxDone.X ? i : maxDone.X;
            maxDone.Y = j > maxDone.Y ? j : maxDone.Y;
        }

        public bool PositionSelected(Vector3 position,
                                    Vector2 editBrushPosition,
                                    float editBrushRadius)
        {
            Vector2 worldPoint = new Vector2(position.X, position.Y);

            /// World space to virtual coords (be careful of rounding/truncating).
            Vector2 w2vScale = new Vector2(1.0f / VirtualMap.CubeSize);
            Vector2 w2vOffset = -VirtualMap.Min * w2vScale - new Vector2(0.5f);

            /// World to normalized brush uv coords.
            /// To get to texel coords multiply by size and round.
            Vector2 w2bScale = new Vector2(1.0f / editBrushRadius);
            Vector2 w2bOffset = new Vector2(
                0.5f - editBrushPosition.X * w2bScale.X,
                0.5f - editBrushPosition.Y * w2bScale.Y);

            Brush2DManager.Brush2D brush = Brush2DManager.GetActiveBrush();

            Vector2 brushPoint = worldPoint * w2bScale + w2bOffset;
            float brushPointSample;
            float brushBilinearSample;
            if (brush.Sample(brushPoint, out brushPointSample, out brushBilinearSample))
            {

                // Only look at the brush if we're in bounds.
                // sample will be -1 if out of bounds.
                // If round shaped brush, corners will be 0
                // so look for > 0.
                if (brushPointSample > 0.5f)
                {
                    return true;
                }
            }
            return false;
        }

        public bool PositionSelected(Vector3 position,
                                    Vector2 editBrushStart,
                                    Vector2 editBrushEnd,
                                    float editBrushRadius)
        {
            // Get brush info.
            Brush2DManager.Brush2D brush = Brush2DManager.GetActiveBrush();
            if (brush == null)
            {
                return false;
            }
            editBrushRadius /= 2.0f;

            Vector2 worldPos = new Vector2(position.X, position.Y);

            Point virtualStart = VirtualMap.WorldToVirtualIndex(editBrushStart);
            Point virtualEnd = VirtualMap.WorldToVirtualIndex(editBrushEnd);

            Vector2 startToEnd = editBrushEnd - editBrushStart;
            Vector2 toParm = startToEnd;
            float invRadius = editBrushRadius > 0.0f ? 1.0f / editBrushRadius : 0.0f;
            if (virtualStart == virtualEnd)
            {
                /// Start and end positions are the same. This degenerate case
                /// is treated as a circle.
                toParm = Vector2.Zero;
            }
            else
            {
                toParm /= startToEnd.LengthSquared();
            }

            Vector2 w2bScale = new Vector2(0.5f / editBrushRadius);

            float t = Vector2.Dot(worldPos - editBrushStart, toParm);
            float tOrig = t;
            t = MathHelper.Clamp(t, 0.0f, 1.0f);
            Vector2 closest = editBrushStart + t * (editBrushEnd - editBrushStart);

            Vector2 w2bOffset = new Vector2(
                0.5f - closest.X * w2bScale.X,
                0.5f - closest.Y * w2bScale.Y);

            Vector2 uv = worldPos * w2bScale + w2bOffset;

            float brushPointSample;
            float brushBilinearSample;
            if (brush.Sample(uv, out brushPointSample, out brushBilinearSample))
            {
                if (brushPointSample > 0.5f)
                {
                    return true;
                }
            }

            return false;
        }

        public bool PositionSelected(Vector3 position)
        {
            Point virtIndex = VirtualMap.WorldToVirtualIndex(new Vector2(position.X, position.Y));
            ushort matIdx = VirtualMap.GetColor(virtIndex.X, virtIndex.Y, TerrainMaterial.EmptyMatIdx);
            return TerrainMaterial.HasFlags(matIdx, TerrainMaterial.Flags.Selection);
        }

        /// virtual coord (i,j) to world space (x,y)
        ///     x = (i + 0.5f) * cubeSize + Min
        ///     or
        ///     x = i * cubeSize + (Min + cubeSize / 2)
        /// world space (x,y) to virtual coord (i,j)
        ///     i = (x - (Min + cubeSize/2)) / cubeSize
        ///     or
        ///     i = x / cubeSize - Min / cubeSize - 0.5
        /// world space (x,y) to brush space (u,v)
        ///     u = x / editBrushRadius + 0.5f - editBrushPosition.X / editBrushRadius
        ///     or
        ///     u = (x - editBrushPosition.X) / editBrushRadius + 0.5f
        /// brush space (u,v) to brush texel (i,j)
        ///     i = Round(u * size.X - 0.5)
        ///     or
        ///     i = (int)(u * size.X - 0.5 + 0.5)
        ///     or
        ///     i = (int)(u * size.X)
        /// <summary>
        /// Renders into the current heightmap
        /// </summary>
        /// <param name="editBrushPosition">Position of the brush.</param>
        /// <param name="editBrushRadius">Radius of the brush.</param>
        /// <param name="editMode">Mode:  up, down, smooth, rough</param>
        private void RenderToHeightMap(
                                        VirtualMap map,
                                        Vector2 editBrushPosition,
                                        float editBrushRadius,
                                        EditMode editMode,
                                        float editSpeed)
        {
            /// Tools that operate on the single brush position go here.
            switch (editMode)
            {
                case EditMode.WaterRaise:
                    ++waterCounter;
                    if (InGame.inGame.UnderBudget)
                    {
                        float waterHeight = GetWaterBase(editBrushPosition);
                        float terrainHeight = GetTerrainHeightFlat(editBrushPosition);
                        waterHeight = Math.Max(waterHeight, terrainHeight);

                        // Hint if there's no terrain.
                        if (terrainHeight == 0)
                        {
                            Common.HintSystem.WaterNoTerrainHint.Activate();
                        }

                        /// adjustTo20Hz = actual frame time / frame time @ 20Hz
                        ///              = actual frame time * 20 frames / sec
                        float adjustTo20Hz = Time.WallClockFrameSeconds * 20.0f;
                        float raiseRate = CubeSize * 0.5f * adjustTo20Hz * editSpeed;
                        waterHeight += raiseRate;

                        Water water = VirtualMap.GetWater(editBrushPosition);
                        if (water != null)
                        {
                            water.BaseHeight = waterHeight;

                            //move the seed position to the cursor
                            water.SeedPosition2D = editBrushPosition; 
                            VirtualMap.AddForRefresh(water, editBrushPosition, terrainHeight);
                        }
                        else
                        {
                            //only allow new water points when in edit mode
                            if (!WaterBusy && InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.RunSim)
                            {
                                VirtualMap.CreateWater(editBrushPosition, waterHeight);
                                VirtualMap.FlushWaterUpdate();
                            }
                        }

                    }
                    return;
                case EditMode.WaterLower:
                    {
                        ++waterCounter;
                        Water water = VirtualMap.GetWater(editBrushPosition);
                        if (water != null)
                        {
                            float waterHeight = water.BaseHeight;
                            float terrainHeight = GetTerrainHeightFlat(editBrushPosition);

                            float adjustTo20Hz = Time.WallClockFrameSeconds * 20.0f;
                            float raiseRate = CubeSize * 0.5f * adjustTo20Hz * editSpeed;
                            waterHeight -= raiseRate;
                            if (waterHeight > terrainHeight)
                            {
                                water.BaseHeight = waterHeight;
                                
                                //move the seed position to the cursor
                                water.SeedPosition2D = editBrushPosition; 
                                VirtualMap.AddForRefresh(water, editBrushPosition, terrainHeight);
                            }
                            else
                            {
                                //only delete water when in edit mode
                                if (InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.RunSim)
                                {
                                    water.BaseHeight = 0.0f;

                                    // The water is at min level but the user is still pressing the key/button/trigger.
                                    // Tell the system to allow an update so that the water on top of the terrain cubes
                                    // goes away.
                                    VirtualMap.IgnoreSuppressWaterUpdateThisFrame = true;
                                    VirtualMap.EraseWater(editBrushPosition);
                                }
                            }
                        }
                    }
                    return;
                case EditMode.WaterChange:
                    {
                        ++waterCounter;
                        Water water = VirtualMap.GetWater(editBrushPosition);
                        if (water != null)
                        {
                            water.SetType(Water.CurrentType);
                        }
                    }
                    return;
                case EditMode.MaterialReplace:
                    {
                        VirtualMap.ChangeMaterial(editBrushPosition);
                    }
                    return;

                case EditMode.Noop:
                    /// Nothing to do.
                    return;

                // Fall through to the brush type tools.
                default:
                    break;
            }



            // Get brush info.
            Brush2DManager.Brush2D brush = Brush2DManager.GetActiveBrush();

            // Transform brush position to lower left of brush from center.
            Vector2 editBrushSWCorner = editBrushPosition - 0.5f * new Vector2(editBrushRadius);

            /// Virtual coords to world space.
            Vector2 v2wScale = new Vector2(VirtualMap.CubeSize);
            Vector2 v2wOffset = VirtualMap.Min + new Vector2(VirtualMap.CubeSize * 0.5f);

            /// World space to virtual coords (be careful of rounding/truncating).
            Vector2 w2vScale = new Vector2(1.0f / VirtualMap.CubeSize);
            Vector2 w2vOffset = -VirtualMap.Min * w2vScale - new Vector2(0.5f);

            /// World to normalized brush uv coords.
            /// To get to texel coords multiply by size and round.
            Vector2 w2bScale = new Vector2(1.0f / editBrushRadius);
            Vector2 w2bOffset = new Vector2(
                0.5f - editBrushPosition.X * w2bScale.X,
                0.5f - editBrushPosition.Y * w2bScale.Y);

            // Calc min and max values for where on the grid the brush is.
            // Normalize brush position.
            Vector2 posMin = editBrushSWCorner * w2vScale + w2vOffset;
            Vector2 posMax = (editBrushPosition + new Vector2(editBrushRadius)) * w2vScale + w2vOffset;

            // Convert these to height map numbers.  Add 1 to the max values since
            // these will be used as loop limits.
            Point min = new Point(
                (int)Math.Floor(posMin.X),
                (int)Math.Floor(posMin.Y));
            Point max = new Point(
                1 + (int)Math.Ceiling(posMax.X),
                1 + (int)Math.Ceiling(posMax.Y));
            // Clamp to valid limits.
            min.X = Math.Max(min.X, 0);
            min.Y = Math.Max(min.Y, 0);
            max.X = Math.Min(max.X, VirtualMap.VirtualSize.X);
            max.Y = Math.Min(max.Y, VirtualMap.VirtualSize.Y);

            Point center = new Point((min.X + max.X) / 2, (min.Y + max.Y) / 2);
            int centerMatIdx = map.GetColor(center.X, center.Y, CurrentMaterialIndex);

            Point minDone = max;
            Point maxDone = min;

            /// Tools that need a pre-pass over the terrain to compute local surface
            /// info can do so here.
            Plane editPlane = new Plane(Vector3.UnitZ, 0.0f);
            switch (editMode)
            {
                case EditMode.RaiseVol:
                case EditMode.LowerVol:
                case EditMode.LevelVol:
                    editPlane = FindPlane3(
                        min, max,
                        v2wScale, v2wOffset,
                        w2bScale, w2bOffset,
                        brush);
                    break;

                /// Fall through to the brush type tools.
                default:
                    break;
            }

            brushCenterHeight = Math.Max(GetTerrainHeight(editBrushPosition), MinHeight);
            LevelHeight = brushCenterHeight;

            bool snap = InGame.inGame.SnapToGrid;
            bool click = LowLevelMouseInput.Left.WasPressed
                || LowLevelMouseInput.Right.WasPressed
                || GamePadInput.GetGamePad0().LeftTriggerButton.WasPressed
                || GamePadInput.GetGamePad0().RightTriggerButton.WasPressed;

            // For each vertex in the height map, map it back to brush coordinates.
            for (int i = min.X; i < max.X; i++)
            {
                for (int j = min.Y; j < max.Y; j++)
                {
                    // Get point in world coords.
                    Vector2 worldPoint = new Vector2(
                        i * v2wScale.X + v2wOffset.X,
                        j * v2wScale.Y + v2wOffset.Y);

                    // And then convert to brush coordinates.
                    Vector2 brushPoint = worldPoint * w2bScale + w2bOffset;

                    float brushPointSample;
                    float brushBilinearSample;

                    // Only look at the brush if we're in bounds of the brush.  We still need 
                    // to check that the result samples are non-zero before use.
                    if (brush.Sample(brushPoint, out brushPointSample, out brushBilinearSample))
                    {
                        float height = map.GetHeight(i, j);
                        float newHeight = height;

                        switch (editMode)
                        {
                            case EditMode.RaiseVol:
                                newHeight = RaiseVolume(brushBilinearSample, editBrushRadius * volumeScale, worldPoint, height, editPlane);
                                break;
                            case EditMode.LowerVol:
                                newHeight = LowerVolume(brushBilinearSample, editBrushRadius * volumeScale, worldPoint, height, editPlane);
                                break;
                            case EditMode.LevelVol:
                                newHeight = LevelVolume(worldPoint, height, editPlane);
                                break;
                            case EditMode.Raise:
                                if (snap)
                                {
                                    if (click)
                                    {
                                        if (height > 0.0f)
                                        {
                                            ++raiseCounter;
                                            newHeight = height + VirtualMap.CubeSize;
                                        }
                                    }
                                }
                                else
                                {
                                    ++raiseCounter;
                                    newHeight = Raise(i, j, height, editSpeed * brushBilinearSample);
                                }
                                break;
                            case EditMode.Lower:
                                if (snap)
                                {
                                    if (click)
                                    {
                                        if (height > 0.0f)
                                        {
                                            ++lowerCounter;
                                            newHeight = MathHelper.Max(height - VirtualMap.CubeSize, VirtualMap.CubeSize);
                                        }
                                    }
                                }
                                else
                                {
                                    ++lowerCounter;
                                    newHeight = Lower(i, j, height, editSpeed * brushBilinearSample);
                                }
                                break;
                            case EditMode.Smooth:
                                ++smoothCounter;
                                newHeight = Smooth(i, j, height, editSpeed * brushBilinearSample);
                                break;
                            case EditMode.Roughen:
                                newHeight = Roughen(i, j, height, editSpeed * brushBilinearSample);
                                break;
                            case EditMode.Hill:
                                newHeight = Hill(i, j, height, editSpeed * brushBilinearSample);
                                break;
                            case EditMode.Level:
                                newHeight = Level(i, j, height, editSpeed * brushBilinearSample);
                                break;
                            case EditMode.LevelSnap:
                                newHeight = LevelSnap(i, j, height * brushBilinearSample);
                                break;
                            case EditMode.Max:
                                newHeight = MaxCardinal(i, j, height, editSpeed * brushBilinearSample);
                                break;
                            case EditMode.Min:
                                newHeight = MinCardinal(i, j, height, editSpeed * brushBilinearSample);
                                break;
                            case EditMode.PaintMaterial:
                                if (brushPointSample > 0)
                                {
                                    ++paintCounter;
                                    newHeight = Paint(i, j, height);
                                }
                                break;
                            case EditMode.PaintAndAddMaterial:
                                if (InGame.inGame.OverBudget)
                                {
                                    /// Over budget, just bail immediately
                                    return;
                                }
                                if (brushPointSample > 0)
                                {
                                    ++paintCounter;
                                    newHeight = PaintAndAddAtCenter(i, j, height);
                                }
                                break;
                            case EditMode.Delete:
                                if (brushPointSample > 0)
                                {
                                    ++deleteCounter;
                                    newHeight = Delete(i, j, height);
                                }
                                break;
                            case EditMode.AddAtZero:
                                newHeight = AddAtZero(i, j, height);
                                if (InGame.inGame.OverBudget)
                                {
                                    /// Over budget, just bail immediately
                                    return;
                                }
                                break;
                            case EditMode.AddAtMax:
                                newHeight = AddAtMax(i, j, height);
                                if (InGame.inGame.OverBudget)
                                {
                                    /// Over budget, just bail immediately
                                    return;
                                }
                                break;
                            case EditMode.AddAtCenter:
                                // Adds new terrain.
                                if (InGame.inGame.OverBudget)
                                {
                                    // Over budget, just bail immediately
                                    return;
                                }
                                if (brushPointSample > 0)
                                {
                                    newHeight = AddAtCenter(i, j, height);
                                }
                                break;
                            default:
                                break;

                        }   // end of switch on editMode

                        // Apply to heightmap.
                        if (height != newHeight)
                        {
                            map.SetHeight(i, j, newHeight);

                            // If the new height is 0 also set the material value to empty.
                            if (newHeight == 0)
                            {
                                map.SetColor(i, j, TerrainMaterial.EmptyMatIdx);
                            }
                        }
                    }
                }
            }
            switch (editMode)
            {
                /// Paint material doesn't need to cause a recheck
                /// of heights and rebuild of roads.
                case EditMode.PaintMaterial:
                    break;

                /// But most edits do.
                default:
                    Editing = true;
                    break;
            }

        }   // end of Terrain RenderToHeightMap()

        private Plane FindPlane3(
            Point min,
            Point max,
            Vector2 v2wScale,
            Vector2 v2wOffset,
            Vector2 w2bScale,
            Vector2 w2bOffset,
            Brush2DManager.Brush2D brush)
        {
            VirtualMap map = VirtualMap;

            Vector2 b2wScale = new Vector2(1.0f / w2bScale.X, 1.0f / w2bScale.Y);
            Vector2 b2wOffset = new Vector2(-w2bOffset.X / w2bScale.X, -w2bOffset.Y / w2bScale.Y);

            Vector2 northWorld = new Vector2(
                0.0f * b2wScale.X + b2wOffset.X - CubeSize, 1.0f * b2wScale.Y + b2wOffset.Y + CubeSize);

            Vector2 southWorld = new Vector2(
                1.0f * b2wScale.X + b2wOffset.X + CubeSize, 0.0f * b2wScale.Y + b2wOffset.Y - CubeSize);

            Vector2 eastWorld = new Vector2(
                1.0f * b2wScale.X + b2wOffset.X + CubeSize, 1.0f * b2wScale.Y + b2wOffset.Y + CubeSize);

            Vector2 westWorld = new Vector2(
                0.0f * b2wScale.X + b2wOffset.X - CubeSize, 0.0f * b2wScale.Y + b2wOffset.Y - CubeSize);

            Vector3 north = new Vector3(northWorld, map.GetHeightFlat(northWorld));
            Vector3 south = new Vector3(southWorld, map.GetHeightFlat(southWorld));
            Vector3 east = new Vector3(eastWorld, map.GetHeightFlat(eastWorld));
            Vector3 west = new Vector3(westWorld, map.GetHeightFlat(westWorld));

            Vector3 center = (north + south + east + west) * 0.25f;

            Vector3 norm = Vector3.Cross(north - center, west - center)
                            + Vector3.Cross(west - center, south - center)
                            + Vector3.Cross(south - center, east - center)
                            + Vector3.Cross(east - center, north - center);
            norm.Normalize();


            float dist = Vector3.Dot(center, norm);

            return new Plane(norm, dist);
        }
        private Plane FindPlane2(
            Point min,
            Point max,
            Vector2 v2wScale,
            Vector2 v2wOffset,
            Vector2 w2bScale,
            Vector2 w2bOffset,
            Brush2DManager.Brush2D brush)
        {
            VirtualMap map = VirtualMap;

            Vector2 b2wScale = new Vector2(1.0f / w2bScale.X, 1.0f / w2bScale.Y);
            Vector2 b2wOffset = new Vector2(-w2bOffset.X / w2bScale.X, -w2bOffset.Y / w2bScale.Y);

            Vector2 northWorld = new Vector2(
                0.5f * b2wScale.X + b2wOffset.X, 1.0f * b2wScale.Y + b2wOffset.Y);

            Vector2 southWorld = new Vector2(
                0.5f * b2wScale.X + b2wOffset.X, 0.0f * b2wScale.Y + b2wOffset.Y);

            Vector2 eastWorld = new Vector2(
                1.0f * b2wScale.X + b2wOffset.X, 0.5f * b2wScale.Y + b2wOffset.Y);

            Vector2 westWorld = new Vector2(
                0.0f * b2wScale.X + b2wOffset.X, 0.5f * b2wScale.Y + b2wOffset.Y);

            Vector3 north = Vector3.Zero;
            Vector3 northNorm = Vector3.Zero;
            map.GetHeightAndNormal(northWorld, out north, out northNorm);

            Vector3 south = Vector3.Zero;
            Vector3 southNorm = Vector3.Zero;
            map.GetHeightAndNormal(southWorld, out south, out southNorm);

            Vector3 east = Vector3.Zero;
            Vector3 eastNorm = Vector3.Zero;
            map.GetHeightAndNormal(eastWorld, out east, out eastNorm);

            Vector3 west = Vector3.Zero;
            Vector3 westNorm = Vector3.Zero;
            map.GetHeightAndNormal(westWorld, out west, out westNorm);

            Vector3 norm = northNorm + southNorm + eastNorm + westNorm;
            norm.Normalize();

            float dist = Vector3.Dot(north, norm)
                        + Vector3.Dot(south, norm)
                        + Vector3.Dot(east, norm)
                        + Vector3.Dot(west, norm);
            dist *= 0.25f;

            return new Plane(norm, dist);
        }

        static UInt32 noiseSeed = 0;
        public static void Reseed()
        {
            noiseSeed = (UInt32)BokuGame.bokuGame.rnd.Next();
        }
        private float Noise(int iv, int jv)
        {
            UInt32 seed = 0;

            for (int k = 0; k < 16; ++k)
            {
                seed |= (UInt32)(((iv) & (1 << k)) << (31 - 3 * k));
                seed |= (UInt32)(((jv) & (1 << k)) << (31 - 3 * k + 1));
            }
            seed ^= noiseSeed;

            const UInt32 scale = 9301;
            const UInt32 offset = 49297;
            const UInt32 modulo = 233280;
            UInt32 ranInt = (scale * seed + offset) % modulo;
            const double divisor = 1.0 / modulo;
            return (float)(ranInt * divisor);
        }
        private float Paint(int i, int j, float h)
        {
            if (h > 0.0f)
            {
                VirtualMap.SetColor(i, j, CurrentMaterialIndex);
            }
            return h;
        }
        private float Delete(int i, int j, float h)
        {
            return 0.0f;
        }
        private float RaiseVolume(float sample, float volumeScale, Vector2 worldPoint, float height, Plane editPlane)
        {
            if (height > 0)
            {
                float newHeight = LevelVolume(worldPoint, height, editPlane);
                newHeight += volumeScale * sample;
                height = Math.Max(height, newHeight);
            }
            return height;
        }
        private float LowerVolume(float sample, float volumeScale, Vector2 worldPoint, float height, Plane editPlane)
        {
            if (height > 0)
            {
                float newHeight = LevelVolume(worldPoint, height, editPlane);
                newHeight -= volumeScale * sample;
                height = Math.Min(height, newHeight);
                height = Math.Max(height, MinHeight);
            }
            return height;
        }
        private float LevelVolume(Vector2 worldPoint, float height, Plane editPlane)
        {
            if (height > 0)
            {
                Vector3 worldPos = new Vector3(worldPoint, height);
                height += editPlane.D - Vector3.Dot(editPlane.Normal, worldPos);
                if (height < MinHeight)
                    height = MinHeight;
            }
            return height;
        }
        private float Raise(int i, int j, float h, float speed)
        {
            // Raise.
            if (h > 0.0f)
            {
                float rate = 10.0f * Time.WallClockFrameSeconds;
                h += rate * speed;
            }
            return h;
        }
        private float Lower(int i, int j, float h, float speed)
        {
            // Lower.
            if (h > 0.0f)
            {
                float rate = 10.0f * Time.WallClockFrameSeconds;
                h -= rate * speed;

                // Prevent from deleting.
                h = Math.Max(h, MinHeight);
            }
            return h;
        }
        private float AdvanceTo(float from, float to, float rate)
        {
            float del = to - from;
            if (del > 0.0f)
            {
                return from + Math.Min(del, rate);
            }
            return from + Math.Max(del, -rate);
        }
        private float ExponentTo(float from, float to, float halfLife, float speed, float min)
        {
            if (speed <= 0.0f)
            {
                return from;
            }
            float k = (float)Math.Pow(0.5f, 1.0f / halfLife);
            float t = (float)Math.Pow(k, Time.WallClockFrameSeconds * speed);
            float del = to - from;
            float step = (1.0f - t) * del;
            min *= speed * Time.WallClockFrameSeconds;
            step = del > 0.0f ? Math.Min(del, Math.Max(step, min)) : Math.Max(del, Math.Min(step, min));
            return from + step;
        }
        private float MaxCardinal(int i, int j, float height, float speed)
        {
            // Max.
            if (height > 0.0f)
            {
                float maxHeight = height;
                // Calc maximum height of surrounding samples.
                float h = VirtualMap.GetHeight(i + 1, j);
                if (h > 0.0f)
                    maxHeight = Math.Max(maxHeight, h);
                h = VirtualMap.GetHeight(i - 1, j);
                if (h > 0.0f)
                    maxHeight = Math.Max(maxHeight, h);
                h = VirtualMap.GetHeight(i, j + 1);
                if (h > 0.0f)
                    maxHeight = Math.Max(maxHeight, h);
                h = VirtualMap.GetHeight(i, j - 1);
                if (h > 0.0f)
                    maxHeight = Math.Max(maxHeight, h);

                float rate = 10.0f * Time.WallClockFrameSeconds;
                height = AdvanceTo(height, maxHeight, rate * speed);
            }
            return height;
        }
        private float MinCardinal(int i, int j, float height, float speed)
        {
            // Min.
            if (height > 0.0f)
            {
                float minHeight = height;
                // Calc minimum height of surrounding samples.
                float h = VirtualMap.GetHeight(i + 1, j);
                if (h > 0.0f)
                    minHeight = Math.Min(minHeight, h);
                h = VirtualMap.GetHeight(i - 1, j);
                if (h > 0.0f)
                    minHeight = Math.Min(minHeight, h);
                h = VirtualMap.GetHeight(i, j + 1);
                if (h > 0.0f)
                    minHeight = Math.Min(minHeight, h);
                h = VirtualMap.GetHeight(i, j - 1);
                if (h > 0.0f)
                    minHeight = Math.Min(minHeight, h);

                float rate = 10.0f * Time.WallClockFrameSeconds;
                height = AdvanceTo(height, minHeight, rate * speed);

                height = Math.Max(height, MinHeight);
            }
            return height;
        }
        private float levelHeight = 0.0f;
        private float levelStart = 0.0f;
        public float LevelHeight
        {
            get { return levelHeight; }
            set { levelHeight = value; }
        }
        public float LevelStart
        {
            get { return levelStart; }
            set { levelStart = value; }
        }
        private float Level(int i, int j, float h, float speed)
        {
            if (h > 0.0f)
            {
                float rate = 1.0f * Time.WallClockFrameSeconds;
                h = ExponentTo(h, LevelHeight, 0.25f, speed, 3.0f);

                h = Math.Max(h, MinHeight);
            }
            return h;
        }
        private float LevelSnap(int i, int j, float h)
        {
            if (h > 0.0f)
            {
                h = LevelHeight;

                h = Math.Max(h, MinHeight);
            }
            return h;
        }
        private float Road(int i, int j, float h, float t, float speed)
        {
            if (h > 0.0f)
            {
                float hLevel = LevelStart + t * (LevelHeight - LevelStart);
                h = ExponentTo(h, hLevel, 0.25f, speed, 3.0f);

                h = Math.Max(h, MinHeight);
            }
            return h;
        }
        private float RoadSnap(int i, int j, float h, float t)
        {
            if (h > 0.0f)
            {
                h = LevelStart + t * (LevelHeight - LevelStart);

                h = Math.Max(h, MinHeight);
            }
            return h;
        }
        private float Hill(int i, int j, float h, float speed)
        {
            if (h > 0.0f)
            {
                int numPass = 5;
                for (int pass = 0; pass < numPass; ++pass)
                {
                    if ((pass & 1) != 0)
                    {
                        h += Noise(i >> pass, j >> pass)
                            * Time.WallClockFrameSeconds
                            * 4.0f
                            * (float)Math.Log(pass + 1)
                            * speed;
                    }
                    else
                    {
                        h += Noise(j >> pass, i >> pass)
                            * Time.WallClockFrameSeconds
                            * 4.0f
                            * (float)Math.Log(pass + 1)
                            * speed;
                    }
                }

                h = Math.Max(h, MinHeight);
            }
            return h;
        }
        private float Roughen(int i, int j, float h, float speed)
        {
            if (h > 0.0f)
            {
                h = h
                    + (Noise(i, j) - 0.5f)
                        * Time.WallClockFrameSeconds
                        * 50.0f
                        * speed;

                // Prevent from deleting.
                h = Math.Max(h, MinHeight);
            }
            return h;
        }
        private float Smooth(int i, int j, float h, float speed)
        {
            // Smooth.
            if (h > 0.0f)
            {
                // Calc average height of surrounding samples.
                float aveHeight = 0;
                float wgt = 0.0f;
                float nei = VirtualMap.GetHeight(i + 1, j);
                if (nei > 0)
                {
                    aveHeight += nei;
                    wgt += 1.0f;
                }
                nei = VirtualMap.GetHeight(i - 1, j);
                if (nei > 0)
                {
                    aveHeight += nei;
                    wgt += 1.0f;
                }
                nei = VirtualMap.GetHeight(i, j + 1);
                if (nei > 0)
                {
                    aveHeight += nei;
                    wgt += 1.0f;
                }
                nei = VirtualMap.GetHeight(i, j - 1);
                if (nei > 0)
                {
                    aveHeight += nei;
                    wgt += 1.0f;
                }
                if (wgt > 0)
                {
                    aveHeight /= wgt;
                    // Move toward average.
                    h = ExponentTo(h, aveHeight, 0.25f, speed, 2.0f);

                    h = Math.Max(h, MinHeight);
                }
            }
            return h;
        }
        private float AddAtZero(int i, int j, float h)
        {
            if (h == 0.0f)
            {
                h = MinHeight;
                VirtualMap.SetColor(i, j, CurrentMaterialIndex);
            }
            return h;
        }
        private float AddAtMax(int i, int j, float h)
        {
            // Don't change unless we're 0.
            if (h == 0.0f)
            {
                h = Math.Max(MinHeight, LevelHeight);

                VirtualMap.SetColor(i, j, CurrentMaterialIndex);
            }
            else
            {
                if (VirtualMap.GetColor(i, j, CurrentMaterialIndex) == CurrentMaterialIndex)
                {
                    h = Math.Max(h, Math.Max(MinHeight, LevelHeight));
                }
            }
            return h;
        }
        private float AddAtCenter(int i, int j, float h)
        {
            if (h == 0.0f)
            {
                ++addCounter;
                h = brushCenterHeight;
                VirtualMap.SetColor(i, j, CurrentMaterialIndex);
            }
            return h;
        }
        private float PaintAndAddAtCenter(int i, int j, float h)
        {
            // If adding, use height at center of brush, else don't change existing height.
            h = VirtualMap.GetHeight(i, j);
            if (h == 0)
            {
                ++addCounter;
                h = brushCenterHeight;
            }
            VirtualMap.SetColor(i, j, CurrentMaterialIndex);
            return h;
        }

        public void SetSelectionToFabric()
        {
            VirtualMap.ChangeSelectedMaterial(TerrainMaterial.GetFabric(VirtualMap.SelectedMatIdx));
        }
        public void SetSelectionToCubic()
        {
            VirtualMap.ChangeSelectedMaterial(TerrainMaterial.GetNonFabric(VirtualMap.SelectedMatIdx));
        }

        /// <summary>
        /// Generate and cache a flood fill from pos.
        /// </summary>
        public void MakeSelection(Vector2 position)
        {
            VirtualMap.MakeMaterialSelection(position);
        }
        /// <summary>
        /// Shrink the current material selection region by one sample border.
        /// Don't allow it to shrink to nothing.
        /// </summary>
        public void ShrinkSelection()
        {
            VirtualMap.ShrinkSelection();
        }
        /// <summary>
        /// Expand material selection by one sample. If ContainSelection is true, this is
        /// clamped at the current material border.
        /// </summary>
        public void ExpandSelection()
        {
            VirtualMap.ExpandSelection();
        }
        /// <summary>
        /// Determine whether expansion of the selected material region beyond original border is allowed.
        /// </summary>
        public bool ContainSelection
        {
            get { return VirtualMap.ContainSelection; }
            set { VirtualMap.ContainSelection = value; }
        }
        /// <summary>
        /// Clear any current material selection.
        /// </summary>
        public void EndSelection()
        {
            VirtualMap.ClearMaterialSelection();
        }
        /// <summary>
        /// Apply the edit effect to the selected region.
        /// </summary>
        /// <param name="editMode"></param>
        /// <param name="editSpeed"></param>
        public void RenderToSelection(
            EditMode editMode,
            float editSpeed)
        {
            if (editMode == EditMode.Noop)
                return;

            if (editMode == EditMode.PaintMaterial || editMode == EditMode.PaintAndAddMaterial)
            {
                VirtualMap.ChangeSelectedMaterial(CurrentMaterialIndex);
                return;
            }

            bool snap = InGame.inGame.SnapToGrid;
            bool click = LowLevelMouseInput.Left.WasPressed 
                || LowLevelMouseInput.Right.WasPressed 
                || GamePadInput.GetGamePad0().LeftTriggerButton.WasPressed
                || GamePadInput.GetGamePad0().RightTriggerButton.WasPressed;

            List<Point> coords = VirtualMap.SelectList;
            if (coords != null)
            {
                float rate = 1.0f;
                VirtualMap map = VirtualMap;
                foreach (Point coord in coords)
                {
                    int i = coord.X;
                    int j = coord.Y;
                    float hOrig = map.GetHeight(i, j);
                    float h = hOrig;
                    switch (editMode)
                    {
                        case EditMode.Raise:
                            if (snap)
                            {
                                if (click)
                                {
                                    if (h > 0.0f)
                                    {
                                        h = h + VirtualMap.CubeSize;
                                    }
                                }
                            }
                            else
                            {
                                h = Raise(i, j, h, editSpeed);
                            }
                            break;
                        case EditMode.Lower:
                            if (snap)
                            {
                                if (click)
                                {
                                    if (h > 0.0f)
                                    {
                                        h = MathHelper.Max(h - VirtualMap.CubeSize, VirtualMap.CubeSize);
                                    }
                                }
                            }
                            else
                            {
                                h = Lower(i, j, h, editSpeed);
                            }
                            break;
                        case EditMode.Smooth:
                            h = Smooth(i, j, h, editSpeed);
                            break;
                        case EditMode.Delete:
                            h = Delete(i, j, h);
                            break;
                        case EditMode.Roughen:
                            h = Roughen(i, j, h, editSpeed);
                            break;
                        case EditMode.Hill:
                            h = Hill(i, j, h, editSpeed);
                            break;
                        case EditMode.Level:
                            h = Level(i, j, h, editSpeed);
                            break;
                        case EditMode.LevelSnap:
                            h = LevelSnap(i, j, h);
                            break;
                        case EditMode.Max:
                            h = MaxCardinal(i, j, h, editSpeed);
                            break;
                        case EditMode.Min:
                            h = MinCardinal(i, j, h, editSpeed);
                            break;

                    }
                    h = hOrig + rate * (h - hOrig);

                    if (h != hOrig)
                    {
                        map.SetHeight(i, j, h);
                    }
                }
                switch (editMode)
                {
                    /// Paint material doesn't need to cause a recheck
                    /// of heights and rebuild of roads.
                    case EditMode.PaintMaterial:
                        break;

                    /// If we've just nuked what we have selected, 
                    /// we better go ahead and clear it.
                    case EditMode.Delete:
                        EndSelection();
                        Editing = true;
                        break;

                    /// But most edits do.
                    default:
                        Editing = true;
                        break;
                }
            }
        }
        private void RenderToHeightMap(
                                        VirtualMap map,
                                        Vector2 editBrushStart,
                                        Vector2 editBrushEnd,
                                        float editBrushRadius,
                                        EditMode editMode,
                                        float editSpeed)
        {
            if (editMode == EditMode.Noop)
                return;

            // Get brush info.
            Brush2DManager.Brush2D brush = Brush2DManager.GetActiveBrush();
            if (brush == null)
            {
                return;
            }
            editBrushRadius /= 2.0f;

            Vector2 editMin = new Vector2(
                Math.Min(editBrushStart.X, editBrushEnd.X),
                Math.Min(editBrushStart.Y, editBrushEnd.Y));
            Vector2 editMax = new Vector2(
                Math.Max(editBrushStart.X, editBrushEnd.X),
                Math.Max(editBrushStart.Y, editBrushEnd.Y));

            Point virtualStart = map.WorldToVirtualIndex(editBrushStart);
            Point virtualEnd = map.WorldToVirtualIndex(editBrushEnd);

            float hStart = map.GetHeight(virtualStart.X, virtualStart.Y);
            float hEnd = map.GetHeight(virtualEnd.X, virtualEnd.Y);

            Vector2 startToEnd = editBrushEnd - editBrushStart;
            Vector2 toParm = startToEnd;
            float invRadius = editBrushRadius > 0.0f ? 1.0f / editBrushRadius : 0.0f;
            if (virtualStart == virtualEnd)
            {
                /// Start and end positions are the same. This degenerate case
                /// is treated as a circle.
                toParm = Vector2.Zero;
            }
            else
            {
                toParm /= startToEnd.LengthSquared();
            }

            Vector2 worldMin = new Vector2(
                editMin.X - editBrushRadius,
                editMin.Y - editBrushRadius);
            Vector2 worldMax = new Vector2(
                editMax.X + editBrushRadius,
                editMax.Y + editBrushRadius);

            Point virtualMin = map.WorldToVirtualIndex(worldMin);
            Point virtualMax = map.WorldToVirtualIndex(worldMax);

            bool editAdd = EditModeIsAdd(editMode);

            Vector2 w2bScale = new Vector2(0.5f / editBrushRadius);

            for (int i = virtualMin.X; i <= virtualMax.X; ++i)
            {
                for (int j = virtualMin.Y; j <= virtualMax.Y; ++j)
                {
                    float hOrig = map.GetHeight(i, j);
                    if (editAdd || (hOrig > 0.0f))
                    {

                        Vector2 pos = map.VirtualIndexToWorld(i, j);

                        float t = Vector2.Dot(pos - editBrushStart, toParm);
                        float tOrig = t;
                        t = MathHelper.Clamp(t, 0.0f, 1.0f);
                        Vector2 closest = editBrushStart + t * (editBrushEnd - editBrushStart);

                        Vector2 w2bOffset = new Vector2(
                            0.5f - closest.X * w2bScale.X,
                            0.5f - closest.Y * w2bScale.Y);

                        //Vector2 uv = (pos - closest) * (invRadius * 0.5f);
                        //uv += new Vector2(0.5f, 0.5f);
                        Vector2 uv = pos * w2bScale + w2bOffset;

                        float brushPointSample;
                        float brushBilinearSample;

                        if (brush.Sample(uv, out brushPointSample, out brushBilinearSample))
                        {
                            float h = hOrig;

                            switch (editMode)
                            {
                                case EditMode.PaintMaterial:
                                    if (brushPointSample > 0)
                                    {
                                        h = Paint(i, j, h);
                                    }
                                    break;
                                case EditMode.PaintAndAddMaterial:
                                    if (InGame.inGame.OverBudget)
                                    {
                                        /// Over budget, just bail immediately
                                        return;
                                    }
                                    if (brushPointSample > 0)
                                    {
                                        h = PaintAndAddAtCenter(i, j, h);
                                        h = Paint(i, j, h);
                                    }
                                    break;
                                case EditMode.Delete:
                                    if (brushPointSample > 0)
                                    {
                                        h = Delete(i, j, h);
                                    }
                                    break;
                                case EditMode.Road:
                                    h = Road(i, j, h, t, editSpeed * brushBilinearSample);
                                    break;
                                case EditMode.RoadSnap:
                                    h = RoadSnap(i, j, h, t);
                                    break;
                                case EditMode.Level:
                                    h = Level(i, j, h, editSpeed * brushBilinearSample);
                                    break;
                                case EditMode.LevelSnap:
                                    h = LevelSnap(i, j, h);
                                    break;
                                case EditMode.Hill:
                                    h = Hill(i, j, h, editSpeed * brushBilinearSample);
                                    break;
                                case EditMode.Roughen:
                                    h = Roughen(i, j, h, editSpeed * brushBilinearSample);
                                    break;
                                case EditMode.Smooth:
                                    h = Smooth(i, j, h, editSpeed * brushBilinearSample);
                                    break;
                                case EditMode.AddAtZero:
                                    if (InGame.inGame.OverBudget)
                                    {
                                        /// Over budget, just bail immediately
                                        return;
                                    }
                                    if (brushPointSample > 0)
                                    {
                                        h = AddAtZero(i, j, h);
                                    }
                                    break;
                                case EditMode.AddAtMax:
                                    if (InGame.inGame.OverBudget)
                                    {
                                        /// Over budget, just bail immediately
                                        return;
                                    }
                                    if (brushPointSample > 0)
                                    {
                                        h = AddAtMax(i, j, h);
                                    }
                                    break;
                                case EditMode.AddAtCenter:
                                    if (InGame.inGame.OverBudget)
                                    {
                                        /// Over budget, just bail immediately
                                        return;
                                    }
                                    if (brushPointSample > 0)
                                    {
                                        h = AddAtCenter(i, j, h);
                                    }
                                    break;
                                case EditMode.Raise:
                                    h = Raise(i, j, h, editSpeed * brushBilinearSample);
                                    break;
                                case EditMode.Lower:
                                    h = Lower(i, j, h, editSpeed * brushBilinearSample);
                                    break;
                                case EditMode.Max:
                                    h = MaxCardinal(i, j, h, editSpeed * brushBilinearSample);
                                    break;
                                case EditMode.Min:
                                    h = MinCardinal(i, j, h, editSpeed * brushBilinearSample);
                                    break;
                                default:
                                    break;
                            }

                            //h = hOrig + brushSample * (h - hOrig);

                            if (h != hOrig)
                            {
                                map.SetHeight(i, j, h);
                            }
                        }
                    }
                }
            }
            switch (editMode)
            {
                /// Paint material doesn't need to cause a recheck
                /// of heights and rebuild of roads.
                case EditMode.PaintMaterial:
                    break;

                /// But most edits do.
                default:
                    Editing = true;
                    break;
            }
        }   // end of Terrain RenderToHeightMap()

        /// <summary>
        /// Reset after terrain editing.
        /// </summary>
        public void PostEditCheck(List<GameThing> gameThingList)
        {
            if (Editing && (LastEdit != Time.FrameCounter))
            {
                for (int i = 0; i < gameThingList.Count; i++)
                {
                    GameThing thing = gameThingList[i];

                    Vector3 pos = thing.Movement.Position;
                    pos.Z = thing.GetPreferredAltitude();
                    thing.Movement.Position = pos;
                }

                Editing = false;

                // And reset any waypoints.
                WayPoint.RecalcWayPointNodeHeights();
            }

            // Also rebuild water if needed.
            if (waterDirty && WaterParticleEmitter.Active)
            {
                RebuildWater();
            }
        }

        /// <summary>
        /// Updating the water is relatively slow so if we try and do it
        /// every single time we edit the heightmap things just get too slow.
        /// So, we have this function which we can call after the fact to 
        /// update the water.
        /// </summary>
        public void RebuildWater()
        {
            if (WaterParticleEmitter != null)
            {
                WaterParticleEmitter.Active = true;
                WaterParticleEmitter.InitParticles(this);
                WaterParticleEmitter.AddToManager();
            }
            waterDirty = false;
        }   // end of Terrain RebuildWater()

        /// <summary>
        /// Return an iterator over all the water sample points in the world.
        /// Use it and throw it away, don't hang on to it.
        /// </summary>
        /// <returns></returns>
        public VirtualMap.WaterIterator IterateWater()
        {
            return new VirtualMap.WaterIterator(VirtualMap);
        }

        /// <summary>
        /// A holder for the edit palette information that we want to 
        /// persist from frame to frame.  Simplifies things by assuming 
        /// that all the objects are square.
        /// </summary>
        public class EditPalette : INeedsDeviceReset
        {
            private const float yPosition = 3.0f;       // Margin to top of screen.
            private const float textureMargin = 0.0f;   // Margin between texture objects.
            private const float activeSize = 1.5f;      // Size of active elements.
            private const float inactiveSize = 1.0f;    // Size of inactive elements.

            private Texture2D shadowMask = null;

            private Vector2[] texturePosition = new Vector2[4];
            private float[] textureSize = new float[4];

            private UIGrid2DTextureElement[] buttons = new UIGrid2DTextureElement[4];

            private int prevTextureIndex = -1;

            // c'tor
            public EditPalette()
            {
                // Create texture elements for the grids.
                // Start with a blob of common parameters.
                UIGridElement.ParamBlob blob = new UIGridElement.ParamBlob();
                blob.width = 1.0f;
                blob.height = 1.0f;
                blob.edgeSize = 0.5f;
                blob.selectedColor = Color.White;
                blob.unselectedColor = Color.White;
                blob.normalMapName = @"QuarterRound4NormalMap";

                buttons[0] = new UIGrid2DTextureElement(blob, @"Terrain\GroundTextures\" + "holes");
                buttons[1] = new UIGrid2DTextureElement(blob, @"Terrain\GroundTextures\" + "holes");
                buttons[2] = new UIGrid2DTextureElement(blob, @"Terrain\GroundTextures\" + "holes");
                buttons[3] = new UIGrid2DTextureElement(blob, @"Terrain\GroundTextures\" + "holes");

                // Set properties & sizes.
                for (int i = 0; i < 4; i++)
                {
                    buttons[i].NoZ = true;
                    buttons[i].SpecularColor = Color.Gray;

                    textureSize[i] = inactiveSize;
                }

                CalcPositions();
            }

            /// <summary>
            /// Based on the size of each element and the margins, calculate its position.
            /// </summary>
            public void CalcPositions()
            {
                Vector2 cur = new Vector2(0, yPosition);

                int ickyConstantFixMeTexCount = 4;

                // lay out the textures left to right
                for (int i = 0; i < ickyConstantFixMeTexCount; i++)
                {
                    texturePosition[i] = cur;
                    cur.X += textureSize[i] + textureMargin;
                }

                // Calculate the overall width so that we can center the palette on the screen.
                float width = cur.X - textureMargin;
                float leftEdge = -width / 2.0f;

                // Calc position of each button.
                texturePosition[0].X = leftEdge + textureSize[0] / 2.0f;
                for (int i = 1; i < ickyConstantFixMeTexCount; i++)
                {
                    texturePosition[i].X = texturePosition[i - 1].X + textureSize[i - 1] / 2.0f + textureSize[i] / 2.0f + textureMargin;
                }
            }   // end of EditPalette CalcPositions()

            /// <summary>
            /// Renders the terrain editing palette.
            /// </summary>
            /// <param name="editBrushIndex">Index of the current brush being used.</param>
            /// <param name="editingHeightMap">Are we editing the heightmap (as opposed to the textures)?</param>
            /// <param name="editTextureIndex">Index of texture to render in palette.</param>
            public void Render(int editBrushIndex, bool editingHeightMap, int editTextureIndex)
            {
                // First, check if the active texture has changed.  If so, create twitches to smoothly change scales.
                if (editTextureIndex != prevTextureIndex)
                {
                    if (prevTextureIndex != -1)
                    {
                        int index = prevTextureIndex % 4;
                        TwitchManager.Set<float> set = delegate(float value, Object param) { textureSize[index] = value; };
                        TwitchManager.CreateTwitch<float>(textureSize[index], inactiveSize, set, 0.2f, TwitchCurve.Shape.EaseInOut);
                    }
                    {
                        int index = editTextureIndex % 4;
                        TwitchManager.Set<float> set = delegate(float value, Object param) { textureSize[index] = value; };
                        TwitchManager.CreateTwitch<float>(textureSize[index], activeSize, set, 0.2f, TwitchCurve.Shape.EaseInOut);
                    }

                    prevTextureIndex = editTextureIndex;
                }

                // Update positions.
                CalcPositions();

                /*
                // Render the brush.
                Texture2D brushTexture = InGame.inGame.Terrain.GetEditBrushTextureFromIndex(editBrushIndex);
                // put brush halfway between leftmost texture and left edge of screen.
                brushPosition.X = texturePosition[0].X / 2.0f - brushSize / 2.0f;
                editPaletteQuad.RenderWithShadowMask(brushTexture, shadowMask, brushPosition, new Vector2(brushSize));
                */

                // Render the texture swatches if needed.
                if (!editingHeightMap)
                {
                    PerspectiveUICamera camera = new PerspectiveUICamera();

                    // Set up params for rendering UI with this camera.
                    BokuGame.bokuGame.shaderGlobals.SetCamera(camera);

                    TerrainMaterial[] materials = TerrainMaterial.Materials;

                    int matBase = (editTextureIndex / 4) * 4;

                    buttons[0].Scale = textureSize[0];
                    buttons[0].Position = new Vector3(texturePosition[0], 0.0f);
                    buttons[0].DiffuseTexture = materials[0 + matBase].BotTex[(int)Tile.Face.Top];
                    buttons[0].Update();
                    buttons[0].Render(camera);

                    buttons[1].Scale = textureSize[1];
                    buttons[1].Position = new Vector3(texturePosition[1], 0.0f);
                    buttons[1].DiffuseTexture = materials[1 + matBase].BotTex[(int)Tile.Face.Top];
                    buttons[1].Update();
                    buttons[1].Render(camera);

                    buttons[2].Scale = textureSize[2];
                    buttons[2].Position = new Vector3(texturePosition[2], 0.0f);
                    buttons[2].DiffuseTexture = materials[2 + matBase].BotTex[(int)Tile.Face.Top];
                    buttons[2].Update();
                    buttons[2].Render(camera);

                    buttons[3].Scale = textureSize[3];
                    buttons[3].Position = new Vector3(texturePosition[3], 0.0f);
                    buttons[3].DiffuseTexture = materials[3 + matBase].BotTex[(int)Tile.Face.Top];
                    buttons[3].Update();
                    buttons[3].Render(camera);
                }

            }   // end of EditPalette Render()


            public void LoadContent(bool immediate)
            {
                if (shadowMask == null)
                {
                    shadowMask = KoiLibrary.LoadTexture2D(@"Textures\Terrain\TexturePaletteMask");
                }

                for (int i = 0; i < 4; i++)
                {
                    BokuGame.Load(buttons[i], immediate);
                }
            }   // end of EditPalette LoadContent()

            public void InitDeviceResources(GraphicsDevice device)
            {
            }

            public void UnloadContent()
            {
                DeviceResetX.Release(ref shadowMask);

                for (int i = 0; i < 4; i++)
                {
                    BokuGame.Unload(buttons[i]);
                }
            }   // end of EditPalette UnloadContent()

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
                for (int i = 0; i < 4; ++i)
                {
                    BokuGame.DeviceReset(buttons[i], device);
                }
            }

        }   // end of class EditPalette

        /// <summary>
        /// Renders the current edit brush and the texture palette if not -1.
        /// </summary>
        /// <param name="editBrushIndex">Index for current brush texture.</param>
        /// <param name="editingHeightmap">Are we editing the height map or the terrain?</param>
        /// <param name="editTextureIndex">Index for current in-use texture map or height map edit mode.</param>
        public void RenderEditPalette(int editBrushIndex, bool editingHeightmap, ushort editMatIdx) //ToDo(DZ): This method is never called. What's it for? Can we remove it?
        {
            editPalette.Render(editBrushIndex, editingHeightmap, editMatIdx);
            CurrentMaterialIndex = editMatIdx;
        }   // end of Terrain RenderEditPalette()

        public struct CursorVertex : IVertexType
        {
            public Vector3 position;
            public Vector2 corner;

            private const int Stride = 20;
            public static VertexDeclaration decl = null;
            private static readonly VertexElement[] elements =
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
            };

            public CursorVertex(Vector3 position, Vector2 corner)
            {
                this.position = position;
                this.corner = corner;
            }

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

        }   // end of CursorVertex



        //
        // Visualize visibility testing.
        // Uncomment StoreTestRay lines above and Render call in InGame to use...
        //
        static List<TestRay> rayList = new List<TestRay>();
        class TestRay
        {
            public Vector3 p0;
            public Vector3 p1;
            public Color color;

            public TestRay(Vector3 p0, Vector3 p1, Color color)
            {
                this.p0 = p0;
                this.p1 = p1;
                this.color = color;
            }
        }
        static public void StoreTestRay(Vector3 p0, Vector3 p1, Color color)
        {
            TestRay tr = new TestRay(p0, p1, color);
            rayList.Add(tr);
        }

        static public void RenderTestRays(Camera camera)
        {
            for (int i = 0; i < rayList.Count; i++)
            {
                TestRay tr = (TestRay)rayList[i];
                Utils.DrawLine(camera, tr.p0, tr.p1, tr.color);
            }
            rayList.Clear();
        }

        protected Texture2D LoadGroundTexture(string filename)
        {
            return KoiLibrary.LoadTexture2D(@"Textures\Terrain\GroundTextures\" + filename);
        }

        public static void RebuildAll()
        {
            if (Current != null)
            {
                Terrain terrain = Current;
                terrain.VirtualMap.RebuildAll();
                terrain.RebuildWater();
            }
        }

        public void LoadContent(bool immediate)
        {
            editFilter = new EditFilter();
            editPalette = new EditPalette();

            BokuGame.Load(editFilter, immediate);
            BokuGame.Load(editPalette, immediate);
            BokuGame.Load(waterParticleEmitter, immediate);

            VirtualMap.LoadContent(immediate);
        }   // end of Terrain LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
            VirtualMap.InitDeviceResources(device);

            RebuildWater();

            MakeTransforms();
        }

        public static void LoadShared()
        {
            // Init the effect.
            if (effectColor == null)
            {
                LoadEffect();
            }
        }

        public static void InitSharedDeviceResources(GraphicsDevice device)
        {
            LoadMaterialsColor(device, effectColor);
            LoadMaterialsEdit(device, effectEdit);

            InitBusyMessage(device);
        }

        public static void UnloadShared()
        {
            UnLoadMaterials();
            UnLoadEffect();

            UnloadBusyMessage();
        }

        /// <summary>
        /// Initialize our busy message during load.
        /// </summary>
        /// <param name="device"></param>
        private static void InitBusyMessage(GraphicsDevice device)
        {
            if (busyMessage == null)
            {
                // TODO (****) *** How does this get scaled if window changes size???
                Point deviceSize = new Point(device.Viewport.Width, device.Viewport.Height);
                busyMessage = new SimpleMessage();
                busyMessage.Center = new Point(deviceSize.X / 2, deviceSize.Y / 6);

                busyMessage.AddTexture(KoiLibrary.LoadTexture2D(@"Textures\Terrain\busyframe_01"));
                busyMessage.AddTexture(KoiLibrary.LoadTexture2D(@"Textures\Terrain\busyframe_02"));
                busyMessage.AddTexture(KoiLibrary.LoadTexture2D(@"Textures\Terrain\busyframe_03"));
                busyMessage.AddTexture(KoiLibrary.LoadTexture2D(@"Textures\Terrain\busyframe_04"));
                busyMessage.AddTexture(KoiLibrary.LoadTexture2D(@"Textures\Terrain\busyframe_05"));
                busyMessage.AddTexture(KoiLibrary.LoadTexture2D(@"Textures\Terrain\busyframe_06"));
                busyMessage.AddTexture(KoiLibrary.LoadTexture2D(@"Textures\Terrain\busyframe_07"));
                busyMessage.AddTexture(KoiLibrary.LoadTexture2D(@"Textures\Terrain\busyframe_08"));

                Texture2D messageTexture = busyMessage.Texture;
                busyMessage.Size = new Point(messageTexture.Width, messageTexture.Height);

                busyMessage.Text = "Text Unset";
                busyMessage.Font = SharedX.GetGameFont18Bold;
                busyMessage.TextCenter = new Point(
                    busyMessage.Center.X,
                    busyMessage.Center.Y + busyMessage.Size.Y / 2);

                busyMessage.Period = 1.0f;
            }
        }
        /// <summary>
        /// Discard our busy message
        /// </summary>
        private static void UnloadBusyMessage()
        {
            busyMessage = null;
        }

        static public int CompareMatUISlots(int lhs, int rhs)
        {
            /// Try sorting by priority
            if (TerrainMaterial.GetUIPriority((ushort)lhs) > TerrainMaterial.GetUIPriority((ushort)rhs))
                return -1;
            if (TerrainMaterial.GetUIPriority((ushort)lhs) < TerrainMaterial.GetUIPriority((ushort)rhs))
                return 1;

            /// Priority is the same, so go by index to make a stable sort.
            if (lhs < rhs)
                return -1;
            if (lhs > rhs)
                return 1;

            return 0;
        }

        private static void LoadMaterialsColor(GraphicsDevice device, Effect effect)
        {
            CheckMaterials();
            for (ushort i = 0; i < TerrainMaterial.MaxNum; ++i)
            {
                TerrainMaterial.Materials[i].Init(i);
                TerrainMaterial.Materials[i].LoadColor(device, effect, i);
            }
            SortMaterials(true);
        }
        private static void LoadMaterialsEdit(GraphicsDevice device, Effect effect)
        {
            CheckMaterials();
            for (ushort i = 0; i < TerrainMaterial.MaxNum; ++i)
            {
                TerrainMaterial.Materials[i].Init(i);
                TerrainMaterial.Materials[i].LoadEdit(device, effect, i);
            }
            SortMaterials(true);
        }
        public static void SortMaterials(bool store)
        {
            for (ushort i = 0; i < TerrainMaterial.MaxNum; ++i)
            {
                uiSlotToMatIdx[i] = i;
                matIdxToUISlot[i] = i;
            }
            Array.Sort(uiSlotToMatIdx, CompareMatUISlots);
            for (int uiSlot = 0; uiSlot < TerrainMaterial.MaxNum; ++uiSlot)
            {
                matIdxToUISlot[uiSlotToMatIdx[uiSlot]] = uiSlot;
            }
            if (store)
            {
                for (int i = 0; i < TerrainMaterial.MaxNum; ++i)
                {
                    matIdxToLabel[i] = matIdxToUISlot[i] + 1;
                }
            }
        }

        private static void UnLoadMaterials()
        {
            for (int i = 0; i < TerrainMaterial.MaxNum; ++i)
            {
                if (TerrainMaterial.Materials[i] != null)
                    TerrainMaterial.Materials[i].Dispose();
                TerrainMaterial.Materials[i] = null;
            }
        }

        private static Matrix[] MakeTransforms()
        {
            Matrix[] bumpToWorld = new Matrix[Tile.NumFaces];

            bumpToWorld[(int)Tile.Face.Top] = Matrix.Identity;

            bumpToWorld[(int)Tile.Face.Front] = new Matrix(
                1.0f, 0.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 1.0f, 0.0f,
                0.0f, -1.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f);
            bumpToWorld[(int)Tile.Face.Back] = new Matrix(
                -1.0f, 0.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 1.0f, 0.0f,
                0.0f, 1.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f);

            bumpToWorld[(int)Tile.Face.Left] = new Matrix(
                0.0f, -1.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 1.0f, 0.0f,
                -1.0f, 0.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f);
            bumpToWorld[(int)Tile.Face.Right] = new Matrix(
                0.0f, 1.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 1.0f, 0.0f,
                1.0f, 0.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f);

            return bumpToWorld;
        }

        public void UnloadContent()
        {
            Dispose();

            BokuGame.Unload(editFilter);
            BokuGame.Unload(editPalette);

            BokuGame.Unload(waterParticleEmitter);

        }   // end of Terrain UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
            BokuGame.DeviceReset(editFilter, device);
            BokuGame.DeviceReset(editPalette, device);
            if (waterParticleEmitter != null)
            {
                BokuGame.DeviceReset(waterParticleEmitter);
            }
        }

        public void Dispose()
        {
            BatchCache.Unload();
            VirtualMap.UnloadContent();
        }
    }   // end of class Terrain

}   // end of namespace Boku.SimWorld
