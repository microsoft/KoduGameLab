
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

/*
    This file should contain the definitions for all the custom data
    types used by the custom pipeline processor and must also contain
    the ContentTypeReaders for each of these types.
  
    This file should be part of the custom pipeline project but should
    also be included via link to both the Boku projects.  The Boku360
    project's properties must also be modified so that the assembly it
    creates is called "Boku" rather than "Boku360".  This is so we can
    use a single GetRuntimeReader() for both projects.
*/

/* This is needed when *writing* the UIMeshData type, but *cannot* be linked on the
 * 360 (no serialization.compiler)
 * That's why it's not in the UIMeshData.cs file.
 */

namespace TileProcessor
{
    /// <summary>
    /// Writer for UIMeshData
    /// </summary>
    [ContentTypeWriter]
    class UIMeshDataWriter : ContentTypeWriter<UIMeshData>
    {
        protected override void Write(ContentWriter output, UIMeshData value)
        {
            output.Write(value.bBox.Min);
            output.Write(value.bBox.Max);
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            // In order for this to work both Boku projects (Windows and Xbox) must
            // create assemblies named "Boku".
            //
            // Alternatively you could leave the Xbox projects assembly name as Boku360
            // and then change this code to check the targetPlatform parameter.
            return "TileProcessor.UIMeshDataReader, Boku, Version=1.0.0.0, Culture=neutral";
        }
    }
}