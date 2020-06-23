using System;
using System.IO;
using System.Text;
using System.Threading;

using BokuShared;
using BokuShared.Wire;


namespace Boku.Web.Trans
{
    /// <summary>
    /// Web request for retrieving a user's admin level.
    /// </summary>
    public class Hello : CommunityRequest
    {
        protected override string MethodName { get { return "Hello2"; } }

        #region Public Types

        public class Result
        {
            public bool success;
            public object userState;
            public int symmetricIndex;
        }

        #endregion

        #region Public Methods

        public Hello(SendOrPostCallback callback, object userState)
            : base(false, callback, userState)
        {
        }

        #endregion

        #region Protected Methods

        protected override bool ISend()
        {
            progress.Message = "Connecting...";

            // Put the public key in the Hello request.
            Message_HelloRequest request = new Message_HelloRequest();

            // Serialize the Hello request into a buffer.
            byte[] buffer = request.SaveToArray();

            return SendBuffer(buffer);
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
                        Message_Hello2Reply reply = Message_Hello2Reply.Load(buffer);

                        // Set results to data from reply
                        result.symmetricIndex = reply.Index;
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
