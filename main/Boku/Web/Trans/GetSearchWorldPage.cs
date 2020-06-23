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
    /// Web request to download a set of world meta data from the community site.
    /// </summary>
    public class GetSearchWorldPage : CommunityRequest
    {
        protected override string MethodName { get { return "GetSearchWorldPage"; } }

        #region Private Members

        int first;
        int count;
        Genres genreFilter;
        string searchString;
        SortBy sortBy;
        SortDirection sortDir;

        #endregion

        #region Public Types

        public class Result
        {
            public bool success;
            public object userState;
            public WorldPagePacket page;
        }

        #endregion

        #region Public Methods

        public GetSearchWorldPage(
            Genres genreFilter,
            string searchString,
            SortBy sortBy,
            SortDirection sortDir,
            int first,
            int count,
            SendOrPostCallback callback,
            object userState)
            : base(true, callback, userState)
        {
            this.first = first;
            this.count = count;
            this.genreFilter = genreFilter;
            this.searchString = searchString;
            this.sortBy = sortBy;
            this.sortDir = sortDir;
        }

        #endregion

        #region Protected Methods

        protected override bool ISend()
        {
            // We want this operation to happen in the background, so close the progress screen.
            progress.Complete();

            Message_GetSearchWorldPageRequest request = new Message_GetSearchWorldPageRequest();
            request.SiteId = SiteID.Instance.Value.ToString();
            request.UserName = GetUserName();
            request.Community = Program2.SiteOptions.Community;
            request.First = first;
            request.Count = count;
            request.SortBy = sortBy.ToString();
            request.SortDir = sortDir.ToString();
            request.GenreFilter = (int)genreFilter;
            request.SearchString = searchString;

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
                        Message_GetSearchWorldPageReply reply = Message_GetSearchWorldPageReply.Load(buffer);

                        // Load result object with reply values.
                        result.success = reply.ResultCode == ResultCode.Success;
                        result.page = reply.Page;
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

            // The buffers used above can be quite large (multi-megs in size),
            // so force a collection now to avoid them piling up waiting to
            // be collected later.
            GC.Collect();

            return result;
        }

        #endregion
    }
}
