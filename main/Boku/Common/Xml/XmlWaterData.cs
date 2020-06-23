using System;

using Microsoft.Xna.Framework;

namespace Boku.Common.Xml
{
    public class XmlWaterData : BokuShared.XmlData<XmlWaterData>
    {
        /// <summary>
        /// This data is all obsolete. The only part still used is the name field
        /// for lookup into the water dictionary. And the seed position.
        /// </summary>
        public Vector4 Color = new Vector4(0.15f, 0.55f, 0.82f, 10.0f);
        public Vector2 Fresnel = new Vector2(0.6f, 0.4f);
        public float TextureTiling = 0.1f;
        public float Shininess = 1.0f;

        /// <summary>
        /// A starting point for the floodfill to find the extent of this water body.
        /// </summary>
        public Vector3 SeedPosition;

        /// <summary>
        /// string id for what type of water we're using (from the Water dictionary).
        /// </summary>
        public string name = "";
    }
}
