
using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Common;
using Boku.Common.Xml;

namespace Boku.Fx
{
    public class ShowBudget
    {
        #region Members
        private static Texture2D background = null;
        private static Texture2D mask = null;
        private static Texture2D glow = null;

        private static EffectTechnique budgetTechnique = null;
        private static EffectTechnique glowTechnique = null;

        private float lastFrac = -1.0f;

        #region BorderGlowMembers
        /// <summary>
        /// State for the attention border.
        /// </summary>
        private enum GlowState
        {
            Idle,
            Climbing,
            Holding,
            Falling
        };
        private GlowState state = GlowState.Idle;
        private float borderIntensityGoal = 0.0f;
        private Vector3 borderColorGoal = Vector3.Zero;
        private float holdTimer = 0.0f;
        private float borderIntensity = 0.0f;
        private Vector3 borderColor = Vector3.Zero;
        #endregion BorderGlowMembers

        struct Vertex : IVertexType
        {
            public Vector2 position;

            static VertexDeclaration decl = null;
            static VertexElement[] elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                // Total == 8 bytes
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

        }

        private Vertex[] verts = new Vertex[4];
        #endregion Members

        #region Parameter Caching
        enum EffectParams
        {
            Color,
            Height,
            Transform,
            Background,
            Mask,
            Glow,
            BorderGlowColor,
        };
        private static EffectCache effectCache = new EffectCache<EffectParams>();
        private static EffectParameter Parameter(EffectParams param)
        {
            return effectCache.Parameter((int)param);
        }
        private static Effect effect = null;
        #endregion Parameter Caching

        #region Accessors
        #endregion Accessors

        #region Public
        /// <summary>
        /// Constructor, set up the vertices, since they are constant.
        /// </summary>
        public ShowBudget()
        {
            verts[0].position = new Vector2(0.0f, 1.0f);
            verts[1].position = new Vector2(1.0f, 1.0f);
            verts[2].position = new Vector2(0.0f, 0.0f);
            verts[3].position = new Vector2(1.0f, 0.0f);
        }

        /// <summary>
        /// Display self to the screen. Depending on mode, may not be visible.
        /// </summary>
        public void Render()
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            float frac = InGame.inGame.FractionFullUnclamped;

            Vector4 transform = GetTransform(device);

            Vector4 color = CalcColor(frac);
            Vector2 height = CalcHeight(frac);

            effect.CurrentTechnique = budgetTechnique;

            Parameter(EffectParams.Color).SetValue(color);
            Parameter(EffectParams.Height).SetValue(height);
            Parameter(EffectParams.Transform).SetValue(transform);
            Parameter(EffectParams.Background).SetValue(background);
            Parameter(EffectParams.Mask).SetValue(mask);

            DrawPrim(device);

            BorderGlow(device, frac, transform, color);

        }

        #endregion Public

        #region Internal

