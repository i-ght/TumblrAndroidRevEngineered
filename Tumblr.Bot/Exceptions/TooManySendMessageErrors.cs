using System;
using System.Runtime.Serialization;

namespace Tumblr.Bot.Exceptions
{
    internal class TooManySendMessageErrors : InvalidOperationException
    {
        public TooManySendMessageErrors()
        {
        }

        public TooManySendMessageErrors(string message) : base(message)
        {
        }

        public TooManySendMessageErrors(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected TooManySendMessageErrors(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
