// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


/// Relocated from Scenes namespace

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
using Boku.Common;
using Boku.Scenes;
using Boku.SimWorld.Terra;

namespace Boku.Fx
{
    /// <summary>
    /// Sets all the shader globals per frame.
    /// </summary>
    public class ShaderGlobals : INeedsDeviceReset
    {
        /// <summary>
        /// A simple directional light class designed to be used for UI.  These lights vary
        /// in intensity over time so the UI doesn't get to feel too static.
        /// </summary>
        public class DirectionalLight
        {
            #region Members

            Vector3 direction;
            Vector3 originalColor;
            Vector3 currentColor;

            float period0 = 1.0f;
            float period1 = 1.0f;
            float amplitude = 0.0f;

            #endregion Members

            #region Accessors

            public Vector3 Direction
            {
                get { return direction; }
            }

            public Vector3 Color
            {
                get { return currentColor; }
            }

            #endregion

            #region Public

            // c'tor
            public DirectionalLight(Vector3 direction, Vector3 color)
            {
                this.direction = Vector3.Normalize(direction);

                this.originalColor = color;
                this.currentColor = color;

                period0 = 1.0f + (float)BokuGame.bokuGame.rnd.NextDouble();             // in range 1.0 .. 2.0
                period1 = 1.0f + (float)BokuGame.bokuGame.rnd.NextDouble();             // in range 1.0 .. 2.0
                amplitude = 0.1f + 0.2f * (float)BokuGame.bokuGame.rnd.NextDouble();    // in range 0.1 .. 0.3

            }   // end of c'tor

            public void Vary()
            {
                float offset = amplitude * (float)Math.Sin(Time.WallClockTotalSeconds / period0) * (float)Math.Sin(Time.WallClockTotalSeconds / period1);
                currentColor.X = originalColor.X * (1.0f + offset);
                currentColor.Y = originalColor.Y * (1.0f + offset);
                currentColor.Z = originalColor.Z * (1.0f + offset);

            }   // end of Vary()

            #endregion Public

        }   // end of class DirectionalLight

        public class Wind
        {
            #region Members

            const int kNumOsc = 3;
            Vector2[] phases = new Vector2[kNumOsc];
            Vector2[] freqs = new Vector2[kNumOsc];
            Vector2[] speeds = new Vector2[kNumOsc];

            static float windMin = 0.0f;
            static float windMax = 0.25f;

            #region Parameter Caching

            enum EffectParams
            {
                WindStrength,
            }
            static EffectCache effectCache = new EffectCache<EffectParams>();
            static EffectParameter Parameter(EffectParams param)
            {
                return effectCache.Parameter((int)param);
            }

            #endregion Parameter Caching

            #endregion Members

            #region Accessors

            public static float WindMin
            {
                get { return windMin; }
                set { windMin = value; }
            }
            public static float WindMax
            {
                get { return windMax; }
                set { windMax = value; }
            }

            #endregion Accessors

            #region Public

            public Wind()
            {
                freqs[0] = new Vector2(
                    (float)(Math.PI * 2.0 / 20.0),
                    (float)(Math.PI * 2.0 / 20.0));
                freqs[1] = new Vector2(
                    (float)(Math.PI * 2.0 / 40.0),
                    (float)(Math.PI * 2.0 / 40.0));
                freqs[2] = new Vector2(
                    (float)(Math.PI * 2.0 / 60.0),
                    (float)(Math.PI * 2.0 / 60.0));

                speeds[0] = new Vector2(2.5f, 2.5f);
                speeds[1] = new Vector2(3.5f, 3.6f);
                speeds[2] = new Vector2(4.65f, 4.5f);

            }

            public static void Init(Effect effect)
            {
                Wind.effectCache.Load(effect);
            }

            public static void DeInit()
            {
                Wind.effectCache.UnLoad();
            }

            public void Update()
            {
                float dt = (float)Time.GameTimeFrameSeconds;

                for (int osc = 0; osc < kNumOsc; ++osc)
                {
                    phases[osc] += speeds[osc] * dt;
                }
            }

            /// <summary>
            /// Send the wind strength for this position to the shader.
            /// </summary>
            /// <param name="localToWorld"></param>
            public void SetToEffect(Matrix localToWorld)
            {
                float strength = WindAt(localToWorld.Translation);
                Parameter(EffectParams.WindStrength).SetValue(strength);
            }

            /// <summary>
            /// CPU version of the GPU wind strength function in skin.fx.
            /// pos.Z is currently ignored, but might not be in the future.
            /// </summary>
            /// <param name="pos"></param>
            /// <returns></returns>
            public float WindAt(Vector3 pos)
            {
                float strength = 0.0f;
                for (int i = 0; i < kNumOsc; ++i)
                {
                    Vector2 pos2 = new Vector2(pos.X, pos.Y);
                    Vector2 del = pos2 - phases[i];
                    del *= freqs[i];

                    del.X = (float)Math.Cos(del.X);
                    del.Y = (float)Math.Cos(del.Y);

                    strength += MathHelper.Clamp(del.X + del.Y, 0.0f, 1.0f);
                }
                strength = MathHelper.Clamp(strength * (WindMax - WindMin) / 3.0f + WindMin, 0.0f, 1.0f);

                return strength;
            }

