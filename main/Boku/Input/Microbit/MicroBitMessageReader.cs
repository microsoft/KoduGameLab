using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Boku.Input
{
    public class MicroBitMessageReader
    {
        private bool error = false;
        private string msg;
        private StringReader reader;

        public MicroBitMessageReader(string msg)
        {
            this.msg = msg;
            reader = new StringReader(this.msg);
        }

        public int Length()
        {
            return msg.Length;
        }

        public bool Consume(char value)
        {
            if (!ConsumeRaw(value))
                return Error();
            return ConsumeSeparator();
        }

        public bool Consume(string value)
        {
            if (error) return false;
            foreach (char ch in value)
            {
                if (!ConsumeRaw(ch))
                    return Error();
            }
            return ConsumeSeparator();
        }

        private bool ConsumeSeparator()
        {
            return ConsumeRaw('|');
        }

        public bool ReadChar(out char value)
        {
            if (!ReadCharRaw(out value))
                return Error();
            return ConsumeSeparator();
        }

        public bool ReadChars(char[] chars, int bufsize, out int nread)
        {
            if (!ReadCharsRaw(chars, bufsize, out nread))
                return Error();
            return ConsumeSeparator();
        }

        public bool ReadU8Hex(out int value)
        {
            if (!ReadU8HexRaw(out value))
                return Error();
            return ConsumeSeparator();
        }

        public bool ReadU16Hex(out int value)
        {
            if (!ReadU16HexRaw(out value))
                return Error();
            return ConsumeSeparator();
        }

        public bool ReadSignedU16Hex(out short value)
        {
            if (!ReadSignedU16HexRaw(out value))
                return Error();
            return ConsumeSeparator();
        }

        public bool ReadString(out string value)
        {
            value = String.Empty;
            if (error) return false;
            int length;
            if (!ReadU8HexRaw(out length))
                return Error();
            char[] chs = new char[length];
            int nread;
            if (!ReadCharsRaw(chs, length, out nread) || nread != length)
                return Error();
            value = new string(chs, 0, length);
            return ConsumeSeparator();
        }

        public bool ReadToEnd(out string value)
        {
            value = reader.ReadToEnd();
            return true;
        }

        // Supports up to base-36
        private static int FromAscii(char c)
        {
            int value = 0;
            // 0-9?
            if (c >= '0' && c <= '9')
            {
                value = c - '0';
            }
            else
            {
                // A-Z?
                // c &= ~0x20;
                if (c >= 'A' && c <= 'Z')
                {
                    value = 10 + c - 'A';
                }
            }
            return value;
        }

        private bool Error()
        {
            error = true;
            return false;
        }

        private bool ConsumeRaw(char value)
        {
            if (error) return false;
            int ch = reader.Read();
            return (ch == value) ? true : Error();
        }

        private bool ReadCharRaw(out char value)
        {
            value = '\0';
            if (error) return false;
            int ch = reader.Read();
            if (ch >= 0)
            {
                value = (char)ch;
                return true;
            }
            return Error();
        }

        private bool ReadCharsRaw(char[] chars, int bufsize, out int nread)
        {
            nread = 0;
            if (error) return false;
            int index = 0;
            while (bufsize-- > 0)
            {
                if (!ReadCharRaw(out chars[index]))
                    break;
                ++index;
                ++nread;
            }
            return true;
        }

        private bool ReadU8HexRaw(out int value)
        {
            value = 0;
            if (error) return false;
            char[] chs = new char[2];
            int nread;
            if (!ReadCharsRaw(chs, 2, out nread) || nread != 2)
                return Error();
            int hi = FromAscii(chs[0]);
            int lo = FromAscii(chs[1]);
            value = (hi << 4) | lo;
            return true;
        }

        private bool ReadU16HexRaw(out int value)
        {
            value = 0;
            if (error) return false;
            int hi;
            int lo;
            if (!ReadU8HexRaw(out hi))
                return Error();
            if (!ReadU8HexRaw(out lo))
                return Error();
            value = (hi << 8) | lo;
            return true;
        }

        private bool ReadSignedU16HexRaw(out short value)
        {
            value = 0;
            if (error) return false;
            int hi;
            int lo;
            if (!ReadU8HexRaw(out hi))
                return Error();
            if (!ReadU8HexRaw(out lo))
                return Error();
            value = (short)((hi << 8) | lo);
            return true;
        }
    }
}
