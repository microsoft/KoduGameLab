using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

using Boku.Common;

namespace BokuContentProcessors
{
    [ContentProcessor(DisplayName = "Censor Database - Boku")]
    public class CensorContentProcessor : ContentProcessor<CensorContent.CensorContentFile, CensorContent>
    {
        class Logger : IBokuContentBuildLogger
        {
            public ContentProcessorContext Context;

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

        public override CensorContent Process(CensorContent.CensorContentFile input, ContentProcessorContext context)
        {
            Logger logger = new Logger();
            logger.Context = context;

            CensorContent censor = new CensorContent(new ContentIdentity(input.Filename));

            censor.Compile(input, logger);

            return censor;
        }
    }
}
