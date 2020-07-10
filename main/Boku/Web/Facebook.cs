// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;

using BokuShared.Wire;

namespace Boku.Web
{
    public static class Facebook
    {
        public delegate void GetUserInfoCompleteDelegate(bool success, FacebookResultCode fbResultCode, UserInfo user, object state);
        public delegate void PublishWorldCompleteDelegate(bool success, FacebookResultCode fbResultCode, object state);

        public class UserInfo
        {
            public string Id;
            public string Name;
            public Texture2D SquareImage;
        }
        
        static UserInfo user = new UserInfo();

        static BrowserForm form = null;
        public static BrowserForm Form
        {
            get { return form; }
        }

        // Temporary
        public static Guid KoduFacebookId = Guid.NewGuid();

        public static UserInfo User
        {
            get { return user; }
        }

        class GetUserInfoState
        {
            public GetUserInfoCompleteDelegate callback;
            public object state;
        }
        class PublishWorldState
        {
            public PublishWorldCompleteDelegate callback;
            public object state;
        }

        /// <summary>
        /// Retrieves the user's Facebook, id, name and profile picture
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns>transaction id - cancelable via Community.Async_Cancel()</returns>
        public static int GetUserInfo(GetUserInfoCompleteDelegate callback, object state)
        {
            GetUserInfoState async = new GetUserInfoState();

            async.callback = callback;
            async.state = state;

            return Community.Async_GetFacebookUser(KoduFacebookId, GetUserInfoComplete, async);
        }
        static void GetUserInfoComplete(AsyncResult ar)
        {
            AsyncResult_GetFacebookUser result = (AsyncResult_GetFacebookUser)ar;

            GetUserInfoState async = (GetUserInfoState)result.Param;

            if (result.FacebookResultCode == FacebookResultCode.Success && result.FacebookUser != null)
            {
                user.Id = result.FacebookUser.Id;
                user.Name = result.FacebookUser.Name;
            }

            if (async.callback != null)
            {
                async.callback(result.Success, result.FacebookResultCode, user, async.state);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public static int PublishWorld(PublishWorldCompleteDelegate callback, Guid worldId, object state)
        {
            PublishWorldState async = new PublishWorldState();

            async.callback = callback;
            async.state = state;

            return Community.Async_PublishWorldToFacebook(KoduFacebookId, worldId, PublishWorldComplete, async);
        }
        static void PublishWorldComplete(AsyncResult ar)
        {
            AsyncResult_FacebookOperation result = (AsyncResult_FacebookOperation)ar;

            PublishWorldState async = (PublishWorldState)ar.Param;

            if (async.callback != null)
            {
                async.callback(result.Success, result.FacebookResultCode, async.state);
            }
        }

        /// <summary>
        /// Starts a browser-based login process. Completion path is not yet clear, yet
        /// once completed, the server will have a valid auth token and will be able to
        /// perform Facebook operations on our behalf.
        /// </summary>
        public static void StartLoggingIntoFacebook()
        {
            string urlFormat = "http://kodu.cloudapp.net/KoduLogin.aspx?koduFacebookId={0}";
            //string urlFormat = "http://localhost:50000/KoduLogin.aspx?koduFacebookId={0}";
            string url = String.Format(urlFormat, KoduFacebookId);
            Process.Start(url);
        }

        static bool fullScreen = false;

        /// <summary>
        /// Brings up a WinForm with an embedded browser for auth.
        /// Also puts Kodu into windowed mode if needed.
        /// </summary>
        public static void DisplayFacebookAuthForm()
        {
            // If we're currently fullscreen we need to remember this
            // and toggle out to windowed so the auth form will be
            // displayed over the game.
            // TODO (scoy) Should we also do this for picking a printer
            // when we're fullscreen?
            fullScreen = BokuGame.Graphics.IsFullScreen;
            if (fullScreen)
            {
                BokuGame.Graphics.ToggleFullScreen();
            }

            form = new BrowserForm();
            form.Show();
            form.BringToFront();

            form.Closed += FormClosedHandler;

        }   // end of DisplayFacebookAuthForm()

        /// <summary>
        /// The auth form has been closed, restore fullscreen mode if needed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void FormClosedHandler(object sender, System.EventArgs e)
        {
            System.Windows.Forms.WebBrowser wb = Facebook.Form.WebBrowser;

            // TODO (scoy) Get the auth results from the browser before shutting down.
            string fullText = wb.DocumentText;
            string[] lines = fullText.Split()

            if (fullScreen)
            {
                BokuGame.Graphics.ToggleFullScreen();
            }
            fullScreen = false;

            form = null;
        }

    }   // end of class Facebook

}   // end of namespace Boku.Web
