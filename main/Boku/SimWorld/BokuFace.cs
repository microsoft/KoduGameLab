
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
using Boku.Programming;

namespace Boku
{
    /// <summary>
    /// A class to encapsulate the stuff needed just for rendering the face texture.
    /// 
    /// There are three levels of state that the face can have.  The first level is 
    /// the default state which is how we normally see Boku.  The second level is the 
    /// emotional state.  There are "moods" that Boku expresses for a short time.  The
    /// third level is the reactive state.  These are very quick, short term reactions
    /// to events.  Both second and third levels exist for a finite duration of time.
    /// 
    /// A reactive state will override an emotional state which in turn overrides the 
    /// default state.  Whenever a state times out the level is dropped to the next
    /// active state.  The default state never times out.
    /// 
    /// If a state is triggered at the same level as the currently running state then
    /// it will override the current state.
    /// </summary>
    public class BokuFace : OneFace
    {
        #region Accessors
        #endregion Accessors

        #region Public
        public BokuFace(GetModelInstance model)
            : base(model)
        {
            PupilCenter = new Vector2(0.23f, 0.05f);
            PupilSize = 0.6f;
        }   // end of BokuFace c'tor

        #endregion Public

        #region Internal
        #endregion Internal
    }   // end of class BokuFace



}   // end of namespace Boku