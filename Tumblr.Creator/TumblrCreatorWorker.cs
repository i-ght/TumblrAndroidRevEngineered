using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Tumblr.Creator.Exceptions;
using Tumblr.Creator.JsonObjects;
using Tumblr.Creator.SQLite;
using Tumblr.Waifu;
using Tumblr.Waifu.JsonObjects;
using Waifu.Captcha;
using Waifu.Collections;
using Waifu.Imap;
using Waifu.Imap.LoginInfo;
using Waifu.MobileDevice;
using Waifu.Net.Http;
using Waifu.Sys;
using Encoder = System.Drawing.Imaging.Encoder;
using HttpRequest = Waifu.Net.Http.HttpRequest;
using HttpResponse = Waifu.Net.Http.HttpResponse;

namespace Tumblr.Creator
{
    internal class TumblrCreatorWorker : Mode
    {
        private static readonly ConcurrentDictionary<string, int> EmailAccountLoginErrors;
        private static readonly Regex ConfirmLinkRegex;
        private static readonly Regex FormKeyRegex;
        private static readonly Regex VerifyEmailFormActionRegex;
        private static readonly Regex RedRegex;
        private static readonly ReadOnlyCollection<string> CommonDesktopUserAgents;
        private static readonly SemaphoreSlim WriteLock;

        static TumblrCreatorWorker()
        {
            EmailAccountLoginErrors = new ConcurrentDictionary<string, int>();
            RedRegex = new Regex(
                "red=(.*?)&",
                RegexOptions.Compiled | RegexOptions.IgnoreCase
            );
            ConfirmLinkRegex = new Regex(
                "href=\"(.*?)\".*?>This is me",
                RegexOptions.Compiled | RegexOptions.IgnoreCase
            );
            FormKeyRegex = new Regex(
                "<input type=\"hidden\" name=\"form_key\" value=\"(.*?)\"",
                RegexOptions.Compiled | RegexOptions.IgnoreCase
            );
            VerifyEmailFormActionRegex = new Regex(
                "<form id=\"verify_email_form\" action=\"(.*?)\"",
                RegexOptions.Compiled | RegexOptions.IgnoreCase
            );

            var lst = new List<string>
            {
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.90 Safari/537.36",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.101 Safari/537.36",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_12_6) AppleWebKit/603.3.8 (KHTML, like Gecko) Version/10.1.2 Safari/603.3.8",
                "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.90 Safari/537.36"
            };
            CommonDesktopUserAgents = lst.AsReadOnly();
            WriteLock = new SemaphoreSlim(1, 1);
        }

        private readonly Collections _collections;
        private readonly Stats _stats;
        private readonly SQLiteDb _db;
        private readonly SemaphoreSlim _writeLock;

        public TumblrCreatorWorker(
            int index,
            DataGridItem ui,
            Collections collections,
            Stats stats,
            SQLiteDb db,
            SemaphoreSlim writeLock) : base(index, ui)
        {
            _collections = collections;
            _stats = stats;
            _db = db;
            _writeLock = writeLock;
        }

