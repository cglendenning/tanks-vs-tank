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

    // Credited orders not yet confirmed, by transaction ID, so attribution reports each once it is acknowledged.
    private readonly Dictionary<string, (string ProductId, double Price, string Currency)> awaitingAttribution =
        new Dictionary<string, (string ProductId, double Price, string Currency)>();

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
        Wallet.OpenStarterPackWindow(DateTime.UtcNow);
        purchases = new PurchaseFlow(Wallet, this, () => IsAvailable(GemCatalog.StarterPack.ProductId));
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
        return purchases.PlanFor(GemCatalog.Cost(offer));
    }

    /// <summary>What a gem cost spends now, or the pack one tap buys for it, the Starter Pack included (D-037).</summary>
    public OfferPlan PlanFor(int cost)
    {
        return purchases.PlanFor(cost);
    }

    /// <summary>D-037-AC-02: the player has not bought the Starter Pack and the store offers its product.</summary>
    public bool StarterPackOnSale => purchases.StarterPackOnSale;

    /// <summary>D-037-AC-07: the Starter Pack's countdown; zero when it is not on sale.</summary>
    public TimeSpan StarterPackTimeLeft => purchases.StarterPackTimeLeft;

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
        purchases.Request(() => purchases.SpendOrBuy(cost, onApplied, onStatus), onStatus);
    }

    public bool PurchaseInProgress => purchases.InProgress;

    /// <summary>The purchase flow behind every buy button, for services that run their own purchase steps.</summary>
    public PurchaseFlow Purchases => purchases;

    /// <summary>
    /// A screen that started a purchase is closing: a delivery still credits the gems, but spends or applies
    /// nothing for a screen the player has left (D-011).
    /// </summary>
    public void Release(Action<string> onStatus)
    {
        if (purchases.Release(onStatus))
            Debug.Log("[Store] Purchase screen closed; a delivered order will credit its gems only.");
    }

    /// <summary>
    /// Buys a gem pack outright; onCredited runs once the gems are in the wallet.
    /// </summary>
    public void BuyPack(GemPack pack, Action onCredited, Action<string> onStatus)
    {
        purchases.Request(() => purchases.BuyPack(pack, onCredited, onStatus), onStatus);
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

    private bool IsAvailable(string productId)
    {
        var product = FindProduct(productId);
        return product != null && product.availableToPurchase;
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
            .Append(GemCatalog.StarterPack)
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
        {
            TreadShredAnalytics.Log(TreadShred.Analytics.GameEvents.GemPackPurchased(product.definition.id,
                (double)product.metadata.localizedPrice, product.metadata.isoCurrencyCode, order.Info.TransactionID));
            awaitingAttribution[order.Info.TransactionID] =
                (product.definition.id, (double)product.metadata.localizedPrice, product.metadata.isoCurrencyCode);
        }
        storeController.ConfirmPurchase(order);
    }

    private void OnPurchaseConfirmed(Order order)
    {
        if (order is FailedOrder failed)
        {
            // Gems were already credited; the store will redeliver the order
            // and the wallet's transaction history prevents a double credit.
            // The order stays awaiting attribution until a confirmation succeeds.
            Debug.LogWarning("[Store] Purchase confirmation failed: " + failed.FailureReason + " " + failed.Details);
            return;
        }

        // D-023-AC-02: attribution hears of a credited purchase only once the store has it acknowledged.
        if (awaitingAttribution.TryGetValue(order.Info.TransactionID, out var credited))
        {
            awaitingAttribution.Remove(order.Info.TransactionID);
            TreadShredAttribution.PurchaseConfirmed(credited.ProductId, credited.Price, credited.Currency, order.Info.Receipt);
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
