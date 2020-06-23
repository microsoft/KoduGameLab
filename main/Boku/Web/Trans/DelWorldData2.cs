using System;
using System.Diagnostics;
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
    /// Web request for deleting a world from the community site.
    /// </summary>
    public class DelWorldData2 : CommunityRequest
    {
        protected override string MethodName { get { return "DelWorldData2"; } }

        #region Private Members

        Guid worldId;
        string pin;

        #endregion

        #region Public Types

        public class Result
        {
            public bool success;
            public object userState;
        }

        #endregion

        #region Public Methods

        public DelWorldData2(Guid worldId,string pin, SendOrPostCallback callback, object userState)
            : base(true, callback, userState)
        {
            this.worldId = worldId;
            this.pin = pin;
        }

        #endregion

        #region Protected Methods

        protected override bool ISend()
        {
            progress.Message = "Deleting...";

            Message_DelWorldData2Request request = new Message_DelWorldData2Request();
            request.SiteId = SiteID.Instance.Value.ToString();
            request.UserName = GetUserName();
            request.Community = Program2.SiteOptions.Community;
            request.WorldId = worldId;
            request.pin = pin;

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
                        Message_DelWorldData2Reply reply = Message_DelWorldData2Reply.Load(buffer);

                        // Load result object with reply values.
                        result.success = reply.ResultCode == ResultCode.Success;
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
