
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
    public class Cypress : GameProp
    {
        //
        //  Cypress
        //

        public Cypress()
            : base(CypressSRO.GetInstance)
        {
            classification = new Classification("cypress tree",
                                        Classification.Colors.Green,
                                        Classification.Shapes.NotApplicable,
                                        Classification.Tastes.Sweet,
                                        Classification.Smells.Pleasant,
                                        Classification.Physicalities.Static);

            StaticPropChassis staticChassis = new StaticPropChassis();
            Chassis = staticChassis;
            staticChassis.Mass = 500.0f;
            staticChassis.DefaultEditHeight = 0.0f;

            staticRadius = 0.85f;

        }   // end of Cypress c'tor

    }   // end of class Cypress

}   // end of namespace Boku
