
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

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

namespace TileProcessor
{
    /// <summary>
    /// Here's a custom object to hold our custom data (just a bounding box right now)
    /// It will hang off the "tag" field of the mesh
    /// </summary>
    public class UIMeshData
    {
        /// <summary>
        /// bounding box (min and max vectors)
        /// </summary>
        public BoundingBox bBox;
    }

    /// <summary>
    /// Reader for custom mesh data (bounding box)
    /// </summary>
    public class UIMeshDataReader : ContentTypeReader<UIMeshData>
    {
        /// <summary>
        /// Read in the custom data
        /// </summary>
        /// <param name="input">Content reader to pull data from</param>
        /// <param name="existingInstance">Existing mesh data, if any</param>
        /// <returns></returns>
        protected override UIMeshData Read(ContentReader input, UIMeshData existingInstance)
        {
            UIMeshData result = new UIMeshData();
            result.bBox.Min = input.ReadVector3();
            result.bBox.Max = input.ReadVector3();

            return result;
        }
    }
}   // end of namespace TileProcessor
