using System;

namespace Tumblr.Bot.OutgoingMessages
{
    internal class OutgoingMessage
    {
        public OutgoingMessage(
            string toUuid,
            string fromUuid,
            string toUsername,
            string fromUsername,
            string body,
            ScriptWaifu scriptWaifu,
            OutgoingMessageFlags flags)
        {
            ToUuid = toUuid;
            FromUuid = fromUuid;
            ToUsername = toUsername;
            FromUsername = fromUsername;
            Body = body;
            ScriptWaifu = scriptWaifu;
            Flags = flags;
        }

        public OutgoingMessage(
            string toUuid,
            string fromUuid,
            string toUsername,
            string fromUsername,
            string body, 
            OutgoingMessageFlags flags)
        : this(toUuid, fromUuid, toUsername, fromUsername, body, null, flags)
        {
        }

        public string ToUuid { get; }
        public string FromUuid { get; }
        public string ToUsername { get; }
        public string FromUsername { get; }
        public string Body { get; }
        public OutgoingMessageFlags Flags { get; }
        public ScriptWaifu ScriptWaifu { get; }
        public int SendErrors { get; set; }
    }
}
