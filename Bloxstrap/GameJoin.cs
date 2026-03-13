using System.Web;

namespace Bloxstrap
{
    public class GameJoin
    {
        private long RegexMatchLong(string url, string query, string pattern)
        {
            Match match = Regex.Match(url, query + pattern);

            if (!match.Success)
                return 0;

            long.TryParse(match.Groups[1].Value, out long result);

            return result;
        }

        private string RegexMatchString(string url, string query, string pattern)
        {
            Match match = Regex.Match(url, query + pattern);

            if (!match.Success)
                return string.Empty;

            return match.Groups[1].Value;
        }

        public GameJoinData GetJoinDataByLaunchCommand(string launchCommandLine)
        {
            const string LOG_IDENT = "Bootstrapper::GetJoinDataByLaunchCommand";

            const string placelauncherPattern = @"placelauncherurl:(.+?)(\+|$)";
            const string requestTypePattern = @"request=(.+?)&";
            const string commonIntPattern = @"([0-9]+)";
            const string commonIdPattern = @"([a-zA-Z0-9-]+?)(&|\+|$)";

            string? url = null;
            GameJoinData joinData = new(); // by default its unknown

            if (!launchCommandLine.StartsWith("roblox-player:"))
                return joinData; // its either empty or deeplink start, those arent supported (yet?)

            Match urlMatch = Regex.Match(launchCommandLine, placelauncherPattern);
            if (!urlMatch.Success || urlMatch.Groups.Count != 3) return joinData; // the regex failed

            url = HttpUtility.UrlDecode(urlMatch.Groups[1].Value);
            if (string.IsNullOrEmpty(url)) return joinData;

            Match typeMatch = Regex.Match(url, requestTypePattern);
            if (!typeMatch.Success || typeMatch.Groups.Count != 2) return joinData;

            App.Logger.WriteLine(LOG_IDENT, "Detecting join type");

            // yuck
            switch (typeMatch.Groups[1].Value)
            {
                case "RequestGame":
                    {
                        joinData.JoinType = GameJoinType.RequestGame;

                        string joinOrigin = RegexMatchString(url, "joinAttemptOrigin=", commonIdPattern);
                        long placeId = RegexMatchLong(url, "placeId=", commonIntPattern);

                        if (placeId == 0) return joinData;

                        joinData.PlaceId = placeId;
                        joinData.JoinOrigin = joinOrigin;
                        break;
                    }
                case "RequestGameJob":
                    {
                        joinData.JoinType = GameJoinType.RequestGameJob;

                        string joinOrigin = RegexMatchString(url, "joinAttemptOrigin=", commonIdPattern);
                        string jobId = RegexMatchString(url, "gameId=", commonIdPattern);
                        long placeId = RegexMatchLong(url, "placeId=", commonIntPattern);

                        if (string.IsNullOrEmpty(jobId) || placeId == 0) return joinData;

                        joinData.PlaceId = placeId;
                        joinData.JobId = jobId;
                        joinData.JoinOrigin = joinOrigin;
                        break;
                    }
                case "RequestPrivateGame":
                    {
                        joinData.JoinType = GameJoinType.RequestPrivateGame;

                        string accessCode = RegexMatchString(url, "accessCode=", commonIdPattern);
                        long placeId = RegexMatchLong(url, "placeId=", commonIntPattern);

                        if (string.IsNullOrEmpty(accessCode) || placeId == 0) return joinData;

                        joinData.PlaceId = placeId;
                        joinData.AccessCode = accessCode;
                        break;
                    }
                case "RequestFollowUser":
                    {
                        joinData.JoinType = GameJoinType.RequestFollowUser;

                        long userId = RegexMatchLong(url, "userId=", commonIntPattern);

                        if (userId == 0) return joinData;

                        joinData.UserId = userId;
                        break;
                    }
                case "RequestPlayTogetherGame":
                    {
                        joinData.JoinType = GameJoinType.RequestPlayTogetherGame;

                        long placeId = RegexMatchLong(url, "placeId=", commonIntPattern);
                        string conversationId = RegexMatchString(url, "conversationId=", commonIdPattern);

                        if (string.IsNullOrEmpty(conversationId) || placeId == 0) return joinData;

                        joinData.PlaceId = placeId;
                        joinData.JobId = conversationId;
                        break;
                    }
            }

            App.Logger.WriteLine(LOG_IDENT, $"Join type: {joinData.JoinType}");

            return joinData;
        }
    }
}
