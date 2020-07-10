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

namespace Boku
{
    public class Fireball : GameThing
    {
        protected class UpdateObj : UpdateObject
        {
            private Fireball parent = null;

            public UpdateObj(Fireball parent)
            {
                this.parent = parent;
            }   // end of UpdateObj c'tor

            public override void Update()
            {
            }   // end of UpdateObj Update()

            public override void Activate()
            {
            }

            public override void Deactivate()
            {
            }

        }   // end of class UpdateObj


        //
        //  Fireball
        //
        public override BoundingSphere BoundingSphere
        {
            get { return new BoundingSphere(Movement.Position, 0.0f); }
        }

        private UpdateObj updateObj = null;

        private bool state = false;
        private bool pendingState = false;

        public Fireball(Classification.Colors color)
        {
            updateObj = new UpdateObj(this);
            classification = new Classification("fireball",
                                                color,
                                                Classification.Shapes.Ball,
                                                Classification.Tastes.NotApplicable,
                                                Classification.Smells.Stinky,
                                                Classification.Physicalities.NotApplicable);

        }   // end of Fireball c'tor

        private Fireball()
        {
        }

        #region Accessors       
        public override RenderObject RenderObject
        {
            get { return null; }
        }
        #endregion

        public override bool Refresh(ArrayList updateList, ArrayList renderList)
        {
            bool result = false;

            if (state != pendingState)
            {
                if (pendingState)
                {
                    updateList.Add(updateObj);
                    updateObj.Activate();
                }
                else
                {
                    BokuGame.gameListManager.RemoveObject(this);
                    updateObj.Deactivate();
                    updateList.Remove(updateObj);
                    
                    result = true;
                }

                state = pendingState;
            }

            // call refresh on child list

            return result;
        }   // end of Fireball Refresh()

        override public void Activate()
        {
            if (!state)
            {
                pendingState = true;
                BokuGame.objectListDirty = true;
            }
        }
        override public void Deactivate()
        {
            if (state)
            {
                pendingState = false;
                BokuGame.objectListDirty = true;
            }
        }
        /// <summary>
        /// called to check if the object is NOT deactivated and NOT about to be removed
        /// </summary>
        /// <returns></returns>
        public override bool IsAlive()
        {
            return pendingState;
        }

        public override void LoadContent(bool immediate)
        {
        }   // end of Fireball LoadContent()

        public override void InitDeviceResources(GraphicsDeviceManager graphics)
        {
        }

        public override void UnloadContent()
        {
        }   // end of Fireball UnloadContent()


    }   // end of class Fireball

}   // end of namespace Boku
