using System;

namespace TreadShred.Progression
{
    /// <summary>
    /// D-017-AC-09: the HUD shields are the last hits of armor. They go out one at a time in line, starting beside
    /// +N (place 0), so the lit shields are always the last in line, however fast the hits come.
    /// </summary>
    public static class ShieldRules
    {
        public const float DimAlpha = 0.15f;

        public static int Lit(int armorLeft, int shields) => Math.Max(0, Math.Min(armorLeft, shields));

        public static bool IsLit(int place, int armorLeft, int shields) => place >= shields - Lit(armorLeft, shields);
    }
}
