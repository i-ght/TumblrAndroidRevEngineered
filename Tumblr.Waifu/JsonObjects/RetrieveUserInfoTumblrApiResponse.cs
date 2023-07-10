using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tumblr.Waifu.JsonObjects
{

    public class TumblrTourGuides
    {

        [JsonProperty("like")]
        public bool Like { get; set; }

        [JsonProperty("follow")]
        public bool Follow { get; set; }

        [JsonProperty("reblog")]
        public bool Reblog { get; set; }

        [JsonProperty("compose")]
        public bool Compose { get; set; }

        [JsonProperty("appearance")]
        public bool Appearance { get; set; }

        [JsonProperty("search")]
        public bool Search { get; set; }
    }

    public class TumblrGlobal
    {

        [JsonProperty("in_app")]
        public bool InApp { get; set; }

        [JsonProperty("push_notification")]
        public bool PushNotification { get; set; }
    }

    public class TumblrSounds
    {

        [JsonProperty("global")]
        public TumblrGlobal Global { get; set; }
    }

    public class TumblrNotificationSettings
    {

        [JsonProperty("messaging")]
        public string Messaging { get; set; }

        [JsonProperty("general")]
        public string General { get; set; }
    }

    public class TumblrTheme
    {

        [JsonProperty("avatar_shape")]
        public string AvatarShape { get; set; }

        [JsonProperty("background_color")]
        public string BackgroundColor { get; set; }

        [JsonProperty("body_font")]
        public string BodyFont { get; set; }

        [JsonProperty("header_bounds")]
        public string HeaderBounds { get; set; }

        [JsonProperty("header_image")]
        public string HeaderImage { get; set; }

        [JsonProperty("header_image_focused")]
        public string HeaderImageFocused { get; set; }

        [JsonProperty("header_image_scaled")]
        public string HeaderImageScaled { get; set; }

        [JsonProperty("header_stretch")]
        public bool HeaderStretch { get; set; }

        [JsonProperty("link_color")]
        public string LinkColor { get; set; }

        [JsonProperty("show_avatar")]
        public bool ShowAvatar { get; set; }

        [JsonProperty("show_description")]
        public bool ShowDescription { get; set; }

        [JsonProperty("show_header_image")]
        public bool ShowHeaderImage { get; set; }

        [JsonProperty("show_title")]
        public bool ShowTitle { get; set; }

        [JsonProperty("title_color")]
        public string TitleColor { get; set; }

        [JsonProperty("title_font")]
        public string TitleFont { get; set; }

        [JsonProperty("title_font_weight")]
        public string TitleFontWeight { get; set; }
    }

    public class TumblrBlog
    {

        [JsonProperty("admin")]
        public bool Admin { get; set; }

        [JsonProperty("ask")]
        public bool Ask { get; set; }

        [JsonProperty("ask_anon")]
        public bool AskAnon { get; set; }

        [JsonProperty("ask_page_title")]
        public string AskPageTitle { get; set; }

        [JsonProperty("can_message")]
        public bool CanMessage { get; set; }

        [JsonProperty("can_send_fan_mail")]
        public bool CanSendFanMail { get; set; }

        [JsonProperty("can_subscribe")]
        public bool CanSubscribe { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("drafts")]
        public int Drafts { get; set; }

        [JsonProperty("followed")]
        public bool Followed { get; set; }

        [JsonProperty("followers")]
        public int Followers { get; set; }

        [JsonProperty("has_customized_blog_detail")]
        public bool HasCustomizedBlogDetail { get; set; }

        [JsonProperty("is_adult")]
        public bool IsAdult { get; set; }

        [JsonProperty("is_blocked_from_primary")]
        public bool IsBlockedFromPrimary { get; set; }

        [JsonProperty("is_group_channel")]
        public bool IsGroupChannel { get; set; }

        [JsonProperty("is_nsfw")]
        public bool IsNsfw { get; set; }

        [JsonProperty("is_private_channel")]
        public bool IsPrivateChannel { get; set; }

        [JsonProperty("likes")]
        public int Likes { get; set; }

        [JsonProperty("linked_accounts")]
        public IList<object> LinkedAccounts { get; set; }

        [JsonProperty("messages")]
        public int Messages { get; set; }

        [JsonProperty("messaging_allow_follows_only")]
        public bool MessagingAllowFollowsOnly { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("notification_settings")]
        public TumblrNotificationSettings NotificationSettings { get; set; }

        [JsonProperty("notifications")]
        public string Notifications { get; set; }

        [JsonProperty("placement_id")]
        public string PlacementId { get; set; }

        [JsonProperty("posts")]
        public int Posts { get; set; }

        [JsonProperty("primary")]
        public bool Primary { get; set; }

        [JsonProperty("queue")]
        public int Queue { get; set; }

        [JsonProperty("random_name")]
        public bool RandomName { get; set; }

        [JsonProperty("reply_conditions")]
        public string ReplyConditions { get; set; }

        [JsonProperty("seconds_since_last_activity")]
        public int SecondsSinceLastActivity { get; set; }

        [JsonProperty("share_following")]
        public bool ShareFollowing { get; set; }

        [JsonProperty("share_likes")]
        public bool ShareLikes { get; set; }

        [JsonProperty("show_author_avatar")]
        public bool ShowAuthorAvatar { get; set; }

        [JsonProperty("subscribed")]
        public bool Subscribed { get; set; }

        [JsonProperty("theme")]
        public TumblrTheme Theme { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("total_posts")]
        public int TotalPosts { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("updated")]
        public int Updated { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("uuid")]
        public string Uuid { get; set; }
    }

    public class TumblrUser
    {

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("likes")]
        public int Likes { get; set; }

        [JsonProperty("following")]
        public int Following { get; set; }

        [JsonProperty("default_post_format")]
        public string DefaultPostFormat { get; set; }

        [JsonProperty("safe_search")]
        public bool SafeSearch { get; set; }

        [JsonProperty("push_notifications")]
        public bool PushNotifications { get; set; }

        [JsonProperty("tour_guides")]
        public TumblrTourGuides TourGuides { get; set; }

        [JsonProperty("crm_uuid")]
        public string CrmUuid { get; set; }

        [JsonProperty("sounds")]
        public TumblrSounds Sounds { get; set; }

        [JsonProperty("safe_mode")]
        public bool SafeMode { get; set; }

        [JsonProperty("can_modify_safe_mode")]
        public bool CanModifySafeMode { get; set; }

        [JsonProperty("oneid")]
        public int Oneid { get; set; }

        [JsonProperty("find_by_email")]
        public bool FindByEmail { get; set; }

        [JsonProperty("show_online_status")]
        public bool ShowOnlineStatus { get; set; }

        [JsonProperty("conversational_notifications")]
        public bool ConversationalNotifications { get; set; }

        [JsonProperty("blogs")]
        public IList<TumblrBlog> Blogs { get; set; }

        [JsonProperty("owns_customized_blogs")]
        public bool OwnsCustomizedBlogs { get; set; }
    }

    public class UserInfoResponse
    {
        [JsonProperty("user")]
        public TumblrUser User { get; set; }
    }

    public class RetrieveUserInfoTumblrApiResponse
    {

        [JsonProperty("meta")]
        public TumblrApiResponseMeta Meta { get; set; }

        [JsonProperty("response")]
        public UserInfoResponse Response { get; set; }
    }
}
