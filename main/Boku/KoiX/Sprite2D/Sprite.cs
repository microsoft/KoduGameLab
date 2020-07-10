// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

//#define HIT_TEST_DEBUG
//#define COLLISION_DEBUG
//#define INV_DEBUG

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace KoiX
{
    /// <summary>
    /// Simple graphics sprite with support for per-pixel collision detection.
    /// </summary>
    public class Sprite : IDeviceResetX
    {
        protected struct LineSegment
        {
            public Vector2 a;
            public Vector2 b;
        }

        static float sampleThreshold = 0.1f;
        static float collisionThreshold = 0.5f;

        #region Members

        // Color to tint with.
        Color color = Color.White;

        // Position in world.
        Vector2 position;

        // Center of sprite relative to 0,0 for rotation and position.
        Vector2 origin;

        // Scale factor.  For now, assume uniform scaling.
        float scale = 1.0f;

        // Rotation around origin.
        float rotation = 0.0f;

        // Allows sprite to be flipped horizontally or vertically.
        // TODO (****) Need to make sure this works w/ hit and collision testing.
        SpriteEffects flip = SpriteEffects.None;

        // Radius used for bounding circle tests.  Assumes center is at position/origin.
        float boundingRadius;

        // Current texture used for this sprite.
        protected Texture2D texture;
        string textureName;

        // Collision maps.  In list the 0th map matches the texture res.
        // Each further level is smaller.
        int numCollisionMapLevels = 1;
        protected List<byte[,]> collisionMaps;

        // Per-pixel vectors that point "outward" from the edge of the sprite.
        // Idea is not quite fully baked yet...
        Vector2[,] normalMap;

#if HIT_TEST_DEBUG
        Texture2D disc;
        Color discColor = new Color(1, 1, 1, 0.5f);
#endif

        #endregion

        #region Accessors

        /// <summary>
        /// c'tor
        /// </summary>
        /// <param name="textureName"></param>
        /// <param name="numCollisionLevels"></param>
        public Sprite(string textureName, int numCollisionMapLevels = 1)
        {
            this.textureName = textureName;
            this.numCollisionMapLevels = numCollisionMapLevels;
        }   // end of c'tor

        public Color Color
        {
            get { return color; }
            set { color = value; }
        }

        public Vector2 Position
        {
            get { return position; }
            set { position = value; }
        }

        public Vector2 Origin
        {
            get { return origin; }
            set 
            {
                if (origin != value)
                {
                    origin = value;
                    CalcBoundingRadius();
                }
            }
        }

        public float Scale
        {
            get { return scale; }
            set { scale = value; }
        }

        public float Rotation
        {
            get { return rotation; }
            set { rotation = value; }
        }

        public float BoundingRadius
        {
            get { return boundingRadius * Scale; }
        }

        public Texture2D Texture
        {
            get { return texture; }
        }

        #endregion

        #region Public

        public virtual  void Update(SpriteCamera camera)
        {
        }   // end of Update()

        /// <summary>
        /// Render the sprite.  
        /// </summary>
        /// <param name="camera"></param>
        public virtual void Render(SpriteCamera camera)
        {
            SpriteBatch batch = KoiLibrary.SpriteBatch;
            batch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, null, null, null, camera.ViewMatrix);
            {
                Render(camera, batch);
            }
            batch.End();
        }   // end of Render()

        public void Render(SpriteCamera camera, SpriteBatch batch)
        {
            float depth = 0.0f; // 0 is front, 1 is back.
            
            batch.Draw(texture, Position, null, color, Rotation, origin, Scale, flip, depth);

#if HIT_TEST_DEBUG
            // Render circular bound.
            float boundScale = BoundingRadius / 64.0f;
            batch.Draw(disc, Position, null, discColor, 0, new Vector2(64, 64), boundScale, flip, 0);
#endif

        }   // end of Render()

        /// <summary>
        /// HitTest a single point against the sprite.  If the hit is valid
        /// then also fill in the normal.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="hit">Assumed to be already transformed by camera.</param>
        /// <param name="normal"></param>
        /// <returns></returns>
        public bool HitTest(SpriteCamera camera, Vector2 hit, ref Vector2 normal)
        {
            // Iif hit then return true and fill in normal.
            bool result = false;

            Vector2 offset = hit - Position;
            if (offset.Length() < BoundingRadius)
            {
#if HIT_TEST_DEBUG
                discColor = new Color(1, 1, 0, 0.5f);
#endif
                // Ok, we're inside the bounding circel raidus.  Now
                // need to check the individual pixel.

                // Need to scale and rotate offset into texture space.
                offset /= Scale;

                if (Rotation != 0)
                {
                    // We could do this manually instead of using the Matrix
                    // class but would it be enough faster to make up for the
                    // loss of clarity?
                    Matrix mat = Matrix.CreateRotationZ(-Rotation);
                    offset = Vector2.TransformNormal(offset, mat);
                }

                // Add in offset caused by origin.
                Vector2 uv = offset + new Vector2(Origin.X, Origin.Y);

                // For this test, always using the most detailed map makes sense.
                byte[,] map = collisionMaps[0];
                float sample = 0;

                // Only test if uv is inside texture range.
                if (uv.X >= 0 && uv.Y >= 0 && uv.X < map.GetLength(0) && uv.Y < map.GetLength(1))
                {
                    // TODO Include code to handle Sprite flipping.
                    sample = map[(int)uv.X, (int)uv.Y];

                    if (sample > sampleThreshold)
                    {
#if HIT_TEST_DEBUG
                        discColor = new Color(1, 0, 0, 0.5f);
#endif
                        result = true;
                    }
                }

            }
            else
            {
#if HIT_TEST_DEBUG
                discColor = new Color(1, 1, 1, 0.5f);
#endif
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Test for collision between two sprites.
        /// Note that normals are only valid if collision happens.
        /// Normals are in sprite coordinate system.  At a minimum the sprite's rotation
        /// must be applied to them to be useful.
        /// </summary>
        /// <param name="s0"></param>
        /// <param name="s1"></param>
        /// <param name="normal0"></param>
        /// <param name="normal1"></param>
        /// <returns></returns>
        public static bool CollisionTest(Sprite s0, Sprite s1, ref Vector2 normal0, ref Vector2 normal1)
        {
            // It's more efficient to map from a sparcer grid onto a denser
            // one then the other way around.  So we always want to map from
            // the sprite with the larger scale onto the smaller scale.
            // Since this method maps from s1 onto s0 swap them so that
            // s1 is the sprite with the larger scale.
            bool swapped = false;
            if (s0.Scale > s1.Scale)
            {
                Sprite tmp = s0;
                s0 = s1;
                s1 = tmp;

                swapped = true;
            }


            // Do quick circle/circle test.  Early out if possible.
            float distSquared = (s0.Position - s1.Position).LengthSquared();
            float radiiSquared = (s0.BoundingRadius + s1.BoundingRadius);
            radiiSquared *= radiiSquared;

            SpriteBatch batch2 = KoiLibrary.SpriteBatch;
            SpriteFont font2 = KoiLibrary.UIFont10;
            Vector2 pos2 = new Vector2(10, 10);

            if (distSquared > radiiSquared)
            {
                //batch2.DrawString(font2, "circle miss", pos2, Color.White);

                // TODO Replace this when done testing...
                //return false;
            }
            else
            {
                //batch2.DrawString(font2, "circle hit", pos2, Color.Red);
            }



            // Transform to go put s1 into s0's space.
            Matrix mat = Matrix.Identity;

            mat *= Matrix.CreateScale(s1.Scale);
            mat *= Matrix.CreateTranslation(new Vector3(-s1.Origin * s1.Scale, 0));
            mat *= Matrix.CreateRotationZ(s1.Rotation);
            mat *= Matrix.CreateTranslation(new Vector3(s1.Position - s0.Position, 0));
            mat *= Matrix.CreateRotationZ(-s0.Rotation);
            mat *= Matrix.CreateTranslation(new Vector3(s0.Origin * s0.Scale, 0));
            mat *= Matrix.CreateScale(1.0f / s0.Scale);

            Matrix invMat = Matrix.Invert(mat);

#if COLLISION_DEBUG

            Vector2 test = Vector2.Transform(s1.Position, mat);

            SpriteBatch batch = KoiLibrary.SpriteBatch;
            SpriteFont font = KoiLibrary.UIFont10;
            Vector2 pos = new Vector2(10, 50);

            batch.DrawString(font, "s1 pos : " + s1.Position.ToString(), pos, Color.Black);
            pos.Y += font.LineSpacing;
            batch.DrawString(font, "s0 pos : " + s0.Position.ToString(), pos, Color.Black);
            pos.Y += font.LineSpacing;
            pos.Y += font.LineSpacing;

            //batch.DrawString(font, "mat : " + s1.Position.ToString() + " -> " + test.ToString(), pos, Color.Black);
            pos.Y += font.LineSpacing;
            pos.Y += font.LineSpacing;

            bool once = false;
#endif

            // TODO (****) Figure out proper map level to use (if available).  Should just be a function of scale.
            // Note that we only care about s1's scaling since that's the source.
            byte[,] map0 = s0.collisionMaps[0];
            byte[,] map1 = s1.collisionMaps[0];

            // Transform corners of s0 back into s1 space so we can calc the extent
            // of the overlap and limit the number of pixels we look at.
            Vector2 p0 = new Vector2(0, 0);
            Vector2 p1 = new Vector2(map0.GetLength(0), 0);
            Vector2 p2 = new Vector2(map0.GetLength(0), map0.GetLength(1));
            Vector2 p3 = new Vector2(0, map0.GetLength(1));

            p0 = Vector2.Transform(p0, invMat);
            p1 = Vector2.Transform(p1, invMat);
            p2 = Vector2.Transform(p2, invMat);
            p3 = Vector2.Transform(p3, invMat);

#if INV_DEBUG
            {
                SpriteBatch batch = KoiLibrary.SpriteBatch;
                SpriteFont font = KoiLibrary.UIFont10;
                Vector2 pos = new Vector2(10, 50);

                batch.DrawString(font, "p0 : " + p0.ToString(), pos, Color.Black);
                pos.Y += font.LineSpacing;
                batch.DrawString(font, "p1 : " + p1.ToString(), pos, Color.Black);
                pos.Y += font.LineSpacing;
                batch.DrawString(font, "p2 : " + p2.ToString(), pos, Color.Black);
                pos.Y += font.LineSpacing;
                batch.DrawString(font, "p3 : " + p3.ToString(), pos, Color.Black);
                pos.Y += font.LineSpacing;
            }
#endif

            // Create 4 line segments out of the points.
            LineSegment[] segs = new LineSegment[4];
            segs[0].a = p0;
            segs[0].b = p1;
            segs[1].a = p1;
            segs[1].b = p2;
            segs[2].a = p2;
            segs[2].b = p3;
            segs[3].a = p3;
            segs[3].b = p0;

            // Clip those line segments to s1's rect.
            Vector2 min = new Vector2(float.MaxValue);
            Vector2 max = new Vector2(float.MinValue);
            for (int l = 0; l < 4; l++)
            {
                LineSegment seg = segs[l];
                // Clip against x = 0
                if (seg.a.X < 0 && seg.b.X >= 0)
                {
                    float t = -seg.a.X / (seg.b.X - seg.a.X);
                    seg.a = MyMath.Lerp(seg.a, seg.b, t);
                }
                else if (seg.a.X >= 0 && seg.b.X < 0)
                {
                    float t = -seg.b.X / (seg.a.X - seg.b.X);
                    seg.b = MyMath.Lerp(seg.b, seg.a, t);
                }
                // Clip against y = 0
                if (seg.a.Y < 0 && seg.b.Y >= 0)
                {
                    float t = -seg.a.Y / (seg.b.Y - seg.a.Y);
                    seg.a = MyMath.Lerp(seg.a, seg.b, t);
                }
                else if (seg.a.Y >= 0 && seg.b.Y < 0)
                {
                    float t = -seg.b.Y / (seg.a.Y - seg.b.Y);
                    seg.b = MyMath.Lerp(seg.b, seg.a, t);
                }
                // Clip against X = map1.GetLength(0)
                int x = map1.GetLength(0);
                if (seg.a.X < x && seg.b.X >= x)
                {
                    float t = (x - seg.a.X) / (seg.b.X - seg.a.X);
                    seg.b = MyMath.Lerp(seg.a, seg.b, t);
                }
                else if (seg.a.X >= x && seg.b.X < x)
                {
                    float t = (x - seg.b.X) / (seg.a.X - seg.b.X);
                    seg.a = MyMath.Lerp(seg.b, seg.a, t);
                }
                // Clip against Y = map1.GetLength(1)
                int y = map1.GetLength(0);
                if (seg.a.Y < y && seg.b.Y >= y)
                {
                    float t = (y - seg.a.Y) / (seg.b.Y - seg.a.Y);
                    seg.b = MyMath.Lerp(seg.a, seg.b, t);
                }
                else if (seg.a.Y >= y && seg.b.Y < y)
                {
                    float t = (y - seg.b.Y) / (seg.a.Y - seg.b.Y);
                    seg.a = MyMath.Lerp(seg.b, seg.a, t);
                }

                min = MyMath.Min(min, seg.a);
                min = MyMath.Min(min, seg.b);
                max = MyMath.Max(max, seg.a);
                max = MyMath.Max(max, seg.b);
            }

            // Use the extent of the clipped segments to calc the rect to sample from.
            min = MyMath.Max(min, Vector2.Zero);
            max = MyMath.Min(max, new Vector2(map1.GetLength(0), map1.GetLength(1)));

            for (int i = (int)min.X; i < max.X; i++)
            {
                for (int j = (int)min.Y; j < max.Y; j++)
                {
                    // Don't bother transforming blank pixels.
                    if (map1[i, j] > collisionThreshold)
                    {
                        Vector2 uv1 = new Vector2(i, j);
                        Vector2 uv0 = Vector2.Transform(uv1, mat);

#if COLLISION_DEBUG
                        if (!once)
                        {
                            batch.DrawString(font, "uv1 : " + uv1.ToString() + " -> uv0 : " + uv0.ToString(), pos, Color.Black);
                            pos.Y += font.LineSpacing;

                            once = true;
                        }
#endif

                        if (uv0.X >= 0 && uv0.Y >= 0 && uv0.X < map0.GetLength(0) && uv0.Y < map0.GetLength(1))
                        {
                            float sample = map0[(int)uv0.X, (int)uv0.Y];
                            if (sample > collisionThreshold)
                            {
                                // Fill in normals.
                                normal0 = s0.normalMap[(int)uv0.X, (int)uv0.Y];
                                normal1 = s1.normalMap[(int)uv1.X, (int)uv1.Y];

                                if (swapped)
                                {
                                    Vector2 tmp = normal0;
                                    normal0 = normal1;
                                    normal1 = tmp;
                                }
#if COLLISION_DEBUG
                                batch.DrawString(font, "hit", pos, Color.Red);
                                pos.Y += font.LineSpacing;
                                batch.DrawString(font, "uv1 : " + uv1.ToString() + " -> uv0 : " + uv0.ToString(), pos, Color.Black);
                                pos.Y += font.LineSpacing;
#endif
                                return true;
                            }
                        }
                    }
                }
            }

#if COLLISION_DEBUG
            batch.DrawString(font, "miss", pos, Color.Black);
#endif
            return false;
        }   // end of CollidesWith()

        #endregion

        #region Internal

        /// <summary>
        /// Grabs the texture data and generates the collision and normal maps.
        /// </summary>
        /// <param name="data">Texture data, if null or missing, reads from the texture.  Usefull if we've already grabbed the texture data.</param>
        protected void GenerateMaps(Color[] data = null)
        {
            if (data == null)
            {
                data = new Color[texture.Width * texture.Height];
                texture.GetData<Color>(data);
            }
            else
            {
                Debug.Assert(data.Length == texture.Width * texture.Height, "Wrong size!");
            }

            //
            // Create collision mipmaps.  Only store alpha value.
            //
            collisionMaps = new List<byte[,]>();
            for (int levelNum = 0; levelNum < numCollisionMapLevels; levelNum++)
            {
                int w = texture.Width >> levelNum;
                int h = texture.Height >> levelNum;
                
                byte[,] level = new byte[w, h];

                // Full size level?  Get info direct from image.
                if (levelNum == 0)
                {
                    for (int i = 0; i < w; i++)
                    {
                        for (int j = 0; j < h; j++)
                        {
                            int index = i + j * w;
                            level[i, j] = data[index].A;
                        }
                    }
                }
                else
                {
                    // Not first level so get results by taking an average of the previous level.
                    for (int i = 0; i < w; i++)
                    {
                        for (int j = 0; j < h; j++)
                        {
                            level[i, j] = (byte)(((int)collisionMaps[levelNum - 1][i * 2, j * 2] + (int)collisionMaps[levelNum - 1][i * 2 + 1, j * 2] + (int)collisionMaps[levelNum - 1][i * 2, j * 2 + 1] + (int)collisionMaps[levelNum - 1][i * 2 + 1, j * 2 + 1]) / 4);
                        }
                    }
                }

                collisionMaps.Add(level);
            }

            //
            // Create normal map.
            //
            normalMap = new Vector2[texture.Width, texture.Height];

            // Filter with large kernel using alpha values to determine where the shape is.
            // Normals should point away from the shape.
            // This only does the edges.
            float kernelRadius = 4.0f;
            for (int i = 0; i < texture.Width; i++)
            {
                for (int j = 0; j < texture.Height; j++)
                {
                    Vector2 norm = Vector2.Zero;
                    for (int ii = i - (int)kernelRadius; ii < i + (int)kernelRadius + 1; ii++)
                    {
                        for (int jj = j - (int)kernelRadius; jj < j + (int)kernelRadius + 1; jj++)
                        {
                            // Only calc normal for pixels that aren't totally transparent.
                            if (collisionMaps[0][i, j] > 0)
                            {
                                // If a valid sample.
                                if (ii >= 0 && jj >= 0 && ii < texture.Width && jj < texture.Height && i != ii && j != jj && collisionMaps[0][ii, jj] > 0.0f)
                                {
                                    float radSquared = (ii - i) * (ii - i) + (jj - j) * (jj - j);
                                    float rad = (float)Math.Sqrt(radSquared);
                                    if (rad <= kernelRadius)
                                    {
                                        //norm -= collisionMaps[0][ii, jj] * new Vector2(ii - i, jj - j) * (1.0f - rad / kernelRadius);
                                        norm += collisionMaps[0][ii, jj] * new Vector2(i - ii, j - jj);
                                    }
                                }
                            }
                        }
                    }
                    if (norm != Vector2.Zero)
                    {
                        normalMap[i, j] = norm;
                        normalMap[i, j].Normalize();
                    }
                }
            }

            // Fill in center parts.
            // forever
            //      find seed position.
            //      if no seed, break out of loop.
            //      calc cur pixel based on neighbors.
            //      step to next neighbor.  
            //          "step" direction depends on previous step direction
            //          first try left, then forward, then right.
            //          break if none of the neighbors are viable and look for another seed.

            // Direction constants:
            //  dir = 0 =>  0,  1
            //  dir = 1 =>  1,  0
            //  dir = 2 =>  0, -1
            //  dir = 3 => -1,  0
            int dir = 0;
            while (true)
            {
                // Find seed.
                bool found = false;
                int i = 0;
                int j = 0;
                for (i = 0; i < texture.Width; i++)
                {
                    for (j = 0; j < texture.Height; j++)
                    {
                        if (collisionMaps[0][i, j] > 0 && normalMap[i, j] == Vector2.Zero)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found)
                    {
                        break;
                    }
                }

                if (!found)
                {
                    // No seed found, must be done.
                    break;
                }

                while(true)
                {
                    // Calc value for i,j 
                    for (int ii = i - (int)kernelRadius; ii < i + (int)kernelRadius + 1; ii++)
                    {
                        for (int jj = j - (int)kernelRadius; jj < j + (int)kernelRadius + 1; jj++)
                        {
                            // If a valid sample.
                            if (ii >= 0 && jj >= 0 && ii < texture.Width && jj < texture.Height && i != ii && j != jj && collisionMaps[0][ii, jj] > 0.0f)
                            {
                                float radSquared = (ii - i) * (ii - i) + (jj - j) * (jj - j);
                                float rad = (float)Math.Sqrt(radSquared);
                                if (rad <= kernelRadius)
                                {
                                    normalMap[i, j] += normalMap[ii, jj];
                                }
                            }
                        }
                    }
                    normalMap[i, j].Normalize();

                    switch (dir)
                    {
                        case 0:
                            // left
                            if (normalMap[i + 1, j] == Vector2.Zero)
                            {
                                i = i + 1;
                                dir = 1;
                                continue;
                            }
                            // forward
                            if (normalMap[i, j + 1] == Vector2.Zero)
                            {
                                j = j + 1;
                                dir = 0;
                                continue;
                            }
                            // right
                            if (normalMap[i - 1, j] == Vector2.Zero)
                            {
                                i = i - 1;
                                dir = 3;
                                continue;
                            }
                            break;
                        case 1:
                            // left
                            if (normalMap[i, j - 1] == Vector2.Zero)
                            {
                                j = j - 1;
                                dir = 2;
                                continue;
                            }
                            // forward
                            if (normalMap[i + 1, j] == Vector2.Zero)
                            {
                                i = i + 1;
                                dir = 1;
                                continue;
                            }
                            // right
                            if (normalMap[i, j + 1] == Vector2.Zero)
                            {
                                j = j + 1;
                                dir = 0;
                                continue;
                            }
                            break;
                        case 2:
                            // left
                            if (normalMap[i - 1, j] == Vector2.Zero)
                            {
                                i = i - 1;
                                dir = 3;
                                continue;
                            }
                            // forward
                            if (normalMap[i, j - 1] == Vector2.Zero)
                            {
                                j = j - 1;
                                dir = 3;
                                continue;
                            }
                            // right
                            if (normalMap[i + 1, j] == Vector2.Zero)
                            {
                                i = i + 1;
                                dir = 1;
                                continue;
                            }
                            break;
                        case 3:
                            // left
                            if (normalMap[i, j + 1] == Vector2.Zero)
                            {
                                j = j + 1;
                                dir = 0;
                                continue;
                            }
                            // forward
                            if (normalMap[i - 1, j] == Vector2.Zero)
                            {
                                i = i - 1;
                                dir = 3;
                                continue;
                            }
                            // right
                            if (normalMap[i, j - 1] == Vector2.Zero)
                            {
                                j = j - 1;
                                dir = 2;
                                continue;
                            }
                            break;
                    }

                    // If we're here, we need to fine the next seed.  Fall through the loop.
                    break;
                }
                

            }   // end of infinite loop finding and filling seeds.

            // DEBUG ONLY Copy normal data back to color channels.
            /*
            for (int i = 0; i < texture.Width; i++)
            {
                for (int j = 0; j < texture.Height; j++)
                {
                    int index = i + j * texture.Width;
                    data[index] = new Color(0.5f + 0.5f * normalMap[i, j].X, 0.5f + 0.5f * normalMap[i, j].Y, 0, data[index].A / 255.0f);
                }
            }
            texture.SetData<Color>(data);
            */
        }   // end of GenerateMaps()

        /// <summary>
        /// Calculates the bounding circle radius based on the current origin.
        /// This assumes that we don't call this all the time (ie origin isn't 
        /// updated a lot).  So we use the pixel values to calc a tight bound.
        /// </summary>
        void CalcBoundingRadius()
        {
            float maxRadius2 = 0;   // Max radius squared.

            byte[,] level = collisionMaps[0];
            int w = level.GetUpperBound(0);
            int h = level.GetUpperBound(1);

            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    // If pixel has any alpha, consider it.
                    if (level[i, j] > 0)
                    {
                        float dist2 = (new Vector2(origin.X, origin.Y) - new Vector2(i, j)).LengthSquared();
                        if (dist2 > maxRadius2)
                        {
                            maxRadius2 = dist2;
                        }
                    }
                }
            }

            boundingRadius = (float)Math.Sqrt(maxRadius2);

        }   // end of GenerateBoundingCircle()

        #endregion


        #region IDeviceReset Members

        public void LoadContent()
        {
            if (texture == null)
            {
                texture = KoiLibrary.LoadTexture2D(textureName);

                if (texture != null)
                {
                    GenerateMaps();

                    // Have the origin default to the center of the texture.
                    // Note this fails if we go straight to 0, 0 as the origin
                    // since the Origin accessor only calculates a new bound
                    // if the origin has changed.  So always set it to something
                    // other than 0, 0 first.
                    Origin = 0.5f * new Vector2(texture.Width, texture.Height);

                    //Origin = 0.2f * new Vector2(texture.Width, texture.Height);
                    //Origin = 0.0f * new Vector2(texture.Width, texture.Height);
                }
            }
#if HIT_TEST_DEBUG
            disc = KoiLibrary.LoadTexture2D(@"Content\Textures\Disc");
#endif
        }   // end of LoadContent()

        public void UnloadContent()
        {
            DeviceResetX.Release(ref texture);
        }   // end of UnloadContent()

        public void DeviceResetHandler(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        #endregion
    }   // end of class Sprite

}   // end of namespace KoiX
