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
    public class SmallStoneBuilding : GameProp
    {
        //
        //  SmallStoneBuilding
        //

        public SmallStoneBuilding()
            : base(SmallStoneBuildingSRO.GetInstance)
        {
            classification = new Classification("smallstonebuilding",
                                        Classification.Colors.Red,
                                        Classification.Shapes.NotApplicable,
                                        Classification.Tastes.Sweet,
                                        Classification.Smells.Pleasant,
                                        Classification.Physicalities.Collectable);

            StaticPropChassis staticChassis = new StaticPropChassis();
            Chassis = staticChassis;
            staticChassis.Mass = 5000.0f;
            staticChassis.DefaultEditHeight = 0.0f;

        }   // end of SmallStoneBuilding c'tor

    }   // end of class SmallStoneBuilding

}   // end of namespace Boku
