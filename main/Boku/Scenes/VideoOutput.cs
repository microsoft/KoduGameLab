// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
#if !NETFX_CORE
    using Microsoft.Xna.Framework.Net;
#endif

using KoiX;

using Boku.Audio;
using Boku.Base;
using Boku.Fx;
using Boku.Common;
using Boku.Common.Sharing;
using Boku.SimWorld;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.Common.Xml;

namespace Boku
{
    /// <summary>
    /// Hack to output of levels as video.
    /// </summary>
    public class VideoOutput : GameObject, INeedsDeviceReset
    {
        public static VideoOutput Instance = null;

        private const int blockW = 100;
        private const int blockH = 160;

        protected class Shared : INeedsDeviceReset
        {
            private VideoOutput parent = null;

            // This will be the 100xblockH block of data we transfer each frame. 
            // Each byte in this block translates into 2 colored blocks of data.
            // We allowcate and free this each time we are activated/deactivated
            // so we don't end up wasting 30k of space.
            public byte[] data = null;

            public List<string> filenames = null;
            public int curFileIndex = 0;

            public string levelFileShortPath;
            public string levelFileFullPath;
            public string stuffFileShortPath;
            public string stuffFileFullPath;
            public string thumbShortPath;
            public string thumbFullPath;
            public string terrainShortPath;
            public string terrainFullPath;

            public bool dataChanged = true;     // Re-render the rt?
            public Color[] colors = null;
            public Texture2D texture = null;

            // c'tor
            public Shared(VideoOutput parent)
            {
                this.parent = parent;

            }   // end of Shared c'tor

            public void LoadContent(bool immediate)
            {
            }   // end of VideoOutput Shared LoadContent()

            public void InitDeviceResources(GraphicsDevice device)
            {
            }

            public void UnloadContent()
            {
            }   // end of VideoOutput Shared UnloadContent()

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
            }

        }   // end of class Shared

        protected class UpdateObj : UpdateObject
        {
            Shared shared = null;
            VideoOutput parent = null;

            public UpdateObj(VideoOutput parent, Shared shared)
            {
                this.parent = parent;
                this.shared = shared;

            }

            public override void Update()
            {
                if (!parent.Active)
                {
                    return;
                }

                if (CommandStack.Peek() == parent.commandMap)
                {
                    bool done = false;

                    if (shared.curFileIndex >= shared.filenames.Count)
                    {
                        done = true;
                    }
                    else
                    {
                        bool doneCurFile = ShowFile(shared.filenames[shared.curFileIndex]);

                        if (doneCurFile)
                        {
                            ++shared.curFileIndex;
                        }
                    }

                    if (done || Actions.Cancel.WasPressed)
                    {
                        Actions.Cancel.ClearAllWasPressedState();

                        parent.Deactivate();
                        BokuGame.bokuGame.loadLevelMenu.LocalLevelMode = LoadLevelMenu.LocalLevelModes.General;
                        BokuGame.bokuGame.loadLevelMenu.ReturnToMenu = LoadLevelMenu.ReturnTo.MainMenu;
                        BokuGame.bokuGame.loadLevelMenu.Activate();

                        return;
                    }

                }

            }   // end of Update()

            private double kPageTime = 0.1;     // Duration to display each page.
            private double startTime = 0.0;     // When we started displaying cur page.
            private bool setStartTime = false;  // Allows us to set the start time on the frame _after_ we create the texture.
                                                // This will hopefully prevent short frames caused by load delays.

            private string curFilename = null;
            private int fileSize = 0;
            private int curPage = 0;            // Current page of curFile
            private int numPages = 0;
            private int bytesPerPage = (blockH - 4) * blockW;

            private Stream stream = null;

