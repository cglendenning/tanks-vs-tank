using System;
using System.Collections;
using System.Runtime.InteropServices;
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
    private bool rewardedPresentationOpened;
    private bool rewardedPresentationFinalized;
    private bool trackingAuthorizationStarted;
    private bool trackingAuthorizationCompleted;
    private Action rewardedCallback;
    private Coroutine adAudioRoutine;
    private Coroutine consentRetryRoutine;
    private Coroutine startupConsentRoutine;
    private Coroutine rewardedFocusRecoveryRoutine;
    private Coroutine rewardedWatchdogRoutine;
    private int consentRetryCount;
    private float lastInterstitialTime = -999f;

    private const float MinimumInterstitialIntervalSeconds = 90f;
    // Keep the completed-mission scene audible while the ad transition starts,
    // then give the player a deliberate one-second audio ramp instead of an
    // abrupt cut immediately before the native ad is presented.
    private const float AdAudioFadeOutSeconds = 1f;
    private const float AdAudioFadeInSeconds = 0.18f;
    private const float RewardedFocusRecoveryDelaySeconds = 0.75f;
    private const float RewardedWatchdogSeconds = 60f;
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

        // Do not enter native consent/WebView code during the first frame.
        // A device can safely launch the game even when the ad SDK, consent
        // service, or its network is unavailable.
        startupConsentRoutine = StartCoroutine(BeginConsentFlowAfterStartup());
    }

    private IEnumerator BeginConsentFlowAfterStartup()
    {
        yield return null;
        yield return new WaitForSecondsRealtime(1f);

#if UNITY_IOS && !UNITY_EDITOR
        // Apple requires the ATT decision before any SDK can collect data that
        // may be used for tracking. Keep this ahead of UMP and AdMob startup,
        // and fail open after a short timeout so a native prompt/network issue
        // can never block the game itself.
        yield return RequestTrackingAuthorization();
#endif

        startupConsentRoutine = null;

        try
        {
            BeginConsentFlow();
        }
        catch (Exception error)
        {
            Debug.LogWarning("[Ads] Startup initialization skipped: " + error.Message);
            CanRequestAds = false;
            ScheduleConsentRetry();
        }
    }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void TreadShredRequestTrackingAuthorization();

    private IEnumerator RequestTrackingAuthorization()
    {
        if (trackingAuthorizationCompleted)
            yield break;

        trackingAuthorizationStarted = true;
        try
        {
            TreadShredRequestTrackingAuthorization();
        }
        catch (Exception error)
        {
            Debug.LogWarning("[Ads] ATT request unavailable: " + error.Message);
            trackingAuthorizationCompleted = true;
            yield break;
        }

        var elapsed = 0f;
        while (!trackingAuthorizationCompleted && elapsed < 8f)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Never hold up gameplay if iOS fails to deliver the native callback.
        trackingAuthorizationCompleted = true;
    }

    // Called from the native iOS bridge after the ATT prompt (or immediately
    // when iOS has already recorded a decision).
    public void OnTrackingAuthorizationCompleted(string _)
    {
        if (trackingAuthorizationStarted)
            trackingAuthorizationCompleted = true;
    }
