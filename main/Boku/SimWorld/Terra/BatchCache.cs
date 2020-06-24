
#region Debug Defines
//#define MF_SKIPSOME       // Drop a single batch from each render, to see what the batch breakdown is.
//#define MF_RENDERSOLO     // Don't use the bounds tree, just render the list.
//#define MF_RENDERBOX      // Render unclipped interior nodes as green boxes (skipping lower nodes) and clipped nodes as red
//#define MF_HOLDFRUSTUM    // Capture the current frustum for culling from different vantage points.
//#define MF_KEYHACK
#endregion Debug Defines

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#if MF_KEYHACK
using Microsoft.Xna.Framework.Input; // for debug/perf hackery
#endif // MF_KEYHACK

using KoiX;

using Boku.Common;

namespace Boku.SimWorld.Terra
{
    
    /// <summary>
    /// System for collapsing many small batches into larger spatially organized batches.
    /// </summary>
    public class BatchCache
    {
        #region ChildClasses
        /// <summary>
        /// Container for a single draw batch.
        /// </summary>
        private class Batch
        {
            #region Members
            private BoundVBuf data;
            private int numVerts;
            private int numTris;
            #endregion Members

            #region Accessors
            /// <summary>
            /// Bounding box for the geometry in this batch.
            /// </summary>
            public AABB Bounds
            {
                get { return data.Bounds; }
            }
            /// <summary>
            /// The vertex buffer.
            /// </summary>
            public VertexBuffer Buffer
            {
                get { return data.Buffer; }
            }
            /// <summary>
            /// Number of vertices in the batch.
            /// </summary>
            public int NumVerts
            {
                get { return numVerts; }
            }
            /// <summary>
            /// Number of triangles in the batch.
            /// </summary>
            public int NumTris
            {
                get { return numTris; }
            }
            #endregion Accessors

            #region Public
            /// <summary>
            /// Create a wrapper for a drawprim batch.
            /// </summary>
            /// <param name="src"></param>
            public Batch(BoundVBuf src)
            {
                this.data = src;

                numVerts = src.Buffer.VertexCount;
                Debug.Assert((numVerts & 0x3) == 0);
                int numQuads = numVerts / 4;
                numTris = numQuads * 2;
            }

            /// <summary>
            /// Dispose of device content.
            /// </summary>
            public void Dispose()
            {
                data.Dispose();
            }

            /// <summary>
            /// Render this batch of geometry. No renderstate setup, just throw triangles at the card.
            /// </summary>
            /// <param name="camera"></param>
            public void Render(Camera camera)
            {
#if MF_RENDERSOLO
                Frustum.CullResult cull = camera.Frustum.CullTest(Bounds);
                if (cull != Frustum.CullResult.TotallyOutside)
#endif // !MF_RENDERSOLO
                {
                    GraphicsDevice device = KoiLibrary.GraphicsDevice;

                    device.SetVertexBuffer(data.Buffer);

#if MF_KEYHACK
                    if (!KeyboardInputX.IsPressed(Keys.M)
                        || !keyState.IsKeyDown(Keys.N))
                    {
#endif // MF_KEYHACK

                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                            0, 0, NumVerts,
                            0, NumTris);

#if MF_KEYHACK
                    }
#endif // MF_KEYHACK
                }
            }
            #endregion Public

            #region Internal
            #endregion Internal
        }
        /// <summary>
        /// Struct to hold bounds and buffer data for processing.
        /// </summary>
        private struct BoundVBuf
        {
            #region Members
            private AABB bounds;
            private VertexBuffer buffer;
            private int stride;
            #endregion Members

            #region Accessors
            /// <summary>
            /// Bounding box for this buffer.
            /// </summary>
            public AABB Bounds
            {
                get { return bounds; }
            }
            /// <summary>
            /// The vertex data in buffer format.
            /// </summary>
            public VertexBuffer Buffer
            {
                get { return buffer; }
                private set { buffer = value; }
            }
            /// <summary>
            /// Stride of the data.
            /// </summary>
            public int Stride
            {
                get { return stride; }
                private set { stride = value; }
            }
            #endregion Accessors

