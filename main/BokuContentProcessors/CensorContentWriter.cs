using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

namespace BokuContentProcessors
{
    using Boku.Common;

    [ContentTypeWriter]
    public class CensorContentWriter : ContentTypeWriter<CensorContent>
    {
        protected override void Write(ContentWriter output, CensorContent value)
        {
            value.WriteBinaryRepresentation(output);
        }

        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return "Boku.Common.CensorContent, Boku";
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return "Boku.Common.CensorContentReader, Boku";
        }
    }
}
