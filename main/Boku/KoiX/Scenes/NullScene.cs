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
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI;

using Boku;

namespace KoiX.Scenes
{
    /// <summary>
    /// Placeholder scene.  Used when the SceneManager needs to be ignored
    /// while old-style scenes are in control.
    /// </summary>
    public class NullScene : BaseScene
    {
        #region Members
        #endregion

        #region Accessors
        #endregion

        #region Public

        // c'tor
        public NullScene()
            : base("NullScene")
        {
        }

        #endregion

        #region InputEventHandler
        #endregion

        #region Internal
        #endregion


    }   // end of class NullScene

}   // end of namespace KoiX.Scenes
