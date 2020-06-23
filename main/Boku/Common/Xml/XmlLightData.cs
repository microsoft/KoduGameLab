using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Boku.Common.Xml
{
    public class XmlLightData : BokuShared.XmlData<XmlLightData>
    {
        public Vector3 rotationAxis = new Vector3(0.0f, 0.0f, 1.0f);
        public Vector3 local = new Vector3(0.0f, 0.0f, 1.0f);

        public Vector3 color = new Vector3(0.0f, 0.0f, 0.0f);

        public float rotationRate = 0.0f;
    }

    public class XmlLightRigData : BokuShared.XmlData<XmlLightRigData>
    {
        public XmlLightData[] lightData = null;

        public float wrap = 1.0f;

        public string name = null;
    }
}
