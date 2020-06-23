
#region Using
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Microsoft.Xna.Framework.Storage;

using System.Xml.Serialization;
#endregion Using


/* This is an extension of the Storage class defined in Storage.cs
 * The goal here is to isolate the graphics dependencies so that it's easier to
 * re-use Storage.cs in other projects (such as BokuPreBoot.)
 */
namespace Boku.Common
{
    public partial class Storage4
    {
        #region Constants for DDS ReadWrite
        private const int INT_SIZE = 12;
        private const UInt32 DDS_FILE_MAGIC_NUMBER = 0x20534444;
        private const UInt32 DDSD_MIPMAPCOUNT = 0x00020000;
        private const UInt32 DDSD_CAPS = 0x00000001;
        private const UInt32 DDSD_HEIGHT = 0x00000002;
        private const UInt32 DDSD_WIDTH = 0x00000004;
        private const UInt32 DDSD_PITCH = 0x00000008;
        private const UInt32 DDSD_PIXELFORMAT = 0x00001000;
        private const UInt32 DDSD_LINEARSIZE = 0x00080000;
        private const UInt32 DDSD_DEPTH = 0x00800000;

        private const UInt32 DDPF_RGB = 0x00000040;
        private const UInt32 DDSCAPS_COMPLEX = 0x00000008;
        private const UInt32 DDSCAPS_TEXTURE = 0x00001000;
        private const UInt32 DDSCAPS_MIPMAP = 0x00400000;
        #endregion Constants for DDS ReadWrite

        #region File Name Format Constants
        private const string FULL_FILE_PATH_FORMAT = "{0}.{1}";
        #endregion

        #region Texture2D Specific

        /// <summary>
        /// Save texture as a .dds
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        static public bool TextureSaveAsDDS(Texture2D tex, string name)
        {
            name = StripTextureExt(name);

            if (tex != null)
            {
                Stream stream = OpenWrite(name + ".dds");
                BinaryWriter writer = new BinaryWriter(stream);

                writer.Write(DDS_FILE_MAGIC_NUMBER);

                const UInt32 dwSz = 124;
                writer.Write(dwSz);

                UInt32 flags = DDSD_CAPS | DDSD_PIXELFORMAT | DDSD_WIDTH | DDSD_HEIGHT;
                flags |= DDSD_PITCH; // uncompressed.
                writer.Write(flags);

                writer.Write((UInt32)tex.Height);
                writer.Write((UInt32)tex.Width);

                UInt32 bitsPerPix = 32;

                UInt32 pitch = ((UInt32)tex.Width * bitsPerPix) / 8;
                writer.Write(pitch);

                writer.Write((UInt32)0); // depth

                writer.Write((UInt32)1); // num mips

                /// Skip 11 reserved DWORDs
                for (int i = 0; i < 11; ++i)
                {
                    writer.Write((UInt32)0);
                }

                /// Surface pixel format
                #region SurfacePixelFormat
                {
                    writer.Write((UInt32)32); // dwSize of PixelFormat

                    writer.Write(DDPF_RGB);

                    writer.Write((UInt32)0); // fourCC code

                    writer.Write(bitsPerPix);

                    writer.Write((UInt32)0x00ff0000); // red bit mask
                    writer.Write((UInt32)0x0000ff00); // green bit mask
                    writer.Write((UInt32)0x000000ff); // blue bit mask
                    writer.Write((UInt32)0xff000000); // alpha bit mask
                }
                #endregion SurfacePixelFormat

                #region SurfaceCapabilities
                {
                    writer.Write(DDSCAPS_TEXTURE); // just a vanilla texture.

                    writer.Write((UInt32)0); // no cubes or volumes

                    // Skip 2 reserved DWORDS
                    writer.Write((UInt32)0);
                    writer.Write((UInt32)0);
                }
                #endregion SurfaceCapabilities

                writer.Write((UInt32)0); // Another unused/reserved.

                int numPix = tex.Width * tex.Height;

                Color[] pix = new Color[numPix];
                tex.GetData<Color>(pix);

                for (int i = 0; i < numPix; ++i)
                {
                    // Swap red and blue channels
                    Color pixel = pix[i];
                    byte tmp = pixel.R;
                    pixel.R = pixel.B;
                    pixel.B = tmp;

                    writer.Write(pixel.PackedValue);
                }

#if NETFX_CORE
                writer.Flush();
                writer.Dispose();
#else
                writer.Close();
#endif
                Close(stream);

            }

            return false;

        }

