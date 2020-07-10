// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

using Microsoft.Xna.Framework;

namespace Boku.Common.Xml
{
    public class XmlTerrainMaterialData : BokuShared.XmlData<XmlTerrainMaterialData>
    {
        public string[] BotDiffuse = null;
        public string[] TopDiffuse= null;

        public string[] BotBump = null;
        public string[] TopBump = null;

        public Vector4 Color;
        public Vector4 TopColor;

        public float Gloss = 1.0f;
        public float TopGloss = 1.0f;

        public bool TopClamped = true;
        public bool BotClamped = false;

        public float BotBumpStrength = 1.0f;
        public float TopBumpStrength = 1.0f;

        public float Flexibility = 1.0f; // less is more rigid.

        public float Step = 0.0f;

        public float BotScale = 1.0f;
        public float TopScale = 1.0f;

        public Vector4 TopEmissive = Vector4.UnitW;
        public Vector4 BotEmissive = Vector4.UnitW;

        public string TechniqueExt = "";

#if !ADDIN
        public Boku.Audio.Foley.CollisionSound CollisionSound = Boku.Audio.Foley.CollisionSound.unknown;
#endif
    }
}
