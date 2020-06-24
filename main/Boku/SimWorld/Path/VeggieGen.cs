using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using KoiX;

using Boku.Common;
using Boku.SimWorld;
using Boku.SimWorld.Terra;

namespace Boku.SimWorld.Path
{
    class VeggieGen : RoadGenerator
    {
        #region Members
        protected float width = 1.0f;
        protected float density = 0.75f;

        private ModelAnim anim = null;
        private ModelAnim endAnim = null;

        private int lastUpdate = -1;

        protected FBXModel model;
        protected FBXModel endModel;
        #endregion Members

        #region Accessors
        /// <summary>
        /// No trimming needed.
        /// </summary>
        public override bool Trims
        {
            get { return false; }
        }
        /// <summary>
        /// The model to distribute along the path
        /// </summary>
        public virtual FBXModel Model
        {
            get 
            {
                if(model == null)
                    model = ActorManager.GetActor("Popsy").Model;
                return model;
            }
        }
        /// <summary>
        /// The model to plant at nodes (intersections).
        /// </summary>
        public virtual FBXModel EndModel
        {
            get
            {
                if (endModel == null)
                    endModel = ActorManager.GetActor("Daisy").Model;
                return endModel;
            }
        }
        protected ModelAnim Anim
        {
            get { return anim; }
            private set { anim = value; }
        }
        protected ModelAnim EndAnim
        {
            get { return endAnim; }
            private set { endAnim = value; }
        }
        public override float CostPerMeter
        {
            get { return 1.37f; }
        }
        public override float CostPerNode
        {
            get { return 1.0f; }
        }
        #endregion Accessors

        #region Public

        public VeggieGen()
        {
            Anim = new ModelAnim(Model, "wind");
            EndAnim = new ModelAnim(EndModel, "wind");
        }

        public override void Update()
        {
            if (!Time.Paused && (Time.FrameCounter != lastUpdate))
            {
                Anim.Update();
                EndAnim.Update();

                lastUpdate = Time.FrameCounter;
            }
        }

        /// <summary>
        /// Spread flora over new section of road.
        /// </summary>
        /// <param name="section"></param>
        public override void NewSection(Road.Section section)
        {
            RoadMultiRenderObj multiRo = null;
            FBXModel model = Model;
            if (model != null)
            {
                multiRo = new RoadMultiRenderObj();

                Vector2 start = section.Edge.Node0.Position2d;
                Vector2 end = section.Edge.Node1.Position2d;
                Vector2 axis = end - start;
                Vector2 norm = new Vector2(-axis.Y, axis.X);
                norm = Vector2.Normalize(norm) * width;

                float area = axis.Length() * width * 2.0f;

                int count = (int)Math.Ceiling(area * density);

                for (int i = 0; i < count; ++i)
                {
                    float rndTo = (float)BokuGame.bokuGame.rnd.NextDouble();
                    float rndOut = (float)(BokuGame.bokuGame.rnd.NextDouble() * 2.0 - 1.0);

                    Vector2 rndPos = start + rndTo * axis + rndOut * norm;
                    float height = Terrain.GetTerrainHeightFlat(rndPos);

                    height += section.Edge.Node0.Height + rndTo * (section.Edge.Node1.Height - section.Edge.Node0.Height);

                    Matrix localToWorld = Matrix.CreateTranslation(rndPos.X, rndPos.Y, height);

                    RandomizeAngle(ref localToWorld);

                    RoadFBXRenderObj ro = new RoadFBXRenderObj();
                    ro.Model = model;
                    ro.LocalToWorld = localToWorld;
                    ro.Animator = Anim;

                    multiRo.RenderObjList.Add(ro);
                }
            }
            section.RenderObj = multiRo;
        }

        /// <summary>
        /// Spread flora over intersection of road.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public override bool NewFan(
            Road.Intersection isect, 
            Road.Section first, 
            Road.Section second,
            List<Road.RenderObj> fans)
        {
            FBXModel model = EndModel;
            if (model != null)
            {
                RoadFBXRenderObj ro = new RoadFBXRenderObj();

                ro.Model = model;
                ro.Animator = EndAnim;

                Matrix localToWorld = Matrix.Identity;
                Vector3 position = isect.Node.Position;
                position.Z = Terrain.GetTerrainHeightFlat(position) + isect.Node.Height;
                localToWorld.Translation = position;
                RandomizeAngle(ref localToWorld);
                ro.LocalToWorld = localToWorld;

                fans.Add(ro);
                return true;
            }
            return false;
        }
        #endregion Public

        #region Internal
        protected void RandomizeAngle(ref Matrix localToWorld)
        {
            float rndAng = (float)((BokuGame.bokuGame.rnd.NextDouble() * 2.0 - 1.0) * Math.PI);
            float rndSin = (float)Math.Sin(rndAng);
            float rndCos = (float)Math.Cos(rndAng);

            localToWorld.M11 = rndCos;
            localToWorld.M12 = -rndSin;
            localToWorld.M21 = rndSin;
            localToWorld.M22 = rndCos;
        }
        #endregion Internal
    }
}
