/*
 * BasicPaletteEffect.cs
 * Copyright (c) 2006 David Astle
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be included
 * in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
#endregion

namespace Xclna.Xna.Animation
{

    /// <summary>
    /// Provides functionality similar to that of BasicEffect, but uses a 
    /// Matrix Palette.
    /// </summary>
    public sealed class BasicPaletteEffect : Effect
    {
        // All the effect parameters
        private EffectParameter worldParam, viewParam, projectionParam,
            ambientParam, eyeParam, emissiveParam, diffuseParam, lightEnabledParam,
            alphaParam,
            specColorParam, specPowerParam, texEnabledParam, texParam, paletteParam,
            fogEnabledParam, fogStartParam, fogEndParam, fogColorParam;
        private BasicDirectionalLight light0, light1, light2;
        // The location of the camera
        private Vector3 eye;
        // Used to help determine the eye position
        private static Vector3 zero = Vector3.Zero;


        /// <summary>
        /// The max number of bones in the effect's matrix palette.
        /// </summary>
        public readonly int PALETTE_SIZE;


        internal BasicPaletteEffect(GraphicsDevice device, byte[] byteCode,
            int paletteSize)
            : base(device, byteCode, CompilerOptions.PreferFlowControl, null)
        {
            this.PALETTE_SIZE = paletteSize;
            InitializeParameters();
        }

        internal BasicPaletteEffect(GraphicsDevice device, Effect cloneSource)
            : base(device, cloneSource)
        {
            this.PALETTE_SIZE = ((BasicPaletteEffect)cloneSource).PALETTE_SIZE;
            InitializeParameters();
        }

        /// <summary>
        /// Enables the default lighting for this effect.
        /// </summary>
        public void EnableDefaultLighting()
        {
            this.LightingEnabled = true;

            this.light0.DiffuseColor = Color.White.ToVector3();
            this.light0.SpecularColor = Color.Black.ToVector3();
            this.light0.Direction = Vector3.Normalize(new Vector3(-1, 0, -1));
            this.light0.SpecularColor = Color.White.ToVector3();
            this.light1.DiffuseColor = Color.Black.ToVector3();
            this.light1.SpecularColor = Color.Black.ToVector3();
            this.light2.DiffuseColor = Color.Black.ToVector3();
            this.light2.SpecularColor = Color.Black.ToVector3();
            this.SpecularPower = 8.0f;
            this.light0.Enabled = true;
            this.light1.Enabled = false;
            this.light2.Enabled = false;
        }

        private void InitializeParameters()
        {
            paletteParam = Parameters["MatrixPalette"];
            texParam = Parameters["BasicTexture"];
            texEnabledParam = Parameters["TextureEnabled"];
            worldParam = Parameters["World"];
            viewParam = Parameters["View"];
            projectionParam = Parameters["Projection"];
            ambientParam = Parameters["AmbientLightColor"];
            eyeParam = Parameters["EyePosition"];
            emissiveParam = Parameters["EmissiveColor"];
            lightEnabledParam = Parameters["LightingEnable"];
            diffuseParam = Parameters["DiffuseColor"];
            specColorParam = Parameters["SpecularColor"];
            specPowerParam = Parameters["SpecularPower"];
            alphaParam = Parameters["Alpha"];
            fogColorParam = Parameters["FogColor"];
            fogEnabledParam = Parameters["FogEnable"];
            fogStartParam = Parameters["FogStart"];
            fogEndParam = Parameters["FogEnd"];
            light0 = new BasicDirectionalLight(this, 0);
            light1 = new BasicDirectionalLight(this, 1);
            light2 = new BasicDirectionalLight(this, 2);

            FogColor = Vector3.Zero;
            FogStart = 0;
            FogEnd = 1;
            FogEnabled = false;
        }



        /// <summary>
        /// Clones the current BasicPaletteEffect class.
        /// </summary>
        /// <param name="device">The device to contain the new instance.</param>
        /// <returns>A clone of the current instance.</returns>
        public override Effect Clone(GraphicsDevice device)
        {
            return new BasicPaletteEffect(device, this);
        }


        /// <summary>
        /// Sets the parameters of this effect from a BasicEffect instance.
        /// </summary>
        /// <param name="effect">An instance containing the parameters to be copied.</param>
        public void SetParamsFromBasicEffect(BasicEffect effect)
        {
            AmbientLightColor = effect.AmbientLightColor;
            DiffuseColor = effect.DiffuseColor;
            LightingEnabled = effect.LightingEnabled;
            Projection = effect.Projection;
            World = effect.World;
            View = effect.View;
            SpecularColor = effect.SpecularColor;
            EmissiveColor = effect.EmissiveColor;
            SpecularPower = effect.SpecularPower;
            Alpha = effect.Alpha;
            this.Texture = effect.Texture;
            this.TextureEnabled = effect.TextureEnabled;
            SetParamsFromBasicLight(effect.DirectionalLight0, light0);
            SetParamsFromBasicLight(effect.DirectionalLight1, light1);
            SetParamsFromBasicLight(effect.DirectionalLight2, light2);
        }

        private void SetParamsFromBasicLight(Microsoft.Xna.Framework.Graphics.BasicDirectionalLight
            source,
            BasicPaletteEffect.BasicDirectionalLight target)
        {
            target.SpecularColor = source.SpecularColor;
            target.Enabled = source.Enabled;
            target.Direction = source.Direction;
            target.DiffuseColor = source.DiffuseColor;
        }

        /// <summary>
        /// A basic directional light that uses phong shading.
        /// </summary>
        public sealed class BasicDirectionalLight
        {
            private BasicPaletteEffect effect;
            private EffectParameter lightDirParam;
            private EffectParameter difColorParam;
            private EffectParameter lightEnabledParam;
            private EffectParameter specColorParam;

            internal BasicDirectionalLight(BasicPaletteEffect effect, int lightNum)
            {
                this.effect = effect;
                string lightString = "DirLight" + lightNum;
                this.lightDirParam = effect.Parameters[lightString + "Direction"];
                this.difColorParam = effect.Parameters[lightString + "DiffuseColor"];
                this.specColorParam = effect.Parameters[lightString + "SpecularColor"];
                this.lightEnabledParam = effect.Parameters[lightString + "Enable"];
            }

            /// <summary>
            /// Enables or disables this light.
            /// </summary>
            public bool Enabled
            {
                get
                {
                    return lightEnabledParam.GetValueBoolean();
                }
                set
                {
                    lightEnabledParam.SetValue(value);
                }
            }

            /// <summary>
            /// Gets or sets the direction of this light.
            /// </summary>
            public Vector3 Direction
            {
                get
                {
                    return lightDirParam.GetValueVector3();
                }
                set
                {
                    lightDirParam.SetValue(Vector3.Normalize(value));
                }
            }




            /// <summary>
            /// Gets or sets the specular color of this light.
            /// </summary>
            public Vector3 SpecularColor
            {
                get
                {
                    return specColorParam.GetValueVector3();
                }
                set
                {

                    specColorParam.SetValue(value);
                }
            }

            /// <summary>
            /// Gets or sets the diffuse color of this light.
            /// </summary>
            public Vector3 DiffuseColor
            {
                get
                {
                    return difColorParam.GetValueVector3();
                }
                set
                {
                    difColorParam.SetValue(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the texture associated with this effect.
        /// </summary>
        public Texture2D Texture
        {
            get
            {
                return texParam.GetValueTexture2D();
            }
            set
            {
                texParam.SetValue(value);
            }
        }

        /// <summary>
        /// Gets or sets the vertex fog start distance.
        /// </summary>
        public float FogStart
        {
            get
            {
                return fogStartParam.GetValueSingle();
            }
            set
            {
                fogStartParam.SetValue(value);
            }
        }

        /// <summary>
        /// Gets or sets the vertex fog end distance.
        /// </summary>
        public float FogEnd
        {
            get
            {
                return fogEndParam.GetValueSingle();
            }
            set
            {
            
                fogEndParam.SetValue(value);
            }
        }

        /// <summary>
        /// Gets or sets a value enabling the fog. 
        /// </summary>
        public bool FogEnabled
        {
            get
            {
                return fogEnabledParam.GetValueBoolean();
            }
            set
            {
                fogEnabledParam.SetValue(value); 
            }
        }

        /// <summary>
        /// Gets or sets the fog color.
        /// </summary>
        public Vector3 FogColor
        {
            get
            {
                return fogColorParam.GetValueVector3();
            }
            set
            {
                fogColorParam.SetValue(value);
            }
        }

        /// <summary>
        /// Gets or sets the alhpa blending value.
        /// </summary>
        public float Alpha
        {
            get
            {
                return alphaParam.GetValueSingle();
            }
            set
            {
                alphaParam.SetValue(value);
            }
        }

        /// <summary>
        /// Gets or sets a value enabling the textures.
        /// </summary>
        public bool TextureEnabled
        {
            get
            {
                return texEnabledParam.GetValueBoolean();
            }
            set
            {
                texEnabledParam.SetValue(value);
            }
        }

        /// <summary>
        /// Gets or sets the bone palette values.
        /// </summary>
        public Matrix[] MatrixPalette
        {
            get
            {
                return paletteParam.GetValueMatrixArray(PALETTE_SIZE);
            }
            set
            {
                paletteParam.SetValue(value);
            }

        }


        /// <summary>
        /// Gets directional light zero.
        /// </summary>
        public BasicDirectionalLight DirectionalLight0
        {
            get
            {
                return light0;
            }
        }

        /// <summary>
        /// Gets directional light one.
        /// </summary>
        public BasicDirectionalLight DirectionalLight1
        {
            get
            {
                return light1;
            }
        }

        /// <summary>
        /// Gets directional light two.
        /// </summary>
        public BasicDirectionalLight DirectionalLight2
        {
            get
            {
                return light2;
            }
        }

        /// <summary>
        /// Gets or sets the additive ambient color of this effect.
        /// </summary>
        public Vector3 AmbientLightColor
        {
            get
            {
                return ambientParam.GetValueVector3();
            }
            set
            {
                ambientParam.SetValue(value);
            }

        }

        /// <summary>
        /// Gets or sets the specular color of this effect.
        /// </summary>
        public Vector3 SpecularColor
        {
            get
            {
                return specColorParam.GetValueVector3();
            }
            set
            {
                specColorParam.SetValue(value);
            }
        }

        /// <summary>
        /// Gets or sets the specular power of this effect.
        /// </summary>
        public float SpecularPower
        {
            get
            {
                return specPowerParam.GetValueSingle();
            }
            set
            {
                specPowerParam.SetValue(value);
            }
        }

        /// <summary>
        /// Gets or sets the diffuse color of this effect.
        /// </summary>
        public Vector3 DiffuseColor
        {
            get
            {
                return diffuseParam.GetValueVector3();
            }
            set
            {
                diffuseParam.SetValue(value);
            }
        }

        /// <summary>
        /// Enables or disables lighting.
        /// </summary>
        public bool LightingEnabled
        {
            get
            {
                return lightEnabledParam.GetValueBoolean();
            }
            set
            {
                lightEnabledParam.SetValue(value);
            }
        }

        /// <summary>
        /// Gets or sets the emissive color of this effect.
        /// </summary>
        public Vector3 EmissiveColor
        {
            get
            {
                return emissiveParam.GetValueVector3();
            }
            set
            {
                emissiveParam.SetValue(value);
            }
        }

        /// <summary>
        /// Gets or sets the world matrix of this effect.
        /// </summary>
        public Matrix World
        {
            get
            {
                return worldParam.GetValueMatrix();
            }
            set
            {
                worldParam.SetValue(value);
            }
        }


        /// <summary>
        /// Gets or sets the view matrix of this effect.
        /// </summary>
        public Matrix View
        {
            get
            {
                return viewParam.GetValueMatrix();
            }
            set
            {
                Matrix inverseView = Matrix.Invert(value);
                Vector3.Transform(ref zero, ref inverseView, out eye);
                viewParam.SetValue(value);
                eyeParam.SetValue(eye);
            }
        }

        /// <summary>
        /// Gets or sets the projection matrix of this effect.
        /// </summary>
        public Matrix Projection
        {
            get
            {
                return projectionParam.GetValueMatrix();
            }
            set
            {
                projectionParam.SetValue(value);
            }
        }

    }
}