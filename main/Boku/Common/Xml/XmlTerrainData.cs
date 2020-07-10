// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;

namespace Boku.Common.Xml
{
    public class XmlTerrainData : BokuShared.XmlData<XmlTerrainData>
    {
        public Point size;          // Size of raw height map file.
        public Vector3 scale;       // Size of resulting world.
        public bool hasWater = true;
        public string heightMapFilename;
        public string terrainSelectTextureFilename;
        public string terrain0TextureFilename;
        public string terrain1TextureFilename;
        public string terrain2TextureFilename;
        public string terrain3TextureFilename;
        public string terrain4TextureFilename;
        public string skirtTextureFilename;
        public string waterNormalMapTextureFilename;

        public string envMapTextureFilename;

        // List of hidden tiles for this world.
        public Point[] hiddenTiles;

        protected override bool OnLoad()
        {
            FixUpTerrainTexturePaths();
            return true;
        }

        public void FixUpTerrainTexturePaths()
        {
            // If we have any terrain textures in the old directory, fix up the paths
            // to reference the new directory.
            FixPath(ref terrain0TextureFilename);
            FixPath(ref terrain1TextureFilename);
            FixPath(ref terrain2TextureFilename);
            FixPath(ref terrain3TextureFilename);
        }   // end of XmlTerrainData FixUpTerrainTexturePaths()

        private void FixPath(ref string path)
        {
            if (path.Contains(@"\"))
            {
                path = path.Substring(path.LastIndexOf(@"\"));
            }
        }

    }   // end of class XmlTerrainData
}
