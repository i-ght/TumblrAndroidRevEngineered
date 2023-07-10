using System.Collections.Concurrent;
using System.Net;
using Tumblr.Waifu;
using Waifu.Collections;
using Waifu.WPF.SettingsDataGrid;

namespace Tumblr.RecentActivityChecker.UserInterface
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

        public ConcurrentQueue<TumblrAccount> Accounts { get; }
        public ConcurrentQueue<WebProxy> Proxies { get; }
        public FileStreamWaifu ContactsStream => _settings.GetFileStream(Constants.Contacts);
    }
}
