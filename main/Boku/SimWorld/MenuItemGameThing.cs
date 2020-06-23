
using System;
using System.Collections;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku;
using Boku.Base;
using Boku.Common;

namespace Boku.SimWorld
{
    /// <summary>
    /// A wrapper around a game thing giving it support to be a menu item.
    /// </summary>
    public class MenuItemGameThing : RenderObject, ITransform, IBounding
    {
        public GameThing gameThing = null;
        private RenderObject renderObj = null;
        private Transform local = null;
        private Matrix world = Matrix.Identity;
        private ITransform transformParent;

        #region ITransform Members
        Transform ITransform.Local
        {
            get { return local; }
            set { local = value; }
        }

        Matrix ITransform.World
        {
            get { return world; }
        }

        void ITransform.Recalc(ref Matrix parentMatrix)
        {
            world = local.Matrix * parentMatrix;
            gameThing.Movement.Position = world.Translation;

        }
        bool ITransform.Compose()
        {
            bool changed = this.local.Compose();
            if (changed)
            {
                RecalcMatrix();
            }
            return changed;
        }
        ITransform ITransform.Parent
        {
            get
            {
                return this.transformParent as ITransform;
            }
            set
            {
                this.transformParent = value;
            }
        }
        #endregion

        protected void RecalcMatrix()
        {
            Matrix parentWorldMatrix;
            if (transformParent != null)
            {
                parentWorldMatrix = transformParent.World;
            }
            else
            {
                parentWorldMatrix = Matrix.Identity;
            }
            ITransform transformThis = this as ITransform;
            transformThis.Recalc(ref parentWorldMatrix);
        }

        #region IBounding Members
        public BoundingBox BoundingBox
        {
            get { return new BoundingBox(new Vector3(-0.5f), new Vector3(0.5f)); }
        }

        public BoundingSphere BoundingSphere
        {
            get { return new BoundingSphere(new Vector3(), 1.0f); }
        }
        #endregion

        public MenuItemGameThing(GameThing gameThing, RenderObject renderObj)
        {
            this.gameThing = gameThing;
            this.renderObj = renderObj;
            local = new Transform();
        }   // end of MenuItemGameThing c'tor


        public override void Render(Camera camera)
        {
            renderObj.Render(camera);
        }

        public override void Activate()
        {
            renderObj.Activate();
        }

        public override void Deactivate()
        {
            renderObj.Deactivate();
        }
    }   // end of class MenuItemGameThing

}   // end of namespace Boku.SimWorld