        static public bool TextureSaveAsJpeg(Texture2D tex, string name)
        {
            if (tex != null)
            {
                Stream stream = Storage4.OpenWrite(name);
                tex.SaveAsJpeg(stream, tex.Width, tex.Height);
                stream.Close();

                return true;
            }
            return false;
        }

        static public bool TextureSaveAsPng(Texture2D tex, string name)
        {
            if (tex != null)
            {
                Stream stream = Storage4.OpenWrite(name);
                tex.SaveAsPng(stream, tex.Width, tex.Height);
                stream.Close();

                return true;
            }
            return false;
        }

        static public bool TextureSaveToStream(Texture2D tex, Stream stream)
        {
            if (tex != null)
            {
                // A super-hack for the PC
                Debug.Assert(false);
#if !NETFX_CORE
                // TODO (****) save to dds no longer supported.  Try SaveAsPng() or SaveAsJpeg()
                //tex.Save("TextureSaveToStream.dds", ImageFileFormat.Dds);
                // Intentionally uses the filesystem API, not Storage class.
                Stream file = File.Open("TextureSaveToStream.dds", FileMode.Open);
                BinaryReader reader = new BinaryReader(file);
                BinaryWriter writer = new BinaryWriter(stream);

                byte[] buffer = new byte[4096];

                do
                {
                    int count = reader.Read(buffer, 0, buffer.Length);
                    if (count == 0)
                        break;

                    writer.Write(buffer, 0, count);
                }
                while (true);

                reader.Close();

                // Intentionally uses the filesystem API, not Storage class.
                File.Delete("TextureSaveToStream.dds");

                return true;
#endif
            }
            return false;

        }


        /// <summary>
        /// Load a texture from a given stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        static public Texture2D TextureLoad(Stream stream)
        {
            // Figure out what kind of file it is from the header signature.
            byte[] magic = new byte[8];
            stream.Read(magic, 0, 8);
            stream.Seek(0, SeekOrigin.Begin);

            TextureType textureType = TextureFormat(magic);

            switch (textureType)
            {
                case TextureType.png:
                    return TextureLoadPNG(stream);

                case TextureType.dds:
                    return TextureLoadDDS(stream);

                default:
                    break;
            }
            return null;
            //#else // XBOX360

            //Debug.Assert(false);
            // TODO (****) The FromFile() method is no longer supported.  You must now load
            // via a static call to ContentManager but this requires a file name, not a stream.
            // So, we'll need to figure out where this is all called from and fix it.

            /*
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
            TextureCreationParameters createParams = Texture2D.GetCreationParameters(device, stream);
            createParams.MipLevels = 1;
            stream.Seek(0, SeekOrigin.Begin);
            Texture2D tex = Texture2D.FromFile(
                device,
                stream,
                createParams);
            return tex;
            */
            //#endif // XBOX360
        }

        /// <summary>
        /// Load a .dds texture. On PC, will try .png if that fails.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// 
        static public Texture2D TextureLoad(string name)
        {
            return TextureLoad(name, false);
        }

        /// <summary>
        /// Load a .dds texture. On PC, will try .png if that fails.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="wantRetry"></param>
        /// <returns></returns>
        static public Texture2D TextureLoad(string name, bool wantRetry)
        {
            name = StripTextureExt(name);

            if (FileExists(name + ".dds", StorageSource.All))
            {
                Stream stream = OpenRead(name + ".dds", StorageSource.All);
                Texture2D tex = TextureLoadDDS(stream);
                Close(stream);
                return tex;
            }

            if (FileExists(name + ".png", StorageSource.All))
            {
                Stream stream = OpenRead(name + ".png", StorageSource.All);
                Texture2D tex = TextureLoadPNG(stream);
                Close(stream);
                return tex;
            }

            throw new FileNotFoundException(String.Format("Texture2D file not found in {0}: {1}", StorageSource.All, name));
        }

        /// <summary>
        /// Delete given .dds texture. On PC fallback on .png.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        static public void TextureDelete(string name)
        {
            Delete(name + ".dds");
            Delete(name + ".png");
        }

