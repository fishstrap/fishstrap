using Bloxstrap.RobloxInterfaces;

namespace Bloxstrap.Utility
{
    public static class UrlBuilder
    {
        private const string PlacelauncherBaseUrl = "https://www.roblox.com/Game/PlaceLauncher.ashx";

        public static Uri BuildApiUrl(string service, string path, bool secure = true)
        {
            string domain = Deployment.RobloxDomain;
            string url = secure ? "https://" : "http://";
            url += service + ".";
            url += domain + "/";
            url += path;

            return new(url);
        }

        public static string BuildPlacelauncherUrl(long placeId, string? jobId)
        {
            string url = PlacelauncherBaseUrl;
            url += "?request=RequestGameJob&placeId=";
            url += placeId;
            
            if (jobId is not null)
            {
                url += "&gameId=";
                url += jobId;
            }

            return url;
        }

        public static string BuildPrivateGamePlaceLauncher(long placeId, string accessCode)
        {
            string url = PlacelauncherBaseUrl;
            url += "?request=RequestPrivateGame&placeId=";
            url += placeId;
            url += "&accessCode=";
            url += accessCode;

            return url;
        }

        public static string BuildFollowUserPlaceLauncher(long userId)
        {
            string url = PlacelauncherBaseUrl;
            url += "?request=RequestFollowUser&userId=";
            url += userId;

            return url;
        }
    }
}
