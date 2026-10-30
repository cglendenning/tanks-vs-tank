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
        public GemPack(string productId, int gems)
        {
            ProductId = productId;
            Gems = gems;
        }

        public string ProductId { get; }
        public int Gems { get; }
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
            new GemPack("treadshred.gems100", 100),
            new GemPack("treadshred.gems550", 550),
            new GemPack("treadshred.gems1200", 1200)
        };

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
            return null;
        }

        public static OfferPlan Plan(GemOffer offer, int balance)
        {
            var cost = Cost(offer);
            return balance >= cost
                ? new OfferPlan(cost, null)
                : new OfferPlan(cost, SmallestPackCovering(cost - balance));
        }

        public static GemPack SmallestPackCovering(int shortfall)
        {
            for (var i = 0; i < Packs.Count; i++)
            {
                if (Packs[i].Gems >= shortfall)
                    return Packs[i];
            }
            return Packs[Packs.Count - 1];
        }
    }
}
