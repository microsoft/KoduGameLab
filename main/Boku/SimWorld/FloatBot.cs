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
using Boku.Common.ParticleSystem;
using Boku.SimWorld;
using Boku.SimWorld.Chassis;
using Boku.Programming;
using Boku.SimWorld.Terra;
using Boku.Common.Xml;

namespace Boku
{
    public class FloatBot : GameActor
    {
        private SteamPuffEmitter steamPuffEmitter = null;

        public FloatBot(string classificationName, BaseChassis chassis, GetModelInstance getModelInstance, StaticActor staticActor)
            : base(classificationName, classificationName, chassis, getModelInstance, getModelInstance, staticActor)
        {
        }

        protected override void XmlConstruct()
        {
            if (XmlActorParams != null)
            {
                Debug.Assert(XmlActorParams.SmokeSources != null);
                foreach (XmlSmokeSource source in XmlActorParams.SmokeSources)
                {
                    steamPuffEmitter = new SteamPuffEmitter(0.5f);
                    steamPuffEmitter.Usage = source.Usage;
                    AddEmitter(steamPuffEmitter,
                        source.Bone,
                        source.Offset);
                }
            }
            else
            {
                steamPuffEmitter = new SteamPuffEmitter(0.5f);
                AddEmitter(steamPuffEmitter, SteamFunnelPosition);
            }

            base.XmlConstruct();
        }

        public Vector3 SteamFunnelPosition
        {
            get { return Vector3.Transform(new Vector3(-0.3f, 0.0f, 1.4f), Movement.LocalMatrix); }
        }

        public override void Deactivate()
        {
            if (CurrentState != State.Inactive)
            {
                steamPuffEmitter.Dying = true;
            }

            base.Deactivate();
        }

    }   // end of class FloatBot

}