        /// <summary>
        /// Check whether .dds (or on PC .png) texture exists.
        /// </summary>
        /// <param name="nameNoExt"></param>
        /// <returns></returns>
        static public bool TextureExists(string nameNoExt)
        {
            bool ret = false;
            if (FileExists(nameNoExt + ".dds", StorageSource.All))
            {
                ret = true;
            }

            if (FileExists(nameNoExt + ".png", StorageSource.All))
            {
                ret = true;
            }

            return ret;
        }

        /// <summary>
        /// Open texture .dds file for read. On PC fallback on .png.
        /// </summary>
        /// <param name="nameNoExt"></param>
        /// <returns></returns>
        static public Stream TextureFileOpenRead(string nameNoExt, TextureFileType? fileType = null)
        {
            return fileType.HasValue ? TextureFileOpenRead(nameNoExt, StorageSource.All, fileType.Value) : TextureFileOpenRead(nameNoExt, StorageSource.All);
        }

        static public Stream TextureFileOpenRead(string nameNoExt, StorageSource sources)
        {
            if (FileExists(nameNoExt + ".dds", sources))
            {
                return OpenRead(nameNoExt + ".dds", sources);
            }

            if (FileExists(nameNoExt + ".png", sources))
            {
                return OpenRead(nameNoExt + ".png", sources);
            }

            throw new FileNotFoundException(String.Format("Texture2D file not found in {0}: {1}", sources, nameNoExt));
        }

        static public Stream TextureFileOpenRead(string nameNoExt, StorageSource sources, TextureFileType fileType)
        {
            string filePath = string.Format(FULL_FILE_PATH_FORMAT, nameNoExt, fileType.ToString());

            if (FileExists(filePath, sources))
            {
                return OpenRead(filePath, sources);
            }

            throw new FileNotFoundException(String.Format("Texture2D file not found in {0}: {1}", sources, nameNoExt));
        }

        public enum TextureFileType
        {
            png, 
            jpg,
            dds
        }
        #endregion Texture2D Specific

        #region Texture2D Internals
        /// <summary>
        /// Load up a 32 bit texture from a stream.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="tex"></param>
        static private void FillTexture32(BinaryReader reader, Texture2D tex)
        {
            int numPix = tex.Height * tex.Width;

            Color[] pix = new Color[numPix];
            for (int i = 0; i < numPix; ++i)
            {
                UInt32 data = reader.ReadUInt32();
                pix[i].A = (byte)(data >> 24);
                pix[i].R = (byte)(data >> 16);
                pix[i].G = (byte)(data >> 8);
                pix[i].B = (byte)(data >> 0);
            }
            tex.SetData<Color>(pix);
        }