        public override async Task BaseAsync()
        {
            while (_collections.EmailAccounts.Count > 0 &&
                _stats.Created < Settings.Get<int>(Constants.MaxCreates))
            {
                var requeueEmailAccount = true;
                var confirmStatus = ConfirmStatus.None;
                TumblrAccount tumblrAccount = null;

                var emailLoginInfo = _collections.EmailAccounts.GetNext(false);

                try
                {
                    if (await _db.EmailBlacklist.ContainsLoginIdAsync(emailLoginInfo.LoginId)
                        .ConfigureAwait(false))
                    {
                        requeueEmailAccount = false;
                        continue;
                    }

                    EmailAccountLoginErrors.TryAdd(emailLoginInfo.LoginId, 0);
                    UI.Account = emailLoginInfo.LoginId;
                    Interlocked.Increment(ref _stats.Attempts);

                    var cfg = new HttpWaifuConfig
                    {
                        Proxy = _collections.Proxies.GetNext()
                    };
                    var client = new HttpWaifu(cfg);

                    var emailProxy = _collections.EmailProxies.GetNext();

                    await CheckEmailAccountCredentials(
                        emailLoginInfo,
                        emailProxy,
                        client
                    ).ConfigureAwait(false);

                    var regInfo = await CreateAccountRegisterInfo(emailLoginInfo.LoginId)
                        .ConfigureAwait(false);
                    var tumblrCreatorClient = new TumblrCreatorClient(
                        regInfo.SessionInfo,
                        client
                    );

                    await SendPreonboardingRequest(tumblrCreatorClient)
                        .ConfigureAwait(false);

                    //var seconds = ThreadSafeStaticRandom.RandomInt(3, 9);
                    //await UpdateThreadStatusAsync(
                    //    $"Delaying {seconds} seconds before next step: ...",
                    //    seconds * 1000
                    //).ConfigureAwait(false);

                    var nonce = await RetrieveRegistrationNonce(tumblrCreatorClient)
                        .ConfigureAwait(false);

                    try
                    {
                        await CheckIfEmailAndUsernameIsAvailable(
                            tumblrCreatorClient,
                            regInfo
                        ).ConfigureAwait(false);
                    }
                    catch (EmailAlreadyRegisteredException)
                    {
                        requeueEmailAccount = false;
                        throw;
                    }

                    //seconds = ThreadSafeStaticRandom.RandomInt(20, 40);
                    //await UpdateThreadStatusAsync(
                    //    $"Delaying {seconds} seconds before next step: ...",
                    //    seconds * 1000
                    //).ConfigureAwait(false);

                    await CreateAccount(
                        tumblrCreatorClient,
                        regInfo,
                        nonce
                    ).ConfigureAwait(false);

                    Interlocked.Increment(ref _stats.Created);
                    requeueEmailAccount = false;
                    confirmStatus = ConfirmStatus.Uncomfirmed;

                    var entity = new EmailBlacklistEntity(
                        regInfo.Email
                    );
                    await _db.EmailBlacklist.InsertAsync(
                        entity
                    ).ConfigureAwait(false);

                    var oAuthSession = await TumblrOAuthSessionCreator.CreateOAuthSession(
                        regInfo.Email,
                        regInfo.Password,
                        regInfo.SessionInfo,
                        client
                    ).ConfigureAwait(false);

                    tumblrAccount = new TumblrAccount(
                        regInfo.Username,
                        regInfo.Email,
                        regInfo.Password,
                        emailLoginInfo.Password,
                        $"{regInfo.Username}.tumblr.com",
                        regInfo.SessionInfo,
                        oAuthSession
                    );

                    var tumblrClient = new TumblrClient(
                        tumblrAccount,
                        client
                    );

                    await RetrieveConfig(tumblrClient)
                        .ConfigureAwait(false);

                    await RetrieveNotices(tumblrClient)
                        .ConfigureAwait(false);

                    await RetrieveUserInfo(tumblrClient)
                        .ConfigureAwait(false);

                    await SelectTopics(tumblrClient)
                        .ConfigureAwait(false);

                    await RegisterDevice(tumblrClient)
                        .ConfigureAwait(false);

                    await RetrieveDashboard(tumblrClient)
                        .ConfigureAwait(false);

                    await RetrieveUnreadMessagesCount(tumblrClient)
                        .ConfigureAwait(false);

                    await RetrieveTags(tumblrClient)
                        .ConfigureAwait(false);

                    //seconds = ThreadSafeStaticRandom.RandomInt(10, 16);
                    //await UpdateThreadStatusAsync(
                    //    $"Delaying {seconds} seconds before next step: ...",
                    //    seconds * 1000
                    //).ConfigureAwait(false);

                    await UploadAvatar(tumblrClient)
                        .ConfigureAwait(false);

                    await Confirm(
                        emailProxy,
                        emailLoginInfo,
                        client
                    ).ConfigureAwait(false);

                    Interlocked.Increment(ref _stats.Confirmed);
                    confirmStatus = ConfirmStatus.Confirmed;

                    await UpdateThreadStatusAsync(
                        "Account created and confirmed",
                        2000
                    ).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    await HandleException(
                        e,
                        emailLoginInfo
                    ).ConfigureAwait(false);
                }
                finally
                {
                    await Finally(
                        confirmStatus,
                        tumblrAccount,
                        requeueEmailAccount,
                        emailLoginInfo
                    ).ConfigureAwait(false);
                }
            }

            UI.Account = string.Empty;
            UI.Status = string.Empty;
        }

