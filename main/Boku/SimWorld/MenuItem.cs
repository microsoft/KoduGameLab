using System;
using System.Collections;
using System.Text;

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
    abstract public class MenuItem : RenderObject, ITransform, IBounding, INeedsDeviceReset
    {
        #region Accessors

        protected float ScaleModelFixup
        {
            get { return scaleModelFixup; }
            set { scaleModelFixup = value; }
        }
        
        public Classification.Colors Color
        {
            get { return color; }
            set { color = value;  }
        }

        /// <summary>
        /// User friendly name for this menu item.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// Name of the type of this menu item.
        /// </summary>
        public string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        #endregion Accessors

        protected string name = null;
        protected string typeName = null;

        protected Transform local = new Transform();
        protected Matrix world = Matrix.Identity;
        protected Matrix matrixSro;
        protected FBXModel sro = null;
        protected ITransform transformParent;
        protected Classification.Colors color = Classification.Colors.White;
        private ModelAnim modelAnim = null;
        private float desiredRadius;
        protected float scaleModelFixup = 1.0f;

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
                return this.transformParent;
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
            get { return sro.BoundingBox; }
        }

        public BoundingSphere BoundingSphere
        {
            get
            {
                BoundingSphere sphere = sro.BoundingSphere;
                Matrix final = this.matrixSro * local.Matrix;
                sphere.Center = Vector3.Transform(sphere.Center, final);
                sphere.Radius = Vector3.TransformNormal(new Vector3(sphere.Radius / ScaleModelFixup, 0.0f, 0.0f), final).Length();

                return sphere;
            }
        }
        #endregion

        public FBXModel SRO
        {
            get { return sro; }
        }

        /// <summary>
        /// MenuItem c'tor
        /// </summary>
        /// <param name="model">Model used for rendering menu item.</param>
        /// <param name="desiredRadius">Used to adjust size of bot in menu.</param>
        /// <param name="name">User friendly name of bot.</param>
        /// <param name="typeName"></param>
        protected MenuItem(FBXModel model, float desiredRadius, string name, string typeName)
        {
            sro = model;
            this.desiredRadius = desiredRadius;
            this.name = name;
            this.typeName = typeName;

            BokuGame.Load(this);
        }

        public override void Activate()
        {
        }

        public override void Deactivate()
        {
        }

        public override void Render(Camera camera)
        {
            modelAnim.Update();
            modelAnim.SetupIdle(sro);

            sro.RenderColor = Classification.ColorVector4(Color);
            Matrix matrixRender = this.matrixSro * world;
            sro.Render(camera, ref matrixRender, null);
        }

        virtual public void LoadContent(bool immediate)
        {
            BokuGame.Load(sro, immediate);
        }

        virtual public void InitDeviceResources(GraphicsDevice device)
        {
            modelAnim = new ModelAnim(SRO, "idle");

            // the models real center is not the bounding sphere center
            // since the model was created with the origin at the bottom
            // so we need to adjust it

            // Hover Cars are modelled with +Z up so rotate them into the correct
            // orientation for UI use (+Y up).
            this.matrixSro = Matrix.CreateTranslation(-sro.BoundingSphere.Center);
            this.matrixSro *= Matrix.CreateRotationX(MathHelper.ToRadians(-90.0f));
            this.matrixSro *= Matrix.CreateRotationY(MathHelper.ToRadians(-90.0f));
            this.matrixSro *= Matrix.CreateScale(desiredRadius * ScaleModelFixup / sro.BoundingSphere.Radius);
        }

        virtual public void UnloadContent()
        {
            BokuGame.Unload(sro);
        }

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
        }

    }
}
