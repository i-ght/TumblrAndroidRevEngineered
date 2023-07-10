using System;

namespace Tumblr.Bot.Exceptions
{
    internal class DeadProxyException : InvalidOperationException
    {
        public DeadProxyException()
        {
        }

        public DeadProxyException(string message) : base(message)
        {
        }

        public DeadProxyException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
