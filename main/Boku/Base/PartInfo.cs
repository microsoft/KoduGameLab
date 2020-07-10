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

using Boku.Common;
using Boku.Fx;

namespace Boku.Base
{
    /// <summary>
    /// Caches information extracted from a part of a mesh so
    /// we don't have to do this every time the mesh is rendered.
    /// </summary>
    public class PartInfo
    {
        #region Members
        private Vector4 diffuseColor;           
        private float alpha = 1.0f;
        private Vector4 emissiveColor;
        private Vector4 specularColor;
        private float specularPower = 8.0f;

        private Texture2D overlayTexture = null;  // Added programmatically.
        private Texture2D diffuseTexture = null;  // Texture2D that comes with model.

        private bool collision = false;
        private bool render = true;

        private SurfaceSet surfaceSet = null;
        #endregion Members

        #region Accessors
        public Vector4 DiffuseColor 
        {
            get { diffuseColor.W = alpha;  return diffuseColor; }
            set { diffuseColor = value; }
        }
        public float Alpha
        {
            get { return alpha; }
            set { alpha = value; }
        }
        public Vector4 EmissiveColor
        {
            get { return emissiveColor; }
            set { emissiveColor = value; }
        }
        public Vector4 SpecularColor
        {
            get { return specularColor; }
            set { specularColor = value; }
        }
        public float SpecularPower
        {
            get { return specularPower; }
            set { specularPower = value; }
        }
        public Texture2D DiffuseTexture
        {
            get { return diffuseTexture; }
            set { diffuseTexture = value; }
        }
        public Texture2D OverlayTexture
        {
            get { return overlayTexture; }
            set { overlayTexture = value; }
        }
        public bool Collision
        {
            get { return collision; }
            set { collision = value; }
        }
        public bool Render
        {
            get { return render; }
            set { render = value; }
        }
        public SurfaceSet SurfaceSet
        {
            get { return surfaceSet; }
            set { surfaceSet = value; }
        }
        #endregion

        // c'tor
        public PartInfo()
        {
            // Default to a color which stands out.  Bright pink.
            diffuseColor = new Vector4(1.0f, 0.0f, 0.5f, 0.5f);
            alpha = 0.5f;
            SpecularColor = new Vector4();
            EmissiveColor = new Vector4();
        }   // end of PartInfo c'tor
        public PartInfo(PartInfo copy)
        {
            this.overlayTexture = copy.overlayTexture;
            this.diffuseColor = copy.diffuseColor;
            this.alpha = copy.alpha;
            this.emissiveColor = copy.emissiveColor;
            this.specularColor = copy.specularColor;
            this.specularPower = copy.specularPower;
            this.diffuseTexture = copy.diffuseTexture;
        }

        /// <summary>
        /// Scans through the part's effect data and extracts the info we need.
        /// </summary>
        /// <param name="part"></param>
        public void InitFromPart(ModelMeshPart part)
        {
            if (part.Effect != null)
            {
                for (int i = 0; i < part.Effect.Parameters.Count; i++)
                {

                    string name = part.Effect.Parameters[i].Name;

                    switch (name)
                    {
                        case "DiffuseColor":
                            DiffuseColor = new Vector4(((BasicEffect)part.Effect).DiffuseColor, 1.0f);
                            break;
                        case "Alpha":
                            Alpha = ((BasicEffect)part.Effect).Alpha;
                            break;
                        case "EmissiveColor":
                            EmissiveColor = new Vector4(((BasicEffect)part.Effect).EmissiveColor, 1.0f);
                            break;
                        case "SpecularColor":
                            SpecularColor = new Vector4(((BasicEffect)part.Effect).SpecularColor, 1.0f);
                            break;
                        case "SpecularPower":
                            SpecularPower = ((BasicEffect)part.Effect).SpecularPower;
                            break;

                        case "BasicTexture":
                        case "Texture":
                            DiffuseTexture = ((BasicEffect)part.Effect).Texture;
                            // Hack to fix up WHEN and DO textures in programming UI.
                            string tag = part.Tag as string;
                            if(DiffuseTexture == null && !string.IsNullOrEmpty(tag) && tag.Contains("Header"))
                            {
                                if (tag == "When Header")
                                {
                                    DiffuseTexture = CreateProgrammingClauseTexture("programming.When");
                                }
                                else if (tag == "Do Header")
                                {
                                    DiffuseTexture = CreateProgrammingClauseTexture("programming.Do");
                                }
                                else
                                {
                                    Debug.Assert(false, "Why are we here?");
                                }
                            }
                            break;

                        default:
                            break;
                    }   // end of switch on parameter name.
                }   // end of loop over parameters.

                // HACK With changes in content pipeline for 4.0 the Alpha parameter 
                // is not showing up so we have this hack.  The only place this code is 
                // used is for the programming UI.  So, this hack looks for the tag 
                // assocaited with the 'bar' underlying each reflex in the programming
                // UI and forces the alpha to be 0.5f.  Everything else gets alpha = 1.0.
                if (part.Tag != null)
                {
                    if (part.Tag as string == "row far BACK")
                    {
                        Alpha = 0.5f;
                    }
                    else
                    {
                        Alpha = 1.0f;
                    }
                }

            }

        }   // end of InitFromPart()


