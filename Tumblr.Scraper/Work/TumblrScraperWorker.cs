using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tumblr.Scraper.Exceptions;
using Tumblr.Scraper.SQLite;
using Tumblr.Scraper.UserInterface;
using Tumblr.Waifu;
using Tumblr.Waifu.Exceptions;
using Tumblr.Waifu.JsonObjects;
using Waifu.Collections;
using Waifu.Net.Http;
using Waifu.Net.Http.Exceptions;
using Waifu.Sys;

namespace Tumblr.Scraper.Work
{
    internal class TumblrScraperWorker : Mode
    {
        private static readonly ConcurrentDictionary<string, ScrapeSession> ScraperSessions;

        static TumblrScraperWorker()
        {
            ScraperSessions = new ConcurrentDictionary<string, ScrapeSession>();
        }

        private readonly Collections _collections;
        private readonly Stats _stats;
        private readonly SQLiteDb _sqliteDb;
        private readonly WriteWorker _writeWorker;

        public TumblrScraperWorker(
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
            while (!_collections.KeywordsStreamReader.EOF &&
                _collections.Accounts.Count > 0)
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
                        await Scrape(tumblrClient)
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

            UiDataGridItem.Status = string.Empty;
            UiDataGridItem.Account = string.Empty;
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

        private async Task Scrape(TumblrClient client)
        {
            var cursor = string.Empty;
            GetNextKeywordResult result;
            while ((result = await TryGetNextKeyword()
                .ConfigureAwait(false)).Success)
            {
                var keyword = result.Value;
                var pageIndex = 1;
                UiDataGridItem.Status = $"Scraping keyword {keyword} [{pageIndex}]: ...";
                var firstPageResponseContainer = await client.RetrieveSearchResults(
                    keyword,
                    cursor
                ).ConfigureAwait(false);

                await ParseUsers(firstPageResponseContainer)
                    .ConfigureAwait(false);

                var errors = 0;
                cursor = GetCursor(firstPageResponseContainer);
                while (!string.IsNullOrWhiteSpace(cursor))
                {
                    try
                    {
                        var milliseconds = Settings.Get<int>(Constants.DelayBetweenRequests);
                        await UpdateThreadStatusAsync(
                            $"Scraping keyword {keyword} [{++pageIndex}]: ...",
                            TimeSpan.FromMilliseconds(milliseconds)
                        ).ConfigureAwait(false);

                        var responseContainer = await client.RetrieveSearchResults(
                            keyword,
                            cursor
                        ).ConfigureAwait(false);

                        await ParseUsers(responseContainer)
                            .ConfigureAwait(false);

                        cursor = GetCursor(responseContainer);

                        errors = 0;
                    }
                    catch (InvalidOperationException e)
                    {
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

                var entity = new BlacklistItemEntity(keyword);
                await _sqliteDb.KeywordBlacklistTable.InsertAsync(entity)
                    .ConfigureAwait(false);
            }
        }

        private async Task ParseUsers(
            RetrieveSearchResultsTumblrApiResponse responseContainer)
        {
            if (responseContainer.Response == null ||
                responseContainer.Response.Timeline == null ||
                responseContainer.Response.Timeline.Elements == null ||
                responseContainer.Response.Timeline.Elements.Count == 0)
            {
                return;
            }

            var users = responseContainer.Response.Timeline.Elements;
            var items = new List<string>();
            foreach (var user in users)
            {
                if (user == null ||
                    user.Resources == null ||
                    user.Resources.Count == 0)
                {
                    continue;
                }

                foreach (var resrc in user.Resources)
                {
                    if (string.IsNullOrWhiteSpace(resrc.Name) ||
                        string.IsNullOrWhiteSpace(resrc.Url) ||
                        string.IsNullOrWhiteSpace(resrc.Uuid))
                    {
                        continue;
                    }

                    items.Add(
                        $"{resrc.Name}|{resrc.Uuid}"
                    );
                    break;
                }
            }

            Interlocked.Add(ref _stats.Scraped, items.Count);

            await _writeWorker.AddUsersAsync(items.AsReadOnly())
                .ConfigureAwait(false);
        }

        private static string GetCursor(RetrieveSearchResultsTumblrApiResponse responseContainer)
        {
            if (responseContainer.Response == null ||
                responseContainer.Response.Timeline == null ||
                responseContainer.Response.Timeline.Links == null ||
                responseContainer.Response.Timeline.Links.Next == null ||
                responseContainer.Response.Timeline.Links.Next.QueryParams == null)
            {
                return string.Empty;
            }

            return responseContainer.Response.Timeline.Links.Next.QueryParams.Cursor;
        }

        private async Task<GetNextKeywordResult> TryGetNextKeyword()
        {
            while (!_collections.KeywordsStreamReader.EOF)
            {
                var keyword = await _collections.KeywordsStreamReader.GetNextAsync()
                    .ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    continue;
                }

                if (await _sqliteDb.KeywordBlacklistTable.ContainsItemAsync(keyword)
                    .ConfigureAwait(false))
                {
                    continue;
                }

                return new GetNextKeywordResult(true, keyword);
            }

            return new GetNextKeywordResult(false);
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
