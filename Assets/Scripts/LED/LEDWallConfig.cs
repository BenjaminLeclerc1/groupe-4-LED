using LedShow.Core;

public static class LEDWallConfig
{
    public const int VisibleWidth = LedNetworkConfig.WallWidth;
    public const int VisibleHeight = LedNetworkConfig.WallHeight;
    public const int VisibleLedCount = VisibleWidth * VisibleHeight;

    public const float PhysicalSizeMeters = 2f;

    public const int StripCount = 64;
    public const int LedsPerStrip = 259;
    public const int VisibleLedsPerStripLeg = 128;
    public const int UniversesPerStrip = 2;
    public const int UniverseCount = StripCount * UniversesPerStrip;
    public const int ChannelsPerLed = 3;
    public const int LedsPerUniverse = 170;

    public static string[] ControllerIps => LedNetworkConfig.ControllerIps;
}
