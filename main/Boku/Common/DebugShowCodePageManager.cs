// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;
using Boku.Programming;
using Boku.SimWorld.Chassis;

namespace Boku.Common
{
    /// <summary>
    /// Class for showing debug info about which kode page is currently active.
    /// </summary>
    public static class DebugShowCodePageManager
    {
        struct Vertex : IVertexType
        {
            private Vector3 position;
            private Vector2 texCoord;

            static VertexDeclaration decl = null;
            static VertexElement[] elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                // Total == 20 bytes
            };

            public Vertex(Vector3 pos, Vector2 tex)
            {
                position = pos;
                texCoord = tex;
            }   // end of Vertex c'tor

            public VertexDeclaration VertexDeclaration
            {
                get
                {
                    if (decl == null || decl.IsDisposed)
                    {
                        decl = new VertexDeclaration(elements);
                    }
                    return decl;
                }
            }

        }   // end of Vertex

        static VertexBuffer vbuf = null;
        static Vertex[] localVerts = new Vertex[4];
        static Vector2 size;

        // Cache for tile icons.
        static Texture2D[] tiles = new Texture2D[Brain.kCountDefaultTasks];

        static bool initialized = false;

        static void Init()
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            size = new Vector2(0.5f, 0.5f);

            // Init the vertex buffer.
            if (vbuf == null)
            {
                vbuf = new VertexBuffer(device, typeof(Vertex), 4, BufferUsage.WriteOnly);

                // Fill in the local vertex data.
                localVerts[0] = new Vertex(new Vector3(0.0f, size.Y, 0.0f), new Vector2(0.0f, 0.0f));
                localVerts[1] = new Vertex(new Vector3(-size.X, size.Y, 0.0f), new Vector2(1.0f, 0.0f));
                localVerts[2] = new Vertex(new Vector3(-size.X, 0.0f, 0.0f), new Vector2(1.0f, 0.99f));
                localVerts[3] = new Vertex(new Vector3(0.0f, 0.0f, 0.0f), new Vector2(0.0f, 0.99f));

                // Copy to vertex buffer.
                vbuf.SetData<Vertex>(localVerts);
            }

            initialized = true;
        }   // end of Init()

        static public void Render(SmoothCamera camera)
        {
            // Only show these while running.
            if (InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.RunSim)
            {
                return;
            }

            if (!initialized)
            {
                Init();
            }

            for (int i = 0; i < InGame.inGame.gameThingList.Count; i++)
            {
                GameActor actor = InGame.inGame.gameThingList[i] as GameActor;

                if (actor != null)
                {
                    if (actor.DisplayCurrentPage || InGame.DebugDisplayCurrentPage)
                    {
                        // Does actor have any kode?  If not, don't both with display.
                        if (actor.Brain.tasks[0].reflexes.Count > 0)
                        {
                            // Don't bother with first person actors.  We can't see the icon anyway.
                            if (!actor.FirstPerson)
                            {
                                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

                                int curPage = actor.Brain.ActiveTaskId; // Note this is 0 based.
                                Texture2D texture = GetTileForTask(curPage);
                                Debug.Assert(texture != null, "Bad id?");

                                // Use the same position as thought balloons.
                                // Orient to always face camera.
                                Vector3 position;
                                DynamicPropChassis chassis = actor.Chassis as DynamicPropChassis;
                                if (chassis != null && chassis.Tumbles)
                                {
                                    position = actor.HealthBarOffset + actor.Movement.Position + new Vector3(0, 0, -0.3f);
                                }
                                else
                                {
                                    position = actor.WorldThoughtBalloonOffset + new Vector3(0, 0, 0.5f);
                                }
                                Matrix world = Matrix.CreateBillboard(
                                    position,
                                    camera.ActualFrom,
                                    camera.ViewUp,
                                    camera.ViewDir);

                                // Borrow the thought balloon manager effect.
                                Effect effect = ThoughtBalloonManager.Effect;

                                Matrix worldViewProjMatrix = world * camera.ViewMatrix * camera.ProjectionMatrix;
                                effect.Parameters["WorldViewProjMatrix"].SetValue(worldViewProjMatrix);
                                effect.Parameters["WorldMatrix"].SetValue(world);

                                effect.Parameters["ContentTexture"].SetValue(texture);

                                effect.Parameters["Size"].SetValue(size);
                                effect.Parameters["Alpha"].SetValue(1.0f);
                                effect.Parameters["BorderColor"].SetValue(Vector4.One);

                                device.SetVertexBuffer(vbuf);
                                device.Indices = UI2D.Shared.QuadIndexBuff;

                                // Render all passes.
                                for (int j = 0; j < effect.CurrentTechnique.Passes.Count; j++)
                                {
                                    EffectPass pass = effect.CurrentTechnique.Passes[j];
                                    pass.Apply();
                                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4, 0, 2);
                                }

                            }
                        }
                    }
                }
            }
        }   // end of Render()

        static Texture2D GetTileForTask(int task)
        {
            Texture2D tile = tiles[task];

            // If result not cached already or gone bad, reload.
            if (tile == null || tile.IsDisposed || tile.GraphicsDevice == null || tile.GraphicsDevice.IsDisposed)
            {

                string iconName = null;

                switch (task)
                {
                    case 0: iconName = "modifier.taskstar"; break;
                    case 1: iconName = "modifier.taskmoon"; break;
                    case 2: iconName = "modifier.taskheart"; break;
                    case 3: iconName = "modifier.taskrainbow"; break;
                    case 4: iconName = "modifier.tasksun"; break;
                    case 5: iconName = "modifier.taskcloud"; break;
                    case 6: iconName = "modifier.taskg"; break;
                    case 7: iconName = "modifier.taskh"; break;
                    case 8: iconName = "modifier.taski"; break;
                    case 9: iconName = "modifier.taskj"; break;
                    case 10: iconName = "modifier.taskk"; break;
                    case 11: iconName = "modifier.taskl"; break;
                    default:
                        Debug.Assert(false, "Invalid task id.");
                        break;
                }

                tile = BokuGame.Load<Texture2D>(System.IO.Path.Combine(BokuGame.Settings.MediaPath, @"Textures\Tiles", iconName));

                // Cache result.
                tiles[task] = tile;
            }

            return tile;
        }   // end of GetUpidForTask()

    }   // end of class DebugShowCodePageManager
}
