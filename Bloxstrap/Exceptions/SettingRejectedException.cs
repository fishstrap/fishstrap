namespace Bloxstrap.Exceptions
{
    internal class SettingRejectedException : Exception
    {
        public string? Reason { get; }

        public SettingRejectedException(string setting, string value, int status, string? reason)
            : base($"{status} setting {setting} to {value}: {reason ?? "no reason given"}")
        {
            Reason = reason;
        }
    }
}
