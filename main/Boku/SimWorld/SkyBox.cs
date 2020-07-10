// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
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
using Boku.Fx;
using Boku.SimWorld.Terra;

namespace Boku.SimWorld
{
    /// <summary>
    /// The SkyBox is implemented as a filter which just renders a full screen quad.
    /// </summary>
    public class SkyBox : BaseFilter
    {
        #region Members
        /// <summary>
        /// Effective radius of the sky dome.
        /// </summary>
        private static float radius = 0.0f;
        /// <summary>
        /// Effective center of the sky dome.
        /// </summary>
        private static Vector3 center = Vector3.Zero;

        private static Vector4[] currentGradient = new Vector4[5];
        private static Vector4[] sourceGradientFrom = new Vector4[5];

        private static List<Vector4[]> gradients = new List<Vector4[]>();
        #endregion Members

        #region Accessors
        /// <summary>
        /// Effective radius of the sky dome.
        /// </summary>
        public static float Radius
        {
            get { return radius; }
            private set { radius = value; }
        }
        /// <summary>
        /// Effective center of the sky dome.
        /// </summary>
        public static Vector3 Center
        {
            get { return center; }
            private set { center = value; }
        }
        #endregion Accessors

        #region Public
        // c'tor
        public SkyBox()
            :
            base()
        {
        }   // end of SkyBox c'tor

        public void Render(Camera camera, bool effects)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            MoveTo();

            SetUvToPos();

            Vector3 viewDir;
            Vector3 upDir;
            Vector3 rightDir;

            viewDir = camera.ViewDir;
            rightDir = Vector3.Cross(viewDir, camera.Up);
            upDir = Vector3.Cross(rightDir, viewDir);

            viewDir.Normalize();
            rightDir.Normalize();
            upDir.Normalize();

            // Need to scale the up and the right vectors to match the current FOV angle.
            float scale = MathHelper.ToDegrees(camera.Fov) / 90.0f * 1.5f;
            rightDir *= scale;
            upDir *= scale / camera.AspectRatio;

            effect.Parameters["ViewDir"].SetValue(viewDir);
            effect.Parameters["UpDir"].SetValue(upDir);
            effect.Parameters["RightDir"].SetValue(rightDir);
            effect.Parameters["Eye"].SetValue(camera.ActualFrom);

            effect.Parameters["DomeCenter"].SetValue(Center);
            effect.Parameters["DomeRadius"].SetValue(Radius);

            if (Terrain.Current.SkyTransitioning)
            {
                int skyToIdx = Terrain.Current.TransitionToSkyIndex;

                Vector4[] gradientTo = Gradient(skyToIdx);

                float lerpAmount = Terrain.Current.TransitionSkyAmount;

                for (int i = 0; i < 5; ++i)
                {
                    currentGradient[i] = sourceGradientFrom[i] * (1.0f - lerpAmount) + gradientTo[i] * lerpAmount;
                }

                //apply a lerped gradient
                effect.Parameters["Color0"].SetValue(currentGradient[0]);
                effect.Parameters["Color1"].SetValue(currentGradient[1]);
                effect.Parameters["Color2"].SetValue(currentGradient[2]);
                effect.Parameters["Color3"].SetValue(currentGradient[3]);
                effect.Parameters["Color4"].SetValue(currentGradient[4]);
            }
            else
            {
                int skyIdx = Terrain.Current.RunTimeSkyIndex;
                Vector4[] gradient = Gradient(skyIdx);

                for (int i = 0; i < 5; ++i)
                {
                    currentGradient[i] = gradient[i];
                }

                effect.Parameters["Color0"].SetValue(currentGradient[0]);
                effect.Parameters["Color1"].SetValue(currentGradient[1]);
                effect.Parameters["Color2"].SetValue(currentGradient[2]);
                effect.Parameters["Color3"].SetValue(currentGradient[3]);
                effect.Parameters["Color4"].SetValue(currentGradient[4]);
            }

            if (effects)
            {
                effect.CurrentTechnique = effect.Techniques["SkyBoxEffects"];
            }
            else
            {
                effect.CurrentTechnique = effect.Techniques["SkyBox"];
            }