            #region Public
            /// <summary>
            /// Constructor sets up guts, not expected to change ever.
            /// </summary>
            /// <param name="bounds"></param>
            /// <param name="vbuf"></param>
            /// <param name="stride"></param>
            public BoundVBuf(AABB bounds, VertexBuffer vbuf, int stride)
            {
                this.bounds = new AABB(bounds);
                this.buffer = vbuf;
                this.stride = stride;
            }

            /// <summary>
            /// Dispose of device dependent data.
            /// </summary>
            public void Dispose()
            {
                DeviceResetX.Release(ref buffer);
            }
            #endregion Public
        }
        /// <summary>
        /// Node in the hierarchical bounds tree.
        /// </summary>
        private class Node
        {
            /// <summary>
            /// No accessors, this is just a struct. 
            /// Actually, it's a class now, but it used to be a struct.
            /// It's really still a struct, we just need it to be a ref type
            /// so we can update Cull while it's in the List<>. - mf
            /// </summary>
            #region Members
            
            /// <summary>
            /// Index into the batch list of the owning BatchCache.
            /// </summary>
            private int Batch;

            /// <summary>
            /// Bounds min in world space
            /// </summary>
            private Vector3 Min;
            /// <summary>
            /// Bounds max in world space
            /// </summary>
            private Vector3 Max;

            /// <summary>
            /// Left child, -1 for no child.
            /// </summary>
            public Int16 Left;
            /// <summary>
            /// Right child, -1 for no child.
            /// </summary>
            public Int16 Right;

            /// <summary>
            /// Cull result cached from PreCull.
            /// </summary>
            public Frustum.CullResult Cull;
            #endregion Members

            #region Public
            /// <summary>
            /// Empty constructor, mostly because this used to be a struct.
            /// </summary>
            public Node()
            {
            }
            /// <summary>
            /// Set up an interior node.
            /// </summary>
            /// <param name="left"></param>
            /// <param name="right"></param>
            /// <param name="bounds"></param>
            public Node(Int16 left, Int16 right, AABB bounds)
            {
                this.Left = left;
                this.Right = right;
                this.Batch = -1;
                this.Min = bounds.Min;
                this.Max = bounds.Max;
                this.Cull = Frustum.CullResult.TotallyInside;
            }

            /// <summary>
            /// Set up a leaf node.
            /// </summary>
            /// <param name="batch"></param>
            /// <param name="bounds"></param>
            public Node(Int16 batch, AABB bounds)
            {
                this.Left = -1;
                this.Right = -1;
                this.Batch = batch;
                this.Min = bounds.Min;
                this.Max = bounds.Max;
                this.Cull = Frustum.CullResult.TotallyInside;
            }

            /// <summary>
            /// Hierarchical cull check, with result cached. This is just
            /// so we don't have to recheck for each face pass.
            /// </summary>
            /// <param name="camera"></param>
            /// <param name="nodes"></param>
            /// <returns></returns>
            public Frustum.CullResult PreCull(Camera camera, List<Node> nodes)
            {
#if MF_HOLDFRUSTUM
                GamePadInput pad = GamePadInput.GetGamePad0();
                if (pad.ButtonY.IsPressed)
                {
                    _holdFrustum = camera.Frustum.Clone();
                }
                else if (pad.ButtonX.IsPressed)
                {
                    _holdFrustum = null;
                }
                Frustum frustum = _holdFrustum == null ? camera.Frustum : _holdFrustum;
#else // MF_HOLDFRUSTUM
                Frustum frustum = camera.Frustum;
#endif // MF_HOLDFRUSTUM
                
                Cull = frustum.CullTest(Min, Max);

                if (Cull == Frustum.CullResult.PartiallyInside)
                {
                    if (Left >= 0)
                    {
                        Debug.Assert(Right >= 0);
                        nodes[Left].PreCull(camera, nodes);
                        nodes[Right].PreCull(camera, nodes);
                    }
                }
                return Cull;
            }