            #endregion Public

        }   // end of class Wind

        #region Members

        Vector3[] sourceLightColors = new Vector3[4];   //used to cache in-transition light values for smooth transitions
        Vector4[] sourceLightDir = new Vector4[4];      //used to cache in-transition light values for smooth transitions
        float sourceLightWrap;

        static string[] rigNames = { 
            "Day", 
            "Night", 
            "Space", 
            "Dream", 
            "Venus", 
            "Mars",
            "Dark",
            "Really Dark",

            "Rig_Greeter",
            "Rig_UI" // This one must remain last
        };

        // Fixed value blend states.
        static BlendState bloom0_0;
        static BlendState bloom0_004;
        static BlendState bloom0_15;
        static BlendState bloom0_25;
        static BlendState bloom0_9;
        static BlendState bloom1_0;

        DirectionalLight light0 = new DirectionalLight(new Vector3(0.0f, 0.0f, -1.0f), new Vector3(1.0f, 1.0f, 1.0f));
        DirectionalLight light1 = new DirectionalLight(new Vector3(0.8f, -0.6f, 0.2f), new Vector3(1.0f, 0.8f, 0.0f));
        DirectionalLight light2 = new DirectionalLight(new Vector3(0.2f, 0.7f, 0.1f), new Vector3(0.2f, 0.1f, 0.8f));

        LightRig[] lightRigs = null;
        LightRig currentRig = null;
        Wind currentWind = null;

        bool rigInTransition = false;
        int rigTwitchId = -1;

        Effect effect = null;
        TextureCube envTexture = null;
        string defaultEnvTextureName = @"Textures\EnvBrian";
        string envTextureName = @"Textures\EnvBrian";

        Dictionary<string, Effect> effectDict;

        #region Parameter Caching

        public enum EffectParams
        {
            UILightDirection0,
            UILightColor0,
            UILightDirection1,
            UILightColor1,
            UILightDirection2,
            UILightColor2,
            EyeLocation,
            CameraDir,
            CameraUp,
            WorldToCamera,

            LightWrap,
            Shininess,
            ShadowAttenuation,
            BloomColor,

            EnvironmentMap,

            FogColor,
            FogVector,

            DOF_NearPlane,
            DOF_FocalPlane,
            DOF_FarPlane,
            DOF_MaxBlur,

            BloomStrength,

            PreWorld,
        };
        static EffectCache effectCache = new EffectCache<EffectParams>();
        static EffectParameter Parameter(EffectParams param)
        {
            return effectCache.Parameter((int)param);
        }

        #endregion Parameter Caching

        #endregion Members

        #region Accessors

        /// <summary>
        /// Indicates a rig transition is in progress
        /// </summary>
        public bool RigInTransition
        {
            get { return rigInTransition; }
            set { rigInTransition = value; }
        }

        /// <summary>
        /// Indicates a rig twitch id
        /// </summary>
        public int RigTwitchId
        {
            get { return rigTwitchId; }
            set { rigTwitchId = value; }
        }

        /// <summary>
        /// Light rig currently in use.
        /// </summary>
        public LightRig CurrentRig
        {
            get 
            {
                /// In edit mode, if the current rig is pitch black,
                /// use the dark rig.
                /// TODO (****) Need to update this once new UI is up and running.
                /// Should be able to just test for EditScene.
                if ((currentRig != lightRigs[lightRigs.Length - 1])
                    && (InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.RunSim)
                    && (InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.ToolMenu)
                    && (InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.EditWorldParameters))
                {
                    if (currentRig == lightRigs[7])
                        return lightRigs[6];
                }

                return currentRig; 
            }
        }

        bool EnvTextureIsDefault
        {
            get { return envTextureName == defaultEnvTextureName; }
            set { if (value)envTextureName = defaultEnvTextureName; }
        }

        Effect Effect
        {
            get { return effect; }
        }

        Dictionary<string, Effect> EffectDict
        {
            get { return effectDict; }
        }

        Vector4 FogVector
        {
            get
            {
                float fogStart = 1.0f;
                float fogEnd = 400.0f;
                float fogMax = 0.2f;
                return new Vector4(
                    1.0f / (fogEnd - fogStart), // scale
                    -fogStart / (fogEnd - fogStart), // offset
                    fogMax, // maximum fogginess
                    0.0f); // unused
            }
        }

        /// <summary>
        /// Current texture used for environment mapping.
        /// </summary>
        public string EnvTextureName
        {
            set
            {
                if (envTextureName == value)
                    return;

                if (value == null && EnvTextureIsDefault)
                    return;

                if (value != null)
                {
                    envTextureName = value;
                }
                else
                {
                    EnvTextureIsDefault = true;
                }
            }
        }

        /// <summary>
        /// Number of light rigs currently defined.
        /// </summary>
        public static int NumRigs
        {
            get { return rigNames.Length; }
        }

        /// <summary>
        /// Names of all currently defined light rigs.
        /// </summary>
        public static string[] RigNames
        {
            get { return rigNames; }
        }

        /// <summary>
        /// The name of the light rig used for 3d UI objects.
        /// </summary>
        public static string UIRigName
        {
            get { return rigNames[rigNames.Length - 1]; }
        }

