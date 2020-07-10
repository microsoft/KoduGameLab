// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Common;
using Boku.Common.Xml;

namespace Boku.SimWorld.Terra
{
    /// <summary>
    /// The book keeping seed for a single body of water. A body of water
    /// is contiguous, and has a single surface level (not counting waves).
    /// </summary>
    public class Water
    {
        #region Constants
        private const int MaxNumWater = 255;
        public const int InvalidLabel = 0x0;
        public const int InvalidType = -1;
        #endregion

        #region Members

        #region SavedState

        private Vector3 seedPosition = Vector3.Zero;
        private Vector3 originalSeedPosition = Vector3.Zero;
        public class Definition
        {
            /// <summary>
            /// Water color
            /// </summary>
            public Vector4 Color;
            /// <summary>
            /// Fresnel factor
            /// </summary>
            public Vector2 Fresnel;
            /// <summary>
            /// Texture2D tiling scale factor, 1.0f / (World Size covered by single tiling)
            /// </summary>
            public float TextureTiling;
            /// <summary>
            /// Strength of specular and environment maps.
            /// </summary>
            public float Shininess = 1.0f;
            /// <summary>
            /// Emissive component of color
            /// </summary>
            public Vector3 Emissive = Vector3.Zero;
            /// <summary>
            /// Does this water get bloomed out?
            /// </summary>
            public float ExplicitBloom = 0.0f;
            /// <summary>
            /// The name this definition goes by to the rest of the world.
            /// </summary>
            public string Name;
            /// <summary>
            /// The type handle for this definition.
            /// </summary>
            public int Type = 0;

            public Definition()
            {

            }

            public Definition(Definition c)
            {
                Color = c.Color;
                Fresnel = c.Fresnel;
                TextureTiling = c.TextureTiling;
                Shininess = c.Shininess;
                Emissive = c.Emissive;
                ExplicitBloom = c.ExplicitBloom;
                Name = c.Name;
                Type = c.Type;
            }

            public static Definition Default
            {
                get
                {
                    Definition def = new Definition();
                    def.Color = new Vector4(0.15f, 0.55f, 0.82f, 10.0f);
                    def.Fresnel = new Vector2(0.2f, 0.3f);
                    def.TextureTiling = 1.0f / 10.0f;
                    def.Shininess = 1.0f;
                    def.Emissive = Vector3.Zero;
                    def.ExplicitBloom = 0.0f;
                    def.Name = "Lagoon";
                    return def;
                }
            }
        }
        Definition definition = Definition.Default;
        Definition originalDefinition = Definition.Default;

        Definition transitionFromDefinition = Definition.Default;
        Definition transitionToDefinition = Definition.Default;
        private float transitionAmount = 0.0f;
        private bool waterTransitioning = false;
        private int waterTwitchId = -1;

        #endregion SavedState

        private int label = InvalidLabel;

        private List<Vector2> tileCenters = null;

        private bool queuedForErase = false;
        private bool queuedForDispose = false;
        private bool queuedForFill = false;
        private bool edgeOfWorld = false;

        #region Static
        private static Water[] labelToWater = new Water[MaxNumWater];
        private static List<Water> allWaters = new List<Water>();
        private static List<Definition> allTypes = new List<Definition>();
        private static int currentType = 0;
        private static bool justEmptied = false;
        #endregion Static

        #endregion Members

        #region Accessors

        internal bool EdgeOfWorld
        {
            get { return edgeOfWorld; }
            set { edgeOfWorld = value; }
        }

        /// <summary>
        /// Return the nominal (waveless) height of this water body surface.
        /// </summary>
        public float BaseHeight
        {
            get { return SeedPosition.Z; }
            set {
                if (InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.RunSim)
                {
                    originalSeedPosition.Z = value;
                }

                if (value <= originalSeedPosition.Z)
                {
                    seedPosition.Z = value;
                }
            }
        }

