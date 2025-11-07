using System.DirectoryServices.ActiveDirectory;
using System.Runtime.InteropServices;
using System.Web;
using System.Windows;
using System.Windows.Input;
using Bloxstrap.AppData;
using Bloxstrap.Models.APIs;
using Bloxstrap.Models.APIs.RoValra;
using CommunityToolkit.Mvvm.Input;

namespace Bloxstrap.Models.Entities
{
    public class ActivityData
    {

        private long _universeId = 0;

        /// <summary>
        /// If the current activity stems from an in-universe teleport, then this will be
        /// set to the activity that corresponds to the initial game join
        /// </summary>
        public ActivityData? RootActivity;

        public long UniverseId
        {
            get => _universeId;
            set
            {
                _universeId = value;
                UniverseDetails.LoadFromCache(value);
            }
        }

        public long PlaceId { get; set; } = 0;

        public string JobId { get; set; } = string.Empty;

        public DateTime? StartTime { get; set; }

        /// <summary>
        /// This will be empty unless the server joined is a private server
        /// </summary>
        public string AccessCode { get; set; } = string.Empty;
        
        public long UserId { get; set; } = 0;

        public string MachineAddress { get; set; } = string.Empty;

        public bool MachineAddressValid => !string.IsNullOrEmpty(MachineAddress) && !MachineAddress.StartsWith("10.");

        public bool IsTeleport { get; set; } = false;

        public ServerType ServerType { get; set; } = ServerType.Public;

        public DateTime TimeJoined { get; set; }

        public DateTime? TimeLeft { get; set; }

        // everything below here is optional strictly for bloxstraprpc, discord rich presence, or game history

        /// <summary>
        /// This is intended only for other people to use, i.e. context menu invite link, rich presence joining
        /// </summary>
        public string RPCLaunchData { get; set; } = string.Empty;

        public UniverseDetails? UniverseDetails { get; set; }

        public string GameHistoryDescription
        {
            get
            {
                string desc = string.Format(
                    "{0} • {1} {2} {3}", 
                    UniverseDetails?.Data.Creator.Name,
                    TimeJoined.ToString("t"), 
                    Locale.CurrentCulture.Name.StartsWith("ja") ? '~' : '-',
                    TimeLeft?.ToString("t")
                );

                if (ServerType != ServerType.Public)
                    desc += " • " + ServerType.ToTranslatedString();

                return desc;
            }
        }

        public ICommand RejoinServerCommand => new RelayCommand(RejoinServer);

        private SemaphoreSlim serverQuerySemaphore = new(1, 1);

        public string GetInviteDeeplink(bool launchData = true, bool useRobloxUri = false)
        {
            // if our data isnt loaded it uses dummy data
            // we only wait for important data

            // we need useRobloxUri for rejoin feature
            string deeplink = $"{(useRobloxUri ? "roblox://experiences/start" : App.RemoteData.Prop.DeeplinkUrl)}?placeId={PlaceId}";

            if (ServerType == ServerType.Private) // thats not going to work
                deeplink += "&accessCode=" + AccessCode;
            else
                deeplink += "&gameInstanceId=" + JobId;

            if (launchData && !string.IsNullOrEmpty(RPCLaunchData))
                deeplink += "&launchData=" + HttpUtility.UrlEncode(RPCLaunchData);

            return deeplink;
        }

        // we use rovalra's apis in fishstrap.app/joingame
        public void ProcessServerRoValra()
        {
            if (string.IsNullOrEmpty(JobId))
                throw new InvalidOperationException("JobId is null");

            if (PlaceId == 0)
                throw new InvalidOperationException("PlaceId is null");

            var serverBody = new RoValraProcessServerBody
            {
                PlaceId = PlaceId,
                ServerIds = new() { JobId }
            };

            string json = JsonSerializer.Serialize(serverBody);
            HttpContent postContent = new StringContent(json, Encoding.UTF8, "application/json");

            // we dont need to await it since its not as important
            // we want to return uptime quickly
            _ = App.HttpClient.PostAsync("https://apis.rovalra.com/process_servers", postContent);
        }

        public async Task<string?> QueryServerLocation()
        {
            const string LOG_IDENT = "ActivityData::QueryServerLocation";

            if (!MachineAddressValid)
                throw new InvalidOperationException($"Machine address is invalid ({MachineAddress})");

            await serverQuerySemaphore.WaitAsync();

            if (GlobalCache.ServerLocation.TryGetValue(MachineAddress, out string? location))
            {
                serverQuerySemaphore.Release();
                return location;
            }

            try
            {
                var response = await Http.GetJson<RoValraGeolocation>($"https://apis.rovalra.com/v1/geolocation?ip={MachineAddress}");
                var geolocation = response.Location;

                if (geolocation is null)
                    location = Strings.Common_Unknown;
                else
                {
                    if (geolocation.City == geolocation.Region && geolocation.City == geolocation.Country)
                        location = geolocation.Country;
                    else if (geolocation.City == geolocation.Region)
                        location = $"{geolocation.Region}, {geolocation.Country}";
                    else
                        location = $"{geolocation.City}, {geolocation.Region}, {geolocation.Country}";
                }

                GlobalCache.ServerLocation[MachineAddress] = location;
                serverQuerySemaphore.Release();
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to get server location for {MachineAddress}");
                App.Logger.WriteException(LOG_IDENT, ex);

                GlobalCache.ServerLocation[MachineAddress] = location;
                serverQuerySemaphore.Release();

                Frontend.ShowConnectivityDialog(
                    string.Format(Strings.Dialog_Connectivity_UnableToConnect, "rovalra.com"),
                    Strings.ActivityWatcher_LocationQueryFailed,
                    MessageBoxImage.Warning,
                    ex
                );
            }

            return location;
        }

        public override string ToString() => $"{PlaceId}/{JobId}";

        private void RejoinServer()
        {
            string playerPath = new RobloxPlayerData().ExecutablePath;

            Process.Start(playerPath, GetInviteDeeplink(false, true));
        }
    }
}
