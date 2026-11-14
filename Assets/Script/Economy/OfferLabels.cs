using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TreadShred.Economy
{
    public static class OfferLabels
    {
        // Plain-language copy for the result-screen offer and settings store.

        // D-036-AC-01: one price for the outcome the player asked for: the gems it spends, or the store price of the
        // pack that buys it (the pack's gems until the store reports a price).
        public static string ActionButton(string action, OfferPlan plan, string localizedPackPrice)
        {
            if (plan.CanSpendNow)
                return action + " FOR " + CountText.Format(plan.Cost) + " GEMS";
            return action + ": " + (string.IsNullOrEmpty(localizedPackPrice)
                ? "BUY " + CountText.Format(plan.PackToBuy.Gems) + " GEMS"
                : localizedPackPrice);
        }

        public static string Owned(int balance)
        {
            return "YOU HAVE " + CountText.Format(balance) + " GEMS";
        }

        public static string BalanceLine(int balance, OfferPlan plan)
        {
            if (plan.CanSpendNow)
                return Owned(balance);
            // D-037-AC-04: the button will buy the Starter Pack, so the line under it says so.
            return GemCatalog.IsStarterPack(plan.PackToBuy) ? StarterPackLine : Owned(balance) + ". NEED " + CountText.Format(plan.Cost);
        }

        // D-037: the one-time Starter Pack's copy.
        public const string StarterPackTitle = "STARTER PACK";
        public const string StarterPackLimit = "ONE TIME ONLY";
        public static readonly string StarterPackLine =
            StarterPackTitle + ": " + CountText.Format(GemCatalog.StarterPack.Gems) + " GEMS. " + StarterPackLimit;

        /// <summary>D-037-AC-07: the live countdown, rounded up so it never reads 0:00 while the pack is still on sale.</summary>
        public static string StarterPackCountdown(TimeSpan left)
        {
            var seconds = Math.Max(0, (int)Math.Ceiling(left.TotalSeconds));
            return "ENDS IN " + seconds / 60 + ":" + (seconds % 60).ToString("00");
        }

        // Spending gems the player already holds is confirmed in a dialog first.
        // When the balance is short, the store's own purchase sheet is the
        // confirmation, so the boost still applies without a second tap (D-010).
        public static bool NeedsConfirmation(OfferPlan plan)
        {
            return plan.CanSpendNow;
        }

        public static GemConfirmation Confirmation(GemOffer offer, int balance)
        {
            var cost = GemCatalog.Cost(offer);
            var title = offer == GemOffer.Overdrive ? "ACTIVATE OVERDRIVE?" : "USE ARMORED REDEPLOY?";
            return new GemConfirmation(title, "SPEND " + CountText.Format(cost) + " OF YOUR " + CountText.Format(balance) + " GEMS",
                "SPEND " + CountText.Format(cost) + " GEMS", "CANCEL");
        }

        // D-017-AC-08 and D-040: Overdrive offered on the upgrade screen and before the next mission.
        public const string ArmOverdrive = "ARM OVERDRIVE";
        public const string ForNextMission = "FOR YOUR NEXT MISSION";
        public const string OverdriveShots = "2 SHOTS PER TAP";
        public const string OverdriveArmed = "OVERDRIVE ON FOR YOUR NEXT MISSION";
        public const string Active = "ACTIVE";
        public const string NoThanksPlay = "NO THANKS, PLAY";

        // D-035-AC-06: after the first play's free Overdrive, the victory card offers to keep it.
        public const string KeepOverdrive = "KEEP OVERDRIVE";

        // D-020-AC-06: after a paid revive the game waits for the player; GO plays the 3-2-1 first.
        public const string Ready = "READY?";
        public const string Go = "GO";
        public const string NotYet = "NOT YET";

        // Overdrive waits for the next mission, so the panel must say it is on.
        public const string OverdriveActive = "OVERDRIVE ACTIVE";

        // D-020-AC-05, D-009-AC-05: declining ends the run, so while there is a SCORE to lose the offers name it, and the
        // way out says what it costs. Nothing else is taken away.
        public const string ReviveWithFullArmor = "REVIVE NOW WITH FULL ARMOR";
        public const string NoThanks = "NO THANKS";
        public const string EndRun = "END RUN: SCORE BACK TO 0";

        public static string ReviveHeadline(int runScore) =>
            runScore > 0 ? "YOUR " + CountText.Format(runScore) + " SCORE RUN ENDS HERE" : ReviveWithFullArmor;

        public static string ReviveAction(int runScore) => runScore > 0 ? "REVIVE AND KEEP IT" : "REVIVE";

        public static string ReviveDecline(int runScore) => runScore > 0 ? EndRun : NoThanks;

        public static string RedeployKeeps(int scoreKept) =>
            CombatBoosts.RedeployExtraHits + " EXTRA HIT. KEEP YOUR " + CountText.Format(scoreKept) + " SCORE";

        public static string RetryCaptionFor(int scoreKept) => scoreKept > 0 ? EndRun : RetryCaption;

        // The free retry on the result screens. The mission scenes label it with the paid offer's name, which
        // made Armored Redeploy look like a worse deal than a free button; missions relabel it on load.
        public const string Retry = "RETRY";
        public const string SceneRetryLabel = "REDEPLOY";
        // A retry ends the SCORE run; the failure screen says so under RETRY (D-032-AC-06).
        public const string RetryCaption = "SCORE STARTS OVER";

        /// <summary>D-045: how long a unit of supply lasts, Overdrive included: 5 missions won, longer while other supply
        /// takes its turn in the rotation.</summary>
        public const string SupplyLasts = "FOR AT LEAST 5 MISSIONS";
        public const string OverdriveLasts = SupplyLasts;
        public const string OverdriveEffect = "2 SHOTS PER TAP " + OverdriveLasts;

        /// <summary>
        /// The victory screen while Overdrive runs: just bought (gems spent), or carried from earlier (0). A status only:
        /// the card's description already states the effect.
        /// </summary>
        public static string OverdriveActiveDetail(int gemsSpent)
        {
            return gemsSpent > 0 ? CountText.Format(gemsSpent) + " GEMS SPENT" : "ON FROM AN EARLIER MISSION";
        }

        /// <summary>D-009-AC-03: the line that says Overdrive is on, in place of a button that would sell nothing.</summary>
        public static string OverdriveStatus(int gemsSpent) => OverdriveActive + "  |  " + OverdriveActiveDetail(gemsSpent);

        /// <summary>D-040-AC-05: why to take the pack beside an offer the player can already cover.</summary>
        public const string StockUp = "MORE GEMS FOR OVERDRIVE, REDEPLOYS AND UPGRADES";

        public static string GemsAdded(GemPack pack) => "+" + CountText.Format(pack.Gems) + " GEMS ADDED";

        public static string Pack(GemPack pack, string localizedPrice, string prefix = "")
        {
            var label = prefix + CountText.Format(pack.Gems) + " GEMS";
            return string.IsNullOrEmpty(localizedPrice) ? label : label + " FOR " + localizedPrice;
        }

        /// <summary>A store price written in US dollars, a dollar sign and two decimals, as its amount; false for any other format.</summary>
        public static bool TryDollars(string price, out decimal amount)
        {
            amount = 0m;
            var match = price == null ? null : Regex.Match(price, @"^\$(\d+\.\d{2})$");
            return match != null && match.Success &&
                decimal.TryParse(match.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
        }
    }
}
