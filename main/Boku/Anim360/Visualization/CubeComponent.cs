/*
 * CubeComponent.cs
 * Copyright (c) 2007 David Astle
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be included
 * in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Xclna.Xna.Animation.Visualization
{
    /// <summary>
    /// A cube component to assist in visualization of drawable objects.
    /// </summary>
    public sealed class CubeComponent : DrawableGameComponent, IAttachable
    {

        private int[] indices;
        private VertexDeclaration vertexDeclaration;
        private BasicEffect effect;
        private IGraphicsDeviceService graphics;
        private Color color;
        private float sideLength;
        private VertexPositionColor[] verts;
        private Vector3[] buffer;
        private BonePose pose = null;
        private Matrix localTransform = Matrix.Identity;

        /// <summary>
        /// Gets or sets the world matrix.
        /// </summary>
        public Matrix World
        {
            get { return effect.World; }
            set { effect.World = value; }
        }

        /// <summary>
        /// Gets or sets the view matrix.
        /// </summary>
        public Matrix View
        {
            get { return effect.View; }
            set { effect.View = value; }
        }

        /// <summary>
        /// Gets or sets the projection matrix.
        /// </summary>
        public Matrix Projection
        {
            get { return effect.Projection; }
            set { effect.Projection = value; }
        }

        /// <summary>
        /// Gets or sets the color of the cube.
        /// </summary>
        public Color Color
        {
            get
            {
                return color;
            }
            set
            {
                for (int i = 0; i < verts.Length; i++)
                {
                    verts[i].Color = value;
                }
                color = value;
            }
        }


        /// <summary>
        /// Creats a new CubeComponent.
        /// </summary>
        /// <param name="game">The game to which this component will be attached.</param>
        /// <param name="color">The color of the cube.</param>
        /// <param name="sideLength">The length of one side of the cube.</param>
        public CubeComponent(Game game,
            Color color,
            float sideLength)
            : base(game)
        {
            this.sideLength = sideLength;
            this.graphics = (IGraphicsDeviceService)game.Services.GetService(
                typeof(IGraphicsDeviceService));
            effect = new BasicEffect(graphics.GraphicsDevice, null);


            indices = new int[]
            {
                0,1,2, // left face
                2,3,0,

                3,2,6, // top face
                6,7,3,

                7,6,5, // right face
                5,4,7,

                4,5,1, // bottom face
                1,0,4,

                5,6,2, // back face
                2,1,5,
                
                7,4,0, // front face
                0,3,7
            };


            Vector3[] originalVerts = new Vector3[]
            {
                new Vector3(-1, -1, 1), // 0 - front bottom left
                new Vector3(-1, -1, -1),  // 1 - back bottom left
                new Vector3(-1,1,-1),     // 2 - back top left
                new Vector3(-1,1,1),     // 3 - front top left

                new Vector3(1,-1,1),     // 4 - front bottom right
                new Vector3(1,-1,-1),     // 5 - back bottom right
                new Vector3(1,1,-1),     // 6 - back top right
                new Vector3(1,1,1)      // 7 - front top right
            };

            for (int i = 0; i < originalVerts.Length; i++)
            {
                originalVerts[i].X *= sideLength / 2;
                originalVerts[i].Y *= sideLength / 2;
                originalVerts[i].Z *= sideLength / 2;
            }
            vertexDeclaration = new VertexDeclaration(
                graphics.GraphicsDevice,
                VertexPositionColor.VertexElements);
            verts = new VertexPositionColor[8];
            buffer = new Vector3[8];
            for (int i = 0; i < verts.Length; i++)
            {
                verts[i].Position = originalVerts[i];
                verts[i].Color = color;

            }

            effect.VertexColorEnabled = true;
            game.Components.Add(this);
        }

        /// <summary>
        /// Gets the bounding box of the cube in world space.
        /// </summary>
        public BoundingBox BoundingBox
        {
            get
            {
                Matrix world = effect.World;
                for (int i = 0; i < buffer.Length; i++)
                {
                    Vector3.Transform(ref verts[i].Position,
                        ref world,
                        out buffer[i]);
                }
                return BoundingBox.CreateFromPoints(buffer);
            }
        }

        /// <summary>
        /// Immediately releases unmanaged resources.
        /// </summary>
        /// <param name="disposing">False if managed resources should not be disposed.</param>
        protected override void Dispose(bool disposing)
        {
            vertexDeclaration.Dispose();
            effect.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>
        /// Draws the cube.
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        public override void Draw(GameTime gameTime)
        {

            effect.Begin();
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Begin();
                graphics.GraphicsDevice.VertexDeclaration = vertexDeclaration;
                graphics.GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionColor>(
                    PrimitiveType.TriangleList,
                    verts,
                    0,
                    verts.Length,
                    indices,
                    0,
                    12);

                pass.End();
            }
            effect.End();
        }

        #region IAttachable Members

        /// <summary>
        /// Gets or sets the local transform of the cube, before it as affected by the attached bone.
        /// </summary>
        public Matrix LocalTransform
        {
            get { return localTransform; }
            set { localTransform = value; }
        }

        /// <summary>
        /// Gets or sets the combined transform in world coordinates.
        /// </summary>
        Matrix IAttachable.CombinedTransform
        {
            get
            {
                return World;
            }
            set
            {
                World = value;
            }
        }

        /// <summary>
        /// Gets or sets the bone to which this cube is attached.
        /// </summary>
        public BonePose AttachedBone
        {
            get { return pose; }
            set { pose = value; }
        }

        #endregion
    }
}
