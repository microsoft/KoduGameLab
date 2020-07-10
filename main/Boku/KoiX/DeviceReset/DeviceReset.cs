// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.



using System;
using System.Collections;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

namespace KoiX
{
    /// <summary>
    /// This class is a holder for static methods designed to help with device reset.
    /// </summary>
    public class DeviceResetX
    {
        public static void Load( IDeviceResetX foo )
        {
            if (foo != null)
            {
                foo.LoadContent();
            }
        }
        public static void Unload( IDeviceResetX foo )
        {
            if (foo != null)
            {
                foo.UnloadContent();
            }
        }

        public static void Load(object foo)
        {
            if (foo != null && foo is IDeviceResetX)
            {
                (foo as IDeviceResetX).LoadContent();
            }
        }
        public static void Unload(object foo)
        {
            if (foo != null && foo is IDeviceResetX)
            {
                (foo as IDeviceResetX).UnloadContent();
            }
        }
                
        // TODO Should textures have dispose called?  It seems like
        // they should if created by user code but not if just a 
        // ref from Content.  
        // Solution: Don't call Dispose().  Assume that if not from
        // Content then Dispose will get called anyway when object
        // goes out of scope and is GC'd.

        public static void Release( ref Texture foo )
        {
            if (foo != null)
            {
                //foo.Dispose();
            }
            foo = null;
        }
        public static void Release( ref Texture2D foo )
        {
            if (foo != null)
            {
                //foo.Dispose();
            }
            foo = null;
        }
        public static void Release( ref TextureCube foo )
        {
            if (foo != null)
            {
                //foo.Dispose();
            }
            foo = null;
        }
        public static void Release( ref VertexDeclaration foo )
        {
            if (foo != null)
            {
                foo.Dispose();
            }
            foo = null;
        }
        public static void Release( ref VertexBuffer foo )
        {
            if (foo != null)
            {
                foo.Dispose();
            }
            foo = null;
        }
        public static void Release(ref DynamicVertexBuffer foo)
        {
            if (foo != null)
            {
                foo.Dispose();
            }
            foo = null;
        }
        public static void Release(ref IndexBuffer foo)
        {
            if (foo != null)
            {
                foo.Dispose();
            }
            foo = null;
        }
        public static void Release( ref Effect foo )
        {
            // Don't dispose effects, cached in content manager.
            foo = null;
        }
        public static void Release(ref BasicEffect foo)
        {
            if (foo != null)
            {
                foo.Dispose();
            }
            foo = null;
        }
        public static void Release(ref SoundEffect foo)
        {
            if (foo != null)
            {
                foo.Dispose();
            }
            foo = null;
        }
        public static void Release(ref RenderTarget2D foo)
        {
            if (foo != null)
            {
                foo.Dispose();
            }
            foo = null;
        }
        public static void Release(ref SpriteFont foo)
        {
            if (foo != null)
            {
            }
            foo = null;
        }
        public static void Release(ref BlendState foo)
        {
            if (foo != null)
            {
                foo.Dispose();
            }
            foo = null;
        }
        public static void Release(ref SamplerState foo)
        {
            if (foo != null)
            {
                foo.Dispose();
            }
            foo = null;
        }
        public static void Release(ref RasterizerState foo)
        {
            if (foo != null)
            {
                foo.Dispose();
            }
            foo = null;
        }
        public static void Release(ref DepthStencilState foo)
        {
            if (foo != null)
            {
                foo.Dispose();
            }
            foo = null;
        }
        public static void Release(ref Cue foo)
        {
            if (foo != null)
            {
                foo.Dispose();
            }
            foo = null;
        }

        public static bool NeedsLoad(GraphicsResource foo)
        {
            return foo == null || foo.IsDisposed || foo.GraphicsDevice.IsDisposed;
        }

        public static bool NeedsLoad(RenderTarget2D rt)
        {
            return rt == null || rt.IsContentLost || rt.IsDisposed || rt.GraphicsDevice.IsDisposed;
        }
    }   // end of class DeviceReset

}   // end of namespace Koi
