// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Boku.Input
{
    /// <summary>
    /// Represents a pattern to be displayed on the microbit's 5x5 LED matrix.
    /// </summary>
    public class MicroBitImage
    {
        public const int Width = 5;
        public const int Height = 5;

        public bool[,] Pixels = new bool[Width, Height];

        public static readonly MicroBitImage Empty = new MicroBitImage();

        public string Packed
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                for (int y = 0; y < Height; ++y)
                {
                    byte packed = 0;
                    for (int x = 0; x < Width; ++x)
                    {
                        if (Pixels[x, y])
                        {
                            packed |= (byte)(1 << x);
                        }
                    }
                    if (packed > 9)
                    {
                        packed += 7;
                    }
                    packed += (byte)'0';
                    sb.Append((char)packed);
                }
                return sb.ToString();
            }
            set
            {
                if (value.Length == 5)
                {
                    Clear();
                    StringReader reader = new StringReader(value);
                    for (int y = 0; y < Height; ++y)
                    {
                        byte packed = (byte)reader.Read();
                        packed -= (byte)'0';
                        if (packed > 9 + 7)
                        {
                            packed -= 7;
                        }
                        for (int x = 0; x < Width; ++x)
                        {
                            if ((packed & (byte)(1 << x)) != 0)
                            {
                                Pixels[x, y] = true;
                            }
                        }
                    }
                }
            }
        }

        public void Clear()
        {
            for (int y = 0; y < Height; ++y)
            {
                for (int x = 0; x < Width; ++x)
                {
                    Pixels[x, y] = false;
                }
            }
        }

        public MicroBitImage(bool[] LEDs)
        {
            for (int i = 0, x = 0; x < 5; ++x)
            {
                for (int y = 0; y < 5; ++y, ++i)
                {
                    Pixels[4 - y, x] = LEDs[i];
                }
            }
        }

        public MicroBitImage(bool[,] pixels = null)
        {
            if (pixels == null) return;
            if (pixels.Length != Width * Height) return;

            for (int y = 0; y < Height; ++y)
            {
                for (int x = 0; x < Width; ++x)
                {
                    Pixels[x, y] = pixels[x, y];
                }
            }
        }
    }
}
