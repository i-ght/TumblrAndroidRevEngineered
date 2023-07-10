using System;

namespace Tumblr.Bot.Exceptions
{
    internal class ConversationsRetrievalException : InvalidOperationException
    {
        public ConversationsRetrievalException()
        {
        }

        public ConversationsRetrievalException(string message) : base(message)
        {
        }

        public ConversationsRetrievalException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
