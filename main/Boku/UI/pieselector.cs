
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.Fx;
using Boku.UI;
using Boku.Programming;
using Boku.Input;
using Boku.SimWorld;
using Boku.SimWorld.Path;


namespace Boku.UI
{
    public class PieSelector : UiSelector, ITransform
    {
        protected class UpdateObj : UpdateObject
        {
            private PieSelector parent = null;
            private CommandMap commandMap;

            public UpdateObj(PieSelector parent, string uiMode)
            {
                this.parent = parent;

                commandMap = new CommandMap(@"PieSelector");
                commandMap.name = @"PieSelector " + uiMode;
            }

            public bool OwnsFocus()
            {
                return commandMap == CommandStack.Peek();
            }

            public override void Update()
            {
                if (OwnsFocus())
                {
                    GamePadInput pad = GamePadInput.GetGamePad0();

                    if (Actions.Select.WasPressed)
                    {
                        Actions.Select.ClearAllWasPressedState();
                        pad.IgnoreLeftStickUntilZero();

                        // TODO (****) Get rid of the args...
                        parent.OnSelect(null, null);
                    }

                    if (Actions.Cancel.WasPressed)
                    {
                        Actions.Cancel.ClearAllWasPressedState();

                        // TODO (****) Get rid of the args...
                        parent.OnCancel(null, null);
                    }

                    // Check if the currently selected item has help available.
                    // If it doesn't, suppress the help overlay text saying that
                    // help is available.  Note this also makes for a nice place
                    // to set a breakpoint to find all those things we still
                    // need to create help for.
                    Editor editor = InGame.inGame.Editor;
                    bool helpAvailable = false;
                    string objectHelpName = null;   // Internal name used to identify object in help system.
                    string objectName = null;       // Display name of the focus object.
                    if (editor.Active)
                    {
                        helpAvailable = true;
                    }
                    else
                    {
                        // else in the AddItem menu.
                        if(parent.SelectedItem != null)
                        {
                            ActorMenuItem item = parent.SelectedItem.item as ActorMenuItem;
                            if (item != null)
                            {
                                if (item.StaticActor != null)
                                {
                                    objectHelpName = item.StaticActor.NonLocalizedName;
                                    objectName = item.Name;
                                    helpAvailable = true;
                                }
                                else if(item.Name == "plain")
                                {
                                    objectHelpName = "PathGeneric";
                                    objectName = Strings.Localize("actorNames.pathGeneric");
                                    helpAvailable = true;                                 
                                }
                                else if (item.Name == "flora")
                                {
                                    objectHelpName = "PathVeggie";
                                    objectName = Strings.Localize("actorNames.pathVeggie");
                                    helpAvailable = true;
                                }
                                else if (item.Name == "wall")
                                {
                                    objectHelpName = "PathWall";
                                    objectName = Strings.Localize("actorNames.pathWall");
                                    helpAvailable = true;
                                }
                                else if (item.Name == "road")
                                {
                                    objectHelpName = "PathRoad";
                                    objectName = Strings.Localize("actorNames.pathRoad");
                                    helpAvailable = true;
                                }

                                // Don't bother since we have tooltips...
                                //item.DisplayHelpButton = helpAvailable;
                                item.DisplayHelpButton = false;

                                ITransform trans = parent as ITransform;
                                if (trans != null)
                                {
                                    item.PieCenter = trans.World.Translation;
                                }
                                else
                                {
                                    item.PieCenter = Vector3.Zero;
                                }
                            }

                        }
                    }
                    HelpOverlay.SuppressYButton = !helpAvailable;


                    // ToolTip?  But not for the AddItemMenu.
                    if (parent.SelectedItem != null)
                    {
                        string name = objectName;
                        string desc = null;

                        ITransform item = parent.ObjectSelectedItem as ITransform;
                        if (item != null)
                        {
                            Vector2 viewport = new Vector2(BokuGame.bokuGame.GraphicsDevice.Viewport.Width, BokuGame.bokuGame.GraphicsDevice.Viewport.Height);

                            UiCamera camera = InGame.inGame.Editor.Camera;
                            Vector3 offset = new Vector3(0.5f, -0.5f, 0.0f);
                            Point foo = camera.WorldToScreenCoords(item.World.Translation + offset);
                            Vector2 pos = new Vector2(foo.X, foo.Y);

                            bool useAdd = false;

                            if (name == null)
                            {
                                // Programming UI.
                                ProgrammingElement e = parent.SelectedItem.progElement as ProgrammingElement;
                                if (e != null)
                                {
                                    name = e.label;
                                    desc = e.description;
                                }
                                else
                                {
                                    // Nothing else matches, must be a group.
                                    name = Strings.Localize("toolTips.group");
                                }
                            }
                            else
                            {
                                // AddItemMenu
                                // Need to dig a description out of the help system.
                                desc = InGame.inGame.shared.addItemHelpCard.GetHelpDescription(objectHelpName);
                                // Say "Add" instead of "Change" for <A> on tooltip.
                                useAdd = true;
                            }

                            ToolTipManager.ShowTip(name, desc, pos, true, useAdd);
                        }
                    }
                    else
                    {
                        ToolTipManager.ShowTip(null, null, Vector2.Zero, false);
                    }



                    if (Actions.Help.WasPressed)
                    {
                        Actions.Help.ClearAllWasPressedState();

                        // Activate help overlay for current selection.
                        
                        if (editor.Active)
                        {
                            ProgrammingElement focusElement = parent.ParamSelectedItem as ProgrammingElement;
                            InGame.inGame.shared.programmingHelpCard.Activate(focusElement, parent);
                        }
                        else if (objectHelpName != null)
                        {
                            // Must be in add item menu.  Activate help card for bot/object.
                            InGame.inGame.shared.addItemHelpCard.Activate(parent, objectHelpName, objectName);
                        }
                    }

                    // TODO (****) Fold args into function...
                    parent.UpdateSelection(null, new StickEventArgs(pad.LeftStick));

                    // Clear out any other button presses.  This normally shouldn't be needed
                    // but if we're in the programming UI the reflex may think can still safely
                    // steal input.
                    GamePadInput.ClearAllWasPressedState();

                }

                if (parent.WhileHighlit != null)
                    parent.WhileHighlit();

            }

            public override void Activate()
            {
                CommandStack.Push(commandMap);
            }
            public override void Deactivate()
            {
                CommandStack.Pop(commandMap);
                ToolTipManager.Clear();
            }
        }

