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
        private readonly WalletState state;

        public GemWallet(IWalletStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            state = store.Load() ?? new WalletState();
            if (state.creditedTransactions == null)
                state.creditedTransactions = new List<string>();
        }

        public event Action<int> BalanceChanged;

        public int Balance => state.balance;

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

            state.balance += pack.Gems;
            Commit();
            return CreditResult.Credited;
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

        private void Commit()
        {
            store.Save(state);
            BalanceChanged?.Invoke(state.balance);
        }
    }
}