        private async Task Finally(
            ConfirmStatus confirmStatus,
            TumblrAccount tumblrAccount,
            bool requeueEmailAccount,
            EmailAccountLoginInfo imapLoginInfo)
        {
            if (confirmStatus != ConfirmStatus.None &&
                tumblrAccount != null)
            {
                await WriteAccountToFile(
                    tumblrAccount,
                    confirmStatus
                ).ConfigureAwait(false);
            }

            if (requeueEmailAccount)
            {
                await MaybeEnqueueEmailAccountAfterError(imapLoginInfo)
                    .ConfigureAwait(false);
            }
        }

        private async Task HandleException(
            Exception e,
            EmailAccountLoginInfo emailLoginInfo)
        {
            switch (e)
            {
                case InvalidImapCredentialsException _:
                    await WriteInvalidImapAccount(emailLoginInfo)
                        .ConfigureAwait(false);
                    await HandleEmailAlreadyRegisteredOrInvalidImapCredsEx(
                        emailLoginInfo.LoginId,
                        e
                    ).ConfigureAwait(false);
                    break;

                case EmailAlreadyRegisteredException _:
                    await HandleEmailAlreadyRegisteredOrInvalidImapCredsEx(
                        emailLoginInfo.LoginId,
                        e
                    ).ConfigureAwait(false);
                    break;

                default:
                    await LogExceptionAndUpdateUI(e)
                        .ConfigureAwait(false);
                    break;
            }
        }