        /// <summary>
        /// Assume all state is set up, just throw the triangles at the screen.
        /// </summary>
        /// <param name="device"></param>
        private void DrawPrim(GraphicsDevice device)
        {
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; ++i)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];

                pass.Apply();

                device.DrawUserPrimitives<Vertex>(
                    PrimitiveType.TriangleStrip,
                    verts,
                    0,
                    2);

            }
        }

        /// <summary>
        /// Create the scale/offset transform to put the vertices at the right place on the screen.
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        private Vector4 GetTransform(GraphicsDevice device)
        {
            int leftBorder = 0;
            int width = 0;
            int height = 0;
            float fraction = 0.2f;
            if (BokuGame.ScreenSize.X > BokuGame.ScreenSize.Y)
            {
                height = (int)(BokuGame.ScreenSize.Y * fraction + 0.99f);
                width = (int)(height * background.Width / background.Height + 0.5f);
            }
            else
            {
                width = (int)(BokuGame.ScreenSize.X * fraction + 0.99f);
                height = (int)(width * background.Height / background.Width+ 0.5f);
            }

            float maxX = BokuGame.ScreenSize.X - leftBorder;
            float minX = maxX - width;

            maxX = maxX * 2.0f / BokuGame.ScreenSize.X - 1.0f;
            minX = minX * 2.0f / BokuGame.ScreenSize.X - 1.0f;

            int midY = (int)(BokuGame.ScreenSize.Y / 2);
            float maxY = midY + height / 2;
            float minY = midY - (height + 1) / 2;

            maxY = maxY * 2.0f / BokuGame.ScreenSize.Y - 1.0f;
            minY = minY * 2.0f / BokuGame.ScreenSize.Y - 1.0f;

            return new Vector4(
                maxX - minX,
                minY - maxY,
                minX,
                maxY);
        }

        /// <summary>
        /// Calculate the cutoff height of the mercury in the thermometer.
        /// </summary>
        /// <param name="frac"></param>
        /// <returns></returns>
        private Vector2 CalcHeight(float frac)
        {
            Vector2 height = Vector2.Zero;
            height.X = frac * (background.Height - 5.0f) / background.Height;
            height.Y = height.X - 10.0f / background.Height;
            return height;
        }
        /// <summary>
        /// Calculate the color of the mercury.
        /// </summary>
        /// <param name="frac"></param>
        /// <returns></returns>
        private Vector4 CalcColor(float frac)
        {
            Vector4 color = Vector4.Zero;

            float greenCutoff = 0.33f;
            float yellowCutoff = 0.66f;
            if (frac < greenCutoff)
            {
                color = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
            }
            else if (frac < yellowCutoff)
            {
                float parm = MathHelper.Clamp((frac - greenCutoff) / (yellowCutoff - greenCutoff), 0.0f, 1.0f);
                color = MyMath.Lerp(
                    new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
                    new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                    parm);
            }
            else
            {
                float parm = MathHelper.Clamp((frac - yellowCutoff) / (1.0f - yellowCutoff), 0.0f, 1.0f);
                color = MyMath.Lerp(
                    new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                    new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                    parm);
            }
            float startOn = 0.2f;
            float fullOn = 0.33f;
            float minAlpha = 0.2f;

            /// Note that we have different criteria during gameplay. In general,
            /// we want to be more out of the way during gameplay, only showing up
            /// when resources are getting scarce and shutoff is imminent.
            if (InGame.inGame.CurrentUpdateMode == InGame.UpdateMode.RunSim)
            {
                startOn = 0.8f;
                fullOn = 0.85f;
                minAlpha = 0.0f;
            }

            color.W = MathHelper.Clamp((frac - startOn) / (fullOn - startOn), 0.0f, 1.0f);
            color.W = color.W * (1.0f - minAlpha) + minAlpha;
            return color;
        }

        /// Section devoted to the transient border glow.
        #region BorderGlow

        /// <summary>
        /// Draw a transient colored border indicating which way things are changing,
        /// cost going up (red) or down (green).
        /// </summary>
        /// <param name="device"></param>
        /// <param name="frac"></param>
        /// <param name="transform"></param>
        /// <param name="color"></param>
        private void BorderGlow(GraphicsDevice device, float frac, Vector4 transform, Vector4 color)
        {
            effect.CurrentTechnique = glowTechnique;

            SetBorderGlowColor(frac);

            if (borderIntensity > 0.0f)
            {
                //float intensity = MyMath.SmoothStep(0.0f, 1.0f, borderIntensity) * color.W;
                float intensity = borderIntensity * color.W;
                Vector4 colorParam = new Vector4(borderColor * intensity, intensity);
                Parameter(EffectParams.BorderGlowColor).SetValue(colorParam);
                Parameter(EffectParams.Glow).SetValue(glow);
                Parameter(EffectParams.Transform).SetValue(transform);

                DrawPrim(device);
            }
        }
        /// <summary>
        /// Figure out what color and intensity the border glow should be,
        /// based on changes to the game's total cost (frac).
        /// </summary>
        /// <param name="frac"></param>
        private void SetBorderGlowColor(float frac)
        {
            float maxIntensity = 1.0f;
            if (lastFrac >= 0)
            {
                if (lastFrac < frac)
                {
                    /// climbing
                    /// color = red
                    borderIntensityGoal = maxIntensity;
                    borderColorGoal = new Vector3(1.0f, 0.0f, 0.0f);
                    state = GlowState.Climbing;
                }
                else if (lastFrac > frac)
                {
                    /// falling
                    /// color = green
                    /// intensity climbs quickly
                    borderIntensityGoal = maxIntensity;
                    borderColorGoal = new Vector3(0.0f, 1.0f, 0.0f);
                    state = GlowState.Climbing;
                }
                lastFrac = frac;
            }
            else
            {
                lastFrac = frac;
                state = GlowState.Idle;
            }
            float climbRate = maxIntensity / 0.2f;
            float fallRate = maxIntensity / 0.55f;
            switch (state)
            {
                case GlowState.Climbing:
                    borderIntensity = InterpToward(borderIntensity, borderIntensityGoal, climbRate);
                    if (borderIntensity == borderIntensityGoal)
                    {
                        state = GlowState.Holding;
                        holdTimer = 0.0f;
                    }
                    break;
                case GlowState.Holding:
                    holdTimer += Time.WallClockFrameSeconds;
                    float holdTime = 0.15f;
                    if (holdTimer >= holdTime)
                    {
                        state = GlowState.Falling;
                        borderIntensityGoal = 0.0f;
                    }
                    break;
                case GlowState.Falling:
                    borderIntensity = InterpToward(borderIntensity, borderIntensityGoal, fallRate);
                    if (borderIntensity == borderIntensityGoal)
                    {
                        state = GlowState.Idle;
                    }
                    break;
                case GlowState.Idle:
                    break;
            };
            float colorRate = 1.0f / 0.1f;
            borderColor = InterpToward(borderColor, borderColorGoal, colorRate);
        }

        /// <summary>
        /// Constant speed interpolation toward a vector3 goal.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="goal"></param>
        /// <param name="rate"></param>
        /// <returns></returns>
        private static Vector3 InterpToward(Vector3 current, Vector3 goal, float rate)
        {
            return new Vector3(
                InterpToward(current.X, goal.X, rate),
                InterpToward(current.Y, goal.Y, rate),
                InterpToward(current.Z, goal.Z, rate));
        }
        /// <summary>
        /// Constant speed interpolation toward a floating point goal.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="goal"></param>
        /// <param name="rate"></param>
        /// <returns></returns>
        private static float InterpToward(float current, float goal, float rate)
        {
            float step = rate * Time.WallClockFrameSeconds;
            if (current > goal)
            {
                current -= step;
                if (current < goal)
                    current = goal;
            }
            else if (current < goal)
            {
                current += step;
                if (current > goal)
                    current = goal;
            }
            return current;
        }
        #endregion BorderGlow

        #region Bookkeeping
        /// <summary>
        /// Load up anything that doesn't require a device.
        /// </summary>
        /// <param name="immediate"></param>
        public static void LoadContent(bool immediate)
        {
            if (effect == null)
            {
                effect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\ShowBudget");
                ShaderGlobals.RegisterEffect("ShowBudget", effect);
                effectCache.Load(effect, "");

                budgetTechnique = effect.Techniques["ShowBudget"];
                glowTechnique = effect.Techniques["BorderGlow"];
            }

            if (background == null)
            {
                background = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\BudgetBackground");
            }
            if (mask == null)
            {
                mask = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\BudgetMask");
            }
            if (glow == null)
            {
                glow = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\BudgetGlow");
            }
        }
        /// <summary>
        /// Load up anything that requires a device.
        /// </summary>
        /// <param name="graphics"></param>
        public static void InitDeviceResources(GraphicsDevice device)
        {
        }

        /// <summary>
        /// Unload everything.
        /// </summary>
        public static void UnloadContent()
        {
            BokuGame.Release(ref background);
            BokuGame.Release(ref mask);
            BokuGame.Release(ref glow);
            BokuGame.Release(ref effect);
        }

        /// <summary>
        /// Recreate render targets.
        /// </summary>
        public static void DeviceReset(GraphicsDevice device)
        {
        }

        #endregion Bookkeeping

        #endregion Internal
    };
};