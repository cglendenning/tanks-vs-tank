using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TreadShred.Screens.Tests
{
    /// <summary>
    /// Plays through the game the way a player does (start screen, stats, settings, upgrades,
    /// leaderboard, mission select, a mission, victory, defeat, a gem purchase prompt and the
    /// revive offer) on a fresh install and on a veteran's maxed-out save, auditing every screen
    /// on every supported screen shape. Game code is reached by name because it compiles into
    /// Assembly-CSharp, which test assemblies cannot reference.
    /// </summary>
    public sealed class ScreenSweep
    {
        private const int MissionsPerTier = 40;
        private const string FirstMission = "Lv1";

        public readonly List<Finding> Findings = new List<Finding>();
        public readonly List<string> Reached = new List<string>();
        public readonly List<string> Errors = new List<string>();

        public IEnumerator Run()
        {
            LogAssert.ignoreFailingMessages = true;
            Application.logMessageReceived += Record;
            try
            {
                foreach (var veteran in new[] { false, true })
                {
                    var save = veteran ? "VETERAN" : "FRESH";
                    yield return Load("Start");
                    Seed(veteran);
                    yield return Load("Start");
                    yield return Audit(save + " START", "START MISSION");

                    Call(Find("LoadLv"), "Static");
                    yield return Settle(1.5f);
                    yield return Audit(save + " STATS", "CONFIRMED KILLS");
                    Call(Find("LoadLv"), "StaticBack");
                    yield return Settle(1.5f);

                    if (Tap("SETTINGS"))
                    {
                        yield return Settle();
                        yield return Audit(save + " SETTINGS", "VIBRATION");
                        yield return PressBack("TreadShredSettings", save + " SETTINGS");
                        yield return Settle();
                    }
                    if (Tap("UPGRADES"))
                    {
                        yield return Settle();
                        yield return Audit(save + " UPGRADES", "LEVEL ");
                        yield return PressBack("TreadShredUpgrades", save + " UPGRADES");
                        yield return Settle();
                    }
                    OpenLeaderboard(veteran);
                    yield return Settle();
                    yield return Audit(save + " LEADERBOARD", LeaderboardRules.Subtitle);
                    yield return PressBack("TreadShredLeaderboard", save + " LEADERBOARD");
                    yield return Settle();

                    if (Tap("START MISSION"))
                    {
                        // Mission select fades in from black over about two seconds.
                        yield return Settle(3f);
                        yield return Audit(save + " MISSIONS", "MISSION 1");
                    }

                    yield return StartMission();
                    yield return Audit(save + " MISSION", "SCORE:");
                    // Victory() counts down one enemy per call; the last enemy ends the mission.
                    var manager = Find("ManagerScore");
                    SetMember(manager, "CountEmnemy", 1);
                    Call(manager, "Victory");
                    yield return Settle(3.5f);
                    yield return Audit(save + " VICTORY", "OVERDRIVE");

                    yield return StartMission();
                    Call(Find("ManagerScore"), "Failure");
                    yield return Settle(3.5f);
                    yield return Audit(save + " DEFEAT", "ARMORED REDEPLOY");
                    // The gem offer, not the plain REDEPLOY retry beside it.
                    if (TapWhere(label => label.StartsWith("REDEPLOY FOR"), "'REDEPLOY FOR <cost> GEMS'"))
                    {
                        yield return Settle();
                        yield return Audit(save + " GEM PROMPT", "SPEND ");
                        yield return PressBack("TreadShredConfirm", save + " GEM PROMPT");
                    }

                    yield return StartMission();
                    Call(null, "TreadShredRevivePanel.TryOffer", Find("PlayerMoverment"));
                    yield return Settle();
                    yield return Audit(save + " REVIVE", "TANK DESTROYED");
                    yield return PressBack("TreadShredRevive", save + " REVIVE");
                }
            }
            finally
            {
                Application.logMessageReceived -= Record;
                LogAssert.ignoreFailingMessages = false;
                Time.timeScale = 1f;
            }
        }

        private void Record(string message, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error)
                Errors.Add(type + ": " + message);
        }

        // Audits the screen on every shape; a screen that does not show its signature label was not reached.
        private IEnumerator Audit(string screen, string signature)
        {
            foreach (var shape in ScreenAudit.Shapes)
            {
                yield return ScreenAudit.Present(shape);
                Findings.AddRange(ScreenAudit.Inspect(shape, screen));
                if (!ScreenAudit.Shown.Any(label => label.StartsWith(signature)))
                {
                    Findings.Add(new Finding(Defect.NotReached, "[" + screen + ", " + shape.Name + "] did not show '" + signature +
                        "'; it showed: " + string.Join(" | ", ScreenAudit.Shown.Take(12)) +
                        "; hidden: " + string.Join(" | ", ScreenAudit.Hidden.Take(6))));
                    yield break;
                }
            }
            Reached.Add(screen);
        }

        private static IEnumerator Load(string scene)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
            yield return null;
            yield return Settle(2f);
        }

        // Loads the first mission and waits out its countdown (which holds game time at zero) as a player does.
        private IEnumerator StartMission()
        {
            yield return Load(FirstMission);
            for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                yield return new WaitForSecondsRealtime(0.25f);
            if (Time.timeScale == 0f)
                Findings.Add(new Finding(Defect.NotReached, "the mission countdown never finished"));
        }

        // D-014-AC-03: the system Back (Escape on Android) closes the open panel, as its own button does.
        private IEnumerator PressBack(string panel, string screen)
        {
            Call(null, "TreadShredBack.Press");
            yield return null;
            yield return null;
            if (GameObject.Find(panel) != null)
                Findings.Add(new Finding(Defect.BackDoesNotClose, "[" + screen + "] the system Back left " + panel + " open"));
        }

        private static IEnumerator Settle(float seconds = 0.6f)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        // The save a fresh install has, or a veteran's: every upgrade maxed, every mission on every
        // tier cleared, HIGH SCORE at the ceiling, and five-figure gem and kill counts.
        private void Seed(bool veteran)
        {
            var wallet = (GemWallet)Member(Call(null, "TreadShredStore.Ensure"), "Wallet");
            var progression = (PlayerProgression)Member(Call(null, "TreadShredProgress.Ensure"), "Progression");
            if (wallet == null || progression == null)
            {
                Findings.Add(new Finding(Defect.NotReached, "the wallet or progression could not be seeded"));
                return;
            }
            if (!veteran)
            {
                progression.Replace(new ProgressionState());
                wallet.Replace(new WalletState { balance = GemCatalog.StarterGrant, starterGranted = true });
                PlayerPrefs.DeleteKey("TreadShredBestRun");
                PlayerPrefs.DeleteKey("CountKill");
                return;
            }
            progression.Replace(new ProgressionState { coins = int.MaxValue / 4 });
            foreach (UpgradeTrack track in Enum.GetValues(typeof(UpgradeTrack)))
            for (var i = 0; i < 100; i++)
                progression.TryUpgrade(track);
            foreach (DifficultyTier tier in Enum.GetValues(typeof(DifficultyTier)))
            for (var mission = 1; mission <= MissionsPerTier; mission++)
                progression.CompleteMission(tier, mission, true);
            wallet.Replace(new WalletState { balance = 99999, starterGranted = true });
            PlayerPrefs.SetInt("TreadShredBestRun", ScoreRules.MaxRunScore);
            PlayerPrefs.SetFloat("CountKill", 99999f);
        }

        // An empty board on a fresh install; a full board of widest-possible names with the player below it for a veteran.
        private void OpenLeaderboard(bool veteran)
        {
            var name = new string('W', LeaderboardPage.MaxNameLength);
            var rows = veteran
                ? Enumerable.Range(1, LeaderboardPage.TopCount)
                    .Select(rank => new LeaderboardEntry(rank, name, ScoreRules.MaxRunScore - rank, false)).ToList()
                : new List<LeaderboardEntry>();
            LeaderboardEntry? player = veteran ? new LeaderboardEntry(1234567, name, 200000, true) : (LeaderboardEntry?)null;
            var font = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(t => t.font).FirstOrDefault(f => f != null && f.name.Replace(" ", string.Empty).StartsWith("BlackOps"));
            Call(null, "TreadShredLeaderboardPanel.OpenWithPage", font, new LeaderboardPage(rows, player));
        }

        private bool Tap(string label) => TapWhere(text => text == label, "'" + label + "'");

        // Taps the button with that label on the top-most screen, as a player would.
        private bool TapWhere(Func<string, bool> matches, string described)
        {
            var button = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Where(b => b.isActiveAndEnabled && b.interactable)
                .Where(b => b.GetComponentInChildren<Text>() is Text text && matches(Clean(text.text)))
                .OrderByDescending(b => b.GetComponentInParent<Canvas>() is Canvas canvas ? canvas.rootCanvas.sortingOrder : 0)
                .FirstOrDefault();
            if (button == null)
            {
                Findings.Add(new Finding(Defect.NotReached, "no button " + described + " on " + SceneManager.GetActiveScene().name));
                return false;
            }
            button.onClick.Invoke();
            return true;
        }

        private static string Clean(string text) => Regex.Replace(text ?? string.Empty, "\\s+", " ").Trim();

        private static Type GameType(string name) => Type.GetType(name + ", Assembly-CSharp");

        private static UnityEngine.Object Find(string type)
        {
            var found = GameType(type);
            return found == null ? null : UnityEngine.Object.FindFirstObjectByType(found);
        }

        // Calls a method on target, or "Type.Method" statically when target is null; a failure is a finding, never silent.
        private object Call(object target, string method, params object[] args)
        {
            var type = target?.GetType() ?? GameType(method.Substring(0, method.LastIndexOf('.')));
            var name = target == null ? method.Substring(method.LastIndexOf('.') + 1) : method;
            var flags = BindingFlags.Public | BindingFlags.NonPublic | (target == null ? BindingFlags.Static : BindingFlags.Instance);
            var found = type?.GetMethods(flags).FirstOrDefault(m => m.Name == name && m.GetParameters().Length == args.Length);
            if (found == null)
            {
                Findings.Add(new Finding(Defect.NotReached, "could not call " + (type?.Name ?? method) + "." + name +
                    (target == null && type != null ? string.Empty : " (target missing on " + SceneManager.GetActiveScene().name + ")")));
                return null;
            }
            try
            {
                return found.Invoke(target, args);
            }
            catch (TargetInvocationException error)
            {
                Findings.Add(new Finding(Defect.NotReached, type.Name + "." + name + " threw " + error.InnerException));
                return null;
            }
        }

        private void SetMember(object target, string member, object value)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var field = target?.GetType().GetField(member, flags);
            if (field == null)
                Findings.Add(new Finding(Defect.NotReached, "could not set " + member + " on " + (target?.GetType().Name ?? "a missing object")));
            else
                field.SetValue(target, value);
        }

        private static object Member(object target, string member)
        {
            if (target == null)
                return null;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var type = target.GetType();
            return type.GetProperty(member, flags)?.GetValue(target) ?? type.GetField(member, flags)?.GetValue(target);
        }
    }
}
