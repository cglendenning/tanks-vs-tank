using UnityEngine;

/// <summary>
/// Compatibility facade for the legacy scene and ManagerScore callbacks.
/// The actual implementation lives in TankAdService.
/// </summary>
public sealed class GoogleMobileAdsDemoScript : MonoBehaviour
{
    public bool showOnWin;
    public bool showOnLost;
    public string idfullbaner;

    private static GoogleMobileAdsDemoScript instance;
    public static GoogleMobileAdsDemoScript Instance
    {
        get
        {
            if (instance == null)
            {
                var host = new GameObject("Tank Ads Compatibility");
                instance = host.AddComponent<GoogleMobileAdsDemoScript>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        TankAdService.Ensure();
    }

    public void ShowInterstitial()
    {
        TankAdService.Ensure().TryShowInterstitial();
    }
}
