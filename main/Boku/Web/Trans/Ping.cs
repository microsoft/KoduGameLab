// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Threading;

using BokuShared;


namespace Boku.Web.Trans
{
    /// <summary>
    /// Web request to test whether the community site is accessible and responsive.
    /// </summary>
    public class Ping : CommunityRequest
    {
        protected override string MethodName { get { return "Ping"; } }

        #region Public Types

        public class Result
        {
            public bool success;
            public object userState;
        }

        #endregion

        #region Public Methods

        public Ping(SendOrPostCallback callback, object userState)
            : base(true, callback, userState)
        {
        }

        #endregion

        #region Protected Methods

        protected override bool ISend()
        {
            progress.Message = "Pinging...";

            string requestFmt =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<soap12:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap12=\"http://www.w3.org/2003/05/soap-envelope\">\n" +
                "  <soap12:Body>\n" +
                "    <{0} xmlns=\"http://boku.microsoft.com/community\" />\n" +
                "  </soap12:Body>\n" +
                "</soap12:Envelope>\n";

            string requestBody = String.Format(
                requestFmt,
                MethodName);

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
