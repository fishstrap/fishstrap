using Bloxstrap.RobloxInterfaces;
using System;
using System.Net.WebSockets;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Bloxstrap
{
    public class CookiesManager
    {
        private CookieState _state = CookieState.Unknown;

        public EventHandler<CookieState>? StateChanged;
        public CookieState State {
            get => _state;
            set {
                _state = value;
                StateChanged?.Invoke(this, value);
            }
        }
        public bool Loaded => Enabled && State == CookieState.Success;
        private bool Enabled => App.Settings.Prop.AllowCookieAccess;

        public AuthenticatedUser? CurrentUser { get; private set; }

        public bool IsAuthenticated => CurrentUser is not null;

        private string AuthCookie = string.Empty;
        private const string AuthCookieName = ".ROBLOSECURITY";
        private const string SupportedVersion = "1";
        private const string AuthPattern = $@"\t{AuthCookieName}\t(.+?)(;|$)";

        private const string CsrfHeader = "x-csrf-token";

        // tracker is needed for privacy related features like game presence privacy and user presence privacy
        private const string TrackerCookieName = "RBXEventTrackerV2";
        private const string TrackerPattern = $@"\t{TrackerCookieName}\t(.+?)(;|$)";

        private string BrowserTracker = string.Empty;

        private string _csrfToken = string.Empty;
        private string CookiesPath => Path.Combine(Paths.Roblox, "LocalStorage", Deployment.IsDefaultRobloxDomain ? "RobloxCookies.dat" : $"{Deployment.RobloxDomain}_RobloxCookies.dat");

        public async Task<HttpResponseMessage> AuthRequest(HttpRequestMessage request)
        {
            string? host = request.RequestUri?.Host;

            // basic host validation in case we accidentally send authenticated request somewhere unwanted
            if (host is null)
                throw new ArgumentNullException("Host cannot be null");

            if (
                !host.Equals(Deployment.RobloxDomain, StringComparison.OrdinalIgnoreCase) &&
                !host.EndsWith("." + Deployment.RobloxDomain, StringComparison.OrdinalIgnoreCase)
                )
                throw new HttpRequestException($"Host must end with Roblox domain ({Deployment.RobloxDomain})");

            if (!Enabled)
                throw new NullReferenceException("Cookie access is not enabled");

            HttpResponseMessage response = await SendAuthenticated(request);

            if (response.StatusCode != HttpStatusCode.Forbidden
                || !response.Headers.TryGetValues(CsrfHeader, out IEnumerable<string>? tokens)
                || tokens.FirstOrDefault() is not string token
                || String.IsNullOrEmpty(token)
                || token == _csrfToken)
                return response;

            _csrfToken = token;

            response.Dispose();

            return await SendAuthenticated(Repeat(request));
        }

        private async Task<HttpResponseMessage> SendAuthenticated(HttpRequestMessage request)
        {
            request.Headers.Remove(CsrfHeader);

            if (!String.IsNullOrEmpty(_csrfToken))
                request.Headers.Add(CsrfHeader, _csrfToken);

            request.Headers.Remove("Cookie");
            request.Headers.Add("Cookie", String.IsNullOrEmpty(BrowserTracker)
                ? $".ROBLOSECURITY={AuthCookie}"
                : $".ROBLOSECURITY={AuthCookie}; {TrackerCookieName}={BrowserTracker}");

            return await App.HttpClient.SendAsync(request);
        }

        private static HttpRequestMessage Repeat(HttpRequestMessage request)
        {
            var repeat = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Content = request.Content,
                Version = request.Version
            };

            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
                repeat.Headers.TryAddWithoutValidation(header.Key, header.Value);

            return repeat;
        }

        public async Task<HttpResponseMessage> AuthGet(Uri? uri) => await AuthRequest(new HttpRequestMessage { RequestUri = uri, Method = HttpMethod.Get });
        public async Task<HttpResponseMessage> AuthPost(Uri? uri, HttpContent? content) => await AuthRequest(new HttpRequestMessage { RequestUri = uri, Content = content, Method = HttpMethod.Post });

        public void AuthWebsocket(ClientWebSocket webSocket)
        {
            if (!Enabled)
                throw new NullReferenceException("Cookie access is not enabled");

            webSocket.Options.SetRequestHeader("Cookie", $".ROBLOSECURITY={AuthCookie}");
        }

        public async Task<AuthenticatedUser?> GetAuthenticated()
        {
            const string LOG_IDENT = "CookiesManager::GetAuthenticated";
            
            try
            {
                Uri apiUrl = UrlBuilder.BuildApiUrl("users", "v1/users/authenticated");
                HttpResponseMessage response = await AuthGet(apiUrl);
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();
                AuthenticatedUser user = JsonSerializer.Deserialize<AuthenticatedUser>(content)!;

                return user;
            }
            catch (HttpRequestException ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to get authenticated user");
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            return null;
        }

        public async Task<bool> EnsureLoadedAsync()
        {
            if (!Enabled)
                return false;

            if (!Loaded)
                await Task.Run(LoadCookies);

            return Loaded;
        }

        public async Task LoadCookies()
        {
            const string LOG_IDENT = "CookiesManager::LoadCookies";

            // we use the status to infrom user about it in the menu
            if (!Enabled)
            {
                State = CookieState.NotAllowed;
                App.Logger.WriteLine(LOG_IDENT, "Cookie access not allowed");
                return;
            }

            if (!string.IsNullOrEmpty(AuthCookie))
            {
                App.Logger.WriteLine(LOG_IDENT, "Cookie was already loaded!");
                return;
            }

            if (!File.Exists(CookiesPath))
            {
                State = CookieState.NotFound;
                App.Logger.WriteLine(LOG_IDENT, "Cookie file not found");
                return;
            }

            try
            {
                string content = File.ReadAllText(CookiesPath);
                var cookies = JsonSerializer.Deserialize<RobloxCookies>(content)!;

                if (cookies.Version != SupportedVersion)
                    App.Logger.WriteLine(LOG_IDENT, $"Unknown cookie version: {cookies.Version}");

                // here we got the raw bytes data which we have to decrypt with user scope
                // from that we get raw cookies data in roblox's format
                // in our case we will regex it since all we need is auth cookie
                byte[] encryptedData = Convert.FromBase64String(cookies.Cookies);
                byte[] unencryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);

                string rawCookies = Encoding.UTF8.GetString(unencryptedData);
                Match authCookieMatch = Regex.Match(rawCookies, AuthPattern);

                if (!authCookieMatch.Success)
                {
                    State = CookieState.Invalid;
                    App.Logger.WriteLine(LOG_IDENT, "Regex failed for cookies");
                    return;
                }

                string authCookie = authCookieMatch.Groups[1].Value;
                AuthCookie = authCookie; // could use better naming

                Match trackerMatch = Regex.Match(rawCookies, TrackerPattern);

                if (trackerMatch.Success)
                    BrowserTracker = trackerMatch.Groups[1].Value;

                App.Logger.WriteLine(LOG_IDENT, trackerMatch.Success ? "Found a browser tracker" : "No browser tracker in the cookie store");

                // we test the cookie to see if its valid
                AuthenticatedUser? user = await GetAuthenticated();
                if (user is null || user?.Id == 0)
                {
                    State = CookieState.Invalid;
                    App.Logger.WriteLine(LOG_IDENT, "Cookie is invalid");
                    return;
                }

                State = CookieState.Success;
                CurrentUser = user;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to load cookie!");
                App.Logger.WriteException(LOG_IDENT, ex); 

                State = CookieState.Failed;
            }

            return;
        }
    }
}
