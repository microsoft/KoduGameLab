
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

namespace Boku.Common
{

    /// <summary>
    /// Additional collection of static math methods.
    /// </summary>
    class MyMath
    {
        /// <summary>
        /// Linear Interpolation from a to b based on t.  When t==0
        /// the result is a, when t==1 the result is b.
        /// </summary>
        static public float Lerp(float a, float b, float t)
        {
            return a * (1 - t) + b * t;
        }
        static public double Lerp(double a, double b, double t)
        {
            return a * (1 - t) + b * t;
        }
        static public Vector2 Lerp(Vector2 a, Vector2 b, float t)
        {
            return a * (1 - t) + b * t;
        }
        static public Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            return a * (1 - t) + b * t;
        }
        static public Vector4 Lerp(Vector4 a, Vector4 b, float t)
        {
            return a * (1 - t) + b * t;
        }
        static public Matrix Lerp(ref Matrix a, ref Matrix b, float t)
        {
            // Yes, this is wrong mathematically but it works for what we currently 
            // need and is a lot quicker than trying to decompose the matrices into 
            // their individual rotations, translations and scalings.

            Matrix result = Matrix.Identity;
            result.M11 = Lerp(a.M11, b.M11, t);
            result.M12 = Lerp(a.M12, b.M12, t);
            result.M13 = Lerp(a.M13, b.M13, t);
            result.M14 = Lerp(a.M14, b.M14, t);
            result.M21 = Lerp(a.M21, b.M21, t);
            result.M22 = Lerp(a.M22, b.M22, t);
            result.M23 = Lerp(a.M23, b.M23, t);
            result.M24 = Lerp(a.M24, b.M24, t);
            result.M31 = Lerp(a.M31, b.M31, t);
            result.M32 = Lerp(a.M32, b.M32, t);
            result.M33 = Lerp(a.M33, b.M33, t);
            result.M34 = Lerp(a.M34, b.M34, t);
            result.M41 = Lerp(a.M41, b.M41, t);
            result.M42 = Lerp(a.M42, b.M42, t);
            result.M43 = Lerp(a.M43, b.M43, t);
            result.M44 = Lerp(a.M44, b.M44, t);

            return result;
        }

        //math helper function for easing a float towards a target
        static public float InterpTo(float current, float target, float interpSpeed)
        {
            Debug.Assert(interpSpeed > 0.0f);

            float delta = target - current;

            //close enough?
            if (Math.Abs(delta) < 0.001f)
            {
                return target;
            }

            //blend towards target
            return current + delta * MathHelper.Clamp(Time.WallClockFrameSeconds * interpSpeed, 0.0f, 1.0f);
        }

        //math helper function for easing a Vector2 towards a target
        static public Vector2 InterpTo(Vector2 current, Vector2 target, float interpSpeed)
        {
            Debug.Assert(interpSpeed > 0.0f);

            Vector2 delta = target - current;

            //close enough?
            if (delta.LengthSquared() < 0.00001f)
            {
                return target;
            }

            //blend towards target
            return current + delta * MathHelper.Clamp(Time.WallClockFrameSeconds * interpSpeed, 0.0f, 1.0f);
        }

        //math helper function for easing a Vector3 towards a target
        static public Vector3 InterpTo(Vector3 current, Vector3 target, float interpSpeed)
        {
            Debug.Assert(interpSpeed > 0.0f);

            Vector3 delta = target - current;

            //close enough?
            if (delta.LengthSquared() < 0.00001f)
            {
                return target;
            }

            //blend towards target
            return current + delta * MathHelper.Clamp(Time.WallClockFrameSeconds * interpSpeed, 0.0f, 1.0f);
        }

        static public float SmoothStep(float a, float b, float t)
        {
            if (t <= a)
            {
                return 0.0f;
            }
            else if (t >= b)
            {
                return 1.0f;
            }
            else
            {
                t = (t - a) / (b - a);
                return -2.0f * t * t * t + 3.0f * t * t;
            }
        }   // end of MyMath SmoothStep()

