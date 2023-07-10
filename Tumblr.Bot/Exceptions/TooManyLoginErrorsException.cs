using System;

namespace Tumblr.Bot.Exceptions
{
    internal class TooManyLoginErrorsException : InvalidOperationException
    {
        public TooManyLoginErrorsException()
        {
        }

        public TooManyLoginErrorsException(string message) : base(message)
        {
        }

        public TooManyLoginErrorsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
