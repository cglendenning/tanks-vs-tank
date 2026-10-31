package com.tgts.treadshred;

import android.app.Activity;
import android.content.pm.ApplicationInfo;
import android.content.pm.PackageManager;
import android.os.Bundle;
import android.util.Log;
import com.tenjin.android.TenjinSDK;
import com.unity3d.player.UnityPlayer;
import java.util.Map;

/**
 * D-023: Tenjin attribution for Tread Shred on Android. Tenjin credits each install and verified purchase to the
 * AppLovin campaign that drove it and forwards both to AppLovin. The SDK key is written into the manifest at build
 * time from TREAD_SHRED_TENJIN_SDK_KEY (Editor/TenjinAndroidBuild.cs); a build without it reports nothing.
 */
public final class TreadShredTenjin {
    private static final String TAG = "TreadShredTenjin";
    private static final String RECEIVER = "TreadShredAttribution";
    private static final String KEY_META_DATA = "com.tgts.treadshred.TENJIN_SDK_KEY";

    private static TenjinSDK tenjin;

    private TreadShredTenjin() {}

    public static synchronized void start(Activity activity) {
        if (tenjin != null) {
            return;
        }
        String sdkKey = sdkKey(activity);
        if (sdkKey == null || sdkKey.isEmpty()) {
            Log.w(TAG, "No Tenjin SDK key in this build; attribution is off");
            return;
        }
        tenjin = TenjinSDK.getInstance(activity.getApplicationContext(), sdkKey);
        tenjin.setAppStore(TenjinSDK.AppStoreType.googleplay);
        // Purchases are reported by the game once confirmed, so Tenjin's own billing tracker must not count them too.
        tenjin.disableAutoPurchaseTracking();
        tenjin.connect();
    }

    public static synchronized void connect() {
        if (tenjin != null) {
            tenjin.connect();
        }
    }

    public static synchronized void transaction(String productId, String currency, double unitPrice,
                                                String purchaseData, String signature) {
        if (tenjin == null) {
            Log.w(TAG, "Tenjin is off; purchase not reported: " + productId);
            return;
        }
        tenjin.transaction(productId, currency, 1, unitPrice, purchaseData, signature);
    }

    // Answers RECEIVER.OnAttribution with "ad_network\tcampaign_name\tcampaign_id"; empty fields until attributed.
    public static synchronized void requestAttribution() {
        if (tenjin == null) {
            return;
        }
        tenjin.getAttributionInfo(info -> {
            try {
                UnityPlayer.UnitySendMessage(RECEIVER, "OnAttribution",
                        field(info, "ad_network") + "\t" + field(info, "campaign_name") + "\t" + field(info, "campaign_id"));
            } catch (RuntimeException error) {
                Log.w(TAG, "Attribution answer not delivered", error);
            }
        });
    }

    private static String field(Map<String, String> info, String key) {
        String value = info == null ? null : info.get(key);
        return value == null ? "" : value.replace('\t', ' ');
    }

    private static String sdkKey(Activity activity) {
        try {
            ApplicationInfo app = activity.getPackageManager()
                    .getApplicationInfo(activity.getPackageName(), PackageManager.GET_META_DATA);
            Bundle meta = app.metaData;
            return meta == null ? null : meta.getString(KEY_META_DATA);
        } catch (PackageManager.NameNotFoundException error) {
            Log.w(TAG, "Cannot read the Tenjin SDK key", error);
            return null;
        }
    }
}
