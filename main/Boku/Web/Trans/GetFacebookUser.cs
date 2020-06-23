using System;
using System.IO;
using System.Text;
using System.Threading;

#if !NETFX_CORE
    using Boku.Common.Sharing;
#endif

using BokuShared;
using BokuShared.Wire;

namespace Boku.Web.Trans
{
    public class GetFacebookUser : CommunityRequest
    {
        protected override string MethodName { get { return "GetFacebookUser"; } }

        #region Private Members

        Guid koduFacebookId;

        #endregion

        #region Public Types

        public class Result
        {
            public bool success;
            public object userState;
            public FacebookResultCode facebookResultCode;
            public FacebookUser facebookUser;
        }

        #endregion

        #region Public Methods

        public GetFacebookUser(Guid koduFacebookId, SendOrPostCallback callback, object userState)
            : base(true, callback, userState)
        {
            this.koduFacebookId = koduFacebookId;
        }

        #endregion

        #region Protected Methods

        protected override bool ISend()
        {
            progress.Message = "Getting Facebook Info...";

            Message_GetFacebookUserRequest request = new Message_GetFacebookUserRequest();
            request.KoduFacebookId = koduFacebookId;

            return EncryptAndSendBuffer(request.SaveToArray());
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
                string str = string.Empty;

                if (arg.responseBody != null)
                    str = Encoding.ASCII.GetString(arg.responseBody);

                if (success)
                {
                    if (DeSoapify(ref str))
                    {
                        // B64 decode the return value.
                        byte[] buffer = Convert.FromBase64String(str);

                        // Decrypt the buffer.
                        buffer = CryptoHelper.Decrypt(GetSymmetricAlgorithm(), buffer);

                        // Deserialize the reply.
                        Message_GetFacebookUserReply reply = Message_GetFacebookUserReply.Load(buffer);

                        // Load result object with reply values.
                        result.success = reply.ResultCode == ResultCode.Success;

                        if (result.success)
                        {
                            result.facebookResultCode = reply.FacebookResultCode;
                            result.facebookUser = reply.FacebookUser;
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
