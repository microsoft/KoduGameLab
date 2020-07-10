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
using Boku.Common.Xml;
using Boku.Common.ParticleSystem;

namespace Boku
{
    public class InkJet : GameActor
    {
        #region Members
        private InkEmitter  emitter = null;
        #endregion Members

        #region Accessors
        #endregion Accessors

        //
        //  InkJet
        //

       public InkJet(string classificationName, BaseChassis chassis, GetModelInstance getModelInstance, StaticActor staticActor)
            : base(classificationName, classificationName, chassis, getModelInstance, getModelInstance, staticActor)
        {
            //
            emitter = new InkEmitter(InGame.inGame.ParticleSystemManager);
            emitter.Active = true;
            emitter.AddToManager();

            AddEmitter(emitter, Vector3.Zero);

            ClassColor = Base.Classification.Colors.Black;

        }   // end of InkJet c'tor

       protected override void UpdateAttachments()
       {
           // Set emitter color to match classification color.
           emitter.Color = Classification.ColorRGBA;

           base.UpdateAttachments();
       }

    }   // end of class InkJet

}   // end of namespace Boku