        /// <summary>
        /// Load up a .dds texture from a stream.
        /// Only currently supports A8R8G8B8.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        static private Texture2D TextureLoadDDS(Stream stream)
        {
            if (stream != null)
            {
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

                BinaryReader reader = new BinaryReader(stream);

                UInt32 magic = reader.ReadUInt32();
                Debug.Assert(magic == DDS_FILE_MAGIC_NUMBER);

                UInt32 dwSz = reader.ReadUInt32(); // checked but unused
                Debug.Assert(dwSz == 124);

                UInt32 flags = reader.ReadUInt32(); // ignored
                UInt32 height = reader.ReadUInt32();
                UInt32 width = reader.ReadUInt32();

                UInt32 pitchOrLinearSize = reader.ReadUInt32(); // ignored.
                UInt32 depth = reader.ReadUInt32(); // volume textures currently unsupported
                UInt32 numMips = reader.ReadUInt32();

                /// Skip 11 reserved DWORDs
                for (int i = 0; i < 11; ++i)
                {
                    reader.ReadUInt32();
                }

                UInt32 bitsPerPixel = 0;
                /// Surface pixel format
                {
                    UInt32 pfSz = reader.ReadUInt32();
                    Debug.Assert(pfSz == 32);

                    UInt32 pfFlags = reader.ReadUInt32();
                    Debug.Assert((pfFlags & DDPF_RGB) != 0); // compressed not yet supported

                    UInt32 pfFourCC = reader.ReadUInt32(); // ignored, we don't yet do compressed
                    UInt32 pfRGBBitCount = reader.ReadUInt32();
                    Debug.Assert(pfRGBBitCount == 32); // only 32 bit A8R8G8B8

                    bitsPerPixel = pfRGBBitCount;

                    reader.ReadUInt32(); // red bitmask
                    reader.ReadUInt32(); // green bitmask
                    reader.ReadUInt32(); // blue bitmask
                    reader.ReadUInt32(); // alpha bitmask
                }

                /// Surface capabilities
                {
                    UInt32 scCaps1 = reader.ReadUInt32();

                    Debug.Assert((scCaps1 & DDSCAPS_TEXTURE) != 0); // must be a texture
                    if (numMips > 1)
                    {
                        Debug.Assert((flags & DDSD_MIPMAPCOUNT) != 0);
                        Debug.Assert((scCaps1 & DDSCAPS_COMPLEX) != 0);
                        Debug.Assert((scCaps1 & DDSCAPS_MIPMAP) != 0);
                    }
                    else
                    {
                        Debug.Assert((flags & DDSD_MIPMAPCOUNT) == 0);
                        Debug.Assert((scCaps1 & DDSCAPS_COMPLEX) == 0);
                        Debug.Assert((scCaps1 & DDSCAPS_MIPMAP) == 0);
                    }

                    UInt32 scCaps2 = reader.ReadUInt32();
                    Debug.Assert(scCaps2 == 0); // no cubes or volumes yet.

                    // Skip 2 reserved DWORDS
                    reader.ReadUInt32();
                    reader.ReadUInt32();
                }

                reader.ReadUInt32(); // another reserved/unused DWORD

                Texture2D tex = null;
                SurfaceFormat format = SurfaceFormat.Color; // assumes 32 bit rgba

                tex = new Texture2D(device, (int)width, (int)height, false, format);

                switch (bitsPerPixel)
                {
                    case 32:
                        FillTexture32(reader, tex);
                        break;

                    default:
                        tex = null;
                        break;
                }

#if NETFX_CORE
                reader.Dispose();
#else
                reader.Close();
#endif
                Close(stream);

                return tex;
            }
            return null;
        }

        /// <summary>
        /// Load a .png texture from a stream. Only supported on PC.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        static private Texture2D TextureLoadPNG(Stream stream)
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
            Debug.Assert(false);
            // TODO (****) FromFile no longer works, must read via ContentManager.Load<>()
            //return Texture2D.FromFile(device, stream);
            return null;
        }

        /// <summary>
        /// Supported texture types enum.
        /// </summary>
        public enum TextureType
        {
            png,
            dds,

            Unknown // must be last, doubles as NumTypes
        }

        /// <summary>
        /// Determine texture type from header in memory
        /// </summary>
        /// <param name="magic"></param>
        /// <returns></returns>
        static public TextureType TextureFormat(byte[] magic)
        {
            if ((magic[0] == ((DDS_FILE_MAGIC_NUMBER >> 0) & 0xff))
                && (magic[1] == ((DDS_FILE_MAGIC_NUMBER >> 8) & 0xff))
                && (magic[2] == ((DDS_FILE_MAGIC_NUMBER >> 16) & 0xff))
                && (magic[3] == ((DDS_FILE_MAGIC_NUMBER >> 24) & 0xff)))
            {
                return TextureType.dds;
            }

            if ((magic[0] == 137)
                && (magic[1] == 80)
                && (magic[2] == 78)
                && (magic[3] == 71)
                && (magic[4] == 13)
                && (magic[5] == 10)
                && (magic[6] == 26)
                && (magic[7] == 10))
            {
                return TextureType.png;
            }

            return TextureType.Unknown;
        }

        /// <summary>
        /// Determine texture extension from header in memory.
        /// </summary>
        /// <param name="magic"></param>
        /// <returns></returns>
        static public string TextureExt(byte[] magic)
        {
            return TextureFormat(magic).ToString();
        }

        /// <summary>
        /// Strip off a known texture extension from a filename.
        /// Won't strip off other .blah or .blah.blah "extensions".
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        static public string StripTextureExt(string name)
        {
            int numTypes = (int)TextureType.Unknown;
            for (int i = 0; i < numTypes; ++i)
            {
                string ext = "." + ((TextureType)i).ToString();
                if (name.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                    return name.Remove(name.Length - ext.Length, ext.Length);
            }

            return name;
        }

        #endregion Texture2D Internals
    }
}