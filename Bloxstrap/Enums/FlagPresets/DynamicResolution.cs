namespace Bloxstrap.Enums.FlagPresets
{
    public enum DynamicResolution
    {
        [EnumName(FromTranslation = "Default")]
        Default,

        [EnumName(StaticName = "144p")]
        Resolution1,

        [EnumName(StaticName = "240p")]
        Resolution2,

        [EnumName(StaticName = "360p")]
        Resolution3,

        [EnumName(StaticName = "480p")]
        Resolution4,

        [EnumName(StaticName = "720p")]
        Resolution5,

        [EnumName(StaticName = "1080p")]
        Resolution6,

        [EnumName(StaticName = "1440p")]
        Resolution7,

        [EnumName(StaticName = "2160p")]
        Resolution8,

        [EnumName(StaticName = "4320p")]
        Resolution9,

        [EnumName(StaticName = "Custom Value")]
        CustomValue
    }
}
