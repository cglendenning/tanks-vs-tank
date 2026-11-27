using System;
using System.Collections.Generic;

namespace TreadShred.Economy
{
    // Public fields so Unity's JsonUtility can persist it.
    [Serializable]
    public sealed class WalletState
    {
        public int balance;
        public bool starterGranted;
        public List<string> creditedTransactions = new List<string>();

        // D-037: the Starter Pack was bought (it is never offered again), and when its window opened (UTC ticks; 0 = not yet).
        public bool starterPackBought;
        public long starterPackWindowStartTicks;
        // D-037-AC-04: its interstitial has opened by itself once; after that only its result-screen callout opens it.
        public bool starterPackShown;
        // D-037-AC-08: the free test drives of it the player has taken.
        public int starterPackTestDrives;
        // D-047: the War Chest was bought at least once; it stays on sale (D-047-AC-02).
        public bool warChestBought;
        // D-047-AC-08: the player has seen the War Chest, so every result screen keeps it one tap away.
        public bool warChestSeen;
    }

    public interface IWalletStore
    {
        WalletState Load();
        void Save(WalletState state);
    }

    public enum CreditResult
    {
        Credited,
        Duplicate,
        UnknownProduct
    }

    public sealed class GemWallet
    {
        // Stores redeliver only unconfirmed orders, so a short history of
        // recent transaction IDs is enough to stop double credits.
        public const int RememberedTransactionLimit = 64;

        private readonly IWalletStore store;
        internal readonly WalletState state;

        public GemWallet(IWalletStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            state = store.Load() ?? new WalletState();
            if (state.creditedTransactions == null)
                state.creditedTransactions = new List<string>();
        }

        public event Action<int> BalanceChanged;

        public int Balance => state.balance;

        public bool StarterPackBought => state.starterPackBought;

        /// <summary>D-037-AC-04: the Starter Pack's interstitial has already opened by itself.</summary>
        public bool StarterPackShown => state.starterPackShown;

        public void MarkStarterPackShown()
        {
            if (state.starterPackShown)
                return;
            state.starterPackShown = true;
            store.Save(state);
        }

        /// <summary>D-037-AC-08: the Starter Pack test drives taken so far.</summary>
        public int StarterPackTestDrives => state.starterPackTestDrives;

        public void CountStarterPackTestDrive()
        {
            state.starterPackTestDrives++;
            store.Save(state);
        }
        public bool WarChestBought => state.warChestBought;
        public bool WarChestSeen => state.warChestSeen;

        /// <summary>D-047-AC-08: the War Chest has been revealed to the player; saved once.</summary>
        public void MarkWarChestSeen()
        {
            if (state.warChestSeen)
                return;
            state.warChestSeen = true;
            store.Save(state);
        }

        /// <summary>D-037-AC-02: opens the Starter Pack's window at the player's first launch that sells it; never reopens it.</summary>
        public void OpenStarterPackWindow(DateTime utcNow)
        {
            if (state.starterPackWindowStartTicks != 0)
                return;
            state.starterPackWindowStartTicks = utcNow.Ticks;
            store.Save(state);
        }

        /// <summary>
        /// D-037-AC-02, D-037-AC-07: time left to buy the Starter Pack: zero once it is bought, before its window opens and
        /// after it ends. A device clock set back never shows more than the whole window.
        /// </summary>
        public TimeSpan StarterPackTimeLeft(DateTime utcNow)
        {
            if (state.starterPackBought || state.starterPackWindowStartTicks == 0)
                return TimeSpan.Zero;
            var left = new DateTime(state.starterPackWindowStartTicks, DateTimeKind.Utc) + GemCatalog.StarterPackWindow - utcNow;
            if (left <= TimeSpan.Zero)
                return TimeSpan.Zero;
            return left > GemCatalog.StarterPackWindow ? GemCatalog.StarterPackWindow : left;
        }

        public bool GrantStarterIfNeeded()
        {
            if (state.starterGranted)
                return false;

            state.starterGranted = true;
            state.balance += GemCatalog.StarterGrant;
            Commit();
            return true;
        }

        public CreditResult CreditPurchase(string productId, string transactionId)
        {
            var pack = GemCatalog.FindPack(productId);
            if (pack == null)
                return CreditResult.UnknownProduct;

            if (!string.IsNullOrEmpty(transactionId))
            {
                if (state.creditedTransactions.Contains(transactionId))
                    return CreditResult.Duplicate;

                state.creditedTransactions.Add(transactionId);
                if (state.creditedTransactions.Count > RememberedTransactionLimit)
                    state.creditedTransactions.RemoveAt(0);
            }

            // D-037-AC-01: a paid order always credits, even a second Starter Pack; buying it ends the offer.
            if (GemCatalog.IsStarterPack(pack))
                state.starterPackBought = true;
            // D-047-AC-02: so is the War Chest, whose gems the progression turns into its supply at once.
            if (GemCatalog.IsWarChest(pack))
                state.warChestBought = true;
            state.balance += pack.Gems;
            Commit();
            return CreditResult.Credited;
        }

        // Gems earned in play (milestones); the caller guarantees each reward is paid once.
        public void CreditReward(int gems) => Credit(gems);

        // Gems an undone upgrade spent, returned to the wallet (D-017-AC-06).
        public void Refund(int gems) => Credit(gems);

        private void Credit(int gems)
        {
            if (gems <= 0)
                throw new ArgumentOutOfRangeException(nameof(gems), gems, "Gems credited must be positive.");

            state.balance += gems;
            Commit();
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Spend amount must be positive.");
            if (state.balance < amount)
                return false;

            state.balance -= amount;
            Commit();
            return true;
        }

        /// <summary>D-025-AC-03: takes a restored save as a whole, replacing what is held now.</summary>
        public void Replace(WalletState restored)
        {
            if (restored == null)
                throw new ArgumentNullException(nameof(restored));
            state.balance = restored.balance;
            state.starterGranted = restored.starterGranted;
            state.creditedTransactions = new List<string>(restored.creditedTransactions ?? new List<string>());
            state.starterPackBought = restored.starterPackBought;
            state.starterPackShown = restored.starterPackShown;
            state.starterPackTestDrives = restored.starterPackTestDrives;
            state.warChestBought = restored.warChestBought;
            state.warChestSeen = restored.warChestSeen;
            // A save from before the Starter Pack has no window: the one this install opened stands, so a reinstall that
            // restores a newer save adopts its original window and never reopens it (D-037-AC-02).
            if (restored.starterPackWindowStartTicks != 0)
                state.starterPackWindowStartTicks = restored.starterPackWindowStartTicks;
            Commit();
        }

        private void Commit()
        {
            store.Save(state);
            BalanceChanged?.Invoke(state.balance);
        }
    }
}
