using System.Collections.Generic;

namespace TreadShred.Progression
{
    /// <summary>
    /// D-032-AC-01: counts each enemy once. An enemy with two colliders, or two Overdrive rounds arriving in the same
    /// frame, reports its death more than once before Unity removes it; only the first report is a kill.
    /// </summary>
    public sealed class MissionKills
    {
        private readonly HashSet<int> killed = new HashSet<int>();

        public int Count => killed.Count;

        /// <summary>True the first time an enemy is reported, false for every repeat.</summary>
        public bool Claim(int enemyId) => killed.Add(enemyId);
    }
}
