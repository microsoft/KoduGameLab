// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Boku.Common;

namespace Boku.SimWorld.Path
{
    class VeggieGen_C : VeggieGen
    {
        public override FBXModel Model
        {
            get
            {
                if (model == null)
                    model = ActorManager.GetActor("Lavender").Model;
                return model;
            }
        }
        public override FBXModel EndModel
        {
            get
            {
                if (endModel == null)
                    endModel = ActorManager.GetActor("Daisy").Model;
                return endModel;
            }
        }
    }
}
