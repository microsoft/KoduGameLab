using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework.Content.Pipeline;

namespace Boku.Common
{
    public partial class CensorContent : ContentItem
    {
        public CensorContent(ContentIdentity identity)
        {
            this.Identity = identity;
        }
    }
}
