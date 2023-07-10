using Waifu.Sys;

namespace Tumblr.Waifu
{
    public class TumblrAndroidDevice
    {
        public TumblrAndroidDevice(
            string manufacturer,
            string model,
            string osVersion,
            string buildId)
        {
            Manufacturer = manufacturer;
            Model = model;
            OsVersion = osVersion;
            BuildId = buildId;
        }

        public string Manufacturer { get; }
        public string Model { get; }
        public string OsVersion { get; }
        public string BuildId { get; }

        public override string ToString()
        {
            return $"{Manufacturer}|{Model}|{OsVersion}|{BuildId}";
        }

        public static bool TryParse(string input, out TumblrAndroidDevice device)
        {
            device = null;
            if (string.IsNullOrWhiteSpace(input) ||
                !input.Contains("|"))
            {
                return false;
            }

            var split = input.Split('|');
            if (split.Length != 4)
                return false;

            var manufacturer = split[0];
            var model = split[1];
            var version = split[2];
            var buildId = split[3];

            if (StringHelpers.AnyNullOrEmpty(
                manufacturer,
                model,
                version,
                buildId))
            {
                return false;
            }

            device = new TumblrAndroidDevice(manufacturer, model, version, buildId);

            return true;
        }
    }
}
