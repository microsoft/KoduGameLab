
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;
using Boku.Common;
using Boku.UI2D;

namespace Boku.SimWorld.Terra
{
    /// <summary>
    /// Manager class to handle 2d brushes used for terrain editing.
    /// </summary>
    public class Brush2DManager
    {
        public enum BrushType
        {
            None = 0x0000,
            Soft = 0x0001,    
            Binary = 0x0002,
            All = Soft | Binary,

            StretchedSoft = 0x0010,
            StretchedBinary = 0x0020,
            StretchedAll = StretchedSoft | StretchedBinary,

            Selection = 0x100,
        }

        public class Brush2D
        {
            #region Members

            int index;                  // Index into the manager's array for this brush.
                                        // This index may be different than the index for the current set.
            BrushType type;             // Type of brush.
            string textureName;         // Resource name for this brush's texture.
            Texture2D texture;          // Texture2D for this brush.
            string tileTextureName;     // Texture2D used in brush picker.
            string helpOverlay;         // Help overlay associated with this brush.
            float[,] data;              // Raw data for the texture for CPU side manipulation.  Converted to float at load time.

            #endregion

            #region Accessors

            public BrushType Type
            {
                get { return type; }
            }

            public int Index
            {
                get { return index; }
            }

            public Texture2D Texture
            {
                get { return texture; }
                set 
                { 
                    texture = value;

                    if (texture != null)
                    {
                        // Update, in-memory version of this.
                        uint[] raw = new uint[texture.Width * texture.Height];
                        texture.GetData<uint>(raw);
                        data = new float[texture.Width, texture.Height];
                        for (int j = 0; j < texture.Height; j++)
                        {
                            for (int i = 0; i < texture.Width; i++)
                            {
                                // We only need a single channel...
                                // Should end up with value in 0..1 range.
                                data[i, j] = (raw[i + j * texture.Width] & 0x00ff) / 255.0f;
                            }
                        }
                    }
                    else
                    {
                        data = null;
                    }
                }
            }

            /// <summary>
            /// Name of texture resource that is this brush shape.
            /// </summary>
            public string TextureName
            {
                get { return textureName; }
            }

            /// <summary>
            /// Name of texture resource for tile/icon representing this brush in the UI.
            /// </summary>
            public string TileTextureName
            {
                get { return tileTextureName; }
            }

            /// <summary>
            /// Help string for this brush.
            /// </summary>
            public string HelpOverlay
            {
                get { return helpOverlay; }
            }

            #endregion

            #region Public

            public Brush2D(int index, BrushType type, string textureName, string tileTextureName, string helpOverlay)
            {
                this.index = index;
                this.type = type;
                this.textureName = textureName;
                this.tileTextureName = tileTextureName;
                this.helpOverlay = helpOverlay;
            }   // end of Brush2D c'tor

            /// <summary>
            /// Samples the brush texture based on the given in uv coordinates.
            /// Normally these are in 0..1 range.  Outside of that range is outside
            /// of the texture and false is returned.
            /// 
            /// Assumes brushes are the same across all channels.
            /// </summary>
            /// <param name="uv"></param>
            /// <param name="pointSample">Point sampled value for brush.</param>
            /// <param name="bilinearSample">Bilinear interpolated sample for brush.</param>
            /// <returns>True if uv is in brush range.  False otherwise.</returns>
            public bool Sample(Vector2 uv, out float pointSample, out float bilinearSample)
            {
                bool result = false;
                pointSample = -1;       // Set to invalid...
                bilinearSample = -1;

                // Calc samples.  Use same restrictions as old code to make sure
                // everything lines up with the rendered version of the brush.
                Point p = new Point((int)(uv.X * texture.Width), (int)(uv.Y * texture.Height));
                if (p.X >= 0 && p.Y >= 0 && p.X < texture.Width && p.Y < texture.Height)
                {
                    pointSample = data[p.X, p.Y];

                    // Convert 0..1 range into texel indices. 
                    Vector2 texels = uv * new Vector2(texture.Width, texture.Height);
                    texels.X = MathHelper.Clamp(texels.X, 0.0f, texture.Width - 1.001f);
                    texels.Y = MathHelper.Clamp(texels.Y, 0.0f, texture.Height - 1.001f); 

                    // Use bilinear interpolation to get smoothed sample.
                    int i = (int)texels.X;
                    int j = (int)texels.Y;
                    float fracX = texels.X - i;
                    float fracY = texels.Y - j;
                    bilinearSample = (1.0f - fracY) * ((1.0f - fracX) * data[i, j] + fracX * data[i + 1, j])
                                    + fracY * ((1.0f - fracX) * data[i, j + 1] + fracX * data[i + 1, j + 1]);

                    result = true;
                }

                return result;
            }   // end of Sample()