        private async Task WriteAccountToFile(TumblrAccount account, ConfirmStatus step)
        {
            string fileName;
            switch (step)
            {
                case ConfirmStatus.None:
                    return;

                default:
                    fileName = "tumblr-accts_created-unconfirmed.txt";
                    break;

                case ConfirmStatus.Confirmed:
                    fileName = "tumblr-accts_created-confirmed.txt";
                    break;
            }

            await _writeLock.WaitAsync()
                .ConfigureAwait(false);
            try
            {
                using (var fileStream = new FileStream(fileName, FileMode.Append))
                {
                    using (var streamWriter = new StreamWriter(fileStream))
                    {
                        await streamWriter.WriteLineAsync(account.ToString())
                            .ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private async Task WriteInvalidImapAccount(EmailAccountLoginInfo loginInfo)
        {
            await WriteLock.WaitAsync()
                .ConfigureAwait(false);

            try
            {
                using (var streamWriter = new StreamWriter("invalid_imap_credentials.txt", true))
                {
                    streamWriter.WriteLine(loginInfo.ToString());
                }
            }
            finally
            {
                WriteLock.Release();
            }
        }

        private async Task HandleEmailAlreadyRegisteredOrInvalidImapCredsEx(
            string loginId,
            Exception e)
        {
            var entity = new EmailBlacklistEntity(loginId);
            await _db.EmailBlacklist.InsertAsync(entity)
                .ConfigureAwait(false);
            await LogExceptionAndUpdateUI(e)
                .ConfigureAwait(false);
        }

        private async Task CheckEmailAccountCredentials(
            EmailAccountLoginInfo loginInfo,
            WebProxy proxy,
            HttpWaifu client)
        {
            UI.Status = "Checking email credentials via imap: ...";

            switch (loginInfo)
            {
                case ImapLoginInfo imapLoginInfo:
                    if (!await ImapClient.TryVerifyCredentials(
                        proxy,
                        imapLoginInfo.ImapServerAddress,
                        imapLoginInfo.ImapServerPort,
                        imapLoginInfo.LoginId,
                        imapLoginInfo.Password,
                        imapLoginInfo.ImapServerRequiresSsl
                    ).ConfigureAwait(false))
                    {
                        throw new InvalidImapCredentialsException(
                            $"Imap server returned invalid credentials for {imapLoginInfo.LoginId}"
                        );
                    }
                    break;

                case RediffWebLoginInfo rediff:
                    foreach (var cookie in rediff.Cookies)
                        client.Cookies.Add(cookie);

                    if (!await RediffmailClient.CheckSession(client, proxy, rediff))
                    {
                        throw new InvalidImapCredentialsException(
                            "invalid rediff session."
                        );
                    }
                    break;

                default:
                    throw new InvalidOperationException("invalid email type.");
            }
        }

        private async Task SendPreonboardingRequest(
            TumblrCreatorClient tumblrCreatorClient)
        {
            UI.Status = "Sending tumblr preonboarding request: ...";
            await tumblrCreatorClient.RetrievePreOnboarding()
                .ConfigureAwait(false);
        }

        private async Task<string> RetrieveRegistrationNonce(
            TumblrCreatorClient tumblrCreatorClient)
        {
            UI.Status = "Retrieving registration nonce: ...";
            var responseContainer = await tumblrCreatorClient.RetrieveRegistrationNonce()
                .ConfigureAwait(false);
            if (responseContainer == null ||
                responseContainer.Response == null ||
                string.IsNullOrWhiteSpace(responseContainer.Response.Key))
            {
                throw new InvalidOperationException("Failed to parse nonce.");
            }

            return responseContainer.Response.Key;
        }

        private async Task CheckIfEmailAndUsernameIsAvailable(
            TumblrCreatorClient tumblrCreatorClient,
            AcctRegisterInfo regInfo)
        {
            UI.Status = "Checking if email and username is available: ...";

            for (var i = 0; i < 5; i++)
            {
                var responseContainer = await tumblrCreatorClient.RetrieveEmailAndLoginIdValidationResult(
                    regInfo.Email,
                    regInfo.Username
                ).ConfigureAwait(false);
                if (responseContainer.Meta == null ||
                    responseContainer.Response == null)
                {
                    continue;
                }

                switch (responseContainer.Meta.Status)
                {
                    case 200:
                        return;

                    case 400:
                        HandleValidationError(responseContainer, regInfo);
                        break;

                    default:
                        throw new InvalidOperationException(
                            "Unexpected status code returned from validate email and username response."
                        );
                }

                //var seconds = ThreadSafeStaticRandom.RandomInt(3, 9);
                //await UpdateThreadStatusAsync(
                //    $"Delaying {seconds} seconds before next username/email verify attempt: ...",
                //    seconds * 1000
                //).ConfigureAwait(false);
            }

            throw new InvalidOperationException("Failed to find a unique username.");
        }

        private void HandleValidationError(
            RetrieveEmailAndLoginIdValidationTumblrApiResponse responseContainer,
            AcctRegisterInfo regInfo)
        {
            if (responseContainer.Response.UserErrors != null &&
                responseContainer.Response.UserErrors.Count > 0)
            {
                foreach (var err in responseContainer.Response.UserErrors)
                {
                    if (err == null)
                        continue;

                    if (err.Code == 2)
                    {
                        throw new EmailAlreadyRegisteredException(
                            "Email address already registered."
                        );
                    }
                }
            }

            if (responseContainer.Response.TumblelogErrors != null &&
                responseContainer.Response.TumblelogErrors.Count > 0)
            {
                foreach (var err in responseContainer.Response.TumblelogErrors)
                {
                    if (err == null)
                        continue;

                    if (err.Code != 3)
                        continue;
                    regInfo.Username += ThreadSafeStaticRandom.RandomInt(9);
                    break;
                }
            }
        }

        private async Task CreateAccount(
            TumblrCreatorClient tumblrCreatorClient,
            AcctRegisterInfo registerInfo,
            string nonce)
        {
            UI.Status = "Attempting to create tumblr account: ...";

            var responseContainer = await tumblrCreatorClient.CreateAccount(
                registerInfo.Email,
                registerInfo.Username,
                registerInfo.Password,
                registerInfo.Age,
                nonce
            ).ConfigureAwait(false);
            if (responseContainer.Response == null ||
                string.IsNullOrWhiteSpace(responseContainer.Response.Message) ||
                !responseContainer.Response.Message.Contains("Created"))
            {
                throw new InvalidOperationException("Creation attempt failed.");
            }
        }

        private async Task RetrieveConfig(TumblrClient client)
        {
            UI.Status = "Retrieving config: ...";
            await client.RetrieveConfig()
                .ConfigureAwait(false);
        }

        private async Task RetrieveNotices(TumblrClient client)
        {
            UI.Status = "Retrieving notices: ...";
            await client.RetrieveNotices()
                .ConfigureAwait(false);
        }

        private async Task RetrieveUserInfo(TumblrClient client)
        {
            UI.Status = "Retrieving user info: ...";
            await client.RetrieveUserInfo()
                .ConfigureAwait(false);
        }

        private async Task<RetrieveTopicsTumblrApiResponse> RetrieveTopics(
            TumblrClient client)
        {
            UI.Status = "Retrieving topics: ...";
            var responseContainer = await client.RetrieveTopics()
                .ConfigureAwait(false);
            if (responseContainer.Response == null ||
                responseContainer.Response.Topics == null ||
                responseContainer.Response.Topics.Count == 0)
            {
                throw new InvalidOperationException("Tumblr returned empty topics list.");
            }

            return responseContainer;
        }

        private async Task SelectTopics(
            TumblrClient client)
        {
            var topicsResponseContainer = await RetrieveTopics(client)
                .ConfigureAwait(false);

            var queue = new Queue<TumblrTopic>(
                topicsResponseContainer.Response.Topics
            );
            queue.Shuffle();

            UI.Status = "Selecting topics: ...";

            var maxTopics = ThreadSafeStaticRandom.RandomInt(3, 21);
            var cnt = 0;
            var selectedTopics = new List<string>();
            while (queue.Count > 0 && cnt < maxTopics)
            {
                var topic = queue.Dequeue();
                if (string.IsNullOrWhiteSpace(topic.Tag) || topic.Tag.Contains("&"))
                    continue;

                selectedTopics.Add(topic.Tag);
                await client.UpdateInterests(topic.Tag)
                    .ConfigureAwait(false);

                //var seconds = ThreadSafeStaticRandom.RandomInt(3, 8);
                //await UpdateThreadStatusAsync(
                //    $"Delaying {seconds} seconds before selecting next topic: ...",
                //    seconds * 1000
                //).ConfigureAwait(false);
                cnt++;
            }

            await client.CommitUpdatedInterests(
                selectedTopics,
                topicsResponseContainer.Response.Topics
            ).ConfigureAwait(false);
        }

        private async Task RegisterDevice(TumblrClient client)
        {
            UI.Status = "Registering device: ...";

            var pushToken = AndroidHelpers.RandomGcmToken();
            await client.CreateDeviceEntity(pushToken)
                .ConfigureAwait(false);
        }

        private async Task RetrieveDashboard(TumblrClient client)
        {
            UI.Status = "Retrieving dashboard: ...";

            await client.RetrieveDashboard()
                .ConfigureAwait(false);
        }

        private async Task RetrieveUnreadMessagesCount(TumblrClient client)
        {
            UI.Status = "Retrieving unread messages count: ...";

            await client.RetrieveUnreadMessagesCount()
                .ConfigureAwait(false);
        }

        private async Task RetrieveTags(TumblrClient client)
        {
            UI.Status = "Retrieving tags: ...";

            await client.RetrieveTags()
                .ConfigureAwait(false);
        }

        private async Task UploadAvatar(TumblrClient client)
        {
            UI.Status = "Uploading avatar: ...";

            var imageData = SelectRandomAvatarImage();
            if (imageData == null)
                return;

            await client.UpdateAvatar(imageData)
                .ConfigureAwait(false);
        }

        private async Task Confirm(
            WebProxy emailProxy,
            EmailAccountLoginInfo loginInfo,
            HttpWaifu client)
        {
            UI.Status = "Waiting for confirm email: ...";

            var timeoutSetting = Settings.Get<int>(Constants.ConfirmTimeout);
            var timeout = TimeSpan.FromSeconds(timeoutSetting);

            async Task<string> GetEmailBody()
            {
                switch (loginInfo)
                {
                    case ImapLoginInfo imapLoginInfo:
                        var emailBodies = await ImapClient.RetrieveMessagesWithPartialSubject(
                            emailProxy,
                            imapLoginInfo.ImapServerAddress,
                            imapLoginInfo.ImapServerPort,
                            imapLoginInfo.LoginId,
                            imapLoginInfo.Password,
                            "Verify your email address",
                            timeout,
                            imapLoginInfo.ImapServerRequiresSsl
                        ).ConfigureAwait(false);
                        return emailBodies[0];

                    case RediffWebLoginInfo rediffLoginInfo:
                        var bodz = await RediffmailClient.RetrieveMessagesWithPartialSubject(
                            client,
                            emailProxy,
                            rediffLoginInfo,
                            "Verify your email address",
                            timeout
                        ).ConfigureAwait(false);
                        return bodz[0];

                    default:
                        throw new InvalidOperationException("invalid email type.");
                }
            }

            var emailBody = await GetEmailBody()
                .ConfigureAwait(false);

            await HandleConfirmationEmail(
               emailBody,
                client
            ).ConfigureAwait(false);
        }

        private static string ParseLink(string emailBody)
        {
            var link = string.Empty;
            var match = ConfirmLinkRegex.Match(emailBody);
            if (match.Success)
                link = match.Groups[1].Value;

            if (string.IsNullOrWhiteSpace(link))
            {
                throw new InvalidOperationException(
                    "Failed to parse confirm link from email body."
                );
            }

            //\/\/www.rediffmail.com\/cgi-bin\/red.cgi?red=https%3A%2F%2Fmx%2Etumblr%2Ecom%2Fwf%2Fclick%3Fupn%3Drkn%2D2Bn4Ut%2D2BmJG%2D2BjXrxx%2D2FWfVi4zke4arttQnyp9dLQ4HXNLGDHc8xL%2D2FUZNxxcr6MMuUJz4%2D2FAarpNQoVx%2D2BffLRon4JIU7JfgzK6vjT%2D2BDngx9LG1fpPIL1cj7YL2cCJcpn54%2D2B0v2ceiEd1VIWVD%2D2BxUOtzQ%2D3D%2D3D%5FBZ2Wz87tp6IFsQBApkwuqiVvRVVbv4aUVuL2VVhnak%2D2Fpwg75TRbnw%2D2FFViyaQAbvTm6ICly8rq3OtAgatWBJHIR6Qlfq1THaJ25Hw3uVW8pVeR8IkE6%2D2FMQ1WbpgKhUbLtzXH3UlMGB8UEX6hwHwvdMB7wZydgPlJpcceX7qH0df%2D2BNemmU6%2D2F0B4XVk1XpPTuvmTCYp7ZcRpgEJvB0rnMyTlZnsB%2D2FmBAsVvqD%2D2FL3HjLz9ngyUk4QFf3%2D2Bku8Qv1SLYoY00uclisJDfWfXKPhbuBMzMTmQJvCwnV0usj2LFL22s8%2D3D&isImage=0&BlockImage=0&rediffng=0&rogue=6c151b0f49b67a0b642268a2a90154155864c518&rdf=UGhTMAVrBG1TY1NiAzpTZlFhXitbdVY2UmYMeAM7VTM=
            link = link
                .Replace(@"\/", "/");
            if (!link.StartsWith("https"))
                link = $"https:{link}";

            link = HttpUtility.HtmlDecode(link);

            if (link.Contains("red="))
            {
                var match2 = RedRegex.Match(link);
                if (match2.Success)
                {
                    link = match2.Groups[1].Value;
                    link = HttpUtility.UrlDecode(link);
                }
                else
                    throw new InvalidOperationException("failed to parse link from rediff masking link.");
            }
            return link;
        }

        private async Task HandleConfirmationEmail(
            string emailBody,
            HttpWaifu client)
        {
            UI.Status = "Parsing link: ...";

            var link = ParseLink(emailBody);

            UI.Status = "Attempting confirmation with link: ...";

            var userAgent = CommonDesktopUserAgents.RandomSelection();

            UI.Status = "Retrieving confirm link contents: ...";

            var response = await RetrieveConfirmLink(
                link,
                userAgent,
                client
            ).ConfigureAwait(false);

            if (response.ContentBody.Contains("re a real user"))
                return;

            const string expectedKeyword = "verify_email_form";
            if (!response.ContentBody.Contains(expectedKeyword))
            {
                throw HttpHelpers.CreateHttpResponseDidNotHaveExpectedKeywordEx(
                    HttpMethod.GET,
                    link,
                    "verify_email_form"
                );
            }

            UI.Status = "Parsing confirm form: ...";

            var formActionLink = ParseConfirmFormActionLink(response.ContentBody);
            formActionLink = $"{response.Uri.Scheme}://{response.Uri.Host}{formActionLink}";
            var formKey = ParseConfirmFormKey(response.ContentBody);

            var apiKey = Settings.Get<string>(Constants.TwoCaptchaApiKey);
            var twoCap = new TwoCaptchaSolver(apiKey);

            const string dataSiteKey = "6LcaLgATAAAAAMfyoZjBXW33zuRRJq9pFvsE9HJJ";

            var seconds = Settings.Get<int>(Constants.TwoCaptchaSolveTimeout);
            var timeout = TimeSpan.FromSeconds(seconds);

            UI.Status = "Waiting for recaptcha solution: ...";

            var solution = await twoCap.SolveREEEEEcaptchaAsync(
                dataSiteKey,
                response.Uri.AbsoluteUri,
                timeout
            );

            await ConfirmWithCaptchaSolution(
                formActionLink,
                solution,
                formKey,
                userAgent,
                response.Uri.AbsoluteUri,
                client
            ).ConfigureAwait(false);
        }

        private async Task ConfirmWithCaptchaSolution(
            string formActionLink,
            string captchaSln,
            string formKey,
            string userAgent,
            string referer,
            HttpWaifu client)
        {
            UI.Status = "Confirming with captcha sln: ...";

            var postParams = new HttpParameters
            {
                ["g-recaptcha-response"] = captchaSln,
                ["form_key"] = formKey
            };

            const HttpMethod httpMethod = HttpMethod.POST;
            var request = new HttpRequest(httpMethod, formActionLink)
            {
                Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8",
                AcceptEncoding = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                OverrideUserAgent = userAgent,
                Origin = "https://www.tumblr.com",
                Referer = referer,
                OverrideHeaders = new WebHeaderCollection
                {
                    ["Accept-Language"] = "en-US,en;q=0.8"
                },
                ContentType = "application/x-www-form-urlencoded",
                ContentBody = postParams.ToUrlEncodedString()
            };

            var response = await client.SendRequestAsync(request)
                .ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw HttpHelpers.CreateHttpRequestFailedEx(
                    httpMethod,
                    formActionLink,
                    response.StatusCode,
                    response.StatusDescription,
                    client.Config.Proxy
                );
            }

            const string expectedKeywords = "re a real user";
            if (!response.ContentBody.Contains(expectedKeywords))
            {
                throw HttpHelpers.CreateHttpResponseDidNotHaveExpectedKeywordEx(
                    httpMethod,
                    formActionLink,
                    expectedKeywords
                );
            }
        }

        private async Task<HttpResponse> RetrieveConfirmLink(
            string link,
            string userAgent,
            HttpWaifu client)
        {
            const HttpMethod httpMethod = HttpMethod.GET;
            var request = new HttpRequest(httpMethod, link)
            {
                Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8",
                AcceptEncoding = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                OverrideUserAgent = userAgent,
                OverrideHeaders = new WebHeaderCollection
                {
                    ["Accept-Language"] = "en-US,en;q=0.8"
                }
            };

            var response = await client.SendRequestAsync(request)
                .ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw HttpHelpers.CreateHttpRequestFailedEx(
                    httpMethod,
                    link,
                    response.StatusCode,
                    response.StatusDescription,
                    client.Config.Proxy
                );
            }

            return response;
        }

        private static string ParseConfirmFormKey(string html)
        {
            var match = FormKeyRegex.Match(html);

            var formKey = string.Empty;
            if (match.Success)
                formKey = match.Groups[1].Value;

            if (string.IsNullOrWhiteSpace(formKey))
            {
                throw new InvalidOperationException(
                    "Failed to parse form_key."
                );
            }

            return formKey;
        }

        private static string ParseConfirmFormActionLink(string html)
        {
            var match = VerifyEmailFormActionRegex.Match(html);

            var formKey = string.Empty;
            if (match.Success)
                formKey = match.Groups[1].Value;

            if (string.IsNullOrWhiteSpace(formKey))
            {
                throw new InvalidOperationException(
                    "Failed to parse form_key."
                );
            }

            return formKey;
        }

        private async Task<AcctRegisterInfo> CreateAccountRegisterInfo(
            string email)
        {
            var word1 = _collections.Words1.GetNext();
            var word2 = _collections.Words2.GetNext();
            var username = $"{word1}{word2}".ToLower();

            var password = StringHelpers.RandomStringUniqueChars(
                ThreadSafeStaticRandom.RandomInt(8, 14),
                StringDefinition.DigitsAndLowerLetters
            );

            var age = ThreadSafeStaticRandom.RandomInt(19, 30);
            var xId = Guid.NewGuid().ToString();
            var xIdDate = DateTimeHelpers.CurrentTimeMillis().ToString();

            var xsId = Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()));
            var bCookie = await Task.Run(
                () => BCookieFactory.GetBCookie(xsId)
            ).ConfigureAwait(false);

            var carrierInfo = CellyHelpers.RandomCellyCarrierInfo();
            var device = AndroidDevices.Devices.GetNext();

            var tumblrSessionInfo = new TumblrSessionInfo(
               xId,
               xIdDate,
               xsId,
               bCookie,
               device,
               carrierInfo
            );

            var ret = new AcctRegisterInfo(
                username,
                email,
                password,
                age,
                tumblrSessionInfo
            );
            return ret;
        }

