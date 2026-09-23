using System;
using System.Collections;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

/// <summary>
/// One consent-gated AdMob integration shared by iOS and Android.
/// Interstitials are only exposed to gameplay at a level boundary; rewarded
/// ads are only exposed behind an explicit player action; banners are opt-in
/// because the legacy game did not have a safe, non-overlapping banner layout.
/// </summary>
public sealed class TankAdService : MonoBehaviour
{
    private const string TestIosInterstitialId = "ca-app-pub-3940256099942544/4411468910";
    private const string TestAndroidInterstitialId = "ca-app-pub-3940256099942544/1033173712";
    private const string TestIosRewardedId = "ca-app-pub-3940256099942544/1712485313";
    private const string TestAndroidRewardedId = "ca-app-pub-3940256099942544/5224354917";
    private const string TestIosBannerId = "ca-app-pub-3940256099942544/2934735716";
    private const string TestAndroidBannerId = "ca-app-pub-3940256099942544/6300978111";

    public static TankAdService Instance { get; private set; }
    public bool IsReady { get; private set; }
    public bool CanRequestAds { get; private set; }
    public bool CanShowRewarded => IsReady && CanRequestAds && rewarded != null && rewarded.CanShowAd();

    [SerializeField] private TankAdConfiguration configuration;
    private InterstitialAd interstitial;
    private RewardedAd rewarded;
    private BannerView banner;
    private bool consentFlowStarted;
    private bool bannerRequested;
    private bool interstitialPresentationInProgress;
    private bool rewardedPresentationInProgress;
    private bool audioWasPausedBeforeInterstitial;
    private float audioVolumeBeforeInterstitial;
    private bool rewardedAdEarned;
    private Action rewardedCallback;
    private Coroutine adAudioRoutine;
    private Coroutine consentRetryRoutine;
    private int consentRetryCount;
    private float lastInterstitialTime = -999f;

    private const float MinimumInterstitialIntervalSeconds = 90f;
    private const float AdAudioFadeOutSeconds = 0.12f;
    private const float AdAudioFadeInSeconds = 0.18f;
    private const int MaxConsentRetries = 3;

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
        if (interstitialPresentationInProgress || !IsReady || !CanRequestAds ||
            Time.unscaledTime - lastInterstitialTime < MinimumInterstitialIntervalSeconds)
            return false;

        if (interstitial == null || !interstitial.CanShowAd())
        {
            LoadInterstitial();
            return false;
        }