        /// <summary>
        /// The name of the rig used for the opening greeter.
        /// </summary>
        public static string GreeterRigName
        {
            get { return rigNames[rigNames.Length - 2]; }
        }

        /// <summary>
        /// Minimum wind gust level [0..windMax]
        /// </summary>
        public static float WindMin
        {
            get { return Wind.WindMin; }
            set { Wind.WindMin = value; }
        }

        /// <summary>
        /// Maximum wind gust level [windMin..1]
        /// </summary>
        public static float WindMax
        {
            get { return Wind.WindMax; }
            set { Wind.WindMax = value; }
        }

        #endregion Accessors

        #region Public

        /// <summary>
        /// Constructor.
        /// </summary>
        public ShaderGlobals()
        {
            InitLightRigs();
            InitWind();

            // Create fixed value bloom settings.  Hopefully we can use this to cover
            // most of what we need.
            bloom0_0 = CreateBloomBlendState(0.0f);
            bloom0_004 = CreateBloomBlendState(0.004f);
            bloom0_15 = CreateBloomBlendState(0.15f);   // Used for flying scores.
            bloom0_25 = CreateBloomBlendState(0.25f);
            bloom0_9 = CreateBloomBlendState(0.9f);
            bloom1_0 = CreateBloomBlendState(1.0f);

            effectDict = new Dictionary<string, Effect>();

        }   // end of ShaderGlobals c'tor

        public void Update()
        {
            // Don't vary the first light since this illuminates the front of the tiles
            // and Brian wants this to be at full illuminiation all the time.
            //light0.Vary();
            light1.Vary();
            light2.Vary();

            foreach (LightRig rig in lightRigs)
            {
                rig.Update();
            }
            currentWind.Update();

            Shield.Update();
            Ripple.Update();

        }   // end of Update()

        /// <summary>
        /// "Render" for the ShaderGlobals object is a bit different.  What this
        /// is doing is just setting the globals shader values on each effect
        /// in the system.  The design of this is a bit of a legacy hack.  In 
        /// XNA 3.1 we used the "shared" keyword to ensure that the global values
        /// were set on each shader.  With XNA 4 we don't have support for "shared"
        /// so we keep a list of all the shaders in the system and each frame loop
        /// through them setting the globals parameters.  Hopefully this will not
        /// be a perf issue.
        /// </summary>
        /// <param name="camera"></param>
        public void Render(Camera camera)
        {
            foreach (KeyValuePair<string, Effect> kvp in effectDict)
            {
                SetValues(kvp.Value);
            }
        }   // end of Render()

        /// <summary>
        /// Set the parameter values on the given effect.
        /// TODO (****) Can this be made more efficient?  Right now it sets every parameter
        /// every time it is called even though they may not have changed.
        /// </summary>
        /// <param name="effect"></param>
        public void SetValues(Effect effect)
        {
            //
            // 2d UI
            //

            // Don't try and set params if the effect is disposed.  This can happen due to
            // timing foo during device reset.  Really should clean this all out.  In the
            // meantime can just return.  The effect should get replaced next frame and 
            // all will once again be well.
            if (effect.IsDisposed)
            {
                return;
            }

            // All from Globals.fx
            if (effect.Parameters["UILightDirection0"] != null)
            {
                effect.Parameters["UILightDirection0"].SetValue(light0.Direction);
                effect.Parameters["UILightColor0"].SetValue(light0.Color);
                effect.Parameters["UILightDirection1"].SetValue(light1.Direction);
                effect.Parameters["UILightColor1"].SetValue(light1.Color);
                effect.Parameters["UILightDirection2"].SetValue(light2.Direction);
                effect.Parameters["UILightColor2"].SetValue(light2.Color);

                effect.Parameters["FogColor"].SetValue(new Vector3(0.75f, 0.88f, 0.95f/*, 1.0f*/));  // Hazy, light blue.
                effect.Parameters["FogVector"].SetValue(FogVector);

                effect.Parameters["EyeLocation"].SetValue(new Vector4(InGame.inGame.Camera.ActualFrom, 1.0f));
                effect.Parameters["CameraDir"].SetValue(new Vector4(InGame.inGame.Camera.ViewDir, 1.0f));
                effect.Parameters["CameraUp"].SetValue(new Vector4(InGame.inGame.Camera.ViewUp, 1.0f));
                effect.Parameters["WorldToCamera"].SetValue(InGame.inGame.Camera.ViewMatrix);

                effect.Parameters["BloomColor"].SetValue(Vector4.One);
                effect.Parameters["BloomStrength"].SetValue(3.00f);

                float focalDist = InGame.inGame.Camera.GetFocalDistance();
                effect.Parameters["DOF_NearPlane"].SetValue(0.0f);         // currently not used.
                effect.Parameters["DOF_FocalPlane"].SetValue(focalDist);   // Distance at which everything is in focus.  ie blur starts here.
                effect.Parameters["DOF_FarPlane"].SetValue(100.0f);        // Far distance at which blur is max.
                effect.Parameters["DOF_MaxBlur"].SetValue(0.8f);           // Max amount of blur, only applies to far plane.
            }

            //
            // InGame
            //
            if (!RigInTransition)
            {
                CurrentRig.SetToEffect(effect);
            }
            else
            {
                // If the rig is in transition, need to get thse values.
                // TODO (****) This could probably be cleaned up better.  Need an "Active" light rig
                // which just has values pushed into it as it changes.
                currentRig.SetToEffectWhileTransitioning(effect);
            }

            // Set values for point light sources.
            Luz.SetToEffect(effect, false);


            if (effect.Parameters["Shininess"] != null) // In StandardLight.fx
            {
                effect.Parameters["Shininess"].SetValue(1.0f);
            }
            if (effect.Parameters["ShadowAttenuation"] != null) // In Light.fx
            {
                effect.Parameters["ShadowAttenuation"].SetValue(0.7f);
            }

            if (effect.Parameters["EnvironmentMap"] != null)    // In light.fx and others.
            {
                effect.Parameters["EnvironmentMap"].SetValue(envTexture);
            }
        }   // end of SetValues()

