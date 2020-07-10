// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

using Microsoft.Xna.Framework.Content.Pipeline;

namespace BokuContentProcessors
{
    using Boku.Common;

    [ContentImporter(".CSV", CacheImportedData = true, DefaultProcessor = "CensorContentProcessor", DisplayName = "Censor Database - Boku")]
    public class CensorContentImporter : ContentImporter<CensorContent.CensorContentFile>
    {
        class Logger : IBokuContentBuildLogger
        {
            public ContentImporterContext Context;

            public void LogMessage(string message, params object[] messageArgs)
            {
                Context.Logger.LogMessage(message, messageArgs);
            }

            public void LogImportantMessage(string message, params object[] messageArgs)
            {
                Context.Logger.LogImportantMessage(message, messageArgs);
            }

            public void LogWarning(string helpLink, object contentIdentity, string message, params object[] messageArgs)
            {
                Context.Logger.LogWarning(helpLink, contentIdentity as ContentIdentity, message, messageArgs);
            }
        }

        public override CensorContent.CensorContentFile Import(string filename, ContentImporterContext context)
        {
            Logger logger = new Logger();
            logger.Context = context;

            StreamReader reader = new StreamReader(filename);

            CensorContent.CensorContentFile file = CensorContent.ReadSourceRepresentation(reader, filename, logger);

            reader.Close();

            return file;
        }
    }
}
