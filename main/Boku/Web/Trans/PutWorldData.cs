using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

using Boku.Common;
using Boku.Common.Sharing;
using BokuShared;
using BokuShared.Wire;

namespace Boku.Web.Trans
{

    /// <summary>
    /// Web request to upload a world to the community site.
    /// </summary>
    public class PutWorldData : CommunityRequest
    {
        protected override string MethodName { get { return "PutWorldData"; } }

        #region Private Members

        BokuShared.Wire.WorldPacket world;

        #endregion

        #region Public Types

        public class Result
        {
            public bool success;
            public object userState;
            public Guid worldId;
        }

        #endregion

        #region Public Methods

        public PutWorldData(
            BokuShared.Wire.WorldPacket world,
            SendOrPostCallback callback,
            object userState)
            : base(true, callback, userState)
        {
            //Compress world data before sending.
            world.Data.VirtualMapBytes = BokuShared.Compression.Compress(world.Data.VirtualMapBytes);
            world.Data.StuffXmlBytes = BokuShared.Compression.Compress(world.Data.StuffXmlBytes);
            world.Data.WorldXmlBytes = BokuShared.Compression.Compress(world.Data.WorldXmlBytes);

            this.world = world;
        }

        #endregion

        #region Protected Methods
        private string ReformatDate(string input)
        {
            long DATE1970_TICKS = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).Ticks;
#if NETFX_CORE
            Regex DATE_SERIALIZATION_REGEX = new Regex(@"\\/Date\((?<ticks>-?\d+)\)\\/");
#else
            Regex DATE_SERIALIZATION_REGEX = new Regex(@"\\/Date\((?<ticks>-?\d+)\)\\/", RegexOptions.Compiled);
#endif

            Match match;

            while ((match = DATE_SERIALIZATION_REGEX.Match(input)).Success)
            {
                long ticks = long.Parse(match.Groups["ticks"].Value) * 10000;
                DateTime dateTime = new DateTime(ticks + DATE1970_TICKS).ToLocalTime();
                input = input.Replace(match.Value, dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
            }

            return input;
        }

        protected override bool ISend()
        {
            progress.Message = "Uploading level...";

            Message_PutWorldDataRequest2 request = new Message_PutWorldDataRequest2();
            request.SiteId = SiteID.Instance.Value.ToString();
            request.UserName = GetUserName();
            request.Community = Program2.SiteOptions.Community;
            request.World = world;
            request.idHash = Auth.IdHash;
            request.UserId = 0;

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
                        Message_PutWorldDataReply reply = Message_PutWorldDataReply.Load(buffer);

                        // Load result object with reply values.
                        result.success = reply.ResultCode == ResultCode.Success;
                        result.worldId = reply.WorldId;
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
