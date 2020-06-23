
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
    public class GiftBox : GameProp
    {
        //
        //  GiftBox
        //

        public GiftBox()
            : base(GiftBoxSRO.GetInstance)
        {
            classification = new Classification("giftbox",
                                        Classification.Colors.Red,
                                        Classification.Shapes.NotApplicable,
                                        Classification.Tastes.Sweet,
                                        Classification.Smells.Pleasant,
                                        Classification.Physicalities.Collectable);

            DynamicPropChassis dynChassis = new DynamicPropChassis();
            Chassis = dynChassis;
            dynChassis.Mass = 1.0f;
            dynChassis.DefaultEditHeight = 0.0f;
            dynChassis.CoefficientOfRestitution = 0.4f;

            collisionRadius = 0.2f;

        }   // end of GiftBox c'tor

    }   // end of class GiftBox

}   // end of namespace Boku
