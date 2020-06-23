using System;
using System.IO;
using System.Text;
using System.Threading;

using BokuShared;
using BokuShared.Wire;

namespace Boku.Web.Trans
{
    /// <summary>
    /// Web request for deleting a world from the community site.
    /// </summary>
    public class GetCurrentVersion : CommunityRequest
    {
        protected override string MethodName { get { return "GetCurrentVersion"; } }

        #region Private

        string productName;

        #endregion

        #region Public Types

        public class Result
        {
            public bool success;
            public object userState;
            public Message_Version version;
        }

        #endregion

        #region Public Methods

        public GetCurrentVersion(string productName, SendOrPostCallback callback, object userState)
            : base(false, callback, userState)
        {
            this.productName = productName;
        }

        #endregion

        #region Protected Methods

        protected override bool ISend()
        {
            progress.Message = "Checking for updates...";

            Message_GetCurrentVersion request = new Message_GetCurrentVersion();
            request.Token = productName;

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
                        result.version = Message_Version.Load(buffer);
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
