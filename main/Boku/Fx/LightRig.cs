// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


/// Relocated from Scenes namespace

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Common;
using Boku.Common.Xml;

using Boku.SimWorld.Terra;

namespace Boku.Fx
{
    public class LightRig
    {
        #region ChildClasses
        public class Light
        {
            #region Members
            private XmlLightData xmlData = new XmlLightData();

            private Vector3 direction = -Vector3.UnitZ;

            #endregion Members

            #region Accessors
            /// <summary>
            /// Axis of rotation for this light
            /// </summary>
            public Vector3 RotationAxis
            {
                get { return xmlData.rotationAxis; }
                set { xmlData.rotationAxis = value; }
            }
            /// <summary>
            /// Rotation rate in revolutions per second
            /// </summary>
            public float RotationRate
            {
                get { return xmlData.rotationRate; }
                set { xmlData.rotationRate = value; }
            }
            /// <summary>
            /// Light color
            /// </summary>
            public Vector3 Color
            {
                get { return xmlData.color; }
                set { xmlData.color = value; }
            }
            /// <summary>
            /// Direction of light (before rotation applied).
            /// </summary>
            public Vector3 Local
            {
                get { return xmlData.local; }
                set { xmlData.local = value; }
            }
            /// <summary>
            /// Current direction of light (after rotation).
            /// </summary>
            public Vector3 Direction
            {
                get { return direction; }
                private set { direction = value; }
            }
            /// <summary>
            /// The underlying xmlData.
            /// </summary>
            public XmlLightData XmlData
            {
                get { return xmlData; }
                set { xmlData = value; }
            }
            #endregion Accessors

            #region Public
            /// <summary>
            /// Constructor
            /// </summary>
            public Light()
            {
            }

            /// <summary>
            /// Update the light's dynamics.
            /// </summary>
            public void Update()
            {
                double rotation = RotationRate * Time.WallClockTotalSeconds * MathHelper.TwoPi
                    / MathHelper.TwoPi;
                rotation -= Math.Floor(rotation);
                rotation *= MathHelper.TwoPi;

                Matrix rot = Matrix.CreateFromAxisAngle(RotationAxis, (float)rotation);

                Direction = Vector3.Normalize(Vector3.TransformNormal(Local, rot));
            }

            /// <summary>
            /// Set the light to the given effect.
            /// </summary>
            /// <param name="effect"></param>
            /// <param name="which"></param>
            public void Set(Effect effect, int which)
            {
                Debug.Assert(effect != null, "Should have filtered out null effect already");
                /// If this happened more than once a frame, it might be worth using
                /// the EffectCache.
                string dirParm = "LightDirection" + which.ToString();
                effect.Parameters[dirParm].SetValue(Direction);
                string colorParm = "LightColor" + which.ToString();
                effect.Parameters[colorParm].SetValue(Color);
            }
            #endregion Public
        }
        #endregion ChildClasses

        #region Members
        private Light[] lightList = new Light[4];           // must match number in shaders
        private Vector3[] lightColors = new Vector3[4];     // used to cache in-transition light values for smooth transitions
        private Vector4[] lightDir = new Vector4[4];        // used to cache in-transition light values for smooth transitions
                                                            // W element is used to attenuate specular.
        private float lightWrap;                            // used to cache in-transition light values for smooth transitions

        private string name = null;
        private float wrap = 1.0f;

        private const int numLights = 4;

        #region Parameter Caching
        private enum EffectParams
        {
            Wrap = numLights * 2
        };
        private class LightRigEffectCache : EffectCache
        {
            private Effect effect = null;

            public int NumLights
            {
                get { return numLights; }
            }
            public Effect Effect
            {
                get { return effect; }
                set { effect = value; }
            }
            
            override protected int NumParams
            {
                get { return NumLights * 2 + 1; }
            }
            override protected string ParamName(int idx)
            {
                return idx < NumLights
                    ? "LightDirection" + idx
                    : idx == (int)EffectParams.Wrap
                        ? "LightWrap"
                        : "LightColor" + (idx - NumLights).ToString();
            }
        }
        static LightRigEffectCache effectCache = new LightRigEffectCache();
        private static EffectParameter Parameter(int param)
        {
            return effectCache.Parameter(param);
        }
        private static EffectParameter Parameter(EffectParams param)
        {
            return effectCache.Parameter((int)param);
        }
        public static void Init(Effect effect)
        {
            effectCache.Effect = effect;
            effectCache.Load(effect);
        }

        public static void DeInit()
        {
            effectCache.UnLoad();
        }

        #endregion Parameter Caching

        #endregion Members

        #region Accessors
        /// <summary>
        /// List of lights
        /// </summary>
        public Light[] LightList
        {
            get { return lightList; }
        }

        public Vector3[] RuntimeLightColors
        {
            get { return lightColors; }
        }

        public Vector4[] RuntimeLightDir
        {
            get { return lightDir; }
        }

        public float RuntimeLightWrap
        {
            get { return lightWrap; }
        }

