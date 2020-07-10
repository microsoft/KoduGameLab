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
using Boku.SimWorld.Chassis;
using Boku.SimWorld;

namespace Boku
{
    public class BigYucca2 : GameProp
    {
        //
        //  BigYucca2
        //

        public BigYucca2()
            : base(BigYucca2SRO.GetInstance)
        {
            classification = new Classification("tree",
                                        Classification.Colors.Green,
                                        Classification.Shapes.Tube,
                                        Classification.Tastes.Bitter,
                                        Classification.Smells.Pleasant,
                                        Classification.Physicalities.Static);

            StaticPropChassis staticChassis = new StaticPropChassis();
            Chassis = staticChassis;
            staticChassis.Mass = 500.0f;
            staticChassis.DefaultEditHeight = 0.0f;

        }   // end of BigYucca2 c'tor


    }   // end of class BigYucca2

}   // end of namespace Boku
