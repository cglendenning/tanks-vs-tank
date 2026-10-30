using System.Collections.Generic;

namespace TreadShred.Progression
{
    // D-028: replacements for legacy decorative two-part labels baked into the
    // scenes. Any other "//" separator becomes a colon (D-028-AC-03).
    public static class CommandCopy
    {
        public static readonly IReadOnlyDictionary<string, string> LegacyLabels = new Dictionary<string, string>
        {
            { "ARMORED COMBAT // LIVE FIRE RANGE", "ARMORED COMBAT" },
            { "MISSION SELECT // OPERATIONS", "MISSION SELECT" },
            { "SELECT AN OPERATION // 01—25", "SELECT A MISSION" },
            { "TOTAL SCORE // CONFIRMED KILLS // RANK", "SCORE, KILLS AND RANK" },
            { "PAUSED // HOLD", "PAUSED" }
        };

        public static string Replace(string text)
        {
            if (text == null)
                return null;
            if (LegacyLabels.TryGetValue(text, out var replacement))
                return replacement;
            return text.Replace(" // ", ": ").Replace(" //", ":");
        }
    }
}
