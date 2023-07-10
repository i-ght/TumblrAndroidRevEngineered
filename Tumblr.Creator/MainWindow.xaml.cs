using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Tumblr.Creator.SQLite;
using Waifu.Collections;
using Waifu.Imap.LoginInfo;
using Waifu.Net;
using Waifu.Sys;
using Waifu.WPF.SettingsDataGrid;
using MessageBox = System.Windows.MessageBox;

namespace Tumblr.Creator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly SQLiteDb _db;
        private readonly SettingsDataGrid _settingsDataGrid;

        public MainWindow()
        {
            InitializeComponent();
            WorkerMonitorSource = new ObservableCollection<DataGridItem>();
            WorkerMonitor.DataContext = this;
            Settings.Load();
            _settingsDataGrid = SettingsDataGrid();

            Title = $"{Assembly.GetExecutingAssembly().GetName().Name} {Assembly.GetExecutingAssembly().GetName().Version}";

            WorkerMonitor.LoadingRow += (o, args) =>
            {
                args.Row.Header = (args.Row.GetIndex() + 1).ToString();
            };

            var blacklistTable = SQLiteDbTableFactory.CreateBlacklistTable();
            _db = new SQLiteDb(blacklistTable);
        }

        public ObservableCollection<DataGridItem> WorkerMonitorSource { get; }

        private SettingsDataGrid SettingsDataGrid()
        {
            var settings = new ObservableCollection<SettingObj>
            {
                new SettingPrimitive<int>("Max workers", Constants.MaxWorkers, 1),
                new SettingPrimitive<int>("Max creates", Constants.MaxCreates, 1),
                new SettingConcurrentQueue("Imap enabled email accounts", Constants.EmailAccounts),
                new SettingConcurrentQueue(Constants.Proxies),
                new SettingConcurrentQueue("Email proxies", Constants.EmailProxies),
                new SettingConcurrentQueue(Constants.Words1),
                new SettingConcurrentQueue(Constants.Words2),
                new SettingPrimitive<int>("Confirm email timeout in seconds", Constants.ConfirmTimeout, 180),
                new SettingPrimitive<string>("2captcha api key", Constants.TwoCaptchaApiKey, string.Empty),
                new SettingPrimitive<int>("2captcha solve time out in seconds", Constants.TwoCaptchaSolveTimeout, 300),
                new SettingPrimitive<string>("Image directories", Constants.ImageDirs, string.Empty),
                new SettingPrimitive<bool>("Log all exceptions?", Constants.LogAllExceptions, false)
            };

            var settingsPage = (TabItem)TbMain.Items[1];
            var gridContent = (Grid)settingsPage.Content;
            var ret = new SettingsDataGrid(this, gridContent, settings);
            ret.CreateUi();
            return ret;
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            _settingsDataGrid.SavePrimitives();
            Process.GetCurrentProcess().Kill();
        }

        private List<WebProxy> Proxies(string settingKey)
        {
            var ret = new List<WebProxy>();
            var proxyStrs = _settingsDataGrid.GetConcurrentQueue(settingKey);
            if (proxyStrs.Count == 0)
                return ret;

            while (proxyStrs.Count > 0)
            {
                var str = proxyStrs.GetNext(false);
                if (NetHelpers.TryParseWebProxy(str, out var proxy))
                    ret.Add(proxy);
            }

            return ret;
        }

        private List<EmailAccountLoginInfo> Accounts()
        {
            var ret = new List<EmailAccountLoginInfo>();
            var acctStrs = _settingsDataGrid.GetConcurrentQueue(Constants.EmailAccounts);
            if (acctStrs.Count == 0)
                return ret;

            while (acctStrs.Count > 0)
            {
                var acctStr = acctStrs.GetNext(false);
                if (!EmailAccountLoginInfo.TryParse(acctStr, out var loginInfo))
                    continue;
                ret.Add(loginInfo);
#if DEBUG
                Console.WriteLine(loginInfo);
#endif
            }

            return ret;
        }

        private async Task<Collections> Collections()
        {
            var accts = await Task.Run(() => Accounts())
                .ConfigureAwait(false);
            if (accts.Count == 0)
                throw new InvalidOperationException("accounts list is empty or each line has an invalid format.");

            var proxies = await Task.Run(() => Proxies(Constants.Proxies))
                .ConfigureAwait(false);
            if (proxies.Count == 0)
                throw new InvalidOperationException("proxies list is empty.");

            var emailProxies = await Task.Run(() => Proxies(Constants.EmailProxies))
                .ConfigureAwait(false);
            if (emailProxies.Count == 0)
                throw new InvalidOperationException("email proxies list is empty.");

            var collections = new Collections(
                new ConcurrentQueue<EmailAccountLoginInfo>(accts),
                new ConcurrentQueue<WebProxy>(proxies),
                new ConcurrentQueue<WebProxy>(emailProxies),
                _settingsDataGrid
            );

            if (collections.Words1.Count == 0)
            {
                throw new InvalidOperationException("words1 is empty");
            }

            if (collections.Words2.Count == 0)
            {
                throw new InvalidOperationException("words2 is empty");
            }

            collections.ImageDirs.Shuffle();
            collections.Words1.Shuffle();
            collections.Words2.Shuffle();
            collections.Proxies.Shuffle();
            return collections;
        }

        private async Task ProxyReplenisherWorker(
            CancellationToken c,
            Collections collections,
            string settingKey)
        {
            try
            {
                while (!c.IsCancellationRequested)
                {
                    try
                    {
                        var proxies = Proxies(settingKey);
                        if (proxies.Count == 0)
                            continue;

                        foreach (var proxy in proxies)
                            collections.Proxies.Enqueue(proxy);
                    }
                    finally
                    {
                        await Task.Delay(60000, c)
                            .ConfigureAwait(false);
                    }
                }
            }
            catch (TaskCanceledException) {/**/}
        }

        private void InitWorkerMonitor(int maxWorkers)
        {
            WorkerMonitorSource.Clear();

            for (var i = 0; i < maxWorkers; i++)
            {
                var item = new DataGridItem
                {
                    Account = string.Empty,
                    Status = string.Empty
                };
                WorkerMonitorSource.Add(item);
            }
        }

        private async void CmdLaunch_Click(object sender, RoutedEventArgs e)
        {
            _settingsDataGrid.SavePrimitives();

            try
            {
                var collections = await Collections();

                var maxWorkers = Settings.Get<int>(Constants.MaxWorkers);
                InitWorkerMonitor(maxWorkers);

                CmdLaunch.IsEnabled = false;

                using (var c = new CancellationTokenSource())
                {
                    using (var writelock = new SemaphoreSlim(1, 1))
                    {
                        var stats = new Stats();
                        var statsWorker = StatsAsync(c.Token, stats);

                        var proxyReplWorker = ProxyReplenisherWorker(
                            c.Token,
                            collections,
                            Constants.Proxies
                        );
                        var emailProxyReplWorker = ProxyReplenisherWorker(
                            c.Token,
                            collections,
                            Constants.EmailProxies
                        );

                        var tasks = new List<Task>();
                        for (var i = 0; i < maxWorkers; i++)
                        {
                            var cls = new TumblrCreatorWorker(
                                i,
                                WorkerMonitorSource[i],
                                collections,
                                stats,
                                _db,
                                writelock
                            );
                            tasks.Add(cls.BaseAsync());
                        }

                        await Task.WhenAll(tasks);

                        c.Cancel();
                        await Task.WhenAll(
                            statsWorker,
                            proxyReplWorker,
                            emailProxyReplWorker
                        );
                    }

                    CmdLaunch.IsEnabled = true;
                    WorkerMonitorSource.Clear();
                    MessageBox.Show(@"Work complete");
                }
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    this,
                    ex.Message,
                    ex.GetType().Name,
                    MessageBoxButton.OK,
                    MessageBoxImage.Stop
                );
            }
        }

        private async Task StatsAsync(CancellationToken c, Stats stats)
        {
            try
            {
                var start = DateTime.Now;
                while (!c.IsCancellationRequested)
                {
                    var runTime = DateTime.Now.Subtract(start);

                    Title =
                        $"{Assembly.GetExecutingAssembly().GetName().Name} {Assembly.GetExecutingAssembly().GetName().Version} " +
                        $"[{string.Format("{3:D2}:{0:D2}:{1:D2}:{2:D2}", runTime.Hours, runTime.Minutes, runTime.Seconds, runTime.Days)}]";

                    LblAttempts.Content = $"Attempts: [{stats.Attempts:N0}]";
                    LblCreated.Content = $"Created: [{stats.Created:N0}]";
                    LblConfirmed.Content = $"Confirmed: [{stats.Confirmed:N0}]";

                    await Task.Delay(950, c);
                }
            }
            catch (OperationCanceledException) {/**/}
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await _db.EmailBlacklist.InitializeAsync()
                .ConfigureAwait(false);
            await Task.Run(() => AndroidDevices.Init())
                .ConfigureAwait(false);
            await Task.Run(() => AndroidDevices.Devices.Shuffle())
                .ConfigureAwait(false);
        }
    }
}