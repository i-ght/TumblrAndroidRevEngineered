using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Tumblr.Waifu.Exceptions;
using Tumblr.Waifu.JsonObjects;
using Waifu.Collections;
using Waifu.Net.Http;
using Waifu.Sys;

namespace Tumblr.Waifu
{
    public class TumblrClient
    {
        private readonly TumblrAccount _account;
        private readonly HttpWaifu _client;

        public TumblrClient(
            TumblrAccount account,
            HttpWaifu client)
        {
            _account = account ?? throw new ArgumentNullException(nameof(account));
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public HttpWaifuConfig HttpConfig => _client.Config;

        public async Task RetrieveConfig()
        {
            const HttpMethod httpMethod = HttpMethod.GET;
            const string url = "https://api.tumblr.com/v2/config";

            var queryParams = new HttpParameters
            {
                ["force_oauth"] = "false",
                ["sync"] = "true"
            };

            var request = CreateTumblrApiRequest(
                httpMethod,
                url,
                queryParams
            );

            await RetrieveApiResponse(request)
                .ConfigureAwait(false);
        }

        public async Task<RetrieveTopicsTumblrApiResponse> RetrieveTopics()
        {
            const HttpMethod httpMethod = HttpMethod.GET;
            const string url = "https://api.tumblr.com/v2/topics";

            var queryParams = new HttpParameters
            {
                ["version"] = "robbie"
            };

            var request = CreateTumblrApiRequest(
                httpMethod,
                url,
                queryParams
            );
            var response = await RetrieveApiResponse(request)
                .ConfigureAwait(false);

            var json = await Task.Run(
                    () => JsonConvert.DeserializeObject<RetrieveTopicsTumblrApiResponse>(response.ContentBody)
            ).ConfigureAwait(false);
            return json;
        }

        public async Task<RetrieveSearchResultsTumblrApiResponse> RetrieveSearchResults(
            string keyword, string cursor = "")
        {
            const HttpMethod httpMethod = HttpMethod.GET;
            const string url = "https://api.tumblr.com/v2/timeline/search";

            var queryParams = new HttpParameters
            {
                ["query"] = keyword,
                ["timeline_type"] = "tumblrs",
                ["block_nsfw"] = "true"
            };

            if (!string.IsNullOrEmpty(cursor))
                queryParams.Add("cursor", cursor);

            var request = CreateTumblrApiRequest(
                httpMethod,
                url,
                queryParams
            );

            var response = await RetrieveApiResponse(request)
                .ConfigureAwait(false);

            var json = await Task.Run(
                () => JsonConvert.DeserializeObject<RetrieveSearchResultsTumblrApiResponse>(
                    response.ContentBody
                )
            ).ConfigureAwait(false);
            return json;
        }

        public async Task<RetrieveNoticesTumblrApiResponse> RetrieveNotices()
        {
            const HttpMethod httpMethod = HttpMethod.GET;
            const string url = "https://api.tumblr.com/v2/user/notices";

            var queryParams = new HttpParameters
            {
                ["force_oauth"] = "false"
            };

            var request = CreateTumblrApiRequest(
                httpMethod,
                url,
                queryParams
            );

            var response = await RetrieveApiResponse(request)
                .ConfigureAwait(false);

            var json = await Task.Run(
                () => JsonConvert.DeserializeObject<RetrieveNoticesTumblrApiResponse>(response.ContentBody)
            ).ConfigureAwait(false);
            return json;
        }

        public async Task<RetrieveUserInfoTumblrApiResponse> RetrieveUserInfo()
        {
            const HttpMethod httpMethod = HttpMethod.GET;
            const string url = "https://api.tumblr.com/v2/user/info";

            var queryParams = new HttpParameters
            {
                ["force_oauth"] = "false",
                ["private_blogs"] = "true"
            };

            var request = CreateTumblrApiRequest(
                httpMethod,
                url,
                queryParams
            );

            var response = await RetrieveApiResponse(request)
                .ConfigureAwait(false);

            var json = await Task.Run(
                () => JsonConvert.DeserializeObject<RetrieveUserInfoTumblrApiResponse>(response.ContentBody)
            ).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(_account.Uuid))
            {
                _account.Uuid = json.Response.User.CrmUuid;
                if (string.IsNullOrWhiteSpace(_account.Uuid))
                {
                    throw new InvalidOperationException(
                        "Failed to parse tumblr account's uuid."
                    );
                }
            }

            return json;
        }

