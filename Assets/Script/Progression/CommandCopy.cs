using System.Collections.Generic;

namespace TreadShred.Progression
{
    // D-028: replacements for legacy decorative two-part labels baked into
    // the scenes. A "//" separator is kept only where it adds information.
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
            return text != null && LegacyLabels.TryGetValue(text, out var replacement) ? replacement : text;
        }
    }
}
