// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;
using Boku.Common;
using Boku.Common.Xml;
using Boku.Fx;

namespace Boku.SimWorld
{
    public class RoverGreeter : BokuGreeter
    {
        public RoverGreeter(string classificationName, BaseChassis chassis, GetModelInstance getModelInstance, StaticActor staticActor)
            : base(classificationName, chassis, getModelInstance, staticActor)
        {
        }
    }

    /// <summary>
    /// Inner child rendering object for RoverGreeter.
    /// </summary>
    public class RoverGreeterModel : FBXModel
    {
        // c'tor
        public RoverGreeterModel(string resourceName)
            : base(resourceName)
        {
            NeverDisplayCollisions = true;
        }

        /// <summary>
        /// Set our envmap as current, render, then restore.
        /// </summary>
        public override void Render(Camera camera, ref Matrix rootToWorld, List<List<PartInfo>> listPartsReplacement)
        {
            ShaderGlobals.PushEnvMap(RoverGreeter.EnvMap);

            base.Render(camera, ref rootToWorld, listPartsReplacement);

            ShaderGlobals.PopEnvMap();
        }

    }   // end of class RoverGreeterModel
}
