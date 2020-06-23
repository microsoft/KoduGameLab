using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;

using Boku.Common.Sharing;

using BokuShared;
using BokuShared.Wire;

namespace Boku.Web.Trans
{
    public class Instrumentation : CommunityRequest
    {
        protected override string MethodName { get { return "Instrumentation"; } }

        Boku.Common.Instrumentation.Instruments instruments;

        #region Public Types

        public class Result
        {
            public bool success;
            public object userState;
        }

        #endregion

        #region Public

        internal Instrumentation(
            Boku.Common.Instrumentation.Instruments instruments,
            SendOrPostCallback callback,
            object userState)
            : base(false, callback, userState)
        {
            this.instruments = instruments;
        }

        #endregion

        #region Protected

        protected override bool ISend()
        {
            progress.Message = "Sending your feedback...";


            Message_Instrumentation request = new Message_Instrumentation();
            request.UserName = GetUserName();
            request.Community = Program2.SiteOptions.Community;

            for (int i = 0; i < instruments.events.Length; ++i)
            {
                List<Boku.Common.Instrumentation.Event> list = instruments.events[i];
                if (list == null)
                    continue;
                for (int j = 0; j < list.Count; ++j)
                {
                    Boku.Common.Instrumentation.Event src = list[j];
                    InstrumentationPacket.Event dst = new InstrumentationPacket.Event();
                    dst.Name = src.Id.ToString();
                    dst.Comment = src.Comment;
                    request.Instruments.Events.Add(dst);
                }
            }

            for (int i = 0; i < instruments.timers.Length; ++i)
            {
                Boku.Common.Instrumentation.Timer src = instruments.timers[i];
                if (src == null)
                    continue;
                InstrumentationPacket.Timer dst = new InstrumentationPacket.Timer();
                dst.Name = src.Id.ToString();
                dst.TotalTime = src.TotalTime;
                dst.Count = src.Count;
                request.Instruments.Timers.Add(dst);
            }

            for (int i = 0; i < instruments.counters.Length; ++i)
            {
                Boku.Common.Instrumentation.Counter src = instruments.counters[i];
                if (src == null)
                    continue;
                InstrumentationPacket.Counter dst = new InstrumentationPacket.Counter();
                dst.Name = src.Id.ToString();
                dst.Count = src.Count;
                request.Instruments.Counters.Add(dst);
            }

            for (int i = 0; i < instruments.dataItems.Length; ++i)
            {
                List<Boku.Common.Instrumentation.DataItem> list = instruments.dataItems[i];
                if (list == null)
                    continue;
                for (int j = 0; j < list.Count; ++j)
                {
                    Boku.Common.Instrumentation.DataItem src = list[j];
                    InstrumentationPacket.DataItem dst = new InstrumentationPacket.DataItem();
                    dst.Name = src.Id.ToString();
                    dst.Value = src.Value;
                    request.Instruments.DataItems.Add(dst);
                }
            }

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
