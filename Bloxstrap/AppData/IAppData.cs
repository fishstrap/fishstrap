namespace Bloxstrap.AppData
{
    public interface IAppData
    {
        string ProductName { get; }

        string BinaryType { get; }

        string RegistryName { get; }

        string ExecutableName { get; }

        string AppDataDirectory { get; }

        string StaticDirectory {  get; }

        string DynamicDirectory {  get; }

        string Directory { get; }

        string ExecutablePath { get; }

        string CdnExtension { get; }
        
        bool SupportsCustomDeployments { get; }

        AppState State { get; }
    }
}
