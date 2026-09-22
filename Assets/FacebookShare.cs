using UnityEngine;

/// <summary>
/// Compatibility surface for old scene references. The legacy native
/// Facebook SDK was removed; sharing is disabled until a current provider is
/// deliberately configured for both platforms.
/// </summary>
public sealed class FacebookShare : MonoBehaviour
{
    public static string GAME_NAME = "Tanks VS Tank";
    public static string APP_ID = string.Empty;

    private void Start()
    {
        Debug.Log("[Social] Legacy Facebook sharing is disabled in the modern build.");
    }

    public static void postLinkToFacebook(string description)
    {
        Debug.LogWarning("[Social] Facebook sharing is unavailable: the legacy native SDK was removed.");
    }
}
