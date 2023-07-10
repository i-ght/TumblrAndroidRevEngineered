using System;
using System.Runtime.Serialization;

namespace Tumblr.Waifu.Exceptions
{
    public class TumblrSessionNotAuthorizedException : InvalidOperationException
    {
        public TumblrSessionNotAuthorizedException()
        {
        }

        public TumblrSessionNotAuthorizedException(string message) : base(message)
        {
        }

        public TumblrSessionNotAuthorizedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected TumblrSessionNotAuthorizedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
