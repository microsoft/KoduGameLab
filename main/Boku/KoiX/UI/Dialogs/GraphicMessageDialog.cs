using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI;

using Boku.Common;

namespace KoiX.UI.Dialogs
{
    /// <summary>
    /// A simple dialog that shows a texture and a text label.  The 
    /// texture may actually be a series of images which are looped
    /// through like an animated GIF.  The 'G' in GraphicMessageDialog
    /// is pronounced as a hard 'g' as it should also be in GIF no
    /// matter what the creators say...
    /// 
    /// A replacement for the SimpleMessage dialog which was used for 
    /// displaying a "please wait" image when loading levels as well
    /// as the stacking animation when performing terrain processing
    /// operations.
    /// </summary>
    public class GraphicMessageDialog : BaseDialogNonModal
    {
        #region Members

        Vector2 center = Vector2.Zero;

        List<Texture2D> textures = new List<Texture2D>();

        int frameIndex = 0;
        float frameDelay = 1.0f;    // Time between animation frames.
        double lastFrameUpdate = 0; // When did we last update the animation.

        Label label;

        #endregion

        #region Accessors

        /// <summary>
        /// Time between animation frames.
        /// </summary>
        public float FrameDelay
        {
            get { return frameDelay; }
            set { frameDelay = value; }
        }

        public Vector2 Center
        {
            get { return center; }
            set { center = value; }
        }

        #endregion

        #region Public

        public GraphicMessageDialog(string messageId = null, string messageText = null)
            : base(theme: null)
        {
#if DEBUG
            _name = "GraphicMessageDialog";
#endif

            // We just want the message info to render on top of whatever is already going on in the scene.
            BackdropColor = Color.Transparent;
            RenderBaseTile = false;
            Focusable = false;

            SystemFont font = SharedX.GameFont30Bold.systemFont;

            label = new Label(this, font, theme.LightTextColor, outlineColor: theme.DarkTextColor * 0.8f, outlineWidth: 1.3f, labelId: messageId, labelText: messageText);
            AddWidget(label);
            label.Size = label.CalcMinSize();

        }   // end of c'tor

        public override void Update(SpriteCamera camera)
        {
            if (Time.WallClockTotalSeconds > lastFrameUpdate + frameDelay)
            {
                frameIndex = (frameIndex + 1) % textures.Count;
                lastFrameUpdate = Time.WallClockTotalSeconds;
            }

            base.Update(camera);
        }   // end of Update()

        public override void Render(SpriteCamera camera)
        {
            if (textures.Count < 1 || textures[frameIndex] == null)
            {
                return;
            }

            SpriteBatch batch = KoiLibrary.SpriteBatch;

            Rectangle rect = new Rectangle();
            rect.Width = textures[frameIndex].Width;
            rect.Height = textures[frameIndex].Height;
            rect.Location = (center - new Vector2(rect.Width / 2.0f, rect.Height / 2.0f)).RoundToPoint();

            label.Position = center + new Vector2(-label.Size.X / 2.0f, rect.Height / 2.0f);

            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, camera.ViewMatrix);
            {
                batch.Draw(textures[frameIndex], rect, Color.White);
            }
            batch.End();

            base.Render(camera);
        }   // end of Render()

        public void AddTexture(Texture2D texture)
        {
            textures.Add(texture);
        }   // end of AddTexture()

        #endregion

        #region Internal
        #endregion


    }   // end of class GraphicMessageDialog

}   // end of namespace KoiX.UI.Dialogs
