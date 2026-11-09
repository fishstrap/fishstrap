namespace Bloxstrap.Models.Overlay
{
    public class ServerRegion
    {
        public string Code { get; set; } = String.Empty;

        public string Name { get; set; } = String.Empty;

        public bool IsAll => String.IsNullOrEmpty(Code);

        public override string ToString() => Name;
    }
}
