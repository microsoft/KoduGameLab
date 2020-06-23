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
    /// Web request for deleting a world from the community site.
    /// </summary>
    public class VoteOnLevel : CommunityRequest
    {
        protected override string MethodName { get { return "VoteOnLevel"; } }

        #region Private members

        Guid worldId;
        Vote vote;

        #endregion

        #region Public Types

        public class Result
        {
            public bool success;
            public object userState;
        }

        #endregion

        #region Public Methods

        public VoteOnLevel(Guid worldId, Vote vote, SendOrPostCallback callback, object userState)
            : base(true, callback, userState)
        {
            this.worldId = worldId;
            this.vote = vote;
        }

        #endregion

        #region Protected Methods

        protected override bool ISend()
        {
            progress.Message = "Sending your vote...";

            Message_VoteOnLevelRequest request = new Message_VoteOnLevelRequest();
            request.SiteId = SiteID.Instance.Value.ToString();
            request.UserName = GetUserName();
            request.Community = Program2.SiteOptions.Community;
            request.WorldId = worldId;
            request.Vote = (int)vote;

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
                        Message_VoteOnLevelReply reply = Message_VoteOnLevelReply.Load(buffer);

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