        public async Task CreateDeviceEntity(string pushToken)
        {
            const HttpMethod httpMethod = HttpMethod.POST;
            const string url = "https://api.tumblr.com/v2/device/register";

            var postParams = new HttpParameters
            {
                ["uuid"] = pushToken
            };

            var request = CreateTumblrApiRequest(
                httpMethod,
                url,
                postParams: postParams
            );

            await RetrieveApiResponse(request, HttpStatusCode.Created)
                .ConfigureAwait(false);
        }

        public async Task UpdateInterests(string topic)
        {
            const HttpMethod httpMethod = HttpMethod.POST;
            const string url = "https://api.tumblr.com/v2/user/tags/add";

            var postParams = new HttpParameters
            {
                ["tag"] = topic
            };

            var request = CreateTumblrApiRequest(
                httpMethod,
                url,
                postParams: postParams
            );

            await RetrieveApiResponse(request)
                .ConfigureAwait(false);
        }

        public async Task CommitUpdatedInterests(
            IList<string> selectedTopicIds,
            IList<TumblrTopic> seenTopicsCollection)
        {
            const HttpMethod httpMethod = HttpMethod.POST;
            const string url = "https://api.tumblr.com/v2/topics/submit";

            var queryParams = new HttpParameters
            {
                ["version"] = "robbie"
            };

            var lst = new List<string>();
            for (var i = 0; i < seenTopicsCollection.Count; i++)
            {
                var item = seenTopicsCollection[i];
                var name = item.Name;
                lst.Add($"\"{name}\":{i}");
            }

            lst.Shuffle();

            var topics = await Task.Run(
                () => JsonConvert.SerializeObject(selectedTopicIds, Formatting.None)
            ).ConfigureAwait(false);
            var seenTopics = $"{{{string.Join(",", lst)}}}";

            var postParams = new HttpParameters
            {
                ["topics"] = topics,
                ["seen_topics"] = seenTopics,
                ["bucket"] = "robbie"
            };

            var request = CreateTumblrApiRequest(
                httpMethod,
                url,
                queryParams,
                postParams
            );

            await RetrieveApiResponse(request)
                .ConfigureAwait(false);
        }

        public async Task<dynamic> RetrieveDashboard()
        {
            const HttpMethod httpMethod = HttpMethod.GET;
            const string url = "https://api.tumblr.com/v2/timeline/dashboard";

            var queryParams = new HttpParameters
            {
                ["reblog_info"] = "true",
                ["filter"] = "clean",
                ["user_action"] = "false",
                ["algodash"] = "false"
            };

            var request = CreateTumblrApiRequest(
                httpMethod,
                url,
                queryParams
            );

            var response = await RetrieveApiResponse(request)
                .ConfigureAwait(false);

            dynamic json = await Task.Run(
                () => JsonConvert.DeserializeObject(response.ContentBody)
            ).ConfigureAwait(false);
            return json;
        }

        public async Task UpdateAvatar(byte[] imageData)
        {
            const HttpMethod httpMethod = HttpMethod.POST;
            var url = $"https://api.tumblr.com/v2/blog/{_account.BlogUrl}/avatar";

            var data = Convert.ToBase64String(imageData);
            var oAuthParams = new Dictionary<string, string>
            {
                ["data"] = data
            };

            var boundary = Guid.NewGuid().ToString();
            var boundaryLine = $"--{boundary}\r\n";

            var sb = new StringBuilder();
            sb.Append(boundaryLine);
            sb.Append("Content-Disposition: form-data; name=\"data\"\r\n");
            sb.Append($"Content-Length: {data.Length}\r\n\r\n");

            sb.Append($"{data}\r\n");
            sb.Append(boundaryLine);
            sb.Append(
                $"Content-Disposition: form-data; name=\"data\"; filename=\"/storage/sdcard0/Tumblr/avatar_{DateTimeHelpers.UtcTimeStamp(13)}.jpg\"\r\n");
            sb.Append("Content-Type: image/jpeg\r\n");
            sb.Append($"Content-Length: {imageData.Length}\r\n\r\n");

            sb.Append("%IMAGE\r\n");
            sb.Append($"--{boundary}--\r\n");

            var contentData = HttpHelpers.CreateMultipartDataFromStringWithImageMacro(
                sb.ToString(),
                imageData
            );

            var authHeader = TumblrHelpers.CreateOAuthHeader(
                (int)httpMethod,
                url,
                oAuthParams,
                _account.OAuthSession.OAuthToken,
                _account.OAuthSession.OAuthTokenSecret
            );
            var apiHeaders = TumblrHelpers.ApiHeaders(
                _account.SessionInfo,
                authHeader
            );

            var request = new HttpRequest(httpMethod, url)
            {
                AcceptEncoding = DecompressionMethods.GZip,
                OverrideUserAgent = TumblrConstants.UserAgent,
                OverrideHeaders = apiHeaders,
                ContentType = $"multipart/form-data; boundary={boundary}",
                ContentData = contentData
            };

            await RetrieveApiResponse(request)
                .ConfigureAwait(false);
        }