        /// <summary>
        /// Setup camera specific paramters when camera changes.
        /// No push/pop, must be manually reset for use this frame.
        /// Will get reset to the normal camera at the beginning of next frame.
        /// </summary>
        /// <param name="camera"></param>
        public void SetCamera(Camera camera)
        {
            // Yes, this is kind of brute force but it is only used in a few places 
            // where perf is not an issue.
            foreach (KeyValuePair<string, Effect> kvp in effectDict)
            {
                Effect effect = kvp.Value;

                SetCamera(effect, camera);
            }
        }   // end of SetCamera()

        /// <summary>
        /// Sets the camera values on a single effect.
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="camera"></param>
        public void SetCamera(Effect effect, Camera camera)
        {
            if (effect.IsDisposed)
            {
                return;
            }

            if (effect.Parameters["EyeLocation"] != null)
            {
                effect.Parameters["EyeLocation"].SetValue(new Vector4(camera.ActualFrom, 1.0f));
            }
            if (effect.Parameters["CameraUp"] != null)
            {
                effect.Parameters["CameraUp"].SetValue(new Vector4(camera.ViewUp, 1.0f));
            }
            if (effect.Parameters["CameraDir"] != null)
            {
                effect.Parameters["CameraDir"].SetValue(new Vector4(camera.ViewDir, 1.0f));
            }
            if (effect.Parameters["WorldToCamera"] != null)
            {
                effect.Parameters["WorldToCamera"].SetValue(camera.ViewMatrix);
            }
        }   // end of SetCamera()

        public void PushEnvMap(TextureCube map)
        {
            foreach (KeyValuePair<string, Effect> kvp in effectDict)
            {
                Effect effect = kvp.Value;

                if (effect.Parameters["EnvironmentMap"] != null)
                {
                    effect.Parameters["EnvironmentMap"].SetValue(map);
                }
            }
        }   // end of PushEnvMap()

        public void PopEnvMap()
        {
            foreach (KeyValuePair<string, Effect> kvp in effectDict)
            {
                Effect effect = kvp.Value;

                if (effect.Parameters["EnvironmentMap"] != null)
                {
                    effect.Parameters["EnvironmentMap"].SetValue(envTexture);
                }
            }
        }   // end of PopEnvMap()

        public void FixBloomColor(Vector4 color)
        {
            foreach (KeyValuePair<string, Effect> kvp in effectDict)
            {
                Effect effect = kvp.Value;

                if (effect.Parameters["BloomColor"] != null)
                {
                    effect.Parameters["BloomColor"].SetValue(color);
                }
            }
        }   // end of FixBloomColor()

        public void ReleaseBloomColor()
        {
            foreach (KeyValuePair<string, Effect> kvp in effectDict)
            {
                Effect effect = kvp.Value;

                if (effect.Parameters["BloomColor"] != null)
                {
                    effect.Parameters["BloomColor"].SetValue(Vector4.One);
                }
            }
        }   // end of ReleaseBloomColor()

        /// <summary>
        /// Set the current light rig by name.
        /// </summary>
        /// <param name="name"></param>
        public void SetCurrentRig(string name)
        {
            if ((currentRig == null) || (currentRig.Name != name))
            {
                currentRig = null;
                if (lightRigs != null)
                {
                    foreach (LightRig lightRig in lightRigs)
                    {
                        if (lightRig.Name == name)
                        {
                            currentRig = lightRig;
                            break;
                        }
                    }
                    if (currentRig == null)
                    {
                        Debug.Assert(true, "Named rig " + name + " not found");
                        currentRig = lightRigs[0];
                    }
                }
                if ((InGame.inGame != null)
                    && (InGame.inGame.CurrentUpdateMode == InGame.UpdateMode.EditWorldParameters))
                {
                    InGame.RefreshThumbnail = true;
                }
            }
        }   // end of SetCurrentRig()

        public float WindAt(Vector3 position)
        {
            return currentWind.WindAt(position);
        }   // end of WindAt()

        /// <summary>
        /// Set wind parameters to the shader.
        /// </summary>
        /// <param name="localToWorld"></param>
        public void SetUpWind(Matrix localToWorld)
        {
            currentWind.SetToEffect(localToWorld);
        }   // end of SetUpWind()

        /// <summary>
        /// Find the light rig with specified name and set it to current.
        /// Blasts previously current rig.
        /// </summary>
        /// <param name="name"></param>
        public void SetLightRig(string name)
        {
            SetCurrentRig(name);
            Debug.Assert(CurrentRig != null, "Failure setting light rig!!");
        }   // end of SetLightRig()

