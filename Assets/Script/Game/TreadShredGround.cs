using TreadShred.Progression;
using UnityEngine;

/// <summary>D-039: lays the mission's ground over the arena floor as the mission loads.</summary>
public static class TreadShredGround
{
    public const string FloorPath = "Manager/PlaneGround";

    /// <summary>Returns the ground applied (1 to 8), or 0 when the floor or its texture is missing and the scene's floor stays.</summary>
    public static int Apply(int mission)
    {
        var floor = GameObject.Find(FloorPath);
        var renderer = floor != null ? floor.GetComponent<Renderer>() : null;
        if (renderer == null)
        {
            Debug.LogError("[Ground] " + FloorPath + " not found; the mission keeps its scene floor.");
            return 0;
        }

        var ground = GroundRules.GroundFor(mission);
        var texture = Resources.Load<Texture2D>(GroundRules.ResourcePath(ground));
        if (texture == null)
        {
            Debug.LogError("[Ground] Missing " + GroundRules.ResourcePath(ground) + "; the mission keeps its scene floor.");
            return 0;
        }

        var material = renderer.material;
        material.mainTexture = texture;
        material.mainTextureScale = new Vector2(GroundRules.TilingX, GroundRules.TilingY);
        return ground;
    }
}
