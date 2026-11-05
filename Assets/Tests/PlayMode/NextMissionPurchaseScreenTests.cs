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
using UnityEngine.UI;

namespace TreadShred.Screens.Tests
{
    // D-040-AC-05 on the real screen: every NEXT MISSION offer has something to buy with money.
    public sealed class NextMissionPurchaseScreenTests
    {
        private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static object Singleton(string type) => GameType(type).GetMethod("Ensure").Invoke(null, null);

        private static GemWallet Wallet => (GemWallet)Singleton("TreadShredStore").GetType().GetProperty("Wallet").GetValue(Singleton("TreadShredStore"));

        private static PlayerProgression Progression => (PlayerProgression)Singleton("TreadShredProgress").GetType().GetProperty("Progression").GetValue(Singleton("TreadShredProgress"));

        private static CombatBoosts Boosts => (CombatBoosts)GameType("TreadShredBoosts").GetProperty("Current").GetValue(null);

        private static List<string> Texts() =>
            UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t => t.gameObject.activeInHierarchy).Select(t => t.text).ToList();

        private static Button ButtonStarting(string label) =>
            UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .FirstOrDefault(b => b.gameObject.activeInHierarchy && (b.GetComponentInChildren<Text>()?.text ?? string.Empty).StartsWith(label, StringComparison.Ordinal));

        private static IEnumerator OfferAfterVictory(int gems, int coins)
        {
            Wallet.Replace(new WalletState { balance = gems, starterGranted = true, starterPackBought = true });
            Progression.Replace(new ProgressionState { coins = coins, clearedMissions = new[] { 1L, 0L, 0L } });
            Time.timeScale = 1f;
            SceneManager.LoadScene("Lv1");
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                yield return new WaitForSecondsRealtime(0.25f);
            var manager = UnityEngine.Object.FindFirstObjectByType(GameType("ManagerScore"));
            manager.GetType().GetField("CountEmnemy", Members).SetValue(manager, 1);
            manager.GetType().GetMethod("Victory", Members).Invoke(manager, null);
            yield return new WaitForSecondsRealtime(3.5f);
            var buttons = UnityEngine.Object.FindFirstObjectByType(GameType("ManagerButton"));
            buttons.GetType().GetMethod("NextLv").Invoke(buttons, null);
            yield return null;
            Assert.IsNotNull(GameObject.Find("TreadShredNextMissionOffer"), "an offer before the next mission");
        }

        private static IEnumerator Audit(string screen, List<Finding> findings)
        {
            foreach (var shape in ScreenAudit.Shapes)
            {
                yield return ScreenAudit.Present(shape);
                findings.AddRange(ScreenAudit.Inspect(shape, screen));
            }
            UnityEngine.Object.Destroy(GameObject.Find("TreadShredNextMissionOffer"));
            yield return null;
        }

        [UnityTest(Description = "D-040-AC-05: with Overdrive covered by held gems, or an upgrade by held coins, the offer adds the smallest pack; with no gems its own button is the purchase and no pack is added; each passes the screen audit on every shape")]
        public IEnumerator EveryOfferHasSomethingToBuy()
        {
            var findings = new List<Finding>();
            var saved = PlayerPrefs.GetString("TreadShredGemWallet", JsonUtility.ToJson(new WalletState()));
            var pack = OfferLabels.Pack(GemCatalog.Packs[0], string.Empty);
            try
            {
                Boosts.TankLost();
                yield return OfferAfterVictory(100, 0);
                Assert.IsNotNull(ButtonStarting("ACTIVATE FOR 60 GEMS"), "Overdrive covered by held gems");
                Assert.IsNotNull(ButtonStarting(pack), "so the smallest pack is added");
                Assert.Contains(OfferLabels.StockUp, Texts());
                yield return Audit("NEXT MISSION offer: Overdrive with a pack", findings);

                Boosts.TankLost();
                yield return OfferAfterVictory(0, 0);
                Assert.IsNotNull(ButtonStarting("ACTIVATE: "), "no gems: the offer's own button buys a pack");
                Assert.IsNull(ButtonStarting(pack), "so none is added");
                CollectionAssert.DoesNotContain(Texts(), OfferLabels.StockUp);
                yield return Audit("NEXT MISSION offer: Overdrive that buys a pack", findings);

                Boosts.ArmOverdrive();
                yield return OfferAfterVictory(0, 1000);
                Assert.IsNotNull(ButtonStarting("UPGRADE FOR "), "an upgrade covered by held coins");
                Assert.IsNotNull(ButtonStarting(pack), "so the smallest pack is added");
                yield return Audit("NEXT MISSION offer: upgrade with a pack", findings);
            }
            finally
            {
                Boosts.TankLost();
                Wallet.Replace(JsonUtility.FromJson<WalletState>(saved));
                Progression.Replace(new ProgressionState());
                Time.timeScale = 1f;
            }
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));
        }
    }
}
