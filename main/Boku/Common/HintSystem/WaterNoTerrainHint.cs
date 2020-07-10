// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Fx;
using Boku.Common;
using Boku.Common.Xml;

namespace Boku.Common.HintSystem
{
    /// <summary>
    /// Hint ot be displayed when user tries to add water where there's no terrain.
    /// </summary>
    public class WaterNoTerrainHint : BaseHint
    {
        private static bool activate = false;

        public WaterNoTerrainHint()
        {
            id = "WaterNoTerrainHint";

            toastText = Strings.Localize("toast.waterNoTerrainToast");
            modalText = Strings.Localize("toast.waterNoTerrainModal");
        }

        public override bool Update()
        {
            bool result = activate && !disabled;
            activate = false;

            return result;
        }

        /// <summary>
        /// Used to extrnally activate this hint.
        /// </summary>
        public static void Activate()
        {
            activate = true;
        }

    }   // end of class WaterNoTerrainHint

}   // end of namespace Boku.Common.HintSystem