            /// <summary>
            /// Shows the current file.  Should be called each frame with
            /// the same name until this returns true indicating that the 
            /// frame has been displayed for enough time.
            /// </summary>
            /// <param name="filename"></param>
            /// <returns>True when done</returns>
            private bool ShowFile(string filename)
            {
                // New file has been sent.  Init.
                if (filename != curFilename)
                {
                    curFilename = filename;
                    curPage = -1;
                    startTime = 0;

                    try
                    {
                        stream = Storage4.OpenRead(BokuGame.Settings.MediaPath + filename, StorageSource.All);
                    }
                    catch
                    {
                        return true;
                    }

                    if (stream == null)
                    {
                        return true;
                    }
                    
                    fileSize = (int)stream.Length;

                    numPages = fileSize / bytesPerPage;
                    if (numPages * bytesPerPage != fileSize)
                    {
                        ++numPages;
                    }
                }

                if (setStartTime)
                {
                    startTime = Time.WallClockTotalSeconds;
                    setStartTime = false;
                }

                // If we timed out, then either load/show next 
                // page or return done if on last page.
                Double curTime = Time.WallClockTotalSeconds;
                if (startTime + kPageTime < curTime)
                {
                    ++curPage;
                    if (curPage >= numPages)
                    {
                        // Done with this file.
                        Storage4.Close(stream);
                        
                        // Allow the same file to be sent twice in a row.
                        curFilename = null;

                        return true;
                    }
                    else
                    {
                        // Not done so send the next block.

                        setStartTime = true;
                        startTime = curTime;

                        // Clear the data block.
                        for (int j = 0; j < blockH; j++)
                        {
                            for (int i = 0; i < blockW; i++)
                            {
                                shared.data[i + j * blockW] = 0;
                            }
                        }

                        // Load next page of data.
                        int n = fileSize - curPage * bytesPerPage;
                        if (n > bytesPerPage)
                        {
                            n = bytesPerPage;
                        }
                        for (int i = 0; i < n; i++)
                        {
                            shared.data[i] = (byte)stream.ReadByte();
                        }

                        // Put file length on line blockH - 4 starting at x = 0
                        int index = (blockH - 4) * blockW;
                        shared.data[index++] = (byte)(fileSize & 0x000000ff);
                        shared.data[index++] = (byte)((fileSize & 0x0000ff00) >> 8);
                        shared.data[index++] = (byte)((fileSize & 0x00ff0000) >> 16);
                        shared.data[index++] = (byte)((fileSize & 0xff000000) >> 24);

                        // numPages
                        shared.data[index++] = (byte)(numPages & 0x000000ff);
                        shared.data[index++] = (byte)((numPages & 0x0000ff00) >> 8);
                        shared.data[index++] = (byte)((numPages & 0x00ff0000) >> 16);
                        shared.data[index++] = (byte)((numPages & 0xff000000) >> 24);

                        // bytesPerPage
                        shared.data[index++] = (byte)(bytesPerPage & 0x000000ff);
                        shared.data[index++] = (byte)((bytesPerPage & 0x0000ff00) >> 8);
                        shared.data[index++] = (byte)((bytesPerPage & 0x00ff0000) >> 16);
                        shared.data[index++] = (byte)((bytesPerPage & 0xff000000) >> 24);

                        // curPage
                        shared.data[index++] = (byte)(curPage & 0x000000ff);
                        shared.data[index++] = (byte)((curPage & 0x0000ff00) >> 8);
                        shared.data[index++] = (byte)((curPage & 0x00ff0000) >> 16);
                        shared.data[index++] = (byte)((curPage & 0xff000000) >> 24);

                        // Put filename on line blockH - 3.
                        index = (blockH - 3) * blockW;
                        for (int i = 0; i < curFilename.Length; i++)
                        {
                            char c = curFilename[i];
                            shared.data[index++] = (byte)(c & 0x00ff);
                            shared.data[index++] = (byte)((c >> 8) & 0x00ff);
                        }

                        // Calc CRC32 for block, skipping bottom row.
                        byte[] crc = MyMath.CRC32(shared.data, blockW * (blockH - 1));

                        // Set CRC32 data in bottom line.
                        for (int i = 0; i < 4; i++)
                        {
                            shared.data[i + (blockH - 1) * blockW] = crc[i];
                        }

                        shared.dataChanged = true;

                    }   // end of setting up next block

                }   // end if timed out

                return false;

            }   // end of ShowFile()

            public override void Activate()
            {
            }

            public override void Deactivate()
            {
            }

        }   // end of class VideoOutput UpdateObj  

        protected class RenderObj : RenderObject, INeedsDeviceReset
        {
            private VideoOutput parent;
            private Shared shared;

