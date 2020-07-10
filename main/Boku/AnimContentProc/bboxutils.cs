// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Content;

using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using System.IO;

using TileProcessor;

namespace Xclna.Xna.Animation.Reader
{


    class BBoxer
    {
        public static void CalcBoundingBox(MeshContent mesh, ModelContent owner)
        {
            Vector3 minBox = new Vector3(Single.MaxValue, Single.MaxValue, Single.MaxValue);
            Vector3 maxBox = new Vector3(-Single.MaxValue, -Single.MaxValue, -Single.MaxValue);
            foreach (Vector3 x in mesh.Positions)
            {
                minBox.X = Math.Min(minBox.X, x.X);
                minBox.Y = Math.Min(minBox.Y, x.Y);
                minBox.Z = Math.Min(minBox.Z, x.Z);

                maxBox.X = Math.Max(maxBox.X, x.X);
                maxBox.Y = Math.Max(maxBox.Y, x.Y);
                maxBox.Z = Math.Max(maxBox.Z, x.Z);
            }

            // find the ModelMeshContent of the same name and store the bbox there
            // a little funky backwoods of the xna process, methinks
            foreach (ModelMeshContent m in owner.Meshes)
            {
                if (m.Name == mesh.Name)
                {
                    UIMeshData data = new UIMeshData();
                    data.bBox.Min = minBox;
                    data.bBox.Max = maxBox;

                    m.Tag = data;
                }
            }
        }
     }
}
