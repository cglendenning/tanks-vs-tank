using System;
using System.Collections;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// D-033: counts real play and, on a victory screen, asks the platform for its native review prompt: StoreKit on
/// iOS, Google Play In-App Review on Android. The game draws nothing of its own for it; the stores' prompts show
/// the stars and the optional written review. Nothing is given for rating.
/// </summary>
public sealed class TreadShredReviewPrompt : MonoBehaviour
{
    internal const string StateKey = "TreadShredReviewPrompt";

    private sealed class PlayerPrefsStore : IReviewPromptStore
    {
        public ReviewPromptState Load()
        {
            var json = PlayerPrefs.GetString(StateKey, string.Empty);
            return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<ReviewPromptState>(json);
        }

        public void Save(ReviewPromptState state)
        {
            PlayerPrefs.SetString(StateKey, JsonUtility.ToJson(state));
            PlayerPrefs.Save();
        }
    }

    private static TreadShredReviewPrompt instance;
    private ReviewPrompt prompt;
    private bool missionRunning;

    private static TreadShredReviewPrompt Ensure()
    {
        if (instance != null)
            return instance;
        var host = new GameObject("Tread Shred Review Prompt");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<TreadShredReviewPrompt>();
        instance.prompt = new ReviewPrompt(new PlayerPrefsStore());
        return instance;
    }

    public static void MissionStarted()
    {
        var self = Ensure();
        self.prompt.MissionPlayed(DateTime.Now);
        self.missionRunning = true;
    }

    public static void MissionEnded(bool won)
    {
        var self = Ensure();
        if (!self.missionRunning)
            return;
        self.missionRunning = false;
        self.prompt.MissionEnded(won);
    }

    /// <summary>A victory result screen opened: ask after the delay if the player has played enough (D-033-AC-03).</summary>
    public static void VictoryScreenOpened()
    {
        var self = Ensure();
        self.StartCoroutine(self.AskAfterDelay(SceneManager.GetActiveScene().handle));
    }

    /// <summary>D-033-AC-05: the Settings button goes straight to the store's review page.</summary>
    public static void OpenWriteReviewPage()
    {
#if UNITY_ANDROID
        Application.OpenURL(ReviewRules.AndroidStoreUrl);
#else
        Application.OpenURL(ReviewRules.IosWriteReviewUrl);
#endif
    }

    // Game time stands still while paused and Update stops in the background, so only active play counts.
    private void Update()
    {
        if (missionRunning)
            prompt.AddActiveTime(Time.deltaTime);
    }

    private IEnumerator AskAfterDelay(int scene)
    {
        yield return new WaitForSecondsRealtime(ReviewRules.ResultScreenDelaySeconds);
        // The player already moved on (next mission, retry, command): never ask mid-mission.
        if (SceneManager.GetActiveScene().handle != scene)
            yield break;
        if (!prompt.MayRequest(DateTime.UtcNow, Application.version, TreadShredStore.Ensure().PurchaseInProgress))
            yield break;
        prompt.RequestSent(DateTime.UtcNow, Application.version);
        RequestNativePrompt();
    }

    private static void RequestNativePrompt()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (!UnityEngine.iOS.Device.RequestStoreReview())
            Debug.LogWarning("[Review] StoreKit review request is not available on this device.");
#elif UNITY_ANDROID && !UNITY_EDITOR
        PlayReview.Request();
#else
        Debug.Log("[Review] Native review prompt requested (no store in the editor).");
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    // Google Play In-App Review (com.google.android.play:review): request the flow, then launch its card as-is.
    private static class PlayReview
    {
        private sealed class Completed : AndroidJavaProxy
        {
            private readonly Action<AndroidJavaObject> handler;

            public Completed(Action<AndroidJavaObject> handler) : base("com.google.android.gms.tasks.OnCompleteListener")
            {
                this.handler = handler;
            }

            // Named as the Java interface method, so the Play task calls it.
            public void onComplete(AndroidJavaObject task) => handler(task);
        }

        public static void Request()
        {
            try
            {
                var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
                var manager = new AndroidJavaClass("com.google.android.play.core.review.ReviewManagerFactory")
                    .CallStatic<AndroidJavaObject>("create", activity);
                manager.Call<AndroidJavaObject>("requestReviewFlow").Call<AndroidJavaObject>("addOnCompleteListener",
                    new Completed(request =>
                    {
                        if (!request.Call<bool>("isSuccessful"))
                        {
                            Debug.LogWarning("[Review] Play In-App Review flow is not available.");
                            return;
                        }
                        manager.Call<AndroidJavaObject>("launchReviewFlow", activity, request.Call<AndroidJavaObject>("getResult"));
                    }));
            }
            catch (AndroidJavaException error)
            {
                Debug.LogError("[Review] Play In-App Review failed: " + error.Message);
            }
        }
    }
#endif
}