        /// <summary>
        /// Position from which to build this water, should be deepest point.
        /// </summary>
        public Vector3 SeedPosition
        {
            get { return seedPosition; }
            set
            {
                originalSeedPosition = value;
                seedPosition = value;
            }
        }

        public Vector2 SeedPosition2D
        {
            set
            {
                seedPosition.X = originalSeedPosition.X = value.X;
                seedPosition.Y = originalSeedPosition.Y = value.Y;
            }
        }

        /// <summary>
        /// Color of this water body.
        /// </summary>
        public Vector4 Color
        {
            get { return definition.Color; }
            set { definition.Color = value; }
        }
        /// <summary>
        /// Fresnel factor for this water body, scale then offset.
        /// </summary>
        public Vector2 Fresnel
        {
            get { return definition.Fresnel; }
            set { definition.Fresnel = value; }
        }
        /// <summary>
        /// Strength of specular and env map.
        /// </summary>
        public float Shininess
        {
            get { return definition.Shininess; }
            set { definition.Shininess = value; }
        }
        /// <summary>
        /// Emissive component of color.
        /// </summary>
        public Vector3 Emissive
        {
            get { return definition.Emissive; }
            set { definition.Emissive = value; }
        }
        /// <summary>
        /// One over size of a texture tiling in world space.
        /// </summary>
        public float TextureTiling
        {
            get { return definition.TextureTiling; }
            set { definition.TextureTiling = value; }
        }
        /// <summary>
        /// Does this get blown out by bloom?
        /// </summary>
        public float ExplicitBloom
        {
            get { return definition.ExplicitBloom; }
            set { definition.ExplicitBloom = value; }
        }
        /// <summary>
        /// Return the name id for this type of water.
        /// </summary>
        public string TypeName
        {
            get { return definition.Name; }
        }
        /// <summary>
        /// The type handle for this water's definition.
        /// </summary>
        public int Type
        {
            get { return definition.Type; }
        }
        /// <summary>
        /// Look up label for this body.
        /// </summary>
        public int Label
        {
            get { return label; }
            set { label = value; }
        }
        /// <summary>
        /// Defining constants for this water body.
        /// </summary>
        public Definition Define
        {
            get { return definition; }
            private set { definition = value; }
        }
        /// <summary>
        /// The tiles touched by this body.
        /// </summary>
        public List<Vector2> TileCenters
        {
            get { return tileCenters; }
            set { tileCenters = value; }
        }
        /// <summary>
        /// Are we queued for erasure from virtual map?
        /// </summary>
        public bool QueuedForErase
        {
            get { return queuedForErase; }
            set { queuedForErase = value; }
        }
        /// <summary>
        /// Are we queued for a flood fill?
        /// </summary>
        public bool QueuedForFill
        {
            get { return queuedForFill; }
            set { queuedForFill = value; }
        }

        /// <summary>
        /// Are we queued for disposal of resources.
        /// </summary>
        public bool QueuedForDispose
        {
            get { return queuedForDispose; }
            set { queuedForDispose = value; }
        }
        /// <summary>
        /// The current type to be assigned.
        /// </summary>
        public static int CurrentType
        {
            get { return currentType; }
            set
            {
                value = value % allTypes.Count;
                if (value < 0)
                    value += allTypes.Count;
                currentType = value;
            }
        }
        public static List<Definition> Types
        {
            get { return allTypes; }
        }

        /// <summary>
        /// Flag to allow a final pass over water tiles to dispose
        /// when the last water is deleted. 
        /// </summary>
        public static bool JustEmptied
        {
            get { return justEmptied; }
            set { justEmptied = value; }
        }

        #region Internal
        private static Water[] LabelToWater
        {
            get { return labelToWater; }
            set { labelToWater = value; }
        }
        public static List<Water> AllWaters
        {
            get { return allWaters; }
            private set { allWaters = value; }
        }
        #endregion Internal