            public RenderObj(VideoOutput parent, Shared shared)
            {
                this.parent = parent;
                this.shared = shared;
            }

            public override void Render(Camera camera)
            {
                if (!parent.Active)
                {
                    return;
                }

                ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();
                GraphicsDevice device = KoiLibrary.GraphicsDevice;

                HelpOverlay.RefreshTexture();

                RenderTarget2D rt = SharedX.RenderTarget1024_768;

                Vector2 screenSize = new Vector2(device.Viewport.Width, device.Viewport.Height);
                Vector2 rtSize = new Vector2(rt.Width, rt.Height);

                if (shared.dataChanged)
                {
                    shared.dataChanged = false;

#if RenderColorBlocks
                    InGame.SetRenderTarget(rt);

                    // Clear the screen.
                    InGame.Clear(Color.Black);

                    // Render data block to screen.
                    Vector4 colorLo = new Vector4(0, 0, 0, 1);
                    Vector4 colorHi = new Vector4(0, 0, 0, 1);
                    for (int j = 0; j < blockH; j++)
                    {
                        for (int i = 0; i < blockW; i++)
                        {
                            byte b = shared.data[i + j * blockW];

                            byte lo = (byte)(b & 0x0f);
                            byte hi = (byte)((b & 0xf0) >> 4);

                            // Least significant bits go into red.
                            colorLo.X = (lo & 0x03) * 0.25f + 0.125f;
                            colorLo.Y = ((lo >> 2) & 0x03) * 0.25f + 0.125f;

                            colorHi.X = (hi & 0x03) * 0.25f + 0.125f;
                            colorHi.Y = ((hi >> 2) & 0x03) * 0.25f + 0.125f;

                            Block(colorLo, colorHi, i, j);
                        }
                    }

                    InGame.RestoreRenderTarget();
#else

                    try
                    {
                        // Render data block into memory.
                        Color colorLo = Color.Black;
                        Color colorHi = Color.Black;
                        for (int j = 0; j < blockH; j++)
                        {
                            for (int i = 0; i < blockW; i++)
                            {
                                byte b = shared.data[i + j * blockW];

                                byte lo = (byte)(b & 0x0f);
                                byte hi = (byte)((b & 0xf0) >> 4);

                                // Least significant bits go into red.
                                colorLo.R = (byte)((lo & 0x03) * 64 + 32);
                                colorLo.G = (byte)(((lo >> 2) & 0x03) * 64 + 32);

                                colorHi.R = (byte)((hi & 0x03) * 64 + 32);
                                colorHi.G = (byte)(((hi >> 2) & 0x03) * 64 + 32);

                                MemBlock(colorLo, colorHi, i, j);
                            }
                        }

                        shared.texture.SetData<Color>(shared.colors);

                        // Copy to rendertarget.
                        InGame.SetRenderTarget(rt);
                        quad.Render(shared.texture, Vector2.Zero, new Vector2(shared.texture.Width, shared.texture.Height), "TexturedNoAlpha");
                        InGame.RestoreRenderTarget();

                    }
                    catch(Exception e)
                    {
                        if(e!=null)
                        {
                        }

                        // Something faileded, try again next frame.
                        shared.dataChanged = true;
                    }
#endif

                }

                InGame.Clear(Color.Black);

                quad.Render(rt, Vector2.Zero, rtSize, @"TexturedNoAlpha");

            }   // end of Render()  

            /// <summary>
            /// Renders a colored 3x3 block at the given position.
            /// </summary>
            private void Block(Vector4 colorLo, Vector4 colorHi, int i, int j)
            {
                ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();
                Vector2 pos = new Vector2(6, 3) * new Vector2(i, j);
                Vector2 size = new Vector2(3, 3);
                quad.Render(colorLo, pos, size);
                pos.X += 3;
                quad.Render(colorHi, pos, size);
            }   // end of Block()

