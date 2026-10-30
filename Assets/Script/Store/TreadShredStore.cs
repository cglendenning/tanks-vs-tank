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
public sealed class TreadShredStore : MonoBehaviour, IPurchaseGateway
{
    public static TreadShredStore Instance { get; private set; }

    public GemWallet Wallet { get; private set; }

    private StoreController storeController;
    private PurchaseFlow purchases;

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
        purchases = new PurchaseFlow(Wallet, this);
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
        SpendOrBuy(GemCatalog.Cost(offer), onApplied, onStatus);
    }

    /// <summary>
    /// Spends a gem cost, or buys the smallest covering pack and then spends,
    /// so any gem purchase (boost, revive, upgrade top-up) is one tap.
    /// </summary>
    public void SpendOrBuy(int cost, Action onApplied, Action<string> onStatus)
    {
        purchases.SpendOrBuy(cost, onApplied, onStatus);
    }

    public bool PurchaseInProgress => purchases.InProgress;

    /// <summary>
    /// Buys a gem pack outright; onCredited runs once the gems are in the wallet.
    /// </summary>
    public void BuyPack(GemPack pack, Action onCredited, Action<string> onStatus)
    {
        purchases.BuyPack(pack, onCredited, onStatus);
    }

    bool IPurchaseGateway.TryStartPurchase(string productId)
    {
        var product = FindProduct(productId);
        if (product == null || !product.availableToPurchase)
        {
            Debug.LogWarning("[Store] Gem pack unavailable: " + productId);
            return false;
        }

        storeController.PurchaseProduct(product);
        return true;
    }

    // Purchase callbacks belong to the scene that requested them.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        purchases.Abandon();
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

        if (purchases.OrderDelivered(product.definition.id, order.Info.TransactionID, out var result) ==
            OrderOutcome.LeaveUnconfirmed)
        {
            // Unconfirmed orders are refunded by the store instead of being
            // consumed without delivering anything.
            Debug.LogError("[Store] Unknown product purchased, not confirming: " + product.definition.id);
            return;
        }

        Debug.Log("[Store] " + result + " " + product.definition.id + "; balance " + Wallet.Balance);
        if (result == CreditResult.Credited)
            TreadShredAnalytics.Log(TreadShred.Analytics.GameEvents.GemPackPurchased(product.definition.id,
                (double)product.metadata.localizedPrice, product.metadata.isoCurrencyCode, order.Info.TransactionID));
        storeController.ConfirmPurchase(order);
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
        purchases.PurchaseFailed(order.FailureReason == PurchaseFailureReason.UserCancelled);
    }

    private void OnPurchaseDeferred(DeferredOrder order)
    {
        Debug.Log("[Store] Purchase awaiting approval.");
        purchases.PurchaseDeferred();
    }
}
