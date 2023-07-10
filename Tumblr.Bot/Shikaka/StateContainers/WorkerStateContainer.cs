using System;
using System.Collections.Generic;
using Akka.Actor;
using Tumblr.Bot.Enums;
using Tumblr.Bot.OutgoingMessages;
using Tumblr.Bot.UserInterface;
using Waifu.Sys;

namespace Tumblr.Bot.Shikaka.StateContainers
{
    internal class WorkerStateContainer
    {
        public WorkerStateContainer(
            LocalStats localStats,
            Dictionary<string, ScriptWaifu> convos)
        {
            LoginErrors = 0;
            WorkerState = WorkerState.Disconnected;
            LocalStats = localStats;
            Convos = convos;
            FirstInboxLoad = true;
        }

        public bool FirstInboxLoad { get; set; }
        public int LoginErrors { get; set; }
        public WorkerState WorkerState { get; set; }
        public LocalStats LocalStats { get; }
        public DateTimeOffset LastMessageRcvdAt { get; set; }
        public Dictionary<string, ScriptWaifu> Convos { get; }
        public bool WaitingForConversationsRetrieval { get; set; }
        public ICancelable SessionCheckerJob { get; set; }
        public int SendMessageErrors { get; set; }
    }
}
