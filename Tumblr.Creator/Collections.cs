using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Waifu.Collections;
using Waifu.Imap.LoginInfo;
using Waifu.Sys;
using Waifu.WPF.SettingsDataGrid;

namespace Tumblr.Creator
{
    internal class Collections
    {
        private readonly SettingsDataGrid _settings;
        private readonly ConcurrentQueue<string> _imageDirs;

        public Collections(
            ConcurrentQueue<EmailAccountLoginInfo> emailAccounts,
            ConcurrentQueue<WebProxy> proxies,
            ConcurrentQueue<WebProxy> emailProxies,
            SettingsDataGrid settings)
        {
            EmailAccounts = emailAccounts;
            Proxies = proxies;
            EmailProxies = emailProxies;
            _settings = settings;
            _imageDirs = new ConcurrentQueue<string>();
        }

        public ConcurrentQueue<EmailAccountLoginInfo> EmailAccounts { get; }
        public ConcurrentQueue<WebProxy> Proxies { get; }
        public ConcurrentQueue<WebProxy> EmailProxies { get; }
        public ConcurrentQueue<string> Words1 => _settings.GetConcurrentQueue(Constants.Words1);
        public ConcurrentQueue<string> Words2 => _settings.GetConcurrentQueue(Constants.Words2);

        public ConcurrentQueue<string> ImageDirs
        {
            get
            {
                var imageDirs = Settings.Get<string>(Constants.ImageDirs);
                if (imageDirs == null)
                    return _imageDirs;

                if (!Directory.Exists(imageDirs))
                    return _imageDirs;

                var dirs = new HashSet<string>(Directory.GetDirectories(imageDirs));
                if (dirs.SetEquals(_imageDirs))
                    return _imageDirs;

                _imageDirs.Clear();
                foreach (var dir in dirs)
                    _imageDirs.Enqueue(dir);

                return _imageDirs;
            }
        }
    }
}