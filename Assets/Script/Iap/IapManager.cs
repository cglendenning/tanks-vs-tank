using System;
using System.Collections.Generic;
using UnityEngine;

    public class IapManager : MonoBehaviour
    {
        /*IStoreListener*/
    }
        //public static IapManager instance;
        //private static IStoreController m_StoreController;          // The Unity Purchasing system.
        //private static IExtensionProvider m_StoreExtensionProvider; // The store-specific Purchasing subsystems.

    
        //public static string PRODUCT_50_GOLD = "gold50";
        //public static string PRODUCT_100_GOLD = "gold100";
        //public static string PRODUCT_NO_ADS = "noads";

        //// Apple App Store-specific product identifier for the subscription product.
        //private static string kProductNameAppleSubscription = "com.unity3d.subscription.new";

        //// Google Play Store-specific product identifier subscription product.
        //private static string kProductNameGooglePlaySubscription = "com.unity3d.subscription.original";
        //void Awake()
        //{
        //    instance = this;
        //}
        //void Start()
        //{
        //    // If we haven't set up the Unity Purchasing reference
        //    if (m_StoreController == null)
        //    {
        //        // Begin to configure our connection to Purchasing
        //        InitializePurchasing();
        //    }
        //}
        //public void InitializePurchasing()
        //{
        //    // If we have already connected to Purchasing ...
        //    if (IsInitialized())
        //    {
        //        // ... we are done here.
        //        return;
        //    }

        //    // Create a builder, first passing in a suite of Unity provided stores.
        //    var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
          
        //    builder.AddProduct(PRODUCT_50_GOLD, ProductType.Consumable);
        //    builder.AddProduct(PRODUCT_100_GOLD, ProductType.Consumable);
        //    builder.AddProduct(PRODUCT_NO_ADS, ProductType.NonConsumable);
          
        //    UnityPurchasing.Initialize(this, builder);
        //}
        //private bool IsInitialized()
        //{
        //    // Only say we are initialized if both the Purchasing references are set.
        //    return m_StoreController != null && m_StoreExtensionProvider != null;
        //}


        //public void Buy50Gold()
        //{
          
        //    BuyProductID(PRODUCT_50_GOLD);
        //}

        //public void Buy100Gold()
        //{

        //    BuyProductID(PRODUCT_100_GOLD);
        //}

        //public void BuyNoads()
        //{

        //    BuyProductID(PRODUCT_NO_ADS);
        //}

        //void BuyProductID(string productId)
        //{
        //    // If Purchasing has been initialized ...
        //    if (IsInitialized())
        //    {
        //        // ... look up the Product reference with the general product identifier and the Purchasing 
        //        // system's products collection.
        //        Product product = m_StoreController.products.WithID(productId);

        //        // If the look up found a product for this device's store and that product is ready to be sold ... 
        //        if (product != null && product.availableToPurchase)
        //        {
        //            Debug.Log(string.Format("Purchasing product asychronously: '{0}'", product.definition.id));
        //            // ... buy the product. Expect a response either through ProcessPurchase or OnPurchaseFailed 
        //            // asynchronously.
        //            m_StoreController.InitiatePurchase(product);
        //        }
        //        // Otherwise ...
        //        else
        //        {
        //            // ... report the product look-up failure situation  
        //            Debug.Log("BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");
        //        }
        //    }
        //    // Otherwise ...
        //    else
        //    {
        //        // ... report the fact Purchasing has not succeeded initializing yet. Consider waiting longer or 
        //        // retrying initiailization.
        //        Debug.Log("BuyProductID FAIL. Not initialized.");
        //    }
        //}


        //public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        //{
        //    // Purchasing has succeeded initializing. Collect our Purchasing references.
        //    Debug.Log("OnInitialized: PASS");

        //    // Overall Purchasing system, configured with products for this application.
        //    m_StoreController = controller;
        //    // Store specific subsystem, for accessing device-specific store features.
        //    m_StoreExtensionProvider = extensions;
        //}

        //public void OnInitializeFailed(InitializationFailureReason error)
        //{
        //    // Purchasing set-up has not succeeded. Check error for reason. Consider sharing this reason with the user.
        //    Debug.Log("OnInitializeFailed InitializationFailureReason:" + error);
        //}


        //public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        //{
        //    // A consumable product has been purchased by this user.
        //    if (String.Equals(args.purchasedProduct.definition.id, PRODUCT_50_GOLD, StringComparison.Ordinal))
        //    {
        //        Debug.Log("Bạn đã mua 50 vàng");  
        //    }
        
        //    else if (String.Equals(args.purchasedProduct.definition.id, PRODUCT_100_GOLD, StringComparison.Ordinal))
        //    {
        //        Debug.Log("Bạn đã mua 100 vàng");  
        //    }
       
        //    else if (String.Equals(args.purchasedProduct.definition.id, PRODUCT_NO_ADS, StringComparison.Ordinal))
        //    {
        //        Debug.Log("Bạn đã mua không quảng cáo");  
        //    }
        //    else
        //    {
        //        Debug.Log(string.Format("ProcessPurchase: FAIL. Unrecognized product: '{0}'", args.purchasedProduct.definition.id));
        //    }

        //    return PurchaseProcessingResult.Complete;
        //}


        //public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        //{
        //    // A product purchase attempt did not succeed. Check failureReason for more detail. Consider sharing 
        //    // this reason with the user to guide their troubleshooting actions.
        //    Debug.Log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", product.definition.storeSpecificId, failureReason));
        //}
   // }
