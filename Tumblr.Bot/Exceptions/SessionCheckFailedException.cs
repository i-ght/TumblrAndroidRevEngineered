using System;

namespace Tumblr.Bot.Exceptions
{
    internal class SessionCheckFailedException : InvalidOperationException
    {

        public SessionCheckFailedException()
        {
        }

        public SessionCheckFailedException(string message) : base(message)
        {
        }

        public SessionCheckFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
