using Waifu.MobileDevice;
using Waifu.Sys;

namespace Tumblr.Waifu
{
    public class TumblrSessionInfo
    {
        public TumblrSessionInfo(
            string xIdentifier,
            string xIdentifierDate,
            string xsId,
            string bCookie,
            TumblrAndroidDevice device,
            CellyCarrierInfo carrierInfo)
        {
            XIdentifier = xIdentifier;
            XIdentifierDate = xIdentifierDate;
            XsId = xsId;
            BCookie = bCookie;
            XyUserAgent =
                $"Dalvik/1.6.0 (Linux; U; Android {device.OsVersion}; {device.Model} Build/{device.BuildId})/Tumblr/device/{TumblrConstants.AppVersion}/0/{device.OsVersion}/tumblr/";
            DeviceInfo = $"DI/1.0 ({carrierInfo.MCC}; {carrierInfo.MNC}; [WIFI])";
            Device = device;
            CarrierInfo = carrierInfo;
        }

        public TumblrSessionInfo(
            string xIdentifier,
            string xIdentifierDate,
            string xsId,
            string bCookie,
            string xyUserAgent,
            string deviceInfo,
            TumblrAndroidDevice device,
            CellyCarrierInfo carrierInfo)
        {
            XIdentifier = xIdentifier;
            XIdentifierDate = xIdentifierDate;
            XsId = xsId;
            BCookie = bCookie;
            XyUserAgent = xyUserAgent;
            DeviceInfo = deviceInfo;
            Device = device;
            CarrierInfo = carrierInfo;
        }

        public string XIdentifier { get; }
        public string XIdentifierDate { get; }
        public string XsId { get; }
        public string BCookie { get; }
        public string XyUserAgent { get; }
        public string DeviceInfo { get; }
        public TumblrAndroidDevice Device { get; }
        public CellyCarrierInfo CarrierInfo { get; }

        public override string ToString()
        {
            return
                $"{XIdentifier}:{XIdentifierDate}:{XsId}:{BCookie}:{XyUserAgent}:{DeviceInfo}:{Device}:{CarrierInfo}";
        }

        public static bool TryParse(string input, out TumblrSessionInfo sessionInfo)
        {
            sessionInfo = null;

            if (string.IsNullOrWhiteSpace(input) ||
                !input.Contains(":") ||
                !input.Contains("~"))
            {
                return false;
            }

            var split = input.Split(':');
            if (split.Length != 8)
                return false;

            if (StringHelpers.AnyNullOrEmpty(split))
                return false;

            var devStr = split[6];
            if (!TumblrAndroidDevice.TryParse(devStr, out var device))
                return false;

            var carrierInfoStr = split[7];
            if (!CellyCarrierInfo.TryParse(carrierInfoStr, out var carrierInfo))
                return false;

            var xId = split[0];
            var xIdDate = split[1];
            var xsId = split[2];
            var bCookie = split[3];
            var xyUserAgent = split[4];
            var devInfo = split[5];

            sessionInfo = new TumblrSessionInfo(
                xId,
                xIdDate,
                xsId,
                bCookie,
                xyUserAgent,
                devInfo,
                device,
                carrierInfo
            );

            return true;
        }
    }
}
