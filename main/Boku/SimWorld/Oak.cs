
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
using Boku.Programming;
using Boku.SimWorld.Chassis;

namespace Boku
{
    public class Oak : GameProp
    {
        //
        //  Oak
        //

        public Oak()
            : base(OakSRO.GetInstance)
        {
            classification = new Classification("oak tree",
                                        Classification.Colors.Green,
                                        Classification.Shapes.NotApplicable,
                                        Classification.Tastes.Sweet,
                                        Classification.Smells.Pleasant,
                                        Classification.Physicalities.Static);

            StaticPropChassis staticChassis = new StaticPropChassis();
            Chassis = staticChassis;
            staticChassis.Mass = 5000.0f;
            staticChassis.DefaultEditHeight = 0.0f;

            staticRadius = 1.0f;

        }   // end of Oak c'tor

    }   // end of class Oak

}   // end of namespace Boku