            /// <summary>
            /// Check the cached results of PreCull cull check, and render (or not)
            /// and pass on to children (or not) accordingly.
            /// </summary>
            /// <param name="camera"></param>
            /// <param name="batches"></param>
            /// <param name="nodes"></param>
            public void PreCullRender(Camera camera, List<Batch> batches, List<Node> nodes)
            {
                if (Cull == Frustum.CullResult.TotallyInside)
                {
#if MF_RENDERBOX
                    RenderBox(camera, Color.Green);
#else // MF_RENDERBOX
                    Render(camera, batches, nodes);
#endif // MF_RENDERBOX
                }
                else if (Cull == Frustum.CullResult.PartiallyInside)
                {
                    if (Batch >= 0)
                    {
#if MF_RENDERBOX
                        RenderBox(camera, Color.Red);
#else // MF_RENDERBOX
                        batches[Batch].Render(camera);
#endif // MF_RENDERBOX
                    }
                    else
                    {
                        Debug.Assert((Left >= 0) && (Right >= 0));
                        nodes[Left].PreCullRender(camera, batches, nodes);
                        nodes[Right].PreCullRender(camera, batches, nodes);
                    }
                }
            }

#if MF_HOLDFRUSTUM
            /// <summary>
            /// Debug save of a frustum to look at culling from different vantage points.
            /// </summary>
            private static Frustum _holdFrustum = null;
#endif // MF_HOLDFRUSTUM

            /// <summary> 
            /// Recursive render down to batches without cull checking.
            /// </summary>
            /// <param name="camera"></param>
            /// <param name="batches"></param>
            /// <param name="nodes"></param>
            public void Render(Camera camera, List<Batch> batches, List<Node> nodes)
            {
                if (Left >= 0)
                {
                    nodes[Left].Render(camera, batches, nodes);
                }
                if (Right >= 0)
                {
                    nodes[Right].Render(camera, batches, nodes);
                }

                if (Batch >= 0)
                {
#if MF_SKIPSOME
                    if (Batch != BatchCache.currentSkip)
#endif // MF_SKIPSOME
                    {
                        batches[Batch].Render(camera);
                    }
                }
            }
            /// <summary>
            /// Render the bounding box in outline. Debug only.
            /// </summary>
            /// <param name="camera"></param>
            /// <param name="color"></param>
            public void RenderBox(Camera camera, Color color)
            {
                for( int i = 0; i < 8; ++i)
                {
                    _pts[i] = new Vector3(
                        (i & 1) != 0 ? Max.X : Min.X,
                        (i & 2) != 0 ? Max.Y : Min.Y,
                        (i & 4) != 0 ? Max.Z : Min.Z);

                }
                Utils.DrawLine(camera, _pts[0], _pts[1], color);
                Utils.DrawLine(camera, _pts[2], _pts[3], color);
                Utils.DrawLine(camera, _pts[0], _pts[2], color);
                Utils.DrawLine(camera, _pts[1], _pts[3], color);

                Utils.DrawLine(camera, _pts[4], _pts[5], color);
                Utils.DrawLine(camera, _pts[6], _pts[7], color);
                Utils.DrawLine(camera, _pts[4], _pts[6], color);
                Utils.DrawLine(camera, _pts[5], _pts[7], color);

                Utils.DrawLine(camera, _pts[0], _pts[4], color);
                Utils.DrawLine(camera, _pts[1], _pts[5], color);
                Utils.DrawLine(camera, _pts[2], _pts[6], color);
                Utils.DrawLine(camera, _pts[3], _pts[7], color);

                for (int i = 0; i < 8; ++i)
                {
                    Utils.DrawLine(camera, _pts[i], new Vector3(_pts[i].X, _pts[i].Y, _pts[i].Z + 0.5f), Color.Yellow);
                }
            }
            /// <summary>
            /// Internal vector to avoid churning the GC.
            /// </summary>
            private static Vector3[] _pts = new Vector3[8];
            #endregion Public
        }
        #endregion ChildClasses

        #region Members
        /// <summary>
        /// List of all batches we own.
        /// </summary>
        List<Batch> batches = new List<Batch>();

        /// <summary>
        /// List of all nodes in our bounds tree. nodes[0] is root.
        /// </summary>
        List<Node> nodes = new List<Node>();

        /// <summary>
        /// Scratch space for accumulating data to process.
        /// </summary>
        private List<BoundVBuf> _bufferScratch = new List<BoundVBuf>();

        /// <summary>
        /// Stride of the buffer data.
        /// </summary>
        private int stride;

        /// <summary>
        /// Cap on number of verts in a single batch.
        /// </summary>
        const int kMaxCount = 64 * 64 * 4 * 2; // Larger kMaxCount is ok for PCs and it may be needed by the FewerDraws render method
                                               // which uses more vertices