        interstitialPresentationInProgress = true;
        lastInterstitialTime = Time.unscaledTime;
        adAudioRoutine = StartCoroutine(FadeOutAudioThenShowInterstitial(interstitial));
        return true;
    }

    /// <summary>
    /// Shows a user-initiated rewarded ad. The callback is invoked only after
    /// the user earns the reward and the full-screen ad has closed.
    /// </summary>
    public bool TryShowRewarded(Action onRewardEarned)
    {
        if (rewardedPresentationInProgress || onRewardEarned == null ||
            !IsReady || !CanRequestAds)
            return false;

        if (rewarded == null || !rewarded.CanShowAd())
        {
            LoadRewarded();
            return false;
        }

        rewardedPresentationInProgress = true;
        rewardedAdEarned = false;
        rewardedCallback = onRewardEarned;
        adAudioRoutine = StartCoroutine(FadeOutAudioThenShowRewarded(rewarded));
        return true;
    }

    private IEnumerator FadeOutAudioThenShowInterstitial(InterstitialAd ad)
    {
        audioWasPausedBeforeInterstitial = AudioListener.pause;
        audioVolumeBeforeInterstitial = AudioListener.volume;

        yield return FadeAudioVolume(0f, AdAudioFadeOutSeconds);
        AudioListener.pause = true;
        adAudioRoutine = null;

        try
        {
            ad.Show();
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EndInterstitialAudioProtection();
            interstitialPresentationInProgress = false;
            LoadInterstitial();
        }
    }

    private IEnumerator FadeOutAudioThenShowRewarded(RewardedAd ad)
    {
        audioWasPausedBeforeInterstitial = AudioListener.pause;
        audioVolumeBeforeInterstitial = AudioListener.volume;

        yield return FadeAudioVolume(0f, AdAudioFadeOutSeconds);
        AudioListener.pause = true;
        adAudioRoutine = null;

        try
        {
            ad.Show(_ => rewardedAdEarned = true);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            rewarded = null;
            rewardedCallback = null;
            rewardedAdEarned = false;
            EndRewardedAudioProtection();
            rewardedPresentationInProgress = false;
            LoadRewarded();
        }
    }

    private IEnumerator FadeAudioVolume(float targetVolume, float duration)
    {
        float startingVolume = AudioListener.volume;
        if (duration <= 0f)
        {
            AudioListener.volume = targetVolume;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            AudioListener.volume = Mathf.Lerp(startingVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        AudioListener.volume = targetVolume;
    }

    private void EndInterstitialAudioProtection()
    {
        if (adAudioRoutine != null)
            StopCoroutine(adAudioRoutine);

        adAudioRoutine = StartCoroutine(RestoreAudioAfterInterstitial());
    }

    private IEnumerator RestoreAudioAfterInterstitial()
    {
        AudioListener.pause = audioWasPausedBeforeInterstitial;
        yield return FadeAudioVolume(audioVolumeBeforeInterstitial, AdAudioFadeInSeconds);
        adAudioRoutine = null;
        interstitialPresentationInProgress = false;
    }

    private void EndRewardedAudioProtection()
    {
        if (adAudioRoutine != null)
            StopCoroutine(adAudioRoutine);

        adAudioRoutine = StartCoroutine(RestoreAudioAfterRewarded());
    }

    private IEnumerator RestoreAudioAfterRewarded()
    {
        var callback = rewardedCallback;
        var earned = rewardedAdEarned;
        AudioListener.pause = audioWasPausedBeforeInterstitial;
        yield return FadeAudioVolume(audioVolumeBeforeInterstitial, AdAudioFadeInSeconds);
        rewardedCallback = null;
        rewardedAdEarned = false;
        rewardedPresentationInProgress = false;
        adAudioRoutine = null;
        if (earned)
            callback?.Invoke();
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
        var consentUpdateFailed = error != null;
        if (error != null)
            Debug.LogWarning("[Ads] Consent information update failed: " + error.Message);

        ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
        {
            if (formError != null)
            {
                Debug.LogWarning("[Ads] Consent form failed: " + formError.Message);
                ScheduleConsentRetry();
            }

            CanRequestAds = ConsentInformation.CanRequestAds();
            if (CanRequestAds)
            {
                consentRetryCount = 0;
                InitializeMobileAds();
            }
            else if (consentUpdateFailed)
            {
                ScheduleConsentRetry();
            }
        });
    }

    private void InitializeMobileAds()
    {
        if (IsReady || !CanRequestAds)
            return;

        MobileAds.Initialize(_ =>
        {
            IsReady = true;
            LoadInterstitial();
            LoadRewarded();
            if (bannerRequested)
                LoadBanner();
        });
    }

    private void ScheduleConsentRetry()
    {
        if (IsReady || consentRetryRoutine != null || consentRetryCount >= MaxConsentRetries)
            return;

        consentRetryRoutine = StartCoroutine(RetryConsentFlow());
    }

    private IEnumerator RetryConsentFlow()
    {
        var retryNumber = consentRetryCount++;
        var delaySeconds = Mathf.Min(30f, Mathf.Pow(2f, retryNumber));
        yield return new WaitForSecondsRealtime(delaySeconds);

        consentRetryRoutine = null;
        if (!IsReady)
        {
            consentFlowStarted = false;
            BeginConsentFlow();
        }
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
                EndInterstitialAudioProtection();
                LoadInterstitial();
            };
            interstitial.OnAdFullScreenContentFailed += _ =>
            {
                interstitial = null;
                EndInterstitialAudioProtection();
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

    private void LoadRewarded()
    {
        if (!IsReady || !CanRequestAds)
            return;

        var adUnitId = CurrentRewardedId();
        if (string.IsNullOrWhiteSpace(adUnitId))
        {
            Debug.LogWarning("[Ads] Rewarded unit ID is not configured.");
            return;
        }

        rewarded?.Destroy();
        RewardedAd.Load(adUnitId, new AdRequest(), (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning("[Ads] Rewarded load failed: " + error);
                return;
            }

            rewarded = ad;
            rewarded.OnAdFullScreenContentClosed += () =>
            {
                rewarded = null;
                EndRewardedAudioProtection();
                LoadRewarded();
            };
            rewarded.OnAdFullScreenContentFailed += _ =>
            {
                rewarded = null;
                rewardedCallback = null;
                rewardedAdEarned = false;
                EndRewardedAudioProtection();
                rewardedPresentationInProgress = false;
                LoadRewarded();
            };
        });
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

    private string CurrentRewardedId()
    {
        if (configuration.useTestAds)
        {
#if UNITY_IOS
            return TestIosRewardedId;
#elif UNITY_ANDROID
            return TestAndroidRewardedId;
#else
            return string.Empty;
#endif
        }

#if UNITY_IOS
        return configuration.iosRewardedUnitId;
#elif UNITY_ANDROID
        return configuration.androidRewardedUnitId;
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