        public void TransitionToLightRig(string name, float transitionTime)
        {
            //check if another transition was running, if so, kill it
            if (RigInTransition)
            {
                //already transitioning to this rig?  if so, let it finish
                if (CurrentRig.Name == name)
                {
                    return;
                }
                StopLightRigTransition();
            }

            LightRig oldRig = CurrentRig;

            //update the source values we're transitioning from for smooth blending
            if (oldRig.LightList != null)
            {
                for (int i = 0; i < oldRig.LightList.Length; ++i)
                {
                    sourceLightColors[i] = oldRig.RuntimeLightColors[i];
                    sourceLightDir[i] = oldRig.RuntimeLightDir[i];
                }

                sourceLightWrap = oldRig.RuntimeLightWrap;
            }

            SetCurrentRig(name);
            RigInTransition = true;

            //since we're moving from 0.0 to 1.0, use the input as the lerp value (even though it won't technically be linear)
            TwitchManager.Set<float> lightLerp = delegate(float value, Object param)
            {
                CurrentRig.TransitionLightRig(value, sourceLightColors, sourceLightDir, sourceLightWrap);
            };

            TwitchCompleteEvent lightLerpComplete = delegate(Object param)
            {
                RigInTransition = false;
            };

            TwitchCompleteEvent lightLerpTerminated = delegate(Object param)
            {
                //do nothing - ensures the completed event won't be called on termination
            };

            RigTwitchId = TwitchManager.CreateTwitch<float>(0.0f, 1.0f, lightLerp, transitionTime, TwitchCurve.Shape.EaseIn, null, lightLerpComplete, lightLerpTerminated, true);
        }   // end of TransitionToLightRig()

        /// <summary>
        /// Cancels any pending light rig transitions
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void StopLightRigTransition()
        {
            if (RigInTransition && RigTwitchId >= 0)
            {
                TwitchManager.KillTwitch<float>(RigTwitchId);
                RigInTransition = false;
                RigTwitchId = -1;
            }
        }   // end of StopLightRigTransition()

        /// <summary>
        /// Push the named rig as current, returning the name of the old rig (for restore).
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string PushLightRig(string name)
        {
            string oldrig = CurrentRig.Name;
            SetLightRig(name);

            // Need to force params to be updated.
            // TODO (****) This whole scheme is getting confusing and ugly.  Need to fix.
            Render(null);

            return oldrig;
        }   // end of PushLightRig()

        /// <summary>
        /// Restore the old rig. Name should be the name returned by PushLightRig.
        /// </summary>
        /// <param name="name"></param>
        public void PopLightRig(string name)
        {
            SetLightRig(name);
        }   // end of PopLightRig()

        public static Vector4 ParticleTint(bool emissive)
        {
            return emissive
                ? Vector4.One
                : SimWorld.SkyBox.Gradient(Terrain.Current.RunTimeSkyIndex)[5];
        }   // end of ParticleTint()

        /// <summary>
        /// Make the correct Vector3 expected in a shader from the input scalar.
        /// </summary>
        /// <param name="wrap"></param>
        /// <returns></returns>
        public static Vector3 MakeWrapVec(float wrap)
        {
            return new Vector3(
                wrap,
                1.0f / (1.0f + wrap),
                wrap / (1.0f + wrap));
        }   // end of MakeWrapVec()

        /// <summary>
        /// Helper to create a Vector4 suitible for passing into a shader that uses
        /// SizeParticle() shader function.
        /// </summary>
        /// <param name="worldRadius"></param>
        /// <param name="minPix"></param>
        /// <param name="maxPix"></param>
        /// <returns></returns>
        public static Vector4 MakeParticleSizeLimit(float worldRadius, float minPix, float maxPix)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;
            float aspectRatio = BokuGame.ScreenSize.X / BokuGame.ScreenSize.Y;
            return new Vector4(
                worldRadius, // world space radius
                aspectRatio, // aspect ratio
                minPix * 2.0f / BokuGame.ScreenSize.X, // min screen radius in ndc (normalized device coordinates)
                maxPix * 2.0f / BokuGame.ScreenSize.X // max screen radius in ndc
                );
        }   // end of MakeParticleSizeLimit()

