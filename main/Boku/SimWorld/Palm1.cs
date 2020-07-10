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

namespace Boku
{
    public class Palm1 : GameProp
    {
        public Palm1()
            : base(Palm1SRO.GetInstance)
        {
            classification = new Classification("tree",
                                        Classification.Colors.Grey,
                                        Classification.Shapes.NotApplicable,
                                        Classification.Tastes.Bitter,
                                        Classification.Smells.Pleasant,
                                        Classification.Physicalities.Static);

            StaticPropChassis staticChassis = new StaticPropChassis();
            Chassis = staticChassis;
            staticChassis.Mass = 2500.0f;
            staticChassis.DefaultEditHeight = 0.0f;

            staticRadius = 0.1f;

        }   // end of Palm1 c'tor

    }   // end of class Palm1

}   // end of namespace Boku
