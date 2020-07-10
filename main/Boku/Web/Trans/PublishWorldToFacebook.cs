// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Threading;

using Boku.Common.Sharing;

using BokuShared;
using BokuShared.Wire;

namespace Boku.Web.Trans
{
    public class PublishWorldToFacebook : CommunityRequest
    {
        protected override string MethodName { get { return "PublishWorldToFacebook"; } }

        #region Private Members

        Guid koduFacebookId;
        Guid worldId;

        #endregion

        #region Public Types

        public class Result
        {
            public bool success;
            public object userState;
            public FacebookResultCode facebookResultCode;
        }

        #endregion

        #region Public Methods

        public PublishWorldToFacebook(Guid koduFacebookId, Guid worldId, SendOrPostCallback callback, object userState)
            : base(true, callback, userState)
        {
            this.koduFacebookId = koduFacebookId;
            this.worldId = worldId;
        }

        #endregion

        #region Protected Methods

        protected override bool ISend()
        {
            progress.Message = "Publishing...";

            Message_PublishWorldToFacebookRequest request = new Message_PublishWorldToFacebookRequest();
            request.KoduFacebookId = koduFacebookId;
            request.WorldId = worldId;

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
                        Message_PublishWorldToFacebookReply reply = Message_PublishWorldToFacebookReply.Load(buffer);

                        // Load result object with reply values.
                        result.success = reply.ResultCode == ResultCode.Success;

                        if (result.success)
                        {
                            result.facebookResultCode = reply.FacebookResultCode;
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
