
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
using Boku.SimWorld.Terra;
using Boku.Programming;
using Boku.Common.Xml;

namespace Boku
{
    public class SwimFish : GameActor
    {
        public enum MovementModes
        {
            WaterSurface, // surfaced, keep periscope above water
            Water,        // move about in 3d water, brains don't support well due to continued hacks
            WaterFloor,   // keep to the floor of the water
        }

        private MovementModes movementMode = MovementModes.Water;

        private float flexOffset = 10.0f * MathHelper.Pi * (float)BokuGame.bokuGame.rnd.NextDouble();   // Just so all the fish aren't in sync.

        #region Accessors
        public MovementModes MovementMode
        {
            get
            {
                return movementMode;
            }
            set
            {
                this.movementMode = value;
            }
        }
        #endregion

        public SwimFish(string classificationName, BaseChassis chassis, GetModelInstance getModelInstance, StaticActor staticActor)
            : base(classificationName, classificationName, chassis, getModelInstance, getModelInstance, staticActor)
        {
            preRender += SetFlex;
        }

        public void SetFlex(FBXModel model)
        {
            float flex = 0.0f;
            SwimChassis sc = Chassis as SwimChassis;
            if (sc != null)
            {
                flex = sc.BodyFlex
                    + sc.FlexAmplitude
                        * 0.2f
                        * (float)Math.Cos(5.0f * Time.GameTimeTotalSeconds + flexOffset);
            }

            model.Effect.Parameters["Flex"].SetValue(flex);
        }

    }   // end of class SwimFish

}