            device.SetVertexBuffer(vbuf);

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            }

        }   // end of SkyBox Render()

        public override void LoadContent(bool immediate)
        {
            // Init the effect.
            if (effect == null)
            {
                effect = KoiLibrary.LoadEffect(@"Shaders\SkyBox");
                ShaderGlobals.RegisterEffect("SkyBox", effect);
            }

            base.LoadContent(immediate);
        }   // end of SkyBox LoadContent()

        /// <summary>
        /// Caches the current gradient for transitioning to a new value
        /// </summary>
        public static void InitiateTransition()
        {
            for (int i = 0; i < 5; ++i)
            {
                sourceGradientFrom[i] = currentGradient[i];
            }
        }

        /// <summary>
        /// Initialize to a set of world bounds, so we don't have to interpolate to them after load.
        /// </summary>
        /// <param name="bounds"></param>
        public static void Setup(AABB bounds)
        {
            Vector2 min2d = new Vector2(bounds.Min.X, bounds.Min.Y);
            Vector2 max2d = new Vector2(bounds.Max.X, bounds.Max.Y);
            Radius = 2.0f * Vector2.Distance(min2d, max2d);
            Vector3 min = bounds.Min;
            Vector3 max = bounds.Max;
            Vector3 Center = (min + max) / 2.0f;
            center.Z = 0;
        }

        /// <summary>
        /// Search for the best match within the gradient table for the oldGradient.
        /// </summary>
        /// <param name="oldGradient"></param>
        /// <returns></returns>
        public static int Find(Vector4[] oldGradient)
        {
            CheckInit();
            Debug.Assert(gradients.Count > 0);
            float bestDist = Compare(oldGradient, gradients[0]);
            int bestIdx = 0;
            for (int i = 1; i < gradients.Count; ++i)
            {
                float dist = Compare(oldGradient, gradients[i]);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIdx = i;
                }
            }
            return bestIdx;
        }

        /// <summary>
        /// Return gradient corresponding to given index.
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public static Vector4[] Gradient(int idx)
        {
            CheckInit();
            return gradients[idx];
        }
        #endregion Public

        #region Internal

        /// <summary>
        /// Make sure the gradient table is initialized.
        /// </summary>
        private static void CheckInit()
        {
            if (gradients.Count == 0)
            {
                Init();
            }
        }
        /// <summary>
        /// Add a gradient to the gradient table.
        /// </summary>
        /// <param name="gradient"></param>
        private static void AddGradient(Vector4[] gradient)
        {
            Debug.Assert(gradient.Length == 6);
            gradients.Add(gradient);
        }

        /// <summary>
        /// Build the gradient table.
        /// </summary>
        private static void Init()
        {
            Debug.Assert(gradients.Count == 0);

            #region Originals
            // 0 - Sunny day
            Vector4[] gradient = new Vector4[6];
            gradient[0] = new Vector4(0.15f, 0.3f, 0.2f, 0.15f);
            gradient[1] = new Vector4(0.3f, 0.7f, 0.4f, 0.45f);
            gradient[2] = new Vector4(0.6f, 0.8f, 0.7f, 0.5f);
            gradient[3] = new Vector4(0.2f, 0.7f, 1.0f, 0.51f);
            gradient[4] = new Vector4(0.2f, 0.35f, 0.6f, 0.8f);
            gradient[5] = Vector4.One; // psys
            AddGradient(gradient);

            // 1 - All black.  Space.
            gradient = new Vector4[6];
            gradient[0] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);  // black
            gradient[1] = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);  // black
            gradient[2] = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);  // ignored
            gradient[3] = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);  // ignored
            gradient[4] = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);  // ignored
            gradient[5] = new Vector4(0.0f, 0.0f, 0.0f, 1.0f); // psys
            AddGradient(gradient);

            // 2 - Simple black to white ramp.
            gradient = new Vector4[6];
            gradient[0] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);  // black
            gradient[1] = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);  // white
            gradient[2] = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);  // ignored
            gradient[3] = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);  // ignored
            gradient[4] = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);  // ignored
            gradient[5] = new Vector4(0.8f, 0.8f, 0.8f, 1.0f); // psys
            AddGradient(gradient);

            // 3 - Purple to Pink
            gradient = new Vector4[6];
            gradient[0] = new Vector4(0.5f, 0.5f, 0.8f, 0.0f);      // purple
            gradient[1] = new Vector4(0.6f, 0.0f, 0.9f, 0.47f);     // other purple
            gradient[2] = new Vector4(1.0f, 1.0f, 1.0f, 0.5f);      // white 
            gradient[3] = new Vector4(0.96f, 0.80f, 0.95f, 0.6f);   // pink
            gradient[4] = new Vector4(0.96f, 0.60f, 0.80f, 0.9f);   // pink, slightly darker
            gradient[5] = new Vector4(1.0f, 0.9f, 0.9f, 1.0f); // psys
            AddGradient(gradient);

            // 4 - Xbox green to black.
            gradient = new Vector4[6];
            gradient[0] = new Vector4(0.4f, 1.0f, 0.1f, 0.0f);  // bright green
            gradient[1] = new Vector4(0.0f, 0.0f, 0.0f, 0.7f);  // black
            gradient[2] = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);  // ignored
            gradient[3] = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);  // ignored
            gradient[4] = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);  // ignored
            gradient[5] = new Vector4(0.7f, 0.9f, 0.7f, 1.0f); // psys
            AddGradient(gradient);

            // 5 - Sunset.
            gradient = new Vector4[6];
            gradient[0] = new Vector4(0.0f, 0.0f, 0.0f, 0.49f); // black
            gradient[1] = new Vector4(1.0f, 0.8f, 0.0f, 0.50f); // yellow
            gradient[2] = new Vector4(0.6f, 0.0f, 0.0f, 0.52f); // dark red
            gradient[3] = new Vector4(0.0f, 0.0f, 0.0f, 0.6f);  // black 
            gradient[4] = new Vector4(0.0f, 0.0f, 0.0f, 0.6f);  // ignored
            gradient[5] = new Vector4(1.0f, 0.95f, 0.8f, 1.0f); // psys
            AddGradient(gradient);

            // 6 - Mars?
            gradient = new Vector4[6];
            gradient[0] = new Vector4(0.2f, 0.1f, 0.05f, 0.0f);     // almost black
            gradient[1] = new Vector4(0.75f, 0.35f, 0.16f, 0.2f);   // red sand
            gradient[2] = new Vector4(0.83f, 0.62f, 0.43f, 0.5f);   // land at horizon
            gradient[3] = new Vector4(0.96f, 0.89f, 0.76f, 0.501f); // low sky
            gradient[4] = new Vector4(0.57f, 0.44f, 0.34f, 1.0f);   // high sky
            gradient[5] = new Vector4(1.0f, 0.85f, 0.6f, 1.0f); // psys
            AddGradient(gradient);

            // 7 - I've got the blues.
            gradient = new Vector4[6];
            gradient[0] = new Vector4(0.40f, 0.76f, 1.00f, 0.0f);   // light, hazy blue
            gradient[1] = new Vector4(0.27f, 0.62f, 1.00f, 0.5f);   // middle blue
            gradient[2] = new Vector4(0.05f, 0.26f, 0.67f, 1.0f);   // dark blue
            gradient[3] = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);      // ignored
            gradient[4] = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);      // ignored
            gradient[5] = new Vector4(0.9f, 0.9f, 1.0f, 1.0f); // psys
            AddGradient(gradient);

            // 8 - Mars 2
            gradient = new Vector4[6];
            gradient[0] = IntColor(67, 126, 53, 0);         // low glowing green
            gradient[1] = IntColor(86, 38, 69, 126);        // light red sand + purple
            gradient[2] = IntColor(108, 47, 47, 132);      // light red sand
            gradient[3] = IntColor(55, 0, 0, 200);          // dark red
            gradient[4] = IntColor(0, 0, 0, 255);              // black straight up
            gradient[5] = new Vector4(1.0f, 0.8f, 0.7f, 1.0f); // psys
            AddGradient(gradient);

            // 9 - Twilight
            gradient = new Vector4[6];
            gradient[0] = new Vector4(0.0f, 0.0f, 0.1f, 0.4f); // black
            gradient[1] = new Vector4(0.4f, 0.5f, 0.8f, 0.5f); // blue
            gradient[2] = new Vector4(0.9f, 0.95f, 1.0f, 0.6f); // white
            gradient[3] = new Vector4(0.4f, 0.5f, 0.8f, 0.65f);  // blue
            gradient[4] = new Vector4(0.0f, 0.0f, 0.1f, 0.7f);  // black
            gradient[5] = new Vector4(0.6f, 0.7f, 0.85f, 1.0f); // psys
            AddGradient(gradient);

            #endregion Originals

            #region Brian's
            // 10 - G1
            gradient = new Vector4[6];
            gradient[0] = GradientStop(234, 247, 224, 0);
            gradient[1] = GradientStop(137, 241, 191, 30);
            gradient[2] = GradientStop(059, 213, 234, 66);
            gradient[3] = GradientStop(007, 157, 185, 100);
            gradient[4] = GradientStop(0, 0, 0, 100);
            gradient[5] = new Vector4(0.9f, 1.0f, 1.0f, 1.0f); // psys
            AddGradient(gradient);

            // 11 - G2
            gradient = new Vector4[6];
            gradient[0] = GradientStop(234, 174, 255, 0);
            gradient[1] = GradientStop(255, 255, 248, 41);
            gradient[2] = GradientStop(155, 151, 228, 77);
            gradient[3] = GradientStop(095, 094, 158, 100);
            gradient[4] = GradientStop(0, 0, 0, 100);
            gradient[5] = Vector4.One; // psys
            AddGradient(gradient);

            // 12 - G3
            gradient = new Vector4[6];
            gradient[0] = GradientStop(245, 221, 240, 0);
            gradient[1] = GradientStop(243, 243, 235, 39);
            gradient[2] = GradientStop(184, 190, 195, 75);
            gradient[3] = GradientStop(084, 104, 113, 100);
            gradient[4] = GradientStop(0, 0, 0, 100);
            gradient[5] = Vector4.One; // psys
            AddGradient(gradient);

            // 13 - G4
            gradient = new Vector4[6];
            gradient[0] = GradientStop(241, 205, 253, 0);
            gradient[1] = GradientStop(253, 253, 246, 37);
            gradient[2] = GradientStop(152, 148, 227, 77);
            gradient[3] = GradientStop(095, 094, 158, 100);
            gradient[4] = GradientStop(0, 0, 0, 100);
            gradient[5] = Vector4.One; // psys
            AddGradient(gradient);

            // 14 - G5
            gradient = new Vector4[6];
            gradient[0] = GradientStop(234, 247, 225, 0);
            gradient[1] = GradientStop(147, 243, 190, 24);
            gradient[2] = GradientStop(065, 217, 235, 60);
            gradient[3] = GradientStop(007, 158, 185, 100);
            gradient[4] = GradientStop(0, 0, 0, 100);
            gradient[5] = new Vector4(0.9f, 1.0f, 1.0f, 1.0f); // psys
            AddGradient(gradient);

            // 15 - G6
            gradient = new Vector4[6];
            gradient[0] = GradientStop(116, 117, 116, 0);
            gradient[1] = GradientStop(147, 243, 190, 24);
            gradient[2] = GradientStop(078, 137, 144, 61);
            gradient[3] = GradientStop(054, 047, 073, 100);
            gradient[4] = GradientStop(0, 0, 0, 100);
            gradient[5] = new Vector4(0.8f, 0.9f, 0.9f, 1.0f); // psys
            AddGradient(gradient);

            // 16 - G7
            gradient = new Vector4[6];
            gradient[0] = GradientStop(254, 255, 230, 0);
            gradient[1] = GradientStop(194, 238, 232, 24);
            gradient[2] = GradientStop(058, 169, 255, 61);
            gradient[3] = GradientStop(047, 084, 204, 100);
            gradient[4] = GradientStop(0, 0, 0, 100);
            gradient[5] = Vector4.One; // psys
            AddGradient(gradient);

            // 17 - G8
            gradient = new Vector4[6];
            gradient[0] = GradientStop(177, 237, 208, 13);
            gradient[1] = GradientStop(252, 251, 229, 30);
            gradient[2] = GradientStop(014, 131, 248, 53);
            gradient[3] = GradientStop(04021, 052, 111, 77);
            gradient[4] = GradientStop(021, 052, 111, 100);
            gradient[5] = Vector4.One; // psys
            AddGradient(gradient);

            // 18 - G9
            gradient = new Vector4[6];
            gradient[0] = GradientStop(000, 000, 000, 0);
            gradient[1] = GradientStop(035, 040, 073, 15);
            gradient[2] = GradientStop(249, 135, 116, 24);
            gradient[3] = GradientStop(138, 102, 160, 47);
            gradient[4] = GradientStop(108, 176, 233, 71);
            gradient[5] = new Vector4(0.8f, 0.75f, 0.8f, 1.0f); // psys
            AddGradient(gradient);

            // 19 - G10
            gradient = new Vector4[6];
            gradient[0] = GradientStop(000, 000, 000, 9);
            gradient[1] = GradientStop(249, 246, 165, 13);
            gradient[2] = GradientStop(241, 176, 075, 38);
            gradient[3] = GradientStop(233, 100, 100, 55);
            gradient[4] = GradientStop(051, 074, 099, 100);
            gradient[5] = new Vector4(1.0f, 1.0f, 0.8f, 1.0f); // psys
            AddGradient(gradient);

            // 20 - G11
            gradient = new Vector4[6];
            gradient[0] = GradientStop(081, 255, 171, 15);
            gradient[1] = GradientStop(243, 255, 205, 32);
            gradient[2] = GradientStop(235, 255, 172, 38);
            gradient[3] = GradientStop(253, 175, 175, 54);
            gradient[4] = GradientStop(000, 148, 146, 74);
            gradient[5] = new Vector4(1.0f, 1.0f, 0.9f, 1.0f); // psys
            AddGradient(gradient);

            #endregion Brian's
        }


        private static float Compare(Vector4[] oldGradient, Vector4[] gradient)
        {
            Debug.Assert(oldGradient.Length <= gradient.Length);
            float distSq = 0;
            for (int i = 0; i < oldGradient.Length; ++i)
            {
                Vector4 diff = oldGradient[i] - gradient[i];
                distSq += diff.LengthSquared();
            }
            return distSq;
        }

        /// <summary>
        /// Interpolate toward the desired parameters for world size.
        /// </summary>
        private void MoveTo()
        {
            float dt = Time.WallClockFrameSeconds;

            AABB bounds = InGame.TotalBounds;

            Vector3 min = bounds.Min;
            Vector3 max = bounds.Max;
            if (max.Z < Terrain.MaxWaterHeight)
                max.Z = Terrain.MaxWaterHeight;

            float radiusStep = 300.0f * dt;
            float desiredRadius = 2.0f * Vector3.Distance(min, max);

            float kMinRadius = 50.0f;
            if (desiredRadius < kMinRadius)
                desiredRadius = kMinRadius;

            radius = MoveTo(radius, desiredRadius, radiusStep);

            float centerStep = 300.0f * dt;

            Vector3 desiredCenter = (min + max) / 2.0f;

            center.X = MoveTo(center.X, desiredCenter.X, centerStep);
            center.Y = MoveTo(center.Y, desiredCenter.Y, centerStep);
            center.Z = 0;
        }

        /// <summary>
        /// Interpolate from a float value toward another at constant rate.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="del"></param>
        /// <returns></returns>
        private float MoveTo(float from, float to, float del)
        {
            if (from < to)
            {
                from += del;
                if (from > to)
                    from = to;
            }
            else if (from > to)
            {
                from -= del;
                if (from < to)
                    from = to;
            }
            return from;
        }

        /// <summary>
        /// Build a gradient from [0..255] ints.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        private static Vector4 IntColor(int r, int g, int b, int a)
        {
            float rF = (float)r;
            float gF = (float)g;
            float bF = (float)b;
            float aF = (float)a;

            return new Vector4(rF / 255.0f, gF / 255.0f, bF / 255.0f, aF / 255.0f);
        }

        /// <summary>
        /// more syntactic sugar to make it easier to copy gradients from photoshop
        /// rgb are all 0..255; a is 0..100
        /// </summary>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        private static Vector4 GradientStop(int r, int g, int b, int a)
        {
            float rF = (float)r;
            float gF = (float)g;
            float bF = (float)b;
            float aF = (float)a;

            return new Vector4(rF / 255.0f, gF / 255.0f, bF / 255.0f, aF / 100.0f);
        }


        #endregion Internal

    }   // end of class SkyBox

}   // end of Boku.SimWorld