        /// <summary>
        /// Name by which this rig is accessed.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        /// <summary>
        /// How much diffuse lighting wraps around to the dark side [0..1]
        /// </summary>
        public float Wrap
        {
            get { return wrap; }
            set { wrap = value; }
        }
        #endregion Accessors

        #region Public
        /// <summary>
        /// Constructor
        /// </summary>
        public LightRig()
        {
        }

        /// <summary>
        /// Update all owned lights.
        /// </summary>
        public void Update()
        {
            if (LightList != null)
            {
                foreach (Light light in LightList)
                {
                    light.Update();
                }
            }
        }

        /// <summary>
        /// Set all lights' parameters to effect.
        /// Note this completely clobbers any transitional effects.
        /// </summary>
        /// <param name="effect"></param>
        public void SetToEffect(Effect effect)
        {
            if (LightList != null)
            {
                for(int i = 0; i < LightList.Length; ++i)
                {
                    Light light = LightList[i];

                    Vector3 color = light.Color;
                    float maxChan = Math.Max(color.X, Math.Max(color.Y, color.Z));

                    lightColors[i] = light.Color;
                    lightDir[i] = new Vector4(light.Direction, maxChan);

                    string dirParam = "LightDirection" + i.ToString();
                    if (effect.Parameters[dirParam] != null)
                    {
                        effect.Parameters[dirParam].SetValue(new Vector4(light.Direction, maxChan));
                    }
                    string colorParam = "LightColor" + i.ToString();
                    if (effect.Parameters[colorParam] != null)
                    {
                        effect.Parameters[colorParam].SetValue(light.Color);
                    }
                }

                lightWrap = Wrap;
                Parameter(EffectParams.Wrap).SetValue(ShaderGlobals.MakeWrapVec(Wrap));
            }
        }

        public void SetToEffectWhileTransitioning(Effect effect)
        {
            for (int i = 0; i < lightColors.Length; i++)
            {
                string dirParam = "LightDirection" + i.ToString();
                if (effect.Parameters[dirParam] != null)
                {
                    effect.Parameters[dirParam].SetValue(lightDir[i]);
                }
                string colorParam = "LightColor" + i.ToString();
                if (effect.Parameters[colorParam] != null)
                {
                    effect.Parameters[colorParam].SetValue(lightColors[i]);
                }
            }
            if (effect.Parameters["LightWrap"] != null)
            {
                effect.Parameters["LightWrap"].SetValue(ShaderGlobals.MakeWrapVec(lightWrap));
            }

        }   // end of SetToEffectWhileTransitioning()

        /// <summary>
        /// Transition all of the lights from the old rig to the new one, using lerp amount for the amount between.
        /// </summary>
        /// <param name="effect"></param>
        public void TransitionLightRig(float lerpAmount, Vector3[] sourceLightColors, Vector4[] sourceLightDir, float sourceLightWrap)
        {
            if (LightList != null)
            {
                for (int i = 0; i < LightList.Length; ++i)
                {                    
                    Light targetLight = LightList[i];

                    //lerp the color
                    lightColors[i] = sourceLightColors[i] * (1.0f - lerpAmount) + targetLight.Color * lerpAmount;

                    float oldMaxChan = Math.Max(sourceLightColors[i].X, Math.Max(sourceLightColors[i].Y, sourceLightColors[i].Z));
                    float targetMaxChan = Math.Max(targetLight.Color.X, Math.Max(targetLight.Color.Y, targetLight.Color.Z));

                    float maxChan = oldMaxChan * (1.0f - lerpAmount) + targetMaxChan * (lerpAmount);

                    //lerp the direction vector
                    lightDir[i] = sourceLightDir[i] * (1.0f - lerpAmount) + new Vector4(targetLight.Direction, 1.0f) * lerpAmount;
                    lightDir[i].W = maxChan;

                }

                //lerp wrap
                lightWrap = sourceLightWrap * (1.0f - lerpAmount) + Wrap * (lerpAmount);

                /*
                effect.Parameters["LightWrap"].SetValue(ShaderGlobals.MakeWrapVec(lightWrap));
                */
            }
        }
        
        /// <summary>
        /// Write self to xml
        /// </summary>
        /// <param name="xmlData"></param>
        public void ToXml(XmlLightRigData xmlData)
        {
            int numLights = lightList.Length;
            xmlData.lightData = new XmlLightData[numLights];
            xmlData.name = Name;
            xmlData.wrap = Wrap;

            for (int i = 0; i < numLights; ++i)
            {
                xmlData.lightData[i] = lightList[i].XmlData;
            }
        }

        /// <summary>
        /// Load self from xml.
        /// </summary>
        /// <param name="xmlData"></param>
        public void FromXml(XmlLightRigData xmlData)
        {
            int numLights = xmlData.lightData.Length;
            Debug.Assert(numLights == effectCache.NumLights);

            lightList = new Light[numLights];
            Name = xmlData.name;
            Wrap = xmlData.wrap;

            for (int i = 0; i < numLights; ++i)
            {
                lightList[i].XmlData = xmlData.lightData[i];
            }
        }

        #endregion Public

        #region Internal

        #endregion Internal

    }
}