        #endregion Accessors

        #region Public

        /// <summary>
        /// Restores water to it's original values which
        /// may have been changed during run.
        /// </summary>
        public void Reset()
        {
            definition = originalDefinition;
            seedPosition = originalSeedPosition;
        }   // end of Reset()

        /// <summary>
        /// Set the type of water from the library.
        /// </summary>
        /// <param name="type"></param>
        public void SetType(int type)
        {
            definition = allTypes[type];
            originalDefinition = allTypes[type];
        }

        public void SetRuntimeType(int type)
        {
            definition = allTypes[type];
        }

        public void TransitionToWaterType(int newType, float transitionTime)
        {
            //check if another transition was running, if so, don't start another
            if (waterTransitioning)
            {
                //already transitioning to this type?  if so, return and let the current transition finish
                if (allTypes[newType] == transitionToDefinition)
                {
                    return;
                }
                StopWaterTransition();
            }

            transitionFromDefinition = definition;
            transitionToDefinition = allTypes[newType];
            transitionAmount = 0.0f;
            waterTransitioning = true;

            //make a copy so we aren't adjusting the real values
            definition = new Definition(definition);

            //since we're moving from 0.0 to 1.0, use the input as the lerp value (even though it won't technically be linear)
            TwitchManager.Set<float> waterLerp = delegate(float value, Object param)
            {
                transitionAmount = value;
                definition.Color = transitionFromDefinition.Color * (1.0f - value) + transitionToDefinition.Color * value;
                definition.Emissive = transitionFromDefinition.Emissive * (1.0f - value) + transitionToDefinition.Emissive * value;
                definition.ExplicitBloom = transitionFromDefinition.ExplicitBloom * (1.0f - value) + transitionToDefinition.ExplicitBloom * value;
                definition.Fresnel = transitionFromDefinition.Fresnel * (1.0f - value) + transitionToDefinition.Fresnel * value;
                definition.Shininess = transitionFromDefinition.Shininess * (1.0f - value) + transitionToDefinition.Shininess * value;
                definition.TextureTiling = transitionFromDefinition.TextureTiling * (1.0f - value) + transitionToDefinition.TextureTiling * value;
            };

            TwitchCompleteEvent waterLerpComplete = delegate(Object param)
            {
                definition = transitionToDefinition;
                waterTransitioning = false;
            };

            TwitchCompleteEvent waterLerpTerminated = delegate(Object param) {};

            waterTwitchId = TwitchManager.CreateTwitch<float>(0.0f, 1.0f, waterLerp, transitionTime, TwitchCurve.Shape.EaseIn, null, waterLerpComplete, waterLerpTerminated, true);
        }

        public void StopWaterTransition()
        {
            if (waterTransitioning && waterTwitchId >= 0)
            {
                TwitchManager.KillTwitch<float>(waterTwitchId);
            }
        }

        #region Static
        /// <summary>
        /// Look up the water body associated with this label.
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        public static Water FromLabel(int label)
        {
            int i = label - 1;
            Water water = (i >= 0) && (i < MaxNumWater)
                ? LabelToWater[i]
                : null;

            Debug.Assert((water == null) || (water.Label == (byte)label), "Found wrong water in slot");

            return water;
        }

        /// <summary>
        /// Copy out the current state into XML structure.
        /// </summary>
        /// <param name="terrainData"></param>
        public static XmlWaterData[] CopyWaters()
        {
            XmlWaterData[] waterData = new XmlWaterData[AllWaters.Count];

            for (int i = 0; i < AllWaters.Count; ++i)
            {
                waterData[i] = new XmlWaterData();
                AllWaters[i].CopyToXml(waterData[i]);
            }
            return waterData;
        }

        /// <summary>
        /// Throw away all current water bodies.
        /// </summary>
        public static void Flush()
        {
            for (int i = 0; i < MaxNumWater; ++i)
            {
                LabelToWater[i] = null;
            }
            AllWaters.Clear();
        }