            /// <summary>
            /// Renders a block of color in the memory from a texture.
            /// </summary>
            /// <param name="colorLo"></param>
            /// <param name="colorHi"></param>
            /// <param name="i"></param>
            /// <param name="j"></param>
            /// <param name="stride"></param>
            /// <param name="colors"></param>
            private void MemBlock(Color colorLo, Color colorHi, int i, int j)
            {
                int stride = shared.texture.Width;
                int baseIndex = (3 * j) * stride + (6 * i);

                int index = baseIndex;
                shared.colors[index++] = colorLo;
                shared.colors[index++] = colorLo;
                shared.colors[index++] = colorLo;
                shared.colors[index++] = colorHi;
                shared.colors[index++] = colorHi;
                shared.colors[index++] = colorHi;

                index = baseIndex + stride;
                shared.colors[index++] = colorLo;
                shared.colors[index++] = colorLo;
                shared.colors[index++] = colorLo;
                shared.colors[index++] = colorHi;
                shared.colors[index++] = colorHi;
                shared.colors[index++] = colorHi;

                index = baseIndex + stride + stride;
                shared.colors[index++] = colorLo;
                shared.colors[index++] = colorLo;
                shared.colors[index++] = colorLo;
                shared.colors[index++] = colorHi;
                shared.colors[index++] = colorHi;
                shared.colors[index++] = colorHi;
            }   // end of MemBlock()

            public override void Activate()
            {
            }

            public override void Deactivate()
            {
            }


            #region INeedsDeviceReset Members

            public void LoadContent(bool immediate)
            {
            }

            public void InitDeviceResources(GraphicsDevice device)
            {
            }

            public void UnloadContent()
            {
            }

            public void DeviceReset(GraphicsDevice device)
            {
            }

            #endregion
        }   // end of class VideoOutput RenderObj     


        // List objects.
        protected Shared shared = null;
        protected RenderObj renderObj = null;
        protected UpdateObj updateObj = null;

        private enum States
        {
            Inactive,
            Active,
        }

        private States state = States.Inactive;
        private States pendingState = States.Inactive;

        private CommandMap commandMap = new CommandMap("VideoOutput");   // Placeholder for stack.

        #region Accessors

        public bool Active
        {
            get { return (state == States.Active && pendingState != States.Inactive); }
        }

        #endregion

        // c'tor
        public VideoOutput()
        {
            VideoOutput.Instance = this;

            shared = new Shared(this);

            // Create the RenderObject and UpdateObject parts of this mode.
            updateObj = new UpdateObj(this, shared);
            renderObj = new RenderObj(this, shared);

        }   // end of VideoOutput c'tor

        public override bool Refresh(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;

            if (state != pendingState)
            {
                if (pendingState == States.Active)
                {
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
                }

                state = pendingState;
            }

            return result;
        }   // end of VideoOutput Refresh()

        public override void Activate()
        {
            // Should never really happen.
            List<string> files = new List<string>();
            Activate(files);
        }

        public void Activate(List<string> files)
        {
            if (state != States.Active)
            {
                // Grab a ref to the file list.
                shared.filenames = files;
                shared.curFileIndex = 0;

                // Allocate the memory buffers we need.
                shared.data = new byte[blockW * blockH];
                shared.texture = new Texture2D(KoiLibrary.GraphicsDevice, 640, 480);
                shared.colors = new Color[shared.texture.Width * shared.texture.Height];

                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Push(commandMap);

                pendingState = States.Active;
                BokuGame.objectListDirty = true;
            }
        }   // end of VideoOutput Activate()

        override public void Deactivate()
        {
            if (state != States.Inactive)
            {
                // Release file list.
                shared.filenames = null;
                shared.curFileIndex = 0;

                // Release the memeory buffer.
                shared.data = null;
                DeviceResetX.Release(ref shared.texture);
                shared.colors = null;

                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Pop(commandMap);

                pendingState = States.Inactive;
                BokuGame.objectListDirty = true;
            }
        }   // end of VideoOutput Deactivate()

        public void LoadContent(bool immediate)
        {
            BokuGame.Load(shared, immediate);
            BokuGame.Load(renderObj, immediate);
        }   // end of VideoOutput LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
            renderObj.InitDeviceResources(device);
        }

        public void UnloadContent()
        {
            BokuGame.Unload(shared);
            BokuGame.Unload(renderObj);
        }   // end of VideoOutput UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
            BokuGame.DeviceReset(shared, device);
            BokuGame.DeviceReset(renderObj, device);
        }

    }   // end of class VideoOutput

}   // end of namespace Boku
    


