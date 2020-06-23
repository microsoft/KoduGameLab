
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Boku.Common.Sharing;

#if NETFX_CORE
    using Boku.Common;
#endif


namespace Boku.Web.Trans
{
    enum WebMethod
    {
        Get,
        Post
    }

    enum ContentType
    {
        TextPlain,
        TextXml
    }

    /// <summary>
    /// Provides services shared by all community web requests.
    /// </summary>
    public abstract class Request
    {
        protected static string UnencryptedRequestFmt =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<soap12:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap12=\"http://www.w3.org/2003/05/soap-envelope\">" +
            "<soap12:Body>" +
            "<{0} xmlns=\"http://boku.microsoft.com/community\">" +
            "<index>{1}</index>" +
            "<packet>{2}</packet>" +
            "</{0}>" +
            "</soap12:Body>" +
            "</soap12:Envelope>";

        public enum WebResult
        {
            Complete,					// Check resultCode to determine success.
            ErrServiceNotFound,		    // The specified service was not found in the config file.
            ErrConnectFailed,			// Network connect failed.
            ErrDisconnected,			// Prematurely disconnected from server.
            ErrCanceled,                // If the app implements a cancel mechanism, it can use this error code for it.
            ErrAborted,                 // If the app implements an abort mechanism, it can use this error code for it.
        }

        public class WebRequestCompleteArg
        {
            public WebResult result;
            public int resultCode;
            public byte[] responseBody;
            public object userState;
        }

        class Completion
        {
            public Request request;
            public object param;
        }

        static Queue<Completion> completionQueue = new Queue<Completion>();
        static List<Completion> completionList = new List<Completion>();

        public static void Update()
        {
            lock (completionQueue)
            {
                while (completionQueue.Count > 0)
                {
                    completionList.Add(completionQueue.Dequeue());
                }
            }

            foreach (Completion completion in completionList)
            {
                object result = completion.request.IComplete(completion.param);

                if (completion.request.callback != null)
                    completion.request.callback(result);
            }

            completionList.Clear();
        }

        static void EnqueueCompletion(Request request, object param)
        {
            Completion completion = new Completion();
            completion.request = request;
            completion.param = param;

            lock (completionQueue)
            {
                completionQueue.Enqueue(completion);
            }
        }

        public static string UnpackString(byte[] buf)
        {
            if (buf != null)
            {
#if NETFX_CORE
                return Encoding.UTF8.GetString(buf, 0, buf.Length);
#else
                return Encoding.ASCII.GetString(buf);
#endif
            }

            return string.Empty;
        }

        // TODO *******, ****: There's probably a better place for this to live.
        public static string GetUserName()
        {
            return BokuShared.Auth.CreatorName;
        }

        HttpWebRequest request;
        HttpWebResponse response;

        #region Private Members
        private bool secure;
        private bool canceled;
        private bool aborted;
        #endregion

        #region Protected Members
        protected string serviceName;
        protected ProgressOperation progress;
        protected SendOrPostCallback callback;
        protected object userState;
        #endregion

        #region Public Methods
        public Request(string serviceName, bool needsCryptoKey, SendOrPostCallback callback, object userState)
        {
            this.serviceName = serviceName;
            this.secure = needsCryptoKey;
            this.callback = callback;
            this.userState = userState;
        }

        /// <summary>
        /// Cancel the transaction. The callback will be made with the ErrCanceled result code.
        /// NOTE: Due to the asynchronous nature of networks, the transation may complete as you
        /// are canceling it. You have been warned :)
        /// </summary>
        public void Cancel()
        {
            canceled = true;
        }

        /// <summary>
        /// Abort the transaction. The transaction is not interrupted, but no callback will be
        /// made upon completion.
        /// NOTE: Due to the asynchronous nature of networks, the transation may complete as you
        /// are aborting it. You have been warned :)
        /// </summary>
        public void Abort()
        {
            aborted = true;
        }

        public virtual bool Send()
        {
            if (!Program2.SiteOptions.NetworkEnabled)
            {
                return false;
            }

            progress = ProgressScreen.RegisterOperation();

            try
            {
                // If security is turned on, We need to have an Index from the server in order to proceed
                if (secure && Community.SymmetricIndex == null)
                {
                    Hello hello = new Hello(Callback_Hello, null);
                    return hello.Send();
                }
                else
                {
                    if (ISend())
                    {
                        return true;
                    }
                    else
                    {
                        progress.Complete();
                        return false;
                    }
                }
            }
            catch
            {
                progress.Complete();
                return false;
            }
        }

        #endregion

        #region Protected Methods

        protected abstract string MethodName { get; }

