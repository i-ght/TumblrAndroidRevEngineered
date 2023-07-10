using System;

namespace Tumblr.Creator.Exceptions
{
    internal class EmailAlreadyRegisteredException : Exception
    {
        public EmailAlreadyRegisteredException()
        {
        }

        public EmailAlreadyRegisteredException(string message) : base(message)
        {
        }

        public EmailAlreadyRegisteredException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
