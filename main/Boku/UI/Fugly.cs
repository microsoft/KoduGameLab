// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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
using Boku.UI;
using Boku.Programming;
using Boku.Input;
using Boku.Audio;

namespace Boku.UI
{
    /// <summary>
    /// A fugly is a special little object that is used to lure the 
    /// programming cursor away from its normal position and place 
    /// it at the top of the screen where the Page Handle resides.
    /// Fuglys are mutants.  Even though they inherit from ITransform
    /// they have no parents.
    /// </summary>
    public class Fugly : ITransform
    {
        private Transform localTransform = null;

        #region ITransform Members

        public Transform Local
        {
            get { return localTransform; }
            set { localTransform = value; }
        }

        public Matrix World
        {
            get { return localTransform.Matrix; }
        }

        public bool Compose()
        {
            bool changed = this.localTransform.Compose();
            if (changed)
            {
                RecalcMatrix();
            }
            return changed;
        }

        public void Recalc(ref Matrix parentMatrix)
        {
        }

        public ITransform Parent
        {
            get { return null; }
            set { }
        }

        protected void RecalcMatrix()
        {
            ITransform transformThis = this as ITransform;
            Matrix parentMatrix = Matrix.Identity;
            transformThis.Recalc(ref parentMatrix);
        }

        #endregion

        // c'tor
        public Fugly()
        {
            localTransform = new Transform();
        }

    }   // end of class Fugly

}   // end of namespace Boku.UI
