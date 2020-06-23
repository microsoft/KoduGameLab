using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Boku.Input
{
    public class MicroBitMessageWriter
    {
        private StringBuilder sb;
        private StringWriter writer;

        public MicroBitMessageWriter()
        {
            sb = new StringBuilder();
            writer = new StringWriter(sb);
        }

        public override string ToString()
        {
            return writer.ToString();
        }

        public void WriteChar(char value)
        {
            WriteCharRaw(value);
            WriteSeparater();
        }

        public void WriteChars(string value)
        {
            WriteCharsRaw(value);
            WriteSeparater();
        }

        public void WriteString(string value)
        {
            int length = value.Length;
            WriteU8HexRaw(length);
            WriteCharsRaw(value);
            WriteSeparater();
        }

        public void WriteU8Hex(int value)
        {
            WriteU8HexRaw(value);
            WriteSeparater();
        }

        public void WriteU16Hex(int value)
        {
            WriteU16HexRaw(value);
            WriteSeparater();
        }

        private void WriteSeparater()
        {
            writer.Write('|');
        }

        private void WriteCharRaw(char value)
        {
            writer.Write(value);
        }

        private void WriteCharsRaw(string value)
        {
            writer.Write(value);
        }

        private void WriteU8HexRaw(int value)
        {
            int hi = (value & 0xF0) >> 4;
            int lo = value & 0x0F;
            char hiny = ToAscii[hi];
            char lony = ToAscii[lo];
            WriteCharRaw(hiny);
            WriteCharRaw(lony);
        }

        private void WriteU16HexRaw(int value)
        {
            int hi = (value & 0xFF00) >> 8;
            int lo = value & 0x00FF;
            WriteU8HexRaw(hi);
            WriteU8HexRaw(lo);
        }

        private static readonly char[] ToAscii = {'0', '1', '2', '3', '4', '5', '6', '7',
                               '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'};

    }
}