        public class RenderObj : RenderObject
        {
            private PieSelector parent = null;
            public List<RenderObject> renderList = new List<RenderObject>();

            ITransform transform = null;
            ITransform transformParent = null;
            public float radius;

            public RenderObj(PieSelector parent, ITransform transform, ITransform transformParent)
            {
                this.parent = parent;
                this.transform = transform;
                this.transformParent = transformParent;
            }
            
            public bool ViewTested = false;
            
            protected void ReCenter(Camera camera)
            {
                PieSelector.Camera = camera as UiCamera;

                if (parent.OwnsFocus())
                {
                    float kMargin = 0.5f;   // Screen space margin for pies menus.  This is the default value.
                                            // Below we also take into account the radius of the pie menu so
                                            // that for very large pie menus (ones that fill the whole screen)
                                            // we actually center them rather than pushing them off the bottom.

                    UiCamera c = camera as UiCamera;
                    // Calc up/down offset.
                    float shift = transform.World.Translation.Y - c.At.Y + transform.World.Translation.Z * c.ViewDir.Y;
                    float margin = Math.Min(kMargin, c.Height/2.0f - radius);
                    float offscreenAmount = Math.Abs(shift) + radius - c.Height / 2.0f + margin;
                    if (offscreenAmount > 0.0f)
                    {
                        Vector3 offset = c.Offset;
                        offset.Y = shift > 0.0f ? offscreenAmount : -offscreenAmount;
                        c.Offset = offset;
                    }

                    // Calc side-to-side offset.
                    shift = transform.World.Translation.X - c.At.X + transform.World.Translation.Z * c.ViewDir.X;
                    margin = Math.Min(kMargin, c.Width / 2.0f - radius);
                    offscreenAmount = Math.Abs(shift) + radius - c.Width / 2.0f + margin;
                    if (offscreenAmount > 0.0f)
                    {
                        Vector3 offset = c.Offset;
                        offset.X = shift > 0.0f ? offscreenAmount : -offscreenAmount;
                        c.Offset = offset;
                    }
                }

                if (!this.ViewTested)
                {
                    Vector3 center = Vector3.Zero;
                    center = Vector3.Transform(center, transform.World);
                    if (transformParent == null || center == Vector3.Zero || camera.Frustum.CullTest(new BoundingSphere(center, radius)) == Frustum.CullResult.TotallyInside)
                    {
                        this.ViewTested = true;
                    }
                    else
                    {
                        // move toward our parents center some
                        Vector3 centerParent = Vector3.Zero;
                        centerParent = Vector3.Transform(centerParent, transformParent.World);
                        Vector3 diff = centerParent - center;
                        diff.Z = 0.0f;
                        float length = diff.Length() * (float)Time.WallClockFrameSeconds * 2.0f; // over 300 milliseconds
                        if (diff != Vector3.Zero)
                        {
                            diff.Normalize();
                        }
                        diff *= length;
                        transform.Local.Translation += diff;
                        transform.Compose();
                    }
                }
            }

            public override void Render(Camera camera)
            {
                if (InGame.inGame.Editor.RenderPieMenus)
                {

                    // Don't bother to render if one of the help cards is active.
                    if (!(InGame.inGame.shared.addItemHelpCard.Active || InGame.inGame.shared.programmingHelpCard.Active))
                    {
                        ReCenter(camera);

                        //BokuGame.bokuGame.GraphicsDevice.RasterizerState = UI2D.Shared.RasterStateWireframe;
                        for (int iObject = 0; iObject < renderList.Count; iObject++)
                        {
                            RenderObject obj = renderList[iObject] as RenderObject;
                            obj.Render(camera);
                        }

                    }
                }
                else
                {
                    // If not rendering, just store it away for rendering later.
                    InGame.inGame.Editor.PieMenuList.Add(parent);
                }
            }
            public override void Activate()
            {
                for (int iObject = 0; iObject < renderList.Count; iObject++)
                {
                    RenderObject obj = renderList[iObject] as RenderObject;
                    obj.Activate();
                }
            }
            public override void Deactivate()
            {
                for (int iObject = 0; iObject < renderList.Count; iObject++)
                {
                    RenderObject obj = renderList[iObject] as RenderObject;
                    obj.Deactivate();
                }
            }
        }

        public class IndexPrimitiveSlice : INeedsDeviceReset
        {
            private VertexBuffer vertexBuffer = null;
            private IndexBuffer indexBuffer = null;
            private int triangleCount = 0;
            private int vertexCount = 0;

            private SliceType sliceType;
            private float innerRadius;
            private float outerRadius;
            private float arcLength;

            public struct Vertex : IVertexType
            {
                private Vector3 position;
                private Vector3 normal;
                private Vector2 texture;

                static VertexDeclaration decl = null;
                static VertexElement[] elements = new VertexElement[]
                {
                    new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                    new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                    new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                };

                public Vertex(Vector3 position, Vector3 normal, Vector2 texture)
                {
                    this.position = position;
                    this.normal = normal;
                    this.texture = texture;
                }

                public VertexDeclaration VertexDeclaration
                {
                    get
                    {
                        if (decl == null || decl.IsDisposed)
                        {
                            decl = new VertexDeclaration(elements);
                        }
                        return decl;
                    }
                }

            }

            public IndexPrimitiveSlice( float innerRadius, float outerRadius, float arcLength, SliceType sliceType)
            {
                this.sliceType = sliceType;
                this.innerRadius = innerRadius;
                this.outerRadius = outerRadius;
                this.arcLength = arcLength;
                CreateGeometry(BokuGame.bokuGame.GraphicsDevice);
            }

