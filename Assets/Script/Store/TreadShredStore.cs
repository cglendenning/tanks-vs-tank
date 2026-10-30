using System;
using System.Collections.Generic;
using System.Linq;
using TreadShred.Economy;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.SceneManagement;

/// <summary>
/// Bridges the gem wallet to the platform store through Unity IAP:
/// StoreKit on iOS, Google Play Billing on Android.
/// </summary>
public sealed class TreadShredStore : MonoBehaviour
{
    private sealed class PendingOffer
    {
        public GemOffer Offer;
        public Action OnApplied;
        public Action<string> OnStatus;
    }

    public static TreadShredStore Instance { get; private set; }

    public GemWallet Wallet { get; private set; }

    private StoreController storeController;
    private PendingOffer pendingOffer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        Ensure();
    }

    public static TreadShredStore Ensure()
    {
        if (Instance != null)
            return Instance;

        var host = new GameObject("Tread Shred Store");
        return host.AddComponent<TreadShredStore>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Wallet = new GemWallet(new PlayerPrefsWalletStore());
        Wallet.GrantStarterIfNeeded();
        SceneManager.sceneLoaded += OnSceneLoaded;
        ConnectStore();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    public OfferPlan PlanFor(GemOffer offer)
    {
        return GemCatalog.Plan(offer, Wallet.Balance);
    }

    public string PriceFor(GemPack pack)
    {
        var product = FindProduct(pack.ProductId);
        return product != null ? product.metadata.localizedPriceString : null;
    }

    /// <summary>
    /// Spends gems on the offer, or buys the smallest gem pack that covers it
    /// and then spends, so the player taps once either way.
    /// </summary>
    public void RedeemOffer(GemOffer offer, Action onApplied, Action<string> onStatus)
    {
        if (pendingOffer != null)
        {
            onStatus("PURCHASE IN PROGRESS");
            return;
        }

        var plan = PlanFor(offer);
        if (plan.CanSpendNow)
        {
            if (Wallet.TrySpend(plan.Cost))
                onApplied();
            else
                onStatus("NOT ENOUGH GEMS");
            return;
        }

        var product = FindProduct(plan.PackToBuy.ProductId);
        if (product == null || !product.availableToPurchase)
        {
            Debug.LogWarning("[Store] Gem pack unavailable: " + plan.PackToBuy.ProductId);
            onStatus("STORE UNAVAILABLE // TRY AGAIN SOON");
            return;
        }

        pendingOffer = new PendingOffer { Offer = offer, OnApplied = onApplied, OnStatus = onStatus };
        storeController.PurchaseProduct(product);
    }

    // Offer callbacks belong to the scene that requested them.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        pendingOffer = null;
    }

    private Product FindProduct(string productId)
    {
        return storeController?.GetProducts().FirstOrDefault(p => p.definition.id == productId);
    }

    private async void ConnectStore()
    {
        storeController = UnityIAPServices.StoreController();
        storeController.OnStoreConnected += OnStoreConnected;
        storeController.OnStoreDisconnected += OnStoreDisconnected;
        storeController.OnProductsFetched += OnProductsFetched;
        storeController.OnProductsFetchFailed += OnProductsFetchFailed;
        storeController.OnPurchasesFetched += OnPurchasesFetched;
        storeController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
        storeController.OnPurchasePending += OnPurchasePending;
        storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
        storeController.OnPurchaseFailed += OnPurchaseFailed;
        storeController.OnPurchaseDeferred += OnPurchaseDeferred;
        // Orders paid for but never confirmed (crash, kill) come back through
        // OnPurchasePending once purchases are fetched.
        storeController.ProcessPendingOrdersOnPurchasesFetched(true);

        try
        {
            await storeController.Connect();
        }
        catch (Exception error)
        {
            Debug.LogError("[Store] Could not connect to the store: " + error.Message);
        }
    }

    private void OnStoreConnected()
    {
        storeController.FetchProducts(GemCatalog.Packs
            .Select(pack => new ProductDefinition(pack.ProductId, ProductType.Consumable))
            .ToList());
    }

    private void OnStoreDisconnected(StoreConnectionFailureDescription description)
    {
        Debug.LogWarning("[Store] Disconnected: " + description.message);
    }

    private void OnProductsFetched(List<Product> products)
    {
        Debug.Log("[Store] Fetched " + products.Count + " gem packs.");
        storeController.FetchPurchases();
    }

    private void OnProductsFetchFailed(ProductFetchFailed failure)
    {
        Debug.LogWarning("[Store] Gem pack fetch failed for " + failure.FailedFetchProducts.Count +
                         " products: " + failure.FailureReason);
    }

    private void OnPurchasesFetched(Orders orders)
    {
        Debug.Log("[Store] Existing purchases fetched.");
    }

    private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
    {
        Debug.LogWarning("[Store] Existing purchase fetch failed: " + failure.message);
    }

    private void OnPurchasePending(PendingOrder order)
    {
        var product = order.CartOrdered.Items().FirstOrDefault()?.Product;
        if (product == null)
        {
            Debug.LogError("[Store] Pending order has no product; leaving it unconfirmed.");
            return;
        }

        var result = Wallet.CreditPurchase(product.definition.id, order.Info.TransactionID);
        if (result == CreditResult.UnknownProduct)
        {
            // Unconfirmed orders are refunded by the store instead of being
            // consumed without delivering anything.
            Debug.LogError("[Store] Unknown product purchased, not confirming: " + product.definition.id);
            return;
        }

        Debug.Log("[Store] " + result + " " + product.definition.id + "; balance " + Wallet.Balance);
        storeController.ConfirmPurchase(order);
        FulfillPendingOffer();
    }

    private void FulfillPendingOffer()
    {
        if (pendingOffer == null)
            return;

        var offer = pendingOffer;
        pendingOffer = null;
        if (Wallet.TrySpend(GemCatalog.Cost(offer.Offer)))
            offer.OnApplied();
        else
            offer.OnStatus("NOT ENOUGH GEMS");
    }

    private void OnPurchaseConfirmed(Order order)
    {
        if (order is FailedOrder failed)
        {
            // Gems were already credited; the store will redeliver the order
            // and the wallet's transaction history prevents a double credit.
            Debug.LogWarning("[Store] Purchase confirmation failed: " + failed.FailureReason + " " + failed.Details);
        }
    }

    private void OnPurchaseFailed(FailedOrder order)
    {
        Debug.LogWarning("[Store] Purchase failed: " + order.FailureReason + " " + order.Details);
        if (pendingOffer == null)
            return;

        var onStatus = pendingOffer.OnStatus;
        pendingOffer = null;
        onStatus(order.FailureReason == PurchaseFailureReason.UserCancelled
            ? "PURCHASE CANCELLED"
            : "PURCHASE FAILED // TRY AGAIN");
    }

    private void OnPurchaseDeferred(DeferredOrder order)
    {
        Debug.Log("[Store] Purchase awaiting approval.");
        if (pendingOffer == null)
            return;

        var onStatus = pendingOffer.OnStatus;
        pendingOffer = null;
        onStatus("WAITING FOR APPROVAL // GEMS ARRIVE WHEN APPROVED");
    }
}
