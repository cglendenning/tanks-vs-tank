using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TreadShred.Screens.Tests
{
    // D-050 on the real screens: a first install's first five missions ask for nothing; from mission 6 on, as today.
    public sealed class AskFreeStartScreenTests
    {
        private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private const string ProgressionKey = "TreadShredProgression";
        private const string WalletKey = "TreadShredGemWallet";

        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static object Singleton(string type) => GameType(type).GetMethod("Ensure").Invoke(null, null);

        private static PlayerProgression Progression =>
            (PlayerProgression)Singleton("TreadShredProgress").GetType().GetProperty("Progression").GetValue(Singleton("TreadShredProgress"));

        private static GemWallet Wallet =>
            (GemWallet)Singleton("TreadShredStore").GetType().GetProperty("Wallet").GetValue(Singleton("TreadShredStore"));

        private static bool StarterPackOnSale =>
            (bool)Singleton("TreadShredStore").GetType().GetProperty("StarterPackOnSale").GetValue(Singleton("TreadShredStore"));

        private static bool MissionAskFree => (bool)GameType("TreadShredAskFreeStart").GetProperty("MissionAskFree").GetValue(null);

        private static FieldInfo Preview => GameType("TreadShredAskFreeStart").GetField("editorPreview", BindingFlags.NonPublic | BindingFlags.Static);

        private static object Manager() => UnityEngine.Object.FindFirstObjectByType(GameType("ManagerScore"));

        // The result screen's offer card: its buy button, or null when the screen has no card.
        private static Button OfferCard()
        {
            var button = (Button)Manager().GetType().GetField("rewardButton", Members).GetValue(Manager());
            return button != null && button.gameObject.activeInHierarchy ? button : null;
        }

        private static List<string> Texts() =>
            UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t => t.gameObject.activeInHierarchy).Select(t => t.text).ToList();

        private static Button ButtonStarting(string label) =>
            UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .FirstOrDefault(b => b.gameObject.activeInHierarchy && (b.GetComponentInChildren<Text>()?.text ?? string.Empty).StartsWith(label, StringComparison.Ordinal));

        // A first install that has cleared missions 1 to `last` on Recruit, with its Starter Pack window not yet open.
        private static void FirstInstallThrough(int last)
        {
            var state = new ProgressionState();
            state.clearedMissions[(int)DifficultyTier.Recruit] = (1L << last) - 1;
            Progression.Replace(state);
            var progress = Singleton("TreadShredProgress");
            progress.GetType().GetMethod("SelectTier").Invoke(progress, new object[] { DifficultyTier.Recruit });
            Wallet.Replace(new WalletState { balance = 100, starterGranted = true });
            // A restore never closes a window this install opened (D-037-AC-02), so a first install's unopened one is set here.
            var saved = (WalletState)typeof(GemWallet).GetField("state", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Wallet);
            saved.starterPackWindowStartTicks = 0;
        }

        private static IEnumerator Play(string scene)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                yield return new WaitForSecondsRealtime(0.25f);
        }

        private static IEnumerator Win()
        {
            var manager = Manager();
            manager.GetType().GetField("CountEmnemy", Members).SetValue(manager, 1);
            manager.GetType().GetMethod("Victory", Members).Invoke(manager, null);
            yield return new WaitForSecondsRealtime(3.5f);
        }

        private static IEnumerator TapNext()
        {
            var buttons = UnityEngine.Object.FindFirstObjectByType(GameType("ManagerButton"));
            buttons.GetType().GetMethod("NextLv").Invoke(buttons, null);
            yield return null;
            Assert.IsNull(GameObject.Find("TreadShredNextMissionOffer"), "NEXT MISSION opens no offer");
            yield return new WaitForSecondsRealtime(2f);
            // The next mission's 3-2-1 countdown holds game time; play starts when it ends.
            for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                yield return new WaitForSecondsRealtime(0.25f);
        }

        // Runs a test with the ask-free start turned on, then puts progress and the wallet back as they were.
        private static IEnumerator AskFree(Func<IEnumerator> body)
        {
            var progress = PlayerPrefs.GetString(ProgressionKey, string.Empty);
            var wallet = PlayerPrefs.GetString(WalletKey, JsonUtility.ToJson(new WalletState()));
            Preview.SetValue(null, true);
            try
            {
                yield return body();
            }
            finally
            {
                Preview.SetValue(null, false);
                Progression.Replace(string.IsNullOrEmpty(progress) ? new ProgressionState() : JsonUtility.FromJson<ProgressionState>(progress));
                Wallet.Replace(JsonUtility.FromJson<WalletState>(wallet));
                Time.timeScale = 1f;
            }
        }

        [UnityTest(Description = "D-050-AC-01: during the ask-free start a victory screen has no offer card, NEXT MISSION loads the next mission with no offer, no Starter Pack or War Chest opens, and UPGRADES stays")]
        public IEnumerator AVictoryAsksNothing() => AskFree(VictoryAsksNothing);

        private static IEnumerator VictoryAsksNothing()
        {
            FirstInstallThrough(0);
            yield return Play("Lv1");
            Assert.IsTrue(MissionAskFree, "a first install's mission 1");
            yield return Win();
            var texts = Texts();
            Assert.IsNull(OfferCard(), "no Overdrive or upgrade card");
            CollectionAssert.DoesNotContain(texts, OfferLabels.KeepOverdrive);
            CollectionAssert.DoesNotContain(texts, "OVERDRIVE");
            Assert.IsNotNull(ButtonStarting(ProgressionLabels.UpgradesButton), "UPGRADES stays");
            Assert.IsTrue(texts.Any(t => t.StartsWith("SCORE", StringComparison.Ordinal)), "the score stays");
            Assert.IsNull(GameObject.Find("TreadShredStarterOffer"), "no Starter Pack");
            Assert.IsNull(GameObject.Find("TreadShredWarChestOffer"), "no War Chest");
            Assert.AreEqual(TimeSpan.Zero, Wallet.StarterPackTimeLeft(DateTime.UtcNow), "D-050-AC-04: its window stays closed during the ask-free start");
            Assert.IsFalse(StarterPackOnSale);
            yield return TapNext();
            Assert.AreEqual("Lv2", SceneManager.GetActiveScene().name, "NEXT MISSION loads the next mission");
        }

        [UnityTest(Description = "D-050-AC-01: during the ask-free start a lost tank gets no revive offer, and the failure screen has no Armored Redeploy or War Chest, only the free RETRY and UPGRADES")]
        public IEnumerator ALossAsksNothing() => AskFree(LossAsksNothing);

        private static IEnumerator LossAsksNothing()
        {
            FirstInstallThrough(0);
            yield return Play("Lv1");
            var player = UnityEngine.Object.FindFirstObjectByType(GameType("PlayerMoverment"));
            Assert.IsFalse((bool)GameType("TreadShredRevivePanel").GetMethod("TryOffer").Invoke(null, new object[] { player }), "no revive offer");
            Manager().GetType().GetMethod("Failure", Members).Invoke(Manager(), null);
            yield return new WaitForSecondsRealtime(3.5f);
            Assert.IsNull(OfferCard(), "no Armored Redeploy");
            Assert.IsNull(GameObject.Find("TreadShredWarChestOffer"), "no War Chest");
            var retry = GameObject.Find("ButtonRetry");
            Assert.IsTrue(retry != null && retry.activeInHierarchy, "the free RETRY");
            Assert.IsNotNull(ButtonStarting(ProgressionLabels.UpgradesButton), "UPGRADES stays");
            Assert.IsTrue(Texts().Any(t => t.StartsWith("SCORE", StringComparison.Ordinal)), "the score stays");
        }

        [UnityTest(Description = "D-050-AC-02: mission 5's own victory screen asks nothing, and from mission 6 every screen asks as for any player, with the Starter Pack's window opening then (D-050-AC-04)")]
        public IEnumerator MissionSixAsksAsToday() => AskFree(SixAsksAsToday);

        private static IEnumerator SixAsksAsToday()
        {
            FirstInstallThrough(4);
            yield return Play("Lv5");
            Assert.IsTrue(MissionAskFree, "mission 5 starts ask-free");
            yield return Win();
            Assert.IsTrue(Progression.IsCleared(DifficultyTier.Recruit, 5), "mission 5 cleared");
            Assert.IsNull(OfferCard(), "mission 5's own victory screen asks nothing");
            yield return TapNext();
            Assert.AreEqual("Lv6", SceneManager.GetActiveScene().name);
            Assert.IsFalse(MissionAskFree, "mission 6 plays as for any player");
            Assert.Greater(Wallet.StarterPackTimeLeft(DateTime.UtcNow), TimeSpan.Zero, "the Starter Pack's window opened as the ask-free start ended");
            yield return Win();
            Assert.IsNotNull(OfferCard(), "the victory card is back");
        }

        [UnityTest(Description = "D-050-AC-04: the Starter Pack's window stays closed through the ask-free start, mission 5's own victory included, and its full 15 minutes start with the first screen after it")]
        public IEnumerator StarterPackWindowOpensWhenTheStartEnds() => AskFree(WindowOpensWhenTheStartEnds);

        private static IEnumerator WindowOpensWhenTheStartEnds()
        {
            FirstInstallThrough(4);
            yield return Play("Lv5");
            Assert.AreEqual(TimeSpan.Zero, Wallet.StarterPackTimeLeft(DateTime.UtcNow), "closed while the start is ask-free");
            yield return Win();
            Assert.AreEqual(TimeSpan.Zero, Wallet.StarterPackTimeLeft(DateTime.UtcNow), "still closed on mission 5's own victory screen");
            yield return Play("Start");
            Assert.Greater(Wallet.StarterPackTimeLeft(DateTime.UtcNow), TimeSpan.FromMinutes(14), "its full 15 minutes start on the next screen");
        }
    }
}