        public async Task<RetrieveUnreadMessagesCountTumblrApiResponse> RetrieveUnreadMessagesCount()
        {
            const HttpMethod httpMethod = HttpMethod.GET;
            const string url = "https://api.tumblr.com/v2/conversations/unread_messages_count";

            var request = CreateTumblrApiRequest(
                httpMethod,
                url
            );

            var response = await RetrieveApiResponse(request)
                .ConfigureAwait(false);

            var json = await Task.Run(
                () => JsonConvert.DeserializeObject<RetrieveUnreadMessagesCountTumblrApiResponse>(
                    response.ContentBody
                )
            ).ConfigureAwait(false);

            return json;
        }

        public async Task<RetrieveTagsTumblrApiRepsonse> RetrieveTags()
        {
            const HttpMethod httpMethod = HttpMethod.GET;
            const string url = "https://api.tumblr.com/v2/user/tags";

            var queryParams = new HttpParameters
            {
                ["force_oauth"] = "false"
            };

            var request = CreateTumblrApiRequest(
                httpMethod,
                url,
                queryParams
            );

            var response = await RetrieveApiResponse(request)
                .ConfigureAwait(false);

            var json = await Task.Run(
                () => JsonConvert.DeserializeObject<RetrieveTagsTumblrApiRepsonse>(
                    response.ContentBody
                )
            ).ConfigureAwait(false);

            return json;
        }

        public async Task CreateMessage(
            string recipientUuid,
            string messageBody)
        {
            const HttpMethod httpMethod = HttpMethod.POST;
            const string url = "https://api.tumblr.com/v2/conversations/messages";

            if (string.IsNullOrWhiteSpace(_account.Uuid))
            {
                throw new InvalidOperationException(
                    $"{nameof(_account.Uuid)} must not be null or whitespace."
                );
            }

            var postParams = new HttpParameters
            {
                ["participant"] = _account.Uuid,
                ["participants[1]"] = _account.Uuid,
                ["participants[0]"] = recipientUuid,
                ["message"] = messageBody,
                ["type"] = "TEXT"
            };

            var request = CreateTumblrApiRequest(
                httpMethod,
                url,
                postParams: postParams
            );

            var response = await RetrieveApiResponse(request)
                .ConfigureAwait(false);
            if (!response.ContentBody.Contains("OK"))
            {
                throw HttpHelpers.CreateHttpResponseDidNotHaveExpectedKeywordEx(
                    httpMethod,
                    url,
                    "OK"
                );
            }
        }

        public async Task RetrieveConversation(
            string convoId,
            string withUuid)
        {
            const HttpMethod httpMethod = HttpMethod.GET;
            const string url = "https://api.tumblr.com/v2/conversations/messages";

            var queryParams = new HttpParameters
            {
                ["participant"] = _account.Uuid,
                ["conversation_id"] = convoId,
                ["participants[1]"] = withUuid,
                ["participants[0]"] = _account.Uuid
            };

            var request = CreateTumblrApiRequest(
                httpMethod,
                url,
                queryParams
            );

            await RetrieveApiResponse(request)
                .ConfigureAwait(false);
        }

        public async Task<
            RetrieveBlogInfoTumblrApiResponse
        > RetrieveBlogInfo(string username)
        {
            const HttpMethod httpMethod = HttpMethod.GET;
            var url = $"https://api.tumblr.com/v2/blog/{username}.tumblr.com/info";

            var queryParams = new HttpParameters
            {
                ["is_full_blog_info"] = "true",
                ["blog_name"] = username,
                ["prefetch[0]"] = ""
            };

            var request = CreateTumblrApiRequest(
                httpMethod,
                url,
                queryParams
            );

            var response = await RetrieveApiResponse(request)
                .ConfigureAwait(false);

            var json = await Task.Run(() =>
                JsonConvert.DeserializeObject<RetrieveBlogInfoTumblrApiResponse>(
                    response.ContentBody
                )
            );
            return json;
        }

