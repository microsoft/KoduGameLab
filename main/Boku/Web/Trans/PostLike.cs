using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using BokuShared.Wire;

namespace Boku.Web.Trans
{
    public class PostLike : CommunityRequest
    {
         protected override string MethodName { get { return "PostLike"; } }


        #region Public Methods

         public PostLike(
            SendOrPostCallback callback,
            object userState,
            PostLikePacket packet)
            : base(true, callback, userState)
        {
            this.packet = packet;
        }

        #endregion

        PostLikePacket packet;

        #region Public Types

        public class Result
        {
            public bool success;
            public object userState;
        }

        #endregion

        #region Protected Methods


        protected override bool ISend()
        {
            var request = new Message_PostLikeRequest();
            request.packet = this.packet;

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
                        Message_PostLikeReply reply = Message_PostLikeReply.Load(buffer);

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