        private async Task MaybeEnqueueEmailAccountAfterError(
            EmailAccountLoginInfo emailLoginInfo)
        {
            if (!EmailAccountLoginErrors.TryGetValue(
                emailLoginInfo.LoginId,
                out var loginErrors))
            {
                return;
            }

            if (loginErrors++ >= 3)
                return;

            if (await _db.EmailBlacklist.ContainsLoginIdAsync(
                emailLoginInfo.LoginId
            ).ConfigureAwait(false))
            {
                return;
            }

            EmailAccountLoginErrors[
                emailLoginInfo.LoginId
            ] = loginErrors;

            _collections.EmailAccounts.Enqueue(
                emailLoginInfo
            );

            await UpdateThreadStatusAsync(
                $"Attempt failed for {emailLoginInfo.LoginId} ({loginErrors}).  Will retry.",
                5000
            ).ConfigureAwait(false);
        }

        private byte[] SelectRandomAvatarImage()
        {
            for (var i = 0; i < 5; i++)
            {
                var dir = _collections.ImageDirs.GetNext();
                if (string.IsNullOrWhiteSpace(dir) ||
                    !Directory.Exists(dir))
                {
                    continue;
                }

                var jpgFiles = Directory.GetFiles(dir, "*.jpg");
                if (jpgFiles.Length == 0)
                    continue;

                var lst = new List<string>(jpgFiles);
                var pathToImageFile = lst.RandomSelection();

                return RandomizeImage(pathToImageFile);
            }

            return null;
        }

