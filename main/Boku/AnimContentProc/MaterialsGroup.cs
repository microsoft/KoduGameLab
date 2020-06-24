using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace BokuPipeline
{
    /// <summary>
    /// This class is about collapsing up to 8 "materials", all of which use
    /// a single shader, into a single mesh. We then use vertex colors to identify
    /// which "material" goes with which section of the mesh.
    /// There's a good bit of use of cluster names to identify parts.
    /// The result of the collapse is named by the prefix of the source cluster(s),
    /// with the prefix being the part before the first underscore (_).
    /// The material index for a part is from the numeric suffix after the last
    /// underscore (_).
    /// The root of the geometry must contain the string _VC_ in order to turn on
    /// processing.
    /// So, with a root named Base_VC_0, the default SurfaceSet will be "Base", using
    /// index 0 from the set. Another part might be called Base_2, which will use the
    /// index 2 into the same SurfaceSet named Base.
    /// The SurfaceSet is defined in the actor's .xml file (e.g. @"Content\Xml\Actors\Boku.xml")
    /// </summary>
    public class MaterialsGroup
    {
        #region Public
        public static bool PreProcess(NodeContent root)
        {
            bool collapsed = false;
            if (root is MeshContent)
            {
                MeshContent mesh = (MeshContent)root;
                if (mesh.Name.Contains("_VC_"))
                {
                    ColorMaterials(mesh);
                    Collapse(mesh);
                    collapsed = true;
                }
            }
            foreach (NodeContent child in root.Children)
            {
                if (PreProcess(child))
                {
                    collapsed = true;
                }
            }
            return collapsed;
        }

        /// <summary>
        /// Just set the names for reference at runtime. 
        /// Also, if requested, null out the material
        /// so it doesn't get dragged into the runtime.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="nullMats"></param>
        public static void TagMeshParts(ModelContent c, bool nullMats)
        {
            foreach (ModelMeshContent meshContent in c.Meshes)
            {
                foreach (ModelMeshPartContent part in meshContent.MeshParts)
                {
                    part.Tag = part.Material != null
                        ? part.Material.Name
                        : "Default";

                    /// The following line will keep the part.Effect, which we don't use
                    /// from coming over to the runtime and bloating things up.
                    /// Thing is, we currently, for some models, still extract
                    /// things like the diffuse color from the Material. With the new
                    /// model material, it's not an issue, but until they are converted
                    /// we have to take the bad with the good.
                    /// TODO - MAFINCH 
                    if (nullMats)
                    {
                        part.Material = null;
                    }
                }
            }
        }

        /// <summary>
        /// Make sure all of the geometry contains a vertex color channel.
        /// </summary>
        /// <param name="root"></param>
        public static void EnsureVertexColors(NodeContent root)
        {
            if (root is MeshContent)
            {
                MeshContent mesh = (MeshContent)root;
                EnsureVertexColors(mesh);
            }
            foreach (NodeContent child in root.Children)
            {
                EnsureVertexColors(child);
            }
        }
        #endregion Public

        #region Internal
        private static string GroupName(GeometryContent meshPart)
        {
            string groupName = meshPart.Material.Name;
            groupName = groupName.Remove(groupName.IndexOf('_'));
            return groupName;
        }
        private static int MatIndex(GeometryContent meshPart)
        {
            string matName = meshPart.Material.Name;
            matName = matName.Remove(0, matName.LastIndexOf('_') + 1);
            return int.Parse(matName);
        }
        private static Vector4 MatColor(GeometryContent meshPart)
        {
            int matIndex = MatIndex(meshPart);
            Vector4 matColor = Vector4.UnitW;
            if ((matIndex & 4) != 0)
                matColor.X = 1.0f;
            if ((matIndex & 2) != 0)
                matColor.Y = 1.0f;
            if ((matIndex & 1) != 0)
                matColor.Z = 1.0f;
            return (matColor);
        }
        /// <summary>
        /// This is worth some comments.
        /// The PositionIndices are a mapping from the indices in a GeometryContent
        /// to the Positions in its parent MeshContent.
        /// Note that the GeometryContent also has Positions, but these are extracted
        /// from the MeshContent automatically using the PositionIndices.
        /// All other Channels are directly indexed by the Indices.
        /// So:
        /// pos = MeshContent.Positions[GeoCont.Verts.PosIndices[GeoCont.Indices[i]]]
        /// but
        /// pos = GeoCont.Verts.Positions[GeoCont.Indices[i]]
        /// 
        /// When we add some position indices to the GeoCont.Vertices, the positions
        /// are automatically set, because we are giving PositionIndices which map
        /// into the parent MeshContent. This assumes the positions in question are
        /// already in the MeshContent.Positions (which is the case here, because src
        /// and dst are both children of the same MeshContent).
        /// The additional channels are initialized to some unspecified default values,
        /// but they are there now, so all we need to do is copy them from src into dst.
        /// Since they are directly indexed by Indices, we know that their indices
        /// are simply offset by the number of verts that were in dst before we added
        /// in src. Similarly, we add in the src Indices offset by dst's original
        /// number of verts.
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="src"></param>
        private static void AddGeometry(GeometryContent dst, GeometryContent src)
        {
            int baseIndex = dst.Vertices.VertexCount;

            int[] posindices = new int[src.Vertices.VertexCount];
            for (int i = 0; i < src.Vertices.VertexCount; ++i)
            {
                posindices[i] = src.Vertices.PositionIndices[i];
            }
            dst.Vertices.AddRange(posindices);

            /// Append src channel data to dst
            for (int iChan = 0; iChan < src.Vertices.Channels.Count; ++iChan)
            {
                VertexChannel srcChannel = src.Vertices.Channels[iChan];
                VertexChannel dstChannel = dst.Vertices.Channels[iChan];
                for (int i = 0; i < src.Vertices.VertexCount; ++i)
                {
                    dstChannel[i + baseIndex] = srcChannel[i];
                }
            }

            for (int i = 0; i < src.Indices.Count; ++i)
            {
                dst.Indices.Add(src.Indices[i] + baseIndex);
            }

            /// Copy over any opaqueData
            foreach (KeyValuePair<string, object> k in src.OpaqueData)
            {
                if (!dst.OpaqueData.ContainsKey(k.Key))
                    dst.OpaqueData.Add(k.Key, k.Value);
            }
        }
        private static void Collapse(MeshContent mesh)
        {

            /// Identify how many geometries we'll need.
            Dictionary<string, GeometryContent> geoDict = new Dictionary<string, GeometryContent>();

            foreach (GeometryContent meshPart in mesh.Geometry)
            {
                string groupName = GroupName(meshPart);
                if (!geoDict.ContainsKey(groupName))
                {
                    meshPart.Name = groupName;
                    meshPart.Material.Name = groupName;
                    geoDict.Add(groupName, meshPart);
                }
                else
                {
                    GeometryContent dst = geoDict[groupName];

                    AddGeometry(dst, meshPart);
                }
            }
            mesh.Geometry.Clear();
            Dictionary<string, GeometryContent>.ValueCollection geos = geoDict.Values;
            foreach (GeometryContent geo in geos)
            {
                mesh.Geometry.Add(geo);
            }

        }
        private static void ColorMaterials(MeshContent mesh)
        {
            foreach (GeometryContent meshPart in mesh.Geometry)
            {
                Vector4 constColor = MatColor(meshPart);

                bool foundColor = false;
                foreach (VertexChannel channel in meshPart.Vertices.Channels)
                {
                    if (channel.Name == VertexChannelNames.Color(0))
                    {
                        foundColor = true;
                        VertexChannel<Vector4> colorChannel = (VertexChannel<Vector4>)channel;
                        for (int i = 0; i < colorChannel.Count; ++i)
                        {
                            colorChannel[i] = constColor;
                        }
                    }
                }
                if (!foundColor)
                {
                    int cnt = meshPart.Vertices.VertexCount;
                    Vector4[] colors = new Vector4[cnt];
                    for (int i = 0; i < cnt; ++i)
                        colors[i] = constColor;

                    meshPart.Vertices.Channels.Add<Vector4>(
                        VertexChannelNames.Color(0).ToString(), colors);
                }
            }
        }
        /// <summary>
        /// Make sure all of the geometry contains a vertex color channel.
        /// </summary>
        /// <param name="mesh"></param>
        private static void EnsureVertexColors(MeshContent mesh)
        {
            foreach (GeometryContent meshPart in mesh.Geometry)
            {
                //if ((meshPart.Name != null) && !meshPart.Name.StartsWith("SCP_"))
                {
                    bool foundColor = false;
                    foreach (VertexChannel channel in meshPart.Vertices.Channels)
                    {
                        if (channel.Name == VertexChannelNames.Color(0))
                        {
                            foundColor = true;
                            break;
                        }
                    }
                    if (!foundColor)
                    {
                        int cnt = meshPart.Vertices.VertexCount;
                        Vector4[] colors = new Vector4[cnt];
                        for (int i = 0; i < cnt; ++i)
                            colors[i] = Vector4.One;

                        meshPart.Vertices.Channels.Add<Vector4>(
                            VertexChannelNames.Color(0).ToString(), colors);
                    }
                }
            }
        }
        #endregion Internal
    }
}
