namespace Bloxstrap.AppData
{
    public class RobloxPlayerVNGData : CommonAppData, IAppData
    {
        public string ProductName => "Roblox VNG";

        public override string BinaryType => "WindowsPlayer";

        public override string AppDataDirectory => Path.Combine(Paths.LocalAppData, "RobloxPCVNG");

        public string RegistryName => "RobloxPlayer";

        public override string ExecutableName => App.RobloxPlayerAppName;

        public override string CdnExtension => "/vng";

        public override bool SupportsCustomDeployments => false;

        public override AppState State => App.RobloxState.Prop.Player;
    }
}