            protected void CreateGeometry(GraphicsDevice device)
            {
                // Slice of a Ring
                const float thickness = 0.1f;
                int segmentCount = 96 / (int)(MathHelper.TwoPi / this.arcLength);

                // make sure we have an even count at this point so groups have a point
                // and they don't come short on one edge
                if ((segmentCount % 2) == 1)
                {
                    segmentCount++; 
                }

                float arcHalfLength = this.arcLength * 0.5f; // center the slice
                // adjustment for the lower edge if grouped
                float groupExtention = 0.0f;
                if (this.sliceType == SliceType.group)
                {
                    groupExtention = this.innerRadius * 0.35f; // part of the radius
                }

                int radialCount = segmentCount + 1;
                this.triangleCount = segmentCount * 8 + 4; // + edge caps
                this.vertexCount = radialCount * 8 + 8; // + edge caps

                // Init the vertex buffer.
                if (vertexBuffer == null)
                {
                    vertexBuffer = new VertexBuffer(device, typeof(Vertex), this.vertexCount, BufferUsage.WriteOnly);
                }

                // Create local vertices.
                Vertex[] localVerticies = new Vertex[this.vertexCount];

                int index = 0;
                int radialCountOver2 = radialCount / 2;

                // Upper surface.
                Vector3 normalOuter = Vector3.Zero;
                Vector3 normalInner = Vector3.Zero;

                Vector3 normal;
                
                for (int indexSegment = 0; indexSegment < radialCount; indexSegment++)
                {
                    float arc = indexSegment * this.arcLength / segmentCount - arcHalfLength + MathHelper.PiOver2;
                    float sinArc = (float)Math.Sin(arc);
                    float cosArc = (float)Math.Cos(arc);
                    float localExtention = 0;
                    if (indexSegment <= radialCountOver2)
                    {
                        localExtention = groupExtention * (float)indexSegment / (float)radialCountOver2;
                    }
                    else
                    {
                        localExtention = groupExtention - groupExtention * (float)(indexSegment - radialCountOver2) / (float)radialCountOver2;
                    }
                    normalOuter = new Vector3(sinArc, cosArc, 1.0f);
                    normalInner = new Vector3(-sinArc, -cosArc, 1.0f);
                    normalOuter.Normalize();
                    normalInner.Normalize();

                    localVerticies[index++] = new Vertex(new Vector3(sinArc * (outerRadius + localExtention), cosArc * (outerRadius + localExtention), thickness), normalOuter, Vector2.Zero);
//                    localVerticies[index++] = new Vertex(new Vector3(sinArc * outerRadius, cosArc * outerRadius, thickness), normal, Vector2.Zero);
                    localVerticies[index++] = new Vertex(new Vector3(sinArc * innerRadius, cosArc * innerRadius, thickness), normalInner, Vector2.Zero);
                }

                // Lower surface.
                normal = new Vector3(0.0f, 0.0f, -1.0f);
                
                for (int indexSegment = 0; indexSegment < radialCount; indexSegment++)
                {
                    float arc = indexSegment * this.arcLength / segmentCount - arcHalfLength + MathHelper.PiOver2;
                    float sinArc = (float)Math.Sin(arc);
                    float cosArc = (float)Math.Cos(arc);
                    float localExtention = 0;
                    if (indexSegment <= radialCountOver2)
                    {
                        localExtention = groupExtention * (float)indexSegment / (float)radialCountOver2;
                    }
                    else
                    {
                        localExtention = groupExtention - groupExtention * (float)(indexSegment - radialCountOver2) / (float)radialCountOver2;
                    }
                    localExtention *= 2.0f;
                    localVerticies[index++] = new Vertex(new Vector3(sinArc * (outerRadius + localExtention), cosArc * (outerRadius + localExtention), -thickness), normal, Vector2.Zero);
                    localVerticies[index++] = new Vertex(new Vector3(sinArc * innerRadius, cosArc * innerRadius, -thickness), normal, Vector2.Zero);
                }

                Vector3 normalTop = Vector3.Zero;
                Vector3 normalBottom = Vector3.Zero;

                // Outer edge.
                for (int indexSegment = 0; indexSegment < radialCount; indexSegment++)
                {
                    float arc = indexSegment * this.arcLength / segmentCount - arcHalfLength + MathHelper.PiOver2;
                    float sinArc = (float)Math.Sin(arc);
                    float cosArc = (float)Math.Cos(arc);
                    normalTop = new Vector3(sinArc, cosArc, 0.5f);
                    normalTop.Normalize();
                    normalBottom = new Vector3(sinArc, cosArc, -0.5f);
                    normalBottom.Normalize();

                    float localExtention = 0;
                    if (indexSegment <= radialCountOver2)
                    {
                        localExtention = groupExtention * (float)indexSegment / (float)radialCountOver2;
                    }
                    else
                    {
                        localExtention = groupExtention - groupExtention * (float)(indexSegment - radialCountOver2) / (float)radialCountOver2;
                    }
                    localVerticies[index++] = new Vertex(new Vector3(sinArc * (outerRadius + localExtention), cosArc * (outerRadius + localExtention), thickness), normalTop, Vector2.Zero);
                    localVerticies[index++] = new Vertex(new Vector3(sinArc * (outerRadius + localExtention * 2.0f), cosArc * (outerRadius + localExtention * 2.0f), -thickness), normalBottom, Vector2.Zero);
                }

                // Inner edge.
                for (int indexSegment = 0; indexSegment < radialCount; indexSegment++)
                {
                    float arc = indexSegment * this.arcLength / segmentCount - arcHalfLength + MathHelper.PiOver2;
                    float sinArc = (float)Math.Sin(arc);
                    float cosArc = (float)Math.Cos(arc);
                    normalTop = new Vector3(-sinArc, -cosArc, 0.5f);
                    normalTop.Normalize();
                    normalBottom = new Vector3(-sinArc, -cosArc, -0.5f);
                    normalBottom.Normalize();

                    localVerticies[index++] = new Vertex(new Vector3(sinArc * innerRadius, cosArc * innerRadius, thickness), normalTop, Vector2.Zero);
                    localVerticies[index++] = new Vertex(new Vector3(sinArc * innerRadius, cosArc * innerRadius, -thickness), normalBottom, Vector2.Zero);
                }

                // clockwise normal side edge cap
                {
                    float sinArc = (float)Math.Sin(arcHalfLength + MathHelper.PiOver2);
                    float cosArc = (float)Math.Cos(arcHalfLength + MathHelper.PiOver2);
                    normal = new Vector3(sinArc, cosArc, 0.0f);
                    localVerticies[index++] = new Vertex(new Vector3(sinArc * outerRadius, cosArc * outerRadius, thickness), normal, Vector2.Zero);
                    localVerticies[index++] = new Vertex(new Vector3(sinArc * innerRadius, cosArc * innerRadius, thickness), normal, Vector2.Zero);
                    localVerticies[index++] = new Vertex(new Vector3(sinArc * outerRadius, cosArc * outerRadius, -thickness), normal, Vector2.Zero);
                    localVerticies[index++] = new Vertex(new Vector3(sinArc * innerRadius, cosArc * innerRadius, -thickness), normal, Vector2.Zero);
                }

                // counter-clockwise normal side edge cap
                {
                    float sinArc = (float)Math.Sin(-arcHalfLength + MathHelper.PiOver2);
                    float cosArc = (float)Math.Cos(-arcHalfLength + MathHelper.PiOver2);
                    normal = new Vector3(sinArc, cosArc, 0.0f);
                    localVerticies[index++] = new Vertex(new Vector3(sinArc * outerRadius, cosArc * outerRadius, thickness), normal, Vector2.Zero);
                    localVerticies[index++] = new Vertex(new Vector3(sinArc * innerRadius, cosArc * innerRadius, thickness), normal, Vector2.Zero);
                    localVerticies[index++] = new Vertex(new Vector3(sinArc * outerRadius, cosArc * outerRadius, -thickness), normal, Vector2.Zero);
                    localVerticies[index++] = new Vertex(new Vector3(sinArc * innerRadius, cosArc * innerRadius, -thickness), normal, Vector2.Zero);
                }

                // Copy to vertex buffer.
                vertexBuffer.SetData<Vertex>(localVerticies);


                // Create index buffer.
                if (indexBuffer == null)
                {
                    indexBuffer = new IndexBuffer(device, IndexElementSize.SixteenBits, triangleCount * 3, BufferUsage.WriteOnly);
                }

                // Generate the local copy of the data.
                ushort[] localIndexBuffer = new ushort[this.triangleCount * 3];

                index = 0;

                // Upper surface.
                for (int indexSegment = 0; indexSegment < segmentCount; indexSegment++)
                {
                    int baseVertex = 0;
                    localIndexBuffer[index++] = (ushort)(baseVertex + (3 + indexSegment * 2));
                    localIndexBuffer[index++] = (ushort)(baseVertex + (1 + indexSegment * 2));
                    localIndexBuffer[index++] = (ushort)(baseVertex + (2 + indexSegment * 2));
                    localIndexBuffer[index++] = (ushort)(baseVertex + (2 + indexSegment * 2));
                    localIndexBuffer[index++] = (ushort)(baseVertex + (1 + indexSegment * 2));
                    localIndexBuffer[index++] = (ushort)(baseVertex + (0 + indexSegment * 2));
                }

                // Lower surface.
                for (int indexSegment = 0; indexSegment < segmentCount; indexSegment++)
                {
                    int baseVertex = radialCount * 2;
                    localIndexBuffer[index++] = (ushort)(baseVertex + (0 + indexSegment * 2));
                    localIndexBuffer[index++] = (ushort)(baseVertex + (1 + indexSegment * 2));
                    localIndexBuffer[index++] = (ushort)(baseVertex + (2 + indexSegment * 2));
                    localIndexBuffer[index++] = (ushort)(baseVertex + (2 + indexSegment * 2));
                    localIndexBuffer[index++] = (ushort)(baseVertex + (1 + indexSegment * 2));
                    localIndexBuffer[index++] = (ushort)(baseVertex + (3 + indexSegment * 2));
                }

                // Outer ring.
                for (int indexSegment = 0; indexSegment < segmentCount; indexSegment++)
                {
                    int baseVertex = radialCount * 4;
                    localIndexBuffer[index++] = (ushort)(baseVertex + (0 + indexSegment * 2));
                    localIndexBuffer[index++] = (ushort)(baseVertex + (1 + indexSegment * 2));
                    localIndexBuffer[index++] = (ushort)(baseVertex + (2 + indexSegment * 2));
                    localIndexBuffer[index++] = (ushort)(baseVertex + (2 + indexSegment * 2));
                    localIndexBuffer[index++] = (ushort)(baseVertex + (1 + indexSegment * 2));
                    localIndexBuffer[index++] = (ushort)(baseVertex + (3 + indexSegment * 2));
                }
                // Inner ring.
                for (int indexSegment = 0; indexSegment < segmentCount; indexSegment++)
                {
                    int baseVertex = radialCount * 6;
                    localIndexBuffer[index++] = (ushort)(baseVertex + (3 + indexSegment * 2));
                    localIndexBuffer[index++] = (ushort)(baseVertex + (1 + indexSegment * 2));
                    localIndexBuffer[index++] = (ushort)(baseVertex + (2 + indexSegment * 2));
                    localIndexBuffer[index++] = (ushort)(baseVertex + (2 + indexSegment * 2));
                    localIndexBuffer[index++] = (ushort)(baseVertex + (1 + indexSegment * 2));
                    localIndexBuffer[index++] = (ushort)(baseVertex + (0 + indexSegment * 2));
                }

                // clockwise normal side edge cap
                {
                    int baseVertex = radialCount * 8;
    
                    localIndexBuffer[index++] = (ushort)((baseVertex + 3));
                    localIndexBuffer[index++] = (ushort)((baseVertex + 1));
                    localIndexBuffer[index++] = (ushort)((baseVertex + 2));
                    localIndexBuffer[index++] = (ushort)((baseVertex + 2));
                    localIndexBuffer[index++] = (ushort)((baseVertex + 1));
                    localIndexBuffer[index++] = (ushort)((baseVertex + 0));
                }

                // counter-clockwise normal side edge cap
                {
                    int baseVertex = radialCount * 8 + 4;
                    localIndexBuffer[index++] = (ushort)((baseVertex + 0));
                    localIndexBuffer[index++] = (ushort)((baseVertex + 1));
                    localIndexBuffer[index++] = (ushort)((baseVertex + 2));
                    localIndexBuffer[index++] = (ushort)((baseVertex + 2));
                    localIndexBuffer[index++] = (ushort)((baseVertex + 1));
                    localIndexBuffer[index++] = (ushort)((baseVertex + 3));
                }

                // Copy it to the index buffer.
                indexBuffer.SetData<ushort>(localIndexBuffer);
            }