        /// <summary>
        /// Remove the outer soap tags, leaving the XML-formatted response body.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="action"></param>
        /// <param name="typename"></param>
        /// <returns></returns>
        protected bool DeSoapify(ref string str, string typename)
        {
            int startIndex = str.IndexOf(String.Format("<{0}Result", MethodName));
            int endIndex = str.IndexOf(String.Format("</{0}Response>", MethodName));

            if (startIndex != -1 && endIndex != -1)
            {
                str = str.Substring(startIndex, endIndex - startIndex);
                str = str
                    .Replace(String.Format("<{0}Result", MethodName), String.Format("<{0}", typename))
                    .Replace(String.Format("</{0}Result>", MethodName), String.Format("</{0}>", typename));
                return true;
            }

            return false;
        }

        protected bool DeSoapify(ref string str)
        {
            int startIndex = str.IndexOf(String.Format("<{0}Result", MethodName));
            int endIndex = str.IndexOf(String.Format("</{0}Response>", MethodName));

            if (startIndex != -1 && endIndex != -1)
            {
                str = str.Substring(startIndex, endIndex - startIndex);
                str = str
                    .Replace(String.Format("<{0}Result>", MethodName), "")
                    .Replace(String.Format("</{0}Result>", MethodName), "");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Extract a block of XML, including its outer tags.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="typename"></param>
        /// <returns></returns>
        protected bool XmlExtract(ref string str, string typename)
        {
            string startTag = String.Format("<{0}", typename);
            string endTag = String.Format("</{0}>", typename);

            int startIndex = str.IndexOf(startTag);
            int endIndex = str.IndexOf(endTag);

            if (startIndex != -1 && endIndex != -1)
            {
                str = str.Substring(startIndex, endIndex - startIndex + endTag.Length);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Extract a block of XML, removing its outer tags.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="typename"></param>
        /// <returns></returns>
        protected bool XmlRemoveTag(ref string str, string typename)
        {
            string startTag = String.Format("<{0}", typename);
            string endTag = String.Format("</{0}>", typename);

            int startIndex = str.IndexOf(startTag);
            int endIndex = str.IndexOf(endTag, startIndex);

            if (startIndex != -1 && endIndex != -1)
            {
                startIndex = str.IndexOf('>', startIndex) + 1;
                str = str.Substring(startIndex, endIndex - startIndex);
                return true;
            }

            return false;
        }

        protected bool SendBuffer(byte[] buffer)
        {
            // B64 encode the buffer.
            string packet = Convert.ToBase64String(buffer);

            string requestBody = String.Format(
                UnencryptedRequestFmt,
                MethodName,
                Community.SymmetricIndex,
                packet);

            return SendString(requestBody);
        }

        protected bool SendString(string requestBody)
        {
            try
            {
                request = (HttpWebRequest)HttpWebRequest.Create(serviceName);
                request.Method = "POST";

                request.ContentType = "text/xml";

                request.BeginGetRequestStream(request_GetRequestStream, requestBody);

                return true;
            }
            catch
            {
                return false;
            }
        }

        void request_GetRequestStream(IAsyncResult ar)
        {
            try
            {
                string requestBody = (string)ar.AsyncState;

                Stream stream = request.EndGetRequestStream(ar);
                StreamWriter writer = new StreamWriter(stream);
                writer.WriteLine(requestBody);
#if NETFX_CORE
                writer.Flush();
                writer.Dispose();
#else
                writer.Close();
#endif

                request.BeginGetResponse(request_GetResponse, null);
            }
            catch
            {
                OnComplete(null);
            }
        }

        void request_GetResponse(IAsyncResult ar)
        {
            try
            {
                response = (HttpWebResponse)request.EndGetResponse(ar);
                Stream stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream);
                string result = reader.ReadToEnd();
                byte[] buffer = Encoding.UTF8.GetBytes(result);
                
                WebRequestCompleteArg arg = new WebRequestCompleteArg();
                arg.result = WebResult.Complete;
                arg.responseBody = buffer;
                arg.resultCode = (int)response.StatusCode;
                OnComplete(arg);
            }
            catch
            {
                OnComplete(null);
            }
        }

        #endregion

        #region Private Methods

        private void Callback_Hello(object replyObj)
        {
            Hello.Result reply = (Hello.Result)replyObj;

            if (reply.success)
            {
                Community.SymmetricIndex = reply.symmetricIndex;

                if (!ISend())
                {
                    OnComplete(null);
                }
            }
            else
            {
                OnComplete(null);
            }
        }

        private void OnComplete(object param)
        {
            progress.Complete();

            if (aborted)
            {
                return;
            }

            if (canceled)
            {
                WebRequestCompleteArg arg = new WebRequestCompleteArg();
                arg.result = WebResult.ErrCanceled;
                param = arg;
            }
            else if (param == null)
            {
                WebRequestCompleteArg arg = new WebRequestCompleteArg();
                arg.result = WebResult.ErrConnectFailed;
                param = arg;
            }

            EnqueueCompletion(this, param);
        }

        #endregion

        #region Abstract Methods

        protected abstract bool ISend();
        protected abstract object IComplete(object param);

        #endregion
    }
}
