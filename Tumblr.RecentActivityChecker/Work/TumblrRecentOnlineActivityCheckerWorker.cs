using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Tumblr.RecentActivityChecker.Exceptions;
using Tumblr.RecentActivityChecker.SQLite;
using Tumblr.RecentActivityChecker.UserInterface;
using Tumblr.Waifu;
using Tumblr.Waifu.Exceptions;
using Tumblr.Waifu.JsonObjects;
using Waifu.Collections;
using Waifu.Net.Http;
using Waifu.Net.Http.Exceptions;
using Waifu.Sys;

namespace Tumblr.RecentActivityChecker.Work
{
    internal class TumblrRecentOnlineActivityCheckerWorker : Worker
    {
        private static readonly ConcurrentDictionary<string, ScrapeSession> ScraperSessions;

        static TumblrRecentOnlineActivityCheckerWorker()
        {
            ScraperSessions = new ConcurrentDictionary<string, ScrapeSession>();
        }

        private readonly Collections _collections;
        private readonly Stats _stats;
        private readonly SQLiteDb _sqliteDb;
        private readonly WriteWorker _writeWorker;

        public TumblrRecentOnlineActivityCheckerWorker(
            int index,
            DataGridItem ui,
            Collections collections,
            Stats stats,
            SQLiteDb sqliteDb,
            WriteWorker writeWorker)
            : base(index, ui)
        {
            _collections = collections;
            _stats = stats;
            _sqliteDb = sqliteDb;
            _writeWorker = writeWorker;
        }
        public override async Task BaseAsync()
        {
            while (!_collections.ContactsStream.EOF)
            {
                try
                {
                    if (!TryGetNextAccount(out var account))
                        break;

                    if (!ScraperSessions.ContainsKey(account.Username))
                    {
                        ScraperSessions.TryAdd(
                            account.Username,
                            new ScrapeSession()
                        );
                    }

                    var scrapeSession = ScraperSessions[account.Username];

                    UiDataGridItem.Account = account.Username;

                    var httpClientCfg = new HttpWaifuConfig
                    {
                        Proxy = _collections.Proxies.GetNext()
                    };
                    var httpClient = new HttpWaifu(httpClientCfg);
                    var tumblrClient = new TumblrClient(account, httpClient);

                    await Connect(
                        tumblrClient,
                        scrapeSession,
                        account
                    ).ConfigureAwait(false);

                    try
                    {
                        await MainLoop(tumblrClient)
                            .ConfigureAwait(false);
                    }
                    finally
                    {
                        _collections.Accounts.Enqueue(account);
                    }
                }
                catch (Exception e)
                {
                    switch (e)
                    {
                        case ThreadAbortException _:
                        case StackOverflowException _:
                        case OutOfMemoryException _:
                            Environment.FailFast("woopz", e);
                            throw;

                        default:
                            await LogExceptionAndUpdateUserInterface(e)
                                .ConfigureAwait(false);
                            break;
                    }
                }
            }

            UiDataGridItem.Account = string.Empty;
            UiDataGridItem.Status = string.Empty;
        }

        private async Task MainLoop(TumblrClient client)
        {
            var errors = 0;
            while (errors < 5 && 
                !_collections.ContactsStream.EOF)
            {
                var contactStr = await _collections.ContactsStream.GetNextAsync()
                    .ConfigureAwait(false);

                if (!TumblrContact.TryParse(contactStr,
                    out var contact))
                {
                    continue;
                }

                var milliseconds = Settings.Get<int>(Constants.DelayBetweenRequests);
                await UpdateThreadStatusAsync(
                    $"Retrieving info for {contact.Username}: ...",
                    TimeSpan.FromMilliseconds(milliseconds)
                ).ConfigureAwait(false);

                try
                {
                    var info = await client.RetrieveBlogInfo(contact.Username)
                        .ConfigureAwait(false);

                    ParseBlogInfo(contact, info);

                    await AddBlacklist(contact.Username)
                        .ConfigureAwait(false);

                    errors = 0;
                }
                catch (HttpRequestFailedException e)
                {
                    var cont = false;
                    switch (e.HttpStatusCode)
                    {
                        case HttpStatusCode.NotFound:
                            await AddBlacklist(contact.Username)
                                .ConfigureAwait(false);
                            cont = true;
                            break;
                    }

                    if (cont)
                        continue;

                    if (errors++ > 5)
                        throw;

                    await LogExceptionAndUpdateUserInterface(e)
                        .ConfigureAwait(false);

                    var seconds = Settings.Get<int>(Constants.DelayIfRated);
                    var delay = TimeSpan.FromSeconds(seconds - 5);
                    await Task.Delay(delay)
                        .ConfigureAwait(false);
                }
            }
        }

        private async Task AddBlacklist(string username)
        {
            var entity = new BlacklistEntity(username);
            await _sqliteDb.ContactBlacklist.InsertAsync(entity)
                .ConfigureAwait(false);
        }

        private void ParseBlogInfo(
            TumblrContact contact,
            RetrieveBlogInfoTumblrApiResponse blogInfo)
        {
            if (blogInfo == null ||
                blogInfo.Response == null  ||
                blogInfo.Response.Blog == null)
            {
                return;
            }

            var blog = blogInfo.Response.Blog;
            var seconds = blog.SecondsSinceLastActivity;
            if (seconds == -1)
            {
                var oneDay = TimeSpan.FromDays(1);
                seconds = (int)(oneDay.TotalSeconds * -1);
            }

            var timespan = TimeSpan.FromSeconds(seconds);
            contact.TimeSinceLastActivity = timespan;

            if (timespan.Days < Settings.Get<int>(Constants.ConsiderContactRecentValue)
                && timespan.Days != -1)
            {
                Interlocked.Increment(ref _stats.OnlineRecently);
            }
            else
            {
                Interlocked.Increment(ref _stats.NotOnlineRecenty);
            }

            _writeWorker.AddUserAsync(contact).ToApmBegin();
        }

        private async Task Connect(
            TumblrClient client,
            ScrapeSession session,
            TumblrAccount account)
        {
            try
            {
                await CheckSession(client)
                    .ConfigureAwait(false);
            }
            catch (Exception e) when (
                e is HttpRequestFailedException ||
                e is TumblrSessionNotAuthorizedException
            )
            {
                switch (e)
                {
                    case HttpRequestFailedException httpReqFailedEx:
                        if (httpReqFailedEx.HttpStatusCode == 0)
                        {
                            _collections.Accounts.Enqueue(account);
                            throw new DeadProxyException(
                                "Failed to connect to proxy",
                                httpReqFailedEx
                            );
                        }

                        if (session.LoginErrors++ < Settings.Get<int>(
                                Constants.MaxLoginErrors))
                        {
                            _collections.Accounts.Enqueue(account);
                        }
                        throw;

                    case TumblrSessionNotAuthorizedException _:
                        throw;
                }

            }
        }

        private async Task CheckSession(TumblrClient client)
        {
            UiDataGridItem.Status = "Checking accounts session: ...";

            await client.RetrieveUserInfo()
                .ConfigureAwait(false);
        }

        private bool TryGetNextAccount(out TumblrAccount acct)
        {
            if (_collections.Accounts.Count > 0)
            {
                acct = _collections.Accounts.GetNext(false);
                return true;
            }

            acct = null;
            return false;
        }
    }
}
