using System;
using System.IO;
using System.Text;
using System.Threading;

#if NETFX_CORE
#else
    using System.Web;
#endif

using Boku.Common.Sharing;
using BokuShared;
using BokuShared.Wire;

namespace Boku.Web.Trans
{
    public class GetKoduWebUser : CommunityRequest
    {
        protected override string MethodName { get { return "GetKoduWebUser"; } }

        #region Private Members

        string userSecret;

        #endregion

        #region Public Types

        public class Result
        {
            public bool success;
            public object userState;
            public KoduWebUser koduWebUser;
        }

        #endregion

        #region Public Methods

        public GetKoduWebUser(string userSecret, SendOrPostCallback callback, object userState)
            : base(true, callback, userState)
        {
            this.userSecret = userSecret;
        }

        #endregion

        #region Protected Methods

        protected override bool ISend()
        {
            progress.Message = "Getting User Info...";

            Message_GetWebUserRequest request = new Message_GetWebUserRequest();
            request.UserSecret = userSecret;

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
                        Message_GetWebUserReply reply = Message_GetWebUserReply.Load(buffer);

                        // Load result object with reply values.
                        result.success = reply.ResultCode == ResultCode.Success;

                        if (result.success)
                        {
                            result.koduWebUser = reply.KoduUser;
                        }
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