#endif

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

        bool canShowInterstitial;
        try
        {
            canShowInterstitial = interstitial != null && interstitial.CanShowAd();
        }
        catch (Exception error)
        {
            Debug.LogWarning("[Ads] Interstitial availability check failed: " + error.Message);
            canShowInterstitial = false;
        }

        if (!canShowInterstitial)
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

        bool canShowRewarded;
        try
        {
            canShowRewarded = rewarded != null && rewarded.CanShowAd();
        }
        catch (Exception error)
        {
            Debug.LogWarning("[Ads] Rewarded availability check failed: " + error.Message);
            canShowRewarded = false;
        }

        if (!canShowRewarded)
        {
            LoadRewarded();
            return false;
        }

        rewardedPresentationInProgress = true;
        rewardedAdEarned = false;
        rewardedPresentationOpened = false;
        rewardedPresentationFinalized = false;
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
            // Some iOS creatives transition between test ads without raising
            // every intermediate presentation callback. Mark the native
            // presentation before Show so app-return recovery can still
            // release our state if the final close callback is lost.
            rewardedPresentationOpened = true;
            rewardedWatchdogRoutine = StartCoroutine(RewardedPresentationWatchdog());
            ad.Show(_ => rewardedAdEarned = true);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            rewarded = null;
            FinalizeRewardedPresentation(false);
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
        AudioListener.pause = audioWasPausedBeforeInterstitial;
        yield return FadeAudioVolume(audioVolumeBeforeInterstitial, AdAudioFadeInSeconds);
        // Read this after the fade. Google normally delivers the reward before
        // close, but keeping the value live also handles SDKs that deliver the
        // two callbacks in the opposite order.
        var earned = rewardedAdEarned;
        rewardedCallback = null;
        rewardedAdEarned = false;
        rewardedPresentationOpened = false;
        rewardedPresentationInProgress = false;
        adAudioRoutine = null;
        if (earned)
            callback?.Invoke();
    }

    private void FinalizeRewardedPresentation(bool allowReward)
    {
        if (rewardedPresentationFinalized)
            return;

        rewardedPresentationFinalized = true;
        if (!allowReward)
            rewardedAdEarned = false;

        if (rewardedFocusRecoveryRoutine != null)
        {
            StopCoroutine(rewardedFocusRecoveryRoutine);
            rewardedFocusRecoveryRoutine = null;
        }

        if (rewardedWatchdogRoutine != null)
        {
            StopCoroutine(rewardedWatchdogRoutine);
            rewardedWatchdogRoutine = null;
        }

        EndRewardedAudioProtection();
        LoadRewarded();
    }

    private IEnumerator RewardedPresentationWatchdog()
    {
        yield return new WaitForSecondsRealtime(RewardedWatchdogSeconds);
        rewardedWatchdogRoutine = null;

        if (rewardedPresentationInProgress && !rewardedPresentationFinalized)
        {
            Debug.LogWarning("[Ads] Rewarded presentation exceeded the safety timeout; recovering app state without granting an unearned reward.");
            rewarded = null;
            FinalizeRewardedPresentation(false);
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus || !rewardedPresentationInProgress || !rewardedPresentationOpened ||
            rewardedPresentationFinalized || rewardedFocusRecoveryRoutine != null)
            return;

        // A native ad can return focus before the Unity plugin raises its
        // close event. Give that event a short chance to arrive, then recover
        // locally so the player is never left in a blocked presentation state.
        rewardedFocusRecoveryRoutine = StartCoroutine(RecoverRewardedPresentationAfterFocus());
    }

    private void OnApplicationPause(bool paused)
    {
        if (!paused)
            OnApplicationFocus(true);
    }

    private IEnumerator RecoverRewardedPresentationAfterFocus()
    {
        yield return new WaitForSecondsRealtime(RewardedFocusRecoveryDelaySeconds);
        rewardedFocusRecoveryRoutine = null;

        if (rewardedPresentationInProgress && rewardedPresentationOpened &&
            !rewardedPresentationFinalized)
        {
            Debug.LogWarning("[Ads] App regained focus before RewardedAd close callback; recovering presentation state.");
            rewarded = null;
            FinalizeRewardedPresentation(rewardedAdEarned);
        }
    }

    public void ShowPrivacyOptionsForm()
    {
        if (!IsReady)
            return;

        try
        {
            ConsentForm.ShowPrivacyOptionsForm(error =>
            {
                if (error != null)
                    Debug.LogWarning("[Ads] Privacy options form failed: " + error.Message);
            });
        }
        catch (Exception error)
        {
            Debug.LogWarning("[Ads] Privacy options form unavailable: " + error.Message);
        }
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

        try
        {
            ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
            {
                try
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
                }
                catch (Exception callbackError)
                {
                    Debug.LogWarning("[Ads] Consent callback failed: " + callbackError.Message);
                    CanRequestAds = false;
                    ScheduleConsentRetry();
                }
            });
        }
        catch (Exception formError)
        {
            Debug.LogWarning("[Ads] Consent form unavailable: " + formError.Message);
            CanRequestAds = false;
            ScheduleConsentRetry();
        }
    }

    private void InitializeMobileAds()
    {
        if (IsReady || !CanRequestAds)
            return;

        try
        {
            MobileAds.Initialize(_ =>
            {
                try
                {
                    IsReady = true;
                    LoadInterstitial();
                    LoadRewarded();
                    if (bannerRequested)
                        LoadBanner();
                }
                catch (Exception callbackError)
                {
                    IsReady = false;
                    Debug.LogWarning("[Ads] Mobile Ads callback failed: " + callbackError.Message);
                    ScheduleConsentRetry();
                }
            });
        }
        catch (Exception error)
        {
            IsReady = false;
            Debug.LogWarning("[Ads] Mobile Ads initialization skipped: " + error.Message);
            ScheduleConsentRetry();
        }
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
            try
            {
                BeginConsentFlow();
            }
            catch (Exception error)
            {
                Debug.LogWarning("[Ads] Consent retry skipped: " + error.Message);
                CanRequestAds = false;
                ScheduleConsentRetry();
            }
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

        try
        {
            interstitial?.Destroy();
            interstitial = null;
            InterstitialAd.Load(adUnitId, new AdRequest(), (ad, error) =>
            {
                try
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
                }
                catch (Exception callbackError)
                {
                    Debug.LogWarning("[Ads] Interstitial callback failed: " + callbackError.Message);
                }
            });
        }
        catch (Exception loadError)
        {
            Debug.LogWarning("[Ads] Interstitial request skipped: " + loadError.Message);
        }
    }

    private void LoadBanner()
    {
        if (!IsReady || !CanRequestAds)
            return;

        var adUnitId = CurrentBannerId();
        if (string.IsNullOrWhiteSpace(adUnitId))
            return;

        try
        {
            banner?.Destroy();
            banner = new BannerView(adUnitId, AdSize.Banner, AdPosition.Bottom);
            banner.LoadAd(new AdRequest());
        }
        catch (Exception error)
        {
            Debug.LogWarning("[Ads] Banner request skipped: " + error.Message);
        }
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

        try
        {
            rewarded?.Destroy();
            RewardedAd.Load(adUnitId, new AdRequest(), (ad, error) =>
            {
                try
                {
                    if (error != null || ad == null)
                    {
                        Debug.LogWarning("[Ads] Rewarded load failed: " + error);
                        return;
                    }

                    rewarded = ad;
                    rewarded.OnAdFullScreenContentOpened += () =>
                    {
                        rewardedPresentationOpened = true;
                        Debug.Log("[Ads] Rewarded full-screen content opened.");
                    };
                    rewarded.OnAdFullScreenContentClosed += () =>
                    {
                        rewarded = null;
                        Debug.Log("[Ads] Rewarded full-screen content closed.");
                        FinalizeRewardedPresentation(true);
                    };
                    rewarded.OnAdFullScreenContentFailed += error =>
                    {
                        rewarded = null;
                        Debug.LogWarning("[Ads] Rewarded full-screen content failed: " + error);
                        FinalizeRewardedPresentation(false);
                    };
                }
                catch (Exception callbackError)
                {
                    Debug.LogWarning("[Ads] Rewarded callback failed: " + callbackError.Message);
                }
            });
        }
        catch (Exception loadError)
        {
            Debug.LogWarning("[Ads] Rewarded request skipped: " + loadError.Message);
        }
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
