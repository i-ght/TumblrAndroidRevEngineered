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
using Tumblr.Scraper.SQLite;
using Tumblr.Scraper.UserInterface;
using Tumblr.Scraper.Work;
using Tumblr.Waifu;
using Waifu.Collections;
using Waifu.Net;
using Waifu.Sys;
using Waifu.WPF.SettingsDataGrid;
using MessageBox = System.Windows.MessageBox;

namespace Tumblr.Scraper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private CancellationTokenSource _c;
        private Task _writeWorkerTask;

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

            var keywordBlacklistTbl = SQLiteTableFactory.CreateKeywordBlacklistTable();
            _db = new SQLiteDb(keywordBlacklistTbl);
        }

        public ObservableCollection<DataGridItem> WorkerMonitorSource { get; }

        private SettingsDataGrid SettingsDataGrid()
        {
            var settings = new ObservableCollection<SettingObj>
            {
                new SettingPrimitive<int>("Max workers", Constants.MaxWorkers, 1),
                new SettingPrimitive<int>("Max login errors", Constants.MaxLoginErrors, 3),
                new SettingPrimitive<int>("Delay between search requests (milliseconds)", Constants.DelayBetweenRequests, 500),
                new SettingPrimitive<int>("Delay (x) seconds if server returns Rate Limit Exceeded response status code", Constants.DelayIfRated, 20),
                new SettingConcurrentQueue(Constants.Accounts),
                new SettingConcurrentQueue(Constants.Proxies),
                new SettingFileStream(Constants.Keywords),
                new SettingPrimitive<bool>("Log all exceptions?", Constants.LogAllExceptions, false)
            };

            var settingsPage = (TabItem)TbMain.Items[1];
            var gridContent = (Grid)settingsPage.Content;
            var ret = new SettingsDataGrid(this, gridContent, settings);
            ret.CreateUi();
            return ret;
        }

        private async void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (_c != null &&
                !_c.IsCancellationRequested &&
                _writeWorkerTask != null)
            {
                _c.Cancel();
                await _writeWorkerTask
                    .ConfigureAwait(false);
            }

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

        private List<TumblrAccount> Accounts()
        {
            var ret = new List<TumblrAccount>();
            var acctStrs = _settingsDataGrid.GetConcurrentQueue(
                Constants.Accounts
            );
            if (acctStrs.Count == 0)
                return ret;

            while (acctStrs.Count > 0)
            {
                var acctStr = acctStrs.GetNext(false);
                if (!TumblrAccount.TryParse(
                    acctStr,
                    out var imapLoginInfo))
                {
                    continue;
                }
                ret.Add(imapLoginInfo);
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
          
            var collections = new Collections(
                new ConcurrentQueue<TumblrAccount>(accts), 
                new ConcurrentQueue<WebProxy>(proxies),
                _settingsDataGrid
            );
           
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

                using (_c = new CancellationTokenSource())
                {

                    var stats = new Stats();
                    var statsWorker = StatsAsync(_c.Token, stats);
                    var writeWorker = new WriteWorker("tumblr-contacts_scraped.txt");
                    _writeWorkerTask = writeWorker.Base(_c.Token);

                    var proxyReplWorker = ProxyReplenisherWorker(
                        _c.Token,
                        collections,
                        Constants.Proxies
                    );

                    var tasks = new List<Task>();
                    for (var i = 0; i < maxWorkers; i++)
                    {
                        var cls = new TumblrScraperWorker(
                            i,
                            WorkerMonitorSource[i],
                            collections,
                            stats,
                            _db,
                            writeWorker
                        );
                        tasks.Add(cls.BaseAsync());
                    }

                    await Task.WhenAll(tasks);

                    _c.Cancel();
                    await Task.WhenAll(
                        _writeWorkerTask,
                        statsWorker,
                        proxyReplWorker
                    );

                }

                CmdLaunch.IsEnabled = true;
                WorkerMonitorSource.Clear();
                MessageBox.Show(@"Work complete");
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

                    LblScraped.Content = $"Scraped: [{stats.Scraped:N0}]";

                    await Task.Delay(950, c);
                }
            }
            catch (OperationCanceledException) {/**/}
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await _db.InitializeAsync()
                .ConfigureAwait(false);
        }
    }
}
