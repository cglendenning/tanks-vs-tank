using UnityEngine;

/// <summary>
/// Store-safe ad configuration. Keep production identifiers in this asset or
/// inject them in a release build; test ads remain the default until the app
/// has been verified in AdMob Policy Center.
/// </summary>
[CreateAssetMenu(menuName = "Tread Shred/Ad Configuration")]
public sealed class TankAdConfiguration : ScriptableObject
{
    public string iosAppId = "ca-app-pub-3940256099942544~1458002511";
    public string androidAppId = "ca-app-pub-3940256099942544~3347511713";
    public string iosInterstitialUnitId = "ca-app-pub-4402198490627677/5342886245";
    public string androidInterstitialUnitId = "ca-app-pub-4402198490627677/5342886245";
    public string iosBannerUnitId = "";
    public string androidBannerUnitId = "";
    public bool useTestAds = true;
    public bool allowPersonalizedAds = true;
}
