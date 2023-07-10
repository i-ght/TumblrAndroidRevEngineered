using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using Tumblr.Waifu;
using Waifu.Collections;
using Waifu.WPF.SettingsDataGrid;

namespace Tumblr.Bot.UserInterface
{
    internal class Collections
    {
        private readonly SettingsDataGrid _settings;

        public Collections(
            ConcurrentQueue<TumblrAccount> accounts,
            ConcurrentQueue<WebProxy> proxies,
            SettingsDataGrid settings)
        {
            Accounts = accounts;
            Proxies = proxies;
            _settings = settings;
        }

        public FileStreamWaifu Contacts => _settings.GetFileStream(Constants.Contacts);

        public ConcurrentQueue<TumblrAccount> Accounts { get; }
        public ConcurrentQueue<WebProxy> Proxies { get; }

        public ConcurrentQueue<string> Greets => _settings.GetConcurrentQueue(Constants.Greets);
        public ConcurrentQueue<string> Links => _settings.GetConcurrentQueue(Constants.Links);

        public List<string> Script => _settings.GetList(Constants.Script);
        public List<string> Restricts => _settings.GetList(Constants.Restricts);

        public Dictionary<string, List<string>> Keywords => _settings.GetKeywordsList(Constants.Keywords);
    }
}
