using System;
using System.Collections.Generic;

namespace TreadShred.Economy
{
    public enum GemOffer
    {
        ArmoredRedeploy,
        Overdrive
    }

    public sealed class GemPack
    {
        public GemPack(string productId, int gems, int usdCents)
        {
            ProductId = productId;
            Gems = gems;
            UsdCents = usdCents;
        }

        public string ProductId { get; }
        public int Gems { get; }

        // The reference US price tier set in both store consoles (R-GEM-PACKS); players pay local prices.
        public int UsdCents { get; }
    }

    public readonly struct OfferPlan
    {
        public OfferPlan(int cost, GemPack packToBuy)
        {
            Cost = cost;
            PackToBuy = packToBuy;
        }

        public int Cost { get; }

        // Null when the current balance already covers the cost.
        public GemPack PackToBuy { get; }

        public bool CanSpendNow => PackToBuy == null;
    }

    public static class GemCatalog
    {
        // One-time grant so a new player can try a boost before ever paying.
        public const int StarterGrant = 100;

        // Consumable product IDs. They must match App Store Connect and the
        // Google Play Console exactly. Ordered smallest to largest.
        public static readonly IReadOnlyList<GemPack> Packs = new[]
        {
            new GemPack("treadshred.gems100", 100, 99),
            new GemPack("treadshred.gems550", 550, 499),
            new GemPack("treadshred.gems1200", 1200, 999)
        };

        // D-037: the one-time Starter Pack (R-STARTER-PACK). Kept out of Packs so only a caller that knows it is on
        // sale ever chooses it.
        public static readonly GemPack StarterPack = new GemPack("treadshred.starter300", 300, 99);

        // R-STARTER-PACK offer_minutes: how long after the player's first launch the Starter Pack is on sale.
        public static readonly TimeSpan StarterPackWindow = TimeSpan.FromMinutes(15);

        public static bool IsStarterPack(GemPack pack)
        {
            return pack != null && string.Equals(pack.ProductId, StarterPack.ProductId, StringComparison.Ordinal);
        }

        public static int Cost(GemOffer offer)
        {
            switch (offer)
            {
                case GemOffer.ArmoredRedeploy:
                    return 40;
                case GemOffer.Overdrive:
                    return 60;
                default:
                    throw new ArgumentOutOfRangeException(nameof(offer), offer, "Unknown gem offer.");
            }
        }

        public static GemPack FindPack(string productId)
        {
            for (var i = 0; i < Packs.Count; i++)
            {
                if (string.Equals(Packs[i].ProductId, productId, StringComparison.Ordinal))
                    return Packs[i];
            }
            return string.Equals(StarterPack.ProductId, productId, StringComparison.Ordinal) ? StarterPack : null;
        }

        public static OfferPlan Plan(GemOffer offer, int balance, bool starterOnSale = false)
        {
            return PlanFor(Cost(offer), balance, starterOnSale);
        }

        // Spend directly when the balance covers the gem cost, otherwise buy
        // the smallest pack that covers the shortfall.
        public static OfferPlan PlanFor(int cost, int balance, bool starterOnSale = false)
        {
            return balance >= cost
                ? new OfferPlan(cost, null)
                : new OfferPlan(cost, SmallestPackCovering(cost - balance, starterOnSale));
        }

        // D-037-AC-04: while the Starter Pack is on sale it is the pack for any shortfall it covers, so no button
        // sells fewer gems for the same price.
        public static GemPack SmallestPackCovering(int shortfall, bool starterOnSale = false)
        {
            if (starterOnSale && StarterPack.Gems >= shortfall)
                return StarterPack;
            for (var i = 0; i < Packs.Count; i++)
            {
                if (Packs[i].Gems >= shortfall)
                    return Packs[i];
            }
            return Packs[Packs.Count - 1];
        }
    }
}