        /// <summary>
        /// Load up our table of water types. This should eventually
        /// load from XML.
        /// </summary>
        public static void InitTypes()
        {
            Definition def = Definition.Default;
            def.Type = allTypes.Count;
            allTypes.Add(def);

            def = new Definition();
            def.Color = new Vector4(0.1f, 0.3f, 0.2f, 1.0f);
            def.Fresnel = new Vector2(0.7f, 0.7f);
            def.TextureTiling = 1.0f / 5.0f;
            def.Shininess = 0.2f;
            def.Emissive = Vector3.Zero;
            def.Name = "Dark Ocean";
            def.Type = allTypes.Count;
            allTypes.Add(def);

            def = new Definition();
            def.Color = new Vector4(0.6f, 0.4f, 0.1f, 1.0f);
            def.Fresnel = new Vector2(0.2f, 0.6f);
            def.TextureTiling = 1.0f / 30.0f;
            def.Shininess = 1.0f;
            def.ExplicitBloom = 0.0f;
            def.Emissive = Vector3.Zero;
            def.Name = "Texas Red";
            def.Type = allTypes.Count;
            allTypes.Add(def);

            def = new Definition();
            def.Color = new Vector4(0.1f, 0.6f, 0.1f, 1.0f);
            def.Fresnel = new Vector2(0.2f, 0.6f);
            def.TextureTiling = 1.0f / 20.0f;
            def.Shininess = 0.5f;
            def.ExplicitBloom = 0.0f;
            def.Emissive = Vector3.Zero;
            def.Name = "Emerald Coast";
            def.Type = allTypes.Count;
            allTypes.Add(def);

            def = new Definition();
            def.Color = new Vector4(0.2f, 0.1f, 0.1f, 1.0f);
            def.Fresnel = new Vector2(0.2f, 0.6f);
            def.TextureTiling = 1.0f / 30.0f;
            def.Shininess = 1.0f;
            def.Emissive = new Vector3(0.6f, 0.4f, 0.1f);
            def.ExplicitBloom = 0.15f;
            def.Name = "Martian Canal";
            def.Type = allTypes.Count;
            allTypes.Add(def);

            def = new Definition();
            def.Color = new Vector4(0.1f, 0.2f, 0.1f, 1.0f);
            def.Fresnel = new Vector2(0.2f, 0.6f);
            def.TextureTiling = 1.0f / 20.0f;
            def.Shininess = 0.5f;
            def.Emissive = new Vector3(0.1f, 0.6f, 0.1f);
            def.ExplicitBloom = 0.1f;
            def.Name = "Venus";
            def.Type = allTypes.Count;
            allTypes.Add(def);

            def = new Definition();
            def.Color = new Vector4(0.2f, 0.1f, 0.1f, 1.0f);
            def.Fresnel = new Vector2(0.0f, 1.0f);
            def.TextureTiling = 1.0f / 200.0f;
            def.Shininess = 0.0f;
            def.Emissive = new Vector3(0.8f, 0.4f, 0.1f);
            def.ExplicitBloom = 0.2f;
            def.Name = "Lava";
            def.Type = allTypes.Count;
            allTypes.Add(def);

            def = new Definition();
            def.Color = Vector4.UnitW;
            def.Fresnel = new Vector2(0.0f, 1.0f);
            def.TextureTiling = 1.0f / 20.0f;
            def.Shininess = 0.2f;
            def.Emissive = Vector3.Zero;
            def.ExplicitBloom = 0.0f;
            def.Name = "Oil";
            def.Type = allTypes.Count;
            allTypes.Add(def);

            def = new Definition();
            def.Color = new Vector4(0.6f, 0.6f, 0.75f, 1.0f);
            def.Fresnel = new Vector2(0.0f, 1.0f);
            def.TextureTiling = 0.025f;
            def.Shininess = 0.7f;
            def.Emissive = Vector3.Zero;
            def.ExplicitBloom = 0.0f;
            def.Name = "Silver";
            def.Type = allTypes.Count;
            allTypes.Add(def);

            def = new Definition();
            def.Color = new Vector4(0.9f, 0.77f, 0.5f, 1.0f);
            def.Fresnel = new Vector2(0.0f, 1.0f);
            def.TextureTiling = 0.05f;
            def.Shininess = 0.5f;
            def.Emissive = Vector3.Zero;
            def.ExplicitBloom = 0.0f;
            def.Name = "Gold";
            def.Type = allTypes.Count;
            allTypes.Add(def);

        }

