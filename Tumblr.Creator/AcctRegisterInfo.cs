using Tumblr.Waifu;
using Waifu.MobileDevice;

namespace Tumblr.Creator
{
    internal class AcctRegisterInfo
    {
        public AcctRegisterInfo(
            string username,
            string email,
            string password,
            int age,
            TumblrSessionInfo sessionInfo)
        {
            Username = username;
            Email = email;
            Password = password;
            Age = age;
            SessionInfo = sessionInfo;
        }

        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; }
        public int Age { get; }
        public TumblrSessionInfo SessionInfo { get; }
    }
}
