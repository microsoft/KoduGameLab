// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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
    public class ReportError : CommunityRequest
    {
        protected override string MethodName { get { return "ReportError"; } }

        #region Private members

        string errorMessage;
        string stackTrace;
        string addInfo;

        #endregion

        #region Public Types

        public class Result
        {
            public bool success;
            public object userState;
        }

        #endregion

        #region Public Methods

        public ReportError(string errorMessage, string stackTrace, string addInfo, SendOrPostCallback callback, object userState)
            : base(false, callback, userState)
        {
            this.errorMessage = errorMessage;
            this.stackTrace = stackTrace;
            this.addInfo = addInfo;
        }

        #endregion

        #region Protected Methods

        protected override bool ISend()
        {
            progress.Message = "Sending error report...";

            Message_ReportError request = new Message_ReportError();
            request.ErrorMessage = errorMessage;
            request.StackTrace = stackTrace;
            request.AddInfo = addInfo;

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
