// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

namespace Boku.Common
{
    /// <summary>
    /// A container for info needed to create a blob shadow.
    /// </summary>
    public class ShadowBlob
    {
        public Vector3 position;
        public float radius = 0.0f; // Actually store radius^2
        public AABB box;

        public ShadowBlob(Vector3 position, float radius)
        {
            this.position = position;
            this.radius = radius * radius;

            Vector3 min = new Vector3(position.X - radius, position.Y - radius, float.MinValue);
            Vector3 max = new Vector3(position.X + radius, position.Y + radius, float.MaxValue);
            box = new AABB(min, max);
        }

    }   // end of class ShadowBlob

}   // end of namespace BokuCommon