        public async Task<RetrieveConversationsTumblrApiResponse> RetrieveConversations(
            string before = "", string after = "")
        {
            const HttpMethod httpMethod = HttpMethod.GET;
            const string url = "https://api.tumblr.com/v2/conversations";

            var queryParams = new HttpParameters
            {
                ["participant"] =_account.Uuid
            };

            if (!string.IsNullOrWhiteSpace(before))
            {
                queryParams.Add(
                    "before",
                    before
                );
            }

            if (!string.IsNullOrWhiteSpace(after))
            {
                queryParams.Add(
                    "after",
                    after
                );
            }

            var request = CreateTumblrApiRequest(
                httpMethod,
                url,
                queryParams
            );

            var response = await RetrieveApiResponse(request)
                .ConfigureAwait(false);

            var json = await Task.Run(() =>
                JsonConvert.DeserializeObject<RetrieveConversationsTumblrApiResponse>(
                    response.ContentBody
                )
            );
            return json;
        }

        private HttpRequest CreateTumblrApiRequest(
            HttpMethod httpMethod,
            string url,
            HttpParameters queryParams = null,
            HttpParameters postParams = null)
        {
            var oAuthParams = new List<KeyValuePair<string, string>>();

            if (queryParams != null && queryParams.Count > 0)
            {
                foreach (var kvp in queryParams)
                {
                    oAuthParams.Add(
                        new KeyValuePair<string, string>(kvp.Key, kvp.Value)
                    );
                }
            }

            if (postParams != null && postParams.Count > 0)
            {
                foreach (var kvp in postParams)
                {
                    oAuthParams.Add(
                        new KeyValuePair<string, string>(kvp.Key, kvp.Value)
                    );
                }
            }

            var authHeader = TumblrHelpers.CreateOAuthHeader(
                (int)httpMethod,
                url,
                oAuthParams,
                _account.OAuthSession.OAuthToken,
                _account.OAuthSession.OAuthTokenSecret
            );
            var apiHeaders = TumblrHelpers.ApiHeaders(
                _account.SessionInfo,
                authHeader
            );

            string requestUrl;
            if (queryParams != null && queryParams.Count > 0)
            {
                requestUrl = $"{url}?{queryParams.ToUrlEncodedString(false)}";
            }
            else
            {
                requestUrl = url;
            }

            var contentType = string.Empty;
            var contentBody = string.Empty;
            if (httpMethod == HttpMethod.POST && postParams != null)
            {
                contentType = "application/x-www-form-urlencoded";
                contentBody = postParams.ToUrlEncodedString(false);
            }

            var request = new HttpRequest(httpMethod, requestUrl)
            {
                AcceptEncoding = DecompressionMethods.GZip,
                OverrideUserAgent = TumblrConstants.UserAgent,
                OverrideHeaders = apiHeaders,
                ContentType = contentType,
                ContentBody = contentBody
            };

            return request;
        }

        private async Task<HttpResponse> RetrieveApiResponse(HttpRequest request)
        {
            return await RetrieveApiResponse(request, HttpStatusCode.OK)
                .ConfigureAwait(false);
        }

        private async Task<HttpResponse> RetrieveApiResponse(
            HttpRequest request,
            params HttpStatusCode[] acceptableResponseStatusCodes)
        {
            var response = await _client.SendRequestAsync(request)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var httpEx = HttpHelpers.CreateHttpRequestFailedEx(
                    request.HttpMethod,
                    request.Uri.AbsoluteUri,
                    response.StatusCode,
                    response.StatusDescription,
                    _client.Config.Proxy
                );

                throw new TumblrSessionNotAuthorizedException(
                    "Tumblr api server returned unauthorized.", 
                    httpEx
                );
            }

            var acceptable = acceptableResponseStatusCodes.Any(
                status => response.StatusCode == status
            );
            if (acceptable)
                return response;

            throw HttpHelpers.CreateHttpRequestFailedEx(
                request.HttpMethod,
                request.Uri.AbsoluteUri,
                response.StatusCode,
                response.StatusDescription,
                _client.Config.Proxy
            );
        }

    }
}