            public void Render(GraphicsDevice device, Effect effect)
            {
                device.SetVertexBuffer(this.vertexBuffer);
                device.Indices = this.indexBuffer;

                for (int indexPass = 0; indexPass < effect.CurrentTechnique.Passes.Count; indexPass++)
                {
                    EffectPass pass = effect.CurrentTechnique.Passes[indexPass];
                    pass.Apply();
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, this.vertexCount, 0, this.triangleCount);
                }

            }

            // INeedsDeviceReset
            public void LoadContent(bool immediate)
            {
            }

            public void InitDeviceResources(GraphicsDevice device)
            {
                CreateGeometry(device);
            }

            public void UnloadContent()
            {
                BokuGame.Release(ref this.indexBuffer);
                BokuGame.Release(ref this.vertexBuffer);
            }

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
            }

        }   // end of class IndexPrimitiveSlice

        public class RenderObjSlice : RenderObject, INeedsDeviceReset, ITransform
        {
            protected Transform localTransform = new Transform();
            protected Matrix worldMatrix = Matrix.Identity;
            public ITransform transformParent;
            protected IndexPrimitiveSlice slice;
            protected RenderObject item;
            protected List<RenderObject> adornments;

            private Texture2D texture = null;
            private Effect effect = null;
            static public Vector4 ColorNormal = new Vector4(78.0f / 256f, 169f / 256f, 64f / 256f, .95f);
            static public Vector4 ColorSelectedDim = new Vector4(0.49f, 0.49f, 0.0f, 1.0f);
            static public Vector4 ColorSelectedBright = new Vector4(0.78f, 0.78f, 0.0f, 1.0f);


            private Vector4 diffuse = ColorNormal; // Local override for diffuse color.


            public RenderObjSlice(Object parent, Object item, List<RenderObject> adornments, IndexPrimitiveSlice slice)
            {
                this.transformParent = parent as ITransform;
                this.slice = slice;
                this.item = item as RenderObject;
                this.adornments = adornments;
                BokuGame.Load(this);
            }

            public Vector4 DiffuseColor
            {
                set 
                { 
                    this.diffuse = value; 
                }
                get 
                { 
                    return this.diffuse; 
                }
            }

            public override void Render(Camera camera)
            {
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

                Matrix viewMatrix = camera.ViewMatrix;
                Matrix projMatrix = camera.ProjectionMatrix;

                Matrix worldViewProjMatrix = worldMatrix * viewMatrix * projMatrix;
                effect.Parameters["WorldViewProjMatrix"].SetValue(worldViewProjMatrix);
                effect.Parameters["WorldMatrix"].SetValue(worldMatrix);

                // used for debugging
                //effect.Parameters["DiffuseTexture"].SetValue(texture);

                effect.Parameters["DiffuseColor"].SetValue(this.diffuse);
                effect.Parameters["SpecularColor"].SetValue(new Vector4(0.12f, 0.12f, 0.12f, 1.0f));
                effect.Parameters["EmissiveColor"].SetValue(new Vector4(0.01f, 0.01f, 0.01f, 1.0f));
                effect.Parameters["SpecularPower"].SetValue(4.0f);
                effect.Parameters["Shininess"].SetValue(0.0f);

                if (InGame.inGame.renderEffects != InGame.RenderEffect.Normal)
                {
                    effect.CurrentTechnique = effect.Techniques[InGame.inGame.renderEffects.ToString()];
                }
                else
                {
                    effect.CurrentTechnique = effect.Techniques["NoTextureColorPass"];
                }
                this.slice.Render(device, effect);
                if (this.adornments != null)
                {
                    foreach (RenderObject renderObj in this.adornments)
                    {
                        renderObj.Render(camera);
                    }
                }
                // Don't render the models on the pie slices for anything except the normal render pass.
                if (InGame.inGame.renderEffects == InGame.RenderEffect.Normal)
                {
                    this.item.Render(camera);
                }

            }

            public override void Activate()
            {
            }

            public override void Deactivate()
            {
            }

            public void LoadContent(bool immediate)
            {
                // Init the effect.
                if (effect == null)
                {
                    effect = Editor.Effect;
                    if (effect == null)
                    {
                        Editor.Effect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\UI");
                        effect = Editor.Effect;
                        ShaderGlobals.RegisterEffect("UI", effect);
                    }
                }

                // Load the texture.
                if (texture == null)
                {
                    texture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Cursor3D");
                }
            }

            public void InitDeviceResources(GraphicsDevice device)
            {
            }

            public void UnloadContent()
            {
                BokuGame.Release(ref effect);
                BokuGame.Release(ref texture);
            }

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
            }

            // ITransform
            Transform ITransform.Local
            {
                get
                {
                    return this.localTransform;
                }
                set
                {
                    this.localTransform = value;
                }
            }
            Matrix ITransform.World
            {
                get
                {
                    return this.worldMatrix;
                }
            }
            bool ITransform.Compose()
            {
                bool changed = this.localTransform.Compose();
                if (changed)
                {
                    RecalcMatrix();
                }
                return changed;
            }
            void ITransform.Recalc(ref Matrix parentMatrix)
            {
                this.worldMatrix = this.localTransform.Matrix * parentMatrix;
                ITransform transformChild = this.item as ITransform;
                if (transformChild != null)
                {
                    transformChild.Recalc(ref this.worldMatrix);
                }
                if (this.adornments != null)
                {
                    foreach (ITransform transformAdornment in this.adornments)
                    {
                        transformAdornment.Recalc(ref this.worldMatrix);
                    }
                }
            }
            ITransform ITransform.Parent
            {
                get
                {
                    return this.transformParent;
                }
                set
                {
                    this.transformParent = value;
                }
            }
            protected void RecalcMatrix()
            {
                ITransform transformThis = this as ITransform;
                Matrix parentWorldMatrix;
                if (transformParent != null)
                {
                    parentWorldMatrix = transformParent.World;
                }
                else
                {
                    parentWorldMatrix = Matrix.Identity;
                }
                transformThis.Recalc(ref parentWorldMatrix);
            }

        }   // end of class RenderObjSlice

        public enum SliceType
        {
            single,
            group,
        }

        static protected Vector3 SliceOffsetDefault = new Vector3(0.05f, 0.0f, 0.0f);

        /// <summary>
        /// Interpreting touch data for pie selection
        /// </summary>
        // Properties for the underlying 9-grid geometry.

        protected UpdateObj updateObj;
        public RenderObj renderObj;
        protected Transform localTransform = new Transform();
        protected Matrix worldMatrix = Matrix.Identity;
        protected ITransform transformParent;
        protected enum States
        {
            Inactive,
            Active,
        }
        protected States state = States.Inactive;
        protected States pendingState = States.Inactive;

        protected IndexPrimitiveSlice sliceGroup;
        protected IndexPrimitiveSlice sliceSingle;
        protected float radiusAtItems;
        protected Vector3 originalTranslation;

        private string name = "";

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public override Object Parent
        {
            get
            {
                return transformParent;
            }
        }

        // Ugly hack to be able to get the camera from the shim 
        // object back out so we can do mouse hit testing with it.
        static protected UiCamera Camera = null;

        public PieSelector(Object parent, string uiMode)
        {
            this.renderObj = new RenderObj(this, this, parentSelector as ITransform);
            this.updateObj = new UpdateObj(this, uiMode);

            transformParent = parent as ITransform;
        }

        public bool OwnsFocus()
        {
            return updateObj.OwnsFocus();
        }

        protected void BuildLayout()
        {
            this.originalTranslation = this.localTransform.Translation;
            this.renderObj.ViewTested = false;

            float circumferenceAtItems;
            float maxItemRadius;
            const float radiusAtItemSpacing = 0.2f;

            // calculate layout information from items
            CalcLayoutInfoFromItems( out circumferenceAtItems, out maxItemRadius );

            // calc radius of the pie
            this.radiusAtItems = circumferenceAtItems / MathHelper.TwoPi + radiusAtItemSpacing;

            float spacingCircumference = 0.0f; // spacing between items on the circumference

            // adjust spacing if radius is smaller than the inner radius (one item)
            if (this.radiusAtItems < maxItemRadius * 2.0f)
            {
                // radius is too small, must increase 
                this.radiusAtItems = maxItemRadius * 2.0f + radiusAtItemSpacing;
                // and provide the extra spacing between items on the circumference
                float newCircumference = MathHelper.TwoPi * this.radiusAtItems;
                spacingCircumference = (newCircumference - circumferenceAtItems) / items.Count;
                circumferenceAtItems = newCircumference;
            }
            float radiusInside = maxItemRadius * 1.2f; // with a little spacing
            float radiusOutside = this.radiusAtItems + (maxItemRadius * 1.2f); // with a little spacing

            float arcLength = MathHelper.TwoPi / this.items.Count;
            renderObj.radius = radiusOutside;

            // create the two types of slices
            this.sliceSingle = new IndexPrimitiveSlice( radiusInside, radiusOutside, arcLength, SliceType.single);
            this.sliceGroup = new IndexPrimitiveSlice(radiusInside, radiusOutside, arcLength, SliceType.group);

            // create a slice for every item
            for (int indexItem = 0; indexItem < this.items.Count; indexItem++)
            {
                UiSelector.ItemData itemData = this.items[indexItem];
                RenderObjSlice slice;
                if (itemData is UiSelector.GroupData)
                {
                    slice = new RenderObjSlice(this, itemData.item, itemData.Adornments, this.sliceGroup);
                }
                else
                {
                    slice = new RenderObjSlice(this, itemData.item, itemData.Adornments, this.sliceSingle);
                }
                float rot = MathHelper.PiOver2 - indexItem * arcLength; // rotate clockwise around 

                ITransform transformItem = itemData.item as ITransform;
                ITransform transformSlice = slice as ITransform;

                // move the item into place on the slice (see DestroyLayout)
                transformItem.Local.Translation += new Vector3(this.radiusAtItems, 0.0f, 0.0f);
                transformItem.Local.RotationZ -= rot;
                transformItem.Parent = transformSlice; // parent it to the slice
                transformItem.Compose();

                // move any adornments into place on the slice
                if (itemData.Adornments != null)
                {
                    foreach (ITransform transformAdornment in itemData.Adornments)
                    {
                        transformAdornment.Local.Translation += new Vector3(this.radiusAtItems, 0.0f, 0.0f);
                        transformAdornment.Local.RotationZ -= rot;
                        transformAdornment.Parent = transformSlice; // parent it to the slice
                        transformAdornment.Compose();
                    }
                }

                // rotate the slice into place
                transformSlice.Local.OriginTranslation = SliceOffsetDefault; // move away from center to space them
                transformSlice.Local.RotationZ = rot;
                transformSlice.Compose();

                this.renderObj.renderList.Add(slice);
            }
            ITransform transformThis = this as ITransform;
            transformThis.Compose();

            // force an update of the selected item
            IndexSelectedItem = indexCenteredItem;


        }
        protected void DestroyLayout()
        {
           
            this.sliceGroup = null;
            this.sliceSingle = null;
            float arcLength = MathHelper.TwoPi / this.items.Count;
            // clear the transforms on the items
            for (int indexItem = 0; indexItem < this.items.Count; indexItem++)
            {
                UiSelector.ItemData itemData = this.items[indexItem];
                float rot = MathHelper.PiOver2 - indexItem * arcLength; // rotate clockwise around 

                // reverse changes to items (see BuildLayout)
                ITransform transformItem = itemData.item as ITransform;
                transformItem.Local.Translation -= new Vector3(this.radiusAtItems, 0.0f, 0.0f);
                transformItem.Local.RotationZ += rot;
                transformItem.Compose();

                // reverse changes to adornments 
                if (itemData.Adornments != null)
                {
                    foreach (ITransform transformAdornment in itemData.Adornments)
                    {
                        transformAdornment.Local.Translation -= new Vector3(this.radiusAtItems, 0.0f, 0.0f);
                        transformAdornment.Local.RotationZ += rot;
                        transformAdornment.Compose();
                    }
                }
            }

            this.radiusAtItems = 0.0f;
            this.localTransform.Translation = this.originalTranslation;
            this.renderObj.ViewTested = false;
            this.renderObj.radius = 0.0f;

            // just disconnect the list
            renderObj.renderList.Clear();
            this.indexDefaultItem = indexCenteredItem;
        }
        protected void CalcLayoutInfoFromItems(out float circumference, out float maxRadius )
        {
            circumference = 0.0f;
            UiSelector.ItemData itemData;
            maxRadius = float.NegativeInfinity;
            for (int indexItem = 0; indexItem < this.items.Count; indexItem++)
            {
                itemData = this.items[indexItem];
                IBounding boundingItem = itemData.item as IBounding;
                if (boundingItem != null)
                {
                    circumference += boundingItem.BoundingSphere.Radius * 2.0f;
                    maxRadius = MathHelper.Max(maxRadius, boundingItem.BoundingSphere.Radius);
                }
            }
        }

        public void UpdateSelection(Object sender, StickEventArgs args)
        {
            // Handle keyboard input.  This is seperate from the meta-buttons since with
            // the gamepad we use absolute positioning whereas with the keyboard we have
            // to use relative positioning.
            if (KeyboardInput.WasPressedOrRepeat(Keys.Left))
            {
                if (IndexSelectedItem == -1)
                {
                    IndexSelectedItem = (int)(0.75f * items.Count);
                }
                else
                {
                    IndexSelectedItem = (IndexSelectedItem - 1 + items.Count) % items.Count;
                }
                KeyboardInput.ClearAllWasPressedState(Keys.Left);
            }
            else if (KeyboardInput.WasPressedOrRepeat(Keys.Right))
            {
                if (IndexSelectedItem == -1)
                {
                    IndexSelectedItem = (int)(0.25f * items.Count);
                }
                else
                {
                    IndexSelectedItem = (IndexSelectedItem + 1) % items.Count;
                }
                KeyboardInput.ClearAllWasPressedState(Keys.Right);
            } 
            else if (KeyboardInput.WasPressedOrRepeat(Keys.Down))
            {
                if (IndexSelectedItem == -1)
                {
                    IndexSelectedItem = (int)(0.5f * items.Count);
                }
                else
                {
                    IndexSelectedItem = (IndexSelectedItem - 1 + items.Count) % items.Count;
                }
                KeyboardInput.ClearAllWasPressedState(Keys.Down);
            }
            else if (KeyboardInput.WasPressedOrRepeat(Keys.Up))
            {
                if (IndexSelectedItem == -1)
                {
                    IndexSelectedItem = 0;
                }
                else
                {
                    IndexSelectedItem = (IndexSelectedItem + 1) % items.Count;
                }
                KeyboardInput.ClearAllWasPressedState(Keys.Up);
            }


            // Mouse Input
            bool mouseMoving = false;
            {
                Matrix invWorld = Matrix.Invert(worldMatrix);

                UiCamera camera = PieSelector.Camera;
                if(camera == null)
                    camera = InGame.inGame.Editor.Camera;
                Vector2 hitUV = Vector2.Zero;
                if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                {
                    /// Guessed size ONLY
                    //width = 5;
                    //height = 3;
                    for (int i = 0; i < TouchInput.TouchCount; i++)
                    {
                        TouchContact touch = TouchInput.GetTouchContactByIndex(i);

                        // Touch input
                        // If the user touched the menu, move the selection index to the item under the touch.
                        // On touch down, make the item (if any) under the contact the touchedItem.
                        // On touch up, if the touch is still over the touchedItem, activate it.  If not, just clear touchedItem. 

                        hitUV = TouchInput.GetHitOrtho(touch.position, camera, ref invWorld, useRtCoords:false);
                    }

                }
                else
                {
                    hitUV = MouseInput.GetHitOrtho(camera, ref invWorld, false);
                }

                float mag = hitUV.Length();

                // Are we in the "active" band?
                if (mag > radiusAtItems - 1.2f && mag < radiusAtItems + 1.0f)
                {

                    if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                    {
                        args.position = hitUV;
                        mouseMoving = true;

                        //if (TouchInput.WasTouched)
                        //{
                        //    TouchInput.GetOldestTouch().touchedObject = SelectedItem;
                        //}
                    }
                    else
                    // If moving, fake left stick input and let that code deal with it.
                    if (MouseInput.Position != MouseInput.PrevPosition)
                    {
                        args.position = hitUV;
                        mouseMoving = true;
                    }

                    if (MouseInput.Left.WasPressed)
                    {
                        MouseInput.ClickedOnObject = SelectedItem;
                    }
                    if (MouseInput.Left.WasReleased && MouseInput.ClickedOnObject == SelectedItem)
                    {
                        MouseInput.Left.ClearAllWasPressedState();
                        OnSelect(null, null);
                    }
                }
                else if (mag > radiusAtItems - 1.2f)
                {
                    // We're outside of the pie menu.  If the user clicks here, close.
                    if (MouseInput.Left.WasPressed)
                    {
                        OnCancel(null, null);
                    }
                    else if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                    {
                        if ( TouchInput.WasReleased )
                        {
                            OnCancel(null, null);
                        }
                    }
                }

                // Also treat right click as exit.
                if (MouseInput.Right.WasPressed)
                {
                    OnCancel(null, null);
                }
            }

            float magnitude = args.position.Length();

            // If the position is outside our dead area.
            if (magnitude > 0.80f)
            {
                Vector2 dir = args.position;
                dir.Normalize();

                // Calc the angle of the stick.  Set this up so that directly up results 
                // in an angle of 0 and to the right results in pi/2 (clockwise).
                double angle = Math.Acos(dir.Y);
                if (dir.X < 0.0f)
                {
                    angle = MathHelper.TwoPi - angle;
                }

                // Calc the arc covered by each menu item.
                double arcItem = (MathHelper.TwoPi / (double)items.Count);

                // Are we entering for the first time?
                if (IndexSelectedItem == -1)
                {
                    // Add half this as an offset to angle since the 0th item is directly upward.
                    double fooAngle = (angle + arcItem / 2.0) % MathHelper.TwoPi;

                    IndexSelectedItem = (int)(fooAngle / arcItem);

                    // Update help overlay.
                    HelpOverlay.Pop();
                    Editor editor = InGame.inGame.Editor;
                    if (editor.Active)
                    {
                        HelpOverlay.Push(@"PieSelectorProgramming");
                    }
                    else
                    {
                        HelpOverlay.Push(@"PieSelectorAddItem");
                    }

                }
                else
                {
                    // We already have a selected item, so we want to see if the selection has changed.

                    // Calc angle for center of selected item.
                    double selectedAngle = IndexSelectedItem * arcItem;

                    // Calc the stick angle relative to this angle.  Positive is clockwise...
                    double relative = angle - selectedAngle;
                    if (relative > MathHelper.Pi)
                    {
                        relative = relative - MathHelper.TwoPi;
                    }
                    else if (relative < -MathHelper.Pi)
                    {
                        relative = relative + MathHelper.TwoPi;
                    }

                    // Calc max relative angle we need before switching to the 
                    // next item.  Use half the width of the pie segment plus 
                    // a little extra to provide some hysteresis.
                    double maxAngle = arcItem / 2.0f;
                    // Only apply hysteresis when we have more that 4 items in the
                    // pie.  Use 1/3 of the width of the segment.  If we're using
                    // the mouse, don't apply any hysteresis.
                    if (items.Count > 4 && !mouseMoving)
                    {
                        maxAngle += arcItem / 3.0f;
                    }

                    if (relative > maxAngle)
                    {
                        IndexSelectedItem = (IndexSelectedItem + 1) % items.Count;
                    }
                    else if (relative < -maxAngle)
                    {
                        IndexSelectedItem = (IndexSelectedItem - 1 + items.Count) % items.Count;
                    }
                    else if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch) 
                    {
                        // no change in selection
                        if (TouchInput.WasReleased)
                        {
                            //if (TouchInput.GetOldestTouch().touchedObject == SelectedItem)
                            //{
                                OnSelect(null, null);
                                TouchInput.GetOldestTouch().TouchedObject = null; // clear it
                            //}
                        }
                    }

                }

            }   // end if stick is pushed over far enough.

        }   // end of UpdateSelection()

        protected override void HideCursor()
        {
        }

        protected override void ShowCursor()
        {
        }

        protected override void MoveCursor(Vector3 position, int indexNew)
        {
            float twitchTime = 0.1f;

            // Undo previous slice selection state.
            if (this.indexSelectedItem != indexCenteredItem)
            {
                RenderObjSlice slice = this.renderObj.renderList[this.indexSelectedItem] as RenderObjSlice;
                slice.DiffuseColor = RenderObjSlice.ColorNormal;

                if (this.items.Count > 2) // dont move them if only two
                {
                    ITransform transformSlice = slice as ITransform;
                    TwitchManager.Set<Vector3> set = delegate(Vector3 value, Object param)
                    {
                        transformSlice.Local.OriginTranslation = value;
                        transformSlice.Compose();
                    };
                    TwitchManager.CreateTwitch<Vector3>(transformSlice.Local.OriginTranslation, SliceOffsetDefault, set, twitchTime, TwitchCurve.Shape.EaseInOut);
                }
            }

            // apply new slice selection state
            if (indexNew != indexCenteredItem)
            {
                RenderObjSlice slice = this.renderObj.renderList[indexNew] as RenderObjSlice;
                slice.DiffuseColor = RenderObjSlice.ColorSelectedBright;

                if (this.items.Count > 2) // dont move them if only two
                {
                    ITransform transformSlice = slice as ITransform;
                    {
                        TwitchManager.Set<Vector3> set = delegate(Vector3 value, Object param)
                        {
                            transformSlice.Local.OriginTranslation = value;
                            transformSlice.Compose();
                        };
                        TwitchManager.CreateTwitch<Vector3>(transformSlice.Local.OriginTranslation, new Vector3(0.20f, 0.0f, 0.0f), set, twitchTime, TwitchCurve.Shape.EaseInOut);
                    }
                }
            }
        }

        // ITransform
        Transform ITransform.Local
        {
            get
            {
                return this.localTransform;
            }
            set
            {
                this.localTransform = value;
            }
        }
        Matrix ITransform.World
        {
            get
            {
                return this.worldMatrix;
            }
        }
        bool ITransform.Compose()
        {
            bool changed = this.localTransform.Compose();
            if (changed)
            {
                RecalcMatrix();
            }
            return changed;
        }
        void ITransform.Recalc(ref Matrix parentMatrix)
        {
            this.worldMatrix = this.localTransform.Matrix * parentMatrix;
            if (renderObj != null)
            {
                foreach (ITransform transformChild in renderObj.renderList)
                {
                    transformChild.Recalc(ref worldMatrix);
                }
            }
        }
        ITransform ITransform.Parent
        {
            get
            {
                return this.transformParent;
            }
            set
            {
                this.transformParent = value;
            }
        }
        protected void RecalcMatrix()
        {
            ITransform transformThis = this as ITransform;
            Matrix parentWorldMatrix;
            if (transformParent != null)
            {
                parentWorldMatrix = transformParent.World;
            }
            else
            {
                parentWorldMatrix = Matrix.Identity;
            }
            transformThis.Recalc(ref parentWorldMatrix);
        }

        // GameObject
        public override bool Refresh(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;

            RefreshSubSelector(updateList, renderList);

            if (state != pendingState)
            {
                if (pendingState == States.Active)
                {
                    BuildLayout();

                    updateList.Add(updateObj);
                    updateObj.Activate();
                    renderList.Add(renderObj);
                    renderObj.Activate();
                }
                else
                {
                    renderObj.Deactivate();
                    renderList.Remove(renderObj);
                    updateObj.Deactivate();
                    updateList.Remove(updateObj);

                    DestroyLayout();

                    result = true;
                }

                state = pendingState;
            }
            
            return result;
        }

        private Texture2D prevToolIcon = null;
        
        public override void Activate()
        {
            if (state != States.Active)
            {
                pendingState = States.Active;
                BokuGame.objectListDirty = true;

                // Save away tool icon.
                prevToolIcon = HelpOverlay.ToolIcon;

                Editor editor = InGame.inGame.Editor;
                if (editor.Active)
                {
                    HelpOverlay.Push(@"PieSelectorProgrammingNoSelection");
                }
                else
                {
                    HelpOverlay.Push(@"PieSelectorAddItemNoSelection");
                    // Clear the tool icon when the AddItem menu is up.
                    HelpOverlay.ToolIcon = null;
                }
            }
        }

        public override void Deactivate()
        {
            base.Deactivate();

            if (state != States.Inactive && pendingState != States.Inactive)
            {
                pendingState = States.Inactive;
                this.composedObjectPending = null;
                BokuGame.objectListDirty = true;

                for (int i = 0; i < Count; ++i)
                {
                    GroupData groupData = this[i] as GroupData;
                    if (groupData != null)
                    {
                        UiSelector uiSelector = groupData.selectorGroup as UiSelector;
                        if (uiSelector != null)
                        {
                            uiSelector.Deactivate();
                        }
                    }
                }

                // Restore tool icon.
                HelpOverlay.ToolIcon = prevToolIcon;
                prevToolIcon = null;

                HelpOverlay.Pop();
                ToolTipManager.Clear();
            }
        }   // end of Deactivate()

    }   // end of class PieSelector

}   // end of namespace Boku.UI
