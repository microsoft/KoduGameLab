using System;
using System.IO;
using System.Text;
using System.Threading;

using System.Collections.Generic;

using BokuShared;
using BokuShared.Wire;


namespace Boku.Web.Trans
{
    /// <summary>
    /// Provides services shared by all community web requests.
    /// </summary>
    public abstract class CommunityRequest : Request
    {

        //Moved from siteOptions.
        //public static string CommunityUrl = @"https://kodu.cloudapp.net/Community.asmx";//Production
        public static string CommunityUrl = @"https://kodu.cloudapp.net/Community2.asmx";//Production
        //public static string CommunityUrl = @"https://koduclientapi-int.cloudapp.net/Community.asmx"; //Internal
        //public static string CommunityUrl = @"http://localhost.fiddler:50000/Community2.asmx"; //Local
        //public static string CommunityUrl = @"http://koduclientapi-int.cloudapp.net/Community2.asmx"; //Internal
        //public static string CommunityUrl = @"http://localhost:50000/Community2.asmx"; //Local

        #region Public Methods

        public CommunityRequest(bool needsCryptoKey, SendOrPostCallback callback, object userState)
            : base(CommunityUrl, needsCryptoKey, callback, userState) //From siteOptions. NO LONGER USED.
        {
        }

        public override bool Send()
        {
            if (!Program2.SiteOptions.CommunityEnabled)
            {
                return false;
            }

            return base.Send();
        }

        #endregion
    }
}