        /// <summary>
        /// Randomize an image
        /// </summary>
        /// <param name="originalFile"></param>
        /// <returns></returns>
        private static byte[] RandomizeImage(string originalFile)
        {
            try
            {
                if (!File.Exists(originalFile))
                    return new byte[0];

                using (var ms = new MemoryStream())
                {
                    using (var fullsizeImage = Image.FromFile(originalFile))
                    {
                        var maxHeight = fullsizeImage.Height;
                        var oldWidth = fullsizeImage.Width;
                        var newWidth = oldWidth * 3;

                        fullsizeImage.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        fullsizeImage.RotateFlip(RotateFlipType.Rotate180FlipNone);

                        var newHeight = fullsizeImage.Height * newWidth / fullsizeImage.Width;
                        if (newHeight > maxHeight)
                        {
                            newWidth = fullsizeImage.Width * maxHeight / fullsizeImage.Height;
                            newHeight = maxHeight;
                        }

                        using (var newImage = fullsizeImage.GetThumbnailImage(newWidth, newHeight, null, IntPtr.Zero))
                        {
                            var encoderParams = new EncoderParameters(1)
                            {
                                Param =
                                {
                                    [0] = new EncoderParameter(
                                        Encoder.Quality,
                                        ThreadSafeStaticRandom.RandomInt(90, 100)
                                    )
                                }
                            };

                            ImageCodecInfo GetEncoderInfo(string mimeType)
                            {
                                var codecs = ImageCodecInfo.GetImageEncoders();
                                return codecs.FirstOrDefault(t => t.MimeType == mimeType);
                            }

                            newImage.Save(ms, GetEncoderInfo("image/jpeg"), encoderParams);
                        }

                        return ms.ToArray();
                    }
                }
            }
            catch
            {
                return null;
            }
        }
    }
}