using System;
using System.Runtime.Serialization;

namespace Tumblr.RecentActivityChecker.Exceptions
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

        protected DeadProxyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
