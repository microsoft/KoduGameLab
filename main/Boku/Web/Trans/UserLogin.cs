// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Threading;

using Boku.Common;
using Boku.Common.Sharing;

using BokuShared;
using BokuShared.Wire;


namespace Boku.Web.Trans
{
    /// <summary>
    /// Web request for retrieving a user's admin level.
    /// </summary>
    public class UserLogin : CommunityRequest
    {
        protected override string MethodName { get { return "UserLogin"; } }

        #region Public Types

        public class Result
        {
            public bool success;
            public object userState;
            public UserLevel userLevel;
            public Version latestVersion;
        }

        #endregion

        #region Public Methods

        public UserLogin(SendOrPostCallback callback, object userState)
            : base(true, callback, userState)
        {
        }

        #endregion

        #region Protected Methods

        protected override bool ISend()
        {
            progress.Message = "Logging in...";

            Message_UserLoginRequest request = new Message_UserLoginRequest();
            request.SiteId = SiteID.Instance.Value.ToString();
            request.UserName = GetUserName();
            request.Community = Program2.SiteOptions.Community;

            return SendBuffer(request.SaveToArray());
        }

        protected override object IComplete(object param)
        {
            WebRequestCompleteArg arg = (WebRequestCompleteArg)param;

            bool success = (arg.result == WebResult.Complete && arg.resultCode < 300);

            Result result = new Result();
            result.success = success;
            result.userState = userState;

            try
            {
                string str = UnpackString(arg.responseBody);

                if (success)
                {
                    if (DeSoapify(ref str))
                    {
                        // B64 decode the return value.
                        byte[] buffer = Convert.FromBase64String(str);

                        // Deserialize the reply.
                        Message_UserLoginReply reply = Message_UserLoginReply.Load(buffer);

                        // Load result object with reply values.
                        result.success = reply.ResultCode == ResultCode.Success;
                        result.userLevel = reply.UserLevel;
                        result.latestVersion = reply.Version;
                    }
                    else
                    {
                        result.success = false;
                    }
                }
            }
            catch
            {
                result.success = false;
            }

            return result;
        }

        #endregion
    }
}
