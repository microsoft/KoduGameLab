// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using KoiX;

using Boku.Common;

namespace Boku.SimWorld.Path
{
    class RoadFBXRenderObj : Road.RenderObj
    {
        #region Members
        protected FBXModel model;
        protected Matrix localToWorld;

        protected ModelAnim animator = null;
        #endregion Members

        #region Accessor
        /// <summary>
        /// Bounds for this chunk.
        /// </summary>
        public BoundingSphere Sphere
        {
            get
            {
                BoundingSphere sphere = Model.BoundingSphere;
                sphere.Center = Vector3.Transform(sphere.Center, localToWorld);
                return sphere;
            }
        }
        /// <summary>
        /// Model representing this chunk.
        /// </summary>
        public FBXModel Model
        {
            get { return model; }
            set { model = value; }
        }
        public ModelAnim Animator
        {
            get { return animator; }
            set { animator = value; }
        }
        /// <summary>
        /// Transform for this model.
        /// </summary>
        public Matrix LocalToWorld
        {
            get { return localToWorld; }
            set { localToWorld = value; }
        }
        #endregion Accessor

        #region Public
        public void Clear()
        {
            Model = null;
        }

        public void Render(Camera camera, Road road)
        {
            if (Model != null)
            {
                BoundingSphere bound = Model.BoundingSphere;
                bound.Center = Vector3.Transform(bound.Center, localToWorld);
                Frustum.CullResult cull = camera.Frustum.CullTest(bound);
                if (cull != Frustum.CullResult.TotallyOutside)
                {
                    if (Animator != null)
                        Animator.SetupActive(Model);
                    Model.DiffuseColor = road.Path.RGBColor;
                    Model.Render(camera, ref localToWorld, null);
                }
            }
        }

        public void Finish(Road.Section section)
        {
        }

        public void Finish(Road.Intersection isect)
        {
        }
        #endregion Public
    }
}
