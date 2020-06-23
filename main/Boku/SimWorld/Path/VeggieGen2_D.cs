using System;
using System.Collections.Generic;
using System.Text;
using Boku.Common;

namespace Boku.SimWorld.Path
{
    class VeggieGen2_D : VeggieGen2
    {
        #region Accessors
        public override FBXModel Model
        {
            get
            {
                if (model == null)
                    model = ActorManager.GetActor("Daisy").Model;
                return model;
            }
        }
        public override FBXModel EndModel
        {
            get
            {
                if (endModel == null)
                    endModel = ActorManager.GetActor("Daisy").Model;
                return endModel;
            }
        }
        #endregion Accessors

    }
}
