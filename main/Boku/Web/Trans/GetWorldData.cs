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
    /// Web request to download a world from the community site.
    /// </summary>
    public class GetWorldData : CommunityRequest
    {
        protected override string MethodName { get { return "GetWorldData"; } }

        #region Private Members

        Guid worldId;

        #endregion

        #region Public Types

        public class Result
        {
            public bool success;
            public object userState;
            public BokuShared.Wire.WorldPacket world;
        }

        #endregion

        #region Public Methods

        public GetWorldData(Guid worldId, SendOrPostCallback callback, object userState)
            : base(true, callback, userState)
        {
            this.worldId = worldId;
        }

        #endregion

        #region Protected Methods

        protected override bool ISend()
        {
            // The community screen has another method of providing visual feedback, so no need for the generic progress indicator.
            progress.Complete();

            Message_GetWorldDataRequest request = new Message_GetWorldDataRequest();
            request.SiteId = SiteID.Instance.Value.ToString();
            request.UserName = GetUserName();
            request.Community = Program2.SiteOptions.Community;
            request.WorldId = worldId;

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
                        Message_GetWorldDataReply reply = Message_GetWorldDataReply.Load(buffer);

                        // Load result object with reply values.
                        result.success = reply.ResultCode == ResultCode.Success;
                        result.world = reply.World;
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
