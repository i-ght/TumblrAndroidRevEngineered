using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using Waifu.OAuth;

namespace Tumblr.Waifu
{
    public static class TumblrHelpers
    {
        public static string CreateOAuthHeader(
            OAuthHttpMethod httpMethod,
            string url,
            ICollection<KeyValuePair<string, string>> parameters,
            string token = "",
            string tokenSecret = "")
        {

            if (httpMethod < 0 || (int)httpMethod > 4)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(httpMethod),
                    $"{nameof(httpMethod)} must have a value within its defined range."
                );
            }

            if (url == null)
                throw new ArgumentNullException(nameof(url));

            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            using (
                var oauth = new OAuthConsumer(
                    httpMethod,
                    url,
                    TumblrConstants.ConsumerKey,
                    TumblrConstants.ConsumerSecret,
                    new HMACSHA1(),
                    token,
                    tokenSecret,
                    parameters
                )
            )
            {
                var authHeader = oauth.CreateOAuthAuthorizationHeader();
                return authHeader;
            }
        }

        public static string CreateOAuthHeader(
            int httpMethod,
            string url,
            ICollection<KeyValuePair<string, string>> parameters,
            string token = "",
            string tokenSecret = "")
        {

            return CreateOAuthHeader(
                (OAuthHttpMethod)httpMethod,
                url,
                parameters,
                token,
                tokenSecret
            );
        }

        public static WebHeaderCollection ApiHeaders(
            TumblrSessionInfo sessionInfo,
            string authHeader)
        {
            if (sessionInfo == null)
                throw new ArgumentNullException(nameof(sessionInfo));

            if (authHeader == null)
                throw new ArgumentNullException(nameof(authHeader));

            const string features =
                "GRAYWATER_DRAFTS=true&GRAYWATER_QUEUE=true&GRAYWATER_INBOX=true&GRAYWATER_LIKES=true&GRAYWATER_BLOG_LIST=true&GRAYWATER_BLOG_SEARCH=true&GRAYWATER_BLOG_LIKES=true&GRAYWATER_BLOG_POSTS=true&GRAYWATER_DASHBOARD=true&GRAYWATER_TIMELINE_PREVIEW=true&GRAYWATER_SEARCH=true&GRAYWATER_EXPLORE=true&GRAYWATER_TAKEOVER=true&GRAYWATER_TRENDING_TOPIC=true&LOGAN_SQUARE_PARSER=true&LOGAN_SQUARE_PARSE_DASHBOARD=true&LOGAN_SQUARE_PARSE_DISK=true&LABS_ANDROID=true&SSL=true&TOUR_GUIDE=true&SABER=true&BLINKFEED_RATE_LIMIT=false&DOUBLETAP_TO_LIKE=true&CS_LOG_IMAGE_DATA=true&SABER_TICK_IMAGE_DATA=true&MEDIA_GALLERY_GIF_MAKER=true&MEDIA_GALLERY_GIF_MAKER_OVERLAY=true&APP_ATTRIBUTION=true&APP_ATTRIBUTION_CPI=false&VIDEO_PLAYER_MIGRATION=true&NEW_VIDEO_PLAYER_ADS=true&YOUTUBE_VIDEO_PLAYER=true&ALGO_STREAM_DASHBOARD=false&NSFW_SEARCH=true&LS_LOG=true&MOBILE_PERFORMANCE_LOGGING=false&NETWORK_CLASS=true&ALLOW_ONE_ID=false&MENTIONS_IN_REPLIES=true&MENTIONS_IN_REPLIES_BUTTON=true&ACTIVITY_ROLLUPS=true&ACTIVITY_PUSH_NOTIFICATION_STRIPE=true&NEW_SNOWMAN_UX=true&DYNAMIC_IMAGE_SIZES=true&SETTINGS_REDESIGN=true&SAFE_MODE=true&TAP_TO_REPLY=true&SAFE_MODE_OWN_POST=true&BLOCK_MANAGEMENT_MOBILE=true&BLOCK_FROM_NOTIFICATION=true&MOBILE_ANSWERTIME=true&NPF_ADVANCED_POST_OPTIONS=false&NPF_CANVAS=false&NPF_SINGLE_PAGE=false&NPF_PHOTOS=false&NPF_LINK_BLOCKS=false&SAVED_RECENT_SEARCHES=true&GIF_SEARCH_HISTORY=true&SHOW_WRAPPED_TAGS=true&MAKE_FAN_REQUESTS=true&STILL_IMAGE_EDITING=true&BOTTOM_NAV=true&IMAGE_EDITING_STICKERS=true&IMAGE_FILTERS=true&FIND_MY_FRIENDS=false&FIND_MY_FRIENDS_SETTINGS=true&LOGAN_SQUARE_PARSE_UNREAD=false&BUTTONIZE_BLOG_CARD_INLINE=true&BLOG_INFO_PARTIAL_RESPONSE=false&EXPERIMENTR_TEST=disabled&DEFAULT_STATES_FAST_EDIT_AVATAR_HEADER=true&GIF_DATA_SAVING_MODE_ADJUSTMENTS=true&GIF_AS_MP4_IN_LIGHTBOX=false&STATUS_INDICATORS=true&FAST_BLOG_SWITCHER=true&ALPHABETICAL_FOLLOWING_SEARCH=true&IMAGE_PLACEHOLDER_GRADIENTS=true&LOGAN_SQUARE_PHASE2=false&ANDROID_HTTP2_SUPPORT=false&SUPPLY_LOGGING=true&CONVERSATIONAL_NOTIFICATIONS=false";

            var ret = new WebHeaderCollection
            {
                ["X-Version"] = $"device/{TumblrConstants.AppVersion}/0/{sessionInfo.Device.OsVersion}/tumblr/",
                ["X-Identifier"] = sessionInfo.XIdentifier,
                ["X-Identifier-Date"] = sessionInfo.XIdentifierDate,
                ["Accept-Language"] = "en-US",
                ["Pragma"] = "no-cache",
                ["yx"] = sessionInfo.BCookie,
                ["X-Features"] = features,
                ["X-YUser-Agent"] = sessionInfo.XyUserAgent,
                ["di"] = sessionInfo.DeviceInfo,
                ["X-S-ID-Enabled"] = "false",
                ["X-S-ID"] = sessionInfo.XsId,
                ["Authorization"] = authHeader
            };

            return ret;
        }
    }
}
