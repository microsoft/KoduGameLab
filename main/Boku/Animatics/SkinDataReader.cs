// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;


namespace Boku.Animatics
{
    internal class SkinDataReader : ContentTypeReader<SkinDataList>
    {
        protected override SkinDataList Read(ContentReader input, SkinDataList existingInstance)
        {
            return new SkinDataList(input);
        }
    }
}
