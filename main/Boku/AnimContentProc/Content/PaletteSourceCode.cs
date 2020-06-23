/*
 * PaletteSourceCode.cs
 * Copyright (c) 2007 David Astle
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

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

namespace Xclna.Xna.Animation.Content
{
    /// <summary>
    /// Contains the source code for BasicPaletteEffect, but allows instanciation
    /// for variable palette sizes.
    /// </summary>
    public class PaletteSourceCode
    {
        /// <summary>
        /// The size of the matrix palette.
        /// </summary>
        public readonly int PALETTE_SIZE;

        /// <summary>
        /// Creates a new instance of PaletteSourceCode.
        /// </summary>
        /// <param name="size">The size of the matrix palette.</param>
        public PaletteSourceCode(int size)
        {
            PALETTE_SIZE = size;
        }

        private string LightingCode
        {
            get
            {
                return @"


	// For phong shading, the final color of a pixel is equal to 
	// (sum of influence of lights + ambient constant) * texture color at given tex coord
	// First we find the diffuse light, which is simply the dot product of -1*light direction
	// and the normal.  This gives us the component of the reverse light direction in the
	// direction of the normal.  We then multiply the sum of each lights influence by a 
	// diffuse constant.
	// Then we do a similar strategy for specular light; sum the lights then multiply by
	// a specular constant.  In this formula, for each light, we find the dot product between
	// our viewDirection vector and the vector of reflection for the light ray.  This simulates
	// the glare or shinyness that occurs when looking at an object with a reflective surface
	// and when light can bounce of the surface and hit our eyes.
	// We need to be careful with what values we saturate and clamp, otherwise both sides
	// of the object will be lit, or other strange phenomenon will occur.

    // Get the vertex blended normal in world space
    normal = normalize(mul(normal,World));

    // The dot product between the -(light direction) and normal gives us the magnitude
    // of the component of the normal in the direction of the light - in other words,
    // the more parallel the normal is to the light, the brighter the diffuse color.
    // Once we get this, we multiply it by the diffuse light color and sum it up for
    // each light.
    // Finally, we multiply the sum by the diffuse material.
    float3 totalDiffuse = DiffuseColor*
         ((DirLight0Enable ? dot(-DirLight0Direction,normal) * DirLight0DiffuseColor : 0) +
		 (DirLight1Enable ? dot(-DirLight1Direction,normal) * DirLight1DiffuseColor : 0) +
		 (DirLight2Enable ?  dot(-DirLight2Direction,normal) * DirLight2DiffuseColor : 0));


    // This is the vector between the camera and the object in world space, which is used
    // for phong lighting calculation
	float3 viewDirection = normalize(EyePosition - mul(output.position,World));


    // These will store the specular components for each light.
    float3 spec0,spec1,spec2;
    if (DirLight0Enable)
    {
        // The dot product between the - light direction and normal.
        float val = dot(-DirLight0Direction,normal);
        if (val < 0)
        {
            spec0 = float3(0,0,0);
        }
        else
        {
            // Now we find the reflectance vector of the light on the normal, which
            // simulates how the light bounces off of the vertex.  We then find the
            // dot product between this reflect vector and the direction that the
            // camera is facing.  This simulates a light ray bouncing off a shiny surface and
            // into a camera.  We then weight this by the previously computed dot product
            // (val) which determines the degree to which the light reflects off the vertex.
            // Next, we raise this value to a power because by now our value is quite low,
            // and we want to represent the exponential nature of light reflectance.
            // Finally, we multiply the result by the light color.
            spec0 = DirLight0SpecularColor *
                (pow(val*dot(reflect(DirLight0Direction,normal),viewDirection),SpecularPower));
        }
    }
    else
        spec0=float3(0,0,0);

    // Repeat for light1
    if (DirLight1Enable)
    {
        float val = dot(-DirLight1Direction,normal);
        if (val < 0)
        {
            spec1 = float3(0,0,0);
        }
        else
        {
            spec1 = DirLight1SpecularColor *
                (pow(val*dot(reflect(DirLight1Direction,normal),viewDirection),SpecularPower));
        }
    }
    else
        spec1=float3(0,0,0);

    // Repeat for the final light
    if (DirLight2Enable)
    {
        float val = dot(-DirLight2Direction,normal);
        if (val < 0)
        {
            spec2 = float3(0,0,0);
        }
        else
        {
            spec2 = DirLight2SpecularColor *
                (pow(val*dot(reflect(DirLight2Direction,normal),viewDirection),SpecularPower));
        }
    }
    else
        spec2=float3(0,0,0);
    
    // Now we find the total specular by multiplying the specular material by the sum
    // of the lights' specular colors.
	float3 totalSpecular = SpecularColor * (spec0+spec1+spec2);
    // Add the three components together and clamp to (0,1).
	output.color.xyz = saturate(AmbientLightColor+totalDiffuse + totalSpecular);
    output.color.w=1.0;
	output.texcoord = input.texcoord;
    // Find the world location of the postion
    output.position = mul(output.position,World);
    // Pass the distance between the camera and world vertex position to the pixel shader
    output.distance = distance(EyePosition, output.position.xyz);
	// This is the final position of the vertex, and where it will be drawn on the screen
	output.position = mul(output.position,mul(View,Projection));

";

            }
        }
        private string ShaderVariables
        {
            get
            {
                return @"

float4x4 World;
float4x4 View;
float4x4 Projection;
float3 DiffuseColor;
float3 SpecularColor;
float3 AmbientLightColor = float3(0,0,0);
float3 EmissiveColor;
float3 EyePosition;
float3 FogColor;
bool   FogEnable;
float FogStart;
float FogEnd;
bool   DirLight0Enable;
bool   DirLight1Enable;
extern bool    DirLight2Enable;
float3 DirLight0Direction;
float3 DirLight1Direction;
float3 DirLight2Direction;
float3 DirLight0DiffuseColor;
float3 DirLight1DiffuseColor;
float3 DirLight2DiffuseColor;
float3 DirLight0SpecularColor;
float3 DirLight1SpecularColor;
float3 DirLight2SpecularColor;
float Alpha;
float4x4 MatrixPalette[" + this.PALETTE_SIZE.ToString() + @"];
float SpecularPower;
bool TextureEnabled;
bool LightingEnable = false;
texture BasicTexture;

sampler TextureSampler = sampler_state
{
   Texture = (BasicTexture);

};
";
            }

        }
        private string PixelShaderCode
        {
            get
            {
                return @"

void TransformPixel (in PS_INPUT input, out PS_OUTPUT output)
{
    // If lighting is disabled, just weight the texture color by the sum of the emissive and
    // diffuse materials.
	if (LightingEnable == false && TextureEnabled)
    {
		output.color.xyz = tex2D(TextureSampler,input.texcoord).xyz * saturate(EmissiveColor + DiffuseColor);
    }
    // Same as above, except no texture
    else if (LightingEnable == false)
    {
       output.color.xyz = saturate(EmissiveColor + DiffuseColor);
    }
	else
	{
		


		output.color.xyz = TextureEnabled ? tex2D(TextureSampler, input.texcoord).xyz  * input.color.xyz
            : input.color.xyz;

	}
    output.color.w   = 
         TextureEnabled ? tex2D(TextureSampler, input.texcoord).w * Alpha : Alpha;
    

    if (FogEnable)
    {
        // Linear fog works by interpolating between the final lighting color and the
        // fog color.
        // If the distance from the camera to the vertex is less then FogStart,
        // then fog has no affect.
        // If the distance is greater than FogEnd, then the vertex color is set to
        // the fog color.
        // If it lies in between, we interpolate from the shaded vertex color to
        // the fog color as a function of how close it is to the FogEnd.
                
        float dist = (input.distance - FogStart) / (FogEnd - FogStart);
        dist = saturate(dist);
        float3 distv = float3(dist,dist,dist);
        distv = lerp(output.color.xyz,FogColor,distv);
        output.color.xyz = distv;
    }
}
";
            }
        }


        /// <summary>
        /// Returns the source code for BasicPaletteEffect for a max of
        /// 4 influences per vertex.
        /// </summary>
        public string SourceCode4BonesPerVertex
        {
            get
            {
                return ShaderVariables + @"

// This is passed into our vertex shader from Xna
struct VS_INPUT
{
	float4 position : POSITION;
	float4 color : COLOR;
	float2 texcoord : TEXCOORD0;
	float3 normal : NORMAL0;
	half4 indices : BLENDINDICES0;
	float4 weights : BLENDWEIGHT0;
};

// This is passed out from our vertex shader once we have processed the input
struct VS_OUTPUT
{
	float4 position : POSITION;
	float4 color : COLOR;
	float2 texcoord : TEXCOORD0;
    float  distance : TEXCOORD1;
};

// This is passed into our pixel shader from our vertex shader
struct PS_INPUT
{
    float4 color : COLOR;
    float2 texcoord : TEXCOORD0;
    float  distance : TEXCOORD1;
};

// This is passed from our pixel shader and is the color rendered on the screen
// at the pixel.
struct PS_OUTPUT
{
	float4 color : COLOR;
};

// This is the output from our skinning method
struct SKIN_OUTPUT
{
    float4 position;
    float4 normal;
};

// This calculates the skinned vertex and normal position based on the blend indices
// and weights.
// For four indices and four weights, which is what this shader uses,
// the formula for position vertex Vi, weight array W, index array I, and matrix array M is:
// Vf = Vi*W[0]*M[I[0]] + Vi*W[1]*M[I[1]] + Vi*W[2]*M[I[2]] + Vi*W[3]*M[I[3]]
// In fact, the weights may not always add up to 1,
// so we replace the last weight with:
// W[3] = (1 - W[2] - W[1] - W[0])
// The formula is the same for calculating the skinned normal position.
SKIN_OUTPUT Skin4( const VS_INPUT input)
{
    SKIN_OUTPUT output = (SKIN_OUTPUT)0;

    float lastWeight = 1.0;
    float weight = 0;
    for (int i = 0; i < 3; ++i)
    {
        weight = input.weights[i];
        lastWeight -= weight;
        output.position     += mul( input.position, MatrixPalette[input.indices[i]]) * weight;
        output.normal       += mul( input.normal  , MatrixPalette[input.indices[i]]) * weight;
    }
    output.position     += mul( input.position, MatrixPalette[input.indices[3]])*lastWeight;
    output.normal       += mul( input.normal  , MatrixPalette[input.indices[3]])*lastWeight;
    return output;
};

void TransformVertex (in VS_INPUT input, out VS_OUTPUT output)
{

    float3 inputN = normalize(input.normal);
    // Calculate the skinned normal and position
    SKIN_OUTPUT skin = Skin4(input);
    output.position=skin.position;
    float3 normal = skin.normal;



    " + LightingCode + @"
}

" + PixelShaderCode + @"



technique TransformTechnique
{
	pass P0
	{
		VertexShader = compile vs_2_0 TransformVertex();
		PixelShader  = compile ps_2_0 TransformPixel();
	}
}";
            }
        }
    }
}