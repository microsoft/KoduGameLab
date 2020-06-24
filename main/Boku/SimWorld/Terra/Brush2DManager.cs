
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using KoiX;
using KoiX.UI.Dialogs;

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
        public enum BrushShape
        {
            Square,
            Round,
            MediumRound,
            SoftRound,
            LinearSquare,
            LinearRound,
            Magic
        }


        public class Brush2D
        {
            #region Members

            string id;                  // Name used for this brush, matches ids used in BrushTypeDialog. 
            BrushShape shape;           // Shape of brush.
            string textureName;         // Resource name for this brush's texture.
            Texture2D texture;          // Texture2D for this brush.
            string tileTextureName;     // Texture2D used in brush picker.
            string helpOverlay;         // Help overlay associated with this brush.
            float[,] data;              // Raw data for the texture for CPU side manipulation.  Converted to float at load time.
            bool isLinear;              // Is this one of the linear brushes?

            #endregion

            #region Accessors

            public string Id
            {
                get { return id; }
            }

            /// <summary>
            /// Shape of the current brush.
            /// </summary>
            public BrushShape Shape
            {
                get { return shape; }
            }

            public bool IsLinear
            {
                get { return isLinear; }
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

            public Brush2D(string id, BrushShape shape, string textureName, string tileTextureName, string helpOverlay)
            {
                this.id = id;
                this.shape = shape;
                this.textureName = textureName;
                this.tileTextureName = tileTextureName;
                this.helpOverlay = helpOverlay;

                isLinear = (shape == BrushShape.LinearRound) || (shape == BrushShape.LinearSquare);
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

        static List<Brush2D> brushList = null;

        static Dictionary<EditModeTools, BrushShape> toolBrushDict;    // Pairing of current brush for each tool.

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

            brushList.Add(new Brush2D("square", BrushShape.Square, @"Terrain\SquareBrush", @"Terrain\Square", @"BrushPickerSquare"));
            brushList.Add(new Brush2D("round", BrushShape.Round, @"Terrain\HardBrush", @"Terrain\Round", @"BrushPickerHardRound"));
            brushList.Add(new Brush2D("mediumRound", BrushShape.MediumRound, @"Terrain\MidBrush", @"Terrain\MediumRound", @"BrushPickerMediumRound"));
            brushList.Add(new Brush2D("softRound", BrushShape.SoftRound, @"Terrain\SoftBrush", @"Terrain\SoftRound", @"BrushPickerSoftRound"));
            brushList.Add(new Brush2D("linearSquare", BrushShape.LinearSquare, @"Terrain\SquareBrush", @"Terrain\LinearSquare", @"BrushPickerSquareLinear"));
            brushList.Add(new Brush2D("linearRound", BrushShape.LinearRound, @"Terrain\HardBrush", @"Terrain\LinearRound", @"BrushPickerHardRoundLinear"));
            brushList.Add(new Brush2D("magic", BrushShape.Magic, @"Terrain\SelectBrush", @"Terrain\Magic", @"BrushPickerMagic"));

            toolBrushDict = new Dictionary<EditModeTools, BrushShape>();

        }   // end of Brush2DManager c'tor

        /// <summary>
        /// Sets the current brush to be used for the given tool.
        /// </summary>
        /// <param name="tool"></param>
        /// <param name="brush"></param>
        public static void SetBrushOnTool(EditModeTools tool, BrushShape brush)
        {
            toolBrushDict[tool] = brush;
        }   // end of SetBrushOnTool()

        /// <summary>
        /// Returns the current brush for the given tool.
        /// </summary>
        /// <param name="tool"></param>
        /// <returns></returns>
        public static BrushShape GetBrushForTool(EditModeTools tool)
        {
            BrushShape result = BrushShape.Round;   // Default.
            BrushShape brush;
            if (toolBrushDict.TryGetValue(tool, out brush))
            {
                result = brush;
            }

            return result;
        }   // end of GetBrushForTool()

        /// <summary>
        /// Returns brush with matching shape.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Brush2D GetBrush(BrushShape shape)
        {
            Brush2D result = null;

            foreach (Brush2D b in brushList)
            {
                if (b.Shape == shape)
                {
                    result = b;
                    break;
                }
            }

            return result;
        }   // end of GetBrush()

        /// <summary>
        /// Returns the brush associated with the currently active tool.
        /// </summary>
        /// <returns></returns>
        public static Brush2D GetActiveBrush()
        {
            EditModeTools tool = ToolBarDialog.CurTool;
            BrushShape brushShape = GetBrushForTool(tool);
            Brush2D brush = GetBrush(brushShape);
            return brush;
        }   // end GetActiveBrush()

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
