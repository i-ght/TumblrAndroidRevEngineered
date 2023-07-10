using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Tumblr.Creator.JsonObjects;
using Tumblr.Waifu;
using Waifu.Net.Http;

namespace Tumblr.Creator
{
    internal class TumblrCreatorClient
    {
        private readonly TumblrSessionInfo _sessionInfo;
        private readonly HttpWaifu _client;

        public TumblrCreatorClient(
            TumblrSessionInfo acctRegisterInfo,
            HttpWaifu client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (acctRegisterInfo == null)
                throw new ArgumentNullException(nameof(acctRegisterInfo));

            _sessionInfo = acctRegisterInfo;
            _client = client;
        }

        public async Task RetrievePreOnboarding()
        {
            const HttpMethod httpMethod = HttpMethod.GET;
            const string url = "https://api.tumblr.com/v2/preonboarding";

            var queryParams = new HttpParameters
            {
                ["force_oauth"] = "false"
            };

            var request = CreateTumblrApiRequest(
                httpMethod,
                url,
                queryParams
            );

            //Don't need this response.
            await RetrieveApiResponse(request)
                .ConfigureAwait(false);
        }

        public async Task<RetrieveNonceTumblrApiResponse> RetrieveRegistrationNonce()
        {
            const HttpMethod httpMethod = HttpMethod.POST;
            const string url = "https://api.tumblr.com/v2/opieruofnl/asdkfboipewprhjon";

            var postParams = new HttpParameters
            {
                ["api_key"] = TumblrConstants.ConsumerSecret,
                ["key"] = TumblrConstants.RetrieveNonceKey
            };

            var request = CreateTumblrApiRequest(
                httpMethod,
                url,
                postParams: postParams
            );

            var response = await RetrieveApiResponse(request)
                .ConfigureAwait(false);

            var json = await Task.Run(
                () => JsonConvert.DeserializeObject<RetrieveNonceTumblrApiResponse>(
                    response.ContentBody
                )
            );
            return json;
        }

        public async Task<RetrieveEmailAndLoginIdValidationTumblrApiResponse> RetrieveEmailAndLoginIdValidationResult(string email, string loginId)
        {
            const HttpMethod httpMethod = HttpMethod.POST;
            const string url = "https://api.tumblr.com/v2/user/validate";

            var postParams = new HttpParameters
            {
                ["email"] = email,
                ["tumblelog"] = loginId
            };

            var request = CreateTumblrApiRequest(
                httpMethod,
                url,
                postParams: postParams
            );

            var response = await RetrieveApiResponse(
                request,
                HttpStatusCode.OK,
                HttpStatusCode.BadRequest
            );

            string contentBody;
            if (response.ContentBody.Contains("\"response\":[]"))
            {
                contentBody = response.ContentBody.Replace(
                    "\"response\":[]",
                    "\"response\":{}"
                );
            }
            else
            {
                contentBody = response.ContentBody;
            }

            var json = await Task.Run(
                () => JsonConvert.DeserializeObject<RetrieveEmailAndLoginIdValidationTumblrApiResponse>(
                    contentBody
                )
            );
            return json;
        }

        public async Task<CreateAccountTumblrApiResponse> CreateAccount(
            string email,
            string loginId,
            string password,
            int age,
            string nonce)
        {
            const HttpMethod httpMethod = HttpMethod.POST;
            const string url = "https://api.tumblr.com/v2/icwjeroair/nrksaaknsdzc";

            var signature = await Task.Run(
                () => Crypto.TumblrRegisterSignature(
                    loginId,
                    nonce,
                    password,
                    email
                )
            ).ConfigureAwait(false);

            var postParams = new HttpParameters
            {
                ["reblogg"] = TumblrConstants.Reblogg,
                ["nonce"] = nonce,
                ["password"] = password,
                ["email"] = email,
                ["tumblelog"] = loginId,
                ["signature"] = signature,
                ["age"] = age.ToString()
            };

            var request = CreateTumblrApiRequest(
                httpMethod,
                url,
                postParams: postParams
            );

            var response = await RetrieveApiResponse(request)
                .ConfigureAwait(false);

            var json = await Task.Run(
                () => JsonConvert.DeserializeObject<CreateAccountTumblrApiResponse>(
                    response.ContentBody
                )
            ).ConfigureAwait(false);
            return json;
        }

        private static WebHeaderCollection ApiHeaders(
            TumblrSessionInfo sessionInfo,
            string authHeader)
        {
            var ret = new WebHeaderCollection
            {
                ["X-Version"] = $"device/{TumblrConstants.AppVersion}/0/{sessionInfo.Device.OsVersion}/tumblr/",
                ["X-Identifier"] = sessionInfo.XIdentifier,
                ["X-Identifier-Date"] = sessionInfo.XIdentifierDate,
                ["Accept-Language"] = "en-US",
                ["Pragma"] = "no-cache",
                ["yx"] = sessionInfo.BCookie,
                ["X-YUser-Agent"] = sessionInfo.XyUserAgent,
                ["di"] = sessionInfo.DeviceInfo,
                ["X-S-ID-Enabled"] = "false",
                ["X-S-ID"] = sessionInfo.XsId,
                ["Authorization"] = authHeader
            };

            return ret;
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
                oAuthParams
            );
            var apiHeaders = ApiHeaders(_sessionInfo, authHeader);

            string requestUrl;
            if (queryParams != null && queryParams.Count > 0)
            {
                requestUrl = $"{url}?{queryParams.ToUrlEncodedString()}";
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
                contentBody = postParams.ToUrlEncodedString();
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
            return await RetrieveApiResponse(
                request,
                HttpStatusCode.OK
            ).ConfigureAwait(false);
        }

        private async Task<HttpResponse> RetrieveApiResponse(HttpRequest request, params HttpStatusCode[] acceptableResponseStatusCodes)
        {
            var response = await _client.SendRequestAsync(request)
                .ConfigureAwait(false);

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
