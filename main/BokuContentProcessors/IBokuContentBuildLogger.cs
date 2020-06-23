using System;
using System.Collections.Generic;
using System.Text;

namespace Boku.Common
{
    public interface IBokuContentBuildLogger
    {
        void LogImportantMessage(string message, params object[] messageArgs);
        void LogMessage(string message, params object[] messageArgs);
        void LogWarning(string helpLink, object contentIdentity, string message, params object[] messageArgs);
    }

    public class BokuContentBuildConsoleLogger : IBokuContentBuildLogger
    {
        static IBokuContentBuildLogger instance;

        public static IBokuContentBuildLogger Instance
        {
            get { return instance ?? (instance = new BokuContentBuildConsoleLogger()); }
        }

        private BokuContentBuildConsoleLogger()
        {
        }

        public void LogImportantMessage(string message, params object[] messageArgs)
        {
            Console.Write("Important: ");
            Console.WriteLine(message, messageArgs);
        }

        public void LogMessage(string message, params object[] messageArgs)
        {
            Console.Write("Message: ");
            Console.WriteLine(message, messageArgs);
        }

        public void LogWarning(string helpLink, object contentIdentity, string message, params object[] messageArgs)
        {
            Console.Write("Warning: ");
            Console.WriteLine(message, messageArgs);
        }
    }
}