        /// <summary>
        /// Extracts a color from an EffectParameter.
        /// </summary>
        /// <param name="param"></param>
        /// <returns>The extracted color.</returns>
        private Vector4 GetColor(EffectParameter param)
        {
            float[] tmp = new float[3];
#if NETFX_CORE
            tmp = param.GetValueSingleArray();
#else
            tmp = param.GetValueSingleArray(3);
#endif

            Vector4 result = new Vector4(tmp[0], tmp[1], tmp[2], 1.0f);

            return result;
        }   // end of PartInfo GetColor()

        /// <summary>
        /// Create the localized WHEN and DO textures for the programming UI.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private Texture2D CreateProgrammingClauseTexture(string str)
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            string label = Strings.Localize(str);

            Texture2D result = new Texture2D(device, 128, 128);

            SpriteBatch batch = UI2D.Shared.SpriteBatch;

            // Use the 256x256 rendertarget to pre-render our text label.  
            // This gives us the chance to compress it if we need to if 
            // the label is too long to naturally fit on the tile.
            RenderTarget2D rt = UI2D.Shared.RenderTarget256_256;
            InGame.SetRenderTarget(rt);

            InGame.Clear(Color.Transparent);

            UI2D.Shared.GetFont Font = UI2D.Shared.GetGameFont30Bold;
            Vector2 labelSize = Vector2.Zero;
            if (label != null)
            {
                labelSize = Font().MeasureString(label) + new Vector2(3, 2);
                batch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
                TextHelper.DrawString(Font, label, new Vector2(1, 1), Color.White);
                batch.End();
            }

            // Restore backbuffer.
            InGame.RestoreRenderTarget();

            // Now render the texture to the 128x128 rt.
            RenderTarget2D renderTarget = UI2D.Shared.RenderTarget128_128;

            Rectangle destRect = new Rectangle(0, 0, renderTarget.Width, renderTarget.Height);
            InGame.SetRenderTarget(renderTarget);

            InGame.Clear(Color.Black);

            batch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);

            // Draw label
            if (label != null)
            {
                Rectangle srcRect = new Rectangle(0, 0, (int)labelSize.X, (int)labelSize.Y);
                Rectangle dstRect;
                // Center vertically.
                int yPos = (int)((renderTarget.Height - labelSize.Y) / 2.0f);
                if (labelSize.X > renderTarget.Width)
                {
                    // Label is wider than tile, shrink to fit.
                    dstRect = new Rectangle(0, yPos, renderTarget.Width, srcRect.Height);
                }
                else
                {
                    // Label fits on tile, center location.
                    dstRect = new Rectangle((int)((renderTarget.Width - srcRect.Width) / 2.0f), yPos, srcRect.Width, srcRect.Height);
                }
                batch.Draw(rt, dstRect, srcRect, Color.White);
            }

            batch.End();

            // Restore backbuffer.
            InGame.RestoreRenderTarget();

            // Copy rendertarget result into texture.
            int[] data = new int[128 * 128];
            renderTarget.GetData<int>(data);
            result.SetData<int>(data);

            return result;
        }

    }   // end of class PartInfo

}   // end of namespace Boku.Base

