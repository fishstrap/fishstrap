using Bloxstrap.RobloxInterfaces;

namespace Bloxstrap.Utility
{
    public static class UrlBuilder
    {
        public static Uri BuildApiUrl(string service, string path, bool secure = true)
        {
            string domain = Deployment.RobloxDomain;
            string url = secure ? "https://" : "http://";
            url += service + ".";
            url += domain + "/";
            url += path;

            return new(url);
        }
    }
}
