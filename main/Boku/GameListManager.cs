
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Text;

using Boku.Common;
using Boku.Input;

namespace Boku
{
    /// <summary>
    /// This is an object which manages the top level lists
    /// of objects in the game.  There will only be one of
    /// these objects.
    /// </summary>
    public class GameListManager
    {
        // TODO (scoy) Try and change these to be more type specific.
        public List<object> objectList = null;
        public List<UpdateObject> updateList = null;
        public List<RenderObject> renderList = null;
        public List<object> collideList = null;

        TextBlob blob;
        Texture2D deadKoduTexture;

        // c'tor
        public GameListManager()
        {
            objectList = new List<object>();
            updateList = new List<UpdateObject>();
            renderList = new List<RenderObject>();
            collideList = new List<object>();

        }   // end of GameListManager c'tor


        public void AddObject(GameObject obj)
        {
            objectList.Add(obj);
            BokuGame.objectListDirty = true;
        }   // end of GameListManager AddObject()


        public void RemoveObject(GameObject obj)
        {
            objectList.Remove(obj);
            BokuGame.objectListDirty = true;
        }   // end of GameListManager RemoveObject()


        public void Refresh()
        {
            for (int i = 0; i < objectList.Count; ++i)
            {
                GameObject obj = (GameObject)objectList[i];
                if (obj.Refresh(updateList, renderList) && objectList.Count > 0)
                {
                    --i;
                }
            }
        }   // end of GameListManager Refresh()


        public void Update()
        {
            // Lazy allocation
            if (blob == null)
            {
                blob = new TextBlob(SharedX.GetGameFont20, Strings.Localize("mainMenu.paused"), 300);
                blob.Justification = TextHelper.Justification.Center;
                deadKoduTexture = KoiLibrary.LoadTexture2D(@"Textures\SleepyKodu");
            }

#if !NETFX_CORE
            // Check if microbit:driver needs installing.
            if (MicrobitManager.DriverInstalled == false)
            {
                MicrobitManager.ShowDriverDialog();
            }
#endif

#if NETFX_CORE
            if (BokuGame.ScreenSize.X > BokuGame.ScreenSize.Y)
            {
#endif
                for (int i = 0; i < updateList.Count; ++i)
                {
                    UpdateObject obj = updateList[i] as UpdateObject;
                    obj.Update();
                }
#if NETFX_CORE
            }
            else
            {
                // Game paused since in strange snapped mode
            }
#endif
        }   // end of GameListManager Update()


        public void Render()
        {
#if NETFX_CORE
            if (BokuGame.ScreenSize.X > BokuGame.ScreenSize.Y)
            {
#endif
                for (int i = 0; i < renderList.Count; ++i)
                {
                    RenderObject obj = renderList[i] as RenderObject;
                    obj.Render(null);
                }
#if NETFX_CORE
            }
            else
            {
                // Game paused since in strange snapped mode
                GraphicsDevice device = KoiLibrary.GraphicsDevice;
                InGame.Clear(Color.Black);

                // Center Kodu.
                Vector2 size = new Vector2(deadKoduTexture.Width, deadKoduTexture.Height);
                Vector2 pos = (BokuGame.ScreenSize - size) * 0.5f;
                SpriteBatch batch = KoiLibrary.SpriteBatch;
                batch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
                batch.Draw(deadKoduTexture, pos, Color.White);
                batch.End();

                pos.X = BokuGame.ScreenSize.X / 2.0f - 150.0f;
                pos.Y += size.Y + 30.0f;

                blob.RenderWithButtons(pos, new Color(0.8f, 0.8f, 0.8f));

            }
#endif
        }   // end of GameListManager Render()


    }   // end of class GameListManager

}   // end of namespace Boku
