using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Akka.Actor;
using Tumblr.Bot.Helpers;
using Tumblr.Bot.OutgoingMessages;
using Tumblr.Bot.Shikaka.Actors.Supervisor;
using Tumblr.Bot.Shikaka.Messages.Supervisor;
using Tumblr.Bot.Shikaka.PropsContainers;
using Tumblr.Bot.SQLite;
using Tumblr.Bot.SQLite.Entities;
using Tumblr.Bot.UserInterface;
using Tumblr.Waifu;
using Waifu.Collections;
using Waifu.Net;
using Waifu.WPF.SettingsDataGrid;
using Settings = Waifu.Sys.Settings;

namespace Tumblr.Bot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private IActorRef _supervisor;
        private SettingsDataGrid _settingsDataGrid;
        private readonly SQLiteDb _db;

        public MainWindow()
        {
            InitializeComponent();
            WorkerMonitorSource = new ObservableCollection<DataGridItem>();
            WorkerMonitor.DataContext = this;
            Settings.Load();

            var chatBlacklistTable = SQLiteTableFactory.CreateChatBlacklistTable();
            var scriptsTable = SQLiteTableFactory.ScriptsTable();
            var convoStatesTable = SQLiteTableFactory.ConvoStatesTable();
            var greetBlacklistTable = SQLiteTableFactory.CreateGreetBlacklistTable();
            _db = new SQLiteDb(
                chatBlacklistTable,
                scriptsTable,
                convoStatesTable,
                greetBlacklistTable
            );

            Title = $"{Assembly.GetExecutingAssembly().GetName().Name} {Assembly.GetExecutingAssembly().GetName().Version}";

            WorkerMonitor.LoadingRow += (o, args) =>
            {
                args.Row.Header = (args.Row.GetIndex() + 1).ToString();
            };

            
        }

        public ObservableCollection<DataGridItem> WorkerMonitorSource { get; }

        private SettingsDataGrid SettingsDataGrid()
        {
            var settingFileStream = new SettingFileStream(Constants.Contacts);
            settingFileStream.FileStreamWaifuLoadedRaised += OnFileStreamWaifuLoadedRaised;

            var settings = new ObservableCollection<SettingObj>
            {
                new SettingPrimitive<int>("Max login errors", Constants.MaxLoginErrors, 5),
                new SettingPrimitive<int>("Min delay between greets (sec)", Constants.MinGreetDelay, 30),
                new SettingPrimitive<int>("Max delay between greets (sec)", Constants.MaxGreetDelay, 40),
                new SettingPrimitive<int>("Min greets per session", Constants.MinGreetsPerSession, 5),
                new SettingPrimitive<int>("Max greets per session", Constants.MaxGreetsPerSession, 10),
                new SettingPrimitive<int>("Min delay between greet sessions (min)", Constants.MinGreetSessionDelay, 20),
                new SettingPrimitive<int>("Max delay between greet sessions (min)", Constants.MaxGreetSessionDelay, 30),
                new SettingPrimitive<int>("Min delay between message sends (sec)", Constants.MinMsgSendDelay, 30),
                new SettingPrimitive<int>("Max delay between message sends (sec)", Constants.MaxMsgSendDelay, 40),
                new SettingPrimitive<int>("Max total greets per account", Constants.MaxTotalGreets, 8),
                new SettingPrimitive<int>("Max concurrent connection attempts at once", Constants.MaxConcurrentConnectionAttempts, 20),
                new SettingPrimitive<int>("Max concurrent replies allowed per account", Constants.MaxConcurrentReplies, 1),
                new SettingPrimitive<int>("Reconnect account if send message errors is greater than (x)", Constants.MaxSendMessageErrors, 3),
                new SettingPrimitive<bool>("Send next line of script to convos with 0 unread messages on first inbox load?", Constants.SendNextLineToCachedConvos, false),
                new SettingPrimitive<bool>("Disable greeting?", Constants.DisableGreeting, false),
                new SettingPrimitive<bool>("Disable replying?", Constants.DisableReplying, false),
                new SettingPrimitive<bool>("Log all exceptions?", Constants.LogAllExceptions, false),
                new SettingPrimitive<bool>("Enable chat log?", Constants.ChatLogEnabled, false),
                new SettingConcurrentQueue(Constants.Accounts),
                new SettingConcurrentQueue(Constants.Proxies),
                settingFileStream,
                new SettingConcurrentQueue(Constants.Greets),
                new SettingList(Constants.Script),
                new SettingConcurrentQueue(Constants.Links),
                new SettingKeywords(Constants.Keywords),
                new SettingList(Constants.Restricts)
            };

            var settingsPage = (TabItem)TbMain.Items[1];
            var gridContent = (Grid)settingsPage.Content;
            var ret = new SettingsDataGrid(this, gridContent, settings);

            ret.CollectionLoadedRaised += OnListLoadedRaised;

            ret.CreateUi();
            return ret;
        }

        private async void OnFileStreamWaifuLoadedRaised(
            object sender,
            EventArgs eventArgs)
        {
            if (_supervisor != null)
            {
                var msg = new TellContactReaderAdvanceContactStreamReaderToAnItemNotBlacklistedMessage();
                _supervisor.Tell(msg);
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    CmdLaunch.Header = "Please wait while the contact stream advances to a non blacklisted position.";
                    CmdLaunch.IsEnabled = false;
                });
                await ContactStreamHelpers.AdvanceContactStreamReaderToAnItemNotBlacklisted(
                    _settingsDataGrid.GetFileStream(Constants.Contacts).StreamReader,
                    _db
                );
                Dispatcher.Invoke(() =>
                {
                    CmdLaunch.IsEnabled = true;
                    CmdLaunch.Header = "Launch.";
                });
            }
        }

        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            await _db.InitializeAsync()
                .ConfigureAwait(false);

            _settingsDataGrid = SettingsDataGrid();
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            _settingsDataGrid.SavePrimitives();
            Process.GetCurrentProcess().Kill();
        }

        private List<WebProxy> Proxies()
        {
            var ret = new List<WebProxy>();
            var proxyStrs = _settingsDataGrid.GetConcurrentQueue(Constants.Proxies);
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
            var acctStrs = _settingsDataGrid.GetConcurrentQueue(Constants.Accounts);
            if (acctStrs.Count == 0)
                return ret;

            while (acctStrs.Count > 0)
            {
                var str = acctStrs.GetNext(false);
                if (!TumblrAccount.TryParse(str, out var session))
                    continue;

                ret.Add(session);
            }

            return ret;
        }

        private async void OnListLoadedRaised(
            object sender,
            CollectionLoadedRaisedEventArgs e)
        {
            if (e.SettingKey != Constants.Script)
                return;

            switch (e.SettingKey)
            {
                case Constants.Script:
                    await InsertNewScriptEntity(((List<string>)e.Collection).AsReadOnly())
                        .ConfigureAwait(false);
                    break;
            }
        }

        private async Task InsertNewScriptEntity(IReadOnlyCollection<string> script)
        {
            var scriptLines = DbHelpers.ListAsString(script);
            var sha256Sum = await Task.Run(
                () => DbHelpers.CalculateScriptKey(scriptLines)
            ).ConfigureAwait(false);

            if (!await _db.ScriptsTable.ContainsKeyAsync(sha256Sum)
                .ConfigureAwait(false))
            {
                var scriptEntity = new ScriptEntity(
                    sha256Sum,
                    scriptLines,
                    script.Count
                );

                await _db.ScriptsTable.InsertAsync(scriptEntity)
                    .ConfigureAwait(false);
            }
        }

        private async Task ProxyReplenisherWorker(CancellationToken c, Collections collections)
        {
            try
            {
                while (!c.IsCancellationRequested)
                {
                    try
                    {
                        var proxies = Proxies();
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

        private Collections Collections()
        {
            var accounts = Accounts();
            if (accounts.Count == 0)
                throw new InvalidOperationException("accounts list is empty");

            var proxies = Proxies();
            if (proxies.Count == 0)
                throw new InvalidOperationException("proxies list is empty");

            var collections = new Collections(
                new ConcurrentQueue<TumblrAccount>(accounts),
                new ConcurrentQueue<WebProxy>(proxies),
                _settingsDataGrid
            );

            if (collections.Script.Count == 0)
                throw new InvalidOperationException("script file is empty");

            collections.Proxies.Shuffle();

            return collections;
        }

        private void InitGridUi(int maxWorkers)
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

        private async Task<Dictionary<string, ScriptWaifu>> GetConvoStatesFromDb(
            string forBotUsername,
            GlobalStats stats)
        {
            var ret = new Dictionary<string, ScriptWaifu>();
            var items = await _db.ConvoStatesTable.RetrieveByBotUsername(forBotUsername)
                .ConfigureAwait(false);
            var conversationStateEntities =
                items as ReadOnlyCollection<ConversationStateEntity> ?? items.ToList().AsReadOnly();
            if (conversationStateEntities.Count == 0)
                return ret;

            using (var transaction = _db.ChatBlacklistTable.BeginTransaction())
            {
                try
                {
                    foreach (var item in conversationStateEntities)
                    {
                        if (item.IsComplete)
                            continue;

                        if (await _db.ChatBlacklistTable.ContainsItemAsync(item.ContactUsername)
                            .ConfigureAwait(false))
                        {
                            continue;
                        }

                        var scriptEntity = await _db.ScriptsTable.RetrieveByKeyAsync(
                            item.ScriptSha256Sum
                        ).ConfigureAwait(false);
                        var script = DbHelpers.ScriptLinesToList(
                            scriptEntity.ScriptLines
                        );

                        var scriptWaifu = new ScriptWaifu(
                            script,
                            item.ScriptIndex
                        );
                        ret.Add(
                            item.ContactUsername,
                            scriptWaifu
                        );

                        stats.Convos++;
                    }

                    transaction.Commit();
                    return ret;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
                
        }

        private async void CmdLaunch_Click(object sender, RoutedEventArgs e)
        {
            _settingsDataGrid.SavePrimitives();

            try
            {
                var collections = await Task.Run(
                    () => Collections()
                );

                var cnt = collections.Accounts.Count;
                InitGridUi(cnt);

                await InsertNewScriptEntity(
                    collections.Script.AsReadOnly()
                );

                CmdLaunch.IsEnabled = false;

                using (var c = new CancellationTokenSource())
                {
                    var workersOnline = new List<int>();
                    var stats = new GlobalStats();
                    var statsWorker = StatsAsync(c.Token, stats, workersOnline);
                    var proxyWorker = ProxyReplenisherWorker(c.Token, collections);

                    using (var system = ActorSystem.Create("actorsystem"))
                    {
                        var allAcctsConvoStates = new Dictionary<
                            string,
                            Dictionary<
                                string,
                                ScriptWaifu
                            >
                        >();
                        foreach (var acct in collections.Accounts)
                        {
                            var acctUsername = acct.Username;

                            var convos = await GetConvoStatesFromDb(
                                acctUsername,
                                stats
                            ).ConfigureAwait(false);

                            allAcctsConvoStates.Add(acctUsername, convos);
                        }

                        var readonlyAllAcctsConvoStates = new ReadOnlyDictionary<
                            string,
                            Dictionary<
                                string,
                                ScriptWaifu
                            >
                        >(allAcctsConvoStates);

                        var accountsCnt = collections.Accounts.Count;
                        var supervisorPropsContainer = new SupervisorPropsContainer(
                            accountsCnt,
                            collections,
                            WorkerMonitorSource,
                            stats,
                            _db,
                            TxtChatLog,
                            readonlyAllAcctsConvoStates,
                            workersOnline
                        );

                        var props = SupervisorActor.CreateProps(
                            supervisorPropsContainer
                        );

                        _supervisor = system.ActorOf(
                            props,
                            "supervisor"
                        );

                        var startAppSessionMsg = new StartAppSessionMessage();
                        _supervisor.Tell(startAppSessionMsg);

                        await system.WhenTerminated;
                    }

                    _supervisor = null;
                    c.Cancel();
                    await Task.WhenAll(proxyWorker, statsWorker);

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

        private void CmdClearChatLog_Click(object sender, RoutedEventArgs e)
        {
            TxtChatLog.Clear();
        }

        private void TxtChatLog_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
                return;

            TxtChatLog.Focus();
            TxtChatLog.CaretIndex = TxtChatLog.Text.Length;
            TxtChatLog.ScrollToEnd();
        }

        private void TxtChatLog_TextChanged(object sender, TextChangedEventArgs e)
        {
            TxtChatLog.ScrollToEnd();
        }

        private async Task StatsAsync(
            CancellationToken c,
            GlobalStats stats,
            IReadOnlyCollection<int> workersOnline)
        {
            try
            {
                var start = DateTime.Now;
                while (!c.IsCancellationRequested)
                {
                    var runTime = DateTime.Now.Subtract(start);
                    await Dispatcher.InvokeAsync(() =>
                    {
                        Title =
                            $"{Assembly.GetExecutingAssembly().GetName().Name} {Assembly.GetExecutingAssembly().GetName().Version} " +
                            $"[{string.Format("{3:D2}:{0:D2}:{1:D2}:{2:D2}", runTime.Hours, runTime.Minutes, runTime.Seconds, runTime.Days)}]";

                        LblOnline.Content = $"Online: [{workersOnline.Count:N0}]";
                        LblGreets.Content = $"Greets: [{stats.Greets:N0}]";
                        LblConvos.Content = $"Convos: [{stats.Convos:N0}]";
                        LblIn.Content = $"In: [{stats.In:N0}]";
                        LblOut.Content = $"Out: [{stats.Out:N0}]";
                        LblLinks.Content = $"Links: [{stats.Links:N0}]";
                        LblCompleted.Content = $"Completed: [{stats.Completed:N0}]";
                        LblRestricts.Content = $"Restricts: [{stats.Restricts:N0}]";
                    });

                    await Task.Delay(950, c)
                        .ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException) {/**/}
        }
    }
}
