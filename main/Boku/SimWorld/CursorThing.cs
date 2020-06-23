using System;
using System.Collections;
using System.Collections.Generic;
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

namespace Boku.SimWorld
{
    class CursorThing : GameThing
    {
        protected class UpdateCursorObj : UpdateObject
        {
            private GameThing parent = null;
            public Cursor3D cursor3d;

            public UpdateCursorObj(GameThing parent, Cursor3D cursor3d)
            {
                this.parent = parent;
                this.cursor3d = cursor3d;
            } 


            public override void Update()
            {
                // Match position of the static Cursor3D.
                parent.Movement.Position = cursor3d.Position;
            } 

            public override void Activate()
            {
            }

            public override void Deactivate()
            {
            }

        }

        private UpdateCursorObj updateObj = null;
        
        private bool state = false;
        private bool pendingState = false;

        #region Accessors
        public override BoundingSphere BoundingSphere
        {
            get
            {
                return new BoundingSphere(Vector3.Zero, 0.5f);
            }
        }

        public override RenderObject RenderObject
        {
            get { return updateObj.cursor3d.RenderObject; }
        }

        public override bool Mute { get { return false; } set { } }

        public override bool Invulnerable { get { return true; } set { } }

        /// <summary>
        /// Returns the "standard" height above the terrain for instantiating
        /// an object of this type.
        /// </summary>
        public float GetPreferredHeight()
        {
            return 1.0f;
        }
        #endregion

        public CursorThing(Cursor3D cursor3d)
            : base("cursor", cursor3d.Chassis)
        {
            classification.physicality = Classification.Physicalities.NotApplicable;
            classification.Color = Classification.Colors.NotApplicable;
            updateObj = new UpdateCursorObj(this, cursor3d);

            // Copy over references from real cursor.
            movement = cursor3d.Movement;
        }

        public override bool Refresh(List<UpdateObject> updateList, List<RenderObject> renderList)
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

            return result;
        }

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
        }

        public override void InitDeviceResources(GraphicsDevice device)
        {
        }

        public override void UnloadContent()
        {
        }
    }
}
