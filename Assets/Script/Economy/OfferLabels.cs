using System;

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

        /// <summary>D-037-AC-04: the gem store's Starter Pack button, in place of the smallest pack.</summary>
        public static string StarterPackStoreButton(string localizedPrice)
        {
            return Pack(GemCatalog.StarterPack, localizedPrice, StarterPackTitle + ": ");
        }

        // D-037-AC-03/04: why it beats the regular packs, in numbers taken from the catalog.
        public static readonly string StarterPackValue = GemCatalog.StarterPack.Gems / GemCatalog.Packs[0].Gems +
            "X THE GEMS OF THE " + CountText.Format(GemCatalog.Packs[0].Gems) + "-GEM PACK";
        public static readonly string StarterPackBuys = "THAT'S " +
            GemCatalog.StarterPack.Gems / GemCatalog.Cost(GemOffer.Overdrive) + " OVERDRIVES OR " +
            GemCatalog.StarterPack.Gems / GemCatalog.Cost(GemOffer.ArmoredRedeploy) + " ARMORED REDEPLOYS";
        public static readonly string StarterPackReviveHeadline =
            CountText.Format(GemCatalog.StarterPack.Gems) + " GEMS + THIS REVIVE FREE";

        /// <summary>D-037-AC-07: the live countdown, rounded up so it never reads 0:00 while the pack is still on sale.</summary>
        public static string StarterPackCountdown(TimeSpan left)
        {
            var seconds = Math.Max(0, (int)Math.Ceiling(left.TotalSeconds));
            return "ENDS IN " + seconds / 60 + ":" + (seconds % 60).ToString("00");
        }

        /// <summary>D-037-AC-03: the revive panel's main button, which buys the pack and revives without spending gems.</summary>
        public static string StarterPackReviveButton(string localizedPrice)
        {
            var label = "REVIVE + " + CountText.Format(GemCatalog.StarterPack.Gems) + " GEMS";
            return string.IsNullOrEmpty(localizedPrice) ? label : label + ": " + localizedPrice;
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

        // D-035-AC-06: after the first play's free Overdrive, the victory card offers to keep it.
        public const string KeepOverdrive = "KEEP OVERDRIVE";

        // Overdrive waits for the next mission, so the panel must say it is on.
        public const string OverdriveActive = "OVERDRIVE ACTIVE";

        // The free retry on the result screens. The mission scenes label it with the paid offer's name, which
        // made Armored Redeploy look like a worse deal than a free button; missions relabel it on load.
        public const string Retry = "RETRY";
        public const string SceneRetryLabel = "REDEPLOY";
        // A retry ends the SCORE run; the failure screen says so under RETRY (D-032-AC-06).
        public const string RetryCaption = "SCORE STARTS OVER";

        /// <summary>How long Overdrive lasts (D-009-AC-03).</summary>
        public const string OverdriveLasts = "UNTIL YOUR TANK GOES DOWN";
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

        public static string Pack(GemPack pack, string localizedPrice, string prefix = "")
        {
            var label = prefix + CountText.Format(pack.Gems) + " GEMS";
            return string.IsNullOrEmpty(localizedPrice) ? label : label + " FOR " + localizedPrice;
        }
    }
}