            #endregion

        }   // end of struct Brush2D

        private static List<Brush2D> brushList = null;

        #region Accessors
        /// <summary>
        /// Returns the total number of brushes.
        /// </summary>
        public static int NumBrushes
        {
            get { return brushList.Count; }
        }
        #endregion

        private Brush2DManager()
        {
        }

        public static void Init()
        {
            brushList = new List<Brush2D>();

            int curIndex = 0;

            brushList.Add(new Brush2D(curIndex++, BrushType.Binary, @"Terrain\SquareBrush", @"Terrain\Square", @"BrushPickerSquare"));
            brushList.Add(new Brush2D(curIndex++, BrushType.Binary, @"Terrain\HardBrush", @"Terrain\Round", @"BrushPickerHardRound"));
            brushList.Add(new Brush2D(curIndex++, BrushType.Soft, @"Terrain\MidBrush", @"Terrain\MediumRound", @"BrushPickerMediumRound"));
            brushList.Add(new Brush2D(curIndex++, BrushType.Soft, @"Terrain\SoftBrush", @"Terrain\SoftRound", @"BrushPickerSoftRound"));
            brushList.Add(new Brush2D(curIndex++, BrushType.Soft, @"Terrain\MottledBrush", @"Terrain\Mottled", @"BrushPickerMottled"));
            brushList.Add(new Brush2D(curIndex++, BrushType.StretchedBinary, @"Terrain\SquareBrush", @"Terrain\SquareLinear", @"BrushPickerSquareLinear"));
            brushList.Add(new Brush2D(curIndex++, BrushType.StretchedBinary, @"Terrain\HardBrush", @"Terrain\HardRoundLinear", @"BrushPickerHardRoundLinear"));
            brushList.Add(new Brush2D(curIndex++, BrushType.StretchedSoft, @"Terrain\MidBrush", @"Terrain\MediumRoundLinear", @"BrushPickerMediumRoundLinear"));
            brushList.Add(new Brush2D(curIndex++, BrushType.StretchedSoft, @"Terrain\SoftBrush", @"Terrain\SoftRoundLinear", @"BrushPickerSoftRoundLinear"));
            brushList.Add(new Brush2D(curIndex++, BrushType.StretchedSoft, @"Terrain\MottledBrush", @"Terrain\MottledLinear", @"BrushPickerMottledLinear"));
            brushList.Add(new Brush2D(curIndex++, BrushType.Selection, @"Terrain\SelectBrush", @"Terrain\Magic", @"BrushPickerMagic"));

        }   // end of Brush2DManager c'tor

        /// <summary>
        /// Returns the brush associated with the given index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Brush2D GetBrush(int index)
        {
            return index >= 0 ? brushList[index] : null;
        }   // end of Brush2DManager GetBrush()

        /// <summary>
        /// Returns an array of indices for all the brushes matching the given type.
        /// </summary>
        /// <param name="type">Type to filter on.</param>
        /// <returns></returns>
        public static int[] GetBrushSet(BrushType type)
        {
            // Count number of brushes in this set.
            int count = 0;
            foreach (Brush2D brush in brushList)
            {
                if ((brush.Type & type) != 0)
                {
                    count++;
                }
            }

            // Create and return array of indices for the brushes of this type.
            int[] result = new int[count];
            count = 0;
            foreach (Brush2D brush in brushList)
            {
                if ((brush.Type & type) != 0)
                {
                    result[count++] = brush.Index;
                }
            }

            return result;
        }   // end of Brush2DManager GetBrushSet()

        public static void LoadContent(bool immediate)
        {
            // For each brush, start loading the texture.
            foreach (Brush2D brush in brushList)
            {
                brush.Texture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\" + brush.TextureName);
            }
        }   // end of Brush2DManager LoadContent()

        public static void InitDeviceResources(GraphicsDevice device)
        {
        }

        public static void UnloadContent()
        {
            foreach (Brush2D brush in brushList)
            {
                Texture2D tex = brush.Texture;
                BokuGame.Release(ref tex);
                brush.Texture = null;
            }
        }   // end of Brush2DManager UnloadContent()

        /// <summary>
        /// Recreate render targets.
        /// </summary>
        public static void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class Brush2DManager

}   // end of namespace Boku.SimWorld.Terra