        static public double SmoothStep(double a, double b, double t)
        {
            if (t <= a)
            {
                return 0.0;
            }
            else if (t >= b)
            {
                return 1.0;
            }
            else
            {
                t = (t - a) / (b - a);
                return -2.0 * t * t * t + 3.0 * t * t;
            }
        }   // end of MyMath SmoothStep()

        /// <summary>
        /// Comparison with error bound.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        static public bool Equals(float left, float right, float error)
        {
            float diff = left - right;
            if (diff < error && diff > -error)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Comparison with error bound.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        static public bool Equals(Vector2 left, Vector2 right, float error)
        {
            float xDiff = left.X - right.X;
            if (xDiff < error && xDiff > -error)
            {
                float yDiff = left.Y - right.Y;
                if (yDiff < error && yDiff > -error)
                {
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// Returns true if the input value is a power of two.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        static public bool IsPowerOfTwo(int i)
        {
            return (i & (i - 1)) == 0;
        }   // end of MyMath IsPowerOfTwo()

        /// <summary>
        /// Returns a value this is >= i and is a power of two.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        static public int GetNextPowerOfTwo(int i)
        {
            // If we're already a power of two, just return.
            if (IsPowerOfTwo(i))
            {
                return i;
            }

            // Shift high bit (and all others) up to next position.
            i = i << 1;

            // Strip off low bits until only the high bit remains.
            while (true)
            {
                // Strip off current low bit.
                i = i & (i - 1);

                // See if we're done.
                if (IsPowerOfTwo(i))
                {
                    return i;
                }
            }
        }   // end of MyMath GetNextPowerOfTwo()

        static public float Wrap(float val, float min, float max)
        {
            val -= min;
            val /= (max - min);
            val -= (float)(Math.Floor(val));
            val *= (max - min);
            val += min;
            return val;
        } // Wrap val between min and max
        static public Vector2 Wrap(Vector2 val, Vector2 min, Vector2 max)
        {
            return new Vector2(Wrap(val.X, min.X, max.X),
                                Wrap(val.Y, min.Y, max.Y));
        } // Wrap val between min and max
        static public Vector2 Wrap(Vector2 val, float min, float max)
        {
            return Wrap(val, new Vector2(min), new Vector2(max));
        } // Wrap val between min and max
        static public Vector2 Wrap(Vector2 val)
        {
            return Wrap(val, 0.0f, 1.0f);
        } // Wrap val between min and max
        static public Vector3 Wrap(Vector3 val, Vector3 min, Vector3 max)
        {
            return new Vector3(Wrap(val.X, min.X, max.X),
                                Wrap(val.Y, min.Y, max.Y),
                                Wrap(val.Z, min.Z, max.Z));
        } // Wrap val between min and max
        static public Vector3 Wrap(Vector3 val, float min, float max)
        {
            return Wrap(val, new Vector3(min), new Vector3(max));
        } // Wrap val between min and max
        static public Vector3 Wrap(Vector3 val)
        {
            return Wrap(val, 0.0f, 1.0f);
        } // Wrap val between min and max
        static public Vector4 Wrap(Vector4 val, Vector4 min, Vector4 max)
        {
            return new Vector4(Wrap(val.X, min.X, max.X),
                                Wrap(val.Y, min.Y, max.Y),
                                Wrap(val.Z, min.Z, max.Z),
                                Wrap(val.W, min.W, max.W));
        } // Wrap val between min and max
        static public Vector4 Wrap(Vector4 val, float min, float max)
        {
            return Wrap(val, new Vector4(min), new Vector4(max));
        } // Wrap val between min and max
        static public Vector4 Wrap(Vector4 val)
        {
            return Wrap(val, 0.0f, 1.0f);
        } // Wrap val between min and max

        /// <summary>
        /// Floating point modulo that handles negative numbers correctly.
        /// Mostly used to map rotation angles into 0..2pi range.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        static public float Modulo(float a, float b)
        {
            return (a % b + b) % b;
        }

        /// <summary>
        /// If value is NaN, return fallback value.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        static public float NanProtect(float v, float fallback)
        {
            if (float.IsNaN(v))
                return fallback;
            return v;
        }

        /// <summary>
        /// If any members are NaN, return fallback value.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        static public Vector2 NanProtect(Vector2 v, Vector2 fallback)
        {
            if (float.IsNaN(v.X) || float.IsNaN(v.Y))
                return fallback;
            return v;
        }

        /// <summary>
        /// If any members are NaN, return fallback value.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        static public Vector3 NanProtect(Vector3 v, Vector3 fallback)
        {
            if (float.IsNaN(v.X) || float.IsNaN(v.Y) || float.IsNaN(v.Z))
                return fallback;
            return v;
        }

        /// <summary>
        /// Returns 1 if v >= 0, -1 otherwise.
        /// 
        /// TODO (****) remove/rename this.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        static public int Direction(float v)
        {
            return (v >= 0) ? 1 : -1;
        }

        static public float Max(float[] values)
        {
            Debug.Assert(values.Length > 2);
            float result = values[0];
            for (int i = 1; i < values.Length; ++i)
            {
                if (values[i] > result)
                    result = values[i];
            }
            return result;
        }

        static public T Clamp<T>(T value, T min, T max) where T : System.IComparable<T>
        {
            T result = value;
            if (value.CompareTo(min) < 0)
                result = min;
            if (value.CompareTo(max) > 0)
                result = max;
            return result;
        }

        /// <summary>
        /// Returns angle in radians from the given direction vector.
        /// Result is in range [0, 2pi)
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        static public float ZRotationFromDirection(Vector3 dir)
        {
            float angle = 0.0f;

            dir.Z = 0;
            dir.Normalize();

            // If dir was 0 length, just return 0.0 as the angle.
            if (!float.IsNaN(dir.X))
            {
                angle = (float)Math.Acos(dir.X);
                if (dir.Y < 0.0f)
                {
                    angle = MathHelper.TwoPi - angle;
                }
            }
            return angle;
        }

        /// <summary>
        /// Return the index of the value's most significant bit.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        static public int HighBitPos(int n)
        {
            Debug.Assert(n > 0);
            int b = 0;
            if (0 != ((uint)n & (~0u << (1 << 4)))) { b |= (1 << 4); n >>= (1 << 4); }
            if (0 != ((uint)n & (~0u << (1 << 3)))) { b |= (1 << 3); n >>= (1 << 3); }
            if (0 != ((uint)n & (~0u << (1 << 2)))) { b |= (1 << 2); n >>= (1 << 2); }
            if (0 != ((uint)n & (~0u << (1 << 1)))) { b |= (1 << 1); n >>= (1 << 1); }
            if (0 != ((uint)n & (~0u << (1 << 0)))) { b |= (1 << 0); }
            return b;
        }

        /// <summary>
        /// Remaps a value from one range to another.
        /// Not sure what, exactly to call this but I end up doing it all the time.
        ///     RemapRange(value, 0, 100, 32, 212)  -- map from degrees Celcius to Fahrenheit
        ///     RemapRange(value, 0, 2pi, 0, 360)   -- map from radians to degrees
        ///     RemapRange(value, 1, 0.8, 0, 1)     -- map which inverts direction of magnitude (bigger input -> smaller output) 
        /// 
        /// Note that the value doesn't have to be inside the range.  
        /// The mapping is done linearly.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="src0"></param>
        /// <param name="src1"></param>
        /// <param name="dst0"></param>
        /// <param name="dst1"></param>
        /// <returns></returns>
        static public float RemapRange(float value, float src0, float src1, float dst0, float dst1)
        {
            float frac = (value - src0) / (src1 - src0);
            float result = dst0 + frac * (dst1 - dst0);
            return result;
        }   // end of RemapRange()

        static public float CubicBezier(float a, float b, float c, float d, float t)
        {
            float x = (1 - t);
            float v =
                x * x * x * a +
                3 * t * x * x * b +
                3 * t * t * x * c +
                t * t * t * d;
            return v;
        }

        static public Vector3 CubicBezier(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
        {
            return new Vector3(
                CubicBezier(a.X, b.X, c.X, d.X, t),
                CubicBezier(a.Y, b.Y, c.Y, d.Y, t),
                CubicBezier(a.Z, b.Z, c.Z, d.Z, t));
        }

        /// <summary>
        /// Given a point and a line defined by 2 points, return 
        /// the point on the line nearest the input point.
        /// </summary>
        /// <param name="p">The input point</param>
        /// <param name="p0">First point on line</param>
        /// <param name="p1">Second point on line</param>
        /// <returns>Point nearest input point on line</returns>
        static public Vector3 NearestPointOnLine(Vector3 p, Vector3 p0, Vector3 p1)
        {
            Vector3 dir = p1 - p0;
            float dot0 = Vector3.Dot(dir, dir);

            if (dot0 != 0f)
            {
                Vector3 diff = p - p0;
                float dot1 = Vector3.Dot(diff, dir);
                float t = dot1 / dot0;
                p = p0 + t * dir;
            }

            return p;
        }

        private static readonly UInt32[] CRCTable =
        {
            0x00000000, 0x77073096, 0xee0e612c, 0x990951ba, 0x076dc419,
            0x706af48f, 0xe963a535, 0x9e6495a3, 0x0edb8832, 0x79dcb8a4,
            0xe0d5e91e, 0x97d2d988, 0x09b64c2b, 0x7eb17cbd, 0xe7b82d07,
            0x90bf1d91, 0x1db71064, 0x6ab020f2, 0xf3b97148, 0x84be41de,
            0x1adad47d, 0x6ddde4eb, 0xf4d4b551, 0x83d385c7, 0x136c9856,
            0x646ba8c0, 0xfd62f97a, 0x8a65c9ec, 0x14015c4f, 0x63066cd9,
            0xfa0f3d63, 0x8d080df5, 0x3b6e20c8, 0x4c69105e, 0xd56041e4,
            0xa2677172, 0x3c03e4d1, 0x4b04d447, 0xd20d85fd, 0xa50ab56b,
            0x35b5a8fa, 0x42b2986c, 0xdbbbc9d6, 0xacbcf940, 0x32d86ce3,
            0x45df5c75, 0xdcd60dcf, 0xabd13d59, 0x26d930ac, 0x51de003a,
            0xc8d75180, 0xbfd06116, 0x21b4f4b5, 0x56b3c423, 0xcfba9599,
            0xb8bda50f, 0x2802b89e, 0x5f058808, 0xc60cd9b2, 0xb10be924,
            0x2f6f7c87, 0x58684c11, 0xc1611dab, 0xb6662d3d, 0x76dc4190,
            0x01db7106, 0x98d220bc, 0xefd5102a, 0x71b18589, 0x06b6b51f,
            0x9fbfe4a5, 0xe8b8d433, 0x7807c9a2, 0x0f00f934, 0x9609a88e,
            0xe10e9818, 0x7f6a0dbb, 0x086d3d2d, 0x91646c97, 0xe6635c01,
            0x6b6b51f4, 0x1c6c6162, 0x856530d8, 0xf262004e, 0x6c0695ed,
            0x1b01a57b, 0x8208f4c1, 0xf50fc457, 0x65b0d9c6, 0x12b7e950,
            0x8bbeb8ea, 0xfcb9887c, 0x62dd1ddf, 0x15da2d49, 0x8cd37cf3,
            0xfbd44c65, 0x4db26158, 0x3ab551ce, 0xa3bc0074, 0xd4bb30e2,
            0x4adfa541, 0x3dd895d7, 0xa4d1c46d, 0xd3d6f4fb, 0x4369e96a,
            0x346ed9fc, 0xad678846, 0xda60b8d0, 0x44042d73, 0x33031de5,
            0xaa0a4c5f, 0xdd0d7cc9, 0x5005713c, 0x270241aa, 0xbe0b1010,
            0xc90c2086, 0x5768b525, 0x206f85b3, 0xb966d409, 0xce61e49f,
            0x5edef90e, 0x29d9c998, 0xb0d09822, 0xc7d7a8b4, 0x59b33d17,
            0x2eb40d81, 0xb7bd5c3b, 0xc0ba6cad, 0xedb88320, 0x9abfb3b6,
            0x03b6e20c, 0x74b1d29a, 0xead54739, 0x9dd277af, 0x04db2615,
            0x73dc1683, 0xe3630b12, 0x94643b84, 0x0d6d6a3e, 0x7a6a5aa8,
            0xe40ecf0b, 0x9309ff9d, 0x0a00ae27, 0x7d079eb1, 0xf00f9344,
            0x8708a3d2, 0x1e01f268, 0x6906c2fe, 0xf762575d, 0x806567cb,
            0x196c3671, 0x6e6b06e7, 0xfed41b76, 0x89d32be0, 0x10da7a5a,
            0x67dd4acc, 0xf9b9df6f, 0x8ebeeff9, 0x17b7be43, 0x60b08ed5,
            0xd6d6a3e8, 0xa1d1937e, 0x38d8c2c4, 0x4fdff252, 0xd1bb67f1,
            0xa6bc5767, 0x3fb506dd, 0x48b2364b, 0xd80d2bda, 0xaf0a1b4c,
            0x36034af6, 0x41047a60, 0xdf60efc3, 0xa867df55, 0x316e8eef,
            0x4669be79, 0xcb61b38c, 0xbc66831a, 0x256fd2a0, 0x5268e236,
            0xcc0c7795, 0xbb0b4703, 0x220216b9, 0x5505262f, 0xc5ba3bbe,
            0xb2bd0b28, 0x2bb45a92, 0x5cb36a04, 0xc2d7ffa7, 0xb5d0cf31,
            0x2cd99e8b, 0x5bdeae1d, 0x9b64c2b0, 0xec63f226, 0x756aa39c,
            0x026d930a, 0x9c0906a9, 0xeb0e363f, 0x72076785, 0x05005713,
            0x95bf4a82, 0xe2b87a14, 0x7bb12bae, 0x0cb61b38, 0x92d28e9b,
            0xe5d5be0d, 0x7cdcefb7, 0x0bdbdf21, 0x86d3d2d4, 0xf1d4e242,
            0x68ddb3f8, 0x1fda836e, 0x81be16cd, 0xf6b9265b, 0x6fb077e1,
            0x18b74777, 0x88085ae6, 0xff0f6a70, 0x66063bca, 0x11010b5c,
            0x8f659eff, 0xf862ae69, 0x616bffd3, 0x166ccf45, 0xa00ae278,
            0xd70dd2ee, 0x4e048354, 0x3903b3c2, 0xa7672661, 0xd06016f7,
            0x4969474d, 0x3e6e77db, 0xaed16a4a, 0xd9d65adc, 0x40df0b66,
            0x37d83bf0, 0xa9bcae53, 0xdebb9ec5, 0x47b2cf7f, 0x30b5ffe9,
            0xbdbdf21c, 0xcabac28a, 0x53b39330, 0x24b4a3a6, 0xbad03605,
            0xcdd70693, 0x54de5729, 0x23d967bf, 0xb3667a2e, 0xc4614ab8,
            0x5d681b02, 0x2a6f2b94, 0xb40bbe37, 0xc30c8ea1, 0x5a05df1b,
            0x2d02ef8d
        };

        /// <summary>
        /// Calculates CRC32
        /// </summary>
        /// <param name="data">Input array of bytes</param>
        /// <param name="numBytes">Number of bytes in array to use</param>
        /// <returns></returns>
        public static byte[] CRC32(byte[] data, int numBytes)
        {
            Debug.Assert(numBytes <= data.Length);

            UInt32 CRCVal = 0xffffffff;
            for (int i = 0; i < numBytes; i++)
            {
                CRCVal = (CRCVal >> 8) ^ CRCTable[(CRCVal & 0xff) ^ data[i]];
            }
            CRCVal ^= 0xffffffff; // Toggle operation
            byte[] result = new byte[4];

            result[0] = (byte)(CRCVal >> 24);
            result[1] = (byte)(CRCVal >> 16);
            result[2] = (byte)(CRCVal >> 8);
            result[3] = (byte)(CRCVal);

            return result;
        }

    }   // end of class MyMath

}   // end of namespace Boku.Common


