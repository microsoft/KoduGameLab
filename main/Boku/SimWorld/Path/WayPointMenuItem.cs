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

using Boku;
using Boku.Base;
using Boku.Common;
using Boku.Common.ParticleSystem;

namespace Boku.SimWorld.Path
{
    /// <summary>
    /// A wrapper around a WayPoint node giving it support to be a menu item.
    /// </summary>
    public class WayPointMenuItem : RenderObject, ITransform, IBounding, INeedsDeviceReset
    {
        private Transform local = null;
        private Matrix world = Matrix.Identity;
        private Matrix matrixSro;
        private Vector4 color = Color.Purple.ToVector4();
        private ITransform transformParent;
        private const float RenderRadius = 1.1f;

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
            get 
            {
                Vector3 max = new Vector3(RenderRadius);
                max = Vector3.Transform(max, matrixSro);
                max = Vector3.Transform(max, local.Matrix);
                return new BoundingBox(-max, max); 
            }
        }
        public BoundingSphere BoundingSphere
        {
            get 
            {
                BoundingSphere sphere = new BoundingSphere(new Vector3(), RenderRadius);
                Matrix final = this.matrixSro * local.Matrix;
                sphere.Center = Vector3.Transform(sphere.Center, final);
                sphere.Radius = Vector3.TransformNormal(new Vector3(sphere.Radius, 0.0f, 0.0f), final).Length();

                return sphere;
            }
        }
        #endregion

        public WayPointMenuItem(Vector4 color, float desiredRadius)
        {
            this.color = color;
            local = new Transform();

            this.matrixSro = Matrix.CreateScale(desiredRadius / RenderRadius);

        }   // end of WayPointMenuItem c'tor


        public override void Render(Camera camera)
        {
            Matrix matrixRender = this.matrixSro * world;
            Sphere sphere = Sphere.GetInstance();

            // Get the effect we need.  Borrow it from the particle system manager.
            Effect effect = InGame.inGame.ParticleSystemManager.Effect3d;

            // Set up common rendering values.
            effect.CurrentTechnique = effect.Techniques["OpaqueColorPass"];

            // Set parameters.
            effect.Parameters["Radius"].SetValue(1.0f);
            effect.Parameters["DiffuseColor"].SetValue(WayPoint.GetCurrentColor());
            effect.Parameters["SpecularColor"].SetValue(new Vector4(0.9f));
            effect.Parameters["SpecularPower"].SetValue(16.0f);
            effect.Parameters["Alpha"].SetValue(1.0f);

            effect.Parameters["Shininess"].SetValue(0.4f);

            // Render a small network
            Vector3 p0 = new Vector3(-0.5f, -0.4f, 0.4f);
            Vector3 p1 = new Vector3(0.5f, 0.0f, 0.0f);
            Vector3 p2 = new Vector3(-0.1f, 0.4f, -0.4f);

            Matrix mat = Matrix.CreateScale(0.5f) * Matrix.CreateTranslation(p0);
            mat *= matrixRender;
            sphere.Render(camera, ref mat, effect);
            mat = Matrix.CreateScale(0.4f) * Matrix.CreateTranslation(p1);
            mat *= matrixRender;
            sphere.Render(camera, ref mat, effect);
            mat = Matrix.CreateScale(0.3f) * Matrix.CreateTranslation(p2);
            mat *= matrixRender;
            sphere.Render(camera, ref mat, effect);

            p0 = Vector3.Transform(p0, matrixRender);
            p1 = Vector3.Transform(p1, matrixRender);
            p2 = Vector3.Transform(p2, matrixRender);

            Utils.DrawLine(camera, p0, p1, WayPoint.GetCurrentColor());
            Utils.DrawLine(camera, p1, p2, WayPoint.GetCurrentColor());
            Utils.DrawLine(camera, p2, p0, WayPoint.GetCurrentColor());

        }   // end of WayPointMenuItem Render()

        public override void Activate()
        {
        }

        public override void Deactivate()
        {
        }

        public void LoadContent(bool immediate)
        {
        }

        public void InitDeviceResources(GraphicsDevice device)
        {
        }

        public void UnloadContent()
        {
        }

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class WayPointMenuItem

}   // end of namespace Boku.SimWorld
