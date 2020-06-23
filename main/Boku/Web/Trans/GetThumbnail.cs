using System;
using System.IO;
using System.Text;
using System.Threading;

using BokuShared;
using BokuShared.Wire;

namespace Boku.Web.Trans
{
    /// <summary>
    /// Web request to download a set of world meta data from the community site.
    /// </summary>
    public class GetThumbnail : CommunityRequest
    {
        protected override string MethodName { get { return "GetThumbnail"; } }

        #region Private Members

        Guid worldId;

        #endregion

        #region Public Types

        public class Result
        {
            public bool success;
            public object userState;
            public byte[] thumbnailBytes;
        }

        #endregion

        #region Public Methods

        public GetThumbnail(
            Guid worldId,
            SendOrPostCallback callback,
            object userState)
            : base(true, callback, userState)
        {
            this.worldId = worldId;
        }

        #endregion

        #region Protected Methods

        protected override bool ISend()
        {
            // We want this operation to happen in the background, so close the progress screen.
            progress.Complete();

            string requestFmt =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<soap12:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap12=\"http://www.w3.org/2003/05/soap-envelope\">" +
                "  <soap12:Body>" +
                "    <{0} xmlns=\"http://boku.microsoft.com/community\">" +
                "      <worldIdStr>{1}</worldIdStr>" +
                "    </{0}>" +
                "  </soap12:Body>" +
                "</soap12:Envelope>";

            string requestBody = String.Format(
                requestFmt,
                MethodName,
                worldId.ToString());

            return SendString(requestBody);
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
                        result.thumbnailBytes = Convert.FromBase64String(str);
                    }
                }
                str = null;
            }
            catch
            {
                result.success = false;
            }

            GC.Collect();

            return result;
        }

        #endregion
    }
}