        private static IndexBuffer[] IndexBuffers;
        private static int NumBuffers { get { return IndexPatterns == null ? 0 : IndexPatterns.Length; } }
        private static byte[][] IndexPatterns;

        #endregion Members

        #region Accessors
        /// <summary>
        /// List of batches we have processed.
        /// </summary>
        private List<Batch> Batches
        {
            get { return batches; }
        }

        /// <summary>
        /// Stride of our buffer data.
        /// </summary>
        private int Stride
        {
            get { return stride; }
            set { stride = value; }
        }

        /// <summary>
        /// True if we have no data to render.
        /// </summary>
        public bool Empty
        {
            get { return Batches.Count == 0; }
        }
        #endregion Accessors

        #region Public
        /// <summary>
        /// Blank constructor, these are meant to be reused.
        /// </summary>
        public BatchCache()
        {
        }

        /// <summary>
        /// Prepare to accept data for processing.
        /// </summary>
        /// <param name="indexPatterns">
        /// The order in which indices are added to the index buffers. Must 
        /// have element length equal to 4. Note that the number of index
        /// patterns passed will determine the number of index buffers created
        /// (one per pattern).
        /// </param>
        public void Init(int stride, params byte[][] indexPatterns)
        {
#if DEBUG
            foreach (var pattern in indexPatterns)
            {
                Debug.Assert(pattern.Length == 4);
                //ToDo (DZ): Consider creating a IndexPattern struct. This
                // may make the indexPatterns argument more readable and
                // since the index pattern should always be of length 4 a
                // dynamic size array is unnecessary.
            }
#endif
            Stride = stride;
            IndexPatterns = indexPatterns;
            IndexBuffers = new IndexBuffer[NumBuffers];

            _bufferScratch.Clear();
            nodes.Clear();
            Batches.Clear();
        }

        /// <summary>
        /// Add more data to process.
        /// </summary>
        /// <param name="bound"></param>
        /// <param name="vbuf"></param>
        public void Add(AABB bound, VertexBuffer vbuf)
        {
            _bufferScratch.Add(new BoundVBuf(bound, vbuf, Stride));
        }

        /// <summary>
        /// All data has been added, do the processing.
        /// </summary>
        /// <returns></returns>
        public bool Finish<VertexType>() where VertexType : struct
        {
            Partition<VertexType>(0, _bufferScratch.Count - 1);

            _bufferScratch.Clear();

            return Batches.Count > 0;
        }

        /// <summary>
        /// Discard all buffer data.
        /// </summary>
        public void Dispose()
        {
            foreach (Batch batch in Batches)
            {
                batch.Dispose();
            }
            Batches.Clear();
            nodes.Clear();
        }

