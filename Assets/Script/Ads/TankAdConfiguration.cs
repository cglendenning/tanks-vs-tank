using UnityEngine;

/// <summary>
/// Production ad configuration for the published Tread Shred apps.
/// Test IDs remain hard-coded in TankAdService and are selected only by an
/// explicit test configuration, never by this production asset.
/// </summary>
[CreateAssetMenu(menuName = "Tread Shred/Ad Configuration")]
public sealed class TankAdConfiguration : ScriptableObject
{
    public string iosAppId = "ca-app-pub-4402198490627677~4284546322";
    public string androidAppId = "ca-app-pub-4402198490627677~8381484915";
    public string iosInterstitialUnitId = "ca-app-pub-4402198490627677/5342886245";
    public string androidInterstitialUnitId = "ca-app-pub-4402198490627677/3893066258";
    public string iosRewardedUnitId = "ca-app-pub-4402198490627677/3118275391";
    public string androidRewardedUnitId = "ca-app-pub-4402198490627677/6501143322";
    public string iosBannerUnitId = "";
    public string androidBannerUnitId = "";
    public bool useTestAds = false;
    public bool allowPersonalizedAds = true;
}
