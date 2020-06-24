using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using KoiX;

using Boku.Common;

namespace Boku.SimWorld.Path
{
    class RoadMultiRenderObj : Road.RenderObj
    {
        #region Members
        protected BoundingSphere sphere;
        protected List<Road.RenderObj> renderObjs = new List<Road.RenderObj>();
        #endregion Members

        #region Accessors
        public BoundingSphere Sphere
        {
            get { return sphere; }
        }
        #endregion Accessors

        #region Public
        public List<Road.RenderObj> RenderObjList
        {
            get { return renderObjs; }
        }

        public void Clear()
        {
            foreach (Road.RenderObj renderObj in renderObjs)
            {
                renderObj.Clear();
            }
            renderObjs.Clear();
        }

        public void Render(Camera camera, Road road)
        {
            foreach (Road.RenderObj renderObj in renderObjs)
            {
                renderObj.Render(camera, road);
            }
        }

        public void Finish(Road.Section section)
        {
            AABB box = AABB.EmptyBox();
            foreach (Road.RenderObj renderObj in renderObjs)
            {
                renderObj.Finish(section);
                box.Union(renderObj.Sphere);
            }
            sphere = box.MakeSphere();
        }

        public void Finish(Road.Intersection isect)
        {
            AABB box = AABB.EmptyBox();
            foreach (Road.RenderObj renderObj in renderObjs)
            {
                renderObj.Finish(isect);
                box.Union(renderObj.Sphere);
            }
            sphere = box.MakeSphere();
        }

        #endregion Public
    }
}
