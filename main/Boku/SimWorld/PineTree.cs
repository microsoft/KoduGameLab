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

using Boku.Base;
using Boku.Common;
using Boku.SimWorld;
using Boku.SimWorld.Chassis;
using Boku.Programming;

namespace Boku
{
    public class PineTree : GameProp
    {
        //
        //  PineTree
        //

        public PineTree()
            : base(PineTreeSRO.GetInstance)
        {
            classification = new Classification("pine tree",
                                        Classification.Colors.Green,
                                        Classification.Shapes.NotApplicable,
                                        Classification.Tastes.Sweet,
                                        Classification.Smells.Pleasant,
                                        Classification.Physicalities.Static);

            StaticPropChassis staticChassis = new StaticPropChassis();
            Chassis = staticChassis;
            staticChassis.Mass = 2500.0f;
            staticChassis.DefaultEditHeight = 0.0f;

            staticRadius = 0.9f;

        }   // end of PineTree c'tor

    }   // end of class PineTree

}   // end of namespace Boku
