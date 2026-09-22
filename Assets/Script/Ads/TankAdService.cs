using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

/// <summary>
/// One consent-gated AdMob integration shared by iOS and Android.
/// Interstitials are only exposed to gameplay at a level boundary; banners
/// are opt-in because the legacy game did not have a safe, non-overlapping
/// banner layout.
/// </summary>
public sealed class TankAdService : MonoBehaviour
{
    private const string TestIosInterstitialId = "ca-app-pub-3940256099942544/4411468910";
    private const string TestAndroidInterstitialId = "ca-app-pub-3940256099942544/1033173712";
    private const string TestIosBannerId = "ca-app-pub-3940256099942544/2934735716";
    private const string TestAndroidBannerId = "ca-app-pub-3940256099942544/6300978111";

    public static TankAdService Instance { get; private set; }
    public bool IsReady { get; private set; }
    public bool CanRequestAds { get; private set; }

    [SerializeField] private TankAdConfiguration configuration;
    private InterstitialAd interstitial;
    private BannerView banner;
    private bool consentFlowStarted;
    private bool bannerRequested;
    private float lastInterstitialTime = -999f;

    private const float MinimumInterstitialIntervalSeconds = 90f;

    public static TankAdService Ensure()
    {
        if (Instance != null)
            return Instance;

        var host = new GameObject("Tank Ad Service");
        return host.AddComponent<TankAdService>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        configuration = configuration != null
            ? configuration
            : Resources.Load<TankAdConfiguration>("TankAdConfiguration");
        if (configuration == null)
            configuration = CreateFallbackConfiguration();

        BeginConsentFlow();
    }

    public void RequestBanner()
    {
        bannerRequested = true;
        if (IsReady)
            LoadBanner();
    }

    public void HideBanner()
    {
        bannerRequested = false;
        banner?.Hide();
    }

    public bool TryShowInterstitial()
    {
        if (!IsReady || !CanRequestAds || Time.unscaledTime - lastInterstitialTime < MinimumInterstitialIntervalSeconds)
            return false;

        if (interstitial == null || !interstitial.CanShowAd())
        {
            LoadInterstitial();
            return false;
        }

        lastInterstitialTime = Time.unscaledTime;
        interstitial.Show();
        return true;
    }

    public void ShowPrivacyOptionsForm()
    {
        if (!IsReady)
            return;

        ConsentForm.ShowPrivacyOptionsForm(error =>
        {
            if (error != null)
                Debug.LogWarning("[Ads] Privacy options form failed: " + error.Message);
        });
    }

    private void BeginConsentFlow()
    {
#if UNITY_EDITOR
        CanRequestAds = false;
        Debug.Log("[Ads] Editor mode: no live ads are requested.");
#else
        if (consentFlowStarted)
            return;

        consentFlowStarted = true;
        ConsentInformation.Update(new ConsentRequestParameters(), OnConsentInformationUpdated);
#endif
    }

    private void OnConsentInformationUpdated(FormError error)
    {
        if (error != null)
            Debug.LogWarning("[Ads] Consent information update failed: " + error.Message);

        ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
        {
            if (formError != null)
                Debug.LogWarning("[Ads] Consent form failed: " + formError.Message);

            CanRequestAds = ConsentInformation.CanRequestAds();
            InitializeMobileAds();
        });
    }

    private void InitializeMobileAds()
    {
        if (IsReady)
            return;

        MobileAds.Initialize(_ =>
        {
            IsReady = true;
            LoadInterstitial();
            if (bannerRequested)
                LoadBanner();
        });
    }

    private void LoadInterstitial()
    {
        if (!IsReady || !CanRequestAds)
            return;

        var adUnitId = CurrentInterstitialId();
        if (string.IsNullOrWhiteSpace(adUnitId))
        {
            Debug.LogWarning("[Ads] Interstitial unit ID is not configured.");
            return;
        }

        interstitial?.Destroy();
        InterstitialAd.Load(adUnitId, new AdRequest(), (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning("[Ads] Interstitial load failed: " + error);
                return;
            }

            interstitial = ad;
            interstitial.OnAdFullScreenContentClosed += () =>
            {
                interstitial = null;
                LoadInterstitial();
            };
            interstitial.OnAdFullScreenContentFailed += _ =>
            {
                interstitial = null;
                LoadInterstitial();
            };
        });
    }

    private void LoadBanner()
    {
        if (!IsReady || !CanRequestAds)
            return;

        var adUnitId = CurrentBannerId();
        if (string.IsNullOrWhiteSpace(adUnitId))
            return;

        banner?.Destroy();
        banner = new BannerView(adUnitId, AdSize.Banner, AdPosition.Bottom);
        banner.LoadAd(new AdRequest());
    }

    private string CurrentInterstitialId()
    {
        if (configuration.useTestAds)
        {
#if UNITY_IOS
            return TestIosInterstitialId;
#elif UNITY_ANDROID
            return TestAndroidInterstitialId;
#else
            return string.Empty;
#endif
        }

#if UNITY_IOS
        return configuration.iosInterstitialUnitId;
#elif UNITY_ANDROID
        return configuration.androidInterstitialUnitId;
#else
        return string.Empty;
#endif
    }

    private string CurrentBannerId()
    {
        if (configuration.useTestAds)
        {
#if UNITY_IOS
            return TestIosBannerId;
#elif UNITY_ANDROID
            return TestAndroidBannerId;
#else
            return string.Empty;
#endif
        }

#if UNITY_IOS
        return configuration.iosBannerUnitId;
#elif UNITY_ANDROID
        return configuration.androidBannerUnitId;
#else
        return string.Empty;
#endif
    }

    private static TankAdConfiguration CreateFallbackConfiguration()
    {
        var fallback = ScriptableObject.CreateInstance<TankAdConfiguration>();
        fallback.hideFlags = HideFlags.DontSave;
        return fallback;
    }
}