        /// <summary>
        /// Disable the explicit bloom, where alpha is actually being used for it's
        /// intended purpose.
        /// </summary>
        public static void FixExplicitBloom(float amount)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            // Pick nearest constant.  _Most_ rendering uses exctly one of these, 
            // for the rest we can just fake it.  
            if (amount <= 0.0f)
            {
                device.BlendState = bloom0_0;
            }
            else if (amount <= 0.004f)
            {
                device.BlendState = bloom0_004;
            }
            else if (amount <= 0.15f)
            {
                device.BlendState = bloom0_15;
            }
            else if (amount <= 0.25f)
            {
                device.BlendState = bloom0_25;
            }
            else if (amount <= 0.9f)
            {
                device.BlendState = bloom0_9;
            }
            else
            {
                device.BlendState = bloom1_0;
            }
        }   // end of FixExplicitBloom()

        /// <summary>
        /// Re-enable the explicit bloom, hijacking the rendertarget alpha channel.
        /// </summary>
        public static void ReleaseExplicitBloom()
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;
            device.BlendState = BlendState.NonPremultiplied;
        }   // end of ReleaseExplicitBloom()

        /// <summary>
        /// Register an effect with ShaderGLobals to be updated each frame with
        /// new values for shared parameters.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="effect"></param>
        public static void RegisterEffect(string name, Effect effect)
        {
            UnregisterEffect(name);
            BokuGame.bokuGame.shaderGlobals.EffectDict.Add(name, effect);
        }

        public static void UnregisterEffect(string name)
        {
            if (BokuGame.bokuGame.shaderGlobals.EffectDict.ContainsKey(name))
            {
                BokuGame.bokuGame.shaderGlobals.EffectDict.Remove(name);
            }
        }

        #endregion Public

        #region Internal

        /// <summary>
        /// Create the light rig set.
        /// </summary>
        void InitLightRigs()
        {
            int numRigs = ShaderGlobals.NumRigs;

            lightRigs = new LightRig[numRigs];


            for (int i = 0; i < numRigs; ++i)
            {
                lightRigs[i] = new LightRig();
                InitRig(i, ShaderGlobals.RigNames[i]);
            }

            SetCurrentRig("Day");
        }   // end of InitLightRigs()

        void InitWind()
        {
            currentWind = new Wind();
        }

        /// <summary>
        /// Build light rigs in code. Should eventually be loaded from XML.
        /// </summary>
        /// <param name="which"></param>
        /// <param name="name"></param>
        void InitRig(int which, string name)
        {
            Debug.Assert(which < lightRigs.Length, "Initing lightrig past end of array");
            LightRig rig = lightRigs[which];
            rig.Name = name;

            float baseRate = 1.0f / 30.0f;
            float kSqrt2 = (float)Math.Sqrt(2.0);

            for (int i = 0; i < 4; ++i)
            {
                rig.LightList[i] = new LightRig.Light();
            }

            switch (which)
            {
                case 0: // Day
                    rig.LightList[0].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[0].RotationRate = baseRate;
                    rig.LightList[0].Local = new Vector3(0.0f, 1.0f, -1.0f);
                    rig.LightList[0].Color = new Vector3(0.75f, 0.75f, 0.5f);

                    rig.LightList[1].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[1].RotationRate = baseRate;
                    rig.LightList[1].Local = new Vector3(kSqrt2, kSqrt2, 0.1f);
                    rig.LightList[1].Color = new Vector3(0.05f, 0.05f, 0.3f);

                    rig.LightList[2].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[2].RotationRate = baseRate;
                    rig.LightList[2].Local = new Vector3(kSqrt2, -kSqrt2, 0.1f);
                    rig.LightList[2].Color = new Vector3(0.05f, 0.05f, 0.3f);

                    rig.LightList[3].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[3].RotationRate = baseRate;
                    rig.LightList[3].Local = new Vector3(-1.0f, 0.0f, 0.2f);
                    rig.LightList[3].Color = new Vector3(0.2f, 0.2f, 0.1f);

                    rig.Wrap = 1.0f;
                    break;
                case 1: // Night
                    rig.LightList[0].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[0].RotationRate = baseRate;
                    rig.LightList[0].Local = new Vector3(0.0f, 1.0f, -1.0f);
                    rig.LightList[0].Color = new Vector3(0.2f, 0.2f, 0.4f);

                    rig.LightList[1].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[1].RotationRate = -baseRate;
                    rig.LightList[1].Local = new Vector3(kSqrt2, kSqrt2, 0.1f);
                    rig.LightList[1].Color = new Vector3(0.05f, 0.05f, 0.1f);

                    rig.LightList[2].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[2].RotationRate = -baseRate;
                    rig.LightList[2].Local = new Vector3(kSqrt2, -kSqrt2, 0.1f);
                    rig.LightList[1].Color = new Vector3(0.05f, 0.05f, 0.1f);

                    rig.LightList[3].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[3].RotationRate = -baseRate;
                    rig.LightList[3].Local = new Vector3(-1.0f, 0.0f, 0.2f);
                    rig.LightList[1].Color = new Vector3(0.05f, 0.05f, 0.05f);

                    rig.Wrap = 1.0f;
                    break;
                case 2: // Space
                    rig.LightList[0].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[0].RotationRate = 0;
                    rig.LightList[0].Local = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[0].Color = new Vector3(0.9f, 0.9f, 1.0f);

                    rig.LightList[1].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[1].RotationRate = baseRate;
                    rig.LightList[1].Local = new Vector3(kSqrt2, kSqrt2, -0.1f);
                    rig.LightList[1].Color = new Vector3(0.0f, 0.0f, 0.0f);

                    rig.LightList[2].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[2].RotationRate = baseRate;
                    rig.LightList[2].Local = new Vector3(kSqrt2, -kSqrt2, -0.1f);
                    rig.LightList[2].Color = new Vector3(0, 0, 0);

                    rig.LightList[3].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[3].RotationRate = 1.0f / 3.0f;
                    rig.LightList[3].Local = new Vector3(1.0f, 0.0f, 0.0f);
                    rig.LightList[3].Color = new Vector3(0, 0, 0);

                    rig.Wrap = 0.0f;
                    break;
                case 3: // Dream
                    rig.LightList[0].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[0].RotationRate = baseRate * 0.5f;
                    rig.LightList[0].Local = new Vector3(0.0f, 1.0f, -1.0f);
                    rig.LightList[0].Color = new Vector3(0.65f, 0.65f, 0.5f);

                    rig.LightList[1].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[1].RotationRate = -baseRate * 0.5f;
                    rig.LightList[1].Local = new Vector3(kSqrt2, kSqrt2, 0.1f);
                    rig.LightList[1].Color = new Vector3(0.2f, 0.1f, 0.1f);

                    rig.LightList[2].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[2].RotationRate = -baseRate * 0.5f;
                    rig.LightList[2].Local = new Vector3(-kSqrt2, -kSqrt2, 0.1f);
                    rig.LightList[2].Color = new Vector3(0.2f, 0.1f, 0.1f);

                    rig.LightList[3].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[3].RotationRate =  baseRate * 0.5f;
                    rig.LightList[3].Local = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[3].Color = new Vector3(0.15f, 0.05f, 0.15f);

                    rig.Wrap = 1.0f;
                    break;
                case 4: // Venus
                    rig.LightList[0].RotationAxis = new Vector3(0.0f, 1.0f, 0.0f);
                    rig.LightList[0].RotationRate = baseRate * 4;
                    rig.LightList[0].Local = new Vector3(0.0f, 1.0f, 1.0f);
                    rig.LightList[0].Color = new Vector3(0.2f, 0.8f, 0.2f);

                    rig.LightList[1].RotationAxis = new Vector3(1.0f, 0.0f, 0.0f);
                    rig.LightList[1].RotationRate = baseRate * 3;
                    rig.LightList[1].Local = new Vector3(kSqrt2, kSqrt2, 0.0f);
                    rig.LightList[1].Color = new Vector3(0.4f, 0.2f, 0.1f);

                    rig.LightList[2].RotationAxis = new Vector3(1.0f, 0.0f, 0.0f);
                    rig.LightList[2].RotationRate = baseRate * 3;
                    rig.LightList[2].Local = new Vector3(-kSqrt2, -kSqrt2, 0.0f);
                    rig.LightList[2].Color = new Vector3(0.2f, 0.2f, 0.4f);

                    rig.LightList[3].RotationAxis = new Vector3(1.0f, 0.0f, 0.0f);
                    rig.LightList[3].RotationRate = baseRate * 4;
                    rig.LightList[3].Local = new Vector3(1.0f, 0.0f, 2.0f);
                    rig.LightList[3].Color = new Vector3(0.1f, 0.1f, 0.3f);

                    rig.Wrap = 1.0f;
                    break;
                case 5: // Mars
                    rig.LightList[0].RotationAxis = new Vector3(0.0f, -0.1f, -1.0f);
                    rig.LightList[0].RotationRate = baseRate * 2;
                    rig.LightList[0].Local = new Vector3(0.0f, 1.0f, -1.0f);
                    rig.LightList[0].Color = new Vector3(0.7f, 0.66f, 0.5f);

                    rig.LightList[1].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[1].RotationRate = baseRate * 2;
                    rig.LightList[1].Local = new Vector3(kSqrt2, kSqrt2, 0.1f);
                    rig.LightList[1].Color = new Vector3(0.3f, 0.1f, 0.05f);

                    rig.LightList[2].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[2].RotationRate = baseRate * 2;
                    rig.LightList[2].Local = new Vector3(-kSqrt2, -kSqrt2, 0.1f);
                    rig.LightList[2].Color = new Vector3(0.3f, 0.1f, 0.05f);

                    rig.LightList[3].RotationAxis = new Vector3(0.0f, 0.1f, 1.0f);
                    rig.LightList[3].RotationRate = baseRate * 2;
                    rig.LightList[3].Local = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[3].Color = new Vector3(0.3f, 0.1f, 0.05f);

                    rig.Wrap = 1.0f;
                    break;
                case 6: // Dark
                    rig.LightList[0].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[0].RotationRate = baseRate;
                    rig.LightList[0].Local = new Vector3(0.0f, 1.0f, -1.0f);
                    rig.LightList[0].Color = new Vector3(0.15f, 0.15f, 0.25f);

                    rig.LightList[1].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[1].RotationRate = -baseRate;
                    rig.LightList[1].Local = new Vector3(kSqrt2, kSqrt2, 0.1f);
                    rig.LightList[1].Color = new Vector3(0.05f, 0.1f, 0.05f);

                    rig.LightList[2].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[2].RotationRate = -baseRate;
                    rig.LightList[2].Local = new Vector3(kSqrt2, -kSqrt2, 0.1f);
                    rig.LightList[2].Color = new Vector3(0.05f, 0.05f, 0.1f);

                    rig.LightList[3].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[3].RotationRate = -baseRate;
                    rig.LightList[3].Local = new Vector3(1.0f, 0.0f, 0.0f);
                    rig.LightList[3].Color = new Vector3(0.08f, 0.08f, 0.025f);

                    rig.Wrap = 0.0f;
                    break;
                case 7: // Real Dark
                    rig.LightList[0].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[0].RotationRate = 0;
                    rig.LightList[0].Local = new Vector3(0.0f, 0.0f, -1.0f);
                    rig.LightList[0].Color = Vector3.Zero; // new Vector3(0.05f, 0.05f, 0.08f);

                    rig.LightList[1].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[1].RotationRate = -baseRate;
                    rig.LightList[1].Local = new Vector3(kSqrt2, kSqrt2, 0.1f);
                    rig.LightList[1].Color = Vector3.Zero; // new Vector3(0.01f, 0.02f, 0.01f);

                    rig.LightList[2].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[2].RotationRate = -baseRate;
                    rig.LightList[2].Local = new Vector3(kSqrt2, -kSqrt2, 0.1f);
                    rig.LightList[2].Color = Vector3.Zero; // new Vector3(0.01f, 0.01f, 0.02f);

                    rig.LightList[3].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[3].RotationRate = -baseRate;
                    rig.LightList[3].Local = new Vector3(0.0f, -1.0f, 0.0f);
                    rig.LightList[3].Color = Vector3.Zero; // new Vector3(0.017f, 0.015f, 0.005f);
                    break;

                    /// Greeter rig
                case 8:
                    rig.LightList[0].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[0].RotationRate = 0.0f;
                    rig.LightList[0].Local = new Vector3(0.0f, 0.0f, -1.0f);
                    rig.LightList[0].Color = new Vector3(0.8f, 0.8f, 0.8f);

                    rig.LightList[1].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[1].RotationRate = 0.0f;
                    rig.LightList[1].Local = new Vector3(kSqrt2, kSqrt2, -0.2f);
                    rig.LightList[1].Color = new Vector3(0.4f, 0.8f, 0.4f);

                    rig.LightList[2].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[2].RotationRate = 0.0f;
                    rig.LightList[2].Local = new Vector3(kSqrt2, -kSqrt2, -0.2f);
                    rig.LightList[2].Color = new Vector3(0.2f, 0.2f, 0.4f);

                    rig.LightList[3].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[3].RotationRate = 0.0f;
                    rig.LightList[3].Local = new Vector3(-1.0f, 0.0f, 0.2f);
                    rig.LightList[3].Color = new Vector3(1.0f, 0.9f, 0.8f);

                    rig.Wrap = 0.0f;
                    break;


                    /// Rig_UI, must be last in the list
                default:
                    rig.LightList[0].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[0].RotationRate = 0.0f;
                    rig.LightList[0].Local = new Vector3(0.0f, 0.0f, -1.0f);
                    rig.LightList[0].Color = new Vector3(0.8f, 0.8f, 0.8f);

                    rig.LightList[1].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[1].RotationRate = 0.0f;
                    rig.LightList[1].Local = new Vector3(kSqrt2, kSqrt2, -0.2f);
                    rig.LightList[1].Color = new Vector3(0.4f, 0.4f, 0.4f);

                    rig.LightList[2].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[2].RotationRate = 0.0f;
                    rig.LightList[2].Local = new Vector3(kSqrt2, -kSqrt2, -0.2f);
                    rig.LightList[2].Color = new Vector3(0.2f, 0.2f, 0.4f);

                    rig.LightList[3].RotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rig.LightList[3].RotationRate = 0.0f;
                    rig.LightList[3].Local = new Vector3(-1.0f, 0.0f, 0.2f);
                    rig.LightList[3].Color = new Vector3(1.0f, 0.9f, 0.8f);

                    rig.Wrap = 0.0f;
                    break;
            }
        }   // end of InitRig()

        static BlendState CreateBloomBlendState(float bloom)
        {
            BlendState result = new BlendState();

            result.BlendFactor = new Color(0, 0, 0, MathHelper.Clamp(1.0f - bloom, 0.0f, 1.0f));
            if (BokuSettings.Settings.PreferReach)
            {
                result.AlphaSourceBlend = Blend.One;
                result.AlphaDestinationBlend = Blend.Zero;
            }
            else
            {
                result.AlphaSourceBlend = Blend.Zero;
                result.AlphaDestinationBlend = Blend.BlendFactor;
            }

            return result;
        }   // end of CreateBloomBlendState()

        public void LoadContent(bool immediate)
        {
            // Init the effect.  Doesn't really matter which one, just
            // has to be one that uses the same shared parameters.
            if (effect == null)
            {
                effect = KoiLibrary.LoadEffect(@"Shaders\Standard");
                ShaderGlobals.RegisterEffect("Standard", effect);

                effectCache.Load(effect);
                LightRig.Init(effect);
                Wind.Init(effect);
                Luz.Init(effect);
                Shield.Load();
            }

            // Load the environment texture.
            if (envTexture == null)
            {
                envTexture = BokuGame.Load<TextureCube>(BokuGame.Settings.MediaPath + envTextureName);
            }
        }   // end of ShaderGlobals RenderObj LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
            Ripple.Load(device);
        }

        public void UnloadContent()
        {
            if (effect != null)
            {
                effectCache.UnLoad();
                LightRig.DeInit();
                Wind.DeInit();
                Luz.DeInit();
                Shield.Unload();
                Ripple.Unload();
            }
            DeviceResetX.Release(ref effect);
            DeviceResetX.Release(ref envTexture);
        }   // end of ShaderGlobals RenderObj UnloadContent()

        /// <summary>
        /// Recreate render targets.
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
        }

        #endregion Internal

    }   // end of class ShaderGlobals

}   // end of namespace Boku