        /// <summary>
        /// Flood fill to create water at height h from pos extending out
        /// everywhere not separated by land boundaries or end of world.
        /// Will absorb any contained or containing water bodies.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static Water Create(Vector2 pos, float h)
        {
            Water water = new Water();
            water.SeedPosition = new Vector3(pos.X, pos.Y, h);
            if (!water.FindLabel())
                return null;

            water.definition = allTypes[currentType];
            water.originalDefinition = allTypes[currentType];

            return water;
        }
        #endregion Static

        /// <summary>
        /// Load this up from xml.
        /// </summary>
        /// <param name="waterData"></param>
        public void Init(XmlWaterData waterData)
        {
            //make sure any water transitions are stopped
            StopWaterTransition();

            if (String.IsNullOrEmpty(waterData.name))
            {
                waterData.name = "Lagoon";
            }
            if (waterData.name != "")
            {
                definition = FindDefinition(waterData.name);
                originalDefinition = definition;
            }
        }

        /// <summary>
        /// Clear self out of the world.
        /// </summary>
        public void Dispose()
        {
            ReleaseLabel(Label);
            Label = InvalidLabel;
        }

        #endregion Public

        #region Internal
        /// <summary>
        /// Constructor
        /// </summary>
        private Water()
        {
        }
        
        private void CopyToXml(XmlWaterData waterData)
        {
            waterData.Color.X = Color.X;
            waterData.Color.Y = Color.Y;
            waterData.Color.Z = Color.Z;
            waterData.Color.W = Color.W;

            waterData.SeedPosition.X = SeedPosition.X;
            waterData.SeedPosition.Y = SeedPosition.Y;
            waterData.SeedPosition.Z = SeedPosition.Z;

            waterData.Fresnel.X = Fresnel.X;
            waterData.Fresnel.Y = Fresnel.Y;

            waterData.TextureTiling = TextureTiling;

            waterData.Shininess = Shininess;

            waterData.name = TypeName;
        }

        /// <summary>
        /// Find an available label for this water, set it to water and return label.
        /// If none left, return InvalidLabel.
        /// </summary>
        /// <param name="water"></param>
        /// <returns>The water's new label</returns>
        private bool FindLabel()
        {
            for (int i = 0; i < MaxNumWater; ++i)
            {
                if (LabelToWater[i] == null)
                {
                    LabelToWater[i] = this;
                    Label = i + 1;
                    AllWaters.Add(this);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Look up the definition. Doesn't currently happen enough to optimize.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private Definition FindDefinition(string name)
        {
            foreach (Definition def in allTypes)
            {
                if (def.Name == name)
                {
                    return def;
                }
            }
            return null;
        }

        /// <summary>
        /// Mark an label as available for reuse.
        /// </summary>
        /// <param name="label"></param>
        private static void ReleaseLabel(int label)
        {
            --label;
            Debug.Assert((label >= 0) && (label < MaxNumWater), "Invalid Label");
            Debug.Assert(LabelToWater[label] != null, "Releasing unused label");

            AllWaters.Remove(LabelToWater[label]);
            LabelToWater[label] = null;
        
            JustEmptied = AllWaters.Count == 0;
        }
        #endregion Internal
    }
}