        /// <summary>
        /// Do pre-render pass for cull checking, so we don't have to cull check
        /// for each face.
        /// </summary>
        /// <param name="camera"></param>
        public void PreCull(Camera camera)
        {
            Debug.Assert(nodes.Count > 0);
            nodes[0].PreCull(camera, nodes);
        }

#if MF_SKIPSOME
        private double nextInc = 0;
        private int skip = 0;
        internal static int currentSkip = 0;
#endif // MF_SKIPSOME
        /// <summary>
        /// Render the given face for all batches.
        /// </summary>
        public void Render(Camera camera, int buffer)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;
            device.Indices = IndexBuffers[buffer];

#if MF_SKIPSOME
            if (Time.WallClockTotalSeconds > nextInc)
            {
                ++skip;
                skip = skip % Batches.Count;
                nextInc = Time.WallClockTotalSeconds + 1.0f;
            }
            currentSkip = skip;
#endif // MF_SKIPSOME

#if MF_RENDERSOLO
            for(int i = 0; i < Batches.Count; ++i)
            {
                Batch batch = Batches[i];
#if MF_SKIPSOME
                if (i != skip)
#endif // MF_SKIPSOME
                {
                    batch.Render(camera);
                }
            }
#else // MF_RENDERSOLO
            Debug.Assert(nodes.Count > 0);
            nodes[0].PreCullRender(camera, Batches, nodes);
#endif // MF_RENDERSOLO

        }

        public static void Unload()
        {
            for (int i = 0; i < NumBuffers; i++)
            {
                if (IndexBuffers[i] != null)
                    DeviceResetX.Release(ref IndexBuffers[i]);
            }
        }

        #endregion Public

        #region Internal
        /// <summary>
        /// If our segment is small enough, wrap it in a batch.
        /// Else, subdivide recursively.
        /// </summary>
        private Int16 Partition<VertexType>(int lo, int hi) where VertexType : struct
        {
            Debug.Assert((lo >= 0) && (lo <= hi) && (hi < _bufferScratch.Count));

            /// If the total bytes is small enough, we're done, create a batch
            int sumBytes = SumBytes(lo, hi);
            if (sumBytes <= kMaxCount * Stride)
            {
                Create<VertexType>(sumBytes, lo, hi);
                Int16 myBatch = (Int16)(Batches.Count - 1);
                Node node = new Node(myBatch, new AABB(Batches[myBatch].Bounds));
                nodes.Add(node);
                return (Int16)(nodes.Count - 1);
            }

            Debug.Assert(lo < hi, "Only one that's too big?");

            /// Pick a sorting direction and sort
            AABB totalBounds = SortRange(lo, hi);
            
            /// Split near as possible to median by bytes
            int middle = FindMedian(totalBounds, sumBytes, lo, hi);
            Debug.Assert((middle >= lo) && (middle <= hi));

            Node group = new Node();

            Int16 myNode = (Int16)nodes.Count;
            AABB myBounds = new AABB(totalBounds);
            nodes.Add(group);

            /// Recurse
            group.Left = Partition<VertexType>(lo, middle);
            group.Right = Partition<VertexType>(middle + 1, hi);
            nodes[myNode] = new Node(group.Left, group.Right, myBounds);

             //we should be able to assert here that the children bounds have 
             //an aspect ratio 0.5 <= ratio <= 2.0 

            return myNode;
        }
        /// <summary>
        /// Decide which dimension to sort by and do it.
        /// </summary>
        /// <param name="lo"></param>
        /// <param name="hi"></param>
        /// <returns></returns>
        private AABB SortRange(int lo, int hi)
        {
            _boundsScratch.Set(_bufferScratch[lo].Bounds);
            for (int i = lo + 1; i <= hi; ++i)
            {
                _boundsScratch.Union(_bufferScratch[i].Bounds);
            }
            if (SortOnX(_boundsScratch))
            {
                _bufferScratch.Sort(lo, hi - lo + 1, _XCompare);
            }
            else
            {
                _bufferScratch.Sort(lo, hi - lo + 1, _YCompare);
            }
            return _boundsScratch;
        }
        /// <summary>
        /// True if data should be sorted on X, else data should be sorted on Y.
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        private bool SortOnX(AABB bounds)
        {
            return bounds.Max.X - bounds.Min.X > bounds.Max.Y - bounds.Min.Y;
        }
        ///
        /// Old FindMedian function, deprecated.
        ///
        //private int FindMedian(AABB totalBounds, int totalSz, int lo, int hi)
        //{
        //    int halfSz = totalSz / 2;

        //    int sz = 0;
        //    for (int i = lo; i <= hi; ++i)
        //    {
        //        if (sz + _bufferScratch[i].Buffer.SizeInBytes > halfSz)
        //        {
        //            int szBelow = halfSz - sz;
        //            int szAbove = sz + _bufferScratch[i].Buffer.SizeInBytes - halfSz;

        //            return szBelow > szAbove
        //                ? i - 1
        //                : i;
        //        }
        //        sz += _bufferScratch[i].Buffer.SizeInBytes;
        //    }
        //    Debug.Assert(false, "There should have been some median in there");
        //    return (lo + hi) / 2;
        //}
        /// <summary>
        /// Look for the division point within this range.
        /// </summary>
        /// <param name="totalBounds"></param>
        /// <param name="totalSz"></param>
        /// <param name="lo"></param>
        /// <param name="hi"></param>
        /// <returns></returns>
        private int FindMedian(AABB totalBounds, int totalSz, int lo, int hi)
        {
            if (SortOnX(totalBounds))
                return FindMedianX(totalBounds, totalSz, lo, hi);

            return FindMedianY(totalBounds, totalSz, lo, hi);
        }
        /// <summary>
        /// Look for a division point along the X axis.
        /// </summary>
        /// <param name="totalBounds"></param>
        /// <param name="totalSz"></param>
        /// <param name="lo"></param>
        /// <param name="hi"></param>
        /// <returns></returns>
        private int FindMedianX(AABB totalBounds, int totalSz, int lo, int hi)
        {
            int halfSz = totalSz / 2;

            float cutoff = (totalBounds.Max.X + totalBounds.Min.X) * 0.5f;
            for (int i = lo; i <= hi; ++i)
            {
                float center = CenterX(_bufferScratch[i]);
                if (center > cutoff)
                {
                    Debug.Assert(i <= hi);
                    return i - 1;
                }
            }
            Debug.Assert(false, "There should have been some median in there");
            return hi - 1;
        }
        /// <summary>
        /// Look for a division point along the Y axis.
        /// </summary>
        /// <param name="totalBounds"></param>
        /// <param name="totalSz"></param>
        /// <param name="lo"></param>
        /// <param name="hi"></param>
        /// <returns></returns>
        private int FindMedianY(AABB totalBounds, int totalSz, int lo, int hi)
        {
            int halfSz = totalSz / 2;

            float cutoff = (totalBounds.Max.Y + totalBounds.Min.Y) * 0.5f;
            for (int i = lo; i <= hi; ++i)
            {
                float center = CenterY(_bufferScratch[i]);
                if (center > cutoff)
                {
                    Debug.Assert(i <= hi);
                    return i - 1;
                }
            }
            Debug.Assert(false, "There should have been some median in there");
            return hi - 1;
        }
        /// <summary>
        /// Return the center position along X for the given buffer.
        /// </summary>
        /// <param name="buf"></param>
        /// <returns></returns>
        private static float CenterX(BoundVBuf buf)
        {
            return (buf.Bounds.Max.X + buf.Bounds.Min.X) * 0.5f;
        }
        /// <summary>
        /// Compare two buffers by center position along X axis.
        /// </summary>
        private class CompareByX : IComparer<BoundVBuf>
        {
            public int Compare(BoundVBuf lhs, BoundVBuf rhs)
            {
                float lhsCenter = CenterX(lhs);
                float rhsCenter = CenterX(rhs);
                if (lhsCenter < rhsCenter)
                    return -1;
                if (lhsCenter > rhsCenter)
                    return 1;
                return 0;
            }
        };
        /// <summary>
        /// Return the center position along Y for the given buffer.
        /// </summary>
        /// <param name="buf"></param>
        /// <returns></returns>
        private static float CenterY(BoundVBuf buf)
        {
            return (buf.Bounds.Max.Y + buf.Bounds.Min.Y) * 0.5f;
        }
        /// <summary>
        /// Compare two buffers by center position along Y axis.
        /// </summary>
        private class CompareByY : IComparer<BoundVBuf>
        {
            public int Compare(BoundVBuf lhs, BoundVBuf rhs)
            {
                float lhsCenter = CenterY(lhs);
                float rhsCenter = CenterY(rhs);
                if (lhsCenter < rhsCenter)
                    return -1;
                if (lhsCenter > rhsCenter)
                    return 1;
                return 0;
            }
        }
        /// <summary>
        /// Static compare function available when needed.
        /// </summary>
        private static CompareByX _XCompare = new CompareByX();
        /// <summary>
        /// Static compare function available when needed.
        /// </summary>
        private static CompareByY _YCompare = new CompareByY();
        /// <summary>
        /// Static bounds for aggregating.
        /// </summary>
        private static AABB _boundsScratch = new AABB();

        // TODO (scoy) Do we really need to know the number of bytes in the buffer?
        // Dig into this and see if it can be simplified.

        /// <summary>
        /// Add up the number of bytes within all buffers in range.
        /// </summary>
        /// <param name="lo"></param>
        /// <param name="hi"></param>
        /// <returns></returns>
        private int SumBytes(int lo, int hi)
        {
            int sz = 0;
            for (int i = lo; i <= hi; ++i)
            {
                sz += _bufferScratch[i].Buffer.VertexCount * _bufferScratch[i].Buffer.VertexDeclaration.VertexStride;
            }
            return sz;
        }

        /// <summary>
        /// Aggregate the input range into a single batch.
        /// </summary>
        /// <param name="totalSz"></param>
        /// <param name="lo"></param>
        /// <param name="hi"></param>
        private void Create<VertexType>(int totalSz, int lo, int hi) where VertexType : struct
        {
            if (totalSz > 0)
            {
                int totalNumVerts = BytesToCount(totalSz);

                var localVerts = CheckLocalVerts<VertexType>(totalNumVerts);

                int accumSz = 0;
                int iVerts = 0;
                _boundsScratch.Set(_bufferScratch[lo].Bounds);
                for (int i = lo; i <= hi; ++i)
                {
                    _boundsScratch.Union(_bufferScratch[i].Bounds);

                    int numVerts = _bufferScratch[i].Buffer.VertexCount;

                    _bufferScratch[i].Buffer.GetData<VertexType>(localVerts, iVerts, numVerts);

                    iVerts += numVerts;

                    // TODO (scoy) Do we really need this in bytes?  Can we simplify?
                    accumSz += _bufferScratch[i].Buffer.VertexCount * _bufferScratch[i].Buffer.VertexDeclaration.VertexStride;

                    _bufferScratch[i].Dispose();
                }

                Debug.Assert(accumSz == totalSz);

                GraphicsDevice device = KoiLibrary.GraphicsDevice;
                VertexBuffer vbuf = new VertexBuffer(device, typeof(VertexType), totalNumVerts, BufferUsage.WriteOnly);
                vbuf.SetData<VertexType>(localVerts, 0, totalNumVerts);

                Batch batch = new Batch(new BoundVBuf(_boundsScratch, vbuf, Stride));

                Batches.Add(batch);

                CheckIndexBuffer(totalNumVerts);
            }
        }
        private VertexType[] CheckLocalVerts<VertexType>(int totalNumVerts) where VertexType : struct
        {
            if ((!(_localVertsScratch is VertexType[]))
                || (_localVertsScratch.Length < totalNumVerts))
            {
                _localVertsScratch = new VertexType[totalNumVerts];
            }
            return (VertexType[])_localVertsScratch;
        }
        private static Array _localVertsScratch = null;
        /// <summary>
        /// Make sure our static shared index buffers are big enough.
        /// </summary>
        /// <param name="numVerts"></param>
        private void CheckIndexBuffer(int numVerts)
        {
            Debug.Assert((numVerts & 0x3) == 0, "numVerts should be multiple of 4");

            int numQuads = numVerts / 4;
            int numTris = numQuads * 2;
            int numIndices = numTris * 3;

            for (int i = 0; i < NumBuffers; i++)
            {
                var iBuffer = IndexBuffers[i];
                if (iBuffer == null || (numIndices > iBuffer.IndexCount))
                {
                    DeviceResetX.Release(ref iBuffer);

                    var device = KoiLibrary.GraphicsDevice;

                    var indices = new UInt16[numIndices];
                    var iP = IndexPatterns[i];
                    FillIndices(numQuads, indices, iP[0], iP[1], iP[2], iP[3]);
                    iBuffer = new IndexBuffer(device, IndexElementSize.SixteenBits, numIndices, BufferUsage.WriteOnly);
                    iBuffer.SetData(indices);
                    IndexBuffers[i] = iBuffer;
                }
            }
        }

        /// <summary>
        /// Fill in our shared index buffers.
        /// </summary>
        /// <param name="numQuads"></param>
        /// <param name="indices"></param>
        /// <param name="idx0"></param>
        /// <param name="idx1"></param>
        /// <param name="idx2"></param>
        /// <param name="idx3"></param>
        private void FillIndices(int numQuads, UInt16[] indices, int idx0, int idx1, int idx2, int idx3)
        {
            int idxBase = 0;
            int vertBase = 0;
            for (int i = 0; i < numQuads; ++i)
            {
                indices[idxBase + 0] = (UInt16)(vertBase + idx1);
                indices[idxBase + 1] = (UInt16)(vertBase + idx0);
                indices[idxBase + 2] = (UInt16)(vertBase + idx2);

                indices[idxBase + 3] = (UInt16)(vertBase + idx2);
                indices[idxBase + 4] = (UInt16)(vertBase + idx0);
                indices[idxBase + 5] = (UInt16)(vertBase + idx3);

                idxBase += 6;
                vertBase += 4;
            }
        }

        /// <summary>
        /// Convert the byte count into a number of vertices.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private int BytesToCount(int bytes)
        {
            return bytes / Stride;
        }
        #endregion Internal
    }
    
}