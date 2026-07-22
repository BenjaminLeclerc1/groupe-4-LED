namespace LedShow.Core
{
    /// <summary>
    /// Source unique des IPs / constantes réseau du mur LAPS + projecteurs.
    /// </summary>
    public static class LedNetworkConfig
    {
        public const int WallWidth = 128;
        public const int WallHeight = 128;

        public static readonly string[] ControllerIps =
        {
            "192.168.1.45",
            "192.168.1.46",
            "192.168.1.47",
            "192.168.1.48",
        };

        /// <summary>Contrôleur qui porte aussi les lyres / projecteur (dernier IP mur).</summary>
        public static string LightingControllerIp => ControllerIps[ControllerIps.Length - 1];

        public const int LightingUniverse = 33;
    }
}
