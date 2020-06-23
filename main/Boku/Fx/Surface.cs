
using System;
using System.Collections.Generic;
using System.Diagnostics;

using System.Xml;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Boku.Fx
{
    public class Surface : BokuShared.XmlData<Surface>, IEquatable<Surface>, IComparable<Surface>
    {
        #region Members
        private string name = null;

        private Vector3 diffuse = Vector3.Zero;
        private Vector3 tintable = Vector3.One;

        private Vector3 emissive = Vector3.Zero;
        private Vector3 tintedEmissive = Vector3.Zero;

        private Vector3 specularColor = Vector3.One;
        private float specularPower = 4.0f;
        private float envmapIntensity = 1.0f;

        private Vector2 aniso = Vector2.One;

        private float bloom = 0.0f;
        private float noise = 0.0f;
        private float bumpIntensity = 0.0f;
        private float bumpScale = 1.0f;

        private float wrap = 1.0f;

        #endregion Members

        #region Accessors
        /// <summary>
        /// The name of this surface
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        /// <summary>
        /// Diffuse color without tint (total_diffuse = diffuse + tintable * Tint)
        /// </summary>
        public Vector3 Diffuse
        {
            get { return diffuse; }
            set { diffuse = value; }
        }
        /// <summary>
        /// Diffuse color with tint (total_diffuse = diffuse + tintable * Tint)
        /// </summary>
        public Vector3 Tintable
        {
            get { return tintable; }
            set { tintable = value; }
        }
        /// <summary>
        /// Emissive color (total_emiss = Emissive + tintedEmissive * Tint)
        /// </summary>
        public Vector3 Emissive
        {
            get { return emissive; }
            set { emissive = value; }
        }
        /// <summary>
        /// The tintable emissive component (total_emiss = Emissive + tintedEmissive * Tint)
        /// </summary>
        public Vector3 TintedEmissive
        {
            get { return tintedEmissive; }
            set { tintedEmissive = value; }
        }
        /// <summary>
        /// Specular tint
        /// </summary>
        public Vector3 SpecularColor
        {
            get { return specularColor; }
            set { specularColor = value; }
        }
        /// <summary>
        /// Power value of specular exponent
        /// </summary>
        public float SpecularPower
        {
            get { return specularPower; }
            set { specularPower = value; }
        }
        /// <summary>
        /// Strength of environment map addition [0..1]
        /// </summary>
        public float EnvMapIntensity
        {
            get { return envmapIntensity; }
            set { envmapIntensity = value; }
        }
        /// <summary>
        /// Anisotropic strengths, (1.0, 1.0) is neutral, bigger is thinner lobe.
        /// </summary>
        public Vector2 Aniso
        {
            get { return aniso; }
            set { aniso = value; }
        }
        /// <summary>
        /// Explicit bloom amount
        /// </summary>
        public float Bloom
        {
            get { return bloom; }
            set { bloom = value; }
        }
        /// <summary>
        /// Strength of any noise/swirl map
        /// </summary>
        public float Noise
        {
            get { return noise; }
            set { noise = value; }
        }
        /// <summary>
        /// Intensity of the bump map (scale the Z component of the bump normal).
        /// </summary>
        public float BumpIntensity
        {
            get { return bumpIntensity; }
            set { bumpIntensity = value; }
        }
        /// <summary>
        /// Tiling of the bump detail map (higher number means more repeating).
        /// </summary>
        public float BumpScale
        {
            get { return bumpScale; }
            set { bumpScale = value; }
        }
        /// <summary>
        /// Amount light wraps around the object, with 1 being full wrap,
        /// and zero being no wrap past terminator.
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
        public Surface()
        {
        }

        #region Sorting Interfaces
        /// <summary>
        /// Compare by name
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(Surface other)
        {
            return Name.CompareTo(other.Name);
        }
        /// <summary>
        /// Compare by name
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Surface other)
        {
            return Name.Equals(other.Name);
        }
        #endregion Sorting Interfaces

        public override string ToString()
        {
            return Name;
        }

        #endregion Public

        #region Internal
        #endregion Internal
    }
}
