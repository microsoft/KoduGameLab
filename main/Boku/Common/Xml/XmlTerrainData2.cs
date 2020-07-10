// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Xml.Serialization;

namespace Boku.Common.Xml
{
    public class XmlTerrainData2 : BokuShared.XmlData<XmlTerrainData2>
    {
        public string virtualMapFile;

        public float cubeSize;

        public XmlWaterData[] waters;

    }
}
