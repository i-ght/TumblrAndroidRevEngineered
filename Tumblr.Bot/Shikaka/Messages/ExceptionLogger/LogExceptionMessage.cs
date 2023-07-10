using System;

namespace Tumblr.Bot.Shikaka.Messages.ExceptionLogger
{
    internal class LogExceptionMessage
    {
        public LogExceptionMessage(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; }
    }
}
